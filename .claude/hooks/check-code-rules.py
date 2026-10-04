"""PostToolUse hook: after Claude edits a .cs file, flag hard-rule breaks from CLAUDE.md.

Exit code 2 sends the message back to Claude so it fixes the file; exit 0 stays silent.
Only checks rules a text search can decide without false alarms:
  - legacy UnityEngine.Input (the project uses the New Input System)
  - Time.* clocks inside Assets/Scripts/Core (gameplay never reads real time; the TickEngine owns time)
"""
import json
import re
import sys

LEGACY_INPUT = re.compile(
    r"\bInput\.(GetKey\w*|GetAxis\w*|GetButton\w*|GetMouseButton\w*|mousePosition|touches?|GetTouch|anyKey\w*|mouseScrollDelta)\b"
)
REAL_TIME = re.compile(r"\bTime\.(deltaTime|unscaledDeltaTime|fixedDeltaTime|time|unscaledTime)\b")


def main() -> int:
    try:
        payload = json.load(sys.stdin)
        path = payload.get("tool_input", {}).get("file_path", "")
    except (ValueError, AttributeError):
        return 0  # not a payload we understand; never block the edit over the hook itself

    normalised = path.replace("\\", "/")
    if not normalised.endswith(".cs"):
        return 0

    try:
        with open(path, encoding="utf-8") as handle:
            lines = handle.read().splitlines()
    except OSError:
        return 0  # file gone or unreadable (e.g. a delete); nothing to check

    in_core = "/Assets/Scripts/Core/" in normalised
    problems = []
    for number, line in enumerate(lines, start=1):
        code = line.split("//", 1)[0]  # ignore commented-out mentions
        if LEGACY_INPUT.search(code):
            problems.append(f"line {number}: legacy UnityEngine.Input. Use the New Input System.")
        if in_core and REAL_TIME.search(code):
            problems.append(f"line {number}: real-time clock in Core. Gameplay must run on TickEngine ticks, never Time.*.")

    if problems:
        print(f"CLAUDE.md hard-rule break in {path}:\n" + "\n".join(problems), file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
