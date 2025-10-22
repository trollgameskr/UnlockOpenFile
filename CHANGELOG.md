# Changelog

All notable changes to UnlockOpenFile will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-10-21

### Added
- Initial release of UnlockOpenFile
- Core file copying and monitoring functionality
- Automatic synchronization from temporary copy to original file
- File association registration for .xlsx and .csv files
- Windows startup program registration capability
- WinForms user interface with Korean language support
- System tray integration with notifications
- FileSystemWatcher for real-time change detection
- Settings form for configuration management
- Error handling and retry logic for file operations
- Comprehensive documentation:
  - README.md with usage instructions
  - USAGE_GUIDE.md with detailed scenarios
  - ARCHITECTURE.md with technical diagrams
  - IMPLEMENTATION_SUMMARY.md with project overview
- Example scripts:
  - Batch script (open_file.bat)
  - PowerShell script (open_file.ps1)
- MIT License for open source distribution
- .NET 8.0 target framework for Windows

### Features
- ✅ Creates temporary file copy to avoid locking original
- ✅ Monitors changes with 500ms debounce
- ✅ Automatic retry (5 attempts) for locked files
- ✅ User-level registry operations (no admin required)
- ✅ System tray icon with context menu
- ✅ Real-time status logging
- ✅ Balloon notifications for file saves
- ✅ Graceful cleanup on exit
- ✅ Support for large files
- ✅ Process exit detection

### Technical Details
- Target Framework: .NET 8.0 for Windows
- UI Framework: Windows Forms
- Build Size: ~150KB (excluding runtime)
- Lines of Code: ~720
- Build Warnings: 0
- Build Errors: 0

### Requirements Met
All requirements from the problem statement are implemented:
1. ✅ 파일 복사 및 모니터링 (File copy and monitoring)
2. ✅ Windows OS 지원 (Windows OS support)
3. ✅ 파일 연결 기능 (File association capability)
4. ✅ 시작 프로그램 등록 (Startup registration)
5. ✅ 무료 소프트웨어 (Free software, no paid components)

### Known Limitations
- Windows 10 or later required
- Cannot be tested on non-Windows systems
- Sync delay minimum 500ms for change detection
- Requires sufficient temp folder space for large files

### Documentation
- User guide with troubleshooting
- Developer documentation with architecture
- Example scripts for automation
- Korean language documentation throughout

## [Unreleased]

### Added
- Custom application selector UI for per-extension application configuration
- ApplicationSettings class for managing custom application paths in registry
- ListView in SettingsForm to display and manage custom applications
- Add/Modify button to configure custom application for file extensions
- Remove button to delete custom application associations
- Priority system: custom settings > Windows registry > common defaults
- FileSaved event handlers in MainForm and FileOpenerForm for complete sync feedback
- Balloon notifications when file save to original completes successfully

### Changed
- FileManager now checks custom application settings before falling back to Windows defaults
- SettingsForm increased height to 700px to accommodate new UI section
- Updated documentation (README.md, USAGE_GUIDE.md) with custom application feature
- Enhanced automatic sync feature to show both start and completion notifications
- README.md updated to mention system notifications for sync completion

### Fixed
- Temporary files now open with user-specified applications instead of always using Notepad
- Users can now override system default applications through UI
- FileSaved event is now properly utilized to notify users when sync completes

## [Unreleased - Future]

### Planned for Future Releases
- Multi-language support (English, Japanese, etc.)
- Configurable sync delay in settings
- Support for additional file types
- Network drive optimization
- Conflict resolution UI
- File history and versioning
- Settings persistence in config file
- Installer package (MSI/EXE)
- Auto-update mechanism
- Theme support (light/dark mode)

---

## Version History

- **v1.0.0** (2024-10-21) - Initial release with all core features
