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

Expand-Archive $Directory/*source*.zip -DestinationPath $Directory/src
pushd "$Directory/src/"
