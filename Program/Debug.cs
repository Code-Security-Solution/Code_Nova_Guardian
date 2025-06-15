namespace Code_Nova_Guardian;
public static partial class Program
{
    private static string[] debug_args()
    {
        // <T> 프로메세지 O / 번역 X
        //string source_path = "../../Example/Vulnerable-Code-Snippets-Small";
        //string result_path = "../../Scan Result/origin-scan-promode.json";
        //return new[] { "scan", "semgrep", source_path, result_path };

        // <T> 프로메세지 O / 번역 O (--translate)
        //string source_path = "../../Example/Vulnerable-Code-Snippets-Small";
        //string result_path = "../../Scan Result/origin-scan-promode.json";
        //string translate_result_path = "../../Scan Result/origin-scan-promode-translate.json";
        //return new[] { "scan", "semgrep", source_path, result_path, "--translate", translate_result_path };

        // <T> 프로메세지 X (--no-pro-message) / 번역 X
        //string source_path = "../../Example/Vulnerable-Code-Snippets-Small";
        //string result_path = "../../Scan Result/origin-scan-no-promode.json";
        //return new[] { "scan", "semgrep", source_path, result_path, "--no-pro-message" };

        // <T> 프로메세지 X (--no-pro-message) / 번역 O (--translate)
        string source_path = "../../Example/Vulnerable-Code-Snippets-Small";
        string result_path = "../../Scan Result/origin-scan-no-promode.json";
        string translate_result_path = "../../Scan Result/origin-scan-no-promode-translate.json";
        return ["scan", "semgrep", source_path, result_path, "--no-pro-message", "--translate", translate_result_path];

        // <T> Semgrep Token 획득 명령어
        //return new[] { "get-semgrep-token" };
        //return [];
    }

}