using System;
using Microsoft.AspNetCore.Mvc;

namespace AddressFinder.API.Routes;

public static class AddressRoutes
{
    public const string Base = "api/addresses";

    public static IEndpointRouteBuilder MapAddressRoutes(this IEndpointRouteBuilder app)
    {
        var routeGroup = app.MapGroup(Base);


        routeGroup.Map("search", async ([FromQuery] string query, [FromServices] Domain.ISearch searchFeature, HttpContext context) =>
        {
            var result = await searchFeature.Search(query, context.RequestAborted);
            return Results.Ok(result);
        });

        return app;
    }
}

