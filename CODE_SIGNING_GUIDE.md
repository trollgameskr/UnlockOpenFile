# 코드 서명 가이드 (Code Signing Guide)

## 개요

코드 서명은 Windows Defender 및 SmartScreen 오탐 문제를 **근본적으로 해결**하는 가장 효과적인 방법입니다.

## 왜 코드 서명이 필요한가?

### 현재 상황
- ❌ 서명되지 않은 바이너리 = 휴리스틱 분석에서 의심받기 쉬움
- ❌ Windows SmartScreen 경고
- ❌ Windows Defender 오탐 가능성

### 코드 서명 후
- ✅ 신뢰할 수 있는 게시자로 인식
- ✅ SmartScreen 경고 없음
- ✅ Defender 오탐 가능성 대폭 감소
- ✅ 사용자 신뢰도 향상

## 코드 서명 인증서 옵션

### 1. Standard Code Signing Certificate
**비용:** 연간 약 $100-200
**특징:**
- 기본적인 코드 서명
- USB 토큰에 저장
- SmartScreen reputation 점수를 쌓아야 함 (시간 필요)

**권장 제공업체:**
- Sectigo (구 Comodo)
- DigiCert
- GlobalSign

### 2. EV (Extended Validation) Code Signing Certificate ⭐ 권장
**비용:** 연간 약 $300-500
**특징:**
- **즉시 SmartScreen 신뢰 획득**
- USB 토큰 또는 HSM에 저장
- 더 엄격한 신원 검증
- Windows Defender에서 가장 높은 신뢰도

**권장 제공업체:**
- DigiCert EV Code Signing
- Sectigo EV Code Signing
- SSL.com EV Code Signing

### 3. Azure Key Vault (클라우드 서명)
**비용:** 월 약 $5-10 + 서명당 소액
**특징:**
- 클라우드 기반 HSM
- GitHub Actions와 쉽게 통합
- 인증서 관리 간편

## 개인 오픈소스 프로젝트를 위한 대안

### 1. 무료 코드 서명 프로그램
일부 회사들이 오픈소스 프로젝트에 무료 또는 할인된 인증서 제공:

- **SignPath.io** (https://signpath.io)
  - 오픈소스 프로젝트에 무료 코드 서명 제공
  - GitHub Actions 통합
  - 요구사항: OSI 승인 라이선스, 공개 저장소

### 2. 후원 받기
- GitHub Sponsors 활성화
- 코드 서명 비용을 후원 목표로 설정
- 커뮤니티에서 지원 받기

### 3. 회사/조직 등록
- 개인이 아닌 회사/조직으로 등록하면 일부 할인 가능
- 여러 프로젝트에 동일 인증서 사용 가능

## 코드 서명 구현 방법

### 1. 인증서 구매 및 설치

1. 위 제공업체 중 하나에서 인증서 구매
2. 신원 확인 절차 완료 (회사 등록증, 신분증 등)
3. USB 토큰 또는 인증서 파일 수령

### 2. GitHub Actions에서 코드 서명

#### 옵션 A: Azure Key Vault 사용
```yaml
- name: Setup Azure Code Signing
  uses: azure/login@v1
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}

- name: Sign executables
  run: |
    azuresigntool sign -kvu "${{ secrets.AZURE_KEY_VAULT_URL }}" `
      -kvi "${{ secrets.AZURE_CLIENT_ID }}" `
      -kvs "${{ secrets.AZURE_CLIENT_SECRET }}" `
      -kvc "${{ secrets.AZURE_CERT_NAME }}" `
      -tr http://timestamp.digicert.com `
      -v ./publish/UnlockOpenFile.exe
```

#### 옵션 B: SignPath.io 사용 (무료 오픈소스)
```yaml
- name: Sign with SignPath
  uses: signpath/github-action-submit-signing-request@v1
  with:
    api-token: ${{ secrets.SIGNPATH_API_TOKEN }}
    organization-id: ${{ secrets.SIGNPATH_ORGANIZATION_ID }}
    project-slug: 'UnlockOpenFile'
    signing-policy-slug: 'release-signing'
    artifact-configuration-slug: 'exe-signing'
    github-artifact-id: ${{ steps.upload.outputs.artifact-id }}
```

#### 옵션 C: 로컬 인증서 파일 사용
```yaml
- name: Decode certificate
  run: |
    $bytes = [Convert]::FromBase64String("${{ secrets.CERTIFICATE_BASE64 }}")
    [IO.File]::WriteAllBytes("cert.pfx", $bytes)

- name: Sign executables
  run: |
    & "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe" sign `
      /f cert.pfx `
      /p "${{ secrets.CERTIFICATE_PASSWORD }}" `
      /tr http://timestamp.digicert.com `
      /td sha256 `
      /fd sha256 `
      ./publish/UnlockOpenFile.exe
      
- name: Remove certificate
  if: always()
  run: Remove-Item cert.pfx
```

### 3. Timestamp 서버

코드 서명 시 반드시 timestamp를 포함해야 합니다:
- 인증서 만료 후에도 서명이 유효함
- 무료 timestamp 서버:
  - http://timestamp.digicert.com
  - http://timestamp.sectigo.com
  - http://timestamp.globalsign.com

### 4. 검증

서명 후 확인:
```powershell
# 서명 확인
Get-AuthenticodeSignature ./publish/UnlockOpenFile.exe

# 상세 정보
signtool verify /pa /v ./publish/UnlockOpenFile.exe
```

## 예상 비용

### 최소 비용 (무료 옵션)
- SignPath.io: **$0/년**
- 요구사항: OSI 라이선스, 공개 저장소

### 표준 비용
- Standard Code Signing: **$100-200/년**
- EV Code Signing: **$300-500/년**
- Azure Key Vault: **$60-120/년** (월 $5-10)

### 추가 고려사항
- USB 토큰: $20-50 (일회성, 보통 포함)
- HSM: 인증서 가격에 포함

## UnlockOpenFile에 적용하기

### 단계별 계획

1. **평가 단계** (현재)
   - ✅ 메타데이터 개선 완료
   - ✅ 빌드 재현성 개선 완료
   - ✅ 문서화 완료

2. **무료 옵션 시도** (권장 1단계)
   - SignPath.io 신청
   - GitHub Actions 통합
   - 테스트 릴리스 생성

3. **유료 인증서 고려** (필요시)
   - GitHub Sponsors 활성화
   - 후원 목표: 코드 서명 인증서 비용
   - EV 인증서 구매

### SignPath.io 신청 방법

1. https://signpath.io/open-source 방문
2. 프로젝트 정보 제공:
   - 프로젝트 이름: UnlockOpenFile
   - 라이선스: MIT
   - 저장소: https://github.com/trollgameskr/UnlockOpenFile
   - 설명: Windows file management utility

3. 승인 후 GitHub Actions 통합

## 예상 효과

### 코드 서명 전
- ⚠️ Windows Defender 오탐 가능 (경량 빌드)
- ⚠️ SmartScreen 경고
- ⚠️ "알 수 없는 게시자" 표시

### 코드 서명 후
- ✅ Windows Defender 신뢰도 높음
- ✅ SmartScreen 경고 없음 (EV 인증서)
- ✅ "검증된 게시자: trollgameskr" 표시
- ✅ 사용자 신뢰도 대폭 향상

## 참고 자료

- [Microsoft Code Signing Guide](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/code-signing-cert-manage)
- [SignPath.io Documentation](https://signpath.io/documentation)
- [Azure Code Signing](https://docs.microsoft.com/en-us/azure/key-vault/certificates/)
- [DigiCert Code Signing](https://www.digicert.com/signing/code-signing-certificates)

## 현재 권장 사항

1. **즉시 실행**: SignPath.io 무료 옵션 신청 ⭐
2. **중기**: GitHub Sponsors 활성화
3. **장기**: 후원금으로 EV 인증서 구매

## 결론

코드 서명은 초기 비용이 있지만:
- 🎯 오탐 문제를 근본적으로 해결
- 🎯 사용자 경험 크게 향상
- 🎯 프로젝트 전문성 향상

**현재 가장 실용적인 방법:**
- SignPath.io 무료 서명 활용
- 비용 없이 전문적인 코드 서명 획득
