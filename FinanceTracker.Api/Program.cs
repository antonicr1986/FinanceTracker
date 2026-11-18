using System.Text.Json.Serialization;
using FinanceTracker.Api.HealthChecks;
using FinanceTracker.Api.Middleware;
using FinanceTracker.Api.Services;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Sin cabecera "Server: Kestrel": no hace falta anunciar con que esta hecha la API.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Origenes permitidos por CORS, separados por comas. Viven en configuracion y
// no en el codigo para poder anadir un dominio desde los ajustes del App
// Service sin recompilar ni volver a desplegar.
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"]
        ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Serilog sustituye al sistema de logs por defecto. Su configuracion vive en
// appsettings.json, no aqui: asi se puede cambiar el nivel o el destino sin
// recompilar ni volver a desplegar.
//
// writeToProviders: true hace que los logs de Serilog lleguen tambien a los
// proveedores de logging de .NET, entre ellos el de OpenTelemetry, y de ahi a
// Application Insights.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext(),
    writeToProviders: true);

// Observabilidad con Azure Monitor / Application Insights via OpenTelemetry:
// peticiones (endpoint, codigo, duracion), llamadas salientes, excepciones y
// logs. Solo se activa si existe APPLICATIONINSIGHTS_CONNECTION_STRING (en
// Azure, como app setting): en local, en los tests y en el entorno
// desechable del DAST no envia nada.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Azure SQL en modo serverless se pausa tras un rato sin uso, y la
            // primera conexion falla mientras despierta. Para SqlClient eso es
            // un fallo transitorio, asi que EF lo reconoce y reintenta con
            // esperas crecientes en vez de rendirse. Sin esto, la primera
            // visita del dia se llevaba un 500.
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 8,
                maxRetryDelay: TimeSpan.FromSeconds(15),
                errorNumbersToAdd: null);

            // Despertar la base puede acercarse al minuto; los 30 segundos por
            // defecto se quedan cortos.
            sqlOptions.CommandTimeout(90);
        }));

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Cifrado de los secretos TOTP del MFA (ver MfaSecretProtector). Sin la clave
// la API no arranca, igual que sin Jwt:Key.
builder.Services.AddSingleton(MfaSecretProtector.FromBase64(builder.Configuration["Mfa:EncryptionKey"]));
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// Contraseñas filtradas (Have I Been Pwned). Timeout corto: si tarda, el
// registro sigue adelante sin la comprobacion. Add-Padding hace que todas las
// respuestas midan parecido, para que el tamaño no delate el prefijo pedido.
builder.Services.AddHttpClient<IPasswordBreachChecker, PwnedPasswordsChecker>(client =>
{
    client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
    client.Timeout = TimeSpan.FromSeconds(3);
    client.DefaultRequestHeaders.Add("Add-Padding", "true");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FinanceTracker-Api");
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialise enums as strings ("Income"/"Expense") so the client
        // does not have to know the numeric values.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };

        // Firma y caducidad no bastan: tras "cerrar sesion en todos los
        // dispositivos" (o al vincular la cuenta con Google) los tokens
        // anteriores se rechazan aunque no hayan caducado. Cuesta una consulta
        // por peticion autenticada, por clave primaria. Sin "iat" (tokens de
        // antes de este cambio) cuenta como emitido en 1970: valen solo si la
        // cuenta nunca ha revocado sesiones.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                var userIdValue = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var iatValue = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat)?.Value;

                if (!int.TryParse(userIdValue, out var userId))
                {
                    context.Fail("Token without user id.");
                    return;
                }

                var issuedAt = long.TryParse(iatValue, out var iat)
                    ? DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime
                    : DateTime.UnixEpoch;

                var sessions = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();

                if (!await sessions.IsActiveAsync(userId, issuedAt))
                {
                    context.Fail("Session revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// IP real del cliente. Azure (y cualquier ingress o balanceador) termina la
// conexion y reenvia la peticion: sin esto, RemoteIpAddress seria siempre la
// del proxy y el limite de intentos por IP seria un unico contador para todos.
// Se toma solo la ultima entrada de X-Forwarded-For (ForwardLimit = 1 por
// defecto), que es la que añade el proxy de Azure y el cliente no puede falsear.
// Se vacian KnownNetworks/KnownProxies porque la IP del proxy de Azure no es fija.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Limite de intentos en registro, login y Google. Ver Middleware/AuthRateLimiting.cs.
builder.Services.AddAuthRateLimiting();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// La comprobacion de base de datos se etiqueta como "ready": asi /health
// (liveness) puede excluirla y /health/ready (readiness) incluirla.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

var app = builder.Build();

// Aplica las migraciones pendientes al arrancar. Esto hace que la imagen sea
// autosuficiente: contra un SQL Server vacio, la base de datos queda lista sola.
// Con varias replicas habria que mover esto a un paso propio del pipeline.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Cuenta de demostracion con datos de ejemplo, para que cualquiera pueda
    // entrar a ver la aplicacion sin registrarse. No hace nada si ya existe.
    await DemoDataSeeder.SeedAsync(db);
}

// Lo primero, para que todo lo que viene detras (logs incluidos) vea la IP
// real del cliente y no la del proxy.
app.UseForwardedHeaders();

// El manejador de excepciones va justo despues: envuelve a todo lo que viene
// detras, asi ningun error se escapa sin quedar registrado.
app.UseExceptionHandler();

// Cabeceras de seguridad (HSTS, nosniff, CSP...) en todas las respuestas,
// tambien en las de error. Ver Middleware/SecurityHeaders.cs.
app.UseSecurityHeaders();

// Una linea por peticion: metodo, ruta, codigo de estado y duracion.
app.UseSerilogRequestLogging();

// Swagger siempre activo: es la superficie de demo de este proyecto.
app.UseSwagger();
app.UseSwaggerUI();

// Sin UseHttpsRedirection a proposito. Dentro de un contenedor solo se
// escucha en HTTP: el cifrado lo termina la plataforma (ingress, proxy o
// balanceador) antes de llegar aqui. Dejarlo activo no anadia seguridad y si
// causaba problemas: emitia un aviso en cada arranque y, si llegara a
// activarse, respondaria 307 a las comprobaciones de /health que la
// plataforma hace por HTTP, marcando la aplicacion como caida.

app.UseCors("AllowFrontend");

// Despues de CORS, para que la web pueda leer el 429 cuando se pase del limite.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Liveness: "el proceso esta vivo y responde". Predicate => false hace que
// NO ejecute ninguna comprobacion. Es deliberado: si esto fallara porque la
// base de datos esta caida, la plataforma reiniciaria la API sin motivo.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: "puedo atender peticiones de verdad". Aqui si se comprueba la
// base de datos. Si falla, la plataforma deja de enviar trafico a esta
// instancia, pero no la reinicia.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();
