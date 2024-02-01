cd ..
version=$(cat version.txt)
cd /Users/runner/work/Dots/Dots/src/
echo "setting <CFBundleVersion> and <CFBundleShortVersionString> in Dots.csproj to $version"
sed -i '' "s/CFBundleVersion>.*</CFBundleVersion>$version</g" Dots.csproj
sed -i '' "s/CFBundleShortVersionString>.*</CFBundleShortVersionString>$version</g" Dots.csproj


dotnet restore
echo "Building Dots for macOS arm64"
dotnet msbuild -t:BundleApp -property:Configuration=Release -p:UseAppHost=true -p:RuntimeIdentifier=osx-arm64

echo "Prepare App Bundle for arm64"
rm bin/Release/net8.0-macos/osx-arm64/publish/Dots.app/Contents/MacOS/*.pkg
cp Assets/AppIcon.icns bin/Release/net8.0-macos/osx-arm64/publish/Dots.app/Contents/Resources/
cp -Rf bin/Release/net8.0-macos/osx-arm64/Dots.app/Contents/MacOS bin/Release/net8.0-macos/osx-arm64/publish/Dots.app/Contents
cp -Rf bin/Release/net8.0-macos/osx-arm64/Dots.app/Contents/MonoBundle bin/Release/net8.0-macos/osx-arm64/publish/Dots.app/Contents
cp bin/Release/net8.0-macos/osx-arm64/Dots.app/Contents/PkgInfo bin/Release/net8.0-macos/osx-arm64/publish/Dots.app/Contents/

echo "codesign Dots for macOS arm64"
APP_NAME="/Users/runner/work/Dots/Dots/src/bin/Release/net8.0-macos/osx-arm64/publish/Dots.app"
ENTITLEMENTS="/Users/runner/work/Dots/Dots/scripts/Dots.entitlements"

echo "[INFO]______________[INFO] Signing app files"
find "$APP_NAME/Contents/MacOS/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO]______________[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO]______________[INFO] Signing all files in APP_NAME/Contents/MonoBundle"
find "$APP_NAME/Contents/MonoBundle/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO]______________[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO]______________[INFO] Signing app file"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP_NAME"

echo "dittoing Dots for macOS arm64"
cd /Users/runner/work/Dots/Dots/src/bin/Release/net8.0-macos/osx-arm64/publish
macosarm64file=$(echo Dots-$version-macos-arm64.zip)
ditto -c -k --sequesterRsrc --keepParent Dots.app $macosarm64file
xcrun notarytool submit $macosarm64file --apple-id $APPLE_ID --team-id $TEAM_ID --password $APP_SPECIFIC_PWD --verbose --wait

cd /Users/runner/work/Dots/Dots/src/
echo "Building Dots for macOS x64"
dotnet msbuild -t:BundleApp -property:Configuration=Release -p:UseAppHost=true -p:RuntimeIdentifier=osx-x64

echo "Prepare App Bundle for x64"
rm bin/Release/net8.0-macos/osx-x64/publish/Dots.app/Contents/MacOS/*.pkg
cp Assets/AppIcon.icns bin/Release/net8.0-macos/osx-x64/publish/Dots.app/Contents/Resources/
cp -Rf bin/Release/net8.0-macos/osx-x64/Dots.app/Contents/MacOS bin/Release/net8.0-macos/osx-x64/publish/Dots.app/Contents
cp -Rf bin/Release/net8.0-macos/osx-x64/Dots.app/Contents/MonoBundle bin/Release/net8.0-macos/osx-x64/publish/Dots.app/Contents
cp bin/Release/net8.0-macos/osx-x64/Dots.app/Contents/PkgInfo bin/Release/net8.0-macos/osx-x64/publish/Dots.app/Contents/

echo "codesign Dots for macOS x64"
APP_NAME="/Users/runner/work/Dots/Dots/src/bin/Release/net8.0-macos/osx-x64/publish/Dots.app"
ENTITLEMENTS="/Users/runner/work/Dots/Dots/scripts/Dots.entitlements"

echo "[INFO]______________[INFO] Signing app files"
find "$APP_NAME/Contents/MacOS/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO]______________[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO]______________[INFO] Signing all files in APP_NAME/Contents/MonoBundle"
find "$APP_NAME/Contents/MonoBundle/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO]______________[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done

echo "[INFO]______________[INFO] Signing app file"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP_NAME"

echo "dittoing Dots for macOS x64"
cd /Users/runner/work/Dots/Dots/src/bin/Release/net8.0-macos/osx-x64/publish
macosx64file=$(echo Dots-$version-macos-x64.zip)
ditto -c -k --sequesterRsrc --keepParent Dots.app $macosx64file
xcrun notarytool submit $macosx64file --apple-id $APPLE_ID --team-id $TEAM_ID --password $APP_SPECIFIC_PWD --verbose --wait



