# Not a hint: $PSNativeCommandUseErrorActionPreference below exists only from 7.4, and setting it
# on an older host is a silent no-op that leaves a failed signature check unnoticed.
#Requires -Version 7.4

Param (
  [Parameter()]
  [System.IO.DirectoryInfo]$Directory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# $ErrorActionPreference alone does not apply to native commands: gpg only sets $LASTEXITCODE, so
# without this a failed signature check would still reach the extraction at the end and the script
# would exit 0. Only honored from PowerShell 7.4, hence the #Requires above.
$PSNativeCommandUseErrorActionPreference = $true

if (!$Directory)
{
  $Directory = $PSScriptRoot
}

function Assert-Hash
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The artifact to check.')]
    [System.IO.FileInfo]$File
  )

  $HashFile = "$($File.FullName).sha512"
  if (!(Test-Path $HashFile))
  {
    throw "$($File.Name): no $($File.Name).sha512 to check it against"
  }

  $Hash = (@(Get-Content $HashFile)[0] -split '\s+')[0].Trim().ToUpperInvariant()
  $ComputedHash = (Get-FileHash -Algorithm 'SHA512' $File.FullName).Hash.ToUpperInvariant()
  if ($Hash -ne $ComputedHash)
  {
    throw "$($File.Name): SHA-512 mismatch, read $Hash but computed $ComputedHash"
  }

  "$($File.Name): hash ok"
}

# One file records the commit and the artifact set, and is signed with them.
function Get-Manifest
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The artifacts of the release.')]
    [System.IO.FileInfo[]]$Artifacts
  )

  $Manifest = @($Artifacts | Where-Object { $_.Extension -eq '.manifest' })
  if ($Manifest.Count -ne 1)
  {
    throw "expected one .manifest file describing the release, found $($Manifest.Count)"
  }

  return $Manifest[0]
}

# The hash and signature loops only see the files that are there, so without the listed set an
# artifact removed together with its .sha512 and .asc would pass.
function Assert-ArtifactSet
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The artifacts of the release.')]
    [System.IO.FileInfo[]]$Artifacts,
    [Parameter(Mandatory=$true, HelpMessage='The manifest listing them.')]
    [System.IO.FileInfo]$Manifest
  )

  $Listed = @(Get-Content $Manifest.FullName | Where-Object { $_ -like 'artifact=*' } |
    ForEach-Object { $_.Substring('artifact='.Length) })
  if ($Listed.Count -eq 0)
  {
    throw "$($Manifest.Name): lists no artifact"
  }

  $Present = @($Artifacts | ForEach-Object { $_.Name })
  $Missing = @($Listed | Where-Object { $_ -notin $Present })
  if ($Missing.Count -gt 0)
  {
    throw "$($Manifest.Name): listed but not in the release, $($Missing -join ', ')"
  }

  $Extra = @($Present | Where-Object { $_ -notin $Listed })
  if ($Extra.Count -gt 0)
  {
    throw "$($Manifest.Name): in the release but not listed, $($Extra -join ', ')"
  }

  "$($Manifest.Name): all $($Listed.Count) artifacts present"
}

# The manifest and the comment git archive writes into the zip are two independent records of the
# same commit.
function Assert-SourceCommit
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The artifacts of the release.')]
    [System.IO.FileInfo[]]$Artifacts,
    [Parameter(Mandatory=$true, HelpMessage='The manifest recording the commit.')]
    [System.IO.FileInfo]$Manifest
  )

  $Recorded = @(Get-Content $Manifest.FullName | Where-Object { $_ -like 'commit=*' } |
    ForEach-Object { $_.Substring('commit='.Length) })
  if ($Recorded.Count -ne 1)
  {
    throw "$($Manifest.Name): expected one commit line, found $($Recorded.Count)"
  }

  $SourceArchive = @($Artifacts | Where-Object { $_.Name -like '*source*.zip' })
  if ($SourceArchive.Count -ne 1)
  {
    throw "expected one source archive, found $($SourceArchive.Count)"
  }

  $Zip = [System.IO.Compression.ZipFile]::OpenRead($SourceArchive[0].FullName)
  $ArchivedCommit = $Zip.Comment
  $Zip.Dispose()
  if ($Recorded[0].Trim() -ne $ArchivedCommit)
  {
    throw "$($SourceArchive[0].Name): built from commit $ArchivedCommit but the release records $($Recorded[0])"
  }

  "$($SourceArchive[0].Name): commit $ArchivedCommit ok"
}

function Assert-Signature
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The artifacts of the release.')]
    [System.IO.FileInfo[]]$Artifacts
  )

  # A home of its own, so only the downloaded KEYS can verify. Not --keyring: gpg ignores that where
  # common.conf sets use-keyboxd.
  $GnupgHome = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid()))
  $PreviousGnupgHome = $env:GNUPGHOME
  $env:GNUPGHOME = $GnupgHome
  try
  {
    # Never the KEYS next to the artifacts: nothing above verifies it, so importing it would let
    # anyone who can write there supply a release key.
    $Keys = Join-Path $GnupgHome 'KEYS'
    Invoke-WebRequest https://downloads.apache.org/logging/KEYS -OutFile $Keys

    gpg --batch --quiet --import $Keys

    foreach ($Artifact in $Artifacts)
    {
      $Signature = "$($Artifact.FullName).asc"
      if (!(Test-Path $Signature))
      {
        throw "$($Artifact.Name): no $($Artifact.Name).asc to verify it with"
      }

      gpg --batch --verify $Signature $Artifact.FullName
      "$($Artifact.Name): signature ok"
    }
  }
  finally
  {
    # The daemons hold the directory open until told to stop. Wrapped, or a non-zero exit throws under
    # $PSNativeCommandUseErrorActionPreference and abandons the rest of the finally.
    try { gpgconf --kill all 2>&1 | Out-Null } catch { }
    $env:GNUPGHOME = $PreviousGnupgHome
    Remove-Item $GnupgHome -Recurse -Force -ErrorAction SilentlyContinue
  }
}

# Driven from the artifacts, not from the .sha512 and .asc files present, so a missing one fails
# instead of being one loop iteration fewer.
$Artifacts = @(Get-ChildItem $Directory -File |
  Where-Object { $_.Extension -notin '.asc', '.sha512' -and $_.Name -ne 'KEYS' })

if ($Artifacts.Count -eq 0)
{
  throw "No artifacts to verify in $Directory"
}

foreach ($Artifact in $Artifacts)
{
  Assert-Hash $Artifact
}

$Manifest = Get-Manifest $Artifacts
Assert-ArtifactSet $Artifacts $Manifest
Assert-SourceCommit $Artifacts $Manifest

Assert-Signature $Artifacts

Expand-Archive $Directory/*source*.zip -DestinationPath $Directory/src
Push-Location "$Directory/src/"
