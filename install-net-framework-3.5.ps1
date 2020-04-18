Write-Host "Downloading dotnetfx35.exe"
Invoke-WebRequest -Uri "https://download.microsoft.com/download/2/0/E/20E90413-712F-438C-988E-FDAA79A8AC3D/dotnetfx35.exe" -OutFile dotnetfx35.exe
Write-Host "Running dotnetfx35.exe"
Start-Process -FilePath dotnetfx35.exe -ArgumentList "/wait","/passive" -Wait
Write-Host "dotnetfx35 installed"

