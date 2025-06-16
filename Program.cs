using Code_Nova_Guardian.Spectre_CLI;
using Spectre.Console.Cli;
using System.Diagnostics;

namespace Code_Nova_Guardian;

// Program 의 코드를 분리하기 위해 partial class 로 변경
public partial class Program 
{
    // 비동기 메서드 호출을 위해선 기본적으로 await를 붙여야 하며, await 를 호출 하는쪽은
    // async Method로 선언 및 Task Return이 필요하다.
    public static int Main(string[] args)
    {
        // 프로그램 실행시 초기 설정 부분
        setup();

#if DEBUG
        // 디버깅 편의를 위해 우선 args 강제 고정, 전처리기를 이용해 Debug 모드인 경우에만 해당 코드 블럭이 실행된다.
        if (args.Length == 0) // 인자 없이 그냥 실행한 경우 인자를 강제 설정
            args = debug_args();
#endif

        var app = new CommandApp();

        // 프로그램 설정
        app.Configure(config =>
        {
            // 자기 자신 exe 이름 가져오기
            string self_process_name = Process.GetCurrentProcess().MainModule!.ModuleName;

            // exe 실행시 출력되는 이름을 자신의 exe 이름으로 설정
            config.SetApplicationName(self_process_name);
            config.SetApplicationVersion("0.0.2");

            /*
              // 직접 구현한 CustomHelpProvider를 사용하도록 설정(USAGE, OPTIONS 이런거)
              config.SetHelpProvider(new CustomHelpProvider(config.Settings));
              config.Settings.HelpProviderStyles = null;
            */

            // check-requirement : Docker 설치 확인 명령어
            config.AddCommand<CheckRequirementCommand>("check-requirement")
                .WithDescription("필요한 프로그램이 설치되었는지 수동으로 확인합니다.");

            // get-semgrep-token : Semgrep Token 획득 명령어
            // Semgrep scan시 로그인 대신 식별을 위해 사용하는 토큰 (일종의 API Key) 을 얻기 위한 옵션
            config.AddCommand<GetSemgrepTokenCommand>("get-semgrep-token")
                .WithDescription("Semgrep 사용을 위한 토큰을 획득합니다.");

            // scan 명령어 추가 (명령어는 --로 시작하지 않는다. 옵션만 --로 시작)
            config.AddBranch("scan", scan =>
            {
                scan.SetDescription("코드 스캔을 수행하는 명령어 입니다.");
                // scan semgrep - 대부분 구현
                scan.AddCommand<SemgrepCommand>("semgrep")
                    .WithDescription("Semgrep으로 소스코드 분석을 수행합니다. ");
            });
        });

        return app.Run(args);
    }

    // 해당 함수는 해당 CLI 프로그램을 사용하기 위해 필요한 프로그램들이 설치되어 있는지 검사하는 함수다.
    // verbose : 활성화 시 추가적인 디버깅 메세지를 출력한다.
    public static async Task check_requirement()
    {
        /*
          기본적으로 해당 프로그램이 호출하는 보안 솔루션 도구들은 대부분 Docker로 설치해 사용한다.
          이유는 설치할 것이 많은데 한 PC에 모두 설치시 의존성 문제가 발생해 여러가지가 꼬일 수 있기 때문이다.
          Native 보다 성능이 약간 떨어지더라도 Docker Container 격리성의 이점을 살려 Docker로 작업하도록 한다.

          참고로 Docker CLI의 경우 cli 명령어 실행시 Engine 과 동일한 호스트에서 실행시
          Unix Socket(프로세스 IPC와 거의 동일한 개념) 으로 통신하여 처리되고,
          Docker CLI가 Host 외부에 있는 경우 Engine과 http로 통신한다. (REST API)
          자세한 내용 : https://www.devkuma.com/docs/docker/get-started/6-docker-engine-overview/
                       https://senticoding.tistory.com/94

          기본적으로 해당 프로그램은 Docker Engine이 돌아가는 Host와 동일한 위치에서 실행된다고 가정한다.
          C#에서 Docker CLI 실행을 위해 Process 호출 방식으로도 사용할 수 있지만 이미 이에 관한 Docker.DotNet 이라는
          라이브러리가 있어 이를 활용하도록 한다. Docker.DotNet 으로 함수 호출시 내부적으로 socket/http로 엔진을 호출해
          사용한다고 한다. 어짜피 추상화(간략화) 되어 우리는 사용만 하면 된다.
        */
        // Docker 설치되어있는지 확인, DockerRunner은 Docker.DotNet을 한 번 더
        // Wrapping 해서 Docker 관련 여러 편의 기능 제공 (Custom Class)
        bool is_docker_installed = await DockerRunner.DockerRunner.check_installation();
        if (!is_docker_installed) Environment.Exit(1);
    }
}