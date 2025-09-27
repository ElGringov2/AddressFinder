using System;
using AddressFinder.Application.Services;
using AddressFinder.Domain;
using Microsoft.Extensions.Logging;

namespace AddressFinder.Application.Features;

internal class UpdateService(
    IAddressRepository repository,
    IEnumerable<IGovAddressService> addressesServices,
    ILogger<UpdateService> logger) : IUpdateService
{
    public async Task UpdateAsync(string? country = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Début de la mise à jour des adresses postales");
        int totalProcessed = 0;
        foreach (var service in addressesServices)
        {
            try
            {
                logger.LogInformation("Récupération des adresses depuis le service {service}", service!.Country);
                var addresses = await service.GetAddressesAsync(cancellationToken);
                logger.LogInformation("{count} adresses récupérées depuis le service {service}", addresses.Count(), service!.Country);
                await repository.DeleteAllAsync(service.Country, cancellationToken);
                await repository.AddRangeAsync(addresses, cancellationToken);
                totalProcessed += addresses.Count();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors de la mise à jour des adresses postales pour le pays {pays}", service.Country);
            }
        }

        logger.LogInformation("Mise à jour terminée. {totalProcessed} adresses enregistrées", totalProcessed);

    }
}
