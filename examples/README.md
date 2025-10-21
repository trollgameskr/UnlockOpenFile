# UnlockOpenFile Examples

이 폴더에는 UnlockOpenFile을 다양한 방법으로 사용하는 예제가 포함되어 있습니다.

## 파일 목록

### open_file.bat
Windows 배치 파일 예제입니다. 다음과 같은 사용 방법을 보여줍니다:
- 특정 파일 열기
- 인자로 전달된 파일 열기
- 여러 파일 순차적으로 열기 (주석 처리됨)

**사용법:**
```batch
REM 스크립트 직접 실행
open_file.bat

REM 파일을 인자로 전달
open_file.bat "C:\Data\myfile.xlsx"
```

### open_file.ps1
PowerShell 스크립트 예제입니다. 다음과 같은 사용 방법을 보여줍니다:
- 특정 파일 열기
- 매개변수로 파일 경로 받기
- 디렉토리의 모든 Excel 파일 열기 (주석 처리됨)
- 파일 닫힐 때까지 대기 (주석 처리됨)

**사용법:**
```powershell
# 스크립트 직접 실행
.\open_file.ps1

# 파일을 매개변수로 전달
.\open_file.ps1 "C:\Data\myfile.xlsx"
```

## 시작하기 전에

두 스크립트 모두 다음 변수를 수정해야 합니다:
- **Batch**: `UNLOCKER_PATH` 변수를 UnlockOpenFile.exe의 실제 경로로 변경
- **PowerShell**: `$UnlockerPath` 변수를 UnlockOpenFile.exe의 실제 경로로 변경

## 고급 사용 예제

### Python에서 사용
```python
import subprocess
import os

unlocker_path = r"C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe"
file_path = r"C:\Data\example.xlsx"

# 파일 열기
subprocess.Popen([unlocker_path, file_path])
```

### C#에서 사용
```csharp
using System.Diagnostics;

string unlockerPath = @"C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe";
string filePath = @"C:\Data\example.xlsx";

Process.Start(unlockerPath, $"\"{filePath}\"");
```

### 작업 스케줄러와 함께 사용
1. Windows 작업 스케줄러 열기
2. "기본 작업 만들기" 선택
3. 트리거 설정 (예: 매일 오전 9시)
4. 작업: "프로그램 시작"
5. 프로그램/스크립트: UnlockOpenFile.exe 경로
6. 인수 추가: 파일 경로

### 바로 가기 만들기
1. 바탕화면에서 우클릭 > 새로 만들기 > 바로 가기
2. 항목 위치: `"C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe" "C:\Data\report.xlsx"`
3. 이름 지정: "보고서 열기"
4. 바로 가기 아이콘 더블클릭으로 파일 열기

## 자동화 시나리오

### 시나리오 1: 매일 아침 보고서 자동 열기
```batch
@echo off
REM scheduled_open.bat
"C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe" "C:\Reports\daily_report.xlsx"
```
작업 스케줄러로 이 배치 파일을 매일 아침 실행하도록 설정합니다.

### 시나리오 2: 여러 데이터 파일 동시 열기
```powershell
# open_multiple.ps1
$files = @(
    "C:\Data\sales.xlsx",
    "C:\Data\inventory.csv",
    "C:\Data\customers.xlsx"
)

$unlocker = "C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe"

foreach ($file in $files) {
    Start-Process $unlocker -ArgumentList $file
    Start-Sleep -Seconds 2  # 파일 간 2초 대기
}
```

### 시나리오 3: 조건부 파일 열기
```powershell
# conditional_open.ps1
$unlocker = "C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe"
$file = "C:\Data\report.xlsx"

# 파일이 오늘 수정되었는지 확인
if ((Get-Item $file).LastWriteTime.Date -eq (Get-Date).Date) {
    Write-Host "오늘 수정된 파일입니다. 열기..."
    Start-Process $unlocker -ArgumentList $file
} else {
    Write-Host "오래된 파일입니다. 열기 생략."
}
```

## 문제 해결

### 실행 정책 오류 (PowerShell)
PowerShell 스크립트 실행 시 오류가 발생하면:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 경로에 공백이 있는 경우
경로를 항상 따옴표로 감싸세요:
```batch
"%UNLOCKER_PATH%" "%FILE_PATH%"
```

### 관리자 권한 필요
대부분의 경우 관리자 권한이 필요하지 않지만, 시스템 폴더의 파일을 여는 경우:
- 배치 파일: 우클릭 > 관리자 권한으로 실행
- PowerShell: `Start-Process -Verb RunAs`

## 추가 리소스

- [PowerShell 문서](https://docs.microsoft.com/powershell/)
- [Windows 작업 스케줄러 가이드](https://docs.microsoft.com/windows-server/administration/windows-commands/schtasks)
- [배치 스크립트 튜토리얼](https://www.tutorialspoint.com/batch_script/)
