namespace Hospital.Application.DTOs;

public sealed record HealthStatusDto(string Status, bool DatabaseConnected, DateTimeOffset CheckedAtUtc);
