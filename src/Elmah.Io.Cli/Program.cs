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
                new Option<bool>("--nologo", "Doesn't display the startup banner or the copyright message"),
            };

            rootCommand.AddCommand(ClearCommand.Create());
            rootCommand.AddCommand(DataloaderCommand.Create());
            rootCommand.AddCommand(DeploymentCommand.Create());
            rootCommand.AddCommand(DiagnoseCommand.Create());
            rootCommand.AddCommand(ExportCommand.Create());
            rootCommand.AddCommand(ImportCommand.Create());
            rootCommand.AddCommand(LogCommand.Create());
            rootCommand.AddCommand(SourceMapCommand.Create());
            rootCommand.AddCommand(TailCommand.Create());

            if (args == null || args.ToList().TrueForAll(arg => arg != "--nologo"))
            {
                AnsiConsole.Write(new FigletText("elmah.io")
                        .Color(new Color(13, 165, 142)));
                AnsiConsole.MarkupLine("[yellow]Copyright :copyright:[/] [rgb(13,165,142)]elmah.io[/]. All rights reserved.");
            }

            args = args?.Where(arg => arg != "--nologo").ToArray() ?? [];
            AnsiConsole.WriteLine();

            return await rootCommand.InvokeAsync(args);
        }
    }
}
