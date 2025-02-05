namespace Code_Nova_Guardian.API_Keys
{
    /*
      API Key의 선언 부분. 실제로 이 파일은 git에 commit 되며 여기선 api key의 값 할당 없이 뼈대만 선언한다.
      partial class로 선언되어 있고, 이 파일은 APIKeysLocal.cs 에서 실제로 api 값을 설정한다.
      (부분 클래스에 대해선 정확히 잘 알지 못하겠다. 이건 그냥 그렇구나 하고 받아 들인다. 나중에 추가로 공부가 필요할 거 같다.)
      
      이러한 방식은 git commit 시 api key가 하드코딩되어 들어가는 것을 방지하는 방식으로 APIKeysLocal.cs 에 대한 작성법은
      https://stackoverflow.com/questions/21774844/proper-way-to-hide-api-keys-in-git
      여기를 참고한다.
    */

    /*
    APIKeysLocal.cs 파일은 다음과 같은 형태로 작성한다.

    namespace Code_Nova_Guardian.API_Keys
           {
               public static partial class APIKeys
               {
                   static APIKeys()
                   {
                       semgrep_token = "token_value"; // 실제 토큰 값을 여기에 작성한다.
                   }
               }
           
           }
    
    제대로 작성하는 조건
    1. namespace 가 당연히 동일해야 같은 scope(코드 부분) 로 인식된다.
    2. partial class 뒤에 이름이 동일해야 한다. 여기서는 "APIKeys" 라는 이름으로 통일한다.
    */
    public static partial class APIKeys
    {
        public static readonly string semgrep_token; // cli에서 semgrep login 명령어 실행 시 발급 받는 토큰 값
    }

}