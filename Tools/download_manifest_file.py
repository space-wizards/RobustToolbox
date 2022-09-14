import argparse
import json
import os
import requests
import struct
import typing
import zstandard

# Very crappy script to download blobs from manifest download protocol.
# Not well-written. DO NOT use as a thorough protocol reference.

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("server", help="Game server address. Must not have a trailing /")
    parser.add_argument("file_path", help="The VFS path to download from the manifest")
    parser.add_argument("-o", "--output", help="Output path to store the file at. Defaults to file name")
    args = parser.parse_args()

    server = args.server
    file_path = args.file_path
    output = args.output

    if not output:
        output = file_path.split("/")[-1]

    server_info = requests.get(f"{server}/info").json()
    (manifest_url, dl_url) = cdn_urls(server, server_info["build"])

    manifest = requests.get(manifest_url).text

    manifest_lines = iter(manifest.splitlines())

    header = next(manifest_lines)
    print(f"header: {header}")

    i = 0
    for line in manifest_lines:
        (hash, path) = line.split(" ", maxsplit=1)
        if path == file_path:
            break
        i += 1
    else:
        print("Unable to find file in manifest")
        exit(1)

    print(f"File index: {i}")

    dl_req = struct.pack("<I", i)
    dl_resp = requests.post(dl_url, data=dl_req, headers={"Content-Type": "application/octet-stream", "X-Robust-Download-Protocol": "1"}, stream=True)
    dl_resp.raise_for_status()

    dl_head = struct.unpack("<I", dl_resp.raw.read(4))[0]
    print(dl_head)

    blob_compress_enabled = (dl_head & 1) != 0

    file_length = struct.unpack("<I", dl_resp.raw.read(4))[0]
    read_length = file_length
    blob_is_compressed = False
    if blob_compress_enabled:
        compr_length = struct.unpack("<I", dl_resp.raw.read(4))[0]
        if compr_length != 0:
            blob_is_compressed = True
            read_length = compr_length

    data = dl_resp.raw.read(read_length)

    if blob_is_compressed:
        data = zstandard.decompress(data)

    open(output, "wb").write(data)


def cdn_urls(server: str, build_info) -> typing.Tuple[str, str]:
    if build_info["acz"]:
        return (f"{server}/manifest.txt", f"{server}/download")

    return build_info["manifest_url"], build_info["manifest_download_url"]

if __name__ == "__main__":
    main()
