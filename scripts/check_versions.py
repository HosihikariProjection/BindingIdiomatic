#!/usr/bin/env python3
"""Check that every Idiomatic package matches the Interop version it pairs with.

ADR-0027 ties a surface package to its Interop counterpart: same upstream version and the same
binding revision. The Interop `<Version>` is the source of truth; a mismatch means the pair was
released apart, which is exactly what the rule exists to prevent.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

VERSION = re.compile(r"<Version>([^<]+)</Version>")

PAIRS = {
    "endstone": ("Binding.Endstone", "Hosihikari.Binding.Endstone", "Hosihikari.Bindings.Endstone.Interop"),
    "levilamina": ("Binding.LeviLamina", "Hosihikari.Binding.LeviLamina", "Hosihikari.Bindings.LeviLamina.Interop"),
}


def version_of(project: Path) -> str | None:
    if not project.exists():
        return None

    found = VERSION.search(project.read_text(encoding="utf-8"))
    return found.group(1).strip() if found else None


def main(argv: list[str]) -> int:
    root = Path(argv[1]).resolve() if len(argv) > 1 else Path(__file__).resolve().parent.parent
    siblings = Path(argv[2]).resolve() if len(argv) > 2 else root.parent

    problems: list[str] = []
    for surface, (repo, own_name, interop_name) in PAIRS.items():
        own = version_of(root / "src" / own_name / f"{own_name}.csproj")
        interop = version_of(
            siblings / repo / "src" / interop_name / f"{interop_name}.csproj"
        )

        if own is None:
            problems.append(f"{surface}: {own_name} has no <Version>")
        elif interop is None:
            problems.append(f"{surface}: {interop_name} has no <Version>")
        elif own != interop:
            problems.append(f"{surface}: {own_name} is {own}, but {interop_name} is {interop}")

    for problem in problems:
        print(problem)
    print(f"{len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
