using Elmah.Io.Client;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;

namespace Elmah.Io.Cli
{
    class TailCommand : CommandBase
    {
        internal static Command Create()
        {
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true,
            };
            var logIdOption = new Option<Guid>("--logId", "The ID of the log to send the log message to")
            {
                IsRequired = true
            };
            var logCommand = new Command("tail", "Tail log messages from a specified log")
            {
                apiKeyOption, logIdOption
            };
            logCommand.SetHandler(async (apiKey, logId) =>
            {
                var api = Api(apiKey);
                var from = DateTimeOffset.UtcNow;
                var previous = new List<string>();
                while (true)
                {
                    try
                    {
                        Thread.Sleep(5000);
                        var now = DateTimeOffset.UtcNow;
                        var fiveSecondsBefore = from.AddSeconds(-5);
                        var result = await api.Messages.GetAllAsync(logId.ToString(), 0, 0, "*", fiveSecondsBefore, now, false);
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
                            var response = await api.Messages.GetAllAsync(logId.ToString(), i / 10, 10, "*", fiveSecondsBefore, now, false);
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
            }, apiKeyOption, logIdOption);

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
