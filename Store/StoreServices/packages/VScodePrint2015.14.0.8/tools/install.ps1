param($installPath, $toolsPath, $package, $project)

if (-not $toolsPath) { throw "toolsPath parameter wasn't specified" }

if ($dte.Version -eq "14.0") {
    $vsixFileName = "vscodeprint2015.vsix"

} else {
    throw "VScodePrint 2015 Extension is designed to work with Visual Studio 2015"
}

$vsxInstaller = [System.IO.Path]::Combine($toolsPath, $vsixFileName)
Start-Process -FilePath $vsxInstaller 