using FinanceTracker.Infrastructure.Data;

namespace FinanceTracker.Api.Services;

/// <summary>
/// Reinicia la cuenta demo cada noche (DemoResetSchedule): las credenciales son
/// publicas y los visitantes pueden cambiar o ensuciar sus datos. Si la API
/// esta dormida a esa hora (plan gratuito), el reinicio llega en el siguiente
/// arranque (DatabaseInitializer.MigrateAndSeedAsync).
/// </summary>
public class DemoResetService(IServiceScopeFactory scopes, ILogger<DemoResetService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(DemoResetSchedule.DelayUntilNextReset(DateTime.UtcNow), stoppingToken);

            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await DatabaseInitializer.ResetDemoAsync(db);
                logger.LogInformation("Demo account reset");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Un fallo (base de datos despertando, por ejemplo) no debe tirar
                // la API: se reintenta la noche siguiente o en el proximo arranque.
                logger.LogError(e, "Demo account reset failed");
            }
        }
    }
}
