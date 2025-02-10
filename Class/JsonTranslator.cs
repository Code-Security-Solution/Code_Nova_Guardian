﻿/*
  File - scoped Namespace 기능 사용
  일반적으로 C#에서는 namespace 1개를 사용하며 namespace 안에 코드를 넣기 위해
  중괄호를 넣는데, 이렇게 되면 C#의 소스코드는 무조건 들여쓰기 1번으로 시작해 번거롭다.
  File-scoped Namespace 기능을 사용하면 중괄호로 묶지 않고 맨 위에 namespace를 세미콜론 1개로 선언하고,
  중괄호로 묶을 필요가 없어져서 더 코드 읽기가 간편해진다.
*/

using Code_Nova_Guardian.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Code_Nova_Guardian.Class;

// 보안 취약점 검사 도구들이 뱉는 json 결과를 번역하는데 도움을 주는 Translator 객체의 설계도(Class)
public class JsonTranslator
{
    // 번역 타입 지정
    public enum TranslateType
    {
        Dictionary,
        Pattern
    }

    // semgrep json 파일 경로
    private string semgrep_json_file_path;

    // json 파일 읽어와서 다룰 객체
    private TranslateJsonRootObject? root;

    /*
      O(1) 탐색을 위한 딕셔너리, 패턴 번역에선 O(n)으로 반복해 검증하고,
      딕셔너리 번역의 경우 이 변수에 불러와서 O(1) 탐색으로 번역.
      다만 파일에서 초기에 딕셔너리에 불러오기 위해 초기에 O(n) 의 비용이 발생.
    */
    private Dictionary<string, string> dictionary;


    // translate.json 파일의 경로를 인자로 받아서 이를 파싱하고 멤버 변수에 등록한다.
    public JsonTranslator(string semgrep_json_file_path)
    {

        // 멤버 변수 값에 값을 할당한다.
        this.semgrep_json_file_path = semgrep_json_file_path;

        // 들어온 translate.json 파일의 내용을 우선 문자열로 읽어들인다.
        string json_file_data = File.ReadAllText(semgrep_json_file_path);

        // 문자열로 읽어들인 translate.json 파일을 객체로 변환해 파싱한다.
        root = JsonSerializer.Deserialize<TranslateJsonRootObject>(json_file_data);

        if (root == null)
            throw new Exception("[bold red]Error : 번역 json 파일을 파싱하는데 실패했습니다.[/]\n");


        /*
          O(1) 탐색을 위한 Dictionary 변환, 초기 변환에서 O(n) 비용 발생
          여기서 미리 변환해주지 않으면 탐색시 매번 O(n) 으로 탐색해야 해서 성능이 크게 떨어진다.
          참고 : 패턴 번역의 경우 regex match를 매번 실행해서 체크해야 하기에 어쩔 수 없이 매번 O(n)으로 체크해야 한다.
        */
        dictionary = new Dictionary<string, string>();

        // 딕셔너리 내용이 비었으면 이탈
        if (root.dictionary.Length == 0)
            return;

        // 아이템 삽입
        foreach (var item in root.dictionary)
        {
            if (!dictionary.ContainsKey(item.origin))
                dictionary[item.origin] = item.message;
        }
    }

    // 텍스트를 넣어서 번역 타입에 맞게 번역
    // 출력 문자열은 번역 성공시 origin_text에 대응되는 번역 메세지,
    // 실패시엔 빈 문자열 반환
    /// <summary>
    /// 입력된 `origin_text`를 번역 타입(`TranslateType`)에 맞게 번역한다.
    /// 번역 성공 시 대응되는 번역 메시지를 반환하고, 실패 시 빈 문자열("")을 반환한다.
    /// - `TranslateType.Pattern`: 정규식 패턴 기반 번역 (O(n) 탐색)
    /// - `TranslateType.Dictionary`: 사전 기반 번역 (O(1) 탐색)
    /// </summary>
    /// <param name="origin_text">번역할 원본 텍스트</param>
    /// <param name="translate_type">번역 방식 (Dictionary / Pattern)</param>
    /// <returns>번역된 문자열 (없을 경우 빈 문자열 반환)</returns>
    public string translate_text(string origin_text, TranslateType translate_type)
    {
        // 패턴 기반 번역을 수행 (정규식 기반 변환, O(n))
        if (translate_type == TranslateType.Pattern && root?.patterns != null)
        {
            foreach (var pattern in root.patterns)
            {
                Regex regex = new Regex(pattern.regex);

                if (regex.IsMatch(origin_text))
                {
                    return regex.Replace(origin_text, pattern.message);
                }
            }
        }

        // 사전 기반 번역을 수행 (해시맵 기반 변환, O(1))
        if (translate_type == TranslateType.Dictionary)
        {
            return dictionary.TryGetValue(origin_text, out string translated_text) ? translated_text : "";
            // dictionary_map에서 origin_text를 찾으면 번역 결과 반환, 없으면 빈 문자열 반환
        }

        // 번역 실패 시 빈 문자열 반환
        return "";
    }


    public static Dictionary<string, string> semgrep_dic =
        new()
        {
            {
                "Avoid 'gets()'. This function does not consider buffer boundaries and can lead to buffer overflows. Use 'fgets()' or 'gets_s()' instead.",
                "'gets()' 함수 사용을 피하세요. 이 함수는 버퍼 경계를 고려하지 않아 버퍼 오버플로우를 유발할 수 있으므로, 'fgets()'나 'gets_s()'를 대신 사용하세요."
            },
            {
                "Finding triggers whenever there is a strcat or strncat used. This is an issue because strcat or strncat can lead to buffer overflow vulns. Fix this by using strcat_s instead.",
                "strcat 또는 strncat 사용 시 경고가 발생합니다. 이 함수들은 버퍼 오버플로우 취약점을 초래할 수 있으므로, 대신 strcat_s를 사용하여 문제를 해결하십시오."
            },
            {
                "Finding triggers whenever there is a strcpy or strncpy used. This is an issue because strcpy does not affirm the size of the destination array and strncpy will not automatically NULL-terminate strings. This can lead to buffer overflows, which can cause program crashes and potentially let an attacker inject code in the program. Fix this by using strcpy_s instead (although note that strcpy_s is an optional part of the C11 standard, and so may not be available).",
                "strcpy 또는 strncpy 사용 시 경고가 발생합니다. strcpy는 대상 배열의 크기를 확인하지 않으며, strncpy는 문자열의 끝에 자동으로 NULL 문자를 추가하지 않습니다. 이로 인해 버퍼 오버플로우가 발생하여 프로그램 충돌이나 공격자가 코드를 삽입할 위험이 있습니다. 따라서 가능하면 strcpy_s를 사용하십시오 (단, strcpy_s는 C11 표준의 선택적 기능일 수 있습니다)."
            },
            {
                "The application might dynamically evaluate untrusted input, which can lead to a code injection vulnerability. An attacker can execute arbitrary code, potentially gaining complete control of the system. To prevent this vulnerability, avoid executing code containing user input. If this is unavoidable, validate and sanitize the input, and use safe alternatives for evaluating user input.",
                "응용프로그램이 신뢰할 수 없는 입력을 동적으로 평가할 경우, 코드 삽입 취약점(code injection vulnerability)이 발생할 수 있습니다. 공격자는 임의의 코드를 실행하여 시스템 전체를 장악할 수 있으므로, 사용자 입력을 포함한 코드를 실행하지 마시고 불가피한 경우에는 입력을 반드시 검증 및 정제한 후 안전한 평가 방법을 사용하십시오."
            },
            {
                "Untrusted input might be injected into a command executed by the application, which can lead to a command injection vulnerability. An attacker can execute arbitrary commands, potentially gaining complete control of the system. To prevent this vulnerability, avoid executing OS commands with user input. If this is unavoidable, validate and sanitize the input, and use safe methods for executing the commands.",
                "응용프로그램에서 실행되는 명령어에 신뢰할 수 없는 입력이 삽입될 경우, 명령어 삽입(command injection)이 발생할 수 있습니다. 공격자는 임의의 명령어를 실행하여 시스템 전체를 장악할 수 있으므로, 사용자 입력을 포함한 OS 명령어 실행을 피하고 불가피한 경우에는 입력을 반드시 검증 및 정제한 후 안전한 실행 방법을 사용하십시오."
            },
            {
                "User input is passed to a function that executes a shell command. This can lead to remote code execution.",
                "사용자 입력이 셸 명령어를 실행하는 함수에 전달되면 원격 코드 실행 취약점(remote code execution)이 발생할 수 있습니다."
            },
            {
                "Untrusted input might be injected into a command executed by the application, which can lead to a command injection vulnerability. An attacker can execute arbitrary commands, potentially gaining complete control of the system. To prevent this vulnerability, avoid executing OS commands with user input. If this is unavoidable, validate and sanitize the user input, and use safe methods for executing the commands. In PHP, it is possible to use `escapeshellcmd(...)` and `escapeshellarg(...)` to correctly sanitize input that is used respectively as system commands or command arguments.",
                "애플리케이션에서 실행되는 명령어에 신뢰할 수 없는 입력이 삽입되면 명령어 삽입 취약점(command injection vulnerability)이 발생할 수 있습니다. 공격자가 임의의 명령어를 실행하여 시스템 전체를 장악할 수 있으므로, 사용자 입력을 포함한 OS 명령어 실행을 피하십시오. 불가피한 경우 입력을 반드시 검증 및 정제하고 안전한 실행 방법을 사용하십시오. PHP에서는 시스템 명령어나 명령어 인수에 사용되는 입력을 올바르게 정제하기 위해 `escapeshellcmd(...)`와 `escapeshellarg(...)`를 각각 사용할 수 있습니다."
            },
            {
                "Executing non-constant commands. This can lead to command injection. You should use `escapeshellarg()` when using command.",
                "상수가 아닌 명령어를 실행하면 명령어 삽입(command injection)이 발생할 수 있으므로, 명령어 사용 시 `escapeshellarg()`를 사용하십시오."
            },
            {
                "Untrusted input might be injected into a command executed by the application, which can lead to a command injection vulnerability. An attacker can execute arbitrary commands, potentially gaining complete control of the system. To prevent this vulnerability, avoid executing OS commands with user input. If this is unavoidable, validate and sanitize the user input, and use safe methods for executing the commands. In PHP, it is possible to use `escapeshellcmd(...)` and `escapeshellarg(...)` to correctly sanitize input when used respectively as system commands or command arguments.",
                "애플리케이션에서 실행되는 명령어에 신뢰할 수 없는 입력이 포함되면, 명령어 삽입 취약점(command injection vulnerability)이 발생할 수 있습니다. 공격자가 임의의 명령어를 실행하여 시스템 전체를 장악할 수 있으므로, 사용자 입력을 포함한 OS 명령어 실행을 피하십시오. 불가피한 경우 입력을 반드시 검증 및 정제하고 안전한 실행 방법을 사용하십시오. PHP에서는 시스템 명령어나 명령어 인수에 사용되는 입력을 올바르게 정제하기 위해 `escapeshellcmd(...)`와 `escapeshellarg(...)`를 각각 사용할 수 있습니다."
            },
            {
                "Untrusted input might be injected into a command executed by the application, which can lead to a command injection vulnerability. An attacker can execute arbitrary commands, potentially gaining complete control of the system. To prevent this vulnerability, avoid executing OS commands with user input. If this is unavoidable, validate and sanitize the user input, and use safe methods for executing the commands. For more information, see [Command injection prevention for JavaScript ](https://semgrep.dev/docs/cheat-sheets/javascript-command-injection/).",
                "애플리케이션에서 실행되는 명령어에 신뢰할 수 없는 입력이 포함되면, 명령어 삽입 취약점(command injection vulnerability)이 발생할 수 있습니다. 공격자가 이를 악용하면 임의의 명령어를 실행할 수 있으며, 최악의 경우 시스템 전체를 제어할 수도 있습니다.\n이러한 보안 문제를 방지하려면, 사용자 입력을 포함한 OS 명령어 실행을 피해야 합니다. 불가피한 경우 입력을 철저히 검증하고 정제해야 하며, 안전한 실행 방법을 채택해야 합니다.\n보다 자세한 정보는 [JavaScript 명령어 삽입 취약점 방지 가이드](https://semgrep.dev/docs/cheat-sheets/javascript-command-injection/)를 참고하세요."
            },
            {
                "Detected calls to child_process from a function argument `req`. This could lead to a command injection if the input is user controllable. Try to avoid calls to child_process, and if it is needed ensure user input is correctly sanitized or sandboxed.",
                "함수 인수 `req`로부터 child_process 호출이 감지되었습니다. 입력이 사용자 제어 하에 있다면 명령어 삽입(command injection)이 발생할 수 있으므로, child_process 호출을 피하고 불가피한 경우에는 사용자 입력을 올바르게 정제하거나 샌드박스 처리하십시오."
            },
            {
                "Detected calls to child_process from a function argument `cmd`. This could lead to a command injection if the input is user controllable. Try to avoid calls to child_process, and if it is needed ensure user input is correctly sanitized or sandboxed.",
                "함수 인수 `cmd`로부터 child_process 호출이 감지되었습니다. 입력이 사용자 제어 하에 있다면 명령어 삽입(command injection)이 발생할 수 있으므로, child_process 호출을 피하고 불가피한 경우에는 사용자 입력을 올바르게 정제하거나 샌드박스 처리하십시오."
            },
            {
                "Username and password in URI detected",
                "URI에 사용자 이름과 비밀번호가 감지되었습니다."
            },
            {
                "Request data detected in os.system. This could be vulnerable to a command injection and should be avoided. If this must be done, use the 'subprocess' module instead and pass the arguments as a list. See https://owasp.org/www-community/attacks/Command_Injection for more information.",
                "os.system에서 요청 데이터(request data)가 감지되었습니다. 이는 명령어 삽입(command injection)에 노출될 수 있으므로 피해야 합니다. 불가피한 경우 'subprocess' 모듈을 사용하고 인수를 리스트로 전달하십시오. 자세한 내용은 https://owasp.org/www-community/attacks/Command_Injection 을 참조하십시오."
            },
            {
                "User data detected in os.system. This could be vulnerable to a command injection and should be avoided. If this must be done, use the 'subprocess' module instead and pass the arguments as a list.",
                "os.system에서 사용자 데이터가 감지되었습니다. 이는 명령어 삽입(command injection)점에 노출될 수 있으므로 피해야 합니다. 불가피한 경우 'subprocess' 모듈을 사용하고 인수를 리스트로 전달하십시오."
            },
            {
                "Detected Flask app with debug=True. Do not deploy to production with this flag enabled as it will leak sensitive information. Instead, consider using Flask configuration variables or setting 'debug' using system environment variables.",
                "debug=True로 설정된 Flask 앱이 감지되었습니다. 이 플래그가 활성화된 상태로 프로덕션에 배포하면 민감한 정보가 노출될 수 있으므로, 대신 Flask 구성 변수를 사용하거나 시스템 환경 변수를 통해 'debug' 모드를 설정하십시오."
            },
            {
                "Detected user input going into a php include or require command, which can lead to path traversal and sensitive data being exposed. These commands can also lead to code execution. Instead, allowlist files that the user can access or rigorously validate user input.",
                "PHP의 include 또는 require 명령어에 사용자 입력이 사용되는 것이 감지되었습니다. 이는 경로 탐색 및 민감 데이터 노출, 나아가 코드 실행 취약점으로 이어질 수 있으므로, 사용자가 접근할 수 있는 파일을 허용 목록으로 제한하거나 사용자 입력을 엄격하게 검증하십시오."
            },
            {
                "The application builds a file path from potentially untrusted data, which can lead to a path traversal vulnerability. An attacker can manipulate the file path which the application uses to access files. If the application does not validate user input and sanitize file paths, sensitive files such as configuration or user data can be accessed, potentially creating or overwriting files. In PHP, this can lead to both local file inclusion (LFI) or remote file inclusion (RFI) if user input reaches this statement. To prevent this vulnerability, validate and sanitize any input that is used to create references to file paths. Also, enforce strict file access controls. For example, choose privileges allowing public-facing applications to access only the required files.",
                "애플리케이션이 잠재적으로 신뢰할 수 없는 데이터로 파일 경로를 구성하면 경로 탐색 취약점이 발생할 수 있습니다. 공격자는 애플리케이션이 파일에 접근하기 위해 사용하는 경로를 조작할 수 있습니다. 만약 사용자 입력을 검증하지 않고 파일 경로를 정제하지 않는다면, 구성 파일이나 사용자 데이터와 같은 민감한 파일에 접근하여 파일을 생성하거나 덮어쓸 수 있습니다. PHP에서는 이 문장에 사용자 입력이 전달될 경우 로컬 파일 포함(LFI) 또는 원격 파일 포함(RFI) 취약점이 발생할 수 있습니다. 이러한 취약점을 방지하기 위해 파일 경로 참조에 사용되는 모든 입력을 반드시 검증 및 정제하고, 엄격한 파일 접근 제어를 적용하십시오. 예를 들어, 공개 웹 애플리케이션이 필요한 파일에만 접근할 수 있도록 권한을 제한하는 것이 좋습니다."
            },
            {
                "Don't call `system`. It's a high-level wrapper that allows for stacking multiple commands. Always prefer a more restrictive API such as calling `execve` from the `exec` family.",
                "`system` 호출을 피하십시오. 이 함수는 여러 명령어를 연속 실행할 수 있는 고수준 래퍼이므로, 대신 `exec` 계열의 `execve`와 같은 제한적인 API를 사용하는 것이 좋습니다."
            },
            {
                "Avoid using 'scanf()'. This function, when used improperly, does not consider buffer boundaries and can lead to buffer overflows. Use 'fgets()' instead for reading input.",
                "'scanf()' 사용을 피하십시오. 이 함수는 부적절하게 사용될 경우 버퍼 경계를 고려하지 않아 버퍼 오버플로우를 초래할 수 있으므로, 입력을 읽을 때는 대신 'fgets()'를 사용하십시오."
            },
            {
                "LDAP queries are constructed dynamically on user-controlled input. This vulnerability in code could lead to an arbitrary LDAP query execution.",
                "LDAP 쿼리가 사용자 제어 입력에 따라 동적으로 구성되고 있습니다. 이 취약점은 임의의 LDAP 쿼리 실행으로 이어질 수 있습니다."
            },
            {
                "Detected a `MongoClientMPORT` statement that comes from a `req` argument. This could lead to NoSQL injection if the variable is user-controlled and is not properly sanitized. Be sure to properly sanitize the data if you absolutely must pass request data into a mongo query.",
                "`req` 인자로부터 유래된 `MongoClientMPORT` 문이 감지되었습니다. 해당 변수가 사용자 제어 하에 있고 적절히 정제되지 않았다면 NoSQL 삽입(NoSQL injection)으로 이어질 수 있으므로, 요청 데이터(request data)를 Mongo 쿼리에 전달해야 한다면 반드시 데이터를 올바르게 정제하십시오."
            },
            {
                "When a redirect uses user input, a malicious user can spoof a website under a trusted URL or access restricted parts of a site. When using user-supplied values, sanitize the value before using it for the redirect.",
                "리다이렉트에 사용자 입력이 사용될 경우, 악의적인 사용자가 신뢰할 수 있는 URL 아래에서 웹사이트를 위조하거나 제한된 영역에 접근할 수 있습니다. 사용자 제공 값을 사용할 때는 리다이렉트 전에 반드시 해당 값을 정제하십시오."
            },
            {
                "It looks like 'url' is read from user input and it is used to as a redirect. Ensure 'url' is not externally controlled, otherwise this is an open redirect.",
                "'url'이 사용자 입력으로부터 읽혀 리다이렉트에 사용되는 것으로 보입니다. 'url'이 외부에서 제어되지 않도록 하십시오. 그렇지 않으면 오픈 리다이렉트가 발생할 수 있습니다."
            },
            {
                "It looks like 'followPath' is read from user input and it is used to as a redirect. Ensure 'followPath' is not externally controlled, otherwise this is an open redirect.",
                "'followPath'가 사용자 입력으로부터 읽혀 리다이렉트에 사용되는 것으로 보입니다. 'followPath'가 외부에서 제어되지 않도록 하십시오. 그렇지 않으면 오픈 리다이렉트가 발생할 수 있습니다."
            },
            {
                "The application builds a URL using user-controlled input which can lead to an open redirect vulnerability. An attacker can manipulate the URL and redirect users to an arbitrary domain. Open redirect vulnerabilities can lead to issues such as Cross-site scripting (XSS) or redirecting to a malicious domain for activities such as phishing to capture users' credentials. To prevent this vulnerability perform strict input validation of the domain against an allowlist of approved domains. Notify a user in your application that they are leaving the website. Display a domain where they are redirected to the user. A user can then either accept or deny the redirect to an untrusted site.",
                "애플리케이션이 사용자 제어 입력을 사용하여 URL을 구성하면 오픈 리다이렉트 취약점이 발생할 수 있습니다. 공격자는 URL을 조작하여 사용자를 임의의 도메인으로 리다이렉트할 수 있습니다. 오픈 리다이렉트 취약점은 교차 사이트 스크립팅(XSS)이나 피싱과 같은 악의적 활동으로 사용자의 자격 증명을 탈취할 위험이 있으므로, 허용된 도메인 목록에 대해 엄격한 입력 검증을 수행하고 사용자가 웹사이트를 떠날 것임을 알리며 리다이렉트될 도메인을 표시하여 사용자가 수락 또는 거부할 수 있도록 하십시오."
            },
            {
                "A gitleaks hashicorp-tf-password was detected which attempts to identify hard-coded credentials. It is not recommended to store credentials in source-code, as this risks secrets being leaked and used by either an internal or external malicious adversary. It is recommended to use environment variables to securely provide credentials or retrieve credentials from a secure vault or HSM (Hardware Security Module).",
                "하드코딩된 자격 증명을 식별하기 위한 gitleaks hashicorp-tf-password가 감지되었습니다. 자격 증명을 소스 코드에 저장하는 것은 권장되지 않으며, 내부 또는 외부의 악의적 행위자에 의해 비밀 정보가 유출되어 악용될 위험이 있으므로, 자격 증명을 안전하게 제공하기 위해 환경 변수를 사용하거나 보안 볼트 또는 HSM(Hardware Security Module)에서 가져오십시오."
            },
            {
                "Untrusted input might be used to build an HTTP request, which can lead to a Server-side request forgery (SSRF) vulnerability. SSRF allows an attacker to send crafted requests from the server side to other internal or external systems. SSRF can lead to unauthorized access to sensitive data and, in some cases, allow the attacker to control applications or systems that trust the vulnerable service. To prevent this vulnerability, avoid allowing user input to craft the base request. Instead, treat it as part of the path or query parameter and encode it appropriately. When user input is necessary to prepare the HTTP request, perform strict input validation. Additionally, whenever possible, use allowlists to only interact with expected, trusted domains.",
                "신뢰할 수 없는 입력을 사용하여 HTTP 요청을 구성하면 서버 측 요청 위조(SSRF) 취약점이 발생할 수 있습니다. SSRF는 공격자가 서버 측에서 조작된 요청을 내부 또는 외부 시스템으로 전송하여 민감한 데이터에 무단 접근하거나, 신뢰하는 서비스를 제어할 위험을 초래할 수 있습니다. 이러한 취약점을 방지하기 위해, 사용자 입력을 기반 요청 생성에 사용하지 마시고, 입력을 경로나 쿼리 파라미터의 일부로 취급하며 적절히 인코딩하십시오. HTTP 요청 준비에 사용자 입력이 필요한 경우에는 반드시 엄격한 입력 검증을 수행하고, 가능한 경우 허용 목록을 사용하여 예상되고 신뢰할 수 있는 도메인과만 상호작용하십시오."
            },
            {
                "File name based on user input risks server-side request forgery.",
                "사용자 입력을 기반으로 파일 이름을 구성하면 서버 측 요청 위조(SSRF) 위험이 있습니다."
            },
            {
                "The target origin of the window.postMessage() API is set to \"*\". This could allow for information disclosure due to the possibility of any origin allowed to receive the message.",
                "window.postMessage() API의 대상 Origin이 \"*\"로 설정되어 있습니다. 이로 인해 모든 Origin이 메시지를 수신할 수 있어 정보 노출 위험이 발생할 수 있습니다."
            },
            {
                "Detected a formatted string in a SQL statement. This could lead to SQL injection if variables in the SQL statement are not properly sanitized. Use a prepared statements instead. You can obtain a PreparedStatement using 'SqlCommand' and 'SqlParameter'.",
                "SQL 문에서 포맷된 문자열(formatted string)이 감지되었습니다. SQL 문 내의 변수가 올바르게 정제되지 않으면 SQL 삽입(SQL injection)으로 이어질 수 있으므로, 대신 준비된 문(Prepared Statement)을 사용하십시오. 'SqlCommand'와 'SqlParameter'를 사용하여 PreparedStatement를 생성할 수 있습니다."
            },
            {
                "Detected user input used to manually construct a SQL string. This is usually bad practice because manual construction could accidentally result in a SQL injection. An attacker could use a SQL injection to steal or modify contents of the database. Instead, use a parameterized query which is available by default in most database engines. Alternatively, consider using an object-relational mapper (ORM) such as ActiveRecord which will protect your queries.",
                "사용자 입력을 사용하여 수동으로 SQL 문자열을 구성하는 것이 감지되었습니다. 이는 일반적으로 좋지 않은 관행이며, 우연히 SQL 삽입 취약점이 발생할 수 있습니다. 공격자는 SQL 삽입을 통해 데이터베이스의 내용을 탈취하거나 수정할 수 있으므로, 대신 대부분의 데이터베이스 엔진에서 기본적으로 제공되는 파라미터화된 쿼리 또는 ActiveRecord와 같은 객체 관계 매퍼(ORM)를 사용하십시오."
            },
            {
                "A secret is hard-coded in the application. Secrets stored in source code, such as credentials, identifiers, and other types of sensitive data, can be leaked and used by internal or external malicious actors. Use environment variables to securely provide credentials and other secrets or retrieve them from a secure vault or Hardware Security Module (HSM).",
                "애플리케이션에 하드코딩된 비밀 정보가 있습니다. 자격 증명, 식별자 및 기타 민감한 데이터와 같은 비밀 정보를 소스 코드에 저장하면 내부 또는 외부의 악의적 행위자에 의해 유출되어 악용될 위험이 있으므로, 자격 증명 및 기타 비밀 정보를 안전하게 제공하기 위해 환경 변수를 사용하거나 보안 볼트 또는 HSM(Hardware Security Module)에서 가져오십시오."
            },
            {
                "Untrusted input might be used to build a database query, which can lead to a SQL injection vulnerability. An attacker can execute malicious SQL statements and gain unauthorized access to sensitive data, modify, delete data, or execute arbitrary system commands. To prevent this vulnerability, use prepared statements that do not concatenate user-controllable strings and use parameterized queries where SQL commands and user data are strictly separated. Also, consider using an object-relational (ORM) framework to operate with safer abstractions.",
                "신뢰할 수 없는 입력을 사용하여 데이터베이스 쿼리를 구성하면 SQL 삽입 취약점이 발생할 수 있습니다. 공격자는 악의적인 SQL 문을 실행하여 민감한 데이터에 무단 접근하거나, 데이터를 수정 및 삭제하거나 임의의 시스템 명령을 실행할 수 있습니다. 이러한 취약점을 방지하기 위해, 사용자 제어 문자열을 단순 연결하지 않는 준비된 문과 SQL 명령과 사용자 데이터를 엄격히 분리하는 파라미터화된 쿼리를 사용하고, 보다 안전한 추상화를 제공하는 객체 관계 매퍼(ORM) 프레임워크 사용을 고려하십시오."
            },
            {
                "Detected a sequelize statement that is tainted by user-input. This could lead to SQL injection if the variable is user-controlled and is not properly sanitized. In order to prevent SQL injection, it is recommended to use parameterized queries or prepared statements.",
                "사용자 입력에 의해 오염된 sequelize 문이 감지되었습니다. 해당 변수가 사용자 제어 하에 있고 올바르게 정제되지 않은 경우 SQL 삽입(SQL injection)으로 이어질 수 있으므로, SQL 삽입을 방지하기 위해 파라미터화된 쿼리나 준비된 문을 사용하는 것이 좋습니다."
            },
            {
                "Detected user input used to manually construct a SQL string. This is usually bad practice because manual construction could accidentally result in a SQL injection. An attacker could use a SQL injection to steal or modify contents of the database. Instead, use a parameterized query which is available by default in most database engines. Alternatively, consider using an object-relational mapper (ORM) such as Sequelize which will protect your queries.",
                "사용자 입력을 사용하여 수동으로 SQL 문자열을 구성하는 것이 감지되었습니다. 이는 일반적으로 좋지 않은 관행이며, 우연히 SQL 삽입 취약점이 발생할 수 있습니다. 공격자는 SQL 삽입을 통해 데이터베이스의 내용을 탈취하거나 수정할 수 있으므로, 대신 대부분의 데이터베이스 엔진에서 기본 제공되는 파라미터화된 쿼리 또는 Sequelize와 같은 객체 관계 매퍼(ORM)를 사용하는 것을 고려하십시오."
            },
            {
                "User data flows into this manually-constructed SQL string. User data can be safely inserted into SQL strings using prepared statements or an object-relational mapper (ORM). Manually-constructed SQL strings is a possible indicator of SQL injection, which could let an attacker steal or manipulate data from the database. Instead, use prepared statements (`$mysqli->prepare(\"INSERT INTO test(id, label) VALUES (?, ?)\");`) or a safe library.",
                "사용자 데이터가 수동으로 구성된 SQL 문자열에 유입되고 있습니다. 준비된 문이나 객체 관계 매퍼(ORM)를 사용하면 사용자 데이터를 안전하게 SQL 문자열에 삽입할 수 있습니다. 수동으로 구성된 SQL 문자열은 SQL 삽입(SQL injection)의 징후일 수 있으므로, 대신 `$mysqli->prepare(\"INSERT INTO test(id, label) VALUES (?, ?)\");`와 같은 준비된 문 또는 안전한 라이브러리를 사용하십시오."
            },
            {
                "Found a string literal assignment to a Rails session secret `secret_key_base`. Do not commit secret values to source control! Any user in possession of this value may falsify arbitrary session data in your application. Read this value from an environment variable, KMS, or file on disk outside of source control.",
                "Rails 세션 비밀 값 `secret_key_base`에 문자열 리터럴 할당이 발견되었습니다. 비밀 값을 소스 코드 관리에 커밋하지 마십시오! 이 값을 가진 사용자는 애플리케이션에서 임의의 세션 데이터를 위조할 수 있으므로, 이 값을 환경 변수, KMS 또는 소스 코드 관리 외부의 파일에서 읽어오십시오."
            },
            {
                "Detected user input flowing into a manually constructed HTML string. You may be accidentally bypassing secure methods of rendering HTML by manually constructing HTML and this could create a cross-site scripting vulnerability, which could let attackers steal sensitive user data. To be sure this is safe, check that the HTML is rendered safely. Otherwise, use templates (`django.shortcuts.render`) which will safely render HTML instead.",
                "사용자 입력이 수동으로 구성된 HTML 문자열에 유입되고 있습니다. HTML을 수동으로 구성할 경우 안전한 렌더링 방법을 우회하여 교차 사이트 스크립팅(XSS) 취약점이 발생할 수 있으므로, HTML이 안전하게 렌더링되는지 확인하거나 템플릿(예: `django.shortcuts.render`)을 사용하여 안전하게 렌더링하십시오."
            },
            {
                "Detected user input flowing into a manually constructed HTML string. You may be accidentally bypassing secure methods of rendering HTML by manually constructing HTML and this could create a cross-site scripting vulnerability, which could let attackers steal sensitive user data. To be sure this is safe, check that the HTML is rendered safely. Otherwise, use templates (`flask.render_template`) which will safely render HTML instead.",
                "사용자 입력이 수동으로 구성된 HTML 문자열에 유입되고 있습니다. HTML을 수동으로 구성하면 안전한 렌더링 방법을 우회하여 교차 사이트 스크립팅(XSS) 취약점이 발생할 수 있으므로, HTML이 안전하게 렌더링되는지 확인하거나 템플릿(예: `flask.render_template`)을 사용하여 안전하게 렌더링하십시오."
            },
            {
                "Found a template created with string formatting. This is susceptible to server-side template injection and cross-site scripting attacks.",
                "문자열 포맷팅을 사용하여 생성된 템플릿이 발견되었습니다. 이는 서버 측 템플릿 인젝션 및 교차 사이트 스크립팅(XSS) 공격에 취약할 수 있습니다."
            },
            {
                "The application builds a file path from potentially untrusted data, which can lead to a path traversal vulnerability. An attacker can manipulate the path which the application uses to access files. If the application does not validate user input and sanitize file paths, sensitive files such as configuration or user data can be accessed, potentially creating or overwriting files. In Flask apps, consider using the Werkzeug util `werkzeug.utils.secure_filename()` to sanitize paths and filenames.",
                "애플리케이션이 잠재적으로 신뢰할 수 없는 데이터로 파일 경로를 구성하면 경로 탐색 취약점이 발생할 수 있습니다. 공격자는 애플리케이션이 파일에 접근하기 위해 사용하는 경로를 조작할 수 있습니다. 만약 사용자 입력을 검증하지 않고 파일 경로를 정제하지 않는다면, 구성 파일이나 사용자 데이터와 같은 민감한 파일에 접근하여 파일을 생성하거나 덮어쓸 위험이 있습니다. Flask 앱에서는 파일 경로와 파일명을 정제하기 위해 Werkzeug의 `werkzeug.utils.secure_filename()` 유틸리티 사용을 고려하십시오."
            },
            {
                "Hardcoded variable `DEBUG` detected. Set this by using FLASK_DEBUG environment variable",
                "하드코딩된 변수 `DEBUG`가 감지되었습니다. FLASK_DEBUG 환경 변수를 사용하여 설정하십시오."
            },
            {
                "top-level app.run(...) is ignored by flask. Consider putting app.run(...) behind a guard, like inside a function",
                "최상위의 app.run(...)은 Flask에 의해 무시됩니다. app.run(...)을 함수 내부와 같은 조건문 뒤에 배치하는 것을 고려하십시오."
            },
            {
                "Found object deserialization using ObjectInputStream. Deserializing entire Java objects is dangerous because malicious actors can create Java object streams with unintended consequences. Ensure that the objects being deserialized are not user-controlled. If this must be done, consider using HMACs to sign the data stream to make sure it is not tampered with, or consider only transmitting object fields and populating a new object.",
                "ObjectInputStream을 사용한 객체 역직렬화가 감지되었습니다. 전체 Java 객체를 역직렬화하는 것은 위험하며, 악의적인 행위자가 의도하지 않은 결과를 초래하는 Java 객체 스트림을 생성할 수 있습니다. 역직렬화되는 객체가 사용자 제어 하에 있지 않은지 확인하고, 불가피한 경우 데이터 스트림의 변조를 방지하기 위해 HMAC 서명을 적용하거나 객체의 필드만 전송하여 새 객체를 구성하는 방안을 고려하십시오."
            },
            {
                "Detected the use of an insecure deserialization library in a Flask route. These libraries are prone to code execution vulnerabilities. Ensure user data does not enter this function. To fix this, try to avoid serializing whole objects. Consider instead using a serializer such as JSON.",
                "Flask 라우트에서 안전하지 않은 역직렬화 라이브러리의 사용이 감지되었습니다. 이러한 라이브러리는 코드 실행 취약점에 노출될 수 있으므로, 사용자 데이터가 이 함수에 전달되지 않도록 하십시오. 전체 객체를 직렬화하는 것을 피하고, 대신 JSON과 같은 직렬화기를 사용하는 것을 고려하십시오."
            },
            {
                "Avoid using `pickle`, which is known to lead to code execution vulnerabilities. When unpickling, the serialized data could be manipulated to run arbitrary code. Instead, consider serializing the relevant data as JSON or a similar text-based serialization format.",
                "`pickle` 사용을 피하십시오. `pickle`은 코드 실행 취약점을 유발할 수 있으며, 역직렬화 시 직렬화된 데이터가 변조되어 임의의 코드가 실행될 위험이 있으므로, 대신 JSON이나 유사한 텍스트 기반 직렬화 포맷을 사용하여 관련 데이터를 직렬화하는 것을 고려하십시오."
            },
            {
                "Running flask app with host 0.0.0.0 could expose the server publicly.",
                "Flask 앱을 호스트 0.0.0.0으로 실행하면 서버가 공개적으로 노출될 수 있습니다."
            },
            {
                "The following function call serialize.unserialize accepts user controlled data which can result in Remote Code Execution (RCE) through Object Deserialization. It is recommended to use secure data processing alternatives such as JSON.parse() and Buffer.from().",
                "다음 함수 호출인 serialize.unserialize가 사용자 제어 데이터를 허용하여 객체 역직렬화를 통한 원격 코드 실행(RCE)을 유발할 수 있습니다. JSON.parse()나 Buffer.from()과 같은 안전한 데이터 처리 대안을 사용하는 것이 좋습니다."
            },
            {
                "Detected a cookie options with the `SameSite` flag set to \"None\". This is a potential security risk that arises from the way web browsers manage cookies. In a typical web application, cookies are used to store and transmit session-related data between a client and a server. To enhance security, cookies can be marked with the \"SameSite\" attribute, which restricts their usage based on the origin of the page that set them. This attribute can have three values: \"Strict,\" \"Lax,\" or \"None\". Make sure the `SameSite` attribute of the important cookies (e.g., session cookie) is set to a reasonable value. When `SameSite` is set to \"Strict\", no 3rd party cookie will be sent with outgoing requests, this is the most secure and private setting but harder to deploy with good usability. Setting it to \"Lax\" is the minimum requirement. If this wasn't intentional, it's recommended to set the SameSite flag to the `Strict` or `Lax` value, depending on your needs.",
                "쿠키 옵션에서 `SameSite` 플래그가 \"None\"으로 설정된 것이 감지되었습니다. 이는 웹 브라우저가 쿠키를 관리하는 방식에서 발생하는 잠재적 보안 위험입니다. 일반적으로 쿠키는 클라이언트와 서버 간에 세션 관련 데이터를 저장 및 전송하는 데 사용됩니다. 보안을 강화하기 위해 쿠키에 설정한 페이지의 출처를 기준으로 쿠키 사용을 제한하는 `SameSite` 속성을 지정할 수 있으며, 이 속성은 \"Strict\", \"Lax\", 또는 \"None\"의 값을 가질 수 있습니다. 중요한 쿠키(예: 세션 쿠키)의 `SameSite` 속성이 적절한 값으로 설정되어 있는지 확인하십시오. `SameSite`가 \"Strict\"로 설정되면 외부 요청 시 3자 쿠키가 전송되지 않아 가장 안전하지만 사용성이 떨어질 수 있으므로, 최소한 \"Lax\"로 설정해야 합니다. 의도하지 않은 경우, 필요에 따라 `Strict` 또는 `Lax`로 설정하는 것이 좋습니다."
            },
            {
                "Detected a cookie where the `Secure` flag is either missing or disabled. The `Secure` cookie flag instructs the browser to forbid sending the cookie over an insecure HTTP request. Set the `Secure` flag to `true` so the cookie will only be sent over HTTPS. If this wasn't intentional, it's recommended to set the Secure flag to true by adding `secure: true` to the cookie options, so the cookie will always be sent over HTTPS.",
                "쿠키에서 `Secure` 플래그가 없거나 비활성화된 것이 감지되었습니다. `Secure` 플래그는 브라우저에게 안전하지 않은 HTTP 요청을 통해 쿠키가 전송되지 않도록 지시합니다. 쿠키가 HTTPS를 통해서만 전송되도록 `Secure` 플래그를 `true`로 설정하십시오. 의도하지 않은 경우, 쿠키 옵션에 `secure: true`를 추가하여 항상 HTTPS를 통해 쿠키가 전송되도록 하는 것이 좋습니다."
            },
            {
                "Variable 'buff1' was freed twice. This can lead to undefined behavior.",
                "변수 'buff1'가 두 번 해제되었습니다. 이로 인해 정의되지 않은 동작이 발생할 수 있습니다."
            },
            {
                "Variable 'buff1' was used after being freed. This can lead to undefined behavior.",
                "변수 'buff1'가 해제된 후에 사용되었습니다. 이로 인해 정의되지 않은 동작이 발생할 수 있습니다."
            },
            {
                "XPath queries are constructed dynamically on user-controlled input. This could lead to XPath injection if variables passed into the evaluate or compile commands are not properly sanitized. Xpath injection could lead to unauthorized access to sensitive information in XML documents. Thoroughly sanitize user input or use parameterized XPath queries if you can.",
                "XPath 쿼리가 사용자 제어 입력에 따라 동적으로 구성되고 있습니다. 평가나 컴파일 명령에 전달된 변수가 올바르게 정제되지 않으면 XPath 삽입 공격이 발생할 수 있으며, 이는 XML 문서 내 민감한 정보에 무단 접근을 초래할 수 있습니다. 사용자 입력을 철저히 정제하거나 파라미터화된 XPath 쿼리를 사용하십시오."
            },
            {
                "`Echo`ing user input risks cross-site scripting vulnerability. You should use `htmlentities()` when showing data to users.",
                "사용자 입력을 그대로 출력(`echo`)하면 교차 사이트 스크립팅(XSS) 취약점이 발생할 위험이 있으므로, 데이터를 사용자에게 표시할 때는 `htmlentities()`를 사용하십시오."
            },
            {
                "Found direct access to a PHP variable wihout HTML escaping inside an inline PHP statement setting data from `$_REQUEST[...]`. When untrusted input can be used to tamper with a web page rendering, it can lead to a Cross-site scripting (XSS) vulnerability. XSS vulnerabilities occur when untrusted input executes malicious JavaScript code, leading to issues such as account compromise and sensitive information leakage. To prevent this vulnerability, validate the user input, perform contextual output encoding or sanitize the input. In PHP you can encode or sanitize user input with `htmlspecialchars` or use automatic context-aware escaping with a template engine such as Latte.",
                "인라인 PHP 문에서 `$_REQUEST[...]`를 사용하여 데이터를 설정할 때 HTML 이스케이프 없이 PHP 변수에 직접 접근하는 것이 감지되었습니다. 신뢰할 수 없는 입력이 웹 페이지 렌더링을 변경할 경우 교차 사이트 스크립팅(XSS) 취약점이 발생할 수 있으므로, 사용자 입력을 검증하고 상황에 맞는 출력 인코딩을 수행하거나 입력을 정제하십시오. PHP에서는 `htmlspecialchars`를 사용하거나 Latte와 같은 템플릿 엔진의 자동 컨텍스트 인식 이스케이프 기능을 활용할 수 있습니다."
            },
            {
                "User data flows into the host portion of this manually-constructed HTML. This can introduce a Cross-Site-Scripting (XSS) vulnerability if this comes from user-provided input. Consider using a sanitization library such as DOMPurify to sanitize the HTML within.",
                "사용자 데이터가 수동으로 구성된 HTML의 호스트 부분에 유입되고 있습니다. 이 값이 사용자 제공 입력에서 온 경우 교차 사이트 스크립팅(XSS) 취약점이 발생할 수 있으므로, HTML 내부를 정제하기 위해 DOMPurify와 같은 정제 라이브러리 사용을 고려하십시오."
            },
            {
                "Detected directly writing to a Response object from user-defined input. This bypasses any HTML escaping and may expose your application to a Cross-Site-scripting (XSS) vulnerability. Instead, use 'resp.render()' to render safely escaped HTML.",
                "사용자 정의 입력을 사용하여 Response 객체에 직접 작성하는 것이 감지되었습니다. 이 방식은 HTML 이스케이프를 우회하여 교차 사이트 스크립팅(XSS) 취약점에 노출될 수 있으므로, 대신 'resp.render()'를 사용하여 안전하게 이스케이프된 HTML을 렌더링하십시오."
            },
            {
                "The application is using an XML parser that has not been safely configured. This might lead to XML External Entity (XXE) vulnerabilities when parsing user-controlled input. An attacker can include document type definitions (DTDs) or XIncludes which can interact with internal or external hosts. XXE can lead to other vulnerabilities, such as Local File Inclusion (LFI), Remote Code Execution (RCE), and Server-side request forgery (SSRF), depending on the application configuration. An attacker can also use DTDs to expand recursively, leading to a Denial-of-Service (DoS) attack, also known as a `Billion Laughs Attack`. The best defense against XXE is to have an XML parser that supports disabling DTDs. Limiting the use of external entities from the start can prevent the parser from being used to process untrusted XML files. Reducing dependencies on external resources is also a good practice for performance reasons. It is difficult to guarantee that even a trusted XML file on your server or during transmission has not been tampered with by a malicious third-party.",
                "애플리케이션이 안전하게 구성되지 않은 XML 파서를 사용하고 있습니다. 이는 사용자 제어 입력을 파싱할 때 XML 외부 개체(XXE) 취약점을 초래할 수 있습니다. 공격자는 문서 유형 정의(DTD) 또는 XInclude를 포함하여 내부 또는 외부 호스트와 상호작용할 수 있습니다. XXE는 애플리케이션 구성에 따라 로컬 파일 포함(LFI), 원격 코드 실행(RCE), 서버 측 요청 위조(SSRF)와 같은 추가적인 취약점을 유발할 수 있습니다. 또한, 공격자는 DTD를 재귀적으로 확장하여 Billion Laughs Attack이라고 알려진 서비스 거부(DoS) 공격을 수행할 수도 있습니다. XXE에 대한 최선의 방어책은 DTD를 비활성화할 수 있는 XML 파서를 사용하는 것입니다. 초기부터 외부 개체 사용을 제한하면 신뢰할 수 없는 XML 파일을 파싱하는 것을 방지할 수 있습니다. 또한, 외부 리소스에 대한 의존도를 줄이는 것은 성능 측면에서도 좋은 관행입니다. 서버에 있는 신뢰할 수 있는 XML 파일조차도 전송 중 악의적인 제3자에 의해 조작되지 않았다고 보장하기 어렵습니다."
            },
            {
                "The application is using an XML parser that has not been safely configured. This might lead to XML External Entity (XXE) vulnerabilities when parsing user-controlled input. An attacker can include document type definitions (DTDs) which can interact with internal or external hosts. XXE can lead to other vulnerabilities, such as Local File Inclusion (LFI), Remote Code Execution (RCE), and Server-side request forgery (SSRF), depending on the application configuration. An attacker can also use DTDs to expand recursively, leading to a Denial-of-Service (DoS) attack, also known as a Billion Laughs Attack. The best defense against XXE is to have an XML parser that supports disabling DTDs. Limiting the use of external entities from the start can prevent the parser from being used to process untrusted XML files. Reducing dependencies on external resources is also a good practice for performance reasons. It is difficult to guarantee that even a trusted XML file on your server or during transmission has not been tampered with by a malicious third-party.",
                "애플리케이션이 안전하게 구성되지 않은 XML 파서를 사용하고 있습니다. 이는 사용자 제어 입력을 파싱할 때 XML 외부 개체(XXE) 취약점을 초래할 수 있습니다. 공격자는 문서 유형 정의(DTD)를 포함하여 내부 또는 외부 호스트와 상호작용할 수 있습니다. XXE는 애플리케이션 구성에 따라 로컬 파일 포함(LFI), 원격 코드 실행(RCE), 서버 측 요청 위조(SSRF)와 같은 추가적인 취약점을 유발할 수 있습니다. 또한, 공격자는 DTD를 재귀적으로 확장하여 Billion Laughs Attack이라고 알려진 서비스 거부(DoS) 공격을 수행할 수도 있습니다. XXE에 대한 최선의 방어책은 DTD를 비활성화할 수 있는 XML 파서를 사용하는 것입니다. 초기부터 외부 개체 사용을 제한하면 신뢰할 수 없는 XML 파일을 파싱하는 것을 방지할 수 있습니다. 또한, 외부 리소스에 대한 의존도를 줄이는 것은 성능 측면에서도 좋은 관행입니다. 서버에 있는 신뢰할 수 있는 XML 파일조차도 전송 중 악의적인 제3자에 의해 조작되지 않았다고 보장하기 어렵습니다."
            },
        };
}