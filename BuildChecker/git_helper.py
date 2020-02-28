#!/usr/bin/env python3
# Installs git hooks, updates them, updates submodules, that kind of thing.

import subprocess
import sys
import os
import shutil
from pathlib import Path
from typing import List

BUILD_CHECKER_PATH = Path(Path(__file__).resolve().parent)
SS14_ROOT_PATH = Path(BUILD_CHECKER_PATH.parent)
SOLUTION_PATH = Path(SS14_ROOT_PATH/"SpaceStation14.sln")
CURRENT_HOOKS_VERSION = "2" # If this doesn't match the saved version we overwrite them all.
QUIET = "--quiet" in sys.argv
NO_HOOKS = "--nohooks" in sys.argv

def run_command(command: List[str], capture: bool = False) -> subprocess.CompletedProcess:
    """
    Runs a command with pretty output.
    """
    text = ' '.join(command)
    if not QUIET:
        print("$ {}".format(text))

    sys.stdout.flush()

    completed = None

    if capture:
        completed = subprocess.run(command, cwd=str(SS14_ROOT_PATH), stdout=subprocess.PIPE)
    else:
        completed = subprocess.run(command, cwd=str(SS14_ROOT_PATH))

    if completed.returncode != 0:
        raise RuntimeError("Error: command exited with code {}!".format(completed.returncode))

    return completed


def update_submodules():
    """
    Updates all submodules.
    """

    status = run_command(["git", "submodule", "update", "--init", "--recursive"], capture=True)

    if status.stdout.decode().strip():
        print("Git submodules changed. Reloading solution.")
        reset_solution()

def install_hooks():
    """
    Installs the necessary git hooks into .git/hooks.
    """

    # Read version file.
    hooks_version_file = BUILD_CHECKER_PATH/"INSTALLED_HOOKS_VERSION"

    if os.path.isfile(str(hooks_version_file)):
        with open(str(hooks_version_file), "r") as f:
            if f.read() == CURRENT_HOOKS_VERSION:
                if not QUIET:
                    print("No hooks change detected.")
                return

    with open(str(hooks_version_file), "w") as f:
        f.write(CURRENT_HOOKS_VERSION)

    print("Hooks need updating.")

    hooks_target_dir = SS14_ROOT_PATH/".git"/"hooks"
    hooks_source_dir = BUILD_CHECKER_PATH/"hooks"

    if not os.path.exists(str(hooks_target_dir)):
        os.makedirs(str(hooks_target_dir))

    # Clear entire tree since we need to kill deleted files too.
    for filename in os.listdir(str(hooks_target_dir)):
        os.remove(str(hooks_target_dir/filename))

    for filename in os.listdir(str(hooks_source_dir)):
        print("Copying hook {}".format(filename))
        shutil.copy2(str(hooks_source_dir/filename), str(hooks_target_dir/filename))


def reset_solution():
    """
    Force VS to think the solution has been changed to prompt the user to reload it,
    thus fixing any load errors.
    """

    with SOLUTION_PATH.open("r") as f:
        content = f.read()

    with SOLUTION_PATH.open("w") as f:
        f.write(content)

def main():
    if not NO_HOOKS:
        install_hooks()
    update_submodules()

if __name__ == '__main__':
    main()
