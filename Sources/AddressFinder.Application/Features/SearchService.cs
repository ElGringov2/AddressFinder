using System;
using AddressFinder.Application.Services;
using AddressFinder.Domain;

namespace AddressFinder.Application.Features;

internal class SearchService(IAddressRepository addressRepository) : ISearch
{
    public async Task<IEnumerable<SearchResult>> Search(string query, CancellationToken cancellationToken)
    {
        var results = await addressRepository.SearchAsync(query, cancellationToken);
        return [.. results
            .Select(a => new SearchResult(
                NumberAndStreet: (a.Number + " " +  a.Street).TrimStart(),
                City: a.City,
                ZipCode: a.ZipCode,
                Country: a.Country
            ))];
    }
}
