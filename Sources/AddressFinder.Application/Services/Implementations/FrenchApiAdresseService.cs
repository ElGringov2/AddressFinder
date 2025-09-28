using System;
using System.IO.Compression;
using System.Text;
using AddressFinder.Domain.Extensions;
using AddressFinder.Domain;
using Microsoft.Extensions.Logging;

namespace AddressFinder.Application.Services.Implementations;

internal class FrenchApiAdresseService
    (HttpClient httpClient,
    ILogger<FrenchApiAdresseService> logger) 
: IGovAddressService
{
    public string Country => "France";
    public async Task<IEnumerable<Address>> GetAddressesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // URL de l'API gouvernementale (exemple avec l'API Adresse)
            var url = "https://adresse.data.gouv.fr/data/ban/adresses/latest/csv/adresses-67.csv.gz";

            logger.LogInformation("Début du téléchargement des adresses depuis l'API gouvernementale");

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            // Décompression du fichier GZIP
            using var compressedStream = new MemoryStream(content);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);

            var csvContent = await reader.ReadToEndAsync(cancellationToken);

            // Parse du CSV (vous pouvez utiliser CsvHelper pour une solution plus robuste)
            var addresses = ParseCsvContent(csvContent);

            logger.LogInformation("Téléchargement terminé. {adresses} adresses récupérées", addresses.Count);

            return addresses;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des adresses depuis l'API");
            throw;
        }
    }

    private List<Address> ParseCsvContent(string csvContent)
    {
        var adresses = new List<Address>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Ignorer la première ligne (headers)
        for (int i = 1; i < lines.Length; i++)
        {
            var fields = lines[i].Split(';');
            if (fields.Length >= 8)
            {
                adresses.Add(new Address
                {
                    Number = fields[2],
                    Street = fields[4],
                    ZipCode = fields[5],
                    City = fields[7],
                    Country = Country,

                    // Normalisés pour l’index/les recherches
                    StreetNorm  = TextNormalizer.NormalizeForSearch(fields[4]),
                    CityNorm    = TextNormalizer.NormalizeForSearch(fields[7]),
                    CountryNorm = TextNormalizer.NormalizeForSearch(Country)
                });
            }
        }

        return adresses;
    }
}
