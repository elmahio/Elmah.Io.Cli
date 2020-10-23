using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using ShellProgressBar;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Elmah.Io.Cli
{
    class DataloaderCommand : CommandBase
    {
        const string DotNetStackTrace = @"Elmah.Io.TestException: This is a test exception that can be safely ignored.
crosoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Rethrow(ResourceExecutedContextSealed context)
crosoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)
crosoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.InvokeFilterPipelineAsync()
crosoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Logged|17_1(ResourceInvoker invoker)
crosoft.AspNetCore.Routing.EndpointMiddleware.<Invoke>g__AwaitRequestTask|6_0(Endpoint endpoint, Task requestTask, ILogger logger)
mah.Io.Startup.<>c.<<Configure>b__9_1>d.MoveNext() in c:\elmah.io\src\Elmah.Io\Startup.cs:line 364";

        internal static Command Create()
        {
            var dataloaderCommand = new Command("dataloader")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<Guid>("--logId", "The log ID of the log to import messages into")
                {
                    IsRequired = true
                }
            };
            dataloaderCommand.Description = "Load 50 log messages into the specified log";
            dataloaderCommand.Handler = CommandHandler.Create<string, Guid>((apiKey, logId) =>
            {
                var api = new ElmahioAPI(new ApiKeyCredentials(apiKey));
                var random = new Random();
                var yesterday = DateTime.UtcNow.AddDays(-1);
                var options = new ProgressBarOptions
                {
                    ProgressCharacter = '=',
                    ProgressBarOnBottom = false,
                    ForegroundColorDone = ConsoleColor.Green,
                    ForegroundColor = ConsoleColor.White
                };
                var numberOfMessages = 50;
                using (var pbar = new ProgressBar(numberOfMessages, "Loading log messages", options))
                {
                    for (var i = 0; i < numberOfMessages; i++)
                    {
                        var r = random.NextDouble();
                        api.Messages.CreateAndNotify(logId, new CreateMessage
                        {
                            //Application = "Elmah.Io.DataLoader",
                            Cookies = new[]
                            {
                            new Item("ASP.NET_SessionId", "lm5lbj35ehweehwha2ggsehh"),
                            new Item("_ga", "GA1.3.1580453215.1783132008"),
                        },
                            DateTime = yesterday.AddMinutes(random.Next(1440)),
                            Detail = DotNetStackTrace,
                            Form = new[]
                            {
                            new Item ("Username", "Joshua"),
                            new Item ("Password", "********"),
                        },
                            QueryString = new[]
                            {
                            new Item("logid", logId.ToString())
                        },
                            ServerVariables = new[]
                            {
                            new Item("REMOTE_ADDR", "1.1.1.1"),
                            new Item("CERT_KEYSIZE", "256"),
                            new Item("CONTENT_LENGTH", "0"),
                            new Item("QUERY_STRING", "logid=" + logId),
                            new Item("REQUEST_METHOD", Method(r)),
                            new Item("HTTP_USER_AGENT", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.75 Safari/537.36"),
                            new Item("HTTP_CF_IPCOUNTRY", "AU"),
                        },
                            Hostname = "Web01",
                            Severity = Severity(r),
                            Source = "Elmah.Io.Cli.exe",
                            StatusCode = StatusCode(r),
                            Title = Title(r),
                            Type = Type(r),
                            Url = Url(r),
                            Method = Method(r),
                            User = User(r),
                            Version = "1.1.0",
                            Application = "Dataloader",
                        });
                        pbar.Tick();
                    }
                }
            });

            return dataloaderCommand;
        }

        private static string Method(double random)
        {
            if (random > 0.5) return "POST";
            if (random > 0.2) return "GET";
            return null;
        }

        private static string Url(double random)
        {
            if (random > 0.5) return "/api/process";
            if (random > 0.2) return "/api/test";
            return null;
        }

        private static string Type(double random)
        {
            if (random > 0.5) return "System.NullReferenceException";
            if (random > 0.2) return "System.Net.HttpException";
            return null;
        }

        private static string Title(double random)
        {
            if (random > 0.5) return "Object reference not set to an instance of an object.";
            if (random > 0.2)
                return "The controller for path '/api/test' was not found or does not implement IController.";
            return "Processing request";
        }

        private static int? StatusCode(double random)
        {
            if (random > 0.5) return 500;
            if (random > 0.2) return 404;
            return null;
        }

        private static string Severity(double random)
        {
            if (random > 0.5) return "Error";
            if (random > 0.2) return "Warning";
            return "Information";
        }

        private static string User(double random)
        {
            if (random > 0.7) return "thomas@elmah.io";
            if (random > 0.4) return "info@elmah.io";
            return null;
        }
    }
}
