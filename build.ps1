param(
    [ValidateSet("Build", "Publish")]
    [string]$Action = "Build"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "src\AudioMaxing\AudioMaxing.csproj"
$config = Join-Path $PSScriptRoot "NuGet.config"

dotnet restore $project --configfile $config
if ($Action -eq "Publish") {
    dotnet publish $project -c Release --self-contained false `
        --no-restore -o (Join-Path $PSScriptRoot "dist\AudioMaxing")
    Write-Host "Executável: dist\AudioMaxing\AudioMaxing.exe"
} else {
    dotnet build $project -c Release --no-restore
}
