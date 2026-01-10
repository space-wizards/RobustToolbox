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
    name: str
    out_file: str
    inputs: list[str] = field(default_factory=list)
    libs: list[str] = field(default_factory=list)
    lib_search_paths: list[str] = field(default_factory=list)
    pkg_config_libs: list[str] = field(default_factory=list)
    export_symbols: list[str] = field(default_factory=list)
    debug: bool = True

os.chdir(os.path.dirname(os.path.realpath(__file__)))

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--release", action="store_true")

    args = parser.parse_args()
    cmd = ["cargo", "build", "-p", "robust-native-server", "-p", "robust-native-client", "-p", "robust-native-universal"]
    if args.release:
        cmd.append("--release")

    subprocess.run(cmd, check=True)

    target_dir = os.path.join("target", "debug")
    if args.release:
        target_dir = os.path.join("target", "release")

    link(LinkerData(
        name="robust_native_client",
        out_file=os.path.join(target_dir, platform_dylib_name("robust_native_client")),
        inputs=[os.path.join(target_dir, platform_staticlib_name("robust_native_client"))],
        pkg_config_libs=["ogg", "opus", "vorbis"],
        export_symbols=read_symbols_def("ogg")
            + read_symbols_def("vorbis")
            + read_symbols_def("opus")
            + read_symbols_def("client"),
        debug=not args.release))

    link(LinkerData(
        name="robust_native_server",
        out_file=os.path.join(target_dir, platform_dylib_name("robust_native_server")),
        inputs=[os.path.join(target_dir, platform_staticlib_name("robust_native_server"))],
        pkg_config_libs=["ogg", "vorbis"],
        export_symbols=read_symbols_def("ogg")
            + read_symbols_def("vorbis"),
        debug=not args.release))

    link(LinkerData(
        name="robust_native_universal",
        out_file=os.path.join(target_dir, platform_dylib_name("robust_native_universal")),
        inputs=[os.path.join(target_dir, platform_staticlib_name("robust_native_universal"))],
        pkg_config_libs=["ogg", "opus", "vorbis"],
        export_symbols=read_symbols_def("ogg")
            + read_symbols_def("vorbis")
            + read_symbols_def("opus")
            + read_symbols_def("client"),
        debug=not args.release))


def link(data: LinkerData):
    system = platform.system()
    if system == "Darwin":
        link_macos(data)
    if system == "Windows":
        link_windows(data)
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

def link_windows(data: LinkerData):
    with tempfile.NamedTemporaryFile("w+", delete_on_close=False) as def_list_file:
        def_list_file.write(f"LIBRARY {data.name}\n")
        def_list_file.write(f"EXPORTS\n")
        def_list_file.writelines((f"    {n.strip()}\n" for n in data.export_symbols))
        def_list_file.close()

        (path, _) = os.path.splitext(data.out_file)
        out_file_temp = path + "_.dll"

        args = [
            "link.exe",
            "/NOLOGO",
            "/IGNORE:4001",
            "/MACHINE:X64",
            f"/DEF:{def_list_file.name}",
            "/NXCOMPAT",
            "/DEBUG",
            ("/OPT:REF,NOICF" if data.debug else "/OPT:REF,ICF"),
            f"/OUT:{out_file_temp}"
        ] + get_pkg_config_linker_flags(data.pkg_config_libs, msvc=True) + data.inputs

        args.extend(("/LIBPATH:" + p for p in data.lib_search_paths))
        args.extend(data.libs)

        args.extend(["psapi.lib", "shell32.lib", "user32.lib", "advapi32.lib", "bcrypt.lib", "legacy_stdio_definitions.lib", "kernel32.lib", "ntdll.lib", "userenv.lib", "ws2_32.lib", "dbghelp.lib", "/defaultlib:msvcrt"])

        subprocess.run(args, check=True)

        if os.path.exists(data.out_file):
            os.remove(data.out_file)

        if os.path.exists(path + ".pdb"):
            os.remove(path + ".pdb")

        os.rename(out_file_temp, data.out_file)
        os.rename(path + "_.pdb", path + ".pdb")

def get_pkg_config_linker_flags(names: list[str], msvc: bool = False) -> list[str]:
    if not names:
        return []

    args = ["pkgconf", "--libs"]
    if msvc:
        args.append("--msvc-syntax")
    args.extend(names)

    result = subprocess.run(args + names, check=True, stdout=subprocess.PIPE)
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
