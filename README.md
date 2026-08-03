# Service Advisor Assistant

Service Advisor Assistant is a small demo application that helps turn a customer’s vehicle complaint into a structured workshop-style analysis. A Vue 3 front end sends the complaint to an ASP.NET Core API, which calls Azure OpenAI and returns a concise rephrasing plus likely solutions.

## Considerations
In attempting this solution, I decided that this functionality would not necessarily be a standalone solution, but woult be a tool/service within a greater CRM solution. As such I've kept the implementation extremely light and decided to stick to the base ask.

## What the project includes
- Backend API: ASP.NET Core Web API in src/api
- Frontend UI: Vite + Vue 3 + TypeScript in src/ui
- Pattern: a simple request/response flow where the UI posts a complaint to the API and receives structured JSON

## Project structure
- src/api/Program.cs — app setup, dependency injection, CORS, and controller mapping
- src/api/Controllers/AdvisorController.cs — POST /api/advisor endpoint
- src/api/Services/LLMClient.cs — Azure OpenAI integration and JSON response handling
- src/api/Models/ — request and response DTOs
- src/ui/src/App.vue — complaint form and result rendering

## How it works
1. The user enters a complaint in the UI.
2. The UI sends a POST request to /api/advisor with the complaint text.
3. The API hashes the complaint for logging, calls Azure OpenAI, and returns JSON with:
   - rephrasedComplaint
   - solutions
   - note

## Configuration
Set these environment variables before running the API:
- AZURE_OPENAI_ENDPOINT — Azure OpenAI resource endpoint
- AZURE_OPENAI_API_KEY — API key for the Azure OpenAI resource
- AZURE_OPENAI_DEPLOYMENT — deployment/model name to use
- ASPNETCORE_URLS (optional) — e.g. http://localhost:5000

For the UI, set VITE_API_URL to the backend base URL, for example:
- http://localhost:5000

## Run locally
### Backend
```bash
cd src/api
dotnet run
```

### Frontend
```bash
cd src/ui
npm install
npm run dev
```

Open the Vite URL shown in the terminal and use the form to submit a complaint.

## API contract
### POST /api/advisor
Request body:
```json
{
  "complaint": "The car makes a grinding noise when braking"
}
```

Response:
```json
{
  "rephrasedComplaint": "Vehicle produces a grinding noise during braking.",
  "solutions": [
    {
      "issue": "Front brake pads or rotors may be worn",
      "suggestedFix": "Inspect brake pads and rotors; replace pads if worn and check rotor condition.",
      "confidence": 0.7
    }
  ],
  "note": "Ask the customer whether the noise occurs at low speed only."
}
```

## Notes
- The application is intentionally lightweight and intended as a demo/prototype.
- For traceability, the API logs only a SHA-256 hash of the complaint text rather than storing the full content.

