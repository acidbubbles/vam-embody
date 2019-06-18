New-Item -ItemType Directory -Force -Path ./bin/Release | Out-Null

$Code = Get-Content -Path .\ImprovedPOV.cs -Raw
$Code = $Code -Replace "#define POV_DIAGNOSTICS\r\n", ""
$Code = $Code -Replace "(?ms)^\s*#if ?\(POV_DIAGNOSTICS\).+?#endif", ""
$Code = $Code -Replace "\r\n\r\n\r\n", "`r`n`r`n"
$Code | Set-Content -Path bin/Release/ImprovedPOV.cs

$Code = Get-Content -Path .\ImprovedPoVMirrorReflection.cs -Raw
$Code = $Code -Replace "#define POV_DIAGNOSTICS\r\n", ""
$Code = $Code -Replace "(?ms)^\s*#if ?\(POV_DIAGNOSTICS\).+?#endif", ""
$Code = $Code -Replace "\r\n\r\n\r\n", "`r`n`r`n"
$Code | Set-Content -Path bin/Release/ImprovedPoVMirrorReflection.cs

Write-Host "Release available at ./bin/Release"