"""Verify C0 external oracle facts, opaque canary inputs, manifest hashes, and optional salted reveals."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[3]
BINARY_SUFFIXES = {".zip", ".msapp", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".dll"}
SAFE_RELPATH = re.compile(r"^(?![A-Za-z][A-Za-z0-9+.-]*:)(?!.*(?:^|/)[.]{1,2}(?:/|$))[^/\\\r\n\x00]+(?:/[^/\\\r\n\x00]+)*(?!\n)$")


def resolve_confined(root: Path, rel: str, label: str) -> Path:
    if not isinstance(rel, str) or not SAFE_RELPATH.match(rel):
        raise ValueError(f"{label}: invalid relative path: {rel!r}")
    root_real = root.resolve(strict=True)
    candidate = root_real / rel
    # Walk each segment before following anything, refusing symlinks and escapes.
    cur = root_real
    for part in Path(rel).parts:
        cur = cur / part
        if cur.is_symlink():
            raise ValueError(f"{label}: symlink refused: {rel}")
    resolved = candidate.resolve(strict=True)
    if not resolved.is_relative_to(root_real):
        raise ValueError(f"{label}: path escapes root: {rel}")
    return resolved


def normalized_bytes(path: Path) -> bytes:
    raw = path.read_bytes()
    if path.suffix.lower() in BINARY_SUFFIXES:
        return raw
    return raw.replace(b"\r\n", b"\n")


def sha256_file(path: Path) -> str:
    return hashlib.sha256(normalized_bytes(path)).hexdigest()


def sha256_target(target: Path, root: Path | None = None) -> str:
    """Deterministic SHA-256 over a single file or a sorted directory tree (LF-normalized for text)."""
    root_real = root.resolve(strict=True) if root is not None else None
    if target.is_symlink():
        raise ValueError(f"symlink refused: {target}")
    resolved = target.resolve(strict=True)
    if root_real is not None and not resolved.is_relative_to(root_real):
        raise ValueError(f"path escapes root: {target}")
    if resolved.is_file():
        return sha256_file(resolved)
    if not resolved.is_dir():
        raise FileNotFoundError(f"missing target: {target}")
    h = hashlib.sha256()
    files = []
    for p in resolved.rglob("*"):
        if p.is_symlink():
            raise ValueError(f"symlink refused inside directory: {p}")
        if p.is_file():
            if root_real is not None and not p.resolve(strict=True).is_relative_to(root_real):
                raise ValueError(f"file escapes root: {p}")
            files.append(p)
    files.sort(key=lambda p: p.relative_to(resolved).as_posix())
    if not files:
        raise ValueError(f"empty directory: {target}")
    for p in files:
        rel = p.relative_to(resolved).as_posix().encode("utf-8")
        data = normalized_bytes(p)
        h.update(rel + b"\0" + str(len(data)).encode("ascii") + b"\0" + data + b"\0")
    return h.hexdigest()


def canonical_json(obj) -> str:
    return json.dumps(obj, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def commitment_digest(salt: str, payload) -> str:
    body = (salt + "\n" + canonical_json(payload)).encode("utf-8")
    return hashlib.sha256(body).hexdigest()


def check_integrity(root: Path) -> list[str]:
    errors = []
    root_real = root.resolve(strict=True)
    facts_dir = resolve_confined(root_real, "bench/oracle-facts/facts", "facts directory")
    manifest_path = resolve_confined(root_real, "bench/oracle-facts/manifest.sha256", "manifest.sha256")
    commitments_path = resolve_confined(root_real, "bench/canaries/readers/commitments.json", "commitments.json")
    cases_dir = resolve_confined(root_real, "bench/canaries/readers/cases", "canary cases directory")

    fact_files = sorted(facts_dir.glob("*.json"))
    if not fact_files:
        errors.append("no oracle fact files found under bench/oracle-facts/facts/")
    confined_facts = []
    for f in fact_files:
        rel_f = f"bench/oracle-facts/facts/{f.name}"
        try:
            safe_f = resolve_confined(root_real, rel_f, rel_f)
        except (ValueError, OSError) as exc:
            errors.append(str(exc))
            continue
        confined_facts.append(safe_f)
        data = json.loads(safe_f.read_text(encoding="utf-8"))
        sol_rel = data["solution_path"]
        try:
            target = resolve_confined(root_real, sol_rel, f"{safe_f.name} solution_path")
            # For YAML solution.yml (solutions/<name>/solution.yml), hash the solution binding root.
            if data["layout"] == "yaml" and target.name.lower() == "solution.yml":
                if len(Path(sol_rel).parts) < 3:
                    raise ValueError(f"{safe_f.name}: yaml solution_path too shallow: {sol_rel}")
                yaml_root_rel = Path(sol_rel).parents[2].as_posix()
                target = resolve_confined(root_real, yaml_root_rel, f"{safe_f.name} yaml root")
            actual = sha256_target(target, root_real)
        except (ValueError, OSError) as exc:
            errors.append(str(exc))
            continue
        if actual != data["source_sha256"]:
            errors.append(f"{safe_f.name}: source_sha256 mismatch (expected {data['source_sha256']}, actual {actual})")

    commitments = json.loads(commitments_path.read_text(encoding="utf-8"))
    case_dirs = {p.name for p in cases_dir.iterdir() if p.is_dir() and not p.is_symlink()}
    for p in cases_dir.iterdir():
        if p.is_symlink():
            errors.append(f"symlink refused in canary cases directory: {p.name}")
    committed_ids = []
    for entry in commitments["canaries"]:
        cid = entry["canary_id"]
        if cid in committed_ids:
            errors.append(f"duplicate canary_id in commitments.json: {cid}")
        committed_ids.append(cid)
    if case_dirs != set(committed_ids):
        errors.append(f"canary directories {sorted(case_dirs)} != committed ids {sorted(set(committed_ids))}")
    for entry in commitments["canaries"]:
        cid = entry["canary_id"]
        expected_rel = f"bench/canaries/readers/cases/{cid}"
        if entry.get("case_path") != expected_rel:
            errors.append(f"canary {cid}: unexpected case_path {entry.get('case_path')!r}")
            continue
        try:
            target = resolve_confined(root_real, expected_rel, f"canary {cid}")
            actual = sha256_target(target, root_real)
        except (ValueError, OSError) as exc:
            errors.append(str(exc))
            continue
        if actual != entry["input_sha256"]:
            errors.append(f"canary {cid}: input_sha256 mismatch (expected {entry['input_sha256']}, actual {actual})")

    manifest_lines = [line.strip() for line in manifest_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    expected_manifest = {}
    for line in manifest_lines:
        parts = line.split("  ", 1)
        if len(parts) != 2 or not re.fullmatch(r"[0-9a-f]{64}", parts[0]):
            errors.append(f"malformed manifest.sha256 line: {line}")
            continue
        digest, rel = parts
        if rel in expected_manifest:
            errors.append(f"duplicate manifest.sha256 entry: {rel}")
            continue
        expected_manifest[rel] = digest

    required_paths = set(
        ["bench/oracle-facts/schema.json", "bench/canaries/readers/commitments.json"]
        + [f"bench/oracle-facts/facts/{f.name}" for f in fact_files]
    )
    if set(expected_manifest.keys()) != required_paths:
        missing = sorted(required_paths - set(expected_manifest.keys()))
        extra = sorted(set(expected_manifest.keys()) - required_paths)
        errors.append(f"manifest.sha256 file set mismatch (missing={missing}, extra={extra})")

    # Only read paths that belong to the required allowlist and are confined inside root.
    for rel in sorted(required_paths & set(expected_manifest.keys())):
        digest = expected_manifest[rel]
        try:
            p = resolve_confined(root_real, rel, f"manifest {rel}")
        except (ValueError, OSError) as exc:
            errors.append(str(exc))
            continue
        if not p.is_file():
            errors.append(f"manifest entry is not a file: {rel}")
            continue
        actual = sha256_file(p)
        if actual != digest:
            errors.append(f"manifest digest mismatch for {rel}: expected {digest}, actual {actual}")
    return errors


def check_reveal(root: Path, reveal_path: Path) -> list[str]:
    errors = check_integrity(root)
    root_real = root.resolve(strict=True)
    commitments_path = resolve_confined(root_real, "bench/canaries/readers/commitments.json", "commitments.json")
    commitments = json.loads(commitments_path.read_text(encoding="utf-8"))
    reveal = json.loads(reveal_path.read_text(encoding="utf-8"))
    by_id = {c["canary_id"]: c for c in commitments["canaries"]}
    rev_by_id = {c["canary_id"]: c for c in reveal["canaries"]}
    if set(by_id.keys()) != set(rev_by_id.keys()):
        errors.append(f"reveal ids {sorted(rev_by_id)} != committed ids {sorted(by_id)}")
        return errors
    for cid, committed in sorted(by_id.items()):
        entry = rev_by_id[cid]
        actual = commitment_digest(entry["salt"], entry["payload"])
        if actual != committed["label_commitment_sha256"]:
            errors.append(f"canary {cid}: salted commitment mismatch (expected {committed['label_commitment_sha256']}, actual {actual})")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT, help="repository root to verify")
    parser.add_argument("--check", action="store_true", help="verify fact source hashes, canary input hashes, and manifest.sha256")
    parser.add_argument("--reveal", type=Path, default=None, help="verify salted SHA-256 commitments against an external reveal JSON file")
    args = parser.parse_args()

    if not args.check and args.reveal is None:
        parser.error("specify --check or --reveal <path>")

    try:
        errors = check_reveal(args.root, args.reveal) if args.reveal is not None else check_integrity(args.root)
    except (ValueError, OSError) as exc:
        sys.exit(str(exc))
    if errors:
        sys.exit("\n".join(errors))
    if args.reveal is not None:
        print("C0 oracle facts, canary inputs, manifest, and salted reveal commitments verified.")
    else:
        print("C0 oracle facts, canary inputs, and manifest verified.")


if __name__ == "__main__":
    main()
