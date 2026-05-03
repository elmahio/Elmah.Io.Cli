---
name: elmahio
description: Use the Elmah.Io.Cli (binary "elmahio") to query log messages, create deployments, tail logs, upload source maps, and diagnose elmah.io installations from the terminal. Pairs with the bundled elmah.io MCP server for in-conversation API queries.
argument-hint: <log query, deployment info, diagnose request, or task description>
allowed-tools: [Bash, Read, Write, Glob, Grep]
---

# elmah.io CLI

This plugin teaches Claude Code how to drive [`Elmah.Io.Cli`](https://github.com/elmahio/Elmah.Io.Cli) — the official CLI for [elmah.io](https://elmah.io), invoked as `elmahio`. It also bundles the elmah.io MCP server so Claude can query the elmah.io API directly during conversations.

## When to use the CLI vs. the bundled MCP server

Both connect to the same elmah.io account. Pick the surface that fits the context:

| Use the **MCP tools** (`mcp__elmahio__*`) when... | Use the **CLI** (`elmahio …`) when... |
|---|---|
| The user is in an interactive Claude conversation and asks about logs, deployments, uptime, organizations, or messages. | You are generating or running shell scripts, CI configs, GitHub Actions, or commands the user will run in a terminal. |
| You need to fetch a single value to inform the next step of reasoning. | The user asks to "import", "export", "tail", "upload a source map", or otherwise drive a workflow that produces files or long-running output. |
| The data fits in the conversation. | The user wants results piped to disk (CSV, JSON files), processed by other shell tools, or scheduled. |

If both surfaces could work, prefer the MCP tools for live queries and the CLI for anything that touches the filesystem or runs unattended.

## Install

The CLI ships as a .NET tool and as self-contained binaries.

**Cross-platform (recommended):**
```bash
dotnet tool install -g Elmah.Io.Cli
```

After install, the binary is invoked as `elmahio` (not `Elmah.Io.Cli`).

**Self-contained binaries** (no .NET runtime required) are published per release for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64` on the [GitHub releases page](https://github.com/elmahio/Elmah.Io.Cli/releases).

## Authentication

Two ways to authenticate:

1. **One-time login** — runs an interactive prompt and stores the API key locally:
   ```bash
   elmahio login
   ```
   After this, all subsequent commands pick up the stored key automatically.

2. **Per-command `--apiKey`** — required for non-interactive use (CI, scripts):
   ```bash
   elmahio messages list-recent --apiKey "$ELMAHIO_API_KEY" --logId <log-guid>
   ```

The API key needs the relevant permissions for the operation (e.g., "Messages Write" to clear, "Deployments Write" to create deployments).

To clear the stored key:
```bash
elmahio logout
```

## Global flags

These flags are accepted by every command:

| Flag | Description |
|---|---|
| `--apiKey <key>` | Override the stored API key for this invocation. |
| `--proxyHost <host>` | Hostname or IP of an HTTP proxy to use when calling elmah.io. |
| `--proxyPort <port>` | Port of the proxy. |
| `--json` | Where supported, output JSON instead of formatted tables. **Use this whenever you plan to parse the output.** |
| `--nologo` | Suppress the startup banner. Useful in CI logs. |

## Command reference

The CLI has three top-level commands (`login`, `logout`, `diagnose`) and three command groups (`messages`, `deployments`, `logs`). Several legacy aliases also exist at the top level — see [Deprecated aliases](#deprecated-aliases). Always prefer the grouped form.

---

### `login` / `logout`

Authenticate and de-authenticate. See [Authentication](#authentication).

```bash
elmahio login [--apiKey <key>] [--proxyHost <host>] [--proxyPort <port>]
elmahio logout
```

---

### `diagnose`

Scan a project directory for elmah.io integration issues — wrong package versions, missing config, clock drift, etc. Read-only.

```bash
elmahio diagnose [--directory <path>] [--verbose]
```

| Flag | Description |
|---|---|
| `--directory <path>` | Root to scan. Defaults to the current directory. |
| `--verbose` | Emit verbose diagnostic output. |

Detects integrations for: `Elmah.Io.AspNetCore`, `Elmah.Io.Extensions.Logging`, `Elmah.Io` (and `.Mvc` / `.WebApi` / `.AspNet`), `Elmah.Io.Log4Net`, `Elmah.Io.NLog`, `Serilog.Sinks.ElmahIo`. Examines `.csproj` and `packages.config` files; ignores `.git`, `.github`, `bin`, `obj`, `node_modules`, etc.

**Common workflow:** run before shipping a release to catch missing or misconfigured integrations early.

---

### `messages` group

Work with log messages. **All `messages` subcommands require `--logId`.**

#### `messages count`
Count messages matching optional filters.
```bash
elmahio messages count --logId <guid> [--query <lucene>] [--severity <level>] [--from <date>] [--json]
```

| Flag | Description |
|---|---|
| `--logId <guid>` | **Required.** Log to count from. |
| `--query <lucene>` | Lucene/full-text query (e.g., `"NullReferenceException"` or `application:web AND statusCode:500`). |
| `--severity <level>` | One of `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`. |
| `--from <date>` | ISO 8601 datetime or relative (e.g., `2025-01-01`). Defaults to 90 days ago. |

#### `messages get`
Fetch a single message by ID.
```bash
elmahio messages get --logId <guid> --messageId <id> [--json]
```

#### `messages list-recent`
List the most recent messages.
```bash
elmahio messages list-recent --logId <guid> [--count <n>] [--query <lucene>] [--severity <level>] [--from <date>] [--to <date>] [--json]
```

| Flag | Description |
|---|---|
| `--count <n>` | 1–100. Defaults to 10. |
| `--severity <level>` | Defaults to `Error` (filtered to errors unless overridden). |
| `--from` / `--to` | Optional date range. |

#### `messages list-frequent`
List the most frequently occurring error groups (groups by title template).
```bash
elmahio messages list-frequent --logId <guid> [--count <n>] [--query <lucene>] [--severity <level>] [--from <date>] [--to <date>] [--json]
```

| Flag | Description |
|---|---|
| `--count <n>` | 1–25. Defaults to 5. |

---

### `deployments` group

#### `deployments create`
Record a deployment. Useful at the end of CI/CD pipelines.
```bash
elmahio deployments create --version <version> [--created <iso8601>] [--description <text>] [--userName <name>] [--userEmail <email>] [--logId <guid>]
```

| Flag | Description |
|---|---|
| `--version <ver>` | **Required.** The version string (e.g., `1.4.2`, `2025.05.03-1`). |
| `--created <iso8601>` | UTC timestamp. Defaults to now. |
| `--description <text>` | Free-form release notes. |
| `--userName` / `--userEmail` | Person responsible for the deployment. |
| `--logId <guid>` | Scope to a single log; omit for organization-wide. |

#### `deployments list`
List recent deployments.
```bash
elmahio deployments list [--logId <guid>] [--count <n>] [--json]
```

`--count` is 1–25, defaults to 5.

---

### `logs` group

#### `logs list`
List all logs accessible with the API key.
```bash
elmahio logs list [--environment <name>] [--json]
```

#### `logs get`
Get details for a specific log.
```bash
elmahio logs get --logId <guid> [--json]
```

#### `logs clear`
Delete one or more messages from a log. **Destructive — confirm with the user before running.**
```bash
elmahio logs clear --logId <guid> [...]
```

#### `logs dataloader`
Load 50 test log messages into a specified log. Useful for demos and onboarding.
```bash
elmahio logs dataloader --logId <guid>
```

#### `logs export`
Export log messages from a log to a file.
```bash
elmahio logs export --logId <guid> [...]
```

#### `logs import`
Import log messages into a log from a file (CSV/JSON).
```bash
elmahio logs import --logId <guid> [...]
```

#### `logs sourcemap`
Upload a source map and minified JavaScript for a specific URL. Run after frontend deploys so stack traces resolve.
```bash
elmahio logs sourcemap --logId <guid> [...]
```

#### `logs tail`
Stream log messages from a log in real time.
```bash
elmahio logs tail --logId <guid> [...]
```

When the user is iterating on a fix and wants to "watch what comes in," prefer `logs tail` over polling `messages list-recent`.

---

## Deprecated aliases

These top-level commands still work for backwards compatibility, but **do not generate them in new instructions**. Use the grouped forms instead.

| Deprecated | Use instead |
|---|---|
| `elmahio clear` | `elmahio logs clear` |
| `elmahio dataloader` | `elmahio logs dataloader` |
| `elmahio deployment` | `elmahio deployments create` |
| `elmahio export` | `elmahio logs export` |
| `elmahio import` | `elmahio logs import` |
| `elmahio log` | `elmahio logs get` |
| `elmahio sourcemap` | `elmahio logs sourcemap` |
| `elmahio tail` | `elmahio logs tail` |

---

## Common workflows

### Track a release and watch for regressions
```bash
elmahio deployments create --version "$VERSION" --userName "$USER" --description "Release $VERSION"
elmahio logs tail --logId "$LOG_ID"
```

### Audit error volume in CI
```bash
elmahio messages count --logId "$LOG_ID" --severity Error --from "$YESTERDAY" --json | jq '.total'
```

### Identify hot errors before triage
```bash
elmahio messages list-frequent --logId "$LOG_ID" --count 10 --json
```

### Diagnose a project before opening a support ticket
```bash
elmahio diagnose --directory ./src --verbose
```

### Upload a source map after a frontend deploy
```bash
elmahio logs sourcemap --logId "$LOG_ID" [...]
```

---

## Best practices when generating CLI invocations

- **Always pass `--json` when the output will be parsed** by downstream tools or by yourself in a follow-up step. Default human output is colored and not parser-friendly.
- **Always pass `--apiKey` (or rely on a stored key) explicitly in CI**; don't assume an interactive `login` has happened on the build host. Read the key from an environment variable like `$ELMAHIO_API_KEY`.
- **Pass `--nologo`** in CI logs to suppress the startup banner.
- **`--logId` is a GUID** for nearly every subcommand; if the user gives a log name, run `elmahio logs list --json` first to resolve the ID.
- **`--severity` filtering** uses these exact values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`. Default for `list-recent` and `list-frequent` is `Error`.
- **Respect destructive operations.** `logs clear` deletes messages — confirm the log ID and scope with the user before running.
- **For `diagnose`**, run from the solution root or pass `--directory` explicitly. It does not require an API key.

## Troubleshooting

- `No API key provided. Pass --apiKey or run 'elmahio login' first.` — The CLI cannot find a stored key and none was passed. Run `elmahio login` or set `--apiKey`.
- `Login failed: ...` during `elmahio login` — Validate the key against [the elmah.io dashboard](https://app.elmah.io). The login command makes an authenticated `Logs.GetAllAsync` call; failures usually mean an invalid key or network/proxy issue.
- HTTP errors during commands — Check `--proxyHost` / `--proxyPort` if behind a corporate proxy. The CLI talks to `api.elmah.io:443`.
- `diagnose` reports no projects found — Run from the solution root, or pass `--directory <path>` to point at a directory containing `.csproj` or `packages.config` files.

## Links

- [Elmah.Io.Cli on GitHub](https://github.com/elmahio/Elmah.Io.Cli)
- [CLI overview docs](https://docs.elmah.io/cli-overview/)
- [elmah.io main site](https://elmah.io)
