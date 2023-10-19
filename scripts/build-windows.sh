cd ..
version=$(cat version.txt)

cd src
dotnet restore

echo "Building Dots for Windows x64"
dotnet publish "Dots.csproj" -c Release -f net8.0 -r win-x64

echo "zip Dots for Windows x64"
7z a -tzip Dots-$version-win-x64.zip  bin/Release/net8.0/win-x64/publish/Dots.exe
windowsx64file=$(echo Dots-$version-win-x64.zip)
export windowsx64file

echo "Building Dots for Windows x86"
dotnet publish "Dots.csproj" -c Release -f net8.0 -r win-x86

echo "zip Dots for Windows x86"
7z a -tzip Dots-$version-win-x86.zip bin/Release/net8.0/win-x86/publish/Dots.exe
windowsx86file=$(echo Dots-$version-win-x86.zip)
export windowsx86file

echo "Building Dots for Windows arm64"
dotnet publish "Dots.csproj" -c Release -f net8.0 -r win-arm64

echo "zip Dots for Windows arm64"
7z a -tzip Dots-$version-win-arm64.zip bin/Release/net8.0/win-arm64/publish/Dots.exe
windowsarm64file=$(echo Dots-$version-win-arm64.zip)
export windowsarm64file