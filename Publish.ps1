Param (
    [Parameter(Mandatory = $true)][string]$Version
)

New-Item -ItemType Directory -Force -Path ./bin/Release | Out-Null

$Code = Get-Content -Path .\ImprovedPoV_Person.cs -Raw
$Code = $Code -Replace "#define POV_DIAGNOSTICS\r\n", ""
$Code = $Code -Replace "(?ms)^\s*#if ?\(POV_DIAGNOSTICS\).+?#endif", ""
$Code = $Code -Replace "\r\n\r\n\r\n", "`r`n`r`n"
$Code = $Code -Replace "0\.0\.0", "$Version"
$Code | Set-Content -Path bin/Release/ImprovedPoV_Person.cs

$Code = Get-Content -Path .\ImprovedPoV_Mirror.cs -Raw
$Code = $Code -Replace "#define POV_DIAGNOSTICS\r\n", ""
$Code = $Code -Replace "(?ms)^\s*#if ?\(POV_DIAGNOSTICS\).+?#endif", ""
$Code = $Code -Replace "\r\n\r\n\r\n", "`r`n`r`n"
$Code = $Code -Replace "0\.0\.0", "$Version"
$Code | Set-Content -Path bin/Release/ImprovedPoV_Mirror.cs

Write-Host "Release available at ./bin/Release"