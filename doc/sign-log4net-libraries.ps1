# see https://infra.apache.org/release-signing#openpgp-ascii-detach-sig
$DidSomething = $false
$Files = Get-Item $PSScriptRoot/../build/artifacts/* -Include *log4net*.nupkg, *log4net*.zip
foreach ($File in $Files)
{
  $DidSomething = $true
  "signing: $File"
  gpg --armor --output "$($File.FullName).asc" --detach-sig $File.FullName
}

if (!$DidSomething)
{
  Write-Error "No log4net artifacts found - are you sure you're in the right directory?"
  exit 2
}