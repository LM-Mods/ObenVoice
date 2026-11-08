"""Generate the bundled-runtime inventory and collect packaged licence files.

Original utility for Obenseuer Dialogue TTS. It uses only Python packaging
metadata already present in the release and does not download anything.
"""

from __future__ import annotations

import importlib.metadata as metadata
import argparse
import re
import shutil
import sys
from collections import deque
from pathlib import Path

DIRECT = {"kokoro", "numpy", "sounddevice", "soundfile"}
Requirement = None
canonicalize_name = None


def licence_name(dist: metadata.Distribution) -> str:
    value = dist.metadata.get("License-Expression") or dist.metadata.get("License")
    if value and value.strip() and value.strip().upper() != "UNKNOWN" and len(value) <= 160:
        return " ".join(value.split())
    classifiers = dist.metadata.get_all("Classifier", [])
    licences = [item.split(" :: ")[-1] for item in classifiers if item.startswith("License ::")]
    if licences:
        return ", ".join(licences)
    if value and value.strip() and value.strip().upper() != "UNKNOWN":
        return "See bundled package licence text"
    return "Not declared in package metadata"


def dependency_closure(distributions):
    by_name = {canonicalize_name(d.metadata["Name"]): d for d in distributions}
    required = set(DIRECT)
    queue = deque((name, {"en"} if name in {"kokoro", "misaki"} else set()) for name in DIRECT)
    while queue:
        name, extras = queue.popleft()
        dist = by_name.get(canonicalize_name(name))
        if dist is None:
            continue
        for raw in dist.requires or []:
            try:
                requirement = Requirement(raw)
            except Exception:
                continue
            contexts = extras or {""}
            if requirement.marker and not any(
                requirement.marker.evaluate({"extra": extra}) for extra in contexts
            ):
                continue
            child = canonicalize_name(requirement.name)
            child_extras = set(requirement.extras)
            if child not in required:
                required.add(child)
                queue.append((child, child_extras))
            elif child_extras:
                queue.append((child, child_extras))
    return required


def copy_licences(dist: metadata.Distribution, license_root: Path):
    name = canonicalize_name(dist.metadata["Name"])
    destination = license_root / f"{name}-{dist.version}"
    for relative in dist.files or []:
        leaf = relative.name.lower()
        if not re.match(r"^(licen[cs]e|copying|notice)(\.|$)", leaf):
            continue
        source = Path(dist.locate_file(relative))
        if not source.is_file():
            continue
        safe_relative = Path(*[part for part in relative.parts if not part.endswith(".dist-info")])
        target = destination / safe_relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)


def main():
    global Requirement, canonicalize_name
    parser = argparse.ArgumentParser(
        description="Inventory the Python packages already bundled in an assembled release."
    )
    parser.add_argument(
        "release_root",
        type=Path,
        help="Folder containing ObenseuerTTS/runtime (for example an assembled core release)",
    )
    args = parser.parse_args()
    root = args.release_root.resolve()
    site_packages = root / "ObenseuerTTS" / "runtime" / "Lib" / "site-packages"
    output = root / "RUNTIME_DEPENDENCIES.md"
    license_root = root / "licenses" / "python-packages"
    if not site_packages.is_dir():
        parser.error(f"bundled site-packages directory not found: {site_packages}")
    if str(site_packages) not in sys.path:
        sys.path.insert(0, str(site_packages))
    try:
        from packaging.requirements import Requirement as PackagingRequirement
        from packaging.utils import canonicalize_name as packaging_canonicalize_name
    except ImportError as exc:
        parser.error(f"the staged runtime does not contain the required packaging metadata helper: {exc}")
    Requirement = PackagingRequirement
    canonicalize_name = packaging_canonicalize_name
    distributions = sorted(
        metadata.distributions(path=[str(site_packages)]),
        key=lambda item: canonicalize_name(item.metadata["Name"]),
    )
    required = dependency_closure(distributions)
    rows = []
    for dist in distributions:
        name = canonicalize_name(dist.metadata["Name"])
        if name in DIRECT:
            need = "Direct runtime requirement"
        elif name in required:
            need = "Transitive runtime requirement"
        else:
            need = "Retained; not proven safe to remove"
        rows.append((dist.metadata["Name"], dist.version, licence_name(dist), need))
        copy_licences(dist, license_root)
    python_version = sys.version.split()[0]
    lines = [
        "# Bundled Python runtime inventory",
        "",
        f"Generated from the release itself. Python version: **{python_version}**.",
        "",
        "`Retained; not proven safe to remove` means the package is not in the declared dependency closure, "
        "but was conservatively left in place because this release did not prove that deleting it is safe.",
        "",
        "| Package | Version | Licence metadata | Project need |",
        "|---|---:|---|---|",
    ]
    for name, version, licence, need in rows:
        lines.append(f"| {name} | {version} | {licence.replace('|', '/')} | {need} |")
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {output} with {len(rows)} packages")


if __name__ == "__main__":
    main()
