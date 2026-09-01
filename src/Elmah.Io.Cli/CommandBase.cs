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

        protected static Option<string?> ApiKeyOption() =>
            new("--apiKey") { Description = $"An API key with permission to execute the command. If omitted, the {CredentialStore.ApiKeyEnvironmentVariable} environment variable is used, falling back to the key stored via 'elmahio login'." };

        protected static string? ResolveApiKey(string? provided)
        {
            var key = CredentialStore.GetApiKey(provided);
            if (key == null)
                AnsiConsole.MarkupLine($"[red]No API key provided. Pass --apiKey, set {CredentialStore.ApiKeyEnvironmentVariable}, or run 'elmahio login' first.[/]");
            return key;
        }

        protected static Option<bool> JsonOption() =>
            new("--json") { Description = "Output results as JSON instead of formatted text" };

        protected static Option<string?> ProxyHostOption() => new("--proxyHost") { Description = "A hostname or IP for a proxy to use to call elmah.io" };
        protected static Option<int?> ProxyPortOption() => new("--proxyPort") { Description = "A port number for a proxy to use to call elmah.io" };

        private static void Messages_OnMessageFail(object? sender, FailEventArgs e)
        {
            AnsiConsole.MarkupLine($"[red]{(e.Error?.Message ?? "An error happened when calling the elmah.io API")}[/]");
        }
    }
}
