using Spectre.Console;
using System.Collections.Generic;
using System.IO;

namespace Elmah.Io.Cli.Diagnose
{
    internal class ElmahIoExtensionsLogging : DiagnoseBase
    {
        private const string PackageName = "Elmah.Io.Extensions.Logging";

        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string> packagesFound, bool verbose, Dictionary<string, List<string>> hints)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]{PackageName}[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, PackageName.ToLowerInvariant());

            var projectDir = packageFile.Directory;
            var programPath = Path.Combine(projectDir.FullName, "Program.cs");

            string apiKey = null;
            string logId = null;

            if (File.Exists(programPath))
            {
                var programCs = File.ReadAllText(programPath);
                if (!programCs.Contains(".AddElmahIo(") || !programCs.Contains("using Elmah.Io.Extensions.Logging"))
                    ReportError($"A call to [invert]AddElmahIo[/] was not found in [invert]Program.cs[/]. Both [rgb(13,165,142)]{PackageName}[/] and [rgb(13,165,142)]Elmah.Io.AspNetCore[/] provide a method named [invert]AddElmahIo[/]. Make sure to call both if you have both packages installed.");

                var apiKeyLookup = LookupString(programCs, ".AddElmahIo(", ".ApiKey = \"", 32);
                if (apiKeyLookup != null) apiKey = apiKeyLookup;
                var logIdLookup = LookupString(programCs, ".AddElmahIo(", ".LogId = new Guid(\"", 36);
                if (logIdLookup != null) logId = logIdLookup;
            }

            // If we haven't found API key and log ID yet, try looking in appsettings.json
            var appSettingsJson = Path.Combine(projectDir.FullName, "appsettings.json");
            if (string.IsNullOrWhiteSpace(apiKey) && File.Exists(appSettingsJson))
            {
                var apiKeyLookup = LookupString(File.ReadAllText(appSettingsJson), "\"ElmahIo\":", "\"ApiKey\": \"", 32);
                if (apiKeyLookup != null) apiKey = apiKeyLookup;
            }
            if (string.IsNullOrWhiteSpace(logId) && File.Exists(appSettingsJson))
            {
                var logIdLookup = LookupString(File.ReadAllText(appSettingsJson), "\"ElmahIo\":", "\"LogId\": \"", 36);
                if (logIdLookup != null) logId = logIdLookup;
            }

            DiagnoseKeys(apiKey, logId, verbose);

            if (!hints.ContainsKey(PackageName))
            {
                hints.Add(PackageName,
                [
                    "Make sure that you are calling the [invert]AddElmahIo[/] method in the [grey]Program.cs[/] file.",
                    "Make sure that the logging configuration in both code and the [grey]appsettings.json[/] file allows the log severity you are expecting to see in elmah.io.",
                ]);
            }
        }
    }
}
