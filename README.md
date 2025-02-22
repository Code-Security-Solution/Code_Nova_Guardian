# Code_Nova_Guardian

- 해당 프로그램은 ```CLI``` 방식의 프로그램으로, 소스코드의 경로 및 그 소스코드와 상세한 정보를 입력으로 받으면 이에 맞춰서 그 소스코드의 취약점 및 개선점을 분석하고 그 결과를 출력하는 프로그램 입니다.
- 해당 프로그램의 개발은 ```Windows``` 로, 실제로 동작하는 ```OS```는 ```Linux```로 목표를 잡았습니다. 따라서 해당 ```CLI``` 프로그램은 크로스 플랫폼 지원이 가능한 ```C#```으로 개발하였고, **Windows 및 Linux에서 크로스 플랫폼으로 동작합니다.** *(Mac은 공식적으로 지원하지 않습니다.)*

## 해당 프로그램이 의존중인 코드 취약점 분석 도구

- 소스코드의 취약점 및 개선점 분석은 시중에 나와 있는 다양한 오픈소스 형태의 코드 취약점 분석 도구를 조합하고, 자동화 하여 작동합니다. 밑은 해당 프로그램에서 의존하고 있는 코드 취약점 분석 도구 목록입니다. 아직은 프로젝트 초기 단계이기에 의존하는 분석 도구는 개발 경과에 따라 달라질 수 있음에 유의해주세요.

  - Semgrep

    규칙 기반으로 동작하는 코드 분석 도구로, ```SonarQube```와 유사한 정적 분석 도구입니다. 꼭 보안 목적 뿐만이 아니라 규칙을 지정해 코드의 여러가지 부분을 검색해낼 수 있는 장점을 지니고 있습니다. 커뮤니티 버전(무료 버전)으로도 쓸만한 성능을 제공하고 나쁘지 않은 탐지율을 보입니다. 본 프로젝트에서 메인으로 사용하는 도구입니다.

## 빌드

해당 프로그램은 ```visual studio 2024``` 와 ```.NET 8```로 빌드됩니다. net 버전의 경우 향후 변경될 수 있으며 해당 프로그램 여러 보안 검사 도구를 사용하기에 되도록 LTS 버전을 지양합니다. ```vs2024```와 ```.NET 8```이 설치된 어떤 컴퓨터에서든 문제 없이 빌드할 수 있습니다.

## 사용법

- 우선적으로 컴퓨터에 ```Docker```이 깔려 있어야 ```Code Nova Guardian```을 동작시켜볼 수 있습니다. 리눅스나 Unix 계열 운영체제의 경우 인터넷 검색을 통해 ```Docker```을 설치하시면 되며, 윈도우의 경우 ```Docker Desktop```을 설치해주시면 됩니다.
  - 다만 ```Docker Desktop```의 경우 동작을 위해 ```WSL2```가 필요하고```WSL2```가 돌아가기 위해선 윈도우에서 ```Hyper-V``` 및 CPU 가상화 기능이 반드시 활성화 되어야 합니다. 이 자세한 내용 역시 인터넷 검색을 참고해주세요.


## 현재 사용 고려중인 보안 검사 도구들

1. SonarQube
   - 최우선으로 고려했던 도구이나 로컬로 스캐너를 돌린 후 서버를 별도로 돌리고 웹 API로 결과를 가져와야 하는 불편함, 무료인 커뮤니티 버전의 성능이 많이 떨어져 사용을 할 지 말지 고민하고 있습니다.
2. Semgrep - **사용중**
3. CodeQL
4. OWASP Dependency-Check or OWASP ZEP (웹 전용)
5. Security Code Scan (C#, VB.NET 전용)
6. DevSkim
7. Bandit (파이썬 전용)
8. 미정
