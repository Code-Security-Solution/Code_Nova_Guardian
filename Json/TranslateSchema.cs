namespace Code_Nova_Guardian.Json;


public class TranslateJsonRootObject
{
    public Pattern[] patterns { get; set; }
    public dictionary[] dictionary { get; set; }

    // 기본 생성자 추가, json 파일 생성시 기본값 설정용
    public TranslateJsonRootObject()
    {
        dictionary = Array.Empty<dictionary>(); // 또는 []
        patterns = Array.Empty<Pattern>(); // 또는 []
    }
}

// Pattern 을 먼저 확인하고, 나중에 dictionary 를 이용해 번역
public class Pattern
{
    public string regex { get; set; }
    public string message { get; set; }
}

// C#의 기본 타입인 'Dictionary'와 다르게 직접 정의한 것임에 주의
public class dictionary
{
    public string origin { get; set; }
    public string message { get; set; }
}

