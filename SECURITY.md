# 보안 정책 (Security Policy)

## 보안 취약점 보고

보안 취약점을 발견하셨다면:
1. GitHub Issues에 공개적으로 보고하지 **마세요**
2. [보안 이슈 제출](https://github.com/trollgameskr/UnlockOpenFile/security/advisories/new)을 통해 비공개로 보고해주세요

## 개인정보 보호

UnlockOpenFile은:
- **네트워크 통신을 하지 않습니다**: 인터넷 연결이나 외부 서버 통신이 없습니다
- **데이터를 수집하지 않습니다**: 사용자 데이터나 통계를 수집하지 않습니다
- **로컬에서만 작동합니다**: 모든 작업은 사용자의 컴퓨터 내에서만 이루어집니다

## 필요한 권한

UnlockOpenFile이 요구하는 권한:

| 권한 | 목적 | 범위 |
|------|------|------|
| 파일 읽기/쓰기 | 임시 파일 생성 및 원본 파일 동기화 | 사용자가 선택한 파일에만 접근 |
| 레지스트리 접근 (사용자 레벨) | 파일 연결 및 시작 프로그램 설정 | HKEY_CURRENT_USER만 사용, 시스템 레지스트리 변경 없음 |
| 프로세스 모니터링 | 파일 편집 프로그램 종료 감지 | 자신이 실행한 프로세스만 모니터링 |

**관리자 권한이 필요하지 않습니다** - 모든 작업이 사용자 레벨에서 이루어집니다.

## 오픈소스 투명성

- **라이선스**: MIT 라이선스 하에 배포되는 무료 오픈소스 소프트웨어입니다
- **투명성**: 모든 소스 코드가 공개되어 있으며, 누구나 검토할 수 있습니다
- **커뮤니티**: GitHub에서 이슈와 풀 리퀘스트를 통해 커뮤니티와 함께 개발됩니다
- **상업성 없음**: 유료 기능이나 광고가 전혀 없습니다

## 안전성 확인 방법

**소스 코드 검토 및 자체 빌드:**

1. **소스 코드 검토**: 
   - 모든 소스 코드는 [GitHub 저장소](https://github.com/trollgameskr/UnlockOpenFile)에서 공개되어 있습니다
   - 코드를 직접 검토하여 악의적인 동작이 없음을 확인할 수 있습니다

2. **자체 빌드**:
   ```bash
   git clone https://github.com/trollgameskr/UnlockOpenFile.git
   cd UnlockOpenFile
   
   # Standalone 빌드 (권장)
   dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-standalone
   
   # 또는 경량 빌드
   dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
   ```
   직접 소스에서 빌드하여 사용할 수 있습니다.

3. **VirusTotal 검사**:
   - 배포된 파일을 [VirusTotal](https://www.virustotal.com)에 업로드하여 여러 보안 엔진의 검사 결과를 확인할 수 있습니다

## 질문이 있으신가요?

- GitHub Issues에서 질문하기: https://github.com/trollgameskr/UnlockOpenFile/issues
- 소스 코드 보기: https://github.com/trollgameskr/UnlockOpenFile
