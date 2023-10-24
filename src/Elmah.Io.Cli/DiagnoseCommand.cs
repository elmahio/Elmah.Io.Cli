using Elmah.Io.Cli.Diagnose;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Elmah.Io.Cli
{
    class DiagnoseCommand : DiagnoseBase
    {
        internal static Command Create()
        {
            var directoryOption = new Option<string>("--directory", () => Directory.GetCurrentDirectory(), "The root directory to check");
            var verboseOption = new Option<bool>("--verbose", () => false, "Output verbose diagnostics to help debug problems");
            var diagnoseCommand = new Command("diagnose", "Diagnose potential problems with an elmah.io installation")
            {
                directoryOption,
                verboseOption,
            };
            diagnoseCommand.SetHandler((directory, verbose) =>
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

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .Start("Working...", ctx => {
                        foreach (var packageFile in filesWithPackages)
                        {
                            var packagesFound = FindPackages(packageFile, verbose);
                            if (verbose && packagesFound.Count == 0) AnsiConsole.MarkupLine("[grey]No packages found[/]");

                            if (packagesFound.ContainsKey("elmah.io.aspnetcore"))
                                ElmahIoAspNetCore.Diagnose(packageFile, packagesFound, verbose);

                            if (packagesFound.ContainsKey("elmah.io.extensions.logging"))
                                ElmahIoExtensionsLogging.Diagnose(packageFile, packagesFound, verbose);

                            if (packagesFound.ContainsKey("elmah.io") || packagesFound.ContainsKey("elmah.io.mvc") || packagesFound.ContainsKey("elmah.io.webapi") || packagesFound.ContainsKey("elmah.io.aspnet"))
                                ElmahIo.Diagnose(packageFile, packagesFound, verbose);

                            if (packagesFound.ContainsKey("elmah.io.log4net"))
                                ElmahIoLog4Net.Diagnose(packageFile, packagesFound, verbose);

                            if (packagesFound.ContainsKey("elmah.io.nlog"))
                                ElmahIoNLog.Diagnose(packageFile, packagesFound, verbose);

                            if (packagesFound.ContainsKey("serilog.sinks.elmahio"))
                                SerilogSinksElmahIo.Diagnose(packageFile, packagesFound, verbose);
                        }
                    });

                if (!FoundError) AnsiConsole.MarkupLine("[green]No issues found[/]");
            }, directoryOption, verboseOption);

            return diagnoseCommand;
        }

        private static Dictionary<string, string> FindPackages(FileInfo packageFile, bool verbose)
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
                        .Where(pr => pr.Attribute("Include") != null && (pr.Attribute("Include").Value.StartsWith("elmah.io", StringComparison.InvariantCultureIgnoreCase) || pr.Attribute("Include").Value.Equals("serilog.sinks.elmahio", StringComparison.InvariantCultureIgnoreCase)))))
                {
                    packages.Add(pr.Attribute("Include").Value.ToLower(), pr.Attribute("Version") != null ? pr.Attribute("Version").Value : null);
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
                    packages.Add(package.Attribute("id").Value.ToLower(), package.Attribute("version") != null ? package.Attribute("version").Value : null);
                }
            }

            if (verbose) AnsiConsole.MarkupLine($"[grey]Found the following packages: {string.Join(',', packages.Keys)}[/]");

            return packages;
        }
    }
}
