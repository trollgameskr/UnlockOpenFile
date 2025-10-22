# Windows Defender 오탐 해결 방법

이 파일은 Windows Defender가 UnlockOpenFile을 바이러스로 탐지했을 때의 해결 방법을 단계별로 안내합니다.

## 빠른 해결 방법

### 1단계: 프로그램 폴더를 제외 목록에 추가

1. **Windows 보안 열기**
   - 시작 메뉘를 열고 "Windows 보안" 또는 "Windows Security" 검색
   - 또는 설정 > 업데이트 및 보안 > Windows 보안 선택

2. **바이러스 및 위협 방지 설정 열기**
   - "바이러스 및 위협 방지" 클릭
   - "바이러스 및 위협 방지 설정 관리" 클릭

3. **제외 항목 추가**
   - 아래로 스크롤하여 "제외 항목" 섹션 찾기
   - "제외 항목 추가 또는 제거" 클릭
   - "제외 추가" 버튼 클릭
   - "폴더" 선택
   - UnlockOpenFile.exe가 있는 폴더 선택
   - "폴더 선택" 클릭

4. **완료**
   - 이제 UnlockOpenFile을 안전하게 실행할 수 있습니다

### 2단계: 실행 파일 복구 (삭제된 경우)

Windows Defender가 파일을 격리했다면:

1. **Windows 보안** 열기
2. **바이러스 및 위협 방지** > **보호 기록** 클릭
3. UnlockOpenFile 관련 항목 찾기
4. **작업** > **복원** 또는 **허용** 선택

## 영구적인 해결 방법

### Microsoft에 오탐 신고하기

Microsoft에 오탐을 신고하면 향후 업데이트에서 문제가 해결될 수 있습니다:

1. [Microsoft Security Intelligence 제출 포털](https://www.microsoft.com/en-us/wdsi/filesubmission) 방문

2. **Submit a file for malware analysis** 선택

3. 다음 정보 입력:
   - **Email**: 본인의 이메일 주소
   - **Product name**: UnlockOpenFile
   - **Product version**: (다운로드한 버전, 예: 0.9.8)
   - **Publisher**: trollgameskr
   - **File**: UnlockOpenFile.exe 파일 업로드

4. **Detection category** 섹션에서:
   - **This file is clean (false positive)** 선택
   - 설명란에 다음과 같이 작성:
     ```
     This is a legitimate open-source file management utility.
     Source code: https://github.com/trollgameskr/UnlockOpenFile
     License: MIT
     This application helps users edit files without locking them by creating temporary copies.
     It uses legitimate Windows APIs for file monitoring and process management.
     ```

5. **Submit** 클릭

### 소스 코드에서 직접 빌드하기

가장 안전한 방법은 직접 소스 코드에서 빌드하는 것입니다:

**요구 사항:**
- .NET 8.0 SDK
- Git

**빌드 단계:**

```bash
# 1. 저장소 복제
git clone https://github.com/trollgameskr/UnlockOpenFile.git
cd UnlockOpenFile

# 2. 릴리스 빌드
dotnet build -c Release

# 3. 실행 파일 위치
# bin/Release/net8.0-windows/UnlockOpenFile.exe
```

직접 빌드한 실행 파일도 Windows Defender에 의해 탐지될 수 있지만, 자신이 빌드했기 때문에 안전하다는 것을 확신할 수 있습니다.

## 왜 오탐이 발생하나요?

UnlockOpenFile은 다음과 같은 정상적인 기능을 사용합니다:

| 기능 | 목적 | 왜 의심받을 수 있는가 |
|------|------|---------------------|
| **FileSystemWatcher** | 파일 변경 감지 | 일부 악성코드도 파일 변경을 감시함 |
| **Process.Start** | 편집 프로그램 실행 | 다른 프로그램을 실행하는 동작 |
| **Process 모니터링** | 프로그램 종료 감지 | 프로세스를 추적하는 동작 |
| **Registry 접근** | 파일 연결 설정 | 레지스트리를 수정하는 동작 |
| **Named Pipes** | IPC 통신 | 프로세스 간 통신 |
| **파일 복사/삭제** | 임시 파일 관리 | 파일 시스템 조작 |

이러한 기능들은 모두 프로그램의 정상적인 작동을 위해 필요하며, 악의적인 목적이 없습니다.

## 안전성 확인 방법

### 1. VirusTotal 검사

[VirusTotal](https://www.virustotal.com)에 파일을 업로드하여 여러 보안 엔진의 검사 결과를 확인할 수 있습니다:

1. https://www.virustotal.com 방문
2. UnlockOpenFile.exe 파일 드래그 앤 드롭
3. 검사 결과 확인

대부분의 보안 엔진(50개 이상 중)에서 안전한 것으로 판정됩니다.

### 2. SHA256 해시 확인

다운로드한 파일이 공식 릴리스와 동일한지 확인:

**PowerShell에서:**
```powershell
Get-FileHash -Algorithm SHA256 "UnlockOpenFile.exe"
```

**명령 프롬프트에서:**
```cmd
certutil -hashfile "UnlockOpenFile.exe" SHA256
```

결과 해시를 GitHub 릴리스 페이지의 checksums.txt와 비교하세요.

### 3. 소스 코드 검토

모든 소스 코드는 GitHub에서 공개되어 있습니다:
- 저장소: https://github.com/trollgameskr/UnlockOpenFile
- 모든 파일을 검토하여 악의적인 코드가 없음을 확인할 수 있습니다

## 추가 정보

### 네트워크 활동 확인

UnlockOpenFile은 **인터넷 연결을 전혀 사용하지 않습니다**.

확인 방법:
1. Windows 방화벽 로그 확인
2. 네트워크 모니터링 도구 사용 (예: Wireshark)
3. 소스 코드에서 네트워크 관련 코드가 없음을 확인

### 데이터 수집 확인

UnlockOpenFile은:
- 사용자 데이터를 수집하지 않습니다
- 분석 도구를 사용하지 않습니다
- 원격 서버와 통신하지 않습니다
- 모든 작업은 로컬 컴퓨터 내에서만 이루어집니다

## 문제가 계속되나요?

여전히 문제가 있다면:

1. **GitHub Issue 생성**: https://github.com/trollgameskr/UnlockOpenFile/issues
2. 다음 정보를 포함해주세요:
   - Windows 버전
   - Windows Defender 버전
   - 오류 메시지 스크린샷
   - 감지된 위협 이름

우리는 이 문제를 해결하기 위해 최선을 다하고 있으며, 커뮤니티의 도움이 큰 힘이 됩니다.

## 라이선스 및 책임

UnlockOpenFile은 MIT 라이선스 하에 배포되는 무료 오픈소스 소프트웨어입니다.

자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.
