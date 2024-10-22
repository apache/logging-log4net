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

function Update-ReleaseNotes([System.IO.FileInfo]$XmlFile, [string]$Content, [string]$Version)
{
  [Xml]$XmlContent = Get-Content $XmlFile
  $NewChild = New-Object System.Xml.XmlDocument
  $NewChild.LoadXml($Content.Replace('%Version%', $Version))
  $Node = $XmlContent.SelectSingleNode('/document/body/section')
  $Node.PrependChild($XmlContent.ImportNode($NewChild.DocumentElement, $true))
  "$($XmlFile): $NewVersion"
  $XmlContent.Save($XmlFile)
}

Update-XmlVersion $PSScriptRoot/../pom.xml $NewVersion '/*[local-name()="project"]/*[local-name()="version"]'
Update-JsonVersion $PSScriptRoot/../package.json $NewVersion
Update-TextVersion $PSScriptRoot/../doc/MailTemplate.txt $OldVersion $NewVersion
Update-TextVersion $PSScriptRoot/../doc/MailTemplate.Result.txt $OldVersion $NewVersion
Update-TextVersion $PSScriptRoot/../doc/MailTemplate.Announce.txt $OldVersion $NewVersion
Update-XmlVersion $PSScriptRoot/../src/log4net/log4net.csproj $NewVersion '/Project/PropertyGroup/Version'

$ReleaseNoteSection = '
<section id="a%Version%" name="%Version%">
  <section id="a%Version%-breaking" name="Breaking Changes">
  </section>
  <br/>
  Apache log4net %Version% addresses reported issues:
  <section id="a%Version%-bug" name="Bug fixes">
    <ul>
    <li>
      <a href="https://github.com/apache/logging-log4net/issues/tbd">tbd</a> (by tbd)
    </li>
    </ul>
  </section>
  <section id="a%Version%-enhancements" name="Enhancements">
    <ul>
    <li>
      <a href="https://github.com/apache/logging-log4net/issues/tbd">tbd</a> (by tbd)
    </li>
    </ul>
  </section>
</section>'

#Update-ReleaseNotes $PSScriptRoot/../src/site/xdoc/release/release-notes.xml $ReleaseNoteSection $NewVersion