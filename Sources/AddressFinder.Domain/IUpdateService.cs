using System;

namespace AddressFinder.Domain;

public interface IUpdateService
{
    public Task UpdateAsync(string? country = null, CancellationToken cancellationToken = default);
}
