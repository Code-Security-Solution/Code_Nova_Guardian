using Docker.DotNet.Models;
using Spectre.Console;

namespace Code_Nova_Guardian.DockerRunner
{
    public partial class DockerRunner
    {
        /*
          스캔 이외에 Semgrep 을 사용하기 위한 유틸리티 객체의 설계도 (Class) 
        */
        private class SemgrepUtility
        {
            // semgrep 이미지 이름
            private string image_name;

            // 생성자에선 image_name 값을 받는다.
            public SemgrepUtility(string image_name)
            {
                this.image_name = image_name;
            }

            public async Task get_token()
            {
                // Docker Runner 객체 생성
                DockerRunner runner = new DockerRunner();

                // 필요한 이미지가 없다면 자동 설치, 딕셔너리에서 자동 이름 참고
                await runner.install_image(image_name);

                // 토큰을 얻어내기 위해 Semgrep 컨테이너를 -it (interaction) 모드로 실행하고 login 파라미터를 준 후 실행
                try
                {
                    var container_config = new CreateContainerParameters
                    {
                        Image = image_name,

                        // semgrep login 명령 실행
                        Cmd = new List<string>
                        {
                            "semgrep",
                            "login"
                        },

                        HostConfig = new HostConfig
                        {
                            AutoRemove = true
                        },

                        Tty = true, // -it 옵션을 위해 TTY 설정 필요
                        OpenStdin = true, // 표준 입력을 열어 사용자 입력을 받을 수 있도록 설정
                        StdinOnce = true  // 표준 입력이 한 번만 사용됨을 명시
                    };

                    // 컨테이너 생성
                    var response = await runner.docker_client.Containers.CreateContainerAsync(container_config);

                    // 컨테이너 실행
                    bool started = await runner.docker_client.Containers.StartContainerAsync(response.ID, null);
                    if (!started)
                        throw new Exception("Semgrep login 컨테이너 실행에 실패했습니다.");

                    AnsiConsole.MarkupLine("[bold yellow]ℹ\ufe0f 곧 콘솔에 [underline blue]https://semgrep.dev/login...[/] 링크가 나타납니다.[/]");
                    AnsiConsole.MarkupLine("[aqua]클릭[/] 또는 [aqua]Ctrl + 클릭[/]으로 링크에 들어가서 [green]Semgrep[/] 페이지가 나오면,");
                    AnsiConsole.MarkupLine("가능한 방법으로 로그인 후 [bold blue]Activate[/] 버튼을 눌러주세요. 🚀\n\n");

                    // 콘솔 로그 출력
                    await runner.print_container_log_async(response.ID);
                }
                catch (Exception ex)
                {
                    AnsiConsole.Markup($"[bold red]Semgrep Login 오류:[/] {ex.Message}\n");
                }
            }
        }
    }
}
