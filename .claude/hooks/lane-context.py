"""SessionStart hook: tell Claude which parallel lane this folder is and which Unity editor to use.

Several sessions run at once, one per git worktree (docs/parallel-lanes.md), and every Unity editor
connects to the same MCP server. Without this, a session that skips its role skill's step 0 has to
guess which editor is its own. The lane table in docs/parallel-lanes.md is the one source of truth:
this script reads it rather than keeping its own copy.

Prints JSON: additionalContext goes into Claude's context, systemMessage is shown to the user.
Never blocks a session start: any problem becomes a message, and the exit code stays 0.
"""
import json
import os
import subprocess
import sys

LANES_DOC = os.path.join("docs", "parallel-lanes.md")


def current_branch(project_dir: str) -> str:
    result = subprocess.run(
        ["git", "branch", "--show-current"],
        cwd=project_dir, capture_output=True, text=True, timeout=10,
    )
    return result.stdout.strip()


def read_lanes(project_dir: str) -> list:
    """Rows of the lane table: lane, folder, branch, editor. Header and divider rows are skipped."""
    lanes = []
    with open(os.path.join(project_dir, LANES_DOC), encoding="utf-8") as handle:
        for line in handle:
            cells = [cell.strip().strip("`") for cell in line.strip().strip("|").split("|")]
            if len(cells) == 5 and cells[2].startswith("lane/"):
                lanes.append({"lane": cells[0], "folder": cells[1], "branch": cells[2], "editor": cells[3]})
    return lanes


def describe(branch: str, lanes: list) -> tuple:
    """(context for Claude, one-line message for the user)."""
    mine = next((lane for lane in lanes if lane["branch"] == branch), None)
    if mine is None:
        return (
            f"This folder is on branch `{branch}`, which is not a parallel lane (docs/parallel-lanes.md). "
            "Use the one-folder workflow: work on the current branch and skip the lane steps.",
            f"Not a lane (branch {branch}).",
        )

    others = [lane["editor"] for lane in lanes if lane is not mine and not lane["editor"].startswith("none")]
    if mine["editor"].startswith("none"):
        unity = "This lane has no Unity editor: don't use Unity MCP tools at all."
        editor_note = "no Unity"
    else:
        unity = (
            f"Your Unity editor is the MCP instance named `{mine['editor']}`. Before your first Unity tool call, "
            "read `mcpforunity://instances` and call `set_active_instance` with that instance's Name@hash; "
            "do it again whenever a Unity call says several instances are connected (an MCP reconnect forgets it). "
            f"Never run tools in another lane's editor ({', '.join(f'`{name}`' for name in others)})."
        )
        editor_note = f"Unity editor: {mine['editor']}"

    context = (
        f"Parallel lane: **{mine['lane']}** (branch `{mine['branch']}`, folder {mine['folder']}). {unity} "
        "Start and finish tasks with the steps in docs/parallel-lanes.md."
    )
    return context, f"Lane: {mine['lane']} · {editor_note}"


def main() -> int:
    project_dir = os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd()
    try:
        context, message = describe(current_branch(project_dir), read_lanes(project_dir))
    except (OSError, subprocess.SubprocessError) as error:
        # Fail loudly but don't block the session: Claude is told the lane is unknown.
        context = f"The lane-context hook failed ({error}). Work out the lane from docs/parallel-lanes.md by hand."
        message = "Lane hook failed; see docs/parallel-lanes.md."

    print(json.dumps({
        "systemMessage": message,
        "hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": context},
    }))
    return 0


if __name__ == "__main__":
    sys.exit(main())
