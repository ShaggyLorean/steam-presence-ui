$PublishDir = "D:\steam-presence-1.12.3\SteamPresenceUI\bin\Release\net9.0-windows10.0.17763.0\win-x64\publish"
$InstallerDir = "D:\steam-presence-1.12.3\SteamPresenceInstaller"
$PayloadDir = "D:\steam-presence-1.12.3\Payload"
$FinalSetupPath = "D:\steam-presence-1.12.3\Setup_SteamPresence.exe"

# 1. Build UI
Write-Host "1. Building SteamPresenceUI (Release, win-x64, Self-contained)..."
cd "D:\steam-presence-1.12.3\SteamPresenceUI"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 2. Prepare payload directory
Write-Host "2. Preparing payload directory..."
if (Test-Path $PayloadDir) { Remove-Item -Recurse -Force $PayloadDir }
New-Item -ItemType Directory -Force -Path $PayloadDir
Copy-Item "$PublishDir\SteamPresenceUI.exe" -Destination $PayloadDir
Copy-Item "D:\steam-presence-1.12.3\main.py" -Destination $PayloadDir
Copy-Item "D:\steam-presence-1.12.3\requirements.txt" -Destination $PayloadDir
Copy-Item "D:\steam-presence-1.12.3\runningApps.py" -Destination $PayloadDir
Copy-Item "D:\steam-presence-1.12.3\config.json" -Destination $PayloadDir
Copy-Item -Path "D:\steam-presence-1.12.3\data" -Destination $PayloadDir -Recurse

# 3. Creating payload.zip for embedding
Write-Host "3. Creating payload.zip..."
$ZipPath = "$InstallerDir/payload.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath }
Compress-Archive -Path "$PayloadDir/*" -DestinationPath $ZipPath

# 4. Build Installer (Clean + Publish to ensure embedding)
Write-Host "4. Building Installer (Self-contained, with Embedded Payload)..."
cd $InstallerDir
# Ensure we pick up the NEW payload.zip
dotnet clean -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 5. Final Copy
$PublishedInstaller = "$InstallerDir/bin/Release/net9.0/win-x64/publish/SteamPresenceInstaller.exe"
if (Test-Path $PublishedInstaller) {
    Copy-Item $PublishedInstaller $FinalSetupPath -Force
    Write-Host "Success! Final Setup created at: $FinalSetupPath"
} else {
    Write-Error "Error: Could not find published installer at $PublishedInstaller"
}

# 6. Also copy UI exe for convenience
Copy-Item "$PublishDir\SteamPresenceUI.exe" "D:\steam-presence-1.12.3\SteamPresenceUI.exe" -Force
