using Code_Nova_Guardian;
using Code_Nova_Guardian.Class;
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
        // [CommandOption("--path <PATH>")]

        // [CommandOption] 대신 [CommandArgument] 사용, 이렇게 하면 필수 인자로 path를 지정할 수 있다.
        // 여기서 CommandArgument 를 지정하면 Spectre 라이브러리에서 알아서 인자가 들어오지 않으면 에러를 발생시킨다.
        [CommandArgument(0, "<source_path>")]
        [Description("스캔할 소스코드 폴더 경로를 지정합니다.")]
        public required string source_path { get; set; }

        [CommandArgument(1, "<result_path>")]
        [Description("스캔 결과를 저장할 json 파일 경로를 지정합니다.")]
        public required string result_path { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // 인자 체크의 경우 위에서도 언급했지만 Spectre 라이브러리에서 알아서 처리해주므로 여기서 따로 체크할 필요 없다.
        // AnsiConsole.Markup($"인자로 들어온 경로 : {settings.source_path}\n");

        // 여기까지 오면 필요한 인자는 다 갖춰진 상태
        await Program.check_requirement(); // 이 검사까지만 통과하면 작업 시작
        var docker_runner = new DockerRunner();
        await docker_runner.scan_semgrep(settings.source_path, settings.result_path);
        //docker_runner.postprocess_semgrep_result(settings.result_path);

        // JsonTranslator translator = new JsonTranslator(); // 생성자 생성으로 테스트


        return 0;
    }
}