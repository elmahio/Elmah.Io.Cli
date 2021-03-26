using Elmah.Io.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Elmah.Io.Cli
{
    class ExportCommand : CommandBase
    {
        internal static Command Create()
        {
            var today = DateTime.Today;
            var aWeekAgo = today.AddDays(-7);
            var exportCommand = new Command("export")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<Guid>("--logId", "The ID of the log to export messages from")
                {
                    IsRequired = true
                },
                new Option<DateTime>("--dateFrom", $"Defines the Date from which the logs start. Ex. \" --dateFrom {aWeekAgo:yyyy-MM-dd}\"")
                {
                    IsRequired = true,
                },
                new Option<DateTime>("--dateTo", $"Defines the Date from which the logs end. Ex. \" --dateTo {today:yyyy-MM-dd}\"")
                {
                    IsRequired = true,
                },
                new Option<string>(
                    "--filename",
                    getDefaultValue:() => Path.Combine(Directory.GetCurrentDirectory(), $"Export-{DateTime.Now.Ticks}.json"),
                    "Defines the path and filename of the file to export to. Ex. \" -Filename C:\\myDirectory\\myFile.json\""),
                new Option<string>("--query", getDefaultValue:() => "*", "Defines the query that is passed to the API"),
                new Option<bool>("--includeHeaders", "Include headers, cookies, etc. in output (will take longer to export)")
            };
            exportCommand.Description = "Export log messages from a specified log";
            exportCommand.Handler = CommandHandler.Create<string, Guid, DateTime, DateTime, string, string, bool>((apiKey, logId, dateFrom, dateTo, filename, query, includeHeaders) =>
            {
                var api = Api(apiKey);
                var startResult = api.Messages.GetAll(logId.ToString(), 0, 1, query, dateFrom, dateTo, includeHeaders);
                if (startResult == null)
                {
                    AnsiConsole.MarkupLine("[#ffc936]Could not find any messages for this API key and log ID combination[/]");
                }
                else
                {
                    int messSum = startResult.Total.Value;
                    if (messSum > 10000)
                    {
                        AnsiConsole.MarkupLine("[#ffc936]Query returned more than 10,000 messages. The exporter will cap at 10,000 messages. Consider using the -DateFrom, -DateTo, and/or the -Query parameters to limit the search result.[/]");
                        messSum = 10000;
                    }

                    AnsiConsole
                        .Progress()
                        .Start(ctx =>
                        {
                            // Define tasks
                            var task = ctx.AddTask("Exporting log messages", new ProgressTaskSettings
                            {
                                MaxValue = messSum,
                            });

                            if (File.Exists(filename)) File.Delete(filename);
                            using (StreamWriter w = File.AppendText(filename))
                            {
                                int i = 0;
                                w.WriteLine("[");
                                while (i < messSum)
                                {
                                    var respons = api.Messages.GetAll(logId.ToString(), i / 10, 10, query, dateFrom, dateTo, includeHeaders);
                                    foreach (Client.Models.MessageOverview message in respons.Messages)
                                    {
                                        w.WriteLine(JValue.Parse(JsonConvert.SerializeObject(message)).ToString(Formatting.Indented));
                                        i++;
                                        if (i != messSum) w.WriteLine(",");
                                        task.Increment(1);
                                    }
                                }
                                w.WriteLine("]");
                            }

                            task.StopTask();
                        });

                    AnsiConsole.MarkupLine($"[green]Done with export to [/][grey]{filename}[/]");
                }
            });

            return exportCommand;
        }
    }
}
