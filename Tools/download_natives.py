#!/usr/bin/env python3

import os
import sys
import urllib.request
import shutil
import argparse


class NativeDependency:
    def __init__(self):
        self.name = None
        self.version = None

        self.windows_target_filename = None
        self.linux_target_filename = None
        self.macos_target_filename = None

        self.windows_download_url = None
        self.linux_download_url = None
        self.macos_download_url = None


class DepGlfw(NativeDependency):
    def __init__(self):
        super().__init__()

        self.version = "3.3"
        self.name = "glfw"

        base_url = "https://github.com/space-wizards/build-dependencies" \
                  f"/raw/master/natives/glfw/{self.version}/{{0}}"

        self.windows_target_filename = "glfw3.dll"
        self.macos_target_filename = "libglfw.3.dylib"
        self.linux_target_filename = "libglfw.so.3"

        self.windows_download_url = str.format(base_url, self.windows_target_filename)
        self.macos_download_url = str.format(base_url, self.macos_target_filename)
        self.linux_download_url = str.format(base_url, self.linux_target_filename)


class DepSwnfd(NativeDependency):
    def __init__(self):
        super().__init__()

        self.version = "robust_v0.1.0"
        self.name = "swnfd"

        base_url = "https://github.com/space-wizards/nativefiledialog/releases/download/" \
                  f"{self.version}/{{0}}"

        self.windows_target_filename = "swnfd.dll"
        self.macos_target_filename = "libswnfd.dylib"
        self.linux_target_filename = "libswnfd.so"

        self.windows_download_url = str.format(base_url, self.windows_target_filename)
        self.macos_download_url = str.format(base_url, self.macos_target_filename)
        self.linux_download_url = str.format(base_url, self.linux_target_filename)


class DepFreetype(NativeDependency):
    def __init__(self):
        super().__init__()

        self.version = "2.10.1"
        self.name = "freetype"

        self.windows_target_filename = "freetype6.dll"
        self.macos_target_filename = "libfreetype.6.dylib"

        self.windows_download_url = "https://github.com/space-wizards/SharpFont.Dependencies/" \
            "blob/b1baace7f6259f77162247291c970709650029c6/freetype2/2.10/" \
            "msvc14/x64/freetype6.dll?raw=true"
        self.macos_download_url = "https://github.com/space-wizards/build-dependencies/raw/" \
            "master/natives/freetype/2.10.1/libfreetype.6.dylib"


class DepOpenAL(NativeDependency):
    def __init__(self):
        super().__init__()

        self.version = "1"
        self.name = "openal"

        self.windows_target_filename = "openal32.dll"

        self.windows_download_url = "https://github.com/opentk/opentk-dependencies/blob/" \
            "9eb4991b871d2d2c0745d2c8c8c0fa6404f56438/x64/openal32.dll?raw=true"


NATIVES = [
    DepGlfw(),
    DepSwnfd(),
    DepFreetype(),
    DepOpenAL()
]

def main():
    parser = argparse.ArgumentParser()

    parser.add_argument("platform")
    parser.add_argument("targetOS")
    parser.add_argument("natives") # Ignored for now, since only client needs natives.
    parser.add_argument("outputDir")

    args = parser.parse_args()

    if args.platform != "x64":
        return

    # Path to the RobustToolbox repo.
    repo_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))

    for dep in NATIVES:
        assert dep.version is not None
        assert dep.name is not None

        # Figure out download URL & target filename based on platform.
        download_url = None
        target_filename = None

        if args.targetOS == "Windows":
            download_url = dep.windows_download_url
            target_filename = dep.windows_target_filename

        elif args.targetOS == "Linux":
            download_url = dep.linux_download_url
            target_filename = dep.linux_target_filename

        elif args.targetOS == "MacOS":
            download_url = dep.macos_download_url
            target_filename = dep.macos_target_filename

        if download_url is None or target_filename is None:
            # Skip if platform has no dependencies set.
            continue

        dep_dir = os.path.join(repo_dir, "Dependencies", dep.name)

        # Check version of existing dependencies and update if necessary.
        version_file = os.path.join(dep_dir, "VERSION")
        os.makedirs(dep_dir, exist_ok=True)

        existing_version = "?"
        try:
            with open(version_file, "r") as f:
                existing_version = f.read().strip()
        except FileNotFoundError:
            pass

        if existing_version != dep.version:
            for x in os.listdir(dep_dir):
                os.remove(x)

            with open(version_file, "w") as f:
                f.write(dep.version)

        dependency_path = os.path.join(dep_dir, target_filename)
        # Download if necessary.
        if not os.path.exists(dependency_path):
            urllib.request.urlretrieve(download_url, dependency_path)

        target_file_path = os.path.join(args.outputDir, target_filename)

        # Copy if necessary.
        if not os.path.exists(target_file_path) or \
        os.stat(dependency_path).st_mtime > os.stat(target_file_path).st_mtime:
            shutil.copy2(dependency_path, target_file_path)

if __name__ == "__main__":
    main()
