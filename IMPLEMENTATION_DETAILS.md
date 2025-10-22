# Custom Application Selector - Implementation Summary

## 문제 해결 (Problem Solved)

### 원래 문제 (Original Issue)
1. 임시로 카피된 파일이 노트패드로 열림 (Temp files opened with Notepad)
2. 기존에 연결된 프로그램으로 열리지 않음 (Not using originally associated program)
3. UI에서 연결 프로그램을 수정할 수 없음 (No UI to modify associated program)

### 해결 방법 (Solution)
1. ✅ 우선순위 기반 응용 프로그램 선택 시스템 구현
2. ✅ 사용자 지정 응용 프로그램 설정 UI 추가
3. ✅ 레지스트리 기반 설정 저장 (관리자 권한 불필요)

## 구현 상세 (Implementation Details)

### 1. 새 파일 (New Files)

#### ApplicationSettings.cs
```csharp
public static class ApplicationSettings
{
    // 확장자별 응용 프로그램 경로 설정
    public static void SetApplicationPath(string extension, string applicationPath)
    
    // 확장자에 대한 사용자 지정 응용 프로그램 가져오기
    public static string? GetApplicationPath(string extension)
    
    // 사용자 지정 응용 프로그램 제거
    public static void RemoveApplicationPath(string extension)
    
    // 모든 사용자 지정 응용 프로그램 목록 가져오기
    public static Dictionary<string, string> GetAllApplicationPaths()
}
```

**기능**: 레지스트리에 사용자 지정 응용 프로그램 경로 저장/조회

**저장 위치**: `HKEY_CURRENT_USER\Software\UnlockOpenFile\Applications`

### 2. 수정된 파일 (Modified Files)

#### FileManager.cs
**변경 사항**: `GetActualDefaultApplication()` 메서드 수정

**이전 우선순위**:
1. HKEY_CURRENT_USER 레지스트리
2. HKEY_LOCAL_MACHINE 레지스트리
3. 일반적인 응용 프로그램 경로
4. Shell Execute 폴백

**새로운 우선순위**:
1. **사용자 지정 응용 프로그램 (최우선)** ⭐ NEW
2. HKEY_CURRENT_USER 레지스트리
3. HKEY_LOCAL_MACHINE 레지스트리
4. 일반적인 응용 프로그램 경로
5. Shell Execute 폴백

#### SettingsForm.cs
**추가된 UI 컴포넌트**:
- `_customApplicationGroup`: 사용자 지정 응용 프로그램 그룹박스
- `_applicationListView`: 설정된 응용 프로그램 목록 표시
- `_addApplicationButton`: 응용 프로그램 추가/수정 버튼
- `_removeApplicationButton`: 응용 프로그램 제거 버튼

**새 메서드**:
- `LoadCustomApplications()`: ListView 새로고침
- `OnAddApplicationClick()`: 응용 프로그램 추가/수정 다이얼로그
- `OnRemoveApplicationClick()`: 응용 프로그램 제거 확인 다이얼로그

**변경 사항**:
- Form 높이: 500 → 700 (200px 증가)
- TableLayout 행: 4 → 5 (1행 추가)

### 3. 문서 업데이트 (Documentation Updates)

#### README.md
- 기능 목록에 "사용자 지정 응용 프로그램" 추가
- 사용 방법 섹션 추가

#### USAGE_GUIDE.md
- "사용자 지정 응용 프로그램 설정" 섹션 추가
- 설정 방법 상세 설명
- 사용 예제 추가

#### CHANGELOG.md
- [Unreleased] 섹션에 새 기능 추가
- 변경 사항 및 수정 사항 문서화

#### UI_CHANGES.md (신규)
- UI 레이아웃 다이어그램
- 사용자 플로우 설명
- 기술 구현 상세

## 사용 방법 (How to Use)

### 사용자 지정 응용 프로그램 추가

1. **설정 창 열기**
   ```
   UnlockOpenFile.exe
   ```

2. **확장자 입력**
   - "사용자 지정 응용 프로그램" 섹션에서 "추가/수정" 클릭
   - 확장자 입력 (예: `.txt`, `.pdf`, `.docx`)
   - "확인" 클릭

3. **응용 프로그램 선택**
   - 파일 선택 다이얼로그에서 실행 파일(.exe) 선택
   - "열기" 클릭

4. **완료**
   - 설정이 레지스트리에 저장됨
   - ListView에 새 항목 표시됨
   - 로그에 확인 메시지 표시됨

### 파일 열기 (자동으로 사용자 지정 응용 프로그램 사용)

```bash
UnlockOpenFile.exe "C:\path\to\file.txt"
```

- FileManager가 `.txt` 확장자 확인
- ApplicationSettings에서 사용자 지정 응용 프로그램 확인
- 설정된 응용 프로그램으로 임시 파일 열기
- 로그에 "사용자 지정 응용 프로그램 사용: [앱 이름]" 표시

### 사용자 지정 응용 프로그램 제거

1. ListView에서 제거할 항목 선택
2. "제거" 버튼 클릭
3. 확인 다이얼로그에서 "예" 클릭
4. 레지스트리에서 설정 제거됨

## 사용 예제 (Usage Examples)

### 예제 1: Notepad++ 사용
```
확장자: .txt
응용 프로그램: C:\Program Files\Notepad++\notepad++.exe

결과: 모든 .txt 파일이 Notepad++로 열림
```

### 예제 2: LibreOffice Calc 사용
```
확장자: .xlsx
응용 프로그램: C:\Program Files\LibreOffice\program\scalc.exe

결과: 모든 .xlsx 파일이 Excel 대신 LibreOffice Calc로 열림
```

### 예제 3: Chrome으로 PDF 열기
```
확장자: .pdf
응용 프로그램: C:\Program Files\Google\Chrome\Application\chrome.exe

결과: 모든 .pdf 파일이 Chrome으로 열림
```

## 기술 세부사항 (Technical Details)

### 레지스트리 구조
```
HKEY_CURRENT_USER\Software\UnlockOpenFile\Applications
├── .txt = "C:\Program Files\Notepad++\notepad++.exe"
├── .pdf = "C:\Program Files\Adobe\Reader\AcroRd32.exe"
└── .xlsx = "C:\Program Files\LibreOffice\program\scalc.exe"
```

### 코드 흐름
```
파일 열기 요청
    ↓
FileManager.OpenFileAsync()
    ↓
GetActualDefaultApplication(extension)
    ↓
ApplicationSettings.GetApplicationPath(extension)
    ↓
결과 있음? → 사용자 지정 앱 사용
    ↓
결과 없음? → Windows 레지스트리 확인
    ↓
결과 없음? → 일반 앱 경로 확인
    ↓
결과 없음? → Shell Execute 사용
```

### 에러 처리
- 파일 존재 여부 확인
- 레지스트리 접근 오류 처리
- 유효하지 않은 확장자 처리
- 사용자 친화적 오류 메시지
- 모든 작업 로깅

## 이점 (Benefits)

1. **사용자 제어**: 정확히 어떤 응용 프로그램을 사용할지 지정 가능
2. **시스템 기본값 재정의**: 사용자 지정 설정이 Windows 기본값보다 우선
3. **관리자 권한 불필요**: 사용자 수준 레지스트리 사용
4. **확장자별 설정**: 파일 형식마다 다른 응용 프로그램 지정 가능
5. **쉬운 관리**: 간단한 UI로 연결 추가/제거
6. **영구 저장**: 설정이 프로그램 재시작 후에도 유지
7. **투명성**: 어떤 응용 프로그램이 사용되는지 로그에 명확히 표시

## 테스트 계획 (Testing Plan)

### 단위 테스트 (Unit Tests)
- [ ] ApplicationSettings.SetApplicationPath() 테스트
- [ ] ApplicationSettings.GetApplicationPath() 테스트
- [ ] ApplicationSettings.RemoveApplicationPath() 테스트
- [ ] ApplicationSettings.GetAllApplicationPaths() 테스트

### 통합 테스트 (Integration Tests)
- [ ] FileManager와 ApplicationSettings 통합 테스트
- [ ] SettingsForm UI 상호작용 테스트
- [ ] 레지스트리 읽기/쓰기 테스트

### 수동 테스트 (Manual Tests)
- [ ] 설정 창 열기 및 UI 확인
- [ ] 사용자 지정 응용 프로그램 추가
- [ ] ListView에 항목 표시 확인
- [ ] 사용자 지정 응용 프로그램으로 파일 열기
- [ ] 사용자 지정 응용 프로그램 제거
- [ ] 잘못된 확장자 입력 처리
- [ ] 존재하지 않는 실행 파일 선택 처리

## 제한 사항 (Limitations)

1. Windows 전용 기능 (레지스트리 사용)
2. .NET 8.0 Windows Forms 필요
3. 수동 테스트는 Windows 환경 필요
4. 응용 프로그램 실행 파일(.exe)만 지원

## 향후 개선 사항 (Future Enhancements)

- [ ] 설정 내보내기/가져오기 기능
- [ ] 여러 확장자에 대한 일괄 설정
- [ ] 응용 프로그램 아이콘 표시
- [ ] 최근 사용한 응용 프로그램 목록
- [ ] 응용 프로그램 경로 자동 감지
- [ ] 설정 백업/복원 기능

## 참고 자료 (References)

- [README.md](README.md) - 프로젝트 개요
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - 상세 사용 가이드
- [UI_CHANGES.md](UI_CHANGES.md) - UI 변경 다이어그램
- [CHANGELOG.md](CHANGELOG.md) - 변경 이력
- [ARCHITECTURE.md](ARCHITECTURE.md) - 아키텍처 문서
