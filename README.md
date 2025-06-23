# Code_Nova_Guardian

- 해당 프로그램은 ```CLI``` 방식의 프로그램으로, 소스코드의 경로 및 그 소스코드와 상세한 정보를 입력으로 받으면 이에 맞춰서 그 소스코드의 취약점 및 개선점을 분석하고 그 결과를 출력하는 프로그램 입니다.
- 해당 프로그램의 개발은 ```Windows``` 로, 실제로 동작하는 ```OS```는 ```Linux```로 목표를 잡았습니다. 따라서 해당 ```CLI``` 프로그램은 크로스 플랫폼 지원이 가능한 ```C#```으로 개발하였고, **Windows 및 Linux에서 크로스 플랫폼으로 동작합니다.** *(Mac은 공식적으로 지원하지 않습니다.)*

## 🛠️ 해당 프로그램이 의존중인 코드 취약점 분석 도구

- 소스코드의 취약점 및 개선점 분석은 시중에 나와 있는 다양한 오픈소스 형태의 코드 취약점 분석 도구를 조합하고, 자동화 하여 작동합니다. 밑은 해당 프로그램에서 의존하고 있는 코드 취약점 분석 도구 목록입니다. 아직은 프로젝트 초기 단계이기에 의존하는 분석 도구는 개발 경과에 따라 달라질 수 있음에 유의해주세요.

  - Semgrep

    규칙 기반으로 동작하는 코드 분석 도구로, ```SonarQube```와 유사한 정적 분석 도구입니다. 꼭 보안 목적 뿐만이 아니라 규칙을 지정해 코드의 여러가지 부분을 검색해낼 수 있는 장점을 지니고 있습니다. 커뮤니티 버전(무료 버전)으로도 쓸만한 성능을 제공하고 나쁘지 않은 탐지율을 보입니다. 본 프로젝트에서 메인으로 사용하는 도구입니다.

## ⚙️ 빌드

해당 프로그램은 ```Visual Studio 2024``` 와 ```.NET 8```로 빌드됩니다. net 버전의 경우 향후 변경될 수 있으며 해당 프로그램 여러 보안 검사 도구를 사용하기에 되도록 LTS 버전을 지양합니다. ```VS2024```와 ```.NET 8```이 설치된 어떤 컴퓨터에서든 문제 없이 빌드할 수 있습니다.



빌드를 원하시면 ```.NET 8```을 설치하신 후, 소스코드 안의 ```build.bat```을 실행시켜주세요. ```bin\Publish``` 폴더에 빌드 결과가 저장됩니다. 기본적으로 실행 파일의 경우 ```.NET 8```을 설치하지 않아도 사용할 수 있게 바이너리 안에 ```.NET 8```을 내장시키는 형태로 빌드를 하고 있습니다. 이 이유로 일종의 Trade Off로 바이너리 안의 ```.NET```을 풀어서 실행시켜야 하기에 실행 파일의 초기 로딩 속도는 조금 느려집니다.

## 📌 요구사항

- 우선적으로 컴퓨터에 ```Docker```이 깔려 있어야 ```Code Nova Guardian```을 동작시켜볼 수 있습니다. 리눅스나 Unix 계열 운영체제의 경우 인터넷 검색을 통해 ```Docker```을 설치하시면 되며, 윈도우의 경우 ```Docker Desktop```을 설치해주시면 됩니다.
  - 다만 ```Docker Desktop```의 경우 동작을 위해 ```WSL2```가 필요하고```WSL2```가 돌아가기 위해선 윈도우에서 ```Hyper-V``` 및 CPU 가상화 기능이 반드시 활성화 되어야 합니다. 이 자세한 내용 역시 인터넷 검색을 참고해주세요.
- 또한 해당 CLI 프로그램 구동시 ```Windows Terminal``` 프로그램으로 구동하기를 강력 권장합니다. 이유는 해당 프로그램에서 가독성을 위해 이모지 아이콘 및 유니코드 특수문자를 사용해 로그를 출력하기 때문입니다. 윈도우 11 최신 버전부턴 ```Windows Terminal```이 기본 탑재되어 있는 경우가 있으나 설치되어 있지 않거나, 윈도우 10을 사용중이라면 ```Microsoft Store``` 프로그램을 이용해 설치해주세요.

## 🏗️ 사용 방법

리눅스, 윈도우 2개의 OS를 기준으로 사용 방법을 소개합니다. 우선 기초적인 **Semgrep 을 통한 코드 취약점 탐색**에 대해 다룹니다.

[해당 링크]("https://github.com/Code-Security-Solution/Code_Nova_Guardian/releases/latest")로 이동해 OS에 맞게 실행 파일을 받아주세요. CLI 환경에서 테스트를 진행할 수 있습니다.



```bash
docker -v
```

위에서 설명한대로 해당 도구는 Docker을 사용하여 Semgrep 의 실행 환경을 분리하고 있습니다. 위 명령어를 쳐서 docker가 설치되었는지 우선 확인해주세요.



```
Docker version 28.0.1, build 068a01e
```

제대로 설치됐다면 docker의 버전, 빌드명이 표시되어야 합니다. 만약에 표시되지 않는 경우



```bash
sudo wget -qO- http://get.docker.com/ | sh # 배포판에 관계없이 설치
```

리눅스의 경우 다음 명령어로 docker을 설치합니다. 이 스크립트를 사용하면 배포판에 관계없이 docker 설치가 가능합니다. 설치에는 시간이 소요되니 잠시 기다려주세요.



https://docs.docker.com/desktop/setup/install/windows-install/

윈도우의 경우엔 위 링크에서 64비트(=x86_64) 버전의 Docker Desktop 을 받아 설치해줍니다.  ```WSL2``` 가 제대로 설치되어야만 제대로 설치가 가능합니다. WSL2 설치, Hyper-V 가상화, BIOS 가상화 활성화 등에 관해선 인터넷에 여러 정보가 있으니 참고해주세요. 여기선 생략합니다.



```bash
.\Code_Nova_Guardian.exe check-requirement # 윈도우의 경우
sudo ./Code_Nova_Guardian check-requirement # 리눅스의 경우
```

아까 전에 받은 실행 파일 위치로 cd 명령어를 통해 이동후 위와 같이 check-requirement 옵션으로 프로그램을 실행합니다. Docker이 제대로 설치되었고, API로 호출 가능한지 확인하는 파라미터입니다.

**참고로 윈도우의 경우 Docker Desktop이 반드시 실행된 상태서 해당 명령어를 수행해야 하고, 리눅스의 경우 슈퍼 유저 권한인 sudo로 실행을 권장합니다.**

* 윈도우 사용자에 한해 Docker Desktop이 기본 경로 ```C:\Program Files\Docker\Docker\Docker Desktop.exe```에 설치된 경우 실행되고 있지 않으면 자동 실행 기능이 구현되어 있습니다. 다만, 기본 경로가 아닌 다른 경로에 설치하면 작동하지 않으며 항상 동작을 보장하지 않으므로 자동 실행 기능이 작동하지 않으면 **직접 실행** 해주세요.



```bash
Docker Host의 상태를 확인합니다.

✅ Docker가 Host에서 실행 중입니다!
🐳 도커 버전: YOUR_VERSION_HERE
🔗 API 버전: YOUR_API_VERSION_HERE
필요한 프로그램 설치가 확인되었습니다.
```

제대로 인식이 성공하면 위와 같은 메세지가 출력되어야 정상입니다. 

또한 해당 프로그램이 실행된 순간 같은 경로에 설정 파일을 저장하기 위한 CNG 폴더가 자동 생성됩니다.



만약 오류가 발생한다면 다시 언급하지만 윈도우의 경우엔 Docker Desktop 의 실행 상태 여부를, 리눅스의 경우 sudo로 실행했는지 꼭 확인해주세요.

**추가 주의 사항 : 실행 파일 명을 "cng.exe" 로 변경하는건 상관없지만 "cng" 로 변경하지마세요. 설정 파일을 저장하는 폴더의 이름이 CNG 기 때문에 중복되어 프로그램 실행이 불가합니다.**



```powershell
# 윈도우
git clone https://github.com/snoopysecurity/Vulnerable-Code-Snippets.git # 취약점이 존재하는 코드들 모아놓은 레포지토리 다운로드
ren "Vulnerable-Code-Snippets\SQL Injection\Cryptolog,php" "Cryptolog.php" # 레포지토리 확장자 오타 수정
```

```bash
# 리눅스
git clone https://github.com/snoopysecurity/Vulnerable-Code-Snippets.git # 취약점이 존재하는 코드들 모아놓은 레포지토리 다운로드
mv "Vulnerable-Code-Snippets/SQL Injection/Cryptolog,php" "Vulnerable-Code-Snippets/SQL Injection/Cryptolog.php" # 레포지토리 확장자 오타 수정
```

이제 취약점이 존재하는 코드들을 여럿 모아놓은 **[Vulnerable-Code-Snippets](https://github.com/snoopysecurity/Vulnerable-Code-Snippets)** 저장소의 코드 파일들을 가져올 시간입니다. 이 취약점이 가득한 코드들로 테스트를 진행합니다. git 명령어를 사용해 다운로드 합니다. 



---

### 2024-03-30 추가

제 개발 환경에서 재현이 불가능해, 이유는 모르겠으나 ```git clone```으로 파일을 다운로드해 가져온 경우 일부 파일이 깨지는 문제가 보고되었습니다. 관련 issue는 아래 스캔이 실패한 경우에 적혀 있습니다. 문제 발생을 원치 않으시면 위 Vulnerable-Code-Snippets 사이트로 직접 들어가 다운로드 받는것도 추천합니다.

---



```bash
.\Code_Nova_Guardian.exe get-semgrep-token # 윈도우
sudo ./Code_Nova_Guardian.exe get-semgrep-token # 리눅스
```

이제 Semgrep으로 코드의 보안 취약점을 스캔해볼 차례입니다. 다만 Semgrep scan을 위해선 로그인이 필요하며, 이 로그인 식별을 위해 사용되는 것이 바로 ```token``` 이라는 값입니다. 위 명령어를 통해 token 값을 획득해봅시다.



```
✅ Docker가 Host에서 실행 중입니다!
🐳 도커 버전: 28.0.1
🔗 API 버전: 1.48
⚡ returntocorp/semgrep 이미지가 존재하지 않습니다. 다운로드를 시작합니다!
Pulling from returntocorp/semgrep
Pulling fs layer
Pulling fs layer
Pulling fs layer
...
Download complete
Download complete
Download complete
Download complete
Download complete
Download complete
Downloading
...
returntocorp/semgrep : 이미지 다운로드가 완료되었습니다!
ℹ️ 곧 콘솔에 https://semgrep.dev/login... 링크가 나타납니다.
클릭 또는 Ctrl + 클릭으로 링크에 들어가서 Semgrep 페이지가 나오면,
가능한 방법으로 로그인 후 Activate 버튼을 눌러주세요. 🚀


Login enables additional proprietary Semgrep Registry rules and running custom policies from Semgrep Cloud Platform.
Opening login at: https://semgrep.dev/... <= 이 주소로 이동

Once you've logged in, return here and you'll be ready to start using new Semgrep rules.
```

초기에 Semgrep 이미지가 Docker에 설치되어 있지 않다면 프로그램이 이를 자동으로 설치합니다. 다운로드를 기다려주시면 되며 마지막으로 기다리면 콘솔에 지침이 나오는데 이 지침대로 따라주시면 됩니다. 콘솔에서 https://semgrep.dev/... 로 시작하는 링크를 Ctrl + 클릭으로 이동하고, 로그인 후 Activate를 눌러주세요.

만약에 로그인을 했는데 Activate 가 뜨지 않는다면 해당 링크에 다시 접근해주세요.



```
Login enables additional proprietary Semgrep Registry rules and running custom policies from Semgrep Cloud Platform.
Opening login at: https://semgrep.dev/login...

Once you've logged in, return here and you'll be ready to start using new Semgrep rules.
Saved login token

        YOUR_TOKEN_HERE <<< 이것을 복사

in /root/.semgrep/settings.yml.
Note: You can always generate more tokens at https://semgrep.dev/orgs/-/settings/tokens
```

다시 터미널 창으로 돌아오면 ```YOUR_TOKEN_HERE``` 부분에 얻어낸 토큰이 뜹니다. 이 토큰을 잘 복사해 보관해주세요. API Key와 같은 존재기 때문에 소중하게 보관해주셔야 합니다. **이 값을 남에게 함부로 전송하거나 유출하지 마세요.**



이후 CNG 폴더 -> api_key.json 파일을 열고 YOUR_API_KEY_HERE 부분에 방금 얻은 토큰값을 입력합니다. 공백은 없어야 하며 오타가 발생해도 안됩니다. 토큰값이 입력되지 않으면 스캔이 정상적으로 이루어지지 않습니다.



### 스캔 시작

```bash
# 윈도우
.\Code_Nova_Guardian.exe scan semgrep "./Vulnerable-Code-Snippets" "./code-scan-result.json" --no-pro-message --translate "./code-scan-result_translated.json"

# 리눅스
sudo ./Code_Nova_Guardian scan semgrep "./Vulnerable-Code-Snippets" "./code-scan-result.json" --no-pro-message --translate "./code-scan-result_translated.json"
```

이제 Semgrep을 통한 코드 취약점 분석을 시도합니다. 현재 최적화 작업을 하지 않았고 탐지율을 높히기 위해 1500개 이상의 규칙을 사용중이라 고사양 CPU 및 16GB 이상의 RAM을 권장하고 있습니다. (추후에 수정될 예정)

스캔에는 시간이 좀 소요되며 다 끝나면 ```code-scan-result.json``` 파일이 생성되며 이곳에 코드 분석 결과가 저장됩니다. 또한 번역 사전에 의해 번역되고 포맷팅 까지 완료된 ```code-scan-result_translated.json``` 파일 역시 생성됩니다.

만약에 출력 결과에 Semgrep Pro 결제 메세지를 포함 시키고 싶으신 분들은 ```--no-pro-message``` 옵션을 제거하시고 스캔을 진행하시면 됩니다.

## 🕵️ 문제 해결

### 만약 get-semgrep-token이 작동하지 않을 시

```bash
docker run -it returntocorp/semgrep semgrep login # 토큰을 얻기 위한 로그인 시도
```

만약에 ```get-semgrep-token``` 명령어가 작동하지 않는다면 returntocorp/semgrep 컨테이너를 -it (interaction) 모드로 직접 login 파라미터를 준 후 실행시켜서 토큰을 획득해주세요. 위 명령어로 실행 후 아까 앞에서 본 지침을 따라주시면 됩니다. 동작 자체는 동일합니다.



### ❌ 스캔이 실패한 경우

스캔이 실패한 경우엔 여러가지 요인이 있겠지만 현재 확인된 결과로는 스캔할 파일에 문제가 발생시에 스캔 중에 오류가 발생할 확률이 높습니다.
현재 정확한 이유는 확인되지 않았지만 ```git clone```을 통해 받은 ```Vulnerable-Code-Snippets``` 의 파일 중 ```'Command Injection/cmd2.php'``` 파일에 문제가 생겨 스캔 실패가 발생한 것을 확인했습니다.
따라서 관련 문제 발생시 ```cmd2.php``` 파일을 삭제 바라며 이외의 문제가 되는 파일이 있으면 직접 열어보시고, 문제가 있다고 판정시 삭제후 스캔을 진행하시길 바라겠습니다.



## 🔒 Semgrep 이외의 보안 검사 도구들

해당 프로젝트는 보안 취약점 탐색 시 현재로선 **Semgrep**만 이용합니다.  

이유에 관해선 해당 [문서]("https://github.com/Code-Security-Solution/Code_Nova_Guardian/blob/main/AboutSecurityTools.md")를 참고해주세요.
