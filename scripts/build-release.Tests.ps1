#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

<#
.SYNOPSIS
  Checks that build-release.ps1 stops at the first failure and tags nothing unconfirmed.

.DESCRIPTION
  Runs the script against fake native commands, so nothing is built, signed, tagged or pushed.

  Run with: Invoke-Pester ./scripts/build-release.Tests.ps1
#>

BeforeAll {
  . (Join-Path $PSScriptRoot 'FakeCommands.TestHelper.ps1')
}

Describe 'build-release.ps1' {
  BeforeEach {
    $script:Root = New-ScratchTree -Script 'build-release.ps1'
  }

  AfterEach {
    Remove-Item $script:Root -Recurse -Force -ErrorAction SilentlyContinue
  }

  It 'signs nothing when the build fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' -Fail 'dotnet'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('git status', 'git rev-parse', 'dotnet test')
  }

  It 'builds no site when signing fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' -Fail 'gpg'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('git status', 'git rev-parse', 'dotnet test', 'git archive', 'zip -r',
      'gpg --armor')
  }

  It 'asks for no tag when the site build fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' -Fail 'mvnw'

    $result.ExitCode | Should -Not -Be 0
    $result.Output | Should -Not -BeLike '*NonInteractive*'
    $result.Calls[-1] | Should -Be 'mvnw site'
  }

  It 'signs all seven artifacts and tags nothing without confirmation' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1'

    $result.ExitCode | Should -Not -Be 0
    $result.Output | Should -BeLike '*NonInteractive*'
    $result.Calls | Should -Be @('git status', 'git rev-parse', 'dotnet test', 'git archive', 'zip -r',
      'gpg --armor', 'gpg --armor', 'gpg --armor', 'gpg --armor', 'gpg --armor', 'gpg --armor',
      'gpg --armor', 'mvnw site')
  }

  It 'ships no artifact without a hash' {
    Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' | Out-Null

    $artifacts = Get-ChildItem (Join-Path $script:Root 'build' 'artifacts') -Exclude '*.sha512', '*.asc'
    $artifacts.Name | Should -HaveCount 7
    $artifacts | Where-Object { !(Test-Path "$($_.FullName).sha512") } | Should -BeNullOrEmpty
  }

  It 'writes every hash as one LF-terminated line' {
    Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' | Out-Null

    $hashes = Get-ChildItem (Join-Path $script:Root 'build' 'artifacts') -Filter '*.sha512'
    $hashes | Should -HaveCount 7
    foreach ($hash in $hashes)
    {
      [System.IO.File]::ReadAllText($hash.FullName) | Should -MatchExactly '^[0-9a-f]{128} \*\./\S+\n\z'
    }
  }

  It 'removes the artifacts of an earlier run' {
    $stale = Join-Path $script:Root 'build' 'artifacts' 'apache-log4net-0.0.0.nupkg'
    New-Item -ItemType File -Force -Path $stale | Out-Null

    Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' | Out-Null

    Test-Path $stale | Should -BeFalse
  }

  It 'builds nothing when the working tree is dirty' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' -Dirty

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('git status')
  }

  It 'archives the commit it built' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1'

    $archive = @($result.Lines | Where-Object { $_ -like 'git archive*' })
    $archive | Should -HaveCount 1
    $archive[0] | Should -BeLike "* $script:FakeCommitHash"
  }

  It 'records the commit and the artifact set in the manifest' {
    Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' | Out-Null

    $artifacts = Join-Path $script:Root 'build' 'artifacts'
    $manifest = Get-ChildItem $artifacts -Filter '*.manifest'
    $manifest | Should -HaveCount 1
    $lines = Get-Content $manifest.FullName
    $lines | Should -Contain "commit=$script:FakeCommitHash"
    $listed = @($lines | Where-Object { $_ -like 'artifact=*' })
    $listed | Should -HaveCount 7
    $present = @(Get-ChildItem $artifacts -Exclude '*.sha512', '*.asc' | ForEach-Object { "artifact=$($_.Name)" })
    $listed | Sort-Object | Should -Be ($present | Sort-Object)
    # LF on every platform, or verify-release.sh compares names with a trailing CR.
    [System.IO.File]::ReadAllText($manifest.FullName) | Should -Not -BeLike "*`r*"
  }
}
