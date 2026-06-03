# 영웅 스켈레톤 + 애니메이션 교체 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Rule 01 준수:** 이 프로젝트는 자동 커밋 금지. 각 Task 의 "Stage" 단계는 `git add` 까지만 수행하고 `git commit` 은 실행하지 않는다. 최종 커밋 메시지(안)는 파이프라인 마무리에서 사용자에게 제시한다.

**Goal:** 영웅(`EHero.Knight`) 비주얼을 파랑 캡슐 → SazenGames 스켈레톤으로 교체하고, 전투 상태(입장·대기·이동·도주·공격·피격·사망)에 반응하는 애니메이션을 재생한다.

**Architecture:** `Knight.prefab` 루트의 게임 컴포넌트는 무손상 유지, 캡슐 메시만 스켈레톤 비주얼 자식(SkinnedMesh+Animator)으로 교체. 애니메이션 구동은 순수 로직 클래스 `CharacterAnimationController`(테스트 가능) + 이를 Unity 이벤트에 와이어링하는 `CharacterAnimationDriver`(MonoBehaviour, View) + `Animator` 래퍼 `AnimatorSink` 로 3분할. 도메인 상태(IHealth/IMover/IAttacker)를 관찰만 하고 Animator 파라미터에 반영(Rule 02 §6).

**Tech Stack:** Unity 6 / Humanoid Avatar / AnimatorController / NUnit(EditMode·PlayMode) / ChvjPackage.

---

## File Structure

| 파일 | 책임 | 신규/수정 |
|---|---|---|
| `Assets/_Lair/Art/Characters/Skeleton/` (이관 대상) | 스켈레톤 메시·클립·머티리얼·텍스처·아바타 | 이동(신규 위치) |
| `Assets/_Lair/Art/Animations/Knight.controller` | 영웅 AnimatorController(상태머신) | 신규 |
| `Assets/_Lair/Scripts/Character/CommonInterface.cs` | `IAnimatorSink` 추가(애니 채널 추상) | 수정 |
| `Assets/_Lair/Scripts/Character/AnimatorSink.cs` | `IAnimatorSink` 구현 — UnityEngine.Animator 래퍼 | 신규 |
| `Assets/_Lair/Scripts/Character/CharacterAnimationController.cs` | 순수 결정 로직(피격 가드·사망 우선·속도 매핑) | 신규 |
| `Assets/_Lair/Scripts/Character/CharacterAnimationDriver.cs` | MonoBehaviour — 이벤트 구독 + Tick + 풀 리셋 | 신규 |
| `Assets/_Lair/Art/Characters/Knight.prefab` | 비주얼 자식 교체 + Animator + Driver 부착 | 수정 |
| `Assets/_Lair/Tests/EditMode/CharacterAnimationControllerTests.cs` | 결정 로직 단위 테스트 | 신규 |
| `Assets/_Lair/Tests/PlayMode/HeroAnimationSmokeTests.cs` | 프리팹 로드·파라미터 토글 스모크 | 신규 |

**Animator 파라미터 계약** (모든 Task 공유): `Speed`(float), `Attack`(trigger), `Hit`(trigger), `Dead`(bool), `Spawn`(trigger).

**`IAnimatorSink` 계약** (Task 1 에서 정의, 이후 전부 이 시그니처 사용):
```csharp
void SetSpeed(float speed);
void TriggerAttack();
void TriggerHit();
void SetDead(bool dead);
void TriggerSpawn();
```

> 참고: `IAttacker` 는 이미 `event Action<IHealth> OnHit` 를 노출하므로 인터페이스 확장 불필요. FleeMode 는 `AutoCombatAI`(동일 도메인·전투 유닛에 항상 존재)에서 읽는다.

---

## Task 1: `IAnimatorSink` 인터페이스 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs` (파일 끝, namespace 내부)

- [ ] **Step 1: `IAnimatorSink` 정의 추가**

`CommonInterface.cs` 의 `namespace Lair.Character { ... }` 닫는 중괄호 직전에 추가:
```csharp
    //# ===== 애니메이션 채널 =====

    //# 애니메이션 구동의 Unity 의존을 격리하는 sink. 실제 구현은 Animator 래퍼,
    //# 테스트는 Fake 로 대체 → CharacterAnimationController 를 EditMode 에서 검증 가능.
    public interface IAnimatorSink
    {
        void SetSpeed(float speed);
        void TriggerAttack();
        void TriggerHit();
        void SetDead(bool dead);
        void TriggerSpawn();
    }
```

- [ ] **Step 2: 컴파일 확인**

Unity 에디터에서 `editor_recompile` 후 `editor_read_log` — 컴파일 에러 0 건.
Expected: 에러 없음.

- [ ] **Step 3: Stage** (Rule 01 — commit 금지)
```bash
git add Assets/_Lair/Scripts/Character/CommonInterface.cs
```

---

## Task 2: `CharacterAnimationController` 순수 로직 (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/CharacterAnimationController.cs`
- Test: `Assets/_Lair/Tests/EditMode/CharacterAnimationControllerTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

`CharacterAnimationControllerTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    public class CharacterAnimationControllerTests
    {
        private class FakeSink : IAnimatorSink
        {
            public float Speed;
            public int AttackCount;
            public int HitCount;
            public bool Dead;
            public int SpawnCount;
            public void SetSpeed(float speed) => Speed = speed;
            public void TriggerAttack() => AttackCount++;
            public void TriggerHit() => HitCount++;
            public void SetDead(bool dead) => Dead = dead;
            public void TriggerSpawn() => SpawnCount++;
        }

        //# walkSpeed=1, runSpeed=2, hitCooldown=0.4, attackSuppress=0.5
        private CharacterAnimationController Make(FakeSink sink)
            => new CharacterAnimationController(sink, hitReactionCooldown: 0.4f, attackSuppressWindow: 0.5f);

        [Test]
        public void Tick_NotMoving_SetsSpeedZero()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.Tick(isMoving: false, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(0f, sink.Speed);
        }

        [Test]
        public void Tick_Moving_SetsWalkSpeed()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.Tick(isMoving: true, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(1f, sink.Speed);
        }

        [Test]
        public void Tick_Fleeing_SetsRunSpeed()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.Tick(isMoving: true, isFleeing: true, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(2f, sink.Speed);
        }

        [Test]
        public void OnAttack_TriggersAttackOnce()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnAttack(now: 0f);
            Assert.AreEqual(1, sink.AttackCount);
        }

        [Test]
        public void OnDamaged_FirstHit_TriggersHit()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            Assert.AreEqual(1, sink.HitCount);
        }

        [Test]
        public void OnDamaged_WithinCooldown_Suppressed()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            c.OnDamaged(now: 0.2f);   //# < 0.4 쿨다운
            Assert.AreEqual(1, sink.HitCount);
        }

        [Test]
        public void OnDamaged_AfterCooldown_TriggersAgain()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            c.OnDamaged(now: 0.5f);   //# > 0.4 쿨다운
            Assert.AreEqual(2, sink.HitCount);
        }

        [Test]
        public void OnDamaged_DuringAttackWindow_Suppressed()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnAttack(now: 1f);
            c.OnDamaged(now: 1.3f);   //# < 0.5 공격 억제창
            Assert.AreEqual(0, sink.HitCount);
        }

        [Test]
        public void OnDied_SetsDead_AndTickStopsUpdatingSpeed()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            Assert.IsTrue(sink.Dead);
            sink.Speed = 99f;
            c.Tick(isMoving: true, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(99f, sink.Speed);   //# 사망 후 속도 갱신 안 함
        }

        [Test]
        public void OnAttack_WhenDead_Ignored()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            c.OnAttack(now: 1f);
            Assert.AreEqual(0, sink.AttackCount);
        }

        [Test]
        public void Reset_ClearsDeadState()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            c.Reset();
            Assert.IsFalse(sink.Dead);
            c.Tick(isMoving: true, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(1f, sink.Speed);   //# 리셋 후 다시 동작
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

Unity Test Runner(EditMode) 실행 — `CharacterAnimationControllerTests`.
Expected: 컴파일 실패(`CharacterAnimationController` 미정의).

- [ ] **Step 3: 최소 구현 작성**

`CharacterAnimationController.cs`:
```csharp
namespace Lair.Character
{
    //# 애니메이션 결정 로직 — Unity 비의존(순수 C#) → EditMode 테스트 대상.
    //# 도메인 상태를 받아 IAnimatorSink 로만 출력. 비즈니스 로직 없음(표현 결정만).
    public class CharacterAnimationController
    {
        private readonly IAnimatorSink _sink;
        private readonly float _hitReactionCooldown;
        private readonly float _attackSuppressWindow;

        private bool _dead;
        private float _lastHitTime;
        private float _lastAttackTime;

        public CharacterAnimationController(
            IAnimatorSink sink, float hitReactionCooldown, float attackSuppressWindow)
        {
            _sink = sink;
            _hitReactionCooldown = hitReactionCooldown;
            _attackSuppressWindow = attackSuppressWindow;
            ResetState();
        }

        //# 풀 재사용/재진입 시 호출 — 상태 초기화 + Dead=false 반영.
        public void Reset()
        {
            ResetState();
            _sink.SetDead(false);
        }

        private void ResetState()
        {
            _dead = false;
            _lastHitTime = float.NegativeInfinity;
            _lastAttackTime = float.NegativeInfinity;
        }

        public void OnSpawn() => _sink.TriggerSpawn();

        //# IAttacker.OnHit 구독 — 공격 적중 순간 스윙 재생.
        public void OnAttack(float now)
        {
            if (_dead)
                return;
            _lastAttackTime = now;
            _sink.TriggerAttack();
        }

        //# Health.OnChanged 감소 감지 시. 스팸 가드 — 공격 중/쿨다운 내면 억제(spec §6).
        public void OnDamaged(float now)
        {
            if (_dead)
                return;
            if (now - _lastAttackTime < _attackSuppressWindow)
                return;
            if (now - _lastHitTime < _hitReactionCooldown)
                return;
            _lastHitTime = now;
            _sink.TriggerHit();
        }

        public void OnDied()
        {
            _dead = true;
            _sink.SetDead(true);
        }

        //# 매 프레임 — 이동 여부/도주 여부로 Speed 파라미터 결정.
        public void Tick(bool isMoving, bool isFleeing, float walkSpeed, float runSpeed)
        {
            if (_dead)
                return;
            float speed = 0f;
            if (isMoving)
                speed = isFleeing ? runSpeed : walkSpeed;
            _sink.SetSpeed(speed);
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Unity Test Runner(EditMode) — `CharacterAnimationControllerTests`.
Expected: 11개 전부 PASS.

- [ ] **Step 5: Stage**
```bash
git add Assets/_Lair/Scripts/Character/CharacterAnimationController.cs Assets/_Lair/Scripts/Character/CharacterAnimationController.cs.meta Assets/_Lair/Tests/EditMode/CharacterAnimationControllerTests.cs Assets/_Lair/Tests/EditMode/CharacterAnimationControllerTests.cs.meta
```

---

## Task 3: `AnimatorSink` — Animator 래퍼

**Files:**
- Create: `Assets/_Lair/Scripts/Character/AnimatorSink.cs`

- [ ] **Step 1: 구현 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# IAnimatorSink 의 런타임 구현 — UnityEngine.Animator 파라미터로 위임.
    //# 파라미터명은 Knight.controller 계약과 일치해야 함(Speed/Attack/Hit/Dead/Spawn).
    public class AnimatorSink : IAnimatorSink
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");
        private static readonly int SpawnId = Animator.StringToHash("Spawn");

        private readonly Animator _animator;

        public AnimatorSink(Animator animator) => _animator = animator;

        public void SetSpeed(float speed) => _animator.SetFloat(SpeedId, speed);
        public void TriggerAttack() => _animator.SetTrigger(AttackId);
        public void TriggerHit() => _animator.SetTrigger(HitId);
        public void SetDead(bool dead) => _animator.SetBool(DeadId, dead);
        public void TriggerSpawn() => _animator.SetTrigger(SpawnId);
    }
}
```

- [ ] **Step 2: 컴파일 확인** — `editor_recompile` 후 에러 0건.

- [ ] **Step 3: Stage**
```bash
git add Assets/_Lair/Scripts/Character/AnimatorSink.cs Assets/_Lair/Scripts/Character/AnimatorSink.cs.meta
```

---

## Task 4: `CharacterAnimationDriver` MonoBehaviour

**Files:**
- Create: `Assets/_Lair/Scripts/Character/CharacterAnimationDriver.cs`

- [ ] **Step 1: 구현 작성**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# View 계층(Rule 02 §6) — 도메인 상태를 관찰만 하고 Animator 에 반영.
    //# 영웅/몬스터 공통 재사용 가능하게 인터페이스 의존. 결정 로직은 Controller 에 위임.
    [RequireComponent(typeof(Health))]
    public class CharacterAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _walkSpeed = 1f;
        [SerializeField] private float _runSpeed = 2f;
        [SerializeField] private float _hitReactionCooldown = 0.4f;
        [SerializeField] private float _attackSuppressWindow = 0.5f;

        private IHealth _health;
        private IMover _mover;
        private IAttacker _attacker;
        private AutoCombatAI _ai;
        private CharacterAnimationController _controller;

        private int _lastKnownHp;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            _health = GetComponent<IHealth>();
            _mover = GetComponent<IMover>();
            _attacker = GetComponent<IAttacker>();
            _ai = GetComponent<AutoCombatAI>();
            _controller = new CharacterAnimationController(
                new AnimatorSink(_animator), _hitReactionCooldown, _attackSuppressWindow);
        }

        //# 풀 재사용 — 상태 리셋 + 입장 연출 + 이벤트 구독.
        private void OnEnable()
        {
            _controller.Reset();
            _lastKnownHp = _health != null ? _health.Current : 0;

            if (_health != null)
            {
                _health.OnChanged += HandleHpChanged;
                _health.OnDied += HandleDied;
            }
            if (_attacker != null)
                _attacker.OnHit += HandleAttackHit;

            _controller.OnSpawn();
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnChanged -= HandleHpChanged;
                _health.OnDied -= HandleDied;
            }
            if (_attacker != null)
                _attacker.OnHit -= HandleAttackHit;
        }

        private void Update()
        {
            bool fleeing = _ai != null && _ai.FleeMode;
            bool moving = _mover != null && _mover.IsMoving;
            _controller.Tick(moving, fleeing, _walkSpeed, _runSpeed);
        }

        private void HandleHpChanged(int current, int max)
        {
            if (current < _lastKnownHp)
                _controller.OnDamaged(Time.time);
            _lastKnownHp = current;
        }

        private void HandleDied() => _controller.OnDied();

        private void HandleAttackHit(IHealth target) => _controller.OnAttack(Time.time);
    }
}
```

- [ ] **Step 2: 컴파일 확인** — `editor_recompile` 후 에러 0건.

- [ ] **Step 3: Stage**
```bash
git add Assets/_Lair/Scripts/Character/CharacterAnimationDriver.cs Assets/_Lair/Scripts/Character/CharacterAnimationDriver.cs.meta
```

---

## Task 5: 스켈레톤 에셋 이관 (Rule 04 §2)

**Files:**
- Move: `Assets/SazenGames/Skeleton/Art/Meshes/Skeleton_Model_110.fbx` → `Assets/_Lair/Art/Characters/Skeleton/`
- Move: 사용 클립 9종(`Skeleton_idle/walk_forward/run_forward/slash01/slash02/stab/take_damage/death/spawn`) → `Assets/_Lair/Art/Characters/Skeleton/Animations/`
- Move: 관련 머티리얼 → `Assets/_Lair/Art/Materials/`, 텍스처 → `Assets/_Lair/Art/Sprites/` (또는 `Textures/` 신설)

- [ ] **Step 1: 이관 폴더 생성 + 에셋 이동(.meta 동행)**

Unity 에디터 Project 창에서 드래그 이동(또는 `editor_execute_menu`/파일 이동 후 `editor_refresh_assets`). `.meta` 동행으로 GUID 보존 — 프리팹/Avatar 참조 무손실.
- 메시 FBX: `Assets/_Lair/Art/Characters/Skeleton/Skeleton_Model_110.fbx`
- 클립 FBX 9종: `Assets/_Lair/Art/Characters/Skeleton/Animations/`
- 머티리얼: `Assets/_Lair/Art/Materials/`, 텍스처: `Assets/_Lair/Art/Sprites/`

> 미사용 클립(jump/fall/scream/revive/underground/turn_L/R/throw + RootMotion 2종)은 이관하지 않는다(YAGNI). `Assets/SazenGames/Skeleton/Demo`·`Scripts`·`Documentation` 도 이관 제외(필요 시 별도 정리).

- [ ] **Step 2: import 설정 확인**

이관된 FBX 들의 Rig 가 Humanoid 인지, 클립 FBX 들이 메시 FBX 의 Avatar 를 `Copy From Other Avatar` 로 참조하는지 확인. 메시 FBX 의 Avatar 를 단일 소스로 사용.
Expected: 9개 클립 전부 동일 Avatar 참조, Loop 설정 — idle/walk/run = Loop Time ON, slash/stab/take_damage/death/spawn = OFF.

- [ ] **Step 3: 콘솔 에러 확인** — `editor_read_log` 에 누락 참조/Avatar 에러 0건.

- [ ] **Step 4: Stage**
```bash
git add Assets/_Lair/Art/Characters/Skeleton Assets/_Lair/Art/Materials Assets/_Lair/Art/Sprites Assets/SazenGames
```
> 이동은 삭제(원위치)+추가(새위치)로 잡히므로 양쪽 경로 모두 add.

---

## Task 6: `Knight.controller` AnimatorController 생성

**Files:**
- Create: `Assets/_Lair/Art/Animations/Knight.controller`

- [ ] **Step 1: 컨트롤러 생성 + 파라미터 추가**

`Assets/_Lair/Art/Animations/` 에서 Create → Animator Controller → `Knight`. 파라미터 추가:
- `Speed` (Float), `Attack` (Trigger), `Hit` (Trigger), `Dead` (Bool), `Spawn` (Trigger)

- [ ] **Step 2: 상태 배치 + 클립 할당**

| 상태 | 클립 | 비고 |
|---|---|---|
| Spawn | `Skeleton_spawn` | Entry 의 기본 상태로 지정 |
| Idle | `Skeleton_idle` | Loop |
| Move | BlendTree(`Speed`): 0→Idle 보간 생략, `walk_forward`(Speed≈1) → `run_forward`(Speed≈2) | Loop |
| Attack | `Skeleton_slash01` | Task 7 에서 멀티클립 랜덤화 |
| Hit | `Skeleton_take_damage` | |
| Death | `Skeleton_death` | Write Defaults 종료 후 마지막 프레임 유지 |

- [ ] **Step 3: 전이(Transition) 설정**

- Spawn → Idle: Exit Time(클립 종료) 자동.
- Idle ↔ Move: `Speed` > 0.1 → Move, `Speed` < 0.1 → Idle (Has Exit Time OFF).
- AnyState → Attack: `Attack`(trigger). Attack → Idle/Move: Exit Time.
- AnyState → Hit: `Hit`(trigger). Hit → Idle/Move: Exit Time.
- AnyState → Death: `Dead` == true (trigger 아님, bool). Death 는 self-loop 없음.

- [ ] **Step 4: 콘솔 에러 확인** — `editor_read_log` 0건.

- [ ] **Step 5: Stage**
```bash
git add Assets/_Lair/Art/Animations/Knight.controller Assets/_Lair/Art/Animations/Knight.controller.meta
```

---

## Task 7: 공격 클립 랜덤화 (slash01/slash02/stab)

**Files:**
- Modify: `Assets/_Lair/Art/Animations/Knight.controller` (Attack 서브 상태)

- [ ] **Step 1: Attack 을 BlendTree(Direct) 또는 서브 스테이트머신으로 변경**

Attack 진입 시 `AttackVariant`(Int, 0~2) 파라미터로 분기하는 서브 스테이트머신 구성: 0=`slash01`, 1=`slash02`, 2=`stab`. 또는 Attack 진입 시 3클립 중 랜덤 선택되는 BlendTree.

- [ ] **Step 2: 파라미터 추가 + Sink/Controller 연동**

`Knight.controller` 에 `AttackVariant`(Int) 추가. `IAnimatorSink.TriggerAttack()` 호출 직전 변형 선택이 필요하므로 시그니처를 `TriggerAttack(int variant)` 로 확장:
- `CommonInterface.cs` `IAnimatorSink.TriggerAttack` → `void TriggerAttack(int variant);`
- `AnimatorSink`: `public void TriggerAttack(int variant) { _animator.SetInteger(VariantId, variant); _animator.SetTrigger(AttackId); }` (`VariantId = StringToHash("AttackVariant")`)
- `CharacterAnimationController.OnAttack`: `_sink.TriggerAttack(_rng.Next(0, 3));` — 생성자에 `System.Random _rng = new System.Random()` 추가, 테스트 결정성 위해 seed 주입 가능한 오버로드 제공.
- `FakeSink.TriggerAttack(int variant)` 로 테스트 갱신 — `LastAttackVariant` 기록 + `AttackCount++`.

- [ ] **Step 3: 테스트 갱신·통과 확인**

`OnAttack_TriggersAttackOnce` 등 기존 테스트의 `TriggerAttack` 시그니처 반영. seed 고정 생성자로 variant 범위(0~2) 검증 테스트 1개 추가.
Run: Unity Test Runner(EditMode). Expected: 전부 PASS.

- [ ] **Step 4: Stage**
```bash
git add Assets/_Lair/Scripts/Character/CommonInterface.cs Assets/_Lair/Scripts/Character/AnimatorSink.cs Assets/_Lair/Scripts/Character/CharacterAnimationController.cs Assets/_Lair/Tests/EditMode/CharacterAnimationControllerTests.cs Assets/_Lair/Art/Animations/Knight.controller
```

---

## Task 8: `Knight.prefab` 비주얼 교체 + 컴포넌트 부착

**Files:**
- Modify: `Assets/_Lair/Art/Characters/Knight.prefab`

- [ ] **Step 1: 프리팹 열기 + 캡슐 비주얼 제거**

`prefab_open` `Knight.prefab`. 루트의 `MeshFilter`(캡슐) + `MeshRenderer` 제거. CapsuleCollider·Rigidbody·게임 컴포넌트는 유지.

- [ ] **Step 2: 스켈레톤 비주얼 자식 추가**

`Skeleton_Model_110` (또는 `Skeleton_110.prefab` 의 모델 부분)을 Knight 루트의 자식으로 배치(`Visual`). SkinnedMeshRenderer 포함. 자식 Transform 으로 스케일/접지 보정 — 기존 캡슐의 시각적 높이·바닥 정렬에 맞춤. 콜라이더/이동은 루트 기준 그대로.

- [ ] **Step 3: Animator 연결**

`Visual` 의 `Animator.controller` = `Knight.controller`, `Avatar` = 스켈레톤 메시 Avatar.

- [ ] **Step 4: `CharacterAnimationDriver` 부착 + 참조 연결**

Knight 루트에 `CharacterAnimationDriver` 추가. `_animator` 필드에 `Visual` 의 Animator 드래그. `_walkSpeed`/`_runSpeed`/`_hitReactionCooldown`(0.4)/`_attackSuppressWindow`(0.5) 인스펙터 확인.

- [ ] **Step 5: 저장 + 콘솔 확인**

`prefab_save` 후 `prefab_close`. `editor_read_log` — 누락 참조/NRE 0건.

- [ ] **Step 6: Stage**
```bash
git add Assets/_Lair/Art/Characters/Knight.prefab
```

---

## Task 9: PlayMode 스모크 테스트

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/HeroAnimationSmokeTests.cs`

- [ ] **Step 1: 스모크 테스트 작성**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    public class HeroAnimationSmokeTests
    {
        //# 프리팹 직접 로드 대신 컴포넌트 합성 — Addressable 의존 없이 드라이버 동작만 검증.
        [UnityTest]
        public IEnumerator Driver_OnSpawn_SetsAnimatorWithoutError()
        {
            GameObject go = new GameObject("HeroTest");
            Animator animator = go.AddComponent<Animator>();
            go.AddComponent<Health>();
            CharacterAnimationDriver driver = go.AddComponent<CharacterAnimationDriver>();

            //# Animator 컨트롤러 미할당 상태에서도 NRE 없이 OnEnable 통과(가드) 확인.
            yield return null;
            Assert.IsNotNull(driver);
            Object.Destroy(go);
        }
    }
}
```

> 주: Animator controller 미할당 시 `SetFloat` 등이 경고만 내고 throw 하지 않음을 확인. 본격 통합 테스트(실제 Knight 프리팹 Addressable 로드 + 전투 상태 토글)는 test-engineer 단계에서 확장.

- [ ] **Step 2: 테스트 실행** — Unity Test Runner(PlayMode). Expected: PASS.

- [ ] **Step 3: Stage**
```bash
git add Assets/_Lair/Tests/PlayMode/HeroAnimationSmokeTests.cs Assets/_Lair/Tests/PlayMode/HeroAnimationSmokeTests.cs.meta
```

---

## Task 10: 인게임 육안 검증

- [ ] **Step 1: Battle 씬 재생**

`editor_open_scene` `Assets/_Lair/Scenes/Battle.unity` → `sim_play`. 영웅 입장(spawn) → 이동(walk) → 몬스터 교전(slash) → 피격(take_damage, 스팸 억제 확인) → 사망(death) 순으로 애니메이션이 전투 상태에 맞게 재생되는지 육안 확인.

- [ ] **Step 2: `screenshot_game` 으로 상태별 캡처** — 입장/이동/공격/사망 4컷.

- [ ] **Step 3: 콘솔 에러 0건 확인** (`editor_read_log`), `sim_stop`.

---

## Self-Review

**Spec 커버리지:**
- §3 구조(루트 유지+비주얼 자식) → Task 8 ✅
- §4 AnimatorController → Task 6·7 ✅
- §5 구동 컴포넌트(인터페이스 의존, OnHit 구독, 풀 리셋) → Task 2·3·4 ✅
- §6 피격 스팸 가드(공격중/쿨다운) → Task 2 테스트 `OnDamaged_DuringAttackWindow_Suppressed`/`OnDamaged_WithinCooldown_Suppressed` ✅
- §7 에셋 이관 → Task 5 ✅
- §8 테스트(EditMode 매핑/PlayMode 스모크) → Task 2·9 ✅
- §9 미해결: IAttacker 이벤트(이미 존재 — 확장 불필요로 확정), walk/run(Task 6 BlendTree로 확정), 이관 범위(사용분만 — Task 5 확정), 피격 쿨다운 0.4(Task 2·4 확정), 공격 랜덤(Task 7 확정) — 전부 plan 에서 해소 ✅

**Placeholder 스캔:** 코드/테스트 전부 실제 내용 기재. 에셋 Task(5·6·8·10)는 Unity 에디터 수작업 특성상 단계 서술형이나 각 단계가 구체 행동·검증 포함.

**타입 일관성:** `IAnimatorSink` 시그니처가 Task 1 정의 → Task 2/3 사용 일치. 단 Task 7 에서 `TriggerAttack()` → `TriggerAttack(int variant)` 로 의도적 확장(해당 Task 가 Sink·Controller·FakeSink·테스트를 함께 갱신하도록 명시) — 전후 정합. Animator 파라미터명(Speed/Attack/Hit/Dead/Spawn/AttackVariant)이 `AnimatorSink` 해시·`Knight.controller`·드라이버에서 동일.

**주의(실행자 참고):** Task 7 이 Task 2/3 의 `TriggerAttack` 시그니처를 바꾸므로, Task 7 까지 한 묶음으로 실행하거나 Task 2 를 처음부터 `TriggerAttack(int)` 로 작성해도 무방. 단순화를 위해 Task 2 는 무인자 버전으로 시작하고 Task 7 에서 확장하는 순서를 권장.
