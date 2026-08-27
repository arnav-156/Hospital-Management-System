using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs;

public sealed class PaginationRequest
{
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;

    [Range(1, 100_000)]
    public int Page { get; init; } = 1;

    [Range(1, MaximumPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public int Skip => (Page - 1) * PageSize;
}
