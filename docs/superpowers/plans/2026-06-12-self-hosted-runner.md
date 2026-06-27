# Self-Hosted GitHub Actions Runner (M5 Pro Mac) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Register this M5 Pro Mac as a self-hosted GitHub Actions runner with label `m5-pro-local`, configure it as a persistent launch agent, and update the integration workflow to target it — eliminating free-tier minute consumption on PR feedback and delivering near-instant job start times.

**Architecture:** The GitHub Actions runner application lives at `~/actions-runner/` and runs as a macOS launch agent under the current user (not a system daemon, to avoid permission pitfalls on a dev machine). Only `deploy-integration.yml` (the PR/branch pipeline) is moved to the self-hosted runner — `deploy.yml` (production, runs on master) stays on `ubuntu-latest` to ensure production deploys always have a clean, available environment. The `xvfb-run` wrapper in the integration workflow is Linux-specific and is removed since macOS Playwright runs headlessly without it. Markdown-only pushes are excluded at the top-level `on.push.paths-ignore` level.

**Tech Stack:** GitHub Actions runner v2.335.1 (macOS arm64), launchctl / `svc.sh` (service management), YAML workflow files, Playwright (already headless on macOS)

---

## Scope

This plan covers **configuration and infrastructure only** — no application code changes. Changes are:

1. Runner binary downloaded and registered
2. Runner service installed and started
3. `deploy-integration.yml` — `runs-on` changes, remove `xvfb-run`, add top-level path ignore
4. `deploy.yml` — unchanged (stays on `ubuntu-latest`)
5. Smoke test commit to verify end-to-end

---

## File Structure

- **Create:** `~/actions-runner/` — runner binary directory (outside repo, do NOT commit)
- **Modify:** `.github/workflows/deploy-integration.yml` — `runs-on` targets, remove `xvfb-run`, add `paths-ignore`

---

## Task 1: Download and extract the runner binary

**Files:**
- Create: `~/actions-runner/` (outside repo — do NOT commit this directory)

- [ ] **Step 1: Create the runner directory**

```bash
mkdir -p ~/actions-runner
cd ~/actions-runner
```

- [ ] **Step 2: Download the macOS arm64 runner**

```bash
curl -o actions-runner-osx-arm64-2.335.1.tar.gz -L \
  https://github.com/actions/runner/releases/download/v2.335.1/actions-runner-osx-arm64-2.335.1.tar.gz
```

Expected: a 127 MB `.tar.gz` file.

- [ ] **Step 3: Verify the checksum matches GitHub's published hash**

Go to https://github.com/actions/runner/releases/tag/v2.335.1, find the `actions-runner-osx-arm64-2.335.1.tar.gz` row in the SHA-256 checksums section, copy the hash, then verify:

```bash
echo "<PASTE_SHA256_HERE>  actions-runner-osx-arm64-2.335.1.tar.gz" | shasum -a 256 --check
```

Expected output: `actions-runner-osx-arm64-2.335.1.tar.gz: OK`

- [ ] **Step 4: Extract the archive**

```bash
tar xzf actions-runner-osx-arm64-2.335.1.tar.gz
```

Expected: `run.sh`, `config.sh`, `svc.sh`, and supporting binaries appear in `~/actions-runner/`.

---

## Task 2: Generate a registration token and configure the runner

The runner needs a short-lived token from GitHub to register against the repo.

- [ ] **Step 1: Generate a fresh registration token via the GitHub CLI**

```bash
gh api \
  --method POST \
  -H "Accept: application/vnd.github+json" \
  /repos/DARKinVADER/TinyHeroes/actions/runners/registration-token \
  --jq '.token'
```

Copy the printed token — it expires in 60 minutes.

- [ ] **Step 2: Run the configuration script**

```bash
cd ~/actions-runner
./config.sh \
  --url https://github.com/DARKinVADER/TinyHeroes \
  --token <TOKEN_FROM_PREVIOUS_STEP> \
  --name "m5pro-local" \
  --labels "self-hosted,m5-pro-local" \
  --work "_work" \
  --unattended
```

Expected: Output ends with `Runner successfully added`.

- [ ] **Step 3: Verify the runner appears in GitHub**

```bash
gh api repos/DARKinVADER/TinyHeroes/actions/runners \
  --jq '.runners[] | {name, status, labels: [.labels[].name]}'
```

Expected: One entry with `name: "m5pro-local"`, `status: "offline"` (it hasn't started yet), labels include `"self-hosted"` and `"m5-pro-local"`.

---

## Task 3: Install and start the runner as a macOS launch agent

The `svc.sh` script installs a `launchctl` agent plist for the current user. This keeps the runner alive across reboots without needing root.

- [ ] **Step 1: Install the runner service**

```bash
cd ~/actions-runner
./svc.sh install
```

Expected: Output similar to `Creating launch agent in /Users/<you>/Library/LaunchAgents/`. Does NOT require `sudo`.

- [ ] **Step 2: Start the service**

```bash
./svc.sh start
```

Expected: `Service started`.

- [ ] **Step 3: Verify the service is running**

```bash
./svc.sh status
```

Expected output includes `Active: active (running)` or the macOS equivalent (`status = 0`).

- [ ] **Step 4: Verify the runner comes online in GitHub**

```bash
gh api repos/DARKinVADER/TinyHeroes/actions/runners \
  --jq '.runners[] | {name, status}'
```

Expected: `"status": "idle"` (not `"offline"`).

---

## Task 4: Update `deploy-integration.yml` — switch `runs-on`, remove `xvfb-run`, add path ignore

**Files:**
- Modify: `.github/workflows/deploy-integration.yml`

This file has **6 jobs**: `check-pr`, `detect-changes`, `test-backend`, `test-frontend`, `deploy-integration-frontend`, `deploy-integration-api`.

Three changes are needed:
1. All `runs-on` → `[self-hosted, m5-pro-local]`
2. Remove `xvfb-run` from the Playwright step (Linux-only; macOS Playwright runs headlessly without it)
3. Add top-level `paths-ignore` so markdown/docs-only pushes skip the entire workflow

- [ ] **Step 1: Add `paths-ignore` to the `on.push` trigger**

The current `on.push` block looks like:

```yaml
on:
  push:
    branches-ignore:
      - master
  pull_request:
    types: [opened, reopened, synchronize]
    branches-ignore:
      - master
  workflow_dispatch:
```

Change it to:

```yaml
on:
  push:
    branches-ignore:
      - master
    paths-ignore:
      - '**/*.md'
      - 'docs/**'
  pull_request:
    types: [opened, reopened, synchronize]
    branches-ignore:
      - master
  workflow_dispatch:
```

Note: `paths-ignore` only applies to `push` events, not `pull_request`. PR events always run when there's a PR — that's intentional.

- [ ] **Step 2: Change all `runs-on: ubuntu-latest` to `runs-on: [self-hosted, m5-pro-local]`**

There are **6 jobs** — replace every occurrence:

```yaml
# Replace every instance of:
    runs-on: ubuntu-latest
# With:
    runs-on: [self-hosted, m5-pro-local]
```

Verify after editing:

```bash
grep -c "runs-on" .github/workflows/deploy-integration.yml
```
Expected: `6`

```bash
grep "runs-on" .github/workflows/deploy-integration.yml
```
Expected: all 6 lines show `[self-hosted, m5-pro-local]`, none show `ubuntu-latest`.

- [ ] **Step 3: Remove the `xvfb-run` wrapper from the Playwright step**

Find the `Run frontend e2e tests` step and change:

```yaml
      - name: Run frontend e2e tests
        if: needs.check-pr.outputs.has_pr == 'true'
        run: xvfb-run --auto-servernum npm run e2e -- --reporter=line,junit
        working-directory: frontend
        env:
          PLAYWRIGHT_JUNIT_OUTPUT_FILE: test-results/e2e-junit.xml
```

To:

```yaml
      - name: Run frontend e2e tests
        if: needs.check-pr.outputs.has_pr == 'true'
        run: npm run e2e -- --reporter=line,junit
        working-directory: frontend
        env:
          PLAYWRIGHT_JUNIT_OUTPUT_FILE: test-results/e2e-junit.xml
```

- [ ] **Step 4: Verify the YAML is valid**

```bash
npx js-yaml .github/workflows/deploy-integration.yml > /dev/null && echo "YAML valid" || echo "YAML INVALID"
```

Expected: `YAML valid`

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/deploy-integration.yml
git commit -m "ci: switch integration workflow to self-hosted m5-pro-local runner"
```

---

## Task 5: Smoke test — verify end-to-end

- [ ] **Step 1: Push the feature branch and open a PR**

```bash
git push -u origin HEAD
gh pr create \
  --title "ci: self-hosted runner on M5 Pro Mac (#129)" \
  --body "Closes #129" \
  --base master
```

- [ ] **Step 2: Watch the integration workflow trigger on your machine**

```bash
tail -f ~/actions-runner/_diag/Runner_*.log
```

Expected: Within 2 seconds of the push, the runner picks up the job (you'll see `Running job: ...` in the log).

- [ ] **Step 3: Verify job start latency**

In the GitHub Actions tab for the PR, check the time between "push" and "job started". Expected: under 5 seconds (typically under 2s vs. 30–60s for GitHub-hosted runners).

- [ ] **Step 4: Confirm all jobs pass**

All jobs in `deploy-integration.yml` should complete successfully — backend tests, frontend unit tests, Playwright e2e, and integration deployments. No `xvfb-run: command not found` errors.

- [ ] **Step 5: Verify local npm cache is used on the second run**

Make a trivial commit (e.g., add a blank line to any file, then revert) and push again. On the second run, `npm ci` should complete in 10–30 seconds instead of 60–120 seconds because `~/.npm` (the tarball cache) is already populated on your machine. `dotnet restore` will similarly pull from `~/.nuget/packages`. The speed-up comes from the network being local, not from skipping the install step itself.

---

## Task 6: Document the setup

**Files:**
- Modify: `docs/deployment.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add runner recovery section to `docs/deployment.md`**

Add the following section at the end of `docs/deployment.md`:

````markdown
## Self-Hosted CI Runner (M5 Pro Mac)

The GitHub Actions runner lives at `~/actions-runner/` on the development Mac. It runs as a macOS launch agent (user-level, no `sudo` needed).

### Check runner status

```bash
cd ~/actions-runner && ./svc.sh status
```

### Start / stop / restart

```bash
cd ~/actions-runner
./svc.sh start
./svc.sh stop
./svc.sh restart
```

### If the runner goes offline

1. Check service status: `./svc.sh status`
2. Check logs: `tail -100 ~/actions-runner/_diag/Runner_*.log`
3. Start the service: `./svc.sh start`
4. If the token is expired (runner shows `Unauthorized`), re-register:
   ```bash
   TOKEN=$(gh api --method POST /repos/DARKinVADER/TinyHeroes/actions/runners/registration-token --jq '.token')
   cd ~/actions-runner
   ./config.sh --url https://github.com/DARKinVADER/TinyHeroes --token $TOKEN --name "m5pro-local" --labels "self-hosted,m5-pro-local" --replace --unattended
   ./svc.sh start
   ```

### Uninstalling the runner

```bash
cd ~/actions-runner
./svc.sh stop
./svc.sh uninstall
./config.sh remove --token $(gh api --method POST /repos/DARKinVADER/TinyHeroes/actions/runners/registration-token --jq '.token')
```
````

- [ ] **Step 2: Add CHANGELOG entry**

Add under `## [Unreleased]` → `### Changed` in `CHANGELOG.md`:

```markdown
### Changed
- CI pipelines now run on a local self-hosted M5 Pro Mac runner, eliminating GitHub-hosted minute consumption and delivering near-instant job start times.
```

- [ ] **Step 3: Commit documentation**

```bash
git add docs/deployment.md CHANGELOG.md
git commit -m "docs: add self-hosted runner setup and recovery notes"
```

---

## Self-Review

### Spec coverage check

| AC Item | Covered in task |
|---|---|
| Runner downloaded, configured, authenticated | Task 1 + 2 |
| Labels `self-hosted` and `m5-pro-local` | Task 2 Step 2 (`--labels`) |
| Configured as native macOS launch agent (`svc.sh`) | Task 3 |
| Workflow updated to `runs-on: [self-hosted, m5-pro-local]` | Task 4 Step 2 |
| Local caching verified (subsequent runs use `~/.npm`, `~/.nuget`) | Task 5 Step 5 |
| Test commit triggers workflow in < 2 seconds | Task 5 Step 3 |
| Stop triggering on docs/markdown changes | Task 4 Step 1 (`paths-ignore`) |

### Corrections from review

- `deploy.yml` (production) **intentionally left on `ubuntu-latest`** — self-hosted runner is for PR feedback speed, not production deploys
- Task 6 (public repo fork approval) **removed** — repo is private
- Markdown path ignore uses **top-level `on.push.paths-ignore`** not `paths-filter` negation (which is unsupported)
- Task 5 Step 5 caching note corrected: `npm ci` always reinstalls; speed-up is from local `~/.npm` tarball cache, not reuse of `node_modules`

### Consistency check

- Runner labels in `config.sh` (`self-hosted,m5-pro-local`) match `runs-on` arrays in workflow YAML (`[self-hosted, m5-pro-local]`) — consistent
- `xvfb-run` removal applied only to `deploy-integration.yml` (the only file changing) — correct
- `svc.sh` commands consistent between Task 3 (setup) and Task 6 (ops docs)
