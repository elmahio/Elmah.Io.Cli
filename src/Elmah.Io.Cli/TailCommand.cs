using Elmah.Io.Client;
using Spectre.Console;
using System.CommandLine;
using System.Globalization;

namespace Elmah.Io.Cli
{
    class TailCommand : CommandBase
    {
        private sealed class RowModel
        {
            public string DateTime { get; init; } = "Unknown date";
            public string Severity { get; init; } = "";
            public string Message { get; init; } = "";
            public bool IsNew { get; set; }
        }

        internal static Command Create()
        {
            return BuildCommand(deprecated: true);
        }

        internal static Command CreateSubcommand()
        {
            return BuildCommand(deprecated: false);
        }

        private static Command BuildCommand(bool deprecated)
        {
            var apiKeyOption = ApiKeyOption();
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log to send the log message to", Required = true };
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var logCommand = new Command("tail", $"{(deprecated ? "(deprecated) " : "")}Tail log messages from a specified log")
            {
                apiKeyOption, logIdOption, proxyHostOption, proxyPortOption
            };
            logCommand.SetAction(async result =>
            {
                if (deprecated)
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] 'elmahio tail' is deprecated. Use 'elmahio logs tail' instead.");

                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var table = new Table
                {
                    Expand = true,
                };
                table.Border(TableBorder.Rounded).BorderColor(Color.Grey);
                table.AddColumn("DateTime");
                table.AddColumn("Severity");
                table.AddColumn("Message");

                AnsiConsole.Write(
                    new Rule("[bold yellow]📡  Tailing log[/]")
                        .LeftJustified()
                        .RuleStyle("grey"));
                AnsiConsole.MarkupLine("[dim]Press [black on white] CTRL [/]+[black on white] C [/] to quit[/]");
                Console.WriteLine();

                await AnsiConsole
                    .Live(table)
                    .StartAsync(async ctx =>
                    {
                        var api = Api(resolvedKey, host, port);
                        var rows = new List<RowModel>(256);
                        var seen = new List<string>();
                        var from = DateTimeOffset.UtcNow;

                        while (true)
                        {
                            try
                            {
                                await Task.Delay(5000);
                                var now = DateTimeOffset.UtcNow;
                                var fiveSecondsBefore = from.AddSeconds(-5);
                                var pollResult = await api.Messages.GetAllAsync(logId.ToString(), 0, 0, "*", fiveSecondsBefore, now, false);
                                if (pollResult == null || !pollResult.Total.HasValue || pollResult.Total.Value == 0)
                                {
                                    from = now;
                                    seen.Clear();
                                    continue;
                                }

                                int total = pollResult.Total.Value;
                                int i = 0;
                                var messages = new List<MessageOverview>();
                                while (i < total)
                                {
                                    var response = await api.Messages.GetAllAsync(logId.ToString(), i / 10, 10, "*", fiveSecondsBefore, now, false);
                                    messages.AddRange(response.Messages.Where(msg => !seen.Contains(msg.Id)));
                                    i += response.Messages.Count;
                                }

                                seen.Clear();

                                if (messages.Count > 0)
                                {
                                    UnstarExistingRows(table, rows);
                                    AppendNewRows(table, rows, messages, seen);
                                    ctx.Refresh();
                                }

                                from = now;
                            }
                            catch (Exception e)
                            {
                                AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                            }
                        }

                    });
            });

            return logCommand;
        }

        private static void UnstarExistingRows(Table table, List<RowModel> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!row.IsNew) continue;
                row.IsNew = false;
                table.UpdateCell(i, 2, Markup.Escape(row.Message));
            }
        }

        private static void AppendNewRows(
            Table table,
            List<RowModel> rows,
            IEnumerable<MessageOverview> messages,
            List<string> seen)
        {
            foreach (var m in messages.OrderBy(x => x.DateTime ?? DateTimeOffset.MinValue))
            {
                var vm = new RowModel
                {
                    DateTime = m.DateTime?.ToLocalTime().ToString(CultureInfo.CurrentCulture) ?? "Unknown date",
                    Severity = m.Severity ?? "Information",
                    Message = m.Title ?? "(no title)",
                    IsNew = true
                };

                var date = $"[dim]{vm.DateTime}[/]";
                var sev = $"{GetColor(vm.Severity)}{Markup.Escape(vm.Severity)}[/]";
                var msg = $"⭐ [bold]{Markup.Escape(vm.Message)}[/]";

                table.AddRow(date, sev, msg);
                rows.Add(vm);
                seen.Add(m.Id);
            }
        }

        private static string GetColor(string severity)
        {
            return severity switch
            {
                "Verbose" => "[#cccccc]",
                "Debug" => "[#95c1ba]",
                "Information" => "[#0da58e]",
                "Warning" => "[#ffc936]",
                "Error" => "[#e6614f]",
                "Fatal" => "[#993636]",
                _ => "[#0da58e]",
            };
        }
    }
}
