"""Verify C0 external oracle facts, opaque canary inputs, manifest hashes, and optional salted reveals."""

import argparse
import hashlib
import json
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[3]
ORACLE_DIR = ROOT / "bench/oracle-facts"
CANARY_DIR = ROOT / "bench/canaries/readers"
BINARY_SUFFIXES = {".zip", ".msapp", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".dll"}


def normalized_bytes(path: Path) -> bytes:
    raw = path.read_bytes()
    if path.suffix.lower() in BINARY_SUFFIXES:
        return raw
    return raw.replace(b"\r\n", b"\n")


def sha256_file(path: Path) -> str:
    return hashlib.sha256(normalized_bytes(path)).hexdigest()


def sha256_target(target: Path) -> str:
    """Deterministic SHA-256 over a single file or a sorted directory tree (LF-normalized for text)."""
    if target.is_file():
        return sha256_file(target)
    if not target.is_dir():
        raise FileNotFoundError(f"missing target: {target}")
    h = hashlib.sha256()
    files = sorted((p for p in target.rglob("*") if p.is_file()), key=lambda p: p.relative_to(target).as_posix())
    if not files:
        raise ValueError(f"empty directory: {target}")
    for p in files:
        rel = p.relative_to(target).as_posix().encode("utf-8")
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
    oracle_dir = root / "bench/oracle-facts"
    canary_dir = root / "bench/canaries/readers"
    facts_dir = oracle_dir / "facts"
    manifest_path = oracle_dir / "manifest.sha256"
    commitments_path = canary_dir / "commitments.json"

    fact_files = sorted(facts_dir.glob("*.json"))
    if not fact_files:
        errors.append("no oracle fact files found under bench/oracle-facts/facts/")
    for f in fact_files:
        data = json.loads(f.read_text(encoding="utf-8"))
        sol_rel = data["solution_path"]
        target = root / sol_rel
        # For YAML solution.yml, hash the whole solution binding root (two levels above solutions/<name>/solution.yml)
        if data["layout"] == "yaml" and target.name.lower() == "solution.yml":
            target = target.parents[2]
        actual = sha256_target(target)
        if actual != data["source_sha256"]:
            errors.append(f"{f.name}: source_sha256 mismatch (expected {data['source_sha256']}, actual {actual})")

    commitments = json.loads(commitments_path.read_text(encoding="utf-8"))
    case_dirs = {p.name for p in (canary_dir / "cases").iterdir() if p.is_dir()}
    committed_ids = {c["canary_id"] for c in commitments["canaries"]}
    if case_dirs != committed_ids:
        errors.append(f"canary directories {sorted(case_dirs)} != committed ids {sorted(committed_ids)}")
    for entry in commitments["canaries"]:
        cid = entry["canary_id"]
        actual = sha256_target(canary_dir / "cases" / cid)
        if actual != entry["input_sha256"]:
            errors.append(f"canary {cid}: input_sha256 mismatch (expected {entry['input_sha256']}, actual {actual})")

    manifest_lines = [line.strip() for line in manifest_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    expected_manifest = {}
    for line in manifest_lines:
        parts = line.split("  ", 1)
        if len(parts) != 2:
            errors.append(f"malformed manifest.sha256 line: {line}")
            continue
        expected_manifest[parts[1]] = parts[0]

    required_paths = (
        ["bench/oracle-facts/schema.json", "bench/canaries/readers/commitments.json"]
        + [f"bench/oracle-facts/facts/{f.name}" for f in fact_files]
    )
    if set(expected_manifest.keys()) != set(required_paths):
        missing = sorted(set(required_paths) - set(expected_manifest.keys()))
        extra = sorted(set(expected_manifest.keys()) - set(required_paths))
        errors.append(f"manifest.sha256 file set mismatch (missing={missing}, extra={extra})")
    for rel, digest in sorted(expected_manifest.items()):
        p = root / rel
        if not p.is_file():
            errors.append(f"manifest entry missing on disk: {rel}")
            continue
        actual = sha256_file(p)
        if actual != digest:
            errors.append(f"manifest digest mismatch for {rel}: expected {digest}, actual {actual}")
    return errors


def check_reveal(root: Path, reveal_path: Path) -> list[str]:
    errors = check_integrity(root)
    commitments = json.loads((root / "bench/canaries/readers/commitments.json").read_text(encoding="utf-8"))
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

    errors = check_reveal(args.root, args.reveal) if args.reveal is not None else check_integrity(args.root)
    if errors:
        sys.exit("\n".join(errors))
    if args.reveal is not None:
        print("C0 oracle facts, canary inputs, manifest, and salted reveal commitments verified.")
    else:
        print("C0 oracle facts, canary inputs, and manifest verified.")


if __name__ == "__main__":
    main()
