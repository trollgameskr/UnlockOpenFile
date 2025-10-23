# UnlockOpenFile

파일 열때 에디터가 점유해서 다른 프로그램에서 접근할 수 없던 불편함을 해결하기 위한 Windows 프로그램

> 💡 **빠른 시작:** 5분 안에 시작하려면 [QUICKSTART.md](QUICKSTART.md)를 참조하세요!

## 📥 다운로드

**⭐ 권장: [Standalone 빌드](https://github.com/trollgameskr/UnlockOpenFile/releases/latest)를 사용하세요**

- ✅ .NET Runtime 설치 불필요
- ✅ 단일 실행 파일로 간편하게 사용

**고급 사용자용:** 경량 빌드도 제공됩니다. .NET 8.0 Runtime이 이미 설치된 환경에서 사용하세요.

## 📚 문서 가이드

- **[QUICKSTART.md](QUICKSTART.md)** - 5분 빠른 시작 가이드
- **[USAGE_GUIDE.md](USAGE_GUIDE.md)** - 상세 사용법과 시나리오
- **[FRAMEWORK_DEPENDENT_FIX.md](FRAMEWORK_DEPENDENT_FIX.md)** - Framework-dependent 빌드 실행 문제 해결
- **[CODE_SIGNING_GUIDE.md](CODE_SIGNING_GUIDE.md)** - 코드 서명 가이드
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - 기술 아키텍처 문서
- **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** - 구현 개요
- **[CHANGELOG.md](CHANGELOG.md)** - 버전 변경 이력
- **[TAG_UPDATE_GUIDE.md](TAG_UPDATE_GUIDE.md)** - 태그 업데이트 가이드 (빌드 워크플로우 관련)
- **[examples/](examples/)** - 사용 예제 스크립트

## 기능

- **파일 복사 및 열기**: Excel, CSV 등의 파일을 열 때 자동으로 임시 복사본을 생성하여 원본 파일을 잠그지 않습니다
- **자동 동기화**: 복사본이 수정되면 자동으로 원본 파일에 변경 사항을 저장하고 시스템 알림으로 저장 완료를 알려줍니다
- **단일 인스턴스 관리**: 여러 파일을 열어도 하나의 관리 프로그램 창에서 모든 파일을 관리합니다
- **자동 종료**: 모든 파일이 닫히면 관리 프로그램도 자동으로 종료됩니다
- **파일 연결**: Excel(.xlsx), CSV(.csv) 파일을 이 프로그램과 연결하여 기본 프로그램으로 설정할 수 있습니다
- **사용자 지정 응용 프로그램**: 파일 확장자별로 사용할 응용 프로그램을 직접 지정할 수 있습니다
- **스마트 응용 프로그램 감지**: Excel이 설치되어 있으면 Excel로, 없으면 LibreOffice Calc로, 둘 다 없으면 Notepad로 CSV 파일을 자동으로 엽니다
- **시작 프로그램 등록**: Windows 시작 시 자동으로 실행되도록 설정할 수 있습니다
- **시스템 트레이 아이콘**: 백그라운드에서 실행되며 시스템 트레이에서 관리할 수 있습니다

## 요구 사항

**Standalone 빌드 (권장):**
- Windows 10 이상

**경량 빌드:**
- Windows 10 이상
- .NET 8.0 Runtime

## 빌드 방법

개발 빌드:
```bash
dotnet build
```

릴리스 빌드:
```bash
dotnet build -c Release
```

배포용 패키지 생성:

**Standalone 빌드 (권장):**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-standalone
```

**경량 빌드:**
```bash
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

빌드된 실행 파일은 각각 `publish-standalone/UnlockOpenFile.exe` 또는 `publish/UnlockOpenFile.exe`에 생성됩니다.

자체 포함형 빌드 (.NET Runtime 포함):
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-standalone
```

## 사용 방법

### 1. 파일 직접 열기

```bash
UnlockOpenFile.exe "파일경로.xlsx"
```

파일을 인자로 전달하면 자동으로 복사본을 생성하고 기본 프로그램으로 엽니다.

### 2. 설정 창 열기

인자 없이 실행하면 설정 창이 열립니다:

```bash
UnlockOpenFile.exe
```

설정 창에서 다음을 수행할 수 있습니다:
- Windows 시작 프로그램 등록/해제
- 파일 형식 연결 (Excel, CSV)
- 파일 연결 해제

### 3. 파일 연결 설정

설정 창에서 "Excel 파일 (.xlsx) 연결" 또는 "CSV 파일 (.csv) 연결" 버튼을 클릭하면:
- 해당 형식의 파일을 더블클릭할 때 이 프로그램을 통해 열립니다
- 원본 파일을 잠그지 않으면서 편집할 수 있습니다

### 4. 시작 프로그램 등록

설정 창에서 "Windows 시작 시 자동 실행" 체크박스를 선택하면:
- Windows 시작 시 자동으로 프로그램이 백그라운드에서 실행됩니다
- 파일을 열 때 바로 사용할 수 있습니다

### 5. 사용자 지정 응용 프로그램 설정

설정 창에서 파일 확장자별로 사용할 응용 프로그램을 직접 지정할 수 있습니다:

**설정 방법:**
1. 설정 창을 엽니다 (인자 없이 실행)
2. "사용자 지정 응용 프로그램" 섹션에서 "추가/수정" 버튼 클릭
3. 파일 확장자 입력 (예: `.txt`, `.docx`)
4. 사용할 응용 프로그램 실행 파일(.exe) 선택
5. 설정 완료

이제 UnlockOpenFile로 해당 확장자의 파일을 열면 지정한 응용 프로그램으로 열립니다.

## 작동 원리

1. 사용자가 파일을 열면 프로그램이 임시 폴더에 복사본을 생성합니다
2. 기본 프로그램(Excel, LibreOffice Calc 등)으로 복사본을 엽니다
3. FileSystemWatcher를 통해 복사본의 변경 사항을 실시간으로 감지합니다
4. 복사본이 수정되면 자동으로 원본 파일에 변경 사항을 저장합니다
5. 열린 파일의 편집 프로그램이 종료되면 해당 파일이 자동으로 닫힙니다
6. 모든 파일이 닫히면 관리 프로그램도 자동으로 종료됩니다
7. 여러 파일을 열어도 하나의 관리 창에서 모든 파일을 관리합니다

### 응용 프로그램 선택 우선순위

CSV 및 Excel 파일을 열 때 다음 순서로 응용 프로그램을 선택합니다:

**CSV 파일 (.csv):**
1. Microsoft Excel (설치되어 있는 경우)
2. LibreOffice Calc (설치되어 있는 경우)
3. Notepad (기본 대체 프로그램)

**Excel 파일 (.xlsx, .xls):**
1. Microsoft Excel (설치되어 있는 경우)
2. LibreOffice Calc (설치되어 있는 경우)

사용자 지정 응용 프로그램을 설정한 경우 해당 설정이 최우선으로 적용됩니다.

## 주의 사항

- 관리자 권한이 필요하지 않습니다 (사용자 레벨 레지스트리 사용)
- 임시 파일은 시스템 임시 폴더에 생성됩니다
- 원본 파일이 잠기지 않으므로 다른 프로그램에서도 동시에 접근할 수 있습니다
- 파일 변경 감지는 약간의 지연(500ms)이 있을 수 있습니다

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포되는 무료 오픈소스 소프트웨어입니다. 상업적 사용을 포함한 모든 용도로 자유롭게 사용할 수 있습니다. 유료 프로그램이나 유료 라이브러리를 사용하지 않습니다.

자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

## 문제 해결

### 파일이 열리지 않는 경우
- .NET 8.0 Runtime이 설치되어 있는지 확인하세요 (framework-dependent 빌드 사용 시)
- Standalone 빌드를 사용하면 .NET Runtime 설치 없이 실행 가능합니다
- 파일 경로에 특수 문자가 있는지 확인하세요

### Framework-dependent 빌드가 실행되지 않는 경우
- [FRAMEWORK_DEPENDENT_FIX.md](FRAMEWORK_DEPENDENT_FIX.md)를 참조하세요
- 가장 쉬운 해결 방법: Standalone 빌드를 사용하세요

### 파일 연결이 작동하지 않는 경우
- 탐색기를 새로 고치거나 로그아웃 후 다시 로그인하세요
- Windows 설정 > 앱 > 기본 앱에서 파일 형식을 확인하세요

### 변경 사항이 저장되지 않는 경우
- 프로그램 상태 창에서 로그를 확인하세요
- 원본 파일이 읽기 전용인지 확인하세요
