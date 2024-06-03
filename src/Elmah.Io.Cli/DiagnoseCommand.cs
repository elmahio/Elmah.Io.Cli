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
        private static readonly string[] ignoreDirs = [".git", ".github", ".vs", ".vscode", "bin", "obj", "packages", "node_modules"];

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

                var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
                var filesWithPackages = rootDir
                    .EnumerateFiles("*.csproj", options)
                    .Where(f => !ignoreDirs.Contains(f.DirectoryName))
                    .Concat(rootDir.EnumerateFiles("packages.config", options).Where(f => !ignoreDirs.Contains(f.DirectoryName)));

                if (!filesWithPackages.Any())
                {
                    AnsiConsole.MarkupLine("[red]No project or packages files found[/]");
                    return;
                }

                var hintsByPackage = new Dictionary<string, List<string>>();

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .Start("Working...", ctx =>
                    {
                        foreach (var packageFile in filesWithPackages)
                        {
                            var packagesFound = FindPackages(packageFile, verbose);
                            if (verbose && packagesFound.Count == 0) AnsiConsole.MarkupLine("[grey]No packages found[/]");

                            if (packagesFound.ContainsKey("elmah.io.aspnetcore"))
                                ElmahIoAspNetCore.Diagnose(packageFile, packagesFound, verbose, hintsByPackage);

                            if (packagesFound.ContainsKey("elmah.io.extensions.logging"))
                                ElmahIoExtensionsLogging.Diagnose(packageFile, packagesFound, verbose, hintsByPackage);

                            if (packagesFound.ContainsKey("elmah.io") || packagesFound.ContainsKey("elmah.io.mvc") || packagesFound.ContainsKey("elmah.io.webapi") || packagesFound.ContainsKey("elmah.io.aspnet"))
                                ElmahIo.Diagnose(packageFile, packagesFound, verbose, hintsByPackage);

                            if (packagesFound.ContainsKey("elmah.io.log4net"))
                                ElmahIoLog4Net.Diagnose(packageFile, packagesFound, verbose, hintsByPackage);

                            if (packagesFound.ContainsKey("elmah.io.nlog"))
                                ElmahIoNLog.Diagnose(packageFile, packagesFound, verbose, hintsByPackage);

                            if (packagesFound.ContainsKey("serilog.sinks.elmahio"))
                                SerilogSinksElmahIo.Diagnose(packageFile, packagesFound, verbose, hintsByPackage);
                        }
                    });

                if (!FoundError)
                {
                    var rule = new Rule("[green]No issues found[/]")
                    {
                        Style = Style.Parse("green"),
                        Justification = Justify.Left
                    };
                    AnsiConsole.Write(rule);
                }

                Console.WriteLine();
                AnsiConsole.MarkupLine($"If you are still experiencing problems logging to elmah.io here are some things to try out.");
                Console.WriteLine();
                var generalTable = new Table().NoBorder().HideHeaders();
                generalTable.AddColumn(new TableColumn("").Width(2));
                generalTable.AddColumn(new TableColumn(""));

                generalTable.AddRow(":light_bulb:", "Make sure that all [rgb(13,165,142)]Elmah.Io.*[/] NuGet packages are referencing the newest stable version.");
                generalTable.AddRow(":light_bulb:", "Make sure that the API key is valid and allow the Messages Write permission.");
                generalTable.AddRow(":light_bulb:", "Make sure that your server has an outgoing internet connection and that it can communicate with [invert]api.elmah.io:443[/].");
                generalTable.AddRow(":light_bulb:", "Make sure that you didn't enable any Ignore filters or set up any Rules with an ignore action on the log.");
                generalTable.AddRow(":light_bulb:", "Make sure that you don't have any code catching all exceptions happening in your system and ignoring them (could be a logging filter, a piece of middleware, or similar).");
                generalTable.AddRow(":light_bulb:", "Make sure that you haven't reached the message limit included in your current plan. Your current usage can be viewed on the Subscription tab on organization settings.");
                AnsiConsole.Write(generalTable);

                foreach (var package in hintsByPackage)
                {
                    Console.WriteLine();
                    var rule = new Rule($":package: [rgb(13,165,142)]{package.Key}[/]")
                    {
                        Justification = Justify.Left
                    };
                    AnsiConsole.Write(rule);
                    var table = new Table().NoBorder().HideHeaders();
                    table.AddColumn(new TableColumn("").Width(2));
                    table.AddColumn(new TableColumn(""));
                    foreach (var hint in package.Value)
                    {
                        table.AddRow(":light_bulb:", hint);
                    }
                    AnsiConsole.Write(table);
                }
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
                    packages.Add(pr.Attribute("Include").Value.ToLower(), pr.Attribute("Version")?.Value);
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
                    packages.Add(package.Attribute("id").Value.ToLower(), package.Attribute("version")?.Value);
                }
            }

            if (verbose) AnsiConsole.MarkupLine($"[grey]Found the following packages: {string.Join(',', packages.Keys)}[/]");

            return packages;
        }
    }
}
