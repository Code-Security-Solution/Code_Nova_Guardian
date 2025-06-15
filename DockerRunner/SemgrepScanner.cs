using Code_Nova_Guardian.Json;
using Docker.DotNet.Models;
using Spectre.Console;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static SemgrepCommand;

namespace Code_Nova_Guardian.Class;

public partial class DockerRunner
{
    /*
      단일 책임 원칙(SRP)에 맞춰 Semgrep 관련 기능을 SemgrepScanner 이라는 객체로 분리.
      아직 객체지향 5원칙 (SOLID) 에 대해 제대로 공부해본적은 없지만 우선적으로 SRP를 적용해보았다.
      단일 책임 원칙 : 객체는 단 하나의 책임만 가져야 한다는 원칙
      :: 여기서 책임 = 기능 담당, 즉, 하나의 클래스는 하나의 기능 담당하여 하나의 책임을 수행하는데 집중되어야 있어야 한다는 의미
      출처: https://inpa.tistory.com/entry/OOP
      다만 Semgrep은 Docker로 돌리고 있기 때문에 DockerRunner에 속한 중첩 클래스로 외부에 노출하지 않게 구현.

      + partial 키워드로 별도의 파일로 분리되어 있지만 논리적으로는 하나다.
      응용 프로그램이 컴파일될 때 분할된 파일이 결합되기 때문.
      출처 : https://developer-talk.tistory.com/472
      C/C++의 include와 비슷한 개념이라고 생각하면 될 거 같다.
    */
    private class SemgrepScanner
    {
        // cli scan 돌릴때 api key 개념으로 사용되는 token 값
        private string cli_token;

        // semgrep 이미지 이름
        private string image_name;

        // 생성자에선 cli_token 값과, image_name 값을 받는다.
        public SemgrepScanner(string cli_token, string image_name)
        {
            this.cli_token = cli_token;
            this.image_name = image_name;
        }


        /*
          semgrep 으로 scan 하는 함수
            source_path : 스캔할 소스코드가 모여 있는 폴더(=디렉토리) 경로
            result_path : 스캔 결과 json 파일을 저장할 경로
        */
        public async Task scan(string source_path, string result_path, SemgrepScanOptions options)
        {
            // 입력 검증, 통과 못하면 Exception을 던지고 종료
            validate_scan_inputs(source_path, result_path);

            // 상대 경로 -> 절대 경로 변환
            /*
              Docker run 명령어를 통해 실행시 일반적으로 Docker은 마운트 경로가 상대 경로면 경로를 찾지 못한다고 한다.
              따라서 상대 경로가 들어와도 절대 경로로 변환한다.
              여기서 상대 경로->절대 경로로 변환하는 기준의 경우엔 이 cli 프로그램이 실행되는 위치를 기준으로 한다.
              :: 다만, docker compose는 상대 경로를 줘도 마운트가 가능하다고 한다.
              이 정보는 DeepSeek 검색 엔진 & R1, ChatGPT4o 의 검색 결과에 기반한다.
            */
            string abs_source_path = Path.GetFullPath(source_path);
            string abs_result_path = Path.GetFullPath(result_path);

            // 절대 경로에서 파일명만 추출 (확장자 포함)
            string result_file_name = Path.GetFileName(abs_result_path); // 값 : *.json

            // result_path 의 디렉토리 경로만 추출
            string result_dir_path = Path.GetDirectoryName(abs_result_path)!;

            try
            {
                // Docker Runner 객체 생성
                DockerRunner runner = new DockerRunner();

                // 필요한 이미지가 없다면 자동 설치, 딕셔너리에서 자동 이름 참고
                await runner.install_image(image_name);

                var container_config = new CreateContainerParameters
                {
                    // 이미지 이름
                    Image = image_name,

                    // 실행할 명령어 인자
                    // semgrep 실행 인수: "semgrep" + config_args + (JSON 옵션) + json-output
                    Cmd = ["semgrep", .. new Global.Global.SemgrepRules().rules_args, $"--json-output=/output/{result_file_name}"],

                    HostConfig = new HostConfig
                    {
                        Binds = new List<string>
                        {
                            /*
                              source_path 경로를 컨테이너 내부의 /src에 마운트해서 처리.
                              이 코드의 의미는 semgrep 이 Docker 환경에서 실행될 때,
                              들어온 경로에 있는 파일을 스캔하기 위해 source_path 경로를
                              자신의 컨테이너 /src 가상 위치에 스스로 마운트 해서 알아서 경로를 잡아 실행한다는 것이다.
                              이렇게 하면 도커가 알아서 /src 컨테이너 경로를 관리하기에 사용자는 신경 쓸 필요가 없다.
                              이것에 대한 자세한 설명은 인터넷 도커 볼륨 마운트 관련 문서 참고
                            */
                            $"{abs_source_path}:/src",   // 소스코드 경로는 컨테이너 내부 /src에 마운트
                            $"{result_dir_path}:/output" // 결과 파일 경로는 컨테이너 내부 /output에 마운트
                        },
                        AutoRemove = true // 컨테이너 종료 시 자동 삭제, 스캔만 하고 버릴것이기에 필수
                    },
                    Tty = true, // 컨테이너 실행시 TTY 활성화,  ANSI 색상 출력을 위한 필수 설정 / 이 옵션 비활성화 시 Semgrep 의 색상 출력이 안된다.
                    Env = new List<string>
                    {
                        $"SEMGREP_APP_TOKEN={cli_token}" // 환경 변수 설정, 토큰값을 활성화 해야 Pro Rules 사용 가능
                        ,
                        // "NO_COLOR=1" // ANSI 색상 출력 비활성화
                    }
                };

                // Debug ======================================================================================
                var debug_cmd = new[] { "semgrep" } // semgrep 실행 파일 실행
                    .Concat(new Global.Global.SemgrepRules()
                        .rules_args) // rules.json 에 정의된 순서대로 "--config=p/{rule}"
                                     // .Append("--json") // 결과를 json 형식으로 모니터에 출력, 주지 않으면 그냥 터미널에 semgrep이 알아서 정리해서 출력 (모니터 출력에 관한 옵션이므로 뭘 주던 상관 X, 의미 X)
                    .Append($"--json-output=/output/{result_file_name}") // json 결과 파일 경로
                    .ToList();

                // 디버그용 명령어 예쁘게 출력
                AnsiConsole.MarkupLine("[bold cyan]Semgrep :[/] 컨테이너 실행 명령어:");

                // 각 인수를 백슬래시(\)로 이어서 멀티라인 문자열 생성
                var pretty_cmd = string.Join(" \\\n  ", debug_cmd);

                // Panel로 감싸서 테두리와 헤더 적용
                AnsiConsole.Write(
                    new Panel($"[yellow]{pretty_cmd}[/]")
                        .Header("[bold green]Command Preview[/]")
                        .HeaderAlignment(Justify.Center)
                        .Expand()
                );
                // ===================================================================================================

                // 컨테이너 생성
                var response = await runner.docker_client.Containers.CreateContainerAsync(container_config);

                // 컨테이너 실행
                await runner.docker_client.Containers.StartContainerAsync(response.ID, null);
                AnsiConsole.Markup($"[bold cyan]\ud83d\udd0d Semgrep :[/] [bold yellow]{abs_source_path}[/] 에서 스캔을 시작합니다.\n");

                // 컨테이너 실행 로그를 실시간 출력
                await runner.print_container_log_async(response.ID);

                // 컨테이너 종료 대기
                await runner.docker_client.Containers.WaitContainerAsync(response.ID);

                // 스캔 성공 여부를 확인. 제대로 스캔이 되었다면 파일이 생성되어 있어야 하고 비어있지 않아야 한다.
                if (!File.Exists(abs_result_path) || new FileInfo(abs_result_path).Length == 0)
                {
                    throw new Exception($"스캔이 실패했습니다. 스캔 결과 파일이 존재하지 않거나 비어있습니다 스캔할 파일의 유효성을 확인해주세요: {abs_result_path}");
                }

                AnsiConsole.Markup($"[bold cyan]✅ Semgrep :[/] [bold yellow]{abs_source_path}[/] 에서 스캔을 완료했습니다!\n");

                // 출력 완료 메세지
                AnsiConsole.Markup($"[bold cyan]📂 Semgrep :[/] 결과 파일을 [bold yellow]{abs_result_path}[/] 에 저장했습니다.\n");


                // 만들어진 json 파일을 후처리 (번역 + 포맷팅 + Promessage 제거)
                post_process(result_path, options);

                // 후처리 완료 메세지
                AnsiConsole.Markup($"[bold cyan]\u2728 Semgrep :[/] 결과 파일 후처리가 완료되었습니다.\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[bold red]Semgrep :[/] 오류가 발생했습니다: {ex.Message}\n");
            }
        }

        /*
          스캔 입력값 검증을 위한 private 메서드
          source_path와 result_path의 유효성을 검사한다.
        */
        private void validate_scan_inputs(string source_path, string result_path)
        {
            // 스캔 파일 경로가 비어있거나, 유효하지 않으면 예외 던지고 종료
            if (string.IsNullOrEmpty(source_path) || !Directory.Exists(source_path))
                throw new ArgumentException($"소스 코드 경로가 비어있거나 유효하지 않습니다: {source_path}");

            // 결과 파일 경로가 파일 경로가 아닌 디렉토리인 경우
            if (Directory.Exists(result_path))
                throw new ArgumentException($"결과 파일 경로는 폴더 경로일 수 없습니다: {result_path}");

            // 결과 파일 경로가 json 파일이 아닌 경우
            if (!result_path.EndsWith(".json"))
                throw new ArgumentException($"결과 파일 경로는 json 파일이어야 합니다: {result_path}");
        }

        /*
          semgrep 에서 스캔이 완료되어 json 파일이 생성되면 이를 후처리하는 함수.
          기본적으로 semgrep 의 json 출력은 깔끔하게 format 되어 있지 않아서 formatting 시키고, 번역도 시킨다.
          테스트를 위해 우선은 public 처리, 나중에 캡슐화를 위해 private 처리할 예정.
        */
        // 현재 여러 옵션을 섞어 쓸 때 버그가 발생중 : 수정 필요
        public void post_process(string result_path, SemgrepScanOptions options)
        {
            // 입력 검증
            if (string.IsNullOrEmpty(result_path) || !File.Exists(result_path))
                throw new ArgumentException(
                    $"json 파일 경로가 비어있거나 해당 파일이 존재하지 않습니다 후처리 과정을 진행할 수 없습니다. : {result_path}\n");

            // JSON 파일을 UTF-8 인코딩으로 읽기
            string json_content = File.ReadAllText(result_path, Encoding.UTF8);

            /*
              원래는 json 파싱을 위해 Newtonsoft.Json 이라는 외부 라이브러리를 사용했으나, C#이 최신 버전으로 오면서
              System.Text.Json 이라는 내장 라이브러리로 json 파싱을 공식적으로 지원하기 시작했다.
              성능도 이게 더 낫다니깐 이걸 써보도록 하자.
            */
            // JSON 문자열을 파싱. 자체 정의한 json Class 객체로 변환 (역직렬화)
            SemgrepJsonRootObject? root = JsonSerializer.Deserialize<SemgrepJsonRootObject>(json_content);
            if (root == null)
                throw new Exception("[bold red]Error : Semgrep 결과 JSON 파일을 파싱하는데 실패했습니다.[/]\n");

            // no-pro-message 옵션이 활성화된 경우 Semgrep Pro Mode 메시지를 json 결과에서 제거
            if (options.no_pro_message)
            {
                if (root.errors != null)
                {
                    /*
                     root.errors 리스트에서
                       - 메시지가 비었거나 공백인 항목 또는
                       - "is only supported"와 "pro engine"이라는 문구를 포함하지 않는 항목만 남기고
                       (포함되면 !에 의해 false가 되고 걸러진다)
                     나머지는 제거해서 다시 배열로 저장해 대입
                     코드에서 SQL 쿼리문 처럼 쓸 수 있는 LINQ라는 좋은 기능
                    */
                    root.errors = root.errors
                        .Where(error =>
                            !(error.message.ToLower().Contains("is only supported") &&
                              error.message.ToLower().Contains("pro engine")))
                        .ToArray();

                    // 프로메세지를 거른 것을 포맷팅 없이 원본 파일에 반영한다
                    string json_result = JsonSerializer.Serialize(root);
                    File.WriteAllText(result_path, json_result, Encoding.UTF8);

                    AnsiConsole.Markup("[bold cyan]Semgrep :[/] Semgrep Pro 모드 메시지를 제거했습니다.\n");
                }
                else
                {
                    AnsiConsole.Markup("[bold red]Semgrep :[/] Semgrep Pro 모드 메시지가 없습니다.\n");
                }
            }

            // translate_result_path 가 비어있지 않고 설정되어야만 번역 진행
            if (!string.IsNullOrEmpty(options.translate_result_path))
            {
                // 스캔 결과가 비어있지 않으면 번역 Logic 수행
                if (root.results != null && root.results.Length != 0)
                {
                    // 이 함수 호출시 원본 root.results 변수는 내용이 변경된다.
                    translate_message(root.results);

                    // result를 다시 json으로 직렬화하고 파일로 저장
                    // 어차피 포맷팅 해서 깔끔하게 저장해야 하기에 번역 여부와 관계없이 필요한 작업
                    string json_result = JsonSerializer.Serialize(root, new JsonSerializerOptions
                    {
                        // 깔끔한 포맷팅을 위한 들여쓰기 설정
                        WriteIndented = true,
                        // 한글이 유니코드 이스케이프 없이 저장되는 설정, 이걸 안주면 유니코드 관련 글자가 모두 \uXXXX 이런식으로 저장된다.
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    // UTF-8 인코딩으로 파일 저장
                    File.WriteAllText(options.translate_result_path, json_result, Encoding.UTF8);
                    AnsiConsole.Markup("[bold cyan]Semgrep :[/] JSON 번역 & 포맷팅이 완료되었습니다.\n");
                }
                else
                {
                    // 결과가 비어있을 경우
                    AnsiConsole.Markup("[bold red]Semgrep :[/] 스캔 결과가 비어있습니다. 번역할 내용이 없습니다.\n");
                }
            }
        }

        /*
          json 사전 / 패턴 기반 번역 처리
          Result 배열을 받는 Call by ref 함수.
          참조하여 원본을 직접 변경한다.
          사실 (객체) 배열 원본 자체가 변하는 건 Low Level 한 개념이라
          High Level 언어에는 어울리지 않을 거 같지만
          현재 json 을 객체화해서 큰 메모리 덩어리로 다루는 만큼
          성능을 위해 복사하지 않고 바로 참조(포인팅해서 메모리 접근)하는것은 필수적이다.
        */
        public void translate_message(Result[] results)
        {
            // 전역 변수 값 가져오기 위해 객체 생성
            var paths = new Global.Global.Paths();

            // 파일 저장 메세지 출력
            AnsiConsole.Markup("[bold cyan]Semgrep :[/] 결과 메세지를 처리합니다.\n");

            // 만약에 번역 사전의 내용이 비어있는 초기 상태라면 Github에서 번역 사전 / 패턴 파일을 가져와서 번역 사전을 업데이트한다.
            load_dict(paths.semgrep_translate_file_path);

            // 결과 메세지를 반복하여 읽기
            foreach (var result in results)
            {
                result.extra.message = translate_text(result.extra.message, paths.semgrep_translate_file_path);
            }
        }

        /*
          번역 사전을 로드하거나 초기화하는 함수
          번역 사전이 비어있는 경우 Github에서 다운로드
        */
        private void load_dict(string translate_file_path)
        {
            string translate_json = File.ReadAllText(translate_file_path, Encoding.UTF8);
            TranslateJsonRootObject? translate_root = JsonSerializer.Deserialize<TranslateJsonRootObject>(translate_json);

            if (translate_root == null)
                throw new Exception("[bold red]Error : 번역 JSON 파일을 파싱하는데 실패했습니다.[/]\n");

            // 번역 사전이 비어있는 경우
            if (translate_root.dictionary.Length == 0 && translate_root.patterns.Length == 0)
                download_dict(translate_file_path);
        }

        /*
          Github에서 번역 사전을 다운로드하는 함수
        */
        private void download_dict(string translate_file_path)
        {
            try
            {
                AnsiConsole.Markup("[bold cyan]Semgrep :[/] 번역 사전이 초기 상태입니다. Github에서 번역 사전을 다운로드 시작합니다.\n");

                using HttpClient client = new HttpClient();
                string url = "https://github.com/Code-Security-Solution/Code_Nova_Guardian/raw/refs/heads/main/Translate/semgrep.translate.json";
                byte[] file_bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(translate_file_path, file_bytes);
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[bold red]Semgrep :[/] 번역 사전 다운로드 중 오류가 발생했습니다: {ex.Message}\n사용자 지정 번역 사전으로 진행합니다.");
            }
        }

        /*
          단일 메시지를 번역하는 함수
          패턴 번역을 우선 시도하고, 실패시 딕셔너리 번역을 시도
        */
        private string translate_text(string message, string translate_file_path)
        {
            JsonTranslator translator = new JsonTranslator(translate_file_path);

            /*
              우선 패턴 번역을 수행해서 결과값이 있는지 확인
              패턴 번역의 경우 사용자가 최우선적으로 등록하는 특수한 번역 형태이므로
              딕셔너리 번역(1:1 사전 번역) 보다 반드시 먼저 확인하도록 한다.
            */
            string pattern_result = translator.translate_text(message, JsonTranslator.TranslateType.Pattern);
            if (pattern_result != "")
                return pattern_result;


            // 결과값이 비었다면 패턴 번역 실패, 딕셔너리 번역도 확인
            string dic_result = translator.translate_text(message, JsonTranslator.TranslateType.Dictionary);
            if (dic_result != "")
                return dic_result;


            // 번역 실패시 번역 사전에 추가
            add_to_dict(message, translate_file_path);
            return message;
        }

        /*
          번역 실패한 메시지를 번역 사전에 추가하는 함수
        */
        private void add_to_dict(string message, string translate_file_path)
        {
            string translate_json = File.ReadAllText(translate_file_path, Encoding.UTF8);
            TranslateJsonRootObject? translate_root = JsonSerializer.Deserialize<TranslateJsonRootObject>(translate_json);

            if (translate_root == null)
                throw new Exception("[bold red]Error : 번역 JSON 파일을 파싱하는데 실패했습니다.[/]\n");

            // 기존 딕셔너리에 없는 경우 추가
            if (translate_root.dictionary.All(entry => entry.origin != message))
            {
                // 번역 필요한 것은 "" 로 빈 문자열로 등록. 여기 안에 번역할 값을 JSON 파일을 열어서 직접 쓰면 된다.
                var new_entry = new dictionary { origin = message, message = "" };
                var updated_dictionary = translate_root.dictionary.ToList();
                updated_dictionary.Add(new_entry);
                translate_root.dictionary = updated_dictionary.ToArray();

                // json 파일에 작성
                string updated_json = JsonSerializer.Serialize(translate_root, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                File.WriteAllText(translate_file_path, updated_json, Encoding.UTF8);
                AnsiConsole.Markup($"[bold yellow]번역 파일에 새로운 항목을 추가합니다:[/] {Markup.Escape(message)}\n");
            }
        }
    }
}

