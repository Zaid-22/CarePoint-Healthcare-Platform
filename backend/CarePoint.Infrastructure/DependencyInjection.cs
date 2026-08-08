using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using CarePoint.Application.Interfaces;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Repositories;
using CarePoint.Infrastructure.Services;
using CarePoint.Application.Configuration;

namespace CarePoint.Infrastructure;

/// <summary>
/// Registers Infrastructure services: DbContext, Identity, Repositories.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });
        services.Configure<ClinicTimeSettings>(configuration.GetSection(ClinicTimeSettings.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClinicClock, ClinicClock>();

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IPasswordResetEmailSender, SmtpPasswordResetEmailSender>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddSingleton<IMedicalDocumentStorage, LocalMedicalDocumentStorage>();
        services.AddSingleton<IProfileImageStorage, LocalProfileImageStorage>();

        return services;
    }
}
