using CsvHelper;
using Elmah.Io.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.CommandLine;
using System.Globalization;

namespace Elmah.Io.Cli
{
    class ExportCommand : CommandBase
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
            var today = DateTime.Today;
            var aWeekAgo = today.AddDays(-7);
            var apiKeyOption = ApiKeyOption();
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log to export messages from", Required = true };
            var dateFromOption = new Option<DateTimeOffset>("--dateFrom") { Description = $"Defines the Date from which the logs start. Ex. \" --dateFrom {aWeekAgo:yyyy-MM-dd}\"", Required = true };
            var dateToOption = new Option<DateTimeOffset>("--dateTo") { Description = $"Defines the Date from which the logs end. Ex. \" --dateTo {today:yyyy-MM-dd}\"", Required = true };
            var filenameOption = new Option<string>("--filename") { Description = "Defines the path and filename of the file to export to. Ex. \" --filename C:\\myDirectory\\myFile.json\" or \" --filename myFile.csv\"" };
            var queryOption = new Option<string>("--query") { Description = "Defines the query that is passed to the API", DefaultValueFactory = _ => "*" };
            var includeHeadersOption = new Option<bool>("--includeHeaders") { Description = "Include headers, cookies, etc. in output (will take longer to export)" };
            var formatOption = new Option<ExportFormat>("--format") { Description = "The format to export", DefaultValueFactory = _ => ExportFormat.Json };
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var exportCommand = new Command("export", $"{(deprecated ? "(deprecated) " : "")}Export log messages from a specified log")
            {
                apiKeyOption, logIdOption, dateFromOption, dateToOption, filenameOption, queryOption, includeHeadersOption, formatOption, proxyHostOption, proxyPortOption
            };
            exportCommand.SetAction(async result =>
            {
                if (deprecated)
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] 'elmahio export' is deprecated. Use 'elmahio logs export' instead.");

                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var dateFrom = result.GetValue(dateFromOption);
                var dateTo = result.GetValue(dateToOption);
                var filenameRaw = result.GetValue(filenameOption);
                var query = result.GetValue(queryOption) ?? "*";
                var includeHeaders = result.GetValue(includeHeadersOption);
                var format = result.GetValue(formatOption);
                var proxyHost = result.GetValue(proxyHostOption);
                var proxyPort = result.GetValue(proxyPortOption);
                var filename = !string.IsNullOrWhiteSpace(filenameRaw)
                    ? filenameRaw
                    : Path.Combine(Directory.GetCurrentDirectory(), $"Export-{DateTimeOffset.Now.Ticks}.{format.ToString().ToLower()}");

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;

                if (format == ExportFormat.Csv && includeHeaders) AnsiConsole.MarkupLine("[#ffc936]Including headers is not supported when exporting to CSV[/]");

                var api = Api(resolvedKey, proxyHost, proxyPort);
                try
                {
                    var startResult = await api.Messages.GetAllAsync(logId.ToString(), 0, 1, query, dateFrom, dateTo, includeHeaders);
                    if (startResult == null || startResult.Total == null || startResult.Total.Value == 0)
                    {
                        AnsiConsole.MarkupLine("[#ffc936]Could not find any messages for this API key and log ID combination[/]");
                        return;
                    }

                    int messSum = startResult.Total.Value;

                    if (File.Exists(filename)) File.Delete(filename);

                    await AnsiConsole
                        .Progress()
                        .StartAsync(async ctx =>
                        {
                            var task = ctx.AddTask("Exporting log messages", new ProgressTaskSettings
                            {
                                MaxValue = messSum,
                            });

                            using (var w = new StreamWriter(filename))
                            {
                                string? searchAfter = null;
                                var firstMessage = true;

                                if (format == ExportFormat.Json) w.WriteLine("[");

                                using var csv = format == ExportFormat.Csv ? new CsvWriter(w, CultureInfo.InvariantCulture) : null;
                                if (csv != null)
                                {
                                    csv.WriteHeader<MessageOverview>();
                                    csv.NextRecord();
                                }

                                while (true)
                                {
                                    var response = await api.Messages.GetAllAsync(
                                        logId.ToString(),
                                        pageSize: 100,
                                        query: query,
                                        from: dateFrom,
                                        to: dateTo,
                                        includeHeaders: includeHeaders,
                                        searchAfter: searchAfter
                                    );

                                    if (response.Messages.Count == 0)
                                    {
                                        task.Increment(task.MaxValue - task.Value);
                                        task.StopTask();
                                        break;
                                    }

                                    foreach (MessageOverview message in response.Messages)
                                    {
                                        if (format == ExportFormat.Json)
                                        {
                                            if (!firstMessage) w.WriteLine(",");
                                            firstMessage = false;
                                            w.WriteLine(JToken.Parse(JsonConvert.SerializeObject(message)).ToString(Formatting.Indented));
                                        }
                                        else if (csv != null)
                                        {
                                            csv.WriteRecord(message);
                                            csv.NextRecord();
                                        }

                                        task.Increment(1);
                                    }

                                    searchAfter = response.SearchAfter;
                                }

                                if (format == ExportFormat.Json) w.WriteLine("]");
                            }

                            task.StopTask();
                        });

                    AnsiConsole.MarkupLine($"[green]Done with export to [/][grey]{filename}[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return exportCommand;
        }

        private enum ExportFormat
        {
            Json,
            Csv,
        }
    }
}
