using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;

namespace Elmah.Io.Cli.Diagnose
{
    internal class ElmahIoNLog : DiagnoseBase
    {
        private const string PackageName = "Elmah.Io.NLog";

        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string> packagesFound, bool verbose, Dictionary<string, List<string>> hints)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]{PackageName}[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, PackageName.ToLowerInvariant());

            var projectDir = packageFile.Directory;
            var webConfigPath = Path.Combine(projectDir.FullName, "web.config");
            var appConfigPath = Path.Combine(projectDir.FullName, "app.config");
            var nlogConfigPath = Path.Combine(projectDir.FullName, "nlog.config");
            var foundElmahIoConfig = false;

            string fileContent = null;

            bool FindConfig(string file)
            {
                return file.Contains("<nlog", StringComparison.InvariantCultureIgnoreCase)
                    && file.Contains("name=\"elmahio\"", StringComparison.InvariantCultureIgnoreCase)
                    && (file.Contains("type=\"elmah.io\"", StringComparison.InvariantCultureIgnoreCase)
                        || file.Contains("type=\"elmahio:elmah.io\"", StringComparison.InvariantCultureIgnoreCase));
            }

            if (File.Exists(webConfigPath) && FindConfig(File.ReadAllText(webConfigPath)))
            {
                foundElmahIoConfig = true;
                fileContent = File.ReadAllText(webConfigPath);
            }
            if (File.Exists(appConfigPath) && FindConfig(File.ReadAllText(appConfigPath)))
            {
                foundElmahIoConfig = true;
                fileContent = File.ReadAllText(appConfigPath);
            }
            if (File.Exists(nlogConfigPath) && FindConfig(File.ReadAllText(nlogConfigPath)))
            {
                foundElmahIoConfig = true;
                fileContent = File.ReadAllText(nlogConfigPath);
                AnsiConsole.MarkupLine("Validating [grey]nlog.config[/]. Any errors may be resolved by changing the target type from [invert]elmah.io[/] to [invert]elmahio:elmah.io[/].");
                ValidateXmlAgainstSchema(
                    "nlog.config",
                    fileContent,
                    verbose,
                    ("http://www.nlog-project.org/schemas/NLog.xsd", "http://www.nlog-project.org/schemas/NLog.xsd"),
                    ("http://www.nlog-project.org/schemas/NLog.Targets.Elmah.Io.xsd", "http://www.nlog-project.org/schemas/NLog.Targets.Elmah.Io.xsd"));
            }

            if (!foundElmahIoConfig)
                ReportError("NLog configuration for the elmah.io target could not be found.");

            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                var apiKey = LookupString(fileContent, "type=\"elmah.io\"", " apiKey=\"", 32);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    apiKey = LookupString(fileContent, "type=\"elmahio:elmah.io\"", " apiKey=\"", 32);
                }

                var logId = LookupString(fileContent, "type=\"elmah.io\"", " logId=\"", 36);
                if (string.IsNullOrWhiteSpace(logId))
                {
                    logId = LookupString(fileContent, "type=\"elmahio:elmah.io\"", " logId=\"", 36);
                }

                DiagnoseKeys(apiKey, logId, verbose);
            }
            else if (verbose)
            {
                AnsiConsole.MarkupLine("[grey]No file content found for log4net[/]");
            }

            if (!hints.ContainsKey(PackageName))
            {
                hints.Add(PackageName, new List<string>
                {
                    $"Make sure that your [grey]nlog.config[/] file is valid and contains the code for [rgb(13,165,142)]{PackageName}[/].",
                    "Always make sure to call [invert]LogManager.Shutdown()[/] before exiting the application to make sure that all log messages are flushed.",
                    "Extend the [invert]nlog[/] element with [invert]internalLogLevel=\"Warn\" internalLogFile=\"c:\\temp\\nlog-internal.log[/] and inspect that log file for any internal NLog errors.",
                });
            }
        }
    }
}
