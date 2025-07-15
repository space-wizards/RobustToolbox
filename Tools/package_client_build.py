#!/usr/bin/env python3
# Packages a full release build of the client that can be loaded by the launcher.
# Native libraries are not included.

import os
import shutil
import subprocess
import sys
import zipfile
import argparse
import glob
from enum import Enum, auto

from typing import List, Optional

try:
    from colorama import init, Fore, Style
    init()

except ImportError:
    # Just give an empty string for everything, no colored logging.
    class ColorDummy(object):
        def __getattr__(self, name):
            return ""

    Fore = ColorDummy()
    Style = ColorDummy()


p = os.path.join

PLATFORM_WIN = "win"
PLATFORM_LINUX = "linux"
PLATFORM_OSX = "osx"
PLATFORM_FREEBSD = "freebsd"

TARGET_OS_WINDOWS = "Windows"
TARGET_OS_MACOS = "MacOS"
TARGET_OS_LINUX = "Linux"
TARGET_OS_FREEBSD = "FreeBSD"

class TargetOS(Enum):
    Windows = auto()
    MacOS = auto()
    Linux = auto()
    FreeBSD = auto()

RID_WIN_X64 = f"{PLATFORM_WIN}-x64"
RID_WIN_ARM64 = f"{PLATFORM_WIN}-arm64"
RID_LINUX_X64 = f"{PLATFORM_LINUX}-x64"
RID_LINUX_ARM64 = f"{PLATFORM_LINUX}-arm64"
RID_OSX_X64 = f"{PLATFORM_OSX}-x64"
RID_OSX_ARM64 = f"{PLATFORM_OSX}-arm64"
RID_FREEBSD_X64 = f"{PLATFORM_FREEBSD}-x64"
RID_FREEBSD_ARM64 = f"{PLATFORM_FREEBSD}-arm64"

DEFAULT_RIDS = [RID_WIN_X64, RID_WIN_ARM64, RID_LINUX_X64, RID_LINUX_ARM64, RID_OSX_X64, RID_OSX_ARM64]
ALL_RIDS = [RID_WIN_X64, RID_WIN_ARM64, RID_LINUX_X64, RID_LINUX_ARM64, RID_OSX_X64, RID_OSX_ARM64, RID_FREEBSD_X64, RID_FREEBSD_ARM64]

IGNORED_RESOURCES = {
    ".gitignore",
    ".directory",
    ".DS_Store"
}

IGNORED_FILES_WINDOWS = {
    "libGLESv2.dll",
    "openal32.dll",
    "e_sqlite3.dll",
    "libsndfile-1.dll",
    "libglib-2.0-0.dll",
    "freetype6.dll",
    "libinstpatch-2.dll",
    "fluidsynth.dll",
    "libgobject-2.0-0.dll",
    "libintl-8.dll",
    "Robust.Client.exe",
    "glfw3.dll",
    "libgthread-2.0-0.dll",
    "libEGL.dll",
    "swnfd.dll",
    "zstd.dll",
    "libsodium.dll"
}

IGNORED_FILES_MACOS = {
    "libe_sqlite3.dylib",
    "libfreetype.6.dylib",
    "libglfw.3.dylib",
    "Robust.Client",
    "libswnfd.dylib",
    "zstd.dylib",
    "libsodium.dylib",
}

IGNORED_FILES_LINUX = {
    "libe_sqlite3.so",
    "libopenal.so",
    "libglfw.so.3",
    "Robust.Client",
    "libswnfd.so",
    "zstd.so",
    "libsodium.so"
}

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Packages the Robust client repo for release on all platforms.")
    parser.add_argument("--platform",
                        "-p",
                        action="store",
                        choices=ALL_RIDS,
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    parser.add_argument("--skip-build",
                        action="store_true",
                        help=argparse.SUPPRESS)

    args = parser.parse_args()
    platforms: list[str] = args.platform
    skip_build: bool = args.skip_build

    if not platforms:
        platforms = DEFAULT_RIDS

    # Validate that nobody put invalid platform names in.
    for rid in platforms:
        if rid not in ALL_RIDS:
            print(Fore.RED + f"Invalid platform specified: '{rid}'" + Style.RESET_ALL)
            exit(1)

    if os.path.exists("release"):
        print(Fore.BLUE + Style.DIM +
              "Cleaning old release packages (release/Robust.Client_*)..." + Style.RESET_ALL)
        for past in glob.glob("release/Robust.Client_*"):
            os.remove(past)
    else:
        os.mkdir("release")

    for platform in platforms:
        build_for_platform(platform, skip_build)


def build_for_platform(rid: str, skip_build: bool):
    print(Fore.GREEN + f"Building for platform '{rid}'..." + Style.RESET_ALL)

    if not skip_build:
        wipe_bin()

    platform = rid.split('-', maxsplit=2)[0]
    if platform == PLATFORM_WIN:
        build_windows(rid, skip_build)
    elif platform == PLATFORM_LINUX:
        build_linux_like(rid, TargetOS.Linux, skip_build)
    elif platform == PLATFORM_OSX:
        build_macos(rid, skip_build)
    elif platform == PLATFORM_FREEBSD:
        build_linux_like(rid, TargetOS.FreeBSD, skip_build)

def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)

    if os.path.exists("bin"):
        shutil.rmtree("bin")


def build_windows(rid: str, skip_build: bool) -> None:
    if not skip_build:
        publish_client(rid, TargetOS.Windows)
        if sys.platform != "win32":
            subprocess.run(["Tools/exe_set_subsystem.py", p("bin", "Client", rid, "publish", "Robust.Client"), "2"])

    print(Fore.GREEN + f"Packaging {rid} client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", f"Robust.Client_{rid}.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("bin", "Client", rid, "publish"), "", client_zip, IGNORED_FILES_WINDOWS)
    copy_resources("Resources", client_zip)
    # Cool we're done.
    client_zip.close()

def build_macos(rid: str, skip_build: bool) -> None:
    if not skip_build:
        publish_client(rid, TargetOS.MacOS)

    print(Fore.GREEN + f"Packaging {rid} client..." + Style.RESET_ALL)
    # Client has to go in an app bundle.
    client_zip = zipfile.ZipFile(p("release", f"Robust.Client_{rid}.zip"), "a",
                                 compression=zipfile.ZIP_DEFLATED)

    contents = p("Space Station 14.app", "Contents", "Resources")
    copy_dir_into_zip(p("BuildFiles", "Mac", "Space Station 14.app"), "Space Station 14.app", client_zip)
    copy_dir_into_zip(p("bin", "Client", rid, "publish"), contents, client_zip, IGNORED_FILES_MACOS)
    copy_resources(p(contents, "Resources"), client_zip)
    client_zip.close()


def build_linux_like(rid: str, target_os: TargetOS, skip_build: bool) -> None:
    if not skip_build:
        publish_client(rid, target_os)

    print(Fore.GREEN + "Packaging %s client..." % rid + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "Robust.Client_%s.zip" % rid), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("bin", "Client", rid, "publish"), "", client_zip, IGNORED_FILES_LINUX)
    copy_resources("Resources", client_zip)
    # Cool we're done.
    client_zip.close()


def publish_client(runtime: str, target_os: TargetOS) -> None:
    base = [
        "dotnet", "publish",
        "--runtime", runtime,
        "--no-self-contained",
        "-c", "Release",
        f"/p:TargetOS={target_os}",
        "/p:FullRelease=True"
    ]

    subprocess.run(base + ["Robust.Client/Robust.Client.csproj"], check=True)


def copy_resources(target, zipf):
    do_resource_copy(target, "Resources", zipf, IGNORED_RESOURCES)


def do_resource_copy(target, source, zipf, ignore_set):
    for filename in os.listdir(source):
        if filename in ignore_set:
            continue

        path = p(source, filename)
        target_path = p(target, filename)
        if os.path.isdir(path):
            copy_dir_into_zip(path, target_path, zipf)

        else:
            zipf.write(path, target_path)


def zip_entry_exists(zipf, name):
    try:
        # Trick ZipInfo into sanitizing the name for us so this awful module stops spewing warnings.
        zinfo = zipfile.ZipInfo.from_file("Resources", name)
        zipf.getinfo(zinfo.filename)
    except KeyError:
        return False
    return True


def copy_dir_into_zip(directory, basepath, zipf, ignored={}):
    if basepath and not zip_entry_exists(zipf, basepath):
        zipf.write(directory, basepath)

    for root, _, files in os.walk(directory):
        relpath = os.path.relpath(root, directory)
        if relpath != "." and not zip_entry_exists(zipf, p(basepath, relpath)):
            zipf.write(root, p(basepath, relpath))

        for filename in files:
            zippath = p(basepath, relpath, filename)
            filepath = p(root, filename)

            if filename in ignored:
                continue

            message = "{dim}{diskroot}{sep}{zipfile}{dim} -> {ziproot}{sep}{zipfile}".format(
                sep=os.sep + Style.NORMAL,
                dim=Style.DIM,
                diskroot=directory,
                ziproot=zipf.filename,
                zipfile=os.path.normpath(zippath))

            print(Fore.CYAN + message + Style.RESET_ALL)
            zipf.write(filepath, zippath)


def copy_dir_or_file(src: str, dst: str):
    """
    Just something from src to dst. If src is a dir it gets copied recursively.
    """

    if os.path.isfile(src):
        shutil.copy2(src, dst)

    elif os.path.isdir(src):
        shutil.copytree(src, dst)

    else:
        raise IOError("{} is neither file nor directory. Can't copy.".format(src))


if __name__ == '__main__':
    main()
