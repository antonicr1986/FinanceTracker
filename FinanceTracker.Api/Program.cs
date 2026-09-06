using FinanceTracker.Api.HealthChecks;
using FinanceTracker.Api.Middleware;
using FinanceTracker.Api.Services;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog sustituye al sistema de logs por defecto. Su configuracion vive en
// appsettings.json, no aqui: asi se puede cambiar el nivel o el destino sin
// recompilar ni volver a desplegar.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add services to the container.
builder.Services.AddControllers();

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
    });

builder.Services.AddAuthorization();

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
}

// El manejador de excepciones va el primero: envuelve a todo lo que viene
// detras, asi ningun error se escapa sin quedar registrado.
app.UseExceptionHandler();

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
