namespace Code_Nova_Guardian.Class;
using Docker.DotNet;
using Docker.DotNet.Models;
using Spectre.Console;
using static Code_Nova_Guardian.Global.Global;

/*
  Docker을 활용한 여러 기능들을 간편하게 사용할 수 있는 DockerRunner 객체의 설계도(Class)
  아래는 지원 기능
  1. 여러 보안 취약점 도구 CLI 호출 및 결과 제공
  2. Docker CLI가 Host와 같은 위치에 있는지 체크 (Docker 설치여부 체크)
*/

/*
  부분 클래스로 선언.
  C#의 partial 키워드를 활용하면, 클래스의 내용의 일부를 여러 파일로 나눌 수 있다.
  현재 DockerRunner 에서 의존하는 보안 취약점 도구들은 DockerRunner 의 중첩 클래스로써 구현되어 있는데,
  한 DockerRunner 파일 안에 중첩 클래스가 들어가버리면 가독성이 떨어져서 이를 파일로 따로 분리하기 위해 사용한다.
*/
public partial class DockerRunner
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
            ShowStderr = true,   // 표준 오류 로그를 포함
            Timestamps = false // 타임 스탬프는 비활성, 활성화시 콘솔 출력 앞에 시간이 자동 추가
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
            // AnsiConsole 로 출력하지 않아야 원래 콘솔에서 보던것과 동일하게 나옴을 확인
            Console.WriteLine(line);
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

        //Console.WriteLine($"Semgrep API 토큰: {semgrep_token}");
        if (string.IsNullOrEmpty(semgrep_token) || semgrep_token == APIKeys.EMPTY_API_VALUE)
            throw new ArgumentException("Semgrep API 토큰이 비어있습니다. api_key.json 파일을 확인해주세요.");

        // 확인 완료되었으면 스캔 시작
        SemgrepScanner semgrep_scanner = new SemgrepScanner(semgrep_token, docker_image[SecurityTool.Semgrep]);
        await semgrep_scanner.scan(source_path, result_path);
        //semgrep_scanner.post_process(result_path);
    }
}