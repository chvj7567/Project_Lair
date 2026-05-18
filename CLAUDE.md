# Project_Lair — 작업 룰

이 프로젝트의 모든 작업은 아래 룰을 **반드시 준수**한다.
각 룰은 `.claude/rules/` 하위에 개별 파일로 관리된다.

## 기준 패키지
- `Packages/com.chvj.unityinfra` (ChvjPackage)

## 룰 목록

@.claude/rules/01-no-auto-commit.md
@.claude/rules/02-comment-prefix.md
@.claude/rules/03-loose-coupling.md
@.claude/rules/04-prefab-repeated-assets.md
@.claude/rules/05-mvvm-pattern.md
@.claude/rules/06-interface-for-parent.md
@.claude/rules/07-base-on-chvj-package.md

## 요약 (Quick Reference)

1. **자동 커밋 금지 / 커밋 메시지 포맷** — 커밋 실행 X, 메시지(안)만 전달. 포맷: `# [주제] - 커밋 메시지` (한글, 주제는 `[feat]/[fix]/[refactor]/[chore]/[docs]/[test]/[style]/[perf]/[asset]/[infra]`)
2. **주석은 `//#`** — 단일 라인 주석 접두어 통일
3. **종속성 최소화** — 인터페이스/주입 우선, 구체 참조/싱글톤 직접 호출 지양
4. **반복 에셋 프리팹화** — 2회 이상 반복 시 프리팹, 변형은 Variant
5. **MVVM 적용** — View ↔ ViewModel ↔ Model 단방향, View에 로직 금지
6. **상위 스크립트는 인터페이스로** — `GetComponentInParent<IXxx>()` 사용
7. **ChvjPackage 기준** — 신규 작업 전 패키지 내 기능 우선 확인, 공통 기능은 패키지에 추가
