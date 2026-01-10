## Building

### Dependencies

`robust-native` has various dependencies on non-Rust code that must be procured using [native-build](https://github.com/space-wizards/native-build/). After procuring them, you should set your `PATH` as appropriate to make the build aware of vcpkg's artifacts. This looks something like this:

```powershell
# Windows powershell

push-location "path\to\native\build"
vcpkg\bootstrap-vcpkg.bat
vcpkg\vcpkg.exe install
$env:PKG_CONFIG_PATH="$pwd\vcpkg_installed\x64-windows\lib\pkgconfig"
$env:PATH="$pwd\vcpkg_installed\x64-windows\tools\;$env:PATH"
pop-location
```

```bash
# Linux/macOS
pushd "path/to/native/build"
vcpkg/bootstrap-vcpkg.sh
vcpkg/vcpkg.exe install
export PKG_CONFIG_PATH="$(pwd)/vcpkg_installed/x64-linux/lib/pkgconfig"
export PATH="$(pwd)/vcpkg_installed/x64-linux/tools/:$PATH"
popd
```

### Linking final shared library

Due to limitations of Rust, we cannot have rust directly output `cdylib`-type binaries.
