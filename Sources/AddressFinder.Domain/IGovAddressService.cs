using System;

namespace AddressFinder.Domain;

public interface IGovAddressService
{
    string Country { get; }
    Task<IEnumerable<Address>> GetAddressesAsync(CancellationToken cancellationToken = default);
}