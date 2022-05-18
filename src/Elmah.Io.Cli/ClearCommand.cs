using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

namespace Elmah.Io.Cli
{
    class ClearCommand : CommandBase
    {
        internal static Command Create()
        {
            var clearCommand = new Command("clear")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<Guid>("--logId", "The log ID of the log to clear messages")
                {
                    IsRequired = true
                },
                new Option<string>("--query", "Clear messages matching this query (use * for all messages)")
                {
                    IsRequired = true
                },
                new Option<DateTimeOffset>("--from", "Optional date and time to clear messages from"),
                new Option<DateTimeOffset>("--to", "Optional date and time to clear messages to"),
            };
            clearCommand.Description = "Delete one or more messages from a log";
            clearCommand.Handler = CommandHandler.Create<string, Guid, string, DateTimeOffset?, DateTimeOffset?>((apiKey, logId, query, from, to) =>
            {
                var api = Api(apiKey);
                try
                {
                    api.Messages.DeleteAll(logId.ToString(), new Client.Search
                    {
                        Query = query,
                        From = from,
                        To = to,
                    });

                    AnsiConsole.MarkupLine("[green]Successfully cleared messages[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
                }
            });

            return clearCommand;
        }
    }
}
