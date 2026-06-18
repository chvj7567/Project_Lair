# [Content Audit] Dps 액티브 Frenzy 공속 배율·MarkOfDeath 피해 배율 하드코딩 — BalanceConfig 손잡이 미설계

- **날짜**: 2026-06-19
- **감사 회차**: git log 기준 12회차
- **선정 후보**: Frenzy × MarkOfDeath 동시 활성 창 — 2.24× DPS 압박, 수치 BalanceConfig 미등록

---

## §0 현황 스냅샷

| 카테고리 | 기획 목표 | 실제 구현 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab ✅) | 0 |
| 몬스터 | 6 | 6 (Wisp/Wraith/Reaper/Hex/Plague/Phantom ✅) | 0 |
| 패시브 카드 | 16 | 16 assets ✅ | 0 |
| 액티브 카드 | 12 | 12 assets ✅ | 0 |
| 영웅 스킬 | 3 (Phase 1/2/3) | 3 ✅ (P1 Dash HP85%, P2 Nova HP65%, P3 Orbit HP45%) | 0 |

**전체 콘텐츠 갭**: 0 — 에셋 레벨 미구현 없음.

### §0.1 미구현/미해결 누적 항목

| 항목 | 출처 | 상태 |
|---|---|---|
| SwarmRush 액티브 카드 | card-renewal.md §3.4 | 기획 확정, 구현 미착수. Multiply.asset("빠른 번식") 현재 잔존 |
| DebugAutoPicker 훅 | qa-reports/2026-05-22.md | BattleController에 퍼블릭 픽 API 없어 QA 시뮬레이터 완전 차단 |

---

## §1 과거 감사 이력 (git log 기반 중복 회피)

git log `--grep="# \[Routines\]\[Daily Content Audit\]"` 기준 11개 엔트리:

| 날짜 | 주제 요약 | 축 |
|---|---|---|
| 2026-06-07 | Reaper 공격속도 카드 계수 | Dps |
| 2026-06-09 | Hex 사거리 부스트 | Dps |
| 2026-06-11 | Swarm 글로벌 캡 압박 | Swarm |
| 2026-06-12 | Phantom 스폰 주기 조정 | Swarm |
| 2026-06-13 | Swarm Tier2 복합 스폰 | Swarm |
| 2026-06-15 | HeroPoisonAura Debuff 패시브 복합 | Debuff |
| 2026-06-16 | Phantom+Wisp 합산 캡 돌파 | Swarm |
| 2026-06-17 | Swarm Tier3 스폰 주기 복합 | Swarm |
| 2026-06-18 | Swarm 패시브 SpawnWisps 축 긴장 | Swarm |

Dps 축 최근 감사: 2026-06-09 (10일 경과) → **완전 클리어**.  
Frenzy / MarkOfDeath 단독 또는 조합 감사: **이력 없음**.

---

## §2 선정 후보

### 후보: Frenzy × MarkOfDeath 동시 활성 창 (Dps 액티브 복합)

**ECardId**: `Frenzy` (광폭화) · `MarkOfDeath` (죽음의 표식)  
**축**: Dps — 액티브 카드 2종  
**한 줄 요약**: Dps 액티브 두 장이 동시 발동될 때 2.24× net DPS가 발생하지만, 두 배율 수치 모두 BalanceConfig에 등록되지 않아 코드 수정 없이 조정 불가.

### 점수

| 축 | 점수 | 근거 |
|---|---|---|
| 검증가치 | 4 | 검증 데이터 없이 2.24× 복합이 실제 전투에서 매 활성 창마다 반복되는지 불명. 강도 과도·부족 모두 가능 |
| 구현비용 | 2 | BalanceConfig에 두 필드 추가 + MonsterBuffService/MarkOfDeathEffect SO 연결. 로직 변경 최소 |
| 시너지폭 | 4 | Dps Tier1(Power ×1.3) + Tier2(Cooldown ×0.8) 적층 시 Reaper 기준 29.25 DPS/s (기저 12 대비 ×2.44). Hex·Reaper 혼용 빌드에서 광범위 영향 |
| 데이터근거 | 4 | MonsterBuffService.cs 0.67f / MarkOfDeathEffect SO 1.5 수치 문서화 확인, Dps Tier 계수 card-renewal.md §4.2 참조 — 계산 근거 명확 |

**총점**: 4 + (6 − 2) + 4 + 4 = **16**

---

## §3 분석

### 3.1 Frenzy (광폭화) 메커니즘

- **효과**: 전체 몬스터 공격 쿨다운 `CooldownScale *= 0.67f` → 실질 공속 ÷0.67 ≈ **+49% 공속**
- **지속**: 10s, 재선택 시 dedup(중복 인스턴스 없이 잔여+10s 연장)
- **3픽 최대**: 누적 최대 30s 창
- **코드 위치**: `MonsterBuffService.cs` — `AddBuff(EMonsterBuff.Frenzy)` 분기 내 `0.67f` **리터럴 하드코딩**
- **BalanceConfig 등록 여부**: ❌ 없음

```
//# MonsterBuffService.cs (발췌 의사코드)
case EMonsterBuff.Frenzy:
    stat.CooldownScale *= 0.67f;   //# BalanceConfig 참조 없이 리터럴
    break;
```

### 3.2 MarkOfDeath (죽음의 표식) 메커니즘

- **효과**: 영웅이 받는 피해 `_dmgTakenMul = 1.5` (× 배율)
- **지속**: 5s, **duration-stacking**(재선택마다 잔여+5s 누적 — Frenzy dedup과 다름)
- **3픽 최대**: 잔여 누적 최대 15s 창
- **코드 위치**: `MarkOfDeathEffect.cs` 또는 관련 SO 필드 `_dmgTakenMul=1.5`
- **BalanceConfig 등록 여부**: ❌ 없음

### 3.3 동시 활성 창 합산

두 카드가 겹치는 구간에서 net effective DPS 승수:

```
net_multiplier = (1 / CooldownScale) × dmgTakenMul
               = (1 / 0.67) × 1.5
               ≈ 1.493 × 1.5
               = 2.24×
```

| 상태 | Reaper DPS/s (기저 12) |
|---|---|
| 기저 | 12.0 |
| Frenzy 단독 (×1/0.67) | 17.9 |
| MarkOfDeath 단독 (×1.5) | 18.0 |
| **Frenzy + MarkOfDeath 동시** | **26.9** |

### 3.4 Dps Tier 적층 시나리오

Dps 7카드(Tier3) 달성 시 추가 배율 — card-renewal.md §4.2:

- **Tier1** (3카드): Power ×1.3
- **Tier2** (5카드): Cooldown ×0.8 (추가 공속 효과)

Tier2까지 달성한 Reaper의 동시 활성 창 DPS:

```
12 × 1.3 × (1/0.8) × (1/0.67) × 1.5
= 12 × 1.3 × 1.25 × 1.493 × 1.5
≈ 43.8 DPS/s per Reaper
```

Hex 혼합 빌드(Hex 공격력 부스트 적층)까지 고려하면 단일 리퍼만으로도 5분 내 사망 가속 압력이 상당.

### 3.5 설계 위험 — BalanceConfig 손잡이 미설계

두 수치 모두 런타임 조정 경로가 없어 **빠른 밸런싱 이터레이션 불가**:

| 수치 | 현재 위치 | 조정 방법 |
|---|---|---|
| Frenzy CooldownScale (0.67f) | MonsterBuffService.cs 리터럴 | 코드 수정 + 빌드 |
| MarkOfDeath dmgTakenMul (1.5f) | SO 필드 (BalanceConfig 미연결) | SO Inspector 수정 (BalanceConfig 없이 분산) |

QA 시뮬레이터 자체가 현재 차단(DebugAutoPicker 미구현)된 상태이므로, 동시 발동 창 빈도·지속 비율을 실 데이터로 검증한 적이 없다.

---

## §4 구현 제안

### 4.1 BalanceConfig에 두 필드 추가

```csharp
//# BalanceConfig.cs 또는 관련 SO
[Header("Dps 액티브 배율")]
public float FrenzyCooldownScale = 0.67f;      //# 공속 배율 (낮을수록 빠름)
public float MarkOfDeathDamageMul = 1.5f;      //# 영웅 피해 배율
```

### 4.2 MonsterBuffService.cs 수정

```csharp
//# (Before) 리터럴 하드코딩
stat.CooldownScale *= 0.67f;

//# (After) BalanceConfig 참조
stat.CooldownScale *= BalanceConfig.Instance.FrenzyCooldownScale;
```

### 4.3 MarkOfDeathEffect 수정

MarkOfDeath SO의 `_dmgTakenMul` 필드를 BalanceConfig 참조로 교체하거나, BalanceConfig에서 읽어 SO를 override하는 패턴 적용.

### 4.4 구현 비용 요약

| 작업 | 예상 복잡도 |
|---|---|
| BalanceConfig 필드 2개 추가 | 매우 낮음 |
| MonsterBuffService.cs 리터럴 교체 | 매우 낮음 |
| MarkOfDeathEffect SO/코드 연결 | 낮음 |
| QA 시뮬레이터 복구 (선제 조건) | 높음 (별도 DebugAutoPicker 미구현) |

---

## §5 검증 기준

QA 시뮬레이터 복구 후 아래를 검증한다:

1. **동시 활성 빈도**: 5분 전투 중 Frenzy + MarkOfDeath 겹치는 구간이 총 몇 초인가
2. **기대 DPS 일치**: 겹치는 구간에서 실측 몬스터→영웅 피해가 2.24× 이내인가
3. **Tier3 빌드 생존 범위**: Dps Tier3 + 동시 창에서 영웅이 목표 생존 2~4분(§8) 내에 사망하는가
4. **Frenzy dedup 정합**: 3픽 시 최대 30s 창 연장이 실제 BuffDuration 로그로 확인되는가
5. **MarkOfDeath stacking 정합**: 3픽 시 잔여+5s 누적 최대 15s 창 실측 확인

---

## §6 관련 파일

| 파일 | 관련 섹션 |
|---|---|
| `Assets/_Lair/Scripts/Card/Effects/FrenzyEffect.cs` (또는 MonsterBuffService.cs) | CooldownScale 0.67f 리터럴 |
| `Assets/_Lair/Art/Cards/Items/Frenzy.asset` | Frenzy SO 데이터 |
| `Assets/_Lair/Art/Cards/Items/MarkOfDeath.asset` | dmgTakenMul 1.5 필드 |
| `Assets/_Lair/Data/BalanceConfig.asset` | 필드 추가 대상 |
| `Assets/_Lair/Scripts/Card/Effects/MarkOfDeathEffect.cs` | dmgTakenMul 적용 코드 |
| `docs/design/card-renewal.md` §3.2, §4.2 | Frenzy/MarkOfDeath SO 데이터, Dps Tier 계수 |
| `docs/qa-reports/2026-05-22.md` | QA 차단 상태 (DebugAutoPicker 미구현) |

---

## §7 비개발자 요약

**지금 어떤 상황인가?**

"광폭화" 카드(몬스터 공격 50% 빠르게)와 "죽음의 표식" 카드(영웅이 받는 피해 1.5배)를 둘 다 고르고 두 효과가 겹칠 때, 영웅은 평소보다 **2.2배 이상의 피해**를 받는다.

이 수치들이 얼마나 강한지 조절하는 다이얼이 설계 파일(BalanceConfig)에 없다. 즉 "너무 강하다" 싶어도 코드를 직접 고쳐야만 조정할 수 있는 상태.

**왜 지금 잡아야 하나?**

영웅 스킬(Nova, Dash 등)이 추가된 지금, 영웅이 버티는 시간에 이미 변화가 생겼다. 이 두 카드의 조합이 그 시간을 얼마나 더 단축하는지 아직 측정한 적이 없다. 밸런스 조정 주기가 빨라질수록 조정 다이얼 없이는 이터레이션이 힘들어진다.

**제안 한 줄**

BalanceConfig에 Frenzy 공속 배율·MarkOfDeath 피해 배율 두 항목을 추가해 코드 없이 인스펙터에서 조정 가능하게 만든다.

---

## §8 다음 회차 제안 (참고)

- **Tank 패시브 3픽 복합 (IronWill × WispHpBoost × WraithDamageBoost)**: tank-tier3-renewal.md 기반 Wraith ×4.10 HP 누적이 실전 생존 압박을 어느 선까지 흡수하는지 미검증
- **Hex 사거리 × MarkOfDeath 조합**: Dps 원거리 압박이 근접 스택 vs. 원거리 스택에서 어떻게 다른지
