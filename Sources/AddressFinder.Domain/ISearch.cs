using System;

namespace AddressFinder.Domain;

public interface ISearch
{
    public Task<IEnumerable<SearchResult>> Search(string query, CancellationToken cancellationToken = default);
}


public record SearchResult(string NumberAndStreet, string City, string ZipCode, string Country);