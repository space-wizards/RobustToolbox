param (
    [Parameter(Mandatory=$true)][string]$nativeBuildPath
)

$platform = "windows"
if ($IsLinux) {
    $platform = "linux"
} elseif ($IsMacOS) {
    $platform = "osx"
}

$arch = switch ([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture) {
    "X64" { "x64" }
    "Arm64" { "arm64" }
}

$triple = "$arch-$platform"

Write-Host "Target triple is $triple"

$pkgConfigTools = $(Join-Path $nativeBuildPath "vcpkg_installed" $triple "tools" "pkgconf")

if ($IsWindows) {
    $env:PATH="$pkgConfigTools;$env:PATH"
} else {
    $env:PATH=$pkgConfigTools + ":$env:PATH"
}

$env:PKG_CONFIG_PATH = $(Join-Path $nativeBuildPath "vcpkg_installed" $triple "lib" "pkgconfig")
$env:INCLUDE = $(Join-Path $nativeBuildPath "vcpkg_installed" $triple "include")
