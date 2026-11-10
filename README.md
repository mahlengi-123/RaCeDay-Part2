# RaCeDay â€“ Part 2: Web Application and API

## Portfolio of Evidence 

RaCeDay is an event management system for running, walking and cycling events. Organisers manage events and categories, while Participants can browse events, enrol and view their results.

Part 2 develops the system using **ASP.NET Core**, **C#**, **Entity Framework Core** and **Microsoft SQL Server**.

## Main Features

### Administrator
- Manage user accounts and roles.

### Organiser
- Create and manage events and categories.
- View event enrolments and manage race results.

### Participant
- Register, log in and manage their profile.
- Browse events and select a category.
- Enrol in events, view enrolments and check results.

## Technologies Used

- Visual Studio 2022 and C#
- ASP.NET Core MVC and ASP.NET Core Identity
- Entity Framework Core (Code First)
- SQL Server LocalDB / SQL Server Management Studio (SSMS)
- Git and GitHub
- Swagger/OpenAPI and automated tests, where configured

## Database

The development database is **`RaCeDay_Complete_Dev`**. Entity Framework Core migrations create and update the tables, including:

- `AspNetUsers` and other Identity tables â€“ user accounts and roles
- `Events` â€“ event details
- `Categories` â€“ event categories
- `Enrolments` â€“ participant entries
- `Results` â€“ race results

## Setup Instructions

1. Install **Visual Studio 2022** with the **ASP.NET and web development** workload, the appropriate **.NET SDK**, and **SQL Server LocalDB** (or a compatible SQL Server instance).
2. Clone the repository:

   ```bash
   git clone https://github.com/mahlengi-123/RaCeDay-Part2.git
   ```
3. Open the RaCeDay solution (`.sln` or `.slnx`) in Visual Studio.
4. Check the database connection string in `appsettings.json`. For LocalDB, use the correct server name, normally `(localdb)\\MSSQLLocalDB`, and the database name `RaCeDay_Complete_Dev`. **Do not commit passwords or private connection strings.**
5. Select **Build â†’ Rebuild Solution** to restore packages and build the project.
6. Open **Tools â†’ NuGet Package Manager â†’ Package Manager Console** and run:

   ```powershell
   Update-Database -Context ApplicationDbContext
   ```
   This applies the existing EF Core migrations to the configured database. Do not rerun the separate Part 1 SQL creation script against this application database.
7. Press **F5** (or **Ctrl + F5**) to start the website.
8. Register a test user or sign in using an account already configured in your development database. If email confirmation is required, complete the configured confirmation process.

## Testing
- In Visual Studio, open **Test â†’ Test Explorer â†’ Run All Tests**.
- Alternatively, run `dotnet test` from the solution folder if a test project is included.
- Check registration, login, role restrictions, event management and enrolments.
- If Swagger is configured, open its URL shown by the running application to test available API endpoints.
- Check the **Actions** tab on GitHub for the CI workflow result, if a workflow has been added.

## GitHub Repository
[RaCeDay Part 2 on GitHub](https://github.com/mahlengi-123/RaCeDay-Part2)
The repository  includes the source code, migrations, README, relevant tests and GitHub Actions workflow where required. Commit meaningful changes throughout development.

## CI_CD_Success.png
<img width="933" height="401" alt="image" src="https://github.com/user-attachments/assets/aa95e57f-ef04-4eab-9a2a-b052d9c830c6" />


## Demonstration Video
**Part 2 YouTube walkthrough:** _Add your video link here before submission._

The demonstration shows login and roles, event and category management, participant enrolment, SQL Server data, tests and the GitHub Actions result.

## Conclusion
RaCeDay Part 2 builds on the Part 1 database design by developing an ASP.NET Core application with user authentication, role-based access and event management features. The project should be tested against the assessment rubric before final submission.

## Part 3 repair and verification â€” 10 November 2026

The MVC views now use the standard `Views/<Controller>/<Action>.cshtml` structure.
The organiser login route opens the data-backed Dashboard. Participants have their own
dashboard, enrolments, results and profile navigation. Identity controls authentication
and roles; session records the signed-in user ID and role and is cleared on logout.
Only administrators can manage user roles.

The Events table and Edit form retain the simple Bootstrap layout shown in the assessment
examples. Create/Edit support optional 2 MB JPG, PNG, GIF and WebP banners; Details displays
the saved banner. Category Create/Edit/Delete pages are included. Events and categories
with enrolments cannot be deleted.

### Run and test

From the repository folder:

```powershell
dotnet restore "RaCeDay Part2.sln"
dotnet build "RaCeDay Part2.sln"
dotnet test "RaCeDay Part2.sln" --no-build
dotnet run --project "RaCeDay Part 2.csproj" --launch-profile https
```

Open https://localhost:7240. The 46 unit tests are in `Tests/` and reference the
actual MVC application. The solution includes them in Debug and Release builds.
A separate local HTTP verification exercised organiser password login, dashboard,
event creation/editing, categories, participant enrolment, results and logout:
48 checks passed, including a real Azure upload and a readable banner URL; temporary users, records and test uploads were removed.

### Configuration

Keep secrets in User Secrets or environment variables, never source control:

- `ConnectionStrings:DefaultConnection`: SQL Server connection string.
- `AzureBlob:ConnectionString`: banner storage; the existing
  `AzureStorage:ConnectionString` key is also accepted.
- `AzureBlob:ContainerName`: existing banner container, default `event-banners`.
- `SeedAdmin:Email` and `SeedAdmin:Password`: optional initial admin configuration.

Email confirmation remains enabled. Existing users' passwords and roles are not reset
by these repairs.

### Docker

A multi-stage .NET 8 Dockerfile publishes the application and runs as the .NET image's
non-root user on port 8080.

```powershell
docker build -t raceday .
docker run --rm -p 8080:8080 --env-file runtime.env raceday
```

Create an untracked `runtime.env` locally containing the required configuration,
using environment variable keys such as `ConnectionStrings__DefaultConnection`,
`AzureBlob__ConnectionString`, and `AzureBlob__ContainerName`.
Use a SQL Server instance reachable from the Linux container; Windows LocalDB and
Windows integrated authentication cannot be used as the container's SQL connection.
For a local classroom demonstration, `ASPNETCORE_ENVIRONMENT=Development` enables
the existing development email sender. For production, configure a real email sender.

Docker execution has not been verified: Docker Desktop's engine was not running
on the laptop during the repair.

### Remaining rubric evidence

- Banner uploads currently use the MVC storage service directly. The rubric's highest
  Azure band requires an MVC-to-API upload flow; that architecture is not implemented here.
- CI configuration is present; a successful GitHub Actions run after pushing these changes
  is still required as evidence.
- Record and link the Part 3 demonstration video and add genuine screenshots.
- Do not claim a particular commit count or Docker result without the corresponding evidence.





# RaceDay — Part 3
## PROG6212: Event Management System

## Project Overview

RaceDay is an ASP.NET Core MVC application for running, walking and cycling events.

Administrators manage user roles. Organisers manage events, categories, enrolments and results. Participants browse events, enrol and view their results.

## Technologies

- C# and .NET 8
- ASP.NET Core MVC and Razor Views
- ASP.NET Core Identity
- Entity Framework Core and SQL Server
- Azure Blob Storage
- Bootstrap, CSS and JavaScript
- Docker
- xUnit and GitHub Actions

## Authentication and Role-Based Navigation

The application supports registration, login and logout.

The three roles are Admin, Organiser and Participant. Authorisation restricts protected pages and actions according to the signed-in user's role.

Session stores the user's ID and role after login and is cleared on logout. Identity authentication cookies control authenticated access.

Email confirmation remains enabled.

## Organiser Interface

Organisers can:

- Access their dashboard.
- Create, view, edit and delete their own events.
- Upload and replace event banners.
- Create, edit and delete event categories.
- View participant enrolments for their events.
- Capture finishing times and positions after an event.

Events and categories with enrolments cannot be deleted.

## Participant Interface

Participants can:

- Browse events and view event details.
- Enter upcoming events by selecting a category.
- View their own enrolments.
- Withdraw before an event when no result exists.
- View finishing times and positions.
- Access their dashboard and profile.

## MVC Structure

### Models

Event, Category, Enrolment, Result and ApplicationUser represent the application's data.

### Views

Razor views are organised under:

- Views/Home
- Views/Dashboard
- Views/Events
- Views/Categories
- Views/Enrolments
- Views/Results
- Views/UserRoles
- Views/Shared

### Controllers

Controllers process requests, validate input, check permissions and communicate with the database and services.

Identity account pages handle registration, login, logout and profile management.

## Azure Blob Storage

Event banners are stored in the event-banners container.

Supported formats are JPG, JPEG, PNG, GIF and WebP, with a maximum size of 2 MB.

The application validates image uploads and saves the blob name and image URL in SQL Server. Event details display the saved banner. Editing an event allows the banner to be replaced.

The current implementation uploads directly through the MVC storage service.

Outstanding highest-band requirement:
The rubric requires the file to pass from MVC to an API, with the API uploading it to Azure and returning the URL. This separate upload flow is not yet implemented.

## Local Setup

1. Install the .NET 8 SDK and Visual Studio's ASP.NET development workload.
2. Configure a SQL Server connection.
3. Configure Azure Blob Storage through User Secrets.
4. Restore and build the solution.
5. Start the application.

Required configuration keys:

- ConnectionStrings:DefaultConnection
- AzureBlob:ConnectionString
- AzureBlob:ContainerName

The existing AzureStorage:ConnectionString key is also supported.

Do not commit credentials or runtime.env.

Commands:

    dotnet restore "RaCeDay Part2.sln"
    dotnet build "RaCeDay Part2.sln"
    dotnet run --project "RaCeDay Part 2.csproj" --launch-profile https

Local address:

https://localhost:7240

The application applies pending Entity Framework migrations during startup.

## Testing

The Tests folder contains automated tests referencing the application.

Recorded verification on 10 November 2026:

- 46 automated tests passed.
- 48 live checks passed, covering authentication, dashboards, event management, categories, enrolments, results, logout and Azure uploads.
- Temporary test accounts, records and uploaded images were removed.

Run the tests:

    dotnet test "RaCeDay Part2.sln"

These results do not replace a complete visual review or Docker demonstration.

## GitHub and CI/CD

Repository:

 https://github.com/mahlengi-123/RaCeDay-Part2

Workflow:

.github/workflows/dotnet.yml

The workflow restores dependencies, builds the solution including the MVC project, and runs automated tests.

Verified successful workflow:

https://github.com/mahlengi-123/RaCeDay-Part2/actions/runs/42898700669

Before submission:

- Place Part 3 in a clearly named branch or subfolder.
- Confirm at least 20 meaningful commits cover the assessed development.
- Check that the final submitted revision passes CI.

The previously checked repository contained 46 total commits. This alone does not establish that 20 commits specifically cover Part 3.

## Docker

The repository includes a multi-stage .NET 8 Dockerfile.

Build:

    docker build -t raceday .

Run:

    docker run --rm -p 8080:8080 --env-file runtime.env raceday

The runtime.env file must contain configuration for a SQL Server instance reachable from the container and Azure Blob Storage.

Windows LocalDB cannot run inside the Linux container.

Docker execution has not yet been verified. A successful container build and running application must be demonstrated before claiming this rubric requirement is complete.

## User Interface

The application uses shared layouts and Bootstrap components.

The final UI review must check:

- Consistent fonts, colours, spacing and buttons.
- Role-appropriate navigation.
- Clear validation and error messages.
- Responsive layouts.
- Readable event banners and status indicators.
- Keyboard access and visible focus.

## Demonstration Video link


## Screenshot of Event Baner
<img width="635" height="260" alt="BobBanner Image Azure Storage" src="https://github.com/user-attachments/assets/fab88013-b08f-4c31-98f2-d2749d97084d" />





## Submission Checklist

- [ ] Clearly named Part 3 branch or subfolder.
- [ ] At least 20 meaningful commits confirmed.
- [ ] Final GitHub Actions run passes.
- [ ] Source code and Dockerfile included.
- [ ] Docker build and run demonstrated.
- [ ] MVC-to-API upload requirement completed.
- [ ] Final UI consistency review completed.
- [ ] Genuine application and CI screenshots included.
- [ ] Unlisted YouTube link added to this README.
- [ ] Correct GitHub submission link submitted on ARC.

