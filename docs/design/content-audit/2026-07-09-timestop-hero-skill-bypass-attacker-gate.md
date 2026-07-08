# Content Audit — 2026-07-09 — Swarm 액티브 TimeStop 발동 중 영웅 스킬(HeroSkillRunner)이 IAttacker.Enabled를 확인하지 않아 계속 시전됨 — TimeStopDuration BalanceConfig 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (`docs/design/project_lair_concept.md`)
- 참조 spec/plan 수: 30개 (specs 30, plans 30)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED, 훅 미구현)
- 과거 감사 이력 (git log `# [Routines][Daily Content Audit]`): 21건 (가장 최근: 2026-07-07)

---

## 1. 현황

| 카테고리 | 컨셉 §11 목표 | 실제 에셋/SO | 차이 |
|---|---|---|---|
| 영웅 | 1 (기사) | 1 (`Knight.prefab`) | 없음 |
| 몬스터 | 6종 | 6종 (`Wisp·Wraith·Reaper·Hex·Plague·Phantom.prefab`) | 없음 |
| 패시브 카드 | 16장 | 16장 (SO 28개 중 P 16장 확인) | 없음 |
| 액티브 카드 | 12장 | 12장 (SO 28개 중 A 12장 확인) | 없음 |
| 카드 효과 클래스 | 28 | 28개 cs (TimeStopEffect 포함) | 없음 |

### 계획 있으나 미구현

- **DebugAutoPicker 훅**: QA 리포트(2026-05-22 §3)에서 `BattleController.DebugAutoPicker` 델리게이트 추가 요청 → gameplay-programmer 미구현 상태. 헤드리스 시뮬레이션 전면 차단 중.
- **HeroSkillRunner × TimeStop 상호작용 명문화**: `hero-skills.md` §1 에 스킬이 기본 근접 공격과 가산된다고 명시하나, TimeStop(이동·공격 완전 정지) 중 스킬 시전 여부는 어느 기획서에도 없음.

### QA 권고 미해결

- QA 리포트 2026-05-22 §3 — `DebugAutoPicker` 훅 없이 시뮬레이션 불가. 21회차 감사 기간 내내 미해결.
- QA 리포트 2026-05-22 §4.1 — MCP 헤드리스 환경에서 frameCount 고정 → 실제 플레이 시뮬 불가. 해결 방식(대화형 에디터 vs UnityTest 래핑) 미결.

### 과거 감사 후보 (git log 조회 결과 — 21건)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-07-07 | 1be6efc | Debuff 패시브 HeroAttackDown 3픽+Tier2(×0.85) 복합 영구 공격력 ×0.358 — MinHeroAttackScale(영구) 손잡이 미설계 |
| 2026-07-06 | bddf4f3 | Tank 액티브 ToughHide·IronWill·GuardianRage 3중 데미지 감소 복합 ×0.2625 — MinMonsterDamageTakenScale 손잡이 미설계 |
| 2026-07-05 | 78c61f3 | Debuff 액티브 Weaken _factor·_duration 하드코딩 — WeakenFactor·MinHeroAttackScaleFloor BalanceConfig 손잡이 미설계 |
| 2026-07-04 | 9b3303b | Debuff 액티브 Weaken 영웅 스킬 도입 후 실효성 급감 — WeakenFactor BalanceConfig 손잡이 미설계 |
| 2026-07-03 | 647bc82 | Dps 패시브 HexRangeBoost 3픽+Tier3 복합 Hex 사거리 ×3.567 — 영웅 AI 타격 우선순위 영구 회피 + MaxHexRangeMul 손잡이 미설계 |
| 2026-07-02 | ea6803e | Debuff 액티브 Bleed '이동/정지 무관' 구현-설계 불일치 — 트리거 조건 결정 요청 |
| 2026-07-01 | 148ae90 | Dps ReplaceReapersToHex 3픽(×2.197)+Tier1(×1.3) 복합 Power ×2.856 — MaxDpsPowerMul 손잡이 미설계 |
| 2026-06-30 | db9b2d7 | Debuff 액티브 Bleed 3픽(30s) + Tier3 EternalBleed(영구) 동시 활성 출혈 합산 -3%/s — MaxBleedRatioPerSec 손잡이 미설계 |
| 2026-06-29 | 07d6dd7 | Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 스포너 출력 5대 — MaxSpawnerSimultaneousOutput 손잡이 미설계 |
| 2026-06-28 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-26 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-24 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — 액티브 트리거 9→5 감소 후 Tier3 도달 난이도 50% 픽집중 · SynergyTierThreshold 손잡이 미설계 |
| 2026-06-23 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-22 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — BalanceConfig MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — BalanceConfig MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-15 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 (영웅 HP 4000 기준 3픽 합산 1.875% — 스킬 도입 후 격차 확대) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Swarm 액티브 TimeStop 발동 중 영웅 스킬(HeroSkillRunner)이 IAttacker.Enabled를 확인하지 않아 계속 시전됨 — TimeStopDuration BalanceConfig 손잡이 미설계

- **카테고리**: Swarm 액티브 / 설계 공백 (영웅 스킬 상호작용 미정의)
- **요지**: TimeStop 카드는 `IAttacker.Enabled = false` + `IMover.Speed = 0f` 로 영웅 이동·근접 공격을 5초간 완전 정지시킨다. 그러나 `HeroSkillRunner.Update()` 는 `IAttacker.Enabled` 를 전혀 확인하지 않아, HP 게이트로 해금된 영웅 스킬(DashStrike / AoeNova / OrbitingBlade)이 TimeStop 중에도 계속 `Tick()` 되고 쿨다운이 0 이하가 되면 그대로 발동한다. "완전 정지" 설명과 실제 동작이 불일치하며, 이 상호작용의 의도가 어느 기획서에도 없다. 추가로 `TimeStopEffect._duration = 5f` 는 SO 인스펙터 필드로만 존재하고 BalanceConfig 중앙 손잡이가 없어 런타임 튜닝 경로가 없다.
- **검증/구현/시너지/데이터**: 5 / 2 / 4 / 5 → 종합 **18**
- **근거**:
  - `TimeStopAura.cs:38-41` — `_mover.Speed = 0f` + `_attacker.Enabled = false`
  - `HeroSkillRunner.cs:52-53` — Update 진입 조건: `_loadout == null || _gate == null || _health == null || _health.IsAlive == false` 만 체크. `IAttacker.Enabled` 미체크
  - `AutoCombatAI.cs:130-132` — 이동·근접 공격은 `_attacker.Enabled == false` 로 보류, 스킬은 별도 컴포넌트라 영향 없음
  - `DashStrikeSkillData.cs:38-52` — `Tick()` 은 `IAttacker.Enabled` 를 보지 않고 쿨다운만 체크
  - `TimeStopEffect.cs:11` — `[SerializeField] private float _duration = 5f` (BalanceConfig 미연결)
- **MVP 범위**: 컨셉 §11.2 ✅ (액티브 카드 12장 + 영웅 스킬은 2026-06-04 사용자 승격)

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**  
   액티브 픽 팝업(30초 트리거)에서 플레이어가 TimeStop 카드를 선택하는 순간 발동된다. 영웅 HP 가 65% 이하(AoeNova 해금)이거나 45% 이하(OrbitingBlade 해금)에서 픽하면 이미 활성화된 스킬이 쿨다운 중이거나 막 발동 직전일 수 있다.

2. **화면 변화**  
   영웅 위치에 TimeStopShield FX(EVisual.TimeStopShield — 5초 지속 시각 이펙트)가 스폰된다. 영웅의 이동 애니메이션이 멈추고 근접 공격 애니도 중단된다. **그러나** 영웅 스킬 FX(`HeroDashFx` 부채꼴, `HeroNovaFx` 구, `HeroOrbitBladeFx`)는 쿨다운이 만료되는 즉시 정상적으로 스폰·재생된다. 플레이어 시점에서 "멈춘 영웅이 DashStrike 부채꼴 이펙트를 뿜는" 시각적 모순이 발생한다.

3. **입력 행동**  
   플레이어는 TimeStop 발동 이후 별도 입력 없이 5초를 기다린다. 카드 선택이 이미 끝났으므로 추가 조작은 없다.

4. **시스템 반응**  
   `TimeStopAura.OnAttached()` 가 즉시 `IMover.Speed = 0`, `IAttacker.Enabled = false` 를 설정한다. 동시에 `HeroSkillRunner.Update()` 는 프레임마다 정상 실행되어 각 스킬 런타임의 `Tick(ctx, Time.deltaTime)` 을 호출한다. 쿨다운이 남은 스킬은 카운트다운이 계속 줄어들고, 0 이하가 되면 `DamageMonstersInCone` / `DamageMonstersInRing` 등을 즉시 호출해 몬스터에 데미지 + 넉백을 입힌다.

5. **반복·재발생 패턴**  
   TimeStop 은 최대 3픽(액티브 9회 중 3회) 가능하다. 3픽이면 게임 내 총 15초(= 5min × 5%)를 "영웅 이동·근접 공격 정지" 상태로 보낸다. 각 5초 정지 구간 내에 스킬이 쿨다운을 소진하면, 정지 중에도 스킬이 1회 이상 발동된다(DashStrike 쿨다운 3s → 5s 정지 중 1회 보장 발동).

6. **종료·해소 조건**  
   `HeroAuraRunner` 가 5초(`_duration`) 후 `TimeStopAura.OnDetached()` 를 호출해 `IMover.Speed` 와 `IAttacker.Enabled` 를 백업값으로 복원한다. 이후 영웅이 이동·근접 공격을 재개한다. 스킬은 TimeStop 중에도, 종료 후에도 계속 Tick 된다.

7. **다른 시스템과 상호작용**  
   - **Bleed 액티브 (07-02 감사)**: "영웅 이동 시" Bleed 트리거 조건이 미결인 상황에서 TimeStop + Bleed 동시 활성이면, Bleed 가 이동 무관으로 구현됐다면 정지 중에도 출혈(-2%/s × 10s)이 계속 진행된다.  
   - **GuardianRage(IronWill) 액티브 (07-06 감사)**: TimeStop + GuardianRage 동시 구간(최대 15s 정지 내 15s IronWill 중첩)에서 영웅 스킬이 발동되면 WispWraithPowerBoostEffect 의 데미지 감소 상태가 유지된 채 스킬이 오히려 더 안전하게 몬스터를 청소하는 "플레이어가 의도하지 않은 무적 구간"이 발생할 수 있다.  
   - **HeroSkillPhaseGate**: TimeStop 발동 시점이 HP 게이트 직후(85%/65%/45%)라면 스킬 쿨다운이 초기값(Cooldown)에서 시작하므로, 5s 정지 중 첫 발동까지 최대 3s(DashStrike)~5s(OrbitingBlade 예상) 지연 가능 — 단, DashStrike(쿨다운 3s)는 5s 정지 중 반드시 1회 이상 발동.

8. **엣지 케이스**  
   - 영웅이 HP 45% 직후(OrbitingBlade 해금) 에 TimeStop 을 픽한 경우: 3개 스킬 모두 활성이며, 정지 5초 내에 DashStrike(쿨다운 3s) 1회 + OrbitingBlade(인터벌 기반, 미확인) 지속 데미지가 발동 → 정지 중 몬스터 다수 사망 가능.  
   - TimeStop 중 영웅 HP 게이트 교차(새 스킬 해금): `HeroSkillPhaseGate.Poll()` 은 `IAttacker.Enabled` 미체크이므로 정지 중에도 새 스킬 해금 + SkillUnlockCutscene 발동(`OnSkillUnlocked?.Invoke`) 가능 — 컷인이 TimeStop FX 와 동시 재생되는 연출 충돌.  
   - `_duration = 5f` 를 특정 SO 에서 수동으로 키울 경우(예: 10f), HeroSkillRunner bypass 시간이 두 배로 늘어나 밸런스 영향 크게 상승. BalanceConfig 손잡이 없으므로 중앙 관리 불가.

9. **유저 정보·피드백**  
   현재 TimeStop FX(TimeStopShield)는 5초 지속 비주얼이 있으나, 영웅 스킬 FX(DashFx 부채꼴 등)가 그 위에 오버레이된다. 유저 입장에서 "영웅이 멈췄는데 공격은 하네?" 라는 혼란이 발생한다. 기획 의도가 "TimeStop 중 스킬은 허용" 이라면 HUD/카드 설명에 "이동·근접 공격만 정지" 로 명시해야 하고, 의도가 "완전 정지"라면 `HeroSkillRunner.Update()` 에 IAttacker.Enabled 체크를 추가해야 한다.

---

### 보류

- **WallOfWisps(Tank 액티브)**: 영웅 주변 4방위 Wisp 즉시 소환이 Spawner ring 시스템을 우회해 글로벌 캡 동작이 미정의. 독립적 이슈이나 이번 회차는 TimeStop 이 종합 점수 우세(18 vs 15).
- **SpawnReapers(Dps 패시브)**: 3픽 + Dps Tier1 복합 스포너 출력 누적이 SpawnPhantoms(06-29 감사) 와 동일 패턴 — 차별성 부족.
- **Fear(Debuff 액티브)**: flee-stabilize-center-pull 설계와 연동하나 종합 점수 낮음(11). Bleed(07-02) 감사의 "이동 조건 미결"이 먼저 해소돼야 Fear+Bleed 콤보 평가 가능.

---

## 3. 과거 감사 대비 차별성

git log 조회 21건 검토 완료.

**가장 유사했던 과거 커밋**: 없음 — 21건 중 "TimeStop", "HeroSkillRunner", "IAttacker.Enabled", "스킬 우회" 를 주제로 한 항목이 없다. (과거 영역: Swarm 패시브 2건·Swarm 액티브 1건, 모두 스폰 주기/이속/Slow 배율 손잡이 이슈)

**content-audit 폴더 파일** `2026-06-12-timestop-fear-duration-cap-balance-config.md` 가 존재한다(git log 검색에는 미노출 — 구 포맷 커밋). 해당 파일은 파일명으로 보아 **"TimeStop·Fear의 _duration 하드코딩 → BalanceConfig 손잡이 미설계"** 를 다뤘을 가능성이 높다. 본 보고의 차별점:

- **구 파일**: TimeStop 지속 시간(`_duration`)이 BalanceConfig 에 없어 중앙 튜닝 불가 — *손잡이 설계 공백*
- **본 보고**: TimeStop 발동 중 `HeroSkillRunner` 가 `IAttacker.Enabled` 를 무시하고 영웅 스킬을 계속 실행 — *상호작용 설계 공백 (버그 or 미명문화 의도)*

두 이슈는 카테고리(TimeStop 카드)가 같으나 **요지**(손잡이 누락 vs 스킬 우회)와 **근거**(BalanceConfig vs HeroSkillRunner.cs)가 전혀 다르다. 본 보고는 구 파일의 _duration 손잡이 제안도 함께 언급(§5)하여 보완 관계를 명확히 한다.

---

## 4. 제외 (범위 밖)

- **DebugAutoPicker 구현**: gameplay-programmer 영역 (QA 훅), 본 루틴 범위 밖.
- **신규 영웅/몬스터/카드 추가**: CLAUDE.md §8 금지 ("신규 영웅·몬스터·카드 리소스 제작 금지").
- **서버 연동 로직 직접 수정**: 이 레포는 클라이언트 연동 코드만 허용 (CLAUDE.md §8).
- **HeroSkillRunner 즉각 수정**: 이 보고는 제안 단계이며 수정은 gameplay-programmer + game-designer 결정 후.

---

## 5. 다음 단계 제안

1. **game-designer 결정 필요**: TimeStop 중 영웅 스킬 발동을 허용(의도적 메커니즘)할지, 차단(완전 정지)할지 명시.
   - **허용** 시 → 카드 설명 텍스트를 "이동·근접 공격 정지 (스킬 유지)" 로 변경. 기획서(`hero-skills.md` §1) 에도 명문화.
   - **차단** 시 → `HeroSkillRunner.Update()` 시작부에 `IAttacker` 확인 로직 추가(`gameplay-programmer` 약 5줄). 혹은 별도 `IHeroStunnable` 인터페이스 도입.
2. **BalanceConfig 손잡이 추가(구 파일 보완)**: `MaxTimeStopDuration` (단일 발동 최대 초, 기본 5f) 를 BalanceConfig 에 추가해 `TimeStopEffect._duration` 을 런타임에 클램핑 — gameplay-programmer 약 3줄.
3. **SkillUnlockCutscene 충돌 명문화**: TimeStop 정지 중 스킬 해금 컷인 동시 발동 시나리오를 `skill-unlock-cutscene.md` 에 명시하거나 컷인 시 TimeStop 강제 종료 정책 결정.
4. 채택 시 game-designer 에게 정식 기획 요청.

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에는 영웅을 5초 동안 꼼짝 못 하게 만드는 "시간 정지" 카드가 있다. 영웅이 멈추면 걷지도, 때리지도 못해서 몬스터들이 마음껏 공격할 수 있어야 한다. 그런데 자세히 코드를 보면, 영웅이 레벨업처럼 자동으로 배우는 특수 공격(부채꼴 돌진, 광역 폭발 등)은 "시간 정지" 카드의 영향을 전혀 받지 않아서, 몸은 멈춰 있어도 특수 공격은 계속 때리고 있다. 마치 동상처럼 굳어 있는 영웅이 눈에서 빔을 쏘는 셈이다. 이게 의도한 재미인지, 아니면 고쳐야 할 버그인지가 결정되지 않았고, 또 "5초"라는 숫자를 게임 밖에서 손쉽게 바꿀 수 있는 설정 창도 없다. 그래서 이번에 제안하는 것은: "시간 정지 중 특수 공격을 허용할 것인지 차단할 것인지 기획서에 명확히 적고, 5초 지속 시간을 중앙 설정으로 뽑아 조정하기 쉽게 만들자"는 것이다.
