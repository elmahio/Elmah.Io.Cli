using Elmah.Io.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System;
using System.CommandLine;
using System.IO;

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
            var exportCommand = new Command("export", "Export log messages from a specified log")
            {
                apiKeyOption, logIdOption, dateFromOption, dateToOption, filenameOption, queryOption, includeHeadersOption
            };
            exportCommand.SetHandler(async (apiKey, logId, dateFrom, dateTo, filename, query, includeHeaders) =>
            {
                var api = Api(apiKey);
                try
                {
                    var startResult = await api.Messages.GetAllAsync(logId.ToString(), 0, 1, query, dateFrom, dateTo, includeHeaders);
                    if (startResult == null)
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

                                if (File.Exists(filename)) File.Delete(filename);
                                using (StreamWriter w = File.AppendText(filename))
                                {
                                    string searchAfter = null;
                                    var firstMessage = true;
                                    w.WriteLine("[");
                                    while (true)
                                    {
                                        var response = await api.Messages.GetAllAsync(logId.ToString(), pageSize: 100, query: query, from: dateFrom, to: dateTo, includeHeaders: includeHeaders, searchAfter: searchAfter);
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

                        AnsiConsole.MarkupLine($"[green]Done with export to [/][grey]{filename}[/]");
                    }
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
                }
            }, apiKeyOption, logIdOption, dateFromOption, dateToOption, filenameOption, queryOption, includeHeadersOption);

            return exportCommand;
        }
    }
}
