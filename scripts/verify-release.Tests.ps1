#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

<#
.SYNOPSIS
  Checks that verify-release.ps1 fails closed.

.DESCRIPTION
  Only the stages before the KEYS download are covered, because everything after it needs the
  network and a gpg installation. Those stages are the ones that used to pass silently: a release
  with no artifacts, an artifact with no hash file, and an artifact whose hash does not match.

  Run with: Invoke-Pester ./scripts/verify-release.Tests.ps1
#>

BeforeAll {
  $script:VerifyRelease = Join-Path $PSScriptRoot 'verify-release.ps1'

  function New-ReleaseDirectory
  {
    $directory = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid()))
    return $directory.FullName
  }

  function Add-Artifact
  {
    param ([string]$Directory, [string]$Name = 'apache-log4net-binaries-9.9.9.zip', [switch]$WithHash, [string]$Hash)

    $path = Join-Path $Directory $Name
    'artifact contents' | Out-File -FilePath $path -Encoding ascii
    if ($WithHash)
    {
      if (!$Hash)
      {
        $Hash = (Get-FileHash -Algorithm SHA512 $path).Hash
      }
      "$Hash *$Name" | Out-File -FilePath "$path.sha512" -Encoding ascii
    }
    return $path
  }
}

Describe 'verify-release.ps1' {
  BeforeEach {
    $script:Directory = New-ReleaseDirectory
    $script:GnupgHomeBefore = $env:GNUPGHOME
  }

  AfterEach {
    Remove-Item $script:Directory -Recurse -Force -ErrorAction SilentlyContinue
  }

  It 'refuses a directory holding no artifacts' {
    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage 'No artifacts to verify*'
  }

  It 'refuses an artifact that has no hash file' {
    Add-Artifact -Directory $script:Directory | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage '*no apache-log4net-binaries-9.9.9.zip.sha512 to check it against*'
  }

  It 'refuses an artifact whose hash does not match' {
    Add-Artifact -Directory $script:Directory -WithHash -Hash ('0' * 128) | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage '*SHA-512 mismatch*'
  }

  It 'counts a KEYS file lying next to the artifacts as neither artifact nor evidence' {
    'planted' | Out-File -FilePath (Join-Path $script:Directory 'KEYS') -Encoding ascii

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage 'No artifacts to verify*'
  }

  It 'leaves GNUPGHOME alone when it fails before reaching gpg' {
    Add-Artifact -Directory $script:Directory | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } | Should -Throw

    $env:GNUPGHOME | Should -BeExactly $script:GnupgHomeBefore
  }
}
