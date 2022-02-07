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

            rootCommand.AddCommand(ExportCommand.Create());
            rootCommand.AddCommand(LogCommand.Create());
            rootCommand.AddCommand(TailCommand.Create());
            rootCommand.AddCommand(DataloaderCommand.Create());

            if (args == null || args.All(arg => arg != "--nologo"))
            {
                AnsiConsole.Write(new FigletText("elmah.io")
                        .LeftAligned()
                        .Color(new Color(13, 165, 142)));
                AnsiConsole.MarkupLine("[yellow]Copyright (C)[/] [rgb(13,165,142)]elmah.io[/]. All rights reserved.");
            }

            AnsiConsole.WriteLine();

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
