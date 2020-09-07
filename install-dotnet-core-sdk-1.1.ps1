$installer="dotnet-dev-win-x64.1.1.14.exe"
Write-Host "Downloading $installer"
Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/c6b9a396-5e7a-4b91-86f6-f9e8df3bf1dd/6d61addfd6069e404981bede03f8f4f9/$installer" -OutFile $installer
Write-Host "Running $installer"
Start-Process -FilePath $installer -ArgumentList "/wait","/passive" -Wait
Write-Host "dotnet core sdk 1.1 installed"

