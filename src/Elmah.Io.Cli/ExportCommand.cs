using Elmah.Io.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Binding;

namespace Elmah.Io.Cli
{
    class ExportCommand : CommandBase
    {
        internal static Command Create()
        {
            var today = DateTime.Today;
            var aWeekAgo = today.AddDays(-7);
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true,
            };
            var logIdOption = new Option<Guid>("--logId", "The ID of the log to export messages from")
            {
                IsRequired = true
            };
            var dateFromOption = new Option<DateTimeOffset>("--dateFrom", $"Defines the Date from which the logs start. Ex. \" --dateFrom {aWeekAgo:yyyy-MM-dd}\"")
            {
                IsRequired = true,
            };
            var dateToOption = new Option<DateTimeOffset>("--dateTo", $"Defines the Date from which the logs end. Ex. \" --dateTo {today:yyyy-MM-dd}\"")
            {
                IsRequired = true,
            };
            var filenameOption = new Option<string>(
                  "--filename",
                  getDefaultValue: () => Path.Combine(Directory.GetCurrentDirectory(), $"Export-{DateTimeOffset.Now.Ticks}.json"),
                  "Defines the path and filename of the file to export to. Ex. \" --filename C:\\myDirectory\\myFile.json\"");
            var queryOption = new Option<string>("--query", getDefaultValue: () => "*", "Defines the query that is passed to the API");
            var includeHeadersOption = new Option<bool>("--includeHeaders", "Include headers, cookies, etc. in output (will take longer to export)");
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var exportCommand = new Command("export", "Export log messages from a specified log")
            {
                apiKeyOption, logIdOption, dateFromOption, dateToOption, filenameOption, queryOption, includeHeadersOption, proxyHostOption, proxyPortOption
            };
            exportCommand.SetHandler(async (exportModel) =>
            {
                var api = Api(exportModel.ApiKey, exportModel.ProxyHost, exportModel.ProxyPort);
                try
                {
                    var startResult = await api.Messages.GetAllAsync(exportModel.LogId.ToString(), 0, 1, exportModel.Query, exportModel.DateFrom, exportModel.DateTo, exportModel.IncludeHeaders);
                    if (startResult == null || startResult.Total == null || startResult.Total.Value == 0)
                    {
                        AnsiConsole.MarkupLine("[#ffc936]Could not find any messages for this API key and log ID combination[/]");
                    }
                    else
                    {
                        int messSum = startResult.Total.Value;

                        await AnsiConsole
                            .Progress()
                            .StartAsync(async ctx =>
                            {
                                // Define tasks
                                var task = ctx.AddTask("Exporting log messages", new ProgressTaskSettings
                                {
                                    MaxValue = messSum,
                                });

                                if (File.Exists(exportModel.Filename)) File.Delete(exportModel.Filename);
                                using (StreamWriter w = File.AppendText(exportModel.Filename))
                                {
                                    string? searchAfter = null;
                                    var firstMessage = true;
                                    w.WriteLine("[");
                                    while (true)
                                    {
                                        var response = await api.Messages.GetAllAsync(exportModel.LogId.ToString(), pageSize: 100, query: exportModel.Query, from: exportModel.DateFrom, to: exportModel.DateTo, includeHeaders: exportModel.IncludeHeaders, searchAfter: searchAfter);
                                        if (response.Messages.Count == 0)
                                        {
                                            task.Increment(task.MaxValue - task.Value);
                                            task.StopTask();
                                            break;
                                        }
                                        foreach (MessageOverview message in response.Messages)
                                        {
                                            if (!firstMessage) w.WriteLine(",");
                                            firstMessage = false;
                                            w.WriteLine(JToken.Parse(JsonConvert.SerializeObject(message)).ToString(Formatting.Indented));
                                            task.Increment(1);
                                        }
                                        searchAfter = response.SearchAfter;
                                    }
                                    w.WriteLine("]");
                                }

                                task.StopTask();
                            });

                        AnsiConsole.MarkupLine($"[green]Done with export to [/][grey]{exportModel.Filename}[/]");
                    }
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            }, new ExportModelBinder(apiKeyOption, logIdOption, dateFromOption, dateToOption, filenameOption, queryOption, includeHeadersOption, proxyHostOption, proxyPortOption));

            return exportCommand;
        }

        private sealed class ExportModel(string? apiKey, Guid logId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, string? filename, string? query, bool includeHeaders, string? proxyHost, int? proxyPort)
        {
            public string ApiKey { get; set; } = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            public Guid LogId { get; set; } = logId;
            public DateTimeOffset? DateFrom { get; set; } = dateFrom;
            public DateTimeOffset? DateTo { get; set; } = dateTo;
            public string Filename { get; set; } = filename ?? throw new ArgumentNullException(nameof(filename));
            public string Query { get; set; } = query ?? throw new ArgumentNullException(nameof(query));
            public bool IncludeHeaders { get; set; } = includeHeaders;
            public string? ProxyHost { get; set; } = proxyHost;
            public int? ProxyPort { get; set; } = proxyPort;
        }

        private sealed class ExportModelBinder(Option<string> apiKeyOption, Option<Guid> logIdOption, Option<DateTimeOffset> dateFromOption, Option<DateTimeOffset> dateToOption, Option<string> filenameOption, Option<string> queryOption, Option<bool> includeHeadersOption, Option<string?> proxyHostOption, Option<int?> proxyPortOption) : BinderBase<ExportModel>
        {
            private readonly Option<string> apiKeyOption = apiKeyOption;
            private readonly Option<Guid> logIdOption = logIdOption;
            private readonly Option<DateTimeOffset> dateFromOption = dateFromOption;
            private readonly Option<DateTimeOffset> dateToOption = dateToOption;
            private readonly Option<string> filenameOption = filenameOption;
            private readonly Option<string> queryOption = queryOption;
            private readonly Option<bool> includeHeadersOption = includeHeadersOption;
            private readonly Option<string?> proxyHostOption = proxyHostOption;
            private readonly Option<int?> proxyPortOption = proxyPortOption;

            protected override ExportModel GetBoundValue(BindingContext bindingContext)
            {
                return new ExportModel(
                    bindingContext.ParseResult.GetValueForOption(apiKeyOption),
                    bindingContext.ParseResult.GetValueForOption(logIdOption),
                    bindingContext.ParseResult.GetValueForOption(dateFromOption),
                    bindingContext.ParseResult.GetValueForOption(dateToOption),
                    bindingContext.ParseResult.GetValueForOption(filenameOption),
                    bindingContext.ParseResult.GetValueForOption(queryOption),
                    bindingContext.ParseResult.GetValueForOption(includeHeadersOption),
                    bindingContext.ParseResult.GetValueForOption(proxyHostOption),
                    bindingContext.ParseResult.GetValueForOption(proxyPortOption));
            }
        }
    }
}
