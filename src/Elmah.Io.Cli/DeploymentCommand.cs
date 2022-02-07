using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

namespace Elmah.Io.Cli
{
    class DeploymentCommand : CommandBase
    {
        internal static Command Create()
        {
            var deploymentCommand = new Command("deployment")
            {
                new Option<string>("--apiKey", description: "An API key with permission to execute the command")
                {
                    IsRequired = true,
                },
                new Option<string>("--version", "The version number of this deployment")
                {
                    IsRequired = true,
                },
                new Option<DateTime>("--created", "When was this deployment created in UTC"),
                new Option<string>("--description", "Description of this deployment"),
                new Option<string>("--userName", "The name of the person responsible for creating this deployment"),
                new Option<string>("--userEmail", "The email of the person responsible for creating this deployment"),
                new Option<Guid>("--logId", "The ID of a log if this deployment is specific to a single log"),
            };
            deploymentCommand.Description = "Create a new deployment";
            deploymentCommand.Handler = CommandHandler.Create<string, string, DateTime?, string, string, string, Guid?>((apiKey, version, created, description, userName, userEmail, logId) =>
            {
                var api = Api(apiKey);
                try
                {
                    var result = api.Deployments.Create(new Client.CreateDeployment
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
            });

            return deploymentCommand;
        }
    }
}
