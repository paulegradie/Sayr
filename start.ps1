$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

$localAiUrl = "http://localhost:8080"
$modelName = "whisper-1"
$modelUrl = "github:mudler/LocalAI/gallery/whisper-base.yaml@master"

function Wait-HttpReady {
    param(
        [string]$Url,
        [int]$Retries = 60,
        [int]$DelaySeconds = 2
    )

    for ($i = 0; $i -lt $Retries; $i++) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 | Out-Null
            return
        }
        catch {
            Start-Sleep -Seconds $DelaySeconds
        }
    }

    throw "Timeout waiting for $Url"
}

function Ensure-ModelInstalled {
    param(
        [string]$BaseUrl,
        [string]$Id,
        [string]$Url
    )

    $modelsUrl = "$BaseUrl/v1/models"
    $models = Invoke-RestMethod -Uri $modelsUrl -UseBasicParsing

    $found = $false
    if ($models.data) {
        $found = $models.data | Where-Object { $_.id -eq $Id } | ForEach-Object { $true }
    }

    if ($found) {
        Write-Host "Model '$Id' already installed."
        return
    }

    Write-Host "Installing model '$Id'..."
    $body = @{ id = $Id; url = $Url; name = $Id } | ConvertTo-Json
    $apply = Invoke-RestMethod -Method Post -Uri "$BaseUrl/models/apply" -ContentType "application/json" -Body $body

    if (-not $apply.uuid) {
        throw "Model install did not return a job id."
    }

    $jobUrl = "$BaseUrl/models/jobs/$($apply.uuid)"
    for ($i = 0; $i -lt 600; $i++) {
        $job = Invoke-RestMethod -Uri $jobUrl -UseBasicParsing
        if ($job.processed -eq $true) {
            Write-Host "Model install completed."
            return
        }
        Start-Sleep -Seconds 2
    }

    throw "Model install timed out."
}

try {
    Write-Host "Starting LocalAI..."
    Push-Location "infra/localai"
    docker compose up -d
    Pop-Location

    Write-Host "Waiting for LocalAI to be ready..."
    Wait-HttpReady -Url "$localAiUrl/v1/models"

    Write-Host "Ensuring model is installed..."
    Ensure-ModelInstalled -BaseUrl $localAiUrl -Id $modelName -Url $modelUrl

    Write-Host "Publishing Sayr tray app..."
    Write-Host "Stopping any running Sayr tray app..."
    Get-Process -Name "Sayr.Tray" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 250
    dotnet publish "apps/Sayr.Tray/Sayr.Tray.csproj" `
      --configuration Release `
      --runtime win-x64 `
      --self-contained true `
      -p:PublishSingleFile=true `
      -p:PublishTrimmed=false `
      -p:DebugType=none `
      --output ".\publish\win-x64"

    Write-Host "Launching Sayr..."
    Start-Process ".\publish\win-x64\Sayr.Tray.exe"
    exit 0
}
catch {
    Write-Host "Start failed: $($_.Exception.Message)"
    Read-Host "Press Enter to close"
    exit 1
}
