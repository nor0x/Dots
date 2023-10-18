cd ..
version=$(cat version.txt)
cd src
sed -i '' "s/CFBundleVersion>.*</CFBundleVersion>$version</" Dots.csproj
sed -i '' "s/CFBundleShortVersionString>.*</CFBundleShortVersionString>$version</" Dots.csproj
sed -i '' "s/Version>.*</Version>$version</" Dots.csproj

echo "Version is now $version"