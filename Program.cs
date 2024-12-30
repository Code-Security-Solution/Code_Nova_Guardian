using Code_Nova_Guardian.Class;


namespace Code_Nova_Guardian
{
    public class Program
    {
        // 비동기 메서드 호출을 위해선 기본적으로 await를 붙여야 하며, await 를 호출 하는쪽은
        // async Method로 선언 및 Task Return이 필요하다.
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
        // verbose : 활성화 시 추가적인 디버깅 메세지를 출력한다.
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

            // Docker 설치되어있는지 확인, DockerRunner은 Docker.DotNet을 한 번 더
            // Wrapping 해서 Docker 관련 여러 편의 기능 제공 (Custom Class)
            bool is_docker_installed = await DockerRunner.check_installation();
            if (!is_docker_installed) Environment.Exit(1);
        }
    }
}