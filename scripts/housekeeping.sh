set -e

cd ..
version="${RELEASE_VERSION:?RELEASE_VERSION must be set to a SemVer version}"
cd src

if [[ "$OSTYPE" == darwin* ]]; then
	sed_in_place=(-i '')
else
	sed_in_place=(-i)
fi

sed "${sed_in_place[@]}" \
	-e "s|<CFBundleVersion>[^<]*</CFBundleVersion>|<CFBundleVersion>$version</CFBundleVersion>|" \
	-e "s|<CFBundleShortVersionString>[^<]*</CFBundleShortVersionString>|<CFBundleShortVersionString>$version</CFBundleShortVersionString>|" \
	-e "s|<Version>[^<]*</Version>|<Version>$version</Version>|" \
	Dots.csproj

echo "Version is now $version"
# artifact names are decided by vpk at pack time (see build-windows.sh / build-macos.sh)
