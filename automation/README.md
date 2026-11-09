# Automation

## Automation worker (`poll_issues.py`)

Cron-friendly worker for `stkraine/kraineArcade` (Project **#2**, “@stkraine’s untitled project”).

- Labels unseen open issues with `agent:seen` and moves them to **Ready** (normalizes `Backlog` → `Ready`).
- **Auto-implements** actionable issues (`Ready`/`Backlog`) **one at a time** when `ENABLE_AUTO_WORK=1` (default): sets **In progress**, runs `opencode run --auto` (no `-m`), then sets **In review** when an open PR is found (or back to **Ready** on failure). Uses `automation/.worker.lock`.
- **In review feedback:** if new **issue comments** or **PR conversation comments** are detected on an `In review` item, reopens work (`In progress` → address feedback → `In review`). Baselines tracked in `automation/state.json` (gitignored).
- **PR closed/merged:** moves the Project item to **Done**. Status stays **In review** until then.

### Prerequisites
- [GitHub CLI (`gh`)](https://cli.github.com/) installed and authenticated
- [OpenCode CLI](https://opencode.ai) for auto-implementation (`OPENCODE_BIN`, default `~/.opencode/bin/opencode`)
- Auth/token must include `repo` and **Projects (v2) write** (classic PAT: `project`; or `gh auth refresh -h github.com -s project`)
- Cron needs `gh`/`opencode` on `PATH` (e.g., add `PATH=/home/steven/.local/bin:/home/steven/.opencode/bin:/usr/bin:/bin` to the cron line)

### Key env vars
- `ENABLE_AUTO_WORK=0` — disable autonomous implementation (poll/sync only)
- `ENABLE_PROJECT_STATUS=0` — disable Project status updates
- `OPENCODE_TIMEOUT_SEC` (default `3600`)
- `OPENCODE_BIN`, `GH_BIN`, `REPO_DIR`, `LOG_FILE`

### Run manually
```bash
cd /home/steven/Projects/kraineArcade
export GH_TOKEN="$GITHUB_PAT"   # or use gh auth
python3 automation/poll_issues.py
```

Optional env vars:
- `GITHUB_REPO` (default: `stkraine/kraineArcade`)
- `AGENT_SEEN_LABEL` (default: `agent:seen`)
- `POLL_LOG_FILE` (append logs to a file)

### Cron (every 1 minute)
Edit crontab (`crontab -e`) and add:

```cron
* * * * * cd /home/steven/Projects/kraineArcade && GH_TOKEN="$GITHUB_PAT" /usr/bin/python3 automation/poll_issues.py >> /tmp/kraineArcade-poll.log 2>&1
```

Notes:
- Cron runs with a minimal `PATH`; if `gh` isn’t found, use its full path (e.g., `/home/you/.local/bin/gh`) or set `PATH` in the cron line.
- Ensure `GITHUB_PAT` is available to cron (you may need to source it or set it explicitly in a wrapper script; avoid committing secrets).
- Polling every minute can hit API rate limits over time; adjust if needed.
