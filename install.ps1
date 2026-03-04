# install.ps1

Write-Host "=== ChatApp Installer ===" -ForegroundColor Cyan

# 1. Verifica .NET 8
Write-Host "`nChecking .NET 8..." -ForegroundColor Yellow
$dotnet = dotnet --version 2>$null
if (-not $dotnet -or -not $dotnet.StartsWith("8.")) {
    Write-Host "NET 8 not found. Download at: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}
Write-Host ".NET $dotnet found." -ForegroundColor Green

# 2. Verifica Docker
Write-Host "`nChecking Docker..." -ForegroundColor Yellow
$docker = docker --version 2>$null
if (-not $docker) {
    Write-Host "Docker not found. Download at: https://www.docker.com/products/docker-desktop" -ForegroundColor Red
    Start-Process "https://www.docker.com/products/docker-desktop"
    exit 1
}
Write-Host "$docker found." -ForegroundColor Green

# 3. Sobe RabbitMQ
Write-Host "`nStarting RabbitMQ..." -ForegroundColor Yellow
$existing = docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq "rabbitmq" }
if ($existing) {
    docker start rabbitmq | Out-Null
} else {
    docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management | Out-Null
}
Write-Host "RabbitMQ started." -ForegroundColor Green

# 4. Aguarda RabbitMQ estar pronto
Write-Host "`nWaiting for RabbitMQ to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 5. Build
Write-Host "`nBuilding application..." -ForegroundColor Yellow
dotnet build ChatApp.sln -c Release --nologo -q
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit 1
}
Write-Host "Build successful." -ForegroundColor Green

# 6. Inicia API em background
Write-Host "`nStarting API..." -ForegroundColor Yellow
$api = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/ChatApp.Api/ChatApp.Api.csproj --no-build -c Release" `
    -PassThru -WindowStyle Normal
Write-Host "API started (PID $($api.Id))." -ForegroundColor Green

# 7. Aguarda API estar pronta
Write-Host "Waiting for API to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 8. Inicia Bot em background
Write-Host "`nStarting Bot..." -ForegroundColor Yellow
$bot = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/ChatApp.Bot/ChatApp.Bot.csproj --no-build -c Release" `
    -PassThru -WindowStyle Normal
Write-Host "Bot started (PID $($bot.Id))." -ForegroundColor Green

# 9. Abre o browser
Write-Host "`nOpening browser..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
Start-Process "https://localhost:7001"

Write-Host "`n=== ChatApp is running! ===" -ForegroundColor Cyan
Write-Host "API: https://localhost:7001" -ForegroundColor White
Write-Host "Swagger: https://localhost:7001/swagger" -ForegroundColor White
Write-Host "RabbitMQ: http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host "`nPress any key to stop all services..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# 10. Para tudo
Write-Host "`nStopping services..." -ForegroundColor Yellow
Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $bot.Id -Force -ErrorAction SilentlyContinue
docker stop rabbitmq | Out-Null
Write-Host "All services stopped." -ForegroundColor Green