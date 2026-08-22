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
using VenueAxe.Data;
using VenueAxe.Data.Repositories;
using VenueAxe.Domain.Common;
using VenueAxe.Repositories;
using VenueAxe.Services;
using VenueAxe.Web.Hubs;
using Microsoft.AspNetCore.DataProtection;
using VenueAxe.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Configuration (PostgreSQL Exclusive) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=venueaxe_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<VenueAxeDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<ILaneService, LaneService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IWaiverService, WaiverService>();
builder.Services.AddScoped<ILaneGameService, LaneGameService>();

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
builder.Services.AddSignalR();

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
        logger.LogInformation("Applying pending PostgreSQL database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrated successfully. Seeding initial data if required...");
        await DbInitializer.SeedAsync(db);
        logger.LogInformation("Database initialization complete.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
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
