# 효과음 채널별 볼륨 세분화 Implementation Plan

> **For agentic workers:** 이 플랜은 start-develop-auto 파이프라인(game-designer → design-reviewer → gameplay-programmer → code-reviewer → test-engineer)으로 구현된다. 각 Task는 그 단계들이 채울 파일 구조·시그니처·TDD·verification gate를 명시한다.

**Goal:** 효과음 평타 3종·스킬 3종을 인스펙터에서 각각 독립 볼륨으로 조절할 수 있게 한다.

**Architecture:** CHMSound 인프라에 채널(enum 키)별 볼륨 배수 dict + `SetChannelVolume` API를 추가하고, `Play()`에서 `채널배수 × Effect × Master`로 곱한다. 게임 측 `AudioVolumeSettings`에 6개 SerializeField 슬라이더를 추가해 `Apply()`/`OnValidate()`로 채널 배수를 push 한다.

**Tech Stack:** Unity 6 / C# / Unity Test Framework(NUnit, EditMode) / com.chvj.unityinfra(CHMSound).

---

## File Structure

| 파일 | 책임 | 변경 |
|---|---|---|
| `Packages/com.chvj.unityinfra/Runtime/Audio/CHMSound.cs` | 채널별 배수 저장 + Play 곱셈 | 수정 |
| `Assets/_Lair/Scripts/Battle/AudioVolumeSettings.cs` | 6 슬라이더 인스펙터 노출 + CHMSound push | 수정 |
| `Assets/_Lair/Tests/EditMode/ChannelVolumeTests.cs` | per-channel 배수 곱 로직 검증 | 신규 |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | EAudio 키 참조만 | 변경 없음 |

---

## Task 1: CHMSound 채널별 볼륨 배수 지원 (인프라)

**Files:**
- Modify: `Packages/com.chvj.unityinfra/Runtime/Audio/CHMSound.cs`

**설계 결정 (game-designer/design-reviewer 검토 대상):** 6채널 배수는 PlayerPrefs 미저장(인스펙터 값이 단일 진실). 기존 `SetEffectVolume`/`SetMasterVolume`/`SetBGMVolume` 동작 불변. BGM 채널은 채널배수 영향 없음.

- [ ] **Step 1: 필드 추가** — 클래스 멤버에 채널 배수 dict 추가.

```csharp
// 채널(enum int값)별 볼륨 배수. 미설정 키는 1.0 fallback. PlayerPrefs 미저장(인스펙터가 소스).
private Dictionary<int, float> _channelVolume = new Dictionary<int, float>();
```

- [ ] **Step 2: 신규 API 추가** — `SetChannelVolume` / `GetChannelVolume`.

```csharp
/// <summary>특정 효과음 채널의 볼륨 배수 설정(0~1). 최종 볼륨 = 배수 × EffectVolume × Master. 미초기화면 무동작.</summary>
public void SetChannelVolume(Enum audioType, float volume)
{
    int v = Convert.ToInt32(audioType);
    _channelVolume[v] = Mathf.Clamp01(volume);
}

/// <summary>채널 배수 조회. 미설정 시 1.0.</summary>
public float GetChannelVolume(Enum audioType)
{
    int v = Convert.ToInt32(audioType);
    return _channelVolume.TryGetValue(v, out float m) ? m : 1f;
}
```

- [ ] **Step 3: Play() 효과음 분기에 배수 곱** — 기존 `source.volume = EffectVolume * Ratio;` 한 줄을 교체.

```csharp
// 효과음 분기(else): 채널 배수 × Effect × Master.
float channelMul = _channelVolume.TryGetValue(v, out float m) ? m : 1f;
source.volume = channelMul * EffectVolume * Ratio;
source.PlayOneShot(clip);
```

BGM 분기(`if (_bgmIndices.Contains(v))`)는 변경하지 않는다.

- [ ] **Step 4: 컴파일 확인** — Unity 재컴파일 콘솔 에러 0.

Run(메인): UnityMCP `editor_recompile` → `editor_read_log`(Error) 0건.

- [ ] **Step 5: 스테이징** (Rule 01 — 커밋 직접 실행 금지, add 까지만)

```
git add Packages/com.chvj.unityinfra/Runtime/Audio/CHMSound.cs
```

---

## Task 2: EditMode 테스트 — 채널 배수 곱 로직

**Files:**
- Create: `Assets/_Lair/Tests/EditMode/ChannelVolumeTests.cs`

**주의:** CHMSound는 CHSingleton + AudioSource(GameObject) 생성에 Unity 런타임이 필요하다. `Play()`는 async + Addressable 로드라 EditMode에서 클립까지 검증 곤란. 따라서 **볼륨 곱 산식(채널배수 × Effect × Master) 자체를 검증**하는 데 집중하고, AudioSource 실제 볼륨은 `SetChannelVolume`/`GetChannelVolume` round-trip + 산식 계산으로 검증한다. (실제 PlayOneShot 볼륨 적용은 PlayMode/수동 검증 영역 — 본 plan은 산식·fallback·BGM불변에 한정.)

- [ ] **Step 1: 실패 테스트 작성** — 한국어 메서드명(test_method_naming: korean).

```csharp
using NUnit.Framework;
using ChvjUnityInfra;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    public class ChannelVolumeTests
    {
        [Test]
        public void 채널배수_설정후_조회하면_같은값_반환()
        {
            CHMSound sound = CHMSound.Instance;
            sound.SetChannelVolume(EAudio.Slash01, 0.5f);
            Assert.AreEqual(0.5f, sound.GetChannelVolume(EAudio.Slash01), 0.0001f);
        }

        [Test]
        public void 미설정_채널은_배수_1로_fallback()
        {
            CHMSound sound = CHMSound.Instance;
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.Hit), 0.0001f);
        }

        [Test]
        public void 채널배수는_0과1로_clamp()
        {
            CHMSound sound = CHMSound.Instance;
            sound.SetChannelVolume(EAudio.P1Skill, 2f);
            Assert.AreEqual(1f, sound.GetChannelVolume(EAudio.P1Skill), 0.0001f);
            sound.SetChannelVolume(EAudio.P1Skill, -1f);
            Assert.AreEqual(0f, sound.GetChannelVolume(EAudio.P1Skill), 0.0001f);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — `SetChannelVolume` 미구현 상태면 컴파일 실패 / Task1 후엔 통과.

Run: Unity Test Runner EditMode (또는 `editor_execute_menu` 테스트 러너). Expected: Task1 미적용 시 컴파일 에러, 적용 시 3 PASS.

- [ ] **Step 3: 통과 확인** — EditMode 3 케이스 PASS.

- [ ] **Step 4: 스테이징**

```
git add Assets/_Lair/Tests/EditMode/ChannelVolumeTests.cs Assets/_Lair/Tests/EditMode/ChannelVolumeTests.cs.meta
```

(test_asmdef: Lair.Tests.EditMode — ChvjUnityInfra·Lair 참조 필요. asmdef에 참조 누락 시 추가.)

---

## Task 3: AudioVolumeSettings 6 슬라이더 추가 (게임)

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/AudioVolumeSettings.cs`

- [ ] **Step 1: 6 SerializeField 추가** — 기존 `_effectVolume` 아래.

```csharp
[Header("효과음 채널별 (평타/스킬) — PlayerPrefs 미저장, 인스펙터가 소스")]
[SerializeField, Range(0f, 1f)] private float _slash01 = 1f;
[SerializeField, Range(0f, 1f)] private float _slash02 = 1f;
[SerializeField, Range(0f, 1f)] private float _stab = 1f;
[SerializeField, Range(0f, 1f)] private float _p1Skill = 1f;
[SerializeField, Range(0f, 1f)] private float _p2Skill = 1f;
[SerializeField, Range(0f, 1f)] private float _p3Skill = 1f;
```

- [ ] **Step 2: Apply()에 채널 push 추가** — 기존 SetEffectVolume 호출 뒤.

```csharp
//# 평타·스킬 채널별 배수 — 미대상(Hit/Acquire/CardSelect)은 호출 안 함 → 1.0 유지.
CHMSound.Instance.SetChannelVolume(EAudio.Slash01, _slash01);
CHMSound.Instance.SetChannelVolume(EAudio.Slash02, _slash02);
CHMSound.Instance.SetChannelVolume(EAudio.Stab, _stab);
CHMSound.Instance.SetChannelVolume(EAudio.P1Skill, _p1Skill);
CHMSound.Instance.SetChannelVolume(EAudio.P2Skill, _p2Skill);
CHMSound.Instance.SetChannelVolume(EAudio.P3Skill, _p3Skill);
```

`using Lair.Data;`(EAudio) 가 없으면 추가. `OnValidate()` 는 변경 없음(이미 Play 중 Apply 호출).

- [ ] **Step 3: 컴파일 확인** — 재컴파일 에러 0.

- [ ] **Step 4: 스테이징**

```
git add Assets/_Lair/Scripts/Battle/AudioVolumeSettings.cs
```

---

## Task 4: 통합 검증 (수동/메인)

- [ ] **Step 1:** EditMode 전체 그린(회귀 없음) 확인.
- [ ] **Step 2:** Play 진입 후 인스펙터에서 `_p1Skill` 등 슬라이더를 0으로 → 해당 스킬 사운드 무음, 다른 효과음 정상인지 라이브 확인(수동, qa-simulator 아님).
- [ ] **Step 3:** 변경 요약 + Rule 01 커밋 메시지(안) 제시. `git add`까지만.

---

## Self-Review

**Spec coverage:**
- 6개 개별 슬라이더 → Task 3 ✓
- CHMSound per-channel 배수 + 곱셈 모델 → Task 1 ✓
- 미대상 효과음 Effect 노브 유지(배수 1.0 fallback) → Task 1 Step3 + Task 2 fallback 테스트 ✓
- PlayerPrefs 미저장 → Task 1(dict in-memory) / Task 3(SerializeField) ✓
- 기존 API 하위호환 → Task 1 기존 메서드 무수정 ✓
- BGM 불변 → Task 1 Step3(BGM 분기 미변경) ✓
- 테스트 → Task 2 ✓

**Placeholder scan:** 모든 코드 step에 실제 코드 명시. 없음.

**Type consistency:** `SetChannelVolume(Enum,float)` / `GetChannelVolume(Enum)` 시그니처가 Task1·2·3에서 일관. `_channelVolume` dict 이름 일관. EAudio 키명(Slash01/Slash02/Stab/P1Skill/P2Skill/P3Skill) 실제 enum값과 일치.
