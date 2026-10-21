#!/usr/bin/env python3

#
# This helper exists SOLELY to work around https://github.com/rust-lang/rfcs/issues/2771
# Instead of compiling our libs directly to a cdylib, we compile them to a staticlib
# and link manually. This is cool and all, except that's a lot of work.
# And involves three platform-specific linkers. *sigh*
#

import argparse
import os
import subprocess
import platform
import shlex
import tempfile

from dataclasses import dataclass, field

@dataclass
class LinkerData:
    out_file: str
    inputs: list[str] = field(default_factory=list)
    libs: list[str] = field(default_factory=list)
    lib_search_paths: list[str] = field(default_factory=list)
    pkg_config_libs: list[str] = field(default_factory=list)
    export_symbols: list[str] = field(default_factory=list)

os.chdir(os.path.dirname(os.path.realpath(__file__)))

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--release", action="store_true")

    args = parser.parse_args()
    cmd = ["cargo", "build", "-p", "robust-native-server", "-p", "robust-native-client"]
    if args.release:
        cmd.append("--release")

    subprocess.run(cmd, check=True)

    target_dir = os.path.join("target", "debug")
    if args.release:
        target_dir = os.path.join("target", "release")

    link(LinkerData(
        out_file=os.path.join(target_dir, platform_dylib_name("robust_native_client")),
        inputs=[os.path.join(target_dir, platform_staticlib_name("robust_native_client"))],
        pkg_config_libs=["ogg", "opus", "vorbis"],
        export_symbols=read_symbols_def("ogg")
            + read_symbols_def("vorbis")
            + read_symbols_def("opus")
            + read_symbols_def("client")))

    link(LinkerData(
        out_file=os.path.join(target_dir, platform_dylib_name("robust_native_server")),
        inputs=[os.path.join(target_dir, platform_staticlib_name("robust_native_server"))]))

def link(data: LinkerData):
    system = platform.system()
    if system == "Darwin":
        link_macos(data)
    else:
        raise NotImplementedError()

#
# Per-platform linker invocations are mostly inspired by what rustc does
#

def link_macos(data: LinkerData):
    symbol_list_file = tempfile.NamedTemporaryFile("w+")
    symbol_list_file.writelines((f"_{n.strip()}\n" for n in data.export_symbols))
    symbol_list_file.flush()

    # Todo: crosscompile
    args = [
        "cc",
        "-Wl,-exported_symbols_list",
        f"-Wl,{symbol_list_file.name}",
        "-arch", "arm64",
        "-nodefaultlibs",
        "-Wl,-dead_strip",
        "-mmacosx-version-min=11.0.0",
        "-dynamiclib",
        "-o", data.out_file,
        "-liconv", "-lSystem", "-lc", "-lm"
    ] + get_pkg_config_linker_flags(data.pkg_config_libs) + data.inputs

    args.extend(("-L" + p for p in data.lib_search_paths))
    args.extend(("-l" + l for l in data.libs))

    subprocess.run(args, check=True)

def get_pkg_config_linker_flags(names: list[str]) -> list[str]:
    if not names:
        return []

    result = subprocess.run(["pkg-config", "--libs"] + names, check=True, stdout=subprocess.PIPE)
    return shlex.split(result.stdout.decode("utf-8"))

def read_symbols_def(name: str) -> list[str]:
    with open(os.path.join("symbols", name + ".txt"), "r") as f:
        return [line.strip() for line in f.readlines() if not line.startswith("#")]

def platform_staticlib_name(name: str) -> str:
    if platform.system() == "Windows":
        return name + ".lib"

    return f"lib{name}.a"

def platform_dylib_name(name: str) -> str:
    if platform.system() == "Windows":
        return name + ".dll"

    if platform.system() == "Darwin":
        return f"lib{name}.dylib"

    return f"lib{name}.so"

main()
