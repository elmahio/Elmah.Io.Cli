using Spectre.Console;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class ClearCommand : CommandBase
    {
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
            var logIdOption = new Option<Guid>("--logId") { Description = "The log ID of the log to clear messages", Required = true };
            var queryOption = new Option<string>("--query") { Description = "Clear messages matching this query (use * for all messages)", Required = true };
            var fromOption = new Option<DateTimeOffset?>("--from") { Description = "Optional date and time to clear messages from" };
            var toOption = new Option<DateTimeOffset?>("--to") { Description = "Optional date and time to clear messages to" };
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var clearCommand = new Command("clear", $"{(deprecated ? "(deprecated) " : "")}Delete one or more messages from a log")
            {
                apiKeyOption, logIdOption, queryOption, fromOption, toOption, proxyHostOption, proxyPortOption
            };
            clearCommand.SetAction(async result =>
            {
                if (deprecated)
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] 'elmahio clear' is deprecated. Use 'elmahio logs clear' instead.");

                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var query = result.GetValue(queryOption);
                var from = result.GetValue(fromOption);
                var to = result.GetValue(toOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var api = Api(resolvedKey, host, port);
                try
                {
                    await AnsiConsole
                        .Status()
                        .Spinner(new BugShotSpinner())
                        .StartAsync("Deleting...", async ctx =>
                        {
                            await api.Messages.DeleteAllAsync(logId.ToString(), new Client.Search
                            {
                                Query = query,
                                From = from,
                                To = to,
                            });
                        });

                    AnsiConsole.MarkupLine("[green]Successfully cleared messages[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return clearCommand;
        }
    }
}
