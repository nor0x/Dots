set -e

cd "$(dirname "$0")/.."
root=$PWD
version="${RELEASE_VERSION:?RELEASE_VERSION must be set to a SemVer version}"
repo_url="https://github.com/nor0x/Dots"
releases_dir="$root/releases"

# CI sets LINUX_RID so every AppImage is assembled on a runner of its own architecture;
# a bare local run builds both.
rids="${LINUX_RID:-linux-x64 linux-arm64}"

mkdir -p "$releases_dir"
cd src
dotnet restore

for rid in $rids; do
	echo "Building Dots for Linux $rid"
	rm -rf "publish/$rid"
	dotnet publish "Dots.csproj" -c Release -f net10.0 -r "$rid" --self-contained -o "publish/$rid"
	chmod +x "publish/$rid/Dots"

	# plain tarball for anyone who would rather unpack it themselves. No auto-updates from this
	# one - UpdateService hides the update button when Velopack didn't do the install.
	echo "Packing portable tarball for $rid"
	tar -czf "$releases_dir/nor0x.Dots-$rid-Portable.tar.gz" -C "publish/$rid" .

	# pull the previous release of this channel so vpk can build a delta package on top of it.
	# expected to fail on the very first Velopack release - there is nothing to diff against yet.
	echo "Fetching previous release for channel $rid"
	vpk download github --repoUrl "$repo_url" --channel "$rid" --outputDir "$releases_dir" || true

	# packId is the app identity across every channel, so it has to match build-windows.sh and
	# build-macos.sh exactly. It must also NOT be plain "Dots": Velopack wipes its own
	# <packId> directory under the local app data root on every install, which is exactly where
	# Constants.AppDataPath keeps the release index and the downloaded SDK archives.
	echo "Packing Dots for Linux $rid"
	vpk pack \
		--packId nor0x.Dots \
		--packVersion "$version" \
		--packDir "publish/$rid" \
		--packTitle "Dots" \
		--packAuthors "Joachim Leonfellner" \
		--mainExe Dots \
		--icon Assets/iconlogo.png \
		--categories "Development;" \
		--channel "$rid" \
		--runtime "$rid" \
		--outputDir "$releases_dir"
done

echo "Artifacts in $releases_dir:"
ls -la "$releases_dir"
