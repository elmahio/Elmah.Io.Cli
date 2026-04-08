using Spectre.Console;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class DeploymentCommand : CommandBase
    {
        internal static Command Create()
        {
            var apiKeyOption = ApiKeyOption();
            var versionOption = new Option<string>("--version") { Description = "The version number of this deployment", Required = true };
            var createdOption = new Option<DateTimeOffset?>("--created") { Description = "When was this deployment created in UTC" };
            var descriptionOption = new Option<string?>("--description") { Description = "Description of this deployment" };
            var userNameOption = new Option<string?>("--userName") { Description = "The name of the person responsible for creating this deployment" };
            var userEmailOption = new Option<string?>("--userEmail") { Description = "The email of the person responsible for creating this deployment" };
            var logIdOption = new Option<Guid?>("--logId") { Description = "The ID of a log if this deployment is specific to a single log" };
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var deploymentCommand = new Command("deployment", "(deprecated) Create a new deployment")
            {
                apiKeyOption, versionOption, createdOption, descriptionOption, userNameOption, userEmailOption, logIdOption, proxyHostOption, proxyPortOption
            };
            deploymentCommand.SetAction(async result =>
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] 'elmahio deployment' is deprecated. Use 'elmahio deployments create' instead.");

                var apiKey = result.GetValue(apiKeyOption);
                var version = result.GetValue(versionOption);
                var created = result.GetValue(createdOption);
                var description = result.GetValue(descriptionOption);
                var userName = result.GetValue(userNameOption);
                var userEmail = result.GetValue(userEmailOption);
                var logId = result.GetValue(logIdOption);
                var host = result.GetValue(proxyHostOption);
                var port = result.GetValue(proxyPortOption);

                await DeploymentsCommand.ExecuteCreateAsync(apiKey, version, created, description, userName, userEmail, logId, host, port);
            });

            return deploymentCommand;
        }
    }
}
