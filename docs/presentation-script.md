# RaceDay Part 3 - three-minute presentation

Read at a steady pace, around 125 words per minute. Bracketed directions are not spoken.
Show only genuine running pages and completed verification results.

## 0:00 - Introduction and structure
[Show the homepage, then Models, Views and Controllers in Visual Studio.]

Hello. This is my RaceDay Part Three presentation for PROG6212.

RaceDay is an event management application for running, walking and cycling. It supports three roles: administrator, organiser and participant.

The application uses ASP.NET Core MVC, Entity Framework Core and SQL Server. Models represent data such as events, categories, enrolments and results. Controllers handle requests and business rules, while Razor views display the interface. ASP.NET Core Identity manages authentication and role-based access.

## 0:35 - Registration and organiser
[Show registration, log in as organiser, then dashboard, Create, Edit and Categories.]

The registration page validates user details. Email confirmation is required before login, and an administrator manages role assignments.

I will first sign in as an organiser. After login, the organiser dashboard displays their events and enrolment information.

On the Events page, I can create an event by entering its name, description, date, location, distance and event type. I can also select a banner image.

I can edit an event, replace its banner and manage its categories. Categories help participants select the appropriate entry option. The organiser can view enrolments and capture finishing times and positions after an event. Events with enrolments are protected from deletion.

## 1:25 - Participant
[Show a participant session, event details, My Enrolments, My Results and profile.]

Next, I will show the participant interface. Participants can browse events, view details, choose a category and enter an upcoming event. My Enrolments displays their entries, while My Results displays finishing times and positions. Participants can also manage their profile. Protected organiser actions are unavailable to participants.

## 1:55 - MVC to API and Azure
[Show the banner, BannerApiClient, BannersController and event-banners container.]

For banner uploads, the MVC application sends the selected image to a separate API using an HTTP multipart request. The API checks its server key and validates the image before uploading it to Azure Blob Storage.

The API returns the image URL and blob name. MVC saves these values in SQL Server and displays the banner on the event details page. Replacing a banner removes the previous image after the database update succeeds.

## 2:30 - Docker, GitHub and conclusion
[Show Dockerfiles, compose.yaml, the actual Docker job result, branch history and tests.]

The project includes Dockerfiles for the MVC application and API. Docker Compose connects both services to a separate SQL Server container. Configuration is supplied through environment variables, and credentials are excluded from Git.

Finally, the part3-mvc branch contains the submission work. GitHub Actions builds the solution and runs the automated tests. Sixty-six tests passed locally, and twenty-one live checks verified the complete banner flow. Logout clears the session and ends authenticated access.

Thank you for watching my RaceDay presentation.

---
Do not claim that Docker passed unless the Docker job or container run has actually completed successfully.
The hosted Docker smoke test uses development storage configuration; the real Azure upload flow was verified locally.
