# RaceDay - Part 3
PROG6212 event management application built with ASP.NET Core MVC and .NET 8.

## Part 3 submission
Use the [part3-mvc branch](https://github.com/mahlengi-123/RaCeDay-Part2/tree/part3-mvc).
The existing Part 2 history is preserved. Do not recreate old commits or add empty commits for a count.
Submit the Part 3 branch URL on ARC after completing the video.

## Features
- Admin: role management and administrator dashboard.
- Organiser: dashboard, event creation/editing/deletion, banner upload/replacement, categories, enrolment viewing and results capture.
- Participant: dashboard, event browsing and entry, personal enrolments, withdrawal before an event, finishing times/positions and profile.
- Identity authentication, confirmed-email login, role-based authorisation, antiforgery-protected MVC forms and session clearing on logout.
- Events and categories with enrolments cannot be deleted.

## Architecture and banner upload flow
1. The organiser submits an image in the MVC Create or Edit form.
2. EventsController uses BannerApiClient to send the file as multipart/form-data over HTTP.
3. The separate RaceDay.BannerApi project checks its server-only API key and validates the image.
4. The API uploads the file into Azure Blob Storage and returns blobName and imageUrl as JSON.
5. MVC saves these values on the SQL Server Event record and displays the image in Details.
6. Replacement/deletion cleanup also uses the API. The old banner is removed only after the SQL save succeeds.

Both MVC and API validate supported JPG/JPEG/PNG/GIF/WebP images, maximum 2 MB, including image signatures.
Storage exceptions produce safe API errors and user-facing form messages.
The API key is read from configuration and is never sent to the browser.

### Project folders
- Controllers/Controllers: existing MVC controllers.
- Models and Data: entities, Identity user, DbContext and migrations.
- Views: Razor pages grouped by controller and shared layout.
- Services: HTTP banner client, shared contracts, image validation and Azure storage implementation.
- Api: independently runnable .NET 8 API; Azure operations are registered here.
- Tests: MVC/client unit tests.
- ApiTests: API endpoint and API-key tests.
- scripts: local setup and startup commands.

Identity registration, login, logout and profile pages are in Areas/Identity.

## Local configuration and startup
Install .NET 8, SQL Server LocalDB (or another SQL Server instance), and Docker Desktop for container execution.
Configure ConnectionStrings:DefaultConnection and AzureBlob:ConnectionString in MVC User Secrets.
The existing AzureStorage:ConnectionString key is also accepted.
Use an existing event-banners container with image URLs readable by the application.

From PowerShell in the repository:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup-local.ps1
dotnet restore "RaCeDay Part2.sln"
dotnet test "RaCeDay Part2.sln" --configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1
```

The setup command configures MVC/API User Secrets and creates an ignored .env file for Docker.
It does not change the machine's PowerShell execution policy.
The MVC application automatically applies pending database migrations at startup.

Local MVC: https://localhost:7240
Local API health: http://127.0.0.1:5081/health

To start the services manually, use separate terminals:
```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project "Api/RaceDay.BannerApi.csproj" --urls http://127.0.0.1:5081
```
```powershell
dotnet run --project "RaCeDay Part 2.csproj" --launch-profile https
```

Configuration:
- MVC: ConnectionStrings:DefaultConnection, BannerApi:BaseUrl, BannerApi:ApiKey.
- API: BannerApi:ApiKey, AzureBlob:ConnectionString, AzureBlob:ContainerName.
- Optional local initial admin: SeedAdmin:Email and SeedAdmin:Password.
- API defaults to http://127.0.0.1:5081/ in the MVC client.

Keep credentials in User Secrets or the ignored local .env. Never commit .env or runtime.env.
Existing account passwords and roles are preserved.

## Docker - MVC, API and SQL Server
The two multi-stage Dockerfiles publish the applications and run them as non-root users.
Compose provides a separate SQL Server database named RaceDay_Part3_Docker.
It does not copy or overwrite the Windows LocalDB database.
Docker uses persistent volumes for SQL data, profile images and authentication keys.

```powershell
docker compose config --quiet
docker compose --parallel 1 build
docker compose up -d --wait --wait-timeout 240
docker compose ps
docker compose logs --tail 50 web banner-api
```

Docker MVC: http://localhost:8080
Docker API health: http://localhost:5082/health

The SQL container must be healthy before MVC starts. MVC calls http://banner-api:8080/ internally.
The local and Docker databases have separate user accounts; register a user in the Docker application.
This classroom setup enables the existing development email sender; production requires a real email provider.

Stop without deleting database data:
```powershell
docker compose down
```

The .env created by setup contains generated SQL/API credentials and the Azure connection string.
Compose maps these variables to ASP.NET Core configuration:
SQL_SA_PASSWORD, BANNER_API_KEY, AZURE_BLOB_CONNECTION_STRING and AZURE_BLOB_CONTAINER_NAME.

## Verification
Recorded on 10 November 2026:
- 55 MVC/client tests passed.
- 11 API tests passed.
- 21 real local checks passed through MVC -> HTTP API -> Azure -> SQL -> MVC image display.
- Real checks included image replacement, removal of the previous image, event deletion and blob cleanup.
- Unauthenticated API uploads returned HTTP 401.
- Temporary verification users, events and Azure blobs were removed.
- Both applications compile and publish in Release configuration.

The GitHub workflow builds the whole solution and runs both test projects.
A separate Linux job builds the Docker images, starts MVC/API/SQL Server, checks login and API health,
and verifies API authentication and invalid-image rejection.
That Docker smoke check uses a development storage configuration, not production Azure credentials.
The real Azure upload flow is verified separately on the laptop.
Docker verification results must be taken from the completed workflow, not assumed from Dockerfile presence.

## User interface
Shared Bootstrap layout, form fields and validation maintain consistent navigation and styling.
Organiser and participant menus expose role-appropriate actions.
The Events table and Edit form follow the supplied examples.
A final visual walkthrough and video remain necessary; automated tests do not prove every visual detail.

## Video and submission
Unlisted YouTube walkthrough: **ADD YOUR PART 3 VIDEO LINK HERE**.

Show registration/login/logout, both role dashboards, event and category management, enrolments,
results, profile, the multipart MVC-to-API upload, the saved Azure image, MVC source structure,
Docker Compose services and the passing GitHub Actions run.

Before ARC submission:
- Confirm the final part3-mvc revision has passing checks.
- Review the commit history for at least 20 meaningful assessed-development commits.
- Include genuine application, Docker and CI screenshots.
- Replace the YouTube placeholder and submit the Part 3 branch link.
