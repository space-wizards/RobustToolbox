#!/usr/bin/env python3

# This script currently downloads freetyp6.dll and openal32.dll for Windows.
# macOS and Linux both provide those I'm pretty sure so we don't care.
# Well actually I think Linux might not be guaranteed to provide openAL but I don't care yet.

import os
import sys
import urllib.request
import shutil

FILES = [
    ("FREETYPE", "1", "freetype6.dll", "https://github.com/Robmaister/SharpFont.Dependencies/blob/2ea4fe0dc23e8f76d8a51f8ce78becb0faf3a2af/freetype2/2.6/msvc12/x64/freetype6.dll?raw=true"),
    ("OPENAL", "1", "openal32.dll", "https://github.com/opentk/opentk-dependencies/blob/9eb4991b871d2d2c0745d2c8c8c0fa6404f56438/x64/openal32.dll?raw=true")
]

def main():
    platform = sys.argv[1]
    target_os = sys.argv[2]

    # Hah good luck passing something containing a space to the Exec MSBuild Task.
    target_dir = " ".join(sys.argv[3:])

    if platform != "x64":
        print("Error: Unable to download misc deps for any platform outside x64. "
              "If you REALLY want x86 support for some misguided reason, I'm not providing it.")
        exit(1)

    if target_os != "Windows":
        exit(0)

    repo_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
    dependencies_dir = os.path.join(repo_dir, "Dependencies", "misc")
    os.makedirs(dependencies_dir, exist_ok=True)

    for n, v, f, c in FILES:
        download_dependency(target_dir, dependencies_dir, n, v, f, c)

def download_dependency(target_dir, dependencies_dir, name, version, filename, currentURL):
    version_file = os.path.join(dependencies_dir, name + "_VERSION")

    existing_version = "?"
    if os.path.exists(version_file):
        with open(version_file, "r") as f:
            existing_version = f.read().strip()

    dependency_path = os.path.join(dependencies_dir, filename)

    if existing_version != version and os.path.exists(dependency_path):
        os.remove(dependency_path)

    with open(version_file, "w") as f:
        f.write(version)

    if not os.path.exists(dependency_path):
        urllib.request.urlretrieve(currentURL, dependency_path)

    target_file_path = os.path.join(target_dir, filename)

    if not os.path.exists(target_file_path) or \
       os.stat(dependency_path).st_mtime > os.stat(target_file_path).st_mtime:
        shutil.copy2(dependency_path, target_file_path)


if __name__ == "__main__":
    main()

