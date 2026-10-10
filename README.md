# Elmah.Io.Cli

A CLI to execute common tasks against [elmah.io](https://elmah.io).

Documentation: [CLI overview](https://docs.elmah.io/cli-overview/)

## Use with Claude Code

This repo ships a [Claude Code](https://claude.com/claude-code) plugin that teaches Claude how to drive the CLI and bundles the elmah.io MCP server.

Inside Claude Code:

```
/plugin marketplace add elmahio/Elmah.Io.Cli
/plugin install elmahio@elmahio
```

See [`plugins/elmahio/README.md`](plugins/elmahio/README.md) for details. To install only the skill (without the marketplace), run `elmahio skill install`.