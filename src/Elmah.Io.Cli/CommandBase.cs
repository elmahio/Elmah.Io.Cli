using Elmah.Io.Client;
using Spectre.Console;
using System;
using System.Net.Http.Headers;

namespace Elmah.Io.Cli
{
    abstract class CommandBase
    {
        internal static string _assemblyVersion = typeof(CommandBase).Assembly.GetName().Version.ToString();

        protected static IElmahioAPI Api(string apiKey)
        {
            var api = ElmahioAPI.Create(apiKey, new ElmahIoOptions
            {
                Timeout = new TimeSpan(0, 1, 0),
                UserAgent = new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Cli", _assemblyVersion)).ToString(),
            });
            api.Messages.OnMessageFail += Messages_OnMessageFail;
            return api;
        }

        private static void Messages_OnMessageFail(object sender, FailEventArgs e)
        {
            AnsiConsole.MarkupLine($"[red]{(e.Error?.Message ?? "An error happened when calling the elmah.io API")}[/]");
        }
    }
}
