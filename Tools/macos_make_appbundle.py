#!/usr/bin/env python3

import argparse
import os
import re
import shutil
import plistlib

p = os.path.join

symlinkable_re = re.compile(r"(?:runtimes|.+\.(?:dll|pdb|json))$", re.IGNORECASE)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--webview", action="store_true")
    parser.add_argument("--name", required=True)
    parser.add_argument("--directory", required=True)
    parser.add_argument("--apphost", required=True)
    parser.add_argument("--identifier", required=True)
    parser.add_argument("--icon")

    args = parser.parse_args()
    dir: str = args.directory
    name: str = args.name

    # Create base app directory structure.
    os.makedirs(p(dir, f"{name}.app", "Contents", "MacOS"), exist_ok=True)
    os.makedirs(p(dir, f"{name}.app", "Contents", "Resources"), exist_ok=True)
    os.makedirs(p(dir, f"{name}.app", "Contents", "Frameworks"), exist_ok=True)

    # Copy apphost
    dest_apphost = p(dir, f"{name}.app", "Contents", "MacOS", name)
    shutil.copy(p(dir, args.apphost), dest_apphost)

    # Symlink most files in the bin dir.
    symlink_files(args.directory, p(dir, f"{name}.app", "Contents", "MacOS"), "")

    # Copy icon
    if args.icon:
        shutil.copy(args.icon, p(dir, f"{name}.app", "Contents", "Resources", "icon.icns"))

    # Write plist
    plist_dat = {
        "CFBundleName": name,
        "CFBundleDisplayName": name,
        "CFBundleIdentifier": args.identifier,
        "CFBundleIconFile": "icon",
        "CFBundleExecutable": name,
        "LSApplicationCategoryType": "public.app-category.games"
    }

    with open(p(dir, f"{name}.app", "Contents", "Info.plist"), "wb") as f:
        plistlib.dump(plist_dat, f)

    if args.webview:
        chromium_framework_path = p(dir, f"{name}.app", "Contents", "Frameworks", "Chromium Embedded Framework.framework")
        if not os.path.exists(chromium_framework_path):
            os.symlink("../../../Chromium Embedded Framework.framework", chromium_framework_path)

        create_webview_helper(dir, name, args.identifier, None, None)
        create_webview_helper(dir, name, args.identifier, "GPU", "gpu")
        create_webview_helper(dir, name, args.identifier, "Renderer", "renderer")
        create_webview_helper(dir, name, args.identifier, "Alerts", "alerts")

def create_webview_helper(dir: str, name: str, identifier: str, suffix: str | None, identifier_suffix: str | None):
    helper_name = f"{name} helper"
    if suffix is not None:
        helper_name += f" ({suffix})"

    sub_app_path = p(dir, f"{name}.app", "Contents", "Frameworks", f"{helper_name}.app")

    os.makedirs(p(sub_app_path, "Contents", "MacOS"), exist_ok=True)
    os.makedirs(p(sub_app_path, "Contents", "Resources"), exist_ok=True)

    # Copy apphost for Robust.Client.WebView
    shutil.copy(p(dir, "Robust.Client.WebView"), p(sub_app_path, "Contents", "MacOS", helper_name))

    # Symlink files
    symlink_files(dir, p(sub_app_path, "Contents", "MacOS"), "../../../")

    helper_identifier = f"{identifier}.cef.{identifier_suffix}"

    if identifier_suffix is not None:
        helper_identifier += "." + identifier_suffix

    plist_dat = {
        "CFBundleName": f"{name} helper",
        "CFBundleDisplayName": f"{name} helper",
        "CFBundleIdentifier": f"{identifier}.cef.{identifier_suffix}",
        "CFBundleExecutable": helper_name
    }

    with open(p(sub_app_path, "Contents", "Info.plist"), "wb") as f:
        plistlib.dump(plist_dat, f)

def symlink_files(src_dir: str, dest_dir: str, relative: str):
    for file in os.listdir(src_dir):
        if not symlinkable_re.match(file):
            continue

        dest_symlink = p(dest_dir, file)
        if not os.path.islink(dest_symlink):
            os.symlink(f"../../../{relative}{file}", dest_symlink)


main()
