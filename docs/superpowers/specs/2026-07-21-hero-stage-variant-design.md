# 스켈레톤 영웅 5스테이지 재스킨 시스템 — Design Spec

- **날짜**: 2026-07-21
- **단계**: v0.3
- **작성**: brainstorming (메인 오케스트레이터)
- **다음 단계**: writing-plans → game-designer(도메인 수치) → 파이프라인

---

## 1. 의도 (Intent)

던전을 공략하러 오는 **영웅**은 현재 해골(skeleton) 모델 하나(`EHero.Knight` → `Knight.prefab`)뿐이다. 신규 영웅 리소스 제작 없이(CLAUDE.md §8 "신규 영웅 리소스 제작 금지" 준수), **같은 스켈레톤 모델 하나를 셰이더/머터리얼·Transform 레벨에서 재스킨**하여 스테이지마다 "다른 적"으로 보이게 한다. 외형뿐 아니라 스탯도 스테이지가 올라갈수록 강해지는 5단계 난이도 곡선을 만든다.

역방향 보스전 컨셉상 **스테이지 = 새로운 영웅 1명**이며, 플레이어는 매 스테이지 그 영웅을 처치한다.

## 2. 범위 (Scope)

이번 사이클에서 **A+B+C 모두** 구현한다:

- **A. 비주얼 재스킨 레이어** — 스테이지별 외형 변형 데이터 + 스폰 시 적용 로직
- **B. 스테이지 선택 + 순차 해금** — 마을 허브에서 스테이지 선택, 클리어 시 다음 해금
- **C. 스테이지별 스탯 차등** — 배수(multiplier) 방식으로 HP·공격력 스케일링

### 2.1 스코프 근거 (design-reviewer 확인용)

**스테이지 시스템은 v0.3 spec(`docs/superpowers/specs/2026-06-10-village-meta-hub-design.md`) 에 명시된 범위가 아니다.** CLAUDE.md §8 은 "범위 밖 기능은 game-designer 가 명시적으로 승격하기 전까지 착수하지 않는다" 고 규정한다. 본 작업은 **사용자가 대화 중 명시적으로 스테이지 시스템 도입을 요청·승격**하여 착수한다. design-reviewer 는 이 근거를 전제로 내부 일관성·밸런스만 검토하고, "범위 이탈" 자체는 사용자 승격으로 해소된 것으로 처리한다.

## 3. 스테이지별 외형 스펙 (mechanism outline)

한 스켈레톤 모델에 아래 기법을 누적 적용해 실루엣·색을 차별화한다.

| 스테이지 | 적용 기법 | 의도한 인상 |
|---|---|---|
| 1 | **A** 색상 틴트 | 기본 색 변형 |
| 2 | **A+B** 틴트 + 아웃라인 | 실루엣 강조 |
| 3 | **A+C** 틴트 + 이미시브 발광 | 발광하는 적 |
| 4 | **A+B+C** 틴트 + 아웃라인 + 발광 | 정예 |
| 5 | **A+B+C+D** 전부 + 스케일 확대 | 보스급 거대 |

**기법 정의:**

- **A. 색상 틴트** — URP Lit `_BaseColor` 교체. 머터리얼 레벨, 신규 셰이더 불필요.
- **B. 아웃라인/실루엣** — 인버티드-헐(inverted-hull) 방식 **신규 셰이더 1개**. 스켈레톤 메시에 두 번째 서브머터리얼로 부착. **버텍스 스테이지에서 노멀 방향 팽창** → 스키닝(뼈 애니메이션)을 따라가야 한다(정적 메시용 아웃라인이 아님).
- **C. 이미시브 발광** — URP Lit `_EmissionColor` + intensity. 머터리얼 레벨, 신규 셰이더 불필요. `HitFlash` 는 `_BaseColor` 만 만지므로 발광과 충돌하지 않는다.
- **D. 스케일 확대** — 영웅 root Transform 스케일 배수. 셰이더 불필요.

**구체 색상/발광값/스케일 수치는 game-designer 가 컨셉서 §11.4 비주얼 매핑 기준으로 설계한다.**

## 4. 데이터 구조 (SO 정본)

최근 커밋(`밸런스를 다시 SO 정본으로 롤백`)의 방향에 맞춰 **ScriptableObject 를 정본**으로 둔다.

- **`HeroStageVariantConfig`** (신규 SO, 5 엔트리) — 각 스테이지:
  - 외형: `tintColor` / `useOutline` + `outlineColor` / `useEmission` + `emissionColor` + `emissionIntensity` / `scaleMultiplier`
  - 스탯 배수: `hpMultiplier` / `powerMultiplier`
- 스테이지 식별은 **`int`(1~5)** 로만 한다. 새 addressable 을 로드하지 않으므로 `EStage` enum 은 만들지 않는다 — Rule 03 §2 의 "Enum 값명 = 에셋 파일명" 이름매칭 의무를 무의미하게 부르지 않기 위함.
- 스탯 배수의 구체값은 game-designer 가 컨셉서 §8 밸런싱 기준에 맞춰 설계(예시 곡선: HP ×1.0/1.3/1.6/2.0/3.0 — 확정값 아님).

## 5. 적용 컴포넌트 — `HeroStageVariantApplier`

Knight 프리팹에 부착. 스폰 시 현재 스테이지의 variant 데이터를 받아 적용한다.

- 자식 Renderer 의 `_BaseColor` 틴트, `_EmissionColor` 발광, 아웃라인 서브머터리얼 on/off, root 스케일 적용.

### 5.1 ⚠️ 색상 시스템 충돌 방지 (최우선 correctness 이슈)

기존 `HitFlash` 는:
- `Awake` 의 `CacheRenderers()` 에서 현재 `_BaseColor` 를 "원본" 으로 스냅샷 (Awake 에서만, 재실행 없음).
- 피격/공격 flash 후 `RestoreOriginalColors()` 로 그 스냅샷을 되돌림.
- **풀 재사용 시 `OnEnable` 에서도** `RestoreOriginalColors()` 호출.

따라서 variant 틴트를 별도 채널/시점에 적용하면 **첫 피격 flash 후 원복 시 틴트가 지워지고, 풀 재사용마다 사라진다.** `AttackJuice` 도 같은 경로다.

**해결 규칙:**
1. variant 틴트는 `HitFlash` 와 **동일 채널**(`.material` 인스턴스의 `_BaseColor`)로 쓴다. `MaterialPropertyBlock` 을 쓰지 않는다 — material-instance 쓰기와 조합이 어긋난다.
2. **적용 순서**: variant 틴트 적용 → `HitFlash` 가 그 색을 "원본" 으로 (재)캐시하도록 보장한다. 즉 HitFlash 의 restore 타깃이 variant 색이 되게 한다 (variant 적용 후 캐시, 또는 HitFlash 에 variant 색을 원본으로 주입).
3. 이 조정은 `HitFlash` 레이어에서 한 번에 해결하면 `AttackJuice` 도 함께 커버된다.

## 6. 스테이지 선택 & 해금 (범위 B)

- **`MetaProfile`** 에 `SelectedStage`(int) / `ClearedStage`(int) 추가.
- 마을에 **스테이지 선택 팝업** — 기존 `HeroSelectPopup` 패턴(UIBase + CHPoolingScrollView, Rule 03 §3 BuildModalPopup 구조)을 복제.
  - 5 슬롯. `ClearedStage + 1` 까지 해금, 그 이상은 잠금 슬롯 표시(CLAUDE.md §8 "잠금 슬롯" 정책과 결).
  - 선택 시 `SelectedStage` 저장 후 Battle 씬 로드 (기존 `HandleHeroSelected` 흐름과 동형).
- **`BattleController`**: 영웅 로드는 `EHero.Knight` 그대로. `SelectedStage` 를 읽어 스폰 시 `HeroStageVariantApplier` 로 외형 적용 + HP/공격력 배수 반영.
- **승리 시**: `ClearedStage = max(ClearedStage, SelectedStage)` 갱신 → 다음 스테이지 해금.

### 6.1 스테이지 5 종점 처리

스테이지 5(마지막) 클리어 시 **5가 종점**이다. 더 이상 해금할 스테이지가 없으며 "전체 클리어" 상태로 처리한다(추가 스테이지·무한 스케일링 없음). 5 재도전은 허용하되 새 해금은 발생하지 않는다.

## 7. 영향받는 파일 (구현 단계 입력용, 확정 아님)

- 신규: `HeroStageVariantConfig`(SO 스크립트+asset), `HeroStageVariantApplier.cs`, 아웃라인 셰이더 1개, 스테이지 선택 팝업 3-class(Panel/ScrollView/Cell) + 프리팹.
- 수정: `MetaProfile.cs`(필드 추가), `BattleController.cs`(스테이지 읽기+variant 적용), `HitFlash.cs`(variant 색 원본 처리), `VillageController.cs`(스테이지 팝업 진입), `CommonEnum.cs`(EUI 에 스테이지 팝업 키 추가).

## 8. 검증 기준 (성공 조건)

- 스테이지 1~5 각각에서 영웅이 스펙대로 다르게 보인다(틴트/아웃라인/발광/스케일 누적).
- 피격 flash·공격 flash 후에도, 풀 재사용 후에도 스테이지 틴트가 유지된다(§5.1 회귀).
- 잠금 스테이지는 선택 불가, 클리어 시 다음 스테이지 해금, 5는 종점.
- 스테이지가 올라갈수록 영웅 HP·공격력이 배수만큼 강해진다.

## 9. 비범위 (Non-goals)

- E(디졸브/노이즈 셰이더) — 첫 사이클 제외.
- 신규 영웅·몬스터·카드 리소스 제작(CLAUDE.md §8 금지).
- 스테이지별 고유 기믹/AI 변화(스탯 배수 외 로직 차등) — 이후 사이클.
- 무한 스케일링·6스테이지 이상.
