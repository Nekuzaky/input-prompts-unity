Add-Type -AssemblyName System.Drawing

$pack = 'D:\P\input-prompts-unity\Assets\Sprites\input-prompts'
$out  = 'D:\P\input-prompts-unity\.github\social-preview.jpg'

$W = 1280; $H = 640
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# Background: vertical gradient from the dashboard palette.
$rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H)
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(28, 31, 38),
    [System.Drawing.Color]::FromArgb(17, 19, 23),
    90)
$g.FillRectangle($grad, $rect)

$accent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(58, 128, 232))
$g.FillRectangle($accent, (New-Object System.Drawing.Rectangle(0, 0, $W, 6)))

# One icon per family, picked so no two read the same at a glance.
$icons = @(
  "$pack\Keyboard & Mouse\Double\keyboard_w.png",
  "$pack\Keyboard & Mouse\Double\keyboard_a.png",
  "$pack\Keyboard & Mouse\Double\keyboard_s.png",
  "$pack\Keyboard & Mouse\Double\keyboard_d.png",
  "$pack\Keyboard & Mouse\Double\mouse_left.png",
  "$pack\Xbox Series\Double\xbox_button_color_a.png",
  "$pack\PlayStation Series\Double\playstation_button_color_cross.png",
  "$pack\PlayStation Series\Double\playstation_button_color_circle.png",
  "$pack\Nintendo Switch\Double\switch_button_plus.png"
)

$size = 88
$gap = 24
$stripW = ($size * $icons.Count) + ($gap * ($icons.Count - 1))
$x = [int](($W - $stripW) / 2)
$y = 132

foreach ($path in $icons) {
    if (Test-Path -LiteralPath $path) {
        $img = [System.Drawing.Image]::FromFile($path)
        $g.DrawImage($img, (New-Object System.Drawing.Rectangle($x, $y, $size, $size)))
        $img.Dispose()
    }
    $x += $size + $gap
}

function Write-Centered([string]$text, [System.Drawing.Font]$font, [System.Drawing.Color]$color, [int]$top) {
    $brush = New-Object System.Drawing.SolidBrush($color)
    $measured = $g.MeasureString($text, $font)
    $g.DrawString($text, $font, $brush, [single](($W - $measured.Width) / 2), [single]$top)
    $brush.Dispose()
}

$titleFont = New-Object System.Drawing.Font('Segoe UI Semibold', 60, [System.Drawing.FontStyle]::Bold)
$subFont   = New-Object System.Drawing.Font('Segoe UI', 21)
$metaFont  = New-Object System.Drawing.Font('Segoe UI', 16)
$urlFont   = New-Object System.Drawing.Font('Consolas', 16)

Write-Centered 'Input Prompts' $titleFont ([System.Drawing.Color]::FromArgb(236, 239, 245)) 276
Write-Centered 'The right key or button icon, automatically,' $subFont ([System.Drawing.Color]::FromArgb(152, 159, 174)) 392
Write-Centered 'as the player switches device' $subFont ([System.Drawing.Color]::FromArgb(152, 159, 174)) 426
Write-Centered 'Unity 6     Input System     Kenney icons     MIT' $metaFont ([System.Drawing.Color]::FromArgb(104, 111, 127)) 494
Write-Centered 'github.com/Nekuzaky/input-prompts-unity' $urlFont ([System.Drawing.Color]::FromArgb(88, 146, 240)) 552

$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$params = New-Object System.Drawing.Imaging.EncoderParameters(1)
$params.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, 94)

New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
$bmp.Save($out, $codec, $params)

$g.Dispose(); $bmp.Dispose()
Write-Output "saved $out"
