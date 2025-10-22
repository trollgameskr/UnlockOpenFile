# 태그 업데이트 가이드 (Tag Update Guide)

## 문제 상황 (Problem)

빌드 워크플로우 파일(`.github/workflows/build.yml`)이 커밋 `2ff29ec`에서 보안 강화를 위해 전체 커밋 SHA로 고정되었습니다. 그러나 기존 태그들(`v0.9.0`, `v0.9.1`, `v0.9.2`)은 이전 커밋 `ee4b8d4`를 가리키고 있어, 이 태그들에 대한 워크플로우 실행 시 업데이트된 워크플로우가 아닌 구버전 워크플로우가 사용됩니다.

GitHub Actions는 태그가 푸시될 때 해당 태그가 가리키는 커밋의 워크플로우 파일을 사용합니다. 따라서 업데이트된 워크플로우를 사용하려면 태그를 최신 커밋으로 업데이트해야 합니다.

## 현재 상태 (Current Status)

- **최신 main 브랜치 커밋**: `63ccc16` (업데이트된 워크플로우 포함)
- **기존 태그들이 가리키는 커밋**: `ee4b8d4` (구버전 워크플로우)
- **문제되는 태그들**: `v0.9.0`, `v0.9.1`, `v0.9.2`

## 해결 방법 (Solution)

기존 태그를 삭제하고 최신 커밋으로 다시 생성해야 합니다. 다음 단계를 따르세요:

### 1. 로컬 태그 삭제

```bash
git tag -d v0.9.0
git tag -d v0.9.1
git tag -d v0.9.2
```

### 2. 원격 태그 삭제

```bash
git push origin :refs/tags/v0.9.0
git push origin :refs/tags/v0.9.1
git push origin :refs/tags/v0.9.2
```

### 3. 최신 main 브랜치로 전환

```bash
git checkout main
git pull origin main
```

### 4. 새 태그 생성

최신 커밋(업데이트된 워크플로우 포함)으로 태그를 다시 생성합니다:

```bash
git tag -a v0.9.0 -m "Release v0.9.0 (updated workflow)"
git tag -a v0.9.1 -m "Release v0.9.1 (updated workflow)"
git tag -a v0.9.2 -m "Release v0.9.2 (updated workflow)"
```

### 5. 새 태그 푸시

```bash
git push origin v0.9.0
git push origin v0.9.1
git push origin v0.9.2
```

또는 모든 태그를 한 번에 푸시:

```bash
git push origin --tags
```

## 확인 방법 (Verification)

태그 업데이트 후, 다음 명령어로 태그가 올바른 커밋을 가리키는지 확인할 수 있습니다:

```bash
git show-ref --tags
```

또는 GitHub에서 각 태그의 커밋을 확인:
- https://github.com/trollgameskr/UnlockOpenFile/tags

## 향후 릴리스 시 (For Future Releases)

향후 새 버전을 릴리스할 때는 항상 최신 main 브랜치에서 태그를 생성하여 업데이트된 워크플로우가 사용되도록 합니다:

```bash
git checkout main
git pull origin main
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

## 주의사항 (Important Notes)

- 태그를 삭제하고 다시 생성하면 기존 릴리스 노트나 아티팩트에 영향을 줄 수 있습니다.
- GitHub Releases를 사용하는 경우, 태그 재생성 후 릴리스를 다시 확인하거나 재생성해야 할 수 있습니다.
- 사용자가 이미 태그를 체크아웃한 경우, 강제 업데이트가 필요할 수 있습니다.

## 참고 (References)

- 워크플로우 업데이트 커밋: `2ff29ec` (Pin GitHub Actions to full commit SHAs for security compliance)
- 최신 main 브랜치 커밋: `63ccc16` (Merge PR #13)
