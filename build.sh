#!/bin/bash
# Builds the app loader for both macOS architectures and packs one NuGet each.
#
# Needs a full Xcode install and the .NET 6 macOS workload:
#   sudo xcode-select -s /Applications/Xcode.app
#   dotnet workload install macos

set -e

for arch in x64 arm64; do
  rid="osx-$arch"
  echo "Building $rid"
  dotnet build --configuration Release --runtime "$rid" src/app-mac.csproj

  src="src/bin/Release/net6.0-macos/$rid/uno-app-loader.app"
  rm -rf "$arch"
  mkdir -p "$arch"
  cp -R "$src" "$arch"

  nuget pack -NoPackageAnalysis "uno-mac-loader-$arch.nuspec"
  du -h "uno-mac-loader-$arch".*.nupkg
done
