# 빌드 테스트 가이드

## 개요

이 문서는 GitHub 빌드 개선 사항을 테스트하는 방법을 설명합니다.

## 변경 사항 요약

### 1. 프로젝트 설정 개선
- PE 메타데이터 추가 (Trademark, PackageTags)
- 빌드 재현성 개선 (PathMap, EmbedUntrackedSources)
- PE 구조 완전성 향상 (IncludeAllContentForSelfExtract)

### 2. 빌드 워크플로우 개선
- SBOM 생성
- SourceRevisionId 포함
- BUILD_INFO.txt 생성
- 최적 압축 레벨
- 바이너리 SHA256 해시

## 테스트 시나리오

### 테스트 1: 로컬 빌드 확인

**목적:** 개선된 설정으로 로컬 빌드가 정상 작동하는지 확인

**단계:**
```bash
# 1. 저장소 클론
git clone https://github.com/trollgameskr/UnlockOpenFile.git
cd UnlockOpenFile

# 2. 이 브랜치로 전환
git checkout copilot/investigate-github-build-issue

# 3. 빌드
dotnet restore
dotnet build -c Release

# 4. 결과 확인
ls -lh bin/Release/net8.0-windows/

# 5. 실행 파일 정보 확인 (Windows에서)
# PowerShell에서:
Get-ItemProperty bin\Release\net8.0-windows\UnlockOpenFile.dll | Select-Object VersionInfo
```

**예상 결과:**
- ✅ 빌드 성공
- ✅ 경고 1개 (DPI 설정 관련 - 무시 가능)
- ✅ 실행 파일에 메타데이터 포함 확인

### 테스트 2: Publish 빌드 확인

**목적:** Publish 시 모든 메타데이터가 포함되는지 확인

**단계:**
```bash
# 1. Framework-dependent 빌드
dotnet publish -c Release -r win-x64 --self-contained false -o ./test-publish

# 2. Standalone 빌드
dotnet publish -c Release -r win-x64 --self-contained true -o ./test-standalone

# 3. 파일 크기 확인
ls -lh test-publish/UnlockOpenFile.exe
ls -lh test-standalone/UnlockOpenFile.exe

# 4. SHA256 계산
sha256sum test-publish/UnlockOpenFile.exe
sha256sum test-standalone/UnlockOpenFile.exe
```

**예상 결과:**
- ✅ Framework-dependent: ~280KB
- ✅ Standalone: ~170MB
- ✅ SHA256 해시 생성 성공

### 테스트 3: GitHub Actions 빌드

**목적:** GitHub Actions에서 빌드가 정상 작동하고 개선 사항이 적용되는지 확인

**단계:**
1. 새 테스트 태그 생성
   ```bash
   git tag v0.9.9-test
   git push origin v0.9.9-test
   ```

2. GitHub Actions 페이지에서 빌드 진행 상황 확인
   - https://github.com/trollgameskr/UnlockOpenFile/actions

3. 빌드 완료 후 확인 사항:
   - ✅ SBOM 생성 단계 성공 (또는 continue-on-error로 건너뜀)
   - ✅ BUILD_INFO.txt 생성 확인
   - ✅ ZIP 파일 생성 성공
   - ✅ Checksums 계산 성공

4. 릴리스 페이지에서 확인
   - https://github.com/trollgameskr/UnlockOpenFile/releases
   - ✅ BUILD_INFO.txt 정보 포함 확인
   - ✅ Binary SHA256 해시 확인
   - ✅ 빌드 투명성 정보 확인

### 테스트 4: ZIP 파일 내용 확인

**목적:** ZIP 파일 내부에 BUILD_INFO.txt가 포함되어 있고 정보가 정확한지 확인

**단계:**
1. GitHub 릴리스에서 ZIP 다운로드
2. 압축 해제
3. BUILD_INFO.txt 확인
   ```bash
   cat BUILD_INFO.txt
   ```

**예상 내용:**
```
UnlockOpenFile vX.X.X - Standalone Build
================================================

Built on: YYYY-MM-DD HH:MM:SS UTC
Build Environment: GitHub Actions (Windows)
Source Commit: [commit hash]
Source Repository: https://github.com/trollgameskr/UnlockOpenFile

...

Binary SHA256: [hash]
```

### 테스트 5: 바이너리 메타데이터 확인

**목적:** 생성된 실행 파일에 올바른 메타데이터가 포함되어 있는지 확인

**Windows PowerShell에서:**
```powershell
# 1. 파일 속성 확인
Get-ItemProperty UnlockOpenFile.exe | Select-Object VersionInfo | Format-List

# 2. 상세 정보 확인
(Get-ItemProperty UnlockOpenFile.exe).VersionInfo | Format-List *

# 3. 디지털 서명 확인 (현재는 없음)
Get-AuthenticodeSignature UnlockOpenFile.exe
```

**예상 결과:**
```
CompanyName      : trollgameskr
FileDescription  : A Windows application that allows editing files...
FileVersion      : 0.9.8.0
ProductName      : UnlockOpenFile
ProductVersion   : 0.9.8+[commit hash]
LegalCopyright   : Copyright © 2024 trollgameskr. Licensed under MIT License.
LegalTrademarks  : UnlockOpenFile
```

### 테스트 6: 보안 스캔

**목적:** 빌드 파일의 보안 검증

**단계:**
1. VirusTotal에서 스캔
2. 로컬 보안 도구로 검증
3. 결과 비교

**비교 항목:**
- 이전 빌드 vs 새 빌드
- Framework-dependent vs Standalone
- 빌드 품질 비교

### 테스트 7: VirusTotal 스캔

**목적:** 여러 보안 엔진의 검증 확인

**단계:**
1. https://www.virustotal.com 방문
2. UnlockOpenFile.exe 업로드
3. 스캔 결과 대기
4. 결과 분석

**비교 항목:**
- 탐지 엔진 수
- 탐지 이름
- False Positive 비율

### 테스트 8: 빌드 재현성 확인

**목적:** 동일한 소스에서 일관된 바이너리가 생성되는지 확인

**단계:**
```bash
# 1. 첫 번째 빌드
git checkout [commit-hash]
dotnet clean
dotnet publish -c Release -r win-x64 --self-contained false -o ./build1
sha256sum ./build1/UnlockOpenFile.exe > hash1.txt

# 2. 두 번째 빌드 (완전히 새로 시작)
rm -rf bin obj build1
dotnet publish -c Release -r win-x64 --self-contained false -o ./build2
sha256sum ./build2/UnlockOpenFile.exe > hash2.txt

# 3. 비교
diff hash1.txt hash2.txt
```

**예상 결과:**
- ⚠️ 완전히 동일하지 않을 수 있음 (타임스탬프 등)
- ⚠️ 주요 차이점은 최소화되어야 함

### 테스트 9: 릴리스 노트 확인

**목적:** 릴리스 노트에 모든 정보가 포함되어 있는지 확인

**확인 항목:**
- ✅ ZIP 파일 SHA256
- ✅ 바이너리 SHA256
- ✅ 빌드 투명성 섹션
- ✅ 소스 커밋 링크
- ✅ 빌드 워크플로우 링크
- ✅ 자체 빌드 재현 방법

### 테스트 10: 문서 확인

**목적:** 새 문서가 올바르게 작성되고 연결되어 있는지 확인

**확인 항목:**
- ✅ CODE_SIGNING_GUIDE.md 존재
- ✅ SOLUTION_SUMMARY.md 존재
- ✅ README.md에 문서 링크 포함

## 성공 기준

### 필수 (Must Have)
- ✅ 로컬 빌드 성공
- ✅ GitHub Actions 빌드 성공
- ✅ BUILD_INFO.txt 포함
- ✅ SHA256 해시 생성
- ✅ 메타데이터 포함 확인

### 권장 (Should Have)
- ⚠️ 빌드 품질 향상
- ⚠️ 빌드 재현성 향상

### 선택 (Nice to Have)
- 🎯 완전한 빌드 재현성
- 🎯 코드 서명 적용

## 테스트 결과 보고

테스트 완료 후 다음 정보를 기록하세요:

```markdown
## 테스트 결과

### 환경
- Windows 버전:
- .NET SDK 버전:

### 빌드 테스트
- [ ] 로컬 빌드 성공
- [ ] GitHub Actions 빌드 성공
- [ ] BUILD_INFO.txt 생성 확인
- [ ] SHA256 해시 일치 확인

### 보안 검증
- 탐지 엔진 수: X / 70
- 주요 탐지 내용:
- 이전 대비 변화:

### 기타 문제
- 발견된 문제:
- 개선 사항:
```

## 문제 해결

### SBOM 생성 실패
```
continue-on-error: true로 설정되어 있어 빌드는 계속됩니다.
필요시 제거 가능합니다.
```

### 빌드 문제
```
1. Standalone 빌드 사용
2. SignPath.io 코드 서명 신청
```

### 빌드 재현성 문제
```
PathMap과 Deterministic 설정이 적용되었지만
완전한 재현성은 어려울 수 있습니다.
주요 코드는 동일해야 합니다.
```

## 다음 단계

테스트 완료 후:
1. 결과 정리 및 문서화
2. 개선 사항 적용
3. SignPath.io 신청 고려
4. 정식 릴리스 준비
