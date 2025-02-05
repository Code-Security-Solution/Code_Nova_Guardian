using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

public class SonarqubeCommand : Command<SonarqubeCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--path <PATH>")]
        [Description("스캔할 소스코드 경로를 지정합니다.")]
        public required string Path { get; set; }

        [CommandOption("--project-key <KEY>")]
        [Description("SonarQube 프로젝트 키를 지정합니다.")]
        public required string ProjectKey { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.Path) || string.IsNullOrEmpty(settings.ProjectKey))
        {
            AnsiConsole.Markup("[bold red]경로와 프로젝트 키를 모두 지정해주세요.[/]\n");
            return 1;
        }

        // SonarQube 실행 로직 (TODO: 실제 구현 필요)
        AnsiConsole.Markup($"[bold green]SonarQube: {settings.Path} 경로를 {settings.ProjectKey} 프로젝트로 분석합니다.[/]\n");

        return 0;
    }
}