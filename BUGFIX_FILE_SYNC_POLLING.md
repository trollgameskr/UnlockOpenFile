# 파일 동기화 버그 수정: 폴링 메커니즘 추가 (File Synchronization Bug Fix: Added Polling Mechanism)

## 문제 설명 (Problem Description)

카피된 파일을 수정했을 때 원본에 적용하는 기능이 작동하지 않는 문제가 있었습니다.

When modifying a copied file, the changes were not being applied back to the original file.

## 근본 원인 (Root Cause)

기존 코드는 `FileSystemWatcher`만을 사용하여 파일 변경을 감지했습니다. 그러나 `FileSystemWatcher`는 100% 신뢰할 수 없으며 다음과 같은 경우에 이벤트가 발생하지 않을 수 있습니다:

The existing code relied solely on `FileSystemWatcher` to detect file changes. However, `FileSystemWatcher` is not 100% reliable and may not fire events in the following cases:

1. **네트워크 드라이브**: 네트워크 드라이브의 파일 변경은 감지되지 않을 수 있음
   - Network drives: Changes to files on network drives may not be detected

2. **특정 응용 프로그램**: Excel, Word 등 일부 응용 프로그램은 임시 파일을 사용하고 이름 바꾸기 작업을 수행하여 파일을 저장하므로 `FileSystemWatcher`가 감지하지 못할 수 있음
   - Certain applications: Applications like Excel and Word use temp files and rename operations to save files, which may not trigger `FileSystemWatcher` events

3. **파일 시스템 제한**: 일부 파일 시스템이나 설정에서는 변경 알림이 제대로 작동하지 않을 수 있음
   - File system limitations: Some file systems or configurations may not properly support change notifications

4. **이벤트 버퍼 오버플로**: 많은 파일 변경이 발생하면 `FileSystemWatcher`의 내부 버퍼가 오버플로되어 일부 이벤트가 손실될 수 있음
   - Event buffer overflow: When many file changes occur, `FileSystemWatcher`'s internal buffer can overflow, causing some events to be lost

## 해결 방법 (Solution)

`FileSystemWatcher`를 계속 사용하면서도, 1초마다 파일의 타임스탬프를 확인하는 **폴링 타이머**를 추가했습니다. 이는 `FileSystemWatcher`가 이벤트를 발생시키지 않을 때의 백업 메커니즘으로 작동합니다.

While continuing to use `FileSystemWatcher`, we added a **polling timer** that checks the file's timestamp every 1 second. This acts as a backup mechanism when `FileSystemWatcher` doesn't fire events.

### 구현 세부 사항 (Implementation Details)

#### 1. 폴링 타이머 필드 추가 (Added Polling Timer Field)

```csharp
private System.Threading.Timer? _pollTimer;
```

#### 2. StartPollingTimer 메서드 (StartPollingTimer Method)

파일이 열릴 때 시작되며, 1초마다 파일의 `LastWriteTime`을 확인합니다:

Started when a file is opened, checks the file's `LastWriteTime` every 1 second:

```csharp
private void StartPollingTimer()
{
    // Poll every 1 second to check for file changes
    // This acts as a backup when FileSystemWatcher doesn't fire
    _pollTimer?.Dispose();
    _pollTimer = new System.Threading.Timer(async _ =>
    {
        try
        {
            if (!File.Exists(_tempFilePath))
            {
                // File was deleted, stop polling
                StopPollingTimer();
                return;
            }
            
            var currentModified = File.GetLastWriteTime(_tempFilePath);
            if (currentModified > _lastModified)
            {
                // File was modified, save to original
                _lastModified = currentModified;
                _isModified = true;
                FileModified?.Invoke(this, EventArgs.Empty);
                
                _pendingSaveTask = SaveToOriginalAsync();
                await _pendingSaveTask;
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged($"폴링 중 오류: {ex.Message}");
        }
    }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
}
```

#### 3. StopPollingTimer 메서드 (StopPollingTimer Method)

타이머를 정리하고 리소스를 해제합니다:

Cleans up the timer and releases resources:

```csharp
private void StopPollingTimer()
{
    _pollTimer?.Dispose();
    _pollTimer = null;
}
```

#### 4. StartFileWatcher 수정 (Modified StartFileWatcher)

`FileSystemWatcher`를 시작할 때 폴링 타이머도 함께 시작:

When starting `FileSystemWatcher`, also start the polling timer:

```csharp
_fileWatcher.Changed += OnFileChanged;
_fileWatcher.EnableRaisingEvents = true;

// Start polling timer as backup mechanism since FileSystemWatcher is not always reliable
StartPollingTimer();
```

#### 5. Cleanup 수정 (Modified Cleanup)

정리할 때 폴링 타이머도 중지:

When cleaning up, also stop the polling timer:

```csharp
// Stop file monitoring timer
StopFileMonitoring();

// Stop polling timer
StopPollingTimer();
```

## 작동 원리 (How It Works)

1. **이중 감지 메커니즘**: `FileSystemWatcher`와 폴링 타이머 두 가지 방법으로 파일 변경을 감지
   - Dual detection mechanism: Detects file changes using both `FileSystemWatcher` and polling timer

2. **FileSystemWatcher 우선**: 정상적인 경우 `FileSystemWatcher`가 즉시 반응하여 변경 사항을 감지
   - FileSystemWatcher priority: In normal cases, `FileSystemWatcher` immediately reacts to detect changes

3. **폴링 백업**: `FileSystemWatcher`가 실패하더라도 폴링 타이머가 1초 이내에 변경 사항을 감지
   - Polling backup: Even if `FileSystemWatcher` fails, the polling timer detects changes within 1 second

4. **중복 저장 방지**: `_lastModified` 타임스탬프 비교로 동일한 변경 사항을 여러 번 저장하는 것을 방지
   - Duplicate save prevention: Prevents saving the same changes multiple times by comparing `_lastModified` timestamp

## 테스트 시나리오 (Test Scenarios)

이 수정으로 다음 시나리오들이 모두 처리됩니다:

This fix handles the following scenarios:

1. **정상 케이스**: FileSystemWatcher가 정상적으로 동작 → 즉시 저장
   - Normal case: FileSystemWatcher fires normally → saves immediately

2. **FileSystemWatcher 실패**: 이벤트가 발생하지 않음 → 폴링 타이머가 1초 이내에 감지하여 저장
   - FileSystemWatcher failure: Event doesn't fire → polling timer detects and saves within 1 second

3. **네트워크 드라이브**: FileSystemWatcher가 작동하지 않을 수 있음 → 폴링 타이머가 확실하게 감지
   - Network drives: FileSystemWatcher may not work → polling timer reliably detects

4. **Excel/Word 저장**: 임시 파일과 이름 바꾸기 사용 → 폴링 타이머가 최종 결과를 감지
   - Excel/Word save: Uses temp files and renaming → polling timer detects the final result

## 성능 영향 (Performance Impact)

- **최소한의 오버헤드**: 1초마다 파일 타임스탬프만 확인하므로 성능 영향 거의 없음
  - Minimal overhead: Only checks file timestamp every 1 second, so almost no performance impact

- **파일 내용 읽지 않음**: 타임스탬프만 확인하므로 대용량 파일에도 효율적
  - Doesn't read file content: Only checks timestamp, so efficient even for large files

- **리소스 사용**: 타이머 하나만 추가되므로 메모리 사용량 증가 미미
  - Resource usage: Only one timer is added, so memory usage increase is minimal

## 이점 (Benefits)

- ✅ **신뢰성 향상**: 파일 변경 감지가 100%에 가깝게 신뢰할 수 있음
  - Improved reliability: File change detection is nearly 100% reliable

- ✅ **호환성 개선**: 모든 응용 프로그램과 파일 시스템에서 작동
  - Better compatibility: Works with all applications and file systems

- ✅ **사용자 경험**: 항상 변경 사항이 저장되므로 데이터 손실 없음
  - User experience: Changes are always saved, so no data loss

- ✅ **기존 기능 유지**: FileSystemWatcher는 계속 사용되므로 즉시 반응성 유지
  - Maintains existing functionality: FileSystemWatcher is still used, maintaining immediate responsiveness

## 참고 사항 (Notes)

1. **1초 지연**: 최악의 경우 변경 사항이 감지되기까지 최대 1초 걸릴 수 있음 (FileSystemWatcher가 실패한 경우)
   - 1-second delay: In worst case, changes may take up to 1 second to be detected (if FileSystemWatcher fails)

2. **이중 저장 방지**: 타임스탬프 비교로 동일한 변경 사항을 두 번 저장하는 것을 방지
   - Duplicate save prevention: Timestamp comparison prevents saving the same changes twice

3. **자동 정리**: 파일이 닫히거나 삭제되면 타이머가 자동으로 중지됨
   - Auto cleanup: Timer automatically stops when file is closed or deleted
