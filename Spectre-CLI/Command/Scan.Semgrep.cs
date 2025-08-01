﻿using Code_Nova_Guardian;
using Code_Nova_Guardian.Class;
using Spectre.Console.Cli;
using System.ComponentModel;
using DockerRunner = Code_Nova_Guardian.DockerRunner.DockerRunner;

/*
  참고 : 해당 프로그램은 Spectre.Console.Cli 을 통해 CLI를 쉽게 구현했으며,
  여기서 나오는 코드 패턴은 Spectre.Console.Cli 에서 제공하는 설계를 기반으로 하므로,
  그냥 코드를 보고 이런 형태구나 받아들이면 된다. (supported by GPT4o)
*/
public class SemgrepCommand : AsyncCommand<SemgrepCommand.Settings>
{
    public class Settings : CommandSettings
    {
        // 필수 인자들 =======================================================================================================
        /*
          [CommandOption("--path <PATH>")
          [CommandOption] 대신[CommandArgument] 사용, 이렇게 하면 필수 인자로 path를 지정할 수 있다.
          여기서 CommandArgument 를 지정하면 Spectre 라이브러리에서 알아서 인자가 들어오지 않으면 에러를 발생시킨다.
        */
        [CommandArgument(0, "<source_path>")]
        [Description("스캔할 소스코드 폴더 경로를 지정합니다.")]
        public required string source_path { get; set; }

        [CommandArgument(1, "<result_path>")]
        [Description("스캔 결과를 저장할 json 파일 경로를 지정합니다.")]
        public required string result_path { get; set; }

        // 옵션 인자들 =======================================================================================================
        // Semgrep scan시 돈 내고 쓰는 Pro Mode를 그냥 사용했을 때 나오는 pro 메세지를 출력에서 그냥 제거하는 옵션
        [CommandOption("--no-pro-message")]
        [Description("Semgrep Pro 모드 메시지를 출력하지 않습니다.")]
        public bool no_pro_message { get; set; } // 옵션 넣으면 true, 안 넣으면 false

        [CommandOption("--translate <translated_result_path>")]
        [Description("번역된 스캔 결과를 저장할 JSON 파일 경로를 지정합니다. 번역시 CNG 폴더안의 Semgrep 사전 파일을 이용합니다.")]
        public string? translate_result_path { get; set; }
        // ===================================================================================================================
    }

    // Semgrep Options 을 묶어서 보내기 위해 클래스 하나 정의
    public class SemgrepScanOptions
    {
        public bool no_pro_message { get; set; }
        public string? translate_result_path { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {

        // 인자 체크의 경우 위에서도 언급했지만 Spectre 라이브러리에서 알아서 처리해주므로 여기서 따로 체크할 필요 없다.
        // AnsiConsole.Markup($"인자로 들어온 경로 : {settings.source_path}\n");

        var options = new SemgrepScanOptions
        {
            no_pro_message = settings.no_pro_message,
            translate_result_path = settings.translate_result_path
        };

        // 여기까지 오면 필요한 인자는 다 갖춰진 상태
        await Program.check_requirement(); // 이 검사까지만 통과하면 작업 시작
        var docker_runner = new DockerRunner();

        await docker_runner.scan_semgrep(settings.source_path, settings.result_path, options);
        return 0;
    }
}