# 변경 사항 (Changes)

## 주요 개선 사항

### 1. 파일 닫을 때 프로그램 자동 종료
- 파일 편집기(Excel 등)가 종료되면 해당 파일이 자동으로 관리 목록에서 제거됩니다
- 모든 열린 파일이 닫히면 관리 프로그램도 자동으로 종료됩니다
- 더 이상 수동으로 관리 창을 닫을 필요가 없습니다

### 2. 단일 인스턴스 관리
- 여러 파일을 열어도 하나의 관리 창만 실행됩니다
- 새로운 파일을 열면 기존 관리 창에 자동으로 추가됩니다
- Named Pipe를 통한 프로세스 간 통신으로 구현
- Mutex를 사용하여 단일 인스턴스 보장

### 3. 통합 파일 관리 UI
- 새로운 `MainForm`을 통해 모든 열린 파일을 한눈에 확인
- 파일 목록에서 각 파일의 상태 실시간 확인
- 통합 로그 창에서 모든 파일의 활동 추적
- "모두 닫기" 버튼으로 한 번에 모든 파일 정리

## 기술적 변경 사항

### 새로운 파일
- **MainForm.cs**: 여러 파일을 관리하는 중앙 관리 폼
  - ListView로 파일 목록 표시
  - 각 파일의 FileManager 인스턴스 관리
  - 프로세스 종료 이벤트 처리

### 수정된 파일

#### Program.cs
- Mutex를 사용한 단일 인스턴스 구현
- Named Pipe를 통한 IPC 서버/클라이언트 구현
- 파일 인자가 있을 때 MainForm 시작
- 기존 인스턴스로 파일 경로 전달

#### FileManager.cs
- `ProcessExited` 이벤트 추가
- 프로세스 종료 시 이벤트 발생
- 다중 파일 관리를 위한 개선

#### FileOpenerForm.cs
- ProcessExited 이벤트 핸들러 추가
- 프로세스 종료 시 폼 자동 닫기
- (기존 단일 파일 관리 폼으로 유지, MainForm으로 대체됨)

#### README.md
- 새로운 기능 설명 추가
- 작동 원리 업데이트

## 사용 시나리오

### 시나리오 1: 단일 파일 열기
```
1. file1.xlsx를 더블클릭
2. MainForm이 열리고 file1.xlsx가 목록에 추가됨
3. Excel에서 file1.xlsx 편집
4. Excel 종료
5. MainForm이 자동으로 닫힘
```

### 시나리오 2: 여러 파일 열기
```
1. file1.xlsx를 더블클릭 → MainForm 열림
2. file2.csv를 더블클릭 → 같은 MainForm에 file2.csv 추가
3. file3.xlsx를 더블클릭 → 같은 MainForm에 file3.xlsx 추가
4. Excel에서 file1 닫기 → file1만 목록에서 제거, MainForm은 유지
5. file2 닫기 → file2 제거, MainForm은 유지
6. file3 닫기 → file3 제거, MainForm 자동 종료
```

### 시나리오 3: 설정 창
```
1. 인자 없이 UnlockOpenFile.exe 실행
2. SettingsForm이 열림 (단독 실행)
3. 설정 완료 후 닫기
```

## 기술 상세

### 단일 인스턴스 구현
- Mutex 이름: `UnlockOpenFile_SingleInstance_Mutex`
- Named Pipe 이름: `UnlockOpenFile_IPC_Pipe`
- IPC 메시지 형식:
  - `FILE:<파일경로>` - 파일 열기 요청
  - `SHOW_SETTINGS` - 설정 창 표시 요청

### 프로세스 종료 감지
- Process.Exited 이벤트 사용
- EnableRaisingEvents = true로 이벤트 활성화
- 비동기 이벤트 처리 (Invoke 사용)

### 멀티 스레딩
- IPC 서버는 백그라운드 스레드에서 실행
- UI 업데이트는 Invoke를 통해 메인 스레드에서 처리
- 파일 변경 감지는 FileSystemWatcher의 자체 스레드 사용
