#!/usr/bin/env python3
"""
Automation worker for stkraine/kraineArcade (rate-limit optimized).

- Labels unseen open issues (`agent:seen`) and syncs GitHub Project (v2) status.
- Auto-implements actionable issues (Ready/Backlog) one at a time via `opencode run --auto`.
- Handles In review feedback (issue + PR conversation comments) and Needs info replies.
- Moves to Done when PR is closed/merged.

Optimizations: cooldown on rate limits, cached project items/PR lookups per run,
skip redundant status edits, reduced API calls.
"""

from __future__ import annotations

import json
import os
import random
import re
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO = os.environ.get("GITHUB_REPO", "stkraine/kraineArcade")
REPO_DIR = Path(os.environ.get("REPO_DIR", "/home/steven/Projects/kraineArcade"))
SEEN_LABEL = os.environ.get("AGENT_SEEN_LABEL", "agent:seen")
LOG_FILE = os.environ.get("POLL_LOG_FILE")
STATE_FILE = REPO_DIR / "automation" / "state.json"
LOCK_FILE = REPO_DIR / "automation" / ".worker.lock"
COOLDOWN_FILE = REPO_DIR / "automation" / ".ratelimit-cooldown"
OPENCODE_BIN = os.environ.get("OPENCODE_BIN", str(Path.home() / ".opencode/bin/opencode"))
OPENCODE_TIMEOUT_SEC = int(os.environ.get("OPENCODE_TIMEOUT_SEC", "3600"))
RATE_LIMIT_COOLDOWN_SEC = int(os.environ.get("RATE_LIMIT_COOLDOWN_SEC", "900"))  # 15 min default

# GitHub Projects (v2)
PROJECT_OWNER = os.environ.get("PROJECT_OWNER", "stkraine")
PROJECT_NUMBER = os.environ.get("PROJECT_NUMBER", "2")
PROJECT_ID = os.environ.get("PROJECT_ID", "PVT_kwHOBIs4f84BqxEy")
PROJECT_STATUS_FIELD_ID = os.environ.get("PROJECT_STATUS_FIELD_ID", "PVTSSF_lAHOBIs4f84BqxEyzhpGrzE")
STATUS_OPTIONS = {
    "Backlog": "f75ad846",
    "Ready": "61e4505c",
    "Needs info": "6fe10d79",
    "In progress": "47fc9ee4",
    "In review": "df73e18b",
    "Done": "98236657",
}
ENABLE_PROJECT_STATUS = os.environ.get("ENABLE_PROJECT_STATUS", "1") not in ("0", "false", "False")
ENABLE_AUTO_WORK = os.environ.get("ENABLE_AUTO_WORK", "1") not in ("0", "false", "False")
GH_BIN = os.environ.get("GH_BIN", "gh")


class RateLimitError(RuntimeError):
    pass


def now_iso() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def log(msg: str) -> None:
    line = f"[{now_iso()}] {msg}"
    print(line, flush=True)
    if LOG_FILE:
        try:
            Path(LOG_FILE).parent.mkdir(parents=True, exist_ok=True)
            with open(LOG_FILE, "a", encoding="utf-8") as f:
                f.write(line + "\n")
        except OSError as e:
            print(f"[{now_iso()}] WARN: failed to write log file: {e}", flush=True)


def env_for_gh() -> dict[str, str]:
    env = os.environ.copy()
    if not env.get("GH_TOKEN") and env.get("GITHUB_PAT"):
        env["GH_TOKEN"] = env["GITHUB_PAT"]
    extra = f"{Path.home()}/.local/bin:{Path.home()}/.opencode/bin"
    env["PATH"] = extra + ":" + env.get("PATH", "")
    return env


def is_rate_limited(text: str) -> bool:
    t = (text or "").lower()
    return "rate limit" in t or "abuse detection" in t or "secondary rate limit" in t


def run(cmd: list[str], check: bool = True, cwd: Path | None = None, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
    res = subprocess.run(cmd, text=True, capture_output=True, env=env_for_gh(), check=False, cwd=str(cwd) if cwd else None, timeout=timeout)
    combined = (res.stdout or "") + "\n" + (res.stderr or "")
    if is_rate_limited(combined):
        raise RateLimitError(combined.strip()[:500])
    if check and res.returncode != 0:
        raise subprocess.CalledProcessError(res.returncode, cmd, output=res.stdout, stderr=res.stderr)
    return res


def gh_json(args: list[str], check: bool = True) -> Any:
    res = run([GH_BIN] + args, check=check)
    out = (res.stdout or "").strip()
    if not out:
        return None
    return json.loads(out)


def set_cooldown(seconds: int | None = None) -> None:
    secs = seconds if seconds is not None else RATE_LIMIT_COOLDOWN_SEC
    until = time.time() + max(60, secs)
    try:
        COOLDOWN_FILE.parent.mkdir(parents=True, exist_ok=True)
        COOLDOWN_FILE.write_text(json.dumps({"until": until, "set_at": now_iso()}), encoding="utf-8")
    except OSError:
        pass
    log(f"Rate limit cooldown set for ~{int(secs/60)} min")


def cooldown_active() -> bool:
    try:
        if not COOLDOWN_FILE.exists():
            return False
        data = json.loads(COOLDOWN_FILE.read_text(encoding="utf-8"))
        if time.time() < float(data.get("until", 0)):
            return True
        COOLDOWN_FILE.unlink(missing_ok=True)
    except Exception:
        try:
            COOLDOWN_FILE.unlink(missing_ok=True)
        except OSError:
            pass
    return False


def load_state() -> dict[str, Any]:
    if not STATE_FILE.exists():
        return {"issues": {}}
    try:
        return json.loads(STATE_FILE.read_text(encoding="utf-8"))
    except Exception:
        return {"issues": {}}


def save_state(state: dict[str, Any]) -> None:
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    tmp = STATE_FILE.with_suffix(".tmp")
    tmp.write_text(json.dumps(state, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    tmp.replace(STATE_FILE)


def acquire_lock() -> bool:
    LOCK_FILE.parent.mkdir(parents=True, exist_ok=True)
    try:
        fd = os.open(str(LOCK_FILE), os.O_CREAT | os.O_EXCL | os.O_WRONLY)
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write(f"pid={os.getpid()}\nstarted={now_iso()}\n")
        return True
    except FileExistsError:
        try:
            age = time.time() - LOCK_FILE.stat().st_mtime
            if age > OPENCODE_TIMEOUT_SEC * 2:
                log(f"WARN: removing stale lock ({int(age)}s old)")
                LOCK_FILE.unlink(missing_ok=True)
                return acquire_lock()
        except OSError:
            pass
        return False


def release_lock() -> None:
    try:
        LOCK_FILE.unlink(missing_ok=True)
    except OSError:
        pass


def ensure_label() -> None:
    result = run([GH_BIN, "label", "create", SEEN_LABEL, "--repo", REPO, "--color", "0E8A16",
                  "--description", "Issue has been seen by the polling agent"], check=False)
    if result.returncode == 0:
        log(f"Created label '{SEEN_LABEL}'")


def fetch_open_issues() -> list[dict[str, Any]]:
    data = gh_json(["issue", "list", "--repo", REPO, "--state", "open", "--json", "number,title,url,labels,updatedAt,body", "--limit", "100"])
    return data or []


def get_project_items() -> list[dict[str, Any]]:
    data = gh_json(["project", "item-list", PROJECT_NUMBER, "--owner", PROJECT_OWNER, "--limit", "200", "--format", "json"], check=False)
    if not isinstance(data, dict):
        return []
    return data.get("items") or []


def project_item_for_issue(items: list[dict[str, Any]], issue_number: int) -> dict[str, Any] | None:
    for item in items:
        content = item.get("content") or {}
        if content.get("type") == "Issue" and content.get("number") == issue_number and content.get("repository") == REPO:
            return item
    return None


def ensure_project_item(issue_url: str, issue_number: int, items: list[dict[str, Any]]) -> str | None:
    item = project_item_for_issue(items, issue_number)
    if item and item.get("id"):
        return item["id"]
    run([GH_BIN, "project", "item-add", PROJECT_NUMBER, "--owner", PROJECT_OWNER, "--url", issue_url], check=False)
    refreshed = get_project_items()
    items.clear()
    items.extend(refreshed)
    item = project_item_for_issue(items, issue_number)
    return item.get("id") if item else None


def set_project_status(issue_url: str, issue_number: int, status_name: str, items: list[dict[str, Any]] | None = None) -> bool:
    if not ENABLE_PROJECT_STATUS:
        return False
    option_id = STATUS_OPTIONS.get(status_name)
    if not option_id:
        log(f"WARN: unknown status '{status_name}'")
        return False
    local_items = items if items is not None else get_project_items()
    item = project_item_for_issue(local_items, issue_number)
    if item and item.get("status") == status_name:
        return True  # skip redundant edit
    item_id = ensure_project_item(issue_url, issue_number, local_items)
    if not item_id:
        log(f"WARN: could not find/add project item for issue #{issue_number}")
        return False
    # Recheck after ensure (it may have refreshed)
    item = project_item_for_issue(local_items, issue_number)
    if item and item.get("status") == status_name:
        return True
    res = run([GH_BIN, "project", "item-edit", "--id", item_id, "--project-id", PROJECT_ID,
               "--field-id", PROJECT_STATUS_FIELD_ID, "--single-select-option-id", option_id], check=False)
    if res.returncode == 0:
        log(f"Project #{PROJECT_NUMBER} issue #{issue_number} -> {status_name}")
        if item:
            item["status"] = status_name
        else:
            local_items.append({"id": item_id, "status": status_name, "content": {"type": "Issue", "number": issue_number, "repository": REPO, "url": issue_url}})
        return True
    log(f"WARN: failed to set status for #{issue_number}: {(res.stderr or res.stdout).strip()}")
    return False


def add_label(issue_number: int) -> None:
    run([GH_BIN, "issue", "edit", str(issue_number), "--repo", REPO, "--add-label", SEEN_LABEL], check=False)


def fetch_prs() -> list[dict[str, Any]]:
    # Batch fetch: open PRs + recently updated closed/merged (for Done transitions)
    prs: list[dict[str, Any]] = []
    for state in ("open", "closed"):
        data = gh_json(["pr", "list", "--repo", REPO, "--state", state, "--json",
                        "number,title,body,url,state,headRefName,updatedAt,mergedAt,closedAt", "--limit", "50"], check=False)
        if isinstance(data, list):
            prs.extend(data)
    return prs


def pr_references_issue(pr: dict[str, Any], issue_number: int) -> bool:
    text = f"{pr.get('title') or ''}\n{pr.get('body') or ''}"
    # Match #N, Closes #N, Fixes #N, Resolves #N (case-insensitive)
    return re.search(rf"(?<!\w)#\s*{issue_number}\b", text) is not None


def find_pr_for_issue(issue_number: int, prs: list[dict[str, Any]]) -> dict[str, Any] | None:
    matches = [p for p in prs if pr_references_issue(p, issue_number)]
    if not matches:
        return None
    open_prs = [p for p in matches if p.get("state") == "OPEN"]
    pool = open_prs or matches
    return sorted(pool, key=lambda p: p.get("updatedAt") or p.get("mergedAt") or p.get("closedAt") or "", reverse=True)[0]


def get_issue_comments(issue_number: int) -> list[dict[str, Any]]:
    data = gh_json(["issue", "view", str(issue_number), "--repo", REPO, "--json", "comments"], check=False)
    if not isinstance(data, dict):
        return []
    return data.get("comments") or []


def get_pr_comments(pr_number: int) -> list[dict[str, Any]]:
    data = gh_json(["pr", "view", str(pr_number), "--repo", REPO, "--json", "comments"], check=False)
    if not isinstance(data, dict):
        return []
    return data.get("comments") or []


def latest_comment_marker(comments: list[dict[str, Any]]) -> str | None:
    if not comments:
        return None
    last = comments[-1]
    return last.get("url") or last.get("id") or last.get("createdAt")


def has_new_feedback(issue_number: int, pr_number: int | None, state: dict[str, Any]) -> tuple[bool, str, bool]:
    issues_state = state.get("issues") or {}
    has_entry = str(issue_number) in issues_state
    istate = issues_state.get(str(issue_number)) or {}
    issue_comments = get_issue_comments(issue_number)
    pr_comments = get_pr_comments(pr_number) if pr_number else []
    issue_marker = latest_comment_marker(issue_comments)
    pr_marker = latest_comment_marker(pr_comments)
    prev_issue = istate.get("last_issue_comment")
    prev_pr = istate.get("last_pr_comment")
    if not has_entry:
        return False, "baseline", True
    reasons = []
    changed = False
    if issue_marker != prev_issue and (issue_marker is not None or prev_issue is not None):
        changed = True
        reasons.append("new issue comment(s)")
    if pr_marker != prev_pr and (pr_marker is not None or prev_pr is not None):
        changed = True
        reasons.append("new PR comment(s)")
    return changed, ", ".join(reasons) if reasons else "", False


def record_feedback_markers(issue_number: int, pr_number: int | None, state: dict[str, Any]) -> None:
    issues = state.setdefault("issues", {})
    istate = issues.setdefault(str(issue_number), {})
    issue_comments = get_issue_comments(issue_number)
    pr_comments = get_pr_comments(pr_number) if pr_number else []
    istate["last_issue_comment"] = latest_comment_marker(issue_comments)
    istate["last_pr_comment"] = latest_comment_marker(pr_comments)
    istate["updated"] = now_iso()


def run_opencode(prompt: str, title: str) -> tuple[bool, str]:
    if not Path(OPENCODE_BIN).exists():
        return False, f"opencode not found at {OPENCODE_BIN}"
    cmd = [OPENCODE_BIN, "run", "--auto", "--title", title, prompt]
    log(f"Running opencode: --title {title!r}")
    try:
        res = run(cmd, check=False, cwd=REPO_DIR, timeout=OPENCODE_TIMEOUT_SEC)
    except subprocess.TimeoutExpired:
        return False, f"opencode timed out after {OPENCODE_TIMEOUT_SEC}s"
    out = (res.stdout or "") + ("\n" + res.stderr if res.stderr else "")
    if len(out) > 4000:
        out = out[:2000] + "\n...[truncated]...\n" + out[-1500:]
    ok = res.returncode == 0
    log(f"opencode {'succeeded' if ok else 'failed'} (exit {res.returncode})")
    if out.strip():
        log("opencode output (tail): " + out.strip().splitlines()[-1][:500])
    return ok, out


def build_implementation_prompt(issue: dict[str, Any], issue_body: str) -> str:
    num = issue["number"]
    title = issue.get("title", "")
    url = issue.get("url", "")
    return f"""You are an automated coding agent working in {REPO_DIR} (repo {REPO}).

Task: Implement GitHub issue #{num}: {title}
Issue URL: {url}

Issue description:
{issue_body or '(no description)'}

Requirements:
- Work in the existing repo. Follow existing conventions and project structure.
- Use `gh` (already authenticated) and git as needed.
- **If requirements are unclear or you are blocked on a decision you cannot safely make:** post specific clarifying questions as an issue comment (`gh issue comment {num} --repo {REPO} --body "..."`), set GitHub Project #2 status for issue #{num} to **"Needs info"** (field id: {PROJECT_STATUS_FIELD_ID}, option id: {STATUS_OPTIONS['Needs info']}, project id: {PROJECT_ID}; find item id via `gh project item-list 2 --owner {PROJECT_OWNER} --format json` then `gh project item-edit ... --single-select-option-id {STATUS_OPTIONS['Needs info']}`), and **stop without creating a PR**.
- Otherwise: create a feature branch (e.g., `feat/issue-{num}-<slug>`), implement the changes, run relevant tests/checks if present.
- Commit with a clear message referencing #{num}.
- Push the branch and open a PR to `main` with `gh pr create`, including "Closes #{num}" in the PR body.
- Update GitHub Project #2 (owner {PROJECT_OWNER}, project id {PROJECT_ID}) status for issue #{num} to "In review" when the PR is opened.
  Status field id: {PROJECT_STATUS_FIELD_ID}, "In review" option id: {STATUS_OPTIONS['In review']}.
  Use: `gh project item-list 2 --owner {PROJECT_OWNER} --format json` to find the item id, then
  `gh project item-edit --id <ITEM_ID> --project-id {PROJECT_ID} --field-id {PROJECT_STATUS_FIELD_ID} --single-select-option-id {STATUS_OPTIONS['In review']}`.
- If you cannot fully complete the task, still open a draft PR with what you have and explain what remains.

Be thorough and safe. Do not expose secrets.
"""


def build_feedback_prompt(issue_number: int, pr: dict[str, Any], feedback_summary: str) -> str:
    pr_num = pr.get("number")
    branch = pr.get("headRefName", "")
    return f"""You are an automated coding agent working in {REPO_DIR} (repo {REPO}).

Task: Address new feedback on GitHub issue #{issue_number} (PR #{pr_num}, branch `{branch}`).
Detected: {feedback_summary}

Requirements:
- Fetch and check out the PR branch (`gh pr checkout {pr_num}` or `git fetch && git checkout {branch}`).
- Read the latest feedback: `gh issue view {issue_number} --repo {REPO} --comments` and `gh pr view {pr_num} --repo {REPO} --comments`.
- Implement the requested changes, run relevant tests/checks if present.
- Commit and push updates to the same PR branch. Reply in PR/issue comments if helpful (optional).
- Ensure GitHub Project #2 issue #{issue_number} remains/status is "In review" (option id {STATUS_OPTIONS['In review']}, field {PROJECT_STATUS_FIELD_ID}, project {PROJECT_ID}).
- Do not merge the PR. Do not mark Done (Done happens when the PR is closed).

Be thorough and safe. Do not expose secrets.
"""


def process_actionable_issue(issue: dict[str, Any], state: dict[str, Any], items: list[dict[str, Any]], prs: list[dict[str, Any]]) -> None:
    num = issue["number"]
    url = issue.get("url", "")
    title = issue.get("title", "")
    log(f"Starting work on #{num}: {title}")
    set_project_status(url, num, "In progress", items)
    body = issue.get("body") or ""
    prompt = build_implementation_prompt(issue, body)
    ok, _ = run_opencode(prompt, title=f"Issue #{num}: {title}"[:80])
    prs = fetch_prs()  # refresh after work
    pr = find_pr_for_issue(num, prs)
    if pr and pr.get("state") == "OPEN":
        set_project_status(url, num, "In review", items)
        record_feedback_markers(num, pr.get("number"), state)
        log(f"Issue #{num} -> In review (PR #{pr.get('number')})" + ("" if ok else " [opencode non-zero exit]"))
        return
    status_item = project_item_for_issue(get_project_items(), num)
    status = (status_item or {}).get("status")
    if status in ("Needs info", "In review", "Done"):
        record_feedback_markers(num, pr.get("number") if pr else None, state)
        log(f"Issue #{num} left in '{status}' after run (no open PR).")
        return
    set_project_status(url, num, "Ready", items)
    log(f"Issue #{num} returned to Ready (no open PR detected). Check logs.")


def process_in_review_item(issue: dict[str, Any], state: dict[str, Any], items: list[dict[str, Any]], prs: list[dict[str, Any]]) -> bool:
    num = issue["number"]
    url = issue.get("url", "")
    pr = find_pr_for_issue(num, prs)
    if pr and pr.get("state") in ("CLOSED", "MERGED"):
        set_project_status(url, num, "Done", items)
        log(f"Issue #{num} -> Done (PR #{pr.get('number')} {pr.get('state')})")
        return True
    pr_num = pr.get("number") if pr and pr.get("state") == "OPEN" else None
    changed, reason, baseline = has_new_feedback(num, pr_num, state)
    if baseline:
        record_feedback_markers(num, pr_num, state)
        return False
    if not changed:
        return False
    log(f"Issue #{num} has {reason}; addressing feedback")
    set_project_status(url, num, "In progress", items)
    prompt = build_feedback_prompt(num, pr or {}, reason)
    run_opencode(prompt, title=f"Feedback #{num}: {issue.get('title','')}"[:80])
    set_project_status(url, num, "In review", items)
    record_feedback_markers(num, pr_num, state)
    return True


def process_needs_info_item(issue: dict[str, Any], state: dict[str, Any], items: list[dict[str, Any]]) -> bool:
    num = issue["number"]
    url = issue.get("url", "")
    changed, reason, baseline = has_new_feedback(num, None, state)
    if baseline:
        record_feedback_markers(num, None, state)
        return False
    if not changed:
        return False
    log(f"Issue #{num} has {reason}; moving Needs info -> Ready")
    set_project_status(url, num, "Ready", items)
    record_feedback_markers(num, None, state)
    return True


def sync_labels_and_ready(open_issues: list[dict[str, Any]], items: list[dict[str, Any]]) -> None:
    for issue in open_issues:
        labels = [l.get("name") for l in (issue.get("labels") or [])]
        num = issue["number"]
        url = issue.get("url", "")
        item = project_item_for_issue(items, num)
        status = (item or {}).get("status")
        if SEEN_LABEL not in labels:
            add_label(num)
            log(f"NEW ISSUE #{num}: {issue.get('title','')} — {url}")
            if status in (None, "", "Backlog"):
                set_project_status(url, num, "Ready", items)
        elif status == "Backlog":
            set_project_status(url, num, "Ready", items)


def main() -> int:
    # Small jitter to avoid thundering herd
    time.sleep(random.uniform(0, 3))
    if cooldown_active():
        log("Rate-limit cooldown active; skipping run.")
        return 0
    if not acquire_lock():
        log("Another worker run is in progress; exiting.")
        return 0
    try:
        ensure_label()
        open_issues = fetch_open_issues()
        issues_by_num = {i["number"]: i for i in open_issues}
        items = get_project_items() if ENABLE_PROJECT_STATUS else []
        prs = fetch_prs() if ENABLE_PROJECT_STATUS else []
        sync_labels_and_ready(open_issues, items)

        if not ENABLE_AUTO_WORK:
            log("Auto-work disabled (ENABLE_AUTO_WORK=0).")
            return 0

        state = load_state()
        items = get_project_items() if ENABLE_PROJECT_STATUS else []
        prs = fetch_prs() if ENABLE_PROJECT_STATUS else []

        if any((it.get("status") == "In progress") for it in items):
            log("An item is already In progress; skipping new work this cycle.")
            for it in items:
                content = it.get("content") or {}
                if content.get("type") != "Issue" or content.get("repository") != REPO:
                    continue
                num = content.get("number")
                if it.get("status") == "In review" and num in issues_by_num:
                    process_in_review_item(issues_by_num[num], state, items, prs)
                elif it.get("status") == "Needs info" and num in issues_by_num:
                    process_needs_info_item(issues_by_num[num], state, items)
                elif it.get("status") == "In review":
                    pr = find_pr_for_issue(num, prs)
                    if pr and pr.get("state") in ("CLOSED", "MERGED"):
                        set_project_status(content.get("url", ""), num, "Done", items)
            save_state(state)
            return 0

        for it in items:
            content = it.get("content") or {}
            if content.get("type") != "Issue" or content.get("repository") != REPO:
                continue
            num = content.get("number")
            if it.get("status") == "In review" and num in issues_by_num:
                acted = process_in_review_item(issues_by_num[num], state, items, prs)
                if acted:
                    save_state(state)
                    return 0
            elif it.get("status") == "In review" and num not in issues_by_num:
                pr = find_pr_for_issue(num, prs)
                if pr and pr.get("state") in ("CLOSED", "MERGED"):
                    set_project_status(content.get("url", ""), num, "Done", items)
            elif it.get("status") == "Needs info" and num in issues_by_num:
                if process_needs_info_item(issues_by_num[num], state, items):
                    save_state(state)
                    return 0

        actionable = []
        for it in items:
            if it.get("status") not in ("Ready", "Backlog"):
                continue
            content = it.get("content") or {}
            if content.get("type") == "Issue" and content.get("repository") == REPO:
                num = content.get("number")
                if num in issues_by_num:
                    actionable.append((num, issues_by_num[num]))
        if actionable:
            actionable.sort(key=lambda x: x[0])
            _, issue = actionable[0]
            process_actionable_issue(issue, state, items, prs)
            save_state(state)
            return 0

        log("No actionable issues.")
        save_state(state)
        return 0
    except RateLimitError as e:
        log(f"ERROR: rate limited: {e}")
        set_cooldown()
        return 1
    except FileNotFoundError as e:
        log(f"ERROR: required tool not found: {e}")
        return 1
    except subprocess.CalledProcessError as e:
        msg = (e.stderr or e.stdout or str(e)).strip()
        log(f"ERROR: command failed: {msg}")
        if is_rate_limited(msg):
            set_cooldown()
        return 1
    except Exception as e:
        log(f"ERROR: {e}")
        if is_rate_limited(str(e)):
            set_cooldown()
        return 1
    finally:
        release_lock()


if __name__ == "__main__":
    raise SystemExit(main())
