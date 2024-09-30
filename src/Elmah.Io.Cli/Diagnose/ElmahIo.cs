using Spectre.Console;

namespace Elmah.Io.Cli.Diagnose
{
    internal class ElmahIo : DiagnoseBase
    {
        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string?> packagesFound, bool verbose, Dictionary<string, List<string>> hints)
        {
            if (packageFile.DirectoryName == null) return;
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, "elmah.io", "elmah.io.aspnet", "elmah.io.mvc", "elmah.io.webapi");

            if (packagesFound.ContainsKey("elmah.bootstrapper"))
                ReportError("elmah.io cannot be configured using ELMAH Bootstrapper (remove the elmah.bootstrapper NuGet package).");

            var webConfigPath = Path.Combine(packageFile.DirectoryName, "web.config");
            if (File.Exists(webConfigPath))
            {
                var webConfig = File.ReadAllText(webConfigPath);
                if (!webConfig.Contains("<sectionGroup name=\"elmah\""))
                    ReportError("No section group named 'elmah' found in web.config.");
                if (!webConfig.Contains("<section name=\"errorLog\""))
                    ReportError("No section named 'errorLog' found in web.config.");
                if (!webConfig.Contains("<add name=\"ErrorLog\" type=\"Elmah.ErrorLogModule, Elmah\""))
                    ReportError("No error log module found in httpModules or modules in web.config.");
                if (!webConfig.Contains("<elmah>"))
                    ReportError("No <elmah> element found in web.config.");
                if (!webConfig.Contains("<errorLog "))
                    ReportError("No <errorLog> element found in web.config.");
                if (!webConfig.Contains("type=\"Elmah.Io.ErrorLog, Elmah.Io\""))
                    ReportError("No <errorLog> with type ELmah.Io.ErrorLog type found in web.config.");

                var apiKey = LookupString(webConfig, "type=\"Elmah.Io.ErrorLog, Elmah.Io\"", " apiKey=\"", 32);
                var logId = LookupString(webConfig, "type=\"Elmah.Io.ErrorLog, Elmah.Io\"", " logId=\"", 36);

                DiagnoseKeys(apiKey, logId, verbose);
            }
            else
            {
                ReportError("Web.config file not found.");
            }

            if (!hints.ContainsKey("Elmah.Io"))
            {
                hints.Add("Elmah.Io",
                [
                    "Make sure that you have the [rgb(13,165,142)]elmah.corelibrary[/] NuGet package installed in the latest stable version.",
                    "Make sure that your [grey]web.config[/] file is valid and that it contains the ELMAH configuration.",
                ]);
            }
        }
    }
}
