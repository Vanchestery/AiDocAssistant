# Pack screenshots WITHOUT upscaling (keeps native resolution, max width 1920)
Add-Type -AssemblyName System.Drawing

$assets = "C:\Users\ivan-\.cursor\projects\c-Users-ivan-source-repos-AiDocAssistant\assets"
$outDir = "C:\Users\ivan-\source\repos\AiDocAssistant\docs\screenshots"
$maxWidth = 1920  # only downscale if wider; NEVER upscale

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Save-Cropped {
    param(
        [string]$Source,
        [string]$Dest,
        [int]$Top = 90,
        [int]$Bottom = 48,
        [int]$Left = 0,
        [int]$MaxW = 1920
    )

    $img = [System.Drawing.Image]::FromFile($Source)
    $w = $img.Width - $Left
    $h = $img.Height - $Top - $Bottom
    if ($w -le 0 -or $h -le 0) { throw "Invalid crop for $Source" }

    $cropRect = New-Object System.Drawing.Rectangle $Left, $Top, $w, $h
    $cropped = $img.Clone($cropRect, $img.PixelFormat)
    $img.Dispose()

    $targetW = $cropped.Width
    $targetH = $cropped.Height
    if ($targetW -gt $MaxW) {
        $ratio = $MaxW / [double]$targetW
        $targetW = $MaxW
        $targetH = [Math]::Max(1, [int][Math]::Round($cropped.Height * $ratio))
    }

    $bmp = New-Object System.Drawing.Bitmap $targetW, $targetH
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.DrawImage($cropped, 0, 0, $targetW, $targetH)
    $g.Dispose()
    $cropped.Dispose()

    $bmp.Save($Dest, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Saved $Dest (${targetW}x${targetH}) from $([System.IO.Path]::GetFileName($Source))"
    $bmp.Dispose()
}

# Optional: drop PNGs with exact names here for full-res manual captures
$manualDir = "C:\Users\ivan-\OneDrive\Рабочий стол\AiDocAssistant_screenshots_raw"
if (Test-Path $manualDir) {
    Get-ChildItem $manualDir -Filter "*.png" | ForEach-Object {
        $dest = Join-Path $outDir $_.Name
        if ($_.Name -match '^\d{2}-') {
            Copy-Item $_.FullName $dest -Force
            Write-Host "Copied manual $($_.Name)"
        }
    }
}

$map = @{
    "01-documents.png"       = "c__Users_ivan-_AppData_Roaming_Cursor_User_workspaceStorage_431bb187fd1835756c7d3ecdf1ff55f7_images_image-1f6facec-d180-4a0c-a12a-6fddbaea567c.png"
    "02-extraction.png"      = "c__Users_ivan-_AppData_Roaming_Cursor_User_workspaceStorage_431bb187fd1835756c7d3ecdf1ff55f7_images_image-92eb1ce0-b9c3-4333-b9a9-a8c44ac0add2.png"
    "03-agent-reconcile.png" = "c__Users_ivan-_AppData_Roaming_Cursor_User_workspaceStorage_431bb187fd1835756c7d3ecdf1ff55f7_images_image-33fb0be3-df24-44b7-9092-98b62b948ffe.png"
    "04-metrics.png"         = "c__Users_ivan-_AppData_Roaming_Cursor_User_workspaceStorage_431bb187fd1835756c7d3ecdf1ff55f7_images_image-1924d067-b1c8-4eda-84df-c40d977a3577.png"
    "05-mcp-cursor.png"      = "c__Users_ivan-_AppData_Roaming_Cursor_User_workspaceStorage_431bb187fd1835756c7d3ecdf1ff55f7_images_2026-08-18_22-29-47-a7bf02bb-962c-444e-826c-a682707e6d7c.png"
    "06-rag-chat.png"        = "c__Users_ivan-_AppData_Roaming_Cursor_User_workspaceStorage_431bb187fd1835756c7d3ecdf1ff55f7_images_image-f4086db6-e90a-48bc-91d3-7f33c8349341.png"
}

foreach ($dest in $map.Keys) {
    $outPath = Join-Path $outDir $dest
    if (Test-Path $outPath) {
        # skip if manual full-res already copied
        $existing = [System.Drawing.Image]::FromFile($outPath)
        if ($existing.Width -gt 1100) { $existing.Dispose(); Write-Host "Skip $dest (manual/full-res exists)"; continue }
        $existing.Dispose()
    }
    $src = Join-Path $assets $map[$dest]
    if (-not (Test-Path $src)) { Write-Warning "Missing $src"; continue }
    $top = 90; $bottom = 48; $left = 0
    if ($dest -like "05-*") { $top = 0 }
    Save-Cropped -Source $src -Dest $outPath -Top $top -Bottom $bottom -Left $left -MaxW $maxWidth
}

Write-Host "`nFor BEST quality: save originals at 100% zoom to Desktop\AiDocAssistant_screenshots_raw\ as 01-documents.png ... then re-run this script."
