using Spectre.Console;

namespace Elmah.Io.Cli.Diagnose
{
    internal class ElmahIoAspNetCore : DiagnoseBase
    {
        private const string PackageName = "Elmah.Io.AspNetCore";

        internal static void Diagnose(FileInfo packageFile, Dictionary<string, string?> packagesFound, bool verbose, Dictionary<string, List<string>> hints)
        {
            AnsiConsole.MarkupLine($"Found [rgb(13,165,142)]{PackageName}[/] in [grey]{packageFile.FullName}[/].");
            DiagnosePackageVersion(packagesFound, verbose, PackageName.ToLowerInvariant());

            var projectDir = packageFile.Directory;
            if (projectDir == null) return;
            var startupPath = Path.Combine(projectDir.FullName, "Startup.cs");
            var programPath = Path.Combine(projectDir.FullName, "Program.cs");
            var foundElmahIoConfig = false;
            string? fileWithElmahConfig = null;

            string? apiKey = null;
            string? logId = null;

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
            {
                ReportError("A call to [invert]AddElmahIo[/] and [invert]UseElmahIo[/] was not found in [grey]Startup.cs[/] or [grey]Program.cs[/].");
            }
            else if (!string.IsNullOrWhiteSpace(fileWithElmahConfig))
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
                    ReportError("[invert]UseElmahIo[/] must be called after [invert]UseDeveloperExceptionPage[/]");
                else if (useExceptionHandlerIndex != -1 && index < useExceptionHandlerIndex)
                    ReportError("[invert]UseElmahIo[/] must be called after [invert]UseExceptionHandler[/]");
                else if (useAuthorizationIndex != -1 && index < useAuthorizationIndex)
                    ReportError("[invert]UseElmahIo[/] must be called after [invert]UseAuthorization[/]");
                else if (useAuthenticationIndex != -1 && index < useAuthenticationIndex)
                    ReportError("[invert]UseElmahIo[/] must be called after [invert]UseAuthentication[/]");
                else if (useEndpointsIndex != -1 && index > useEndpointsIndex)
                    ReportError("[invert]UseElmahIo[/] must be called before [invert]UseEndpoints[/]");
                else if (mapControllerRouteIndex != -1 && index > mapControllerRouteIndex)
                    ReportError("[invert]UseElmahIo[/] must be called before [invert]MapControllerRoute[/]");
                else if (useMvcIndex != -1 && index > useMvcIndex)
                    ReportError("[invert]UseElmahIo[/] must be called before [invert]UseMvc[/]");
                else if (usePiranhaIndex != -1 && index > usePiranhaIndex)
                    ReportError("[invert]UseElmahIo[/] must be called before [invert]UsePiranha[/]");
                else if (useUmbracoIndex != -1 && index > useUmbracoIndex)
                    ReportError("[invert]UseElmahIo[/] must be called before [invert]UseUmbraco[/]");
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
                    "Make sure that you are calling both the [invert]AddElmahIo[/] and [invert]UseElmahIo[/] methods in the [grey]Program.cs[/] or [grey]Startup.cs[/] file.",
                    "Make sure that you call the [invert]UseElmahIo[/] method after invoking other [invert]Use*[/] methods that in any way inspect exceptions (like [invert]UseDeveloperExceptionPage[/] and [invert]UseExceptionHandler[/]).",
                    "Make sure that you call the [invert]UseElmahIo[/] method before invoking [invert]UseMvc[/], [invert]UseEndpoints[/], and similar.",
                ]);
            }
        }
    }
}
