/*
  File - scoped Namespace 기능 사용
  일반적으로 C#에서는 namespace 1개를 사용하며 namespace 안에 코드를 넣기 위해
  중괄호를 넣는데, 이렇게 되면 C#의 소스코드는 무조건 들여쓰기 1번으로 시작해 번거롭다.
  File-scoped Namespace 기능을 사용하면 중괄호로 묶지 않고 맨 위에 namespace를 세미콜론 1개로 선언하고,
  중괄호로 묶을 필요가 없어져서 더 코드 읽기가 간편해진다.
*/

using Code_Nova_Guardian.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;

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
                // patterns 안에 항목에 regex가 비어있지 않을때만 반복
                if (pattern.regex != null)
                {
                    // 사용자가 json 에 등록한 regex 패턴으로 regex 객체 생성
                    Regex regex = new Regex(pattern.regex);

                    // 입력 텍스트에서 regex 패턴 일치 체크
                    Match match = regex.Match(origin_text);

                    // 정규식 패턴이 일치할 경우 처리
                    if (regex.IsMatch(origin_text))
                        // 정규식으로 매칭된 부분을 바탕으로 자리 표시자를 동적으로 교체
                        return format_message(pattern.message, match);
                }
            }
        }

        // 사전 기반 번역을 수행 (해시맵 기반 변환, O(1))
        if (translate_type == TranslateType.Dictionary)
        {
            // dictionary_map에서 origin_text를 찾으면 번역 결과 반환, 없으면 빈 문자열 반환
            return dictionary.TryGetValue(origin_text, out string translated_text) ? translated_text : "";
        }

        // 번역 실패 시 빈 문자열 반환
        return "";
    }

    // 메시지에서 $1, $2 등을 동적으로 대체
    private string format_message(string message_template, Match match)
    {
        // 메시지에서 $1, $2, $3 등 플레이스홀더를 차례대로 대체
        for (int i = 1; i < match.Groups.Count; i++)
        {
            // 그룹 번호에 맞는 플레이스홀더($1, $2 등)을 매칭된 값으로 교체
            message_template = message_template.Replace($"${i}", match.Groups[i].Value);
        }

        return message_template;
    }

    //public void hardcode_to_file()
    //{
    //    // semgrep_dic에 있는 하드코딩된 번역 항목들을 JSON 파일의 dictionary 부분에 병합합니다.
    //    // 기존에 있던 dictionary 항목들은 유지하고, 중복되지 않는 항목들만 추가하도록 합니다.

    //    // 기존 JSON 파일에서 읽어들인 dictionary 항목들을 리스트로 변환합니다.
    //    // 만약 root.dictionary가 null이라면 새 리스트를 생성합니다.
    //    var updatedDictionaryList = new List<dictionary>();
    //    if (root.dictionary != null)
    //    {
    //        updatedDictionaryList.AddRange(root.dictionary);
    //    }

    //    // semgrep_dic의 각 항목을 순회하며, 동일한 origin이 없는 경우에만 추가합니다.
    //    foreach (var pair in semgrep_dic)
    //    {
    //        // 기존 리스트에 동일한 origin이 있는지 확인합니다.
    //        bool exists = updatedDictionaryList.Any(item => item.origin.Equals(pair.Key));
    //        if (!exists)
    //        {
    //            updatedDictionaryList.Add(new dictionary
    //            {
    //                origin = pair.Key,
    //                message = pair.Value  // 여기서 'message'는 번역된 텍스트를 의미합니다.
    //            });
    //        }
    //    }

    //    // 업데이트된 리스트를 배열로 변환하여 root.dictionary에 재할당합니다.
    //    root.dictionary = updatedDictionaryList.ToArray();

    //    // JSON 직렬화 옵션 (가독성을 위해 들여쓰기를 적용)
    //    var serializeOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    //    // 업데이트된 root 객체를 JSON 문자열로 직렬화합니다.
    //    string jsonOutput = JsonSerializer.Serialize(root, serializeOptions);

    //    // semgrep_json_file_path에 해당하는 파일에 직렬화된 JSON을 저장합니다.
    //    File.WriteAllText(semgrep_json_file_path, jsonOutput);
    //}

}