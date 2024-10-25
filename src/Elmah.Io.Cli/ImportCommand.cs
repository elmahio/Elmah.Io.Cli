using System.CommandLine;
using System.Globalization;
using System.Text;
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
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var importCommand = new Command("import", "Import log messages to a specified log")
            {
                apiKeyOption, logIdOption, typeOption, filenameOption, dateFromOption, dateToOption, proxyHostOption, proxyPortOption
            };
            importCommand.SetHandler(async (apiKey, logId, logFileType, filename, dateFrom, dateTo, host, port) =>
            {
                var api = Api(apiKey, host, port);
                try
                {
                    var filenameFileInfo = new FileInfo(filename);
                    if (!filenameFileInfo.Exists)
                    {
                        AnsiConsole.MarkupLine($"[red]Input file not found: {filename}[/]");
                        return;
                    }

                    await AnsiConsole
                        .Status()
                        .Spinner(Spinner.Known.Star)
                        .StartAsync("Importing...", async ctx =>
                        {
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
                    });
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            }, apiKeyOption, logIdOption, typeOption, filenameOption, dateFromOption, dateToOption, proxyHostOption, proxyPortOption);

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
                    ServerVariables = [],
                    Data = [],
                    QueryString = [],
                    Cookies = [],
                };

                if (!string.IsNullOrWhiteSpace(line.c_ip) && line.c_ip != "-")
                {
                    message.ServerVariables.Add(new Item("Client-Ip", line.c_ip));
                    parts.Add(line.c_ip);
                }

                if (!string.IsNullOrWhiteSpace(line.cs_method) && line.cs_method != "-")
                {
                    message.Method = line.cs_method;
                    parts.Add(line.cs_method);
                }

                if (!string.IsNullOrWhiteSpace(line.cs_uri_stem) && line.cs_uri_stem != "-")
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

                if (!string.IsNullOrWhiteSpace(line.s_computername) && line.s_computername != "-")
                {
                    message.Hostname = line.s_computername;
                }
                else if (!string.IsNullOrWhiteSpace(line.cs_host) && line.cs_host != "-")
                {
                    message.Hostname = line.cs_host;
                }

                if (!string.IsNullOrWhiteSpace(line.cs_host) && line.cs_host != "-")
                {
                    var sb = new StringBuilder();
                    sb.Append(line.cs_host);
                    if (!string.IsNullOrWhiteSpace(line.s_port) && line.s_port != "-")
                    {
                        sb.Append(':').Append(line.s_port);
                    }

                    message.ServerVariables.Add(new Item("Host", sb.ToString()));
                }

                if (!string.IsNullOrWhiteSpace(line.cs_username) && line.cs_username != "-") message.User = line.cs_username;
                if (!string.IsNullOrWhiteSpace(line.cs_User_Agent) && line.cs_User_Agent != "-") message.ServerVariables.Add(new Item("User-Agent", line.cs_User_Agent));
                if (!string.IsNullOrWhiteSpace(line.s_sitename) && line.s_sitename != "-") message.Application = line.s_sitename;
                if (!string.IsNullOrWhiteSpace(line.s_ip) && line.s_ip != "-") message.ServerVariables.Add(new Item("X-Server-Ip", line.s_ip));
                if (!string.IsNullOrWhiteSpace(line.cs_version) && line.cs_version != "-") message.ServerVariables.Add(new Item("HttpVersion", line.cs_version));
                if (!string.IsNullOrWhiteSpace(line.cs_Referer) && line.cs_Referer != "-") message.ServerVariables.Add(new Item("Referer", line.cs_Referer));
                if (!string.IsNullOrWhiteSpace(line.sc_substatus) && line.sc_substatus != "-") message.Data.Add(new Item("Substatus", line.sc_substatus));
                if (!string.IsNullOrWhiteSpace(line.sc_win32_status) && line.sc_win32_status != "-") message.Data.Add(new Item("Win32 Status", line.sc_win32_status));
                if (!string.IsNullOrWhiteSpace(line.sc_bytes) && line.sc_bytes != "-") message.Data.Add(new Item("Bytes Sent", line.sc_bytes));
                if (!string.IsNullOrWhiteSpace(line.cs_bytes) && line.cs_bytes != "-") message.Data.Add(new Item("Bytes Received", line.cs_bytes));
                if (!string.IsNullOrWhiteSpace(line.time_taken) && line.time_taken != "-") message.Data.Add(new Item("Time Taken", line.time_taken));

                if (!string.IsNullOrWhiteSpace(line.cs_uri_query) && line.cs_uri_query != "-")
                {
                    var pairs = line.cs_uri_query.Split('&', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pair in pairs)
                    {
                        var keyAndValue = pair.Split('=');
                        if (keyAndValue.Length == 2)
                        {
                            message.QueryString.Add(new Item(keyAndValue[0], keyAndValue[1]));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(line.cs_Cookie) && line.cs_Cookie != "-")
                {
                    var pairs = line.cs_Cookie.Split(";+", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pair in pairs)
                    {
                        var keyAndValue = pair.Split('=');
                        if (keyAndValue.Length == 2)
                        {
                            message.Cookies.Add(new Item(keyAndValue[0], keyAndValue[1]));
                        }
                    }
                }

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
                var line = await streamReader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',');
                var clientIp = columns[0].Trim();
                var user = columns[1].Trim();
                var date = columns[2].Trim();
                var time = columns[3].Trim();
                var service = columns[4].Trim();
                var serverName = columns[5].Trim();
                var serverIp = columns[6].Trim();
                var timeTaken = columns[7].Trim();
                var clientBytesSent = columns[8].Trim();
                var serverBytesSent = columns[9].Trim();
                var statusCode = columns[10].Trim();
                var windowsStatusCode = columns[11].Trim();
                var method = columns[12].Trim();
                var url = columns[13].Trim();
                var parameters = columns[14].Trim();

                if (!DateTimeOffset.TryParse($"{date} {time}", CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AssumeLocal, out DateTimeOffset dateTime)) continue;
                dateTime = dateTime.ToUniversalTime();
                if (dateTime <= (from ?? DateTimeOffset.MinValue) || dateTime >= (to ?? DateTimeOffset.MaxValue)) continue;

                var message = new CreateMessage
                {
                    DateTime = dateTime,
                    ServerVariables = [],
                    Data = [],
                };
                var parts = new List<string>();

                if (!string.IsNullOrWhiteSpace(clientIp) && clientIp != "-")
                {
                    message.ServerVariables.Add(new Item("Client-Ip", clientIp));
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
                if (!string.IsNullOrWhiteSpace(service) && service != "-") message.Application = service;
                if (!string.IsNullOrWhiteSpace(serverIp) && serverIp != "-") message.Data.Add(new Item("Server IP address", serverIp));
                if (!string.IsNullOrWhiteSpace(timeTaken) && timeTaken != "-") message.Data.Add(new Item("Time taken", timeTaken));
                if (!string.IsNullOrWhiteSpace(clientBytesSent) && clientBytesSent != "-") message.Data.Add(new Item("Client bytes sent", clientBytesSent));
                if (!string.IsNullOrWhiteSpace(serverBytesSent) && serverBytesSent != "-") message.Data.Add(new Item("Server bytes sent", serverBytesSent));
                if (!string.IsNullOrWhiteSpace(windowsStatusCode) && windowsStatusCode != "-") message.Data.Add(new Item("Windows status code", windowsStatusCode));
                if (!string.IsNullOrWhiteSpace(parameters) && parameters != "-") message.Data.Add(new Item("Parameters", parameters));

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
