using Elmah.Io.Client;
using System;
using System.Net.Http.Headers;

namespace Elmah.Io.Cli
{
    abstract class CommandBase
    {
        protected static IElmahioAPI Api(string apiKey)
        {
            var api = ElmahioAPI.Create(apiKey);
            api.HttpClient.Timeout = new TimeSpan(0, 1, 0);
            api.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Cli", typeof(CommandBase).Assembly.GetName().Version.ToString())));
            return api;
        }
    }
}
