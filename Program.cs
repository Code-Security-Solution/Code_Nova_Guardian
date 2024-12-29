using Docker.DotNet;

namespace Code_Nova_Guardian
{
    public class Program
    {
        // 비동기 메서드 호출을 위해선 기본적으로 await를 붙여야 하며, await 를 호출 하는쪽은
        // async Method로 선언 및 Task Return이 권장된다.
        public static async Task Main(string[] args)
        {
            // 작업 실행 전 필요 프로그램이 설치 되어 있나 체크 (비동기 호출)
            await check_requirement();

            // SonarQube 실행 로직 시작
            process_sonar_qube();

            // Semgrep 실행 로직 시작
            process_semgrep();
        }


        /*
          Main 함수가 static 함수이므로, 객체 생성 없이 바로 함수를 호출하려면 static 함수여야 한다.
          참고 : https://jettstream.tistory.com/571
        */
        private static void process_sonar_qube()
        {
            // TODO : implement here
        }
        private static void process_semgrep()
        {
            // TODO : implement here
        }

        // 해당 함수는 해당 CLI 프로그램을 사용하기 위해 필요한 프로그램들이 설치되어 있는지 검사하는 함수다.
        private static async Task check_requirement()
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

            // Docker 설치되어있는지 확인
            await check_docker_installation();
        }

        // Docker Engine 설치 여부 확인
        private static async Task check_docker_installation()
        {
            Console.WriteLine("Docker Host의 상태를 확인합니다.");

            try
            {
                /*
                  DockerClientConfiguration 생성자에 아무 값 안주면 알아서 Unix Socket이나 pipe 활용해서 IPC로 Docker Engine에 통신 하는 듯 하다.
                  만약 Docker Host가 CLI 와 다른 컴퓨터에 있는 경우 http 주소등을 명시해줘야 하고, socket 파일 이름을 명시적으로 줄 수도 있다.
                  추가로 컴퓨터 네트워크 시간에 배운 내용에 따르면, Unix socket을 socat 과 같은 Tool을 이용해 개방시켜 공유할 수도 있다. (Advanced Technique)
                */
                using (var client = new DockerClientConfiguration().CreateClient())
                {
                    // Docker 버전 정보 요청 (Docker가 실행 중인지 확인)
                    var version = await client.System.GetVersionAsync();

                    // 문제가 없다면 위에서 Exception Jump 없이 이 부분이 실행되고 함수는 종료되게 된다.
                    Console.WriteLine("Docker가 Host에서 실행중입니다.");
                    Console.WriteLine($"도커 버전: {version.Version}");
                    Console.WriteLine($"API 버전: {version.APIVersion}");
                }
            }
            catch (DockerApiException api_ex)
            {
                Console.WriteLine("Docker가 실행 중이 아니거나 이 호스트에 설치되지 않았습니다.");
                Console.WriteLine($"Docker API 오류: {api_ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("예기치 못한 오류가 발생했습니다.");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}