#Requires -Version 7.4

<#
.SYNOPSIS
  Runs a build script in a scratch tree against fake native commands, for the Pester tests.

.DESCRIPTION
  The fakes log every call and create the files the scripts go on to read, so a test sees which
  commands ran without building, signing, tagging or pushing anything.
#>

# The commit the fake "git rev-parse" reports, so a test can assert what was archived and recorded.
$script:FakeCommitHash = 'a1b2c3d4e5f6071829a3b4c5d6e7f80912345678'

# Logs its call, fails when named in FAKE_FAIL (by command, or by command and first argument, as
# in "dotnet docfx"), and otherwise writes the files the real command would, where it is told to,
# so a wrong output path fails as it would for real. "git status" and "git rev-parse" answer on
# stdout instead, FAKE_DIRTY faking an unclean tree and FAKE_NO_OUTPUT a docfx that wrote nothing.
# "curl -o" writes a package with an assembly and its xml for every framework a script may look
# for, and "dotnet docfx" its index.html at the -o base, or at target/site/api without one.
$script:FakeCommand = @'
param([string]$Name)
Add-Content -Path $env:FAKE_LOG -Value "$Name $($args -join ' ')"
if ($env:FAKE_FAIL -in $Name, "$Name $($args[0])") { exit 1 }
if ($Name -eq 'git')
{
  switch ($args[0])
  {
    'status' { if ($env:FAKE_DIRTY) { ' M src/log4net/Core/LogImpl.cs' } }
    'rev-parse' { $env:FAKE_SHA }
  }
}
$outputs = switch ($Name)
{
  'dotnet'
  {
    $root = Split-Path $PSScriptRoot
    switch ($args[0])
    {
      'tool' { }
      'docfx'
      {
        $base = $args.IndexOf('-o') -ge 0 ? $args[$args.IndexOf('-o') + 1] : "$root/target/site/api"
        if (!$env:FAKE_NO_OUTPUT) { "$base/index.html" }
      }
      default
      {
        $version = ($args -join ' ') -replace '.*PackageVersion=(\S+).*', '$1'
        "$root/build/artifacts/log4net.$version.nupkg", "$root/build/artifacts/log4net.Ext.Mail.$version.nupkg", "$root/build/Release/net462/log4net.dll"
      }
    }
  }
  'git' { if ($args[0] -eq 'archive') { $args[$args.IndexOf('--output') + 1] } }
  'zip' { $args[1] }
  'curl'
  {
    $package = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid()))
    foreach ($lib in 'net462', 'netstandard2.0')
    {
      foreach ($file in 'log4net.dll', 'log4net.xml', 'log4net.Ext.Mail.dll', 'log4net.Ext.Mail.xml')
      {
        New-Item -ItemType File -Force -Path (Join-Path $package 'lib' $lib $file) | Out-Null
      }
    }
    Compress-Archive -Path (Join-Path $package 'lib') -DestinationPath $args[$args.IndexOf('-o') + 1] -Force
    Remove-Item $package -Recurse -Force
  }
}
$outputs | ForEach-Object { New-Item -ItemType File -Force -Path $_ | Out-Null }
'@

function New-ScratchTree
{
  param ([Parameter(Mandatory, HelpMessage='The script under test, relative to scripts/.')][string]$Script)

  $root = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid()))
  $scripts = New-Item -ItemType Directory -Path (Join-Path $root 'scripts')
  $bin = New-Item -ItemType Directory -Path (Join-Path $root 'fakebin')
  # The script keeps its place under scripts/, as the paths it derives from its own depend on it.
  $target = Join-Path $scripts $Script
  New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
  Copy-Item (Join-Path $PSScriptRoot $Script) $target
  Copy-Item (Join-Path $PSScriptRoot 'verify-release.*') $scripts
  # build-release.ps1 calls it by path, and the build scripts import Common.psm1.
  Copy-Item (Join-Path $PSScriptRoot 'build-api-docs.ps1') $scripts
  Copy-Item (Join-Path $PSScriptRoot 'Common.psm1') $scripts
  # The version build-api-docs.ps1 names in its titles.
  Set-Content -Path (New-Item -ItemType File -Force -Path (Join-Path $root 'src' 'Directory.Build.props')) -Value '<Project><PropertyGroup><VersionPrefix>9.9.9</VersionPrefix></PropertyGroup></Project>'
  # The shared docfx sources the old-version script copies.
  Copy-Item (Join-Path $PSScriptRoot '..' 'src' 'site' 'docfx' 'logo.svg') (New-Item -ItemType Directory -Path (Join-Path $root 'src' 'site' 'docfx'))
  New-Item -ItemType Directory -Path (Join-Path $root 'src' 'site' 'docfx' 'template') | Out-Null
  'license' | Set-Content (Join-Path $root 'LICENSE')
  'notice' | Set-Content (Join-Path $root 'NOTICE')
  Set-Content -Path (Join-Path $bin 'fake.ps1') -Value $script:FakeCommand
  foreach ($name in 'dotnet', 'git', 'gpg', 'zip', 'mvnw', 'curl')
  {
    # mvnw is called by path from the root, the others through PATH.
    $directory = $name -eq 'mvnw' ? $root : $bin
    if ($IsWindows)
    {
      Set-Content -Path (Join-Path $directory "$name.cmd") -Value "@pwsh -NoProfile -File `"$bin\fake.ps1`" $name %*`r`n@exit /b %ERRORLEVEL%"
    }
    else
    {
      $path = Join-Path $directory $name
      Set-Content -Path $path -Value "#!/bin/sh`nexec pwsh -NoProfile -File '$bin/fake.ps1' $name `"`$@`""
      chmod +x $path
    }
  }
  return $root.FullName
}

function Invoke-InScratchTree
{
  param ([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Script, [string]$Fail,
    [switch]$Dirty, [switch]$NoOutput, [string[]]$Arguments = @())

  $log = Join-Path $Root 'calls.log'
  New-Item -ItemType File -Path $log -Force | Out-Null
  $path = $env:PATH
  try
  {
    $env:PATH = (Join-Path $Root 'fakebin') + [System.IO.Path]::PathSeparator + $path
    $env:FAKE_LOG = $log
    $env:FAKE_FAIL = $Fail
    $env:FAKE_SHA = $script:FakeCommitHash
    $env:FAKE_DIRTY = $Dirty ? '1' : ''
    $env:FAKE_NO_OUTPUT = $NoOutput ? '1' : ''
    # NonInteractive makes the confirmation pause throw, standing in for a release manager who says no.
    $output = pwsh -NoProfile -NonInteractive -File (Join-Path $Root 'scripts' $Script) @Arguments 2>&1
    $exitCode = $LASTEXITCODE
  }
  finally
  {
    $env:PATH = $path
    Remove-Item Env:FAKE_LOG, Env:FAKE_FAIL, Env:FAKE_SHA, Env:FAKE_DIRTY, Env:FAKE_NO_OUTPUT -ErrorAction SilentlyContinue
  }
  return [pscustomobject]@{
    ExitCode = $exitCode
    Calls = @(Get-Content $log | ForEach-Object { ($_ -split ' ')[0..1] -join ' ' })
    # The whole logged line, for an assertion about an argument rather than about the command.
    Lines = @(Get-Content $log)
    Output = $output -join [Environment]::NewLine
  }
}
