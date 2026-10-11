#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

<#
.SYNOPSIS
  Checks that build-old-api-docs.ps1 puts each version's reference where its site layout wants it.

.DESCRIPTION
  Runs the script against fake native commands, so nothing is downloaded and docfx never runs.

  Run with: Invoke-Pester ./scripts/archived/build-old-api-docs.Tests.ps1
#>

BeforeAll {
  . (Join-Path $PSScriptRoot '..' 'FakeCommands.TestHelper.ps1')

  # A site checkout with one folder of each layout: 2.0.8 with release/ and the old Sandcastle
  # pages, 3.0.4 with release/ and a Maven menu without the SDK Reference entry, 3.4.0 as Antora
  # output with the header that lacks the API Reference link.
  function New-FakeSite
  {
    param ([Parameter(Mandatory)][string]$Root)

    $site = Join-Path $Root 'site'
    New-Item -ItemType File -Force -Path (Join-Path $site 'log4net-2.0.8' 'release' 'sdk' 'html' 'T_log4net_ILog.htm') | Out-Null
    $header = @'
    <div id="topbar-nav" class="navbar-menu">
      <div class="navbar-end">
        <a class="navbar-item" href="https://github.com/apache/logging-log4net">GitHub</a>
      </div>
'@
    foreach ($page in 'index.html', (Join-Path 'manual' 'configuration' 'appenders.html'))
    {
      $path = Join-Path $site 'log4net-3.4.0' $page
      New-Item -ItemType File -Force -Path $path | Out-Null
      Set-Content -Path $path -Value $header
    }
    $menu = @'
                                <li class="none{1}">
                  <a href="{0}faq.html" title="FAQ">FAQ</a>
          </li>
                </ul>
'@
    $root = Join-Path $site 'log4net-3.0.4'
    # The FAQ page marks its own entry active, as the Maven skin does.
    Set-Content -Path (New-Item -ItemType File -Force -Path (Join-Path $root 'release' 'faq.html')) -Value ($menu -f '../', ' active')
    Set-Content -Path (Join-Path $root 'index.html') -Value ($menu -f 'release/', '')
    New-Item -ItemType File -Force -Path (Join-Path $root 'release' 'manual' 'configuration.html') | Out-Null
    Set-Content -Path (Join-Path $root 'release' 'manual' 'configuration.html') -Value ($menu -f '../', '')
    return $site
  }
}

Describe 'build-old-api-docs.ps1' {
  BeforeEach {
    $script:Root = New-ScratchTree -Script 'archived/build-old-api-docs.ps1'
    $script:Site = New-FakeSite -Root $script:Root
  }

  AfterEach {
    Remove-Item $script:Root -Recurse -Force -ErrorAction SilentlyContinue
  }

  It 'downloads nothing from a folder that is no site checkout' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Root

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -BeNullOrEmpty
  }

  It 'builds nothing when the download fails' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site -Fail 'curl'

    $result.ExitCode | Should -Not -Be 0
    $result.Calls | Should -Be @('dotnet tool', 'curl --fail')
  }

  It 'replaces the Sandcastle pages of a release folder' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '2.0.8'

    $result.ExitCode | Should -Be 0
    $sdk = Join-Path $script:Site 'log4net-2.0.8' 'release' 'sdk'
    Test-Path (Join-Path $sdk 'index.html') | Should -BeTrue
    Test-Path (Join-Path $sdk 'html') | Should -BeFalse
    Test-Path (Join-Path $script:Site 'log4net-2.0.8' 'api') | Should -BeFalse
    $result.Calls | Should -Be @('dotnet tool', 'curl --fail', 'dotnet docfx')
    $result.Lines[-1] | Should -BeLike '*_appTitle=Apache log4net 2.0.8 API Reference*'
  }

  It 'adds api/ and a header link to an Antora folder' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '3.4.0'

    $result.ExitCode | Should -Be 0
    $folder = Join-Path $script:Site 'log4net-3.4.0'
    Test-Path (Join-Path $folder 'api' 'index.html') | Should -BeTrue
    Get-Content (Join-Path $folder 'index.html') -Raw | Should -BeLike '*href="api/index.html">API Reference</a>*GitHub</a>*'
    Get-Content (Join-Path $folder 'manual' 'configuration' 'appenders.html') -Raw | Should -BeLike '*href="../../api/index.html">API Reference</a>*'
    # Ext.Mail shipped from 3.4.0, so two packages.
    $result.Calls | Should -Be @('dotnet tool', 'curl --fail', 'curl --fail', 'dotnet docfx')
  }

  It 'adds an SDK Reference menu entry to a release folder without one' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '3.0.4'

    $result.ExitCode | Should -Be 0
    $folder = Join-Path $script:Site 'log4net-3.0.4'
    Test-Path (Join-Path $folder 'release' 'sdk' 'index.html') | Should -BeTrue
    Get-Content (Join-Path $folder 'index.html') -Raw | Should -BeLike '*FAQ</a>*<a href="release/sdk/index.html" title="SDK Reference">SDK Reference</a>*'
    Get-Content (Join-Path $folder 'release' 'manual' 'configuration.html') -Raw | Should -BeLike '*<a href="../sdk/index.html" title="SDK Reference">SDK Reference</a>*'
    Get-Content (Join-Path $folder 'release' 'faq.html') -Raw | Should -BeLike '*<li class="none active">*FAQ</a>*<li class="none">*<a href="../sdk/index.html" title="SDK Reference">SDK Reference</a>*'
    # A second run adds nothing.
    Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '3.0.4' | Out-Null
    [regex]::Matches((Get-Content (Join-Path $folder 'index.html') -Raw), '>SDK Reference</a>') | Should -HaveCount 1
  }

  It 'adds the header link only once' {
    Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '3.4.0' | Out-Null
    Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '3.4.0' | Out-Null

    $links = [regex]::Matches((Get-Content (Join-Path $script:Site 'log4net-3.4.0' 'index.html') -Raw), 'API Reference')
    $links | Should -HaveCount 1
  }

  It 'takes a comma separated version list from a shell' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '2.0.8,3.4.0'

    $result.ExitCode | Should -Be 0
    @($result.Lines | Where-Object { $_ -like 'dotnet docfx *' }) | Should -HaveCount 2
  }

  It 'builds every folder by default, oldest first' {
    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site

    $result.ExitCode | Should -Be 0
    $docfx = @($result.Lines | Where-Object { $_ -like 'dotnet docfx *' })
    $docfx | Should -HaveCount 3
    $result.Lines[1] | Should -BeLike '*/log4net/2.0.8/*'
    # Each version in its own work folder, not nested in the previous one.
    $docfx[0] | Should -BeLike '*/old-api/2.0.8/docfx/*'
    $docfx[1] | Should -BeLike '*/old-api/3.0.4/docfx/*'
    $docfx[2] | Should -BeLike '*/old-api/3.4.0/docfx/*'
  }

  It 'downloads the 1.2 releases under their nuget.org version' {
    New-Item -ItemType File -Force -Path (Join-Path $script:Site 'log4net-1.2.15' 'release' 'faq.html') | Out-Null

    $result = Invoke-InScratchTree -Root $script:Root -Script 'archived/build-old-api-docs.ps1' -Arguments '-SiteRoot', $script:Site, '-Version', '1.2.15'

    $result.ExitCode | Should -Be 0
    $result.Lines[1] | Should -BeLike '*/log4net/2.0.5/log4net.2.0.5.nupkg'
    Test-Path (Join-Path $script:Site 'log4net-1.2.15' 'release' 'sdk' 'index.html') | Should -BeTrue
  }
}
