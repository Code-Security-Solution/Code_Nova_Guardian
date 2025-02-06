/*
  Docker을 활용한 여러 기능들을 간편하게 사용할 수 있는 DockerRunner 객체의 설계도(Class)
  아래는 지원 기능
  1. 여러 보안 취약점 도구 CLI 호출 및 결과 제공
  2. Docker CLI가 Host와 같은 위치에 있는지 체크 (Docker 설치여부 체크)
*/

using Code_Nova_Guardian.API_Keys;
using Docker.DotNet;
using Docker.DotNet.Models;
using Spectre.Console;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// ReSharper disable All

namespace Code_Nova_Guardian.Class
{
    public class DockerRunner
    {
        // DockerRunner 클래스에서 실행 가능한 보안 취약점 도구 목록
        public enum SecurityTool
        {
            Semgrep
        }

        // 각 보안 취약점 도구의 이미지 이름
        // 딕셔너리로 대응시켜 직접 관리
        private Dictionary<SecurityTool, string> image_dic = new Dictionary<SecurityTool, string>
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
                    AnsiConsole.Markup("[bold red]Docker가 Host에서 실행중입니다![/]\n");
                    AnsiConsole.Markup($"[cyan]도커 버전:[/] [bold]{version.Version}[/]\n");
                    AnsiConsole.Markup($"[cyan]API 버전:[/] [bold]{version.APIVersion}[/]\n");
                    return true; // 성공 Task 반환
                }
            }
            catch (DockerApiException api_ex)
            {
                AnsiConsole.Markup($"[red]Docker API 오류:[/] [italic]{api_ex.Message}[/]\n");
                AnsiConsole.Markup("[bold red]Docker API 관련 오류가 발생했습니다. Docker의 상황이 정상인지 확인해주세요.[/]\n");
                return false; // 실패 Task 반환
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup("[bold red]예기치 못한 오류가 발생했습니다.[/]\n");
                AnsiConsole.Markup($"[red]오류:[/] [italic]{ex.Message}[/]\n");
                AnsiConsole.Markup("[bold red]Docker가 실행 중이 아니거나 이 호스트에 설치되지 않았습니다.[/]\n");
                AnsiConsole.Markup("[bold red]Docker가 설치됐으면 실행해주시고, 그렇지 않다면 설치해주세요.[/]\n");
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
                    AnsiConsole.Markup($"[bold yellow]{image_name} : 이미지가 존재하지 않습니다. 다운로드를 시작합니다...[/]\n");
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

                    AnsiConsole.Markup($"[bold green]{image_name} : 이미지 확인이 완료되었습니다.[/]\n");
                }
            }
            catch (Exception ex)
            {
                // 예외를 상위로 전달.
                throw new Exception($"이미지 확인 및 다운로드 중 오류 발생: {ex.Message}", ex);
            }
        }

        // 컨테이너 id로 로그 실시간 출력 + ANSI 제거 후 파일 기록
        private async Task print_container_log_async(string container_id)
        {
            // 로그 파일 경로 설정
            string log_file_path = Path.Combine(Directory.GetCurrentDirectory(), "container_logs_clean.log");

            // 컨테이너 로그 파라미터 설정 (실시간 스트림, 표준 출력/에러 포함)
            var logs_params = new ContainerLogsParameters
            {
                Follow = true,
                ShowStdout = true,
                ShowStderr = true,
                Timestamps = false
            };

            try
            {
                using (var stream = await docker_client.Containers.GetContainerLogsAsync(container_id, logs_params, CancellationToken.None))
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(log_file_path, append: true, Encoding.UTF8)) // ANSI 없는 클린 로그 저장
                    {
                        await writer.WriteLineAsync($"[{DateTime.UtcNow}] 컨테이너 {container_id} 로그 시작\n");

                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            // ANSI 코드 및 애니메이션 문자를 제거
                            string clean_line = remove_ansi_sequences_for_semgrep(line);

                            if (string.IsNullOrWhiteSpace(clean_line)) continue; // 빈 줄은 기록하지 않음

                            // 진행률 및 남은 시간 추출
                            var progress_info = extract_progress_info(line);

                            if (progress_info != null)
                                Console.WriteLine($"진행률: {progress_info.Value.progress}% | 진행 시간: {progress_info.Value.remaining_time}");
                            else
                                Console.WriteLine(clean_line); // 콘솔 출력

                            await writer.WriteLineAsync(clean_line); // 클린 로그 파일 저장
                        }

                        await writer.WriteLineAsync($"[{DateTime.UtcNow}] 컨테이너 {container_id} 로그 종료\n");
                    }
                }
            }
            catch (DockerApiException ex)
            {
                AnsiConsole.MarkupLine($"[red]로그 수신 오류: {ex.Message}[/]");
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]로그 수신이 취소되었습니다.[/]");
            }
        }

        // 로그에서 진행률 및 남은 시간 추출
        private static (int progress, string remaining_time)? extract_progress_info(string input)
        {
            // 정규식 패턴: "숫자% 숫자:숫자:숫자"
            string pattern = @"(\d+)%\s+(\d+:\d+:\d+)";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                int progress = int.Parse(match.Groups[1].Value);
                string remaining_time = match.Groups[2].Value;
                return (progress, remaining_time);
            }

            return null; // 진행률 정보가 없으면 null 반환
        }

        /*
          ANSI 이스케이프 코드 및 애니메이션 문자를 제거하는 함수, 아직은 semgrep 전용
          semgrep 이 python 으로 실행되며 rich 라는 라이브러리를 사용해서 ANSI 색상을 출력하는데,
          로그 출력엔 불필요 하기에 제거한다.
          또한 이유는 모르겠지만 다이아몬드 모양이나 각종 유니코드 모양이 파워쉘 & 윈도우 터미널로 실행을 해도
          글자가 깨지는 채로 출력되서 콘솔 출력에서도 제거한다.
        */
        private string remove_ansi_sequences_for_semgrep(string input)
        {
            // ANSI 이스케이프 코드 정규식 패턴
            string ansi_pattern = @"\x1B\[[0-9;]*[mK]";
            string cleaned = Regex.Replace(input, ansi_pattern, ""); // ANSI 코드 제거

            // Semgrep 애니메이션 로딩 문자, 쓸모없는 특수문자들 제거 (⠋, ⠙, ⠹, ⠼, 💎)
            string[] forbidden_chrs = { "⠋", "⠙", "⠹", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏", "⠸", "\ud83d\udc8e " };
            foreach (var ch in forbidden_chrs)
            {
                cleaned = cleaned.Replace(ch, "");
            }

            return cleaned.Trim(); // 앞뒤 공백 제거
        }


        /*
          semgrep 으로 scan 하는 함수
            source_path : 스캔할 소스코드가 모여 있는 폴더(=디렉토리) 경로
            result_path : 스캔 결과 json 파일을 저장할 경로
        */
        public async Task scan_semgrep(string source_path, string result_path)
        {
            // 입력 검증 =========================================================================================
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
            // source_path 상대 경로 -> 절대 경로 변환
            /*
              Docker run 같은걸로 실행시 일반적으로 docker은 마운트 경로가 상대 경로면 경로를 찾지 못한다고 한다.
              따라서 상대 경로가 들어와도 절대 경로로 변환한다.
              여기서 상대 경로->절대 경로로 변환하는 기준의 경우엔 이 cli 프로그램이 실행되는 위치를 기준으로 한다.
              :: 다만, docker - compose는 상대 경로를 줘도 마운트가 가능하다고 한다.
              이 정보는 DeepSeek 검색 엔진 & R1, ChatGPT4o 의 검색 결과에 기반한다.
            */
            string abs_source_path = Path.GetFullPath(source_path);

            // 동일하게 result_path 도 상대 경로면 절대 경로로 변환
            string abs_result_path = Path.GetFullPath(result_path);

            // 절대 경로에서 파일명만 추출 (확장자 포함)
            string result_file_name = Path.GetFileName(abs_result_path); // 값 : *.json

            // result_path 의 디렉토리 경로 추출
            string result_dir_path = Path.GetDirectoryName(abs_result_path);

            // 설치할 이미지 명
            string image_name = image_dic[SecurityTool.Semgrep];

            // APIKeysLocal.cs 에서 api_key는 관리. 커밋되지 않으므로 안전하게 로컬에서 사용.
            string semgrep_token = APIKeys.semgrep_token;
            if (string.IsNullOrEmpty(semgrep_token))
                throw new ArgumentException("Semgrep API 토큰이 비어있습니다. APIKeysLocal.cs 파일을 확인해주세요.");

            try
            {
                // 필요한 이미지가 없다면 자동 설치, 딕셔너리에서 자동 이름 참고
                await install_image(image_name);

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
                        "--json",                        // 결과를 json 형식으로 출력
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
                        $"SEMGREP_APP_TOKEN={semgrep_token}" // 환경 변수 설정
                        ,
                        "NO_COLOR=1" // ANSI 색상 출력 비활성화
                    },
                };

                // 컨테이너 생성
                var response = await docker_client.Containers.CreateContainerAsync(container_config);

                // 컨테이너 실행
                await docker_client.Containers.StartContainerAsync(response.ID, null);
                AnsiConsole.Markup($"[bold green]Semgrep : {abs_source_path} 에서 스캔을 시작합니다.[/]\n");

                // 컨테이너 실행 로그를 실시간 출력
                await print_container_log_async(response.ID);

                // 컨테이너 종료 대기
                await docker_client.Containers.WaitContainerAsync(response.ID);
                AnsiConsole.Markup($"[bold cyan]Semgrep : {abs_source_path} 에서 스캔을 완료했습니다![/]\n");

                // 출력 완료 메세지
                AnsiConsole.Markup($"[bold green]Semgrep : 결과 파일을 {abs_result_path} 에 저장했습니다.[/]\n");

                // 만들어진 json 파일을 후처리
                postprocess_semgrep_result(result_path);

                // 후처리 완료 메세지
                AnsiConsole.Markup($"[bold green]Semgrep : 결과 파일 후처리가 완료되었습니다.[/]\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[bold red]Semgrep : 오류가 발생했습니다: {ex.Message}[/]\n");
            }
        }

        // semgrep 에서 스캔이 완료되어 json 파일이 생성되면 이를 후처리하는 함수.
        // 기본적으로 semgrep 의 json 출력은 깔끔하게 format 되어 있지 않아서 formatting 시키고, 번역도 시킨다.
        // 테스트를 위해 우선은 public 처리, 나중에 캡슐화를 위해 private 처리할 예정.
        public void postprocess_semgrep_result(string result_path)
        {
            if (string.IsNullOrEmpty(result_path) || !File.Exists(result_path))
                throw new ArgumentException($"json 파일 경로가 비어있거나 유효하지 않습니다: {result_path}");

            // JSON 파일을 UTF-8 인코딩으로 읽기
            string json_content = File.ReadAllText(result_path, Encoding.UTF8);

            /*
              JsonDocument 사용하여 JSON 포맷팅
              System.Text.Json의 JsonDocument를 사용하면, 객체 변환 없이 JSON을 포맷할 수 있다.
              원래는 json 파싱을 위해 Newtonsoft.Json 이라는 외부 라이브러리를 사용했으나, C#이 최신 버전으로 오면서
              System.Text.Json 이라는 내장 라이브러리로 json 파싱을 공식적으로 지원하기 시작했다.
              성능도 이게 더 낫다니깐 이걸 써보도록 하자.
            */
            using var json_doc = JsonDocument.Parse(json_content); // 문자열 -> JSON 객체로 파싱
            var options = new JsonSerializerOptions { WriteIndented = true }; // 들여쓰기 옵션 줘서 포맷팅
            string formatted_json = JsonSerializer.Serialize(json_doc.RootElement, options); // 직렬화 시켜서 json 객체 -> 문자열로 얻어내기

            // 원본 파일에 영향 안 주기 위해 새로운 파일 이름으로 변경
            string json_file_name = Path.GetFileName(result_path);
            string replaced_file_name = json_file_name.Replace(".json", "_formatted.json");
            string replaced_file_path = Path.Combine(Path.GetDirectoryName(result_path) ?? ".", replaced_file_name);

            // 포맷팅된 JSON을 파일에 덮어쓰기
            File.WriteAllText(replaced_file_path, formatted_json, Encoding.UTF8);
            AnsiConsole.Markup("[bold green]Semgrep : JSON 포맷팅이 완료되었습니다.[/]\n");


        }
    }
}
