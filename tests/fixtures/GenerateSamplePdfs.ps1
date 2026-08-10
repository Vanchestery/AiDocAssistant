# Generates valid minimal PDFs for AiDocAssistant demos (PdfPig-compatible).
# Run: powershell -ExecutionPolicy Bypass -File tests/fixtures/GenerateSamplePdfs.ps1

$ErrorActionPreference = 'Stop'

function New-InvoicePdf {
    param([string]$Path, [string]$Total, [string]$Vat)

    $lines = @(
        'SCHET NA OPLATU No 2026-041 ot 15.07.2026',
        'Postavshchik: OOO SeverTread INN 7701234567',
        'Pokupatel: OOO KofePoint INN 7707654321',
        'Kofe Arabika 40 kg po 1250.00 = 50000.00',
        'Espresso 120 sht po 320.00 = 38400.00',
        'Stakan 60 up po 180.00 = 10800.00',
        'Sirop 25 sht po 540.00 = 13500.00',
        "Itogo: $Total RUB",
        "NDS: $Vat"
    )

    $streamLines = New-Object System.Collections.Generic.List[string]
    [void]$streamLines.Add('BT')
    [void]$streamLines.Add('/F1 11 Tf')
    [void]$streamLines.Add('50 750 Td')
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($i -gt 0) { [void]$streamLines.Add('0 -16 Td') }
        [void]$streamLines.Add("($($lines[$i])) Tj")
    }
    [void]$streamLines.Add('ET')
    $stream = ($streamLines -join "`n") + "`n"
    $streamBytes = [System.Text.Encoding]::ASCII.GetBytes($stream)

    $objects = New-Object System.Collections.Generic.List[string]
    [void]$objects.Add('1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj')
    [void]$objects.Add('2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj')
    [void]$objects.Add('3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj')
    [void]$objects.Add("4 0 obj<</Length $($streamBytes.Length)>>stream")
    [void]$objects.Add([System.Text.Encoding]::ASCII.GetString($streamBytes).TrimEnd())
    [void]$objects.Add('endstream')
    [void]$objects.Add('endobj')
    [void]$objects.Add('5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj')

    $body = "%PDF-1.4`n" + ($objects -join "`n") + "`n"
    $bodyBytes = [System.Text.Encoding]::ASCII.GetBytes($body)

    function Get-Offset([byte[]]$bytes, [string]$marker) {
        $m = [System.Text.Encoding]::ASCII.GetBytes($marker)
        for ($i = 0; $i -le $bytes.Length - $m.Length; $i++) {
            $match = $true
            for ($j = 0; $j -lt $m.Length; $j++) {
                if ($bytes[$i + $j] -ne $m[$j]) { $match = $false; break }
            }
            if ($match) { return $i }
        }
        throw "Marker not found: $marker"
    }

    $xrefLines = New-Object System.Collections.Generic.List[string]
    [void]$xrefLines.Add('xref')
    [void]$xrefLines.Add('0 6')
    [void]$xrefLines.Add('0000000000 65535 f ')
    for ($n = 1; $n -le 5; $n++) {
        $off = Get-Offset $bodyBytes "$n 0 obj"
        [void]$xrefLines.Add(('{0:D10} 00000 n ' -f $off))
    }

    $xrefPos = $bodyBytes.Length
    $tail = ($xrefLines -join "`n") + "`n" + 'trailer<</Size 6/Root 1 0 R>>' + "`n" + "startxref`n$xrefPos`n%%EOF`n"
    $all = New-Object byte[] ($bodyBytes.Length + [System.Text.Encoding]::ASCII.GetByteCount($tail))
    [Array]::Copy($bodyBytes, 0, $all, 0, $bodyBytes.Length)
    [Array]::Copy([System.Text.Encoding]::ASCII.GetBytes($tail), 0, $all, $bodyBytes.Length, [System.Text.Encoding]::ASCII.GetByteCount($tail))
    [System.IO.File]::WriteAllBytes($Path, $all)
}

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$fixtures = Join-Path $repoRoot 'tests\fixtures'
$desktop = [Environment]::GetFolderPath('Desktop')

New-InvoicePdf (Join-Path $fixtures 'sample-invoice-a-112700.pdf') '112700.00' '18783.33'
New-InvoicePdf (Join-Path $fixtures 'sample-invoice-b-115000.pdf') '115000.00' '19166.67'
New-InvoicePdf (Join-Path $desktop 'AiDocAssistant_schet_A_112700.pdf') '112700.00' '18783.33'
New-InvoicePdf (Join-Path $desktop 'AiDocAssistant_schet_B_115000.pdf') '115000.00' '19166.67'

Write-Host 'Sample PDFs generated OK.'
