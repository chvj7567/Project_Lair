# 타격 피드백 (Hit Feedback) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **파이프라인 주의**: 이 plan은 start-develop 파이프라인의 입력이다. **정확한 도메인 수치(색 hex, 스케일 배율·지속, 부상 거리·시간, 폰트 크기, 파티클 개수·수명, 워밍 count)는 game-designer 기획서가 단일 진실(SoT)** 이다. 아래 코드의 숫자/색은 *기본 예시값* 이며, 구현 시 기획서 확정값으로 치환한다. spec: `docs/superpowers/specs/2026-06-01-hit-feedback-design.md`.

**Goal:** 영웅/몬스터가 때릴 때(공격자 스케일 펀치·색 플래시)와 맞을 때(임팩트 파티클·데미지 숫자 팝업) 프리미티브 기반 타격 피드백을 준다.

**Architecture:** 기존 `HitFlash` 와 동일한 엔티티별 컴포넌트 방식. 공격자 측 `AttackJuice`(`MeleeAttacker.OnHit` 구독), 피격자 측 `DamageFeedback`(`Health.OnChanged` 델타 구독). 실제 풀 스폰은 무상태 `HitFeedbackSpawner` 가 대행(워밍 프리팹 캐시 → 동기 Pop). 데미지 숫자 색은 데미지 출처가 `TakeDamage` 직전 피격자에 스탬프(`IDamageColorSink`).

**Tech Stack:** Unity 6 / URP, C#, ChvjPackage(`CHMResource`/`CHMPool`/`CHText`), NUnit (EditMode). 트윈 라이브러리 없음 → 코루틴 lerp.

---

## 파일 구조

| 파일 | 책임 | 신규/수정 |
|---|---|---|
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `EVisual` 에 `HitImpact`·`DamagePopup` 추가 | 수정 |
| `Assets/_Lair/Scripts/Character/CommonInterface.cs` | `IDamageColorSink` 추가 | 수정 |
| `Assets/_Lair/Scripts/Character/MeleeAttacker.cs` | `DamageColor` 프로퍼티 + TakeDamage 직전 색 스탬프 | 수정 |
| `Assets/_Lair/Scripts/Character/AttackJuice.cs` | 공격자 스케일 펀치 + 색 플래시 + 대표색 주입 | 신규 |
| `Assets/_Lair/Scripts/Character/DamageFeedback.cs` | 피격자 델타 감지 → 임팩트·팝업 스폰, 색 스탬프 수신 | 신규 |
| `Assets/_Lair/Scripts/Battle/HitFeedbackSpawner.cs` | 워밍 프리팹 캐시 + 동기 Pop 스폰 대행 | 신규 |
| `Assets/_Lair/Scripts/Character/DamagePopup.cs` | 팝업 셀 — 숫자·색 세팅, 빌보드, 부상+페이드, Push | 신규 |
| `Assets/_Lair/Scripts/Character/ReturnToPoolAfter.cs` | 일정 시간 후 자동 `CHMPool.Push` (HitImpact 용) | 신규 |
| `Assets/_Lair/Scripts/Card/HitFeedbackPalette.cs` | DoT 데미지 숫자색 단일 출처(독·출혈) | 신규 |
| `Assets/_Lair/Scripts/Card/Auras/PoisonAura.cs` · `BleedAura.cs` | TakeDamage 직전 디버프색 스탬프 | 수정 |
| `Assets/_Lair/Editor/LairVisualPrefabBuilder.cs` | `HitImpact`·`DamagePopup` 빌드 + `PoisonAura`/`BleedStatus` 색 변경 | 수정 |
| `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs` | 캐릭터 프리팹에 `AttackJuice`·`DamageFeedback` 부착 | 수정 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | 신규 FX 풀 워밍 + `HitFeedbackSpawner` 초기화 | 수정 |
| `Assets/_Lair/Tests/EditMode/Character/HitFeedbackTests.cs` | 단위 테스트 | 신규 |

> **테스트 메서드 네이밍**: 프로젝트 규약 = 한글 (`project.md` `test_method_naming: korean`). asmdef = `Lair.Tests.EditMode`.

---

## Task 1: EVisual 키 2종 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (EVisual enum)

- [ ] **Step 1: EVisual 에 키 추가**

`EVisual` 의 기존 마지막(`BleedStatus`) 뒤에 추가 (Rule 08 — 값명 = 프리팹 파일명):

```csharp
    public enum EVisual
    {
        PoisonAura,
        SlowStatus,
        FearStatus,
        WeakenStatus,
        AttackDownStatus,
        TimeStopStatus,
        BleedStatus,
        //# 타격 피드백 (2026-06-01)
        HitImpact,    //# 피격 지점 프리미티브 버스트 파티클
        DamagePopup,  //# 부상+페이드 데미지 숫자 (월드스페이스 TMP+CHText)
    }
```

- [ ] **Step 2: 컴파일 확인**

Unity 재컴파일(`editor_recompile`) → 에러 없음 확인.

---

## Task 2: IDamageColorSink 인터페이스

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs`

- [ ] **Step 1: 인터페이스 추가**

```csharp
namespace Lair.Character
{
    //# 데미지 숫자 색 스탬프 수신구. 데미지 출처가 TakeDamage 직전 호출.
    public interface IDamageColorSink
    {
        void StampDamageColor(Color color);
    }
}
```

> `UnityEngine` using 필요 시 파일 상단 확인.

- [ ] **Step 2: 컴파일 확인** — 재컴파일 에러 없음.

---

## Task 3: MeleeAttacker 색 스탬프 (TakeDamage 직전)

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/MeleeAttacker.cs:51-62` (`TryAttack`)
- Test: `Assets/_Lair/Tests/EditMode/Character/HitFeedbackTests.cs`

> **핵심**: `OnHit` 은 `TakeDamage` *이후* 발행되므로 색 스탬프에 쓸 수 없다. 반드시 `TakeDamage` *직전* 스탬프.

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Character;

public class HitFeedbackTests
{
    //# 테스트용 IHealth + IDamageColorSink 더블.
    private class FakeTarget : MonoBehaviour, IHealth, IDamageColorSink
    {
        public int Max => 100;
        public int Current { get; private set; } = 100;
        public int EffectiveMaxHp => 100;
        public float Ratio => Current / 100f;
        public bool IsAlive => Current > 0;
        public Color StampedColor = Color.clear;
        public int StampOrderHp = -1;   //# 스탬프 시점의 Current (스탬프가 TakeDamage 前인지 검증)
        public event System.Action<int,int> OnChanged;
        public event System.Action OnDied;
        public void TakeDamage(int amount) { Current -= amount; OnChanged?.Invoke(Current, Max); if (Current <= 0) OnDied?.Invoke(); }
        public void StampDamageColor(Color c) { StampedColor = c; StampOrderHp = Current; }
    }

    [Test]
    public void 근접공격_적중시_TakeDamage_직전_대표색_스탬프()
    {
        GameObject go = new GameObject("attacker");
        MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
        atk.Configure(range: 5f, cooldown: 0f, power: 10);
        atk.DamageColor = Color.red;

        GameObject tgtGo = new GameObject("target");
        FakeTarget tgt = tgtGo.AddComponent<FakeTarget>();

        bool hit = atk.TryAttack(tgt, Vector3.zero, Vector3.zero, now: 1f);

        Assert.IsTrue(hit);
        Assert.AreEqual(Color.red, tgt.StampedColor);
        //# 스탬프가 데미지 적용 전(Current=100)에 찍혀야 함
        Assert.AreEqual(100, tgt.StampOrderHp);

        Object.DestroyImmediate(go); Object.DestroyImmediate(tgtGo);
    }
}
```

- [ ] **Step 2: 실패 확인** — `DamageColor` 미정의로 컴파일 실패.

- [ ] **Step 3: MeleeAttacker 수정**

`Power`/`CooldownScale` 등 인근에 프로퍼티 추가:

```csharp
        //# 데미지 숫자 대표색. AttackJuice 가 OnEnable 에 몸체색 주입. 기본 백색.
        public Color DamageColor { get; set; } = Color.white;
```

`TryAttack` 의 `target.TakeDamage(...)` 직전에 스탬프 삽입:

```csharp
            if (now - _lastAttackTime < _cooldown * CooldownScale) return false;

            //# 데미지 숫자 색 스탬프 — TakeDamage 가 OnChanged 를 동기 발행하기 전에 찍어야 한다.
            if (target is Component tc && tc != null)
                tc.GetComponent<IDamageColorSink>()?.StampDamageColor(DamageColor);

            target.TakeDamage(Mathf.RoundToInt(_power * PowerScale));
            _lastAttackTime = now;
            OnHit?.Invoke(target);
            return true;
```

- [ ] **Step 4: 통과 확인** — EditMode 테스트 PASS.

- [ ] **Step 5: 커밋** — Rule 01 (자동 커밋 금지) → 파이프라인 마지막에 일괄. 여기선 생략.

---

## Task 4: HitFeedbackPalette (DoT 색 단일 출처)

**Files:**
- Create: `Assets/_Lair/Scripts/Card/HitFeedbackPalette.cs`

> **색 값은 game-designer 기획서 확정값**. 아래는 spec §2.2 의도(어둡게+hue 비틀어 Wisp 녹색/Reaper 빨강과 분리)에 따른 *예시 기본값*.

- [ ] **Step 1: 작성**

```csharp
using UnityEngine;

namespace Lair.Card
{
    //# DoT(독·출혈) 데미지 숫자 색 단일 출처. FX 프리팹(PoisonAura/BleedStatus) 색과 동일 값 유지.
    //# spec §2.2 — Wisp 녹색(#22C55E)/Reaper 빨강(#EF4444)과 명도+hue 로 분리. 값은 game-designer 확정.
    public static class HitFeedbackPalette
    {
        //# 독 — 짙은 에메랄드/틸 (예시값, 기획서 확정 치환)
        public static readonly Color Poison = new Color(0.043f, 0.357f, 0.290f, 1f);  //# #0B5B4A 예시
        //# 출혈 — 짙은 크림슨/마룬 (예시값)
        public static readonly Color Bleed  = new Color(0.451f, 0.039f, 0.149f, 1f);  //# #730A26 예시
    }
}
```

- [ ] **Step 2: 컴파일 확인.**

---

## Task 5: DamageFeedback (피격자 측)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/DamageFeedback.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/HitFeedbackTests.cs`

- [ ] **Step 1: 실패 테스트 추가**

```csharp
    [Test]
    public void 데미지_입으면_델타만큼_팝업_요청_회복은_무시()
    {
        GameObject go = new GameObject("victim");
        Health hp = go.AddComponent<Health>();   //# Awake 에서 Current=Max(100)
        DamageFeedback fb = go.AddComponent<DamageFeedback>();
        FakeSpawner spy = new FakeSpawner();
        fb.SetSpawnerForTest(spy);
        go.SetActive(true);   //# OnEnable 구독

        hp.TakeDamage(30);
        Assert.AreEqual(1, spy.PopupCount);
        Assert.AreEqual(30, spy.LastAmount);

        hp.Heal(10);          //# 회복은 팝업 없음
        Assert.AreEqual(1, spy.PopupCount);

        Object.DestroyImmediate(go);
    }
```

`FakeSpawner` 는 `IHitFeedbackSpawner`(Task 6 정의) 구현 스파이 — `SpawnImpact`/`SpawnPopup` 호출 카운트·인자 기록.

- [ ] **Step 2: 실패 확인** — `DamageFeedback`/`IHitFeedbackSpawner` 미정의.

- [ ] **Step 3: 구현**

```csharp
using UnityEngine;

namespace Lair.Character
{
    //# 피격자 측 — Health.OnChanged 델타<0 감지 → 임팩트+숫자 스폰. 색은 출처가 스탬프.
    //# HitFlash 와 동일한 _lastHp 델타 추적. Health 이벤트 시그니처 무변경.
    [RequireComponent(typeof(Health))]
    public class DamageFeedback : MonoBehaviour, IDamageColorSink
    {
        private Health _health;
        private int _lastHp = -1;
        private Color _nextColor = Color.white;
        private IHitFeedbackSpawner _spawner;   //# 기본 = HitFeedbackSpawner.Instance

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnChanged += HandleChanged;
                _lastHp = _health.Current;
            }
            _nextColor = Color.white;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnChanged -= HandleChanged;
        }

        public void StampDamageColor(Color color) => _nextColor = color;

        //# 테스트 주입구.
        public void SetSpawnerForTest(IHitFeedbackSpawner s) => _spawner = s;

        private void HandleChanged(int current, int max)
        {
            if (_lastHp < 0) { _lastHp = current; return; }
            if (current < _lastHp)
            {
                int amount = _lastHp - current;
                IHitFeedbackSpawner sp = _spawner ?? HitFeedbackSpawner.Instance;
                if (sp != null)
                {
                    Vector3 pos = transform.position;
                    sp.SpawnImpact(pos, _nextColor);
                    sp.SpawnPopup(pos, amount, _nextColor);
                }
            }
            _lastHp = current;
        }
    }
}
```

- [ ] **Step 4: 통과 확인** — EditMode PASS.

---

## Task 6: HitFeedbackSpawner (풀 스폰 대행)

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/HitFeedbackSpawner.cs`

> 무상태 스폰 대행. 워밍된 프리팹 핸들을 캐시해 **동기 Pop**(고빈도 타격에 async 로드 회피). 레지스트리 아님.

- [ ] **Step 1: 인터페이스 + 구현**

```csharp
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    public interface IHitFeedbackSpawner
    {
        void SpawnImpact(Vector3 pos, Color color);
        void SpawnPopup(Vector3 pos, int amount, Color color);
    }

    public class HitFeedbackSpawner : MonoBehaviour, IHitFeedbackSpawner
    {
        public static HitFeedbackSpawner Instance { get; private set; }

        private GameObject _impactPrefab;
        private GameObject _popupPrefab;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        //# BattleController 가 워밍 후 호출 — 로드된 프리팹 핸들 주입.
        public void Init(GameObject impactPrefab, GameObject popupPrefab)
        {
            _impactPrefab = impactPrefab;
            _popupPrefab = popupPrefab;
        }

        public void SpawnImpact(Vector3 pos, Color color)
        {
            if (_impactPrefab == null) return;
            CHPoolable p = CHMPool.Instance.Pop(_impactPrefab, null);
            if (p == null) return;
            p.transform.position = pos;
            //# 색 적용 + 자동 Push 는 프리팹 컴포넌트(ReturnToPoolAfter + Renderer/PS) 가 담당.
            ApplyColor(p.gameObject, color);
        }

        public void SpawnPopup(Vector3 pos, int amount, Color color)
        {
            if (_popupPrefab == null) return;
            CHPoolable p = CHMPool.Instance.Pop(_popupPrefab, null);
            if (p == null) return;
            DamagePopup popup = p.GetComponent<DamagePopup>();
            if (popup == null) { CHMPool.Instance.Push(p); return; }
            popup.Play(pos, amount, color);
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            Renderer rd = go.GetComponentInChildren<Renderer>();
            if (rd == null) return;
            Material mat = rd.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
        }
    }
}
```

> `DamagePopup` 은 `Lair.Character`. `HitFeedbackSpawner` 는 `Lair.Battle` → 같은 asmdef(Lair) 내 참조 OK.

- [ ] **Step 2: 컴파일 확인.**

---

## Task 7: DamagePopup 셀 (부상+페이드)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/DamagePopup.cs`

> 수치(부상 거리·지속·시작 폰트 크기)는 기획서 확정값. 아래 예시값.

- [ ] **Step 1: 구현**

```csharp
using System.Collections;
using ChvjUnityInfra;
using TMPro;
using UnityEngine;

namespace Lair.Character
{
    //# 월드스페이스 데미지 숫자. Rule 11 — TMP 엔 CHText 동반(프리팹에 부착).
    [RequireComponent(typeof(CHPoolable))]
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;     //# 프리팹 인스펙터 참조 (CHText 동반)
        [SerializeField] private float _rise = 1.2f;
        [SerializeField] private float _duration = 0.7f;

        private Coroutine _co;
        private Camera _cam;

        private void OnEnable() => _cam = Camera.main;

        private void OnDisable()
        {
            if (_co != null) { StopCoroutine(_co); _co = null; }
        }

        public void Play(Vector3 worldPos, int amount, Color color)
        {
            transform.position = worldPos;
            if (_text != null)
            {
                _text.text = amount.ToString();
                _text.color = new Color(color.r, color.g, color.b, 1f);
            }
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(PlayCo(worldPos, color));
        }

        private IEnumerator PlayCo(Vector3 start, Color color)
        {
            float t = 0f;
            while (t < _duration)
            {
                t += Time.deltaTime;
                float k = t / _duration;
                transform.position = start + Vector3.up * (_rise * k);
                if (_cam != null)
                    transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
                if (_text != null)
                    _text.color = new Color(color.r, color.g, color.b, 1f - k);   //# 페이드
                yield return null;
            }
            _co = null;
            CHPoolable self = GetComponent<CHPoolable>();
            if (self != null) CHMPool.Instance.Push(self);
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인.**

---

## Task 8: ReturnToPoolAfter (HitImpact 자동 반환)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/ReturnToPoolAfter.cs`

- [ ] **Step 1: 구현**

```csharp
using System.Collections;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Character
{
    //# OnEnable 후 _seconds 뒤 자동 CHMPool.Push. 파티클 버스트 수명용.
    [RequireComponent(typeof(CHPoolable))]
    public class ReturnToPoolAfter : MonoBehaviour
    {
        [SerializeField] private float _seconds = 0.6f;
        private Coroutine _co;

        private void OnEnable() => _co = StartCoroutine(Co());
        private void OnDisable() { if (_co != null) { StopCoroutine(_co); _co = null; } }

        private IEnumerator Co()
        {
            yield return new WaitForSeconds(_seconds);
            CHPoolable self = GetComponent<CHPoolable>();
            if (self != null) CHMPool.Instance.Push(self);
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인.**

---

## Task 9: AttackJuice (공격자 측)

**Files:**
- Create: `Assets/_Lair/Scripts/Character/AttackJuice.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/HitFeedbackTests.cs`

> 스케일 펀치 배율·지속·플래시 색은 기획서 확정값. 색 플래시는 `HitFlash` 의 material-instance 캐시 방식 참고.

- [ ] **Step 1: 실패 테스트 추가**

```csharp
    [Test]
    public void OnHit_시_공격자_몸체색을_MeleeAttacker_DamageColor_로_주입()
    {
        GameObject go = new GameObject("attacker");
        MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
        AttackJuice juice = go.AddComponent<AttackJuice>();
        juice.SetRepresentativeColorForTest(Color.green);
        go.SetActive(true);   //# OnEnable → DamageColor 주입

        Assert.AreEqual(Color.green, atk.DamageColor);
        Object.DestroyImmediate(go);
    }
```

- [ ] **Step 2: 실패 확인.**

- [ ] **Step 3: 구현**

```csharp
using System.Collections;
using UnityEngine;

namespace Lair.Character
{
    //# 공격자 측 — MeleeAttacker.OnHit 구독. 스케일 펀치 + 색 플래시 + 대표색 주입.
    [RequireComponent(typeof(MeleeAttacker))]
    public class AttackJuice : MonoBehaviour
    {
        [SerializeField] private float _punchScale = 1.15f;
        [SerializeField] private float _punchDuration = 0.12f;

        private MeleeAttacker _attacker;
        private Vector3 _baseScale;
        private Color _repColor = Color.white;
        private bool _repColorCached;
        private Coroutine _co;

        private void Awake()
        {
            _attacker = GetComponent<MeleeAttacker>();
            _baseScale = transform.localScale;
            CacheRepColor();
        }

        private void OnEnable()
        {
            if (_attacker != null)
            {
                _attacker.OnHit += HandleHit;
                _attacker.DamageColor = _repColor;
            }
            transform.localScale = _baseScale;   //# 풀 재사용 리셋
        }

        private void OnDisable()
        {
            if (_attacker != null) _attacker.OnHit -= HandleHit;
            if (_co != null) { StopCoroutine(_co); _co = null; }
            transform.localScale = _baseScale;
        }

        public void SetRepresentativeColorForTest(Color c)
        {
            _repColor = c; _repColorCached = true;
            if (_attacker != null) _attacker.DamageColor = c;
        }

        //# 몸체 대표색 — HitFlash 와 동일 제외 규칙(Aura/HpBar) 적용한 첫 Renderer 의 _BaseColor.
        private void CacheRepColor()
        {
            if (_repColorCached) return;
            Renderer[] rds = GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (Renderer rd in rds)
            {
                if (rd == null) continue;
                string n = rd.gameObject.name;
                if (n.StartsWith("Aura") || n.StartsWith("HpBar")) continue;
                Material m = rd.sharedMaterial;
                if (m == null) continue;
                _repColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                _repColorCached = true;
                return;
            }
        }

        private void HandleHit(IHealth target)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(PunchCo());
        }

        private IEnumerator PunchCo()
        {
            float t = 0f;
            while (t < _punchDuration)
            {
                t += Time.deltaTime;
                float k = t / _punchDuration;
                //# 0→1 동안 baseScale → punch → baseScale (sin 반원).
                float s = 1f + (_punchScale - 1f) * Mathf.Sin(k * Mathf.PI);
                transform.localScale = _baseScale * s;
                yield return null;
            }
            transform.localScale = _baseScale;
            _co = null;
        }
    }
}
```

> **색 플래시**: 스케일 펀치와 함께 공격자 색을 순간 변경하려면 `HitFlash` 의 `_matInstances`/`Restore` 방식을 재사용하는 별도 메서드를 추가하거나, `HitFlash` 에 `public void FlashOnce()` 를 노출해 `AttackJuice` 가 호출. **공통화 방식은 구현자가 결정** (단 `HitFlash` 의 피격 플래시 동작은 유지). 기획서에 공격자 플래시 색·세기 확정값 반영.

- [ ] **Step 4: 통과 확인** — EditMode PASS.

---

## Task 10: DoT 아우라 색 스탬프

**Files:**
- Modify: `Assets/_Lair/Scripts/Card/Auras/PoisonAura.cs` (TakeDamage 직전)
- Modify: `Assets/_Lair/Scripts/Card/Auras/BleedAura.cs:35` (TakeDamage 직전)

- [ ] **Step 1: BleedAura 스탬프 삽입**

`Tick` 의 `hero.TakeDamage(...)` 직전:

```csharp
                _acc -= 1f;
                StampColor(hero, Lair.Card.HitFeedbackPalette.Bleed);
                hero.TakeDamage(Mathf.RoundToInt(hero.Max * _ratio));
```

파일 하단에 헬퍼 추가(또는 공통 확장 메서드):

```csharp
        private static void StampColor(IHealth hero, Color c)
        {
            if (hero is Component comp && comp != null)
                comp.GetComponent<IDamageColorSink>()?.StampDamageColor(c);
        }
```

- [ ] **Step 2: PoisonAura 동일 패턴** — `TakeDamage` 직전 `HitFeedbackPalette.Poison` 스탬프.

- [ ] **Step 3: 회귀 테스트** — 기존 Bleed/Poison 데미지 테스트가 여전히 통과(스탬프가 데미지 수치에 영향 없음).

---

## Task 11: 프리팹 빌더 — FX 2종 생성 + 색 변경

**Files:**
- Modify: `Assets/_Lair/Editor/LairVisualPrefabBuilder.cs`

- [ ] **Step 1: PoisonAura/BleedStatus 색 변경**

`BuildPoisonAura` 의 색을 `HitFeedbackPalette.Poison` 동일값으로, `StatusSpecs` 의 `BleedStatus` ColorHex 를 `HitFeedbackPalette.Bleed` 동일 hex 로 교체 (spec §2.2 / 기획서 확정값). **숫자색과 반드시 일치**.

- [ ] **Step 2: HitImpact 빌드 메서드 추가**

`ParticleSystem` 프리팹 생성 — Renderer `renderMode = Mesh`, mesh = 작은 Cube/Sphere(`PrimitiveType` 메시), 텍스처 없음, burst N개, 짧은 수명. `Collider` 제거. `CHPoolable` + `ReturnToPoolAfter` 부착. `Art/FX/HitImpact.prefab` 저장 + Addressables 등록(주소=파일명, 라벨=Resource). 개수·수명은 기획서.

- [ ] **Step 3: DamagePopup 빌드 메서드 추가**

월드스페이스 루트(`RectTransform` 또는 단순 GameObject) + 자식 `TMP_Text` + **`CHText`**(Rule 11) + `DamagePopup` + `CHPoolable`. `DamagePopup._text` 인스펙터 참조 연결. `Art/FX/DamagePopup.prefab` 저장 + Addressables 등록. 폰트 크기는 기획서.

- [ ] **Step 4: BuildAllVisuals 에 신규 빌드 호출 추가** + 메뉴 실행(`Lair/Setup/B1 - Build Visual Prefabs`) 후 프리팹 2종·색 변경 확인.

> Editor 메뉴 실행은 UnityMCP `editor_execute_menu` 또는 사용자 수동. 프리팹 생성·Addressables 등록 검증.

---

## Task 12: 캐릭터 프리팹에 컴포넌트 부착

**Files:**
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs:173-176` 인근

- [ ] **Step 1: AttackJuice / DamageFeedback 부착**

`go.AddComponent<HitFlash>();` 인근(영웅·몬스터 공통 경로)에 추가:

```csharp
            go.AddComponent<HitFlash>();
            go.AddComponent<AttackJuice>();     //# 공격자 스케일 펀치 + 색 플래시
            go.AddComponent<DamageFeedback>();  //# 피격 임팩트 + 데미지 숫자
            go.AddComponent<DespawnOnDeath>();
```

- [ ] **Step 2: 빌더 메뉴 실행** — 영웅+몬스터 6종 프리팹 재생성. 두 컴포넌트가 모든 캐릭터 프리팹에 붙었는지 확인.

---

## Task 13: BattleController 워밍 + Spawner 초기화

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs:639-661` (워밍 메서드)

- [ ] **Step 1: 신규 FX 풀 워밍 + Spawner Init**

워밍 메서드에 추가 (기존 EVisual 워밍 루프와 별도, count 넉넉히 — 기획서 확정):

```csharp
            //# 타격 피드백 FX — 동시 표시 상한 없음 → 넉넉히 워밍 (count 기획서).
            GameObject impact = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.HitImpact);
            if (impact != null) CHMPool.Instance.CreatePool(impact, count: 24);
            GameObject popup = await CHMResource.Instance.LoadAsync<GameObject>(EVisual.DamagePopup);
            if (popup != null) CHMPool.Instance.CreatePool(popup, count: 24);

            //# HitFeedbackSpawner 보장 + 프리팹 핸들 주입.
            HitFeedbackSpawner spawner = FindOrCreateHitFeedbackSpawner();
            spawner.Init(impact, popup);
```

`FindOrCreateHitFeedbackSpawner` — 씬에 없으면 `new GameObject("HitFeedbackSpawner").AddComponent<HitFeedbackSpawner>()`. (씬 진입점 생성이므로 런타임 스폰 풀 대상 아님 — Rule 03 §4 예외: 매니저성 단일 오브젝트.)

- [ ] **Step 2: PlayMode 스모크** — 전투 진입 후 타격 시 콘솔 에러 없이 팝업/임팩트가 Pop/Push 되는지(`sim_play` + 로그 확인).

---

## Task 14: 통합 회귀 + 풀 재사용 테스트

**Files:**
- Test: `Assets/_Lair/Tests/EditMode/Character/HitFeedbackTests.cs`

- [ ] **Step 1: 풀 재사용 리셋 테스트**

`DamageFeedback`/`AttackJuice` 가 OnDisable→OnEnable 후 `_lastHp` 재캐시·스케일 원복·구독 누수 없음 검증.

- [ ] **Step 2: DoT 경로 색 스탬프 테스트**

`BleedAura.Tick` 으로 `FakeTarget`(이동중) 에 데미지 → `StampedColor == HitFeedbackPalette.Bleed` 검증.

- [ ] **Step 3: 전체 EditMode 스위트 실행** — 신규+기존(특히 `HitFlash`·Bleed/Poison) 전부 PASS.

---

## Self-Review (작성자 체크 — 완료)

- **Spec 커버리지**: §1 요소4종→Task5/7/9, §2 색규칙→Task3/4/10, §2.3 스탬프(TakeDamage 前)→Task3/10, §4 FX2종→Task7/8/11, §4.3 색변경→Task11, §5 워밍→Task13, §8 테스트→Task5/9/14. 누락 없음.
- **Placeholder**: 도메인 수치는 "기획서 확정값 + 실행 가능한 예시 기본값" 으로 명시 — TBD 아님(파이프라인 분담 의도).
- **타입 일관성**: `IDamageColorSink.StampDamageColor`, `MeleeAttacker.DamageColor`, `IHitFeedbackSpawner.SpawnImpact/SpawnPopup`, `DamagePopup.Play(Vector3,int,Color)`, `HitFeedbackPalette.Poison/Bleed` — 태스크 간 시그니처 일치 확인.
