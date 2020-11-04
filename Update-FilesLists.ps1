cd (Split-Path ($MyInvocation.MyCommand.Path))

# Get source files
$sourceFiles = ( `
    ls ./src/*.cs -Recurse `
    | % { $_.FullName.Substring((pwd).Path.Length + 1) } `
)

# Embody.cslist
$sourceFiles > .\Embody.cslist

# Embody.csproj
( Get-Content ".\Embody.csproj" -Raw ) -Replace "(?sm)(?<=^ +<!-- SourceFiles -->`r?`n).*?(?=`r?`n +<!-- /SourceFiles -->)", `
    [System.String]::Join("`r`n", ($sourceFiles | % { "    <Compile Include=`"$_`" />" } ) ) `
| Set-Content ".\Embody.csproj" -NoNewline

# meta.json
$allFiles = (ls ./src/*.cs -Recurse) + (ls *.cslist) `
    | % { $_.FullName.Substring((pwd).Path.Length + 1) }
( Get-Content ".\meta.json" -Raw ) -Replace "(?sm)(?<=^  `"contentList`": \[`r?`n).*?(?=`r?`n  \],)", `
    [System.String]::Join("`r`n", ($allFiles | % { "    `"Custom\\Scripts\\AcidBubbles\\Embody\\$($_.Replace("\", "\\"))`"," } ) ).Trim(",") `
| Set-Content ".\meta.json" -NoNewline
