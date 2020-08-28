using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Threading;

namespace Elmah.Io.Cli
{
    class TailCommand : CommandBase
    {
        internal static Command Create()
        {
            var logCommand = new Command("tail")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<Guid>("--logId", "The ID of the log to send the log message to")
                {
                    IsRequired = true
                },
            };
            logCommand.Description = "Tail log messages from a specified log";
            logCommand.Handler = CommandHandler.Create<string, Guid>((apiKey, logId) =>
            {
                var api = Api(apiKey);
                var from = DateTime.UtcNow;
                while (true)
                {
                    Thread.Sleep(5000);
                    var newNow = DateTime.UtcNow;
                    var result = api.Messages.GetAll(logId.ToString(), 0, 0, "*", from, newNow, false);
                    if (result?.Total.HasValue != true || result.Total.Value == 0)
                    {
                        from = newNow;
                        continue;
                    };

                    int total = result.Total.Value;
                    int i = 0;
                    while (i < total)
                    {
                        var respons = api.Messages.GetAll(logId.ToString(), i / 10, 10, "*", from, newNow, false);
                        foreach (MessageOverview message in respons.Messages)
                        {
                            Console.WriteLine($"{message.DateTime.Value}|{message.Severity}|{message.Title}");
                            i++;
                        }
                    }

                    from = newNow;
                }

                return 0;
            });

            return logCommand;
        }
    }
}
