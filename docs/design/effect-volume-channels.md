# 효과음 채널별 볼륨 세분화 — 기획서 (얇은 도메인 결정)

## § 헤더

- **목표**: 효과음 중 평타 3종(Slash01/Slash02/Stab)·스킬 3종(P1Skill/P2Skill/P3Skill)을 인스펙터에서 각각 독립 볼륨으로 dev 튜닝할 수 있게 한다.
- **검증 가설**: 평타가 연속 타격으로 겹쳐 들리는 라우드니스 문제를, 채널별 배수를 라이브로 내려 청각 밸런스를 잡을 수 있는가.
- **현재 단계 범위 적합성**: 범위 내. MVP §8 — 인스펙터 기반 dev 튜닝 확장이며 인게임 설정 화면을 신설하지 않는다.
- **핵심 메커니즘**: CHMSound에 채널(enum 키)별 볼륨 배수 dict를 두고, 효과음 재생 시 `채널배수 × EffectVolume × MasterVolume` 으로 곱한다. 게임 측 AudioVolumeSettings가 6개 슬라이더 값을 push 한다. BGM·Master·기존 Effect 노브 동작 불변.

> 본 기획서는 spec/plan에 기술 결정이 모두 락된 **얇은 기획서**다. 새 메커니즘을 발명하지 않으며, 도메인성 결정 3건 확정과 spec/plan 모순 점검만 수행한다.

---

## 1. 도메인 결정

### 결정 1 — 6개 채널 슬라이더 초기값: 전부 1.0 유지 (권장)

| 채널 | 초기값 |
|---|---|
| Slash01 | 1.0 |
| Slash02 | 1.0 |
| Stab | 1.0 |
| P1Skill | 1.0 |
| P2Skill | 1.0 |
| P3Skill | 1.0 |

**근거**: 평타 3종이 영웅 자동 기본공격으로 연속·교대 재생되어 누적 시 시끄러울 여지는 있으나, (a) 실제 누적 라우드니스를 측정한 데이터가 없어 0.8 같은 특정 감쇠값을 단정할 근거가 없고, (b) 본 작업 자체가 "인스펙터에서 라이브로 직접 내려 귀로 맞추는" dev 튜닝 도구다. 1.0을 중립 출발점(= 기존 동작과 정확히 동일, 회귀 0)으로 두고 dev가 슬라이더를 내리며 맞추는 것이 가설 검증에 맞다. 평타 감쇠가 필요하다고 청취로 확인되면 그때 인스펙터 serialize 값으로 고정한다 — 코드 기본값을 미리 0.8로 박으면 "1.0 = 무변경" 이라는 기준선을 잃는다.

**대안 검토**:
- 대안 A(채택): 6채널 전부 1.0. trade-off — 첫 진입 시 평타가 시끄러울 수 있으나 즉시 슬라이더로 조정 가능. 기준선 보존.
- 대안 B: 평타 3종만 0.8, 스킬 3종 1.0. trade-off — 첫 청취 인상이 부드러워질 수 있으나, 0.8의 근거가 없고 "무변경 기준선" 이 사라져 dev가 기존 대비 얼마나 내렸는지 분간 어려움.

→ 데이터 없는 임의 감쇠보다 기준선 보존 + 라이브 튜닝이 우선. **A 채택.** (감쇠값이 필요하면 청취 후 결정 — 결정 메트릭: dev 라이브 청취에서 평타 누적이 스킬/Hit 대비 거슬리는 정도.)

### 결정 2 — 곱셈 모델 확정: 채널배수 × EffectVolume × MasterVolume

플레이어(여기선 dev) 체감상 자연스럽다. **Effect 노브가 6채널 전체의 상위 마스터로 동작**하고, Master가 다시 그 위 최상위로 동작하는 단방향 중첩 구조다. 채널 슬라이더는 "이 효과음을 Effect 기준 대비 몇 % 로 낼지" 의 상대 배수이므로, Effect를 0으로 내리면 6채널 모두 함께 0이 되는 동작이 직관과 일치한다. spec §2 결정 락과 동일. **확정.**

### 결정 3 — 대상 경계 확정: Hit/AcquireSkill/CardSelect는 개별 노브 없이 Effect만

세분 요구가 들어온 대상은 영웅의 평타(Slash01/Slash02/Stab)와 스킬(P1Skill/P2Skill/P3Skill)뿐이다. Hit(데미지 임팩트), AcquireSkill(해금 컷인), CardSelect(선택 확정 SFX)는 빈도·중첩 문제가 평타/스킬만큼 크지 않고 세분 요구도 없다. 이 3종은 `SetChannelVolume` 미호출 → 배수 1.0 fallback → 기존 Effect 노브만 받는다 (회귀 0). spec §2·§3.2와 모순 없음. **그대로 락.**

---

## 2. spec / plan 일치 확인

| 항목 | spec/plan | 본 기획서 | 일치 |
|---|---|---|---|
| 분리 단위 | 평타 3 + 스킬 3 = 6 개별 | 동일 | ✓ |
| 곱셈 모델 | 채널배수 × Effect × Master | 결정 2 동일 | ✓ |
| 비대상 3종 | Effect 노브만 (배수 1.0 fallback) | 결정 3 동일 | ✓ |
| 초기값 | plan Task3 = 전부 1.0 | 결정 1 = 전부 1.0 | ✓ |
| 영속성 | PlayerPrefs 미저장, 인스펙터 serialize 단일 진실 | 동일 | ✓ |
| 기존 API | SetBGM/Effect/MasterVolume 동작 유지 | 동일 | ✓ |
| 노출 방식 | 인스펙터 dev 튜닝 확장 (설정 화면 신설 아님) | 동일 (MVP §8 준수) | ✓ |
| EAudio 키 | Slash01/Slash02/Stab/P1Skill/P2Skill/P3Skill | CommonEnum.cs 실제 enum과 글자 일치 확인 | ✓ |

**불일치 발견**: 없음. spec/plan과 모순 0건. (spec이 단일 진실이므로 본 기획서는 결정 1의 초기값을 spec과 동일한 1.0으로 정렬했으며, 임의 변경하지 않았다.)

---

## 3. 구현 요청사항 (gameplay-programmer 용)

기술 결정은 spec/plan에 이미 락되어 있다. 본 표는 gameplay-programmer가 채울 도메인 값(특히 6 기본값)만 명시한다.

### 6 채널 슬라이더 기본값 (AudioVolumeSettings SerializeField)

| 필드 | EAudio 키 | 기본값 | 비고 |
|---|---|---|---|
| `_slash01` | `EAudio.Slash01` | 1.0 | 평타 variant 0 |
| `_slash02` | `EAudio.Slash02` | 1.0 | 평타 variant 1 |
| `_stab` | `EAudio.Stab` | 1.0 | 평타 variant 2 |
| `_p1Skill` | `EAudio.P1Skill` | 1.0 | 스킬 DashStrike |
| `_p2Skill` | `EAudio.P2Skill` | 1.0 | 스킬 AoeNova |
| `_p3Skill` | `EAudio.P3Skill` | 1.0 | 스킬 OrbitingBlade |

모두 `[SerializeField, Range(0f, 1f)]`. 비대상 3종(Hit/AcquireSkill/CardSelect)은 슬라이더·`SetChannelVolume` 호출 없음.

- **Enum 추가**: 없음 — EAudio의 기존 6개 키 참조만. (CommonEnum.cs 변경 없음.)
- **Interface 추가**: 없음.
- **에셋 키**: 없음 — 신규 에셋·프리팹 없음. 기존 효과음 클립 재사용.
- **SO 스키마**: 없음 — ScriptableObject 변경 없음. AudioVolumeSettings(MonoBehaviour) SerializeField 6개 추가가 데이터 면의 전부이며 PlayerPrefs 미저장.

---

## 4. MVP 범위 확인

- 인게임 설정 화면 신설 없음 — 인스펙터 SerializeField 6개 추가뿐. MVP §8 "설정 화면 신설 금지" 준수.
- 메타/서버 무관.
- 신규 메커니즘·콘텐츠 없음 — 기존 효과음 볼륨 경로의 세분화 도구.
