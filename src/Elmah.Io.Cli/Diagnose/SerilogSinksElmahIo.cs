using Spectre.Console;
using System.Collections.Generic;
using System.IO;

namespace Elmah.Io.Cli.Diagnose
{
    internal class SerilogSinksElmahIo : DiagnoseBase
    {
        private const string PackageName = "Serilog.Sinks.ElmahIo";

        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string> packagesFound, bool verbose, Dictionary<string, List<string>> hints)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]{PackageName}[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, PackageName.ToLowerInvariant());

            var projectDir = packageFile.Directory;
            var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
            var csFiles = projectDir.EnumerateFiles("*.cs", options);
            var foundElmahIoConfig = false;
            foreach (var csFile in csFiles)
            {
                var fileContent = File.ReadAllText(csFile.FullName);
                if (fileContent.Contains(".ElmahIo("))
                {
                    foundElmahIoConfig = true;

                    var apiKey = LookupString(fileContent, ".ElmahIo(", "ElmahIoSinkOptions(\"", 32);
                    var logId = LookupString(fileContent, ".ElmahIo(", ", new Guid(\"", 36);

                    DiagnoseKeys(apiKey, logId, verbose);

                    break;
                }
            }

            if (!foundElmahIoConfig)
                ReportError("Serilog configuration for the elmah.io sink could not be found.");

            if (!hints.ContainsKey(PackageName))
            {
                hints.Add(PackageName,
                [
                    $"Make sure that your Serilog configuration calls the [invert].ElmahIo[/] method to set up the sink.",
                    "Always make sure to call [invert]Log.CloseAndFlush()[/] before exiting the application to make sure that all log messages are flushed.",
                    "Set up Serilog's SelfLog to inspect any errors happening inside Serilog or the elmah.io sink: [invert]Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));[/].",
                ]);
            }
        }
    }
}
