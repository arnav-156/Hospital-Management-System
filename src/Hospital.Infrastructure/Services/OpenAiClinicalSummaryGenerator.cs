using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Hospital.Application.DTOs.Ai;
using Hospital.Application.DTOs.Treatments;
using Hospital.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Hospital.Infrastructure.Services;

public sealed class OpenAiClinicalSummaryGenerator(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options) : IClinicalSummaryGenerator
{
    public async Task<ClinicalSummaryGeneration> GenerateAsync(IReadOnlyList<TreatmentDto> history, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.ApiKey))
            return ClinicalSummaryGeneration.Unavailable("NotConfigured");

        var prompt = BuildPrompt(history);
        using var request = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = JsonContent.Create(new { model = settings.Model, input = prompt, temperature = 0.2, max_output_tokens = 500 }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ClinicalSummaryGeneration.Unavailable("ProviderUnavailable");

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var summary = document.RootElement.TryGetProperty("output_text", out var output) ? output.GetString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(summary) || summary.Length > 8000)
                return ClinicalSummaryGeneration.Unavailable("InvalidResponse");

            return new ClinicalSummaryGeneration(true, summary, settings.Model, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return ClinicalSummaryGeneration.Unavailable("ProviderUnavailable");
        }
        catch (JsonException)
        {
            return ClinicalSummaryGeneration.Unavailable("InvalidResponse");
        }
    }

    private static string BuildPrompt(IEnumerable<TreatmentDto> history) =>
        "You summarize clinical records for an authorized doctor. Produce a concise factual history using only the supplied records. Do not diagnose, prescribe, recommend treatment, infer missing facts, or address the patient directly. Clearly distinguish recorded facts from unknown information. This output is AI-generated and requires doctor review.\n\nRecords:\n" +
        string.Join("\n", history.Select(item => $"- {item.TreatmentDateTime:yyyy-MM-dd}: Diagnosis={Clean(item.Diagnosis)}; Prescription={Clean(item.Prescription)}; Progress={Clean(item.ProgressNotes)}; Notes={Clean(item.TreatmentNotes)}"));

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "not recorded" : value.Trim();
}
