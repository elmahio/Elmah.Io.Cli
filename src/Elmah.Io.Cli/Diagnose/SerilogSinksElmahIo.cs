using Spectre.Console;
using System.Collections.Generic;
using System.IO;

namespace Elmah.Io.Cli.Diagnose
{
    internal class SerilogSinksElmahIo : DiagnoseBase
    {
        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string> packagesFound, bool verbose)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Serilog.Sinks.ElmahIo[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, "serilog.sinks.elmahio");

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
        }
    }
}
