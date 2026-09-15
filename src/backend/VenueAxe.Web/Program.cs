using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using VenueAxe.Data;
using VenueAxe.Data.Repositories;
using VenueAxe.Domain.Common;
using VenueAxe.Repositories;
using VenueAxe.Services;
using VenueAxe.Web.Hubs;
using Microsoft.AspNetCore.DataProtection;
using VenueAxe.Web.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting VenueAxe API host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "VenueAxe")
        .WriteTo.Console(new RenderedCompactJsonFormatter()));

// --- 1. Database Configuration (PostgreSQL Exclusive) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=venueaxe_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<VenueAxeDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// --- 2. Data Protection Keys (Persisted to PostgreSQL Database) ---
builder.Services.AddDataProtection()
    .SetApplicationName("VenueAxe")
    .PersistKeysToDbContext<VenueAxeDbContext>();

// --- 3. Ambient Multi-Tenant Context & Enterprise DI Patterns ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpContextUserContext>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Domain Repositories
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILaneRepository, LaneRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingConfigRepository, BookingConfigRepository>();
builder.Services.AddScoped<IWaiverRepository, WaiverRepository>();
builder.Services.AddScoped<ILaneSessionRepository, LaneSessionRepository>();

// Register Domain Application Services
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<ILaneService, LaneService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ISquarePaymentService, SquarePaymentService>();
builder.Services.AddScoped<IWaiverService, WaiverService>();
builder.Services.AddScoped<ILaneGameService, LaneGameService>();
builder.Services.AddScoped<IWaiverPdfService, VenueAxe.Infrastructure.Pdf.WaiverPdfService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

builder.Services.Configure<VenueAxe.Infrastructure.Storage.StorageOptions>(builder.Configuration.GetSection(VenueAxe.Infrastructure.Storage.StorageOptions.SectionName));
var storageProvider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "LocalStorage";
if (storageProvider.Equals("Backblaze", StringComparison.OrdinalIgnoreCase) ||
    storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<VenueAxe.Application.Services.IVenueAssetStorageService, VenueAxe.Infrastructure.Storage.BackblazeVenueAssetStorageService>();
}
else
{
    builder.Services.AddScoped<VenueAxe.Application.Services.IVenueAssetStorageService, VenueAxe.Infrastructure.Storage.LocalVenueAssetStorageService>();
}

builder.Services.AddHostedService<VenueAxe.Web.BackgroundServices.SessionLifecycleBackgroundService>();

// --- 3. Cookie Authentication (Strictly No JWT) ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "VenueAxe.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// --- 4. MVC Controllers, Areas & SignalR ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
var signalRBuilder = builder.Services.AddSignalR();
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["REDIS_CONNECTION"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("VenueAxe");
    });
}

// --- 5. CORS (SvelteKit Frontend Integration with Credentials) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:4173", "http://127.0.0.1:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- 6. OpenAPI / Swagger Documentation ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Database Migration & Seeding ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VenueAxeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Applying pending PostgreSQL database migrations for environment {Environment}", app.Environment.EnvironmentName);
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrated successfully. Seeding initial data if required");
        await DbInitializer.SeedAsync(db);
        logger.LogInformation("Database initialization complete");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        throw;
    }
}

// --- Middleware Pipeline ---
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "VenueAxe API v1");
    });
}

app.UseCors("FrontendPolicy");
app.UseStaticFiles();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseAuthentication();
app.UseAuthorization();

// Map MVC Areas & Controllers
app.MapControllerRoute(
    name: "areas",
    pattern: "api/{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

// Map SignalR Real-Time Hub
app.MapHub<LaneHub>("/hubs/lane");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "VenueAxe API host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
