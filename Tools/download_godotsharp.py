#!/usr/bin/env python3
import os
import requests

GODOTSHARP_URL = "https://ss14.silvertorch5.io/ss14_builds/godotsharp/GodotSharp.dll"

def main():
    print("Downloading GodotSharp.dll maybe...")
    repo_dir = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
    godotsharp_dir = os.path.join(repo_dir, "SS14.Client.Godot", ".mono", "assemblies")
    os.makedirs(godotsharp_dir, exist_ok=True)
    godotsharp_filename = os.path.join(godotsharp_dir, "GodotSharp.dll")
    last_modified_filename = os.path.join(godotsharp_dir, "LAST_MODIFIED")

    headers = {
        "User-Agent": "GodotSharp fetcher"
    }

    if os.path.exists(godotsharp_filename) and os.path.exists(last_modified_filename):
        with open(last_modified_filename, "r") as f:
            last_modified = f.read().strip()
        headers["If-Modified-Since"] = last_modified
        #print(headers)

    r = requests.get(GODOTSHARP_URL, headers=headers)
    #print(r.headers)

    if not r.ok:
        print("ERROR: Bad status code from GodotSharp download!")
        print(r.status_code)
        exit(1)

    # Not modified!
    if r.status_code == 304:
        print("File not modified. Not downloading.")
        exit(0)

    print("File modified. Downloading.")
    with open(godotsharp_filename, "wb") as f:
        f.write(r.content)

    with open(last_modified_filename, "w") as f:
        f.write(r.headers["Last-Modified"])

if __name__ == '__main__':
    main()
