# UI Changes - Custom Application Selector

## Overview
This document describes the UI changes made to the SettingsForm to support custom application selection.

## Settings Form Layout (Before)

```
┌────────────────────────────────────────────────────────────┐
│ UnlockOpenFile - 설정                                      │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 시작 프로그램                                         │ │
│  │                                                       │ │
│  │  ☐ Windows 시작 시 자동 실행                         │ │
│  │                                                       │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 파일 연결                                            │ │
│  │                                                       │ │
│  │  [Excel 파일 (.xlsx) 연결] [CSV 파일 (.csv) 연결]   │ │
│  │  [연결 해제]                                         │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 로그 출력 영역                                       │ │
│  │                                                       │ │
│  │                                                       │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
│                                           [닫기]          │
└────────────────────────────────────────────────────────────┘
Size: 700x500
```

## Settings Form Layout (After - With Custom Application Feature)

```
┌────────────────────────────────────────────────────────────┐
│ UnlockOpenFile - 설정                                      │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 시작 프로그램                                         │ │
│  │                                                       │ │
│  │  ☐ Windows 시작 시 자동 실행                         │ │
│  │                                                       │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 파일 연결                                            │ │
│  │                                                       │ │
│  │  [Excel 파일 (.xlsx) 연결] [CSV 파일 (.csv) 연결]   │ │
│  │  [연결 해제]                                         │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 사용자 지정 응용 프로그램              ★ NEW ★       │ │
│  │                                                       │ │
│  │  ┌────────────────────────────────────────────────┐  │ │
│  │  │확장자  │ 응용 프로그램 경로                    │  │ │
│  │  ├────────────────────────────────────────────────┤  │ │
│  │  │.txt    │ C:\Program Files\Notepad++\notepad++.│  │ │
│  │  │.pdf    │ C:\Program Files\Adobe\Reader\Acro..│  │ │
│  │  └────────────────────────────────────────────────┘  │ │
│  │                                    [추가/수정] [제거]│ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 로그 출력 영역                                       │ │
│  │                                                       │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
│                                           [닫기]          │
└────────────────────────────────────────────────────────────┘
Size: 700x700 (height increased by 200px)
```

## New Dialog: Add/Modify Custom Application

```
┌──────────────────────────────────────────┐
│ 확장자 입력                              │
├──────────────────────────────────────────┤
│                                          │
│  파일 확장자를 입력하세요 (예: .txt, .xlsx):  │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │.txt                                │ │
│  └────────────────────────────────────┘ │
│                                          │
│                     [확인]      [취소]  │
└──────────────────────────────────────────┘

After clicking [확인], a file picker dialog opens:

┌──────────────────────────────────────────┐
│ .txt 파일을 열 응용 프로그램 선택        │
├──────────────────────────────────────────┤
│  파일 이름: notepad++.exe                │
│                                          │
│  파일 형식: 실행 파일 (*.exe)            │
│                                          │
│                     [열기]      [취소]  │
└──────────────────────────────────────────┘
```

## UI Components Added

### 1. Custom Application GroupBox
- **Name**: `_customApplicationGroup`
- **Text**: "사용자 지정 응용 프로그램"
- **Height**: 200px
- **Location**: Row 2 in main TableLayoutPanel

### 2. Application ListView
- **Name**: `_applicationListView`
- **Columns**: 
  - "확장자" (100px)
  - "응용 프로그램 경로" (450px)
- **Size**: 560x110
- **Features**: Full row select, grid lines

### 3. Add/Modify Button
- **Name**: `_addApplicationButton`
- **Text**: "추가/수정"
- **Size**: 80x30
- **Action**: Opens extension input dialog, then file picker

### 4. Remove Button
- **Name**: `_removeApplicationButton`
- **Text**: "제거"
- **Size**: 80x30
- **Action**: Removes selected custom application association

## User Flow

1. **Adding a Custom Application**
   ```
   User clicks [추가/수정]
   → Extension dialog appears
   → User enters ".txt"
   → User clicks [확인]
   → File picker appears
   → User selects "notepad++.exe"
   → Setting is saved to registry
   → ListView is refreshed
   → Log shows confirmation message
   ```

2. **Removing a Custom Application**
   ```
   User selects ".txt" row in ListView
   → User clicks [제거]
   → Confirmation dialog appears
   → User clicks [Yes]
   → Setting is removed from registry
   → ListView is refreshed
   → Log shows confirmation message
   ```

3. **Opening a File with Custom Application**
   ```
   User opens file.txt with UnlockOpenFile
   → FileManager checks ApplicationSettings
   → Custom app found: notepad++.exe
   → Temp file opened with notepad++
   → Log shows "사용자 지정 응용 프로그램 사용: notepad++.exe"
   ```

## Registry Structure

```
HKEY_CURRENT_USER\Software\UnlockOpenFile\Applications
│
├── .txt = "C:\Program Files\Notepad++\notepad++.exe"
├── .pdf = "C:\Program Files\Adobe\Reader\AcroRd32.exe"
└── .docx = "C:\Program Files\LibreOffice\Writer\swriter.exe"
```

## Benefits

1. **User Control**: Users can specify exactly which application to use
2. **Override System Defaults**: Custom settings take priority over Windows defaults
3. **No Admin Required**: Settings stored in user-level registry
4. **Per-Extension**: Different applications for different file types
5. **Easy Management**: Simple UI to add/remove associations
6. **Persistent**: Settings survive application restarts
7. **Transparent**: Clear logging of which application is being used

## Examples

### Example 1: Use Notepad++ for Text Files
- Extension: `.txt`
- Application: `C:\Program Files\Notepad++\notepad++.exe`

### Example 2: Use LibreOffice for Excel Files
- Extension: `.xlsx`
- Application: `C:\Program Files\LibreOffice\Calc\scalc.exe`

### Example 3: Use Chrome for PDF Files
- Extension: `.pdf`
- Application: `C:\Program Files\Google\Chrome\Application\chrome.exe`

## Technical Implementation

### ApplicationSettings Class
- Static utility class
- Registry operations in `HKEY_CURRENT_USER`
- Methods:
  - `SetApplicationPath(string extension, string path)`
  - `GetApplicationPath(string extension) → string?`
  - `RemoveApplicationPath(string extension)`
  - `GetAllApplicationPaths() → Dictionary<string, string>`

### FileManager Integration
- Modified `GetActualDefaultApplication()` method
- Priority order:
  1. Custom application settings (highest)
  2. User-specific registry (HKCU)
  3. System-wide registry (HKLM)
  4. Common application defaults
  5. Shell execute fallback (lowest)

### Error Handling
- File existence validation
- Registry access error handling
- Invalid extension handling
- User-friendly error messages
- Log all operations
