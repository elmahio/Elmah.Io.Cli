using Elmah.Io.Client.Models;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Elmah.Io.Cli
{
    class LogCommand : CommandBase
    {
        internal static Command Create()
        {
            var logCommand = new Command("log")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<Guid>("--logId", "The ID of the log to send the log message to")
                {
                    IsRequired = true
                },
                new Option<string>("--application", "Used to identify which application logged this message. You can use this if you have multiple applications and services logging to the same log"),
                new Option<string>("--detail", "A longer description of the message. For errors this could be a stacktrace, but it's really up to you what to log in there."),
                new Option<string>("--hostname", "The hostname of the server logging the message."),
                new Option<string>("--title", "The textual title or headline of the message to log.") { IsRequired = true },
                new Option<string>("--titleTemplate", "The title template of the message to log. This property can be used from logging frameworks that supports structured logging like: \"{user} says {quote}\". In the example, titleTemplate will be this string and title will be \"Gilfoyle says It's not magic. It's talent and sweat\"."),
                new Option<string>("--source", "The source of the code logging the message. This could be the assembly name."),
                new Option<int>("--statusCode", "If the message logged relates to a HTTP status code, you can put the code in this property. This would probably only be relevant for errors, but could be used for logging successful status codes as well."),
                new Option<DateTime>("--dateTime", "The date and time in UTC of the message. If you don't provide us with a value in dateTime, we will set the current date and time in UTC."),
                new Option<string>("--type", "The type of message. If logging an error, the type of the exception would go into type but you can put anything in there, that makes sense for your domain."),
                new Option<string>("--user", "An identification of the user triggering this message. You can put the users email address or your user key into this property."),
                new Option<string>("--severity", "An enum value representing the severity of this message. The following values are allowed: Verbose, Debug, Information, Warning, Error, Fatal"),
                new Option<string>("--url", "If message relates to a HTTP request, you may send the URL of that request. If you don't provide us with an URL, we will try to find a key named URL in serverVariables."),
                new Option<string>("--method", "If message relates to a HTTP request, you may send the HTTP method of that request. If you don't provide us with a method, we will try to find a key named REQUEST_METHOD in serverVariables."),
                new Option<string>("--version", "Versions can be used to distinguish messages from different versions of your software. The value of version can be a SemVer compliant string or any other syntax that you are using as your version numbering scheme."),
            };
            logCommand.Description = "Log a message to the specified log";
            logCommand.Handler = CommandHandler.Create<string, Guid, MessageModel>(
                (apiKey, logId, messageModel) =>
                {
                    var api = Api(apiKey);
                    var message = api.Messages.CreateAndNotify(logId, new CreateMessage
                    {
                        Application = messageModel.Application,
                        DateTime = messageModel.DateTime ?? DateTime.UtcNow,
                        Detail = messageModel.Detail,
                        Hostname = messageModel.Hostname,
                        Method = messageModel.Method,
                        Severity = messageModel.Severity,
                        Source = messageModel.Source,
                        StatusCode = messageModel.StatusCode,
                        Title = messageModel.Title,
                        TitleTemplate = messageModel.TitleTemplate,
                        Type = messageModel.Type,
                        Url = messageModel.Url,
                        User = messageModel.User,
                        Version = messageModel.Version,
                    });
                    if (message != null)
                    {
                        AnsiConsole.MarkupLine($"[#0da58e]Message successfully logged to https://app.elmah.io/errorlog/search?logId={logId}&hidden=true&expand=true&filters=id:%22{message.Id}%22#searchTab[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[#e6614f]Message not logged[/]");
                    }
                });

            return logCommand;
        }

        private class MessageModel
        {
            public string Application { get; set; }
            public string Detail { get; set; }
            public string Hostname { get; set; }
            public string Title { get; set; }
            public string TitleTemplate { get; set; }
            public string Source { get; set; }
            public int? StatusCode { get; set; }
            public DateTime? DateTime { get; set; }
            public string Type { get; set; }
            public string User { get; set; }
            public string Severity { get; set; }
            public string Url { get; set; }
            public string Method { get; set; }
            public string Version { get; set; }
        }
    }
}
