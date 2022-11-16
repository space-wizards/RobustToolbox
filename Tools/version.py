#!/usr/bin/env python3

# Tools/version.py 1.2.3

import subprocess
import sys
import os
import argparse
import time
from typing import List

def main():
    parser = argparse.ArgumentParser(description = "Tool for versioning RobustToolbox: commits the version config update and sets your local tag.")
    parser.add_argument("version", help = "Version that will be written to tag. Format: 0.x.x.x")
    parser.add_argument("--file-only", action = "store_true", help = "Does not perform the Git part of the update (for writes only, not undos!)")
    parser.add_argument("--undo", action = "store_true", help = "Macro to rebase over last commit and remove version tag. Version still required.")

    result = parser.parse_args()

    version:   str  = result.version
    undo:      bool = result.undo
    file_only: bool = result.file_only

    if undo:
        undo_version(version)
    else:
        write_version(version, file_only)


def verify_version(version: str):
    parts = version.split(".")
    if len(parts) != 4:
        print("Version must be split into four parts with '.'")
        sys.exit(1)
    for v in parts:
        # this verifies parsability, exceptions here are expected for bad input
        int(v)
    if int(parts[0]) != 0:
        print("Major version must be 0")
        sys.exit(1)

def write_version(version: str, file_only: bool):
    # Writing operation
    if version == None:
        print("Version required for a writing operation (try --help)")
        sys.exit(1)

    # Verify
    verify_version(version)

    update_release_notes(version)

    # Update
    with open("MSBuild/Robust.Engine.Version.props", "w") as file:
        file.write("<Project>" + os.linesep)
        file.write("    <!-- This file automatically reset by Tools/version.py -->"  + os.linesep)
        file.write("    <PropertyGroup><Version>" + version + "</Version></PropertyGroup>" + os.linesep)
        file.write("</Project>" + os.linesep)

    if not file_only:
        # Commit
        subprocess.run(["git", "commit", "--allow-empty", "-m", "Version: " + version, "MSBuild/Robust.Engine.Version.props", "RELEASE-NOTES.md"], check=True)

        # Tag
        subprocess.run(["git", "tag", "v" + version], check=True)
        print("Tagged as v" + version)
    else:
        print("Did not tag " + version)


def update_release_notes(version: str):
    with open("RELEASE-NOTES.md", "r") as file:
        lines = file.readlines()

    template_start = lines.index("<!--START TEMPLATE\n")
    template_end   = lines.index("END TEMPLATE-->\n", template_start)
    master_header  = lines.index("## Master\n", template_end)

    template_lines = lines[template_start + 1 : template_end]

    # Go through and delete "*None yet*" entries.
    i = master_header
    while i < len(lines):
        if lines[i] != "*None yet*\n":
            i += 1
            continue

        # Delete many lines around it, to remove the header and some whitespace too.
        del lines[i - 3 : i + 1]
        i -= 3

    # Replace current "master" header with the new version to tag.
    lines[master_header] = f"## {version}\n"

    # Insert template above newest version.
    lines[master_header : master_header] = template_lines

    with open("RELEASE-NOTES.md", "w") as file:
        file.writelines(lines)


def undo_version(version: str):
    # Might want to work out some magic here to auto-identify the version from the commit
    if version == None:
        print("Version required for undo operation (try --help)")
        sys.exit(1)

    # Delete the version (good verification all by itself really)
    subprocess.run(["git", "tag", "-d", "v" + version], check=True)
    # Tag the commit we're about to delete because we could be deleting the wrong thing.
    savename = "version-undo-backup-" + str(int(time.time()))
    subprocess.run(["git", "tag", savename], check=True)
    # *Outright eliminate the commit from the branch!* - Dangerous if we get rid of the wrong commit, hence backup
    subprocess.run(["git", "reset", "--keep", "HEAD^"], check=True)
    print("Done (deleted commit saved as " + savename + ")")

main()
