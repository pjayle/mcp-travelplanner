# MCP Client — AI Travel Planner (TravelPlanner.Web)

This repository contains the TravelPlanner.Web Razor Pages application — an MCP client AI travel planner that helps plan trips using an AI backend. The project targets .NET 9 and is designed to run locally or be published to a hosting environment.

Key highlights
- Razor Pages web application (ASP.NET Core) targeting .NET 9
- Integrates with an AI service to generate travel itineraries and recommendations
- Designed for local development in Visual Studio or via the dotnet CLI

Prerequisites
- .NET 9 SDK installed (dotnet --version should report a 9.x SDK)
- Visual Studio 2022/2026 or VS Code (recommended: Visual Studio Community 2026)
- (Optional) Docker to run containerized image
- API credentials for the AI provider used by the app (set as environment variables)

Configuration
1. Provide AI API credentials and any other secrets required by the app. The app reads configuration from appsettings.* and environment variables. Recommended environment variables (adjust to your setup):

   - AI_API_KEY  — the API key for your AI provider
   - AI_API_ENDPOINT — (optional) provider endpoint
   - ASPNETCORE_ENVIRONMENT — set to Development for local debugging

Example (PowerShell, one-time for session):

```powershell
$env:AI_API_KEY = "your_api_key_here"
$env:AI_API_ENDPOINT = "https://api.example.com"
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

If the project uses dotnet user-secrets in development, you can instead run:

```powershell
cd <project-folder>
dotnet user-secrets set "AI_API_KEY" "your_api_key_here"
dotnet user-secrets set "AI_API_ENDPOINT" "https://api.example.com"
```

Quickstart (CLI)
1. Clone the repository and open a terminal in the repository root.
2. Restore and build:

```powershell
dotnet restore
dotnet build
```

3. Run the app:

```powershell
dotnet run --project ./TravelPlanner.Web/TravelPlanner.Web.csproj
```

4. Open a browser and navigate to https://localhost:5001 or http://localhost:5000 (check console output for exact URL).

Running in Visual Studio
1. Open TravelPlanner.Web.slnx in Visual Studio.
2. Ensure the selected launch profile is the web project and run (F5) or Start Without Debugging (Ctrl+F5).

Publishing
To publish a self-contained build or prepare for deployment:

```powershell
dotnet publish ./TravelPlanner.Web/TravelPlanner.Web.csproj -c Release -o ./publish
```

Deployment options include deploying the published output to a hosting provider, IIS, Azure App Service, or building a Docker image and deploying to a container platform.

Notes for GitHub upload
- Add a sensible .gitignore to avoid committing user secrets, local databases, and build artifacts.
- Confirm no API keys or secrets are present in source before committing.

Contributing
Contributions and issues are welcome. Please open a PR for changes and avoid committing secrets or credentials.

License
Include or update licensing information appropriate for your project.

