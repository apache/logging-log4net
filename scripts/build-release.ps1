param(
  $Version = '3.2.1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-HashAndSignature
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The file to hash.')]
    [System.IO.FileInfo]$File
  )
  $File.FullName
  $ComputedHash = (Get-FileHash -Algorithm 'SHA512' $File).Hash.ToLowerInvariant()
  $ComputedHash
  Set-Content -Path "$($File.FullName).sha512" -Value "$ComputedHash *./$($File.Name)"
  gpg --armor --output "$($File.FullName).asc" --detach-sig $File.FullName
}

"cleaning $PSScriptRoot/../build/ ..." 
Remove-Item $PSScriptRoot/../build/ -Force -Recurse -ErrorAction SilentlyContinue
'building ...'
dotnet test -c Release "-p:GeneratePackages=true;PackageVersion=$Version" $PSScriptRoot/../src/log4net/log4net.csproj
'compressing source ...'
pushd $PSScriptRoot/..
git archive --format=zip --output $PSScriptRoot/../build/artifacts/apache-log4net-source-$Version.zip master
popd
'compressing binaries ...'
Copy-Item $PSScriptRoot/verify-release.* $PSScriptRoot/../build/artifacts/
Copy-Item $PSScriptRoot/../LICENSE $PSScriptRoot/../build/Release/
Copy-Item $PSScriptRoot/../NOTICE $PSScriptRoot/../build/Release/
pushd $PSScriptRoot/../build/Release
zip -r $PSScriptRoot/../build/artifacts/apache-log4net-binaries-$Version.zip .
popd
'signing ...'
Move-Item $PSScriptRoot/../build/artifacts/log4net.$Version.nupkg $PSScriptRoot/../build/artifacts/apache-log4net.$Version.nupkg
Write-HashAndSignature $PSScriptRoot/../build/artifacts/apache-log4net.$Version.nupkg
Write-HashAndSignature $PSScriptRoot/../build/artifacts/apache-log4net-source-$Version.zip
Write-HashAndSignature $PSScriptRoot/../build/artifacts/apache-log4net-binaries-$Version.zip
Write-HashAndSignature $PSScriptRoot/../build/artifacts/verify-release.ps1
Write-HashAndSignature $PSScriptRoot/../build/artifacts/verify-release.sh
'cleaning site ...'
Remove-Item $PSScriptRoot/../target/ -Force -Recurse -ErrorAction SilentlyContinue
'building site ...'
pushd $PSScriptRoot/..
./mvnw site
popd
'creating tag ...'
pause
git tag "rc/$Version-rc1"
'pushing tag ...'
git push --tags
