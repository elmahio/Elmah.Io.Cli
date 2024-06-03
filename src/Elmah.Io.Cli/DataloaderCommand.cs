using Elmah.Io.Client;
using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class DataloaderCommand : CommandBase
    {
        const string Stack = @"   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Rethrow(ResourceExecutedContextSealed context)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.InvokeFilterPipelineAsync()
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Logged|17_1(ResourceInvoker invoker)
   at Microsoft.AspNetCore.Routing.EndpointMiddleware.<Invoke>g__AwaitRequestTask|6_0(Endpoint endpoint, Task requestTask, ILogger logger)
   at Elmah.Io.Startup.<>c.<<Configure>b__9_1>d.MoveNext() in c:\elmah.io\src\Elmah.Io\Startup.cs:line 364";
        const string DotNetStackTrace = @"Elmah.Io.TestException: This is a test exception that can be safely ignored.
" + Stack;

        internal static Command Create()
        {
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true,
            };
            var logIdOption = new Option<Guid>("--logId", "The log ID of the log to import messages into")
            {
                IsRequired = true
            };
            var dataloaderCommand = new Command("dataloader", "Load 50 log messages into the specified log")
            {
                apiKeyOption, logIdOption
            };
            dataloaderCommand.SetHandler(async (apiKey, logId) =>
            {
                var api = Api(apiKey);
                var random = new Random();
                var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
                await AnsiConsole
                    .Progress()
                    .StartAsync(async ctx =>
                    {
                        var numberOfMessages = 50;

                        // Define tasks
                        var task = ctx.AddTask("Loading log messages", new ProgressTaskSettings
                        {
                            MaxValue = numberOfMessages,
                        });

                        try
                        {
                            for (var i = 0; i < numberOfMessages; i++)
                            {
                                var r = random.NextDouble();
                                var dateTime = yesterday.AddMinutes(random.Next(1440));
                                await api.Messages.CreateAndNotifyAsync(logId, new CreateMessage
                                {
                                    //Application = "Elmah.Io.DataLoader",
                                    Cookies =
                                    [
                                        new Item("ASP.NET_SessionId", "lm5lbj35ehweehwha2ggsehh"),
                                        new Item("_ga", "GA1.3.1580453215.1783132008"),
                                    ],
                                    Data = Data(r),
                                    DateTime = dateTime,
                                    Detail = Detail(r),
                                    Form =
                                    [
                                        new Item ("Username", "Joshua"),
                                        new Item ("Password", "********"),
                                    ],
                                    QueryString =
                                    [
                                        new Item("logid", logId.ToString())
                                    ],
                                    ServerVariables =
                                    [
                                        new Item("REMOTE_ADDR", "1.1.1.1"),
                                        new Item("CERT_KEYSIZE", "256"),
                                        new Item("CONTENT_LENGTH", "0"),
                                        new Item("QUERY_STRING", "logid=" + logId),
                                        new Item("REQUEST_METHOD", Method(r)),
                                        new Item("HTTP_USER_AGENT", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.75 Safari/537.36"),
                                        new Item("HTTP_CF_IPCOUNTRY", "AU"),
                                        new Item("URL", Url(r)),
                                        new Item("HTTP_HOST", "foo.bar"),
                                    ],
                                    Breadcrumbs = Breadcrumbs(r, dateTime),
                                    Hostname = Hostname(r),
                                    Severity = Severity(r),
                                    Source = "Elmah.Io.Cli.exe",
                                    StatusCode = StatusCode(r),
                                    Title = Title(r),
                                    TitleTemplate = TitleTemplate(r),
                                    Type = Type(r),
                                    Url = Url(r),
                                    Method = Method(r),
                                    User = User(r),
                                    Version = "1.1.0",
                                    Application = "Dataloader",
                                    Category = Category(r),
                                });
                                task.Increment(1);
                            }
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
                        }
                        finally
                        {
                            task.StopTask();
                        }
                    });

                AnsiConsole.MarkupLine("[green]Successfully loaded [/][grey]50[/][green] log messages[/]");
            }, apiKeyOption, logIdOption);

            return dataloaderCommand;
        }

        private static Item[] Data(double random)
        {
            var items = new List<Item>
            {
                new("Father", "Stephen Falken"),
            };

            if (random > 0.5)
            {
                items.Add(new("X-ELMAHIO-EXCEPTIONINSPECTOR", Inspector("System.NullReferenceException", "Object reference not set to an instance of an object.")));
            }
            else if (random > 0.2)
            {
                items.Add(new("X-ELMAHIO-EXCEPTIONINSPECTOR", Inspector("System.Net.HttpException", "The controller for path '/api/test' was not found or does not implement IController.")));
            }

            return [.. items];
        }

        private static string Inspector(string type, string message)
        {
            return JsonConvert.SerializeObject(new
            {
                Type = type,
                Message = message,
                StackTrace = Stack,
                Source = "Elmah.Io.Cli.exe",
                Data = new List<KeyValuePair<string, string>>
                {
                    new("Data one", "Data one value"),
                    new("Data two", "Data two value")
                },
                ExceptionSpecific = new List<KeyValuePair<string, string>>
                {
                    new("Some arg", "Some value")
                }
            });
        }

        private static List<Breadcrumb> Breadcrumbs(double random, DateTimeOffset end)
        {
            if (random > 0.5) return
            [
                new Breadcrumb { DateTime = end.AddSeconds(-1), Action = "request", Message = "Navigating to URL", Severity = "Information" },
                new Breadcrumb { DateTime = end.AddSeconds(-2), Action = "submit", Message = "Submitting form", Severity = "Information" },
            ];

            return [];
        }

        private static string Hostname(double random)
        {
            if (random > 02) return "Web01";
            return null;
        }

        private static string Category(double random)
        {
            if (random > 0.5) return "Microsoft.Hosting.Lifetime";
            if (random > 0.2) return "Elmah.Io";
            return null;
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

        private static string Detail(double random)
        {
            if (random > 0.2) return DotNetStackTrace;
            return null;
        }

        private static string Title(double random)
        {
            if (random > 0.7) return "An unhandled exception has occurred while executing the \"request\".";
            if (random > 0.5) return "Object reference not set to an instance of an object.";
            if (random > 0.2)
                return "The controller for path '/api/test' was not found or does not implement IController.";
            return "Processing request";
        }

        private static string TitleTemplate(double random)
        {
            if (random > 0.7) return "An unhandled exception has occurred while executing the {action}.";
            return Title(random);
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
