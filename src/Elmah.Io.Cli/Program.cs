using Spectre.Console;
using System.CommandLine;
using System.Linq;

namespace Elmah.Io.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand("CLI for executing various actions against elmah.io")
            {
                new Option<bool>("--nologo", "Doesn't display the startup banner or the copyright message"),
            };

            rootCommand.AddCommand(ClearCommand.Create());
            rootCommand.AddCommand(DataloaderCommand.Create());
            rootCommand.AddCommand(DeploymentCommand.Create());
            rootCommand.AddCommand(DiagnoseCommand.Create());
            rootCommand.AddCommand(ExportCommand.Create());
            rootCommand.AddCommand(LogCommand.Create());
            rootCommand.AddCommand(SourceMapCommand.Create());
            rootCommand.AddCommand(TailCommand.Create());

            if (args == null || args.All(arg => arg != "--nologo"))
            {
                AnsiConsole.Write(new FigletText("elmah.io")
                        .Color(new Color(13, 165, 142)));
                AnsiConsole.MarkupLine("[yellow]Copyright (C)[/] [rgb(13,165,142)]elmah.io[/]. All rights reserved.");
            }

            args = args.Where(arg => arg != "--nologo").ToArray();
            AnsiConsole.WriteLine();

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
