#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

<#
.SYNOPSIS
  Checks that verify-release.ps1 fails closed.

.DESCRIPTION
  Only the stages before the KEYS download are covered, because everything after it needs the
  network and a gpg installation. Those stages are the ones that used to pass silently: a release
  with no artifacts, an artifact with no hash file, an artifact whose hash does not match, an
  artifact set that disagrees with the manifest, and a source archive that is not the commit the
  manifest records.

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
    param ([string]$Directory, [string]$Name = 'apache-log4net-binaries-9.9.9.zip', [switch]$WithHash,
      [string]$Hash, [string]$Content = 'artifact contents')

    $path = Join-Path $Directory $Name
    # NoNewline, so an empty Content really is a zero byte file.
    $Content | Out-File -FilePath $path -Encoding ascii -NoNewline
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

  # A zip with a commit id in its archive comment, as git archive produces.
  function Add-SourceArchive
  {
    param ([string]$Directory, [string]$Commit, [string]$Name = 'apache-log4net-source-9.9.9.zip')

    $path = Join-Path $Directory $Name
    $zip = [System.IO.Compression.ZipFile]::Open($path, 'Create')
    try
    {
      $zip.Comment = $Commit
      $writer = New-Object System.IO.StreamWriter $zip.CreateEntry('README.md').Open()
      try { $writer.Write('sources') } finally { $writer.Dispose() }
    }
    finally
    {
      $zip.Dispose()
    }
    "$((Get-FileHash -Algorithm SHA512 $path).Hash) *$Name" | Out-File -FilePath "$path.sha512" -Encoding ascii
    return $path
  }

  # The manifest lists itself, as the one build-release.ps1 writes does.
  function Add-Manifest
  {
    param ([string]$Directory, [string]$Commit, [string[]]$Listed,
      [string]$Name = 'apache-log4net-9.9.9.manifest')

    $path = Join-Path $Directory $Name
    $lines = @()
    if ($Commit)
    {
      $lines += "commit=$Commit"
    }
    $lines += @(@($Listed) + $Name | ForEach-Object { "artifact=$_" })
    $lines | Out-File -FilePath $path -Encoding ascii
    "$((Get-FileHash -Algorithm SHA512 $path).Hash) *$Name" | Out-File -FilePath "$path.sha512" -Encoding ascii
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

  It 'refuses a release that has no manifest' {
    Add-SourceArchive -Directory $script:Directory -Commit ('a' * 40) | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage 'expected one .manifest file*found 0*'
  }

  It 'refuses an artifact the manifest does not list' {
    $source = Add-SourceArchive -Directory $script:Directory -Commit ('a' * 40)
    Add-Artifact -Directory $script:Directory -WithHash | Out-Null
    Add-Manifest -Directory $script:Directory -Commit ('a' * 40) -Listed (Split-Path $source -Leaf) | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage '*in the release but not listed, apache-log4net-binaries-9.9.9.zip*'
  }

  It 'refuses a release missing an artifact the manifest lists' {
    $source = Add-SourceArchive -Directory $script:Directory -Commit ('a' * 40)
    Add-Manifest -Directory $script:Directory -Commit ('a' * 40) `
      -Listed @((Split-Path $source -Leaf), 'apache-log4net-9.9.9.nupkg') | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage '*listed but not in the release, apache-log4net-9.9.9.nupkg*'
  }

  It 'refuses a manifest that records no commit' {
    $source = Add-SourceArchive -Directory $script:Directory -Commit ('a' * 40)
    Add-Manifest -Directory $script:Directory -Listed (Split-Path $source -Leaf) | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage '*expected one commit line, found 0*'
  }

  It 'refuses a source archive that is not the commit the manifest records' {
    $source = Add-SourceArchive -Directory $script:Directory -Commit ('a' * 40)
    Add-Manifest -Directory $script:Directory -Commit ('b' * 40) -Listed (Split-Path $source -Leaf) | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } |
      Should -Throw -ExpectedMessage '*built from commit aaa*but the release records bbb*'
  }

  It 'leaves GNUPGHOME alone when it fails before reaching gpg' {
    Add-Artifact -Directory $script:Directory | Out-Null

    { & $script:VerifyRelease -Directory $script:Directory } | Should -Throw

    $env:GNUPGHOME | Should -BeExactly $script:GnupgHomeBefore
  }
}
