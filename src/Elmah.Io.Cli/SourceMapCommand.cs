using Spectre.Console;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class SourceMapCommand : CommandBase
    {
        internal static Command Create()
        {
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true,
            };
            var logIdOption = new Option<Guid>("--logId", "The ID of the log which should contain the minified JavaScript and source map")
            {
                IsRequired = true,
            };
            var pathOption = new Option<string>("--path", "An URL to the online minified JavaScript file")
            {
                IsRequired = true,
            };
            var sourceMapOption = new Option<string>("--sourceMap", "The source map file. Only files with an extension of .map and content type of application/json will be accepted")
            {
                IsRequired = true,
            };
            var minifiedJavaScriptOption = new Option<string>("--minifiedJavaScript", "The minified JavaScript file. Only files with an extension of .js and content type of text/javascript will be accepted")
            {
                IsRequired = true,
            };
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var sourceMapCommand = new Command("sourcemap", "Upload a source map and minified JavaScript")
            {
                apiKeyOption, logIdOption, pathOption, sourceMapOption, minifiedJavaScriptOption, proxyHostOption, proxyPortOption
            };
            sourceMapCommand.SetHandler(async (apiKey, logId, path, sourceMap, minifiedJavaScript, host, port) =>
            {
                var api = Api(apiKey, host, port);
                try
                {
                    if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri? uri))
                    {
                        AnsiConsole.MarkupLine($"[red]Unknown URL: {path}[/]");
                        return;
                    }

                    var sourceMapFileInfo = new FileInfo(sourceMap);
                    var minifiedJavaScriptFileInfo = new FileInfo(minifiedJavaScript);


                    if (!sourceMapFileInfo.Exists)
                    {
                        AnsiConsole.MarkupLine($"[red]SourceMap file not found: {sourceMap}[/]");
                        return;
                    }

                    if (!minifiedJavaScriptFileInfo.Exists)
                    {
                        AnsiConsole.MarkupLine($"[red]Minified JavaScript file not found: {minifiedJavaScript}[/]");
                        return;
                    }

                    using var sourceMapStream = sourceMapFileInfo.OpenRead();
                    using var scriptStream = minifiedJavaScriptFileInfo.OpenRead();

                    await api.SourceMaps.CreateOrUpdateAsync(
                        logId.ToString(),
                        uri,
                        new Client.FileParameter(sourceMapStream, sourceMapFileInfo.Name, "application/json"),
                        new Client.FileParameter(scriptStream, minifiedJavaScriptFileInfo.Name, "text/javascript"));

                    AnsiConsole.MarkupLine($"[#0da58e]SourceMap successfully uploaded[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            }, apiKeyOption, logIdOption, pathOption, sourceMapOption, minifiedJavaScriptOption, proxyHostOption, proxyPortOption);

            return sourceMapCommand;
        }
    }
}
