# UnlockOpenFile Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Action                             │
│                  (Double-click Excel file)                       │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            v
┌─────────────────────────────────────────────────────────────────┐
│                      UnlockOpenFile.exe                          │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                      Program.cs                            │  │
│  │          - Parse command line arguments                    │  │
│  │          - Launch FileOpenerForm or SettingsForm           │  │
│  └───────────────────────────┬───────────────────────────────┘  │
│                              │                                   │
│      ┌───────────────────────┴────────────────────┐            │
│      v                                              v            │
│  ┌──────────────────────┐              ┌──────────────────────┐│
│  │  FileOpenerForm.cs   │              │   SettingsForm.cs    ││
│  │  - Show status UI    │              │  - File association  ││
│  │  - System tray icon  │              │  - Startup settings  ││
│  │  - Log operations    │              │  - Registry ops      ││
│  └──────┬───────────────┘              └──────────────────────┘│
│         │                                                        │
│         v                                                        │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                  FileManager.cs                           │  │
│  │  ┌────────────────────────────────────────────────────┐  │  │
│  │  │ 1. Copy original → temp file                       │  │  │
│  │  │ 2. Start FileSystemWatcher                         │  │  │
│  │  │ 3. Open temp file with default app                 │  │  │
│  │  │ 4. Monitor for changes                             │  │  │
│  │  │ 5. Sync changes back to original                   │  │  │
│  │  │ 6. Cleanup on exit                                 │  │  │
│  │  └────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                            │
                ┌───────────┴────────────┐
                v                        v
    ┌────────────────────┐    ┌───────────────────┐
    │  Original File     │    │   Temp File       │
    │  C:\Data\file.xlsx │    │   %TEMP%\...      │
    │  (Unlocked)        │    │   (Being edited)  │
    └────────────────────┘    └───────────────────┘
                │                        │
                │    FileSystemWatcher   │
                │    detects changes     │
                │◄───────────────────────┘
                │
                v
    ┌────────────────────┐
    │  Synchronized      │
    │  Original File     │
    └────────────────────┘
```

## Component Interaction Flow

### Opening a File

```
User → Explorer → UnlockOpenFile.exe "file.xlsx"
                        │
                        v
                  Program.Main()
                        │
                        v
              args.Length > 0? ─Yes→ new FileOpenerForm(args[0])
                        │                      │
                        No                     v
                        │              FileManager.OpenFileAsync()
                        │                      │
                        v                      ├─→ Copy file to temp
              new SettingsForm()               ├─→ Start FileSystemWatcher
                                               ├─→ Process.Start(tempFile)
                                               └─→ Monitor for changes
```

### File Change Detection and Sync

```
Excel/App modifies temp file
            │
            v
   FileSystemWatcher.Changed event
            │
            v
   FileManager.OnFileChanged()
            │
            v
   Wait 500ms (debounce)
            │
            v
   Check LastWriteTime
            │
            v
   SaveToOriginalAsync()
            │
            ├─→ Retry loop (5 attempts)
            ├─→ File.Copy(temp → original)
            └─→ Raise FileSaved event
                      │
                      v
            FileOpenerForm updates UI
                      │
                      v
            System tray notification
```

### File Association Registration

```
User → SettingsForm → "Excel 파일 연결" button
                              │
                              v
                  RegisterFileAssociation(".xlsx")
                              │
                              ├─→ Create HKCU\Software\Classes\.xlsx
                              ├─→ Create HKCU\Software\Classes\UnlockOpenFile.xlsx
                              ├─→ Set DefaultIcon
                              ├─→ Set shell\open\command
                              └─→ Log success
                                        │
                                        v
                              User logs out/in
                                        │
                                        v
                        Double-click .xlsx file
                                        │
                                        v
                        UnlockOpenFile launches
```

## Data Flow

### File Path Transformations

```
Original File Path:
C:\Users\John\Documents\Report.xlsx
            │
            v
Temp File Generation:
%TEMP%\Report_copy_638349123456789.xlsx
= C:\Users\John\AppData\Local\Temp\Report_copy_638349123456789.xlsx
            │
            v
Process.Start opens with:
- Excel.exe
- Default .xlsx handler
            │
            v
User edits → FileSystemWatcher → Sync back to:
C:\Users\John\Documents\Report.xlsx
```

## Registry Structure

### File Association

```
HKEY_CURRENT_USER
└── Software
    └── Classes
        ├── .xlsx
        │   └── (Default) = "UnlockOpenFile.xlsx"
        │
        └── UnlockOpenFile.xlsx
            ├── (Default) = "Excel 파일 (UnlockOpenFile)"
            ├── DefaultIcon
            │   └── (Default) = "C:\Path\UnlockOpenFile.exe,0"
            └── shell
                └── open
                    └── command
                        └── (Default) = "C:\Path\UnlockOpenFile.exe" "%1"
```

### Startup Registration

```
HKEY_CURRENT_USER
└── Software
    └── Microsoft
        └── Windows
            └── CurrentVersion
                └── Run
                    └── UnlockOpenFile = "C:\Path\UnlockOpenFile.exe"
```

## Threading Model

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Thread                             │
│  - WinForms event loop                                       │
│  - User interactions                                         │
│  - Status updates                                            │
│  - System tray operations                                    │
└────────────┬────────────────────────────────────────────────┘
             │
             │ Invoke() for cross-thread calls
             │
┌────────────┴────────────────────────────────────────────────┐
│                   FileSystemWatcher Thread                   │
│  - Monitors file changes                                     │
│  - Raises Changed events                                     │
│  - Async file operations                                     │
│  - Auto-sync to original                                     │
└──────────────────────────────────────────────────────────────┘
```

## Error Handling Strategy

```
┌─────────────────┐
│  User Action    │
└────────┬────────┘
         │
         v
┌─────────────────────────────┐
│  Try-Catch Block            │
│  - Validate input           │
│  - Check file exists        │
│  - Perform operation        │
└────────┬────────────────────┘
         │
    ┌────┴────┐
    │ Success │ Failure
    v         v
┌────────┐  ┌──────────────────┐
│  Log   │  │  Catch Exception │
│  OK    │  │  - Log error     │
└────────┘  │  - Show message  │
            │  - Retry if I/O  │
            └──────────────────┘
```

## State Management

### FileManager States

```
┌─────────────┐
│ Initialized │
└──────┬──────┘
       │ OpenFileAsync()
       v
┌─────────────┐
│   Copying   │
└──────┬──────┘
       │
       v
┌─────────────┐
│  Watching   │◄──────┐
└──────┬──────┘       │
       │              │
       │ File         │ Continue
       │ changed      │ watching
       v              │
┌─────────────┐       │
│   Syncing   │───────┘
└──────┬──────┘
       │ Process exits
       v
┌─────────────┐
│  Cleanup    │
└─────────────┘
```

## Performance Considerations

### Debouncing

```
Multiple rapid file changes:
t=0ms:    Change detected
t=100ms:  Change detected  } Debounce
t=200ms:  Change detected  } window
t=500ms:  → Single sync operation
```

### Retry Logic

```
Attempt 1: Immediate
         ↓ (IOException)
Attempt 2: Wait 1000ms
         ↓ (IOException)
Attempt 3: Wait 1000ms
         ↓ (IOException)
Attempt 4: Wait 1000ms
         ↓ (IOException)
Attempt 5: Wait 1000ms
         ↓ (IOException)
    Fail: Log error
```

## Security Model

```
┌──────────────────────────────────────────────────────────┐
│              User-Level Operations Only                   │
│  - No admin privileges required                           │
│  - HKEY_CURRENT_USER registry only                        │
│  - User's temp folder only                                │
│  - User's own files only                                  │
└──────────────────────────────────────────────────────────┘
            │
            v
┌──────────────────────────────────────────────────────────┐
│              Windows Security Context                     │
│  - Files inherit user permissions                         │
│  - Temp folder is user-specific                           │
│  - Registry keys are user-specific                        │
└──────────────────────────────────────────────────────────┘
```

## Extension Points

For future enhancements, the architecture supports:

1. **Multiple File Types**: Add more extensions in SettingsForm
2. **Custom Sync Logic**: Extend FileManager with different strategies
3. **Conflict Resolution**: Add UI in FileOpenerForm
4. **Settings Persistence**: Add configuration file support
5. **Plugin System**: Abstract file operations interface
6. **Cloud Integration**: Add sync to cloud storage
7. **Version Control**: Add file history tracking

## Dependencies

```
UnlockOpenFile.exe
    │
    ├── System.Windows.Forms (WinForms UI)
    ├── System.IO.FileSystem (File operations)
    ├── System.Diagnostics.Process (Launch apps)
    ├── Microsoft.Win32.Registry (Registry operations)
    └── System.IO.FileSystemWatcher (Change monitoring)
         
All dependencies are part of .NET 8.0 Framework
No external NuGet packages required
```

## Deployment Architecture

```
Development:
    Source Code → dotnet build → bin/Debug/...

Testing:
    Source Code → dotnet build -c Release → bin/Release/...

Distribution:
    Source Code → dotnet publish → publish/UnlockOpenFile.exe
                                  → Runtime dependencies
                                  
Installation:
    User downloads → Extract to folder → Run UnlockOpenFile.exe
                                        → Optional: Register file associations
                                        → Optional: Enable startup
```

This architecture ensures:
- ✅ Separation of concerns
- ✅ Easy maintenance
- ✅ Extensibility
- ✅ User-friendly operation
- ✅ Robust error handling
- ✅ Efficient performance
