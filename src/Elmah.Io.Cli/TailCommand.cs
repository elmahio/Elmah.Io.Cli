using Elmah.Io.Client;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;

namespace Elmah.Io.Cli
{
    class TailCommand : CommandBase
    {
        internal static Command Create()
        {
            var logCommand = new Command("tail")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<Guid>("--logId", "The ID of the log to send the log message to")
                {
                    IsRequired = true
                },
            };
            logCommand.Description = "Tail log messages from a specified log";
            logCommand.Handler = CommandHandler.Create<string, Guid>((apiKey, logId) =>
            {
                var api = Api(apiKey);
                var from = DateTime.UtcNow;
                var previous = new List<string>();
                while (true)
                {
                    try
                    {
                        Thread.Sleep(5000);
                        var now = DateTime.UtcNow;
                        var fiveSecondsBefore = from.AddSeconds(-5);
                        var result = api.Messages.GetAll(logId.ToString(), 0, 0, "*", fiveSecondsBefore, now, false);
                        if (result?.Total.HasValue != true || result.Total.Value == 0)
                        {
                            from = now;
                            previous.Clear();
                            continue;
                        };

                        int total = result.Total.Value;
                        int i = 0;
                        var messages = new List<MessageOverview>();
                        while (i < total)
                        {
                            var response = api.Messages.GetAll(logId.ToString(), i / 10, 10, "*", fiveSecondsBefore, now, false);
                            messages.AddRange(response.Messages.Where(msg => !previous.Contains(msg.Id)));
                            i += response.Messages.Count;
                        }

                        previous.Clear();

                        foreach (var message in messages.OrderBy(msg => msg.DateTime.Value))
                        {
                            var table = new Table();
                            table.HideHeaders();
                            table.Expand = true;
                            table.AddColumns(new TableColumn("") { Width = 17 }, new TableColumn("") { Width = 9 }, new TableColumn(""));
                            table.AddRow(message.DateTime.Value.ToLocalTime().ToString(), $"{GetColor(message.Severity)}{message.Severity}[/]", message.Title);
                            AnsiConsole.Write(table);
                            previous.Add(message.Id);
                        }

                        from = now;
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
                    }
                }

#pragma warning disable CS0162 // Unreachable code detected
                return 0;
#pragma warning restore CS0162 // Unreachable code detected
            });

            return logCommand;
        }

        private static string GetColor(string severity)
        {
            switch(severity)
            {
                case "Verbose":
                    return "[#cccccc]";
                case "Debug":
                    return "[#95c1ba]";
                case "Information":
                    return "[#0da58e]";
                case "Warning":
                    return "[#ffc936]";
                case "Error":
                    return "[#e6614f]";
                case "Fatal":
                    return "[#993636]";
            }

            return "[#0da58e]";
        }
    }
}
