using Spectre.Console;
using System.Collections.Generic;
using System.IO;

namespace Elmah.Io.Cli.Diagnose
{
    internal class ElmahIoAspNetCore : DiagnoseBase
    {
        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string> packagesFound, bool verbose)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]Elmah.Io.AspNetCore[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, "elmah.io.aspnetcore");

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
                var mapControllerRouteIndex = fileWithElmahConfig.IndexOf(".MapControllerRoute(");
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
                else if (mapControllerRouteIndex != -1 && index > mapControllerRouteIndex)
                    ReportError("UseElmahIo must be called before MapControllerRoute");
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

            DiagnoseKeys(apiKey, logId, verbose);
        }
    }
}
