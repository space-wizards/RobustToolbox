#!/usr/bin/env python3

import os
import shutil
import tempfile
import urllib.request
import zipfile

NATIVES_TARGET_DIR = os.path.join("..", "Third-Party", "extlibs", "Windows")
CSFML_ZIP_URL = "https://www.sfml-dev.org/files/CSFML-2.4-windows-32-bit.zip"
NATIVES_ZIP_DIR = "CSFML/bin/"
NATIVE_FILES = [
    "csfml-audio-2.dll",
    "csfml-system-2.dll",
    "csfml-window-2.dll",
    "csfml-graphics-2.dll",
    "csfml-network-2.dll",
]

def main():
    # Since the script isn't ran by the build file if the natives already exist.
    # ALWAYS refresh them when we're ran.
    if os.path.exists(NATIVES_TARGET_DIR):
        shutil.rmtree(NATIVES_TARGET_DIR)

    os.makedirs(NATIVES_TARGET_DIR)

    print(NATIVES_TARGET_DIR)

    # Download CSFML for i386 Windows.
    response = urllib.request.urlopen(CSFML_ZIP_URL)
    data = response.read()
    # Write data to temporary file since zipfile won't read from bytes objects.
    with tempfile.TemporaryFile() as tmp:
        tmp.write(data)
        tmp.seek(0)
        with zipfile.ZipFile(tmp) as zipf:
            for native in NATIVE_FILES:
                fullnative = NATIVES_ZIP_DIR + native
                contents = zipf.read(fullnative)
                with open(os.path.join(NATIVES_TARGET_DIR, native), "wb") as nativef:
                    nativef.write(contents)

if __name__ == '__main__':
    main()
