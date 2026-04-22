function BuildAndTest([string]$tfm, [string]$l4n) {
  ""
  "************************************************************"
  "*** Running $l4n on $tfm"
  "*** "
  remove-item .\bin,.\obj -rec -ea 0
  dotnet build .\Log4NetConcurrencyRepro.csproj -c Release -f $tfm -p Log4NetVersion=$l4n
  1..20 | 
   %{ $testok = 0; $testnok = 0 } `
	{ dotnet vstest .\bin\Release\$tfm\Log4NetConcurrencyRepro.dll; if (!$lastexitcode) { $testok++ } else { $testnok++ } } `
	{ "***`n***`n*** Completed $l4n on $tfm. Outcome: OK=$testok, NOK=$testnok`n***" 
	  $script:result += "$l4n on $tfm : `tOK=$testok, NOK=$testnok"
	}
  ""
}

$result = @()

BuildAndTest 'net472'          '3.3.0'
BuildAndTest 'net472'          '2.0.14'
BuildAndTest 'net10.0-windows' '3.3.0'
BuildAndTest 'net10.0-windows' '2.0.14'
BuildAndTest 'net472'          '3.0.4'
BuildAndTest 'net472'          '2.0.17'
BuildAndTest 'net10.0-windows' '3.0.4'
BuildAndTest 'net10.0-windows' '2.0.17'

""
"************************************************************"
"*** Overall results:"
$result
""