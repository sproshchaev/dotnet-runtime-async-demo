"""Checks the invariants from expected.txt against results/actual.txt.

Only facts that do not depend on the OS or the CPU are checked:
inequalities and zeros. Timings (info.*) are printed but never compared.
Exit code 1 on any mismatch.
"""
import operator
import sys

OPS = {"<": operator.lt, "<=": operator.le, "==": operator.eq, ">": operator.gt, ">=": operator.ge}


def load(path):
    values = {}
    with open(path, encoding="utf-8") as f:
        for line in f:
            parts = line.split()
            if len(parts) == 2:
                values[parts[0]] = float(parts[1])
    return values


def operand(token, values):
    try:
        return float(token)
    except ValueError:
        return values[token]


def main(expected_path, actual_path):
    values = load(actual_path)
    failed = 0
    with open(expected_path, encoding="utf-8") as f:
        for raw in f:
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            left, op, right = line.split()
            try:
                ok = OPS[op](operand(left, values), operand(right, values))
            except KeyError as missing:
                ok = False
                print(f"FAIL {line}  (no value {missing})")
                failed += 1
                continue
            print(f"{'OK  ' if ok else 'FAIL'} {line}")
            failed += 0 if ok else 1
    print(f"\n{'all invariants hold' if not failed else f'{failed} invariant(s) failed'}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1], sys.argv[2]))
