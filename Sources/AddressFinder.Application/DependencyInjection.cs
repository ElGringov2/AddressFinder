using System;
using AddressFinder.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AddressFinder.Application;

public static class DependencyInjection
{
public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ISearch, Features.SearchService>();
        builder.Services.AddScoped<IUpdateService, Features.UpdateService>();
        builder.Services.AddScoped<IGovAddressService, Services.Implementations.FrenchApiAdresseService>();
        builder.Services.AddHostedService<HostServices.UpdateHostedService>();
        return builder;
    }
}