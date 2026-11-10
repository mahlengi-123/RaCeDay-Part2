$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$webSecretsDir = Join-Path $env:APPDATA 'Microsoft\UserSecrets\aspnet-RaCeDay-c0fbb9d9-1562-492e-98f6-e50f4489c05b'
$webSecretsPath = Join-Path $webSecretsDir 'secrets.json'
if (!(Test-Path $webSecretsPath)) { throw 'Configure the MVC User Secrets, including Azure storage, first.' }
$settings = Get-Content $webSecretsPath -Raw | ConvertFrom-Json
$azure = $settings.'AzureBlob:ConnectionString'
if ([string]::IsNullOrWhiteSpace($azure)) { $azure = $settings.'AzureStorage:ConnectionString' }
if ([string]::IsNullOrWhiteSpace($azure)) { throw 'Azure storage is missing from MVC User Secrets.' }
$key = $settings.'BannerApi:ApiKey'
if ([string]::IsNullOrWhiteSpace($key)) { $key = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N') }
if ($key.Length -lt 32) { throw 'The configured API key must contain at least 32 characters.' }
$container = $settings.'AzureBlob:ContainerName'
if ([string]::IsNullOrWhiteSpace($container)) { $container = 'event-banners' }

$settings | Add-Member -NotePropertyName 'BannerApi:ApiKey' -NotePropertyValue $key -Force
$settings | Add-Member -NotePropertyName 'BannerApi:BaseUrl' -NotePropertyValue 'http://127.0.0.1:5081/' -Force
$settings | ConvertTo-Json -Depth 10 | Set-Content $webSecretsPath -Encoding UTF8

$apiSecretsDir = Join-Path $env:APPDATA 'Microsoft\UserSecrets\raceday-part3-banner-api'
New-Item -ItemType Directory $apiSecretsDir -Force | Out-Null
@{
    'BannerApi:ApiKey' = $key
    'AzureBlob:ConnectionString' = $azure
    'AzureBlob:ContainerName' = $container
} | ConvertTo-Json | Set-Content (Join-Path $apiSecretsDir 'secrets.json') -Encoding UTF8

$envPath = Join-Path $root '.env'
if (!(Test-Path $envPath)) {
    $sqlPassword = 'Rd3!' + [Guid]::NewGuid().ToString('N') + 'Aa9'
    @(
        '# Generated locally. Never commit this file.'
        "BANNER_API_KEY='$key'"
        "AZURE_BLOB_CONNECTION_STRING='$azure'"
        "AZURE_BLOB_CONTAINER_NAME='$container'"
        "SQL_SA_PASSWORD='$sqlPassword'"
    ) | Set-Content $envPath -Encoding UTF8
    Write-Output 'Created ignored Docker configuration.'
} else {
    Write-Output 'Existing Docker configuration preserved.'
}
Write-Output 'MVC and API User Secrets configured. No credentials were printed.'
