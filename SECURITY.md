# 보안 및 Windows Defender 안내

## Windows Defender 오탐 (False Positive) 안내

### 왜 Windows Defender가 바이러스로 탐지하나요?

UnlockOpenFile은 완전히 안전한 오픈소스 소프트웨어이지만, **경량 빌드(framework-dependent build)**가 일부 보안 프로그램에서 오탐될 수 있습니다:

**오탐 현황:**
- ❌ **경량 빌드** (UnlockOpenFile-vX.X.X.zip): Windows Defender가 `Trojan:Script/Wacatac.B!ml`로 탐지할 수 있음
- ✅ **Standalone 빌드** (UnlockOpenFile-vX.X.X-standalone.zip): 오탐 가능성 낮음

**왜 경량 빌드만 오탐되나요?**

경량 빌드는 다음과 같은 특성 때문에 휴리스틱 분석에서 의심될 수 있습니다:
1. **작은 파일 크기**: 약 300KB의 작은 실행 파일
2. **동적 로딩**: .NET Runtime을 동적으로 로드하는 특성
3. **SingleFile 압축**: 단일 파일로 압축되어 있어 일부 패커(packer)와 유사하게 보일 수 있음

반면 Standalone 빌드는:
- .NET Runtime을 포함하여 약 170MB의 완전한 PE 구조
- 더 많은 메타데이터와 리소스 포함
- 네이티브 코드가 포함되어 있어 더 "정상적"으로 보임

**프로그램의 정상 기능:**

UnlockOpenFile은 다음과 같은 정상적인 기능을 사용합니다:
1. **프로세스 모니터링**: 파일을 여는 프로그램의 종료 여부를 감지합니다
2. **Named Pipes 사용**: 단일 인스턴스 관리를 위해 프로세스 간 통신(IPC)을 사용합니다
3. **레지스트리 접근**: 파일 연결 설정 및 시작 프로그램 등록을 위해 레지스트리를 사용합니다
4. **파일 시스템 감시**: FileSystemWatcher를 통해 임시 파일의 변경사항을 감지합니다
5. **코드 서명 부재**: 현재 버전은 코드 서명이 되어 있지 않습니다

이러한 기능들은 모두 프로그램의 정상적인 작동을 위해 필요하며, 악의적인 목적이 없습니다.

### 안전성 확인 방법

**🌟 가장 쉬운 방법: Standalone 빌드 사용**

Standalone 빌드는 Windows Defender 오탐 가능성이 매우 낮습니다:
- 다운로드: `UnlockOpenFile-vX.X.X-standalone.zip`
- .NET Runtime 설치 불필요
- 완전한 실행 파일 구조로 인해 보안 프로그램의 신뢰도가 높음

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
   - Standalone 빌드는 대부분의 보안 엔진에서 안전한 것으로 판정됩니다

### Windows Defender에서 허용하는 방법

#### 방법 1: 파일 예외 추가

1. Windows 보안 열기 (시작 메뉴 > "Windows 보안" 검색)
2. "바이러스 및 위협 방지" 클릭
3. "바이러스 및 위협 방지 설정 관리" 클릭
4. "제외 항목" 섹션에서 "제외 항목 추가 또는 제거" 클릭
5. "제외 추가" 클릭 > "파일" 선택
6. UnlockOpenFile.exe 파일 선택

#### 방법 2: 폴더 예외 추가

1. 위와 동일한 방법으로 "제외 추가"까지 진행
2. "폴더" 선택
3. UnlockOpenFile.exe가 있는 폴더 선택

### Microsoft에 오탐 신고하기

오탐 판정을 개선하려면 Microsoft에 신고할 수 있습니다:

1. [Microsoft 보안 인텔리전스 제출 포털](https://www.microsoft.com/en-us/wdsi/filesubmission) 방문
2. "Submit a file for malware analysis" 선택
3. 파일 업로드 및 정보 제공
4. "This file is a false positive (clean)" 선택

### 오픈소스 보증

- **라이선스**: MIT 라이선스 하에 배포되는 무료 오픈소스 소프트웨어입니다
- **투명성**: 모든 소스 코드가 공개되어 있으며, 누구나 검토할 수 있습니다
- **커뮤니티**: GitHub에서 이슈와 풀 리퀘스트를 통해 커뮤니티와 함께 개발됩니다
- **상업성 없음**: 유료 기능이나 광고가 전혀 없습니다

### 향후 계획

오탐을 줄이기 위한 향후 계획:

1. **코드 서명 인증서 적용** (비용 문제로 현재는 미적용)
2. **지속적인 Microsoft 오탐 신고**
3. **빌드 프로세스 개선** (결정론적 빌드, 재현 가능한 빌드)
4. **자동화된 보안 검사** (VirusTotal 등)

### 보안 취약점 보고

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

| 권한 | 목적 | 악용 가능성 |
|------|------|------------|
| 파일 읽기/쓰기 | 임시 파일 생성 및 원본 파일 동기화 | 사용자가 선택한 파일에만 접근 |
| 레지스트리 접근 (사용자 레벨) | 파일 연결 및 시작 프로그램 설정 | HKEY_CURRENT_USER만 사용, 시스템 레지스트리 변경 없음 |
| 프로세스 모니터링 | 파일 편집 프로그램 종료 감지 | 자신이 실행한 프로세스만 모니터링 |

**관리자 권한이 필요하지 않습니다** - 모든 작업이 사용자 레벨에서 이루어집니다.

## 질문이 있으신가요?

- GitHub Issues에서 질문하기: https://github.com/trollgameskr/UnlockOpenFile/issues
- 소스 코드 보기: https://github.com/trollgameskr/UnlockOpenFile
