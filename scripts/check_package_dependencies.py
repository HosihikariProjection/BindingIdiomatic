#!/usr/bin/env python3
"""Pin packed surface packages to their Interop pair exactly.

ADR-0027 ties a surface package to its Interop counterpart: same version, exact range. A nested
project reference carries no version into the nuspec, so the packed dependency comes out as a bare
minimum, which would let a consumer resolve a later Interop than the package was built against.
This rewrites that one line in place and fails if any pair is still not exact afterwards.

Run after `dotnet pack`, before pushing.
"""

from __future__ import annotations

import re
import sys
import zipfile
from pathlib import Path

# ABI types cross the Idiomatic boundary unchanged, so the base is pinned to the ABI the
# surfaces were built against for the same reason the pairs are pinned to each other.
PAIRS = {
    "Hosihikari.Binding": "Hosihikari.Bindings.Abi",
    "Hosihikari.Binding.Bedrock": "Hosihikari.Bindings.Bedrock.Interop",
    "Hosihikari.Binding.Endstone": "Hosihikari.Bindings.Endstone.Interop",
    "Hosihikari.Binding.LeviLamina": "Hosihikari.Bindings.LeviLamina.Interop",
}

DEPENDENCY = re.compile(r'<dependency id="([^"]+)" version="([^"]+)"')


def main(argv: list[str]) -> int:
    package_root = Path(argv[1]).resolve() if len(argv) > 1 else Path("artifacts/package")
    packages = sorted(package_root.glob("*.nupkg"))
    if not packages:
        print(f"no packages under {package_root}")
        return 1

    problems: list[str] = []
    rewritten = 0
    for package in packages:
        with zipfile.ZipFile(package) as archive:
            entries = {name: archive.read(name) for name in archive.namelist()}
        nuspec_name = next(name for name in entries if name.endswith(".nuspec"))
        nuspec = entries[nuspec_name].decode("utf-8")

        pair = PAIRS.get(nuspec_id(nuspec))
        if pair is None:
            continue

        fixed = nuspec
        for dependency, version in DEPENDENCY.findall(nuspec):
            if dependency == pair and not version.startswith("["):
                fixed = fixed.replace(
                    f'<dependency id="{dependency}" version="{version}"',
                    f'<dependency id="{dependency}" version="[{version}]"',
                )
                rewritten += 1

        if fixed != nuspec:
            entries[nuspec_name] = fixed.encode("utf-8")
            with zipfile.ZipFile(package, "w", zipfile.ZIP_DEFLATED) as archive:
                for name, data in entries.items():
                    archive.writestr(name, data)

        for dependency, version in DEPENDENCY.findall(fixed):
            if dependency == pair and not version.startswith("["):
                problems.append(f"{package.name}: {pair} still not exact ('{version}')")

    for problem in problems:
        print(problem)
    print(f"{rewritten} pinned, {len(problems)} problem(s)")
    return 1 if problems else 0


def nuspec_id(nuspec: str) -> str:
    found = re.search(r"<id>([^<]+)</id>", nuspec)
    return found.group(1).strip() if found else ""


if __name__ == "__main__":
    sys.exit(main(sys.argv))
