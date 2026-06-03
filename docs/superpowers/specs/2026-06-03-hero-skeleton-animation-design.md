# 영웅 스켈레톤 + 애니메이션 교체 — 설계 (spec)

- 작성일: 2026-06-03
- 대상: 영웅(`EHero.Knight`) 비주얼을 파랑 캡슐 → SazenGames 스켈레톤으로 교체하고, 전투 상태에 반응하는 애니메이션을 붙인다.
- 단계 배경: 몬스터는 이미 LittleGhost 아트로 비주얼 교체가 시작됨(예: `Wisp.prefab`이 LittleGhost 프리팹을 자식으로 물고 `Ghost_Kid.controller`의 단일 idle 루프 재생). 본 작업은 그 아트 패스에 **영웅을 맞추는 연속선**이며, 추가로 *전투 상태 반응 애니메이션*이라는 새 인프라를 도입한다. MVP §8(프리미티브 고정) 제약은 아트 패스 진행에 따라 영웅 1종 한해 푸는 것으로 사용자 합의됨 → game-designer가 기획서에서 승격 근거·범위 한계를 명시한다.

## 1. 목표 / 검증

- **목표**: 영웅이 자동전투 상태(입장·대기·이동·도주·공격·피격·사망)에 맞춰 스켈레톤 애니메이션을 재생한다.
- **비목표(YAGNI)**: 몬스터 6종 애니메이션화(이번 범위 아님), 루트모션, 무기(Falchion) 장착 연출, 사운드 훅, 미사용 클립(jump/fall/scream/revive/underground/turn/throw) 활용.

## 2. 에셋 현황

- 스켈레톤 위치: `Assets/SazenGames/Skeleton/`
- 모델: `Art/Meshes/Skeleton_Model_110.fbx` — **Humanoid 리그**(animationType 3). 프리팹 `Prefabs/Skeleton_110.prefab`.
- 애니메이션 19종 전부 Humanoid. 본 작업에서 쓰는 클립:
  - `Skeleton_idle` (combat idle)
  - `Skeleton_walk_forward`
  - `Skeleton_run_forward`
  - `Skeleton_slash01` / `Skeleton_slash02` / `Skeleton_stab`
  - `Skeleton_take_damage`
  - `Skeleton_death`
  - `Skeleton_spawn`
- 현재 영웅: `Assets/_Lair/Art/Characters/Knight.prefab` — 빌트인 Capsule Mesh + MeshRenderer + CapsuleCollider + Rigidbody + 게임 컴포넌트(Health/AutoCombatAI/SimpleMover/MeleeAttacker/SimpleRotator/HeroEntryDriver 등). **Animator 없음.** `BattleController`가 `CHMResource.LoadAsync<GameObject>(EHero.Knight)`로 로드.

## 3. 구조 — 게임플레이 무손상, 비주얼만 교체

`Knight.prefab` 루트의 게임 컴포넌트·콜라이더·리지드바디는 전부 유지한다. 루트의 **Capsule MeshFilter/MeshRenderer만 제거**하고, 자식으로 **스켈레톤 비주얼 GameObject**(SkinnedMeshRenderer + Animator)를 배치한다.

```
Knight (root) — Health/AutoCombatAI/SimpleMover/MeleeAttacker/SimpleRotator/HeroEntryDriver
              + CapsuleCollider + Rigidbody + CharacterAnimationDriver(신규)
  └─ Visual (스켈레톤 모델 인스턴스) — SkinnedMeshRenderer + Animator(Knight.controller)
```

- 콜라이더/이동은 루트 기준 그대로 → 전투·충돌·사거리 로직 무손상.
- 스켈레톤 모델 스케일/오프셋은 기존 캡슐의 시각적 크기·접지에 맞춰 자식 Transform에서 보정.
- Rule 04 §2: 사용하는 스켈레톤 에셋은 `Assets/_Lair/Art/` 하위로 이관(.meta 동행, GUID 보존)하는 것을 원칙으로 한다. 이관 범위·방식은 plan에서 결정.

## 4. AnimatorController — `Knight.controller` (신규)

상태와 파라미터:

| 상태 | 클립 | 진입 조건 |
|---|---|---|
| Spawn | `Skeleton_spawn` | `Spawn` 트리거 (입장 시 1회), 종료 후 Idle로 |
| Idle | `Skeleton_idle` | 기본 상태, `Speed` ≈ 0 |
| Move | `Skeleton_walk_forward` / `Skeleton_run_forward`(BlendTree 또는 Speed 임계) | `Speed` > 0 |
| Attack | `slash01`/`slash02`/`stab` 중 랜덤 | `Attack` 트리거. 종료 후 직전 상태 복귀 |
| Hit | `Skeleton_take_damage` | `Hit` 트리거 |
| Death | `Skeleton_death` | `Dead` = true. 종료 후 마지막 프레임 유지 |

- 파라미터: `Speed`(float), `Attack`(trigger), `Hit`(trigger), `Dead`(bool), `Spawn`(trigger).
- 이동: 평상시 `walk_forward`, 공포 도주(FleeMode)는 `run_forward`. `Speed` 값 구간으로 BlendTree 처리하거나, `Speed`에 도주 시 가속값을 실어 run 임계를 넘기는 방식 중 plan에서 택1.
- Death는 모든 상태에서 인터럽트 가능(`Dead` bool, AnyState 전이).

## 5. 구동 컴포넌트 — `CharacterAnimationDriver` (신규)

- **계층**: View (Rule 02 §6). 도메인 상태(Health/AutoCombatAI/Mover/Attacker)를 **관찰만** 하고 Animator에 반영한다. 비즈니스 로직·상태 변경 없음.
- **의존**: 인터페이스(IHealth/IMover/IAttacker) 우선 의존으로 작성해 **몬스터 재사용** 가능하게 설계한다. 단 이번 단계에선 영웅에만 연결한다(YAGNI — 몬스터 와이어링은 후속).
  - `MeleeAttacker.OnHit`은 현재 구체 클래스 이벤트다. plan에서 ① `IAttacker`에 공격 이벤트 노출 추가 vs ② 드라이버가 구체 `MeleeAttacker` 참조 중 택1(전자 권장 — Rule 02 §5 종속성 최소화).
- **입력 → Animator 매핑**:

| 게임 신호 | 소스 | Animator 반영 |
|---|---|---|
| 입장 | `HeroEntryDriver` 활성/`OnEnable` | `Spawn` 트리거 1회 |
| 이동/대기 | `IMover` 속도 또는 AutoCombatAI Moving 여부 | `Speed` float 세팅 |
| 공포 도주 | `AutoCombatAI.FleeMode` | `Speed`를 run 임계 이상으로 |
| 공격 적중 | `MeleeAttacker.OnHit` | `Attack` 트리거 |
| 피격 | `Health.OnChanged` 감소 감지 | `Hit` 트리거 (가드 적용 — §6) |
| 사망 | `Health.OnDied` | `Dead` = true |

- **풀 재사용**: `OnEnable`/`OnDisable`에서 트리거·`Dead`·`Speed` 리셋(Rule 03 §4 상태 리셋). Animator도 기본 상태로 Rebind/리셋.

## 6. 피격 리액션 스팸 가드 (핵심 리스크 대응)

영웅은 무리에 둘러싸여 초당 다회 피격된다. 매 피격마다 full-body `take_damage`를 재생하면 공격·이동이 끊겨 동작이 깨져 보인다.

- **규칙**: `Hit` 트리거는 ① 현재 Attack 상태가 아닐 때 + ② 최소 간격 쿨다운(기본 0.4s, 밸런스 조정 가능) 경과 시에만 발행한다.
- 그 외 피격 피드백은 기존 머티리얼 플래시(AttackJuice / HitFeedback) 경로를 유지해 "맞고 있음"을 표현한다.
- 쿨다운 값은 기획서에서 수치로 명시하고, 필요 시 BalanceConfig 노출 여부를 game-designer가 판단.

## 7. 영향 범위 / 변경 대상

- `Assets/_Lair/Art/Characters/Knight.prefab` — 비주얼 자식 교체 + Animator + Driver 부착.
- 신규: `Knight.controller`(AnimatorController), `CharacterAnimationDriver.cs`(+ 필요 시 `IAttacker` 이벤트 확장).
- 스켈레톤 에셋 일부를 `Assets/_Lair/Art/` 하위로 이관(plan에서 범위 확정).
- `BattleController`의 로딩 경로(`EHero.Knight`)는 **변경 없음** — 프리팹 내부만 바뀐다.
- 코드/씬 외 게임플레이 수치(Health/사거리/쿨다운 등) 변경 없음 → 밸런스 영향 없음(애니 표현만).

## 8. 테스트 관점

- EditMode: `CharacterAnimationDriver`의 상태→파라미터 매핑 로직(이동 임계, 피격 가드 쿨다운/공격 중 억제, 사망 우선)을 인터페이스 모킹으로 검증.
- PlayMode: 프리팹 로드 후 Animator 파라미터가 전투 상태 변화에 따라 기대대로 토글되는지(입장→대기→이동→공격→피격→사망) 스모크.

## 9. 미해결/플랜 결정 사항

- 이동 walk↔run 전환을 BlendTree로 할지 Speed 임계로 할지.
- `IAttacker` 공격 이벤트 노출 추가 여부(권장) vs 구체 참조.
- 스켈레톤 에셋 이관 범위(전체 폴더 vs 사용 클립·메시만).
- Humanoid Avatar 매핑 방식(모델 FBX의 기존 Avatar 재사용).
- 피격 가드 쿨다운 기본값 확정(0.4s 제안).
