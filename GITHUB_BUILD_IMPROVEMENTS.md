# GitHub Build 바이러스 오탐 개선 사항

## 문제 요약

**현상:**
- 로컬에서 `dotnet build` 명령어로 생성된 파일은 Windows 11 Defender에서 바이러스로 검출되지 않음
- GitHub Actions 빌드로 생성된 ZIP 파일과 압축 해제한 DLL/EXE 파일은 바이러스로 인식됨

**원인 분석:**
1. **서명되지 않은 바이너리**: 코드 서명 인증서 없음
2. **빌드 환경 차이**: GitHub Actions와 로컬 환경의 미묘한 차이
3. **메타데이터 부족**: PE 파일 헤더에 충분한 식별 정보 부족
4. **압축 아카이브 메타데이터**: ZIP 파일 생성 시 메타데이터 차이

## 적용된 해결 방법

### 1. 프로젝트 파일 개선 (UnlockOpenFile.csproj)

#### 1.1 추가 PE 메타데이터
```xml
<!-- Additional metadata for better PE identification and trust -->
<ApplicationManifestPath>app.manifest</ApplicationManifestPath>
<Win32Resource></Win32Resource>
<NoWin32Manifest>false</NoWin32Manifest>
<Trademark>UnlockOpenFile</Trademark>
<PackageTags>file-management;windows;excel;csv;file-lock;open-source</PackageTags>
```

**효과:**
- PE 파일 헤더에 더 많은 식별 정보 포함
- Windows가 파일의 출처와 목적을 더 잘 이해
- Trademark 정보로 정품 소프트웨어임을 표시

#### 1.2 빌드 재현성 개선
```xml
<!-- Ensure consistent builds across environments -->
<PathMap>$(MSBuildProjectDirectory)=/_/</PathMap>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
```

**효과:**
- GitHub Actions와 로컬 빌드가 더 일관된 바이너리 생성
- 경로 정보를 표준화하여 결정론적 빌드 달성
- 소스 코드 임베딩으로 디버깅 정보 향상

#### 1.3 추가 컨텐츠 포함
```xml
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
```

**효과:**
- SingleFile 빌드에 모든 리소스 완전히 포함
- PE 구조가 더 완전하고 정상적으로 보임

### 2. GitHub Actions 워크플로우 개선 (.github/workflows/build.yml)

#### 2.1 SBOM (Software Bill of Materials) 생성
```yaml
- name: Generate SBOM (Software Bill of Materials)
  run: |
    dotnet tool install --global Microsoft.Sbom.DotNetTool
    sbom-tool generate -b ./bin/Release -bc ./UnlockOpenFile.csproj ...
```

**효과:**
- 소프트웨어 구성 요소 명세서 생성
- 투명한 의존성 관리
- 보안 감사 추적 가능

#### 2.2 빌드에 소스 커밋 정보 포함
```yaml
dotnet publish ... -p:SourceRevisionId=${{ github.sha }}
```

**효과:**
- 바이너리에 정확한 소스 커밋 해시 포함
- 빌드 재현성 검증 가능
- 바이너리와 소스 코드 매핑 명확화

#### 2.3 BUILD_INFO.txt 파일 생성
각 빌드에 다음 정보를 포함한 텍스트 파일 추가:
- 빌드 날짜 및 시간
- 소스 커밋 해시
- 빌드 환경 정보
- 바이너리 SHA256 해시
- 검증 링크 (소스 코드, 빌드 워크플로우)

**효과:**
- ZIP 파일을 열었을 때 즉시 빌드 정보 확인 가능
- 사용자가 바이너리 진위 확인 가능
- 투명성 향상

#### 2.4 개선된 ZIP 압축
```powershell
Compress-Archive -Path ./publish/* -DestinationPath ... -CompressionLevel Optimal -Force
```

**효과:**
- 최적 압축 레벨 사용
- 더 일관된 아카이브 생성
- 메타데이터 깨끗하게 유지

#### 2.5 바이너리 해시 추가
ZIP 파일뿐만 아니라 내부 바이너리의 SHA256도 제공:
```
ZIP Archives:
- UnlockOpenFile-vX.X.X-standalone.zip: [hash]
- UnlockOpenFile-vX.X.X.zip: [hash]

Binaries (inside ZIPs):
- Standalone UnlockOpenFile.exe: [hash]
- Framework-dependent UnlockOpenFile.exe: [hash]
```

**효과:**
- ZIP 압축 전후 파일 무결성 검증 가능
- 더 세밀한 보안 검증
- 사용자가 압축 해제 후 바이너리 확인 가능

#### 2.6 릴리스 노트 개선
빌드 투명성 섹션 추가:
- 정확한 소스 커밋 링크
- 빌드 워크플로우 파일 링크
- GitHub Actions 로그 링크
- 자체 빌드 재현 방법

**효과:**
- 완전한 빌드 추적성
- 사용자가 직접 검증 가능
- 오픈소스 신뢰도 향상

### 3. 왜 이런 변경들이 도움이 되는가?

Windows Defender와 다른 안티바이러스는 휴리스틱 분석을 사용하여 파일을 평가합니다:

#### 3.1 시그널 개선
- **더 많은 메타데이터** = 더 신뢰할 수 있는 파일로 보임
- **일관된 빌드** = 의심스러운 변동성 감소
- **투명한 출처** = 검증 가능한 소프트웨어

#### 3.2 PE 구조 개선
- **완전한 PE 헤더** = 정상적인 애플리케이션 구조
- **임베디드 메타데이터** = Windows 파일 속성에서 확인 가능
- **Trademark/Tags** = 정품 소프트웨어 식별자

#### 3.3 빌드 재현성
- **PathMap** = 빌드 경로 차이 제거
- **SourceRevisionId** = 정확한 소스 추적
- **Deterministic** = 동일한 입력 → 동일한 출력

### 4. 한계 및 추가 고려사항

#### 4.1 여전히 남아있는 한계
- **코드 서명 없음**: 가장 효과적인 해결책이지만 비용 발생 (연간 $300-500)
- **경량 빌드 특성**: SingleFile + 작은 크기 = 여전히 의심받을 수 있음

#### 4.2 추가 개선 가능 사항
1. **코드 서명 인증서 구매** (비용 필요)
   - EV (Extended Validation) 인증서 권장
   - SmartScreen 및 Defender 신뢰도 크게 향상
   
2. **자동 VirusTotal 제출** (API 키 필요)
   - 릴리스 시 자동으로 VirusTotal 스캔
   - 릴리스 노트에 VirusTotal 링크 포함
   
3. **Microsoft 자동 신고** (API 필요)
   - 새 릴리스마다 Microsoft에 자동 제출
   - 오탐 데이터베이스 업데이트 가속화

### 5. 예상 효과

#### 5.1 즉각적 효과
- ✅ 빌드 재현성 향상
- ✅ PE 메타데이터 풍부화
- ✅ 사용자 검증 용이성 증가
- ✅ 투명성 및 신뢰도 향상

#### 5.2 중장기 효과
- ⚠️ 일부 휴리스틱 점수 개선 (완전하지는 않음)
- ⚠️ 새로운 빌드 환경 표준화로 오탐 가능성 감소
- ⚠️ 사용자 교육 및 자가 검증 능력 향상

### 6. 권장 사항

#### 6.1 사용자
- 🌟 **Standalone 빌드 사용** (가장 안전, 오탐 없음)
- 🔧 경량 빌드 사용 시: Windows Defender 제외 설정
- ✅ 다운로드 후 BUILD_INFO.txt 및 checksums.txt 확인

#### 6.2 개발자
- 📝 모든 릴리스에서 checksums.txt 제공
- 🔍 VirusTotal에 수동 업로드하여 검사 결과 모니터링
- 📢 Microsoft에 오탐 신고 (https://www.microsoft.com/en-us/wdsi/filesubmission)

### 7. 테스트 및 검증

#### 7.1 로컬 빌드 테스트
```bash
# 재현 가능한 빌드 테스트
git clone https://github.com/trollgameskr/UnlockOpenFile.git
cd UnlockOpenFile
git checkout <commit-hash>
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish

# SHA256 확인
Get-FileHash -Algorithm SHA256 "./publish/UnlockOpenFile.exe"
```

#### 7.2 GitHub Actions 빌드 비교
1. 로컬에서 빌드
2. GitHub Actions에서 빌드
3. 두 바이너리의 SHA256 비교
4. (이상적으로) 동일해야 하지만 일부 차이 존재 가능

### 8. 결론

이번 개선 사항은:
- ✅ **빌드 투명성 대폭 향상**
- ✅ **PE 메타데이터 개선으로 신뢰도 증가**
- ✅ **사용자 검증 용이성 향상**
- ⚠️ **오탐 가능성 일부 감소** (완전 해결은 아님)

**근본적인 해결을 위해서는:**
- 코드 서명 인증서 필요 (비용 발생)
- 또는 Standalone 빌드 사용 권장

**현재 개선 사항은:**
- 비용 없이 최대한의 개선 제공
- 빌드 품질 및 투명성 향상
- 사용자가 직접 검증할 수 있는 도구 제공

## 관련 문서
- [SECURITY.md](SECURITY.md)
- [WINDOWS_DEFENDER_FIX.md](WINDOWS_DEFENDER_FIX.md)
- [FALSE_POSITIVE_MITIGATION_SUMMARY.md](FALSE_POSITIVE_MITIGATION_SUMMARY.md)
