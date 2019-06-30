Param (
    [Parameter(Mandatory = $true)][string]$Version
)

New-Item -ItemType Directory -Force -Path ./publish | Out-Null

$Code = Get-Content -Path .\src\ImprovedPoV.cs -Raw
$Code = $Code -Replace "#define POV_DIAGNOSTICS\r\n", ""
$Code = $Code -Replace "(?ms)^\s*#if ?\(POV_DIAGNOSTICS\).+?#endif", ""
$Code = $Code -Replace "\r\n\r\n\r\n", "`r`n`r`n"
$Code = $Code -Replace "0\.0\.0", "$Version"
$Code | Set-Content -Path ./publish/ImprovedPoV.cs

Write-Host "Release available at ./publish"