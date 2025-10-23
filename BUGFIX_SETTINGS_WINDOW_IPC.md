# Settings Window IPC Timeout Fix

## Problem Description

설정 창이 열려 있을때 *.csv파일을 열면 아래 경고창 표시되면 열리지않음:
```
---------------------------
오류
---------------------------
실행 중인 프로그램과 통신할 수 없습니다: The operation has timed out.
---------------------------
확인   
---------------------------
```

Translation: When the settings window is open and a *.csv file is opened, an error dialog appears saying "Cannot communicate with the running program: The operation has timed out." and the file does not open.

## Root Cause Analysis

### Issue: IPC Server Not Started When Settings Window is Main Form

When the application is launched without arguments:
1. The program shows SettingsForm as the main application window
2. `Application.Run(new SettingsForm())` is called
3. **The IPC (Inter-Process Communication) server is NOT started**
4. The application waits for user interaction with SettingsForm

When a user tries to open a CSV file while SettingsForm is open:
1. Windows launches a second instance: `UnlockOpenFile.exe "file.csv"`
2. The second instance detects an existing instance (mutex check fails)
3. It tries to send the file path via named pipe IPC
4. **Times out after 5 seconds** because no IPC server is listening
5. Shows error dialog and exits

### Previous Code Flow

```csharp
// In Main()
if (args.Length > 0)
{
    _mainForm = new MainForm();
    _mainForm.OpenFile(args[0]);
    StartIPCServer();  // ✓ Server started
    Application.Run(_mainForm);
}
else
{
    // ✗ Server NOT started
    Application.Run(new SettingsForm());
}
```

The IPC server was only started when a file was opened (MainForm path), not when showing settings.

## Changes Made

### Program.cs

#### 1. Added SettingsForm Reference
```csharp
private static SettingsForm? _settingsForm;
```

This allows the IPC server to interact with SettingsForm when it's the main window.

#### 2. Always Start IPC Server
```csharp
try
{
    // Always start IPC server to handle file open requests
    StartIPCServer();

    if (args.Length > 0)
    {
        _mainForm = new MainForm();
        _mainForm.OpenFile(args[0]);
        Application.Run(_mainForm);
    }
    else
    {
        // Show settings form if no file is specified
        // IPC server is running, so file open requests will be handled
        _settingsForm = new SettingsForm();
        Application.Run(_settingsForm);
    }
}
```

Now the IPC server starts in both cases, ensuring it can receive file open requests.

#### 3. Handle File Open When Only SettingsForm Exists
```csharp
if (message.StartsWith("FILE:"))
{
    var filePath = message.Substring(5);
    
    if (_mainForm != null && !_mainForm.IsDisposed)
    {
        // MainForm exists, open file in it
        _mainForm.Invoke(() => {
            _mainForm.OpenFile(filePath);
            _mainForm.Show();
            _mainForm.WindowState = FormWindowState.Normal;
            _mainForm.BringToFront();
        });
    }
    else if (_settingsForm != null && !_settingsForm.IsDisposed)
    {
        // MainForm doesn't exist but SettingsForm is running
        // Create MainForm on the UI thread
        _settingsForm.Invoke(() => {
            _mainForm = new MainForm();
            _mainForm.OpenFile(filePath);
            _mainForm.Show();
            _mainForm.WindowState = FormWindowState.Normal;
            _mainForm.BringToFront();
        });
    }
}
```

When a file open request is received:
- If MainForm exists: use it (existing behavior)
- If only SettingsForm exists: create MainForm on demand (new behavior)

#### 4. Improved SHOW_SETTINGS Handling
```csharp
else if (message == "SHOW_SETTINGS")
{
    if (_mainForm != null && !_mainForm.IsDisposed)
    {
        // Show settings dialog in MainForm
        _mainForm.Invoke(() => {
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        });
    }
    else if (_settingsForm != null && !_settingsForm.IsDisposed)
    {
        // SettingsForm is already shown, just bring it to front
        _settingsForm.Invoke(() => {
            _settingsForm.Show();
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
        });
    }
}
```

Now handles bringing SettingsForm to front when it's already the main window.

## Solution Summary

### Before
```
SettingsForm open → User opens CSV file → No IPC server → Timeout error
```

### After
```
SettingsForm open → IPC server running → User opens CSV file → 
  → IPC receives request → Creates MainForm → Opens file successfully ✓
```

## Testing Scenarios

### Scenario 1: Open CSV file when Settings window is open (Bug Fix)
1. Run `UnlockOpenFile.exe` (no arguments)
2. SettingsForm opens
3. Double-click a `.csv` file in Windows Explorer
4. **Expected:** MainForm is created and file opens successfully
5. **Result:** ✓ Fixed - No timeout error

### Scenario 2: Open multiple files (Existing Functionality)
1. Run `UnlockOpenFile.exe "file1.csv"`
2. MainForm opens with file1
3. Double-click `file2.csv`
4. **Expected:** file2 opens in existing MainForm
5. **Result:** ✓ Works as before - No regression

### Scenario 3: Open settings when SettingsForm is already open
1. Run `UnlockOpenFile.exe` (no arguments)
2. SettingsForm opens
3. Run `UnlockOpenFile.exe` again
4. **Expected:** SettingsForm brought to front
5. **Result:** ✓ Improved - Now properly handled

## Impact

### Files Changed
- **Program.cs**: 1 file, 50 insertions, 17 deletions

### Behavior Changes
1. **IPC server always runs** - Started regardless of which form is shown
2. **MainForm created on demand** - When file is opened and only SettingsForm exists
3. **Both forms can coexist** - SettingsForm and MainForm can be open simultaneously

### No Breaking Changes
- All existing functionality preserved
- No changes to MainForm, SettingsForm, or FileManager
- Only Program.cs modified

## Thread Safety

All UI operations are performed on the correct UI thread:
- Uses `Form.Invoke()` to marshal calls to the UI thread
- Works with both MainForm and SettingsForm synchronization contexts
- Background IPC server thread properly communicates with UI forms

## Future Improvements

Potential enhancements (not required for this fix):
1. Close SettingsForm automatically when MainForm is created
2. Add option to dock SettingsForm in MainForm
3. Unified form management system

## Related Issues

This fix resolves:
- CSV file opening timeout when settings window is open
- Lack of IPC server when only settings are shown
- Inability to open files after showing settings first

## Version

Fixed in version: 0.9.9 (pending)
