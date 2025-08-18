#!/usr/bin/env python3
"""
Golden Boundary Aggregation Audit

Purpose:
  Assist Phase 7 (Boundary Aggregation) by detecting groups of Golden baseline
  manifest entries that are likely aggregatable (repetitive scenarios differing only by
  numeric boundary or repetition count).

Heuristics Implemented:
  * Group by (RuleCode, ExpectedAction) and list ShortNames.
  * Flag groups with count >= 3.
  * Additional pattern hint: names sharing a common alphabetic prefix (e.g. EmojiFlood*, EmojiBoundary*)
    are clustered and shown with a suggested merged label.

Output:
  Human-readable summary to stdout. Use --json for machine-readable JSON summary.

Usage:
  python scripts/golden_boundary_audit.py [--manifest path] [--json]

Exit codes:
  0 success
  2 manifest not found / invalid

This tool is read-only; it does not modify any repository files.
"""
from __future__ import annotations
import argparse
import json
import os
import re
import sys
from collections import defaultdict, Counter
from dataclasses import dataclass
from typing import List, Dict, Any

DEFAULT_MANIFEST = os.path.join("ClubDoorman.Baseline", "golden", "manifest.json")

PREFIX_RE = re.compile(r"^([A-Za-z]+)")

@dataclass
class Entry:
    id: int
    short: str
    rule: str
    action: str | None


def load_manifest(path: str) -> List[Entry]:
    if not os.path.isfile(path):
        print(f"[ERROR] Manifest not found: {path}", file=sys.stderr)
        sys.exit(2)
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    entries_raw = data.get("Entries", [])
    out: List[Entry] = []
    for e in entries_raw:
        out.append(Entry(
            id = e.get("Id"),
            short = e.get("ShortName"),
            rule = e.get("RuleCode"),
            action = e.get("ExpectedAction")
        ))
    return out


def common_prefix(names: List[str]) -> str | None:
    if not names:
        return None
    # Take alphabetic leading chunk of each name
    heads = []
    for n in names:
        m = PREFIX_RE.match(n)
        if not m:
            return None
        heads.append(m.group(1))
    if len(set(heads)) == 1:
        return heads[0]
    return None


def analyze(entries: List[Entry]) -> Dict[str, Any]:
    groups: Dict[tuple, List[Entry]] = defaultdict(list)
    for e in entries:
        groups[(e.rule, e.action)] .append(e)

    result = []
    for (rule, action), lst in sorted(groups.items(), key=lambda kv: (kv[0][0] or '', kv[0][1] or '')):
        names = [e.short for e in lst]
        pref = common_prefix(names)
        flagged = len(lst) >= 3 and rule not in {"Links", "Pass"}  # skip very broad or neutral groups for now
        # Sub-cluster by numeric suffix patterns (e.g. Repeat1, Repeat2)
        numeric_counts = Counter()
        for n in names:
            m = re.search(r"(\d+)$", n)
            if m:
                numeric_counts[m.group(1)] += 1
        has_numeric_series = len(numeric_counts) >= 2
        result.append({
            "rule": rule,
            "expectedAction": action,
            "count": len(lst),
            "names": names,
            "commonAlphaPrefix": pref,
            "hasNumericSeries": has_numeric_series,
            "flaggedCandidate": flagged and (pref is not None or has_numeric_series)
        })
    return {"groups": result}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--manifest", default=DEFAULT_MANIFEST, help="Path to golden manifest.json")
    ap.add_argument("--json", action="store_true", help="Emit JSON instead of text")
    args = ap.parse_args()

    entries = load_manifest(args.manifest)
    report = analyze(entries)

    if args.json:
        json.dump(report, sys.stdout, indent=2, ensure_ascii=False)
        return

    print("Golden Boundary Aggregation Audit")
    print(f"Manifest: {args.manifest}")
    print("Total entries:", len(entries))
    print()
    for g in report["groups"]:
        mark = "*" if g["flaggedCandidate"] else " "
        print(f"[{mark}] Rule={g['rule']:<15} Action={g['expectedAction'] or '-':<8} Count={g['count']:<2} Prefix={g['commonAlphaPrefix'] or '-':<10} NumSeries={'Y' if g['hasNumericSeries'] else 'N'}")
        if g["flaggedCandidate"]:
            print("     Names:", ", ".join(g["names"]))
    print()
    print("Legend: '*' = candidate for aggregation (>=3 similar entries with prefix or numeric series).")

if __name__ == "__main__":
    main()
