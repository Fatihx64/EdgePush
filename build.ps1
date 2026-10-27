$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'dist') | Out-Null
& $compiler /nologo /target:winexe /optimize+ /platform:x64 "/out:$PSScriptRoot\dist\EdgePush.exe" "/win32icon:$PSScriptRoot\assets\EdgePush.ico" "/resource:$PSScriptRoot\assets\EdgePush.ico,EdgePush.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\src\EdgePush.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\click.wav') -Destination (Join-Path $PSScriptRoot 'dist\click.wav') -Force
