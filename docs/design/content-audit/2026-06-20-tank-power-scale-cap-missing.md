# Content Audit — 2026-06-20 — ReplaceWispsToWraith 3픽 × Tank Tier2 — Wisp·Wraith Power 상한 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (2026-06-10 갱신)
- 참조 spec/plan 수: 30개 (specs 30 · plans 30)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED 상태)
- 과거 감사 이력 (git log): 12건 (가장 최근: 2026-06-18)

---

## 1. 현황

### 카테고리별 컨셉 vs 실제

| 카테고리 | 컨셉 (§11.3) | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (기사) | Knight.prefab + LittleGhost 시리즈 (스킬 도입, 06-04) | 일치 (스킬 추가됨) |
| 몬스터 | 6종 | Wisp/Wraith/Reaper/Hex/Plague/Phantom 6종 프리팹 ✓ | 없음 |
| 패시브 카드 | 16장 | Art/Cards/Items/ 에 16장 .asset ✓ | 없음 |
| 액티브 카드 | 12장 | Art/Cards/Items/ 에 12장 .asset ✓ | 없음 |
| 카드 효과 클래스 | 28개 | Scripts/Card/Effects/ 에 28개 .cs ✓ | 없음 |

### 계획 있으나 미구현

- **Multiply → SwarmRush 교체** (`card-renewal.md §3.4`): 원안에서 Multiply 폐기 + SwarmRush(Phantom 6마리 즉시 소환) 신설 계획이었으나, 현행 `Multiply.asset`("빠른 번식", `FastBreedingEffect` ×0.6 팬텀 스포너 주기) 잔존. SwarmRush 미구현.
- **QA 자동화 훅** (`qa-reports/2026-05-22.md §3`): `BattleController.DebugAutoPicker` 델리게이트 미구현으로 헤드리스 시뮬레이션 차단 지속.

### QA 권고 미해결

- 2026-05-22 리포트: BLOCKED — 카드 자동 픽 API 부재. `BattleController.DebugAutoPicker` 10줄 구현 요청 미처리. QA 캠페인 전체 대기 중.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 (UTC) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — BalanceConfig MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-15 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 (영웅 HP 4000 기준 3픽 합산 1.875% — 스킬 도입 후 격차 확대) |
| 2026-06-14 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — 종별 HP 불균형 + BalanceConfig 손잡이 이관 제안 |
| 2026-06-13 | c07cc2c | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 (메타 상점 복합 위험 시나리오 연계) |
| 2026-06-12 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |
| 2026-06-08 | 2002c8b | Debuff 액티브 출혈 카드 비율 재조정 제안 (Bleed 2%→1%/s) |
| 2026-06-07 | 307ec17 | Dps 축 ReaperAtkSpeed 배율 재조정 제안 (Cooldown ×0.7→×0.75, Tier2 중첩 하한 없음 해소) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### ReplaceWispsToWraith 3픽 × Tank Tier2 — Wisp·Wraith Power 상한 미설계

- **카테고리**: Tank 패시브 · BalanceConfig 손잡이 누락
- **요지**: Tank 패시브 카드 `ReplaceWispsToWraith`("공포의 군세")를 3픽하면 Wisp·Wraith 공격력(Power)이 ×2.197로 상승한다. 여기에 Tank Tier2(5장 임계)가 동시 발동하면 ×1.2가 추가로 곱산돼 합계 ×2.636이 된다. BalanceConfig에 Wisp·Wraith Power 배율 상한 손잡이가 없어 이 조합이 의도보다 큰 DPS를 낼 경우 런타임 튜닝 경로가 없다.
- **검증/구현/시너지/데이터**: 5 / 2 / 4 / 4 → 종합 **17**
- **근거**:
  - `docs/design/card-renewal.md §3.1` — `ReplaceWispsToWraith`: `_powerMul=1.3`, 곱연산 누적, 멱등 아님. 3픽 상한 ×1.3³ = ×2.197
  - `docs/design/card-renewal.md §4.2` — Tank Tier2: "Wisp·Wraith Power ×1.2 (글로벌 영구)"
  - `docs/design/card-renewal.md §7.1` — 전역 3픽 캡: "3픽 값이 모든 카드의 실효 상한"
  - `docs/design/project_lair_concept.md §11.3` — Wisp DPS 10, Wraith DPS 20 (베이스)
  - `docs/design/project_lair_concept.md §8` — 밸런싱 기준: 영웅 2~4분 사망
  - `docs/design/card-renewal.md §4.3` — "Hero HP 4600 / 평균 사망 76s 베이스라인"
- **MVP 범위**: 컨셉 §11.2 항목 표 ✓ — 패시브 카드 효과값/배율 재조정 허용 범위. 신규 종·카드 없음.

#### 수치 검산

| 항목 | 베이스 | 3픽 후 | Tier2 추가 | 최종 (3픽 + Tier2) |
|---|---|---|---|---|
| ReplaceWispsToWraith 배율 | ×1.0 | ×2.197 | ×2.636 | ×2.636 |
| Wisp DPS | 10 | 22.0 | 26.4 | **26.4** |
| Wraith DPS | 20 | 43.9 | 52.7 | **52.7** |
| Wisp 6마리 기여 (필드 캡 근사) | 60 DPS | 132 DPS | 158 DPS | **158 DPS** |
| Wraith 3마리 기여 | 60 DPS | 132 DPS | 158 DPS | **158 DPS** |
| Tank 몬스터 합계 DPS | **120 DPS** | 264 DPS | 316 DPS | **316 DPS** |

> Wisp·Wraith 수량 근사: 필드 캡 18에서 Reaper/Phantom/Plague/Hex 스포너가 동시 출력 중이므로 Wisp 6마리 + Wraith 3마리는 보수적 추정치. 실제 비율은 스포너 주기에 의존(Wisp 9s, Wraith 20s).
> 베이스 DPS 120 → 3픽+Tier2 316으로 **2.64배 증가**. Reaper/Hex/Plague/Phantom DPS는 별도 추가됨.

BalanceConfig 현황:
- `MonsterStatRow`: Key / CharacterStat(Hp·Power·Range·Cooldown·MoveSpeed) / SpawnPeriod 필드 존재
- **Power 배율 상한 손잡이**: 없음 (`MaxWispPowerScale`, `MaxWraithPowerScale`, `MaxTankPowerScale` 미존재)
- 현행 조정 경로: `ReplaceWispsToWraith` SO의 `_powerMul` 값(1.3)을 직접 수정하는 방법뿐 — 1픽 단위 수치 변경으로만 대응 가능, 3픽 시의 누적 상한 자체를 설정할 손잡이 없음

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**  
   패시브 선택지(영웅 HP 10%마다, 최대 9번) 팝업에서 `ReplaceWispsToWraith`("공포의 군세")가 3택 중 하나로 제시될 때 발동한다. 첫 픽(×1.3), 2픽(×1.69), 3픽(×2.197) 각각 즉시 Wisp·Wraith 전체 Power에 글로벌 적용된다. Tank Tier2 임계(5장)에 도달하는 순간 ×1.2가 별도 발화해 최종 ×2.636이 확정된다.

2. **화면 변화**  
   카드 픽 직후 전투가 재개되며, 필드의 Wisp·Wraith 타격 수치가 즉시 ×1.3 증가한 DPS로 영웅 HP 바를 더 빠르게 깎는다. BattleHud 좌측 시너지 패널에서 TANK 카운트가 1 증가해 다음 임계(Tier1=3장, Tier2=5장)를 향한 진행 상황을 표시한다. 카드 팝업에서 ReplaceWispsToWraith를 2번 이상 픽한 경우 카드 우측 상단에 `×K` 배지가 표시된다.

3. **입력 행동**  
   플레이어는 3택 중 "공포의 군세" 카드를 클릭(CHButton)한다. 1픽·2픽·3픽 각각 동일한 카드 외형이지만 `×2`, `×3` 배지로 누적 상태를 식별할 수 있다. 전역 3픽 캡 도달 후에는 이 카드가 후보 풀에서 제외되어 4번 이상 제시되지 않는다.

4. **시스템 반응**  
   `WispWraithPowerBoostEffect.Apply()` → `IBattleContext.RegisterMonsterTypeBuff(Wisp, Power, 1.3)` + `RegisterMonsterTypeBuff(Wraith, Power, 1.3)` 2회 호출. 기존 dict 값에 ×1.3 곱산해 누적. 3픽 후 dict에는 Wisp Power ×2.197, Wraith Power ×2.197이 등록된다. Tank Tier2 발화(`BuildSynergyService.TankSynergyTier2`) 시 동일 표면으로 ×1.2 추가 누적 → ×2.636 확정. 이후 스폰되는 Wisp·Wraith 인스턴스에 소급 적용된다(`RegisterMonsterTypeBuff`는 현재 필드 + 이후 스폰 모두 적용).

5. **반복·재발생 패턴**  
   ReplaceWispsToWraith는 전역 3픽 캡 이후 후보 제외. 그러나 Tank Tier1(3장, HP ×1.3)과 Tank Tier2(5장, Power ×1.2)는 동 축 다른 카드(WispHpBoost, WraithDamageBoost, SpawnWraith 등)로도 누적 카운트를 올릴 수 있어, ReplaceWispsToWraith 3픽 전에 Tier2가 먼저 발화할 수도 있다. 어느 순서로 발화하든 합산 Power ×2.636에는 변화 없음(곱산 순서 무관).

6. **종료·해소 조건**  
   Power 보정은 라운드 내 영구 유지되며 해제 조건이 없다. 영웅 사망(승리) 또는 5분 타임오버(패배) 시 라운드가 종료되어 다음 런에서 초기화된다. 메타 상점 영구 업그레이드에 Power 관련 항목이 있다면 다음 런에도 기저값이 달라질 수 있으나, 현재 BalanceConfig에 Power 기저 손잡이가 없으므로 v0.3 기준 영향 없음.

7. **다른 시스템과 상호작용**  
   - **WispHpBoost 3픽 동반** (HP ×2.197 추가): Wisp는 HP도 높고 DPS도 강한 "고내구 고딜" 유닛으로 변모 → Tank 빌드의 Dps 역할 침범 가능성. Tank의 빌드 정체성인 "묶어두기"보다 "빠르게 깎기"로 성격이 이동.
   - **BloodThirst(Dps A, 처치 시 +30 HP 회복, 30s)**: Wisp·Wraith DPS ×2.636 상태에서 처치 속도가 빨라지면 BloodThirst 발동 빈도 증가 → 몬스터 필드 생존율 추가 상승.
   - **Tank Tier1 HP ×1.3 동반**: Wisp HP 200×1.3=260, Wraith HP 500×1.3=650 → 영웅이 Wisp 1마리 제거에 260/50=5.2타, Wraith 1마리에 13타 필요 → 처치 속도 저하, 더 긴 시간 DPS 적용.
   - **Dps 축 Frenzy(전체 공속 +50%, 10s)**: Wisp·Wraith도 Frenzy 대상 → ×2.636 Power 상태에서 공속까지 50% 상승 시 DPS 추가 1.5배 (최대 DPS ×3.954).

8. **엣지 케이스**  
   ReplaceWispsToWraith 3픽 + Tank Tier2 + Frenzy 동시 활성 시, Wisp DPS = 10 × 2.636 × 1.5 = 39.5, Wraith DPS = 20 × 2.636 × 1.5 = 79.1. 필드 Wisp 6 + Wraith 3 → 6×39.5 + 3×79.1 = 237 + 237 = 474 DPS (Tank 몬스터만). 여기에 Reaper·Hex·Plague·Phantom DPS를 합산하면 영웅 HP 4600 기준 평균 사망 76s 베이스라인이 크게 앞당겨질 가능성이 있다. BalanceConfig에 Power 배율 상한이 없어 이 조합에서 사망 시간을 조정할 손잡이가 없음.

9. **유저 정보·피드백**  
   빌드 카운트 바(BattleHud 좌측)에서 TANK 축 진행도(현재 장수/다음 임계)가 표시되어 Tier2 근접 여부를 직관적으로 알 수 있다. 그러나 현재 게임 UI 어디에도 "Wisp·Wraith Power가 현재 몇 배인지"를 수치로 표시하는 창이 없다. 3픽 후 Power ×2.197이 되었는지 플레이어가 확인할 방법이 없어, Tier2 발화 토스트(축 시너지 Tier 발동 문구) 외에는 배율 상승 피드백이 없다.

### 보류

- **Debuff Tier3 EternalBleedAura + BleedEffect 중첩 합산**: `EternalBleedAura(ratio=0.01)` 구현 확인 (`Scripts/Card/Auras/EternalBleedAura.cs`, `DebuffSynergyTier3.cs`). Bleed 카드(ratio=0.02, 10s) 중첩 시 이동당 3%/s 합산 가능. 검증가치 높으나 Debuff 카테고리가 직전 7일 이내 2회(06-12·06-15) 등장해 보류.
- **SpawnWraith 3픽 (+3 출력) + Wraith 주기 20s 병목**: Wraith 동시 4마리/20s 공급 vs 필드 캡 18 병목. Tank Tier3(캡 +6)와 교차 시너지 가능. Tank 계열이나 데이터 근거 부족(QA 미실행)으로 보류.
- **Multiply(FastBreedingEffect, ×0.6 팬텀 주기) + SpawnerHaste(×0.8 전체 주기) 복합**: Phantom 스포너 0.6×0.8=0.48배 주기 (6s→2.88s). Swarm 카테고리가 직전 7일 3회(06-13·06-16·06-17) 등장해 보류.

---

## 3. 과거 감사 대비 차별성

git log 조회 12건 검토 완료. 가장 유사했던 과거 커밋:
- **abe2ecd (2026-06-10)** "Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안"
  - 차별점: 06-10 감사는 Tank **액티브** 카드(IronWill·ToughHide·GuardianRage)의 **받는 데미지** 누적 하한을 다룸. 오늘 제안은 Tank **패시브** 카드(`ReplaceWispsToWraith`)의 **공격력 배율** 상한을 다루며, Tank 빌드가 딜 역할을 침범하는 정체성 위기까지 포함 — 대상(공격 vs 방어), 카드(패시브 vs 액티브), 이슈 방향(상한 vs 하한) 모두 다름.
- **3a9bed3 (2026-06-17)** "Tank·Swarm 교차 픽 딜레마 설계 검증 제안"
  - 차별점: 06-17 감사는 SpawnWisps(Swarm 패시브)의 축 귀속 딜레마를 다룸. 오늘 제안은 ReplaceWispsToWraith(Tank 패시브)의 Power 상한 미설계로 완전히 다른 카드·이슈.

---

## 4. 제외 (범위 밖)

- Power 배율 상한을 제어할 신규 시스템(예: Power Cap 시스템) 설계: BalanceConfig 손잡이 1개 추가로 충분하므로 시스템 신설 불필요. 기존 패턴(`MinHeroAttackScale` 등) 따름.
- WispWraithPowerBoostEffect 효과 클래스 삭제·대체: 현행 구현 유지 + BalanceConfig 손잡이 추가가 최소 비용.
- Wisp·Wraith 기저 DPS(컨셉 §11.3 Wisp 10 / Wraith 20) 변경: 밸런스 조정 흐름 별도 사이클 대상.
- Tank Tier2 효과량(×1.2) 자체 변경: card-renewal.md §4.2가 단일 진실, 별도 기획 필요.

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청:
  1. BalanceConfig에 `MaxTankPowerScale`(또는 `MaxWispPowerScale` + `MaxWraithPowerScale` 종별 2개) 손잡이 추가 기획
  2. 추가할 손잡이의 초기값 결정 — 현행 상한 ×2.636 기준으로 클램프 위치 설계 (예: ×2.0 상한 → 3픽+Tier2 조합에서 ×2.197→×2.0으로 제한, Frenzy 연계 시 추가 폭발 방지)
  3. 수치 결정 후 gameplay-programmer → code-reviewer → test-engineer → qa-simulator 흐름 권장

---

## 6. 쉬운 설명 (비개발자 요약)

"공포의 군세" 카드는 게임에서 우리가 풀어놓은 위스프(초록 공)와 레이스(회색 상자)의 공격력을 30% 올려주는 카드다. 이 카드를 3번 고르면 공격력이 무려 2배가 넘게 올라가고, 탱커 빌드를 5장 모으면 또 20%가 더 붙는다. 그런데 이 조합을 얼마나 강하게 만들 수 있는지 조절하는 슬라이더(손잡이)가 설정 파일에 없어서, 시험해보고 너무 강하다 싶어도 수치를 조정하기가 매우 불편한 구조다. 그래서 이번에 제안하는 것은: 공격력 배율에 상한선 손잡이를 하나 추가해서, 탱커 빌드가 딜러 역할까지 압도하지 않도록 튜닝 경로를 마련하자는 것.
