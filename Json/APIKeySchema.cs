namespace Code_Nova_Guardian.Json;
using static Code_Nova_Guardian.Global.Global;

// API Key값을 모아두는 api_key.json 파일의 정의
public class APIKeyJsonRootObject
{
    // json 기본값 지정을 위해, C# 최신 기능인 프로퍼티 초기화 기능 활용
    public string semgrep { get; set; } = APIKeys.EMPTY_API_VALUE;
}