using Spectre.Console;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class SourceMapCommand : CommandBase
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
            var logIdOption = new Option<Guid>("--logId") { Description = "The ID of the log which should contain the minified JavaScript and source map", Required = true };
            var pathOption = new Option<string>("--path") { Description = "An URL to the online minified JavaScript file", Required = true };
            var sourceMapOption = new Option<string>("--sourceMap") { Description = "The source map file. Only files with an extension of .map and content type of application/json will be accepted", Required = true };
            var minifiedJavaScriptOption = new Option<string>("--minifiedJavaScript") { Description = "The minified JavaScript file. Only files with an extension of .js and content type of text/javascript will be accepted", Required = true };
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var sourceMapCommand = new Command("sourcemap", $"{(deprecated ? "(deprecated) " : "")}Upload a source map and minified JavaScript")
            {
                apiKeyOption, logIdOption, pathOption, sourceMapOption, minifiedJavaScriptOption, proxyHostOption, proxyPortOption
            };
            sourceMapCommand.SetAction(async result =>
            {
                if (deprecated)
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] 'elmahio sourcemap' is deprecated. Use 'elmahio logs sourcemap' instead.");

                var apiKey = result.GetValue(apiKeyOption);
                var logId = result.GetValue(logIdOption);
                var path = result.GetValue(pathOption);
                var sourceMap = result.GetValue(sourceMapOption);
                var minifiedJavaScript = result.GetValue(minifiedJavaScriptOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                var resolvedKey = ResolveApiKey(apiKey);
                if (resolvedKey == null) return;
                var api = Api(resolvedKey, host, port);
                try
                {
                    if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri? uri))
                    {
                        AnsiConsole.MarkupLine($"[red]Unknown URL: {path}[/]");
                        return;
                    }

                    var sourceMapFileInfo = new FileInfo(sourceMap!);
                    var minifiedJavaScriptFileInfo = new FileInfo(minifiedJavaScript!);

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

                    await AnsiConsole
                        .Status()
                        .Spinner(new BugShotSpinner())
                        .StartAsync("Uploading...", async ctx =>
                        {
                            using var sourceMapStream = sourceMapFileInfo.OpenRead();
                            using var scriptStream = minifiedJavaScriptFileInfo.OpenRead();

                            await api.SourceMaps.CreateOrUpdateAsync(
                                logId.ToString(),
                                uri,
                                new Client.FileParameter(sourceMapStream, sourceMapFileInfo.Name, "application/json"),
                                new Client.FileParameter(scriptStream, minifiedJavaScriptFileInfo.Name, "text/javascript"));
                        });

                    AnsiConsole.MarkupLine($"[#0da58e]SourceMap successfully uploaded[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            });

            return sourceMapCommand;
        }
    }
}
