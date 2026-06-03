# 영웅 스켈레톤 + 전투 반응 애니메이션 — 기획서

> 작성: game-designer · 2026-06-03
> 입력 문서: spec `docs/superpowers/specs/2026-06-03-hero-skeleton-animation-design.md` · plan `docs/superpowers/plans/2026-06-03-hero-skeleton-animation.md`
> 본 기획서는 spec/plan 이 **결정하지 않은 도메인 수치·범위·승격 근거**를 확정한다. 코드 구조·시그니처·TDD 는 plan 단일 진실 — 중복 명세하지 않는다.

---

## § 헤더

- **목표**: 영웅(`EHero.Knight`) 비주얼을 파랑 캡슐 → SazenGames 스켈레톤으로 교체하고, 전투 상태(입장·대기·이동·도주·공격·피격·사망)에 반응하는 애니메이션을 재생한다.
- **검증 가설**: 영웅이 전투 상태에 맞춰 움직일 때 "5분 자동전투 + HP%/시간 트리거 선택지"의 가독성·체감이 **저하 없이(중립) 또는 향상(긍정)** 되는가. 게임플레이 수치는 불변이므로 본 작업은 stage_goal 자체를 검증 대상으로 삼지 않고, **표현 추가가 기존 검증을 해치지 않음**을 보증하는 데 초점.
- **현재 단계 범위 적합성**: **범위 밖 → 명시적 승격**. 컨셉 §11.4 "캐릭터는 프리미티브 고정" 을 **영웅 1종에 한해** 푼다 (승격 근거 §3). 몬스터 애니 확장·루트모션·무기 연출·사운드는 본 범위 밖.
- **핵심 메커니즘**: 스켈레톤 비주얼을 `Knight.prefab` 의 자식으로 부착(루트 게임 컴포넌트 무손상). `Speed` float + `Attack`/`Hit`/`Spawn` trigger + `Dead` bool 5개 파라미터로 상태 전이. 무리에 둘러싸여 초당 다회 피격되는 영웅을 위해 **피격 리액션 스팸 가드**(쿨다운 + 공격 중 억제)를 둔다.

---

## 1. 애니메이션 사용 기획 (핵심)

스켈레톤은 클립 19종을 제공한다. 영웅에 쓸 **9 클립 → 7 상태** 매핑을 아래로 확정한다 (사용자 결정 "풍성 7+상태").

### 1.1 사용 클립 ↔ 전투 상태 매핑

| # | 전투 상태(영웅) | Animator 상태 | 사용 클립 | 트리거 / 조건 | Loop | 게임 신호 소스 |
|---|---|---|---|---|---|---|
| 1 | 입장 | Spawn | `Skeleton_spawn` | `Spawn` 트리거 1회 (진입 시), 종료 후 Idle | OFF | `HeroEntryDriver` 활성 / `OnEnable` |
| 2 | 대기 | Idle | `Skeleton_idle` | 기본 상태, `Speed` < 0.1 | ON | (속도 0) |
| 3 | 이동 | Move(walk) | `Skeleton_walk_forward` | `Speed` ≈ `walkSpeed`(1.0) | ON | `IMover.IsMoving == true` & `FleeMode == false` |
| 4 | 공포 도주 | Move(run) | `Skeleton_run_forward` | `Speed` ≈ `runSpeed`(2.0) | ON | `AutoCombatAI.FleeMode == true` (Fear/도주 카드) |
| 5 | 공격 | Attack | `Skeleton_slash01` / `Skeleton_slash02` / `Skeleton_stab` **랜덤 1택** | `Attack` 트리거, 종료 후 직전 상태 복귀 | OFF | `IAttacker.OnHit` (공격 적중 순간) |
| 6 | 피격 | Hit | `Skeleton_take_damage` | `Hit` 트리거 (스팸 가드 적용 §2) | OFF | `IHealth.OnChanged` 의 HP 감소 감지 |
| 7 | 사망 | Death | `Skeleton_death` | `Dead == true` (AnyState 인터럽트), 종료 후 마지막 프레임 유지 | OFF | `IHealth.OnDied` |

검산: 사용 클립 = idle·walk_forward·run_forward·slash01·slash02·stab·take_damage·death·spawn = **9 클립**. 상태 = Spawn·Idle·Move·Attack·Hit·Death = **6 Animator 상태** (Move 가 walk/run 2클립을 BlendTree 로 묶음 → 사용자 표현 "7+상태"는 walk/run 을 별 상태로 센 값과 정합).

> **#5 공격 트리거 타이밍 — 의도 확정**: 공격 스윙은 `IAttacker.OnHit`(= `TryAttack` 성공으로 **적중이 확정된 직후**)에 재생된다. 따라서 시각상 "스윙 → 데미지" 가 아니라 "데미지 확정 → 스윙" 순서로, 스윙이 적중 후 후행 재생되는 형태다. **이는 의도된 선택**이다 — 본 작업은 게임플레이(공격 판정 타이밍·데미지)를 일절 바꾸지 않는 표현 전용 레이어이므로, 기존 공격 판정 이벤트(`OnHit`)에 모션을 얹기만 한다. 자동전투에서 공격 쿨다운 1.0s(컨셉 §11.3) 대비 스윙 모션 0.4~0.6s 라 다음 판정 전에 스윙이 종료돼, 후행 재생이라도 "공격하고 있다" 로 충분히 읽힌다. 선행 스윙(모션 시작 시점에 판정) 으로 바꾸려면 공격 판정 파이프라인을 건드려야 하므로 본 범위 밖.

### 1.2 미사용 클립 (명시 제외)

아래 10 클립은 본 범위에서 **사용하지 않는다** (이관도 하지 않음 — plan Task 5 YAGNI). 사유를 명시해 후속에서 의도적 제외임을 보존한다.

| 미사용 클립 | 제외 사유 |
|---|---|
| `Skeleton_jump` / `Skeleton_fall` | 영웅은 2.5D 평면 자동이동 — 점프/낙하 게임플레이 상태 없음 |
| `Skeleton_scream` | 대응 게임 상태 없음 (도주는 run 으로 표현) |
| `Skeleton_revive` | 영웅 부활 메커니즘 없음 (사망 = 패배 확정, 컨셉 §4) |
| `Skeleton_underground` | 잠복/소환 연출 — 영웅 입장은 spawn 단일로 충분 |
| `Skeleton_turn_L` / `Skeleton_turn_R` | 방향 전환은 기존 `SimpleRotator`(루트 Y 회전)가 담당 — 회전 클립 불필요 |
| `Skeleton_throw` | 영웅은 근접 평타만 (원거리 없음, 컨셉 §11.3) |
| RootMotion 클립 2종 (`*_RM` 류) | 이동은 게임 로직(`SimpleMover`)이 루트 Transform 으로 처리 → 루트모션 비활성 (spec 비목표) |

> 후속 승격 시(몬스터 애니 등) 위 클립 일부가 재평가될 수 있으나, 본 범위에서는 **활용 0**.

---

## 2. 밸런스 / 표현 수치 확정

게임플레이 수치(HP·사거리·공격 쿨다운·데미지)는 **불변**이다. 아래는 **표현 전용** 파라미터로, 애니메이션이 깨져 보이지 않을 임계값이다.

### 2.1 피격 리액션 쿨다운 — `hitReactionCooldown` = **0.4s** (확정)

- **문제**: 영웅 HP 1000, 무리에 둘러싸여 다수 몬스터에게 동시 피격. 매 피격마다 full-body `take_damage`(약 0.5~0.8s 모션)를 재생하면 영웅이 영구 경직 → 공격·이동 모션이 전혀 안 보임("맞는 인형" 현상).
- **결정값**: 0.4s. 한 번 `Hit` 발행 후 0.4s 내 추가 피격은 리액션 억제(머티리얼 플래시로만 피드백).
- **근거 (검산)**:
  - 영웅 피격 빈도 추정: 평균 동시 교전 몬스터 4~8마리(Swarm 빌드 시 10+), 몬스터 공격 주기 약 1.0~1.5s → 영웅 피격 간격 **초당 3~8회** 수준.
  - `take_damage` 모션을 0.4s 주기로만 재생 → 초당 최대 2.5회 리액션 → 이동·공격 모션이 끼어들 여유 확보.
  - 0.4s 는 "맞고 있다"가 끊겨 보이지 않을 최소 가독 간격(2~3회/초)이면서, 영웅이 계속 경직되지 않을 상한. 더 짧으면(예: 0.2s) 경직 누적, 더 길면(예: 0.8s) 피격 리액션이 드물어 "맞는 느낌" 약화.
- **데이터 검증 메트릭**: 인게임 육안 검증(plan Task 10)에서 "Swarm 빌드 다중 피격 상황에서 영웅이 idle/walk/slash 모션을 1초 내 1회 이상 보이는가". 깨지면 0.5~0.6s 로 상향. 정량 검증이 필요하면 qa-simulator 의 "영웅 take_damage 재생 비율 / 초당 idle·walk·attack 모션 노출 프레임 비율" 메트릭으로 결정.

### 2.2 공격 억제창 — `attackSuppressWindow` = **0.5s** (확정)

- **문제**: 영웅이 공격 모션(slash/stab) 재생 중 피격되면 `Hit` 가 끼어들어 공격 스윙이 중간에 끊긴다 → 공격이 "헛스윙"처럼 보임. 영웅의 핵심 행동(공격)이 가장 잘 보여야 한다(컨셉 §11.4 "영웅은 가장 잘 보여야 함").
- **결정값**: 0.5s. `Attack` 트리거 후 0.5s 내 피격은 `Hit` 발행 억제.
- **근거 (검산)**: slash01/slash02/stab 모션 길이 약 0.4~0.6s. 억제창 0.5s 는 평균 공격 모션을 **끝까지 보여주고 자연 종료**시킨 뒤 피격 리액션을 허용. 0.5s < 영웅 공격 쿨다운 1.0s(컨셉 §11.3) 이므로 다음 공격 사이클엔 영향 없음 → 공격-피격 표현이 시간상 겹치지 않게 분리.
- **우선순위 규칙**: 동일 프레임에 사망 조건이 성립하면 `Dead`(AnyState 인터럽트)가 공격/피격 모두를 무시하고 최우선. (plan `OnDied`/`OnAttack_WhenDead_Ignored` 테스트로 보증)

### 2.2-b 도주(FleeMode) 중 피격 시 정책 — **Hit 허용 (run 을 짧게 끊고 복귀)** (확정)

- **문제**: 공포 도주(`FleeMode == true`)는 영웅이 공격하지 않으므로 공격 억제창(§2.2)이 작동하지 않는다. 이 상태에서 피격되면 `Hit` 트리거가 run(loop) 모션을 끊을 수 있다. run 유지(Hit 무시) vs Hit 허용(짧게 끊고 복귀) 중 정책을 정해야 한다.
- **결정값**: **Hit 허용** — 도주 중에도 피격 시 `Hit` 리액션을 정상 발행한다. 별도의 "도주 중 Hit 억제" 분기를 두지 않는다.
- **근거 (검산)**:
  - 도주 중 피격 빈도는 **피격 쿨다운 `hitReactionCooldown` = 0.4s(§2.1) 가 이미 제한**한다 → 초당 최대 2.5회. take_damage(약 0.5~0.8s) 가 끝나면 `Hit → Move(run)` Exit Time 전이로 run 으로 자동 복귀하므로, run 이 영구 중단되지 않고 "도주 중 가끔 움찔하며 계속 도망친다" 로 읽힌다.
  - 도주 중 피격 리액션은 **카드 효과 피드백을 오히려 강화**한다: 플레이어가 영웅을 도망치게(Fear) 만든 뒤 무리에게 얻어맞는 그림이 "포위해서 처치 중" 이라는 던전 주인 판타지(컨셉 §4 역방향 보스전)에 부합. run 을 경직 없이 유지하면 "안 맞는 것처럼" 보여 오히려 피드백이 약화된다.
  - run 유지(Hit 무시) 안의 단점: 도주 중 피격이 전혀 표현되지 않아, 도주가 곧 무적처럼 보이는 오인 가능. 채택하지 않음.
- **구현 영향 없음**: 별도 분기 불요 — 현행 결정 로직(`CharacterAnimationController.OnDamaged`, plan Task 2)이 `FleeMode` 를 보지 않고 쿨다운·공격억제창만 본다. 도주 중엔 공격억제창이 자연히 비활성(공격 안 함)이므로 쿨다운(0.4s)만 통과하면 `Hit` 발행 → **본 결정은 추가 코드 없이 충족**된다.

### 2.3 이동 Speed 파라미터 / BlendTree 임계 — 확정

| 파라미터 | 값 | 의미 |
|---|---|---|
| `walkSpeed` | **1.0** | 평상시 이동 시 `Speed` 세팅값 → `walk_forward` |
| `runSpeed` | **2.0** | `FleeMode == true`(공포 도주) 시 `Speed` 세팅값 → `run_forward` |
| Idle↔Move 임계 | **0.1** | `Speed > 0.1` → Move, `Speed < 0.1` → Idle |
| Move BlendTree | walk(Speed=1.0) ↔ run(Speed=2.0) 1D 보간 | `Speed` 값에 따라 walk/run 클립 블렌딩 |

- **결정 방식**: spec §9 의 "walk↔run 을 BlendTree vs Speed 임계 중 택1"은 plan Task 6 에서 **BlendTree(1D, `Speed`)** 로 확정됨 → 본 기획서도 BlendTree 로 통일.
- **중요 — 표현/물리 분리**: `walkSpeed`/`runSpeed`(1.0 / 2.0)는 **애니메이션 파라미터 값**일 뿐이며, 영웅의 **실제 이동 속도(게임플레이)와 무관**하다. 영웅 물리 이동 속도는 기존 `SimpleMover` 가 그대로 담당하고 본 작업으로 변경되지 않는다. 즉 도주 시 영웅이 실제로 더 빨라지는지는 기존 `FleeMode` 게임 로직이 결정하고, 본 작업은 그 상태에 run 모션을 **얹기만** 한다.
- **근거**: walk:run = 1:2 비율은 run 모션이 "확연히 다급해 보이는" 시각 대비를 주는 표준값. 도주(Fear 카드 등) 가 한눈에 구분돼야 플레이어가 "내가 영웅을 도망치게 만들었다"는 카드 효과 피드백을 인지 → 시너지 가시성에 기여.

### 2.4 수치 노출 위치 — **프리팹 SerializeField** 권고 (BalanceConfig 미노출)

| 후보 | 장점 | 단점 | 판정 |
|---|---|---|---|
| BalanceConfig.asset 노출 | 중앙 집중, 밸런스 패스에서 일괄 조정 | 4개 값이 **게임플레이가 아닌 표현 전용** — BalanceConfig(전투 밸런스 SoT)에 비전투 값 혼입 → 오염 | ✗ |
| **프리팹 `CharacterAnimationDriver` SerializeField** | 비주얼/표현 값을 비주얼 컴포넌트 옆에 응집, 영웅 외 캐릭터 재사용 시 인스턴스별 조정 자유 | 중앙 일괄 조정 불가(단 4개·표현값이라 빈도 낮음) | **✓ 권장** |

- **권고**: `hitReactionCooldown`(0.4)·`attackSuppressWindow`(0.5)·`walkSpeed`(1.0)·`runSpeed`(2.0) 4개는 `CharacterAnimationDriver` 의 `[SerializeField]` 로 둔다 (plan Task 4 와 일치 — plan 이 이미 이 4개를 SerializeField 로 정의함).
- **사유**: 이 값들은 **전투 밸런스가 아니라 애니메이션 표현 임계**다. BalanceConfig 는 HP/데미지/쿨다운 등 "재미·승률에 직결되는" 게임플레이 수치의 단일 진실이어야 하므로, 표현 전용 값이 섞이면 SoT 가 흐려진다. 표현 값은 변경 시 인게임 육안 검증으로 판정되지 그 자체로 승률을 바꾸지 않는다.

---

## 3. MVP §8 승격 근거 · 범위 한계

### 3.1 승격 근거

컨셉 §11.4 / CLAUDE.md §8 은 "캐릭터(영웅·몬스터) 비주얼은 프리미티브 도형 고정, 아트 작업 금지"를 정한다. 본 작업은 이 규칙을 **영웅 1종에 한해** 명시적으로 푼다. 근거:

1. **이미 진행 중인 아트 패스의 연속선** — 몬스터는 이미 LittleGhost 아트로 비주얼 교체가 **시작됨**: `Wisp.prefab` 이 LittleGhost 프리팹을 자식으로 물고 `Ghost_Kid.controller`(단일 idle 루프)를 재생 중 (최근 커밋 `91cb580`·`0c9fbb4` "Little_GhostLP 사용 에셋 _Lair/Art 이관"). 즉 "캐릭터 프리미티브 고정"은 몬스터 측에서 이미 사용자 승인 하에 부분 해제된 상태다. 영웅만 파랑 캡슐로 남으면 **비주얼 일관성이 깨진다**(아트 몬스터 vs 도형 영웅).
2. **검증 무해성** — 게임플레이 수치 불변(§4). 표현만 추가하므로 stage_goal 검증을 훼손하지 않는다.
3. **인프라 가치** — 본 작업은 단순 영웅 1종 교체를 넘어 **전투 상태 반응 애니메이션 인프라**(`CharacterAnimationController`/`Driver`/`AnimatorSink`, 인터페이스 의존)를 도입한다. 이 인프라는 몬스터 6종에 재사용 가능하게 설계되어(인터페이스 의존, spec §5) 후속 몬스터 애니 확장의 토대가 된다.

→ **사용자 합의(2026-06-03, spec §5 기록)에 따라 영웅 1종 + 애니 인프라까지를 §8 프리미티브 고정의 예외로 승격한다.**

### 3.2 범위 한계 (이번 작업이 하지 않는 것)

| 항목 | 본 범위 | 후속(별도 승격 필요) |
|---|---|---|
| 영웅(Knight) 1종 스켈레톤 + 7상태 애니 | ✅ | — |
| 애니 인프라(Controller/Driver/Sink, 인터페이스 재사용 설계) | ✅ | — |
| 몬스터 6종 애니메이션 와이어링 | ❌ | 인프라는 재사용 가능하나 **와이어링은 후속**. 현재 Wisp 의 단일 idle 루프는 본 인프라로 교체하지 않음 |
| 루트모션 / 무기(Falchion) 장착 연출 | ❌ | 비목표 (spec §1) |
| 사운드 훅(공격·피격·사망 SFX) | ❌ | §8 사운드 금지 유지 |
| 카드 효과 전용 추가 모션(둔화·출혈 상태 모션) | ❌ | 현행 머티리얼 플래시/프리미티브 상태 표시(컨셉 §11.4) 유지 |

> 후속 작업(몬스터 애니 확장 등)은 별도 spec/기획서로 다시 §8 승격을 거친다. 본 기획서의 승격 범위는 **영웅 1종 + 인프라**로 닫힌다.

### 3.3 기존 피격 플래시·상태 표시와의 공존 — 렌더러 재지정 결정 (확정)

캡슐 `MeshRenderer` 제거 시 기존 피드백 컴포넌트(`HitFlash`/`AttackJuice`/`DamageFeedback`)의 타깃 렌더러가 깨지는지가 쟁점이었다. **코드를 직접 확인한 결과, 렌더러 재지정 코드는 불필요하다.** 근거:

| 컴포넌트 | 렌더러 획득 방식 (실측) | 캡슐→SkinnedMesh 교체 영향 |
|---|---|---|
| `HitFlash` | `CacheRenderers()` 가 `GetComponentsInChildren<Renderer>(includeInactive:true)` 로 **자식 렌더러 전부 수집**, `_BaseColor` 반전/원복. 제외 prefix `ExcludeNamePrefixes = {"Aura", "HpBar"}` 둘뿐 | **자동 포함** — 스켈레톤을 자식으로 부착하면 SkinnedMeshRenderer 가 그대로 수집됨. 재지정 불요 |
| `AttackJuice` | `GetComponentsInChildren<Renderer>` 폴백으로 대표색 획득(영웅은 `_heroWhiteDamageColor`=흰색 오버라이드 경로 우선) + 루트 `transform.localScale` punch | **무영향** — 스케일은 루트 기준, 대표색은 흰색 오버라이드라 렌더러 의존 없음 |
| `DamageFeedback` | 루트 `Collider.bounds` 만 사용(팝업 위치). 렌더러 미사용 | **무영향** — 콜라이더는 루트 유지(§4) |

→ **결정: 피드백 컴포넌트의 렌더러 재지정 코드를 추가하지 않는다.** `HitFlash`/`AttackJuice` 가 이미 자식 렌더러를 런타임 스캔하므로, 스켈레톤 비주얼을 Knight 루트의 자식으로 부착하는 것만으로 플래시·틴트가 SkinnedMeshRenderer 에 자동 적용된다.

**구현 필수 조건 (gameplay-programmer 가 Task 8 프리팹 작업 시 반드시 충족)**:

| # | 조건 | 사유 (위반 시 증상) |
|---|---|---|
| 1 | 스켈레톤 비주얼 머티리얼은 **`_BaseColor` 프로퍼티를 갖는 URP Lit 계열**일 것 | `HitFlash.WriteColor`/`ReadColor` 가 `_BaseColor` 우선 사용. 없으면 `mat.color` 폴백이나 URP 셰이더에서 `mat.color`=`_BaseColor` 미연동 시 플래시·공격 틴트가 보이지 않음 |
| 2 | 비주얼 GameObject 및 그 자식 이름이 **`Aura`/`HpBar` prefix 로 시작하지 말 것** | `ExcludeNamePrefixes` 매칭 시 해당 렌더러가 플래시 대상에서 제외돼 피격 시 깜빡임이 누락됨. 스켈레톤 자식 메시명에 `Aura`/`HpBar` 가 우연히 들어가지 않도록 확인 |
| 3 | 둔화 Sphere·공포 Cube 등 **상태 비주얼 자식 부착점과 HP바는 루트(Knight) 기준 유지** | 상태 비주얼은 `IStatusVisual.Offset` 로 루트 기준 배치(예: `BleedAura.Offset`). 비주얼 자식(Visual) 하위로 옮기면 스켈레톤 애니 본 변형/스케일 보정에 끌려가 위치가 흔들림 |
| 4 | **PlayMode 에서 SkinnedMesh 에 피격 플래시가 실제로 보이는지 육안 확인** (plan Task 10 에 포함) | 조건 1·2 위반은 컴파일·NRE 없이 "플래시만 안 보이는" 무증상 회귀라 정적 검증으로 안 잡힘 — 반드시 실행 확인 |

### 3.4 영웅 출혈 색상 표시 — 본 작업 범위 밖 (무변화)

컨셉 §11.4 의 "출혈 = 영웅 색상 변경(#991B1B)" 의 **별도 지속 색상 적용(몸체 `_BaseColor` 상시 틴트) 컴포넌트는 현재 `Card/` 에 존재하지 않는다** (코드 확인: `BleedAura` 는 `IStatusVisual` 로 `EVisual.BleedStatus` 자식 비주얼 + 데미지 숫자 색 스탬프 `HitFeedbackPalette.Bleed` 만 적용하고, 영웅 몸체 색을 상시 변경하지 않는다).

→ **결정: 영웅 출혈 몸체 틴트는 본 작업 범위 밖으로 둔다 (기존 동작 유지 = 무변화).**
- 캡슐 제거로 깨질 기존 출혈 몸체 틴트 코드가 없으므로, 본 작업이 새로 손댈 출혈 표현이 없다.
- 출혈 표현(데미지 숫자 색 + `BleedStatus` 자식 비주얼)은 현행대로 유지되며 스켈레톤 교체와 독립적이다.
- "영웅 몸체를 #991B1B 로 상시 틴트" 를 SkinnedMesh `_BaseColor` 로 신규 구현하는 것은 **본 MVP 범위를 넘는 별도 작업**(상시 틴트 vs 피격 플래시의 색 충돌 처리·우선순위 설계가 추가로 필요)이므로 본 기획서에 포함하지 않는다. 필요 시 후속 기획서로 분리한다.

---

## 4. 검증 영향 (stage_goal 중립/긍정 확인)

stage_goal = "5분 자동전투 + HP%/시간 트리거 선택지가 재미있는가". 본 작업의 검증 영향을 항목별로 확인한다.

| 검증 차원 | 영향 | 판정 |
|---|---|---|
| 게임플레이 수치 (영웅 HP 1000 / 공격력 50 / 공속 1s / 사거리 / 몬스터 DPS) | **불변** — 코드/SO 수치 변경 0 (spec §7) | 중립 |
| 전투·충돌·사거리 로직 | **불변** — 콜라이더/Rigidbody/이동은 루트 기준 유지, 비주얼만 자식 교체 (spec §3) | 중립 |
| 로딩 경로 | **불변** — `BattleController` 의 `CHMResource.LoadAsync<GameObject>(EHero.Knight)` 그대로, 프리팹 내부만 변경 | 중립 |
| 카드 효과 가독성 (시너지 가시성) | **긍정 가능** — 도주(run)·공격(slash) 모션이 카드 효과(Fear/도주, 공격 빈도)를 시각적으로 강화 → "내 카드가 영웅에게 뭘 했는지" 인지 향상 | 긍정 |
| 영웅 가독성 (컨셉 §11.4 "영웅은 가장 잘 보여야 함") | **리스크 관리됨** — 피격 스팸 가드(§2)로 영웅이 "맞는 인형"으로 굳지 않게 보장. 검증 못 하면 §2.1 메트릭으로 재조정 | 중립~긍정 |

**결론**: 게임플레이는 완전 불변, 표현만 추가 → stage_goal 검증에 **중립(최소) / 긍정(시너지 가시성 강화)**. 본 작업이 "재미·밸런스 검증을 새로 요구하는" 변경이 아니므로 qa-simulator 풀 시뮬은 **불필요**하고, 인게임 육안 검증(plan Task 10)으로 충분하다. 단 §2.1 피격 가드가 다중 피격 상황에서 깨지면 그 수치 한정으로 qa-simulator 메트릭(§2.1) 검토.

---

## 5. 구현 요청사항 (gameplay-programmer 용)

> 코드 구조·시그니처·파일 경로·TDD 는 **plan 이 단일 진실**이다. 본 절은 plan 과 **모순 없이**, 도메인 결정값과 에셋 키만 재확인한다 (중복 명세 최소화).

### 5.1 Enum 값
- **신규 Enum 추가 없음.** 영웅 로드 키 `EHero.Knight` 는 기존 그대로 사용 (프리팹 내부만 변경, 파일명·키 불변 — Rule 03 §2).

### 5.2 Interface
- `IAnimatorSink` 신규 (plan Task 1, `CommonInterface.cs` 내 `Lair.Character` namespace). 시그니처는 plan 계약을 따른다. **단 공격 클립 랜덤화(§1.1 #5) 때문에 `TriggerAttack` 은 `void TriggerAttack(int variant)` 로 확정** (plan Task 7 과 일치 — variant 0=slash01 / 1=slash02 / 2=stab).
- `IHealth`(`OnChanged(int,int)`·`OnDied`)·`IMover`(`IsMoving`)·`IAttacker`(`OnHit(IHealth)`)·`AutoCombatAI.FleeMode` 는 **기존 그대로 관찰**. 인터페이스 확장 불필요 (plan 확정).

### 5.3 에셋 키 (파일명 = 식별자, Rule 03 §2 / Rule 04 §2)
- AnimatorController: `Knight.controller` → `Assets/_Lair/Art/Animations/Knight.controller`
- 스켈레톤 비주얼: `Skeleton_Model_110.fbx` + 사용 클립 9종(§1.1)을 `Assets/_Lair/Art/Characters/Skeleton/` 하위로 이관 (plan Task 5, 사용분만 이관 — 미사용 10종 §1.2 제외)
- 클립 파일명 = §1.1 표의 클립명 그대로 (`Skeleton_spawn`/`idle`/`walk_forward`/`run_forward`/`slash01`/`slash02`/`stab`/`take_damage`/`death`)

### 5.4 Animator 파라미터 (Knight.controller 계약 — §1.1·§2 결정 반영)
| 파라미터 | 타입 | 값/용도 |
|---|---|---|
| `Speed` | Float | Idle↔Move 임계 0.1 / walk=1.0 / run=2.0 (§2.3) |
| `Attack` | Trigger | 공격 스윙 (variant 와 함께) |
| `AttackVariant` | Int | 0~2 (0=slash01 / 1=slash02 / 2=stab). `CharacterAnimationController` 가 `OnAttack` 시 0~2 랜덤 주입 후 `AttackVariant` 세팅 → `Attack` 트리거 (plan Task 7) |
| `Hit` | Trigger | 피격 리액션 (스팸 가드 통과 시만, §2.1·§2.2) |
| `Dead` | Bool | 사망 (AnyState 인터럽트, 최우선) |
| `Spawn` | Trigger | 입장 1회 |

### 5.5 SO 스키마 / 수치 필드 (표현 전용 — `CharacterAnimationDriver` SerializeField, §2.4)
| 필드 | 타입 | 기본값 | 근거 |
|---|---|---|---|
| `_hitReactionCooldown` | float | **0.4** | §2.1 |
| `_attackSuppressWindow` | float | **0.5** | §2.2 |
| `_walkSpeed` | float | **1.0** | §2.3 |
| `_runSpeed` | float | **2.0** | §2.3 |

> 별도 ScriptableObject 신설 없음 — 표현 값은 프리팹 컴포넌트 인스펙터에 둔다(§2.4 권고). BalanceConfig.asset 미수정.

---

## 6. Self-Review

- **Placeholder 잔존**: 0건. 미정 마커/애매 권유/두 갈래 위임 없음. 모든 수치(0.4 / 0.5 / 1.0 / 2.0 / 임계 0.1) 확정 + 검산 라인 동반. §3.3 렌더러 재지정·§2.2-b 도주 중 피격 정책은 "구현 검증 사항" 같은 미결 표현을 제거하고 코드 확인 기반으로 결정 완료 형태로 재작성.
- **스펙 커버리지**: spec §1(목표/비목표)→§헤더·§3.2 / §2(에셋)→§5.3 / §3(구조)→§4(불변 확인)·§3.3 / §4(컨트롤러)→§1.1·§5.4 / §5(드라이버)→§5.2 / §6(스팸 가드)→§2.1·§2.2·§2.2-b / §7(영향)→§4 / §9(미해결)→§2·§5 매핑. 갭 0건. (구조·시그니처 상세는 의도적으로 plan 위임 — 본 기획서 범위 외 명시.)
- **내부 일관성**: 0.4/0.5/1.0/2.0/0.1 이 §1·§2·§5 에서 동일. walk:run=1:2 일관. 사용 클립 9종이 §1.1·§1.2·§5.3 에서 동일. §3.3 렌더러 결정(재지정 불요)이 §2.1 의 "억제된 피격도 플래시로 표현" 전제와 정합(플래시가 SkinnedMesh 에 자동 적용되므로 성립).
- **시그니처/명명 일관성**: `IAnimatorSink`·`TriggerAttack(int variant)`·`AttackVariant`·`Speed`/`Attack`/`Hit`/`Dead`/`Spawn`·`FleeMode`·`IsMoving`·`OnChanged`·`OnDied`·`OnHit`·`EHero.Knight`·`Knight.controller`·`Skeleton_*` 클립명, 그리고 §3.3 의 `HitFlash`/`AttackJuice`/`DamageFeedback`·`CacheRenderers`·`ExcludeNamePrefixes`·`_BaseColor`·`EVisual.BleedStatus`·`HitFeedbackPalette.Bleed`·`BleedAura` 모두 plan 및 실제 코드(`HitFlash.cs`·`AttackJuice.cs`·`DamageFeedback.cs`·`BleedAura.cs`·`AutoCombatAI.cs` 직접 확인)와 글자 그대로 일치. 변형 표기 0건.
- **모호 표현**: 0건. 표현/물리 분리(§2.3), 노출 위치 단일 권고(§2.4), 렌더러 재지정 결정(§3.3 — "불필요" 단정), 도주 중 피격 정책(§2.2-b — "Hit 허용" 단일안), 출혈 범위(§3.4 — "범위 밖" 단정)로 두 갈래·검증 위임 제거.
- **스코프**: 단일 구현 단위(영웅 1종 + 인프라). 몬스터 확장은 §3.2, 출혈 몸체 틴트 신규 구현은 §3.4 로 명시 분리.
- **구현 요청사항 완전성**: Enum(신규 없음)·Interface(IAnimatorSink)·에셋 키·Animator 파라미터·SerializeField 수치 + §3.3 프리팹 필수 조건 4건 모두 명세.

→ **Self-Review: 통과** (design-reviewer BLOCKER 1 + MAJOR 1 + MINOR 2 보강 반영. MAJOR 2 는 plan 오타로 기획서 무관 — 메인이 plan 정정).
