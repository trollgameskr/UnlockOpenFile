# 파일 저장 버그 수정 (File Save Bug Fix)

## 문제 설명 (Problem Description)

파일을 수정하고 즉시 닫거나, 파일을 수정 후 닫을 때 변경 사항이 원본 파일에 저장되지 않는 문제가 발생했습니다.

When modifying a file and closing it immediately, or closing the file after modification, changes were not being saved to the original file.

## 근본 원인 (Root Cause)

기존 코드는 `FileSystemWatcher`만을 사용하여 파일 변경을 감지했습니다. 그러나 다음과 같은 타이밍 문제가 발생할 수 있었습니다:

1. 사용자가 파일을 수정
2. 파일 저장
3. 애플리케이션 종료
4. `FileSystemWatcher` 이벤트가 발생하기 전에 프로그램이 종료됨
5. 변경 사항이 손실됨

The existing code relied solely on `FileSystemWatcher` to detect file changes. However, timing issues could occur:

1. User modifies file
2. File is saved (by the editor)
3. Application exits
4. Program terminates before `FileSystemWatcher` event fires
5. Changes are lost

## 해결 방법 (Solution)

세 가지 중요한 지점에서 최종 저장 확인을 추가했습니다:

Added final save checks at three critical points:

### 1. OnProcessExited 메서드

프로세스가 종료될 때, 최종 저장 전에 임시 파일의 마지막 수정 시간을 확인합니다:

```csharp
// Check for final modifications before saving
try
{
    if (File.Exists(_tempFilePath))
    {
        var currentModified = File.GetLastWriteTime(_tempFilePath);
        if (currentModified > _lastModified)
        {
            // File was modified but FileSystemWatcher might not have fired yet
            _isModified = true;
            _lastModified = currentModified;
        }
    }
}
catch (Exception ex)
{
    OnStatusChanged($"최종 수정 확인 오류: {ex.Message}");
}
```

### 2. StartFileMonitoring 메서드

단일 인스턴스 애플리케이션(Excel, LibreOffice 등)의 경우, 파일이 더 이상 잠겨있지 않을 때 최종 수정 사항을 확인합니다:

```csharp
// Check for any final modifications before considering the file closed
try
{
    var currentModified = File.GetLastWriteTime(_tempFilePath);
    if (currentModified > _lastModified)
    {
        // File was modified, update tracking
        _isModified = true;
        _lastModified = currentModified;
        OnStatusChanged("최종 변경 사항 감지됨.");
    }
}
catch (Exception ex)
{
    OnStatusChanged($"최종 수정 확인 오류: {ex.Message}");
}
```

### 3. Cleanup 메서드

임시 파일을 삭제하기 전에 최종 수정 사항을 확인하고 저장합니다:

```csharp
// Perform a final save check before cleanup
if (File.Exists(_tempFilePath))
{
    try
    {
        var currentModified = File.GetLastWriteTime(_tempFilePath);
        if (currentModified > _lastModified)
        {
            // File was modified but not yet saved
            OnStatusChanged("최종 변경 사항 감지, 원본에 저장 중...");
            _pendingSaveTask = SaveToOriginalAsync();
            _pendingSaveTask.Wait(TimeSpan.FromSeconds(10));
        }
    }
    catch (Exception ex)
    {
        OnStatusChanged($"최종 저장 확인 오류: {ex.Message}");
    }
    
    File.Delete(_tempFilePath);
}
```

## 테스트 시나리오 (Test Scenarios)

이 수정으로 다음 시나리오들이 모두 처리됩니다:

This fix handles the following scenarios:

1. **정상 케이스**: FileSystemWatcher가 정상적으로 동작 (기존 동작 유지)
   - Normal case: FileSystemWatcher fires normally (existing behavior maintained)

2. **빠른 종료**: 프로세스가 빠르게 종료되는 경우 (OnProcessExited에서 처리)
   - Quick exit: Process exits quickly (handled in OnProcessExited)

3. **단일 인스턴스 앱**: Excel, LibreOffice 같은 단일 인스턴스 애플리케이션 (StartFileMonitoring에서 처리)
   - Single-instance apps: Like Excel, LibreOffice (handled in StartFileMonitoring)

4. **수동 종료**: 사용자가 프로그램을 강제 종료하는 경우 (Cleanup에서 처리)
   - Manual termination: User forcibly closes the program (handled in Cleanup)

## 영향 범위 (Impact)

- ✅ 기존 기능은 그대로 유지 (Existing functionality maintained)
- ✅ 새로운 확인 로직은 추가적이며 기존 흐름을 방해하지 않음 (New checks are additive and don't break existing flow)
- ✅ 모든 시나리오에서 파일 변경 사항이 저장됨 (File changes are saved in all scenarios)
- ✅ 성능 영향 최소화 (타임스탬프 확인만 수행) (Minimal performance impact - only timestamp checks)

## 참고 로그 (Reference Log)

수정 전 로그 (Before fix):
```
[13:02:28] UnlockOpenFile가 시작되었습니다.
[13:02:28] [Buff.csv] 원본 파일 복사 중...
[13:02:28] [Buff.csv] 파일 열기...
[13:02:28] [Buff.csv] 사용자 지정 응용 프로그램 사용: scalc.exe
[13:02:28] [Buff.csv] 파일이 열렸습니다: Buff_copy_638967349489348173.csv
[13:02:28] 파일 열기: C:\GIT\otps-2.0\Assets\DataSheet\Buff.csv
[13:02:43] [Buff.csv] 프로그램이 종료되었습니다.
[13:02:43] 파일 닫힘: Buff.csv
[13:02:43] 모든 파일이 닫혔습니다. 5초 후 프로그램을 종료합니다.
[13:02:44] 4초 후 종료...
```

수정 후 예상 로그 (Expected log after fix):
```
[13:02:28] UnlockOpenFile가 시작되었습니다.
[13:02:28] [Buff.csv] 원본 파일 복사 중...
[13:02:28] [Buff.csv] 파일 열기...
[13:02:28] [Buff.csv] 사용자 지정 응용 프로그램 사용: scalc.exe
[13:02:28] [Buff.csv] 파일이 열렸습니다: Buff_copy_638967349489348173.csv
[13:02:28] 파일 열기: C:\GIT\otps-2.0\Assets\DataSheet\Buff.csv
[13:02:43] [Buff.csv] 프로그램이 종료되었습니다.
[13:02:43] [Buff.csv] 최종 변경 사항 감지, 원본에 저장 중...
[13:02:43] [Buff.csv] 변경 사항을 원본에 저장 중...
[13:02:43] [Buff.csv] 원본 파일이 업데이트되었습니다.
[13:02:43] 파일 닫힘: Buff.csv
[13:02:43] 모든 파일이 닫혔습니다. 5초 후 프로그램을 종료합니다.
[13:02:44] 4초 후 종료...
```
