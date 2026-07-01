# Content Audit — 2026-07-02 — Dps ReplaceReapersToHex 3픽(×2.197) + Dps Tier1(×1.3) 복합 Power ×2.856, MaxDpsPowerMul 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (docs/design/project_lair_concept.md)
- 참조 spec/plan 수: 30개 (specs 30 / plans 30)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED 상태)
- 과거 감사 이력 (git log): 19건 (가장 최근: 2026-06-30 UTC / db9b2d7)

---

## 1. 현황

| 카테고리 | 컨셉 §11 목표 | 실제 에셋 수 | 차이 |
|---|---|---|---|
| 영웅 | 1명 | 1개 (Knight.prefab) | 일치 ✅ |
| 몬스터 | 6종 | 6종 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 일치 ✅ |
| 패시브 카드 | 16장 | 16장 (.asset 28개 중 P=16) | 일치 ✅ |
| 액티브 카드 | 12장 | 12장 (.asset 28개 중 A=12) | 일치 ✅ |
| 카드 효과 클래스 | 28개 | 28개 (.cs 28개) | 일치 ✅ |

### 계획 있으나 미구현
- `Multiply` → `SwarmRushEffect`(팬텀 6마리 즉시 소환) 교체: `card-renewal.md` §3.5 에 SwarmRush 신설 예정으로 기록되어 있으나 현행 `FastBreedingEffect`(팬텀 스포너 주기 ×0.6) 잔존. (docs/design/content-audit/2026-06-04-multiply-to-swarm-rush-active-replace.md 기참조)
- QA 자동 픽 훅(`BattleController.DebugAutoPicker`): QA 리포트 §3 요청사항으로 남아있음 — 미구현.

### QA 권고 미해결
- `BattleController.DebugAutoPicker` (#if UNITY_EDITOR 델리게이트) 구현 → qa-simulator 하베스 구축 가능 (2026-05-22 리포트 §3)
- `LairSimWindow` / `SimDriver` 작성: 훅 구현 선행 필요 (2026-05-22 리포트 §4)
- 실측 메트릭(평균 사망 시각·클리어율·카드별 픽률) 미수집 상태 — 수치 근거는 현재 이론 계산에만 의존

### 과거 감사 후보 (git log 조회 결과)
| 날짜 (UTC) | SHA | 카테고리 | subject 설명 |
|---|---|---|---|
| 2026-06-10 | abe2ecd | Tank | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-11 | e4c765b | Swarm | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-12 | 8de2ecb | Debuff | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-13 | c07cc2c | BalanceConfig | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 |
| 2026-06-14 | 6e02b2a | Dps | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — BalanceConfig 손잡이 이관 제안 |
| 2026-06-15 | 68db140 | Debuff | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-16 | d8fdcfe | Swarm | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-18 | dcaa8b7 | Dps | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-22 | 9118936 | Swarm | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-23 | b83b566 | Dps | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-24 | 128bdb8 | BalanceConfig | 시너지 임계값 3/5/7 하드코딩 — SynergyTierThreshold 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor 손잡이 미설계 |
| 2026-06-26 | 614c299 | Tank | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-28 | 6d21dc5 | Debuff | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-29 | 07d6dd7 | Swarm | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계 |
| 2026-06-30 | db9b2d7 | Debuff | Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Dps ReplaceReapersToHex 3픽(Power ×2.197) + Dps Tier1(Reaper·Hex Power ×1.3) 복합 — Power ×2.856, MaxDpsPowerMul BalanceConfig 손잡이 미설계

- **카테고리**: BalanceConfig 손잡이 / Dps 축 패시브
- **요지**: `ReplaceReapersToHex` 패시브 카드를 3픽하면 Reaper·Hex Power가 ×1.3³ = ×2.197이 된다. 여기에 Dps Tier1(같은 축 3장 누적 즉시 발화) — "Reaper·Hex Power ×1.3" — 이 복합되면 누적 배율이 ×2.197 × 1.3 = **×2.856** 에 달하며, 이 Power 상한을 제어하는 `MaxDpsPowerMul` 손잡이가 `BalanceConfig.asset` 에 없다.
- **검증/구현/시너지/데이터**: 4/2/4/3 → 종합 **15**
- **근거**:
  - `docs/design/card-renewal.md` §3.2 #4 (ReplaceReapersToHex 현행 — Power ×1.3 곱연산 누적)
  - `docs/design/card-renewal.md` §4.2 Tier 표 Dps Tier1 (Reaper·Hex Power ×1.3 글로벌 영구)
  - `docs/design/card-3pick-cap.md` §2.1 (HexRangeBoost/ReplaceReapersToHex 3픽 천장 — ×2.197)
  - `docs/design/continuous-spawn-round.md` §4 (Reaper base Power=6, Cooldown=0.5s / Hex base Power=9, Cooldown=1.0s)
- **MVP 범위**: 컨셉 §11.2 — 패시브 카드 효과값/배율 재조정. 카드 매수(P16/A12) 및 시너지 임계(3/5/7) 불변.

#### 복합 수치 시나리오

| 단계 | 카드 | Reaper Power 배율 | Hex Power 배율 |
|---|---|---|---|
| 베이스 | — | ×1.0 (Power=6) | ×1.0 (Power=9) |
| ReplaceReapersToHex 1픽 | Dps P | ×1.3 | ×1.3 |
| ReplaceReapersToHex 2픽 | Dps P | ×1.69 | ×1.69 |
| ReplaceReapersToHex 3픽 (캡) | Dps P | **×2.197** | **×2.197** |
| + Dps Tier1 발화 (3장 누적) | 시너지 | **×2.856** | **×2.856** |
| → 실효 Power | — | **17.1** | **25.7** |
| → 실효 DPS (쿨다운 기본) | — | 34.2/s (0.5s) | 25.7/s (1.0s) |

Tier2(Dps 5장 누적)까지 발화하면 쿨다운 ×0.8 추가:

| 종 | Tier2 후 쿨다운 | 실효 DPS |
|---|---|---|
| Reaper | 0.4s | **42.9/s** |
| Hex | 0.8s | **32.1/s** |

SpawnReapers 패시브 3픽 시 Reaper Spawner 동시 출력 +3 → 필드 내 Reaper 최대 4마리 상시 유지 가정:

```
실효 총 DPS = 4 × 42.9 + 1 × 25.7 = 171.6 + 25.7 ≈ 197 DPS
영웅 HP 4000 / 197 = 약 20.3초 사망 예측 (몬스터 공격 100% 적중, 영웅 반격 무시)
```

**컨셉 §8 밸런스 기준 "영웅 2~4분 사이 사망"의 하한(120초)을 약 6배 초과하는 시나리오.** `MaxDpsPowerMul` 손잡이가 없으므로 현재 BalanceConfig 조정만으로는 이 상한을 제어할 수 없다.

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**
   영웅 HP 90%에서 첫 패시브 픽 팝업이 열린다. 3장 후보 중 `ReplaceReapersToHex`("처형 명령")가 포함되면 플레이어는 첫 픽에서 선택 가능하다. 이후 HP 80%, 70% 시점에도 같은 카드가 후보로 다시 등장할 수 있으며(전역 3픽 캡까지), 최대 3회 픽으로 Power ×2.197 누적에 도달한다. Dps Tier1 발화는 Dps 축 카드 누계가 3장이 된 순간 즉시 1회 발화한다.

2. **화면 변화**
   `ReplaceReapersToHex` 픽 직후 필드의 모든 Reaper·Hex 색상이 짧게 반짝이며 강화 표시가 나타난다(카드 리뉴얼 §7 기준 Effect 적용 연출). Dps Tier1 발화 시 시너지 패널의 DPS 축 아이콘 마커가 1개에서 2개로 증가하며 추가 강화 알림이 표시된다. 이때 필드 몬스터의 기본 Power값이 즉시 상향되어 영웅 HP 바 감소 속도가 육안으로도 빨라진다.

3. **입력 행동**
   플레이어는 패시브 픽 팝업에서 `ReplaceReapersToHex` 카드를 탭/클릭한다. 이 행동이 최대 3번 발생하면 Power 배율 누적이 완료된다. Dps Tier1은 자동 발화로 별도 입력이 없다. 플레이어 관점에서는 "같은 빨강 테두리 카드를 3번 눌렀더니 적들이 눈에 띄게 강해졌다"는 경험이 된다.

4. **시스템 반응**
   `ReaperHexPowerBoostEffect.Apply(_ctx)` 가 호출될 때마다 `_ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStat.PowerScale, ×1.3)` 과 `_ctx.RegisterMonsterTypeBuff(EMonster.Hex, …)` 가 곱연산 누적된다. 3픽 후 3장 누계 → `BuildSynergyService` 가 Dps Tier1을 감지해 즉시 `Reaper·Hex PowerScale ×1.3` 추가 발화. 최종 누적 배율 ×2.856이 모든 현재·이후 Reaper·Hex 인스턴스에 소급 적용된다. 이 배율에는 현재 상한 코드(`Mathf.Min` 또는 `MaxDpsPowerMul` 손잡이)가 없다.

5. **반복·재발생 패턴**
   `ReplaceReapersToHex` 는 전역 3픽 캡(card-3pick-cap.md)에 의해 3픽 이후 풀에서 제외된다. 그러나 Power 누적 배율은 3픽 완료 후에도 영구 유지된다. Dps Tier1은 한 런에 1회만 발화하지만, Tier2(5장), Tier3(7장)가 추가로 쿨다운·사거리를 강화해 복합 압박이 점진적으로 가중된다. Spawner의 Reaper 생산은 5분 내내 계속되므로 Power 배율 상한이 없으면 후반 필드 DPS가 무제한 상승한다.

6. **종료·해소 조건**
   한 런이 끝나면(승리/패배) `BuildSynergyService.Reset` 과 `CardPickCounter` 리셋으로 Power 배율이 초기화된다. 다음 런에서는 다시 베이스 Power(Reaper=6, Hex=9)로 시작한다. 즉 배율은 런 내 영구 효과이나 런 간 재설정이 보장된다.

7. **다른 시스템과 상호작용**
   - `Frenzy`(A, 모든 몬스터 공속+50% 10초) 동시 발동 시: Reaper CooldownScale이 일시적으로 추가 단축되어 DPS 폭발(예: Reaper DPS 42.9→64.4/s).
   - `MarkOfDeath`(A, 영웅 받피 ×1.5 5초) 겹치면: Power×2.856 × 피해 증폭 ×1.5 → 5초 창 안에 영웅 HP 급감.
   - `SpawnReapers`(P, Reaper Spawner 출력 +1) 3픽 조합: 필드 내 Reaper 최대 4마리 상시 → 총 DPS 4배 이상.
   - Dps Tier2(5장 시): CooldownScale ×0.8 추가. Power 상한 없이 CooldownScale 상한(`MinReaperCooldownScale`, 2026-06-23 감사 제안)도 미적용이면 이중 무제한 강화.

8. **엣지 케이스**
   - Reaper Spawner가 아직 몬스터를 스폰하지 않은 초반(런 시작 직후 12s 이전): 필드에 Reaper가 없어 Power 배율이 누적되더라도 실효 DPS 기여 0 → 전투 초반에는 배율 문제가 체감되지 않는다.
   - Dps Tier1이 발화하는 시점에 Reaper·Hex가 모두 필드에 없는 경우: Power 버프가 등록되지만 적용 대상 없음 → 이후 스폰된 인스턴스에 소급 적용되는 구조.
   - 영웅이 Reaper를 빠르게 처치해 필드 Reaper 수가 낮게 유지되는 상황: Power가 높아도 Reaper 수가 적으면 총 DPS 감소. 그러나 Reaper Spawner 주기(12s)로 지속 보충되어 장기적으론 고 DPS 유지.

9. **유저 정보·피드백**
   카드 팝업의 "처형 명령" 카드 설명("리퍼·헥스 데미지 +30% 영구")은 단픽 효과만 보여준다. 3픽 누적 천장(×2.197)이나 Dps Tier1 복합 결과(×2.856)가 플레이어에게 사전 안내되지 않는다. 시너지 패널의 Dps 축 바도 장 수만 표시하고 Power 최종 배율을 숫자로 보여주지 않아, 플레이어가 현재 몬스터 실효 Power가 기본값의 2.8배임을 인지하기 어렵다.

### 보류
- **WallOfWisps(ToughHide) 2픽 이후 멱등 no-op 설계 검토**: `AddBuff` dedup으로 2픽 이후 효과량 고정(1픽과 동일) — 카드 중복 픽의 실효가 없어 사용자 기만적. 단 이는 3픽 캡으로 포섭 가능하고 BalanceConfig 손잡이보다 설계 의도 재검토 성격. 다음 회차 후보.
- **Tank Tier3(GlobalCap +6=24) 하드코딩**: `MaxGlobalCapBonus` 손잡이 미설계. Tank 카테고리가 7일 이내(2026-06-26)에 있어 이번 회차 보류.

---

## 3. 과거 감사 대비 차별성
- git log 19건 검토 완료.
- **가장 유사한 과거 커밋**: `0fb40b1` (2026-06-19) "Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — MaxTankPowerScale 손잡이 미설계"
  - 차별점: ① 대상 축이 Tank → **Dps**로 다름. ② 복합 대상이 Tank Tier2(Power+생존) → **Dps Tier1(Power 즉시 발화) + Tier2(공속)**으로 다름. ③ 손잡이 이름이 `MaxTankPowerScale` → `MaxDpsPowerMul`로 다름. ④ Dps Tier1·2 복합 시 공속 감소까지 포함된 이중 무제한 상황이 Tank와 달리 Speed Tier까지 연쇄함.
- **7일 이내(2026-06-25~07-02) Dps 카테고리**: git log에서 0건. ✅
- **요지·카테고리·근거 3요소 중 2요소 이상 동일한 과거 커밋**: 없음.

---

## 4. 제외 (범위 밖)
- 신규 카드 효과 클래스 추가: 컨셉 §11 MVP 범위에서 카드 리소스 추가 금지.
- 신규 영웅·몬스터 추가: §9 절대 금지.
- 서버 연동 연계 밸런스: 클라이언트 로컬 전투 범위에 한정.
- ReplaceReapersToHex 효과 모델 원안(Spawner 종 교체)으로 롤백: 현행 에셋(Power 강화)을 SoT로 유지. card-renewal.md 변경 이력 블록 §1.3 참조.

---

## 5. 다음 단계 제안
- 채택 시 → **game-designer** 에게 정식 기획 요청: `MaxDpsPowerMul` float 값(예: 2.5) 결정 및 `BalanceConfig.asset` 손잡이 추가 범위 확정.
- 구현 시 → `ReaperHexPowerBoostEffect.Apply()` 및 `BuildSynergyService` Dps Tier1 발화 경로에 `Mathf.Min(current, config.MaxDpsPowerMul)` 가드 추가.
- 검증 시 → qa-simulator 훅 구현(2026-05-22 QA §3) 후 Dps 집중 빌드 전략으로 N판 시뮬레이션, 영웅 평균 사망 시각이 120초 이상인지 확인.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에는 "처형 명령"이라는 카드가 있는데, 이걸 3번 뽑으면 몬스터 두 종류(리퍼와 헥스)가 기본 공격력의 약 2.2배로 강해진다. 거기다 같은 계열 카드를 3장 모으면 자동으로 발동하는 "보너스 효과"가 공격력을 또 1.3배로 늘려서, 최종적으로 약 2.9배가 된다. 영웅의 체력이 4,000인데 이 상태에서 몬스터가 여러 마리 쏟아지면 이론상 20초 만에 영웅이 쓰러질 수 있다. 평소 목표는 "2~4분 안에 영웅 처치"인데 이건 그것보다 6배나 빠른 셈이다. 문제는 이 공격력이 얼마까지 오를 수 있는지 제한하는 설정값이 없어서, 나중에 밸런스를 조정하려면 코드를 직접 수정해야 한다는 점이다. 그래서 이번에 제안하는 것은: 공격력 최대 배율을 하나의 설정 숫자(예: 2.5배까지만)로 제한할 수 있게 만들자는 것이다.
