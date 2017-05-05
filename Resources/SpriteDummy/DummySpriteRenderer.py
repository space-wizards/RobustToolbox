#!/usr/bin/env python3
# Used for debugging BuildResourcePack.py without needing to be on Windows.
# Writes a single dummy png to the output dir.
# Doesn't work on Windows due to lack of shebangs. RIP.
# Well at least not directly from buildResourcePack.py
# NOTE: Requires Pillow

from PIL import Image
import os

print("*** Running dummy sprite renderer! ***")
image = Image.new("RGB", (32, 32), "#FF0000")

if not os.path.exists("output"):
    os.mkdir("output")

image.save(os.path.join("output", "test.png"))