#!/usr/bin/env python3
"""Create a deterministic ZIP from a staging directory."""
from __future__ import annotations
import argparse
from pathlib import Path
import stat
import zipfile

FIXED_TIME = (1980, 1, 1, 0, 0, 0)

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    args = parser.parse_args()
    source = args.source.resolve()
    destination = args.destination.resolve()
    if not source.is_dir():
        raise SystemExit(f"Source directory does not exist: {source}")
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists(): destination.unlink()
    files = sorted(path for path in source.rglob('*') if path.is_file())
    with zipfile.ZipFile(destination, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in files:
            relative = path.relative_to(source).as_posix()
            info = zipfile.ZipInfo(relative, FIXED_TIME)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = (stat.S_IFREG | 0o644) << 16
            info.create_system = 3
            archive.writestr(info, path.read_bytes(), compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)
    print(destination)
    return 0

if __name__ == '__main__': raise SystemExit(main())
