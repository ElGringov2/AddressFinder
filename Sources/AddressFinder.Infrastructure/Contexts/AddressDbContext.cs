using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AddressFinder.Infrastructure.Contexts;

public class AddressDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite("Data Source=addresses.db");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AddressDbContext).Assembly);
    }
}

public class AddressDbContextFactory : IDesignTimeDbContextFactory<AddressDbContext>
{
    public AddressDbContext CreateDbContext(string[] args)
    {
        return new AddressDbContext();
    }
}