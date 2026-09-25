#Requires -Version 7.4

<#
.SYNOPSIS
  Runs a build script in a scratch tree against fake native commands, for the Pester tests.

.DESCRIPTION
  The fakes log every call and create the files the scripts go on to read, so a test sees which
  commands ran without building, signing, tagging or pushing anything.
#>

# Logs its call, fails when named in FAKE_FAIL, and otherwise writes the files the real command
# would, where it is told to, so a wrong output path fails as it would for real.
$script:FakeCommand = @'
param([string]$Name)
Add-Content -Path $env:FAKE_LOG -Value "$Name $($args -join ' ')"
if ($env:FAKE_FAIL -eq $Name) { exit 1 }
$outputs = switch ($Name)
{
  'dotnet'
  {
    $version = ($args -join ' ') -replace '.*PackageVersion=(\S+).*', '$1'
    $build = "$(Split-Path $PSScriptRoot)/build"
    "$build/artifacts/log4net.$version.nupkg", "$build/artifacts/log4net.Ext.Mail.$version.nupkg", "$build/Release/net462/log4net.dll"
  }
  'git' { if ($args[0] -eq 'archive') { $args[$args.IndexOf('--output') + 1] } }
  'zip' { $args[1] }
}
$outputs | ForEach-Object { New-Item -ItemType File -Force -Path $_ | Out-Null }
'@

function New-ScratchTree
{
  param ([Parameter(Mandatory)][string]$Script)

  $root = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid()))
  $scripts = New-Item -ItemType Directory -Path (Join-Path $root 'scripts')
  $bin = New-Item -ItemType Directory -Path (Join-Path $root 'fakebin')
  Copy-Item (Join-Path $PSScriptRoot $Script) $scripts
  Copy-Item (Join-Path $PSScriptRoot 'verify-release.*') $scripts
  'license' | Set-Content (Join-Path $root 'LICENSE')
  'notice' | Set-Content (Join-Path $root 'NOTICE')
  Set-Content -Path (Join-Path $bin 'fake.ps1') -Value $script:FakeCommand
  foreach ($name in 'dotnet', 'git', 'gpg', 'zip', 'mvnw')
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
  param ([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Script, [string]$Fail)

  $log = Join-Path $Root 'calls.log'
  New-Item -ItemType File -Path $log -Force | Out-Null
  $path = $env:PATH
  try
  {
    $env:PATH = (Join-Path $Root 'fakebin') + [System.IO.Path]::PathSeparator + $path
    $env:FAKE_LOG = $log
    $env:FAKE_FAIL = $Fail
    # NonInteractive makes the confirmation pause throw, standing in for a release manager who says no.
    $output = pwsh -NoProfile -NonInteractive -File (Join-Path $Root 'scripts' $Script) 2>&1
    $exitCode = $LASTEXITCODE
  }
  finally
  {
    $env:PATH = $path
    Remove-Item Env:FAKE_LOG, Env:FAKE_FAIL -ErrorAction SilentlyContinue
  }
  return [pscustomobject]@{
    ExitCode = $exitCode
    Calls = @(Get-Content $log | ForEach-Object { ($_ -split ' ')[0..1] -join ' ' })
    Output = $output -join [Environment]::NewLine
  }
}
