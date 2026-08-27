using Hospital.Application.DTOs.Feedback;
using Hospital.Application.DTOs;
namespace Hospital.Application.Interfaces;
public interface IFeedbackService { Task<FeedbackDto> CreateAsync(int patientUserId, CreateFeedbackRequest request, CancellationToken cancellationToken); Task<IReadOnlyList<FeedbackDto>> GetMineAsync(int patientUserId, PaginationRequest pagination, CancellationToken cancellationToken); }
