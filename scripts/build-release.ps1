param(
  $Version = '3.1.0'
)
"cleaning $PSScriptRoot/../build/ ..."
rm -rf $PSScriptRoot/../build/*
'building ...'
dotnet test -c Release "-p:GeneratePackages=true;PackageVersion=$Version" $PSScriptRoot/../src/log4net/log4net.csproj
'compressing source ...'
pushd $PSScriptRoot/..
git archive --format=zip --output $PSScriptRoot/../build/artifacts/apache-log4net-source-$Version.zip master
popd
'compressing binaries ...'
cp $PSScriptRoot/verify-release.ps1 $PSScriptRoot/../build/artifacts/
cp $PSScriptRoot/../LICENSE $PSScriptRoot/../build/Release/
cp $PSScriptRoot/../NOTICE $PSScriptRoot/../build/Release/
pushd $PSScriptRoot/../build/Release
zip -r $PSScriptRoot/../build/artifacts/apache-log4net-binaries-$Version.zip .
popd
'signing ...'
mv $PSScriptRoot/../build/artifacts/log4net.$Version.nupkg $PSScriptRoot/../build/artifacts/apache-log4net.$Version.nupkg
pushd $PSScriptRoot/../build/artifacts
gpg --armor --output ./apache-log4net.$Version.nupkg.asc --detach-sig ./apache-log4net.$Version.nupkg
sha512sum -b ./apache-log4net.$Version.nupkg > ./apache-log4net.$Version.nupkg.sha512
gpg --armor --output ./apache-log4net-source-$Version.zip.asc --detach-sig ./apache-log4net-source-$Version.zip
sha512sum -b ./apache-log4net-source-$Version.zip > ./apache-log4net-source-$Version.zip.sha512
gpg --armor --output ./apache-log4net-binaries-$Version.zip.asc --detach-sig ./apache-log4net-binaries-$Version.zip
sha512sum -b ./apache-log4net-binaries-$Version.zip > ./apache-log4net-binaries-$Version.zip.sha512
gpg --armor --output ./verify-release.ps1.asc --detach-sig ./verify-release.ps1
sha512sum -b ./verify-release.ps1 > ./verify-release.ps1.sha512
popd
'cleaning site ...'
rm -rf $PSScriptRoot/../target/*
'building site ...'
pushd $PSScriptRoot/..
./mvnw site
popd
'creating tag ...'
pause
git tag "rc/$Version-rc1"
'pushing tag ...'
git push --tags
