using System;
using AddressFinder.Domain;

namespace AddressFinder.Application.Services;

public interface IAddressRepository
{
    Task AddRangeAsync(IEnumerable<Address> adresses, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
    Task DeleteAllAsync(string? country, CancellationToken cancellationToken = default);

    Task<IEnumerable<Address>> SearchAsync(string query, CancellationToken cancellationToken = default);
}