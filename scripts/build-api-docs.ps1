#Requires -Version 7.4

<#
.SYNOPSIS
  Builds the API reference into target/site/api with docfx, warnings as errors.

.DESCRIPTION
  Configuration in src/site/docfx/docfx.json.

.PARAMETER Version
  The version named in the page titles. The VersionPrefix of src/Directory.Build.props by default.
#>

param(
  [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# $ErrorActionPreference alone does not apply to native commands: dotnet only sets $LASTEXITCODE,
# so without this a failed docfx run would still look like a built reference.
# Only honored from PowerShell 7.4, hence the #Requires above.
$PSNativeCommandUseErrorActionPreference = $true

Import-Module $PSScriptRoot/Common.psm1 -Force

$Root = "$PSScriptRoot/.."
$Config = "$Root/src/site/docfx/docfx.json"
$Output = "$Root/target/site/api"
$Footer = Get-ApiDocsFooter
if (!$Version)
{
  $Version = ([xml](Get-Content $Root/src/Directory.Build.props)).SelectSingleNode('/Project/PropertyGroup/VersionPrefix').InnerText
}
$Title = "Apache log4net $Version API Reference"

'cleaning API reference ...'
Remove-Directory $Root/src/site/docfx/log4net
Remove-Directory $Root/src/site/docfx/log4net.Ext.Mail
Remove-Directory $Output

'restoring docfx ...'
# The tool manifest in .config is found from the working directory, hence the cd.
Invoke-InDirectory $Root { dotnet tool restore }

'building API reference ...'
Invoke-InDirectory $Root { dotnet docfx $Config --warningsAsErrors -m "_appTitle=$Title" -m "_appFooter=$Footer" }

if (!(Test-Path $Output/index.html))
{
  throw "docfx produced no API reference at $Output"
}
