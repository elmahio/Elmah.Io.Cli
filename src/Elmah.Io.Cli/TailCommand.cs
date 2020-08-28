using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
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
                var previous = new List<string>();
                while (true)
                {
                    Thread.Sleep(5000);
                    var now = DateTime.UtcNow;
                    var fiveSecondsBefore = from.AddSeconds(-5);
                    var result = api.Messages.GetAll(logId.ToString(), 0, 0, "*", fiveSecondsBefore, now, false);
                    if (result?.Total.HasValue != true || result.Total.Value == 0)
                    {
                        from = now;
                        previous.Clear();
                        continue;
                    };

                    int total = result.Total.Value;
                    int i = 0;
                    var messages = new List<MessageOverview>();
                    while (i < total)
                    {
                        var response = api.Messages.GetAll(logId.ToString(), i / 10, 10, "*", fiveSecondsBefore, now, false);
                        messages.AddRange(response.Messages.Where(msg => !previous.Contains(msg.Id)));
                        i += response.Messages.Count;
                    }

                    previous.Clear();

                    foreach (var message in messages.OrderBy(msg => msg.DateTime.Value))
                    {
                        SetColor(message.Severity);
                        Console.WriteLine($"{message.DateTime.Value}|{message.Severity}|{message.Title}");
                        previous.Add(message.Id);
                        ResetColor();
                    }

                    from = now;
                }

                return 0;
            });

            return logCommand;
        }

        private static void SetColor(string severity)
        {
            switch(severity)
            {
                case "Verbose":
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case "Debug":
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case "Information":
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case "Warning":
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case "Error":
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case "Fatal":
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
        }

        private static void ResetColor()
        {
            Console.ResetColor();
        }
    }
}
