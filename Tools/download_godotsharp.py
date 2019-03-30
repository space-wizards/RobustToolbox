#!/usr/bin/env python3

import os
import sys
import urllib.request
import shutil

FILES = [
    ("GODOT", "1", "GodotSharp.dll", "https://changelog.ss13.moe/static/GodotSharp.dll"),
]

def main():
    repo_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
    dependencies_dir = os.path.join(repo_dir, "Dependencies", "godot")
    os.makedirs(dependencies_dir, exist_ok=True)

    for n, v, f, c in FILES:
        download_dependency(dependencies_dir, n, v, f, c)

def download_dependency(dependencies_dir, name, version, filename, currentURL):
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


if __name__ == "__main__":
    main()

