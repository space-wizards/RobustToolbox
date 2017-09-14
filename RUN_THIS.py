#!/usr/bin/env python3

# Import future so people on py2 still get the clear error that they need to upgrade.
from __future__ import print_function
import os
import sys
import traceback

VERSION = sys.version_info
NO_PROMPT = "--no-prompt" in sys.argv

sane_input = raw_input if VERSION.major < 3 else input

def main():
    if VERSION.major < 3 or (VERSION.major == 3 and VERSION.minor < 5):
        print("ERROR: You need at least Python 3.5 to build SS14.")
        # Need "press enter to exit" stuff because Windows just immediately closes conhost.
        if not NO_PROMPT:
            sane_input("Press enter to exit...")
        exit(1)

    # Import git_helper by modifying the path.
    ss14_dir = os.path.dirname(os.path.abspath(__file__))
    sys.path.append(os.path.join(ss14_dir, "BuildChecker"))

    try:
        import git_helper
        git_helper.main()

    except Exception as e:
        print("ERROR:")
        traceback.print_exc()
        print("This was NOT intentional. If the error is not immediately obvious, ask on Discord or IRC for help.")
        if not NO_PROMPT:
            sane_input("Press enter to exit...")
        exit(1)

if __name__ == "__main__":
    main()
    if not NO_PROMPT:
        sane_input("Success! Press enter to exit...")
