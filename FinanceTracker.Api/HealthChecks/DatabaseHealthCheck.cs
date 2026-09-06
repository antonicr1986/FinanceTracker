using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinanceTracker.Api.HealthChecks
{
    /// <summary>
    /// Comprueba que la base de datos acepta conexiones.
    ///
    /// Se usa solo en la comprobacion de "readiness" (/health/ready), no en la
    /// de "liveness" (/health). El motivo: si la base de datos se cae, reiniciar
    /// la API no arregla nada. Lo que hay que hacer es dejar de enviarle
    /// trafico hasta que la base vuelva.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _dbContext;

        public DatabaseHealthCheck(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _dbContext.Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Healthy("La base de datos responde.");
                }

                return HealthCheckResult.Unhealthy("La base de datos no acepta conexiones.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Error al conectar con la base de datos.", ex);
            }
        }
    }
}
