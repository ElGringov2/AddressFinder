using AddressFinder.API.Routes;
using AddressFinder.Application;
using AddressFinder.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplication();
builder.AddInfrastructure();
builder.Services.AddHttpClient();
var app = builder.Build();

app.MapAddressRoutes();
app.MapUpdateRoutes();

app.MigrateDatabase();

app.Run();
