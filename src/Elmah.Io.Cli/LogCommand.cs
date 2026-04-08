using Elmah.Io.Client;
using Spectre.Console;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class LogCommand : CommandBase
    {
        internal static Command Create()
        {
            return BuildCommand(deprecated: true);
        }

        internal static Command CreateSubcommand()
        {
            return BuildCommand(deprecated: false);
        }

        private static Command BuildCommand(bool deprecated)
        {
            var apiKeyOption = ApiKeyOption();
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log to send the log message to", Required = true };
            var applicationOption = new Option<string>("--application") { Description = "Used to identify which application logged this message. You can use this if you have multiple applications and services logging to the same log" };
            var detailOption = new Option<string>("--detail") { Description = "A longer description of the message. For errors this could be a stacktrace, but it's really up to you what to log in there." };
            var hostnameOption = new Option<string>("--hostname") { Description = "The hostname of the server logging the message." };
            var titleOption = new Option<string>("--title") { Description = "The textual title or headline of the message to log.", Required = true };
            var titleTemplateOption = new Option<string>("--titleTemplate") { Description = "The title template of the message to log. This property can be used from logging frameworks that supports structured logging like: \"{user} says {quote}\". In the example, titleTemplate will be this string and title will be \"Gilfoyle says It's not magic. It's talent and sweat\"." };
            var sourceOption = new Option<string>("--source") { Description = "The source of the code logging the message. This could be the assembly name." };
            var statusCodeOption = new Option<int>("--statusCode") { Description = "If the message logged relates to a HTTP status code, you can put the code in this property. This would probably only be relevant for errors, but could be used for logging successful status codes as well." };
            var dateTimeOption = new Option<DateTimeOffset?>("--dateTime") { Description = "The date and time in UTC of the message. If you don't provide us with a value in dateTime, we will set the current date and time in UTC." };
            var typeOption = new Option<string>("--type") { Description = "The type of message. If logging an error, the type of the exception would go into type but you can put anything in there, that makes sense for your domain." };
            var userOption = new Option<string>("--user") { Description = "An identification of the user triggering this message. You can put the users email address or your user key into this property." };
            var severityOption = new Option<string>("--severity") { Description = "An enum value representing the severity of this message. The following values are allowed: Verbose, Debug, Information, Warning, Error, Fatal." };
            var urlOption = new Option<string>("--url") { Description = "If message relates to a HTTP request, you may send the URL of that request. If you don't provide us with an URL, we will try to find a key named URL in serverVariables." };
            var methodOption = new Option<string>("--method") { Description = "If message relates to a HTTP request, you may send the HTTP method of that request. If you don't provide us with a method, we will try to find a key named REQUEST_METHOD in serverVariables." };
            var versionOption = new Option<string>("--version") { Description = "Versions can be used to distinguish messages from different versions of your software. The value of version can be a SemVer compliant string or any other syntax that you are using as your version numbering scheme." };
            var correlationIdOption = new Option<string>("--correlationId") { Description = "CorrelationId can be used to group similar log messages together into a single discoverable batch. A correlation ID could be a session ID from ASP.NET Core, a unique string spanning multiple microsservices handling the same request, or similar." };
            var categoryOption = new Option<string>("--category") { Description = "The category to set on the message. Category can be used to emulate a logger name when created from a logging framework." };
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();

            var logCommand = new Command(deprecated ? "log" : "create", $"{(deprecated ? "(deprecated) " : "")}Log a message to the specified log")
            {
                apiKeyOption, logIdOption, applicationOption, detailOption, hostnameOption, titleOption, titleTemplateOption, sourceOption, statusCodeOption,
                dateTimeOption, typeOption, userOption, severityOption, urlOption, methodOption, versionOption, correlationIdOption, categoryOption,
                proxyHostOption, proxyPortOption,
            };
            logCommand.SetAction(async result =>
            {
                if (deprecated)
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] 'elmahio log' is deprecated. Use 'elmahio messages create' instead.");

                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var application = result.GetValue(applicationOption);
                var detail = result.GetValue(detailOption);
                var hostname = result.GetValue(hostnameOption);
                var title = result.GetValue(titleOption);
                var titleTemplate = result.GetValue(titleTemplateOption);
                var source = result.GetValue(sourceOption);
                var statusCode = result.GetValue(statusCodeOption);
                var dateTime = result.GetValue(dateTimeOption);
                var type = result.GetValue(typeOption);
                var user = result.GetValue(userOption);
                var severity = result.GetValue(severityOption);
                var url = result.GetValue(urlOption);
                var method = result.GetValue(methodOption);
                var version = result.GetValue(versionOption);
                var correlationId = result.GetValue(correlationIdOption);
                var category = result.GetValue(categoryOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var api = Api(resolvedKey, host, port);
                try
                {
                    var message = await api.Messages.CreateAndNotifyAsync(logId, new CreateMessage
                    {
                        Application = application,
                        DateTime = dateTime ?? DateTimeOffset.UtcNow,
                        Detail = detail,
                        Hostname = hostname,
                        Method = method,
                        Severity = severity,
                        Source = source,
                        StatusCode = statusCode,
                        Title = title,
                        TitleTemplate = titleTemplate,
                        Type = type,
                        Url = url,
                        User = user,
                        Version = version,
                        CorrelationId = correlationId,
                        Category = category,
                    });
                    if (message != null)
                    {
                        AnsiConsole.MarkupLine($"[#0da58e]Message successfully logged to [/][grey]https://app.elmah.io/errorlog/search?logId={logId}&hidden=true&expand=true&filters=id:%22{message.Id}%22#searchTab[/]");
                        AnsiConsole.MarkupLine($"Fetch by running the 'elmahio messages get --logId {logId} --messageId {message.Id}' command");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[#e6614f]Message not logged[/]");
                    }
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return logCommand;
        }
    }
}
