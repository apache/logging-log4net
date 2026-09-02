#Requires -Version 7.4

param(
  $Version = '3.4.1',
  $Preview = '1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# $ErrorActionPreference alone does not apply to native commands: dotnet, gpg and git only set
# $LASTEXITCODE, so without this a failing build would still be signed and tagged.
# Only honored from PowerShell 7.4, hence the #Requires above.
$PSNativeCommandUseErrorActionPreference = $true

'building ...'
dotnet build -c Release "-p:GeneratePackages=true;PackageVersion=$Version-preview.$Preview" $PSScriptRoot/../src/log4net.sln
'signing ...'
gpg --armor --output $PSScriptRoot/../build/artifacts/log4net.$Version-preview.$Preview.nupkg.asc --detach-sig $PSScriptRoot/../build/artifacts/log4net.$Version-preview.$Preview.nupkg
gpg --armor --output $PSScriptRoot/../build/artifacts/log4net.Ext.Mail.$Version-preview.$Preview.nupkg.asc --detach-sig $PSScriptRoot/../build/artifacts/log4net.Ext.Mail.$Version-preview.$Preview.nupkg
'create tag?'
pause
'creating tag ...'
git tag "rc/$Version-preview.$Preview"
'pushing tag ...'
git push --tags
