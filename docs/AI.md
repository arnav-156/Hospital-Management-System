# AI summary feature

The optional AI feature is a doctor-only summary of existing treatment history. It does not create, update, diagnose, prescribe, or otherwise modify medical records.

- The API sends no patient identity to the provider—only treatment record content needed for the summary.
- The prompt instructs the provider to state recorded facts only and never make treatment recommendations.
- Every request records minimal audit metadata: patient/doctor IDs, outcome, model, record count, and non-sensitive failure code. Prompts, summaries, tokens, and API keys are not logged.
- A provider/configuration/response failure returns normal history with a prominent doctor-review disclaimer, so core clinical workflows remain available.
- Enable only with external configuration: `OpenAi__Enabled=true` plus `OpenAi__ApiKey` or `OPENAI_API_KEY`.

AI output is informational and must be reviewed by an authorized clinician.
