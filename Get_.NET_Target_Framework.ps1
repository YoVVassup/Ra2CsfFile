$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-File `"$PSCommandPath`""
    exit
}

$versions = @("net40", "net45", "net451", "net452", "net46", "net461", "net462", "net47", "net471", "net472", "net48")

foreach ($version in $versions) {
    $url = "https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.$version"
    $outputZip = Join-Path -Path $env:TEMP -ChildPath "Microsoft.NETFramework.ReferenceAssemblies.$version.zip"
    $outputFolder = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\"

    Invoke-WebRequest -Uri $url -OutFile $outputZip
    Expand-Archive -Path $outputZip -DestinationPath $outputFolder\tmp -force
    xcopy "$outputFolder\tmp\build\.NETFramework\*" $outputFolder /E /H /Y
    Remove-Item $outputZip -Force
    Remove-Item $outputFolder\tmp -Recurse -Force
}