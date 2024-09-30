using Spectre.Console;
using System;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class ClearCommand : CommandBase
    {
        internal static Command Create()
        {
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true,
            };
            var logIdOption = new Option<Guid>("--logId", "The log ID of the log to clear messages")
            {
                IsRequired = true
            };
            var queryOption = new Option<string>("--query", "Clear messages matching this query (use * for all messages)")
            {
                IsRequired = true
            };
            var fromOption = new Option<DateTimeOffset?>("--from", "Optional date and time to clear messages from");
            var toOption = new Option<DateTimeOffset?>("--to", "Optional date and time to clear messages to");
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var clearCommand = new Command("clear", "Delete one or more messages from a log")
            {
                apiKeyOption, logIdOption, queryOption, fromOption, toOption, proxyHostOption, proxyPortOption
            };
            clearCommand.SetHandler(async (apiKey, logId, query, from, to, host, port) =>
            {
                var api = Api(apiKey, host, port);
                try
                {
                    await api.Messages.DeleteAllAsync(logId.ToString(), new Client.Search
                    {
                        Query = query,
                        From = from,
                        To = to,
                    });

                    AnsiConsole.MarkupLine("[green]Successfully cleared messages[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            }, apiKeyOption, logIdOption, queryOption, fromOption, toOption, proxyHostOption, proxyPortOption);

            return clearCommand;
        }
    }
}
