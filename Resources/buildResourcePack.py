#!/usr/bin/env python3
# -*- coding: UTF-8 -*-

# Potential improvements:
#  * Asynchronous file I/O.
#  * Stream files in from MSpriteRenderer instead of waiting for it to complete.
#  * Don't wait on all atlasses to finish before starting non-texture files.

# TODO:
#  * Better handling of errors.
#  * Better logging.

import argparse
import asyncio
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import typing # autism.
import zipfile

try:
    from colorama import init, Fore, Back, Style
    init()

except ImportError:
    # Just give an empty string for everything, no colored logging.
    class ColorDummy(object):
        def __getattr__(self, name):
            return ""

    Fore = ColorDummy()
    Style = ColorDummy()
    Back = ColorDummy()

IS_WINDOWS = sys.platform in ("win32", "cygwin")


async def main():
    parser = argparse.ArgumentParser(description="Builds the SS14 resource pack zip file.")
    parser.add_argument("--no-atlas",
                        dest="atlas",
                        help=("Disable texture atlas generation. "
                              "This severely ruins rendering performance in game. Beware."),
                        action="store_false")

    parser.add_argument("--no-animations",
                        dest="animations",
                        help="Skip generating animations",
                        action="store_false")

    parser.add_argument("--sprite-renderer",
                        dest="renderer",
                        type=Path,
                        default=Path("SpriteRenderer/MSpriteRenderer.exe"),
                        help="Provide a custom path to the animations sprite renderer's directory.")

    parser.add_argument("--atlas-tool",
                        dest="atlas_tool",
                        type=str,
                        default="../Tools/AtlasTool.exe",
                        help="Provide a custom path to the atlas tool used.")

    parser.add_argument("--out",
                        dest="out",
                        type=Path,
                        default=Path("ResourcePack.zip"),
                        help="Output file.")

    parser.add_argument("--no-temp-out-file",
                        dest="temp_out",
                        action="store_false",
                        help=("Do not write to a temporarily file during operation, "
                              "instead write to the output file directly. "
                              "Note that this will cause a current zip to be corrupted "
                              "if the script gets killed midway through."))

    parser.add_argument("--resources-dir",
                        dest="source",
                        type=Path,
                        default=Path("."),
                        help="Set the directory to pull resources from.")

    # For debugging on non-Windows systems.
    # You're probably gonna want to pass --sprite-renderer SpriteDummy/DummySpriteRenderer.py too.
    parser.add_argument("--force-animations-build",
                        dest="force_animations",
                        action="store_true",
                        help=argparse.SUPPRESS)
    

    args = parser.parse_args()

    animations = args.animations
    atlas = args.atlas
    atlas_command = [args.atlas_tool]
    atlas_dir = None
    textures_path = args.source.joinpath("Textures")

    if not IS_WINDOWS:
        if animations and not args.force_animations:
            print(Fore.YELLOW + "WARNING: animations generation will be disabled because you are not on Windows.")
            print("  Pass --no-animations to surpress this warning." + Style.RESET_ALL)
            animations = False
        
        # Maybe softcode this to allow using .NET core?
        atlas_command.insert(0, "mono")

    outfile = args.out
    if args.temp_out:
        outfile = outfile.with_suffix(".tmp")

    zip_target = zipfile.ZipFile(str(outfile), "w", zipfile.ZIP_DEFLATED, allowZip64=False)
    
    if atlas:
        atlas_dir = tempfile.TemporaryDirectory()
        atlas_dir.path = Path(atlas_dir.name)

    # Separare so we can easily hand them to build_animations()
    # Which can then easily hand them to handle_texture_directory.
    # Also bit of a misnomer, sorry about that.
    atlas_args = {
        "zip_target": zip_target,
        "atlas": atlas,
        "atlas_command": atlas_command,
        "atlas_out_dir": atlas_dir.name if atlas_dir else Path("")
    }

    # Animations atlas building gets done after build_animations.
    # If animations are done.
    tasks = [
        handle_texture_directory(
            input_dir=textures_path.joinpath(name),
            atlas_name=name,
            **atlas_args
        ) for name in [
            "Decals",
            "Effects",
            "Items",
            "Objects",
            "Tiles",
            "UserInterface"
        ]
    ]

    tasks.append(handle_texture_directory(textures_path.joinpath("Unatlased"), zip_target, False))

    if animations:
        tasks.append(build_animations(args.renderer, atlas_args))

    await asyncio.gather(*tasks)

    if atlas:
        # Gotta copy over the files from the atlas output dir.
        for filepath in atlas_dir.path.iterdir():
            zip_path = Path("TAI" if filepath.suffix == ".TAI" else "Textures")
            zip_target.write(str(filepath), str(zip_path.joinpath(filepath.name)))

        write_zip_dir_node(zip_target, "TAI/")

    write_zip_dir_node(zip_target, "Textures/")

    # Copy over non-image files.
    for otherassets in ["Fonts", "ParticleSystems", "Shaders"]:
        for (dirpath, dirnames, filenames) in os.walk(str(args.source.joinpath(otherassets))):
            dirpath = Path(dirpath)
            write_zip_dir_node(zip_target, str(dirpath.relative_to(args.source)))
            for filename in filenames:
                filepath = dirpath.joinpath(filename)
                targetpath = filepath.relative_to(args.source)

                print(Fore.CYAN + "Wrote {0} -> {1}".format(filepath, targetpath) + Style.RESET_ALL)
                zip_target.write(str(filepath), str(targetpath))


    # Clean up.
    zip_target.close()
    if args.temp_out:
        outfile.replace(args.out)

    if atlas_dir is not None:
        atlas_dir.cleanup()


async def build_animations(path: Path, atlas_args: typing.Dict[str, typing.Any]):
    dirpath = path.parent
    outdir = dirpath.joinpath("output")
    if outdir.exists():
        print(Fore.CYAN + "Deleting previous output from the animation renderer..." + Style.RESET_ALL)
        def handle_errors(function, path, excinfo):
            print(Fore.RED + "  ERROR: failed to remove {0}: {1}".format(path, excinfo[1].strerror) + Style.RESET_ALL)
        
        shutil.rmtree(str(outdir), onerror=handle_errors)

    outdir.mkdir()

    print(Fore.GREEN + "Running the animation renderer, this will take a while..." + Style.RESET_ALL)
    exec_path = path.resolve()
    process = await asyncio.create_subprocess_exec(str(exec_path), cwd=str(dirpath))
    await process.wait()

    await handle_texture_directory(input_dir=outdir, atlas_name="Animations", **atlas_args)

    zip_target = atlas_args["zip_target"]
    write_zip_dir_node(zip_target, "Animations/")
    # Write animation XML files to ZIP/Animations/
    for xmlfile in outdir.glob("*.xml"):
        targetpath = Path("Animations").joinpath(xmlfile.name)
        print(Fore.CYAN + "Wrote {0} -> {1}".format(xmlfile, targetpath) + Style.RESET_ALL)
        zip_target.write(str(xmlfile), str(targetpath))


async def handle_texture_directory(input_dir: Path,
                                   zip_target: zipfile.ZipFile,
                                   atlas: bool,
                                   atlas_name: str = None,
                                   atlas_command: typing.List[str] = None,
                                   atlas_out_dir: Path = None):

    if not atlas:
        # NOTE: Hardcoding for .png is probably bad.
        # But it prevents the animation XML files from being copied so...
        for pngfile in input_dir.glob("**/*.png"):
            texturepath = Path("Textures").joinpath(pngfile.name)
            zip_target.write(str(pngfile), str(texturepath))
            print(Fore.CYAN + "Wrote {0} -> {1}".format(pngfile, texturepath) + Style.RESET_ALL)

        return

    print(Fore.GREEN + "Generating atlas for {0}...".format(atlas_name) + Style.RESET_ALL)
    process = await asyncio.create_subprocess_exec(
        *atlas_command,
        "-n", atlas_name,
        "-o", str(atlas_out_dir),
        "-i", str(input_dir)
    )

    print(Fore.GREEN + "Done generating atlas for {0}.".format(atlas_name) + Style.RESET_ALL)

    await process.wait()


# If it's stupid and it works, is it still stupid?
def write_zip_dir_node(file: zipfile.ZipFile, path: str):
    """
    Attempt to write a dummy directory node into the zip file.
    Does nothing if the node already exists.
    """

    if not path.endswith("/"):
        path += "/"

    try:
        info = file.getinfo(path)

    except:
        info = zipfile.ZipInfo()
        info.date_time = (2001, 9, 11, 8, 46, 0)
        info.filename = path

        file.writestr(info, "")
        print(Fore.CYAN + Style.DIM + "Wrote " + path + " zip directory." + Style.RESET_ALL)


if __name__ == "__main__":
    loop = asyncio.get_event_loop()
    if IS_WINDOWS:
        loop = asyncio.ProactorEventLoop()
        asyncio.set_event_loop(loop)

    loop.run_until_complete(main())
