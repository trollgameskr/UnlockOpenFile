# 원본 파일 연결 정보 저장 및 복원 기능

## 개요

UnlockOpenFile 프로그램이 파일 확장자를 자신과 연결할 때, 기존 OS에 등록된 원본 파일 연결 정보를 저장하고, 나중에 연결을 해제할 때 원본 연결을 복원할 수 있는 기능이 추가되었습니다.

## 문제점

이전 버전에서는:
- 파일 확장자(.xlsx, .csv 등)를 UnlockOpenFile과 연결하면 기존 연결 정보가 영구적으로 사라짐
- 연결 해제 시 원래 사용하던 프로그램으로 복원되지 않음
- 사용자가 수동으로 기본 프로그램을 다시 설정해야 했음

## 해결 방법

### 1. ApplicationSettings 클래스 확장

새로운 레지스트리 경로 추가:
```csharp
private const string OriginalAssociationsPath = @"Software\UnlockOpenFile\OriginalAssociations";
```

새로운 메서드 추가:
- `SaveOriginalAssociation(string extension, string progId)` - 원본 ProgId 저장
- `GetOriginalAssociation(string extension)` - 저장된 원본 ProgId 조회
- `RemoveOriginalAssociation(string extension)` - 저장된 원본 정보 삭제
- `GetAllOriginalAssociations()` - 모든 저장된 원본 연결 정보 조회

### 2. SettingsForm.RegisterFileAssociation() 수정

파일 연결 등록 전에 기존 ProgId를 저장:
```csharp
// Save the original association before overwriting
string? originalProgId = null;
using var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}");
if (extKey != null)
{
    originalProgId = extKey.GetValue("")?.ToString();
    if (!string.IsNullOrEmpty(originalProgId) && 
        !originalProgId.StartsWith("UnlockOpenFile"))
    {
        ApplicationSettings.SaveOriginalAssociation(extension, originalProgId);
    }
}
```

### 3. SettingsForm.OnUnregisterClick() 수정

연결 해제 시 원본 ProgId 복원:
```csharp
string? originalProgId = ApplicationSettings.GetOriginalAssociation(ext);
if (!string.IsNullOrEmpty(originalProgId))
{
    // Restore the original association
    using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}"))
    {
        extKey.SetValue("", originalProgId);
    }
    ApplicationSettings.RemoveOriginalAssociation(ext);
}
else
{
    // No original association saved, just delete the current one
    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", false);
}
```

### 4. SettingsForm.OnResetAllClick() 수정

모든 설정 초기화 시에도 원본 연결 복원:
- `.xlsx`, `.csv` 등록된 모든 확장자에 대해 원본 ProgId가 있으면 복원
- 원본 정보가 없으면 연결만 삭제

## 사용 시나리오

### 시나리오 1: Excel 파일 연결 및 해제
1. 사용자가 원래 Excel을 `.xlsx` 기본 프로그램으로 사용 중
2. UnlockOpenFile에서 "Excel 파일 (.xlsx) 연결" 클릭
   - 원본 ProgId(예: "Excel.Sheet.12") 저장
   - UnlockOpenFile로 연결 등록
3. 나중에 "연결 해제" 클릭
   - 저장된 원본 ProgId "Excel.Sheet.12" 복원
   - `.xlsx` 파일이 다시 Excel과 연결됨

### 시나리오 2: 모든 설정 초기화
1. 사용자가 여러 파일 형식을 UnlockOpenFile과 연결
2. "모든 설정 초기화" 클릭
   - 저장된 모든 원본 연결 정보 복원
   - 파일들이 원래 사용하던 프로그램과 다시 연결됨

## 저장 위치

원본 파일 연결 정보는 다음 레지스트리 경로에 저장됩니다:
```
HKEY_CURRENT_USER\Software\UnlockOpenFile\OriginalAssociations
```

각 값:
- 이름: 파일 확장자 (예: `.xlsx`, `.csv`)
- 데이터: 원본 ProgId (예: `Excel.Sheet.12`, `csvfile`)

## 주의사항

1. **UnlockOpenFile ProgId는 저장 안 됨**: 이미 UnlockOpenFile이 연결된 상태에서 다시 연결하는 경우, 원본 정보를 저장하지 않습니다.

2. **레지스트리 권한**: HKEY_CURRENT_USER 사용하므로 관리자 권한 불필요

3. **자동 정리**: 
   - 연결 해제 시 복원 후 저장된 원본 정보 자동 삭제
   - "모든 설정 초기화" 시 모든 원본 정보 삭제

4. **호환성**: 기존 사용자의 경우 첫 연결 시에는 원본 정보가 없으므로 연결 해제 시 그냥 삭제됩니다. 다음번 연결부터는 원본 정보가 저장됩니다.

## 이점

- ✅ 사용자 경험 개선: 원클릭으로 원상복구 가능
- ✅ 데이터 보존: 기존 설정 정보가 유실되지 않음
- ✅ 안전한 테스트: 언제든지 원래대로 돌릴 수 있어 부담 없이 시도 가능
- ✅ 자동화: 수동으로 설정할 필요 없음
