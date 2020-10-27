#!/usr/bin/env python3

import argparse
import os
import shutil
import urllib.request

from typing import List

OS_WINDOWS = "Windows"
OS_LINUX = "Linux"
OS_MACOS = "MacOS"

# BIG NOTE:
# If you add natives here, make SURE to edit package_release_build.py in content,
# so that the native gets copied there too.


class NativeDependency:
    def __init__(self):
        self.name = None
        self.version = None

    def run(self, dep_dir: str, targetOS: str, output_dir: str):
        pass

    def check_version(self, dep_dir: str) -> bool:
        version_file = os.path.join(dep_dir, "VERSION")
        os.makedirs(dep_dir, exist_ok=True)

        existing_version = None
        try:
            with open(version_file, "r") as f:
                existing_version = f.read().strip()
        except FileNotFoundError:
            pass

        if existing_version != self.version:
            for x in os.listdir(dep_dir):
                r = os.path.join(dep_dir, x)
                if os.path.isdir(r):
                    shutil.rmtree(r)
                else:
                    os.remove(r)

            with open(version_file, "w") as f:
                f.write(self.version)

            return True

        return False


class SimpleDependency(NativeDependency):
    def __init__(self):
        super().__init__()

        self.windows_target_filename = None
        self.linux_target_filename = None
        self.macos_target_filename = None

        self.windows_download_url = None
        self.linux_download_url = None
        self.macos_download_url = None

    def run(self, dep_dir: str, targetOS: str, output_dir: str):
        # Figure out download URL & target filename based on platform.
        download_url = None
        target_filename = None

        if targetOS == OS_WINDOWS:
            download_url = self.windows_download_url
            target_filename = self.windows_target_filename

        elif targetOS == OS_LINUX:
            download_url = self.linux_download_url
            target_filename = self.linux_target_filename

        elif targetOS == OS_MACOS:
            download_url = self.macos_download_url
            target_filename = self.macos_target_filename

        if download_url is None or target_filename is None:
            # Skip if platform has no dependencies set.
            return

        # Check version of existing dependencies and update if necessary.
        self.check_version(dep_dir)

        dependency_path = os.path.join(dep_dir, target_filename)
        # Download if necessary.
        if not os.path.exists(dependency_path):
            urllib.request.urlretrieve(download_url, dependency_path)

        target_file_path = os.path.join(output_dir, target_filename)

        # Copy if necessary.
        if not os.path.exists(target_file_path) or \
                os.stat(dependency_path).st_mtime > os.stat(target_file_path).st_mtime:
            shutil.copy2(dependency_path, target_file_path)


class DepGlfw(SimpleDependency):
    def __init__(self):
        super().__init__()

        self.version = "3.3.2"
        self.name = "glfw"

        base_url = "https://github.com/space-wizards/build-dependencies" \
                   f"/raw/master/natives/glfw/{self.version}/{{0}}"

        self.windows_target_filename = "glfw3.dll"
        self.macos_target_filename = "libglfw.3.dylib"
        self.linux_target_filename = "libglfw.so.3"

        self.windows_download_url = str.format(base_url, self.windows_target_filename)
        self.macos_download_url = str.format(base_url, self.macos_target_filename)
        self.linux_download_url = str.format(base_url, self.linux_target_filename)


class DepSwnfd(SimpleDependency):
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


class DepFreetype(SimpleDependency):
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


class DepOpenAL(SimpleDependency):
    def __init__(self):
        super().__init__()

        self.version = "1.20.1"
        self.name = "openal"

        self.windows_target_filename = "openal32.dll"
        self.linux_target_filename = "libopenal.so.1"

        base_url = "https://github.com/space-wizards/build-dependencies" \
                   f"/raw/master/natives/openal/{self.version}/{{0}}"

        self.windows_download_url = str.format(base_url, self.windows_target_filename)
        self.linux_download_url = str.format(base_url, "libopenal.so")


class DepFluidsynth(NativeDependency):
    BASE_URL = "https://github.com/space-wizards/build-dependencies/blob/ccf2cf448883dd9b2d999ff9fb5c4533229231bd" \
               "/natives/fluidsynth/2.1.0/"

    FILES = [
        "fluidsynth.dll",
        "libglib-2.0-0.dll",
        "libgobject-2.0-0.dll",
        "libgthread-2.0-0.dll",
        "libinstpatch-2.dll",
        "libintl-8.dll",
        "libsndfile-1.dll"
    ]

    def __init__(self):
        super().__init__()

        self.name = "fluidsynth"
        self.version = "6aea18bef458595e9580e8bc7612aff439001303"

    def run(self, dep_dir: str, targetOS: str, output_dir: str):
        if targetOS != OS_WINDOWS:
            return

        #print("FLUIDSYNTH FOR WINDOWS")
        os_dep_dir = os.path.join(dep_dir, targetOS)

        if self.check_version(dep_dir):
            #print("MUST UPDATE")
            os.makedirs(os_dep_dir, exist_ok=True)
            # Download new version

            for filename in DepFluidsynth.FILES:
                url = DepFluidsynth.BASE_URL + filename + "?raw=true"
                urllib.request.urlretrieve(url, os.path.join(os_dep_dir, filename))

        #print("COPYING")
        for file in os.listdir(os_dep_dir):
            source_file_path = os.path.join(os_dep_dir, file)
            target_file_path = os.path.join(output_dir, file)

            #print(file, source_file_path, target_file_path)

            if not os.path.exists(target_file_path) or \
                    os.stat(source_file_path).st_mtime > os.stat(target_file_path).st_mtime:
                #print("COPY")
                shutil.copy2(source_file_path, target_file_path)

class DepAngle(NativeDependency):
    BASE_URL = "https://github.com/space-wizards/build-dependencies/blob/master/natives/ANGLE/2020-05-15-1/"

    FILES = [
        "libGLESv2.dll",
        "libEGL.dll"
    ]

    def __init__(self):
        super().__init__()

        self.name = "ANGLE"
        self.version = "2020-05-15-1-fix"

    def run(self, dep_dir: str, targetOS: str, output_dir: str):
        if targetOS != OS_WINDOWS:
            return

        print("ANGLE FOR WINDOWS")
        os_dep_dir = os.path.join(dep_dir, targetOS)

        if self.check_version(dep_dir):
            os.makedirs(os_dep_dir, exist_ok=True)
            # Download new version

            for filename in DepAngle.FILES:
                url = DepAngle.BASE_URL + filename + "?raw=true"
                urllib.request.urlretrieve(url, os.path.join(os_dep_dir, filename))

        #print("COPYING")
        for file in os.listdir(os_dep_dir):
            source_file_path = os.path.join(os_dep_dir, file)
            target_file_path = os.path.join(output_dir, file)

            #print(file, source_file_path, target_file_path)

            if not os.path.exists(target_file_path) or \
                    os.stat(source_file_path).st_mtime > os.stat(target_file_path).st_mtime:
                #print("COPY")
                shutil.copy2(source_file_path, target_file_path)


NATIVES: List[SimpleDependency] = [
    DepGlfw(),
    DepSwnfd(),
    DepFreetype(),
    DepOpenAL(),
    DepFluidsynth(),
    DepAngle()
]


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument("platform")
    parser.add_argument("targetOS")
    parser.add_argument("natives")  # Ignored for now, since only client needs natives.
    parser.add_argument("outputDir")

    args = parser.parse_args()

    #if args.platform != "x64":
    #    return

    print("Copying dependencies")
    print(args.targetOS, args.outputDir)

    # Path to the RobustToolbox repo.
    repo_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))

    for dep in NATIVES:
        assert dep.version is not None
        assert dep.name is not None

        dep_dir = os.path.join(repo_dir, "Dependencies", dep.name)

        dep.run(dep_dir, args.targetOS, args.outputDir)


if __name__ == "__main__":
    main()
