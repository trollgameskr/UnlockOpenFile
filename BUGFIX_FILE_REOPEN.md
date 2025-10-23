# File Reopen Issue Fix

## Problem Description

파일을 열었다가 수정 후 닫았지만 "이미 열림" 으로 표시되고 다시 열려고 하면 열리지 않는 문제

Translation: Files that were opened, modified, and closed still show as "already open" and cannot be reopened.

## Root Cause Analysis

### Issue 1: Slow File Monitoring
When single-instance applications (like Excel) are used to open files:
1. The application process exits immediately after launching (passes control to existing instance)
2. FileManager detects quick process exit (< 3 seconds) and starts file monitoring timer
3. The timer was checking every **5 seconds** with a **5 minute timeout** after last modification
4. If user closes file and tries to reopen within this window, the file appears "already open"

### Issue 2: No Defensive Check
The `OpenFile` method in MainForm only checked if the file path existed in the `_fileManagers` dictionary, without verifying if the file was actually still in use.

### Issue 3: Race Condition with IPC
When a file is closed and the MainForm is closing, the IPC server could still receive reopen requests and try to invoke on a disposed form.

## Changes Made

### 1. FileManager.cs

#### Added `IsFileStillInUse()` Method
```csharp
public bool IsFileStillInUse()
{
    // Check if temp file exists and is locked
    if (!File.Exists(_tempFilePath))
        return false;
        
    return IsFileLocked(_tempFilePath);
}
```

This allows checking if a FileManager is still actively managing a file.

#### Improved File Monitoring Responsiveness
- **Polling interval**: Reduced from 5 seconds → **2 seconds**
- **Timeout after unlock**: Reduced from 5 minutes → **10 seconds**

Changes in `StartFileMonitoring()`:
- Faster detection of file closure
- Quicker cleanup when file is no longer locked
- Better support for immediate reopen scenarios

### 2. MainForm.cs

#### Enhanced `OpenFile()` Method
```csharp
if (_fileManagers.ContainsKey(filePath))
{
    // Check if the file is actually still in use
    var existingManager = _fileManagers[filePath];
    if (!existingManager.IsFileStillInUse())
    {
        // File is no longer in use, clean it up and allow reopening
        AddLog($"기존 파일이 더 이상 사용 중이 아닙니다. 정리 중...: {filePath}");
        RemoveFile(filePath);
        // Continue to open the file below
    }
    else
    {
        AddLog($"파일이 이미 열려 있습니다: {filePath}");
        UpdateFileStatus(filePath, "이미 열림");
        
        // Show and bring the main form to front
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.BringToFront();
        return;
    }
}
```

Benefits:
- Validates that file is actually in use before blocking reopen
- Automatically cleans up stale entries
- Brings MainForm to front when file is legitimately already open

### 3. Program.cs

#### Added Form Disposal Check in IPC Server
```csharp
if (_mainForm != null && !_mainForm.IsDisposed && !string.IsNullOrEmpty(message))
```

Prevents attempting to invoke operations on a disposed form.

## Test Scenarios

### Scenario 1: Quick Reopen After Close
**Before Fix:**
1. Open file → Excel launches → File monitoring starts
2. Close Excel → Timer hasn't fired yet
3. Reopen immediately → "Already open" error ❌

**After Fix:**
1. Open file → Excel launches → File monitoring starts (2s interval)
2. Close Excel → File unlocked
3. Reopen immediately → Detects file not in use → Cleans up → Reopens ✅

### Scenario 2: Reopen After Modification
**Before Fix:**
1. Open file → Modify → Close
2. Timer requires 5 minutes of inactivity
3. Try to reopen → "Already open" error ❌

**After Fix:**
1. Open file → Modify → Close
2. After 10 seconds of inactivity (file unlocked) → Automatic cleanup
3. Try to reopen → Detects file not in use → Reopens ✅

### Scenario 3: Actually Already Open
**Before Fix:**
1. Open file → Keep editor open
2. Try to reopen → "Already open" error (correct) ✅
3. But MainForm not visible

**After Fix:**
1. Open file → Keep editor open
2. Try to reopen → "Already open" message ✅
3. MainForm brought to front for visibility ✅

## Impact Assessment

### Performance
- Slightly increased CPU usage due to faster polling (2s vs 5s)
- More responsive file closure detection
- Negligible impact on system resources

### Compatibility
- Fully backward compatible
- No breaking changes to existing functionality
- Improves user experience without changing behavior

### Edge Cases Handled
- Single-instance applications (Excel, Word, etc.)
- Quick close-and-reopen operations
- File cleanup when editor crashes
- IPC requests to closing/disposed forms

## Future Improvements

Potential enhancements for even better handling:
1. Use FileSystemWatcher events for instant deletion detection (instead of polling)
2. Implement process handle monitoring for more reliable detection
3. Add user notification when automatic cleanup occurs
4. Configurable timeout values in settings

## Testing Recommendations

### Manual Testing
1. Open an Excel file with UnlockOpenFile
2. Make changes and save
3. Close Excel immediately
4. Within 1-2 seconds, try to reopen the same file
5. Verify file opens without "already open" error

### Automated Testing
Due to Windows Forms and file system dependencies, automated testing requires:
- Mocking FileSystemWatcher
- Process lifecycle simulation
- Timer event simulation
- Integration test environment with Excel installed
