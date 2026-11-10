# Part 3 Completion Implementation Plan

**Goal:** Complete MVC-to-API banner uploads, organise the Part 3 branch and verify Docker.
**Architecture:** Keep existing MVC pages and SQL Server entities. Add a separate .NET 8 banner API. MVC sends multipart requests with a server-only API key; only the API uploads/deletes Azure blobs. Docker Compose runs MVC, API and an isolated SQL Server database.
**Constraints:** Preserve user accounts, existing events, 2 MB image validation, antiforgery on MVC forms and current Git history. Never commit credentials. The local SQL database is not copied into or overwritten by Docker.

- [ ] Extract shared image validation and link storage contracts into Api/RaceDay.BannerApi.csproj.
- [ ] Add protected POST/DELETE /api/banners endpoints and GET /health.
- [ ] Register BannerApiClient as MVC's IBlobStorageService and show API failures as form errors.
- [ ] Add unit tests for API-key rejection, image validation, HTTP multipart requests, JSON results and API failures.
- [ ] Add API project to the solution and build both projects in CI on part3-mvc.
- [ ] Add Docker Compose with SQL health checks, persistent volumes and ignored environment configuration.
- [ ] Run automated tests and real MVC/API/Azure upload, replacement, display and cleanup checks.
- [ ] Build/run Docker; record actual results and any environment blocker.
- [ ] Update README to the actual commands and evidence; commit/publish part3-mvc.
