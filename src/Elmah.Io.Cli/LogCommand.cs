﻿using Elmah.Io.Client;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Binding;

namespace Elmah.Io.Cli
{
    class LogCommand : CommandBase
    {
        internal static Command Create()
        {
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true,
            };
            var logIdOption = new Option<Guid>("--logId", "The ID of the log to send the log message to")
            {
                IsRequired = true
            };
            var applicationOption = new Option<string>("--application", "Used to identify which application logged this message. You can use this if you have multiple applications and services logging to the same log");
            var detailOption = new Option<string>("--detail", "A longer description of the message. For errors this could be a stacktrace, but it's really up to you what to log in there.");
            var hostnameOption = new Option<string>("--hostname", "The hostname of the server logging the message.");
            var titleOption = new Option<string>("--title", "The textual title or headline of the message to log.")
            {
                IsRequired = true
            };
            var titleTemplateOption = new Option<string>("--titleTemplate", "The title template of the message to log. This property can be used from logging frameworks that supports structured logging like: \"{user} says {quote}\". In the example, titleTemplate will be this string and title will be \"Gilfoyle says It's not magic. It's talent and sweat\".");
            var sourceOption = new Option<string>("--source", "The source of the code logging the message. This could be the assembly name.");
            var statusCodeOption = new Option<int>("--statusCode", "If the message logged relates to a HTTP status code, you can put the code in this property. This would probably only be relevant for errors, but could be used for logging successful status codes as well.");
            var dateTimeOption = new Option<DateTimeOffset?>("--dateTime", "The date and time in UTC of the message. If you don't provide us with a value in dateTime, we will set the current date and time in UTC.");
            var typeOption = new Option<string>("--type", "The type of message. If logging an error, the type of the exception would go into type but you can put anything in there, that makes sense for your domain.");
            var userOption = new Option<string>("--user", "An identification of the user triggering this message. You can put the users email address or your user key into this property.");
            var severityOption = new Option<string>("--severity", "An enum value representing the severity of this message. The following values are allowed: Verbose, Debug, Information, Warning, Error, Fatal");
            var urlOption = new Option<string>("--url", "If message relates to a HTTP request, you may send the URL of that request. If you don't provide us with an URL, we will try to find a key named URL in serverVariables.");
            var methodOption = new Option<string>("--method", "If message relates to a HTTP request, you may send the HTTP method of that request. If you don't provide us with a method, we will try to find a key named REQUEST_METHOD in serverVariables.");
            var versionOption = new Option<string>("--version", "Versions can be used to distinguish messages from different versions of your software. The value of version can be a SemVer compliant string or any other syntax that you are using as your version numbering scheme.");
            var correlationIdOption = new Option<string>("--correlationId", "CorrelationId can be used to group similar log messages together into a single discoverable batch. A correlation ID could be a session ID from ASP.NET Core, a unique string spanning multiple microsservices handling the same request, or similar.");
            
            var logCommand = new Command("log", "Log a message to the specified log")
            {
                apiKeyOption, logIdOption, applicationOption, detailOption, hostnameOption, titleOption, titleTemplateOption, sourceOption,
                statusCodeOption, dateTimeOption, typeOption, userOption, severityOption, urlOption, methodOption, versionOption, correlationIdOption
            };
            logCommand.SetHandler(async (apiKey, logId, messageModel) =>
            {
                var api = Api(apiKey);
                try
                {
                    var message = await api.Messages.CreateAndNotifyAsync(logId, new CreateMessage
                    {
                        Application = messageModel.Application,
                        DateTime = messageModel.DateTime ?? DateTimeOffset.UtcNow,
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
                        CorrelationId = messageModel.CorrelationId,
                    });
                    if (message != null)
                    {
                        AnsiConsole.MarkupLine($"[#0da58e]Message successfully logged to [/][grey]https://app.elmah.io/errorlog/search?logId={logId}&hidden=true&expand=true&filters=id:%22{message.Id}%22#searchTab[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[#e6614f]Message not logged[/]");
                    }
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
                }
            }, apiKeyOption, logIdOption, new MessageModelBinder(applicationOption, detailOption, hostnameOption, titleOption, titleTemplateOption, sourceOption, statusCodeOption, dateTimeOption, typeOption, userOption, severityOption, urlOption, methodOption, versionOption, correlationIdOption));

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
            public DateTimeOffset? DateTime { get; set; }
            public string Type { get; set; }
            public string User { get; set; }
            public string Severity { get; set; }
            public string Url { get; set; }
            public string Method { get; set; }
            public string Version { get; set; }
            public string CorrelationId { get; set; }
        }

        private class MessageModelBinder : BinderBase<MessageModel>
        {
            private Option<string> applicationOption;
            private Option<string> detailOption;
            private Option<string> hostnameOption;
            private Option<string> titleOption;
            private Option<string> titleTemplateOption;
            private Option<string> sourceOption;
            private Option<int> statusCodeOption;
            private Option<DateTimeOffset?> dateTimeOption;
            private Option<string> typeOption;
            private Option<string> userOption;
            private Option<string> severityOption;
            private Option<string> urlOption;
            private Option<string> methodOption;
            private Option<string> versionOption;
            private Option<string> correlationIdOption;

            public MessageModelBinder(Option<string> applicationOption, Option<string> detailOption, Option<string> hostnameOption, Option<string> titleOption, Option<string> titleTemplateOption, Option<string> sourceOption, Option<int> statusCodeOption, Option<DateTimeOffset?> dateTimeOption, Option<string> typeOption, Option<string> userOption, Option<string> severityOption, Option<string> urlOption, Option<string> methodOption, Option<string> versionOption, Option<string> correlationIdOption)
            {
                this.applicationOption = applicationOption;
                this.detailOption = detailOption;
                this.hostnameOption = hostnameOption;
                this.titleOption = titleOption;
                this.titleTemplateOption = titleTemplateOption;
                this.sourceOption = sourceOption;
                this.statusCodeOption = statusCodeOption;
                this.dateTimeOption = dateTimeOption;
                this.typeOption = typeOption;
                this.userOption = userOption;
                this.severityOption = severityOption;
                this.urlOption = urlOption;
                this.methodOption = methodOption;
                this.versionOption = versionOption;
                this.correlationIdOption = correlationIdOption;
            }

            protected override MessageModel GetBoundValue(BindingContext bindingContext)
            {
                return new MessageModel
                {
                    Application = bindingContext.ParseResult.GetValueForOption(applicationOption),
                    Detail = bindingContext.ParseResult.GetValueForOption(detailOption),
                    Hostname = bindingContext.ParseResult.GetValueForOption(hostnameOption),
                    Title = bindingContext.ParseResult.GetValueForOption(titleOption),
                    TitleTemplate = bindingContext.ParseResult.GetValueForOption(titleTemplateOption),
                    Source = bindingContext.ParseResult.GetValueForOption(sourceOption),
                    StatusCode = bindingContext.ParseResult.GetValueForOption(statusCodeOption),
                    DateTime = bindingContext.ParseResult.GetValueForOption(dateTimeOption),
                    Type = bindingContext.ParseResult.GetValueForOption(typeOption),
                    User = bindingContext.ParseResult.GetValueForOption(userOption),
                    Severity = bindingContext.ParseResult.GetValueForOption(severityOption),
                    Url = bindingContext.ParseResult.GetValueForOption(urlOption),
                    Method = bindingContext.ParseResult.GetValueForOption(methodOption),
                    Version = bindingContext.ParseResult.GetValueForOption(versionOption),
                    CorrelationId = bindingContext.ParseResult.GetValueForOption(correlationIdOption),
                };
            }
        }
    }
}
