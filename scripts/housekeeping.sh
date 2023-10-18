cd ../src/

# read version from version.txt
version=$(cat version.txt)
# replace value of CFBundleVersion in Dots.csproj
sed -i '' "s/CFBundleVersion>.*</CFBundleVersion>$version</" Dots.csproj
# replace value of CFBundleShortVersionString in Dots.csproj
sed -i '' "s/CFBundleShortVersionString>.*</CFBundleShortVersionString>$version</" Dots.csproj

