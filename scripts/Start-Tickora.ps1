param(
    [switch]$DatabaseOnly,
    [switch]$SkipBuild,
    [string]$WslDistribution = 'Ubuntu-24.04'
)
$ErrorActionPreference = 'Stop'
$tickoraRoot = Split-Path $PSScriptRoot -Parent
$tickoraTools = Join-Path $tickoraRoot '.tools'
New-Item -ItemType Directory -Force -Path $tickoraTools | Out-Null
$tickoraEnvFile = Join-Path $tickoraRoot '.env'
if (!(Test-Path -LiteralPath $tickoraEnvFile)) {
    $tickoraSqlSecret = 'Sql!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $tickoraDemoSecret = 'Demo!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
    Set-Content -LiteralPath $tickoraEnvFile -Value "SQL_PASSWORD=$tickoraSqlSecret`nDEMO_PASSWORD=$tickoraDemoSecret"
}
$tickoraSettings = @{}
Get-Content -LiteralPath $tickoraEnvFile | Where-Object { $_ -match '^[A-Z_]+=' } | ForEach-Object {
    $tickoraPair = $_.Split('=', 2); $tickoraSettings[$tickoraPair[0]] = $tickoraPair[1]
}
if (!$tickoraSettings.SQL_PASSWORD -or !$tickoraSettings.DEMO_PASSWORD) { throw 'Set SQL_PASSWORD and DEMO_PASSWORD in .env.' }
$tickoraLinuxPath = (& wsl -d $WslDistribution -- wslpath -a -u $tickoraRoot).Trim()
if ($LASTEXITCODE -ne 0) { throw 'The configured WSL distribution could not be started.' }
# systemd services alone do not keep WSL alive. An attached Docker event stream does.
$tickoraKeeperFile = Join-Path $tickoraTools 'wsl-keeper.pid'
$tickoraKeeper = if (Test-Path -LiteralPath $tickoraKeeperFile) {
    Get-CimInstance Win32_Process -Filter "ProcessId=$(Get-Content -LiteralPath $tickoraKeeperFile)" -ErrorAction SilentlyContinue
}
if (!$tickoraKeeper -or $tickoraKeeper.CommandLine -notlike '*docker events*label=com.docker.compose.project=tickora*') {
    $tickoraKeeperProcess = Start-Process -FilePath "$env:WINDIR\System32\wsl.exe" -ArgumentList @(
        '-d', $WslDistribution, '-u', 'root', '--', 'docker', 'events', '--filter', 'label=com.docker.compose.project=tickora'
    ) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $tickoraTools 'wsl-events.log') -RedirectStandardError (Join-Path $tickoraTools 'wsl-events.error.log')
    Set-Content -LiteralPath $tickoraKeeperFile -Value $tickoraKeeperProcess.Id
}
$tickoraWslIp = ((& wsl -d $WslDistribution -- hostname -I).Trim() -split '\s+')[0]
& wsl -d $WslDistribution -u root --cd $tickoraLinuxPath -- env "TICKORA_WSL_IP=$tickoraWslIp" docker compose -p tickora -f docker-compose.yml -f docker-compose.wsl.yml up -d tickora-sqlserver --wait
if ($LASTEXITCODE -ne 0) { throw 'SQL Server did not become healthy.' }
@{ SqlServer = "$tickoraWslIp,1433"; BaseUrl = 'http://localhost:5180'; ContainerUrl = "http://${tickoraWslIp}:5181"; WslDistribution = $WslDistribution } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $tickoraTools 'local.json')
Write-Host "SQL Server ready at ${tickoraWslIp},1433. Credentials are in the ignored .env file."
if ($DatabaseOnly) { return }

$tickoraDotnet = Join-Path $env:LOCALAPPDATA 'Tickora/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $tickoraDotnet)) { $tickoraDotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_ROOT = Split-Path $tickoraDotnet -Parent
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5180'
$env:ConnectionStrings__TickoraDb = "Server=$tickoraWslIp,1433;Database=TickoraDb;User ID=sa;Password=$($tickoraSettings.SQL_PASSWORD);TrustServerCertificate=true"
$env:Demo__Password = $tickoraSettings.DEMO_PASSWORD
$tickoraAppPidFile = Join-Path $tickoraTools 'web.pid'
if (Test-Path -LiteralPath $tickoraAppPidFile) {
    $tickoraOldApp = Get-CimInstance Win32_Process -Filter "ProcessId=$(Get-Content -LiteralPath $tickoraAppPidFile)" -ErrorAction SilentlyContinue
    if ($tickoraOldApp -and $tickoraOldApp.CommandLine.Contains($tickoraRoot) -and $tickoraOldApp.CommandLine.Contains('Tickora.Web.dll')) {
        Stop-Process -Id $tickoraOldApp.ProcessId
    }
}
Push-Location $tickoraRoot
try {
    if (!$SkipBuild) { & $tickoraDotnet build Tickora.slnx; if ($LASTEXITCODE -ne 0) { throw 'Build failed.' } }
    & $tickoraDotnet run --project src/Tickora.Web --no-build -- --migrate --seed
    if ($LASTEXITCODE -ne 0) { throw 'Database migration or development seed failed.' }
    $tickoraDll = Join-Path $tickoraRoot 'src/Tickora.Web/bin/Debug/net10.0/Tickora.Web.dll'
    $tickoraProcess = Start-Process -FilePath $tickoraDotnet -ArgumentList ('"' + $tickoraDll + '"') -WorkingDirectory (Join-Path $tickoraRoot 'src/Tickora.Web') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $tickoraTools 'web.log') -RedirectStandardError (Join-Path $tickoraTools 'web.error.log')
    Set-Content -LiteralPath $tickoraAppPidFile -Value $tickoraProcess.Id
    Write-Host 'TICKORA started at http://localhost:5180. Logs are in .tools/web.log.'
} finally { Pop-Location }
