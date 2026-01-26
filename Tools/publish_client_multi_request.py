#!/usr/bin/env python3

"""
Publish build artifacts to Robust.Cdn using the "multi-request" publish API:

  POST /fork/{fork}/publish/start
  POST /fork/{fork}/publish/file   (repeat for each file)
  POST /fork/{fork}/publish/finish

Authentication:
  Authorization: Bearer <token>

This uploads files directly (streamed) to the CDN server, so it does not need
SSH access and does not load whole zips into memory.
"""

import argparse
import http.client
import json
import os
import sys
from pathlib import Path
from typing import Dict, Iterable, Tuple
from urllib.parse import urlsplit


def _split_base_url(base_url: str) -> Tuple[str, str, str]:
    # Returns (scheme, netloc, base_path_without_trailing_slash)
    if not base_url.endswith("/"):
        raise ValueError("CDN base URL must end with '/' (e.g. https://cdn.example.com/)")

    u = urlsplit(base_url)
    if u.scheme not in ("http", "https"):
        raise ValueError("CDN base URL must be http(s)")
    if not u.netloc:
        raise ValueError("CDN base URL missing host")

    base_path = u.path.rstrip("/")
    return u.scheme, u.netloc, base_path


def _connection(scheme: str, netloc: str) -> http.client.HTTPConnection:
    if scheme == "https":
        return http.client.HTTPSConnection(netloc, timeout=300)
    return http.client.HTTPConnection(netloc, timeout=300)


def _check_response(resp: http.client.HTTPResponse, action: str) -> None:
    # Robust.Cdn returns 204 NoContent on success.
    if 200 <= resp.status < 300:
        # Drain body for keep-alive safety.
        _ = resp.read()
        return

    body = resp.read().decode("utf-8", errors="replace")
    raise RuntimeError(f"{action} failed: HTTP {resp.status} {resp.reason}\n{body}")


def _post_json(
    conn: http.client.HTTPConnection,
    path: str,
    token: str,
    payload: Dict[str, str],
    action: str,
) -> None:
    data = json.dumps(payload).encode("utf-8")
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json",
        "Content-Length": str(len(data)),
    }

    conn.request("POST", path, body=data, headers=headers)
    resp = conn.getresponse()
    _check_response(resp, action)


def _post_file(
    conn: http.client.HTTPConnection,
    path: str,
    token: str,
    version: str,
    file_path: Path,
) -> None:
    size = file_path.stat().st_size
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/octet-stream",
        "Content-Length": str(size),
        "Robust-Cdn-Publish-File": file_path.name,
        "Robust-Cdn-Publish-Version": version,
    }

    conn.putrequest("POST", path)
    for k, v in headers.items():
        conn.putheader(k, v)
    conn.endheaders()

    with file_path.open("rb") as f:
        while True:
            chunk = f.read(1024 * 1024)
            if not chunk:
                break
            conn.send(chunk)

    resp = conn.getresponse()
    _check_response(resp, f"upload {file_path.name}")


def _iter_files(dir_path: Path) -> Iterable[Path]:
    for p in sorted(dir_path.iterdir()):
        if p.is_file():
            yield p


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cdn-url", default=os.environ.get("ROBUST_CDN_URL", ""))
    parser.add_argument("--fork", default=os.environ.get("ROBUST_CDN_FORK", "robust"))
    parser.add_argument("--token", default=os.environ.get("PUBLISH_TOKEN", ""))
    parser.add_argument("--version", required=True)
    parser.add_argument("--engine-version", required=True)
    parser.add_argument("--dir", default="release")
    args = parser.parse_args()

    if not args.cdn_url:
        print("error: --cdn-url (or ROBUST_CDN_URL) is required", file=sys.stderr)
        return 2
    if not args.token:
        print("error: --token (or PUBLISH_TOKEN) is required", file=sys.stderr)
        return 2

    scheme, netloc, base_path = _split_base_url(args.cdn_url)

    # Build endpoint paths.
    prefix = f"{base_path}/fork/{args.fork}/publish"
    start_path = f"{prefix}/start"
    file_path = f"{prefix}/file"
    finish_path = f"{prefix}/finish"

    dir_path = Path(args.dir)
    if not dir_path.exists() or not dir_path.is_dir():
        print(f"error: directory does not exist: {dir_path}", file=sys.stderr)
        return 2

    files = list(_iter_files(dir_path))
    if not files:
        print(f"error: no files found in {dir_path}", file=sys.stderr)
        return 2

    print(f"Starting publish to {args.cdn_url} fork={args.fork} version={args.version}")

    conn = _connection(scheme, netloc)
    try:
        _post_json(
            conn,
            start_path,
            args.token,
            {"version": args.version, "engineVersion": args.engine_version},
            "start publish",
        )

        for f in files:
            print(f"Uploading {f.name} ({f.stat().st_size} bytes)")
            _post_file(conn, file_path, args.token, args.version, f)

        _post_json(conn, finish_path, args.token, {"version": args.version}, "finish publish")
    finally:
        conn.close()

    print("SUCCESS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

