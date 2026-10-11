#Requires -Version 7.4

param(
  $Version = '3.5.1',
  [ValidateRange('Positive')]
  [int]$Rc = 1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# $ErrorActionPreference alone does not apply to native commands: dotnet, git, zip, gpg and mvnw
# only set $LASTEXITCODE, so without this a failing build would still be packaged and signed.
# Only honored from PowerShell 7.4, hence the #Requires above.
$PSNativeCommandUseErrorActionPreference = $true

Import-Module $PSScriptRoot/Common.psm1 -Force

$Root = "$PSScriptRoot/.."
$ArtifactDirectory = "$Root/build/artifacts"
$ManifestName = "apache-log4net-$Version.manifest"
$ArtifactNames = @(
  "apache-log4net.$Version.nupkg",
  "apache-log4net.Ext.Mail.$Version.nupkg",
  "apache-log4net-source-$Version.zip",
  "apache-log4net-binaries-$Version.zip",
  'verify-release.ps1',
  'verify-release.sh',
  $ManifestName)

# The binaries come from the working tree, the source archive from a git ref, and both are signed
# as one release, so they must come from one commit.
function Get-ReleaseCommit
{
  Invoke-InDirectory $Root {
    $GitStatus = git status --porcelain
    if ($GitStatus)
    {
      throw "the working tree is not clean, so the binaries and the source archive would not match:$([Environment]::NewLine)$($GitStatus -join [Environment]::NewLine)"
    }

    git rev-parse --verify HEAD
  }
}

# Records the commit and the artifact set, and is signed with them. Without the set a missing
# artifact goes unnoticed, since the verifiers can only check the files that are there.
function Write-Manifest
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The commit the release was built from.')]
    [string]$Commit,
    [Parameter(Mandatory=$true, HelpMessage='The artifact names, the manifest included.')]
    [string[]]$Names
  )

  # LF on every platform: Set-Content would write CRLF on Windows and the artifact names would then
  # carry a trailing CR into the comparison in verify-release.sh.
  $Lines = @("commit=$Commit") + ($Names | ForEach-Object { "artifact=$_" })
  Set-Content -Path $ArtifactDirectory/$ManifestName -NoNewline -Value (($Lines -join "`n") + "`n")
}

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
  # LF on every platform: the macOS sha512sum reads a CR from a Windows build as part of the file name.
  Set-Content -NoNewline -Path "$($File.FullName).sha512" -Value "$ComputedHash *./$($File.Name)`n"
  gpg --armor --output "$($File.FullName).asc" --detach-sig $File.FullName
}

"cleaning $Root/build/ ..."
Remove-Directory $Root/build/

'verifying release tree ...'
$CommitHash = Get-ReleaseCommit

'building ...'
dotnet test -c Release "-p:GeneratePackages=true;PackageVersion=$Version" $Root/src/log4net.sln

'compressing source ...'
Invoke-InDirectory $Root {
  git archive --format=zip --output $ArtifactDirectory/apache-log4net-source-$Version.zip $CommitHash
}

'compressing binaries ...'
Copy-Item $PSScriptRoot/verify-release.ps1, $PSScriptRoot/verify-release.sh $ArtifactDirectory/
Copy-Item $Root/LICENSE, $Root/NOTICE $Root/build/Release/
Invoke-InDirectory $Root/build/Release {
  zip -r $ArtifactDirectory/apache-log4net-binaries-$Version.zip .
}

'signing ...'
Move-Item $ArtifactDirectory/log4net.$Version.nupkg $ArtifactDirectory/apache-log4net.$Version.nupkg
Move-Item $ArtifactDirectory/log4net.Ext.Mail.$Version.nupkg $ArtifactDirectory/apache-log4net.Ext.Mail.$Version.nupkg
Write-Manifest -Commit $CommitHash -Names $ArtifactNames
foreach ($ArtifactName in $ArtifactNames)
{
  Write-HashAndSignature $ArtifactDirectory/$ArtifactName
}

'cleaning site ...'
Remove-Directory $Root/target/

'building site ...'
Invoke-InDirectory $Root { ./mvnw site }

# After the site: target/ was removed above and ./mvnw site writes into it.
'building API reference ...'
& $PSScriptRoot/build-api-docs.ps1 -Version $Version

'creating tag ...'
pause
git tag "rc/$Version-rc$Rc"
'pushing tag ...'
git push --tags
