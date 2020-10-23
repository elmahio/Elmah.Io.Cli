﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

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

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
