# elmah.io plugin for Claude Code

This plugin gives Claude Code first-class knowledge of the [elmah.io](https://elmah.io) ecosystem:

- A **skill** ([`SKILL.md`](skills/elmahio/SKILL.md)) that teaches Claude how to drive [`Elmah.Io.Cli`](https://github.com/elmahio/Elmah.Io.Cli) — the official CLI, invoked as `elmahio`.
- The **elmah.io MCP server** (`https://mcp.elmah.io/mcp`), bundled inline so `/plugin install` registers it automatically. OAuth handles per-user authentication.

## Install

From inside Claude Code:

```
/plugin marketplace add elmahio/Elmah.Io.Cli
/plugin install elmahio@elmahio
```

The first time Claude calls an MCP tool, the elmah.io OAuth flow opens in your browser. After approval, both the skill and the MCP tools are active.

## Already configured the elmah.io MCP server manually?

If you previously added the elmah.io MCP server to `~/.claude.json`, the plugin install registers it again under the plugin scope. You can either:

- Leave both in place (harmless but redundant), or
- Remove the manual `mcpServers.elmahio` entry from `~/.claude.json` once the plugin is installed.

## What the skill covers

`SKILL.md` is a reference manual aimed at Claude. It documents:

- Install and authentication (`elmahio login`, `--apiKey` for CI)
- The `messages`, `deployments`, `logs` command groups
- The `diagnose` command for scanning projects for elmah.io integration issues
- Common workflows (track a deploy, watch logs, identify frequent errors, upload source maps)
- When to prefer the MCP server vs. the CLI
- Best practices for CI use (`--json`, `--nologo`, environment-variable API keys)

## Source

This plugin is maintained alongside the CLI itself in the [Elmah.Io.Cli repo](https://github.com/elmahio/Elmah.Io.Cli). Keeping the skill in the same repo means the docs evolve with the commands.
