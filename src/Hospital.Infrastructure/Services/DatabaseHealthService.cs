using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Services;

public sealed class DatabaseHealthService(
    HospitalManagementDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<DatabaseHealthService> logger) : ISystemHealthService
{
    private static readonly Action<ILogger, Exception?> DatabaseHealthCheckFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1, nameof(DatabaseHealthCheckFailed)),
        "Database connectivity health check failed.");

    public async Task<HealthStatusDto> GetHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var databaseConnected = await dbContext.Database.CanConnectAsync(cancellationToken);
            return new HealthStatusDto(
                databaseConnected ? "healthy" : "unhealthy",
                databaseConnected,
                timeProvider.GetUtcNow());
        }
        catch (Exception)
        {
            DatabaseHealthCheckFailed(logger, null);
            return new HealthStatusDto("unhealthy", false, timeProvider.GetUtcNow());
        }
    }
}
