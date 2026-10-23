param([switch]$KeepDatabase)
$ErrorActionPreference = 'Stop'
$tickoraRoot = Split-Path $PSScriptRoot -Parent
$tickoraPidFile = Join-Path $tickoraRoot '.tools/web.pid'
if (Test-Path -LiteralPath $tickoraPidFile) {
    $tickoraProcess = Get-CimInstance Win32_Process -Filter "ProcessId=$(Get-Content -LiteralPath $tickoraPidFile)" -ErrorAction SilentlyContinue
    if ($tickoraProcess -and $tickoraProcess.CommandLine.Contains($tickoraRoot) -and $tickoraProcess.CommandLine.Contains('Tickora.Web.dll')) {
        Stop-Process -Id $tickoraProcess.ProcessId
    }
}
if (!$KeepDatabase) {
    $tickoraConfig = Join-Path $tickoraRoot '.tools/local.json'
    if (Test-Path -LiteralPath $tickoraConfig) {
        $tickoraLocal = Get-Content -LiteralPath $tickoraConfig -Raw | ConvertFrom-Json
        $tickoraLinuxPath = (& wsl -d $tickoraLocal.WslDistribution -- wslpath -a -u $tickoraRoot).Trim()
        $tickoraIp = $tickoraLocal.SqlServer.Split(',')[0]
        & wsl -d $tickoraLocal.WslDistribution -u root --cd $tickoraLinuxPath -- env "TICKORA_WSL_IP=$tickoraIp" docker compose -p tickora -f docker-compose.yml -f docker-compose.wsl.yml stop
        if ($LASTEXITCODE -ne 0) { throw 'Could not stop TICKORA containers.' }
        $tickoraKeeperFile = Join-Path $tickoraRoot '.tools/wsl-keeper.pid'
        if (Test-Path -LiteralPath $tickoraKeeperFile) {
            $tickoraKeeper = Get-CimInstance Win32_Process -Filter "ProcessId=$(Get-Content -LiteralPath $tickoraKeeperFile)" -ErrorAction SilentlyContinue
            if ($tickoraKeeper.CommandLine -like '*docker events*label=com.docker.compose.project=tickora*') { Stop-Process -Id $tickoraKeeper.ProcessId }
        }
    }
}
Write-Host 'TICKORA stopped. Database and private-file volumes are preserved.'
