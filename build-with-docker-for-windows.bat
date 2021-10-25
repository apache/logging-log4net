docker run -v %~dp0%:C:\dev -v %USERPROFILE%\.nuget\packages:C:\packages -t davydm/net-build-tools:vs2019 "npm ci && npm run build"
