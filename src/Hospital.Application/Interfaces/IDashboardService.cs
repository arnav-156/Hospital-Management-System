using Hospital.Application.DTOs.Dashboard;

namespace Hospital.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetAsync(int userId, string role, CancellationToken cancellationToken);
}
