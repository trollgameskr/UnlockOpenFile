# Windows Defender 오탐 대응 변경 사항 요약

## 개요

이 문서는 Windows 11 Defender의 바이러스 오탐(False Positive) 문제를 해결하기 위해 적용한 변경 사항을 요약합니다.

## 문제점

v0.9.8 릴리스에서 Windows 11 Defender가 **경량 빌드(framework-dependent build)**를 `Trojan:Script/Wacatac.B!ml`로 탐지하는 문제가 보고되었습니다. 

**특이사항:**
- ❌ **경량 빌드** (UnlockOpenFile-vX.X.X.zip): 바이러스로 탐지됨
- ✅ **Standalone 빌드** (UnlockOpenFile-vX.X.X-standalone.zip): 탐지되지 않음

이는 프로그램의 정상적인 기능(프로세스 모니터링, 파일 시스템 감시, 레지스트리 접근 등)과 경량 빌드의 특성(작은 파일 크기, 동적 로딩, SingleFile 압축)이 일부 보안 프로그램의 휴리스틱 분석에서 의심스러운 패턴으로 인식되기 때문입니다.

## 적용된 해결 방법

### 1. 경량 빌드 최적화 (UnlockOpenFile.csproj) - **NEW in v0.9.10**

경량 빌드의 오탐을 줄이기 위해 다음 설정을 추가했습니다:

```xml
<!-- Additional publish settings to reduce false positives -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <!-- ReadyToRun compilation for native code generation -->
  <PublishReadyToRun>true</PublishReadyToRun>
  <!-- Single file publication for framework-dependent build -->
  <PublishSingleFile>true</PublishSingleFile>
  <!-- Include native libraries in single file -->
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  <!-- Optimize for size while maintaining compatibility -->
  <DebugType>embedded</DebugType>
  <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

**효과:**
- ReadyToRun (R2R) 컴파일로 네이티브 코드 생성 → 더 "정상적"인 실행 파일로 인식
- SingleFile 옵션으로 단일 실행 파일 생성 → DLL 분산 방지, 더 깔끔한 구조
- 임베디드 디버그 심볼 → PE 파일 구조가 더 완전해짐
- 하지만 **여전히 오탐 가능성 존재** (경량 빌드 특성상)

**한계:**
경량 빌드는 근본적으로 작은 크기(약 300KB)와 동적 .NET Runtime 로딩 특성 때문에 휴리스틱 분석에서 의심받을 수 있습니다. 완전한 해결은 어렵습니다.

### 2. Standalone 빌드를 주 배포 버전으로 권장 - **NEW in v0.9.10**

빌드 워크플로우와 릴리스 노트를 업데이트하여:
- Standalone 빌드를 **주 권장 다운로드**로 명시
- 경량 빌드에 대한 **명확한 경고** 추가
- 두 빌드의 차이점과 오탐 가능성 설명

**효과:**
- 사용자가 오탐이 적은 Standalone 빌드를 우선 선택하도록 유도
- 경량 빌드는 고급 사용자 또는 .NET Runtime이 이미 설치된 환경용으로 제공
- 투명한 정보 제공으로 사용자 혼란 방지

### 3. 어셈블리 메타데이터 강화 (UnlockOpenFile.csproj) - **기존 적용됨**

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

### 4. 애플리케이션 매니페스트 개선 (app.manifest) - **기존 적용됨**

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

### 5. 보안 문서 작성 (SECURITY.md) - **업데이트됨**

포괄적인 보안 문서를 작성하여 다음 내용을 포함했습니다:

- 오탐이 발생하는 이유 설명
- **경량 빌드 vs Standalone 빌드 차이점 설명** (NEW)
- **Standalone 빌드 사용 권장** (NEW)
- 안전성 확인 방법 (소스 코드 검토, 자체 빌드, VirusTotal)
- Windows Defender에서 허용하는 방법
- Microsoft에 오탐 신고하는 방법
- 개인정보 보호 정책
- 필요한 권한 목록과 용도

**효과:**
- 사용자에게 투명한 정보 제공
- 오탐 해결을 위한 자가 진단 도구 제공
- Standalone 빌드 사용을 통한 근본적 해결 방법 제시

### 6. 단계별 해결 가이드 작성 (WINDOWS_DEFENDER_FIX.md) - **업데이트됨**

사용자가 즉시 문제를 해결할 수 있도록 상세한 단계별 가이드를 작성했습니다:

- **🌟 Standalone 빌드 사용 권장 (가장 쉬운 방법)** (NEW)
- Windows Defender 제외 항목 추가 방법
- 격리된 파일 복구 방법
- Microsoft 오탐 신고 방법
- 소스 코드에서 직접 빌드하는 방법
- VirusTotal 검사 방법
- SHA256 해시 확인 방법

**효과:**
- 비기술 사용자도 쉽게 따라할 수 있는 가이드
- Standalone 빌드로 전환하여 근본적 해결 가능
- 즉각적인 문제 해결 가능

### 7. 빌드 워크플로우 개선 (.github/workflows/build.yml) - **업데이트됨**

릴리스 프로세스에 다음 기능을 추가했습니다:

```yaml
- name: Calculate checksums
  # SHA256 체크섬 계산 및 파일로 저장
  
- name: Create Release with GitHub CLI
  # 릴리스 노트에 보안 안내 추가
  # ⭐ Standalone 빌드를 주 권장 다운로드로 명시 (NEW)
  # ⚠️ 경량 빌드의 오탐 가능성 명확히 경고 (NEW)
  # 두 빌드의 차이점 상세 설명 (NEW)
  # SHA256 체크섬 포함
  # VirusTotal 검사 안내
  # checksums.txt 파일 첨부
```

**효과:**
- 사용자가 파일 무결성을 확인할 수 있음
- 릴리스 페이지에서 바로 보안 정보 확인 가능
- Standalone 빌드 우선 다운로드 유도
- 경량 빌드 사용자에게 명확한 경고 제공
- 투명한 빌드 프로세스

### 8. GitHub Issue 템플릿 추가 - **기존 적용됨**

두 가지 이슈 템플릿을 추가했습니다:

1. **false-positive.md**: Windows Defender 오탐 신고 전용
2. **bug_report.md**: 일반 버그 리포트

**효과:**
- 구조화된 문제 보고
- 오탐 사례 데이터 수집
- Microsoft 신고를 위한 집단 행동 조정 가능

### 9. README 업데이트 - **기존 적용됨**

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
5. ✅ **빌드 재현성 개선**: NEW - PathMap, EmbedUntrackedSources 적용
6. ✅ **SBOM 생성**: NEW - 소프트웨어 구성 요소 명세서
7. ✅ **BUILD_INFO.txt**: NEW - 각 빌드에 검증 정보 포함

### 중기 (커뮤니티 참여 필요)

1. **Microsoft에 집단 신고**
   - 더 많은 사용자가 Microsoft에 오탐을 신고할수록 빠르게 해결됩니다
   - Issue 템플릿을 통해 신고 사례를 수집하고 추적할 수 있습니다

2. **VirusTotal 자동 스캔**
   - GitHub Actions에서 자동으로 VirusTotal에 업로드
   - 릴리스 노트에 VirusTotal 링크 자동 포함
   - (VirusTotal API 키 필요)

### 장기 (비용 발생 또는 무료 옵션)

1. **코드 서명 인증서** ⭐ 가장 효과적
   - **무료 옵션**: SignPath.io (오픈소스 프로젝트용 무료 코드 서명)
   - **유료 옵션**: EV (Extended Validation) 코드 서명 인증서 구매
   - 연간 비용: $0 (SignPath.io) 또는 $300-$500 (EV 인증서)
   - 효과: Windows SmartScreen 및 Defender 신뢰도 크게 향상
   - 자세한 내용: [CODE_SIGNING_GUIDE.md](CODE_SIGNING_GUIDE.md) 참조

2. **Azure SignTool 사용**
   - 클라우드 기반 코드 서명
   - 월 비용: $5-10
   - 효과: 코드 서명 인증서와 동일

## 예상 효과

### 즉각적 효과

1. **Standalone 빌드 사용 권장** (NEW)
   - 🌟 가장 효과적인 해결 방법
   - Windows Defender 오탐 가능성 매우 낮음
   - 사용자가 별도 설정 없이 안전하게 사용 가능
   - 즉시 다운로드하여 사용 가능

2. **경량 빌드 최적화** (NEW)
   - ReadyToRun 컴파일과 SingleFile로 일부 휴리스틱 점수 향상
   - 하지만 **완전한 해결은 어려움** (근본적 특성 문제)
   - 고급 사용자 또는 .NET Runtime 환경용으로 제공

3. **메타데이터 개선**
   - 일부 휴리스틱 분석 엔진에서 신뢰도 점수 향상
   - 실행 파일 정보 창에서 제품 정보 확인 가능

4. **사용자 교육**
   - 사용자가 스스로 문제를 해결할 수 있음
   - 오탐에 대한 이해도 향상
   - Standalone vs 경량 빌드 선택 가능

### 중장기 효과

1. **Microsoft 신고 누적**
   - 더 많은 신고가 접수되면 Microsoft가 오탐 패턴 조정
   - 향후 업데이트에서 자동으로 해결될 가능성

2. **커뮤니티 신뢰**
   - 투명한 보안 정보 제공으로 프로젝트 신뢰도 향상
   - 오픈소스 특성 강조

## 결론

이번 변경 사항은 Windows Defender 오탐 문제를 다음과 같이 개선합니다:

### 경량 빌드 (Framework-dependent)
- ⚠️ **여전히 오탐 가능성 존재**: Windows Defender가 `Trojan:Script/Wacatac.B!ml`로 탐지할 수 있음
- 🔧 ReadyToRun, SingleFile 등의 최적화 적용으로 일부 개선
- 📝 명확한 경고와 해결 방법 제공
- 👥 .NET Runtime이 이미 설치된 환경의 고급 사용자용

### Standalone 빌드 (Self-contained)
- ✅ **오탐 가능성 매우 낮음**: Windows Defender 탐지 사례 없음
- ✅ 별도 설정 불필요
- ✅ **주 권장 다운로드**로 명시
- ✅ 즉시 사용 가능

### 전체적인 개선 사항

1. ✅ 사용자가 즉시 문제를 해결할 수 있는 방법 제공 (Standalone 빌드)
2. ✅ 프로그램의 안전성과 투명성 입증
3. ✅ 장기적으로 오탐 가능성을 줄이는 기반 마련
4. ✅ 전문적이고 신뢰할 수 있는 프로젝트 이미지 구축
5. ✅ 경량 빌드 최적화로 일부 개선 (완전하지는 않음)

**권장 사항:**
- 🌟 대부분의 사용자: **Standalone 빌드 사용** (오탐 없음)
- 🔧 .NET Runtime 환경 사용자: 경량 빌드 + Windows Defender 제외 설정

**코드 서명 없이는 경량 빌드의 완전한 해결이 어렵지만**, 현재 적용한 방법들은 비용 없이 최대한의 개선을 제공하며, **Standalone 빌드를 통한 근본적인 해결 방법**을 제시합니다.

## 관련 문서

- [SECURITY.md](SECURITY.md) - 보안 및 오탐 안내
- [WINDOWS_DEFENDER_FIX.md](WINDOWS_DEFENDER_FIX.md) - 단계별 해결 가이드
- [.github/ISSUE_TEMPLATE/false-positive.md](.github/ISSUE_TEMPLATE/false-positive.md) - 오탐 신고 템플릿
