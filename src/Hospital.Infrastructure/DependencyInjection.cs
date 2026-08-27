using Hospital.Application.Interfaces;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Ai;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Data.Entities;
using Hospital.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, JwtOptions jwtOptions, IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(jwtOptions);

        services.AddDbContext<HospitalManagementDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ISystemHealthService, DatabaseHealthService>();
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwtOptions.Issuer;
            options.Audience = jwtOptions.Audience;
            options.SigningKey = jwtOptions.SigningKey;
            options.AccessTokenLifetimeMinutes = jwtOptions.AccessTokenLifetimeMinutes;
        });
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ITreatmentService, TreatmentService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = configuration["OpenAi:ApiKey"] ?? configuration["OPENAI_API_KEY"];
            options.Model = configuration["OpenAi:Model"] ?? options.Model;
            if (bool.TryParse(configuration["OpenAi:Enabled"], out var enabled)) options.Enabled = enabled;
        });
        services.AddSingleton(new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") });
        services.AddScoped<IClinicalSummaryGenerator, OpenAiClinicalSummaryGenerator>();
        services.AddScoped<IMedicalHistorySummaryService, MedicalHistorySummaryService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
