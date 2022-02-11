using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Elmah.Io.Cli
{
    class SourceMapCommand : CommandBase
    {
        internal static Command Create()
        {
            var sourceMapCommand = new Command("sourcemap", "Upload a source map and minified JavaScript")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<Guid>("--logId", "The ID of the log which should contain the minified JavaScript and source map")
                {
                    IsRequired = true,
                },
                new Option<string>("--path", "An URL to the online minified JavaScript file")
                {
                    IsRequired = true,
                },
                new Option<string>("--sourceMap", "The source map file. Only files with an extension of .map and content type of application/json will be accepted")
                {
                    IsRequired = true,
                },
                new Option<string>("--minifiedJavaScript", "The minified JavaScript file. Only files with an extension of .js and content type of text/javascript will be accepted")
                {
                    IsRequired = true,
                },
            };
            sourceMapCommand.Handler = CommandHandler.Create<string, Guid, string, string, string>((apiKey, logId, path, sourceMap, minifiedJavaScript) =>
            {
                var api = Api(apiKey);
                try
                {
                    if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri uri))
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

                    api.SourceMaps.CreateOrUpdate(
                        logId.ToString(),
                        uri,
                        new Client.FileParameter(sourceMapStream, sourceMapFileInfo.Name, "application/json"),
                        new Client.FileParameter(scriptStream, minifiedJavaScriptFileInfo.Name, "text/javascript"));

                    AnsiConsole.MarkupLine($"[#0da58e]SourceMap successfully uploaded[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                }
            });

            return sourceMapCommand;
        }
    }
}
