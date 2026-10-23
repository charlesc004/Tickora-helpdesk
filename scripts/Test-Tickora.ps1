param([string]$BrowserChannel = 'msedge', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$tickoraRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Start-Tickora.ps1') -SkipBuild:$SkipBuild
$tickoraSettings = @{}
Get-Content -LiteralPath (Join-Path $tickoraRoot '.env') | Where-Object { $_ -match '^[A-Z_]+=' } | ForEach-Object {
    $tickoraPair = $_.Split('=', 2); $tickoraSettings[$tickoraPair[0]] = $tickoraPair[1]
}
$tickoraLocal = Get-Content -LiteralPath (Join-Path $tickoraRoot '.tools/local.json') -Raw | ConvertFrom-Json
$env:TICKORA_SQL_TESTS = '1'
$env:TICKORA_TEST_CONNECTION = "Server=$($tickoraLocal.SqlServer);Database=master;User ID=sa;Password=$($tickoraSettings.SQL_PASSWORD);TrustServerCertificate=true"
$env:TICKORA_E2E = '1'
$env:TICKORA_SMOKE = '1'
$env:TICKORA_BROWSER_CHANNEL = $BrowserChannel
$env:TICKORA_BASE_URL = $tickoraLocal.BaseUrl
$env:TICKORA_SCREENSHOTS = Join-Path $tickoraRoot 'docs/screenshots'
$env:Demo__Password = $tickoraSettings.DEMO_PASSWORD
for ($tickoraAttempt = 0; $tickoraAttempt -lt 30; $tickoraAttempt++) {
    try { $tickoraReady = Invoke-RestMethod "$($tickoraLocal.BaseUrl)/health/ready"; if ($tickoraReady.status -eq 'ready') { break } } catch { }
    Start-Sleep -Seconds 1
}
if ($tickoraReady.status -ne 'ready') { throw 'Application readiness did not succeed.' }
Push-Location $tickoraRoot
try {
    & dotnet test tests/Tickora.UnitTests --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed.' }
    & dotnet test tests/Tickora.IntegrationTests --no-build
    if ($LASTEXITCODE -ne 0) { throw 'SQL integration tests failed.' }
    & dotnet test tests/Tickora.E2ETests --no-build -- "Playwright.LaunchOptions.Channel=$BrowserChannel"
    if ($LASTEXITCODE -ne 0) { throw 'Browser tests failed.' }
    if (Select-String -LiteralPath (Join-Path $tickoraRoot '.tools/web.log') -Pattern '^fail:' -Quiet) {
        throw 'The application logged an unexpected error during acceptance tests. Inspect .tools/web.log.'
    }
} finally { Pop-Location }
