using System;
using System.IO.Compression;
using System.Text;
using AddressFinder.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AddressFinder.Application.HostServices;

public class UpdateHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<UpdateHostedService> logger)
        : IHostedService, IDisposable
{
    private Timer _timer = null!;
    private CancellationTokenSource _tokenSource = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service de mise à jour des adresses postales démarré");

        var delay = CalculateDelayUntilNextSaturday23h();
        _timer = new Timer(ExecuteUpdate, null, delay, TimeSpan.FromDays(7));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Service de mise à jour des adresses postales arrêté");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void ExecuteUpdate(object? state)
    {
        using var scope = scopeFactory.CreateScope();
        var updateService = scope.ServiceProvider.GetRequiredService<IUpdateService>();
        await updateService.UpdateAsync(null, _tokenSource.Token);
    }
    private static TimeSpan CalculateDelayUntilNextSaturday23h()
    {
        var now = DateTime.Now;
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)now.DayOfWeek + 7) % 7;
        var nextSaturday = now.Date.AddDays(daysUntilSaturday).AddHours(23);

        if (daysUntilSaturday == 0 && now.Hour < 23)
        {
            return nextSaturday - now;
        }

        if (daysUntilSaturday == 0)
        {
            nextSaturday = nextSaturday.AddDays(7);
        }

        return nextSaturday - now;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _tokenSource.Cancel();
        _tokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}