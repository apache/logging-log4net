#Requires -Version 7.4

<#
.SYNOPSIS
  Shared by the build scripts, which import it.
#>

Set-StrictMode -Version Latest
# A module takes its preferences from the global scope, not from the importing script, so
# without this a failed delete below would be reported and ignored.
$ErrorActionPreference = 'Stop'

# Paired, so a throw inside cannot leave the caller in the wrong directory.
function Invoke-InDirectory
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The directory to run in.')]
    [string]$Directory,
    [Parameter(Mandatory=$true, HelpMessage='What to run there.')]
    [scriptblock]$Action
  )

  Push-Location $Directory
  try
  {
    & $Action
  }
  finally
  {
    Pop-Location
  }
}

# Tested rather than silenced: -ErrorAction SilentlyContinue would also swallow a delete that
# failed, leaving a stale artifact or page behind for whoever copies the directory.
function Remove-Directory
{
  param
  (
    [Parameter(Mandatory=$true, HelpMessage='The directory to remove if it exists.')]
    [string]$Directory
  )

  if (Test-Path $Directory)
  {
    Remove-Item $Directory -Force -Recurse
  }
}

# The Antora footer from antora-playbook.yaml, with the year computed as it is there, since
# docfx has no year variable. Passed on the command line, so docfx.json never goes stale.
function Get-ApiDocsFooter
{
  @"
Copyright &copy; 2007-$((Get-Date).Year) <a href="https://www.apache.org/">The Apache Software Foundation</a>.
Licensed under the <a href="https://www.apache.org/licenses/LICENSE-2.0">Apache Software License, Version 2.0</a>.
Please read our <a href="https://privacy.apache.org/policies/privacy-policy-public.html">privacy policy</a>.
<br/>Apache, log4net, and the Apache logo are trademarks or registered trademarks of The Apache Software Foundation.
Other names may be trademarks of their respective owners.
"@ -replace "`r?`n", ' '
}

Export-ModuleMember -Function Invoke-InDirectory, Remove-Directory, Get-ApiDocsFooter
