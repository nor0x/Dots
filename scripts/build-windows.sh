cd ..
version=$(cat version.txt)

cd src
dotnet restore
echo "Building Dots for Windows x64"
dotnet publish "Dots.csproj" -c Release -f net8.0 -r win-x64
echo "Building Dots for Windows x86"
dotnet publish "Dots.csproj" -c Release -f net8.0 -r win-x86
echo "Building Dots for Windows arm64"
dotnet publish "Dots.csproj" -c Release -f net8.0 -r win-arm64

echo "zip Dots for Windows x64"
cd bin/Release/net8.0/win-x64/publish
zip -r Dots-$version-win-x64.zip Dots.exe
windowsx64file=$(echo Dots-$version-win-x64.zip)
export windowsx64file

echo "zip Dots for Windows x86"
cd ../win-x86/publish
zip -r Dots-$version-win-x86.zip Dots.exe
windowsx86file=$(echo Dots-$version-win-x86.zip)
export windowsx86file

echo "zip Dots for Windows arm64"
cd ../win-arm64/publish
zip -r Dots-w-$versionin-arm64.zip Dots.exe
windowsarm64file=$(echo Dots-$version-win-arm64.zip)
export windowsarm64file