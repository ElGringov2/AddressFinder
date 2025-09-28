using System;
using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AddressFinder.Infrastructure.Contexts;

public class AddressDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = "Data Source=addresses.db";
            var connection = new SqliteConnection(connectionString);
            
            SqliteFunctionProvider.RegisterFunctions(connection);
            
            optionsBuilder.UseSqlite(connection);
        }
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AddressDbContext).Assembly);
modelBuilder.HasDbFunction(typeof(SqliteFunctionProvider)
    .GetMethod(nameof(SqliteFunctionProvider.UnaccentLower))!)
    .HasName("UNACCENT_LOWER");

modelBuilder.HasDbFunction(typeof(SqliteFunctionProvider)
    .GetMethod(nameof(SqliteFunctionProvider.DamerauLevenshteinDistanceCapped))!)
    .HasName("DL_CAP");
    }
}

public class AddressDbContextFactory : IDesignTimeDbContextFactory<AddressDbContext>
{
    public AddressDbContext CreateDbContext(string[] args)
    {
        return new AddressDbContext();
    }
}

public class SqliteFunctionProvider
{
    public static void RegisterFunctions(SqliteConnection connection)
    {
        connection.CreateFunction<string, string, int, int>(
            "DL_CAP", DamerauLevenshteinDistanceCapped);

        connection.CreateFunction<string, string>("UNACCENT_LOWER", UnaccentLower);
    }

    public static int DamerauLevenshteinDistanceCapped(string s, string t, int max)
    {
        s ??= ""; t ??= "";
        if (s.Length == 0) return Math.Min(t.Length, max + 1);
        if (t.Length == 0) return Math.Min(s.Length, max + 1);
        if (s == t) return 0;

        if (Math.Abs(s.Length - t.Length) > max) return max + 1; // impossible d'être <= max

        int n = s.Length, m = t.Length;
        int INF = n + m;
        var d = new int[n + 2, m + 2];
        d[0, 0] = INF;
        for (int i = 0; i <= n; i++) { d[i + 1, 0] = INF; d[i + 1, 1] = i; }
        for (int j = 0; j <= m; j++) { d[0, j + 1] = INF; d[1, j + 1] = j; }

        var last = new Dictionary<char, int>();
        for (int i = 1; i <= n; i++)
        {
            int lastMatchCol = 0;
            int rowMin = int.MaxValue;

            for (int j = 1; j <= m; j++)
            {
                int i1 = last.TryGetValue(t[j - 1], out var v) ? v : 0;
                int j1 = lastMatchCol;

                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                if (cost == 0) lastMatchCol = j;

                int del = d[i, j + 1] + 1;
                int ins = d[i + 1, j] + 1;
                int sub = d[i, j] + cost;
                int trans = d[i1, j1] + (i - i1 - 1) + 1 + (j - j1 - 1);

                int vmin = Math.Min(Math.Min(del, ins), Math.Min(sub, trans));
                d[i + 1, j + 1] = vmin;
                if (vmin < rowMin) rowMin = vmin;
            }

            last[s[i - 1]] = i;

            if (rowMin > max) return max + 1; // early exit
        }

        int res = d[n + 1, m + 1];
        return res <= max ? res : max + 1;
    }
    
    public static string UnaccentLower(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark &&
                cat != UnicodeCategory.SpacingCombiningMark &&
                cat != UnicodeCategory.EnclosingMark)
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}