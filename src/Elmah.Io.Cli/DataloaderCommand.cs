using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Elmah.Io.Cli
{
    class DataloaderCommand : CommandBase
    {
        const string DotNetStackTrace = @"
System.Web.HttpException (0x80004005): The controller for path '/api/test' was not found or does not implement IController.
   at System.Web.Mvc.DefaultControllerFactory.GetControllerInstance(RequestContext requestContext, Type controllerType)
   at System.Web.Mvc.DefaultControllerFactory.CreateController(RequestContext requestContext, String controllerName)
   at System.Web.Mvc.MvcHandler.ProcessRequestInit(HttpContextBase httpContext, IController& controller, IControllerFactory& factory)
   at System.Web.Mvc.MvcHandler.BeginProcessRequest(HttpContextBase httpContext, AsyncCallback callback, Object state)
   at System.Web.Mvc.MvcHandler.BeginProcessRequest(HttpContext httpContext, AsyncCallback callback, Object state)
   at System.Web.Mvc.MvcHandler.System.Web.IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, Object extraData)
   at System.Web.HttpApplication.CallHandlerExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute()
   at System.Web.HttpApplication.CallHandlerExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute()
   at System.Web.HttpApplication.ExecuteStep(IExecutionStep step, Boolean& completedSynchronously)";
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
            dataloaderCommand.Description = "Load a configurable number of error messages into an elmah.io log";
            dataloaderCommand.Handler = CommandHandler.Create<string, Guid>((apiKey, logId) =>
            {
                var api = new ElmahioAPI(new ApiKeyCredentials(apiKey));
                var random = new Random();
                var yesterday = DateTime.UtcNow.AddDays(-1);
                for (var i = 0; i < 50; i++)
                {
                    var r = random.NextDouble();
                    api.Messages.CreateAndNotify(logId, new CreateMessage
                    {
                        //Application = "Elmah.Io.DataLoader",
                        Cookies = new[]
                        {
                            new Item("ASP.NET_SessionId", "lm5lbj35ehweehwha2ggsehh")
                        },
                        DateTime = yesterday.AddMinutes(random.Next(1440)),
                        Detail = DotNetStackTrace,
                        Form = new[]
                        {
                            new Item("Username", "ThomasArdal")
                        },
                        QueryString = new[]
                        {
                            new Item("logid", logId.ToString())
                        },
                        ServerVariables = new[]
                        {
                            new Item("CERT_KEYSIZE", "256"),
                            new Item("CONTENT_LENGTH", "0"),
                            new Item("QUERY_STRING", "logid=" + logId),
                            new Item("REQUEST_METHOD", "POST"),
                            new Item("HTTP_USER_AGENT", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36")
                        },
                        Hostname = "Web01",
                        Severity = Error(r),
                        Source = "Elmah.Io.DataLoader.exe",
                        StatusCode = StatusCode(r),
                        Title = Title(r),
                        Type = Type(r),
                        Url = Url(r),
                        User = User(r),
                        Version = "1.1.0",
                    });
                }
            });

            return dataloaderCommand;
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

        private static string Error(double random)
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
