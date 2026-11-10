$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$api = Join-Path $root 'Api\RaceDay.BannerApi.csproj'
$web = Join-Path $root 'RaCeDay Part 2.csproj'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet build (Join-Path $root 'RaCeDay Part2.sln')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Start-Process dotnet -WorkingDirectory $root -ArgumentList "run --no-build --project `"$api`" --urls http://127.0.0.1:5081"
dotnet run --no-build --project $web --launch-profile https
