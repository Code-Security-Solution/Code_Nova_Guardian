# ❗ 모종의 이유로 사용하지 않는 보안 취약점 검사 도구들

- SonarQube
  - 최우선으로 고려했던 도구이나 로컬로 스캐너를 돌린 후 서버를 별도로 돌려야 하는 번거로움, 웹 API로 결과를 가져와야 하는 불편함, 무료인 커뮤니티 버전의 낮은 보안 취약점 탐지율로 사용을 중단하였습니다.

- CodeQL
  - 설치의 번거로움으로 사용하지 않습니다.

- OWASP Dependency-Check or OWASP ZAP
  - 웹 전용이며 OWASP ZAP은 동적 분석도구라 정적 분석만 커버하는 본 프로젝트 상 사용하지 않습니다.

- Security Code Scan
  - C#, VB.NET 등 특정 프로그래밍 언어에 한정 되어 있습니다.
- Bandit
  - Python 에만 한정되어 있습니다.

- DevSkim
  - 사용자 Pool 이나 정보의 부재로 사용하지 않습니다.
