Param (
    [Parameter(Mandatory = $true)][string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path ./publish | Out-Null
New-Item -ItemType Directory -Force -Path ./publish/src | Out-Null
New-Item -ItemType Directory -Force -Path ./publish/src/Hair | Out-Null
New-Item -ItemType Directory -Force -Path ./publish/src/Skin | Out-Null

If(Test ImprovedPoV-$Version.zip) {
    Remove-Item ImprovedPoV-$Version.zip -Force | Out-Null
}

$Code = Get-Content -Path .\ADD_THIS.cslist -Raw
$Code = $Code -Replace "src/Diagnostics.cs\r\n", ""
$Code.Trim() | Set-Content -Path publish/ADD_THIS.cslist

Get-Content ADD_THIS.cslist | ForEach-Object {
    Write-Host Processing $_
    $Code = Get-Content -Path $_ -Raw
    $Code = $Code -Replace "#define POV_DIAGNOSTICS\r\n", ""
    $Code = $Code -Replace "(?ms)^\s*#if ?\(!?POV_DIAGNOSTICS\).+?#endif", ""
    $Code = $Code -Replace "\r\n\r\n\r\n", "`r`n`r`n"
    $Code = $Code -Replace "0\.0\.0", "$Version"
    $Code | Set-Content -Path ./publish/$_
}

7z a ImprovedPoV-$Version.zip ./publish/*

Write-Host "Release available at ./publish/ and package at ./ImprovedPoV-$Version.zip"