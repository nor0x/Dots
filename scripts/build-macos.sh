set -e

cd "$(dirname "$0")/.."
root=$PWD
version="${RELEASE_VERSION:?RELEASE_VERSION must be set to a SemVer version}"
repo_url="https://github.com/nor0x/Dots"
releases_dir="$root/releases"
entitlements="$root/scripts/Dots.entitlements"
# keep in sync with <SupportedOSPlatformVersion> in src/Dots.csproj
min_macos=13.0

cd src
dotnet restore

for rid in osx-arm64 osx-x64; do
	echo "Building Dots for macOS $rid"
	dotnet msbuild -t:BundleApp -property:Configuration=Release -p:UseAppHost=true -p:RuntimeIdentifier=$rid

	echo "Prepare App Bundle for $rid"
	bundle="bin/Release/net10.0-macos/$rid/publish/Dots.app"
	rm -f $bundle/Contents/MacOS/*.pkg
	cp Assets/AppIcon.icns $bundle/Contents/Resources/
	cp -Rf bin/Release/net10.0-macos/$rid/Dots.app/Contents/MacOS $bundle/Contents
	cp -Rf bin/Release/net10.0-macos/$rid/Dots.app/Contents/MonoBundle $bundle/Contents
	cp bin/Release/net10.0-macos/$rid/Dots.app/Contents/PkgInfo $bundle/Contents/

	# Dotnet.Bundle does not emit LSMinimumSystemVersion, so Finder falls back to the launcher's
	# LC_BUILD_VERSION and reports a bare "you can't use this version" with no required-version hint.
	/usr/libexec/PlistBuddy -c "Delete :LSMinimumSystemVersion" "$bundle/Contents/Info.plist" 2>/dev/null || true
	/usr/libexec/PlistBuddy -c "Add :LSMinimumSystemVersion string $min_macos" "$bundle/Contents/Info.plist"

	# Guard against the deployment target silently following the CI Xcode again (see the
	# SupportedOSPlatformVersion note in Dots.csproj). A too-new minos ships an app that macOS
	# refuses to launch, and nothing else in the pipeline catches it - it signs and notarizes fine.
	actual_minos=$(otool -l "$bundle/Contents/MacOS/Dots" | awk '/LC_BUILD_VERSION/{f=1} f && $1=="minos"{print $2; exit}')
	echo "Deployment target of $rid launcher: minos=$actual_minos (expected $min_macos)"
	if [ "$actual_minos" != "$min_macos" ]; then
		echo "ERROR: launcher minos is $actual_minos, expected $min_macos." >&2
		echo "       The bundle would not launch on macOS older than $actual_minos." >&2
		exit 1
	fi

	# pull the previous release of this channel so vpk can build a delta package on top of it.
	# expected to fail on the very first Velopack release - there is nothing to diff against yet.
	echo "Fetching previous release for channel $rid"
	vpk download github --repoUrl "$repo_url" --channel "$rid" --outputDir "$releases_dir" || true

	# vpk owns codesigning and notarization here. It injects its own updater binary into the bundle,
	# so signing beforehand would leave that binary unsigned. It also signs Contents/MonoBundle,
	# which the net10.0-macos TFM produces and plain `codesign --deep` cannot handle
	# (velopack/velopack#106, fixed in velopack/velopack#292).
	#
	# --noInst skips the .pkg installer: signing one needs a "Developer ID Installer" certificate,
	# which is a different cert to the "Developer ID Application" one in BUILD_CERTIFICATE_BASE64.
	# New users install from the portable zip, which self-extracts in Finder. Drop --noInst once
	# the installer certificate is available.
	# packId matches build-windows.sh - see the note there about the %LocalAppData%\Dots collision
	echo "Packing Dots for macOS $rid"
	vpk pack \
		--packId nor0x.Dots \
		--packVersion "$version" \
		--packDir "$bundle" \
		--packTitle "Dots" \
		--packAuthors "Joachim Leonfellner" \
		--mainExe Dots \
		--icon Assets/AppIcon.icns \
		--bundleId com.nor0x.dots \
		--channel "$rid" \
		--outputDir "$releases_dir" \
		--signAppIdentity "$SIGNING_IDENTITY" \
		--signEntitlements "$entitlements" \
		--notaryProfile "$NOTARY_PROFILE" \
		--noInst
done

echo "Artifacts in $releases_dir:"
ls -la "$releases_dir"
