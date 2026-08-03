# Service Advisor Assistant

Minimal example: .NET API that uses Azure OpenAI to rephrase a customer's vehicle fault description and return structured possible solutions, plus a small TypeScript test UI.

Overview
- API: src/ServiceAdvisorApi (ASP.NET minimal Web API)
- UI: ui (TypeScript, simple fetch-based frontend)

Environment
- Set these environment variables when running the API:
  - AZURE_OPENAI_ENDPOINT — e.g. https://your-resource.openai.azure.com
  - AZURE_OPENAI_KEY — API key for Azure OpenAI
  - AZURE_OPENAI_DEPLOYMENT — the deployment name (model) to use
  - ASPNETCORE_URLS (optional) — to set listening URL, e.g. http://localhost:5000

Security & Logging
- We do NOT persist user prompts. For traceability we log only a SHA256 hash of the user's input and basic metadata. No prompt content is written to disk or stored.

Run API (dotnet 7+)
- cd src/api
- dotnet run

Run UI
- cd src/ui
- npm install
- npm run build
- Serve the ui directory (for quick test: npx http-server . -c-1 or any static server) and open index.html

API Contract
POST /api/advisor
Body: { "complaint": "string" }
Response: JSON object:
{
  "rephrasedComplaint": "...",
  "solutions": [ { "issue": "..", "suggestedFix": ".." }, ... ]
}


