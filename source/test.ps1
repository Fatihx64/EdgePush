$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$exe = Join-Path $PSScriptRoot 'dist\EdgePush.exe'
foreach ($mode in @('--self-test','--diagnostics','--clock-test','--gradient-test')) {
    $process = Start-Process -FilePath $exe -ArgumentList $mode -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Check failed: $mode (exit $($process.ExitCode))" }
}
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'dist\self-test.txt'), (Join-Path $PSScriptRoot 'dist\ui-test.txt'), (Join-Path $PSScriptRoot 'dist\clock-test.txt')
