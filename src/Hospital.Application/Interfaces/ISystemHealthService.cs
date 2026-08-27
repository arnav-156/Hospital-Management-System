using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface ISystemHealthService
{
    Task<HealthStatusDto> GetHealthAsync(CancellationToken cancellationToken);
}
