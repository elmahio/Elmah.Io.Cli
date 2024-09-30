using Elmah.Io.Client;
using Spectre.Console;
using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;

namespace Elmah.Io.Cli
{
    abstract class CommandBase
    {
        internal static readonly string? _assemblyVersion = typeof(CommandBase).Assembly.GetName().Version?.ToString();

        protected static IElmahioAPI Api(string apiKey, string? proxyHost = null, int? proxyPort = null)
        {
            var options = new ElmahIoOptions
            {
                Timeout = new TimeSpan(0, 1, 0),
                UserAgent = new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Cli", _assemblyVersion ?? "1.0")).ToString(),
            };

            if (!string.IsNullOrWhiteSpace(proxyHost) && proxyPort.HasValue)
            {
                options.WebProxy = new WebProxy(proxyHost, proxyPort.Value);
            }

            var api = ElmahioAPI.Create(apiKey, options);
            api.Messages.OnMessageFail += Messages_OnMessageFail;
            return api;
        }

        protected static Option<string?> ProxyHostOption() => new("--proxyHost", "A hostname or IP for a proxy to use to call elmah.io");
        protected static Option<int?> ProxyPortOption() => new("--proxyPort", "A port number for a proxy to use to call elmah.io");

        private static void Messages_OnMessageFail(object? sender, FailEventArgs e)
        {
            AnsiConsole.MarkupLine($"[red]{(e.Error?.Message ?? "An error happened when calling the elmah.io API")}[/]");
        }
    }
}
