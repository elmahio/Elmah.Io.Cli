using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Binding;

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
            var descriptionOption = new Option<string?>("--description", "Description of this deployment");
            var userNameOption = new Option<string?>("--userName", "The name of the person responsible for creating this deployment");
            var userEmailOption = new Option<string?>("--userEmail", "The email of the person responsible for creating this deployment");
            var logIdOption = new Option<Guid?>("--logId", "The ID of a log if this deployment is specific to a single log");
            var proxyHostOption = ProxyHostOption();
            var proxyPortOption = ProxyPortOption();
            var deploymentCommand = new Command("deployment", "Create a new deployment")
            {
                apiKeyOption, versionOption, createdOption, descriptionOption, userNameOption, userEmailOption, logIdOption, proxyHostOption, proxyPortOption
            };
            deploymentCommand.SetHandler(async (apiKey, deploymentModel, host, port) =>
            {
                var api = Api(apiKey, host, port);
                try
                {
                    await api.Deployments.CreateAsync(new Client.CreateDeployment
                    {
                        Version = deploymentModel.Version,
                        Created = deploymentModel.Created,
                        Description = string.IsNullOrWhiteSpace(deploymentModel.Description) ? null : deploymentModel.Description,
                        UserName = string.IsNullOrWhiteSpace(deploymentModel.UserName) ? null : deploymentModel.UserName,
                        UserEmail = string.IsNullOrWhiteSpace(deploymentModel.UserEmail) ? null : deploymentModel.UserEmail,
                        LogId = deploymentModel.LogId.HasValue ? deploymentModel.LogId.Value.ToString() : null,
                    });

                    AnsiConsole.MarkupLine($"[#0da58e]Deployment successfully created[/]");
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{e.Message}[/]");
                }
            }, apiKeyOption, new DeploymentModelBinder(versionOption, createdOption, descriptionOption, userNameOption, userEmailOption, logIdOption), proxyHostOption, proxyPortOption);

            return deploymentCommand;
        }

        private sealed class DeploymentModel(string? version)
        {
            public string Version { get; set; } = version ?? throw new ArgumentNullException(nameof(version));
            public DateTimeOffset? Created { get; set; }
            public string? Description { get; set; }
            public string? UserName { get; set; }
            public string? UserEmail { get; set; }
            public Guid? LogId { get; set; }
        }

        private sealed class DeploymentModelBinder(Option<string> versionOption, Option<DateTimeOffset?> createdOption, Option<string?> descriptionOption, Option<string?> userNameOption, Option<string?> userEmailOption, Option<Guid?> logIdOption) : BinderBase<DeploymentModel>
        {
            private readonly Option<string> versionOption = versionOption;
            private readonly Option<DateTimeOffset?> createdOption = createdOption;
            private readonly Option<string?> descriptionOption = descriptionOption;
            private readonly Option<string?> userNameOption = userNameOption;
            private readonly Option<string?> userEmailOption = userEmailOption;
            private readonly Option<Guid?> logIdOption = logIdOption;

            protected override DeploymentModel GetBoundValue(BindingContext bindingContext)
            {
                return new DeploymentModel(bindingContext.ParseResult.GetValueForOption(versionOption))
                {
                    Created = bindingContext.ParseResult.GetValueForOption(createdOption),
                    Description = bindingContext.ParseResult.GetValueForOption(descriptionOption),
                    UserName = bindingContext.ParseResult.GetValueForOption(userNameOption),
                    UserEmail = bindingContext.ParseResult.GetValueForOption(userEmailOption),
                    LogId = bindingContext.ParseResult.GetValueForOption(logIdOption),
                };
            }
        }
    }
}
