# Windows Defender 오탐 대응 변경 사항 요약

## 개요

이 문서는 Windows 11 Defender의 바이러스 오탐(False Positive) 문제를 해결하기 위해 적용한 변경 사항을 요약합니다.

## 문제점

v0.9.8 릴리스에서 Windows 11 Defender가 UnlockOpenFile을 바이러스로 탐지하는 문제가 보고되었습니다. 이는 프로그램의 정상적인 기능(프로세스 모니터링, 파일 시스템 감시, 레지스트리 접근 등)이 일부 보안 프로그램의 휴리스틱 분석에서 의심스러운 패턴으로 인식되기 때문입니다.

## 적용된 해결 방법

### 1. 어셈블리 메타데이터 강화 (UnlockOpenFile.csproj)

Windows Defender는 실행 파일의 메타데이터를 분석하여 신뢰도를 평가합니다. 다음 정보를 추가했습니다:

```xml
<!-- Assembly Information to reduce false positives -->
<AssemblyName>UnlockOpenFile</AssemblyName>
<AssemblyTitle>UnlockOpenFile</AssemblyTitle>
<Product>UnlockOpenFile</Product>
<Company>trollgameskr</Company>
<Copyright>Copyright © 2024 trollgameskr. Licensed under MIT License.</Copyright>
<Description>A Windows application that allows editing files (Excel, CSV) without locking the original file by creating temporary copies</Description>
<Version>0.9.8</Version>
<FileVersion>0.9.8.0</FileVersion>
<AssemblyVersion>0.9.8.0</AssemblyVersion>
<NeutralLanguage>ko</NeutralLanguage>
<Authors>trollgameskr</Authors>
<PackageProjectUrl>https://github.com/trollgameskr/UnlockOpenFile</PackageProjectUrl>
<RepositoryUrl>https://github.com/trollgameskr/UnlockOpenFile</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<PackageLicenseExpression>MIT</PackageLicenseExpression>

<!-- Enable deterministic builds for better reproducibility -->
<Deterministic>true</Deterministic>
<ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
```

**효과:**
- 실행 파일에 명확한 제품 정보 포함
- 결정론적 빌드로 재현 가능성 향상
- 오픈소스 저장소 정보 명시

### 2. 애플리케이션 매니페스트 개선 (app.manifest)

매니페스트 파일에 더 많은 정보를 추가했습니다:

```xml
<assemblyIdentity version="0.9.8.0" name="UnlockOpenFile" processorArchitecture="amd64" type="win32"/>
<description>UnlockOpenFile - A tool to edit files without locking them</description>

<!-- DPI Awareness for high-DPI displays -->
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/PM</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2, PerMonitor</dpiAwareness>
    <longPathAware xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">true</longPathAware>
  </windowsSettings>
</application>
```

**효과:**
- 프로그램 설명 추가
- 정확한 버전 정보
- Windows 호환성 정보 명시

### 3. 보안 문서 작성 (SECURITY.md)

포괄적인 보안 문서를 작성하여 다음 내용을 포함했습니다:

- 오탐이 발생하는 이유 설명
- 안전성 확인 방법 (소스 코드 검토, 자체 빌드, VirusTotal)
- Windows Defender에서 허용하는 방법
- Microsoft에 오탐 신고하는 방법
- 개인정보 보호 정책
- 필요한 권한 목록과 용도

**효과:**
- 사용자에게 투명한 정보 제공
- 오탐 해결을 위한 자가 진단 도구 제공

### 4. 단계별 해결 가이드 작성 (WINDOWS_DEFENDER_FIX.md)

사용자가 즉시 문제를 해결할 수 있도록 상세한 단계별 가이드를 작성했습니다:

- Windows Defender 제외 항목 추가 방법
- 격리된 파일 복구 방법
- Microsoft 오탐 신고 방법
- 소스 코드에서 직접 빌드하는 방법
- VirusTotal 검사 방법
- SHA256 해시 확인 방법

**효과:**
- 비기술 사용자도 쉽게 따라할 수 있는 가이드
- 즉각적인 문제 해결 가능

### 5. 빌드 워크플로우 개선 (.github/workflows/build.yml)

릴리스 프로세스에 다음 기능을 추가했습니다:

```yaml
- name: Calculate checksums
  # SHA256 체크섬 계산 및 파일로 저장
  
- name: Create Release with GitHub CLI
  # 릴리스 노트에 보안 안내 추가
  # SHA256 체크섬 포함
  # VirusTotal 검사 안내
  # checksums.txt 파일 첨부
```

**효과:**
- 사용자가 파일 무결성을 확인할 수 있음
- 릴리스 페이지에서 바로 보안 정보 확인 가능
- 투명한 빌드 프로세스

### 6. GitHub Issue 템플릿 추가

두 가지 이슈 템플릿을 추가했습니다:

1. **false-positive.md**: Windows Defender 오탐 신고 전용
2. **bug_report.md**: 일반 버그 리포트

**효과:**
- 구조화된 문제 보고
- 오탐 사례 데이터 수집
- Microsoft 신고를 위한 집단 행동 조정 가능

### 7. README 업데이트

메인 README에 보안 관련 문서 링크를 추가했습니다:

```markdown
> 🛡️ **보안 안내:** Windows Defender 오탐 관련 안내는 [SECURITY.md](SECURITY.md)를 참조하세요!
> 🔧 **오탐 해결:** Windows Defender 오탐 해결 방법은 [WINDOWS_DEFENDER_FIX.md](WINDOWS_DEFENDER_FIX.md)를 참조하세요!
```

**효과:**
- 사용자가 바로 도움말을 찾을 수 있음
- 문제 발생 시 자가 해결 가능

## 추가로 고려할 사항

### 단기 (즉시 가능)

1. ✅ **결정론적 빌드 활성화**: 이미 적용됨
2. ✅ **상세한 메타데이터**: 이미 적용됨
3. ✅ **문서화**: 이미 적용됨
4. ✅ **체크섬 제공**: 이미 적용됨

### 중기 (커뮤니티 참여 필요)

1. **Microsoft에 집단 신고**
   - 더 많은 사용자가 Microsoft에 오탐을 신고할수록 빠르게 해결됩니다
   - Issue 템플릿을 통해 신고 사례를 수집하고 추적할 수 있습니다

2. **VirusTotal 자동 스캔**
   - GitHub Actions에서 자동으로 VirusTotal에 업로드
   - 릴리스 노트에 VirusTotal 링크 자동 포함
   - (VirusTotal API 키 필요)

### 장기 (비용 발생)

1. **코드 서명 인증서 구매**
   - EV (Extended Validation) 코드 서명 인증서 구매
   - 연간 비용: 약 $300-$500
   - 효과: Windows SmartScreen 및 Defender 신뢰도 크게 향상
   - 단점: 개인 오픈소스 프로젝트에서는 비용 부담

2. **Azure SignTool 사용**
   - 클라우드 기반 코드 서명
   - 월 비용: 변동적
   - 효과: 코드 서명 인증서와 동일

## 예상 효과

### 즉각적 효과

1. **메타데이터 개선**
   - 일부 휴리스틱 분석 엔진에서 신뢰도 점수 향상
   - 실행 파일 정보 창에서 제품 정보 확인 가능

2. **사용자 교육**
   - 사용자가 스스로 문제를 해결할 수 있음
   - 오탐에 대한 이해도 향상

### 중장기 효과

1. **Microsoft 신고 누적**
   - 더 많은 신고가 접수되면 Microsoft가 오탐 패턴 조정
   - 향후 업데이트에서 자동으로 해결될 가능성

2. **커뮤니티 신뢰**
   - 투명한 보안 정보 제공으로 프로젝트 신뢰도 향상
   - 오픈소스 특성 강조

## 결론

이번 변경 사항은 Windows Defender 오탐 문제를 **완전히 제거하지는 못하지만**, 다음과 같은 개선을 제공합니다:

1. ✅ 사용자가 즉시 문제를 해결할 수 있는 방법 제공
2. ✅ 프로그램의 안전성과 투명성 입증
3. ✅ 장기적으로 오탐 가능성을 줄이는 기반 마련
4. ✅ 전문적이고 신뢰할 수 있는 프로젝트 이미지 구축

**코드 서명 없이는 완전한 해결이 어렵지만**, 현재 적용한 방법들은 비용 없이 최대한의 개선을 제공합니다.

## 관련 문서

- [SECURITY.md](SECURITY.md) - 보안 및 오탐 안내
- [WINDOWS_DEFENDER_FIX.md](WINDOWS_DEFENDER_FIX.md) - 단계별 해결 가이드
- [.github/ISSUE_TEMPLATE/false-positive.md](.github/ISSUE_TEMPLATE/false-positive.md) - 오탐 신고 템플릿
