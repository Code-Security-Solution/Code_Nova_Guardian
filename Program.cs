namespace Code_Nova_Guardian
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 작업 실행 전 필요 프로그램이 설치 되어 있나 체크
            check_requirement();

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

        }

        // 해당 함수는 해당 CLI 프로그램을 사용하기 위해 필요한 프로그램들이 설치되어 있는지 검사하는 함수다.
        private static void check_requirement()
        {
            throw new NotImplementedException();
        }
    }
}