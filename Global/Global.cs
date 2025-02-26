using Code_Nova_Guardian.Json;
using System.Text.Json;

namespace Code_Nova_Guardian.Global;

/*
  전역 변수로 사용할 변수를 모아놓은 클래스
  static으로 두기엔 static 키워드는 메모리가 프로그램이 종료될 때까지 유지되므로
  static은 여기서 피하고, 필요할 때마다 new로 생성해서 사용하도록 한다.
*/
public class Global
{
    /*
      전역 변수를 카테고리 별로 관리하기 위해 중첩 클래스(nested class)로 구현
      Path로 하면 System.IO.Path와 충돌이 나므로 Paths로 변경
      저장할 파일들의 경로를 전역적으로 관리
    */
    public class Paths
    {
        // 모든 파일을 저장할 루트 폴더 경로.
        public string root_dir_path;

        // Semgrep 전체 파일 저장 폴더 경로
        public string semgrep_dir_path;

        // 우선 사전 파일, 패턴 파일을 따로 나누지 않고 1개의 파일로 관리, 이 파일 안에서 사전 기반 / 패턴 기반 번역 모두 수행
        public string semgrep_translate_file_path;

        // API KEY 모아놓는 파일 경로
        public string api_key_file_path;

        // 생성자, 여기서 값을 변경
        public Paths()
        {
            // 실행 중인 exe 파일이 위치한 디렉터리를 가져옴
            string exe_dir = AppContext.BaseDirectory;

            /*
              Code Nova Guardian의 약자 CNG
              이 경로는 "./CNG" 와 같이 상대 경로로 관리해선 안된다.
              이유: 확인해보니 상대 경로로 실행하면 콘솔에서 실행한 경로를 기준으로 상대 경로가 형성되어 버린다.
              즉 cd로 해당 exe 위치까지 오지 않으면 경로가 꼬여버린다는 말.
              무조건 exe 와 같은 경로에서 CNG를 참조하도록 해야 한다.
            */
            root_dir_path = Path.Combine(exe_dir, "CNG");

            // API KEY 모아놓는 파일 경로
            api_key_file_path = Path.Combine(root_dir_path, "api_key.json");

            // Semgrep 결과 파일 저장 폴더 경로
            semgrep_dir_path = Path.Combine(root_dir_path, "semgrep");

            // 우선 사전 파일, 패턴 파일을 따로 나누지 않고 1개의 파일로 관리, 이 파일 안에서 사전 기반 / 패턴 기반 번역 모두 수행
            semgrep_translate_file_path = Path.Combine(semgrep_dir_path, "translate.json");
        }
    }

    /*
      프로그램에서 사용할 API Key.
      json 파일 내부에 값을 읽어서 사용
      유출 방지를 위해 절대로 소스코드에 하드코딩하거나 하지 않는다.
    */
    public class APIKeys
    {
        // API KEY 에 value로 지정할 기본값, 외부에서 객체 생성 없이 직접 접근할 수 있어서 static으로 지정
        public static string EMPTY_API_VALUE = "YOUR_API_KEY_HERE";

        // Semgrep 에서 사용하는 CLI token
        public string semgrep_cli_token;

        // 생성자에서 생성될때마다 파일 읽기 발생
        public APIKeys()
        {
            // JSON 파일 읽기
            string json_string = File.ReadAllText(new Paths().api_key_file_path);

            // JSON을 APIKeyJsonRootObject 객체로 변환
            APIKeyJsonRootObject? api_data = JsonSerializer.Deserialize<APIKeyJsonRootObject>(json_string);
            if (api_data == null)
                throw new Exception("api key json 파일 파싱에 실패하였습니다.");
            semgrep_cli_token = api_data.semgrep;
        }
    }
}