using AddressFinder.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AddressFinder.Infrastructure.Extensions;

public static class InfrastructureServicesExtensions
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<Contexts.AddressDbContext>();
        builder.Services.AddScoped<IAddressRepository, Repositories.AddressRepository>();
        return builder;
    }
}