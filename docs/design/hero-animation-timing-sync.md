# 영웅 애니메이션 ↔ 게임플레이 타이밍 동기화 — 기획서 (enhancement)

> 작성: game-designer · 2026-06-03 (rev3 — BLOCKER 0 통과 후 비차단 권장 2건 마감: 영웅 애니 트리거 주체 단정·strike 순환 방지 §2.7 / CooldownScale 표기 정정 §7.4)
> 선행 기획서: `docs/design/hero-skeleton-animation.md` (애니 적용 완료분)
> 입력: 사용자 직접 요구(간이 입력) — spec/plan 신규 없음. 선행 기획서 §1.1 #5 의 "데미지 확정 → 스윙 후행 재생(표현 전용)" 결정을 **본 enhancement 가 의도적으로 덮어쓴다**. 본 작업은 표현 전용 레이어가 아니라 **게임플레이 데미지 타이밍을 애니에 종속**시키는 변경이다(선행 기획서의 "게임플레이 불변" 전제는 본 기획서 범위에서 깨진다 — §0 참조).

> **rev2 핵심 — "공통 컴포넌트 보존" 대전제**: `AutoCombatAI`·`MeleeAttacker`·`SimpleRotator` 는 **영웅 + 몬스터 6종이 공유**하는 컴포넌트다(`AutoCombatAI.cs` L5 주석 "영웅/몬스터 공통"). 흐름을 무조건 바꾸면 strike 클립 이벤트가 없는 몬스터는 데미지가 영영 0이 된다. 따라서 본 enhancement 의 **모든 흐름 변경(데미지 지연·공격 게이트·스폰 게이트·회전 스냅)은 영웅 한정 분기**로 좁힌다. 분기 스위치는 §B 에 단일 정의한다. 몬스터는 본 변경 후에도 **현행 즉시-데미지 경로를 100% 유지**한다.

---

## § 헤더

- **목표**: 영웅(`EHero.Knight`)의 3가지 애니↔게임플레이 desync 를 해소한다 — ① 스폰 애니 종료 후 이동·전투 시작(스폰 게이팅), ② 공격은 애니 windup 후 strike 프레임에 데미지 적용(흐름 역전), ③ 회전 즉시 스냅.
- **검증 가설**: 데미지가 "쿨다운 차면 즉시"가 아니라 "공격 모션의 타격 순간"에 적용되어도 5분 자동전투의 페이싱·가독성이 유지되는가. 특히 **공격 애니 길이(1.13~1.63s) > 쿨다운(1.0s)** 충돌을 푼 규칙이 영웅 DPS 를 깨지 않고 자연스러운 공격 리듬을 만드는가.
- **현재 단계 범위 적합성**: **범위 내** — 영웅 1종 애니는 선행 기획서 §3 에서 이미 §8 프리미티브 고정의 예외로 승격됨. 본 enhancement 도 그 승격 범위(영웅 한정)를 벗어나지 않는다. 몬스터·루트모션·무기·사운드는 여전히 범위 밖.
- **핵심 메커니즘**:
  1. **스폰 게이팅** — spawn 클립 종료 애니 이벤트(`OnSpawnAnimEnd`) → 그때까지 이동/전투 입력 봉인 → 게이트 해제.
  2. **공격 흐름 역전** — `AutoCombatAI` 교전+쿨다운 충족 시 *데미지 즉시 적용*이 아니라 *공격 애니 트리거(windup)*. 클립 strike 프레임에 박은 애니 이벤트(`OnAttackStrike`)가 **그 순간 실제 데미지**를 적용. 공격 모션 재생 중엔 이동 정지·다음 공격 보류.
  3. **회전 스냅** — `SimpleRotator` 보간 제거(즉시 yaw 적용).

---

## B. 영웅 한정 분기 규칙 (B1 — 공통 컴포넌트 보존의 단일 진실)

> 본 절은 design-reviewer **B1(최우선)** 을 닫는다. 이 절이 "어떤 변경이 영웅에만 걸리고 몬스터는 어떻게 보존되는가"의 **단일 진실**이며, 다른 모든 절(§1·§2·§3·§4)의 영웅 한정 여부는 이 표를 참조한다.

### B.1 문제 — 흐름을 무조건 바꾸면 몬스터가 깨진다

`AutoCombatAI.Update`(L106~112 교전 분기)는 `_attacker.TryAttack(...)` 으로 **그 자리에서 즉시 데미지**를 적용한다. 이 분기를 "데미지를 strike 이벤트로 미룸"으로 무조건 바꾸면:

- 영웅: strike 클립 이벤트(`OnAttackStrike`)가 박혀 있어 데미지가 strike 시점에 적용됨. OK.
- 몬스터 6종: strike 클립 이벤트가 **없다**(애니 와이어링 자체가 선행 기획서 §3.2 에서 후속으로 분리됨, Wisp 는 단일 idle 루프뿐). 데미지를 미루면 **strike 이벤트가 영영 안 와서 데미지 0** → 전투 자체가 붕괴.

### B.2 결정 — `MeleeAttacker.DeferStrike` 플래그 (영웅 프리팹만 true)

`MeleeAttacker` 에 **`bool DeferStrike` 플래그**(SerializeField, 기본 **false = 즉시 데미지**)를 둔다. 영웅(`Knight.prefab`)의 `MeleeAttacker` 만 인스펙터에서 **true** 로 설정한다. 몬스터 6종 프리팹은 false(기본값) 그대로 → **무변경**.

`AutoCombatAI` 교전 분기의 행동을 이 플래그로 분기한다:

| `DeferStrike` | 교전 분기 행동 | 데미지 적용 시점 | 대상 |
|---|---|---|---|
| **false** (기본) | 현행 그대로 `TryAttack(th, …, now)` 호출 | **즉시**(쿨다운 충족 프레임) | 몬스터 6종 — **현행 100% 보존** |
| **true** | `IsAttacking==false && 쿨다운 만료` 일 때 `TryBeginAttack(th, …, now)` 호출(애니 트리거·쿨다운 기록, 데미지 X). strike 이벤트가 `TryApplyStrike(now)` 호출 | **strike 프레임**(windup 지연) | 영웅 1종만 |

**대안 비교 (분기 위치)**:

| 안 | 방식 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| A | `MeleeAttacker.DeferStrike` bool 플래그(영웅 프리팹만 true), AutoCombatAI 가 이 플래그로 분기 | 분기 1개로 모든 영웅 한정 흐름 묶임, 몬스터 프리팹 무변경, 테스트 시 플래그만 토글 | AutoCombatAI 에 분기 if 1개 추가 | **✓ 권장** — 메인 제안과 일치. 가장 적은 표면적으로 공통 컴포넌트 보존 |
| B | 영웅 전용 `HeroCombatAI` 서브클래스 분리 | AutoCombatAI 본체 무수정 | 6종과 공유하던 히스테리시스·도주 로직을 영웅이 상속/중복 유지해야 함, RequireComponent 체인 재설계 | ✗ (과한 분기, 회귀 위험) |
| C | `ITargetProvider` 처럼 `IStrikeTimingPolicy` 컴포넌트 주입 | 가장 OOP | 단일 분기에 인터페이스 1개 신설 — YAGNI | ✗ (MVP 과설계) |

**권장 = A**. AutoCombatAI 교전 분기는 다음 의사 흐름으로 좁힌다 (시그니처·구현은 gameplay-programmer 영역, 본 표는 **계약**):

```
교전(_engaged) 분기:
  _rotator?.FaceDirection(...)        //# 회전은 §B.3 참조 (영웅 한정 스냅)
  _mover.Stop()
  if (_attacker.DeferStrike == false)         //# 몬스터 — 현행
      _attacker.TryAttack(th, self, target, now)
  else                                        //# 영웅 — 흐름 역전
      if (IsAttacking == false && /* 쿨다운 만료는 TryBeginAttack 내부 검사 */)
          if (_attacker.TryBeginAttack(th, self, target, now))
              //# 애니 트리거 + IsAttacking=true (§3.2 게이트가 처리)
```

### B.3 본 enhancement 변경의 영웅 한정 여부 — 한눈 표

| 변경 | 영웅 한정? | 몬스터 동작 | 분기 근거 |
|---|---|---|---|
| 데미지 strike 지연(흐름 역전, §2) | **영웅만** | 즉시 데미지(현행) | `DeferStrike` (§B.2) |
| `IsAttacking` 공격 게이트(§3) | **영웅만** | 게이트 미부착 → 보류 로직 자체가 안 걸림 | `IAttackGate` 컴포넌트가 영웅 프리팹에만 부착(§M2) |
| 스폰 게이팅(§1) | **영웅만** | 몬스터는 HeroEntryDriver 자체가 없음 | HeroEntryDriver 는 영웅 전용 컴포넌트(이미 영웅만) |
| 피격 Hit 억제 기준 변경(§3.4) | **영웅만** | 몬스터는 애니 와이어링 없음 → 무관 | `CharacterAnimationDriver` 가 영웅 프리팹에만 부착(선행 §3.2) |
| **회전 즉시 스냅(§4)** | **영웅만** | 몬스터는 540°/s 보간 유지 | `SimpleRotator._snapInstant` 플래그(§4.2) — 영웅 프리팹만 true |

> **회전 스냅 영웅 한정 명시(B1·M2·m2 통합)**: 회전 즉시 스냅도 **영웅 한정**이다. `SimpleRotator` 가 6종 공통이므로 무조건 보간을 제거하면 몬스터 회전감까지 바뀐다. §4.2 에서 `_snapInstant` bool 플래그(기본 false=보간)로 분기하고 영웅 프리팹만 true 로 둔다.

---

## 0. 게임플레이 영향 선언 (선행 기획서와의 차이)

선행 기획서는 "게임플레이 수치·타이밍 완전 불변, 표현만 추가"를 전제로 했다. **본 enhancement 는 그 전제 중 "데미지 적용 *타이밍*"을 의도적으로 변경한다.** 변경 범위를 명확히 한정한다:

| 항목 | 선행 기획서 | 본 enhancement |
|---|---|---|
| 데미지 **수치**(영웅 Power 50 등) | 불변 | **불변** (변경 0) |
| 사거리(`Range`) / 쿨다운(`Cooldown`) **값** | 불변 | **불변** (값 변경 0, 단 쿨다운 *해석*은 §3.3 규칙으로 보강) |
| 데미지 적용 **시점** | 쿨다운 충족 즉시 (`TryAttack` 내부) | **공격 애니 strike 프레임** (windup 만큼 지연) |
| 공격 애니 재생 순서 | 데미지 확정 → 스윙(후행) | **스윙 windup → strike(데미지) → 회복**(선행) |
| 이동 시작 시점 | 영웅 활성 즉시 | **spawn 애니 종료 후** (영웅 한정) |
| 회전 | 540°/s 보간 | **즉시 스냅 (영웅 한정 — `_snapInstant`, §4.2)** |

> 위 모든 변경은 **영웅 한정**이다(§B). 몬스터 6종은 데미지 즉시·이동 즉시·회전 보간 모두 현행 보존.

→ 데미지 적용 시점이 평균 0.4~0.9s(클립별 windup) 지연되므로 **영웅 실효 DPS 가 미세하게 하락**할 수 있다. 이는 **인지된 영향**이며 수치 튜닝은 §6 에서 qa-simulator 후속으로 분리한다. 본 기획서는 "동작이 깨지지 않을 규칙"까지만 확정한다.

---

## 1. 스폰 게이팅

### 1.1 현재 동작 (문제)

- `BattleController.SpawnHero`(L295~318)가 영웅 Pop → AutoCombatAI 를 `enabled=false` 로 끄고 → `HeroEntryDriver.enabled = true` 로 **즉시 march(BattleZone.Center 이동) 시작**. `CharacterAnimationDriver.OnEnable` 이 `OnSpawn()` 으로 spawn 트리거를 발행하지만, march(이동)는 spawn 클립(40f, ~1.33s) 재생과 **무관하게 즉시 진행**된다.
- 결과: 영웅이 등장 모션(`Skeleton_spawn`) 중인데 이미 미끄러져 이동하는 desync.

### 1.2 결정 — 게이트는 HeroEntryDriver/AutoCombatAI **내부 플래그** (B3 — BattleController 무수정)

> 본 절은 design-reviewer **B3** 을 닫는다. **`BattleController` 의 스폰 흐름(L295~349)은 변경하지 않는다.** BattleController 는 현행대로 `HeroEntryDriver.enabled = true` 를 호출하되, **march 가 실제로 시작될지는 HeroEntryDriver 내부 게이트 플래그**가 결정한다.

**메커니즘 — "활성화 ≠ march 시작" 분리**:

- HeroEntryDriver 에 **내부 게이트 플래그 `_marchGateOpen`**(기본 **false = 닫힘**)을 둔다. `enabled=true` 가 되어 `Update` 가 돌더라도, `_marchGateOpen == false` 면 **이동하지 않고 정지 유지**(`_mover.Stop()`)한다. spawn 클립 종료 신호(`OnSpawnAnimEnd`)를 받으면 게이트가 open 되고 그때부터 Center 로 march 한다.
- 게이트 open 트리거 = spawn 클립 마지막 프레임(40f)에 박은 애니 이벤트 `OnSpawnAnimEnd` → relay(§B4) 가 루트 HeroEntryDriver 의 게이트를 open. 안전망 fallback 타임아웃(1.8s)도 게이트를 강제 open.

**BattleController 호출 흐름은 글자 하나 안 바뀐다**:

```
[현행 = 변경 후 동일]
  BattleController.SpawnHero: AutoCombatAI.enabled=false → HeroEntryDriver.enabled=true   (무수정)
  BattleController.HandleHeroReachedCenter: AutoCombatAI.enabled=true + clock 시작          (무수정)
  zone==null 폴백: EnableHeroAIAfterDelay(3f) → AutoCombatAI.enabled=true                  (무수정)

[달라지는 곳 = HeroEntryDriver 내부]
  enabled=true 이후에도 _marchGateOpen==false 동안은 Stop 유지(이동 안 함)
  OnSpawnAnimEnd 또는 fallback 1.8s → _marchGateOpen=true → 그때부터 Center 로 march
```

**zone==null 폴백 경로 게이트 존중(B3 명시)**: zone 이 없으면 HeroEntryDriver 가 동작하지 않고 `EnableHeroAIAfterDelay(3f)` 가 3초 후 AutoCombatAI 를 켠다. 이 폴백에서도 영웅이 spawn 모션 중 전투를 시작하면 안 되므로, **AutoCombatAI 에도 동일 게이트 플래그 `_spawnGateOpen`**(기본 false)를 둔다 — `DeferStrike==true`(영웅) 일 때 게이트가 닫혀 있으면 교전/이동을 보류한다. AutoCombatAI 의 게이트도 같은 relay(`OnSpawnAnimEnd`)/fallback 으로 open. 몬스터(`DeferStrike==false`)는 이 게이트를 **무시**(현행 동작) — `OnEnable` 에서 `_spawnGateOpen=true` 로 강제하거나, `DeferStrike==false` 면 게이트 검사 자체를 건너뛴다(단일 진실: §B.2 의 영웅 한정 규칙).

> **요약**: BattleController 의 `enabled=true/false` 호출 시퀀스는 보존하되, "enabled 가 곧 행동 개시"라는 암묵 결합을 끊고 **행동 개시 = 게이트 open** 으로 한 단계 미룬다. BattleController 변경 0줄.

### 1.3 대안 비교 (게이트 신호 소스)

| 안 | 신호 소스 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| A | spawn 클립 끝 **애니 이벤트** `OnSpawnAnimEnd` | 클립 길이 변경에 자동 추종(이벤트는 클립에 박힘), 정확한 종료 프레임 | 클립에 이벤트 키 베이크 필요 | **✓ 권장** — 본 enhancement 가 어차피 공격 strike 이벤트(§2)를 베이크하므로 동일 파이프라인 재사용 |
| B | Animator **state 종료 감지**(`StateInfo.normalizedTime >= 1`) 폴링 | 클립 이벤트 불요 | Driver 가 매 프레임 state 폴링 → Update 비용 + spawn state 이름 하드 의존 | ✗ |
| C | 고정 타이머(1.33s 대기) | 가장 단순 | 클립 길이 변경 시 desync 재발, 매직넘버 | ✗ |

**권장 = A**. spawn 클립의 마지막 프레임(40f, ~1.33s)에 `OnSpawnAnimEnd` 애니 이벤트를 베이크한다. 안전망으로 **fallback 타임아웃 1.8s**(spawn 길이 1.33s + 0.47s 여유)를 두어, 이벤트 누락(클립 교체로 키 유실 등) 시에도 영웅이 영구 봉인되지 않게 한다 — 타임아웃 발화 시 게이트를 강제 open.

**검산**: fallback 1.8s = spawn 1.33s × 1.35 (35% 마진). 마진은 fps 변동·재생 지연 흡수용. 1.8s 이내 이벤트가 정상 도착하면 fallback 은 발화하지 않는다.

### 1.4 게이트 상태 머신 (단순) — 내부 플래그 기준

게이트는 별도 컴포넌트가 아니라 **HeroEntryDriver 의 `_marchGateOpen` + AutoCombatAI 의 `_spawnGateOpen` 두 플래그**로 표현된다(둘 다 같은 신호로 동시에 open). BattleController 의 `enabled` 토글과 직교한다.

```
GateState(논리): Closed → Open  (단방향, 풀 재사용 시 OnEnable 에서 Closed 로 리셋)
- Closed (_marchGateOpen=false / _spawnGateOpen=false):
    · HeroEntryDriver: enabled 여도 Update 에서 Stop 유지 (march 안 함)
    · AutoCombatAI(영웅, DeferStrike=true): enabled 여도 교전/이동 보류
    · 몬스터(DeferStrike=false): 게이트 무시 — 현행대로 즉시 동작
- Open:
    · HeroEntryDriver: Center 로 march 시작
    · AutoCombatAI(영웅): 게이트 통과 → 기존 교전 루프 진입
- 전이 트리거: OnSpawnAnimEnd 이벤트  OR  fallback 타이머(1.8s) 만료
- 리셋: 풀 재사용 시 각 컴포넌트 OnEnable 에서 영웅이면 false(Closed), 몬스터면 true(무시)
```

---

## 2. 공격 strike 동기화 (흐름 역전)

### 2.1 현재 동작 (문제)

`AutoCombatAI.Update` (line 111): 교전+쿨다운 충족 시 `_attacker.TryAttack(...)` 이 **그 자리에서 즉시 데미지 적용** + `OnHit` 발행. 애니는 `OnHit` 을 구독한 `CharacterAnimationDriver.HandleAttackHit` 이 **데미지 확정 *후* 후행 재생**. → "데미지 → 스윙" 순서.

### 2.2 결정 — windup → strike(데미지) → recovery

데미지 적용을 애니의 타격 프레임으로 옮긴다. 공격 1사이클을 3구간으로 본다:

```
[Attack 트리거]───windup───[strike 프레임: 데미지 적용]───recovery───[클립 종료]
       ↑ AutoCombatAI 가 발행            ↑ OnAttackStrike 애니 이벤트가 데미지 호출
```

**흐름 역전 상세**:

1. `AutoCombatAI` 가 교전+쿨다운 충족을 판정하면, **데미지를 적용하지 않고** "공격 시작(windup)"만 트리거한다. 이때 **쿨다운 타임스탬프를 이 시점에 찍는다**(다음 공격 판정 기준 = windup 시작 시점, §3.3).
2. 공격 클립이 재생되다가 strike 프레임에 도달하면, 그 클립에 박힌 애니 이벤트 `OnAttackStrike` 가 발화한다.
3. `OnAttackStrike` 수신 컴포넌트가 **실제 데미지(= 기존 `TryAttack` 의 데미지+`OnHit` 부분)를 그 순간 적용**한다.

### 2.3 책임 분리 — 누가 무엇을 하는가

기존 `TryAttack` 한 메서드가 (사거리 검사 + 쿨다운 검사 + 데미지 적용 + OnHit 발행)을 한 번에 했다. 이를 **두 시점으로 분할**한다:

| 단계 | 시점 | 담당 | 행위 |
|---|---|---|---|
| **공격 개시 판정** | windup 시작 | `AutoCombatAI` | 타겟 유효·사거리·쿨다운 검사 통과 → 공격 애니 트리거 + 쿨다운 타임스탬프 기록 + "현재 타겟" 캐싱 |
| **strike 데미지** | strike 프레임 | strike 이벤트 수신 컴포넌트 | 캐싱된 타겟에 데미지 적용 + `OnHit` 발행 (사거리 **재검사** 포함 — §2.5) |

**strike 이벤트 수신 컴포넌트 = `CharacterAttackStrikeRelay`(신규) — Visual(Animator) 자식에 부착 (B4 단정)**.

> 본 단락은 design-reviewer **B4** 를 닫는다. "같은 또는 부모" 양자택일 표현을 제거하고 단정한다.

**확정 배치**: Unity `AnimationEvent` 는 **Animator 와 같은 GameObject 의 MonoBehaviour 메서드만** 호출한다(부모로 전파되지 않음 — 메인 코드 확인). Animator 는 영웅 프리팹의 **`Visual` 자식 GameObject**(루트 `Knight` 가 아님)에 있다. 따라서:

- `CharacterAttackStrikeRelay` 는 **`Visual` 자식 GameObject(= Animator 와 같은 GameObject)에 부착**한다.
- relay 는 `Awake` 에서 `GetComponentInParent<MeleeAttacker>()` / `GetComponentInParent<IAttackGate>()` 로 **루트(Knight)의 게임플레이 컴포넌트를 잡아** strike·end·spawnEnd 를 위임한다.
- 위임 대상: `OnAttackStrike` → 루트 `MeleeAttacker.TryApplyStrike(Time.time)`, `OnAttackEnd` → 루트 게이트 `EndAttack()`, `OnSpawnAnimEnd` → 루트 HeroEntryDriver/AutoCombatAI 게이트 open(§1.2).

**근거**:
- `CharacterAnimationDriver` 는 **표현 전용(View, Rule 02 §6)** 이라 데미지 적용(게임플레이 로직)을 담을 수 없다. strike→데미지 중계는 **별도 얇은 릴레이**로 분리한다.
- relay 가 Visual 자식에 있고 루트를 `GetComponentInParent` 로 참조하므로, AnimationEvent 의 "같은 GameObject only" 제약과 게임플레이 로직의 "루트 소유" 원칙을 동시에 만족한다.
- relay 는 루트 컴포넌트를 **인터페이스/얇은 메서드 호출로만** 참조(Rule 02 §5·§7) — 구체 양방향 참조 없음.

### 2.4 MeleeAttacker 계약 변경 — TryAttack 2단 분할

`MeleeAttacker.TryAttack` 의 단일 책임을 **개시 판정** 과 **strike 데미지** 로 나눈다. 메서드 시그니처 변경은 gameplay-programmer 영역이나, 본 기획서는 **계약(무엇을 보장해야 하는가)** 을 확정한다:

| 신규/변경 메서드 (계약) | 책임 | 반환/효과 |
|---|---|---|
| `bool TryBeginAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)` | 사거리·쿨다운·타겟 유효 검사. 통과 시 **쿨다운 타임스탬프 기록(`_lastAttackTime = now`)** + 타겟 캐싱. **데미지 적용 안 함**. | true = 공격 애니 트리거해도 됨 / false = 보류 |
| `bool TryApplyStrike(float now)` | strike 이벤트 시 호출. 캐싱된 타겟에 **사거리·생존 재검사 후 데미지 적용 + DamageColor 스탬프 + `OnHit` 발행**. | true = 데미지 들어감 / false = 타겟 이탈·사망으로 헛스윙 |

#### 2.4-a CooldownScale / PowerScale 곱셈 계약 (B2 — 적용 시점 단정)

> 본 소절은 design-reviewer **B2** 를 닫는다. 현행 `MeleeAttacker.TryAttack` 은 쿨다운을 `_cooldown * CooldownScale`(L59), 데미지를 `_power * PowerScale`(L65)로 곱한다. 두 배율은 `MonsterBuffService`(광폭화/무력화/약화)가 매 tick 갱신한다. 2분할 후 **각 배율을 어느 시점에 읽는가**를 단정한다.

| 배율 | 현행(`TryAttack`) | 2분할 후 읽는 시점 | 근거 |
|---|---|---|---|
| `CooldownScale` | `now - _lastAttackTime < _cooldown * CooldownScale` 게이트 | **`TryBeginAttack` 시점**(공격 개시 판정) — windup 시작 프레임의 `CooldownScale` 로 쿨다운 게이트 계산 | 쿨다운은 "다음 공격을 언제 시작할 수 있나"의 판정이므로 **개시 시점** 배율이 맞다. strike 시점에 다시 읽으면 이미 시작된 공격을 소급 차단하게 되어 모순 |
| `PowerScale` | `_power * PowerScale` 데미지 | **`TryApplyStrike` 시점**(strike 프레임) — strike 순간의 `PowerScale` 로 데미지 산정 | windup(0.45~0.90s) 동안 영웅에 걸린 무력화/약화 카드 배율이 변할 수 있으므로, **타격이 실제로 들어가는 순간의 배율**이 직관적·정합적 |

- **영웅 한정이라 영향 대상 명시**: 본 흐름 역전은 영웅(`DeferStrike==true`)에만 적용된다(§B). 따라서 위 곱셈 시점 변경은 **영웅에 붙는 카드(무력화·약화 등 영웅 대상 디버프) 기준**으로만 의미가 있다. 몬스터는 `TryAttack`(즉시) 경로를 그대로 타므로 현행 곱셈 시점(즉시 1회)이 보존된다.
- **검산 — 영웅 PowerScale 변동 폭**: 영웅에 약화/무력화가 걸려 `PowerScale` 이 1.0→0.7 로 변하는 경우, windup 0.9s(stab) 사이에 카드가 발동하면 strike 시점 읽기로 0.7 이 반영된다(개시 시점 읽기였다면 1.0 으로 과대 적용). 즉 strike 시점 읽기가 **카드 효과를 더 정확히** 반영한다.

- 기존 `OnHit` 이벤트 계약은 **유지** — 발행 시점이 "데미지 적용 순간"이라는 점은 동일하므로 `OnHit` 구독자(`PlagueSlowOnHit`, 그리고 §M1 의 `AttackJuice`)는 호출 규약 **무변경**. (`OnHit` 은 여전히 "데미지가 실제로 들어간 순간" 발행되며, 그 순간이 strike 프레임으로 옮겨질 뿐.)
- `Range`/`Cooldown`/`Power`(IAttacker) · `PowerScale`(IAttacker) · `CooldownScale`/`DamageColor`(MeleeAttacker 구체 프로퍼티 — §7.4) **계약·값 불변** — 읽는 *시점*만 위 표대로 분할.

### 2.5 strike 시점 사거리 재검사 (헛스윙 규칙)

windup(0.4~0.9s) 동안 타겟이 사거리를 벗어나거나 죽을 수 있다. strike 시점에 `TryApplyStrike` 가 **사거리·생존 재검사**한다:

- 재검사 **통과** → 데미지 적용(정상 타격).
- 재검사 **실패**(타겟 이탈/사망) → 데미지 없음(헛스윙). 애니는 그대로 끝까지 재생(시각상 "허공을 벴다"). 쿨다운은 이미 windup 시작에 찍혔으므로 다음 공격은 정상 재개.

이는 "쿨다운 즉시 데미지" 대비 **약간의 빗맞음(miss)을 새로 도입**한다 — §6 에서 DPS 영향으로 인지하고 후속 검증 대상으로 분리.

### 2.4-b AttackJuice 펀치/플래시도 strike 시점으로 이동 — 의도됨 (M1)

> 본 소절은 design-reviewer **M1** 을 닫는다.

`AttackJuice`(`AttackJuice.cs`)도 `MeleeAttacker.OnHit` 을 구독해 **스케일 펀치(×1.15, 0.12s) + 흰색 플래시(HitFlash.FlashAttack)**를 재생한다(L41·L109~116). 본 enhancement 가 `OnHit` 발행을 windup 시작 → strike 프레임으로 옮기므로, **펀치/플래시 연출도 자동으로 strike 시점으로 이동**한다(코드 변경 없이 구독 지점이 strike 로 옮겨진 결과).

- **이 이동은 의도된 것이다**: 펀치(영웅 몸이 살짝 커졌다 줄어듦)와 흰색 플래시가 "칼이 적에 닿는 타격 순간"에 터지는 것이 시각적으로 자연스럽다. 현행(즉시 데미지 = 모션 시작 전)에는 펀치가 스윙보다 먼저 터져 "때리기 전에 움찔"하는 어색함이 있었다. strike 시점 이동으로 **타격감이 오히려 개선**된다.
- **AttackJuice 코드 무변경**: `OnHit` 구독 규약이 그대로이므로 `AttackJuice` 는 손대지 않는다. 영웅 한정(`DeferStrike==true`)이라 몬스터의 `AttackJuice`(있다면)는 여전히 즉시 펀치 — 현행 보존.

### 2.6 strike 프레임 측정 정책

**정의**: strike 프레임 = "영웅의 칼/손이 타격 사거리(target)에 닿는 프레임" — 즉 시각적으로 무기가 가장 앞으로 뻗어 적에 닿는 순간. 클립별로 육안 측정한다(메인이 Unity 에서 수행).

**초기 추정값 (육안 보정 대상 — 후보)**:

| 클립 | 길이 | strike 추정 normalizedTime | strike 추정 프레임(@~30fps) | 비고 |
|---|---|---|---|---|
| `Skeleton_slash01` | 34f (~1.13s) | **~0.40** | ~14f (~0.45s) | 횡베기 — 칼이 정면 지나는 중반 |
| `Skeleton_slash02` | 34f (~1.13s) | **~0.40** | ~14f (~0.45s) | 반대 횡베기 — slash01 대칭 |
| `Skeleton_stab` | 49f (~1.63s) | **~0.55** | ~27f (~0.90s) | 찌르기 — 팔이 최대로 뻗는 후반부 |

> 위 값은 **추정 후보**다. 메인이 Unity 에서 각 클립을 재생하며 칼/손이 닿는 프레임을 육안 확정한 뒤 그 프레임에 `OnAttackStrike` 이벤트를 박는다. 확정값이 추정과 다르면 **확정값이 단일 진실**이며 본 표는 참고로만 남는다.

> **m1 — 프레임 환산 단서**: 위 "프레임(@~30fps)" 환산은 클립의 **실제 임포트 fps 를 확인한 뒤** 유효하다(`Skeleton_*` 클립이 24/30/60fps 중 무엇으로 임포트됐는지 ModelImporter 에서 확인). 임포트 fps 가 30 이 아니면 프레임 수치는 달라진다. **단일 진실은 프레임 수가 아니라 `AnimationEvent.time`(초) 값**이며, 박는 순간은 normalizedTime(0~1)·초로 지정한다 — 본 표의 프레임 칸은 참고 보조다.

**저장 위치**: strike 프레임 정보는 **애니 이벤트 키 자체에 박힌다**(클립의 `AnimationEvent.time`). 별도 SO/데이터 필드에 normalizedTime 을 중복 저장하지 않는다 — 단일 진실은 클립의 이벤트 키. (튜닝은 클립 이벤트 키를 옮기는 것으로 한다.)

**베이크 방식**: FBX 임포터의 `ModelImporter.clipAnimations` → 각 `ModelImporterClipAnimation.events` 에 `AnimationEvent { functionName = "OnAttackStrike", time = <strike초> }` 추가. (gameplay-programmer 가 에디터 스크립트 또는 인스펙터 Animation 탭에서 베이크 — §7.)

### 2.7 영웅 공격 애니 트리거 주체 — 개시 시점 단정 + strike 순환 방지 (rev3 권장1)

> 본 소절은 design-reviewer 비차단 **권장1** 을 닫는다. "흐름 역전 후 영웅 공격 애니를 *누가·언제* 트리거하는가"를 단정한다.

**문제 — OnHit 에 애니 트리거가 묶이면 순환**: 현재 영웅은 `CharacterAnimationDriver.HandleAttackHit`(L86)이 `MeleeAttacker.OnHit` 을 구독 → `controller.OnAttack(now)` → `TriggerAttack(variant)` 로 **공격 애니를 트리거**한다. 흐름 역전(§2) 후 영웅의 `OnHit` 은 **strike 시점**(데미지 적용 순간)에 발행되므로, 애니 트리거가 `OnHit` 에 그대로 묶여 있으면 "strike → 애니 트리거 → 또 windup → 또 strike" **순환**이 된다. 따라서 영웅(`DeferStrike==true`)의 공격 애니 트리거는 `OnHit`(strike 후행)이 아니라 **`TryBeginAttack` 성공(개시) 시점**에 발생해야 한다.

**결정 — 개시 트리거 주체 = `AutoCombatAI` 가 판정, 애니 출력은 게이트 경유로 Driver 가 수행**:

| 단계 | 주체 | 행위 |
|---|---|---|
| ① 개시 판정 | `AutoCombatAI`(게임플레이) | 교전 분기에서 `TryBeginAttack` 성공 시 `IAttackGate.BeginAttack()` 호출(§3.2) |
| ② 애니 트리거 발행 | `IAttackGate.BeginAttack()` 구현(루트, 영웅만) | `BeginAttack()` 안에서 `IsAttacking=true` 와 함께 **공격 애니 트리거 신호를 발행**한다. 신호 전달 형태(게이트의 `event OnAttackBegin` 을 Driver 가 구독 vs 게이트가 Driver 트리거 진입점을 직접 호출)는 gameplay-programmer 재량 — 단 **계약**은 "개시 시점에 정확히 1회 애니 트리거 신호가 Driver 로 전달된다". 게이트는 게임플레이 판정(사거리·쿨다운)을 **하지 않는다** — 이미 `TryBeginAttack` 이 통과시킨 결과만 받아 애니 신호를 낸다 |
| ③ Animator 출력 | `CharacterAnimationDriver` → `CharacterAnimationController.OnAttack` → `_sink.TriggerAttack(variant)` | 표현 전용. variant(0~2) 랜덤 선택은 현행 `OnAttack` 그대로 |

- **`HandleAttackHit`(OnHit→애니 트리거) 영웅 우회**: 영웅에서 `CharacterAnimationDriver.HandleAttackHit`(L86)의 **OnHit→`controller.OnAttack` 경로는 애니 트리거에 쓰지 않는다**. 영웅의 `OnHit` 은 **데미지 연출 전용**(`AttackJuice` 펀치/플래시 §2.4-b, `PlagueSlowOnHit`)으로만 남는다. 구현 형태(영웅이면 `OnHit` 구독을 애니 트리거에서 분리 / Driver 가 게이트의 개시 신호를 별도 구독)는 gameplay-programmer 영역이나, **계약은 "영웅 공격 애니 트리거는 개시(②) 경로에서만 1회, OnHit(strike) 경로에서는 트리거하지 않음"** 으로 단정한다.
- **몬스터(`DeferStrike==false`) 무관**: 몬스터 6종은 공격 애니 와이어링 자체가 없으므로(선행 §3.2) `OnHit→OnAttack` 현행 경로를 그대로 둔다(애니 sink 가 없어 시각 변화 없음). 즉 `HandleAttackHit` 코드 자체는 **몬스터용으로 보존**되고, 영웅에서만 애니 트리거 경로가 개시(②)로 옮겨진다.
- **Rule 02 §6 정합(한 줄)**: View(`CharacterAnimationDriver`/`CharacterAnimationController`)는 게임플레이 판정을 하지 않는다 — 사거리·쿨다운·타겟 유효 판정은 모두 `AutoCombatAI`+`MeleeAttacker.TryBeginAttack`(게임플레이)이 끝내고, 게이트는 그 **결과 신호만** Driver 로 전달한다. Driver 는 받은 신호로 `TriggerAttack`(Animator 출력)만 수행 → 표현 전용 경계 유지.

> **트리거 주체 결론**: 영웅 공격 애니 = `AutoCombatAI` 가 `TryBeginAttack` 성공으로 개시 판정 → `IAttackGate.BeginAttack()` 이 개시 신호 발행 → `CharacterAnimationDriver`(View)가 `TriggerAttack` 출력. `OnHit`(strike) 경로는 영웅 애니 트리거에서 제외(연출 전용). 순환 차단.

---

## 3. 공격 중 상태 규칙 (애니 > 쿨다운 충돌 해소)

### 3.1 충돌의 본질

- 공격 쿨다운 = **1.0s** (`MeleeAttacker._cooldown`).
- 공격 애니 길이 = slash **1.13s** / stab **1.63s** → **애니가 쿨다운보다 길다**.
- 흐름 역전(§2) 후, 만약 쿨다운만 보고 다음 공격을 트리거하면: slash 재생 중(1.13s) 쿨다운(1.0s)이 차서 **이전 공격 애니가 끝나기 전에 다음 공격이 트리거** → 애니가 중간에 끊기고 windup 만 반복돼 strike 가 영영 안 나올 수 있다.

### 3.2 결정 — 공격 중 상태(IsAttacking) 게이트 + 소유자 확정 (M2)

> 본 절은 design-reviewer **M2** 를 닫는다. 게이트의 **소유 컴포넌트**와 **참조 경로**를 확정한다.

영웅에 **"공격 모션 재생 중"(`IsAttacking`) 상태**를 둔다. 이 상태는 windup 시작 ~ 클립 종료(`OnAttackEnd` 이벤트)까지 true.

**소유자·참조 경로 (M2 단정)**:

- `IsAttacking` 상태는 **루트(Knight)에 부착된 `IAttackGate` 구현 컴포넌트가 소유**한다(`bool IsAttacking { get; }` · `void BeginAttack()` · `void EndAttack()`).
- `IAttackGate` 컴포넌트는 **영웅 프리팹에만 부착**한다. 몬스터 6종은 미부착 → 아래 AutoCombatAI 분기에서 게이트가 null 이라 **보류 로직 자체가 안 걸림**(현행 동작 보존).
- `AutoCombatAI` 는 `Awake` 에서 `GetComponent<IAttackGate>()` 로 잡는다(없으면 null). 교전 분기에서:
  - 게이트 null(몬스터) → 현행 즉시 공격(§B.2 `DeferStrike==false` 경로와 함께).
  - 게이트 존재(영웅) → `IsAttacking==true` 동안 **다음 공격 트리거 보류 + 이동 정지**.
- `BeginAttack()` 은 `TryBeginAttack` 성공 직후 호출(IsAttacking=true) + **공격 애니 트리거 개시 신호 발행**(§2.7 ②). `EndAttack()` 은 `OnAttackEnd` relay(§B4)가 호출(IsAttacking=false).

| 규칙 | 내용 |
|---|---|
| **다음 공격 보류** | 게이트 존재 && `IsAttacking == true` 동안 `AutoCombatAI` 는 새 공격을 트리거하지 않는다(쿨다운이 차도 보류). 클립 종료(`OnAttackEnd`) 후에만 다음 공격 개시 판정 재개. |
| **이동 정지** | 게이트 존재 && `IsAttacking == true` 동안 영웅은 이동하지 않는다(`Stop` 유지). 교전 중이므로 어차피 정지 상태지만, windup~recovery 동안 회전/이동 명령도 보류해 모션이 흔들리지 않게 한다. (회전은 strike 전까지 타겟 방향 1회 스냅만 허용 — §4.) |
| **클립 종료 신호** | 각 공격 클립 마지막 프레임에 `OnAttackEnd` 애니 이벤트를 박아 relay → 루트 게이트 `EndAttack()` → `IsAttacking = false`. (strike 이벤트 `OnAttackStrike` 와 별개 — strike 는 클립 중반, end 는 클립 끝.) |

### 3.3 쿨다운 해석 보강 — "쿨다운 만료 AND 공격 안 하는 중"

기존 쿨다운 검사(`now - _lastAttackTime >= _cooldown * CooldownScale`)는 유지하되(영웅은 §B.2 대로 `TryBeginAttack` 내부에서 검사), **영웅 공격 개시 조건에 게이트들을 AND 로 추가**한다:

```
영웅(DeferStrike==true) 공격 개시 가능
  = (사거리 OK) AND (쿨다운 만료) AND (IAttackGate.IsAttacking == false) AND (_spawnGateOpen)
몬스터(DeferStrike==false) = 현행 그대로 (게이트 AND 항 없음)
```

- slash(1.13s)의 경우: 쿨다운 1.0s 는 클립 종료(1.13s)보다 먼저 차지만, `IsAttacking` 이 1.13s 까지 true 라 **실질 공격 주기는 ~1.13s** 로 늘어난다(클립 길이가 하한).
- stab(1.63s): 실질 주기 ~1.63s.
- 따라서 **공격 주기 = max(쿨다운 1.0s, 클립 길이)** 가 된다. 클립이 쿨다운보다 길므로 클립 길이가 지배 → 공격 빈도가 현행보다 낮아진다.

**대안 비교 (충돌 해소 방식)**:

| 안 | 방식 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| A | `IsAttacking` 게이트(클립 끝까지 보류) | 모션이 항상 끝까지 재생, 구현 단순, 데이터 변경 0 | 공격 주기가 클립 길이로 늘어 DPS 하락(slash 1.0→1.13s, stab 1.0→1.63s) | **✓ 권장** — 동작 안정성·시각 완결성 우선, DPS 보정은 §6 후속 |
| B | 쿨다운을 클립 길이에 맞춰 상향(slash 1.13/stab 1.63) | 쿨다운 값이 실제 주기와 일치(명시적) | 클립별 쿨다운 분기 필요, 게임플레이 밸런스 값(쿨다운) 변경 → 밸런스 재검증 트리거 | ✗ (값 변경은 후속 밸런스 사이클) |
| C | strike 후 recovery 캔슬(쿨다운 차면 recovery 끊고 다음 windup) | 공격 주기 ~1.0s 유지(DPS 보존) | 모션이 매번 중간에 끊겨 "뚝뚝 끊기는" 시각, recovery 가 안 보임 | ✗ (시각 완결성 훼손 — 본 enhancement 의 목적과 상충) |

**권장 = A**. 이유: 본 enhancement 의 목적이 "애니와 게임플레이의 자연스러운 동기화"이므로 모션 완결성(끝까지 재생)이 최우선. DPS 하락은 §6 에서 쿨다운 값 조정(안 B 의 값만 빌림) 또는 클립 재생속도 조정으로 후속 보정하되, **그 결정은 qa-simulator 데이터 후에** 내린다.

### 3.4 공격 중 피격 — 선행 기획서 §2.2 정합 + 필드 겸용 금지 (M3)

> 본 절은 design-reviewer **M3** 을 닫는다. **`_attackSuppressWindow`(0.5s) 를 fallback 타임아웃으로 겸용하지 않는다** — 겸용 시 피격 리액션이 1.8s 억제되어 선행 기획서 §2.1(피격 가독성: 초당 2.5회 리액션) 과 선행 PlayMode 테스트(`HeroAnimationDriverTests`)의 attackSuppress 가정이 깨진다.

본 enhancement 의 `IsAttacking` 상태는 선행 0.5s 윈도우보다 길게(클립 전체 1.13~1.63s) 공격을 보호한다. 정합 규칙을 **두 개의 독립 메커니즘으로 분리**한다:

**(1) 피격 Hit 억제 기준 — `IsAttacking` 플래그 (영웅 한정)**

- 영웅의 피격 `Hit` 억제는 **`IsAttacking == true` 동안** 적용한다(공격 모션 내내 `Hit` 리액션 억제 → 스윙이 안 끊김).
- 단 이는 **영웅 한정**이다(`IAttackGate` 가 영웅에만 부착, §M2). `CharacterAnimationController.OnDamaged` 가 게이트를 참조하려면 영웅의 `CharacterAnimationDriver` 가 게이트 상태를 controller 에 넘긴다.
- **선행 `_attackSuppressWindow`(0.5s) 필드 값은 0.5 그대로 유지**한다 — 의미·값 변경 0. `IsAttacking` 억제가 추가로 얹히는 형태이며, 선행 테스트의 0.5s 가정·동작은 보존된다. (영웅이 공격 중이 아닐 때는 기존 0.5s 윈도우가 그대로 작동.)
- 단 `Dead` 는 여전히 최우선(AnyState 인터럽트) — `IsAttacking` 중에도 사망 시 즉시 death 로 전이.

**(2) 공격 end 누락 fallback 타임아웃 — 별도 신규 필드 `_attackEndFallback`(1.8s)**

- `OnAttackEnd` 이벤트가 클립 교체 등으로 유실되면 `IsAttacking` 이 영영 true 로 굳어 영웅이 공격을 멈춘다. 이를 막는 안전 타임아웃은 **`_attackSuppressWindow` 겸용이 아니라 신규 SerializeField `_attackEndFallback`(기본 1.8s)** 로 분리한다.
- `BeginAttack()` 후 `_attackEndFallback` 초가 지나도 `OnAttackEnd` 가 안 오면 게이트가 `IsAttacking` 강제 해제. 검산: stab 1.63s + 0.17s 마진 = 1.8s.
- 소유: 이 fallback 타이머는 게이트(`IAttackGate` 구현)가 가진다 — 게이트가 `BeginAttack` 시점을 알고 `IsAttacking` 을 소유하므로 fallback 도 같은 곳에 둔다.

> **요약(M3)**: `_attackSuppressWindow` = 0.5(불변, 피격 억제 보조) / `_attackEndFallback` = 1.8(신규, 공격 end 안전망) — **두 필드 분리**. 겸용으로 0.5→1.8 상향하지 않는다.

---

## 4. 회전 즉시 스냅

### 4.1 현재 동작

`SimpleRotator`(6종 공통)가 `_turnSpeedDegPerSec = 540` 으로 `Update` 에서 `MoveTowardsAngle` 보간. 540°/s = 180°를 0.33s. 자동전투에서 타겟이 빠르게 바뀌면 영웅이 "빙글 도는" 잔상.

### 4.2 결정 — 즉시 스냅 (영웅 한정, m2)

> 본 절은 design-reviewer **m2** 를 닫고 **§B(B1)** 의 회전 한정 규칙을 구현한다. `SimpleRotator` 는 **6종 공통**이므로 무조건 보간을 제거하면 몬스터 회전감까지 바뀐다. 회전 즉시 스냅은 **영웅 한정**이다.

**대안 비교**:

| 안 | 방식 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| A | `TurnSpeedDegPerSec` 를 매우 큰 값(예 9999)으로 | 코드 변경 0(인스펙터 값만) | 여전히 deltaTime 의존(1프레임 지연), 매직넘버, **영웅/몬스터 구분 불가**(영웅 인스턴스만 9999 줘야 하는데 의미 불명확) | △ |
| B | `SimpleRotator` 에 `_snapInstant` bool(기본 false=보간). true 면 `FaceDirection` 이 즉시 `ApplyYaw`. **영웅 프리팹만 true** | 보간 완전 제거(1프레임 지연 0), 영웅/몬스터 명시 분기, 몬스터는 보간 보존 | `SimpleRotator` 에 분기 if 1개 + 필드 1개 추가 | **✓ 권장** |

**권장 = B**. `SimpleRotator` 에 **`[SerializeField] bool _snapInstant`**(기본 **false = 현행 보간**)를 추가한다. 영웅(`Knight.prefab`)의 `SimpleRotator` 만 인스펙터에서 **true**.

- `_snapInstant == false`(몬스터 6종): 현행 그대로 — `FaceDirection` 은 목표 yaw 저장, `Update` 가 `MoveTowardsAngle` 보간. **무변경**.
- `_snapInstant == true`(영웅): `FaceDirection` 호출 시 목표 yaw 를 **즉시 `ApplyYaw`** 적용(현 `SnapToDirection` 의 ApplyYaw 경로). `Update` 의 보간 step 은 영웅에 대해 사실상 즉시 도달(또는 `_snapInstant` 면 Update 보간 skip).
- `SnapToDirection`(OnEnable 초기 스냅)·`ApplyYaw`(Rigidbody MoveRotation) 경로는 **양쪽 공통으로 그대로**.
- `IRotator.TurnSpeedDegPerSec` 프로퍼티 계약은 **유지**(인터페이스 변경 없음) — 영웅(`_snapInstant=true`)에서는 무시되는 값이 되고, 몬스터에서는 현행대로 보간 속도로 쓰인다. 인터페이스를 깨지 않으려 프로퍼티는 남기고, 주석으로 "`_snapInstant=true` 시 미사용"을 명시.
- **공격 중 회전 정책(§3.2 정합)**: `IsAttacking == true` 동안은 `FaceDirection` 호출을 `AutoCombatAI` 가 보류하므로(이동 정지 규칙), 공격 windup 직전 타겟 방향으로 1회 스냅된 뒤 strike 까지 그 방향 고정 — 즉시 스냅이라도 공격 중 빙빙 도는 문제 없음.

### 4.3 물리 정합 (기존 유지)

`ApplyYaw` 의 Rigidbody `MoveRotation` 경로는 유지(transform.rotation 직접 쓰기가 MovePosition 을 덮는 문제 방지 — 기존 주석). 즉시 스냅도 동일 경로로 적용한다.

---

## 5. 전체 흐름 통합도 (영웅 = `DeferStrike==true` 경로)

> BattleController 호출(`enabled=true/false`)은 현행 그대로(§B3·§1.2). 게이트는 컴포넌트 내부 플래그. Animator/relay 는 Visual 자식, 게임플레이는 루트(§B4).

```
[영웅 Pop]  (BattleController.SpawnHero — 무수정)
  └ AutoCombatAI.enabled=false, HeroEntryDriver.enabled=true   (현행 호출 그대로)
  └ CharacterAnimationDriver.OnEnable → OnSpawn() (Visual Animator: spawn 애니 재생)
  └ 게이트 Closed: HeroEntryDriver._marchGateOpen=false / AutoCombatAI._spawnGateOpen=false
      └ HeroEntryDriver.Update: Stop 유지 (march 안 함)
      │
      │  spawn 클립 마지막 프레임
      ▼
[Visual: OnSpawnAnimEnd 이벤트  또는  fallback 1.8s]
  └ CharacterAttackStrikeRelay(Visual) → GetComponentInParent 로 루트 게이트 open
  └ _marchGateOpen=true / _spawnGateOpen=true → HeroEntryDriver march 시작
      │
      │  zone Center 도달
      ▼
[HeroEntryDriver → BattleZone.NotifyHeroReachedCenter]
  └ BattleController.HandleHeroReachedCenter → AutoCombatAI.enabled=true + BattleClock.Start  (무수정)
      │   (zone==null 폴백: EnableHeroAIAfterDelay(3f) → enabled=true, 단 _spawnGateOpen 존중)
      ▼
[AutoCombatAI.Update — 교전 루프, DeferStrike==true]
  교전 && _spawnGateOpen && IAttackGate.IsAttacking==false
    └ MeleeAttacker.TryBeginAttack (사거리·쿨다운[CooldownScale] 검사, 데미지 X, 쿨다운 기록, 타겟 캐싱)
    └ 성공 시: IAttackGate.BeginAttack() (IsAttacking=true) + 공격 애니 트리거(variant 0~2)
        │  windup
        ▼
  [Visual: OnAttackStrike 이벤트 (strike 프레임)]
    └ CharacterAttackStrikeRelay → GetComponentInParent<MeleeAttacker>().TryApplyStrike(now)
        └ 사거리·생존 재검사 통과 시: 데미지(_power*PowerScale[strike시점]) + DamageColor + OnHit 발행
        └ OnHit 구독자: PlagueSlowOnHit + AttackJuice(펀치/플래시, §M1)
        │  recovery
        ▼
  [Visual: OnAttackEnd 이벤트 (클립 끝)  또는  _attackEndFallback 1.8s]
    └ relay → IAttackGate.EndAttack() (IsAttacking=false) → 다음 공격 개시 판정 재개

[몬스터 6종 = DeferStrike==false] — 위 전체 분기 우회: 교전 시 TryAttack 즉시 데미지(현행 100% 보존)
```

---

## 6. 밸런스 영향 (후순위 — qa-simulator 분리)

본 enhancement 는 데미지 적용 시점을 늦추고 공격 주기를 클립 길이로 늘리므로 **영웅 실효 DPS 가 하락**한다. 정량 인지:

| 변화 | 현행 | 변경 후 | 추정 영향 |
|---|---|---|---|
| 공격 주기 | 쿨다운 1.0s | max(1.0s, 클립 길이) = slash 1.13s / stab 1.63s | 공격 빈도 ~11%(slash) ~ ~38%(stab) 감소 |
| 데미지 지연 | 0s (즉시) | windup 0.45~0.90s | 첫 타격이 늦게 들어감(교전 시작 직후 영향) |
| 헛스윙(miss) | 없음 | windup 중 타겟 이탈 시 발생 | 추가 DPS 손실(빈도 미측정) |

- **stage_goal 영향 한 줄**: "5분 자동전투 + HP%/시간 트리거 선택지" 의 핵심 루프는 유지되나, **영웅 처치 시간(=5분 안에 잡는가)** 메트릭이 영웅 DPS 가 아닌 *던전 측 DPS* 에 좌우되므로 영웅 공격 빈도 하락은 stage_goal 검증을 **오히려 미세하게 쉽게**(영웅이 덜 강해짐) 만든다 → 검증 방향에 치명적이지 않다. 단 영웅 평타가 몬스터를 솎는 양이 줄어 무리 생존이 늘 수 있으므로 페이싱 재확인 필요.
- **결정**: 수치 튜닝(쿨다운 값·클립 재생속도·strike 프레임)은 **본 기획서에서 변경하지 않는다**. 구현·육안 검증 후 desync 가 해소된 상태에서 qa-simulator 로 다음 메트릭을 측정해 후속 밸런스 사이클(`.claude/project.md` "밸런스 조정 흐름")로 분리한다:
  - 영웅 평균 공격 횟수/분, 헛스윙 비율, 평균 영웅 처치 시간(승률), 무리 평균 생존 수.
  - 측정 결과 영웅 DPS 하락이 페이싱을 깬다고 판단되면: ① slash/stab 쿨다운을 클립 길이에 맞춰 조정 또는 ② 클립 재생속도(Animator speed) 상향으로 windup 단축 — 둘 중 데이터 기반 선택.

---

## 7. 구현 요청사항 (gameplay-programmer 용)

> 코드 구조·시그니처는 gameplay-programmer 판단 영역. 본 절은 **메커니즘·계약·에셋 베이크 지점**을 명세한다.

### 7.1 애니 이벤트 베이크 (FBX 임포터 — `ModelImporter.clipAnimations`)

각 클립의 `ModelImporterClipAnimation.events` 에 `AnimationEvent` 추가:

| 클립 | 이벤트 functionName | 박을 프레임/시점 | 용도 |
|---|---|---|---|
| `Skeleton_spawn` | `OnSpawnAnimEnd` | 마지막 프레임(40f) | 스폰 게이트 해제(§1) |
| `Skeleton_slash01` | `OnAttackStrike` | strike 프레임(추정 ~0.40 nt, 육안 확정) | 데미지 적용(§2) |
| `Skeleton_slash01` | `OnAttackEnd` | 마지막 프레임(34f) | IsAttacking 해제(§3) |
| `Skeleton_slash02` | `OnAttackStrike` | strike 프레임(추정 ~0.40 nt) | 〃 |
| `Skeleton_slash02` | `OnAttackEnd` | 마지막 프레임(34f) | 〃 |
| `Skeleton_stab` | `OnAttackStrike` | strike 프레임(추정 ~0.55 nt) | 〃 |
| `Skeleton_stab` | `OnAttackEnd` | 마지막 프레임(49f) | 〃 |

> strike 프레임의 추정 normalizedTime 은 §2.6 의 육안 보정 대상. `OnAttackEnd` 는 클립 마지막 프레임 고정.

### 7.2 흐름 역전 대상 메서드 (영웅 한정 — `DeferStrike` 분기)

> BattleController 는 **무수정**(§B3). AutoCombatAI 교전 분기만 `DeferStrike`/게이트로 분기.

| 위치 | 현행 | 변경 |
|---|---|---|
| `AutoCombatAI.Update` (교전 분기, L106~112) | `_attacker.TryAttack(...)` (즉시 데미지) | `_attacker.DeferStrike==false`(몬스터) → **현행 그대로** `TryAttack`. `DeferStrike==true`(영웅) → `_spawnGateOpen && IAttackGate.IsAttacking==false` 일 때만 `TryBeginAttack(...)` → 성공 시 `IAttackGate.BeginAttack()` + 공격 애니 트리거. 데미지 적용 안 함 (§B.2) |
| `MeleeAttacker.TryAttack` | 검사+데미지+OnHit 일체 (몬스터 계속 사용) | **유지** + 2분할 메서드 **추가**: `TryBeginAttack`(검사+`CooldownScale` 쿨다운기록+타겟캐싱) / `TryApplyStrike`(사거리·생존 재검사+`PowerScale`[strike시점] 데미지+DamageColor 스탬프+OnHit) — §2.4·§2.4-a 계약. **`TryAttack` 자체는 제거 금지**(몬스터 6종이 그대로 호출) |
| `MeleeAttacker.DeferStrike` | — | **신규 SerializeField bool**(기본 false). 영웅 프리팹만 true (§B.2) |
| `CharacterAnimationDriver.HandleAttackHit` (OnHit 구독으로 후행 재생, L86) | OnHit → controller.OnAttack(애니 트리거) | **영웅 분기(§2.7)**: 영웅 공격 애니 트리거는 OnHit 후행이 아니라 `TryBeginAttack` 성공(개시) 시점에 `IAttackGate.BeginAttack()` 개시 신호 → Driver 가 `TriggerAttack` 출력. 영웅에서 OnHit→`controller.OnAttack` 애니 트리거 경로는 **우회**(OnHit 은 strike 데미지 연출 = 펀치/플래시·플레이그 전용). 몬스터는 현행 OnHit→OnAttack 유지(애니 와이어링 없는 현 상태와 정합) |

### 7.3 신규 컴포넌트 / 인터페이스 / 플래그

| 항목 | 종류 | 책임 |
|---|---|---|
| `MeleeAttacker.DeferStrike` | bool SerializeField (신규) | 영웅 한정 흐름 역전 스위치. 기본 false=즉시 데미지(몬스터). 영웅 프리팹만 true (§B.2) |
| `CharacterAttackStrikeRelay` | MonoBehaviour (신규) | 애니 이벤트 `OnAttackStrike`/`OnAttackEnd`/`OnSpawnAnimEnd` 수신구. **Visual(Animator) 자식 GameObject 에 부착**(§B4). `Awake` 에서 `GetComponentInParent` 로 루트 `MeleeAttacker`/`IAttackGate`/게이트 참조. strike→`TryApplyStrike(Time.time)`, end→`IAttackGate.EndAttack()`, spawnEnd→루트 게이트 open |
| `IAttackGate` | Interface (신규, `CommonInterface.cs` `Lair.Character`) | 영웅 공격 게이트. `bool IsAttacking { get; }` · `void BeginAttack()` · `void EndAttack()`. 루트에 부착(영웅 프리팹만). `AutoCombatAI` 가 `GetComponent<IAttackGate>()`(null=몬스터), relay 가 `GetComponentInParent<IAttackGate>()` 로 공유. `BeginAttack()` 은 IsAttacking=true + **공격 애니 트리거 개시 신호 발행**(§2.7 ② — Driver 가 구독/호출해 `TriggerAttack` 출력). 게이트는 게임플레이 판정을 하지 않고 개시 결과 신호만 중계. 단일 구현체면 internal 허용 — Rule 02 §9 |

> **인터페이스 분리는 권고**. gameplay-programmer 가 단일 구현체로 판단하면 Rule 02 §9 에 따라 internal 로 둘 수 있다. 단 `AutoCombatAI`(게임플레이)와 `CharacterAttackStrikeRelay`(애니 이벤트 수신)가 구체 클래스로 양방향 직접 참조하지 않도록 인터페이스/이벤트 경유 권고(Rule 02 §5·§7). 스폰 게이트는 별도 인터페이스를 신설하지 않고 **HeroEntryDriver `_marchGateOpen` + AutoCombatAI `_spawnGateOpen` 내부 플래그**로 둔다(§1.2·§1.4) — relay 가 `GetComponentInParent` 로 두 컴포넌트의 open 메서드를 호출.

### 7.4 IAttacker 계약 보강

- `MeleeAttacker` 에 `TryBeginAttack`/`TryApplyStrike` **추가**(`TryAttack` 은 몬스터용으로 유지). `IAttacker` 인터페이스(`CommonInterface.cs` `Lair.Character`)에 두 메서드 + `bool DeferStrike { get; }` 추가. `IAttacker` 의 기존 계약 `OnHit`·`Range`·`Cooldown`·`Power`·`PowerScale`·`Enabled` 불변.
- **표기 정정(rev3 권장2)**: `CooldownScale` 은 `IAttacker` 인터페이스 멤버가 **아니다** — `MeleeAttacker` 의 **구체 프로퍼티**(`MeleeAttacker.cs` L27 `public float CooldownScale { get; set; }`)다. `IAttacker`(`CommonInterface.cs`)에는 `PowerScale` 만 있다. 따라서 "`CooldownScale` 계약 불변"은 **`MeleeAttacker` 구체 프로퍼티의 값·의미 불변**을 뜻하며, 인터페이스 계약 항목이 아니다. (`DamageColor` 도 동일하게 `MeleeAttacker` 구체 프로퍼티 — 인터페이스 멤버 아님, 값·의미 불변.)
- 곱셈 시점: `MeleeAttacker.CooldownScale`(구체) 은 `TryBeginAttack`, `IAttacker.PowerScale` 은 `TryApplyStrike` 에서 읽음 (§2.4-a).
- `OnHit` 발행 시점은 "데미지 실제 적용 순간"(영웅은 strike 프레임 / 몬스터는 즉시)으로 유지 → 구독자(`PlagueSlowOnHit`·`AttackJuice`) 무변경.
- **PlayMode 테스트 영향(M3 정합)**: `HeroAnimationDriverTests` 의 `SubscriptionCountingAttacker` 가 `IAttacker` 를 구현하므로, 인터페이스에 `TryBeginAttack`/`TryApplyStrike`/`DeferStrike` 추가 시 **fake 도 멤버 추가 필요**(test-engineer 단계). 단 fake 의 `DeferStrike=false` 기본이면 기존 `OnHit→OnAttack` 구독 검증 흐름은 보존된다.

### 7.5 회전 변경 (영웅 한정 — `_snapInstant`)

- `SimpleRotator` 에 `[SerializeField] bool _snapInstant`(기본 **false=현행 보간**) 추가. 영웅 프리팹만 true (§4.2·§B.3).
- `_snapInstant==true`: `FaceDirection` 즉시 `ApplyYaw`(보간 skip). `_snapInstant==false`(몬스터 6종): 현행 `MoveTowardsAngle` 보간 **무변경**.
- `SnapToDirection`·`ApplyYaw`(Rigidbody MoveRotation) 경로 양쪽 공통 유지. `IRotator.TurnSpeedDegPerSec` 계약 유지(영웅에서 미사용, 주석 명시 / 몬스터에서 현행대로 사용).

### 7.6 SerializeField / 수치 (변경분)

| 필드 | 위치 | 현행 | 변경 | 근거 |
|---|---|---|---|---|
| `_attackSuppressWindow` | `CharacterAnimationDriver` | 0.5 | **0.5 불변** (겸용 금지 — M3) | §3.4 — fallback 으로 재사용하지 않음 |
| `_attackEndFallback` | 신규 SerializeField (`IAttackGate` 구현 / 영웅) | — | **1.8** (공격 end 누락 안전 타임아웃, M3) | §3.4 검산: stab 1.63 + 0.17 |
| 스폰 게이트 fallback `_spawnGateFallback` | 신규 SerializeField (`HeroEntryDriver` 또는 게이트) | — | **1.8** | §1.3 검산: spawn 1.33 × 1.35 |
| `DeferStrike` | `MeleeAttacker` | — | 영웅 **true** / 몬스터 false (§B.2) | 흐름 역전 영웅 한정 스위치 |
| `_snapInstant` | `SimpleRotator` | — | 영웅 **true** / 몬스터 false (§4.2) | 회전 스냅 영웅 한정 스위치 |
| `_turnSpeedDegPerSec` | `SimpleRotator` | 540 | **불변**(영웅은 `_snapInstant` 로 미사용, 몬스터는 현행) | §4.2 |
| `_hitReactionCooldown` / `_walkSpeed` / `_runSpeed` | `CharacterAnimationDriver` | 0.4 / 1.0 / 2.0 | **불변** | 선행 기획서 §2 |

- **게임플레이 수치(Power/Range/Cooldown 값) 변경 0** — §0·§6.
- 신규 ScriptableObject 없음. BalanceConfig.asset 미수정.

### 7.7 Enum

- **신규 Enum 추가 없음.** `EHero.Knight` 불변. 애니 이벤트 functionName(`OnSpawnAnimEnd`/`OnAttackStrike`/`OnAttackEnd`)은 문자열 계약(Animator 이벤트 호출 규약)이라 Enum 대상 아님 — 단 `CharacterAttackStrikeRelay` 의 메서드명과 **글자 그대로 일치** 필수.

---

## 8. Self-Review (rev2 — BLOCKER 4 + MAJOR 3 + MINOR 2 보강 반영)

**design-reviewer 지적 닫힘 매핑**:

| 지적 | 닫은 위치 | 핵심 |
|---|---|---|
| **B1** 공통 컴포넌트 영웅 한정 분기 | §B(신규) | `MeleeAttacker.DeferStrike` 플래그(영웅만 true). 몬스터는 `TryAttack` 즉시 경로 100% 보존. 회전 스냅도 영웅 한정(`_snapInstant`) 명시 |
| **B2** CooldownScale/PowerScale 곱셈 계약 | §2.4-a | CooldownScale=`TryBeginAttack`, PowerScale=`TryApplyStrike`(strike시점) 읽기 단정 + 검산 |
| **B3** 스폰 게이트 BattleController 무수정 | §1.2·§1.4·§7.2 | HeroEntryDriver `_marchGateOpen` + AutoCombatAI `_spawnGateOpen` 내부 플래그. zone==null 폴백도 게이트 존중 |
| **B4** strike/spawnEnd 수신구 배치 | §2.3·§B4·§7.3 | relay 는 **Visual(Animator) 자식** 부착 + `GetComponentInParent` 위임. "같은/부모" 양자택일 제거 단정 |
| **M1** AttackJuice 펀치/플래시 strike 이동 | §2.4-b | OnHit 이동에 따라 펀치/플래시도 strike 시점 — 의도됨(타격감 개선) 명시 |
| **M2** IsAttacking 게이트 소유자·참조 경로 | §3.2 | `IAttackGate` 루트 부착(영웅만), AutoCombatAI `GetComponent` 로 잡아 보류·정지. null=몬스터 보존 |
| **M3** `_attackSuppressWindow` 겸용 금지 | §3.4·§7.6 | 0.5 불변(피격 억제) + 신규 `_attackEndFallback` 1.8(공격 end 안전망) 분리. 선행 PlayMode 테스트 가정 보존 |
| **m1** strike nt↔프레임 fps 단서 | §2.6 | "실제 임포트 fps 확인 후" 단서 + 단일 진실=`AnimationEvent.time` |
| **m2** 회전 스냅 영웅 한정 | §4.2·§B.3·§0 | `_snapInstant` 플래그(영웅만 true), 몬스터 보간 보존 |

- **Placeholder 잔존**: 0건. fallback 1.8s(공격end·스폰게이트) / 0.5s(피격억제) 검산 동반. strike 프레임은 "추정 후보 + 육안 확정 + fps 단서"로 명시. 모든 대안 비교에 권장안 단일 확정.
- **스펙 커버리지**: spec 없음 — 사용자 직접 요구 3건 + design-reviewer 지적 9건 전부 위 표로 매핑. 갭 0건.
- **내부 일관성**: 클립 길이(34f/34f/49f/40f), 쿨다운 1.0s, 공격주기 max(1.0,클립), `_attackEndFallback`/`_spawnGateFallback` 1.8s, `_attackSuppressWindow` 0.5s, strike nt(0.40/0.40/0.55) 가 §1·§2·§3·§6·§7 동일. **영웅 한정 분기(DeferStrike/IAttackGate/`_snapInstant`/게이트 플래그)** 가 §B·§1·§2·§3·§4·§7 에서 일관 — 몬스터 보존이 모든 절에서 동일 단정.
- **시그니처/명명 일관성**: `DeferStrike`·`TryBeginAttack`·`TryApplyStrike`·`TryAttack`(유지)·`IAttackGate`·`IsAttacking`·`BeginAttack`/`EndAttack`·`_marchGateOpen`·`_spawnGateOpen`·`_attackEndFallback`·`_spawnGateFallback`·`_snapInstant`·`CharacterAttackStrikeRelay`·`OnSpawnAnimEnd`·`OnAttackStrike`·`OnAttackEnd`·`MeleeAttacker`·`AutoCombatAI`·`HeroEntryDriver`·`CharacterAnimationDriver`·`SimpleRotator`·`FaceDirection`·`SnapToDirection`·`ApplyYaw`·`GetComponentInParent`·`CooldownScale`·`PowerScale`·`OnHit`·`AttackJuice`·`PlagueSlowOnHit`·`EHero.Knight` 모두 본문 전체 + 실제 코드(`MeleeAttacker.cs` L59/L65 곱셈, `AutoCombatAI.cs` L106~112 교전, `BattleController.cs` L295~349 스폰, `AttackJuice.cs` L41/L109, `SimpleRotator.cs`, `HeroEntryDriver.cs`)와 글자 일치. 변형 표기 0건.
- **모호 표현**: 0건. 흐름 분기·게이트·곱셈 시점·relay 배치·회전 모두 단일 확정. relay 배치·게이트 위치는 "같은/부모", "또는" 양자택일 표현 제거 후 단정. 인터페이스 분리만 "권고 + Rule 02 §9" 로 구조 재량 명시.
- **스코프**: 단일 구현 단위(영웅 1종 타이밍 동기화). 공통 컴포넌트 보존 분기로 표면적 최소화. 밸런스 튜닝만 §6 후속 분리.
- **구현 요청사항 완전성**: 애니 이벤트 베이크 7건·흐름 역전 대상 메서드(DeferStrike 분기 포함)·신규 컴포넌트/인터페이스/플래그·IAttacker 계약(+PlayMode 테스트 영향)·회전(`_snapInstant`)·SerializeField 변경분(겸용 금지 반영) 모두 명세.

→ **Self-Review: 9항목(BLOCKER 4 + MAJOR 3 + MINOR 2) 보강 후 통과** — §B 신규(영웅 한정 분기 단일 진실) + §2.4-a(곱셈 시점) + §1.2/1.4(내부 게이트) + §2.3/B4(relay Visual 자식 단정) + §2.4-b(AttackJuice) + §3.2(게이트 소유자) + §3.4(필드 겸용 분리) + §2.6(fps 단서) + §4.2(회전 영웅 한정).

### rev3 — BLOCKER 0 통과 후 비차단 권장 2건 마감

| 권장 | 닫은 위치 | 핵심 |
|---|---|---|
| **권장1**(중요) 영웅 공격 애니 트리거 주체 단정 + strike 순환 방지 | §2.7(신규) · §3.2 · §7.2 · §7.3 | 영웅 애니 트리거 = `AutoCombatAI` 개시 판정(`TryBeginAttack` 성공) → `IAttackGate.BeginAttack()` 개시 신호 → Driver 가 `TriggerAttack` 출력. `OnHit`(strike) 경로는 영웅 애니 트리거에서 **우회**(연출 전용) → strike→windup→strike 순환 차단. Rule 02 §6: 게임플레이 판정은 AutoCombatAI/MeleeAttacker, Driver 는 신호 받아 출력만(View 경계 유지) |
| **권장2**(경미) `CooldownScale` 표기 정정 | §7.4 · §2.4 | `CooldownScale`·`DamageColor` 은 `IAttacker` 인터페이스 멤버가 아니라 **`MeleeAttacker` 구체 프로퍼티**(코드 확인: `CommonInterface.cs` IAttacker 에 `PowerScale` 만, `MeleeAttacker.cs` L27 에 `CooldownScale`). 인터페이스 계약 항목에서 분리 표기 |

- **권장1 일관성 점검**: 신규 식별자 `OnAttackBegin`(개시 신호, 구현 형태는 gameplay-programmer 재량으로 명시)·`IAttackGate.BeginAttack`·`TriggerAttack`·`HandleAttackHit`·`OnHit` 표기가 §2.7·§3.2·§7.2·§7.3 에서 글자 일치. 영웅=개시 트리거 / 몬스터=OnHit→OnAttack 보존 분기가 일관.
- **권장2 일관성 점검**: `CooldownScale`(MeleeAttacker 구체) vs `PowerScale`(IAttacker) 구분이 §2.4·§2.4-a·§7.4 에서 모순 없음. §2.4-a 표는 이미 `MeleeAttacker.TryAttack` L59/L65 로 구체 명시되어 있어 정합.

→ **rev3: 권장 2건 마감 후 통과** — BLOCKER 0 유지, 다른 섹션 구조 변경 없음(§2.7 신규 + §3.2/§7.2/§7.3/§7.4/§2.4 표기 보강만).
