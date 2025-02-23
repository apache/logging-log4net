$Version = '3.0.5'
$Preview = '1'
'building ...'
dotnet build -c Release "-p:GeneratePackages=true;PackageVersion=$Version-preview.$Preview" $PSScriptRoot/../src/log4net/log4net.csproj
'signing ...'
gpg --armor --output $PSScriptRoot\..\build\artifacts\log4net.$Version-preview.$Preview.nupkg.asc --detach-sig $PSScriptRoot\..\build\artifacts\log4net.$Version-preview.$Preview.nupkg
'create tag?'
pause
'creating tag ...'
git tag "rc/$Version-preview.$Preview"
'pushing tag ...'
git push --tags