#!/usr/bin/env python3
"""
Poll GitHub issues for stkraine/kraineArcade and log new (unseen) issues.

- Picks up all open issues.
- Tracks "seen" issues by adding the `agent:seen` label.
- Moves newly seen issues to "Ready" in GitHub Project (v2) when configured.
- MVP action: logs new issues to stdout (and optionally a log file).

Requirements:
- GitHub CLI (`gh`) installed and authenticated.
- For Project status updates: `gh` auth/PAT must have Projects write scope (`project`).

Cron example (every 1 minute):
  * * * * * cd /home/steven/Projects/kraineArcade && /usr/bin/env GH_TOKEN="$GITHUB_PAT" /usr/bin/python3 automation/poll_issues.py >> /tmp/kraineArcade-poll.log 2>&1

Adjust PATH/GH_TOKEN as needed for your cron environment.
"""

from __future__ import annotations

import json
import os
import subprocess
from datetime import datetime, timezone
from pathlib import Path

REPO = os.environ.get("GITHUB_REPO", "stkraine/kraineArcade")
SEEN_LABEL = os.environ.get("AGENT_SEEN_LABEL", "agent:seen")
LOG_FILE = os.environ.get("POLL_LOG_FILE")  # optional

# GitHub Projects (v2) integration (optional but enabled by default for this repo)
PROJECT_OWNER = os.environ.get("PROJECT_OWNER", "stkraine")
PROJECT_NUMBER = os.environ.get("PROJECT_NUMBER", "2")
PROJECT_ID = os.environ.get("PROJECT_ID", "PVT_kwHOBIs4f84BqxEy")
PROJECT_STATUS_FIELD_ID = os.environ.get("PROJECT_STATUS_FIELD_ID", "PVTSSF_lAHOBIs4f84BqxEyzhpGrzE")
PROJECT_STATUS_READY_OPTION_ID = os.environ.get("PROJECT_STATUS_READY_OPTION_ID", "61e4505c")
PROJECT_STATUS_IN_REVIEW_OPTION_ID = os.environ.get("PROJECT_STATUS_IN_REVIEW_OPTION_ID", "df73e18b")
ENABLE_PROJECT_STATUS = os.environ.get("ENABLE_PROJECT_STATUS", "1") not in ("0", "false", "False")


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


def find_project_item_id(issue_number: int) -> str | None:
    """Find the Project v2 item ID for an issue number in the configured project."""
    result = run(
        [
            "gh",
            "project",
            "item-list",
            PROJECT_NUMBER,
            "--owner",
            PROJECT_OWNER,
            "--limit",
            "200",
            "--format",
            "json",
        ],
        check=False,
    )
    if result.returncode != 0:
        return None
    try:
        data = json.loads(result.stdout or "{}")
    except json.JSONDecodeError:
        return None
    for item in data.get("items") or []:
        content = item.get("content") or {}
        if content.get("type") == "Issue" and content.get("number") == issue_number:
            if content.get("repository") == REPO:
                return item.get("id")
    return None


def ensure_project_item(issue_url: str, issue_number: int) -> str | None:
    """Ensure issue is added to the project and return its item ID."""
    item_id = find_project_item_id(issue_number)
    if item_id:
        return item_id
    # Try to add; ignore errors (may already exist or lack permission)
    run(
        [
            "gh",
            "project",
            "item-add",
            PROJECT_NUMBER,
            "--owner",
            PROJECT_OWNER,
            "--url",
            issue_url,
        ],
        check=False,
    )
    return find_project_item_id(issue_number)


def set_project_status(issue_url: str, issue_number: int, option_id: str, status_name: str) -> None:
    """Best-effort update of Project v2 Status field."""
    if not ENABLE_PROJECT_STATUS:
        return
    try:
        item_id = ensure_project_item(issue_url, issue_number)
        if not item_id:
            log(f"WARN: could not find/add project item for issue #{issue_number}; skipping status update")
            return
        result = run(
            [
                "gh",
                "project",
                "item-edit",
                "--id",
                item_id,
                "--project-id",
                PROJECT_ID,
                "--field-id",
                PROJECT_STATUS_FIELD_ID,
                "--single-select-option-id",
                option_id,
            ],
            check=False,
        )
        if result.returncode == 0:
            log(f"Project status for issue #{issue_number} set to '{status_name}'")
        else:
            err = (result.stderr or result.stdout or "").strip()
            log(f"WARN: failed to set project status for issue #{issue_number}: {err}")
    except Exception as e:
        log(f"WARN: project status update error for issue #{issue_number}: {e}")


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
        set_project_status(url, num, PROJECT_STATUS_READY_OPTION_ID, "Ready")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
