# RaCeDay – Part 2: Web Application and API

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

- `AspNetUsers` and other Identity tables – user accounts and roles
- `Events` – event details
- `Categories` – event categories
- `Enrolments` – participant entries
- `Results` – race results

## Setup Instructions

1. Install **Visual Studio 2022** with the **ASP.NET and web development** workload, the appropriate **.NET SDK**, and **SQL Server LocalDB** (or a compatible SQL Server instance).
2. Clone the repository:

   ```bash
   git clone https://github.com/mahlengi-123/RaCeDay-Part2.git
   ```
3. Open the RaCeDay solution (`.sln` or `.slnx`) in Visual Studio.
4. Check the database connection string in `appsettings.json`. For LocalDB, use the correct server name, normally `(localdb)\\MSSQLLocalDB`, and the database name `RaCeDay_Complete_Dev`. **Do not commit passwords or private connection strings.**
5. Select **Build → Rebuild Solution** to restore packages and build the project.
6. Open **Tools → NuGet Package Manager → Package Manager Console** and run:

   ```powershell
   Update-Database -Context ApplicationDbContext
   ```
   This applies the existing EF Core migrations to the configured database. Do not rerun the separate Part 1 SQL creation script against this application database.
7. Press **F5** (or **Ctrl + F5**) to start the website.
8. Register a test user or sign in using an account already configured in your development database. If email confirmation is required, complete the configured confirmation process.

## Testing
- In Visual Studio, open **Test → Test Explorer → Run All Tests**.
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
