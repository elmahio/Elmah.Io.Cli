using Spectre.Console;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var rootCommand = new RootCommand("CLI for executing various actions against elmah.io")
            {
                // Options
                new Option<bool>("--nologo") { Description = "Doesn't display the startup banner or the copyright message" },
                // Commands
                LoginCommand.Create(),
                LogoutCommand.Create(),
                DeploymentsCommand.Create(),
                DiagnoseCommand.Create(),
                LogsCommand.Create(),
                MessagesCommand.Create(),
                SkillCommand.Create(),
                // Deprecated commands
                ClearCommand.Create(),
                DataloaderCommand.Create(),
                DeploymentCommand.Create(),
                ExportCommand.Create(),
                ImportCommand.Create(),
                LogCommand.Create(),
                SourceMapCommand.Create(),
                TailCommand.Create()
            };

            if (args == null || args.ToList().TrueForAll(arg => arg != "--nologo" && arg != "--json"))
            {
                AnsiConsole.Write(new FigletText("elmah.io")
                        .Color(new Color(13, 165, 142)));
                AnsiConsole.MarkupLine("[yellow]Copyright :copyright:[/] [rgb(13,165,142)]elmah.io[/]. All rights reserved.");
            }

            args = args?.Where(arg => arg != "--nologo").ToArray() ?? [];
            AnsiConsole.WriteLine();

            return await rootCommand.Parse(args).InvokeAsync(new InvocationConfiguration(), CancellationToken.None);
        }
    }
}
