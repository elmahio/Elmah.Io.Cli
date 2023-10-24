using Elmah.Io.Client;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using System.Xml;

namespace Elmah.Io.Cli.Diagnose
{
    abstract class DiagnoseBase : CommandBase
    {
        protected static bool FoundError = false;

        protected static void DiagnoseKeys(string apiKey, string logId, bool verbose)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(logId))
            {
                if (verbose) AnsiConsole.MarkupLine($"[grey]Could not find API key or log ID[/]");
                return;
            }

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

        protected static string LookupString(string fileContent, string startAt, string start, int requiredLength)
        {
            var startAtIndex = fileContent.IndexOf(startAt, StringComparison.InvariantCultureIgnoreCase);
            if (startAtIndex == -1) return null;
            var startFound = fileContent.IndexOf(start, startAtIndex, StringComparison.InvariantCultureIgnoreCase);
            if (startFound == -1) return null;

            var beginAt = startFound + start.Length;

            return fileContent.Substring(beginAt, requiredLength);
        }

        protected static void DiagnosePackageVersion(Dictionary<string, string> packagesFound, bool verbose, params string[] packageNames)
        {
            var found = false;
            foreach (var packageName in packageNames)
            {
                if (!packagesFound.ContainsKey(packageName)) continue;
                found = true;
                var packageVersion = packagesFound[packageName];
                if (string.IsNullOrWhiteSpace(packageVersion)) continue;

                if (packageVersion.StartsWith("1."))
                    ReportError("An old 1.x package is referenced. Install the newest version from NuGet.");
                else if (packageVersion.StartsWith("2."))
                    ReportError("An old 2.x package is referenced. Install the newest version from NuGet.");
            }

            if (verbose && !found) AnsiConsole.MarkupLine($"[grey]None of the packages {string.Join(',', packageNames)} found in found packages[/]");
        }

        protected static void ReportError(string message)
        {
            AnsiConsole.MarkupLine($"[red]- {message}[/]");
            FoundError = true;
        }

        protected static void ValidateXmlAgainstSchema(string fileName, string fileContent, bool verbose, params (string targetNamespace, string schemaUrl)[] schemaUrls)
        {
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                if (verbose) AnsiConsole.MarkupLine($"[grey]Missing file content when validating against XML schema[/]");
                return;
            }

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
                catch (Exception e)
                {
                    if (verbose) AnsiConsole.MarkupLine($"[grey]{e.Message}[/]");
                }
            }
        }
    }
}
