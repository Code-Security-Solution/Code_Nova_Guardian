using Code_Nova_Guardian.Class;
using Code_Nova_Guardian.Json;
using Code_Nova_Guardian.Spectre_CLI;
using Newtonsoft.Json;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Text;

// ReSharper disable All


namespace Code_Nova_Guardian
{
    public class Program
    {
        // 비동기 메서드 호출을 위해선 기본적으로 await를 붙여야 하며, await 를 호출 하는쪽은
        // async Method로 선언 및 Task Return이 필요하다.
        public static int Main(string[] args)
        {
            // 프로그램 실행시 초기 설정 부분
            setup();

#if DEBUG
            // 디버깅 편의를 위해 우선 args 강제 고정, 전처리기를 이용해 Debug 모드인 경우에만 해당 코드 블럭이 실행된다.
            if (args.Length == 0)
            {
                //string source_path =
                //    "C:\\Users\\pgh268400\\Lab\\CSharp\\Code_Nova_Guardian\\bin\\Example\\Vulnerable-Code-Snippets";
                //string result_path = "./CNG/semgrep/result/origin_scan-no-promode.json";
                //args = new[] { "scan", "semgrep", source_path, result_path, "--no-pro-message" }; // 기본 실행 인자

                string source_path =
                    "C:\\Users\\pgh268400\\Lab\\CSharp\\Code_Nova_Guardian\\bin\\Example\\Vulnerable-Code-Snippets";
                string result_path = "./CNG/semgrep/result/origin_scan-promode.json";
                args = new[] { "scan", "semgrep", source_path, result_path }; // 기본 실행 인자
            }
#endif

            var app = new CommandApp();

            // 프로그램 설정
            app.Configure(config =>
            {
                // 자기 자신 exe 이름 가져오기
                string self_process_name = Process.GetCurrentProcess().MainModule.ModuleName;

                // exe 실행시 출력되는 이름을 자신의 exe 이름으로 설정
                config.SetApplicationName(self_process_name);
                config.SetApplicationVersion("0.0.1");

                // 직접 구현한 CustomHelpProvider를 사용하도록 설정 (USAGE, OPTIONS 이런거)
                // config.SetHelpProvider(new CustomHelpProvider(config.Settings));
                // config.Settings.HelpProviderStyles = null;

                // check-requirement 명령어 추가
                config.AddCommand<CheckRequirementCommand>("check-requirement")
                    .WithDescription("필요한 프로그램이 설치되었는지 수동으로 확인합니다.");


                // scan 명령어 추가 (명령어는 --로 시작하지 않는다. 옵션만 --로 시작)
                config.AddBranch("scan", scan =>
                {
                    scan.SetDescription("코드 스캔을 수행하는 명령어 입니다.");
                    // scan semgrep - 대부분 구현
                    scan.AddCommand<SemgrepCommand>("semgrep")
                        .WithDescription("Semgrep으로 소스코드 분석을 수행합니다. ");

                    // scan sonarqube - 현재 미구현
                    //scan.AddCommand<SonarqubeCommand>("sonarqube")
                    //    .WithDescription("SonarQube로 소스코드 분석을 수행합니다.");
                });
            });

            return app.Run(args);
        }

        // 프로그램 실행시 초기 설정을 진행하는 함수
        private static void setup()
        {
            try
            {
                /*
                  특수문자 깨짐 방지를 위해 최초로 UTF8 인코딩 설정. 
                  이 세팅을 늦게 하면 오히려 콘솔 출력이 깨지는 경우가 있다.
                  가장 최초로 실행해야 할 필수 코드.
                  이걸로 Semgrep 출력에서 유니코드 특문이 깨져서 개고생했는데...
                  https://github.com/spectreconsole/spectre.console/issues/113
                  관련 이슈로 해결 ㅠㅠ
                  이거 넣으면 유니코드 특수문자 콘솔에서 깨지지 않고 아주 잘 나온다.
                  (Windows Terminal 에서 구동 기준, cmd 창으로만 실행은 확인 X)
                */
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                // 전역 변수 가져오기
                var paths = new Global.Global.Paths();

                // 중요 파일들을 저장할 폴더 생성
                // Directory.CreateDirectory = 폴더가 존재하지 않으면 생성, 이미 존재하는 경우 무시
                Directory.CreateDirectory(paths.root_dir_path);

                // Semgrep 저장 폴더 생성
                Directory.CreateDirectory(paths.semgrep_dir_path);

                // API Key 파일 생성을 위해 빈 JSON 객체 생성
                APIKeyJsonRootObject empty_api_json = new APIKeyJsonRootObject();

                // JSON 직렬화
                string api_json_string = JsonConvert.SerializeObject(empty_api_json, Formatting.Indented);

                // API Key JSON 파일 저장
                if (!File.Exists(paths.api_key_file_path))
                    File.WriteAllText(paths.api_key_file_path, api_json_string, Encoding.UTF8);

                // Semgrep 번역용 json 파일 생성, 위와 동일한 방향으로 생성
                TranslateJsonRootObject empty_semgrep_json = new TranslateJsonRootObject();
                string semgrep_json_string = JsonConvert.SerializeObject(empty_semgrep_json, Formatting.Indented);
                if (!File.Exists(paths.semgrep_translate_file_path))
                    File.WriteAllText(paths.semgrep_translate_file_path, semgrep_json_string, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }
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
            bool is_docker_installed = await DockerRunner.check_installation();
            if (!is_docker_installed) Environment.Exit(1);
        }
    }
}