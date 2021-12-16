if (-not (Test-Path dotnetfx35.exe)) {
    Write-Host "Downloading dotnetfx35.exe"
    Invoke-WebRequest -Uri "https://download.microsoft.com/download/2/0/E/20E90413-712F-438C-988E-FDAA79A8AC3D/dotnetfx35.exe" -OutFile dotnetfx35.exe
}

Write-Host "Running dotnetfx35.exe"
$process = Start-Process -FilePath dotnetfx35.exe -ArgumentList "/wait","/passive" -Wait -PassThru
if ($process.ExitCode -eq 0) {
    Write-Host "dotnetfx35 installed"
} else {
    Write-Host "dotnetfx35 installer returned exit code ${process.ExitCode}"
}

if (-not (Test-Path dotnetfx35client.exe)) {
    Write-host "Downloading dotnetfx35client.exe"
    Invoke-WebRequest -Uri "https://download.microsoft.com/download/c/d/c/cdc0f321-4f72-4a08-9bac-082f3692ecd9/DotNetFx35Client.exe" -OutFile dotnetfx35client.exe
}

Write-Host "Running dotnetfx35client.exe"
$process = Start-Process -FilePath dotnetfx35client.exe -ArgumentList "/quiet","/passive" -Wait -PassThru
if ($process.ExitCode -eq 0) {
    Write-Host "dotnetfx35client installed"
} else {
    Write-Host "dotnetfx35client installer returned exit code ${process.ExitCode}"
}
