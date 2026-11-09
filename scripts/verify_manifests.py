#!/usr/bin/env python3
"""Validate docs/idiomatic-*.tsv against the committed Interop sources.

One row per type. Columns are tab separated:

    <interop type full name>  <forms>  <members kept as methods, with reason>

The manifest names types under the singular `Hosihikari.Binding` root, because that
is the namespace the layer will publish. Until the Interop rename lands (projection
ADR-0022) the checked-in Interop still uses the plural root, so the rule below maps
the prefix before looking the type up.

A row fails when the type does not exist, when a form is not one of the five known
ones, or when a kept-method column is empty.
"""

from __future__ import annotations

import sys
from pathlib import Path

FORMS = {"属性", "方法", "事件", "运算符", "索引器", "构造函数"}

INTEROP_ROOTS = {
    "endstone": ("Binding.Endstone", "Hosihikari.Bindings.Endstone.Interop"),
    "levilamina": ("Binding.LeviLamina", "Hosihikari.Bindings.LeviLamina.Interop"),
    "bedrock": ("Binding.Bedrock", "Hosihikari.Bindings.Bedrock.Interop"),
}


def to_interop_name(name: str, surface: str) -> str:
    """Rewrite a manifest name from the singular root to the checked-in plural Interop root."""
    surface_name = surface.capitalize() if surface != "levilamina" else "LeviLamina"
    singular = f"Hosihikari.Binding.{surface_name}."
    plural = f"Hosihikari.Bindings.{surface_name}.Interop."
    return name.replace(singular, plural, 1) if name.startswith(singular) else name


def declared_types(generated: Path) -> set[str]:
    """Collect the fully qualified names of the public types under one generated tree."""
    found: set[str] = set()
    for path in generated.rglob("*.g.cs"):
        namespace = ""
        for line in path.read_text(encoding="utf-8").splitlines():
            if line.startswith("namespace "):
                namespace = line[len("namespace "):].strip().rstrip(";")
            elif line.startswith(("public ", "internal ")):
                head = line.split("(", 1)[0].split(":", 1)[0]
                for keyword in (" class ", " struct ", " enum ", " interface "):
                    if keyword in head:
                        name = head.split(keyword, 1)[1].split()[0].strip()
                        if name and namespace:
                            found.add(f"{namespace}.{name}")
                        break
    return found


def check_surface(root: Path, surface: str) -> list[str]:
    repo, plural_root = INTEROP_ROOTS[surface]
    manifest = root / "docs" / f"idiomatic-{surface}.tsv"
    if not manifest.exists():
        return [f"{manifest.name}: missing"]

    generated = root.parent / repo / "src" / f"{plural_root}" / "Generated"
    if not generated.is_dir():
        return [f"{manifest.name}: interop tree not found at {generated}"]

    known = declared_types(generated)
    problems: list[str] = []
    for number, row in enumerate(manifest.read_text(encoding="utf-8").splitlines(), start=1):
        if not row.strip():
            continue
        cells = row.split("\t")
        if number == 1 and cells[0] == "类型全名":
            continue
        if len(cells) != 3:
            problems.append(f"{manifest.name}:{number}: expected 3 columns, got {len(cells)}")
            continue

        name, forms, kept = (cell.strip() for cell in cells)
        mapped = to_interop_name(name, surface)
        if mapped not in known:
            problems.append(f"{manifest.name}:{number}: no such Interop type: {mapped}")

        unknown = [form for form in (part.strip() for part in forms.split(";")) if form and form not in FORMS]
        if unknown:
            problems.append(f"{manifest.name}:{number}: unknown forms {unknown}")

        if not kept:
            problems.append(f"{manifest.name}:{number}: empty kept-method column")

    return problems


def main(argv: list[str]) -> int:
    root = Path(argv[1]).resolve() if len(argv) > 1 else Path(__file__).resolve().parent.parent
    problems: list[str] = []
    for surface in INTEROP_ROOTS:
        problems.extend(check_surface(root, surface))

    for problem in problems:
        print(problem)
    print(f"{len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
