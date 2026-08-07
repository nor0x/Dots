set -e

cd "$(dirname "$0")/.."
root=$PWD
version="${RELEASE_VERSION:?RELEASE_VERSION must be set to a SemVer version}"
repo_url="https://github.com/nor0x/Dots"
releases_dir="$root/releases"

cd src
dotnet restore

for rid in win-x64 win-x86 win-arm64; do
	echo "Building Dots for Windows $rid"
	rm -rf "publish/$rid"
	dotnet publish "Dots.csproj" -c Release -f net10.0 -r "$rid" --self-contained -o "publish/$rid"

	# pull the previous release of this channel so vpk can build a delta package on top of it.
	# expected to fail on the very first Velopack release - there is nothing to diff against yet.
	echo "Fetching previous release for channel $rid"
	vpk download github --repoUrl "$repo_url" --channel "$rid" --outputDir "$releases_dir" || true

	# packId must NOT be plain "Dots": Velopack installs to %LocalAppData%\<packId> and wipes that
	# directory on every install, which is exactly where Constants.AppDataPath keeps the release
	# index and downloaded SDK installers.
	echo "Packing Dots for Windows $rid"
	vpk pack \
		--packId nor0x.Dots \
		--packVersion "$version" \
		--packDir "publish/$rid" \
		--packTitle "Dots" \
		--packAuthors "Joachim Leonfellner" \
		--mainExe Dots.exe \
		--icon Assets/appicon.ico \
		--channel "$rid" \
		--runtime "$rid" \
		--outputDir "$releases_dir"
done

echo "Artifacts in $releases_dir:"
ls -la "$releases_dir"
