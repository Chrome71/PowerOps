# Defective-reader canaries (`C0`)

This folder holds four opaque synthetic solutions (`cases/canary-a/` through `cases/canary-d/`) and their hash-committed expectations (`commitments.json`), designed to detect defective or incomplete readers without leaking defect labels inside the fixtures or requiring manual owner blinding labor.

## Opaque hash-commitment protocol

1. **Opaque inputs**: Every case under `cases/canary-{a,b,c,d}/` uses neutral publisher (`canarypub`, prefix `cnry`), neutral solution names (`CanarySolutionA`..`CanarySolutionD`), fake GUIDs (`00000000-0000-0000-0000-000000c000xx`), and fake hosts (`*.contoso.example`). No filename, XML comment, or JSON string inside `cases/` reveals which defect class a case tests.
2. **Committed hashes (`commitments.json`)**:
   - `input_sha256`: deterministic SHA-256 over the sorted relative paths and bytes of all files in `cases/<id>/` (text files LF-normalized for Windows checkout portability; `.msapp` zips hashed as exact raw bytes).
   - `label_commitment_sha256`: `sha256(salt + "\n" + canonical_json)` over each canary's defect class, target defective-reader behavior, and exact expected extracted facts. The salts and canonical reveal payloads live outside git during blind `C1` verification and are revealed only after the independent verifier runs `C1` blind against `cases/canary-{a,b,c,d}/`.
3. **Verification**:
   - Always-on integrity check (zero reveal needed): `python3 bench/canaries/readers/verify_commitments.py --check`
   - Post-verification cryptographic reveal check: `python3 bench/canaries/readers/verify_commitments.py --reveal <reveal.json>`
