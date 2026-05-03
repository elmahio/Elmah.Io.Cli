using Spectre.Console;
using System.CommandLine;
using System.Reflection;

namespace Elmah.Io.Cli
{
    class SkillCommand
    {
        private const string SkillResourceName = "Elmah.Io.Cli.Resources.SKILL.md";

        internal static Command Create()
        {
            var skillCommand = new Command("skill", "Work with the elmah.io Claude Code skill")
            {
                CreateInstallSubcommand(),
            };
            return skillCommand;
        }

        private static Command CreateInstallSubcommand()
        {
            var scopeOption = new Option<string>("--scope")
            {
                Description = "Where to install: 'user' (~/.claude/skills) or 'project' (./.claude/skills)",
                DefaultValueFactory = _ => "user"
            };
            var forceOption = new Option<bool>("--force")
            {
                Description = "Overwrite an existing SKILL.md without prompting",
                DefaultValueFactory = _ => false
            };
            var installSubcommand = new Command("install", "Install the elmahio SKILL.md into a Claude Code skills directory")
            {
                scopeOption, forceOption,
            };
            installSubcommand.SetAction(result =>
            {
                var scope = result.GetValue(scopeOption) ?? "user";
                var force = result.GetValue(forceOption);

                string baseDir;
                if (string.Equals(scope, "user", StringComparison.OrdinalIgnoreCase))
                {
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "skills");
                }
                else if (string.Equals(scope, "project", StringComparison.OrdinalIgnoreCase))
                {
                    baseDir = Path.Combine(Directory.GetCurrentDirectory(), ".claude", "skills");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Unknown scope '{Markup.Escape(scope)}'. Use 'user' or 'project'.[/]");
                    return;
                }

                var targetDir = Path.Combine(baseDir, "elmahio");
                var targetFile = Path.Combine(targetDir, "SKILL.md");

                if (File.Exists(targetFile) && !force)
                {
                    AnsiConsole.MarkupLine($"[yellow]SKILL.md already exists at[/] [dim]{Markup.Escape(targetFile)}[/]");
                    var overwrite = AnsiConsole.Prompt(new ConfirmationPrompt("Overwrite?") { DefaultValue = false });
                    if (!overwrite)
                    {
                        AnsiConsole.MarkupLine("[dim]Aborted.[/]");
                        return;
                    }
                }

                var content = LoadEmbeddedSkill();
                if (content == null)
                {
                    AnsiConsole.MarkupLine("[red]Embedded SKILL.md resource not found. This is a packaging bug — please report it.[/]");
                    return;
                }

                Directory.CreateDirectory(targetDir);
                File.WriteAllText(targetFile, content);

                AnsiConsole.MarkupLine($"[#0da58e]Installed elmahio skill to[/] [dim]{Markup.Escape(targetFile)}[/]");
                AnsiConsole.MarkupLine("[dim]Restart Claude Code (or open a new session) for the skill to load.[/]");
            });

            return installSubcommand;
        }

        private static string? LoadEmbeddedSkill()
        {
            var assembly = typeof(SkillCommand).Assembly;
            using var stream = assembly.GetManifestResourceStream(SkillResourceName);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
