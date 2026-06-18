# Card Ideas — 2026-06-17 — 딜러 라인 완성: Reaper·Hex 생존성·돌파력 강화 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: Dps 딜러 내구도 완성 — 현재 Dps 축 7장은 공속·사거리·스포너 수량·공격력·혼합 즉시 소환(2026-06-01 기제안)에 집중되어 있으나, **Reaper(HP 100)·Hex(HP 60) 의 극단적인 낮은 HP 로 인해 영웅에게 금방 처치되는 구조적 약점**이 전혀 해소되지 않았다. 또한 Tank 축에 GuardianRage(방어적 임시 버프)가 있듯 Dps 축에도 **공격적 임시 버프** 액티브가 없다. 오늘 3장이 이 두 공백을 채운다.
- **목록**: 리퍼 방패 (Reaper Armor) / 헥스 방어막 (Hex Barrier) / Dps 과부하 (DPS Overdrive)
- **기존 28장 + git log 과거 19회차(2026-05-28 ~ 2026-06-16)와의 중복 회피 확인됨**
  - 기존 28장: Reaper HP 배율 카드 전무. Hex HP 배율 카드 전무. 공격적 Dps 임시 버프 액티브 전무.
    - ReaperAtkSpeed = Reaper 공속 ×0.7 (HP 아님). HexRangeBoost = Hex 사거리 ×1.4 (HP 아님).
    - Frenzy = 전체 공속 +50%(10s, 전체 종) — 오늘 DPS Overdrive는 Reaper+Hex 한정 공격력+속도 동시 버프.
    - GuardianRage = Wisp·Wraith 방어적 버프(HP×2 + 受damage×0.5). 오늘은 Reaper·Hex 공격적 버프.
  - 2026-06-01 리퍼·헥스 DPS 심화: Reaper 공격력 ×1.35 / Hex 공속 ×0.75 / 처형 부대 혼합 즉시 소환 → HP 배율 전혀 다름.
  - 2026-06-07 레이스·팬텀 각성: Wraith Power·MoveSpeed / Phantom OnHit → Reaper·Hex 무관.
  - 2026-06-12 팬텀·플레이그·헥스 갭 채우기: Phantom HP 배율·Plague Power·SpawnHexes(스포너 출력) → Hex HP 배율 아님(SpawnHexes 는 스포너 수량).
  - 나머지 16회차 전부: 도주 처벌·킬 카운터·Tank 재생·축 교환·전술 배치·공격 반격·와일드 — 어느 것도 Reaper/Hex HP 배율 또는 Dps 공격적 임시 버프 아님 ✅

---

## 1. 리퍼 방패 (Reaper Armor) — 가칭

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - Reaper 종 HP 글로벌 영구 ×1.5.
  - 기본 Reaper: HP 100, DPS 40, 공속 1s.
  - 이 카드 픽 후: HP 150 → 영웅(공격력 50/타 기준) 3타 필요(50+50+50=150). 기존 2타에서 +1타.
  - 필드 Reaper 평균 3마리 × 150 HP = 총 HP 450 → 영웅이 Reaper 라인을 청소하는 데 걸리는 시간 ≈ 1.5s 추가.
  - 밸런스 근거 (컨셉 §8): Wisp HP 200, WraithDamageBoost→HP ×1.5(500→750)와 비교해 Reaper ×1.5 (100→150)는 여전히 전선의 약체. 생존시간 50% 증가이지만 절대 수치는 낮아 탱커를 대체하지 않음.
  - 중첩 픽: 2픽 → HP ×1.5×1.5=×2.25 (225). 3픽 → ×3.375 (337). 영웅 6.75타 → 고강도 빌드에서 Reaper 가 실질 탱킹 가능해지는 전략 심화 옵션.
- **구현 패턴**: `ReaperArmorEffect.cs` — `WispHpBoostEffect` 구조 그대로, `EMonster.Reaper`, HP multiplier `1.5f`. `MonsterBuffService.ApplySpeciesHpMultiplier(EMonster.Reaper, 1.5f)` 1줄 호출. 신규 패턴 없음.
- **시너지 후크**:
  - **ReaperAtkSpeed** (공속 ×0.7): 오래 살면서 더 빠르게 공격 → DPS 지속 시간 ×1.5배 × 공속 강화. Reaper 라인이 비로소 진짜 딜러로.
  - **리퍼 격살 ReaperLethalStrike** (제안 2026-06-01, 공격력 ×1.35): HP 150 + 공격력 54 → 생존하면서 세게 치는 Reaper. Reaper 빌드의 핵심 3종 세트(AtkSpeed + Armor + LethalStrike) 완성.
  - **MarkOfDeath** (영웅 受damage ×1.5, 5s): Reaper 가 오래 살며 해당 5s 창 동안 지속적 DPS → 창 내 Reaper DPS = 40/타 × 1.5 × 살아있는 마릿수.
  - **BloodThirst** (처치 시 인근 HP +30): Reaper 가 빠른 처치 → 주변 몬스터 회복 트리거 → Reaper 생존율 간접 향상.
- **구현 비용 추정**: 1 (WispHpBoostEffect 구조 그대로. 종 Enum 교체 1곳만)
- **중복 재검증**: 기존 28장에 Reaper HP 배율 카드 없음. 과거 19회차: 리퍼·헥스 DPS 심화(2026-06-01)는 공격력·공속·혼합 소환 — HP 아님. 어느 회차에서도 Reaper HP 배율 제안 없음. 최초 제안. ✓

---

## 2. 헥스 방어막 (Hex Barrier) — 가칭

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - Hex 종 HP 글로벌 영구 ×1.5.
  - 기본 Hex: HP 60, DPS 30 (원거리), 공속 기준.
  - 이 카드 픽 후: HP 90 → 영웅 1~2타 범위(50+50=100>90이지만 공속 1s 간격 — 첫 타 50 남아 40 HP, 두 번째 타 전에 거리 조정 가능성).
  - Hex 는 원거리(사거리 있음) 특성 덕에 영웅 AI 가 근접 탱커를 먼저 처치하는 경향 → Hex 의 실 생존은 거리 기반. HP 90은 "뜻하지 않게 영웅이 Hex 를 직접 타격할 때" 방어막으로 의미 있음.
  - 밸런스 근거: Hex HP 60은 게임 내 최저 HP. 가장 낮은 HP → 가장 낮은 倍율(×1.5)로도 가장 극적인 절대 생존력 변화. Hex 원거리 특성과 HP 배율이 결합되면 후방 포격선이 안정화되어 DPS 지속성이 높아짐.
  - 중첩 픽: 2픽 → HP 90×1.5=135. 영웅 3타 필요 → Hex 가 원거리 + 거리 조정으로 실질 생존 역할.
- **구현 패턴**: `HexBarrierEffect.cs` — `WispHpBoostEffect` 구조, `EMonster.Hex`, HP multiplier `1.5f`. 1줄 호출.
- **시너지 후크**:
  - **HexRangeBoost** (사거리 ×1.4): 더 멀리서 더 오래 → 원거리 HP + 사거리 = 영웅이 Hex 에 다가가기 전에 장거리 포격 지속.
  - **헥스 연사 HexRapidFire** (제안 2026-06-01, 공속 ×0.75): Hex Barrier + HexBarrier + HexRapidFire = 오래 살며 더 빠르게 쏘는 원거리 포병 빌드 완성.
  - **ReplaceReapersToHex** (Reaper 스포너 → Hex 교체): Hex 수 최대화 + HexBarrier → Hex 전문 빌드.
  - **SpawnHexes** (제안 2026-06-12, Hex 스포너 출력 +1): HexBarrier + SpawnHexes = Hex 양 × 생존력 동시 강화.
  - **MarkOfDeath** + Hex: Hex 가 원거리 안전거리에서 살아남아 해당 5s 창 동안 연속 DPS.
- **구현 비용 추정**: 1 (WispHpBoostEffect 구조 그대로. 종 Enum 교체 1곳)
- **중복 재검증**: 기존 28장에 Hex HP 배율 카드 없음. 2026-06-12 SpawnHexes 는 스포너 출력 +1(수량) — HP 아님. 과거 19회차 어디에도 Hex HP 배율 없음. 최초 제안. ✓

---

## 3. Dps 과부하 (DPS Overdrive) — 가칭

- **카테고리**: 액티브 Dps 버프
- **효과 모델**:
  - 8초간 Reaper·Hex 종 전체 공격력 ×2.5 + 이동속도 ×1.5 동시 적용.
  - **Reaper 과부하 시 DPS**: 공격력 40 × 2.5 = 100/타, 공속 1s → 100 DPS/마리. 필드 Reaper 3마리 × 100 = 300 DPS. 8s × 300 = 잠재 피해 2400 (영웅 HP 1000의 240%).
  - **Hex 과부하 시 DPS**: 공격력 30 × 2.5 = 75/타. Hex 2마리 × 75 = 150 DPS. 8s × 150 = 1200.
  - 실제 피해: 영웅이 몬스터를 처치하며 감쇄. 예측 실 기여 ≈ 30~40% → 720~960 잠재 피해 중 200~400 실 기여.
  - 이동속도 ×1.5: Reaper 가 영웅에게 더 빠르게 붙으며 DPS 발생 → 특히 TimStop(영웅 5s 정지) 직전에 사용 시 이동속도 의미 없지만 TimeStop 해제 후 즉시 추격 효율 증가.
  - **GuardianRage 와 대칭 설계**: GuardianRage = Wisp·Wraith HP×2 + 受damage×0.5 (15s, 방어형). DPS Overdrive = Reaper·Hex 공격력×2.5 + 이동속도×1.5 (8s, 공격형). 지속시간이 짧은 대신 강도는 훨씬 강함 — Tank 는 버티고, Dps 는 터뜨리는 철학.
  - 밸런스 근거 (§8 2~4분 밴드): 8초 창이 짧아 영웅이 반격으로 Reaper 를 빠르게 처치하면 효과 약화. 1회 발동으로 영웅을 킬할 수 없게 설계 — 다른 카드와 콤보 시 위협 수준. 중첩 픽 가능(2픽 = 2회 독립 발동, 타이머 합산 or 순차).
- **구현 패턴**: `DpsOverdriveEffect.cs` — `GuardianRageEffect` 구조 참조. `MonsterBuffService.ApplyMultiSpeciesBuff([EMonster.Reaper, EMonster.Hex], powerMul: 2.5f, speedMul: 1.5f, duration: 8f)`. 타이머 만료 후 원래 값 복원. GuardianRage 가 Wisp+Wraith 한 쌍을 처리한 것처럼 Reaper+Hex 한 쌍 처리.
- **시너지 후크**:
  - **MarkOfDeath** (영웅 受damage ×1.5, 5s) + DPS Overdrive: 5s 중첩 창에서 Reaper 100 × 1.5 = 150 DPS/마리 × 3마리 = 450 DPS. 타이밍 코어 콤보.
  - **리퍼 격살 ReaperLethalStrike** (제안 2026-06-01, 공격력 ×1.35) + DPS Overdrive: 40 × 1.35 × 2.5 = 135 DPS/마리. 배율 중첩 (곱연산).
  - **ReaperArmor** (Reaper HP ×1.5): 오래 살며 8s 내내 과부하 DPS 유지.
  - **Frenzy** (전체 공속 +50%, 10s) + DPS Overdrive: 공속 증가 × 공격력 증가 → Reaper 150 DPS (100 DPS × 1.5 공속). 단 Frenzy 공속 효과 적용 여부 = 쿨다운 ×0.67. Reaper 과부하 DPS ≈ 100/0.67 ≈ 149 DPS/마리.
  - **처형 부대 ExecutionSquad** (제안 2026-06-01, Reaper 3 + Hex 2 즉시 소환): 소환 즉시 DPS Overdrive → 5마리 전부 과부하. 최고 파급 콤보.
- **구현 비용 추정**: 2 (GuardianRageEffect 구조 재사용. 종 배열 [Reaper, Hex] 로 확장 + duration 파라미터만 조정)
- **중복 재검증**:
  - Frenzy (기존): 전체 종 공속 +50%, 10s → DPS Overdrive 는 Reaper·Hex 한정 공격력×2.5+속도, 8s. 대상 종·효과 유형·강도 모두 다름. ✓
  - GuardianRage (기존): Wisp·Wraith 방어 버프 → DPS Overdrive 는 Reaper·Hex 공격 버프. 대칭이지 중복 아님. ✓
  - 과거 19회차 어느 파일에서도 "Reaper·Hex 종 한정 공격적 임시 버프" 없음. ✓

---

## 4. 공통 테마 고찰

세 카드는 **"Dps 딜러 라인의 구조적 약점 해소"** 라는 하나의 필요에서 출발한다.

현재 Dps 축 빌드의 문제:
- 공격력·공속이 강화되어도 Reaper(HP 100)/Hex(HP 60)가 영웅에게 금방 처치 → 강화 효과를 지속할 수 없음
- 보강 이전 시뮬: Reaper 공속 ×0.7 + 공격력 ×1.35 적용 Reaper = 77 DPS. 그러나 영웅 1타(50)에 2타 맞으면 사망 → 실제로 2타 이상 공격하기 어려운 구조.

| 카드 | 해결하는 약점 | 축 역할 |
|---|---|---|
| Reaper Armor | Reaper 순식간에 처치당하는 문제 | 딜러 생존 |
| Hex Barrier | Hex 극저 HP 로 인한 DPS 미발현 | 원거리 포병 지속 |
| DPS Overdrive | Dps 축 공격적 임시 버프 부재 | 폭발 순간 딜 |

**왜 오늘 이 테마를 골랐는가:**
- QA 리포트가 BLOCKED 상태이므로 데이터 대신 구조 분석 근거.
- 모든 6종 몬스터 중 Reaper(HP 100)·Hex(HP 60)가 가장 낮은 HP. Wisp(200)·Wraith(500) 대비 극단적 격차.
- Dps 축 7장 모두가 강화를 "주는" 카드이지만, 그 강화를 "지속할 수 있게" 하는 카드는 없음.
- Tank 축: GuardianRage(임시 방어 버프 있음). Dps 축: 공격적 임시 버프 없음 → 대칭 설계로 채울 최적 타이밍.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- **채택 우선순위**: ReaperArmor ≥ HexBarrier > DPS Overdrive
  - ReaperArmor / HexBarrier: 구현 비용 1. WispHpBoostEffect 구조 그대로. 즉시 채택 가능.
  - DPS Overdrive: GuardianRageEffect 구조 재사용. 종 배열 확장 필요 — 구현 비용 2.
- ECardId 후보: `ReaperArmor`, `HexBarrier`, `DpsOverdrive`.
- Dps 축 7장에 추가하면 총 10장 → v0.2 Dps 풀 확장 달성에 기여.
- v0.2 풀 확장 진입 전까지 backlog 보관.
- **선결 확인 사항 (gameplay-programmer 와)**: `MonsterBuffService.ApplyMultiSpeciesBuff(species[])` 형태의 다종 배열 API 가 기존 `GuardianRageEffect` 에서 이미 Wisp+Wraith 를 처리하는 방식 확인 후 DPS Overdrive 구현 착수 권장.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 리퍼와 헥스는 제일 강한 딜러인데, 너무 쉽게 죽는다는 큰 약점이 있습니다. 예를 들어 영웅이 리퍼를 두 번만 때리면 죽어버리고, 헥스는 한 번만 맞아도 쓰러질 지경입니다. 이미 리퍼를 더 빠르게, 더 아프게 만드는 카드는 있는데, 정작 그 강해진 능력을 써먹기도 전에 죽어버리는 게 문제입니다. 그리고 탱커(위스프·레이스)에게는 잠시 엄청 강해지는 "수호자의 분노" 카드가 있는데, 딜러(리퍼·헥스)에게는 그런 폭발력 카드가 한 장도 없습니다. 그래서 오늘 제안하는 카드 3장은: 리퍼가 좀 더 맞아도 버티도록 HP 를 늘려주는 카드, 헥스가 원거리 포격을 더 오래 유지하도록 HP 를 늘려주는 카드, 그리고 리퍼와 헥스 전부를 8초 동안 공격력 2.5배·이동속도 1.5배로 폭발시키는 카드입니다.
