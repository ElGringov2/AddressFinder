using System;
using AddressFinder.Application.Services;
using AddressFinder.Domain;
using AddressFinder.Infrastructure.Contexts;
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

    public async Task<IEnumerable<Address>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<Address>();
        }

        var keywords = query.Trim().ToLower()
            .Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(k => k.Length >= 2)
            .ToList();

        if (!keywords.Any())
        {
            return Enumerable.Empty<Address>();
        }

        // D'abord filtrer avec Entity Framework
        var queryable = _dbSet.AsQueryable();

        foreach (var keyword in keywords)
        {
            queryable = queryable.Where(a =>
                EF.Functions.Like(a.Street.ToLower(), $"%{keyword}%") ||
                EF.Functions.Like(a.City.ToLower(), $"%{keyword}%") ||
                EF.Functions.Like(a.ZipCode.ToLower(), $"%{keyword}%") ||
                EF.Functions.Like(a.Number.ToLower(), $"%{keyword}%") ||
                EF.Functions.Like(a.Country.ToLower(), $"%{keyword}%"));
        }

        // Récupérer les résultats en mémoire
        var filteredAddresses = await queryable.ToListAsync(cancellationToken);

        // Puis calculer la pertinence en mémoire et trier
        var scoredAddresses = filteredAddresses
            .Select(address => new
            {
                Address = address,
                Relevance = CalculateRelevance(address, keywords)
            })
            .OrderByDescending(x => x.Relevance)
            .ThenBy(x => x.Address.City)
            .ThenBy(x => x.Address.Street)
            .Take(50)
            .Select(x => x.Address);

        return scoredAddresses;
    }

    private static int CalculateRelevance(Address address, List<string> keywords)
    {
        int score = 0;

        foreach (var keyword in keywords)
        {
            var lowerKeyword = keyword.ToLower();

            if (address.Street.ToLower().Contains(lowerKeyword))
                score += 10;
            if (address.City.ToLower().Contains(lowerKeyword))
                score += 8;
            if (address.ZipCode.ToLower().Contains(lowerKeyword))
                score += 6;
            if (address.Number.ToLower().Contains(lowerKeyword))
                score += 4;
            if (address.Country.ToLower().Contains(lowerKeyword))
                score += 2;
        }

        return score;
    }
}
