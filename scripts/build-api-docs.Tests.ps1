#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

<#
.SYNOPSIS
  Checks that build-api-docs.ps1 stops at the first failure and leaves no stale page behind.

.DESCRIPTION
  Runs the script against fake native commands, so docfx is neither restored nor run.

  Run with: Invoke-Pester ./scripts/build-api-docs.Tests.ps1
#>

BeforeAll {
  . (Join-Path $PSScriptRoot 'FakeCommands.TestHelper.ps1')
}

Describe 'build-api-docs.ps1' {
  BeforeEach {
    $script:Root = New-ScratchTree -Script 'build-api-docs.ps1'
  }

  AfterEach {
    Remove-Item $script:Root -Recurse -Force -ErrorAction SilentlyContinue
  }

  It 'builds nothing when the tool restore fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1' -Fail 'dotnet tool'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('dotnet tool')
  }

  It 'fails when docfx fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1' -Fail 'dotnet docfx'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('dotnet tool', 'dotnet docfx')
  }

  It 'builds the reference with warnings as errors' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1'

    $result.ExitCode | Should -Be 0
    $docfx = @($result.Lines | Where-Object { $_ -like 'dotnet docfx *' })
    $docfx | Should -HaveCount 1
    $docfx[0] | Should -BeLike '* --warningsAsErrors *'
    Test-Path (Join-Path $script:Root 'target' 'site' 'api' 'index.html') | Should -BeTrue
  }

  It 'names the version of the build props in the title' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1'

    $docfx = @($result.Lines | Where-Object { $_ -like 'dotnet docfx *' })
    $docfx[0] | Should -BeLike '*_appTitle=Apache log4net 9.9.9 API Reference*'
  }

  It 'names the version it is given in the title' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1' -Arguments '-Version', '3.5.0'

    $docfx = @($result.Lines | Where-Object { $_ -like 'dotnet docfx *' })
    $docfx[0] | Should -BeLike '*_appTitle=Apache log4net 3.5.0 API Reference*'
  }

  It 'puts the current year and the privacy policy into the footer' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1'

    $docfx = @($result.Lines | Where-Object { $_ -like 'dotnet docfx *' })
    $docfx[0] | Should -BeLike "*_appFooter=Copyright*2007-$((Get-Date).Year)*privacy-policy-public.html*"
  }

  It 'fails when docfx writes no output' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1' -NoOutput

    $result.ExitCode | Should -Not -Be 0
    $result.Output | Should -BeLike '*no API reference*'
  }

  It 'removes the reference of an earlier run' {
    $stale = Join-Path $script:Root 'target' 'site' 'api' 'stale.html'
    New-Item -ItemType File -Force -Path $stale | Out-Null

    Invoke-InScratchTree -Root $script:Root -Script 'build-api-docs.ps1' | Out-Null

    Test-Path $stale | Should -BeFalse
  }
}
