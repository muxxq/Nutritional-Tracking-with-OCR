#!/usr/bin/env python3
"""Line coverage per assembly from Coverlet's Cobertura reports, with the NFR-17 gate.

Usage: coverage_report.py <unit-results-dir> [<all-results-dir>]

Reports are searched recursively, so <all-results-dir> can be the parent of the unit and integration directories.

Reports from several test projects are merged line by line (a line counts as covered if any project hit it).
EF Core migrations are generated code and are left out. The gate applies to the unit tests only, as NFR-17 asks:
Domain and Application must each reach MIN_PERCENT. Exits 1 when they don't.
"""
import collections
import glob
import os
import sys
import xml.etree.ElementTree as ET

MIN_PERCENT = 70.0
GATED = ("NutriScan.Domain", "NutriScan.Application")


def coverage(results_dir):
    lines = collections.defaultdict(dict)  # (assembly, file) -> {line number: covered}
    for report in glob.glob(os.path.join(results_dir, "**", "coverage.cobertura.xml"), recursive=True):
        for package in ET.parse(report).getroot().iter("package"):
            for cls in package.iter("class"):
                filename = cls.get("filename")
                if "Migrations" in filename:
                    continue
                file_lines = lines[(package.get("name"), filename)]
                for line in cls.iter("line"):
                    number = int(line.get("number"))
                    file_lines[number] = file_lines.get(number, False) or int(line.get("hits")) > 0

    totals = collections.defaultdict(lambda: [0, 0])
    for (assembly, _), file_lines in lines.items():
        totals[assembly][0] += sum(file_lines.values())
        totals[assembly][1] += len(file_lines)
    return {a: (c, t, 100.0 * c / t) for a, (c, t) in sorted(totals.items()) if t}


def table(title, results):
    rows = [f"### {title}", "", "| Assembly | Lines covered | Coverage |", "|---|---:|---:|"]
    rows += [f"| {a} | {c} / {t} | {p:.1f}% |" for a, (c, t, p) in results.items()]
    return "\n".join(rows) + "\n"


def main():
    unit = coverage(sys.argv[1])
    output = [table("Unit tests (NFR-17 gate: Domain and Application >= 70%)", unit)]
    if len(sys.argv) > 2:
        output.append(table("All tests (unit + integration on PostgreSQL)", coverage(sys.argv[2])))

    failures = [a for a in GATED if unit.get(a, (0, 0, 0.0))[2] < MIN_PERCENT]
    output.append(f"**Gate:** {'FAILED for ' + ', '.join(failures) if failures else 'passed'}\n")

    markdown = "\n".join(output)
    print(markdown)
    if summary := os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(summary, "a", encoding="utf-8") as f:
            f.write(markdown)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
