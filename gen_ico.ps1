Add-Type -AssemblyName System.Drawing
$inputPath = "C:\Users\r1otp\Downloads\b2772fdc-8746-42d4-a016-6926add54e8d_removalai_preview.png"
$outputPath = "d:\steam-presence-1.12.3\SteamPresenceUI\Assets\appicon.ico"

$source = [System.Drawing.Bitmap]::FromFile($inputPath)
$fs = [System.IO.File]::Create($outputPath)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICO Header
$bw.Write([byte]0x00); $bw.Write([byte]0x00); 
$bw.Write([uint16]1); 
$bw.Write([uint16]4); 

$sizes = @(16, 32, 48, 256)
$allData = New-Object "System.Collections.Generic.List[byte[]]"
$dataOffset = 6 + (16 * 4)

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($source, 0, 0, $s, $s)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    if ($s -eq 256) {
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    } else {
        # BITMAPINFOHEADER (40 bytes)
        [byte[]]$bih = New-Object byte[] 40
        [BitConverter]::GetBytes([int]40).CopyTo($bih, 0)
        [BitConverter]::GetBytes([int]$s).CopyTo($bih, 4)
        [BitConverter]::GetBytes([int]($s * 2)).CopyTo($bih, 8)
        [BitConverter]::GetBytes([uint16]1).CopyTo($bih, 12)
        [BitConverter]::GetBytes([uint16]32).CopyTo($bih, 14)
        # Size of image
        $stride = $s * 4
        $imgSize = $stride * $s
        $andSize = (($s + 31) -band -bnot 31) / 8 * $s
        [BitConverter]::GetBytes([int]($imgSize + $andSize)).CopyTo($bih, 20)
        $ms.Write($bih, 0, 40)

        # Pixels (bottom-up)
        $rect = New-Object System.Drawing.Rectangle(0, 0, $s, $s)
        $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $pixels = New-Object byte[] ($data.Stride * $s)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
        $bmp.UnlockBits($data)
        for ($y = $s - 1; $y -ge 0; $y--) {
            $ms.Write($pixels, $y * $data.Stride, $data.Stride)
        }

        # AND Mask (opaque)
        $andMask = New-Object byte[] $andSize
        $ms.Write($andMask, 0, $andMask.Length)
    }
    
    $bytes = $ms.ToArray()
    $allData.Add($bytes)
    
    # Directory Entry
    $val256 = if ($s -ge 256) { [byte]0 } else { [byte]$s }
    $bw.Write($val256) # Width
    $bw.Write($val256) # Height
    $bw.Write([byte]0); $bw.Write([byte]0); 
    $bw.Write([uint16]1); 
    $bw.Write([uint16]32); 
    $bw.Write([uint32]$bytes.Length); 
    $bw.Write([uint32]$dataOffset); 
    
    $dataOffset += $bytes.Length
    $bmp.Dispose()
    $ms.Dispose()
}

foreach ($d in $allData) { $bw.Write($d) }
$bw.Close(); $fs.Close(); $source.Dispose()
Write-Host "Definitive multi-size 32-bit ICO generated: $outputPath"
