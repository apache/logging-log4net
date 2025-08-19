Param (
  [Parameter()]
  [System.IO.DirectoryInfo]$Directory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (!$Directory)
{
  $Directory = $PSScriptRoot
}

function Verify-Hash
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The file containing the hash.')]
    [System.IO.FileInfo]$File
  ) 
  $Line = @(Get-Content $File.FullName)[0]
  $Fields = $Line -split '\s+'
  $Hash = $Fields[0].Trim().ToUpper()
  $Filename = $Fields[1].Trim()
  if ($Filename.StartsWith("*"))
  {
    $Filename = $Filename.Substring(1).Trim()
  }

  $ComputedHash = (Get-FileHash -Algorithm 'SHA512' "$($File.DirectoryName)/$Filename").Hash.ToUpperInvariant()

  if($Hash -eq $ComputedHash)
  {
    "$($Filename): Passed"
  }
  else
  {
    Write-Error "$($Filename): Not Passed" -ErrorAction Continue
    Write-Error "Read from file: $Hash" -ErrorAction Continue
    Write-Error "Computed: $ComputedHash"  -ErrorAction Continue
  }
}

foreach ($File in Get-ChildItem $Directory *.sha512)
{
  Verify-Hash $File
}

Invoke-WebRequest https://downloads.apache.org/logging/KEYS -OutFile $Directory/KEYS
gpg --import -q $Directory/KEYS

foreach ($File in Get-ChildItem $Directory *.asc)
{
  gpg --verify $File
}

Expand-Archive $Directory/*source*.zip -DestinationPath $Directory/src
pushd "$Directory/src/"
