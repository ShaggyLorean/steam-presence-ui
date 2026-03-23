Add-Type -AssemblyName System.Drawing
$pngPath = "D:\steam-presence-1.12.3\SteamPresenceUI\Assets\appicon.png"
$icoPath = "D:\steam-presence-1.12.3\SteamPresenceUI\Assets\appicon.ico"

$bmp = [System.Drawing.Bitmap]::FromFile($pngPath)

# Create a 32x32 ICO for compatibility
$newBmp = New-Object System.Drawing.Bitmap(32, 32)
$graphics = [System.Drawing.Graphics]::FromImage($newBmp)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.DrawImage($bmp, 0, 0, 32, 32)
$graphics.Dispose()

$hIcon = $newBmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fileStream = [System.IO.File]::Create($icoPath)
$icon.Save($fileStream)
$fileStream.Close()

$newBmp.Dispose()
$bmp.Dispose()
