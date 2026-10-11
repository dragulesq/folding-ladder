#!/usr/bin/env python3
"""Offline checks on the B3 FeatureScript, to catch mistakes before spending Onshape API calls.

Checks brackets balance in every file, that B3_AllInOne.fs matches what
build_all_in_one.py makes, and, given a copy of the FeatureScript standard
library at the version the files import, that every function, constant and
enum value the files use is defined somewhere.

Usage: python3 tools/fs_lint.py [STD_DIR]

STD_DIR is a checkout of github.com/javawizard/onshape-std-library-mirror
at the commit for the version in the files' import line ("Version 2641.0").
"""
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "onshape"
KEYWORDS = {"if", "for", "while", "function", "return", "returns", "switch", "catch", "try", "import", "annotation",
            "precondition", "is", "in", "else", "var", "const", "export", "throw", "break", "continue",
            "number", "string", "array", "map", "boolean", "undefined", "true", "false",
            "FeatureScript", "path", "version"}


def strip(text):
    """Blank out comments and string literals, keeping line numbers."""
    out, i, n = [], 0, len(text)
    while i < n:
        c = text[i]
        if text.startswith("//", i):
            j = text.find("\n", i)
            j = n if j < 0 else j
            i = j
        elif text.startswith("/*", i):
            j = text.find("*/", i) + 2
            out.append("\n" * text.count("\n", i, j))
            i = j
        elif c == '"':
            j = i + 1
            while text[j] != '"':
                j += 2 if text[j] == "\\" else 1
            out.append('""')
            i = j + 1
        else:
            out.append(c)
            i += 1
    return "".join(out)


def check_brackets(name, code):
    pairs = {")": "(", "]": "[", "}": "{"}
    stack, bad = [], []
    for ln, line in enumerate(code.splitlines(), 1):
        for c in line:
            if c in "([{":
                stack.append((c, ln))
            elif c in pairs:
                if not stack or stack[-1][0] != pairs[c]:
                    bad.append(f"{name}:{ln}: unmatched '{c}'")
                else:
                    stack.pop()
    bad += [f"{name}:{ln}: unclosed '{c}'" for c, ln in stack]
    return bad


def std_names(std):
    names, enums = set(), {}
    for f in std.glob("*.fs"):
        code = strip(f.read_text(errors="replace"))
        names.update(re.findall(r"export\s+(?:function|const|predicate|type|enum|operator)\s+(\w+)", code))
        code = re.sub(r"annotation\s*\{[^{}]*\}", "", code)
        for en, body in re.findall(r"export\s+enum\s+(\w+)\s*\{(.*?)\}", code, re.S):
            enums[en] = set(re.findall(r"\b([A-Z_][A-Z0-9_]*)\b", body))
    return names, enums


def main():
    files = sorted(SRC.glob("*.fs"))
    problems = []
    codes = {f.name: strip(f.read_text()) for f in files}
    for name, code in codes.items():
        problems += check_brackets(name, code)

    before = (SRC / "B3_AllInOne.fs").read_text()
    subprocess.run([sys.executable, str(ROOT / "tools" / "build_all_in_one.py")], check=True, capture_output=True)
    if (SRC / "B3_AllInOne.fs").read_text() != before:
        problems.append("B3_AllInOne.fs was out of date; rebuilt it, commit the change")

    if len(sys.argv) > 1:
        std, enums = std_names(Path(sys.argv[1]))
        own = set()
        for code in codes.values():
            own.update(re.findall(r"(?:function|const|var)\s+(\w+)", code))
            own.update(re.findall(r"(\w+)\s+is\s+\w+", code))
            own.update(re.findall(r"for\s*\(\s*var\s+(\w+)", code))
        known = std | own | KEYWORDS
        for name, code in codes.items():
            for ln, line in enumerate(code.splitlines(), 1):
                for ident in re.findall(r"(?<![\w.\"])([A-Za-z_]\w*)\b", line):
                    if ident not in known and not ident[0].isdigit():
                        problems.append(f"{name}:{ln}: '{ident}' is not defined in these files or the std library")
                for en, member in re.findall(r"\b([A-Z]\w*)\.([A-Z_][A-Z0-9_]*)\b", line):
                    if en in enums and member not in enums[en]:
                        problems.append(f"{name}:{ln}: {en}.{member} is not a value of enum {en}")
    for p in sorted(set(problems)):
        print(p)
    print("lint ok" if not problems else f"{len(set(problems))} problem(s)")
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
