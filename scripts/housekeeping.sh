cd ..
version=$(cat version.txt)
cd src

sed -i '' "s/CFBundleVersion>.*</CFBundleVersion>$version</" Dots.csproj
sed -i '' "s/CFBundleShortVersionString>.*</CFBundleShortVersionString>$version</" Dots.csproj
sed -i '' "s/Version>.*</Version>$version</" Dots.csproj

sed -i Dots.csproj -e "s/CFBundleVersion>.*</CFBundleVersion>$version</" 
sed -i Dots.csproj -e "s/CFBundleShortVersionString>.*</CFBundleShortVersionString>$version</"
sed -i Dots.csproj -e "s/Version>.*</Version>$version</"

echo "Version is now $version"
# artifact names are decided by vpk at pack time (see build-windows.sh / build-macos.sh)
