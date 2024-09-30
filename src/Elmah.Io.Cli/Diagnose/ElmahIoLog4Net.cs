using Spectre.Console;

namespace Elmah.Io.Cli.Diagnose
{
    internal class ElmahIoLog4Net : DiagnoseBase
    {
        private const string PackageName = "Elmah.Io.Log4Net";

        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string?> packagesFound, bool verbose, Dictionary<string, List<string>> hints)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]{PackageName}[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, PackageName.ToLowerInvariant());

            var projectDir = packageFile.Directory;
            if (projectDir == null) return;
            var webConfigPath = Path.Combine(projectDir.FullName, "web.config");
            var appConfigPath = Path.Combine(projectDir.FullName, "app.config");
            var log4netConfigPath = Path.Combine(projectDir.FullName, "log4net.config");
            var foundElmahIoConfig = false;

            string? fileContent = null;

            static bool FindConfig(string file)
            {
                return file.Contains("<log4net", StringComparison.InvariantCultureIgnoreCase)
                    && file.Contains("name=\"ElmahIoAppender\"", StringComparison.InvariantCultureIgnoreCase)
                    && file.Contains("type=\"elmah.io.log4net.ElmahIoAppender, elmah.io.log4net\"", StringComparison.InvariantCultureIgnoreCase);
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
            if (File.Exists(log4netConfigPath) && FindConfig(File.ReadAllText(log4netConfigPath)))
            {
                foundElmahIoConfig = true;
                fileContent = File.ReadAllText(log4netConfigPath);
                ValidateXmlAgainstSchema("log4net.config", fileContent, verbose, ("", "https://elmah.io/schemas/log4net.xsd"));
            }

            if (!foundElmahIoConfig)
                ReportError("log4net configuration for the elmah.io appender could not be found.");

            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                var apiKey = LookupString(fileContent, "type=\"elmah.io.log4net.ElmahIoAppender, elmah.io.log4net\"", "apiKey value=\"", 32);
                var logId = LookupString(fileContent, "type=\"elmah.io.log4net.ElmahIoAppender, elmah.io.log4net\"", "logId value=\"", 36);

                DiagnoseKeys(apiKey, logId, verbose);
            }
            else if (verbose)
            {
                AnsiConsole.MarkupLine("[grey]No file content found for log4net[/]");
            }

            if (!hints.ContainsKey(PackageName))
            {
                hints.Add(PackageName,
                [
                    $"Make sure that your [grey]log4net.config[/] file is valid and contains the code for [rgb(13,165,142)]{PackageName}[/].",
                    "Include the following app setting in the [grey]app.config[/]/[grey]web.config[/] file to enable log4net's internal logger and inspect the console for any errors: [invert]<add key=\"log4net.Internal.Debug\" value=\"true\"/>[/]."
                ]);
            }
        }
    }
}
