using Elmah.Io.Client;
using Spectre.Console;
using System.CommandLine;

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
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var logCommand = new Command("tail", "Tail log messages from a specified log")
            {
                apiKeyOption, logIdOption, proxyHostOption, proxyPortOption
            };
            logCommand.SetHandler(async (apiKey, logId, host, port) =>
            {
                var api = Api(apiKey, host, port);
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
                        if (result == null || !result.Total.HasValue || result.Total.Value == 0)
                        {
                            from = now;
                            previous.Clear();
                            continue;
                        }

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

                        foreach (var message in messages.OrderBy(msg => msg.DateTime ?? DateTimeOffset.MinValue))
                        {
                            var table = new Table();
                            table.Border(TableBorder.None);
                            table.HideHeaders();
                            table.Expand = true;
                            table.AddColumns(new TableColumn("") { Width = 17 }, new TableColumn("") { Width = 9 }, new TableColumn(""));
                            table.AddRow(message.DateTime?.ToLocalTime().ToString() ?? "Unknown date", $"{GetColor(message.Severity)}{message.Severity}[/]", message.Title);
                            AnsiConsole.Write(table);
                            previous.Add(message.Id);
                        }

                        from = now;
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                    }
                }
            }, apiKeyOption, logIdOption, proxyHostOption, proxyPortOption);

            return logCommand;
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
