#!/usr/bin/env python3

# Tools/version.py 1.2.3

import subprocess
import sys
import argparse
import time

parser = argparse.ArgumentParser(description = "Tool for versioning RobustToolbox: commits the version config update and sets your local tag.")
parser.add_argument("version", help = "Version that will be written to tag")
parser.add_argument("--file-only", action = "store_true", help = "Does not perform the Git part of the update (for writes only, not undos!)")
parser.add_argument("--undo", action = "store_true", help = "Macro to rebase over last commit and remove version tag. Version still required.")

result = parser.parse_args()

def verify_version():
    parts = result.version.split(".")
    if len(parts) != 3:
        print("Version must be split into three parts with '.'")
        sys.exit(1)
    for v in parts:
        # this verifies parsability, exceptions here are expected for bad input
        int(v)

def write_version():
    # Writing operation
    if result.version == None:
        print("Version required for a writing operation (try --help)")
        sys.exit(1)

    # Verify
    verify_version()

    # Update
    file = open("MSBuild/Robust.Engine.Version.props", "w", newline="\n")
    file.write("<Project>\n")
    file.write("    <!-- This file automatically reset by Tools/version.py -->\n")
    file.write("    <PropertyGroup><Version>" + result.version + "</Version></PropertyGroup>\n")
    file.write("</Project>\n")
    file.close()

    # Commit
    subprocess.run(["git", "commit", "--allow-empty", "-m", "Version: " + result.version, "MSBuild/Robust.Engine.Version.props"]).check_returncode()

    # Tag
    if not result.file_only:
        subprocess.run(["git", "tag", "v" + result.version]).check_returncode()
    print("Tagged as v" + result.version)

def undo_version():
    # Might want to work out some magic here to auto-identify the version from the commit
    if result.version == None:
        print("Version required for undo operation (try --help)")
        sys.exit(1)
    # Delete the version (good verification all by itself really)
    subprocess.run(["git", "tag", "-d", "v" + result.version]).check_returncode()
    # Tag the commit we're about to delete because we could be deleting the wrong thing.
    savename = "version-undo-backup-" + str(int(time.time()))
    subprocess.run(["git", "tag", savename]).check_returncode()
    # *Outright eliminate the commit from the branch!* - Dangerous if we get rid of the wrong commit, hence backup
    subprocess.run(["git", "reset", "--keep", "HEAD^"]).check_returncode()
    print("Done (deleted commit saved as " + savename + ")")

if result.undo:
    undo_version()
else:
    write_version()

