using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using CarePoint.Application.Configuration;
using CarePoint.API.Middleware;
using CarePoint.API.Filters;
using CarePoint.Application;
using CarePoint.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Net;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// --- Application & Infrastructure DI ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- JWT Settings ---
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is required.");
if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
    throw new InvalidOperationException("JwtSettings:Secret must be configured with at least 32 characters.");
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
    throw new InvalidOperationException("JwtSettings:Issuer and JwtSettings:Audience must be configured.");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
var medicalDocumentSettings = builder.Configuration
    .GetSection(MedicalDocumentSettings.SectionName)
    .Get<MedicalDocumentSettings>() ?? new MedicalDocumentSettings();
if (medicalDocumentSettings.MaxBytesPerPatient <= 0 ||
    medicalDocumentSettings.UploadPermitLimit <= 0 ||
    medicalDocumentSettings.UploadWindowMinutes <= 0)
{
    throw new InvalidOperationException("Medical document quota and rate-limit settings must be greater than zero.");
}
builder.Services.Configure<MedicalDocumentSettings>(
    builder.Configuration.GetSection(MedicalDocumentSettings.SectionName));

// --- Authentication ---
builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var configuredProxy in builder.Configuration
                 .GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
    {
        if (!IPAddress.TryParse(configuredProxy, out var address))
            throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contains invalid IP '{configuredProxy}'.");
        options.KnownProxies.Add(address);
    }
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("document-upload", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = medicalDocumentSettings.UploadPermitLimit,
                Window = TimeSpan.FromMinutes(medicalDocumentSettings.UploadWindowMinutes),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// --- CORS ---
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// --- Controllers & Swagger ---
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "CarePoint API", Version = "v1" });
});

var app = builder.Build();

// Deployment command: migrate and seed only essential reference data, then exit.
// Keeping this explicit avoids coupling API liveness to database availability.
if (CarePoint.Infrastructure.Data.DatabaseInitializationCommand.IsRequested(args))
{
    using var initializationScope = app.Services.CreateScope();
    var initializationContext = initializationScope.ServiceProvider
        .GetRequiredService<CarePoint.Infrastructure.Data.ApplicationDbContext>();
    await initializationContext.Database.MigrateAsync();
    await CarePoint.Infrastructure.Data.DatabaseSeeder.SeedAsync(
        initializationScope.ServiceProvider, seedDemoData: false);
    Log.Information("Database migrations and essential reference-data seeding completed.");
    return;
}

// --- Middleware Pipeline ---
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseForwardedHeaders();

// Must be called before UseHttpsRedirection and UseAuthorization/UseAuthentication
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

// Local/container convenience only. Production should run migrations as a deployment step so
// liveness remains available when the database is temporarily unavailable.
var initializeDatabase = builder.Configuration.GetValue(
    "Database:InitializeOnStartup", app.Environment.IsDevelopment());
if (initializeDatabase)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CarePoint.Infrastructure.Data.ApplicationDbContext>();
    await context.Database.MigrateAsync();
    var seedDemoData = app.Environment.IsDevelopment() && builder.Configuration.GetValue("SeedDemoData", false);
    await CarePoint.Infrastructure.Data.DatabaseSeeder.SeedAsync(scope.ServiceProvider, seedDemoData);
}

app.Run();
