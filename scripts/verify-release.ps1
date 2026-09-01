Param (
  [Parameter()]
  [System.IO.DirectoryInfo]$Directory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# $ErrorActionPreference alone does not apply to native commands: gpg only sets $LASTEXITCODE, so
# without this a failed signature check would still reach the extraction at the end and the script
# would exit 0. Requires PowerShell 7.3+.
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

# Everything that is not a hash, a signature or the key file has to be covered by both. Driving the
# checks from the artifacts, rather than from the .sha512 and .asc files that happen to be present,
# is what turns a missing signature into a failure instead of one loop iteration fewer.
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

# A key ring of its own, holding only the downloaded KEYS. Importing into the default key ring
# would accept a signature from any key this machine already has, not only from a key in the
# Logging Services KEYS file.
$KeyringDirectory = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid()))
try
{
  # Downloaded into that directory, and never read from the one being verified: a KEYS file sitting
  # next to the artifacts is not covered by any of the checks above, so importing it would let
  # anyone who can place a file there have their own key accepted as a release key.
  $Keys = Join-Path $KeyringDirectory 'KEYS'
  Invoke-WebRequest https://downloads.apache.org/logging/KEYS -OutFile $Keys

  $Keyring = Join-Path $KeyringDirectory 'logging-keys.gpg'
  gpg --no-default-keyring --keyring $Keyring --batch --quiet --import $Keys

  foreach ($Artifact in $Artifacts)
  {
    $Signature = "$($Artifact.FullName).asc"
    if (!(Test-Path $Signature))
    {
      throw "$($Artifact.Name): no $($Artifact.Name).asc to verify it with"
    }

    gpg --no-default-keyring --keyring $Keyring --batch --verify $Signature $Artifact.FullName
    "$($Artifact.Name): signature ok"
  }
}
finally
{
  Remove-Item $KeyringDirectory -Recurse -Force -ErrorAction SilentlyContinue
}

Expand-Archive $Directory/*source*.zip -DestinationPath $Directory/src
pushd "$Directory/src/"
