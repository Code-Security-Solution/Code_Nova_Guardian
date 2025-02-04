/*
  Docker을 활용한 여러 기능들을 간편하게 사용할 수 있는 DockerRunner 객체의 설계도(Class)
  아래는 지원 기능
  1. 여러 보안 취약점 도구 CLI 호출 및 결과 제공
  2. Docker CLI가 Host와 같은 위치에 있는지 체크 (Docker 설치여부 체크)
*/

using Docker.DotNet;
using Docker.DotNet.Models;
using Spectre.Console;

namespace Code_Nova_Guardian.Class
{
    public class DockerRunner
    {
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
            Console.WriteLine("Docker Host의 상태를 확인합니다.");

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
                AnsiConsole.Markup("[bold red]Docker가 실행 중이 아니거나 이 호스트에 설치되지 않았습니다.[/]\n");
                AnsiConsole.Markup("[bold red]Docker가 설치됐으면 실행해주시고, 그렇지 않다면 설치해주세요.[/]\n");
                AnsiConsole.Markup($"[red]Docker API 오류:[/] [italic]{api_ex.Message}[/]\n");
                return false; // 실패 Task 반환
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup("[bold red]예기치 못한 오류가 발생했습니다.[/]\n");
                AnsiConsole.Markup($"[red]오류:[/] [italic]{ex.Message}[/]\n");
                return false; // 실패 Task 반환
            }
        }


        // semgrep scan 시 필요한 이미지 설치
        private async Task install_semgrep_image(string image_name)
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

                // 이미지가 없는 경우
                if (images == null || images.Count == 0)
                {
                    // 이미지가 없으므로 Docker Hub에서 자동으로 다운로드(Pull)합니다.
                    AnsiConsole.Markup($"[bold yellow]{image_name} : 이미지가 존재하지 않습니다. 다운로드를 시작합니다...[/]\n");
                    await docker_client.Images.CreateImageAsync(new ImagesCreateParameters
                    {
                        FromImage = image_name.Split(':')[0],
                        Tag = image_name.Contains(':') ? image_name.Split(':')[1] : "latest"
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

                    AnsiConsole.Markup($"[bold green]{image_name} : semgrep 이미지 확인이 완료되었습니다.[/]\n");
                }
            }
            catch (Exception ex)
            {
                // 예외를 상위로 전달.
                throw new Exception($"이미지 확인 및 다운로드 중 오류 발생: {ex.Message}", ex);
            }
        }


        /*
          semgrep 으로 scan 하는 함수
            source_path : 스캔할 소스코드가 모여 있는 폴더(=디렉토리) 경로
            result_path : 스캔 결과 json 파일을 저장할 경로
        */
        public async Task scan_semgrep(string source_path, string result_path = "")
        {
            // 스캔 파일 경로가 비어있거나, 유효하지 않으면 예외 던지고 종료
            if (string.IsNullOrEmpty(source_path) || !Directory.Exists(source_path))
                throw new ArgumentException($"소스 코드 경로 비어있거나 유효하지 않습니다: {source_path}");


            // 상대 경로 -> 절대 경로 변환
            /*
              Docker run 같은걸로 실행시 일반적으로 docker은 마운트 경로가 상대 경로면 경로를 찾지 못한다고 한다.
              따라서 상대 경로가 들어와도 절대 경로로 변환한다.
              여기서 상대 경로->절대 경로로 변환하는 기준의 경우엔 이 cli 프로그램이 실행되는 위치를 기준으로 한다.
              :: 다만, docker - compose는 상대 경로를 줘도 마운트가 가능하다고 한다.
              이 정보는 DeepSeek 검색 엔진 & R1, ChatGPT4o 의 검색 결과에 기반한다.
            */
            string abs_source_path = Path.GetFullPath(source_path);

            // 설치할 이미지 명
            string image_name = "returntocorp/semgrep";

            try
            {
                // 필요한 이미지가 없다면 설치
                await install_semgrep_image(image_name);

                var container_config = new CreateContainerParameters
                {
                    // 이미지 이름
                    Image = image_name,

                    // 실행할 명령어 인자
                    Cmd = new List<string> { "semgrep", "--config=auto" },
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
                            $"{abs_source_path}:/src"
                        },
                        AutoRemove = true // 컨테이너 종료 시 자동 삭제, 스캔만 하고 버릴것이기에 필수
                    }
                };

                // 컨테이너 생성
                var response = await docker_client.Containers.CreateContainerAsync(container_config);

                // 컨테이너 실행
                await docker_client.Containers.StartContainerAsync(response.ID, null);
                AnsiConsole.Markup($"[bold green]Semgrep : {abs_source_path} 에서 스캔을 시작합니다.[/]\n");

                // 컨테이너 종료 대기
                await docker_client.Containers.WaitContainerAsync(response.ID);
                AnsiConsole.Markup($"[bold cyan]Semgrep : {abs_source_path} 에서 스캔을 완료했습니다![/]\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[bold red]Semgrep : 오류가 발생했습니다: {ex.Message}[/]\n");
            }
        }
    }
}
