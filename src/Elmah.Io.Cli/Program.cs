using Spectre.Console;
using System.CommandLine;

namespace Elmah.Io.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.Description = "CLI for executing various actions against elmah.io";

            rootCommand.AddCommand(ExportCommand.Create());
            rootCommand.AddCommand(LogCommand.Create());
            rootCommand.AddCommand(TailCommand.Create());
            rootCommand.AddCommand(DataloaderCommand.Create());

            AnsiConsole.Write(new FigletText("elmah.io")
                    .LeftAligned()
                    .Color(new Color(13, 165, 142)));

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
