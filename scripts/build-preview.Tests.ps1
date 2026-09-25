#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

<#
.SYNOPSIS
  Checks that build-preview.ps1 stops at the first failure and tags nothing unconfirmed.

.DESCRIPTION
  Runs the script against fake native commands, so nothing is built, signed, tagged or pushed.

  Run with: Invoke-Pester ./scripts/build-preview.Tests.ps1
#>

BeforeAll {
  . (Join-Path $PSScriptRoot 'FakeCommands.TestHelper.ps1')
}

Describe 'build-preview.ps1' {
  BeforeEach {
    $script:Root = New-ScratchTree -Script 'build-preview.ps1'
  }

  AfterEach {
    Remove-Item $script:Root -Recurse -Force -ErrorAction SilentlyContinue
  }

  It 'signs nothing when the build fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-preview.ps1' -Fail 'dotnet'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('dotnet build')
  }

  It 'tags nothing when signing fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-preview.ps1' -Fail 'gpg'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('dotnet build', 'gpg --armor')
  }

  It 'signs both packages and tags nothing without confirmation' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-preview.ps1'

    $result.ExitCode | Should -Not -Be 0
    $result.Output | Should -BeLike '*NonInteractive*'
    $result.Calls | Should -Be @('dotnet build', 'gpg --armor', 'gpg --armor')
  }
}
