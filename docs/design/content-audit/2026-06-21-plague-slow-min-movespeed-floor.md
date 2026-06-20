# Content Audit — 2026-06-21 — PlagueSlowBoost 3픽 + Debuff Tier1 복합 영웅 이동속도 하한 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (2026-06-10 최종 갱신)
- 참조 spec/plan 수: 30개 (specs 30 / plans 30)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED, 헤드리스 훅 미구현으로 시뮬 미실행)
- 과거 감사 이력 (git log): 12건 (가장 최근: 2026-06-19)

---

## 1. 현황

### 카테고리별 컨셉 대비 실제

| 카테고리 | 컨셉 §11 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (Knight) | 1개 (`Knight.prefab`) | 0 |
| 몬스터 | 6종 | 6개 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 0 |
| 패시브 카드 | 16장 | 16장 (4축 × 4장, `Assets/_Lair/Art/Cards/Items/` 28개 중 패시브 16) | 0 |
| 액티브 카드 | 12장 | 12장 (4축 × 3장) | 0 |
| 카드 효과 클래스 | 28개 | 28개 (`Assets/_Lair/Scripts/Card/Effects/`) | 0 |
| 캐릭터 프리팹 | 7개 (영웅 1 + 몬스터 6) | 14개 (추가 LittleGhost 시리즈 등 v0.3 아트 대응) | +7 (범위 확장) |

### 계획 있으나 미구현

- **SwarmRush 미구현**: `card-renewal.md` §3.4 에서 `Multiply` 폐기 후 `SwarmRush`(팬텀 6마리 즉시 소환) 신설 예정이나 현행 `Multiply.asset`("빠른 번식", FastBreedingEffect, 팬텀 스포너 주기 ×0.6) 가 잔존. 광역 압살 폐기 의도 미실현.
- **`DebugAutoPicker` 훅 미구현**: QA 리포트(2026-05-22) §3 에서 gameplay-programmer 에게 요청했으나 구현 여부 미확인. 헤드리스 시뮬레이션 불가 상태.
- **MinHeroMoveSpeedScale BalanceConfig 손잡이 미설계**: Plague SlowFactor 복합 누적 하한이 `BalanceConfig.asset` 에 없음 (본 감사 주 후보 — §2 참조).

### QA 권고 미해결

- **2026-05-22 QA 보고서 전체 미해결**: 시뮬레이션 인프라(DebugAutoPicker 훅) 가 미구현으로 QA 6차 이후 본격 재시뮬 미실행. QA 6차 권고 ④⑤(5분 타임오버 ≥1판, 클리어율 ≤80%) 는 card-renewal.md §9.7 기준 QA 7차 검증 항목이나 헤드리스 환경 미비로 미확인.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 (KST) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-19 | `0fb40b1` | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — BalanceConfig MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | `dcaa8b7` | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | `3a9bed3` | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | `d8fdcfe` | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — BalanceConfig MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-15 | `68db140` | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 (영웅 HP 4000 기준 3픽 합산 1.875% — 스킬 도입 후 격차 확대) |
| 2026-06-14 | `6e02b2a` | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — 종별 HP 불균형 + BalanceConfig 손잡이 이관 제안 |
| 2026-06-13 | `c07cc2c` | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 (메타 상점 복합 위험 시나리오 연계) |
| 2026-06-12 | `8de2ecb` | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-11 | `e4c765b` | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | `abe2ecd` | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | `440794c` | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |
| 2026-06-08 | `2002c8b` | Debuff 액티브 출혈 카드 비율 재조정 제안 (Bleed 2%→1%/s) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### PlagueSlowBoost 3픽 + Debuff Tier1 복합 영웅 이동속도 하한 미설계 — MinHeroMoveSpeedScale BalanceConfig 손잡이 추가

- **카테고리**: Debuff 패시브 / BalanceConfig 손잡이
- **요지**: `PlagueSlowBoost` 3픽 시 영웅 이동속도가 기본 대비 27%(Debuff Tier1 포함)까지 떨어질 수 있으나 `MinHeroMoveSpeedScale` 하한이 `BalanceConfig` 에 없어 경계값이 설계되지 않은 상태다. `Slow` 액티브(×0.5) 병용 시 이론상 13.5%까지 낮아져 사실상 정지에 가깝고, 이 극단값이 의도된 밸런스 범위인지 검증이 필요하다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 5 / 2 / 4 / 4 → 종합 **17**
- **근거**:
  - 컨셉 §11.3: Plague "공격 시 영웅 둔화 20%" (SlowFactor 기본 0.8)
  - `card-renewal.md` §3.3 `PlagueSlowBoost`: `_slowFactor=0.75`, "BaseSlowFactor 0.8 × 0.75 = 0.6 곱연산"
  - `card-renewal.md` §4.2 Debuff Tier1: "Plague SlowFactor ×0.8 (강한 둔화 추가)"
  - `card-renewal.md` §3.4 Slow 액티브: `_heroFactor=0.5` (별도 아우라로 곱연산)
  - `BalanceConfig` 에 `MinHeroMoveSpeedScale` 키 미존재 확인 (§5.4 / §1 참조)
- **복합 수치 계산**:

  | 상황 | SlowFactor | 영웅 이속 |
  |---|---|---|
  | Plague 접촉 (기본) | 0.80 | 80% |
  | PlagueSlowBoost 1픽 | 0.80 × 0.75 = **0.60** | 60% |
  | PlagueSlowBoost 2픽 | 0.80 × 0.75² = **0.45** | 45% |
  | PlagueSlowBoost 3픽 | 0.80 × 0.75³ = **0.3375** | 33.75% |
  | + Debuff Tier1 (×0.8) | 0.3375 × 0.80 = **0.27** | 27% |
  | + Slow 액티브 (×0.5) | 0.27 × 0.50 = **0.135** | 13.5% |

- **MVP 범위**: 컨셉 §11.2 ✅ (패시브 카드 조정 + BalanceConfig 손잡이 추가 — 카드 매수·종·영웅·몬스터 변동 없음)

#### 유저 플로우

1. **노출 시점·트리거**  
   영웅 HP가 10% 감소할 때마다 발생하는 패시브 선택지 팝업에 `PlagueSlowBoost`("역병의 손길")가 Tank·Dps·Swarm 축 카드들과 경쟁하여 나타난다. Plague 스포너(#4, 180° 위치)가 전투 시작 시 자동 배치되어 있어 Plague 가 이미 스폰되고 있는 상태에서 효과가 즉시 반영된다. 선택지에 나타날 때 빌드 카운트 바에서 Debuff 카운트가 올라가는 것을 확인할 수 있다.

2. **화면 변화**  
   PlagueSlowBoost 픽 직후부터 Plague 가 영웅에 접촉할 때 영웅 몸에 부착되는 하늘 반투명 구체(둔화 표시, 컨셉 §11.4)가 생긴다. 영웅의 이동 애니메이션이 눈에 띄게 느려지며, 3픽 후에는 영웅이 천천히 기어가는 수준이 된다. Debuff Tier1(3장) 발화 시점에는 화면 상단에 "Debuff 시너지 Tier 1 발동!" 토스트가 1.5초 표시되고 빌드 카운트 바 Debuff 셀이 0.3초 펄스한다.

3. **입력 행동**  
   플레이어는 HP% 선택 팝업에서 3장 중 `PlagueSlowBoost` 카드를 클릭한다. 이미 2픽 상태라면 전역 3픽 캡 규칙에 따라 세 번째 선택 시에도 후보에 포함되지만, 3픽 후에는 이 카드가 후보에서 영구 제외된다. Debuff Tier1 임계(3장)에 도달하는 카드라면 Dps·Swarm 축 카드와 혼합해도 Debuff 카운트 3장이 되는 순간 자동 발화한다.

4. **시스템 반응**  
   픽 시 `PlagueSlowBoostEffect.Apply()` 가 호출되어 `IBattleContext.RegisterMonsterTypeBuff(EMonster.Plague, SlowFactor, 0.75)` 로 Plague 종의 SlowFactor 배율을 0.75씩 곱연산 영구 적용한다. 이후 Plague 가 영웅에 접촉하는 순간 Hero Aura 시스템이 갱신된 SlowFactor(0.6 → 0.45 → 0.3375)를 영웅 이동속도 스케일에 반영한다. Debuff Tier1 도달 시 `BuildSynergyService` 가 추가 ×0.8 슬로우를 별도 RegisterMonsterTypeBuff 로 한 번 더 적용해 실효 SlowFactor 가 0.27 이 된다.

5. **반복·재발생 패턴**  
   Plague 스포너는 10초 주기로 Plague 를 계속 스폰하므로, 필드에 항상 1마리 이상의 Plague 가 순환한다. 영웅의 AutoCombat AI(가장 가까운 몬스터 자동 이동)로 인해 영웅이 Plague 에 자주 근접하며, 슬로우 효과가 Plague 생존 기간 내내 지속된다. SpawnPlagues 패시브를 추가로 픽하면 Plague 수량이 최대 4마리/주기로 늘어 슬로우 접촉 빈도가 올라간다.

6. **종료·해소 조건**  
   PlagueSlowBoost 의 효과는 Plague 종 전체에 영구 적용되어 전투 중 해제되지 않는다. 개별 슬로우 오라는 영웅과 Plague 가 일정 거리 이상 떨어지면 잔여 지속시간 후 소멸하지만, 다음 Plague 가 접촉하면 즉시 재적용된다. 전투 종료(영웅 HP 0 또는 5분 타임오버) 시에만 모든 효과가 리셋된다.

7. **다른 시스템과 상호작용**  
   가장 위험한 교차점은 `Slow` 액티브 카드(Swarm 축)와의 병용이다 — Slow 가 별도 영웅 이동속도 오라(×0.5, 10초)를 추가하여 이미 0.27 상태인 영웅 이속이 0.135 로 수렴한다. 또한 Swarm Tier1(Phantom·Wisp MoveSpeed ×1.3)이 발동된 경우 몬스터는 빠르고 영웅만 느려지는 극단적 속도 비대칭이 발생한다. Debuff Tier2 자동 등록(HeroAttackDown ×0.85 영구)까지 활성화되면 영웅이 느리고 약해져 Plague 를 처치하기도 어려워지는 복합 압박이 만들어진다.

8. **엣지 케이스**  
   `MinHeroMoveSpeedScale` 하한이 없으면 이동속도 스케일이 0에 수렴하는 경로가 이론상 열린다 — AutoCombat AI 의 pathfinding 이 이동속도 0 또는 근사값에서 예상치 않게 동작할 수 있다(목표지점 도달 전 무한 이동 시도). Fear 카드(도주 3초)와 함께 활성화 시 "극도로 느린 속도로 도망"하는 시각적 기이함이 발생할 수 있다. TimeStop(정지 5초)과 슬로우 오라가 동시에 걸린 상태에서 TimeStop 해제 후 슬로우가 제대로 재적용되는지 상태 동기화 검증이 필요하다.

9. **유저 정보·피드백**  
   현재 영웅 위 둔화 표시(하늘 파란 Sphere)는 "둔화 중"임을 나타내지만 실제 배율 수치는 노출되지 않는다. 플레이어는 영웅의 시각적 이동 속도로 둔화 강도를 간접 체감할 수 있을 뿐이다. BuildSynergyPanel 에서 Debuff 카운트와 티어 마커 아이콘으로 시너지 상태를 확인할 수 있으나, "이 Debuff 빌드가 영웅을 얼마나 느리게 만들었는가"의 정량 피드백은 현재 설계에 없다. `MinHeroMoveSpeedScale` 설계 시 인스펙터에서 수치를 직접 확인하고 조정할 수 있어 밸런싱 반복이 쉬워진다.

### 보류

- **`Weaken` 3픽 지속시간 누적 + HeroAttackDown 공격력 복합 하한**: 2026-06-12 `MinHeroAttackScale` 감사(`8de2ecb`)와 구조가 유사(공격력 하한, 동일 Debuff 패시브 카테고리). 명확한 차별성 없어 보류.
- **`SpawnPlagues` 3픽 Plague 밀도 + PlagueSlowBoost 복합**: 슬로우 빈도 증가 각도지만 본 후보(SlowFactor 수치 하한)에 포함된 구조이므로 별도 후보로 내기에 부족. 본 후보의 §유저플로우 5·7 에서 언급.
- **Debuff Tier3 EternalBleedAura 구현 상태**: card-renewal.md §4.5 에서 신규 표면(`IBattleContext.ApplyHeroAura` + `-1f`) 필요로 명시되어 있으나, git log 조회 범위(2026-06-08 이후) 내 해당 감사 이력 없음. 단, `docs/design/content-audit/` 폴더의 기존 파일 목록에서 추정되는 이전 감사(폴더 직접 읽기 금지 정책으로 내용 미확인). 미확정으로 보류.

---

## 3. 과거 감사 대비 차별성

git log 조회 12건 검토 완료.

**가장 유사했던 과거 커밋들과 차별점:**

| 커밋 | 내용 | 본 감사와 차이 |
|---|---|---|
| `d8fdcfe` (2026-06-16) | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — MaxMoveSpeedScale **몬스터** 이속 **상한** | 대상: **몬스터**(빠름) vs **영웅**(느림). 방향: **상한 캡** vs **하한 플로어**. 완전 반대 방향의 다른 개체. |
| `8de2ecb` (2026-06-12) | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — MinHeroAttackScale | 동일 축(Debuff 패시브), 동일 구조(BalanceConfig 하한 손잡이). 그러나 스탯이 다름: **공격력** vs **이동속도**. 이동속도 하한이 공격력 하한과 독립적으로 설계되어야 하는 이유: Plague 고유 메커니즘(접촉 슬로우 지속)이 HeroAttackDown(즉시 영구 적용)과 작동 방식이 달라 별도 클램프 로직 필요. |
| `68db140` (2026-06-15) | Debuff 패시브 HeroPoisonAura DPS 5 실효 기여도 재조정 | 동일 축(Debuff 패시브)이지만 완전히 다른 카드·스탯·현상. DPS 재조정 vs 이동속도 하한. |

**결론**: 12건 중 이동속도 하한(`MinHeroMoveSpeedScale`) 또는 `PlagueSlowBoost` 를 주제로 삼은 감사 없음. 카테고리(Debuff 패시브) 일부 중복이 있으나 스탯·현상·설계 공백이 모두 상이해 채택 기준 충족.

---

## 4. 제외 (범위 밖)

- **Plague 종 추가 또는 새 슬로우 메커니즘 설계**: 컨셉 §11 에서 몬스터 6종 고정, CLAUDE.md §8 "신규 영웅·몬스터·카드 리소스 제작 금지" — 제외.
- **HeroPoisonAura DPS 재조정**: 2026-06-15 감사(`68db140`)에서 이미 제안됨 — 중복 제외.
- **Plague 슬로우와 Fear(공포 도주) 간 우선순위 인터페이스 리팩터**: 엔지니어링 구조 변경 범위로, 본 감사 범위(BalanceConfig 손잡이 추가) 초과 — 제외.

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청:  
  `MinHeroMoveSpeedScale` 적정값 설계 (예: 0.25 또는 0.30), `BalanceConfig.asset` 에 추가, `PlagueSlowAura` 또는 슬로우 적용 로직에 클램프 1줄 삽입 (gameplay-programmer 구현 ~30분 수준).
- 병행 검토 권장: `MinHeroMoveSpeedScale` 설계 시 `Slow` 액티브 카드(×0.5)와 누적 시나리오도 함께 명세해 두면, 향후 Debuff + Swarm 교차 픽 시 예상치 못한 정지 버그를 예방할 수 있다.

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 "플레이그"라는 보라색 몬스터는 영웅에게 닿으면 영웅을 느리게 만든다. 그런데 "역병의 손길"이라는 카드를 3번 고르고, 디버프 시너지 보너스까지 발동되면, 영웅이 평소 속도의 4분의 1 수준으로 느려진다. 여기에 "던전의 점성"이라는 액티브 카드까지 쓰면 영웅이 거의 멈춰 있는 것처럼 보일 수도 있다. 문제는 "영웅 속도가 아무리 느려도 이 이상은 안 된다"는 최소값이 게임 어디에도 설정되어 있지 않다는 것이다. 그래서 이번에 제안하는 것은: 영웅의 이동속도 최솟값을 게임 설정 파일(BalanceConfig)에 추가해, 영웅이 완전히 멈추지 않도록 안전장치를 만들자.
