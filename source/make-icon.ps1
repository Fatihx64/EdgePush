Add-Type -AssemblyName System.Drawing
Add-Type 'using System;using System.Runtime.InteropServices;public static class IconNative {[DllImport("user32.dll")]public static extern bool DestroyIcon(IntPtr icon);}'
$bitmap = New-Object Drawing.Bitmap 64,64
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = 'AntiAlias'
$graphics.Clear([Drawing.Color]::Transparent)
$background = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(20,27,37))
$graphics.FillEllipse($background,1,1,62,62)
$monitorPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(170,200,220)),3
$graphics.DrawRectangle($monitorPen,9,19,17,27)
$graphics.DrawRectangle($monitorPen,38,19,17,27)
$glowPen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(45,0,225,245)),11
$graphics.DrawLine($glowPen,32,11,32,53)
$linePen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(0,225,245)),3
$graphics.DrawLines($linePen,[Drawing.Point[]]@((New-Object Drawing.Point 33,10),(New-Object Drawing.Point 28,29),(New-Object Drawing.Point 36,34),(New-Object Drawing.Point 30,54)))
$handle=$bitmap.GetHicon()
$icon=[Drawing.Icon]::FromHandle($handle)
$stream=[IO.File]::Create((Join-Path $PSScriptRoot 'assets\EdgePush.ico'))
$icon.Save($stream)
$stream.Dispose()
$icon.Dispose()
[IconNative]::DestroyIcon($handle) | Out-Null
$linePen.Dispose();$glowPen.Dispose();$monitorPen.Dispose();$background.Dispose();$graphics.Dispose();$bitmap.Dispose()
