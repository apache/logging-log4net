#Requires -Version 7.4

<#
.SYNOPSIS
  Builds the API reference for the versions already published in logging-log4net-site.

.DESCRIPTION
  A one-off for the versions published before 3.5.1, hence archived. Takes each
  log4net-<version> folder of a site checkout, downloads that release from nuget.org
  and builds its reference with docfx from the shipped dll and xml, as build-api-docs.ps1 does
  for the version being released. A folder with the old release/ layout gets release/sdk/,
  where the Sandcastle pages used to be, and an SDK Reference menu entry where the pages lack
  one; an Antora folder gets api/ and an API Reference link in its header, like the current
  site. Old doc comments carry unresolved crefs, so warnings are not errors here.

.PARAMETER SiteRoot
  The checkout of https://github.com/apache/logging-log4net-site.

.PARAMETER Version
  The versions to build, by folder name without the log4net- prefix, comma separated when
  called from a shell (-Version 2.0.8,3.4.0). All of them by default.
#>

param(
  [Parameter(Mandatory=$true)]
  [string]$SiteRoot,
  [string[]]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# $ErrorActionPreference alone does not apply to native commands: curl and dotnet only set
# $LASTEXITCODE. Only honored from PowerShell 7.4, hence the #Requires above.
$PSNativeCommandUseErrorActionPreference = $true

Import-Module $PSScriptRoot/../Common.psm1 -Force

$Root = "$PSScriptRoot/../.."
$Sources = "$Root/src/site/docfx"
$Work = "$Root/target/old-api"
$Footer = Get-ApiDocsFooter
# The 1.2.x releases went to nuget.org as 2.0.x (2.0.2 was never released).
$PackageVersions = @{ '1.2.12' = '2.0.1'; '1.2.13' = '2.0.3'; '1.2.14' = '2.0.4'; '1.2.15' = '2.0.5' }
# The largest surface first: the event log and ASP.NET types exist only on .NET Framework. The
# folder names changed with 2.0.9.
$Frameworks = 'net462', 'net45', 'net45-full', 'net40-full'
$FirstExtMailVersion = [version]'3.4.0'

function Get-Package
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The package id, in lower case as the flat container wants it.')]
    [string]$Id,
    [Parameter(Mandatory=$true, HelpMessage='The package version.')]
    [string]$PackageVersion,
    [Parameter(Mandatory=$true, HelpMessage='Where to unpack it.')]
    [string]$Directory
  )

  # A .zip name, since Expand-Archive refuses other extensions.
  $archive = "$Directory.zip"
  curl --fail -sSL -o $archive "https://api.nuget.org/v3-flatcontainer/$Id/$PackageVersion/$Id.$PackageVersion.nupkg"
  Expand-Archive -Path $archive -DestinationPath $Directory -Force
}

function Get-LibDirectory
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The unpacked package.')]
    [string]$Package,
    [Parameter(Mandatory=$true, HelpMessage='The lib folders to accept, preferred first.')]
    [string[]]$Names
  )

  foreach ($name in $Names)
  {
    if (Test-Path "$Package/lib/$name/log4net*.dll")
    {
      return "$Package/lib/$name"
    }
  }
  throw "$Package has none of the lib folders $($Names -join ', ')"
}

# The docfx sources for one version: the shared files from src/site/docfx, and a docfx.json,
# toc.yml and index.md that take the assemblies instead of the projects.
function Write-DocFxSources
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The folder to write into.')]
    [string]$Directory,
    [Parameter(Mandatory=$true, HelpMessage='The lib folder with log4net.dll and log4net.xml.')]
    [string]$Log4NetLib,
    [Parameter(HelpMessage='The lib folder with log4net.Ext.Mail.dll, if the release had it.')]
    [string]$ExtMailLib,
    [Parameter(Mandatory=$true, HelpMessage='The href of the manual, relative to the output folder.')]
    [string]$ManualHref
  )

  Remove-Directory $Directory
  New-Item -ItemType Directory -Path $Directory | Out-Null
  Copy-Item $Sources/logo.svg $Directory
  Copy-Item $Sources/template $Directory/template -Recurse
  Set-Content -Path $Directory/manual.md -Value "---`nredirect_url: $ManualHref`n---"

  $metadata = @(@{ src = @(@{ src = $Log4NetLib; files = @('log4net.dll') }); dest = 'log4net'
      namespaceLayout = 'nested'; memberLayout = 'samePage'; disableGitFeatures = $true })
  $toc = "- name: log4net`n  href: log4net/`n"
  $index = "# API Reference`n`nThis reference is generated from the XML documentation comments of the shipped assembly.`n" +
    "Start with the [log4net](xref:log4net) namespace."
  if ($ExtMailLib)
  {
    $metadata += @{ src = @(@{ src = $ExtMailLib; files = @('log4net.Ext.Mail.dll') })
      references = @(@{ src = $Log4NetLib; files = @('log4net.dll') }); dest = 'log4net.Ext.Mail'
      namespaceLayout = 'nested'; memberLayout = 'samePage'; disableGitFeatures = $true }
    $toc += "- name: log4net.Ext.Mail`n  href: log4net.Ext.Mail/`n"
    $index = $index.Replace('the shipped assembly', 'the two shipped assemblies').Replace('namespace.',
      "namespace, or with`n[log4net.Ext.Mail.Appender](xref:log4net.Ext.Mail.Appender) for the MailKit based SMTP appender.")
  }
  $toc += "- name: Manual`n  href: manual.md`n"
  $index += "`n`nConfiguration, examples and the FAQ are in the [manual](manual.md).`n"
  Set-Content -Path $Directory/toc.yml -Value $toc -NoNewline
  Set-Content -Path $Directory/index.md -Value $index -NoNewline

  $config = @{
    metadata = $metadata
    build = @{
      content = @(@{ files = @('index.md', 'manual.md', 'toc.yml', 'log4net/**/*.yml', 'log4net.Ext.Mail/**/*.yml') })
      resource = @(@{ files = @('logo.svg') })
      dest = '.'
      template = @('default', 'modern', 'template')
      globalMetadata = @{ _appName = 'Apache log4net'; _appLogoPath = 'logo.svg'; _appFaviconPath = 'logo.svg'
        _enableSearch = $true; _disableContribution = $true }
    }
  }
  Set-Content -Path $Directory/docfx.json -Value ($config | ConvertTo-Json -Depth 10)
}

# The Antora pages were generated before the API reference existed, so their header lacks the
# link that antora-playbook.yaml now adds. Inserted before the GitHub link, with the path
# relative to each page.
function Add-HeaderLink
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The version folder.')]
    [string]$Folder
  )

  $github = '<a class="navbar-item" href="https://github.com/apache/logging-log4net">GitHub</a>'
  foreach ($page in Get-ChildItem $Folder -Filter '*.html' -Recurse | Where-Object { $_.FullName -notlike "*$([IO.Path]::DirectorySeparatorChar)api$([IO.Path]::DirectorySeparatorChar)*" })
  {
    $html = Get-Content $page.FullName -Raw
    if (!$html -or !$html.Contains($github) -or $html.Contains('>API Reference</a>'))
    {
      continue
    }
    $depth = @($page.FullName.Substring($Folder.Length).Split([IO.Path]::DirectorySeparatorChar) | Where-Object { $_ }).Count - 1
    $href = ('../' * $depth) + 'api/index.html'
    $html = $html -replace "([ \t]*)<div class=`"navbar-end`">(\s*)$([regex]::Escape($github))",
      "`$1<div class=`"navbar-end`">`$2<a class=`"navbar-item`" href=`"$href`">API Reference</a>`$1</div>`n`$1<div class=`"navbar-end`">`$2$github"
    Set-Content -Path $page.FullName -Value $html -NoNewline
  }
}

# The Maven site pages of 2.0.17 and 3.0.x have no SDK Reference entry in their menu, unlike the
# versions before. Inserted after the FAQ entry, whose href also gives the depth of the page and
# which carries the class "active" on the FAQ page itself.
function Add-MenuLink
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The version folder.')]
    [string]$Folder
  )

  foreach ($page in Get-ChildItem $Folder -Filter '*.html' -Recurse | Where-Object { $_.FullName -notlike "*$([IO.Path]::DirectorySeparatorChar)sdk$([IO.Path]::DirectorySeparatorChar)*" })
  {
    $html = Get-Content $page.FullName -Raw
    if (!$html -or $html.Contains('>SDK Reference</a>') -or $html -notmatch 'title="FAQ">FAQ</a>')
    {
      continue
    }
    $entry = '$1<li class="none$2">$3<a href="$4faq.html" title="FAQ">FAQ</a>$5</li>' + "`n" +
      '$1<li class="none">$3<a href="$4sdk/index.html" title="SDK Reference">SDK Reference</a>$5</li>'
    $html = $html -replace '([ \t]*)<li class="none( active)?">(\s*)<a href="([^"]*)faq\.html" title="FAQ">FAQ</a>(\s*)</li>', $entry
    Set-Content -Path $page.FullName -Value $html -NoNewline
  }
}

if (!(Test-Path $SiteRoot/log4net-*))
{
  throw "$SiteRoot has no log4net-<version> folder, is it a checkout of logging-log4net-site?"
}
# pwsh -File hands a shell's "2.0.8,3.4.0" over as one string.
$Version = @($Version | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$folders = Get-ChildItem $SiteRoot -Directory -Filter 'log4net-*' |
  Where-Object { !$Version -or $Version -contains $_.Name.Substring(8) } |
  Sort-Object { [version]$_.Name.Substring(8) }

'restoring docfx ...'
Invoke-InDirectory $Root { dotnet tool restore }

foreach ($folder in $folders)
{
  $v = $folder.Name.Substring(8)
  "building API reference $v ..."
  $versionWork = "$Work/$v"
  Remove-Directory $versionWork
  New-Item -ItemType Directory -Path $versionWork | Out-Null

  $packageVersion = $PackageVersions[$v] ?? $v
  Get-Package -Id 'log4net' -PackageVersion $packageVersion -Directory $versionWork/log4net
  $log4NetLib = Get-LibDirectory -Package $versionWork/log4net -Names $Frameworks
  $extMailLib = ''
  if ([version]$v -ge $FirstExtMailVersion)
  {
    Get-Package -Id 'log4net.ext.mail' -PackageVersion $v -Directory $versionWork/log4net.Ext.Mail
    $extMailLib = Get-LibDirectory -Package $versionWork/log4net.Ext.Mail -Names 'netstandard2.0'
  }

  if (Test-Path "$($folder.FullName)/release")
  {
    $output = "$($folder.FullName)/release/sdk"
    $manualHref = '../../index.html'
    Add-MenuLink -Folder $folder.FullName
  }
  else
  {
    $output = "$($folder.FullName)/api"
    $manualHref = '../index.html'
    Add-HeaderLink -Folder $folder.FullName
  }

  Write-DocFxSources -Directory $versionWork/docfx -Log4NetLib $log4NetLib -ExtMailLib $extMailLib -ManualHref $manualHref
  Remove-Directory $output
  Invoke-InDirectory $Root {
    dotnet docfx $versionWork/docfx/docfx.json -o $output -m "_appTitle=Apache log4net $v API Reference" -m "_appFooter=$Footer"
  }
  if (!(Test-Path $output/index.html))
  {
    throw "docfx produced no API reference at $output"
  }
}
