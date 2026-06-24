# Content Audit — 2026-06-25 — 시너지 임계값 3/5/7 하드코딩 · 액티브 트리거 9→5 감소 후 픽 경제 미반영

- 작성일: 2026-06-25
- 카테고리: BalanceConfig 시너지 임계 손잡이
- 관련 문서: `docs/superpowers/specs/2026-06-03-monster-cap-removal-active-trigger-trim-design.md` §2B·§4, `docs/design/card-renewal.md` §7, `docs/design/card-3pick-cap.md`

---

## 1. 현황 표

| 카테고리 | 컨셉 수 | 구현 수 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 0 |
| 몬스터 | 6 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom .prefab) | 0 |
| 패시브 카드 | 16 | 16 .asset | 0 |
| 액티브 카드 | 12 | 12 .asset | 0 |

**계획 있으나 미구현**
- SwarmRush 카드 — 원안 Multiply 교체 예정; `FastBreedingEffect.cs` 잔존, SwarmRushEffect 미생성 (`2026-06-04-multiply-to-swarm-rush-active-replace.md` 감사 기록)

**QA 권고 미해결**
- `CardSelectionPopup.PickByIndex()` 부재 → `DebugAutoPicker` delegate 훅 미구현 (`docs/qa-reports/2026-05-22.md` §3 요청 — 구현 확인 없음)
- 2026-06-03 spec §4: "마무리 후 qa-simulator 별도 검증을 제안" — 실제 시뮬레이션 미수행

**과거 감사 이력 요약** (총 25건, 2026-05-28 ~ 2026-06-24)
- `2026-05-28` ~ `2026-06-07`: 10건 (git 커밋 포맷 상이로 grep 미포착, ls 확인)
- `2026-06-08` ~ `2026-06-24`: 15건 (git log grep 확인)
- 커버된 주요 주제: Debuff Tier3, Swarm Tier3, Tank Tier3 HP×1.4, Multiply→SwarmRush, 각 축 개별 카드 수치(BloodThirst·BleedRatio·HexRange·TankTripleStack·TimestopFear·SwarmTier2Period·HeroPoisonAura·HeroAttackDown·PhantomMoveSpeed·Frenzy+MarkOfDeath·TankPowerScale·PlagueSlow·SwarmFloor·ReaperAtkCdFloor)
- 본 후보는 **SynergyTierThreshold 하드코딩 + 픽 경제 변화 미반영** — 25건 중 해당 항목 없음

---

## 2. 후보 도출 근거

### 2.1 발견된 갭

`BuildSynergyService.cs` 는 시너지 Tier 판정 임계값 `(3, 5, 7)` 을 소스 코드에 직접 고정한다.
`Assets/_Lair/Data/BalanceConfig.asset` 에는 해당 필드가 없다. 즉:

- **인스펙터 또는 에셋 편집만으로 임계값을 바꿀 수 없다**
- qa-simulator 가 "Tier 임계값 x로 조정 → N판 재시뮬" 루프를 돌릴 수 없다
- game-designer 가 밸런스 조정 사이클(`.claude/project.md` §밸런스 조정 흐름 6번 단계)을 따를 때 코드 수정 없이는 임계 조정이 불가능하다

### 2.2 2026-06-03 액티브 트리거 감소가 픽 경제를 바꿨다

`docs/superpowers/specs/2026-06-03-monster-cap-removal-active-trigger-trim-design.md` §2B:
- 변경 전: 30·60·90·120·150·180·210·240·270초 — 9회 픽 기회
- 변경 후: 30·90·150·210·270초 — **5회 픽 기회**

패시브(HP% 9회) + 액티브 픽 기회가 합산되는 한 판의 최대 총픽 수:
| 상태 | 액티브 | 패시브 | 합계 |
|---|---|---|---|
| 변경 전 | 9 | 9 | **18** |
| 변경 후 | 5 | 9 | **14** |

### 2.3 Tier3 도달 필요 픽 집중도 변화

Tier 임계값이 3/5/7로 고정된 상태에서 하나의 축 7장 전부를 확보하려면:

| 판 상황 | 총픽 수 | Tier3(7장) 필요 비율 |
|---|---|---|
| 변경 전 최대 (18픽) | 18 | 7/18 = **38.9%** |
| 변경 후 최대 (14픽) | 14 | 7/14 = **50.0%** |
| 평균 영웅 처치 시간 약 76초 (§8 목표 2~4분 중간값) | ~10 | 7/10 = **70.0%** |

> 평균 게임 기준: 30s(1픽)+90s(2픽) = 76초 도달 전 2회 액티브, 패시브는 HP% — 76초 내 처치 시 액티브 픽 ~2회, 패시브 ~4~6회(영웅 피해 속도 의존). 보수 추정 10픽.

**Tier3 진입이 사실상 단일 축 올인 전략에서만 가능해졌다.** 그러나 임계값 자체가 BalanceConfig 에 없으므로 이 체감 변화를 수치로 대응할 손잡이가 없다.

### 2.4 spec 스스로 검증을 요청했다

`2026-06-03 spec §4`:
> "마무리 후 **qa-simulator 별도 검증을 제안**한다."

해당 QA 는 `docs/qa-reports/2026-05-22.md` 의 BLOCKED 상태(DebugAutoPicker 미구현) 로 인해 실제로 이루어지지 않았다. 임계값 손잡이 설계 + QA 가 함께 해소되어야 한다.

---

## 3. 스코어

| 항목 | 점수 (1~5) | 근거 |
|---|---|---|
| 검증가치 | **5** | Tier 분포가 실 플레이 가능/불가를 가름 — 데이터 없이 감각으로만 조율 중 |
| 구현비용 | **2** | BalanceConfig 에 int 필드 3개 추가 + BuildSynergyService 참조 전환; 테스트 수정 포함해도 소규모 |
| 시너지폭 | **5** | 4개 축 전체 Tier1/2/3 에 영향; 모든 기존 시너지 기획서의 수치 근거가 이 임계값 가정에서 파생 |
| 데이터근거 | **4** | 픽 수 변화 수치 명확; QA BLOCKED 증거 보유; spec 의 QA 권고 문서화됨. 실제 시뮬 데이터 없음(−1) |
| **종합** | **18** | 검증가치 5 + (6−2) + 5 + 4 |

---

## 4. 제안 내용

### 4.1 BalanceConfig 필드 추가

```csharp
//# BalanceConfig.cs 에 추가
[SerializeField] private int _synergyTier1Threshold = 3;
[SerializeField] private int _synergyTier2Threshold = 5;
[SerializeField] private int _synergyTier3Threshold = 7;

public int SynergyTier1Threshold => _synergyTier1Threshold;
public int SynergyTier2Threshold => _synergyTier2Threshold;
public int SynergyTier3Threshold => _synergyTier3Threshold;
```

### 4.2 BuildSynergyService 수정

```csharp
//# BuildSynergyService.cs — 하드코딩 상수 제거, BalanceConfig 참조
//# (X) 기존
private const int Tier1 = 3;
private const int Tier2 = 5;
private const int Tier3 = 7;

//# (O) 수정 후
private readonly BalanceConfig _config;
//# 생성자 또는 Inject 로 주입

int tier1 = _config.SynergyTier1Threshold;
int tier2 = _config.SynergyTier2Threshold;
int tier3 = _config.SynergyTier3Threshold;
```

### 4.3 BalanceConfig.asset 초기값

현행 기획 기준값인 3/5/7 을 유지해 동작 변화 없이 전환한다. 이후 game-designer 가 밸런스 조정 사이클에서 수치를 조정할 수 있다.

### 4.4 손잡이 노출 후 조정 방향 가설 (game-designer 검토 필요)

| 시나리오 | Tier1 | Tier2 | Tier3 | 예상 효과 |
|---|---|---|---|---|
| 현행 유지 | 3 | 5 | 7 | 14픽 최대판에서 Tier3 50% 집중 필요 |
| Tier3 완화 | 3 | 5 | **6** | 14픽 최대판 42.9% 집중으로 Tier3 |
| Tier2 완화 | 3 | **4** | 7 | 중간 단계 진입 완화; 빌드 다양성↑ |
| 전체 완화 | **2** | **4** | **6** | 단기 게임에서도 Tier2/3 체험 가능 |

> 비고: 3픽 캡(`docs/design/card-3pick-cap.md`) 과 연동 필수 — 단일 카드 최대 3픽이므로 Tier3=7 도달에는 최소 3종 카드가 필요. 임계 완화 시 2종 집중 빌드도 Tier2/3 진입 가능해짐.

---

## 5. 구현 의존 관계

```
BalanceConfig.cs (필드 추가)
  └─ BalanceConfig.asset (초기값 3/5/7 설정)
       └─ BuildSynergyService.cs (하드코딩 제거 → BalanceConfig 참조)
            └─ BuildSynergyServiceTests.cs (임계값 파라미터화 테스트 추가)
```

- `BuildSynergyService` 가 `BalanceConfig` 를 생성자 주입으로 받아야 테스트 가능성 유지 (Rule 02 §5)
- `BalanceConfig.asset` 의 `_synergyTier1Threshold` 등은 인스펙터에서 직접 수정 가능해야 함 (Rule 04 §2 — Data/ 폴더 비-Addressable 에셋으로 유지)

---

## 6. 비개발자 요약

### 지금 무슨 일이 일어나고 있나요?

게임에서 카드를 고를 때마다 점점 강해지는 시스템을 **"시너지"** 라고 부릅니다.
같은 종류의 카드를 3장·5장·7장씩 모으면 단계별로 큰 보너스(Tier1·Tier2·Tier3)가 붙는데,
지금은 이 숫자(3·5·7)가 **코드 안에 고정**되어 있어 기획자가 쉽게 바꿀 수 없습니다.

### 왜 지금 문제가 됐나요?

2026-06-03에 카드를 고르는 기회가 **9번 → 5번으로 줄었습니다.**
이전에는 한 판에 최대 18번 카드를 골랐기 때문에 특정 시너지만 집중해도 여유가 있었지만,
지금은 14번밖에 없어서 **가장 강한 Tier3 시너지를 만들려면 뽑는 카드의 절반을 같은 종류로만 채워야** 합니다.
평균 게임 길이 기준으로는 **70%를 한 종류로만** 골라야 하는 상황이 될 수 있습니다.

### 무엇이 필요한가요?

숫자(3·5·7)를 **설정 파일(`BalanceConfig.asset`)에 옮겨** 기획자가 Unity 에디터에서 바로 조절할 수 있게 만들어야 합니다.
코드를 수정하지 않고도 "Tier3 기준을 7에서 6으로 낮춰보자"처럼 실험할 수 있습니다.

### 플레이어는 어떤 차이를 느끼게 되나요?

설정 파일로 옮긴 후 기획자가 수치를 조정하면:
- 지금보다 **다양한 조합으로 Tier3 보너스**를 얻을 수 있게 되거나
- 반대로 집중도 요구를 올려 **전략적 선택의 무게**를 높일 수도 있습니다.
손잡이가 생기기 전까지는 "바꾸고 싶어도 바꿀 수 없는" 상태입니다.

### 유저 흐름 (9단계)

1. 게임 시작 — 영웅이 자동으로 던전 돌파를 시작한다
2. 처음 30초 후 **첫 번째 카드 픽** 선택지 3장이 나온다
3. 이후 90초·150초·210초·270초 — 총 5회 액티브 픽 기회
4. 영웅 HP가 내려갈 때마다 패시브 픽 기회 9회 추가 (HP 90%·80%·70%·…·10%)
5. 한 판 최대 14회 카드 선택 (액티브 5 + 패시브 9)
6. 같은 축 카드 3장이 쌓이면 **Tier1 시너지 보너스** 발동
7. 5장이면 **Tier2**, 7장이면 **Tier3** — 단, 7장은 최대 14픽의 절반을 한 축에 써야 함
8. 카드별 3픽 캡(같은 카드는 최대 3번만 선택 가능)으로 인해 7장 채우려면 최소 3종 카드 필요
9. 5분 안에 영웅 HP 0으로 만들면 승리 — Tier 분포가 핵심 승패 변수

---

## 7. 다음 단계 권고

| 순서 | 담당 | 작업 |
|---|---|---|
| 1 | gameplay-programmer | `BalanceConfig.cs` 에 `SynergyTier1/2/3Threshold` 필드·프로퍼티 추가 |
| 2 | gameplay-programmer | `BalanceConfig.asset` 초기값 3·5·7 설정 |
| 3 | gameplay-programmer | `BuildSynergyService.cs` 하드코딩 상수 제거, BalanceConfig 주입으로 교체 |
| 4 | test-engineer | `BuildSynergyServiceTests` 에 임계값 파라미터화 테스트 추가 |
| 5 | gameplay-programmer | `CardSelectionPopup.DebugAutoPicker` 훅 구현 (QA BLOCKED 해소) |
| 6 | qa-simulator | `2026-06-03 spec §4` 검증 — Tier 분포 N판 시뮬 + 임계값 조정 실험 |
| 7 | game-designer | 시뮬 결과를 바탕으로 Tier 임계값 조정안 작성 |
