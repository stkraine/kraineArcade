# Contributing to kraineArcade

Thanks for contributing to **kraineArcade**, a Unity-based Android/iOS minigames app. This document describes the development rules and workflow that humans and automation/agents should follow so changes stay consistent, reviewable, and safe.

## Repository layout

- **`Assets/SharedLibrary/`** — shared code/assets that can be reused across games.
- **`Assets/Games/<GameName>/`** — game-specific code and assets (for example, `Assets/Games/Tetris/`). Keep game logic isolated where practical and depend on shared code intentionally.
- **`Assets/UI/`** — shared UI assets.
- **`Tests/`** — NUnit-style tests for core logic (see existing tests such as `Tests/TetrisCoreTests.cs`).
- **`docs/`** — project documentation (for example, [running a desktop simulation on Ubuntu 22.04](docs/RUNNING_ON_UBUNTU.md)).
- **`automation/`** — repo automation scripts (issue polling/agent workflow). Changes here can affect issue/project automation; keep them minimal and well-tested.

When adding a new game, prefer creating a dedicated folder under `Assets/Games/<GameName>/` and include a short `README.md` describing scope, implemented behavior, and follow-ups (see `Assets/Games/Tetris/README.md`).

## Development guidelines

### General
- **Follow existing conventions** in the area you change (naming, file organization, patterns, and API style). Match surrounding code rather than introducing a new style.
- **Keep changes focused.** Address the issue or task at hand; avoid unrelated refactors or formatting-only churn.
- **Preserve user/agent work.** Treat unfamiliar files or changes carefully—investigate before deleting or overwriting.
- **No secrets.** Do not commit credentials, tokens, private keys, or environment-specific secrets. Use environment variables or secure local configuration as appropriate.
- **Unity/C# practices:**
  - Write clear, maintainable C# code compatible with the project’s Unity/editor setup.
  - Prefer small, testable core logic classes (plain C# where possible) alongside Unity `MonoBehaviour` integration code.
  - Add brief comments/XML docs only where they clarify non-obvious behavior; avoid noise.
  - Be mindful of mobile constraints (performance, allocations in hot paths, input/orientation differences) when changing runtime behavior.

### Code generation (for agents and contributors)
- **Understand the issue first.** Read the issue, related PRs/docs, and nearby code before implementing.
- **Prefer minimal, correct implementations** over broad rewrites.
- **Update documentation** when behavior, setup, or developer workflows change.
- **If requirements are unclear or a safe decision cannot be made, ask for clarification** (for example, via an issue comment) rather than guessing on product or architecture decisions.

## Testing and validation

Run the most relevant checks for your change before opening a PR.

- **Unit/core logic tests:** Add or update NUnit tests in `Tests/` when changing testable logic. Existing tests use `[TestFixture]`/`[Test]` patterns; follow the same style.
- **Unity validation:** For Unity-scene, prefab, asset, or runtime integration changes, validate in the **Unity Editor** (Play Mode) and, when UI/layout is affected, use the **Device Simulator** as described in [docs/RUNNING_ON_UBUNTU.md](docs/RUNNING_ON_UBUNTU.md).
- **Automation changes:** If you modify `automation/`, sanity-check script behavior locally when feasible (for example, `python3 -m py_compile automation/poll_issues.py`) and describe what was verified.
- **CI:** This repository may not have automated CI configured for every change type. Do not claim checks passed unless you actually ran them; include exact commands/results in the PR description.

## Branching and commits

- **Branch naming:** Use a descriptive feature branch, typically `feat/issue-<number>-<short-slug>` (for example, `feat/issue-11-contributing-guidelines`). Use `fix/...` or `docs/...` when appropriate.
- **Commit messages:** Write clear, focused commit messages. Reference the issue number when applicable (for example, `docs: add contributing guidelines (#11)` or include `#11` in the message body).
- **Scope:** Prefer small commits that tell a coherent story; avoid mixing unrelated changes.

## Pull request requirements

PRs should make review easy and capture how the change was validated.

### PR title
- Clear and specific (what changed and where), aligned with repo conventions.

### PR description
Include:
- **Issue reference:** `Closes #<issue-number>` (or `Refs #...` if not fully closing). This is required when the PR resolves a tracked issue.
- **Summary:** What changed and why.
- **Key changes:** Bullet list of notable files/behaviors.
- **Testing/validation:** What you ran (commands, Unity Play Mode/Device Simulator checks, manual steps) and the results. If no automated tests apply, explain the manual verification.
- **Screenshots/video:** Required for visible UI/layout changes (include Device Simulator profile/orientation when relevant).
- **Risks/follow-ups:** Known limitations, migration notes, or next steps.

A PR template is provided at `.github/pull_request_template.md`; please fill it out.

### Review readiness
- Ensure the branch is up to date enough to merge cleanly when practical.
- Respond to review feedback with follow-up commits (or clear explanations) and keep the PR description/testing notes current.
- Keep PRs scoped; split large work into follow-up issues/PRs when possible.

## Automation and project workflow notes

Some issues may be worked on by the repository’s automation agent (see `automation/README.md`). Regardless of who authors the change, follow the same contribution rules above: clear branch/commits, relevant testing, and a PR description that includes `Closes #<issue>` and validation details.
