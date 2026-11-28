using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinanceTracker.Infrastructure.Data;

/// <summary>
/// Aplica las migraciones y siembra la cuenta demo al arrancar, una replica cada
/// vez. Con varias replicas arrancando a la vez (Kubernetes, HPA) todas
/// intentaban migrar a la vez y una fallaba con "There is already an object
/// named '__EFMigrationsHistory'" y se reiniciaba.
///
/// El cerrojo es de SQL Server (sp_getapplock) y vale para cualquier sitio donde
/// corra la API: la primera replica lo coge, migra y siembra; las demas esperan
/// y, al entrar, ya no tienen nada que hacer. Se suelta al terminar, y si la
/// replica muere a medias SQL Server lo suelta solo al cerrarse la conexion.
/// </summary>
public static class DatabaseInitializer
{
    private const string LockName = "FinanceTracker.DatabaseInitialization";

    /// <summary>Tiempo maximo esperando el cerrojo (la otra replica migrando).</summary>
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Al arrancar: migra y deja la demo como recien creada (ver
    /// DemoDataSeeder.ResetAsync). Asi cada arranque, que en el plan gratuito
    /// llega tras un rato sin visitas, limpia lo que hubieran dejado otros.
    /// </summary>
    public static async Task MigrateAndSeedAsync(AppDbContext db)
    {
        await CreateDatabaseIfMissingAsync(db);
        await WithLockAsync(db, async () =>
        {
            await db.Database.MigrateAsync();
            await DemoDataSeeder.ResetAsync(db);
        });
    }

    /// <summary>
    /// Reinicio programado de la demo (DemoResetService), con el mismo cerrojo:
    /// con varias replicas, una lo hace y las demas lo repiten despues sin chocar.
    /// </summary>
    public static Task ResetDemoAsync(AppDbContext db) =>
        WithLockAsync(db, () => DemoDataSeeder.ResetAsync(db));

    private static async Task WithLockAsync(AppDbContext db, Func<Task> work)
    {
        // Abrir la conexion a mano hace que EF use esta misma para todo: el
        // cerrojo es de la sesion, asi que cubre todo lo que se hace con ella.
        await db.Database.OpenConnectionAsync();
        try
        {
            await AcquireLockAsync(db);
            try
            {
                await work();
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync(
                    "EXEC sp_releaseapplock @Resource = {0}, @LockOwner = 'Session'", LockName);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// Contra un SQL Server vacio (Docker, Kubernetes) la base de datos aun no
    /// existe y no se puede abrir una conexion a ella para coger el cerrojo: se
    /// crea antes. Si dos replicas lo intentan a la vez, la segunda recibe el
    /// error 1801 ("ya existe") y sigue. En Azure SQL ya existe y no se toca.
    /// </summary>
    private static async Task CreateDatabaseIfMissingAsync(AppDbContext db)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (await creator.ExistsAsync())
        {
            return;
        }

        try
        {
            await creator.CreateAsync();
        }
        catch (SqlException e) when (e.Number == 1801)
        {
            // Otra replica la acaba de crear.
        }
    }

    private static async Task AcquireLockAsync(AppDbContext db)
    {
        var command = db.Database.GetDbConnection().CreateCommand();
        await using (command)
        {
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = @name,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Session',
                    @LockTimeout = @timeoutMs;
                SELECT @result;
                """;
            command.CommandTimeout = (int)LockTimeout.TotalSeconds + 30;

            var name = command.CreateParameter();
            name.ParameterName = "@name";
            name.Value = LockName;
            command.Parameters.Add(name);

            var timeout = command.CreateParameter();
            timeout.ParameterName = "@timeoutMs";
            timeout.Value = (int)LockTimeout.TotalMilliseconds;
            command.Parameters.Add(timeout);

            // 0 o 1: concedido. Negativo: timeout, cancelado o error.
            var result = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (result < 0)
            {
                throw new InvalidOperationException(
                    $"Could not acquire the database initialization lock (sp_getapplock returned {result}).");
            }
        }
    }
}
