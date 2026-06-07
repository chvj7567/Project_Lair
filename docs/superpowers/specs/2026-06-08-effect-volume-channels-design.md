# 효과음 채널별 볼륨 세분화 — Design Spec

- 날짜: 2026-06-08
- 단계: MVP (인스펙터 기반 개발 튜닝 확장 — 인게임 설정 화면 신설 아님)
- 파이프라인: start-develop-auto

## 1. 의도 / 범위

현재 볼륨 조절은 `AudioVolumeSettings`(인스펙터 기반, Play 중 라이브 조절)에 Master · BGM · Effect 3슬라이더뿐이고, **Effect는 모든 효과음에 일괄 적용**된다. 효과음을 평타·스킬별로 따로 튜닝할 수 없다.

**목표**: 효과음 중 평타 3종과 스킬 3종을 각각 독립 볼륨 슬라이더로 분리.

- 대상(6종 개별): `Slash01` · `Slash02` · `Stab` · `P1Skill` · `P2Skill` · `P3Skill`
- 비대상(기존 Effect 노브 유지): `Hit` · `AcquireSkill` · `CardSelect`
- 기존 Master · BGM · Effect 노브는 그대로.

**범위 밖 (YAGNI)**: 인게임 플레이어 설정 UI, BGM 세분화, 6채널의 PlayerPrefs 영속화.

## 2. 결정 락 (locked)

| 결정 | 값 |
|---|---|
| 분리 단위 | 평타 3 + 스킬 3 = 6개 개별 슬라이더 |
| 비대상 효과음 | Hit/AcquireSkill/CardSelect는 Effect 노브만 |
| 볼륨 곱셈 모델 | **개별채널배수 × EffectVolume × MasterVolume** (3중 곱) |
| 영속성 | 6개 신규 배수는 **PlayerPrefs 미저장** — 인스펙터 serialize 값이 단일 진실 |
| 기존 API | `SetBGMVolume`/`SetEffectVolume`/`SetMasterVolume` 시그니처·동작 유지 (하위호환) |
| 노출 방식 | 인스펙터 기반 dev 튜닝 확장 (설정 화면 신설 아님 — MVP 제약 준수) |

## 3. 아키텍처 (단방향: 게임 → 인프라)

```
AudioVolumeSettings (게임, MonoBehaviour)
   └─ Apply()/OnValidate() ─▶ CHMSound.SetChannelVolume(EAudio.Xxx, v)   (인프라)
                                       └─ Play() 시 source.volume = 채널배수 × Effect × Master
```

### 3.1 CHMSound 인프라 확장 (`com.chvj.unityinfra/Runtime/Audio/CHMSound.cs`, Rule 03)

- 채널별 배수 저장: `Dictionary<int, float> _channelVolume` (기본 1.0, 미설정 키는 1.0 fallback).
- 신규 public API:
  - `void SetChannelVolume(Enum audioType, float volume)` — 해당 채널 배수 설정(Clamp01). `_audioSourceArr` 미초기화면 무동작 가드.
  - (선택) `float GetChannelVolume(Enum audioType)` — 테스트/조회용. 미설정 시 1.0.
- `Play()` 효과음 분기 변경: 현재 `source.volume = EffectVolume * Ratio` → `source.volume = channelMul * EffectVolume * Ratio`. **BGM 분기는 불변.**
- 의존 방향 유지: 인프라 → 게임 역참조 없음. enum은 `Enum` 베이스 타입으로만 받음(게임의 EAudio 직접 참조 금지).

### 3.2 AudioVolumeSettings 확장 (`Assets/_Lair/Scripts/Battle/AudioVolumeSettings.cs`)

- `[SerializeField, Range(0f,1f)]` 6개 추가 (기본 1.0): `_slash01` `_slash02` `_stab` `_p1Skill` `_p2Skill` `_p3Skill`.
- `Apply()`: 기존 master→bgm→effect 적용 직후, 6채널에 `CHMSound.Instance.SetChannelVolume(EAudio.Xxx, value)` 호출.
- `OnValidate()` 라이브 반영 로직 그대로(Play 중일 때만 Apply).
- Hit/AcquireSkill/CardSelect는 SetChannelVolume 미호출 → 배수 1.0 → 기존 Effect 노브만 받음.

## 4. 데이터 흐름 / 엣지

- 미설정 채널: `_channelVolume` 에 없으면 1.0 → 기존 동작과 동일(회귀 안전).
- `SetEffectVolume`(라이브 드래그)는 모든 효과음 source.volume을 `Effect×Master`로 덮지만, `Play()`가 매 one-shot 직전 `채널배수×Effect×Master`로 재설정하므로 다음 재생부터 채널배수가 반영됨(one-shot은 짧아 체감 무관).
- BGM/Master 동작 불변.

## 5. 테스트 (EditMode)

- `SetChannelVolume(key, 0.5)` 후 해당 효과음 채널의 최종 볼륨이 `0.5 × Effect × Master` 인지.
- 미설정 채널은 1.0 fallback → `Effect × Master` 와 동일.
- BGM 채널은 `SetChannelVolume` 영향 없음.
- 잘못된/미초기화 상태에서 `SetChannelVolume` 호출 시 예외 없이 무동작.

## 6. 영향 파일

- 수정(인프라): `Packages/com.chvj.unityinfra/Runtime/Audio/CHMSound.cs`
- 수정(게임): `Assets/_Lair/Scripts/Battle/AudioVolumeSettings.cs`
- 참조: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (EAudio — 변경 없음, 키 참조만)
- 테스트: `Assets/_Lair/Tests/EditMode/` 신규
