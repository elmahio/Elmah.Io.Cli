using Elmah.Io.Client;
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
            return api;
        }
    }
}
