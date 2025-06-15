using Code_Nova_Guardian.Json;
using Newtonsoft.Json;
using System.Text;

namespace Code_Nova_Guardian;
public static partial class Program
{
    // 프로그램 실행시 초기 설정을 위한 메서드
    // 콘솔 출력 인코딩을 UTF-8로 설정하고, 필요한 폴더 및 파일을 생성한다.
    private static void setup()
    {
        try
        {
            /*
              특수문자 깨짐 방지를 위해 최초로 UTF8 인코딩 설정. 
              이 세팅을 늦게 하면 오히려 콘솔 출력이 깨지는 경우가 있다.
              가장 최초로 실행해야 할 필수 코드.
              이걸로 Semgrep 출력에서 유니코드 특문이 깨져서 개고생했는데...
              https://github.com/spectreconsole/spectre.console/issues/113
              관련 이슈로 해결 ㅠㅠ
              이거 넣으면 유니코드 특수문자 콘솔에서 깨지지 않고 아주 잘 나온다.
              (Windows Terminal 에서 구동 기준, cmd 창으로만 실행은 확인 X)
            */
            Console.OutputEncoding = Encoding.UTF8;

            // 전역 변수 가져오기
            var paths = new Global.Global.Paths();

            // 중요 파일들을 저장할 폴더 생성
            // Directory.CreateDirectory = 폴더가 존재하지 않으면 생성, 이미 존재하는 경우 무시
            Directory.CreateDirectory(paths.root_dir_path);

            // Semgrep 저장 폴더 생성
            Directory.CreateDirectory(paths.semgrep_dir_path);

            // API Key 파일 생성을 위해 빈 JSON 객체 생성
            APIKeyJsonRootObject empty_api_json = new APIKeyJsonRootObject();

            // JSON 직렬화
            string api_json_string = JsonConvert.SerializeObject(empty_api_json, Formatting.Indented);

            // API Key JSON 파일 저장
            if (!File.Exists(paths.api_key_file_path))
                File.WriteAllText(paths.api_key_file_path, api_json_string, Encoding.UTF8);

            // Semgrep 번역용 json 파일 생성, 위와 동일한 방향으로 생성
            TranslateJsonRootObject empty_semgrep_json = new TranslateJsonRootObject();
            string semgrep_json_string = JsonConvert.SerializeObject(empty_semgrep_json, Formatting.Indented);
            if (!File.Exists(paths.semgrep_translate_file_path))
                File.WriteAllText(paths.semgrep_translate_file_path, semgrep_json_string, Encoding.UTF8);

            // Semgrep 규칙 목록 JSON 파일 생성, 위와 동일한 방향으로 생성
            /*
              우선 rules set을 아주 많이 넣어서 스캔 적중률을 강화하는 방향으로 진행.
              속도가 많이 느려지긴 하나 보안 취약점을 최대한 찾아내기 위함. (추후 최적화 필수로 필요.)
              Ryzen 5600x 를 기준으로 1777개의 파일을 스캔하는데 약 10~20초 정도
              Rule set 찾는 곳은 여기 : https://semgrep.dev/explore
            */
            RulesJsonRootObject empty_rules_json = new RulesJsonRootObject
            {
                rules =
                [
                    "security-audit",          // 보안 감사용 규칙셋
                        "xss",                 // XSS 취약점 규칙셋
                        "sql-injection",       // SQL Injection 규칙셋
                        "secrets",             // git에 하드코딩된 비밀번호·키워드 검사
                        "cwe-top-25",          // CWE Top 25 애플리케이션 보안 위험
                        "r2c-security-audit",  // 잠재적 보안 문제 스캔
                        "owasp-top-ten",       // OWASP Top 10 웹 애플리케이션 위험
                        "gitleaks"             // 커밋된 API 키 + 비밀번호 탐지
                ]
            };

            // 파일이 존재하지 않으면 기본값으로 생성
            string rules_json_string = JsonConvert.SerializeObject(empty_rules_json, Formatting.Indented);
            if (!File.Exists(paths.semgrep_rules_file_path))
                File.WriteAllText(paths.semgrep_rules_file_path, rules_json_string, Encoding.UTF8);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(1);
        }
    }
}