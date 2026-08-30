using System.Text;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Hospital.Application.DTOs.Auth;
using Hospital.Api.Middleware;
using Hospital.Infrastructure;
using Hospital.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("HospitalManagementDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'HospitalManagementDb' is required. Configure it in appsettings.Local.json for local development or through ConnectionStrings__HospitalManagementDb for automated environments.");
}
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is required.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32 ||
    jwtOptions.AccessTokenLifetimeMinutes is < 1 or > 1440)
{
    throw new InvalidOperationException("JWT configuration is invalid. Configure issuer, audience, a signing key of at least 32 characters, and a token lifetime between 1 and 1440 minutes.");
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?.Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        uri.AbsolutePath == "/" &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? [];
var allowedCorsOrigins = configuredCorsOrigins.Length > 0
    ? configuredCorsOrigins
    : builder.Environment.IsDevelopment()
        ? ["http://localhost:5173", "http://127.0.0.1:5173"]
        : throw new InvalidOperationException("Configure at least one valid Cors:AllowedOrigins value outside Development.");

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddInfrastructure(connectionString, jwtOptions, builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdText, out var userId) || !await context.HttpContext.RequestServices.GetRequiredService<HospitalManagementDbContext>().Users.AnyAsync(user => user.UserId == userId && user.IsActive, context.HttpContext.RequestAborted))
                {
                    context.Fail("The account is inactive or no longer exists.");
                }
            },
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("HospitalWeb", policy =>
    policy.WithOrigins(allowedCorsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:ApplyDevelopmentSeed"))
{
    var seedPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "database", "seed", "001_DevelopmentSeedData.sql"));
    if (!File.Exists(seedPath))
    {
        throw new FileNotFoundException("Development seed script was not found.", seedPath);
    }

    var seedScript = File.ReadAllText(seedPath);
    seedScript = Regex.Replace(seedScript, @"(?m)^:setvar.*\r?\n", string.Empty)
        .Replace("$(DatabaseName)", "HospitalManagementDb");

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    foreach (var batch in Regex.Split(seedScript, @"(?im)^\s*GO\s*$"))
    {
        if (string.IsNullOrWhiteSpace(batch))
        {
            continue;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = batch;
        await command.ExecuteNonQueryAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["Cache-Control"] = "no-store";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; base-uri 'none'; frame-ancestors 'none'";
        return Task.CompletedTask;
    });

    await next();
});

app.UseExceptionHandler();
app.UseCors("HospitalWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
