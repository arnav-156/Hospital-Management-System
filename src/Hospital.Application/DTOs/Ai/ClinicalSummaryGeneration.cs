using Hospital.Application.DTOs.Treatments;

namespace Hospital.Application.DTOs.Ai;

public sealed record ClinicalSummaryGeneration(bool IsAvailable, string? Summary, string? Model, string? FailureCode)
{
    public static ClinicalSummaryGeneration Unavailable(string failureCode) => new(false, null, null, failureCode);
}

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";
    public bool Enabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4.1-mini";
}
