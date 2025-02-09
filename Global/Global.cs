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

        // Semgrep 번역 사전 파일 경로
        public string semgrep_dict_path;

        // Semgrep 번역 패턴 파일 경로
        public string semgrep_pattern_path;

        // API KEY 모아놓는 파일 경로
        public string api_key_path;

        // 생성자, 여기서 값을 변경
        public Paths()
        {
            // Code Nova Guardian의 약자 CNG
            root_dir_path = "./CNG";

            // API KEY 모아놓는 파일 경로
            api_key_path = Path.Combine(root_dir_path, "api_key.json");

            // Semgrep 결과 파일 저장 폴더 경로
            semgrep_dir_path = Path.Combine(root_dir_path, "semgrep");

            // Semgrep 번역 사전 파일 경로
            semgrep_dict_path = Path.Combine(semgrep_dir_path, "translate_dict.json");

            // Semgrep 번역 패턴 파일 경로
            semgrep_pattern_path = Path.Combine(semgrep_dir_path, "translate_pattern.json");
        }
    }

    // 프로그램에서 사용할 API Key.
    // json 파일 내부에 값을 읽어서 사용
    // 유출 방지를 위해 절대로 소스코드에 하드코딩하거나 하지 않는다.
    public class APIKeys
    {
        // API KEY 에 value로 지정할 기본값
        public static string EMPTY_API_VALUE = "YOUR_API_KEY_HERE";

        // Semgrep 에서 사용하는 CLI token
        public string semgrep_cli_token;

        // 생성자에서 생성될때마다 파일 읽기 발생
        public APIKeys()
        {
            // JSON 파일 읽기
            string json_string = File.ReadAllText(new Paths().api_key_path);

            // JSON을 APIKeyJsonRootObject 객체로 변환
            APIKeyJsonRootObject api_data = JsonSerializer.Deserialize<APIKeyJsonRootObject>(json_string);
            if (api_data == null)
                throw new Exception("api key json 파일 파싱에 실패하였습니다.");
            semgrep_cli_token = api_data.semgrep;
        }
    }
}