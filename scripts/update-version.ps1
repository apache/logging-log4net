Param (
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]$OldVersion,
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]$NewVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Update-XmlVersion([System.IO.FileInfo]$XmlFile, [string]$Version, [string]$NodePath)
{
  [Xml]$XmlContent = Get-Content $XmlFile
  $Node = $XmlContent.SelectSingleNode($NodePath)
  $Node.InnerText = $Version
  "$($Node.OuterXml)"
  $XmlContent.Save($XmlFile)
}

function Update-JsonVersion([System.IO.FileInfo]$JsonFile, [string]$Version)
{
  $JsonContent = Get-Content $JsonFile | ConvertFrom-Json
  $JsonContent.version = $Version
  "$($JsonFile): $($JsonContent.version)"
  $JsonContent | ConvertTo-Json | Out-File $JsonFile
}

function Update-TextVersion([System.IO.FileInfo]$TextFile, [string]$OldVersion, [string]$NewVersion)
{
  $OldContent = Get-Content $TextFile | Out-String
  $NewContent = $OldContent.Replace($OldVersion, $NewVersion)
  "$($TextFile): $NewVersion"
  $Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False
  [System.IO.File]::WriteAllText($TextFile, $NewContent, $Utf8NoBomEncoding)
}

function New-ReleaseNotes([string]$Content, [string]$Version)
{
  [System.IO.FileInfo]$XmlFile = "$PSScriptRoot/../src/changelog/$Version/.release.xml"
  $XmlFile.Directory.Create()
  $Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False
  [System.IO.File]::WriteAllText($XmlFile, $Content, $Utf8NoBomEncoding)
  Copy-Item "$PSScriptRoot/../src/changelog/3.1.0/.release-notes.adoc.ftl" "$PSScriptRoot/../src/changelog/$Version/.release-notes.adoc.ftl"
}

Update-XmlVersion $PSScriptRoot/../pom.xml $NewVersion '/*[local-name()="project"]/*[local-name()="version"]'
Update-JsonVersion $PSScriptRoot/../package.json $NewVersion
Update-TextVersion $PSScriptRoot/../doc/MailTemplate.txt $OldVersion $NewVersion
Update-TextVersion $PSScriptRoot/../doc/MailTemplate.Result.txt $OldVersion $NewVersion
Update-TextVersion $PSScriptRoot/../doc/MailTemplate.Announce.txt $OldVersion $NewVersion
Update-TextVersion $PSScriptRoot/build-preview.ps1 $OldVersion $NewVersion
Update-TextVersion $PSScriptRoot/build-release.ps1 $OldVersion $NewVersion
Update-XmlVersion $PSScriptRoot/../src/log4net/log4net.csproj $NewVersion '/Project/PropertyGroup/Version'

$ReleaseNoteXml = '<?xml version="1.0" encoding="UTF-8"?>
<release xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xmlns="https://logging.apache.org/xml/ns"
         xsi:schemaLocation="https://logging.apache.org/xml/ns https://logging.apache.org/xml/ns/log4j-changelog-0.xsd"
         date="~Date~"
         version="~Version~"/>'.Replace('~Version~', $NewVersion).Replace('~Date~', (Get-Date).AddMonths(2).ToString('yyyy-MM-dd'))

New-ReleaseNotes $ReleaseNoteXml $NewVersion