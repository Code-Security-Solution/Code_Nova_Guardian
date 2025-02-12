namespace Code_Nova_Guardian.Class;
using Docker.DotNet;
using Docker.DotNet.Models;
using Json;
using Spectre.Console;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static Code_Nova_Guardian.Global.Global;


/*
  Docker을 활용한 여러 기능들을 간편하게 사용할 수 있는 DockerRunner 객체의 설계도(Class)
  아래는 지원 기능
  1. 여러 보안 취약점 도구 CLI 호출 및 결과 제공
  2. Docker CLI가 Host와 같은 위치에 있는지 체크 (Docker 설치여부 체크)
*/
public class DockerRunner
{
    // DockerRunner 클래스에서 실행 가능한 보안 취약점 도구 목록
    public enum SecurityTool
    {
        Semgrep
    }

    // 각 보안 취약점 도구의 이미지 이름
    // 딕셔너리로 대응시켜 직접 관리
    protected readonly Dictionary<SecurityTool, string> docker_image = new()
    {
        // 이미지 이름은 Docker Hub에 등록된 이미지 이름을 사용
        // 이곳에 이미지 명을 하드 코딩하여 관리
        { SecurityTool.Semgrep, "returntocorp/semgrep" }
    };

    // 멤버 변수
    // docker client 는 생성자에서 한 번만 connect 해서 생성하고 이후 돌려쓴다
    private DockerClient docker_client;

    // 생성자
    public DockerRunner()
    {
        // new로 객체 메모리에 생성
        docker_client = new DockerClientConfiguration().CreateClient();
    }

    // 소멸자, C#에서 소멸자는 접근 제한자 (private / public / protected) 를 가질 수 없다.
    ~DockerRunner()
    {
        // 객체 소멸시 docker_client 자동 Dispose
        if (docker_client != null)
            docker_client.Dispose();
    }

    // Docker Engine 설치 여부 확인 (객체 생성 없이 바로 실행 가능한 static 함수)
    public static async Task<bool> check_installation()
    {

        AnsiConsole.Markup("[blue]Docker Host[/]의 상태를 확인합니다.\n");

        try
        {
            /*
              DockerClientConfiguration 생성자에 아무 값 안주면 알아서 Unix Socket이나 pipe 활용해서 IPC로 Docker Engine에 통신 하는 듯 하다.
              만약 Docker Host가 CLI 와 다른 컴퓨터에 있는 경우 http 주소등을 명시해줘야 하고, socket 파일 이름을 명시적으로 줄 수도 있다.
              추가로 컴퓨터 네트워크 시간에 배운 내용에 따르면, Unix socket을 socat 과 같은 Tool을 이용해 TCP로 개방시켜 공유할 수도 있다. (Advanced Technique)
            */
            using (var client = new DockerClientConfiguration().CreateClient())
            {
                // Docker 버전 정보 요청 (Docker가 실행 중인지 확인)
                var version = await client.System.GetVersionAsync();

                // 문제가 없다면 위에서 Exception Jump 없이 이 부분이 실행되고 함수는 종료되게 된다.
                AnsiConsole.Markup("\n[bold green]\u2705 Docker가 Host에서 실행 중입니다![/]\n");
                AnsiConsole.Markup($"[cyan]🐳 도커 버전:[/] [bold yellow]{version.Version}[/]\n");
                AnsiConsole.Markup($"[cyan]🔗 API 버전:[/] [bold yellow]{version.APIVersion}[/]\n");
                return true; // 성공 Task 반환
            }
        }
        catch (DockerApiException api_ex)
        {
            AnsiConsole.Markup("\n[bold red]❌ Docker API 오류가 발생했습니다.[/]\n");
            AnsiConsole.Markup($"[red]Error:[/] [italic]{api_ex.Message}[/]\n");
            AnsiConsole.Markup("[bold red]⚠  Docker 상태를 확인해 주세요.[/]\n");
            return false; // 실패 Task 반환
        }
        catch (Exception ex)
        {
            AnsiConsole.Markup("\n[bold red]❌ 예기치 못한 오류가 발생했습니다.[/]\n");
            AnsiConsole.Markup($"[red]Error:[/] [italic]{ex.Message}[/]\n");
            AnsiConsole.Markup("⚠  [bold red]Docker[/]가 [bold yellow]실행[/] 중인지 확인하세요.\n");
            AnsiConsole.Markup("⚠  [bold red]Docker[/]가 설치되지 않았다면, 먼저 [bold cyan]설치[/]해 주세요.\n");
            return false; // 실패 Task 반환
        }
    }

    // 이미지 존재 여부 확인 함수
    private async Task<bool> is_image_exist(string image_name)
    {
        try
        {
            // 'reference' 필터를 사용하여 지정한 이미지가 존재하는지 확인.
            var images = await docker_client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool>
                    {
                        [image_name] = true
                    }
                }
            });
            // 이미지가 존재하는지 여부 반환 (이미지가 존재하면 true, 그렇지 않으면 false)
            bool result = images != null && images.Count > 0;
            return result;
        }
        catch (Exception ex)
        {
            // 예외를 상위로 전달.
            throw new Exception($"이미지 확인 중 오류 발생: {ex.Message}", ex);
        }
    }

    // 이미지 설치 함수
    private async Task install_image(string image_name)
    {
        try
        {
            // 이미지가 없는 경우
            if (!(await is_image_exist(image_name)))
            {
                // 이미지가 없으므로 Docker Hub에서 자동으로 다운로드(Pull) 한다.
                AnsiConsole.Markup($"[bold yellow]⚡ {image_name} 이미지가 존재하지 않습니다. 다운로드를 시작합니다![/]\n");
                await docker_client.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = image_name.Split(':')[0],
                    Tag = image_name.Contains(':') ? image_name.Split(':')[1] : "latest" // 이미지 설치의 경우 우선 latest 로
                },
                    null,
                    new Progress<JSONMessage>(message =>
                    {
                        if (!string.IsNullOrEmpty(message.Status))
                        {
                            AnsiConsole.Markup($"[bold blue]{message.Status}[/]\n");
                        }
                    }));
                AnsiConsole.Markup($"[bold green]{image_name} : 이미지 다운로드가 완료되었습니다![/]\n");
            }
            else
            {

                AnsiConsole.Markup($"[bold green]{image_name}[/] : 이미지 확인이 완료되었습니다.\n");
            }
        }
        catch (Exception ex)
        {
            // 예외를 상위로 전달.
            throw new Exception($"이미지 확인 및 다운로드 중 오류 발생: {ex.Message}", ex);
        }
    }

    // 컨테이너 id로 컨테이너의 출력을 실시간으로 콘솔에 출력하는 함수
    private async Task print_container_log_async(string container_id)
    {
        // Docker 컨테이너 로그를 가져올 때 사용할 로그 파라미터 설정
        var log_parameters = new ContainerLogsParameters
        {
            Follow = true,      // 로그를 지속적으로 스트리밍하도록 설정
            ShowStdout = true,  // 표준 출력 로그를 포함
            ShowStderr = true   // 표준 오류 로그를 포함
        };

        // Docker 클라이언트를 사용하여 비동기적으로 컨테이너 로그 스트림을 가져온다
        using var log_stream = await docker_client.Containers.GetContainerLogsAsync(container_id, log_parameters, CancellationToken.None);

        // 스트림을 읽기 위한 StreamReader 생성
        using var stream_reader = new StreamReader(log_stream);

        // 로그 스트림을 지속적으로 읽고 출력하는 루프
        while (true)
        {
            // 한 줄씩 로그를 비동기적으로 읽는다
            var line = await stream_reader.ReadLineAsync();

            // 더 이상 읽을 로그가 없으면 루프 종료
            if (line == null)
                break;

            // 읽은 로그를 콘솔에 출력
            AnsiConsole.WriteLine(line);
        }
    }

    /*
      semgrep 으로 scan 하는 함수
        source_path : 스캔할 소스코드가 모여 있는 폴더(=디렉토리) 경로
        result_path : 스캔 결과 json 파일을 저장할 경로
    */
    public async Task scan_semgrep(string source_path, string result_path)
    {
        // API_Key가 파일에 제대로 세팅되어 있는지 확인하기
        APIKeys api_keys = new APIKeys();
        string semgrep_token = api_keys.semgrep_cli_token;
        if (string.IsNullOrEmpty(semgrep_token) || semgrep_token == APIKeys.EMPTY_API_VALUE)
            throw new ArgumentException("Semgrep API 토큰이 비어있습니다. api_key.json 파일을 확인해주세요.");

        // 확인 완료되었으면 스캔 시작
        SemgrepScanner semgrep_scanner = new SemgrepScanner(semgrep_token, docker_image[SecurityTool.Semgrep]);
        await semgrep_scanner.scan(source_path, result_path);
    }


    /*
      단일 책임 원칙(SRP)에 맞춰 Semgrep 관련 기능을 SemgrepScanner 이라는 객체로 분리.
      아직 객체지향 5원칙 (SOLID) 에 대해 제대로 공부해본적은 없지만 우선적으로 SRP를 적용해보았다.
      단일 책임 원칙 : 객체는 단 하나의 책임만 가져야 한다는 원칙
      :: 여기서 책임 = 기능 담당, 즉, 하나의 클래스는 하나의 기능 담당하여 하나의 책임을 수행하는데 집중되어야 있어야 한다는 의미
      출처: https://inpa.tistory.com/entry/OOP
      다만 Semgrep은 Docker로 돌리고 있기 때문에 DockerRunner에 속한 중첩 클래스로 외부에 노출하지 않게 구현.
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
        public async Task scan(string source_path, string result_path)
        {
            // 입력 검증 단계 =========================================================================================
            // 스캔 파일 경로가 비어있거나, 유효하지 않으면 예외 던지고 종료
            if (string.IsNullOrEmpty(source_path) || !Directory.Exists(source_path))
                throw new ArgumentException($"소스 코드 경로가 비어있거나 유효하지 않습니다: {source_path}");

            // 결과 파일 경로가 파일 경로가 아닌 디렉토리인 경우
            if (Directory.Exists(result_path))
                throw new ArgumentException($"결과 파일 경로는 폴더 경로일 수 없습니다: {result_path}");

            // 결과 파일 경로가 json 파일이 아닌 경우
            if (!result_path.EndsWith(".json"))
                throw new ArgumentException($"결과 파일 경로는 json 파일이어야 합니다: {result_path}");
            // ===================================================================================================
            // 상대 경로 -> 절대 경로 변환
            /*
              Docker run 명령어를 통해 실행시 일반적으로 Docker은 마운트 경로가 상대 경로면 경로를 찾지 못한다고 한다.
              따라서 상대 경로가 들어와도 절대 경로로 변환한다.
              여기서 상대 경로->절대 경로로 변환하는 기준의 경우엔 이 cli 프로그램이 실행되는 위치를 기준으로 한다.
              :: 다만, docker - compose는 상대 경로를 줘도 마운트가 가능하다고 한다.
              이 정보는 DeepSeek 검색 엔진 & R1, ChatGPT4o 의 검색 결과에 기반한다.
            */
            string abs_source_path = Path.GetFullPath(source_path);
            string abs_result_path = Path.GetFullPath(result_path);

            // 절대 경로에서 파일명만 추출 (확장자 포함)
            string result_file_name = Path.GetFileName(abs_result_path); // 값 : *.json

            // result_path 의 디렉토리 경로만 추출
            string result_dir_path = Path.GetDirectoryName(abs_result_path);

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
                    Cmd = new List<string>
                    {
                        /*
                          우선 rules set을 아주 많이 넣어서 스캔 적중률을 강화하는 방향으로 진행.
                          속도가 많이 느려지긴 하나 보안 취약점을 최대한 찾아내기 위함. (추후 최적화 필수로 필요.)
                          Ryzen 5600x 를 기준으로 1777개의 파일을 스캔하는데 약 10~20초 정도
                        */
                        "semgrep",                    // semgrep 실행 파일 실행
                        "--config=p/security-audit",  // 보안 감사용 규칙셋
                        "--config=p/xss",             // XSS 취약점 규칙셋
                        "--config=p/sql-injection",   // SQL Injection 규칙셋
                        "--config=p/secrets",         // git에 하드코딩으로 커밋되서 올라간 비밀번호, 키워드 등을 찾는 규칙셋
                        "--config=p/cwe-top-25",      // cwe-top-25 : 애플리케이션 보안 위험 상위 25개를 다룬 업계 표준 보고서
                        "--config=p/r2c-security-audit", // 코드의 잠재적 보안 문제를 스캔, 추가 검토가 필요하도록 표시하는 도구
                        "--config=p/owasp-top-ten",      // owasp-top-ten : 웹 애플리케이션 보안 위험 상위 10개를 다룬 업계 표준 보고서
                        "--config=p/gitleaks",        // git 에 커밋된 api key, 비밀번호 같은걸 찾는 규칙셋
                        // "--json",                        // 결과를 json 형식으로 출력, 주지 않으면 그냥 터미널에 semgrep이 알아서 정리해서 출력
                        $"--json-output=/output/{result_file_name}", // json 결과 파일 경로
                    },

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
                        $"SEMGREP_APP_TOKEN={cli_token}" // 환경 변수 설정
                        ,
                        // "NO_COLOR=1" // ANSI 색상 출력 비활성화
                    },
                };

                // 컨테이너 생성
                var response = await runner.docker_client.Containers.CreateContainerAsync(container_config);

                // 컨테이너 실행
                await runner.docker_client.Containers.StartContainerAsync(response.ID, null);
                AnsiConsole.Markup($"[bold cyan]\ud83d\udd0d Semgrep :[/] [bold yellow]{abs_source_path}[/] 에서 스캔을 시작합니다.\n");

                // 컨테이너 실행 로그를 실시간 출력
                await runner.print_container_log_async(response.ID);

                // 컨테이너 종료 대기
                await runner.docker_client.Containers.WaitContainerAsync(response.ID);
                AnsiConsole.Markup($"[bold cyan]✅ Semgrep :[/] [bold yellow]{abs_source_path}[/] 에서 스캔을 완료했습니다!\n");

                // 출력 완료 메세지
                AnsiConsole.Markup($"[bold cyan]📂 Semgrep :[/] 결과 파일을 [bold yellow]{abs_result_path}[/] 에 저장했습니다.\n");

                // 만들어진 json 파일을 후처리
                postprocess_semgrep_result(result_path);

                // 후처리 완료 메세지
                AnsiConsole.Markup($"[bold cyan]\u2728 Semgrep :[/] 결과 파일 후처리가 완료되었습니다.\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[bold red]Semgrep :[/] 오류가 발생했습니다: {ex.Message}\n");
            }
        }

        /*
          semgrep 에서 스캔이 완료되어 json 파일이 생성되면 이를 후처리하는 함수.
          기본적으로 semgrep 의 json 출력은 깔끔하게 format 되어 있지 않아서 formatting 시키고, 번역도 시킨다.
          테스트를 위해 우선은 public 처리, 나중에 캡슐화를 위해 private 처리할 예정.
        */
        public void postprocess_semgrep_result(string result_path)
        {
            if (string.IsNullOrEmpty(result_path) || !File.Exists(result_path))
                throw new ArgumentException($"json 파일 경로가 비어있거나 해당 파일이 존재하지 않습니다 후처리 과정을 진행할 수 없습니다. : {result_path}\n");

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

            // 결과가 비어있지 않으면 번역 Logic 수행
            if (root.results.Length != 0)
            {
                // 파일 저장 메세지 출력
                AnsiConsole.Markup("[bold cyan]Semgrep :[/] 결과 메세지를 처리합니다.\n");

                // 결과 메세지를 반복하여 읽기
                foreach (var result in root.results)
                {
                    string message = result.extra.message;
                    var paths = new Global.Global.Paths();

                    // json 번역 객체 생성
                    JsonTranslator translator = new JsonTranslator(paths.semgrep_translate_file_path);

                    // 우선 패턴 번역을 수행해서 결과값이 있는지 확인
                    string pattern_result = translator.translate_text(message, JsonTranslator.TranslateType.Pattern);

                    // 결과값이 비었다면 패턴 번역 실패, 딕셔너리 번역도 확인
                    if (pattern_result == "")
                    {
                        string dic_result = translator.translate_text(message, JsonTranslator.TranslateType.Dictionary);

                        // 딕셔너리 번역 성공시
                        if (dic_result != "")
                        {
                            // 번역된 내용을 json 에 반영
                            result.extra.message = dic_result;
                        }
                        // 딕셔너리 번역 실패시에는 파일에 번역하도록 파일안 딕셔너리 탭에 값 등록
                        else
                        {
                            // Semgrep 번역 JSON 파일 경로
                            string translate_file_path = paths.semgrep_translate_file_path;

                            // 번역 JSON 파일이 존재하면 로드, 없으면 새로 생성
                            TranslateJsonRootObject? translate_root;
                            if (File.Exists(translate_file_path))
                            {
                                string translate_json = File.ReadAllText(translate_file_path, Encoding.UTF8);
                                translate_root = JsonSerializer.Deserialize<TranslateJsonRootObject>(translate_json);
                            }
                            else
                            {
                                translate_root = new TranslateJsonRootObject();
                            }

                            if (translate_root == null)
                                throw new Exception("[bold red]Error : 번역 JSON 파일을 파싱하는데 실패했습니다.[/]\n");

                            // 딕셔너리 목록이 존재하지 않으면 새로 할당
                            if (translate_root.dictionary == null)
                                translate_root.dictionary = new dictionary[] { };

                            // 기존 딕셔너리에 없는 경우 추가
                            if (!translate_root.dictionary.Any(entry => entry.origin == message))
                            {
                                var new_entry = new dictionary { origin = message, message = "" }; // 번역 필요한 것은 "" 로 빈 문자열로 등록. 여기 안에 번역할 값을 JSON 파일을 열어서 직접 쓰면 된다.
                                var updated_dictionary = translate_root.dictionary.ToList();
                                updated_dictionary.Add(new_entry);
                                translate_root.dictionary = updated_dictionary.ToArray();

                                // 변경된 데이터를 JSON 파일에 저장
                                string updated_json = JsonSerializer.Serialize(translate_root, new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                });

                                File.WriteAllText(translate_file_path, updated_json, Encoding.UTF8);

                                // 로그 출력
                                AnsiConsole.Markup($"[bold yellow]번역 파일에 새로운 항목 추가:[/] {message}\n");
                            }
                        }
                    }
                }
            }

            // result를 다시 json으로 직렬화하고 파일로 저장
            // 어차피 포맷팅 해서 깔끔하게 저장해야 하기에 번역 여부와 관계없이 필요한 작업
            string json_result = JsonSerializer.Serialize(root, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 한글이 유니코드 이스케이프 없이 저장됨, 이걸 안주면 \uXXXX 이런식으로 저장된다.
            });
            File.WriteAllText("translation_semgrep_test.json", json_result, Encoding.UTF8);


            using var json_doc = JsonDocument.Parse(json_content); // 문자열 -> JSON 객체로 파싱
            var options = new JsonSerializerOptions { WriteIndented = true }; // 들여쓰기 옵션 줘서 포맷팅
            string formatted_json = JsonSerializer.Serialize(json_doc.RootElement, options); // 직렬화 시켜서 json 객체 -> 문자열로 얻어내기

            // 원본 파일에 영향 안 주기 위해 새로운 파일 이름으로 변경
            string json_file_name = Path.GetFileName(result_path);
            string replaced_file_name = json_file_name.Replace(".json", "_formatted.json");
            string replaced_file_path = Path.Combine(Path.GetDirectoryName(result_path) ?? ".", replaced_file_name);


            // 포맷팅된 JSON을 파일에 덮어쓰기
            File.WriteAllText(replaced_file_path, formatted_json, Encoding.UTF8);
            AnsiConsole.Markup("[bold cyan]Semgrep :[/] JSON 포맷팅이 완료되었습니다.\n");
        }
    }
}