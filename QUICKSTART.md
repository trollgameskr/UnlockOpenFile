# UnlockOpenFile - Quick Start Guide

빠르게 시작하기 위한 5분 가이드

## 1. 다운로드 (1분)

### 옵션 A: 릴리스 다운로드 (가장 쉬움, 권장)

1. [GitHub Releases](https://github.com/trollgameskr/UnlockOpenFile/releases/latest)로 이동
2. **`UnlockOpenFile-vX.X.X-standalone.zip`** 다운로드 (⭐ 권장)
   - ✅ .NET Runtime 설치 불필요
   - ✅ 압축 해제 후 바로 실행
3. ZIP 압축 해제
4. `UnlockOpenFile.exe` 실행

### 옵션 B: 소스에서 빌드

#### 필요한 것
- .NET 8.0 SDK (개발용)
- Windows 10 이상

#### 빌드 방법 (Standalone, 권장)
```bash
# 저장소 클론
git clone https://github.com/trollgameskr/UnlockOpenFile.git
cd UnlockOpenFile

# Standalone 빌드 (권장)
dotnet publish -c Release -r win-x64 --self-contained true -o publish-standalone

# 실행 파일 위치: publish-standalone/UnlockOpenFile.exe
```

#### 경량 빌드 (고급 사용자용)
```bash
# .NET 8.0 Runtime 필요
dotnet publish -c Release -r win-x64 --self-contained false -o publish

# 실행 파일 위치: publish/UnlockOpenFile.exe
```

## 2. 첫 실행 (1분)

### 설정 창 열기
```bash
# Standalone 빌드를 사용하는 경우
publish-standalone/UnlockOpenFile.exe

# 또는 경량 빌드를 사용하는 경우
publish/UnlockOpenFile.exe

# 또는 다운로드한 경우
UnlockOpenFile.exe
```

이렇게 하면 설정 창이 열립니다.

### 파일 연결 설정
1. "Excel 파일 (.xlsx) 연결" 버튼 클릭
2. 또는 "CSV 파일 (.csv) 연결" 버튼 클릭
3. 로그에서 "파일이 연결되었습니다" 메시지 확인
4. (선택사항) "Windows 시작 시 자동 실행" 체크박스 선택

## 3. 파일 열기 테스트 (1분)

### 방법 1: 탐색기에서
1. Excel 파일을 탐색기에서 찾기
2. 파일 더블클릭
3. UnlockOpenFile이 자동으로 실행되며 상태 창 표시
4. Excel이 열리고 파일 편집 가능

### 방법 2: 명령줄에서
```bash
# Standalone 빌드를 사용하는 경우
publish-standalone/UnlockOpenFile.exe "C:\경로\파일.xlsx"

# 또는 경량 빌드를 사용하는 경우
publish/UnlockOpenFile.exe "C:\경로\파일.xlsx"
```

## 4. 편집 및 저장 (1분)

1. Excel에서 파일 내용 수정
2. Excel에서 저장 (Ctrl+S)
3. UnlockOpenFile 상태 창에서 "변경 사항을 원본에 저장 중..." 메시지 확인
4. 원본 파일이 자동으로 업데이트됨
5. 다른 프로그램에서도 원본 파일에 접근 가능!

## 5. 확인 (1분)

### 원본 파일이 잠기지 않았는지 확인
1. Excel로 파일을 UnlockOpenFile로 열기
2. 다른 프로그램(예: Python 스크립트)에서 같은 파일 읽기
3. 성공! 🎉

### Python 예제
```python
import pandas as pd

# Excel이 열려있어도 읽기 가능!
df = pd.read_excel('C:/경로/파일.xlsx')
print(df.head())
```

## 자주 하는 작업

### 파일 연결 해제
1. UnlockOpenFile.exe 실행 (인자 없이)
2. "연결 해제" 버튼 클릭

### 시작 프로그램 해제
1. UnlockOpenFile.exe 실행 (인자 없이)
2. "Windows 시작 시 자동 실행" 체크박스 해제

### 상태 확인
- 파일을 열면 상태 창이 자동으로 표시됨
- 시스템 트레이에 아이콘이 나타남
- 더블클릭하면 상태 창 다시 열기

## 문제 해결

### "파일을 찾을 수 없습니다"
- 파일 경로가 올바른지 확인
- 파일명에 특수문자가 없는지 확인

### ".NET 8.0이 필요합니다"
- **해결 방법 1 (권장)**: Standalone 빌드 사용 (.NET Runtime 불필요)
- 해결 방법 2: [.NET 8.0 Runtime 다운로드](https://dotnet.microsoft.com/download/dotnet/8.0)

### "변경사항이 저장되지 않습니다"
- 원본 파일이 읽기 전용인지 확인
- 상태 창의 로그 확인

## 다음 단계

더 자세한 정보는 다음 문서를 참조하세요:

- **README.md** - 전체 사용 가이드
- **USAGE_GUIDE.md** - 상세 사용법과 시나리오
- **examples/** - 자동화 스크립트 예제

## 요약

```
1. 다운로드: Standalone 빌드 (권장) 또는 소스에서 빌드
2. 실행: UnlockOpenFile.exe
3. 설정: 파일 연결 클릭
4. 사용: 파일 더블클릭
5. 완료: 자동 동기화!
```

**이제 파일 잠김 문제 없이 자유롭게 파일을 편집하세요! 🚀**
