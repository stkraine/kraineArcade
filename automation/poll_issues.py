#!/usr/bin/env python3
"""
Poll GitHub issues for stkraine/kraineArcade and log new (unseen) issues.

- Picks up all open issues.
- Tracks "seen" issues by adding the `agent:seen` label.
- MVP action: logs new issues to stdout (and optionally a log file).

Requirements:
- GitHub CLI (`gh`) installed and authenticated, OR GH_TOKEN/GITHUB_PAT set.
- Run from cron or manually.

Cron example (every 1 minute):
  * * * * * cd /home/steven/Projects/kraineArcade && /usr/bin/env GH_TOKEN="$GITHUB_PAT" /usr/bin/python3 automation/poll_issues.py >> /tmp/kraineArcade-poll.log 2>&1

Adjust PATH/GH_TOKEN as needed for your cron environment.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

REPO = os.environ.get("GITHUB_REPO", "stkraine/kraineArcade")
SEEN_LABEL = os.environ.get("AGENT_SEEN_LABEL", "agent:seen")
LOG_FILE = os.environ.get("POLL_LOG_FILE")  # optional


def run(cmd: list[str], check: bool = True) -> subprocess.CompletedProcess[str]:
    env = os.environ.copy()
    # Ensure token is available to gh if provided under common names
    if not env.get("GH_TOKEN") and env.get("GITHUB_PAT"):
        env["GH_TOKEN"] = env["GITHUB_PAT"]
    return subprocess.run(cmd, text=True, capture_output=True, env=env, check=check)


def log(msg: str) -> None:
    ts = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    line = f"[{ts}] {msg}"
    print(line, flush=True)
    if LOG_FILE:
        try:
            Path(LOG_FILE).parent.mkdir(parents=True, exist_ok=True)
            with open(LOG_FILE, "a", encoding="utf-8") as f:
                f.write(line + "\n")
        except OSError as e:
            print(f"[{ts}] WARN: failed to write log file: {e}", flush=True)


def ensure_label() -> None:
    # Create label if it doesn't exist; ignore errors if it already exists.
    result = run(
        [
            "gh",
            "label",
            "create",
            SEEN_LABEL,
            "--repo",
            REPO,
            "--color",
            "0E8A16",
            "--description",
            "Issue has been seen by the polling agent",
        ],
        check=False,
    )
    if result.returncode == 0:
        log(f"Created label '{SEEN_LABEL}'")
    # If it failed, it may already exist; that's fine.


def fetch_open_issues() -> list[dict]:
    # Get open issues; gh issue list excludes PRs.
    result = run(
        [
            "gh",
            "issue",
            "list",
            "--repo",
            REPO,
            "--state",
            "open",
            "--json",
            "number,title,url,labels",
            "--limit",
            "100",
        ]
    )
    return json.loads(result.stdout or "[]")


def is_seen(issue: dict) -> bool:
    labels = issue.get("labels") or []
    return any((lbl.get("name") == SEEN_LABEL) for lbl in labels)


def mark_seen(number: int) -> None:
    run(
        [
            "gh",
            "issue",
            "edit",
            str(number),
            "--repo",
            REPO,
            "--add-label",
            SEEN_LABEL,
        ]
    )


def main() -> int:
    try:
        ensure_label()
        issues = fetch_open_issues()
    except FileNotFoundError:
        log("ERROR: `gh` CLI not found. Install GitHub CLI and ensure it's on PATH.")
        return 1
    except subprocess.CalledProcessError as e:
        log(f"ERROR: gh command failed: {e.stderr.strip() or e.stdout.strip() or e}")
        return 1
    except json.JSONDecodeError as e:
        log(f"ERROR: failed to parse gh output: {e}")
        return 1

    new_issues = [i for i in issues if not is_seen(i)]
    if not new_issues:
        log("No new open issues.")
        return 0

    log(f"Found {len(new_issues)} new open issue(s).")
    for issue in sorted(new_issues, key=lambda x: x["number"]):
        num = issue["number"]
        title = issue.get("title", "")
        url = issue.get("url", "")
        log(f"NEW ISSUE #{num}: {title} — {url}")
        try:
            mark_seen(num)
            log(f"Labeled issue #{num} with '{SEEN_LABEL}'")
        except subprocess.CalledProcessError as e:
            log(f"ERROR: failed to label issue #{num}: {e.stderr.strip() or e}")
            # Continue processing others
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
