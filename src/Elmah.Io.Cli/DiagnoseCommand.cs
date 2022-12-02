using Elmah.Io.Client;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Elmah.Io.Cli
{
    class DiagnoseCommand : CommandBase
    {
        internal static Command Create()
        {
            var diagnoseCommand = new Command("diagnose")
            {
                new Option<string>("--directory", () => Directory.GetCurrentDirectory(), "The root directory to check")
            };
            diagnoseCommand.Description = "Diagnose potential problems with an elmah.io installation";
            diagnoseCommand.Handler = CommandHandler.Create<string>((directory) =>
            {
                var rootDir = new DirectoryInfo(directory);
                if (!rootDir.Exists)
                {
                    AnsiConsole.MarkupLine($"[red]Unknown directory: [/][grey]{directory ?? "Not specified"}[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"Running diagnose in [grey]{directory}[/]");

                var ignoreDirs = new[] { ".git", ".github", ".vs", ".vscode", "bin", "obj", "packages", "node_modules" };

                var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
                var filesWithPackages = rootDir
                    .EnumerateFiles("*.csproj", options)
                    .Where(f => !ignoreDirs.Contains(f.DirectoryName))
                    .Concat(rootDir.EnumerateFiles("packages.config", options).Where(f => !ignoreDirs.Contains(f.DirectoryName)));

                if (filesWithPackages.Count() == 0)
                {
                    AnsiConsole.MarkupLine("[red]No project or packages files found[/]");
                    return;
                }

                foreach (var packageFile in filesWithPackages)
                {
                    var packagesFound = FindPackages(packageFile);

                    if (packagesFound.ContainsKey("Elmah.Io.AspNetCore"))
                        DiagnoseAspNetCore(packageFile, packagesFound);

                    if (packagesFound.ContainsKey("Elmah.Io.Extensions.Logging"))
                        DiagnoseExtensionsLogging(packageFile, packagesFound);

                    if (packagesFound.ContainsKey("Elmah.Io") || packagesFound.ContainsKey("Elmah.Io.Mvc") || packagesFound.ContainsKey("Elmah.Io.WebApi") || packagesFound.ContainsKey("Elmah.Io.AspNet"))
                        DiagnoseElmahIo(packageFile, packagesFound);

                    if (packagesFound.ContainsKey("Elmah.Io.Log4Net"))
                        DiagnoseLog4Net(packageFile, packagesFound);

                    if (packagesFound.ContainsKey("Elmah.Io.NLog"))
                        DiagnoseNLog(packageFile, packagesFound);

                    if (packagesFound.ContainsKey("Serilog.Sinks.ElmahIo"))
                        DiagnoseSerilog(packageFile, packagesFound);
                }
            });

            return diagnoseCommand;
        }

        private static void DiagnoseLog4Net(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io.Log4Net[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Elmah.Io.Log4Net");

            var projectDir = packageFile.Directory;
            var webConfigPath = Path.Combine(projectDir.FullName, "web.config");
            var appConfigPath = Path.Combine(projectDir.FullName, "app.config");
            var log4netConfigPath = Path.Combine(projectDir.FullName, "log4net.config");
            var foundElmahIoConfig = false;

            string fileContent = null;

            bool FindConfig(string file)
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
                ValidateXmlAgainstSchema("log4net.config", fileContent, ("", "https://elmah.io/schemas/log4net.xsd"));
            }

            if (!foundElmahIoConfig)
                ReportError("log4net configuration for the elmah.io appender could not be found.");

            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                var apiKey = LookupString(fileContent, "type=\"elmah.io.log4net.ElmahIoAppender, elmah.io.log4net\"", "apiKey value=\"", 32);
                var logId = LookupString(fileContent, "type=\"elmah.io.log4net.ElmahIoAppender, elmah.io.log4net\"", "logId value=\"", 36);

                DiagnoseKeys(apiKey, logId);
            }
        }

        private static void DiagnoseSerilog(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Serilog.Sinks.ElmahIo[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Serilog.Sinks.ElmahIo");

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

                    DiagnoseKeys(apiKey, logId);

                    break;
                }
            }

            if (!foundElmahIoConfig)
                ReportError("Serilog configuration for the elmah.io sink could not be found.");
        }

        private static void DiagnoseNLog(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io.NLog[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Elmah.Io.NLog");

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
                AnsiConsole.MarkupLine("Validating nlog.config. Any errors may be resolved by changing the target type from [grey]elmah.io[/] to [grey]elmahio:elmah.io[/].");
                ValidateXmlAgainstSchema(
                    "nlog.config",
                    fileContent,
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

                DiagnoseKeys(apiKey, logId);
            }
        }

        private static void DiagnoseElmahIo(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Elmah.Io", "Elmah.Io.AspNet", "Elmah.Io.Mvc", "Elmah.Io.WebApi");

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

                DiagnoseKeys(apiKey, logId);
            }
            else
            {
                ReportError("Web.config file not found.");
            }
        }

        private static void DiagnoseExtensionsLogging(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io.Extensions.Logging[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Elmah.Io.Extensions.Logging");

            var projectDir = packageFile.Directory;
            var programPath = Path.Combine(projectDir.FullName, "Program.cs");

            string apiKey = null;
            string logId = null;

            if (File.Exists(programPath))
            {
                var programCs = File.ReadAllText(programPath);
                if (!programCs.Contains(".AddElmahIo("))
                    ReportError("A call to AddElmahIo was not found in Program.cs.");

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

            DiagnoseKeys(apiKey, logId);
        }

        private static void DiagnoseAspNetCore(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io.AspNetCore[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Elmah.Io.AspNetCore");

            var projectDir = packageFile.Directory;
            var startupPath = Path.Combine(projectDir.FullName, "Startup.cs");
            var programPath = Path.Combine(projectDir.FullName, "Program.cs");
            var foundElmahIoConfig = false;
            string fileWithElmahConfig = null;

            string apiKey = null;
            string logId = null;

            if (File.Exists(startupPath))
            {
                var startupCs = File.ReadAllText(startupPath);
                if (startupCs.Contains(".AddElmahIo(") && startupCs.Contains(".UseElmahIo("))
                {
                    foundElmahIoConfig = true;
                    fileWithElmahConfig = startupCs;

                    var apiKeyLookup = LookupString(startupCs, ".AddElmahIo(", ".ApiKey = \"", 32);
                    if (apiKeyLookup != null) apiKey = apiKeyLookup;
                    var logIdLookup = LookupString(startupCs, ".AddElmahIo(", ".LogId = new Guid(\"", 36);
                    if (logIdLookup != null) logId = logIdLookup;
                }
            }
            if (File.Exists(programPath))
            {
                var programCs = File.ReadAllText(programPath);
                if (programCs.Contains(".AddElmahIo(") && programCs.Contains(".UseElmahIo("))
                {
                    foundElmahIoConfig = true;
                    fileWithElmahConfig = programCs;

                    var apiKeyLookup = LookupString(programCs, ".AddElmahIo(", ".ApiKey = \"", 32);
                    if (apiKeyLookup != null) apiKey = apiKeyLookup;
                    var logIdLookup = LookupString(programCs, ".AddElmahIo(", ".LogId = new Guid(\"", 36);
                    if (logIdLookup != null) logId = logIdLookup;
                }
            }

            if (!foundElmahIoConfig)
                ReportError("A call to AddElmahIo and UseElmahIo was not found in Startup.cs or Program.cs.");

            else if (foundElmahIoConfig && !string.IsNullOrWhiteSpace(fileWithElmahConfig))
            {
                var index = fileWithElmahConfig.IndexOf(".UseElmahIo(");
                var useDeveloperExceptionPageIndex = fileWithElmahConfig.IndexOf(".UseDeveloperExceptionPage(");
                var useExceptionHandlerIndex = fileWithElmahConfig.IndexOf(".UseExceptionHandler(");
                var useAuthorizationIndex = fileWithElmahConfig.IndexOf(".UseAuthorization(");
                var useAuthenticationIndex = fileWithElmahConfig.IndexOf(".UseAuthentication(");
                var useEndpointsIndex = fileWithElmahConfig.IndexOf(".UseEndpoints(");
                var useMvcIndex = fileWithElmahConfig.IndexOf(".UseMvc(");
                var usePiranhaIndex = fileWithElmahConfig.IndexOf(".UsePiranha(");
                var useUmbracoIndex = fileWithElmahConfig.IndexOf(".UseUmbraco(");

                if (useDeveloperExceptionPageIndex != -1 && index < useDeveloperExceptionPageIndex)
                    ReportError("UseElmahIo must be called after UseDeveloperExceptionPage");
                else if (useExceptionHandlerIndex != -1 && index < useExceptionHandlerIndex)
                    ReportError("UseElmahIo must be called after UseExceptionHandler");
                else if (useAuthorizationIndex != -1 && index < useAuthorizationIndex)
                    ReportError("UseElmahIo must be called after UseAuthorization");
                else if (useAuthenticationIndex != -1 && index < useAuthenticationIndex)
                    ReportError("UseElmahIo must be called after UseAuthentication");
                else if (useEndpointsIndex != -1 && index > useEndpointsIndex)
                    ReportError("UseElmahIo must be called before UseEndpoints");
                else if (useMvcIndex != -1 && index > useMvcIndex)
                    ReportError("UseElmahIo must be called before UseMvc");
                else if (usePiranhaIndex != -1 && index > usePiranhaIndex)
                    ReportError("UseElmahIo must be called before UsePiranha");
                else if (useUmbracoIndex != -1 && index > useUmbracoIndex)
                    ReportError("UseElmahIo must be called before UseUmbraco");
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

            DiagnoseKeys(apiKey, logId);
        }

        private static void DiagnoseKeys(string apiKey, string logId)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(logId)) return;

            if (apiKey.Length != 32 || !Guid.TryParse(apiKey, out Guid _))
            {
                ReportError($"Invalid API key: {apiKey}");
                return;
            }

            if (logId.Length != 36 || !Guid.TryParse(logId, out Guid _))
            {
                ReportError($"Invalid log ID: {logId}");
                return;
            }

            var api = Api(apiKey);

            try
            {
                var diagnoseResult = api.Logs.Diagnose(logId);
                foreach (var result in diagnoseResult)
                {
                    ReportError(result);
                }
            }
            catch (ElmahIoClientException e)
            {
                ReportError(e.Message);
            }
        }

        private static string LookupString(string fileContent, string startAt, string start, int requiredLength)
        {
            var startAtIndex = fileContent.IndexOf(startAt, StringComparison.InvariantCultureIgnoreCase);
            if (startAtIndex == -1) return null;
            var startFound = fileContent.IndexOf(start, startAtIndex, StringComparison.InvariantCultureIgnoreCase);
            if (startFound == -1) return null;

            var beginAt = startFound + start.Length;

            return fileContent.Substring(beginAt, requiredLength);
        }

        private static Dictionary<string, string> FindPackages(FileInfo packageFile)
        {
            var document = XDocument.Load(packageFile.FullName);

            var packages = new Dictionary<string, string>();

            if (packageFile.Extension.Equals(".csproj", StringComparison.InvariantCultureIgnoreCase))
            {
                var ns = document.Root.GetDefaultNamespace();
                var project = document.Element(ns + "Project");
                var itemGroups = project
                    .Elements(ns + "ItemGroup")
                    .ToList();

                foreach (var pr in itemGroups
                    .SelectMany(ig => ig
                        .Elements(ns + "PackageReference")
                        .Where(pr => pr.Attribute("Include") != null && (pr.Attribute("Include").Value.StartsWith("Elmah.Io") || pr.Attribute("Include").Value.Equals("Serilog.Sinks.ElmahIo")))))
                {
                    packages.Add(pr.Attribute("Include").Value, pr.Attribute("Version") != null ? pr.Attribute("Version").Value : null);
                }
            }
            else if (packageFile.Name.Equals("packages.config", StringComparison.InvariantCultureIgnoreCase))
            {
                var packagesElement = document.Element("packages");
                var packageElements = packagesElement
                    .Elements("package")
                    .ToList();

                foreach (var package in packageElements)
                {
                    packages.Add(package.Attribute("id").Value, package.Attribute("version") != null ? package.Attribute("version").Value : null);
                }
            }

            return packages;
        }

        private static void DiagnosePackageVersion(Dictionary<string, string> packagesFound, params string[] packageNames)
        {
            foreach (var packageName in packageNames)
            {
                if (!packagesFound.ContainsKey(packageName)) continue;
                var packageVersion = packagesFound[packageName];
                if (string.IsNullOrWhiteSpace(packageVersion)) continue;

                if (packageVersion.StartsWith("1."))
                    ReportError("An old 1.x package is referenced. Install the newest version from NuGet.");
                else if (packageVersion.StartsWith("2."))
                    ReportError("An old 2.x package is referenced. Install the newest version from NuGet.");
            }
        }

        private static void ReportError(string message)
        {
            AnsiConsole.MarkupLine($"[red]- {message}[/]");
        }

        private static void ValidateXmlAgainstSchema(string fileName, string fileContent, params (string targetNamespace, string schemaUrl)[] schemaUrls)
        {
            if (string.IsNullOrWhiteSpace(fileContent)) return;

            var r = new StringReader(fileContent);

            var schema = new XmlSchemaSet();
            foreach (var schemaUrl in schemaUrls)
            {
                schema.Add(schemaUrl.targetNamespace, XmlReader.Create(schemaUrl.schemaUrl));
            }
            var xrs = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schema,
            };
            xrs.ValidationEventHandler += (o, s) =>
            {
                ReportError($"Error in {fileName}: {s.Message}");
            };

            using (XmlReader xr = XmlReader.Create(r, xrs))
            {
                try
                {
                    while (xr.Read()) { }
                }
                catch
                {
                }
            }
        }
    }
}
