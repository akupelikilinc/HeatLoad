Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

function RoundedPath([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

$sizes = 16, 20, 24, 32, 40, 48, 64
$colors = @(
    [System.Drawing.Color]::FromArgb(255, 91, 200, 255),
    [System.Drawing.Color]::FromArgb(255, 111, 231, 183),
    [System.Drawing.Color]::FromArgb(255, 183, 166, 255)
)
$heights = 0.42, 0.60, 0.78
$pngs = New-Object System.Collections.Generic.List[byte[]]

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $barW = [single]($s * 0.19)
    $gap = [single]($s * 0.09)
    $left = [single](($s - (3 * $barW + 2 * $gap)) / 2)
    $baseY = [single]($s * 0.88)

    for ($i = 0; $i -lt 3; $i++) {
        $h = [single]($s * $heights[$i])
        $path = RoundedPath $left ($baseY - $h) $barW $h ($barW / 2)
        $brush = New-Object System.Drawing.SolidBrush($colors[$i])
        $g.FillPath($brush, $path)
        $brush.Dispose(); $path.Dispose()
        $left += $barW + $gap
    }

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs.Add($ms.ToArray())
    $ms.Dispose(); $g.Dispose(); $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$i].Length)
    $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush()

$target = Join-Path $PSScriptRoot 'HeatLoad.App\Assets\app.ico'
[System.IO.File]::WriteAllBytes($target, $out.ToArray())
$bw.Dispose(); $out.Dispose()

$ico = New-Object System.Drawing.Icon($target)
"ico written: $target ($((Get-Item $target).Length) bytes, $($ico.Width)x$($ico.Height))"
