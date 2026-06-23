# Content Audit — 2026-06-24 — Dps 패시브 ReaperAtkSpeed 3픽 + Dps Tier2 쿨다운 ×0.8 복합으로 Reaper 공격 쿨다운 0.137s 극단값 → BalanceConfig MinReaperCooldownScale 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (최종 변경 이력 2026-06-10)
- 참조 spec/plan 수: 30개 specs / 29개 plans
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED, DebugAutoPicker 훅 미구현으로 실제 시뮬 미실행)
- 과거 감사 이력 (git log): 14건 (가장 최근: 2026-06-22)

## 1. 현황

| 카테고리 | 컨셉 §11.3 기준 | 실제 (에셋/코드 확인) | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (`Knight.prefab`) | 0 |
| 몬스터 | 6종 | 6종 (`Wisp·Wraith·Reaper·Hex·Plague·Phantom.prefab`) | 0 |
| 패시브 카드 | 16장 | 16장 (`Assets/_Lair/Art/Cards/Items/` 내 16개 .asset) | 0 |
| 액티브 카드 | 12장 | 12장 (동 폴더 12개 .asset) | 0 |
| 카드 효과 클래스 | 28개 | 28개 (`Assets/_Lair/Scripts/Card/Effects/*.cs`) | 0 |

### 계획 있으나 미구현

- **SwarmRush (Phantom 6마리 즉시 소환)** — `card-renewal.md §3.4`: 원안 SwarmRush 신설 예정이나 `Multiply.asset`(`FastBreedingEffect`, 팬텀 스포너 주기 ×0.6 영구)이 잔존. "광역 압살 폐기" 의도 미실현.
- **Debuff Tier3 `EternalBleedAura` (영구 출혈 등록)** — `card-renewal.md §4.5`: `ApplyHeroAura(EternalBleedAura, -1f)` 신규 표면이 필요하다고 명시됨. 구현 여부 불확실.
- **Tank Tier3 `IncrementGlobalMonsterCap(+6)`** — `card-renewal.md §4.5`: 글로벌 캡 18→24 신규 표면 필요, 구현 여부 불확실.
- **Swarm Tier3 `IncrementAllSpawnerOutputs(+1)`** — `card-renewal.md §4.5`: 모든 스포너 동시 출력 +1 신규 표면 필요, 구현 여부 불확실.

### QA 권고 미해결

- `docs/qa-reports/2026-05-22.md` (유일한 리포트): **BLOCKED** — `BattleController.DebugAutoPicker` 훅 미구현으로 시뮬레이션 자체가 실행되지 않았음. 훅 구현 여부는 코드 확인 불가(본 루틴 범위 밖). 이후 QA 리포트 없음 — 모든 밸런스 공백은 실측 데이터 없이 수치 분석으로만 평가.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-22 | 9118936 | Swarm Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | d8fdcfe | Swarm PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-15 | 68db140 | Debuff HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-14 | 6e02b2a | Dps BloodThirst HealAmount=30 하드코딩 — 종별 HP 불균형 + BalanceConfig 손잡이 이관 |
| 2026-06-13 | c07cc2c | BalanceConfig Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 |
| 2026-06-12 | 8de2ecb | Debuff HeroAttackDown 3픽+Tier2 복합 하한 미설계 — MinHeroAttackScale 손잡이 추가 |
| 2026-06-11 | e4c765b | Swarm TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | 440794c | Dps HexRangeBoost 3픽+Tier3 중첩 ring 반경 초과 배율 재조정 제안 |
| 2026-06-08 | 2002c8b | Debuff Bleed 출혈 카드 비율 재조정 제안 (2%→1%/s) |

## 2. 추가 컨텐츠 후보 (권장 1개)

### Dps 패시브 ReaperAtkSpeed 3픽 + Dps Tier2 쿨다운 ×0.8 복합 → Reaper 공격 쿨다운 0.137s 극단값 → BalanceConfig MinReaperCooldownScale 손잡이 미설계

- **카테고리**: BalanceConfig 손잡이 추가 (Dps 패시브 × Layer1 Tier2 시너지 교차)
- **요지**: `ReaperAtkSpeed` 3픽(쿨다운 ×0.7³ = ×0.343)에 Dps Tier2 시너지(쿨다운 ×0.8)가 겹쳐 Reaper 공격 쿨다운이 Base 0.5s → 0.137s로 급락하는데, BalanceConfig에 이 값을 하한으로 클램프하는 `MinReaperCooldownScale` 손잡이가 없다.
- **검증/구현/시너지/데이터**: 5/2/4/4 → 종합 **17**
- **근거**: `card-3pick-cap.md §2.1` (ReaperAtkSpeed 3픽 천장 ×0.343 명시) + `card-renewal.md §4.2` (Dps Tier2: Reaper·Hex Cooldown ×0.8 명시) + `continuous-spawn-round.md §4` (Reaper Base Cooldown 0.5s, Power 6)
- **MVP 범위**: 컨셉 §11.3 Dps 축 패시브 `ReaperAtkSpeed` + 컨셉 §5.2 Dps Tier2 (5장 임계 쿨다운 ×0.8). BalanceConfig 손잡이 추가는 §11.2 범위 내 밸런스 도구.

#### 수치 검산

| 단계 | Reaper 쿨다운 | Reaper DPS (Power=6) | 배율 |
|---|---|---|---|
| 기본 (`continuous-spawn-round.md §4`) | 0.500s | 12.0/s | 기준 |
| `ReaperAtkSpeed` 1픽 (×0.7) | 0.350s | 17.1/s | ×1.43 |
| `ReaperAtkSpeed` 2픽 (×0.49) | 0.245s | 24.5/s | ×2.04 |
| `ReaperAtkSpeed` 3픽 천장 (×0.343) | **0.1715s** | **35.0/s** | ×2.92 |
| + Dps Tier2 (×0.8, 5장 시너지) | **0.137s** | **43.7/s** | **×3.64** |

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**: 패시브 트리거(영웅 HP 10%마다)로 카드 선택지가 뜰 때 `ReaperAtkSpeed` 카드가 3택 중 하나로 등장하면 플레이어가 픽할 수 있다. 동일 패시브 풀 16장에서 추첨되므로 한 런에 3번 모두 당첨되려면 어느 정도의 운이 필요하지만, Dps 빌드를 노리는 플레이어는 의도적으로 반복 픽을 시도할 수 있다. 3픽 캡에 도달하면 해당 카드는 이후 추첨에서 완전히 제외된다.

2. **화면 변화**: `ReaperAtkSpeed` 픽 1회마다 카드 우상단 배지에 "1/3", "2/3"이 순서대로 표시된다(`card-3pick-cap.md §3.1`). Dps 빌드 카운트 바(BattleHud 좌측)가 픽마다 올라가다가 5픽째에 Dps Tier2(쿨다운 ×0.8) 발화 시 화면 중앙 상단에 "Dps 시너지 Tier2 발동!" 토스트(1.5초)와 카운트 바 펄스(0.3초)가 표시된다(`card-renewal.md §8.4`). 필드 위 Reaper들이 이전보다 눈에 띄게 빠른 속도로 영웅을 두드리는 것이 시각적으로 느껴진다.

3. **입력 행동**: 플레이어가 `CardSelectionPopup`에서 `ReaperAtkSpeed` 카드를 최대 3회 선택하는 입력을 한다. Dps Tier2(5픽 달성)를 노린다면 `ReaperAtkSpeed` 3픽 후 다른 Dps 카드(예: `HexRangeBoost`, `SpawnReapers`, `ReplaceReapersToHex` 등) 2장을 추가로 픽해야 한다. Tier1(3픽)은 `ReaperAtkSpeed` 3픽 단독으로 도달 가능하다.

4. **시스템 반응**: 픽 1회마다 `ReaperAtkSpeedEffect`가 `IBattleContext.RegisterMonsterTypeBuff(Reaper, Cooldown, 0.7f)`를 호출해 글로벌 Reaper 쿨다운에 ×0.7을 곱연산 누적하고, 현재 필드의 Reaper들에게도 즉시 소급 적용된다(`card-renewal.md §3.2, continuous-spawn-round.md §6.1`). Dps Tier2 발화 시에는 Reaper·Hex 모두에 추가 쿨다운 ×0.8이 `RegisterMonsterTypeBuff`로 동일 스택 위에 곱산되어 Reaper 쿨다운이 0.1715s → 0.137s로 최종 확정된다.

5. **반복·재발생 패턴**: Reaper 쿨다운 단축은 픽마다 즉시 영구 적용되어 이후 스폰되는 Reaper에도 자동 반영된다. Dps Tier2 발화(5픽) 이후에는 추가적인 쿨다운 단축 경로(Tier3는 Range 강화, 쿨다운 관련 없음)가 없어 0.137s 상태가 런 종료까지 유지된다. 한 런 내 `Frenzy` 액티브 카드(전체 종 쿨다운 ×0.67, 10초)가 추가 발동되면 해당 10초 창에서 Reaper 쿨다운이 0.137s × 0.67 ≈ 0.092s까지 내려갈 수 있다.

6. **종료·해소 조건**: 0.137s 쿨다운 상태는 영웅 처치(승리) 또는 5분 타임오버(패배) 이전에는 되돌릴 수 없다. 현재 `BalanceConfig`에 Reaper 쿨다운 하한을 지정하는 손잡이가 없어(`spawn-period-balance.md §5 참조 — SpawnPeriod만 이관, 전투 쿨다운 별도`), 향후 `SpawnerHaste`나 외부 쿨다운 배율 카드가 추가될 경우 이 극단값이 더 낮아질 수 있다.

7. **다른 시스템과 상호작용**: `SpawnReapers` 3픽(동시 출력 +3)과 결합하면 Reaper 스포너가 4마리씩 동시 스폰하여 DPS 공급량이 스폰 수준에서도 급증한다. `MarkOfDeath` 액티브(영웅 받는 데미지 ×1.5, 5초)가 추가로 적용되면 유효 Reaper DPS가 43.7 × 1.5 ≈ 65.6/s에 도달해 영웅 HP 4000을 이론상 약 61초 만에 소진할 수 있다(단, Reaper HP=100으로 낮아 피해 반격에 취약하여 실전 DPS는 감쇠). Dps Tier1(3픽)으로 동시에 Reaper·Hex Power ×1.3 버프가 들어오므로 Hex DPS도 함께 상승해 전체 Dps 빌드 압박이 복합된다.

8. **엣지 케이스**: Reaper Base HP가 100으로 매우 낮아(`continuous-spawn-round.md §4`), 빠른 공속에도 영웅의 공격 1~2회로 사망한다. 따라서 Reaper가 연속 공격 사이클을 완전히 소화하지 못하고 빠르게 교체 스폰되는 패턴이 반복된다 — 이 경우 "쿨다운 단축"의 실질 효과는 "더 많은 타격 수" 가 아니라 "Reaper가 죽기 전 공격 횟수 최대화"로 나타난다. 또한 `WallOfWisps`(ToughHide, Wisp·Wraith 받피 ×0.75 영구) + `Berserk`(GuardianRage, ×0.5, 15초)가 Tank 카드와 함께 뜨는 혼합 빌드에서는 Reaper의 빠른 쿨다운이 오히려 탱킹 중인 몬스터의 어그로 구조를 방해할 수 있다.

9. **유저 정보·피드백**: 현재 Reaper의 실제 쿨다운 수치는 전투 화면에 어디에도 표시되지 않는다. 빌드 카운트 바와 토스트로 "Dps Tier2 발동"은 알 수 있지만, 이로 인해 쿨다운이 구체적으로 얼마나 단축됐는지 수치 피드백은 없다. 플레이어는 Reaper의 공격 빈도 변화를 시각적으로만 체감하며, 0.137s라는 극단값에 도달했다는 사실을 알 방법이 없다.

### 보류

- **Debuff Tier3 EternalBleedAura 구현 공백** — 검증가치 4, 구현비용 3, 시너지폭 3, 데이터근거 3 = 종합 13. 신규 표면 필요 항목으로 밸런스보다 구현 누락 추적이 주요 관심사이나, 현 루틴 범위(밸런스/손잡이 제안)와 맞지 않아 보류.
- **Tank Tier3 IncrementGlobalMonsterCap 구현 공백** — 종합 14. 마찬가지로 구현 추적 성격 강함.
- **Swarm Tier3 SpawnPhantoms 3픽 복합 (팬텀 출력 최대 5)** — 종합 12. 6-22 (MinSpawnPeriodScale)와 Swarm 축 팬텀 관련 유사 카테고리라 직전 7일 이내 동일 카테고리 주의 기준에 해당 — 차별 근거 약하여 보류.

## 3. 과거 감사 대비 차별성

git log 조회 14건 검토 완료.

가장 유사했던 과거 커밋 2건:
- **dcaa8b7 (2026-06-18)** — "Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박": Frenzy는 `EMonsterBuff.Frenzy`를 통한 전체 종 시한 버프(`MonsterBuffService` 경로), 본 제안은 `RegisterMonsterTypeBuff` 기반 Reaper 전용 영구 쿨다운 × Layer1 Tier2 시너지 교차. 적용 종 범위(전체 vs Reaper 한정)·영구성(시한 10s vs 영구)·경로(`MonsterBuff` vs `RegisterMonsterTypeBuff`)가 모두 다름.
- **440794c (2026-06-09)** — "Dps HexRangeBoost 3픽+Tier3 ring 반경 초과": Dps패시브 × Tier3이지만 Range 스탯 + 위치 기반 오버플로우가 핵심. 본 제안은 Cooldown 스탯 + DPS 폭발이 핵심. 또한 ReaperAtkSpeed 카드 자체는 14번의 과거 감사 어디에도 등장하지 않음.

**결론**: Reaper 쿨다운(공격 주기) 하한 미설계는 과거 14건과 카테고리(Dps패시브·Cooldown·Layer1 Tier2 복합)·요지(쿨다운 극단값 0.137s + BalanceConfig MinReaperCooldownScale 미설계)·근거(card-3pick-cap §2.1 + card-renewal §4.2 + continuous-spawn-round §4)의 조합이 완전히 새로운 항목이다.

## 4. 제외 (범위 밖)

- **SwarmRush 신구현 제안** — 카드 수·에셋 추가에 해당, 컨셉 §11.3 "카드 매수는 lock"에 저촉. 별도 기획 사이클 필요.
- **영웅·몬스터·카드 신규 리소스 제작** — CLAUDE.md §8 "신규 영웅·몬스터·카드 리소스 제작 금지".
- **Frenzy + ReaperAtkSpeed 3픽 복합 극단값** — Frenzy는 시한 버프(10초)이고 6-18에서 유사 패턴 기분석. 독립 후보로 올리지 않음.
- **ReaperAtkSpeed 쿨다운 수치 자체 조정(재조정)** — BalanceConfig 손잡이 없이 수치만 바꾸는 것은 SoT 분산이라 제외. 손잡이 추가가 선행.

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청.
- 기획 내용 제안:
  1. `BalanceConfig.cs`의 `MonsterStatRow`에 `MinCooldownScale` 필드 추가 (0~1 범위, 기본값 0.2f = 쿨다운 최소 20%).
  2. `BalanceConfig.asset`의 Reaper 행에 `MinCooldownScale = 0.3f` 초기값 설정 (0.5s × 0.3 = 0.15s, 현행 0.137s보다 약간 높은 하한 — 즉각 클램프 효과).
  3. `ReaperAtkSpeedEffect`가 `RegisterMonsterTypeBuff` 호출 시, 누적된 쿨다운 배율이 BalanceConfig 하한 아래로 내려가지 않도록 `Mathf.Max(scaledCooldown, baseCD * minScale)` 가드 추가.
  4. 이후 qa-simulator 재실행(훅 구현 후)으로 Dps 3픽+Tier2 빌드의 평균 영웅 사망 시각이 컨셉 §8 "2~4분 사이" 기준 이내에 드는지 검증.

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 리퍼(낫 든 몬스터)는 원래 0.5초마다 한 번씩 영웅을 때린다. 그런데 같은 카드를 세 번 픽하면 리퍼가 거의 0.17초마다 공격하게 되고, 거기에 특정 빌드 보너스까지 붙으면 무려 0.14초마다 두드리는 상태가 된다 — 원래보다 약 3.6배 빠른 셈이다. 이 속도를 조절하는 "브레이크" 역할의 수치 조절 항목이 현재 설정 파일에 없어서, 나중에 비슷한 카드가 하나만 더 추가돼도 속도가 더 내려갈 수 있는 설계 공백이 있다. 그래서 이번에 제안하는 것은: 리퍼가 아무리 강화되더라도 공격 속도가 일정 이하로 내려가지 않도록 하는 안전장치 수치 항목을 설정 파일에 추가하자.
