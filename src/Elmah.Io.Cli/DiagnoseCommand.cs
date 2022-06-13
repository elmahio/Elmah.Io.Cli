using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Elmah.Io.Cli
{
    class DiagnoseCommand : CommandBase
    {
        internal static Command Create()
        {
            var diagnoseCommand = new Command("diagnose")
            {
                new Option<string>("--directory", () => new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "The root directory to check")
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
                    .Where(f => !ignoreDirs.Contains(f.DirectoryName));

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

            bool FindConfig(string file)
            {
                return file.Contains("<log4net", StringComparison.InvariantCultureIgnoreCase)
                    && file.Contains("name=\"ElmahIoAppender\"", StringComparison.InvariantCultureIgnoreCase)
                    && file.Contains("type=\"elmah.io.log4net.ElmahIoAppender, elmah.io.log4net\"", StringComparison.InvariantCultureIgnoreCase);
            }

            if (File.Exists(webConfigPath) && FindConfig(File.ReadAllText(webConfigPath)))
                foundElmahIoConfig = true;
            if (File.Exists(appConfigPath) && FindConfig(File.ReadAllText(appConfigPath)))
                foundElmahIoConfig = true;
            if (File.Exists(log4netConfigPath) && FindConfig(File.ReadAllText(log4netConfigPath)))
                foundElmahIoConfig = true;

            if (!foundElmahIoConfig)
                ReportError("log4net configuration for the elmah.io appender could not be found.");
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
                if (File.ReadAllText(csFile.FullName).Contains(".ElmahIo("))
                {
                    foundElmahIoConfig |= true;
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
            var log4netConfigPath = Path.Combine(projectDir.FullName, "nlog.config");
            var foundElmahIoConfig = false;

            bool FindConfig(string file)
            {
                return file.Contains("<nlog", StringComparison.InvariantCultureIgnoreCase)
                    && file.Contains("name=\"elmahio\"", StringComparison.InvariantCultureIgnoreCase)
                    && file.Contains("type=\"elmah.io\"", StringComparison.InvariantCultureIgnoreCase);
            }

            if (File.Exists(webConfigPath) && FindConfig(File.ReadAllText(webConfigPath)))
                foundElmahIoConfig = true;
            if (File.Exists(appConfigPath) && FindConfig(File.ReadAllText(appConfigPath)))
                foundElmahIoConfig = true;
            if (File.Exists(log4netConfigPath) && FindConfig(File.ReadAllText(log4netConfigPath)))
                foundElmahIoConfig = true;

            if (!foundElmahIoConfig)
                ReportError("NLog configuration for the elmah.io target could not be found.");
        }

        private static void DiagnoseElmahIo(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Elmah.Io", "Elmah.Io.AspNet", "Elmah.Io.Mvc", "Elmah.Io.WebApi");
        }

        private static void DiagnoseExtensionsLogging(FileInfo packageFile, Dictionary<string, string> packagesFound)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io.Extensions.Logging[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, "Elmah.Io.Extensions.Logging");

            var projectDir = packageFile.Directory;
            var programPath = Path.Combine(projectDir.FullName, "Program.cs");
            if (File.Exists(programPath))
            {
                var programCs = File.ReadAllText(programPath);
                if (!programCs.Contains(".AddElmahIo("))
                    ReportError("A call to AddElmahIo was not found in Program.cs.");
            }
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

            if (File.Exists(startupPath))
            {
                var startupCs = File.ReadAllText(startupPath);
                if (startupCs.Contains(".AddElmahIo(") && startupCs.Contains(".UseElmahIo("))
                {
                    foundElmahIoConfig = true;
                    fileWithElmahConfig = startupCs;
                }
            }
            if (File.Exists(programPath))
            {
                var programCs = File.ReadAllText(programPath);
                if (programCs.Contains(".AddElmahIo(") && programCs.Contains(".UseElmahIo("))
                {
                    foundElmahIoConfig = true;
                    fileWithElmahConfig = programCs;
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
        }

        private static Dictionary<string, string> FindPackages(FileInfo packageFile)
        {
            var document = XDocument.Load(packageFile.FullName);
            var ns = document.Root.GetDefaultNamespace();
            var project = document.Element(ns + "Project");
            var itemGroups = project
                .Elements(ns + "ItemGroup")
                .ToList();
            return itemGroups
                .SelectMany(ig => ig
                    .Elements(ns + "PackageReference")
                    .Where(pr => pr.Attribute("Include").Value.StartsWith("Elmah.Io")))
                .ToDictionary(pr => pr.Attribute("Include").Value, pr => pr.Attribute("Version").Value);
        }

        private static void DiagnosePackageVersion(Dictionary<string, string> packagesFound, params string[] packageNames)
        {
            foreach (var packageName in packageNames)
            {
                var packageVersion = packagesFound[packageName];
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
    }
}
