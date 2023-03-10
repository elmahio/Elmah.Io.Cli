using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Elmah.Io.Client;
using Spectre.Console;
using Tx.Windows;

namespace Elmah.Io.Cli
{
    class ImportCommand : CommandBase
    {
        public enum LogFileType
        {
            w3c,
            iis
        }

        internal static Command Create()
        {
            var today = DateTimeOffset.UtcNow;
            var aWeekAgo = today.AddDays(-7);
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true
            };
            var logIdOption = new Option<Guid>("--logId", "The ID of the log to import messages to")
            {
                IsRequired = true
            };
            var typeOption = new Option<LogFileType>("--type", "The type of log file to import. Use 'w3c' for W3C Extended Log File Format and 'iis' for IIS Log File Format")
            {
                IsRequired = true
            };
            var filenameOption = new Option<string>("--filename", "Defines the path and filename of the file to import from. Ex. \" --filename C:\\myDirectory\\log.txt\"")
            {
                IsRequired = true
            };
            var dateFromOption = new Option<DateTimeOffset?>("--dateFrom", $"Defines the Date from which the logs start. Ex. \" --dateFrom {aWeekAgo:yyyy-MM-dd}\"");
            var dateToOption = new Option<DateTimeOffset?>("--dateTo", $"Defines the Date from which the logs end. Ex. \" --dateTo {today:yyyy-MM-dd}\"");
            var importCommand = new Command("import", "Import log messages to a specified log")
            {
                apiKeyOption, logIdOption, typeOption, filenameOption, dateFromOption, dateToOption
            };
            importCommand.SetHandler(async (apiKey, logId, logFileType, filename, dateFrom, dateTo) =>
            {
                var api = Api(apiKey);
                try
                {
                    var filenameFileInfo = new FileInfo(filename);
                    if (!filenameFileInfo.Exists)
                    {
                        AnsiConsole.MarkupLine($"[red]Input file not found: {filename}[/]");
                        return;
                    }

                    if (logFileType == LogFileType.w3c)
                    {
                        await ImportW3CFile(api, filename, logId, dateFrom, dateTo);
                    }
                    else if (logFileType == LogFileType.iis)
                    {
                        await ImportIISFile(api, filename, logId, dateFrom, dateTo);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Unknown log file type: {logFileType}[/]");
                    }
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
                }
            }, apiKeyOption, logIdOption, typeOption, filenameOption, dateFromOption, dateToOption);

            return importCommand;
        }

        private static async Task ImportW3CFile(IElmahioAPI api, string filename, Guid logId, DateTimeOffset? from, DateTimeOffset? to)
        {
            using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var streamReader = new StreamReader(fileStream);
            var messages = new List<CreateMessage>();
            foreach (var line in W3CEnumerable
                .FromStream(streamReader)
                .Where(l => l.dateTime > (from ?? DateTimeOffset.MinValue) && l.dateTime < (to ?? DateTimeOffset.MaxValue)))
            {
                var parts = new List<string>();
                var message = new CreateMessage
                {
                    ServerVariables = new List<Item>(),
                };

                if (!string.IsNullOrWhiteSpace(line.c_ip))
                {
                    message.ServerVariables.Add(new Item("CLIENT-IP", line.c_ip));
                    parts.Add(line.c_ip);
                }

                if (!string.IsNullOrWhiteSpace(line.cs_method))
                {
                    message.Method = line.cs_method;
                    parts.Add(line.cs_method);
                }

                if (!string.IsNullOrWhiteSpace(line.cs_uri_stem))
                {
                    message.Url = line.cs_uri_stem;
                    parts.Add(line.cs_uri_stem);
                }

                if (int.TryParse(line.sc_status, out var status))
                {
                    message.StatusCode = status;
                    parts.Add(status.ToString());
                }

                if (line.dateTime != DateTime.MinValue) message.DateTime = line.dateTime;
                if (!string.IsNullOrWhiteSpace(line.cs_username)) message.User = line.cs_username;
                if (!string.IsNullOrWhiteSpace(line.cs_host)) message.Hostname = line.cs_host;
                if (!string.IsNullOrWhiteSpace(line.cs_User_Agent)) message.ServerVariables.Add(new Item("USER-AGENT", line.cs_User_Agent));

                var title = string.Join(" - ", parts);
                message.Title = !string.IsNullOrWhiteSpace(title) ? title : "Imported line from log file";

                messages.Add(message);

                if (messages.Count >= 50)
                {
                    await api.Messages.CreateBulkAndNotifyAsync(logId, messages);
                    messages.Clear();
                }
            }

            if (messages.Count > 0)
            {
                await api.Messages.CreateBulkAndNotifyAsync(logId, messages);
            }
        }

        private static async Task ImportIISFile(IElmahioAPI api, string filename, Guid logId, DateTimeOffset? from, DateTimeOffset? to)
        {
            using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var streamReader = new StreamReader(fileStream);
            var messages = new List<CreateMessage>();
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',');
                var clientIp = columns[0].Trim();
                var user = columns[1].Trim();
                var date = columns[2].Trim();
                var time = columns[3].Trim();
                var serverName = columns[5].Trim();
                var statusCode = columns[10].Trim();
                var method = columns[12].Trim();
                var url = columns[13].Trim();

                if (!DateTimeOffset.TryParse($"{date} {time}", CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AssumeLocal, out DateTimeOffset dateTime)) continue;
                dateTime = dateTime.ToUniversalTime();
                if (dateTime <= (from ?? DateTimeOffset.MinValue) || dateTime >= (to ?? DateTimeOffset.MaxValue)) continue;

                var message = new CreateMessage
                {
                    DateTime = dateTime,
                    ServerVariables = new List<Item>(),
                };
                var parts = new List<string>();

                if (!string.IsNullOrWhiteSpace(clientIp) && clientIp != "-")
                {
                    message.ServerVariables.Add(new Item("CLIENT-IP", clientIp));
                    parts.Add(clientIp);
                }

                if (!string.IsNullOrWhiteSpace(method) && method != "-")
                {
                    message.Method = method;
                    parts.Add(method);
                }

                if (!string.IsNullOrWhiteSpace(url) && url != "-" && Uri.TryCreate(url, UriKind.Relative, out var _))
                {
                    message.Url = url;
                    parts.Add(url);
                }

                if (!string.IsNullOrWhiteSpace(statusCode) && statusCode != "-" && int.TryParse(statusCode, out var status))
                {
                    message.StatusCode = status;
                    parts.Add(status.ToString());
                }

                if (!string.IsNullOrWhiteSpace(user) && user != "-") message.User = user;
                if (!string.IsNullOrWhiteSpace(serverName) && serverName != "-") message.Hostname = serverName;

                var title = string.Join(" - ", parts);
                message.Title = !string.IsNullOrWhiteSpace(title) ? title : "Imported line from log file";

                messages.Add(message);

                if (messages.Count >= 50)
                {
                    await api.Messages.CreateBulkAndNotifyAsync(logId, messages);
                    messages.Clear();
                }
            }

            if (messages.Count > 0)
            {
                await api.Messages.CreateBulkAndNotifyAsync(logId, messages);
            }
        }
    }
}
