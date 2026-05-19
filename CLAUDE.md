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
@.claude/rules/08-enum-key-naming.md
@.claude/rules/09-common-enum-single-file.md
@.claude/rules/10-common-interface-single-file.md
@.claude/rules/11-use-chvj-ui-components.md
@.claude/rules/12-use-chvj-pool-for-all-spawns.md
@.claude/rules/13-uiarg-with-uibase.md

## 요약 (Quick Reference)

1. **자동 커밋 금지 / 커밋 메시지 포맷** — 커밋 실행 X, 메시지(안)만 전달. 포맷: `# [주제] - 커밋 메시지` (한글, 주제는 `[feat]/[fix]/[refactor]/[chore]/[docs]/[test]/[style]/[perf]/[asset]/[infra]`)
2. **주석은 `//#`** — 단일 라인 주석 접두어 통일
3. **종속성 최소화** — 인터페이스/주입 우선, 구체 참조/싱글톤 직접 호출 지양
4. **반복 에셋 프리팹화** — 2회 이상 반복 시 프리팹, 변형은 Variant
5. **MVVM 적용** — View ↔ ViewModel ↔ Model 단방향, View에 로직 금지
6. **상위 스크립트는 인터페이스로** — `GetComponentInParent<IXxx>()` 사용
7. **ChvjPackage 기준** — 신규 작업 전 패키지 내 기능 우선 확인, 공통 기능은 패키지에 추가
8. **Enum 키 = 에셋명** — `CHMResource`/`CHMUI`는 `Enum.ToString()` 으로 로드. 프리팹/씬/SO 파일명은 Enum 값명과 정확히 일치(대소문자 포함). 카테고리별 Enum 분리(`EUI`, `EMonster`, `EStats` …)
9. **공용 Enum 단일 파일** — 여러 시스템에서 참조되는 Enum은 `Assets/_Lair/Scripts/Data/CommonEnum.cs` 한 파일에 통합. 카테고리별 Enum 자체는 분리 유지(Rule 08), *파일*만 하나
10. **공용 Interface 단일 파일** — 여러 시스템에서 참조되는 Interface는 도메인별 `CommonInterface.cs` 한 파일에 통합 (예: `Scripts/Character/CommonInterface.cs` — IMover/IHealth/IAttacker/ITargetProvider). Interface 자체는 카테고리별 분리 유지, *파일*만 하나
11. **ChvjPackage UI 래퍼 우선** — `CHText`(+TMP_Text), `CHButton`(+Button), `CHToggle`(+Toggle), `CHPoolingScrollView` 사용. Legacy UI Text / 단일 Button 직접 사용 지양. TMP 의존 (com.unity.ugui 2.0+)
12. **모든 스폰은 CHMPool** — 런타임 GameObject 생성은 `CHMPool.Pop` / `Push` 페어. `Instantiate` / `CreatePrimitive` 직접 호출 금지. 사망/만료 시 `Push`, 사전 워밍은 `CreatePool`. OnEnable/OnDisable 로 state reset 필수
13. **UIArg 는 같은 파일에** — `UIArg` 파생(예: `BattleHudArg`)은 페어인 `UIBase` 파생(`BattleHud.cs`)의 상단에 정의. 별도 `XxxArg.cs` 파일 금지
