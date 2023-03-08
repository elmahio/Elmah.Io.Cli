using Spectre.Console;
using System;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class DeploymentCommand : CommandBase
    {
        internal static Command Create()
        {
            var apiKeyOption = new Option<string>("--apiKey", description: "An API key with permission to execute the command")
            {
                IsRequired = true,
            };
            var versionOption = new Option<string>("--version", "The version number of this deployment")
            {
                IsRequired = true,
            };
            var createdOption = new Option<DateTimeOffset?>("--created", "When was this deployment created in UTC");
            var descriptionOption = new Option<string>("--description", "Description of this deployment");
            var userNameOption = new Option<string>("--userName", "The name of the person responsible for creating this deployment");
            var userEmailOption = new Option<string>("--userEmail", "The email of the person responsible for creating this deployment");
            var logIdOption = new Option<Guid?>("--logId", "The ID of a log if this deployment is specific to a single log");
            var deploymentCommand = new Command("deployment", "Create a new deployment")
            {
                apiKeyOption, versionOption, createdOption, descriptionOption, userNameOption, userEmailOption, logIdOption
            };
            deploymentCommand.SetHandler(async (apiKey, version, created, description, userName, userEmail, logId) =>
            {
                var api = Api(apiKey);
                try
                {
                    var result = await api.Deployments.CreateAsync(new Client.CreateDeployment
                    {
                        Version = version,
                        Created = created,
                        Description = string.IsNullOrWhiteSpace(description) ? null : description,
                        UserName = string.IsNullOrWhiteSpace(userName) ? null : userName,
                        UserEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail,
                        LogId = logId.HasValue ? logId.Value.ToString() : null,
                    });

                    AnsiConsole.MarkupLine($"[#0da58e]Deployment successfully created[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                }
            }, apiKeyOption, versionOption, createdOption, descriptionOption, userNameOption, userEmailOption, logIdOption);

            return deploymentCommand;
        }
    }
}
