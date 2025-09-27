using System;
using Microsoft.AspNetCore.Mvc;

namespace AddressFinder.API.Routes;

public static class UpdateRoutes
{
    public const string Base = "api/updates";

    public static IEndpointRouteBuilder MapUpdateRoutes(this IEndpointRouteBuilder app)
    {
        var routeGroup = app.MapGroup(Base);


        routeGroup.Map("update", async ([FromServices] Domain.IUpdateService updateService, HttpContext context) =>
        {
            await updateService.UpdateAsync(null, context.RequestAborted);
            return Results.Ok();
        });

        return app;
    }
}

