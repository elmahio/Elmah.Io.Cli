using Elmah.Io.Client;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;
using System.Globalization;

namespace Elmah.Io.Cli
{
    class MessagesCommand : CommandBase
    {
        internal static Command Create()
        {
            var messagesCommand = new Command("messages", "Work with log messages")
            {
                CreateCountSubcommand(),
                LogCommand.CreateSubcommand(),
                CreateGetSubcommand(),
                CreateListFrequentSubcommand(),
                CreateListRecentSubcommand(),
            };

            return messagesCommand;
        }

        private static Command CreateCountSubcommand()
        {
            var apiKeyOption = ApiKeyOption();
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log", Required = true };
            var queryOption = new Option<string?>("--query") { Description = "Full-text or Lucene query to filter messages" };
            var severityOption = new Option<string?>("--severity") { Description = "Filter by severity (Verbose, Debug, Information, Warning, Error, Fatal)" };
            var fromOption = new Option<DateTimeOffset?>("--from") { Description = "Count messages from this date (defaults to 90 days ago)" };
            var jsonOption = JsonOption();
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var countSubcommand = new Command("count", "Count log messages matching optional filters")
            {
                apiKeyOption, logIdOption, queryOption, severityOption, fromOption, jsonOption, proxyHostOption, proxyPortOption
            };
            countSubcommand.SetAction(async result =>
            {
                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var query = result.GetValue(queryOption);
                var severity = result.GetValue(severityOption);
                var from = result.GetValue(fromOption);
                var json = result.GetValue(jsonOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var api = Api(resolvedKey, host, port);
                try
                {
                    Client.MessagesResult? countResult = null;
                    await AnsiConsole
                        .Status()
                        .Spinner(new BugShotSpinner())
                        .StartAsync("Counting messages...", async ctx =>
                        {
                            var effectiveFrom = from ?? DateTimeOffset.UtcNow.AddDays(-90);
                            var effectiveQuery = BuildQuery(query, severity);
                            countResult = await api.Messages.GetAllAsync(logId.ToString(), 0, 0, effectiveQuery, effectiveFrom, null, false);
                        });

                    var total = countResult?.Total ?? 0;

                    if (json)
                        AnsiConsole.Write(new JsonText(JsonConvert.SerializeObject(new { total }, Formatting.Indented)));
                    else
                        AnsiConsole.MarkupLine($"[#0da58e]Total messages:[/] [bold]{total}[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return countSubcommand;
        }

        private static Command CreateGetSubcommand()
        {
            var apiKeyOption = ApiKeyOption();
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log", Required = true };
            var messageIdOption = new Option<string>("--messageId") { Description = "The ID of the message to fetch", Required = true };
            var jsonOption = JsonOption();
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var getSubcommand = new Command("get", "Fetch a single log message by ID")
            {
                apiKeyOption, logIdOption, messageIdOption, jsonOption, proxyHostOption, proxyPortOption
            };
            getSubcommand.SetAction(async result =>
            {
                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var messageId = result.GetValue(messageIdOption);
                var json = result.GetValue(jsonOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var api = Api(resolvedKey, host, port);
                try
                {
                    Client.Message? msg = null;
                    await AnsiConsole
                        .Status()
                        .Spinner(new BugShotSpinner())
                        .StartAsync("Fetching message...", async ctx =>
                        {
                            msg = await api.Messages.GetAsync(messageId, logId.ToString());
                        });

                    if (msg == null)
                    {
                        if (json) AnsiConsole.Write(new JsonText("{}"));
                        else AnsiConsole.MarkupLine("[yellow]Message not found[/]");
                        return;
                    }

                    if (json)
                    {
                        AnsiConsole.Write(new JsonText(JsonConvert.SerializeObject(msg, Formatting.Indented)));
                        return;
                    }

                    var grid = new Grid();
                    grid.AddColumn(new GridColumn().NoWrap());
                    grid.AddColumn();

                    grid.AddRow("[bold]ID[/]", Markup.Escape(msg.Id ?? ""));
                    grid.AddRow("[bold]Title[/]", Markup.Escape(msg.Title ?? ""));
                    grid.AddRow("[bold]Severity[/]", GetColoredSeverity(msg.Severity));
                    grid.AddRow("[bold]DateTime[/]", $"[dim]{msg.DateTime?.ToLocalTime().ToString(CultureInfo.CurrentCulture) ?? ""}[/]");
                    if (!string.IsNullOrWhiteSpace(msg.Type))
                        grid.AddRow("[bold]Type[/]", Markup.Escape(msg.Type));
                    if (!string.IsNullOrWhiteSpace(msg.Source))
                        grid.AddRow("[bold]Source[/]", Markup.Escape(msg.Source));
                    if (msg.StatusCode.HasValue)
                        grid.AddRow("[bold]Status Code[/]", msg.StatusCode.Value.ToString());
                    if (!string.IsNullOrWhiteSpace(msg.Url))
                        grid.AddRow("[bold]URL[/]", Markup.Escape(msg.Url));
                    if (!string.IsNullOrWhiteSpace(msg.Method))
                        grid.AddRow("[bold]Method[/]", Markup.Escape(msg.Method));
                    if (!string.IsNullOrWhiteSpace(msg.Hostname))
                        grid.AddRow("[bold]Hostname[/]", Markup.Escape(msg.Hostname));
                    if (!string.IsNullOrWhiteSpace(msg.User))
                        grid.AddRow("[bold]User[/]", Markup.Escape(msg.User));
                    if (!string.IsNullOrWhiteSpace(msg.Application))
                        grid.AddRow("[bold]Application[/]", Markup.Escape(msg.Application));
                    if (!string.IsNullOrWhiteSpace(msg.Version))
                        grid.AddRow("[bold]Version[/]", Markup.Escape(msg.Version));
                    if (!string.IsNullOrWhiteSpace(msg.CorrelationId))
                        grid.AddRow("[bold]Correlation ID[/]", Markup.Escape(msg.CorrelationId));
                    if (!string.IsNullOrWhiteSpace(msg.Detail))
                    {
                        AnsiConsole.Write(grid);
                        AnsiConsole.WriteLine();
                        AnsiConsole.Write(new Panel(Markup.Escape(msg.Detail)).Header("Detail").BorderColor(Color.Grey));
                        return;
                    }

                    AnsiConsole.Write(grid);
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return getSubcommand;
        }

        private static Command CreateListRecentSubcommand()
        {
            var apiKeyOption = ApiKeyOption();
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log", Required = true };
            var countOption = new Option<int>("--count") { Description = "Number of messages to return (1-100)", DefaultValueFactory = _ => 10 };
            var queryOption = new Option<string?>("--query") { Description = "Full-text or Lucene query to filter messages" };
            var severityOption = new Option<string?>("--severity") { Description = "Filter by severity (Verbose, Debug, Information, Warning, Error, Fatal)", DefaultValueFactory = _ => "Error" };
            var fromOption = new Option<DateTimeOffset?>("--from") { Description = "Return messages from this date" };
            var toOption = new Option<DateTimeOffset?>("--to") { Description = "Return messages up to this date" };
            var jsonOption = JsonOption();
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var listRecentSubcommand = new Command("list-recent", "List the most recent log messages")
            {
                apiKeyOption, logIdOption, countOption, queryOption, severityOption, fromOption, toOption, jsonOption, proxyHostOption, proxyPortOption
            };
            listRecentSubcommand.SetAction(async result =>
            {
                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var count = result.GetValue(countOption);
                var query = result.GetValue(queryOption);
                var severity = result.GetValue(severityOption);
                var from = result.GetValue(fromOption);
                var to = result.GetValue(toOption);
                var json = result.GetValue(jsonOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var api = Api(resolvedKey, host, port);
                try
                {
                    Client.MessagesResult? listResult = null;
                    var effectiveCount = Math.Clamp(count, 1, 100);
                    var effectiveQuery = BuildQuery(query, severity);
                    await AnsiConsole
                        .Status()
                        .Spinner(new BugShotSpinner())
                        .StartAsync("Fetching messages...", async ctx =>
                        {
                            listResult = await api.Messages.GetAllAsync(logId.ToString(), 0, effectiveCount, effectiveQuery, from, to, false);
                        });

                    if (listResult == null || listResult.Messages == null || listResult.Messages.Count == 0)
                    {
                        if (json) AnsiConsole.Write(new JsonText("[]"));
                        else AnsiConsole.MarkupLine("[yellow]No messages found[/]");
                        return;
                    }

                    if (json)
                    {
                        AnsiConsole.Write(new JsonText(JsonConvert.SerializeObject(new { total = listResult.Total, messages = listResult.Messages }, Formatting.Indented)));
                        return;
                    }

                    var table = new Table { Expand = true };
                    table.Border(TableBorder.Rounded).BorderColor(Color.Grey);
                    table.AddColumn("DateTime");
                    table.AddColumn("Severity");
                    table.AddColumn("Title");
                    table.AddColumn("Source");

                    foreach (var msg in listResult.Messages)
                    {
                        table.AddRow(
                            $"[dim]{msg.DateTime?.ToLocalTime().ToString(CultureInfo.CurrentCulture) ?? ""}[/]",
                            GetColoredSeverity(msg.Severity),
                            Markup.Escape(msg.Title ?? "(no title)"),
                            Markup.Escape(msg.Source ?? "")
                        );
                    }

                    AnsiConsole.MarkupLine($"[dim]Showing {listResult.Messages.Count} of {listResult.Total} messages[/]");
                    AnsiConsole.Write(table);
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return listRecentSubcommand;
        }

        private static Command CreateListFrequentSubcommand()
        {
            var apiKeyOption = ApiKeyOption();
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log", Required = true };
            var countOption = new Option<int>("--count") { Description = "Number of frequent groups to return (1-25)", DefaultValueFactory = _ => 5 };
            var queryOption = new Option<string?>("--query") { Description = "Full-text or Lucene query to filter messages" };
            var severityOption = new Option<string?>("--severity") { Description = "Filter by severity (Verbose, Debug, Information, Warning, Error, Fatal)", DefaultValueFactory = _ => "Error" };
            var fromOption = new Option<DateTimeOffset?>("--from") { Description = "Search from this date" };
            var toOption = new Option<DateTimeOffset?>("--to") { Description = "Search to this date" };
            var jsonOption = JsonOption();
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var listFrequentSubcommand = new Command("list-frequent", "List the most frequently occurring error groups")
            {
                apiKeyOption, logIdOption, countOption, queryOption, severityOption, fromOption, toOption, jsonOption, proxyHostOption, proxyPortOption
            };
            listFrequentSubcommand.SetAction(async result =>
            {
                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var count = result.GetValue(countOption);
                var query = result.GetValue(queryOption);
                var severity = result.GetValue(severityOption);
                var from = result.GetValue(fromOption);
                var to = result.GetValue(toOption);
                var json = result.GetValue(jsonOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var api = Api(resolvedKey, host, port);
                try
                {
                    var effectiveCount = Math.Clamp(count, 1, 25);
                    var effectiveQuery = BuildQuery(query, severity);
                    var effectiveFrom = from ?? DateTimeOffset.UtcNow.AddDays(-90);
                    var allMessages = new List<MessageOverview>();
                    string? nextSearchAfter = null;
                    int maxPages = 10;
                    int pageSize = 100;
                    await AnsiConsole
                        .Status()
                        .Spinner(new BugShotSpinner())
                        .StartAsync("Fetching frequent messages...", async ctx =>
                        {
                            for (int i = 0; i < maxPages; i++)
                            {
                                var result = await api.Messages.GetAllAsync(
                                    logId: logId.ToString(),
                                    pageSize: pageSize,
                                    query: effectiveQuery,
                                    from: effectiveFrom,
                                    to: to,
                                    includeHeaders: false,
                                    searchAfter: nextSearchAfter
                                );

                                if (result.Messages == null || result.Messages.Count == 0) break;

                                allMessages.AddRange(result.Messages);
                                nextSearchAfter = result.SearchAfter;
                                if (string.IsNullOrWhiteSpace(nextSearchAfter)) break;
                            }
                        });

                    if (allMessages == null || allMessages.Count == 0)
                    {
                        if (json) AnsiConsole.Write(new JsonText("[]"));
                        else AnsiConsole.MarkupLine("[yellow]No messages found[/]");
                        return;
                    }

                    // Consider exposing Hash on the API to group on hash rather than title.
                    var groups = allMessages
                        .GroupBy(m => m.TitleTemplate ?? m.Title ?? "(no title)")
                        .Select(g => new { title = g.Key, count = g.Count(), sample = g.First() })
                        .OrderByDescending(g => g.count)
                        .Take(effectiveCount)
                        .ToList();

                    if (json)
                    {
                        AnsiConsole.Write(new JsonText(JsonConvert.SerializeObject(groups, Formatting.Indented)));
                        return;
                    }

                    var table = new Table { Expand = true };
                    table.Border(TableBorder.Rounded).BorderColor(Color.Grey);
                    table.AddColumn("Count");
                    table.AddColumn("Severity");
                    table.AddColumn("Title");
                    table.AddColumn("Source");

                    foreach (var g in groups)
                    {
                        table.AddRow(
                            $"[bold]{g.count}[/]",
                            GetColoredSeverity(g.sample.Severity),
                            Markup.Escape(g.title),
                            Markup.Escape(g.sample.Source ?? "")
                        );
                    }

                    AnsiConsole.Write(table);
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return listFrequentSubcommand;
        }

        private static string BuildQuery(string? query, string? severity)
        {
            if (string.IsNullOrWhiteSpace(severity))
                return query ?? "*";
            var severityClause = $"severity:{severity}";
            return string.IsNullOrWhiteSpace(query) ? severityClause : $"({query}) AND {severityClause}";
        }

        private static string GetColoredSeverity(string? severity)
        {
            return severity switch
            {
                "Verbose" => $"[#cccccc]{Markup.Escape(severity)}[/]",
                "Debug" => $"[#95c1ba]{Markup.Escape(severity)}[/]",
                "Information" => $"[#0da58e]{Markup.Escape(severity)}[/]",
                "Warning" => $"[#ffc936]{Markup.Escape(severity ?? "")}[/]",
                "Error" => $"[#e6614f]{Markup.Escape(severity ?? "")}[/]",
                "Fatal" => $"[#993636]{Markup.Escape(severity ?? "")}[/]",
                _ => Markup.Escape(severity ?? ""),
            };
        }
    }
}
