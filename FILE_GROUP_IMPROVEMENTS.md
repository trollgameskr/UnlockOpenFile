# 파일 그룹 관리 기능 개선

## 개요
이 문서는 파일 그룹 관리 기능의 편의성 개선 사항을 설명합니다.

## 새로운 기능

### 1. 그룹 이름 변경 (Rename Group)
**기능 설명:**
- 기존에 생성된 그룹의 이름을 변경할 수 있습니다.
- 설정 창의 "파일 그룹 관리" 섹션에 "이름 변경" 버튼이 추가되었습니다.

**사용 방법:**
1. 설정 창을 엽니다
2. "파일 그룹 관리" 섹션에서 이름을 변경할 그룹을 선택합니다
3. "이름 변경" 버튼을 클릭하거나, 그룹 이름을 더블클릭합니다
4. 새 그룹 이름을 입력하고 확인 버튼을 클릭합니다

**주요 특징:**
- 중복된 그룹 이름 검사
- 기존 그룹 이름이 기본값으로 표시
- 그룹 목록 더블클릭으로도 이름 변경 가능

**기술적 구현:**
```csharp
// FileGroupManager.RenameGroup() 메서드 활용
private void OnRenameGroupClick(object? sender, EventArgs e)
{
    // 그룹 선택 확인
    // 이름 변경 대화상자 표시
    // 중복 검사
    // FileGroupManager.RenameGroup(oldName, newName) 호출
    // 그룹 목록 새로고침
}

// 더블클릭 이벤트 핸들러
private void OnFileGroupsListViewDoubleClick(object? sender, EventArgs e)
{
    OnRenameGroupClick(sender, e);
}
```

### 2. 최근 파일 목록에서 그룹에 파일 추가
**기능 설명:**
- 최근에 열었던 파일 목록에서 그룹에 파일을 추가할 수 있습니다.
- 여러 파일을 체크박스로 선택하여 한 번에 추가 가능합니다.

**사용 방법:**
1. 설정 창에서 그룹을 선택하고 "파일 관리" 버튼을 클릭합니다
2. 그룹 파일 관리 대화상자에서 "최근 목록에서 추가" 버튼을 클릭합니다
3. 최근 파일 목록에서 추가할 파일을 체크합니다
4. "추가" 버튼을 클릭합니다

**주요 특징:**
- 최근 열었던 파일 목록 표시
- 체크박스로 여러 파일 선택 가능
- 이미 그룹에 있는 파일은 중복 추가되지 않음
- 추가된 파일 개수 표시

**기술적 구현:**
```csharp
// RecentFilesManager 활용
var recentFiles = RecentFilesManager.GetRecentFiles();

// 체크박스가 있는 ListView로 파일 선택
var recentListView = new ListView
{
    CheckBoxes = true,
    // ...
};

// 선택된 파일들을 그룹에 추가
foreach (ListViewItem item in recentListView.Items)
{
    if (item.Checked)
    {
        FileGroupManager.AddFileToGroup(groupName, filePath);
        // UI 업데이트
    }
}
```

### 3. UI 개선사항
**변경 사항:**
- 그룹 파일 관리 대화상자 크기 확대 (600px → 700px)
- 버튼 위치 재정렬 (이름 변경 버튼 추가에 따른 조정)
- 중복 파일 추가 방지 로직 개선

**버튼 레이아웃:**
```
[추가] (y=45)
[이름 변경] (y=85)  ← 새로 추가
[파일 관리] (y=125) ← 위치 조정
[삭제] (y=165)      ← 위치 조정
```

## 코드 품질 개선

### 헬퍼 메서드 추가
중복 로직을 제거하기 위해 `IsFileInListView()` 헬퍼 메서드를 추가했습니다:

```csharp
private bool IsFileInListView(ListView listView, string filePath)
{
    foreach (ListViewItem item in listView.Items)
    {
        if (item.Tag?.ToString()?.Equals(filePath, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }
    }
    return false;
}
```

이 메서드는 다음 위치에서 사용됩니다:
- 파일 추가 시 중복 검사
- 최근 파일 목록에서 추가 시 중복 검사

## 영향을 받는 파일
- `SettingsForm.cs` - 모든 새 기능 구현

## 기존 API 활용
새 기능은 기존 `FileGroupManager` 및 `RecentFilesManager` API를 재사용하여 구현되었습니다:

### FileGroupManager
- `RenameGroup(string oldName, string newName)` - 그룹 이름 변경
- `AddFileToGroup(string groupName, string filePath)` - 파일을 그룹에 추가
- `GetAllGroups()` - 모든 그룹 목록 가져오기
- `GetGroupFiles(string groupName)` - 그룹의 파일 목록 가져오기

### RecentFilesManager
- `GetRecentFiles()` - 최근 파일 목록 가져오기

## 테스트 시나리오

### 그룹 이름 변경 테스트
1. ✅ 정상적인 이름 변경
2. ✅ 중복된 이름으로 변경 시도 (오류 메시지 표시)
3. ✅ 빈 이름으로 변경 시도 (오류 메시지 표시)
4. ✅ 더블클릭으로 이름 변경

### 최근 파일 추가 테스트
1. ✅ 단일 파일 추가
2. ✅ 여러 파일 동시 추가
3. ✅ 이미 그룹에 있는 파일 추가 시도 (중복 방지)
4. ✅ 추가된 파일 개수 확인

## 사용자 경험 개선
- 직관적인 버튼 위치와 명명
- 한국어 사용자 친화적인 메시지
- 더블클릭 지원으로 빠른 작업 가능
- 체크박스로 여러 항목 선택 가능

## 향후 개선 가능 사항
1. 드래그 앤 드롭으로 파일 추가
2. 그룹 간 파일 이동
3. 그룹 내 파일 순서 조정
4. 그룹 병합 기능
