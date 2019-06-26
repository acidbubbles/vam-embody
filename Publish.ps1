Param (
    [Parameter(Mandatory = $true)][string]$Version
)

New-Item -ItemType Directory -Force -Path ./publish | Out-Null

$Code = Get-Content -Path .\ADD_THIS.cslist -Raw
$Code = $Code -Replace "Diagnostics.cs\r\n", ""
$Code | Set-Content -Path publish/ADD_THIS.cslist

Get-Content ADD_THIS.cslist | ForEach-Object {
    $Code = Get-Content -Path $_ -Raw
    $Code = $Code -Replace "#define POV_DIAGNOSTICS\r\n", ""
    $Code = $Code -Replace "(?ms)^\s*#if ?\(POV_DIAGNOSTICS\).+?#endif", ""
    $Code = $Code -Replace "\r\n\r\n\r\n", "`r`n`r`n"
    $Code = $Code -Replace "0\.0\.0", "$Version"
    $Code | Set-Content -Path ./public/src/$_.Filename
}

Write-Host "Release available at ./publish/"