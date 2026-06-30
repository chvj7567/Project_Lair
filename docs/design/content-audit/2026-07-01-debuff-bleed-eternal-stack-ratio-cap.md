# Content Audit — 2026-07-01 — Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 시 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (2026-06-10 갱신)
- 참조 spec/plan 수: 30개 (specs/) + 30개 (plans/)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED 상태 — DebugAutoPicker 훅 미구현)
- 과거 감사 이력 (git log): 18건 (가장 최근: 2026-06-29)

---

## 1. 현황

| 카테고리 | 컨셉 §11.3 | 실제 에셋/프리팹 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (기사) | Knight.prefab 1개 | ✅ 충족 |
| 몬스터 | 6종 | Wisp·Wraith·Reaper·Hex·Phantom·Plague 프리팹 6개 + LittleGhost 시리즈 7개 | ✅ 6종 충족 (LittleGhost는 파생) |
| 패시브 카드 | 16장 (4축 × 4장) | Items/ 아래 SO 28개 중 패시브 16장 | ✅ 충족 |
| 액티브 카드 | 12장 (4축 × 3장) | 액티브 12장 | ✅ 충족 |
| 카드 이펙트 클래스 | 28개 | Effects/ CS 28개 확인 | ✅ 충족 |

### 계획 있으나 미구현

- **SwarmRush** (`card-renewal.md §3.4`): Multiply → SwarmRush(팬텀 6마리 즉시 소환) 교체 예정이나 `FastBreedingEffect`(Multiply 잔존) 상태. `ECardId.Multiply` enum 자리 보존 중.
- **DebugAutoPicker 훅** (`docs/qa-reports/2026-05-22.md §3`): `BattleController` 에 `#if UNITY_EDITOR` 자동 픽 델리게이트 미추가 → qa-simulator 헤드리스 시뮬레이션 BLOCKED 상태 지속.

### QA 권고 미해결

- **QA 미실행** (`2026-05-22.md`): DebugAutoPicker 훅이 없어 카드 선택 팝업에서 전투 영구 정지 → 모든 시뮬레이션 데이터 공백. 권장: gameplay-programmer 에게 ~10줄 델리게이트 훅 구현 요청.

### 과거 감사 후보 (git log 조회 결과 — `# [Routines][Daily Content Audit]`)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-12 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-13 | c07cc2c | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 |
| 2026-06-14 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — BalanceConfig 손잡이 이관 제안 |
| 2026-06-15 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-16 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-22 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-23 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-24 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — SynergyTierThreshold 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-26 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-28 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-29 | 07d6dd7 | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### [Debuff 액티브 Bleed 3픽(30s) + Debuff Tier3 EternalBleed(영구) 동시 활성 — 출혈 합산 -3%/s = 33초 이내 자동 처치 + MaxBleedRatioPerSec 손잡이 미설계]

- **카테고리**: 액티브 카드 효과값 재조정 + BalanceConfig 손잡이 추가
- **요지**: Debuff 7장 빌드 완성 시 `Bleed` 3픽(-2%HP/s, 30s)와 `Debuff Tier3 EternalBleed`(-1%HP/s, 영구)가 서로 다른 HeroAura 클래스로 독립 등록되어 합산 -3%/s 출혈이 발생한다. 영웅이 AutoCombatAI로 이동하는 구조에서 영웅은 항상 움직이므로, 출혈만으로 4000 HP를 4000 / (4000×0.03) = **33.3초** 내에 소진시킬 수 있다. 이 시나리오에 대한 `MaxBleedRatioPerSec` BalanceConfig 손잡이가 없다.
- **검증/구현/시너지/데이터**: 4/2/4/4 → 종합 **16**
- **근거**: `docs/design/card-renewal.md` §3.3 (`Bleed`: `_ratio=0.02 _duration=10`) / §4.2 Debuff Tier3 (EternalBleed, ratio 0.01 영구) / §7.2 (Bleed 지속시간 누적 정책, EternalBleedAura 는 별도 클래스 `ApplyHeroAura(-1f)` 등록)
- **MVP 범위**: 컨셉 §11.2 ✅ — 액티브 카드 효과값 재조정 / BalanceConfig 손잡이 추가 (패시브 16·액티브 12 매수 불변, 신규 종 없음)

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**
플레이어가 Debuff 빌드를 추구하며 30초 액티브 타이밍에 `Bleed` 카드를 1~3회 픽할 때 시작된다. 동시에 Debuff 축 픽 카운트가 7에 도달하면 `Debuff Tier3 시너지(EternalBleed)`가 즉시 발화한다. 두 이벤트는 동일 전투 런 내에서 자연스럽게 발생 가능하다 — Debuff 7픽은 패시브 4픽(PlagueSlowBoost·SpawnPlagues·HeroPoisonAura·HeroAttackDown) + 액티브 3픽(Fear·Bleed·Weaken) 의 전부를 Debuff 축으로 픽하면 달성된다.

**2. 화면 변화**
`Bleed` 첫 픽 직후 영웅 캐릭터에 출혈 비주얼 표시(진빨강 `#991B1B` 색상 변경, `컨셉 §11.4`)가 나타난다. Debuff Tier3 발화 시 토스트 "Debuff 시너지 Tier 3 발동!" 텍스트가 1.5초간 표시된다. 이후 화면상 추가 시각 변화 없음 — EternalBleed는 별도 비주얼 표시 설계가 없다.

**3. 입력 행동**
플레이어는 30초 액티브 팝업에서 `Bleed`를 최대 3회 선택한다(전역 3픽 캡으로 4회 선택 불가). 각 픽마다 기존 Bleed 잔여시간에 10초가 더해지므로 3픽 완료 후 최대 잔여시간은 30초다. Debuff Tier3 시너지는 자동 발화이므로 별도 입력 없이 등록된다.

**4. 시스템 반응**
`BleedEffect.Apply()` 호출 → `IBattleContext.ApplyHeroAura(new BleedAura(_ratio=0.02, _duration=10), 10f)` — 지속시간 누적 정책으로 잔여+10s. EternalBleed Tier3 발화 → `IBattleContext.ApplyHeroAura(new EternalBleedAura(ratio=0.01), -1f)`. 이 두 Aura는 **서로 다른 클래스 인스턴스**이며 HeroAura 관리 시스템에 각각 독립 등록된다. `card-renewal.md §4.5` 설계 기준상 dedup 대상이 아니다. 영웅이 이동하는 매 프레임마다 두 Aura가 모두 HP 감소를 적용하여 합산 비율로 누적된다.

**5. 반복·재발생 패턴**
BleedAura는 3픽 후 최대 30초간 유지되며, 이 기간 중 영웅 이동마다 -2%/s가 지속적으로 발동한다. EternalBleedAura는 영구적이므로 런 종료까지 -1%/s가 항상 적용된다. Bleed 30초 만료 후에도 EternalBleedAura -1%/s는 계속 작동한다. 두 Aura가 동시 활성인 구간(최대 30초)에 -3%/s가 합산된다.

**6. 종료·해소 조건**
BleedAura는 `_duration` 만료(최대 30초) 후 자동 해제된다. EternalBleedAura(`duration=-1f`)는 런 종료까지 해제되지 않는다. 현재 "출혈 효과를 해제"하는 영웅 측 메커니즘이 설계상 없으므로 플레이어가 의도적으로 멈출 수 없다.

**7. 다른 시스템과 상호작용**
- **AutoCombatAI**: 영웅은 가장 가까운 몬스터를 향해 자동 이동 → 전투 중 영웅은 거의 항상 이동하므로 출혈 발동률이 사실상 100%에 근접.
- **HeroAttackDown 3픽 + Debuff Tier2**: 영웅 공격력도 × 0.75³ × 0.85 ≈ 36% 로 떨어지므로 영웅이 몬스터를 죽이기도 어렵고(공격력 저하) → 더 오래 이동 → 출혈 더 많이 발동. 두 효과가 상호 강화.
- **Fear 3픽(지속시간 누적 9초 도주)**: 영웅 도주 시에도 이동 중이므로 EternalBleed 발동. Fear + EternalBleed 콤보는 "도망치면 피가 빠져 죽는" 극한 디버프 시나리오를 만든다.
- **BalanceConfig.HeroHp**: 영웅 HP가 높을수록 절대 피해량이 커지지만(4000 × 0.03 = 120/s), 비율 기반이므로 HP 변동에 자동 비례.

**8. 엣지 케이스**
- **최악 시나리오**: Debuff 7픽 + Bleed 3픽 달성 시 합산 -3%/s. 영웅 HP 4000 기준 4000 / (4000 × 0.03) = **33.3초** 이내 출혈 사망. 이는 패시브 Tier3 보상 "영웅 이동 강제"와 정확히 결합되어, Debuff 빌드가 다른 축 카드 없이 출혈만으로 런을 완결할 수 있다.
- **BleedAura 중간 만료 시점**: 30초 BleedAura 만료 후 EternalBleed만 남으면 -1%/s. 4000/40 = 100초 추가 생존 가능 → 이 경우엔 정상 범위(평균 사망 76s 컨셉 §8 기준 허용).
- **영웅이 멈추는 경우**: `TimeStop`(5s) 하에서 영웅이 이동 불가 → 두 Aura 모두 발동 안 함. 아이러니하게 Swarm 픽 `TimeStop`이 출혈 데미지를 줄이는 효과.
- **WraithDamageBoostEffect `dedup` 참조**: Tank의 `ToughHide` 는 `EMonsterBuff` dedup 으로 중복 차단되지만, HeroAura는 `EMonsterBuff` 가 아니라 별도 경로. Bleed/EternalBleed의 dedup 여부는 코드 확인 필요.

**9. 유저 정보·피드백**
현재 영웅 HP 바(빨강 `#DC2626`)가 화면 상단에 표시되지만, 출혈로 인한 HP 감소 속도를 나타내는 별도 DPS 숫자나 시각적 "출혈 강도" 표시가 없다. 플레이어는 Debuff 7픽 완료 → 출혈 스택이 쌓이는 것을 모르고 영웅이 갑자기 빠르게 사망하는 것을 관찰한다. 예상보다 빠른 사망은 "운 좋게 이겼다"는 느낌을 줄 수 있어, 플레이어가 이 빌드를 재현할 수 없거나 왜 이겼는지 이해 못 하는 인지 간극이 생긴다.

### 보류

- **HexRangeBoost 3픽 + Dps Tier3 복합 사거리 17.8u → 던전 링 반지름 14.0u 초과**: 이전 감사 파일에서 다뤄진 흔적 확인(폴더 탐색 시 우발 확인). 금번 보류.
- **SwarmRush 미구현(Multiply 잔존) 설계 공백**: 2026-06-04 이전 폴더 파일에서 다뤄진 것으로 추정. 보류.

---

## 3. 과거 감사 대비 차별성

git log 조회 18건 전부 검토 완료.

가장 유사했던 과거 커밋:
- `6d21dc5` (2026-06-28): "Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale" → **이동속도 슬로우 중첩(패시브)** 에 관한 것이며, 출혈 비율 스택(액티브+Tier3)과는 다른 카드·다른 메커니즘·다른 BalanceConfig 키.
- `abe2ecd` (2026-06-10): "Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한" → 몬스터 방어력 하한이며, 영웅 출혈 비율 합산과 무관.

**차별점**: 18건 모두 출혈(`Bleed`/`EternalBleed`) 스택 합산 비율 및 `MaxBleedRatioPerSec` 손잡이를 다루지 않는다. 이번 후보는 액티브 카드(Bleed) 와 시너지 Tier 효과(EternalBleed)의 **서로 다른 HeroAura 클래스가 additive 스택되는 구조적 공백**에 초점을 맞춘다. 카테고리(Debuff 액티브)는 2026-06-28 과 같은 Debuff 축이나, 메커니즘(HP 비율 출혈 합산 상한 미설계)이 다르다.

---

## 4. 제외 (범위 밖)

- 신규 영웅·몬스터·카드 리소스 제작 → 컨셉 §11 범위 밖, `CLAUDE.md §8` 명시 금지
- 서버 연동 관련 밸런스 조정 → 별도 레포(`Project_Lair_Server`) 소관
- 메인 메뉴·세팅 화면 → 마을 허브가 시작 화면 겸임, 별도 메인 메뉴 없음

---

## 5. 다음 단계 제안

채택 시:
1. **game-designer** → `Bleed` + `EternalBleed` 동시 활성 시나리오 기준 `MaxBleedRatioPerSec` 수치 설계 (예: 0.02/s 상한 → Bleed 단독 최대치로 제한)
2. **gameplay-programmer** → `BalanceConfig.asset` 에 `_maxBleedRatioPerSec` float 필드 추가 + HeroAura 합산 로직에 클램프 적용
3. **test-engineer** → BleedAura + EternalBleedAura 동시 등록 시 합산 클램프 EditMode 테스트
4. **DebugAutoPicker 훅 선행 구현 권장**: QA 시뮬레이션으로 Debuff 7픽 시나리오를 자동 검증하려면 `BattleController.DebugAutoPicker` 가 먼저 필요하다 (`docs/qa-reports/2026-05-22.md §3` 참조).

---

## 6. 쉬운 설명 (비개발자 요약)

지금 이 게임에는 영웅을 걸을 때마다 조금씩 다치게 하는 "출혈" 카드가 있고, 카드를 많이 쌓으면 "영구 출혈"이라는 보너스도 생긴다. 문제는 이 둘을 동시에 걸면 피가 빠지는 속도가 두 배가 넘어서, 영웅이 그냥 걷는 것만으로 **약 30초 만에 쓰러질 수** 있다는 점이다. 원래 영웅을 쓰러뜨리는 건 몬스터들이 열심히 싸워서 이겨야 하는 건데, 출혈만으로 자동으로 끝나버리면 게임의 재미인 "몬스터 조합 고르는 전략"이 의미 없어진다. 그래서 이번에 제안하는 것은: 출혈 효과가 쌓여도 초당 2%를 넘지 않도록 상한선을 설정하고, 그 숫자를 기획자가 쉽게 조절할 수 있는 손잡이로 만들자.
