# Automation

## Issue polling (`poll_issues.py`)

Polls open GitHub issues for `stkraine/kraineArcade`, logs new issues, and adds the `agent:seen` label to avoid duplicates.

### Prerequisites
- [GitHub CLI (`gh`)](https://cli.github.com/) installed
- Auth via `gh auth login` **or** `GH_TOKEN`/`GITHUB_PAT` environment variable

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
