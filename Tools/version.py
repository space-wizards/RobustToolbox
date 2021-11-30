#!/usr/bin/env python3

# Tools/version.py 1.2.3/v1.2.3

# "I fucking hate the version update script" - Vera the Gradient, 2021
# In the event that you have a hatred of this script, here's what you do:
# + Modify RobustToolbox/MSBuild/Robust.Engine.Version.props
# + Create a tag of the form "v1.2.3" where the version number matches whatever you wrote into the props file
# + Publish the tag on master

import subprocess
import sys
import argparse
import time

parser = argparse.ArgumentParser(description = "Tool for versioning RobustToolbox: commits the version config update and sets your local tag.")
parser.add_argument("version", help = "Version that will be written to tag - must be of form '1.2.3' or 'v1.2.3'")
parser.add_argument("--file-only", action = "store_true", help = "Does not perform the Git part of the update (for writes only, not undos!)")
parser.add_argument("--undo", action = "store_true", help = "Macro to rebase over last commit and remove version tag. Version still required.")

result = parser.parse_args()

def verify_version():
    vr = result.version
    if vr.startswith("v"):
        # strip initial v
        vr = vr[1:]
    parts = vr.split(".")
    if len(parts) != 3:
        print("Version must be split into three parts with '.'")
        sys.exit(1)
    for v in parts:
        # this verifies parsability, exceptions here are expected for bad input
        int(v)
    return vr

def write_version():
    # Writing operation
    if result.version == None:
        print("Version required for a writing operation (try --help)")
        sys.exit(1)

    # Verify
    ver = verify_version()

    # Update
    file = open("MSBuild/Robust.Engine.Version.props", "w", newline="\n")
    file.write("<Project>\n")
    file.write("    <!-- This file automatically reset by Tools/version.py -->\n")
    file.write("    <PropertyGroup><Version>" + ver + "</Version></PropertyGroup>\n")
    file.write("</Project>\n")
    file.close()

    if not result.file_only:
        # Commit
        subprocess.run(["git", "commit", "--allow-empty", "-m", "Version: " + ver, "MSBuild/Robust.Engine.Version.props"]).check_returncode()

        # Tag
        subprocess.run(["git", "tag", "v" + ver]).check_returncode()
        print("Tagged as v" + ver)
    else:
        print("Did not tag " + ver)

def undo_version():
    # Might want to work out some magic here to auto-identify the version from the commit
    if result.version == None:
        print("Version required for undo operation (try --help)")
        sys.exit(1)

    # Verify
    ver = verify_version()

    # Delete the version (good verification all by itself really)
    subprocess.run(["git", "tag", "-d", "v" + ver]).check_returncode()
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

