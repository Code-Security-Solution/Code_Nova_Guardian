using Code_Nova_Guardian;
using Code_Nova_Guardian.Class;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

/*
  참고 : 해당 프로그램은 Spectre.Console.Cli 을 통해 CLI를 쉽게 구현했으며,
  여기서 나오는 코드 패턴은 Spectre.Console.Cli 에서 제공하는 설계를 기반으로 하므로,
  그냥 코드를 보고 이런 형태구나 받아들이면 된다. (supported by GPT4o)
*/
public class SemgrepCommand : AsyncCommand<SemgrepCommand.Settings>
{
    public class Settings : CommandSettings
    {
        //[CommandOption("--path <PATH>")]
        // [CommandOption] 대신 [CommandArgument] 사용, 이렇게 하면 필수 인자로 path를 지정할 수 있다.
        [CommandArgument(0, "<Path>")]
        [Description("스캔할 소스코드 경로를 지정합니다.")]

        public required string Path { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.Markup($"인자로 들어온 경로 : {settings.Path}\n");
        if (string.IsNullOrEmpty(settings.Path))
        {
            AnsiConsole.Markup("[bold red]Error : 스캔할 경로를 지정해주세요.[/]\n");
            return 1;
        }

        // 여기까지 오면 필요한 인자는 다 갖춰진 상태
        await Program.check_requirement(); // 이 검사까지만 통과하면 작업 시작
        var docker_runner = new DockerRunner();
        await docker_runner.scan_semgrep(settings.Path);

        return 0;
    }
}