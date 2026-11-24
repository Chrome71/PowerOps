"""Convert the four frozen role reports to powerops-run/1 aggregates."""

import argparse
from collections import Counter
import difflib
import json
from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[2]
RECORDS = ROOT / "bench/history/records"
REPORTS = ("m38-agent-team", "m55-role-freeze", "m83-zip-linked-reviews", "m96-security-coverage")
CLIENTS = {"Claude": "Claude Code", "Codex": "Codex"}
PASSES = {
    "Plan reviewer": "review-plan",
    "Security reviewer, plan pass": "security-plan",
    "Security plan": "security-plan",
    "Security reviewer, solution pass": "security-solution",
    "Security solution": "security-solution",
    "Security reviewer": "security-solution",
    "Verifier": "verify",
}


def match(pattern, text):
    found = re.search(pattern, text, re.MULTILINE)
    if not found:
        raise ValueError(f"missing or malformed report field matching {pattern!r}")
    return found


def section(report, title):
    return match(r"^## " + re.escape(title) + r"\n([\s\S]*?)(?=^## |\Z)", report)[1]


def table(section_text, header):
    """Select by complete header, accepting differences in Markdown padding."""
    for block in re.findall(r"(?:^\|[^\n]+\|$(?:\n|\Z))+", section_text, re.MULTILINE):
        rows = [[cell.strip() for cell in line[1:-1].split("|")] for line in block.splitlines()]
        if rows[0] == header.split("|"):
            if (len(rows) < 3 or any(len(row) != len(rows[0]) for row in rows)
                    or not all(re.fullmatch(r"-+", s) for s in rows[1])):
                raise ValueError(f"malformed table: {header}")
            return rows[2:]
    raise ValueError(f"missing table: {header}")


def number(cell):
    return int(match(r"^(?:[0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)$", cell)[0].replace(",", ""))


def keyed(rows):
    result = {}
    for key, *values in rows:
        if key in result:
            raise ValueError(f"duplicate row: {key}")
        if not all(values):
            raise ValueError(f"empty field in row: {key}")
        result[key] = values
    return result


def recalls(report, m38, milestone):
    if m38:
        rows = table(section(report, "Results by pass and family"), "Pass|Flaws|Claude|Codex|Pooled")
    else:
        previous = {"M55": "M38 or M39", "M83": "M55", "M96": "M83"}[milestone]
        rows = table(section(report, "Results by family"), "Role|Flaws|Claude|Codex|Pooled|" + previous + ", each client")
    result = {}
    for role, flaws, claude, codex, *_ in rows:
        label = {"Plan flaws": "flaws", "Own, `shown`": "shown", "Own, `not-shown`": "not_shown"}[
            flaws.replace(" (F47 included)", "")]
        for family, cell in zip(CLIENTS, (claude, codex)):
            found, total = map(int, match(r"^(\d+) of (\d+)(?:, [\d.]+%)?$", cell).groups())
            if found > total:
                raise ValueError(f"impossible recall: {cell}")
            q = result.setdefault((family, PASSES[role]), {})
            if label + "_found" in q:
                raise ValueError(f"duplicate recall: {role}, {flaws}")
            q[label + "_found"], q[label + "_total"] = found, total
    return result


def m38_settlements(report):
    clean = section(report, "Clean cases")
    cases = match(r"cases have no keyed flaw: ([^.]+)\.", clean)[1]
    # Only two false Highs are attributed to an individual family and pass.
    security_family = match(r"split on the \d+th, (Claude|Codex)'s S\d+ on solution-\d+\. It was settled as a false High", clean)[1]
    plan_family = match(r"(Claude|Codex)'s R\d+ on plan-\d+ as false", clean)[1]
    verifier_highs, verifier_family = match(r"Verifier: (\d+) High, (Claude|Codex)'s V\d+ .*supported by both scorers", clean).groups()
    result = {}
    for family in CLIENTS:
        for role in ("review-plan", "security-plan", "security-solution", "verify"):
            false = int((family, role) in ((security_family, "security-solution"), (plan_family, "review-plan")))
            case_type = "plan" if role.endswith("plan") else "solution"
            clean_runs = len(re.findall(r"\b" + case_type + r"-\d+\b", cases))
            if not clean_runs:
                raise ValueError(f"missing clean {case_type} cases")
            total = disputed = None
            if role == "verify":
                total = int(verifier_highs) if family == verifier_family else 0
                disputed = 0
            if role.startswith("security-"):
                disputed = false  # The sole split was settled false.
            result[family, role] = dict(highs_false_clean=false, clean_runs=clean_runs,
                                        clean_highs_total=total, clean_highs_disputed=disputed)
    return result


def later_settlements(report, milestone):
    result_section = section(report, "Result")
    rows = keyed(table(result_section, "Keep rule|Limit|Verifier|Security reviewer"))
    clean_row, = (key for key in rows if key.startswith("False Highs across its clean-case runs"))
    clean_runs = len(re.findall(r"solution-\d+", clean_row))
    counts = [match(r"^(\d+), from (\d+) Highs? in (\d+) runs", cell).groups() for cell in rows[clean_row][1:]]
    if not clean_runs or any(int(false) or int(runs) != len(CLIENTS) * clean_runs for false, _, runs in counts):
        raise ValueError("pooled false Highs or clean runs need fresh per-family attribution")

    # M55 and M83 explicitly attribute every clean High to Codex. M96 lists
    # the individual cases by family and role in each settlement.
    claude = Counter()
    scorer = None
    if milestone == "M55":
        match(r"Codex's Verifier .*the one High on a clean case\. Both scorers supported it", section(report, "Wrong states"))
    elif milestone == "M83":
        clean = section(report, "Clean-case Highs")
        match(r"Every High .* came from Codex:", clean)
        scorer = match(r"The scorers split on all \w+: the (\w+) scorer marked them unsupported", clean)[1]
    else:
        clean = section(report, "Clean-case Highs")
        scorer = match(r"each split between the scorers .* the (\w+) scorer all \w+ unsupported", clean)[1]
        per_role = Counter()
        for line in clean.splitlines():
            if line.startswith("- **"):
                findings = match(r"\(Verifier: ([^;]+); Security reviewer: ([^)]+)\)", line)
                for role, refs in zip(("verify", "security-solution"), findings.groups()):
                    for group in refs.split(", "):
                        family, cases = match(r"^(Claude|Codex) (solution-\d+(?: and solution-\d+)*)$", group).groups()
                        n = len(cases.split(" and "))
                        per_role[role] += n
                        if family == "Claude":
                            claude[role] += n
        if [per_role[r] for r in ("verify", "security-solution")] != [int(c[1]) for c in counts]:
            raise ValueError("clean-case settlements disagree with the pooled result")

    result = {}
    for role, (_, total, _) in zip(("verify", "security-solution"), counts):
        for family in CLIENTS:
            n = claude[role] if family == "Claude" else int(total) - claude[role]
            result[family, role] = dict(highs_false_clean=0, clean_runs=clean_runs, clean_highs_total=n,
                                        clean_highs_disputed=n if scorer else 0)
    note = ""
    if scorer:
        verify, security = (int(c[1]) for c in counts)
        note = (f"{verify + security} scorer-split clean-case Highs were settled as unkeyed. "
                f"The {scorer} scorer read {verify} Verifier and {security} Security Highs as unsupported.")
    return result, note


def convert(report, source):
    milestone = match(r"^# (M\d+) report:", report)[1]
    sha = match(r"^Evaluated revision: `([0-9a-f]{40})`", report)[1]
    m38 = milestone == "M38"
    setup = section(report, "Setup")
    clients = keyed(table(setup, "|Claude Code|Codex"))
    model_key = "Model the client reported" if m38 else "Model the report gave"
    scores = recalls(report, m38, milestone)
    cost_section = section(report, "Cost and time")
    replacements = Counter()
    note = extra_cost = ""
    if m38:
        clean = m38_settlements(report)
        replaced = section(report, "Replaced or excluded runs")
        match(r"No case left a denominator", replaced)
        replaced_family = match(r"(Claude|Codex) runs got one replacement each", replaced)[1]
        match(r"All \d+ runs exited 0", replaced)
        for role, case, search in table(replaced, "Pass|Case|Search"):
            replacements[replaced_family, PASSES[role]] += 1
        totals = keyed(table(cost_section, "|Estimate|Actual, Claude|Estimate|Actual, Codex"))
        tokens, elapsed = (totals[k][-1] for k in ("All tokens", "Time, one run at a time"))
        for cell in (tokens, elapsed):
            count = int(match(r"with the (\d+) replaced runs$", cell)[1])
            if count != sum(replacements.values()):
                raise ValueError("replacement count differs from cost table")
        extra_cost = f"Codex reports replacement usage only across all passes. Tokens: {tokens}. Time: {elapsed}."
    else:
        clean, note = later_settlements(report, milestone)
        if milestone == "M96":
            match(r"All \d+ runs exited 0; none hit the limit, none was replaced", setup)
        else:
            match(r"^None\. All \d+ runs exited 0", section(report, "Replaced or excluded runs"))

    rows = table(cost_section, "Client|Pass|Runs|Time|Uncached in|Cached|Output|Cost")
    emitted = set()
    for family, role, runs, wall, uncached, cached, output, price in rows:
        role = PASSES[role]
        key = (family, role)
        if key in emitted:
            raise ValueError(f"duplicate cost row: {key}")
        emitted.add(key)
        i = list(CLIENTS).index(family)
        is_claude = family == "Claude"
        ident = f"{milestone}-{family}-{role}".lower()
        notes = [note] if note else []
        if m38 and not is_claude:
            notes.append(extra_cost)
        reported = clients[model_key][i]
        model = match(r"`([^`]+)` in all \d+", reported)[1] if is_claude else None
        if not is_claude:
            notes.append("Family-wide model reports: " + reported.rstrip(".") + ".")
        window = match(r"\bfrom ([0-9][^.\n]+) UTC", setup)[1] if m38 else clients["Runs"][i]
        end_day = re.search(r"\bto (\d{4}-\d\d-\d\d)\b", window)
        date = end_day[1] if end_day else match(r"^(\d{4}-\d\d-\d\d)\b", window)[1]
        usd = None
        if is_claude:
            usd = float(match(r"^\$(\d+\.\d{2})$", price)[1])
        elif price != "not reported":
            raise ValueError(f"unexpected Codex price: {price}")

        plan = role.endswith("plan")
        q = scores[key]
        kinds = ("flaws",) if plan else ("not_shown", "shown")
        found = sum(q[k + "_found"] for k in kinds)
        total = sum(q[k + "_total"] for k in kinds)
        yield {
            "schemaVersion": 1, "format": "powerops-run/1", "record_id": ident,
            "granularity": "historical_aggregate", "run_count": number(runs), "recorded_at": date,
            "source_ref": source, "commit_sha": sha, "milestone": milestone,
            "task_id": None, "lineage_id": None if plan else "powerops-seed",
            "split": "historical", "arm": "historical",
            "client": {"family": family.lower(), "client_name": CLIENTS[family],
                       "client_version": clients["Client version"][i],
                       "model_requested": match(r"^`([^`]+)`$", clients["Model requested"][i])[1],
                       "model_reported": model, "effort": clients["Effort requested"][i], "pass": role},
            "cost": {"tokens_in": None if is_claude else number(uncached), "cached_read": number(cached),
                     "cached_write": None, "uncached_in_combined": number(uncached) if is_claude else None,
                     "tokens_out": number(output), "usd": usd, "usd_basis": "ratecard" if is_claude else "unknown",
                     "judge_usd": None, "judge_tokens": None, "runner_s": None, "copilot_credits": None},
            "quality": {"checks": None, "score_0_1": found / total if total else None, "mutant_kill_rate": None,
                        "highs_true": None, "critical_fail": None, **q, **clean[key]},
            "time": {"wall_s": number(match(r"^([0-9,]+) s$", wall)[1]), "first_artifact_s": None,
                     "tool_wait_s": None, "approval_wait_s": None, "human_active_min": None,
                     "turns": None, "tool_calls": None, "person_questions": None},
            "reliability": {"end": "completed", "safety_violations": None, "retries": replacements[key],
                            "pass@1": None, "pass^3": None, "replaced_runs": replacements[key],
                            "excluded_runs": 0},
            "notes": " ".join(notes) or None,
        }
    if emitted != clean.keys() or emitted != scores.keys():
        raise ValueError("cost, settlement and recall rows do not cover the same clients and passes")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, default=RECORDS, help="record directory to write or check")
    parser.add_argument("--check", action="store_true", help="compare records without writing")
    args = parser.parse_args()
    expected = {}
    for folder in REPORTS:
        source = f"maintainers/plans/{folder}/report.md"
        try:
            for record in convert((ROOT / source).read_text(encoding="utf-8"), source):
                name = record["record_id"] + ".json"
                if name in expected or not name.startswith(folder.split("-")[0] + "-"):
                    raise ValueError(f"unexpected record id: {name}")
                expected[name] = (json.dumps(record, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
        except (KeyError, IndexError, ValueError, OSError) as error:
            sys.exit(f"{source}: {error}")

    if args.check:
        actual = {p.name for p in args.out.glob("*.json")}
        errors = []
        for name in sorted(actual | expected.keys()):
            if name not in expected:
                errors.append(f"extra record: {name}")
            elif name not in actual:
                errors.append(f"missing record: {name}")
            else:
                # Expected bytes are BOM-free UTF-8. Only checkout line endings may differ.
                data = (args.out / name).read_bytes().replace(b"\r\n", b"\n")
                if data != expected[name]:
                    diff = difflib.unified_diff(data.decode("utf-8", "replace").splitlines(True),
                                                expected[name].decode().splitlines(True), name, "generated")
                    errors.append(f"byte mismatch: {name}\n" + "".join(diff))
        if errors:
            sys.exit("\n".join(errors))
    else:
        args.out.mkdir(parents=True, exist_ok=True)
        for name, data in expected.items():
            (args.out / name).write_bytes(data)
    print(f"{len(expected)} historical records {'verified' if args.check else 'written'}.")


if __name__ == "__main__":
    main()
