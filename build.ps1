param([string]$OutputPath = (Join-Path $PSScriptRoot 'IPCountryTray.exe'))

$ErrorActionPreference = 'Stop'
$compilerPaths = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$csc = $compilerPaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) { throw 'Could not locate the .NET Framework C# compiler.' }

$source = Join-Path $PSScriptRoot 'Program.cs'
if (-not (Test-Path -LiteralPath $source)) { throw "Missing source file: $source" }

& $csc /nologo /target:winexe /optimize+ "/out:$OutputPath" `
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
    /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll `
    /reference:System.Security.dll $source

if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)." }
Write-Host "Build succeeded: $OutputPath"
