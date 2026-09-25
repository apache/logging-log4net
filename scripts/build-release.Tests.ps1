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
    $result.Calls | Should -Be @('dotnet test')
  }

  It 'builds no site when signing fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' -Fail 'gpg'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('dotnet test', 'git archive', 'zip -r', 'gpg --armor')
  }

  It 'asks for no tag when the site build fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' -Fail 'mvnw'

    $result.ExitCode | Should -Not -Be 0
    $result.Output | Should -Not -BeLike '*NonInteractive*'
    $result.Calls[-1] | Should -Be 'mvnw site'
  }

  It 'signs all six artifacts and tags nothing without confirmation' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1'

    $result.ExitCode | Should -Not -Be 0
    $result.Output | Should -BeLike '*NonInteractive*'
    $result.Calls | Should -Be @('dotnet test', 'git archive', 'zip -r',
      'gpg --armor', 'gpg --armor', 'gpg --armor', 'gpg --armor', 'gpg --armor', 'gpg --armor', 'mvnw site')
  }

  It 'ships no artifact without a hash' {
    Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' | Out-Null

    $artifacts = Get-ChildItem (Join-Path $script:Root 'build' 'artifacts') -Exclude '*.sha512', '*.asc'
    $artifacts.Name | Should -HaveCount 6
    $artifacts | Where-Object { !(Test-Path "$($_.FullName).sha512") } | Should -BeNullOrEmpty
  }

  It 'writes every hash as one LF-terminated line' {
    Invoke-InScratchTree -Root $script:Root -Script 'build-release.ps1' | Out-Null

    $hashes = Get-ChildItem (Join-Path $script:Root 'build' 'artifacts') -Filter '*.sha512'
    $hashes | Should -HaveCount 6
    foreach ($hash in $hashes)
    {
      [System.IO.File]::ReadAllText($hash.FullName) | Should -MatchExactly '^[0-9a-f]{128} \*\./\S+\n\z'
    }
  }
}
