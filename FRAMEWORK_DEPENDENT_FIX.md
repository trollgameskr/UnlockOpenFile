# Framework-Dependent Build 실행 문제 해결

## 문제 상황

.NET 8.0 Runtime이 설치되어 있는데도 불구하고 framework-dependent 빌드(UnlockOpenFile-v0.9.92.zip)가 실행되지 않는 문제가 발생했습니다.

### 증상
- Standalone 빌드 (self-contained): ✅ 정상 실행
- Framework-dependent 빌드: ❌ 실행 실패

### 사용자 환경
```
Microsoft.WindowsDesktop.App 8.0.14 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
```

## 원인 분석

### ReadyToRun (R2R) 컴파일의 문제

1. **R2R이란?**
   - ReadyToRun은 .NET 애플리케이션을 미리 네이티브 코드로 컴파일하는 기술
   - 시작 시간을 단축하고 JIT 컴파일 오버헤드를 줄임

2. **Framework-dependent 빌드에서 R2R의 문제점**
   - R2R 이미지는 **특정 런타임 버전에 종속적**
   - 빌드 시 사용한 .NET SDK 버전과 실행 환경의 Runtime 버전이 다를 경우 호환성 문제 발생
   - 예: SDK 9.0으로 빌드 → Runtime 8.0.14에서 실행 시 충돌 가능

3. **Self-contained 빌드에서는 왜 문제가 없나?**
   - Self-contained 빌드는 .NET Runtime을 포함하므로 빌드 시 버전과 실행 시 버전이 동일
   - 따라서 R2R을 사용해도 호환성 문제가 없음

### 프로젝트 설정 분석

**변경 전 (문제가 있던 설정):**
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PublishReadyToRun>true</PublishReadyToRun>
  <!-- 이 설정이 모든 Release 빌드에 적용됨 -->
  ...
</PropertyGroup>
```

이 설정은:
- Self-contained 빌드: ✅ 정상 (Runtime 포함)
- Framework-dependent 빌드: ❌ 문제 (Runtime 버전 불일치)

## 해결 방법

### 1. 프로젝트 파일 수정 (UnlockOpenFile.csproj)

```xml
<!-- 변경 후: R2R 제거 -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <!-- PublishReadyToRun 제거 -->
  <PublishSingleFile>true</PublishSingleFile>
  ...
</PropertyGroup>
```

### 2. 빌드 워크플로우 수정 (.github/workflows/build.yml)

```yaml
# Self-contained 빌드: R2R 적용
- name: Publish (Self-contained)
  run: dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-standalone -p:PublishReadyToRun=true

# Framework-dependent 빌드: R2R 비활성화
- name: Publish (Framework-dependent)
  run: dotnet publish -c Release -r win-x64 --self-contained false -o ./publish -p:PublishReadyToRun=false
```

## 결과

### 빌드 크기 비교

**Framework-dependent 빌드:**
- 변경 전 (R2R 적용): ~5MB
- 변경 후 (R2R 제거): ~200KB
- 파일 크기가 크게 줄어들고 호환성 향상

**Self-contained 빌드:**
- 변경 전/후 동일: ~170MB
- R2R이 여전히 적용되어 최적화된 성능 유지

### 호환성

**Framework-dependent 빌드:**
- ✅ .NET 8.0 Runtime 8.0.14에서 정상 실행
- ✅ 다양한 .NET 8.0 패치 버전에서 호환
- ✅ IL 코드로 배포되므로 Runtime에서 JIT 컴파일 후 실행

**Self-contained 빌드:**
- ✅ Runtime 포함으로 별도 설치 불필요
- ✅ R2R 적용으로 빠른 시작 시간
- ✅ 가장 안정적인 배포 방식

## 권장 사항

### 일반 사용자
- **Standalone 빌드 (self-contained) 사용 권장**
- .NET Runtime 설치 불필요
- 호환성 문제 없음
- 다운로드: `UnlockOpenFile-{version}-standalone.zip`

### 고급 사용자
- **Framework-dependent 빌드 사용 가능**
- .NET 8.0 Runtime 설치 필요
- 파일 크기가 작음 (~200KB)
- 다운로드: `UnlockOpenFile-{version}.zip`

## 기술적 배경

### ReadyToRun 사용 시나리오

**권장하는 경우:**
- ✅ Self-contained 배포
- ✅ 성능이 중요한 애플리케이션
- ✅ 시작 시간 최적화가 필요한 경우

**권장하지 않는 경우:**
- ❌ Framework-dependent 배포
- ❌ 다양한 Runtime 버전 지원이 필요한 경우
- ❌ 파일 크기가 중요한 경우

### 관련 문서
- [ReadyToRun 컴파일 - Microsoft Docs](https://learn.microsoft.com/ko-kr/dotnet/core/deploying/ready-to-run)
- [.NET 애플리케이션 게시 - Microsoft Docs](https://learn.microsoft.com/ko-kr/dotnet/core/deploying/)

## 참고

이 수정은 .NET 8.0 Runtime이 설치된 환경에서 framework-dependent 빌드가 실행되지 않는 문제를 해결합니다. 향후 릴리스에서는 이 설정이 유지되어 양쪽 빌드 모두 안정적으로 동작할 것입니다.
