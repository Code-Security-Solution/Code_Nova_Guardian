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
            {
                docker_client.Dispose();
            }
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

        // semgrep 으로 scan 하는 함수
        // source_path : 스캔할 소스코드가 모여 있는 폴더(=디렉토리) 경로
        public async Task scan_semgrep(string source_path)
        {
            if (string.IsNullOrEmpty(source_path))
                throw new ArgumentException("스캔할 소스 파일 경로가 비어있습니다.", nameof(source_path));

            try
            {
                var container_config = new CreateContainerParameters
                {
                    // 이미지 이름
                    Image = "returntocorp/semgrep",

                    // 실행할 명령어 인자
                    Cmd = new List<string> { "semgrep", "--config=auto" },
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string>
                        {
                            $"{source_path}:/src" // source_path 경로를 컨테이너 내부의 /src에 마운트
                        },
                        AutoRemove = true // 컨테이너 종료 시 자동 삭제
                    }
                };

                // 컨테이너 생성
                var response = await docker_client.Containers.CreateContainerAsync(container_config);

                // 컨테이너 실행
                await docker_client.Containers.StartContainerAsync(response.ID, null);
                AnsiConsole.Markup($"[bold green]Semgrep : {source_path} 에서 스캔을 시작합니다.[/]\n");

                // 컨테이너 종료 대기
                await docker_client.Containers.WaitContainerAsync(response.ID);
                AnsiConsole.Markup($"[bold cyan]Semgrep : {source_path} 에서 스캔을 완료했습니다![/]\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[bold red]Semgrep : 오류가 발생했습니다: {ex.Message}[/]\n");
            }
        }
    }
}
