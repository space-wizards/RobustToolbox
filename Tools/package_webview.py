#!/usr/bin/env python3
# Packages a full release build of the client that can be loaded by the launcher.
# Native libraries are not included.

import os
import shutil
import subprocess
import sys
import zipfile
import argparse

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

PLATFORM_WINDOWS = "win-x64"
PLATFORM_LINUX = "linux-x64"
PLATFORM_LINUX_ARM64 = "linux-arm64"
PLATFORM_MACOS = "osx-x64"

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Packages the Robust.Client.WebView module for release on all platforms.")
    parser.add_argument("--platform",
                        "-p",
                        action="store",
                        choices=[PLATFORM_WINDOWS, PLATFORM_MACOS, PLATFORM_LINUX, PLATFORM_LINUX_ARM64],
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    parser.add_argument("--skip-build",
                        action="store_true",
                        help=argparse.SUPPRESS)

    args = parser.parse_args()
    platforms = args.platform
    skip_build = args.skip_build

    if not platforms:
        platforms = [PLATFORM_WINDOWS, PLATFORM_LINUX]

    if os.path.exists("release"):
        print(Fore.BLUE + Style.DIM +
              "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")

    os.mkdir("release")

    if PLATFORM_WINDOWS in platforms:
        if not skip_build:
            wipe_bin()
        build_windows(skip_build)

    if PLATFORM_LINUX in platforms:
        if not skip_build:
            wipe_bin()
        build_linux(skip_build)

def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)

    RCWebViewBin = p("Robust.Client.WebView", "bin")
    if os.path.exists(RCWebViewBin):
        shutil.rmtree(RCWebViewBin)


def build_windows(skip_build: bool) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project for Windows x64..." + Style.RESET_ALL)

    base_bin = p("Robust.Client.WebView", "bin", "Release", "net6.0")

    if not skip_build:
        build_client_rid("Windows", "win-x64")
        build_client("Windows")
        if sys.platform != "win32":
            subprocess.run(["Tools/exe_set_subsystem.py", p(base_bin, "Robust.Client.WebView.exe"), "2"])


    print(Fore.GREEN + "Packaging win-x64..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "Robust.Client.WebView_win-x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    files_to_copy = [
        "Robust.Client.WebView.dll",
        "Robust.Client.WebView.exe",
        "Robust.Client.WebView.runtimeconfig.json",
        "Robust.Client.WebView.pdb",
        "Robust.Client.WebView.deps.json",
        "Xilium.CefGlue.dll",
        "Xilium.CefGlue.pdb",
    ]

    for f in files_to_copy:
        client_zip.write(p(base_bin, "win-x64", f), f)

    copy_dir_into_zip(p(base_bin, "runtimes", "win-x64", "native"), "", client_zip, {
        "e_sqlite3.dll",
        "fluidsynth.dll",
        "freetype6.dll",
        "glfw3.dll",
        "libglib-2.0-0.dll",
        "libgobject-2.0-0.dll",
        "libgthread-2.0-0.dll",
        "libinstpatch-2.dll",
        "libintl-8.dll",
        "libsndfile-1.dll",
        "openal32.dll",
        "swnfd.dll",
    })

    # Cool we're done.
    client_zip.close()

def build_linux(skip_build: bool) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project for Linux x64..." + Style.RESET_ALL)

    base_bin = p("Robust.Client.WebView", "bin", "Release", "net6.0")

    if not skip_build:
        build_client_rid("Linux", "linux-x64")
        build_client("Linux")

    print(Fore.GREEN + "Packaging linux-x64..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "Robust.Client.WebView_linux-x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    files_to_copy = [
        "Robust.Client.WebView.dll",
        "Robust.Client.WebView.runtimeconfig.json",
        "Robust.Client.WebView.pdb",
        "Robust.Client.WebView",
        "Robust.Client.WebView.deps.json",
        "Xilium.CefGlue.dll",
        "Xilium.CefGlue.pdb",
    ]

    for f in files_to_copy:
        client_zip.write(p(base_bin, "linux-x64", f), f)

    copy_dir_into_zip(p(base_bin, "runtimes", "linux-x64", "native"), "", client_zip, {
        "libglfw.so.3",
        "libe_sqlite3.so",
        "libopenal.so",
        "libswnfd.so",
    })

    # Cool we're done.
    client_zip.close()


def build_client(target_os: str) -> None:
    # Running a publish will fold all the natives in the runtime directories into one folder.
    # This basically nukes the entire setup.
    # As bypass, we do an all-platform build and then copy the natives manually.
    base = [
        "dotnet", "build",
        "-c", "Release",
        f"/p:TargetOS={target_os}",
        "/p:FullRelease=True",
        "--no-self-contained",
    ]

    subprocess.run(base + ["Robust.Client.WebView/Robust.Client.WebView.csproj"], check=True)

def build_client_rid(target_os: str, rid: str) -> None:
    base = [
        "dotnet", "build",
        "-c", "Release",
        "-r", rid,
        f"/p:TargetOS={target_os}",
        "/p:FullRelease=True",
        "--no-self-contained",
    ]

    subprocess.run(base + ["Robust.Client.WebView/Robust.Client.WebView.csproj"], check=True)


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
