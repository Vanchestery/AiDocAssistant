# Build 00-banner.png from docs/screenshots (full-res, downscale only)
Add-Type -AssemblyName System.Drawing

$srcDir = Join-Path $PSScriptRoot "..\docs\screenshots" | Resolve-Path
$bannerPath = Join-Path $srcDir "00-banner.png"

function Resize-ToHeight {
    param([System.Drawing.Image]$Image, [int]$Height)
    $ratio = $Height / [double]$Image.Height
    $w = [Math]::Max(1, [int][Math]::Round($Image.Width * $ratio))
    $bmp = New-Object System.Drawing.Bitmap $w, $Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($Image, 0, 0, $w, $Height)
    $g.Dispose()
    return $bmp
}

function Crop-Image {
    param([System.Drawing.Image]$Image, [int]$X, [int]$Y, [int]$W, [int]$H)
    $rect = New-Object System.Drawing.Rectangle $X, $Y, $W, $H
    return $Image.Clone($rect, $Image.PixelFormat)
}

$bannerW = 1280; $bannerH = 640
$banner = New-Object System.Drawing.Bitmap $bannerW, $bannerH
$bg = [System.Drawing.Graphics]::FromImage($banner)
$bg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$bg.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$bg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (
    (New-Object System.Drawing.Rectangle 0, 0, $bannerW, $bannerH),
    [System.Drawing.Color]::FromArgb(255, 15, 23, 42),
    [System.Drawing.Color]::FromArgb(255, 30, 58, 95),
    [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal
)
$bg.FillRectangle($brush, 0, 0, $bannerW, $bannerH)
$brush.Dispose()

$titleFont = New-Object System.Drawing.Font "Segoe UI", 34, ([System.Drawing.FontStyle]::Bold)
$subFont = New-Object System.Drawing.Font "Segoe UI", 13
$tagFont = New-Object System.Drawing.Font "Segoe UI", 10.5
$bg.DrawString("AiDocAssistant", $titleFont, [System.Drawing.Brushes]::White, 40, 64)
$accent = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 96, 165, 250))
$muted = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 148, 163, 184))
$bg.DrawString("Document AI on .NET 8", $subFont, $accent, 40, 118)
$y = 185
foreach ($t in @("PDF extraction + OCR", "RAG with citations (pgvector)", "Agent tools + MCP for Cursor", "50 tests · 14 eval cases")) {
    $bg.DrawString([char]0x2022 + " " + $t, $tagFont, $muted, 44, $y)
    $y += 26
}
$titleFont.Dispose(); $subFont.Dispose(); $tagFont.Dispose(); $accent.Dispose(); $muted.Dispose()

$mcpFull = [System.Drawing.Image]::FromFile((Join-Path $srcDir "05-mcp-cursor.png"))
$mcpCrop = Crop-Image $mcpFull 280 0 ($mcpFull.Width - 280) $mcpFull.Height
$mcpFull.Dispose()
$mcpHero = Resize-ToHeight -Image $mcpCrop -Height 580
$mcpCrop.Dispose()
$mcpX = [Math]::Max(360, $bannerW - $mcpHero.Width - 16)
$bg.DrawImage($mcpHero, $mcpX, 30)
$mcpHero.Dispose()

$metricsFull = [System.Drawing.Image]::FromFile((Join-Path $srcDir "04-metrics.png"))
$metricsCrop = Crop-Image $metricsFull 0 80 $metricsFull.Width ($metricsFull.Height - 100)
$metricsFull.Dispose()
$metricsThumb = Resize-ToHeight -Image $metricsCrop -Height 140
$metricsCrop.Dispose()
$bg.DrawImage($metricsThumb, 380, 470)
$metricsThumb.Dispose()

$ragFull = [System.Drawing.Image]::FromFile((Join-Path $srcDir "06-rag-chat.png"))
$ragCrop = Crop-Image $ragFull 0 70 $ragFull.Width ($ragFull.Height - 90)
$ragFull.Dispose()
$ragThumb = Resize-ToHeight -Image $ragCrop -Height 140
$ragCrop.Dispose()
$bg.DrawImage($ragThumb, 640, 470)
$ragThumb.Dispose()

$bg.Dispose()
$banner.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Png)
$banner.Dispose()
Write-Host "Saved $bannerPath"
