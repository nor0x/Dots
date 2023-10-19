cd ..
version=$(cat version.txt)
cd src


sed -i Dots.csproj -e "s/CFBundleVersion>.*</CFBundleVersion>$version</" 
sed -i Dots.csproj -e "s/CFBundleShortVersionString>.*</CFBundleShortVersionString>$version</"
sed -i Dots.csproj -e "s/Version>.*</Version>$version</"

echo "Version is now $version"
echo "Setting up file names"

macosx64file=$(echo Dots-$version-macos-x64.zip)
macosarm64file=$(echo Dots-$version-macos-arm64.zip)
windowsx86file=$(echo Dots-$version-win-x86.zip)
windowsx64file=$(echo Dots-$version-win-x64.zip)
windowsarm64file=$(echo Dots-$version-win-arm64.zip)

echo "filenames: $macosx64file $macosarm64file $windowsx86file $windowsx64file $windowsarm64file"
