using System;
using System.Linq.Expressions;
using AddressFinder.Application.Services;
using AddressFinder.Domain;
using AddressFinder.Domain.Extensions;
using AddressFinder.Infrastructure.Contexts;
using LinqKit;
using LinqKit.Core;
using Microsoft.EntityFrameworkCore;

namespace AddressFinder.Infrastructure.Repositories;

public class AddressRepository(AddressDbContext context)
: IAddressRepository
{
    private readonly DbSet<Address> _dbSet = context.Set<Address>();
    public async Task AddRangeAsync(IEnumerable<Address> adresses, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(adresses, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(string? country, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(a => country == null || a.Country == country)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await _dbSet
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IEnumerable<Address>> SearchAsync(string searchTerms, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerms)) return [];

        // 1) Tokens normalisés côté client (même logique que les colonnes *Norm)
        var tokens = searchTerms
            .Split([' ', ',', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(TextNormalizer.NormalizeForSearch)
            .Where(t => t.Length >= 1)     // on garde "1"
            .Distinct()
            .ToList();

        if (tokens.Count == 0) return [];

        // Helpers PRE‑CALC (ne pas appeler dans lambdas EF)
        static string Singular(string t) =>
            (t.Length > 3 && t.EndsWith("s", StringComparison.Ordinal)) ? t[..^1] : t;
        static int CityCap(string t) => t.Length <= 5 ? 1 : 2;
        static int WordCap(string t) => t.Length <= 3 ? 1 : 2;
        static bool IsAllLetters(string t) => t.All(char.IsLetter);

        // 2) Détection ville: dernier token alphabétique suffisamment long
        string? cityHint = tokens.LastOrDefault(t => IsAllLetters(t) && t.Length >= 4);
        var otherTokens = (cityHint is null) ? tokens : tokens.Where(t => t != cityHint).ToList();

        // 3) Contraintes "must" (AND pour tous les tokens)
        //    - City: si cityHint connu, on le force
        //    - Chaque autre token doit matcher au moins un champ (OR interne)
        var predicate = PredicateBuilder.New<Address>(true);

        if (!string.IsNullOrEmpty(cityHint))
        {
            var c = cityHint;
            var cap = CityCap(c);
            // City: LIKE mot entier OU DL capped
            predicate = predicate.And(a =>
                // word-boundary approx: concat pour entourer de spaces
                (" " + (a.CityNorm ?? "") + " ").Contains(" " + c + " ") ||
                SqliteFunctionProvider.DamerauLevenshteinDistanceCapped((a.CityNorm ?? ""), c, cap) <= cap
            );
        }

        foreach (var raw in otherTokens)
        {
            var t = raw;
            var ts = Singular(t);
            var wc = WordCap(t);

            var any = PredicateBuilder.New<Address>(false);

            // Street: mot entier (contains avec espaces) + variante singulier + DL capped
            any = any
                .Or(a => (" " + (a.StreetNorm ?? "") + " ").Contains(" " + t + " "))
                .Or(a => t != ts && (" " + (a.StreetNorm ?? "") + " ").Contains(" " + ts + " "))
                .Or(a => SqliteFunctionProvider.DamerauLevenshteinDistanceCapped((a.StreetNorm ?? ""), t, wc) <= wc);

            // Number: égal ou préfixe exact
            any = any
                .Or(a => (a.Number ?? "") == t)
                .Or(a => (a.Number ?? "").StartsWith(t));

            // Zip: si 5 chiffres → préfixe; sinon fallback contains
            if (t.Length == 5 && t.All(char.IsDigit))
                any = any.Or(a => (a.ZipCode ?? "").StartsWith(t));
            else
                any = any.Or(a => (a.ZipCode ?? "").Contains(t)); // rare

            // Country (norm)
            any = any.Or(a => (" " + (a.CountryNorm ?? "") + " ").Contains(" " + t + " "));

            // Optionnel: City peut aussi contribuer pour les tokens non-ville
            any = any.Or(a => (" " + (a.CityNorm ?? "") + " ").Contains(" " + t + " "));

            predicate = predicate.And(any);
        }

        // 4) Base query
        var baseQuery = _dbSet
            .AsNoTracking()
            .Where(predicate);

        // 5) Tri: DL sur colonnes Norm (cap pré-calculé)
        var first = tokens[0];
        var firstCityCap = CityCap(first);
        var firstWordCap = WordCap(first);

        // Phase stricte (même predicate)
        var strict = await baseQuery
            .OrderBy(a => SqliteFunctionProvider.DamerauLevenshteinDistanceCapped((a.CityNorm ?? ""), first, firstCityCap))
            .ThenBy(a => SqliteFunctionProvider.DamerauLevenshteinDistanceCapped((a.StreetNorm ?? ""), first, firstWordCap))
            .ThenBy(a => (a.ZipCode ?? ""))
            .Take(10)
            .ToListAsync(ct);

        if (strict.Count > 0) return strict;

        // 6) Fallback “relax” MAIS on conserve l’AND sur TOUS les tokens
        //    On ajoute seulement des variantes plus souples (Contains sans word‑boundary + DL plus généreux)
        var relaxed = PredicateBuilder.New<Address>(true);

        if (!string.IsNullOrEmpty(cityHint))
        {
            var c = cityHint;
            var cap = Math.Min(3, CityCap(c) + 1); // cap un peu plus large
            relaxed = relaxed.And(a =>
                (a.CityNorm ?? "").Contains(c) ||
                SqliteFunctionProvider.DamerauLevenshteinDistanceCapped((a.CityNorm ?? ""), c, cap) <= cap
            );
        }

        foreach (var raw in otherTokens)
        {
            var t = raw;
            var ts = Singular(t);
            var cap = Math.Min(3, WordCap(t) + 1);

            var any = PredicateBuilder.New<Address>(false);

            any = any
                .Or(a => (a.StreetNorm ?? "").Contains(t))
                .Or(a => t != ts && (a.StreetNorm ?? "").Contains(ts))
                .Or(a => SqliteFunctionProvider.DamerauLevenshteinDistanceCapped((a.StreetNorm ?? ""), t, cap) <= cap)
                .Or(a => (a.Number ?? "") == t || (a.Number ?? "").StartsWith(t))
                .Or(a => (a.ZipCode ?? "").Contains(t))
                .Or(a => (a.CountryNorm ?? "").Contains(t))
                .Or(a => (a.CityNorm ?? "").Contains(t));

            relaxed = relaxed.And(any);
        }

        var relaxedRes = await _dbSet
            .AsNoTracking()
            .Where(relaxed)
            .OrderBy(a => a.CityNorm)
            .ThenBy(a => a.StreetNorm)
            .ThenBy(a => a.ZipCode)
            .Take(10)
            .ToListAsync(ct);

        return relaxedRes;
    }
}


internal static class SearchHelpers
{
    public static string SingularOrSame(string t)
        => (t.Length > 3 && t.EndsWith("s", StringComparison.Ordinal)) ? t[..^1] : t;
    public static int CityCap(string t) => t.Length <= 5 ? 1 : 2;
    public static int WordCap(string t) => t.Length <= 3 ? 1 : 2;
}