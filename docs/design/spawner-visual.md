# 스포너 시각화 — 기능 기획서

> 작성: game-designer · 2026-05-26
> 대상 버전: MVP

---

## 1. 개요

씬에 배치된 Spawner 6개는 현재 비주얼이 없어 플레이어가 스포너 위치와 쿨다운 상태를 확인할 수 없다. 이 기획서는 다음 두 가지를 추가하여 Spawner 를 플레이어에게 명시적으로 노출한다.

1. **Spawner 본체 비주얼** — Cylinder 납작 디스크(포털 패드)로 스포너 위치를 바닥에 표시. 출력 종(종류)에 따른 색상 틴트를 적용해 Replace 카드 효과가 필드에서 즉시 시각 피드백된다.
2. **스폰 쿨다운 바** — 기존 `HpBar.prefab` 재활용. 0 → 꽉 참 = 다음 스폰까지 진행도를 표시하고, 꽉 찼을 때 스폰이 일어나 다시 0으로 초기화된다. 채워지는 동안 색상이 단계적으로 변화해 임박한 스폰을 예고한다.

MVP 비주얼 방침(컨셉 §11.4)에 따라 아트 에셋 없이 프리미티브 도형 + 색상만 사용한다.

---

## 2. Spawner 본체 비주얼

### 2.1 형태

| 항목 | 값 | 근거 |
|---|---|---|
| 메쉬 | Cylinder | 납작 원판 = 포털 바닥 패드. 몬스터 실루엣(Capsule/Sphere/Cube)과 겹치지 않는다 |
| 스케일 | (2.0, 0.05, 2.0) | 몬스터보다 납작하게, 발판 느낌. 반지름 14 ring 에서 시각적으로 잘 구분됨 |
| Y 위치 (로컬) | 0 (바닥) | 씬 바닥 Floor 위에 착지 |
| 콜라이더 | 제거 | 전투 충돌 영향 없도록 |

### 2.2 색상 — 출력 종 틴트

Spawner 본체 색상은 **현재 출력 중인 몬스터 종**의 컨셉 §11.4 색상과 동일하게 적용한다. Replace 카드가 출력 종을 변경할 때 틴트도 즉시 갱신해, 플레이어가 카드 효과를 필드에서 확인할 수 있다.

| 출력 종 | 틴트 색상 | 원본 (컨셉 §11.4) |
|---|---|---|
| Wisp | `#22C55E` | 초록 |
| Wraith | `#6B7280` | 회색 |
| Reaper | `#EF4444` | 빨강 |
| Hex | `#EAB308` | 노랑 |
| Plague | `#A855F7` | 보라 |
| Phantom | `#1F2937` | 검정(어두운 회색) |

초기화 시 `_currentType` 에 맞는 색을 설정하고, `ReplaceOutput()` 호출 시 틴트를 갱신한다. 색상 갱신 책임은 Spawner 에 있는 시각 컴포넌트(`SpawnerBody` 역할)가 맡거나, `Spawner.ReplaceOutput` 에서 이벤트/콜백으로 알림을 내보내 시각 컴포넌트가 수신한다. 구체적 연결 방식은 gameplay-programmer 판단 영역이다.

### 2.3 에셋 위치

`Art/FX/` 폴더에 배치한다. 새 `Art/Environment/` 폴더를 만들지 않는다. Spawner 는 필드 이펙트에 가까운 비주얼이며, MVP 에서 Environment 카테고리를 별도로 설정하기에 Rule 14 폴더 크기가 충분하다.

- 프리팹 경로: `Assets/_Lair/Art/FX/Spawner.prefab`
- 머티리얼 경로: `Assets/_Lair/Art/Materials/Mat_Spawner_[종이름].mat` (6종 각각)
  - 예: `Mat_Spawner_Wisp.mat`, `Mat_Spawner_Wraith.mat` …

> **Addressables 등록 불필요**: Spawner 는 씬에 사전 배치된 정적 오브젝트다(Rule 12 예외). Spawner 비주얼 자식도 씬과 함께 로드되므로 CHMPool 대상이 아니다.

---

## 3. 스폰 쿨다운 바

### 3.1 표시 로직

| 국면 | 바 동작 |
|---|---|
| **초기 지연 국면** (`_firstSpawnDone == false`) | **0% 고정 (빈 상태)** |
| **주기 국면** (`_firstSpawnDone == true`) | `_timer / _spawnPeriod` 로 진행도 표시 (0 → 1) |
| **스폰 직후** | 0 으로 리셋 후 다시 채워지기 시작 |
| **캡(18마리) 도달로 스폰 skip** | 바가 꽉 찬 상태에서 리셋되어 다시 진행 — 캡 차단 여부를 바로 구분하지 않는다 (MVP 단순화) |

초기 지연 국면(0~2.5초)을 바에 표시하지 않는 이유: 초기 지연(0~2.5s)과 이후 주기(6~20s)의 채우기 속도가 달라 바가 채워지다 갑자기 느려지는 현상이 생긴다. 시각적으로 혼란스럽고 정보 가치도 낮으므로 첫 스폰 완료 전까지 빈 상태로 둔다.

### 3.2 색상 전환 규칙

진행도(`progress = 0 ~ 1`) 에 따라 2단계 전환을 사용한다.

| 진행도 구간 | Fill 색상 | 의미 |
|---|---|---|
| 0% ~ 69% | `#60A5FA` (하늘 파랑) | 여유 — 스폰까지 시간이 남음 |
| 70% ~ 100% | `#F97316` (주황) | 임박 — 곧 스폰됨 |

threshold: **0.7** (임시값, 플레이테스트 후 조정 가능).

배경(Background) 색상: `#374151` (회색) — 기존 HP 바 배경과 동일. 몬스터 HP 바와 배경이 같아도 Fill 색상(`#60A5FA` / `#F97316`)이 HP 바 Fill 색상(`#DC2626` 빨강)과 확연히 달라 용도 구분이 된다.

### 3.3 HpBar.prefab 재활용

몬스터 HP 바(`MonsterHpBar`)가 `HpBar.prefab`(`Background / Fill`) 구조를 재활용하듯, `SpawnerCooldownBar` 도 동일 구조를 재활용한다.

- `Fill` Image 의 `fillAmount` = 진행도(0~1)
- `Fill` Image 의 `color` = 진행도 구간에 따른 색상 전환
- 재활용 패턴은 `AttachMonsterHpBar` (에디터 빌더)가 이미 확립해둔 구조와 동일하다

### 3.4 바 위치 및 빌보드

| 항목 | 값 |
|---|---|
| 로컬 Y 위치 | 0.5 (스포너 디스크 높이 0.05/2 + 약간 위) |
| WorldSpace Canvas 크기 | 120 × 20 픽셀 (HpBar.prefab 기본값 동일) |
| 월드 가로 크기 | 1.2 유닛 (`MonsterHpBar` 패턴과 동일) |
| 빌보드 | `LateUpdate` 에서 `Camera.main.transform.rotation` 으로 캔버스 회전 — `MonsterHpBar.LateUpdate` 와 동일 패턴 |

---

## 4. Progress 데이터 노출

`SpawnerCooldownBar` 는 `Spawner` 의 쿨다운 진행 상태를 읽어야 한다. `MonsterHpBar` 가 `IHealth` 인터페이스를 `GetComponentInParent<IHealth>()` 로 읽는 패턴(Rule 06)을 그대로 따른다.

`Spawner.cs` 에 쿨다운 진행도를 외부에 노출하는 **읽기 전용 계약**이 필요하다. 구체적으로는:

- 초기 지연 국면일 때는 `0f` 반환
- 주기 국면일 때는 `_timer / _spawnPeriod` 를 `[0, 1]` 클램프해 반환

이 값을 `Battle/CommonInterface.cs` 에 **`ISpawnerProgress` 인터페이스**로 선언하고, `Spawner` 가 구현하게 한다. `SpawnerCooldownBar` 는 인터페이스로만 읽는다.

---

## 5. 컴포넌트 구성

### 5.1 Spawner 프리팹 구성 (신규)

씬에 배치된 Spawner 오브젝트에 다음 자식을 추가하거나, 에디터 빌더를 통해 Spawner.prefab 을 새로 생성하여 씬 인스턴스를 교체한다. 두 방식 중 어느 것이 더 적절한지는 gameplay-programmer 가 판단한다. 단, 기존 `AttachMonsterHpBar` 에디터 빌더 패턴을 그대로 따르는 쪽을 권장한다.

```
Spawner (씬 루트 오브젝트)
  ├ SpawnerBody (Cylinder 납작 디스크)
  │    MeshRenderer — Mat_Spawner_[종].mat
  │    Collider 제거
  └ CooldownBarWrapper (WorldSpace Canvas)
       SpawnerCooldownBar.cs
       └ HpBar (HpBar.prefab 인스턴스)
            └ Background
                 └ Fill (Image — fillAmount = progress)
```

### 5.2 SpawnerCooldownBar 컴포넌트

| 항목 | 내용 |
|---|---|
| 파일 위치 | `Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs` |
| 네임스페이스 | `Lair.Battle` |
| 부착 위치 | CooldownBarWrapper 오브젝트 |
| 의존 인터페이스 | `ISpawnerProgress` (Battle/CommonInterface.cs) |
| 상위 탐색 패턴 | `GetComponentInParent<ISpawnerProgress>()` (Rule 06) |
| 직렬화 필드 | `Image _fill` — 빌더가 주입 (MonsterHpBar 패턴 동일) |
| 동작 | `Update()` 매 프레임: progress 읽기 → fillAmount 갱신 → 색상 갱신 |
| 빌보드 | `LateUpdate()` 에서 카메라 rotation 적용 |
| 색상 threshold | `0.7f` (상수 — 추후 필드로 노출 가능) |
| 색상 Cool | `#60A5FA` |
| 색상 Warm | `#F97316` |

### 5.3 Spawner 추가 계약 (ISpawnerProgress)

`Battle/CommonInterface.cs` 에 추가:

```
ISpawnerProgress
  float Progress { get; }  // 0~1, 초기 지연 국면 = 0
```

`Spawner.cs` 에 `ISpawnerProgress` 구현 추가. 계산 로직:
- `_firstSpawnDone == false` → `0f`
- `_firstSpawnDone == true` → `Mathf.Clamp01(_timer / _spawnPeriod)`

---

## 6. 에디터 빌더

`LairCharacterPrefabBuilder` 의 `AttachMonsterHpBar` 패턴을 따라 에디터 빌더 메뉴를 추가한다. 기획 관점에서 필요한 빌드 스텝:

1. SpawnerBody 자식 생성 — Cylinder, (2.0, 0.05, 2.0) 스케일, 콜라이더 제거, 머티리얼 적용
2. CooldownBarWrapper 자식 생성 — WorldSpace Canvas, 위치 (0, 0.5, 0)
3. HpBar.prefab 인스턴스 nest — full-stretch
4. Fill Image 탐색 → `SpawnerCooldownBar._fill` 주입
5. SpawnerCooldownBar 컴포넌트 부착
6. (선택) 씬에 배치된 기존 Spawner 오브젝트에 자동 적용

빌더 메뉴명 예시: `Lair/Setup/S1 - Attach Spawner Visuals`

---

## 7. MVP 범위 확인

| 항목 | 범위 |
|---|---|
| Spawner 본체 프리미티브 비주얼 | MVP 내 (컨셉 §11.4 프리미티브 비주얼 방침) |
| 쿨다운 바 (HpBar.prefab 재활용) | MVP 내 (기존 컴포넌트 재사용) |
| 색상 틴트로 출력 종 표시 | MVP 내 (시너지 가시성 원칙 — Replace 카드 효과 확인) |
| 사운드 연동 | MVP 외 — 사운드 작업 없음 |
| 아트 에셋 | MVP 외 — 프리미티브만 |

캡 차단 시 바 동작(빈 상태 유지 vs 리셋 진행)은 MVP 단순화로 "리셋 진행"을 선택한다. 플레이어가 캡 개념을 학습하는 데 방해가 될 수 있으나 추가 상태 노출 비용이 더 크다. 플레이테스트 후 재검토.

---

## 8. 구현 요청사항 (gameplay-programmer 용)

**Enum**
- `CommonEnum.cs` 의 `EVisual` 에 `Spawner` 값 추가 여부: Spawner 는 씬 정적 오브젝트이므로 Addressables 키가 필요 없다. `EVisual.Spawner` 추가하지 않는다.
- 머티리얼은 에디터 빌더가 직접 생성하므로 별도 Enum 불필요.

**Interface**
- `Assets/_Lair/Scripts/Battle/CommonInterface.cs` 에 `ISpawnerProgress` 추가:
  ```
  public interface ISpawnerProgress
  {
      float Progress { get; }  // 0 = 아직 스폰 없음 또는 방금 스폰 / 1 = 스폰 임박
  }
  ```
- `Spawner.cs` 가 `ISpawnerProgress` 구현 (Progress 프로퍼티 추가)

**에셋 키**
- Addressables 등록 없음 (씬 정적 오브젝트, Rule 12 예외)
- 에디터 빌더가 프리팹/머티리얼을 직접 경로로 생성

**에셋 경로**
- 프리팹: `Assets/_Lair/Art/FX/Spawner.prefab` (선택적 — 빌더 방식에 따라 프리팹 없이 씬 직접 수정도 가능)
- 머티리얼: `Assets/_Lair/Art/Materials/Mat_Spawner_Wisp.mat` 외 5종

**SO 스키마 / 수치 필드**
- 새 SO 없음. 수치는 `SpawnerCooldownBar.cs` 상수로 인라인 (threshold `0.7f`, 색상 두 가지)
- 수치가 잦은 튜닝 대상이 되면 추후 `BalanceConfig` 에 흡수 검토

**컴포넌트 파일**
- 신규 `.cs` 파일: `Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs`
- 수정 `.cs` 파일:
  - `Assets/_Lair/Scripts/Battle/CommonInterface.cs` — `ISpawnerProgress` 추가
  - `Assets/_Lair/Scripts/Battle/Spawner.cs` — `ISpawnerProgress` 구현(Progress 프로퍼티), `OnEnable` 및 `ReplaceOutput` 호출 시 시각 컴포넌트에 출력 종 변경 알림 (SpawnerBody 틴트 갱신 훅)
- 에디터 빌더: `Assets/_Lair/Editor/LairSpawnerVisualBuilder.cs` (신규, 또는 기존 빌더에 통합)

**머티리얼 수**
`Mat_Spawner_[종이름].mat` 6종을 기본 제안하지만, runtime 에 `MaterialPropertyBlock` 으로 색상만 교체하는 방식(공유 1벌)도 허용한다. 최종 선택은 gameplay-programmer 재량.

**색상 상수 요약**
| 용도 | Hex |
|---|---|
| Fill Cool (0~69%) | `#60A5FA` |
| Fill Warm (70~100%) | `#F97316` |
| Spawner Wisp 틴트 | `#22C55E` |
| Spawner Wraith 틴트 | `#6B7280` |
| Spawner Reaper 틴트 | `#EF4444` |
| Spawner Hex 틴트 | `#EAB308` |
| Spawner Plague 틴트 | `#A855F7` |
| Spawner Phantom 틴트 | `#1F2937` |

---

## 변경 이력

- **v0.1 (2026-05-26)**: 초안. Spawner 본체 비주얼(Cylinder 납작 디스크 + 출력 종 틴트) + 쿨다운 바(HpBar.prefab 재활용, 2단계 색상 전환) 기획.
