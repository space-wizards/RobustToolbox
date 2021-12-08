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

PLATFORM_WINDOWS = "windows"
PLATFORM_LINUX = "linux"
PLATFORM_LINUX_ARM64 = "linux-arm64"
PLATFORM_MACOS = "mac"

IGNORED_RESOURCES = {
    ".gitignore",
    ".directory",
    ".DS_Store"
}

IGNORED_FILES_WINDOWS = {
    #"libGLESv2.dll",
    #"openal32.dll",
    #"e_sqlite3.dll",
    #"libsndfile-1.dll",
    #"libglib-2.0-0.dll",
    #"freetype6.dll",
    #"libinstpatch-2.dll",
    #"fluidsynth.dll",
    #"libgobject-2.0-0.dll",
    #"libintl-8.dll",
    #"glfw3.dll",
    #"libgthread-2.0-0.dll",
    #"libEGL.dll",
    #"swnfd.dll"
}

IGNORED_FILES_MACOS = {
    #"libe_sqlite3.dylib",
    #"libfreetype.6.dylib",
    #"libglfw.3.dylib",
    #"libswnfd.dylib"
}

IGNORED_FILES_LINUX = {
    #"libe_sqlite3.so",
    #"libopenal.so",
    #"libglfw.so.3",
    #"libswnfd.so"
}

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Packages the Robust client repo for release on all platforms.")
    parser.add_argument("--platform",
                        "-p",
                        action="store",
                        choices=[PLATFORM_WINDOWS, PLATFORM_MACOS, PLATFORM_LINUX],
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    parser.add_argument("--skip-build",
                        action="store_true",
                        help=argparse.SUPPRESS)
                     
    parser.add_argument("--no-self-contained",
                        action="store_true",
                        help="Does it needs to be .exe")
                     
    parser.add_argument("--client",
                        action="store",
                        help="Overrides client folder")

    args = parser.parse_args()
    platforms = args.platform
    skip_build = args.skip_build
    runtimes = not args.no_self_contained
    client_override = args.client

    if not platforms:
        platforms = [PLATFORM_WINDOWS, PLATFORM_MACOS, PLATFORM_LINUX]

    if os.path.exists("release"):
        print(Fore.BLUE + Style.DIM +
              "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")

    os.mkdir("release")

    if PLATFORM_WINDOWS in platforms:
        if not skip_build:
            wipe_bin()
        build_windows(skip_build, runtimes, client_override)

    if PLATFORM_LINUX in platforms:
        if not skip_build:
            wipe_bin()
        build_linux(skip_build, runtimes, client_override)

    if PLATFORM_LINUX_ARM64 in platforms:
        if not skip_build:
            wipe_bin()
        build_linux_arm64(skip_build, runtimes, client_override)

    if PLATFORM_MACOS in platforms:
        if not skip_build:
            wipe_bin()
        build_macos(skip_build, runtimes, client_override)


def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)

    if os.path.exists("bin"):
        shutil.rmtree("bin")


def build_windows(skip_build: bool, runtimes: bool, client_override: str) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project for Windows x64..." + Style.RESET_ALL)

    if not skip_build:
        publish_client("win-x64", "Windows", runtimes, client_override)
        if sys.platform != "win32":
            subprocess.run(["Tools/exe_set_subsystem.py", p("bin", "Client", "win-x64", "publish", "Robust.Client"), "2"])


    print(Fore.GREEN + "Packaging Windows x64 client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "Content.Client_win-x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("bin", "Content.Client", "win-x64", "publish"), "bin/Content.Client", client_zip, IGNORED_FILES_WINDOWS)
    copy_resources("Resources", client_zip)
    # Cool we're done.
    client_zip.close()

def build_macos(skip_build: bool, runtimes: bool, client_override: str) -> None:
    print(Fore.GREEN + "Building project for macOS x64..." + Style.RESET_ALL)

    if not skip_build:
        publish_client("osx-x64", "MacOS", runtimes, client_override)

    print(Fore.GREEN + "Packaging macOS x64 client..." + Style.RESET_ALL)
    # Client has to go in an app bundle.
    client_zip = zipfile.ZipFile(p("release", "Robust.Client_osx-x64.zip"), "a",
                                 compression=zipfile.ZIP_DEFLATED)

    contents = p("Space Station 14.app", "Contents", "Resources")
    copy_dir_into_zip(p("BuildFiles", "Mac", "Space Station 14.app"), "Space Station 14.app", client_zip)
    copy_dir_into_zip(p("bin", "Content.Client", "win-x64", "publish"), contents, client_zip, IGNORED_FILES_MACOS)
    copy_resources(p(contents, "Resources"), client_zip)
    client_zip.close()


def build_linux(skip_build: bool, runtimes: bool, client_override: str) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project for Linux x64..." + Style.RESET_ALL)

    if not skip_build:
        publish_client("linux-x64", "Linux", runtimes, client_override)

    print(Fore.GREEN + "Packaging Linux x64 client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "Content.Client_linux-x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("bin", "Content.Client", "win-x64", "publish"), "bin/Content.Client", client_zip, IGNORED_FILES_LINUX)
    copy_resources("Resources", client_zip)
    # Cool we're done.
    client_zip.close()



def build_linux_arm64(skip_build: bool, runtimes: bool) -> None:
    # Run a full build.
    # TODO: Linux-arm64 is currently server-only.
    pass
""" print(Fore.GREEN + "Building project for Linux ARM64 (SERVER ONLY)..." + Style.RESET_ALL)

    if not skip_build:
        subprocess.run([
            "dotnet",
            "build",
            "SpaceStation14.sln",
            "-c", "Release",
            "--nologo",
            "/v:m",
            "/p:TargetOS=Linux",
            "/t:Rebuild",
            "/p:FullRelease=True"
        ], check=True)

        publish_client("linux-arm64", "Linux", True)

    print(Fore.GREEN + "Packaging Linux ARM64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Linux_ARM64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server", "linux-arm64", "publish"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    server_zip.close()"""


def publish_client(runtime: str, target_os: str, runtimes: bool, client_override: str) -> None:
    base = [
        "dotnet", "publish",
        "-c", "Release",
        "--runtime", runtime,
        f"/p:TargetOS={target_os}",
        "--use-current-runtime",
        #"-p:PublishSingleFile=true",
        #"/p:FullRelease=True"
    ]
    
    runtimes = [
        "--self-contained",
        "true",
    ]
    base = base + runtimes
    
    client = "Content.Client/Content.Client.csproj"
    if (client_override):
        client = client_override

    subprocess.run(base + [client], check=True)
    subprocess.run(base + ["Content.Client/Content.Client.csproj"], check=True)


def copy_resources(target, zipf):
    do_resource_copy("RobustToolbox/Resources", "RobustToolbox/Resources", zipf, IGNORED_RESOURCES)
    do_resource_copy("Resources", "Resources", zipf, IGNORED_RESOURCES)


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
