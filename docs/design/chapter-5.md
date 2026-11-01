# 챕터 5 "서리 대성당" + 신규 영웅 성기사 (Paladin) - 기능 기획서

> 작성: game-designer · 날짜: 2026-10-18 · 단계: v0.3
> 입력: `CLAUDE.md` §8 ⑦(챕터 5 승격 2026-10-18) · `chapter-4.md`(선례 형식·도출식 적용) · `chapter-3-hero.md` · `chapter-system.md`(§0.4 도출식·상한, §3 규칙 6종, §3.2 임계 이격, E-1) · `hero-skills.md` · `project_lair_concept.md`(§5.2 4축 시너지, 카드 28장 표) · 실제 코드/데이터(`ChapterGate.cs` · `StageProgress.cs` · `ChapterDatabase.cs` · `AutoCombatAI.cs` · `HeroAuraRunner.cs` · `BattleContext.cs` · `PlagueSlowOnHit.cs` · `Card/Auras/*.cs` · `Card/Effects/*.cs` · `Art/Cards/Items/*.asset` · `WorldMapViewModel.cs` · `Chapter.json` · `Stage.json` · `Stage_Rule*.json` · `HeroArchetype.json` · `hero_skills.json` · `balance_config.json` · `Strings_Ko.json`)
> **시안(합의용, 사용자 기열람)**: `.mockups/hero-ch5-paladin.html`(영웅 5단계 · 팔레트 A/B · 스킬 FX · 전투 미니 프리뷰) · `.mockups/scene-bg-ch5-frost.html`(배경, 4장 대비). 메인이 `node .mockups/server.js` 로 `http://localhost:8777/` 제공. **시안의 수치(스테이지 표·보스 트리거)는 초안이며 본 문서 §5.3·§5.2 가 대체한다**(차이는 §5.5).
> **데이터 구조표**: 본 문서 §2 (신규 스키마 2종 `HeroArchetype_Guard` · `HeroArchetype_DebuffResist` - 사용자 승인 게이트 대상)
> **변경 이력 (2026-11-01)**: 몬스터 육성 재화 분리(`monster-growth-currencies.md` §15) 구현에 맞춰 현행화 - 역병귀 적중 효과 둔화(`SlowAura`) → 독(`PlagueToxinAura`, 부착 컴포넌트 `PlagueToxinOnHit`), 독장판(`HeroPoisonAura`/`PoisonAura`) 삭제 → 끈질긴 저주(`LingeringCurse`) 신설, 신앙의 가호 대상·정화의 빛 해제 목록·편성 힌트 1412(`약화·독에 저항합니다. 디버프보다 힘으로 맞서세요.`)·응징 출처·G11 지표 갱신. §1.2 발견 3·§1.3 의 손실 분석은 당시(둔화 기준) 기록이며 현행 값은 각 줄의 "(2026-11-01 현행)" 주석을 따른다.

---

## § 헤더

- **목표**: 마지막 챕터 5(5스테이지, 선형 21~25)와 챕터 영웅 **성기사**(가장 가까운 몬스터와 정면 교전하고, 자신에게 디버프를 건 몬스터를 먼저 응징하며, 디버프 지속·강도를 줄이는 근접 영웅, 신규 AI `Guardian`, 스킬 3종, 5단계 외형), 신규 맵 배경 "서리 대성당"을 기획한다. 규칙은 기존 6종만 재사용한다(신규 규칙 0종). 이 챕터가 열리면 `MaxStage` 25, 월드맵 5번째 탭이 실제 탭이 되며 5챕터 구조가 완성된다.
- **검증 가설**: (1) 영웅이 디버프에 저항하면 **Debuff 빌드가 약해지고 Tank·Dps·Swarm 빌드가 해답이 되는가** - Ch2 Tank 약 -> Ch3 Dps 약 -> Ch4 Swarm 약 -> Ch5 Debuff 약으로 4축이 한 번씩 약점이 된다. (2) 기사(중앙 고정) -> 궁수(카이팅) -> 암살자(약자 사냥+이탈) -> 광전사(군중 돌진) -> 성기사(디버퍼 응징)로 5개 챕터 모두 "다른 판" 체감이 유지되는가. (3) 톱니형 곡선(Ch4 보스 -> Ch5 S1 하강)이 마지막 챕터에서도 진행 동기를 유지하고, 5-5 가 상점 만렙 기준 처치 가능한가.
- **현재 단계 범위 적합성**: **범위 안.** `CLAUDE.md` §8 ⑦ "챕터 5(5-1~5-5): 신규 영웅 직업·맵 배경·스테이지 데이터·규칙 배치(합의용 시안 기준, 세부는 game-designer 기획서에서 확정)"에 해당한다. 신규 규칙 0종. ★ 목표, 악몽(Heat), 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 계속 금지이며 본 문서에 포함하지 않는다. 서버 연동(Firebase 클라이언트 코드만) · 메인 메뉴/세팅 화면 금지는 변경 없음. **신규 UI 위젯 없음**(5번째 탭은 `Chapter.json` row 5 로 구동되는 기존 탭 위젯).
- **핵심 메커니즘**: (a) 고유 패시브 **"신앙의 가호"** - Debuff 축 출처(카드 + 역병귀 적중)의 시한 디버프 지속 x0.6, 영구 디버프 강도(감소분) x0.6. Swarm·Dps 축 카드(`Slow` `TimeStop` `MarkOfDeath`)는 대상이 아니다. (b) `Guardian` AI 는 기사의 최근접 정면 교전에서 중앙 끌림을 빼고 **응징 표적**(디버프를 건 몬스터를 2.0초 동안 강제 표적)을 더한다. (c) 스킬 3종(심판의 망치 / 정화의 빛 / 성역)은 HP 85/65/45% 게이트 재사용. (d) 난이도는 eq 배수 5-1 1.71 -> 5-5 2.70(Ch5 상한 2.80 이내, 상점 효과 100% 소진 시점), 규칙은 봉인 x2 · 안개 · 샘 · 보스 페이즈 덫 0.75 + 결계 0.35.

---

## 1. 컨셉 상세화 (테마·영웅 확정분)

### 1.1 확정 컨셉 (시안 기준)

| 구분 | 기사 (Ch1) | 궁수 (Ch2) | 암살자 (Ch3) | 광전사 (Ch4) | **성기사 (Ch5)** |
|---|---|---|---|---|---|
| 전투 방식 | 정면 근접, 중앙 끌림 | 카이팅(후퇴) | 습격 + 슬립 이탈 | 최대 군집 돌진 + 범위 베기 | **최근접 정면 교전 + 디버퍼 응징, 중앙 끌림 없음** |
| aiType | Melee | Kiter | Skirmisher | Charger | **Guardian (신규)** |
| 고유 특성 | - | - | - | 범위 베기 | **신앙의 가호(디버프 저항)** |
| 빌드 압박 | - | Tank 약 | Dps 약 | Swarm 약 | **Debuff 약** |
| 맵 / 발광색 | 지하 묘지 / 소울 그린 | 어둠의 숲 / 라임 | 그림자 성채 / 바이올렛 | 불꽃의 용광로 / 엠버 주홍 | **서리 대성당 / 서리 청 `150,220,255`** |

시안 대비 바뀐 점은 §5.5 에 모은다.

### 1.2 고유 패시브 근거 확인 - 영웅에게 실제로 걸리는 디버프 (코드·데이터 실측)

영웅에게 붙는 효과는 모두 `HeroAuraRunner.Attach(aura, duration)` 를 거친다. 경로는 두 갈래다: 카드 효과 -> `BattleContext.ApplyHeroAura` -> `Attach`, 그리고 **`PlagueSlowOnHit.HandleHit` -> `runner.Attach` 직접 호출**(BattleContext 우회). `Attach` 는 같은 aura 타입이 이미 있으면 새 인스턴스를 버리고 `Remain += duration` 으로 연장한다(`IDistinctHeroAura` 예외). `duration < 0` 은 무기한.

| 출처 | 축 | aura | 지속(에셋 실측) | 효과 | 패시브 대상 |
|---|---|---|---|---|---|
| 역병귀 적중 (`PlagueToxinOnHit`, 2026-10-29 `PlagueSlowOnHit` 에서 교체) | Debuff (몬스터) | `PlagueToxinAura` | 3초/적중, 재적중 시 3초로 **갱신**(연장 누적 아님) | 독 1중첩/적중(최대 3, T1 이면 5), 1초마다 중첩 x 역병귀 공격력 x 독 피해 배율(`PlagueSlowBoost` "역병의 손길" x1.4/픽) | **대상 (시한)** |
| ~~`HeroPoisonAura` (P)~~ | Debuff | ~~`PoisonAura`~~ | - | 2026-10-29 카드 삭제(대체 카드 `LingeringCurse` 는 디버프가 아니라 Debuff 축 시한 디버프 지속 x(1 + 0.25n) 보정 - 신앙의 가호 x0.6 과 같은 부착 지점에서 곱) | - |
| `Fear` (A) | Debuff | `FearAura` | 3초 | 도주 | **대상 (시한)** |
| `Bleed` (A) | Debuff | `BleedAura` | 10초 | 매 1초 최대 HP 2% (코드상 이동 여부 무관) | **대상 (시한)** |
| `Weaken` (A) | Debuff | `WeakenAura` | 10초 | 공격력 x0.5 | **대상 (시한)** |
| `HeroAttackDown` (P) | Debuff | `HeroAttackDownAura` | 무기한 | 공격력 x0.75 | **대상 (영구 - 강도)** |
| Debuff 시너지 T2 | Debuff | `HeroAttackDownAura` | 무기한 | 공격력 x0.85 | **대상 (영구 - 강도)** |
| Debuff 시너지 T3 | Debuff | `EternalBleedAura` | 무기한 | 이동 중 매 1초 최대 HP 1% | **대상 (영구 - 강도)** |
| `Slow` (A) | **Swarm** | `SlowAura` | 10초 | 이동 x0.5 + 몬스터 가속 | 제외 |
| `TimeStop` (A) | **Swarm** | `TimeStopAura` | 5초 | 완전 정지 | 제외 |
| `MarkOfDeath` (A) | **Dps** | `MarkOfDeathAura` | 5초 | 받는 피해 x1.5 | 제외 |

**발견 1 - 시안 정의("지속 x0.6")만으로는 Debuff 압박이 반쪽이다.** Debuff 축 7장 중 가장 센 축 효과 3개(`HeroAttackDown` P, T2, T3)가 무기한(`-1`)이라 지속 배율이 아무 일도 하지 않는다. 지속 배율만 쓰면 축 패시브 4장 중 `HeroAttackDown` 이 그대로 남고, 시너지 T2/T3 도 손실이 0 이다.

**발견 2 - aura 타입으로 정의하면 다른 축이 함께 약해진다.** Swarm 의 `Slow` 카드는 역병귀 둔화와 같은 `SlowAura` 클래스이고, `TimeStop`(Swarm)·`MarkOfDeath`(Dps)도 같은 `ApplyHeroAura` 경로다. "영웅이 받는 모든 디버프"로 정의하면 Swarm 2장·Dps 1장이 함께 약해져 "Debuff 만 약" 가설이 흐려진다.

**발견 3 - "디버프 3개 이상" 정화 조건은 사실상 발동하지 않는다.** 액티브 카드는 30초마다 1장이고, 패시브 적용 후 시한 디버프 지속은 Fear 1.8초 · Bleed 6.0초 · Weaken 6.0초다. 액티브 디버프끼리는 겹치지 않으므로 동시에 붙는 대상 디버프는 보통 "역병귀 둔화 + 액티브 1종" = 2종이고, 3종은 패시브 픽 직후 독장판(3.0초)까지 겹쳐야 나온다. 그래서 정화 조건을 **2종 이상**으로 낮춘다(§4). (2026-11-01 현행: 독장판이 삭제되고 역병귀 둔화가 독으로 바뀌어 현실적 조합은 "역병귀 독 + 액티브 1종" = 2종이다. 정화 조건 2종 결론은 그대로.)

**패시브 정의 대안**

| 안 | 정의 | Debuff 축 영향 | 다른 축 영향 | 평가 |
|---|---|---|---|---|
| **A. 출처 한정 + 시한 지속 x0.6 + 영구 강도 x0.6 - 권장** | Debuff 축 출처 8개(위 표 "대상")만. 시한은 지속 x0.6, 영구는 **감소분** x0.6 (공격력 배율 f -> 1 - (1 - f) x 0.6, 출혈 비율 r -> r x 0.6) | 7장 전부 + 시너지 T1~T3 모두 약화 | 0 | 축 전체가 고르게 약해지고 다른 축은 손대지 않아 가설이 선명하다 |
| B. 출처 한정 + 시한 지속 x0.6만 (시안 그대로) | Debuff 축 시한 5개만 | `HeroAttackDown` P·T2·T3 손실 0 | 0 | 구현은 가장 싸지만 Debuff 가 "약"이 아니라 "조금 덜 셈" 수준. G13 에서 압박 미달 위험 |
| C. 타입 기준 전부 x0.6 (지속 + 강도) | 영웅 aura 전체 | 전부 약화 | Swarm `Slow`·`TimeStop`, Dps `MarkOfDeath` 약화 | 구현이 가장 단순하나 Swarm·Dps 도 약해져 "Debuff 약" 신호가 섞인다 |

**권장: A.** 패시브 이름은 **"신앙의 가호"**로 정한다(시안 "받는 디버프(둔화·독·공포·약화) 지속 x0.6" 에 출혈·영구 강도·출처 한정을 더한 확장).

### 1.3 Debuff 축 카드별 손실 (안 A 기준, 5-1 기준값)

5-1 실효 공격력 60 · 쿨다운 1.1 -> 기본 DPS 54.5 (§3.2).

| 카드 / 시너지 | 기본 | 성기사 | 손실 |
|---|---|---|---|
| `PlagueSlowBoost` · `SpawnPlagues` · T1 (역병귀 둔화) | 적중당 1.5초 연장, 역병귀 쿨다운 1.0 이라 공격 중 매초 +0.5초씩 쌓여 역병귀가 죽은 뒤에도 남음 | 적중당 0.9초 -> 공격 중 커버율 0.9 / 1.0 = 90%, 역병귀 사망 0.9초 뒤 해제. **응징 표적**(§3.3)으로 역병귀(HP 50)가 60 피해 1타에 우선 처치 | 둔화가 "누적 지속"에서 "공격 중에만 90%"로 바뀌고 역병귀 생존 시간이 짧아짐. **(2026-11-01 현행)** 효과가 독으로 바뀌어 독 지속 3초 → 1.8초(x0.6, 중첩·피해는 보정 없음). 역병귀 쿨다운 1.0 이라 공격 중에는 매 적중 갱신으로 계속 유지되고, 역병귀 사망·이탈 후 1.8초 뒤 해제. 응징 표적 규칙은 그대로 |
| ~~`HeroPoisonAura` (P)~~ | 5 DPS x 5초 = 25 | 5 x 3.0초 = 15 | -40% (-10). **(2026-11-01 현행)** 카드 삭제. 대체 `LingeringCurse` 3픽이면 시한 디버프 지속 x1.75 x0.6 = x1.05 로 신앙의 가호를 상쇄(`monster-growth-currencies.md` §15.3·게이트 PQ5) |
| `HeroAttackDown` (P) | 공격력 x0.75 (감소 25%) | 1 - 0.25 x 0.6 = **x0.85** (감소 15%) | 감소분 -40% |
| `Fear` (A) | 3.0초 도주 | 1.8초 | -1.2초 |
| `Bleed` (A) | 2% x 10초 = 최대 HP 20% | 2% x 6초 = 12% | -8%p |
| `Weaken` (A) | x0.5 10초 -> 깎는 피해 54.5 x 0.5 x 10 = 272.5 | 6초 -> 54.5 x 0.5 x 6 = 163.5 | -109 (-40%) |
| 시너지 T2 (`HeroAttackDown` 자동) | x0.85 (감소 15%) | 1 - 0.15 x 0.6 = **x0.91** | 감소분 -40% |
| 시너지 T3 (영구 출혈) | 이동 중 1%/초 | 0.6%/초 | -40% |
| (65% 이후) 정화의 빛 | - | 대상 시한 디버프 2종 이상이면 12초마다 전부 해제 | 남은 시한 디버프 추가 손실 |

결론: Debuff 축 7장과 시너지 T1~T3 이 모두 40% 안팎 손실을 받고, Tank·Dps·Swarm 축 카드는 손실 0 이다. Debuff 약 압박이 데이터상 성립한다. 반대로 지나치면 Debuff 빌드가 죽은 빌드가 되므로 G13 에 하한(Debuff 빌드 승률 >= 전체 평균의 0.5배)을 둔다(§9.3).

### 1.4 4축 빌드 가위바위보 (가설, G11~G13 으로 검증)

몬스터 처치 타수(HP: Phantom 30 · Plague 50 · Hex 60 · Reaper 100 · Wisp 200 · Wraith 500, 상점 종족 강화·카드 HP 배율 미적용 기준):

| 종 | HP | 5-1 기본 공격(60) | 5-5 기본 공격(83) | 심판의 망치(110) |
|---|---|---|---|---|
| Phantom | 30 | 1 | 1 | 1 |
| Plague | 50 | 1 | 1 | 1 |
| Hex | 60 | 1 | 1 | 1 |
| Reaper | 100 | 2 | 2 | 1 |
| Wisp | 200 | 4 | 3 | 2 |
| Wraith | 500 | 9 | 7 | 5 |

검산: 100/60 = 1.67 -> 2, 200/60 = 3.33 -> 4, 500/60 = 8.33 -> 9. 100/83 = 1.20 -> 2, 200/83 = 2.41 -> 3, 500/83 = 6.02 -> 7. 망치 200/110 = 1.82 -> 2, 500/110 = 4.55 -> 5.

| 축 | 성기사 상대 | 이유 |
|---|---|---|
| **Debuff** | **약** | §1.3: 축 효과 전체 40% 안팎 손실 + 역병귀 응징(1타 처치) + 정화 |
| Tank | 강 | 단일 대상 근접이라 Wisp(3~4타)·Wraith(7~9타)가 영웅을 오래 붙잡는다. 이동 2.9 로 가장 느려 진로 방해가 잘 먹힌다 |
| Dps | 중립 이상 | 성역(받는 피해 x0.6)으로 일부 상쇄되지만 33% 가동률(6.0 / 18.0)이라 나머지 시간엔 Reaper·Hex 단발 피해가 그대로 들어간다. Dps 축 `MarkOfDeath` 는 패시브 제외 |
| Swarm | 중립 | 기본 공격은 단일 대상이라 다수에 약하지만 심판의 망치(반경 2.4, Phantom 1타)가 6초마다 군집을 지운다. Swarm 축 `Slow`·`TimeStop` 은 패시브 제외 |

어긋나면 §2.2 `HeroArchetype_DebuffResist` 두 배율(`durationMul` / `penaltyMul`)만 조정한다.

---

## 2. 데이터 구조표 (사용자 승인 게이트 - 필수 확인)

> Rule 02 §11-1 적용. 챕터 5 의 챕터·스테이지·규칙 row 는 **기존 스키마에 값(row)만 추가**한다. 신규 스키마는 `HeroArchetype_Guard` · `HeroArchetype_DebuffResist` 2종뿐이다. `HeroArchetype.json` / `Chapter.json` / `Stage*.json` / `hero_skills.json` / `Strings_Ko.json` 은 필드 변경 없이 row 만 늘어난다.

### 2.1 엔티티 / 관계 트리 (변경분만)

```
Chapter (기존)         ChapterRow         <- row 5 추가 (마지막 챕터)
 +-- Stage (기존)      StageRow           <- (5,1)~(5,5) 5 row 추가
 |     +-- Stage_Rule / _Param / _Trigger (기존) <- 5-2~5-5 row 추가 (§5.2)
 +-- (Chapter.heroId) --> HeroArchetype (기존)  <- row "Paladin" 추가 (aiType = Guardian)
                           +-- HeroArchetype_Kite (기존, Archer 만)
                           +-- HeroArchetype_Skirmish (기존, Assassin 만)
                           +-- HeroArchetype_Charge / _Cleave (기존, Berserker 만)
                           +-- HeroArchetype_Guard (신규, 선택적 관계)  HeroArchetype_GuardRow
                           |        <- aiType=Guardian 인 영웅만 존재 (Paladin 1 row)
                           +-- HeroArchetype_DebuffResist (신규, 선택적 관계)  HeroArchetype_DebuffResistRow
                           |        <- 디버프 저항 패시브를 가진 영웅만 존재 (Paladin 1 row)
                           +-- (스킬 로드아웃) hero_skills.json loadout row (heroId="Paladin")
SO (Object 참조): HeroStageVariantConfig <- ChapterId=5 엔트리 5개 추가
```

**신규 스키마를 둘로 가른 이유 (§11-1 단계 3)**: "응징 표적 AI 튜닝"(`Guard`)은 **이동·표적 방식**이고, "디버프 저항"(`DebuffResist`)은 **받는 효과 보정**이다. 저항은 AI 가 달라도 성립하므로 서로 독립이며(Ch4 `Charge`/`Cleave` 분리와 같은 판단), 둘 다 일부 영웅에만 존재하는 선택적 관계라 `HeroArchetypeRow` 에 직접 얹으면 다른 4영웅에 1.0/0 sentinel 이 생긴다. 이름은 `HeroArchetype_<관계명>` 규칙(§11-1-5). 핵심 엔티티 한 문장: `HeroArchetype_GuardRow` = "응징 표적 AI 를 쓰는 영웅 직업 하나의 표적 튜닝 값", `HeroArchetype_DebuffResistRow` = "디버프 저항 패시브를 가진 영웅 직업 하나의 저항 배율".

### 2.2 신규 row 필드 표

**HeroArchetype_GuardRow** - `HeroArchetype_Guard.json` (EData `HeroArchetype_Guard`)

| 필드 | 타입 | 키/관계 | 설명 | Paladin 초안값 |
|---|---|---|---|---|
| `heroId` | string | PK -> `HeroArchetypeRow.heroId` (aiType=Guardian) | | "Paladin" |
| `retributionLockSec` | float | | 응징 표적 유지 시간 (이 동안 일반 표적 교체 금지) | 2.0 |

**HeroArchetype_DebuffResistRow** - `HeroArchetype_DebuffResist.json` (EData `HeroArchetype_DebuffResist`)

| 필드 | 타입 | 키/관계 | 설명 | Paladin 초안값 |
|---|---|---|---|---|
| `heroId` | string | PK -> `HeroArchetypeRow.heroId` | | "Paladin" |
| `durationMul` | float | | 대상 시한 디버프의 부착·연장 지속 배율 (0 초과 1 이하) | 0.6 |
| `penaltyMul` | float | | 대상 영구 디버프의 감소분 배율 (0 이상 1 이하). 공격력 배율 f -> 1 - (1 - f) x `penaltyMul`, 출혈 비율 r -> r x `penaltyMul` | 0.6 |

대상 출처 목록(§1.2 "대상" 8개)은 데이터가 아니라 규칙으로 고정한다(콘텐츠 튜닝값이 아닌 설계 정의).

**HeroArchetypeRow 추가 row** (기존 스키마)

| heroId | nameKey | hpScale | powerScale | range | cooldown | moveSpeed | aiType |
|---|---|---|---|---|---|---|---|
| Paladin | 1025 (성기사) | 1.10 | 0.85 | 1.6 | 1.1 | 2.9 | Guardian |

**ChapterRow 추가 row**

| chapterId | nameKey | themeTextKey | heroId | backdropKey |
|---|---|---|---|---|
| 5 | 1005 | 1015 | Paladin | Battle_Backdrop_FrostCathedral |

**StageRow 추가 5 row**: §5.3 표. **Stage_Rule / Param / Trigger 추가 row**: §5.2 표. **신규 스킬 `$type` 3종**: §4.

### 2.3 Strings_Ko id 배정 (규칙: 챕터명 100x · 테마 101x · 영웅명 102x · 스테이지명 11x1~11x5. 1005 · 1015 · 1025 · 1141~1145 미사용 확인)

| id | 값 |
|---|---|
| 1005 | 서리 대성당 |
| 1015 | 성기사단의 정화 |
| 1025 | 성기사 |
| 1141 | 얼어붙은 회랑 |
| 1142 | 침묵의 성가대석 |
| 1143 | 고해의 예배당 |
| 1144 | 얼음 성물 보관소 |
| 1145 | 서리 대주교의 제단 |

규칙명(1201~1206)·설명(1221~1226)·UI 문구(1301~1326)는 기존 id 재사용. 5번째 탭이 실제 탭이 되면 `1312 "{0}장 ???"` 는 출시 빌드에서 쓰이지 않게 되지만 `ChapterSlotCount`(5)보다 row 가 적은 테스트 DB 경로가 계속 쓰므로 **삭제하지 않는다**. 스킬 표시명 "심판의 망치 / 정화의 빛 / 성역"은 규칙명(성수 샘 등)과 겹치지 않는다(`hero_skills.json` `displayName`, Strings id 불필요).

---

## 3. 성기사 영웅 (Paladin)

### 3.1 직업 개요

- 콘셉트: 서리 대성당을 지키는 해골 성기사. 몸 앞을 가리는 큰 연 방패 + 한 손 철퇴 + 날개 투구 + 십자 휘장 + 바닥까지 끌리는 망토. 다섯 직업 중 유일하게 방패를 든다.
- 기사처럼 최근접 몬스터와 정면으로 싸우지만 **중앙으로 돌아가지 않고**(`_centerPullEnabled` off), 자신에게 디버프를 건 몬스터를 먼저 응징한다. 이동은 다섯 중 가장 느리다(2.9).

### 3.2 스탯 틀 (초안 - G0/G1 검증 후 확정)

| 항목 | 값 | 기사 / 궁수 / 암살자 / 광전사 | 근거 |
|---|---|---|---|
| hpScale / powerScale | 1.10 / 0.85 | 1.00/1.00, 0.80/0.80, 0.70/0.70, 1.25/0.60 | 방패 영웅이라 기사보다 두껍게, 단일 대상이므로 광전사(0.60)보다 높고 기사보다 낮게 |
| range / cooldown / moveSpeed | 1.6 / 1.1 / 2.9 | 1.5/1.0/3.0, 6.0/1.0/3.2, 1.6/0.7/4.2, 1.8/1.2/3.4 | 철퇴 사거리 +0.1, 무거운 내려찍기로 쿨다운 1.1, 방패 무게로 가장 느림 |
| aiType | Guardian (신규) | Melee / Kiter / Skirmisher / Charger | §3.3 |
| 패시브 | 신앙의 가호 | - | §1.2 안 A |
| 기본 공격 | 근접 단일 대상, 사거리 1.6 | | 모션 0.6초, `OnAttackStrike` 0.200s(F3), `OnAttackEnd` 0.580s(F8) |

**스테이지별 실효 공격력** (`StageProgress.ScaleHeroStat(50, 0.85, powerMultiplier)` - decimal 1회 round-half-up): `powerMultiplier` 1.40 / 1.50 / 1.65 / 1.80 / 1.95 -> **60 / 64 / 70 / 77 / 83**. 검산: 50 x 0.85 = 42.5. x1.40 = 59.5 -> 60 (**.5 경계**), x1.50 = 63.75 -> 64, x1.65 = 70.125 -> 70, x1.80 = 76.5 -> 77 (**.5 경계**), x1.95 = 82.875 -> 83. DPS = 피해 / 1.1 = 54.5 / 58.2 / 63.6 / 70.0 / 75.5 (60/1.1 = 54.55, 64/1.1 = 58.18, 70/1.1 = 63.64, 77/1.1 = 70.00, 83/1.1 = 75.45). 선례대로 기사 같은 인덱스 스테이지 DPS(50 / 55 / 60 / 67.5 / 75)와 비교하면 109% / 106% / 106% / 104% / 101%. G11 통과 범위는 이 중심값(101~109%)을 감싸 기사의 95~120% 로 둔다.

### 3.3 AI - Guardian 사양

**상태**

| 상태 | 진입 | 행동 | 해제 |
|---|---|---|---|
| 대기 | 필드에 몬스터 없음 | 제자리 정지 (중앙 복귀 없음) | 몬스터 등장 -> 이동 |
| 이동 | 표적 거리 > `range`(1.6) | 표적 방향으로 `moveSpeed`(2.9) 이동 | 거리 <= 1.6 -> 교전 |
| 교전 | 표적 거리 <= 1.6 | 정지 + 단일 공격(쿨다운 1.1). 기존 교전 히스테리시스(`_engageBuffer`) 그대로 | 표적 사망 -> 표적 재선택. 사거리 + 버퍼 이탈 -> 이동 |
| 응징 (표적 규칙) | 몬스터 M 이 영웅에게 **대상 디버프를 부착**(현행 출처는 역병귀 적중 독뿐(2026-10-29 둔화에서 교체) - 역병귀 사거리 1.0 이라 가해자는 항상 영웅 곁이므로 거리 조건을 두지 않는다) | 표적을 M 으로 즉시 교체하고 `retributionLockSec`(2.0) 동안 고정(일반 표적 히스테리시스 `_retargetMargin` 1.5 무시). 이동/교전 상태는 위 규칙 그대로 | M 사망 · 2.0초 경과 -> 일반 표적 규칙(최근접 + `_retargetMargin` 히스테리시스) |

- 일반 표적: 기사와 같은 최근접 + 거리 기준 고정 표적(`_retargetMargin` 1.5).
- 응징 중 다른 몬스터가 다시 디버프를 걸면 표적을 그 몬스터로 바꾸고 2.0초를 새로 잰다(가장 최근 가해자 우선).
- 카드 출처 디버프(Fear·Bleed·Weaken·`HeroAttackDown`, 독장판은 2026-10-29 삭제)는 가해 몬스터가 없으므로 응징을 발동하지 않는다.
- 공포(`FleeMode`)가 걸리면 기존 순서대로 FleeMode 가 우선한다(성기사는 1.8초로 짧아짐).

**AI 대안 비교 (디버프 반응 방식)**

| 안 | 내용 | 평가 |
|---|---|---|
| **A. 응징 표적 - 권장** | 위 표. 신규 AI 1종, 튜닝 1필드 | 기사와 "무엇을 먼저 치는가"가 달라 다른 판이 되고, Debuff 축 몬스터(역병귀)를 직접 압박한다. Ch4 §1.2 가 방패병(Warden)을 "기사와 겹침"으로 기각한 사유를 해소 |
| B. `Melee` 재사용 + 중앙 끌림 off | 신규 AI 0. 디버프 반응은 패시브·정화 스킬만 | 가장 싸지만 기사와 차이가 중앙 끌림 하나뿐이라 Ch4 Warden 기각 사유가 그대로 남는다 |
| C. 디버프 시 방패 자세 | 대상 디버프가 붙어 있는 동안 정지 + 받는 피해 x0.8 | 디버프가 오히려 영웅을 단단하게 만들어 플레이어가 "디버프를 걸었더니 손해"를 이해하기 어렵다. 정지가 Fear/Slow 체감과 섞임 |

**`AutoCombatAI` 충돌 점검 포인트 (gameplay-programmer 필독)**

| # | 기존 로직 | 충돌/주의 | 처리 방침 |
|---|---|---|---|
| 1 | Update 게이트 순서: 사망 -> TimeStop(`IAttacker.Enabled`) -> 도약 -> 스폰 게이트 -> `IsAttacking` -> 타깃 없음 -> 고정 타깃 -> FleeMode -> 중앙 끌림 -> Kite/Skirmish/Charge -> 교전/이동 | Guardian 은 별도 이동 분기가 없고 **표적 해석 단계**만 바뀐다 | 응징 표적은 고정 타깃 해석(`ResolveStickyTarget`) 자리에서 덮어쓴다. 이후 교전/이동은 기존 Melee 경로. `aiType == Guardian` 일 때만 활성(`GuardSettings` 주입), 다른 4영웅 동작 불변 |
| 2 | 중앙 끌림 `_centerPullEnabled` | 성기사는 끌림 없음 | 프리팹 플래그 off (Charger 처럼 코드 강제가 아니라 프리팹 값) |
| 3 | `PlagueSlowOnHit.HandleHit(IHealth target)` 는 가해 몬스터를 영웅에 전달하지 않음 (2026-10-29 `PlagueToxinOnHit` 로 교체 - 같은 요구 적용) | 응징 표적에 가해자 정보가 필요 | 영웅 쪽에서 "디버프를 건 몬스터"를 알 수 있는 통지가 필요하다는 요구만 고정. 방식은 gameplay-programmer 판단 |
| 4 | `HeroAuraRunner.Attach` 연장(`Remain += duration`) | 패시브는 첫 부착뿐 아니라 **연장분에도** 적용돼야 함 | 대상 출처의 `duration` 인자 자체를 x0.6 해서 넘기면 연장도 자동으로 x0.6. 두 부착 경로(BattleContext·`PlagueToxinOnHit` 직접 호출, 2026-10-29 이전 `PlagueSlowOnHit`) 모두 적용 |
| 5 | `SlowAura` 공유 슬롯 | (2026-10-29 이전) 역병귀 둔화(대상)와 Swarm `Slow` 카드(제외)가 같은 타입이라 한 슬롯에 합쳐짐 | **(현행)** 역병귀는 별도 타입 `PlagueToxinAura` 를 쓰므로 `SlowAura` 출처는 Swarm `Slow` 카드뿐이다. 공유 슬롯 문제 해소. 정화 처리는 §4 · E-C5-1 |
| 6 | 영구 디버프 강도 | `HeroAttackDownAura`(P·T2)·`EternalBleedAura`(T3) 는 생성 시 배율·비율을 받음 | 대상 영웅이면 생성 배율을 §2.2 `penaltyMul` 식으로 보정 (0.75 -> 0.85, 0.85 -> 0.91, 0.01 -> 0.006) |
| 7 | `HeroHologramMode`(마을 쇼케이스) | 성기사도 쇼케이스 영웅 | 마을에서는 전투·AI 컴포넌트 off(다른 4영웅과 같은 경로) |
| 8 | `BattleController` 영웅별 분기 | Charger 주입(675행 인접)·로드아웃 맵(837행)·`WarmVisual`(1609행) | §8 체크리스트 |

### 3.4 클립 사양 (시트 slash01 행)

- 대기(`idle`): 방패를 몸 앞에 세우고 철퇴를 오른쪽 아래로 늘어뜨린 자세.
- 공격(`slash01`): 9프레임 x 15fps = 0.600s. F0 철퇴 들기 -> F1~F2 머리 위로 크게 감기 -> **F3 앞으로 내려찍기 = strike**(이벤트 0.200s: 0.200 x 15 = 3.0 -> F3) -> F4 성광 충격 링(시트 프레임에 그려진 연출, 추가 피해 없음) -> F5~F7 복귀 -> F8 = 대기 F0 와 같은 포즈. `OnAttackEnd` 0.580s(F8 구간 0.533~0.600s). 방패는 공격 내내 앞을 가린다. 쿨다운 1.1 중 0.5초 여유(1.1 - 0.6).
- 이동: `walk`(8프레임 10fps) / `run`(8프레임 14fps). 스킬 3종은 `slash01`/`idle` 클립 + FX 로 표현하며 별도 클립은 만들지 않는다.
- 시트 규격은 기존 4영웅과 같다: 셀 72px, 20열 x 9행(1440x648), 행 순서 idle/walk/run/slash01/slash02/stab/hit/death/spawn, pivot (0.4236, 0.2917), PPU 31. `slash02`·`stab` 행은 `slash01` 행 프레임 복제(D-C4-6 과 같은 방식, 결정 항목 D-C5-7).

### 3.5 5단계 외형 (`HeroStageVariantConfig` ChapterId=5)

기존 규약(단계별 독립 시트 베이크, 2단계 림 `#F8FAFC`, 5단계 림 `#FDE047`, 발광 3~5단계, 5단계만 루트 스케일). 시트 파일: `Paladin_Sheet.png`(5-1) + `Paladin_Sheet_S2.png` ~ `Paladin_Sheet_S5.png` 와 각 `_Emission.png` 5장, `Paladin_SheetSpec.json` 1개. `tiers` = `{Paladin_Sheet:0, Paladin_Sheet_S2:1, Paladin_Sheet_S3:2, Paladin_Sheet_S4:3, Paladin_Sheet_S5:4}`.

| 스테이지 | 외형 (시안 PARTCHG) | `Tier` | `UseEmission` | `EmissionIntensity` | `ScaleMultiplier` | A안 몸 색 / 림 / 발광 | B안 몸 색 / 림 / 발광 |
|---|---|---|---|---|---|---|---|
| 5-1 얼어붙은 회랑 | 사슬 두건 · 잿빛 십자 휘장 · 사슬 갑옷 · 짧은 망토 · 둥근 목방패(금 테) · 나무 곤봉 | 0 | false | 0 | 1.00 | `#9CA3AF` 잿빛 은 / 흑(#0A0D14) / 없음 | `#6B88AA` 잿빛 청 / 흑 / 없음 |
| 5-2 침묵의 성가대석 | 코가드 투구 + 깃털(+5셀) · 연 방패(금 십자) · 날 철퇴 · 긴 망토 · 금 어깨 장식 · 철 장화 | 1 | false | 0 | 1.00 | `#CBD5E1` 백은 / `#F8FAFC` / 없음 | `#4B7BC8` 사파이어 / `#F8FAFC` / 없음 |
| 5-3 고해의 예배당 | 통투구(십자 눈구멍) + 작은 날개(+7셀) · 문장 발광 방패 · 발광 철퇴 · 망토 털 깃 · 서리 입자 | 2 | true | 1.5 | 1.00 | `#E2E8F0` 서리 백은 / 흑 / `#7DD3FC` | `#60A5E0` 서리 사파이어 / 흑 / `#7DD3FC` |
| 5-4 얼음 성물 보관소 | 큰 날개 투구(+10셀) + 후광 · 대형 연 방패 · 대형 철퇴 · 이중 망토 · 금 견갑 | 3 | true | 2.2 | 1.00 | `#F1F5F9` 순백 + 금 / `#FDE68A` / `#A5F3FC` | `#8CC4EC` 빙청 + 금 / `#FDE68A` / `#A5F3FC` |
| 5-5 서리 대주교의 제단 | 날개 왕관(+15셀) + 큰 후광 · 태양 문장 거대 방패 · 성광 전쟁망치 · 날개 망토 · 금 휘장 테 · 바닥 성광 오라 | 4 | true | 3.2 | **1.30** | `#FFFFFF` 광휘 + 금 / `#FDE047` / `#FDE047` | `#35577F` 심청 + 금 / `#FDE047` / `#FDE047` |

- **스케일 1.30 근거**: 기사 1.4 · 궁수 1.25 · 암살자 1.20 · 광전사 1.35. 성기사는 최근접 근접이라 콜라이더가 커져도 표적 도달이 빨라질 뿐 손해가 적고, 큰 방패·날개 실루엣이 이미 넓어 1.30 으로 기사·광전사보다 낮춘다. G11 의 "표적 도달 시간"으로 확인한다.
- **실루엣**: R2'(돌출 >= 4/6/9/14 텍셀)·R3'(증가율 >= 10/18/30/45%)를 같은 값으로 적용한다. 판정은 시트 베이크 후 최종 실측으로 한다(미달 시 날개·후광·방패 크기 보완).
- **다섯 직업 구분**: 기사 = 대각 직선 검 + 낮은 캡, 궁수 = 세로 호 활 + 뾰족 후드, 암살자 = 쌍단검 + 복면 + 목도리, 광전사 = 머리 위로 솟은 도끼 + 뿔 + 두꺼운 견갑, **성기사 = 몸 앞을 가리는 연 방패 + 날개 투구 + 바닥까지 끌리는 망토**. 몸 정면 실루엣이 방패로 가장 넓다.

**팔레트 대안 (사용자 선택 필요, 결정 항목 D-C5-3)** - 색상각은 시안 표가 hex 에서 직접 계산한다. 기존 점유 색상각: 기사 0°·142°·199°·271° / 궁수 36.4°·37.7°·82.7°·328.6° / 암살자 171.2°·238.7°·300° / 광전사 112.8°·220.0°·213.1°·27.0°·13.9°.

| 안 | 내용 | 기존 영웅과 최소 색상각 | 장점 | 단점 |
|---|---|---|---|---|
| **A. 백은 + 금 (무채색 몸) - 권장** | 몸은 백은 명도 램프 5단계, 금 장식·서리 발광만 유채색 | 몸: 전부 무채색(판정 제외). 발광: 5-3 `#7DD3FC` 199.4° = 기사 199° 와 0.4°, 5-4 `#A5F3FC` 186.2° = 기사와 12.8°, 5-5 `#FDE047` 50.4° = 궁수 37.7° 와 12.7° (발광은 면적이 작아 판정 완화) | 몸 색 충돌 0. 어두운 청색 배경(`#05080d` · 바닥 `#16202c`/`#223142`)에서 흰 몸이 가장 잘 보인다 | ① 인접 단계 몸 명도차가 162 -> 211 -> 231 -> 244 -> 255 로 49 / 20 / 13 / 11 이라 3~5단계는 실루엣·발광에 의존 ② 5-3 발광 199.4° 가 **배경 발광색(서리 청 200°)과 같은 색**이라 발광이 배경에 묻힐 위험. 보완: 5-3 발광만 금 `#FDE68A`(48°)로 바꾸는 변형을 준비 |
| B. 사파이어 휘장 | 휘장·망토를 사파이어 청 램프로 | 5-1 `#6B88AA` 212.4° = 광전사 213.1° 와 0.7°, 5-2 `#4B7BC8` 217.0° = 광전사 220.0° 와 3.0° (25° 미달 다수) | 한눈에 "서리 성기사" | 광전사 4-3 강청과 거의 같은 색이고, 몸 색까지 배경과 같은 청색 계열이라 배경 대비가 A 보다 낮다 |

권장 이유: B 는 기존 영웅과 몸 색 충돌(0.7°·3.0°)과 배경 동색 문제를 동시에 갖는다. A 는 몸 색 충돌이 없고 배경 대비가 가장 높다. A 의 5-3 발광 동색 문제는 시안 `hero-ch5-paladin.html` 에서 서리 대성당 배경 위에 겹쳐 확인하고, 묻히면 5-3 금 발광 변형을 쓴다.

---

## 4. 스킬 3종 (영웅 스킬 시스템 `hero_skills.json` 확장)

게이트: HP **85% / 65% / 45%**(패시브 임계와 0.05 오프셋). `HeroSkillRunner`, 쿨다운 자동 시전, 컷인 재사용. 수치는 초안, G0/G1 검증 후 확정.

| 스킬 (`$type`, `fileName`) | 게이트 | 필드 / 값 | 효과 · 처치 타수 |
|---|---|---|---|
| **심판의 망치** `JudgmentHammerSkillData`, `HeroSkill_JudgmentHammer` ("심판의 망치") | 85% | `damage` 110 / `cooldown` 6.0 / `radius` 2.4 / `stunSec` 0.6 / `castRange` 5.0 | 현재 표적 몬스터가 영웅에게서 5.0 이내이면 그 위치에 빛의 망치가 떨어져 반경 2.4 안 몬스터에 110 피해 + 0.6초 기절(공격·이동 정지). 처치 타수: Phantom·Plague·Hex·Reaper 1타, Wisp 2타, Wraith 5타 |
| **정화의 빛** `PurifyingLightSkillData`, `HeroSkill_PurifyingLight` ("정화의 빛") | 65% | `triggerDebuffCount` 2 / `healPercent` 0.08 / `cooldown` 12.0 | 영웅에게 붙은 **대상 시한 디버프 슬롯**(`SlowAura` · `PlagueToxinAura` · `FearAura` · `BleedAura` · `WeakenAura` - 2026-10-29 `PoisonAura` 삭제 → `PlagueToxinAura` 추가)이 2종 이상이면 그 슬롯을 전부 해제하고 최대 HP 8% 회복. 독은 중첩 전부가 한 슬롯이라 1종으로 센다. 영구 디버프(`HeroAttackDownAura` · `EternalBleedAura`)는 해제하지 않는다(이미 패시브로 감소분 x0.6). **`SlowAura` 슬롯은 1종으로 세고 해제한다**(현행 출처는 Swarm `Slow` 카드뿐, E-C5-1) |
| **성역** `SanctuarySkillData`, `HeroSkill_Sanctuary` ("성역") | 45% | `durationSec` 6.0 / `damageTakenMul` 0.6 / `radius` 2.6 / `monsterSpeedMul` 0.7 / `cooldown` 18.0 / `triggerMonsterCount` 3 / `triggerRadius` 2.6 | 반경 2.6 안 몬스터가 3마리 이상이면 시전 위치에 성역을 펼친다: 6.0초 동안 영웅 받는 피해 x0.6(위치 무관) + 성역 안 몬스터 이동 x0.7 |

- `loadout` row: `{heroId: "Paladin", hpFraction: 0.85, skill: "HeroSkill_JudgmentHammer"}`, `{.., 0.65, "HeroSkill_PurifyingLight"}`, `{.., 0.45, "HeroSkill_Sanctuary"}`.
- 자동 시전 우선순위(동시에 조건 참): 정화의 빛 > 성역 > 심판의 망치. 근거: 정화는 조건이 드물게 맞으므로 맞을 때 놓치지 않고, 성역은 포위 시 생존기, 망치는 6초마다 도는 상시 딜.
- 정화 조건 2종 근거(§1.2 발견 3): 패시브 적용 후 액티브 디버프는 서로 겹치지 않아 "역병귀 둔화 + 액티브 1종"(2026-11-01 현행: "역병귀 독 + 액티브 1종")이 현실적 최대치다. 3종으로 두면 G13 에서 발동 0회가 나올 위험이 높다.
- 이론 보조 DPS(참고): 망치 110 x 3마리 / 6.0 = 55.0. 성역 평균 받는 피해 = 1 - 0.4 x (6.0 / 18.0) = x0.867. 정화 회복 최대 8% / 12초.
- 받는 피해 곱연산: 성역 x0.6 과 `BossBulwark` x0.6 이 겹치면 0.36(엣지 E-C5-3).
- 컷인(시간정지 1.9초 x 3회) 재사용.
- 효과 로직 재사용 원칙: 구형 범위 피해(`IHeroSkillContext`), 영웅 받는 피해 배율(`BossBulwark`/`BladeWhirl`), 고정 위치 몬스터 감속 구역(`SmokeBomb` `monsterSpeedMul`). **신규는 "몬스터 기절"(코드에 기존 기절 효과 없음 - grep 확인)과 "영웅 시한 디버프 조회·해제 + 최대 HP 비율 회복"뿐이다.**

---

## 5. 챕터 5 스테이지와 난이도 곡선

### 5.1 규칙 배정 (신규 규칙 0종, 기존 6종 재사용)

콘셉트: 서리 대주교가 대성당 곳곳에 봉인과 결계를 걸었다. 봉인 대상은 시안대로 Hex(Dps) · Plague(Debuff)다. 6종족 모두 Ch1~4 에서 한 번씩 봉인됐으므로 Ch5 는 두 번째 봉인이며, Plague 봉인은 "Debuff 약" 챕터에서 축 몬스터마저 늦게 열리는 마감 압박이다.

| 스테이지 | 규칙 | 의도 |
|---|---|---|
| 5-1 얼어붙은 회랑 | 없음 | 톱니 골. 응징 표적·신앙의 가호 학습 |
| 5-2 침묵의 성가대석 | `SealedNest` (Hex 봉인, 해제 0.60) | 원거리 딜러가 막혀 근접 교전 위주 - Dps 해답이 Reaper 로 좁아진다 |
| 5-3 고해의 예배당 | `DenseFog` | Hex 사거리 5.0 -> 3.5. 느린 성기사에게 Hex 가 가까이 붙어야 해 응징·망치 범위에 잘 걸린다 |
| 5-4 얼음 성물 보관소 | `HolySpring` + `SealedNest` (Plague 봉인, 해제 0.55) | 샘이 성기사를 회복시키고(중앙 끌림이 없어 체류율 0.4 가정), Debuff 축 몬스터가 55% 까지 없다 |
| 5-5 서리 대주교의 제단 | `HunterTraps` (0.75) + `BossBulwark` (0.35) | 앞에서 덫(몬스터 감속)으로 몬스터가 영웅 곁에 오래 머물고, 마지막 35% 에서 대주교의 결계가 최후 버티기를 만든다 |

**보스 페이즈 대안 비교** - 시안의 `BossBulwark` **0.70 은 데이터 검증을 통과하지 못한다**(`ChapterDatabase.cs` 457행: 트리거 hpFraction 이 패시브 임계 0.9~0.1 과 같으면 오류). 사용 가능 값은 0.75 / 0.55 / 0.35 이고, 앞 보스 조합은 Ch1 결계 0.55 · Ch2 화살비 0.75 + 덫 0.35 · Ch3 결계 0.75 + 덫 0.35 · Ch4 결계 0.55 + 화살비 0.35 다.

| 안 | 페이즈 구성 | 평가 |
|---|---|---|
| **A. 덫 0.75 + 결계 0.35 - 권장** | 2페이즈, 5개 보스 중 유일한 조합·순서 | 시안의 두 규칙(결계 + 덫)을 유지하면서 3장 복제를 피한다. 결계를 마지막에 둬 "대주교의 최후 결계"로 마지막 챕터 마감 서사가 맞다. 결계 15초가 성역(45% 이후 18초 주기)과 겹칠 수 있어 G13 에서 마지막 구간 시간 확인 |
| B. 화살비 0.75 + 결계 0.55 + 덫 0.35 | 3페이즈 | 최종 보스 에스컬레이션은 크지만 Ch4 가 기각한 사유(필드 점유 12마리 미만 위험, ruleFactor 추정 확대)가 그대로 남는다 |
| C. 결계 0.75 + 덫 0.35 | 2페이즈 (시안 0.70 -> 0.75 최소 보정) | 시안과 가장 가깝지만 3-5 와 완전히 같은 구성이다(Ch4 §5.1 C 기각 사유) |

### 5.2 규칙 row (`Stage_Rule.json` / `Stage_Rule_Param.json` / `Stage_Rule_Trigger.json`)

| chapterId | stageIndex | ruleIndex | ruleId | 트리거 hpFraction | 파라미터 |
|---|---|---|---|---|---|
| 5 | 2 | 1 | SealedNest | (row 없음 = 상시) | `lockedSpecies` 3 (Hex), `unlockHpFraction` 0.60 |
| 5 | 3 | 1 | DenseFog | (없음) | `monsterRangeMul` 0.7, `minRange` 0.8 |
| 5 | 4 | 1 | HolySpring | (없음) | `centerX` 0.0, `centerZ` 0.0, `radius` 3.0, `healPercentPerSec` 0.4, `activeSec` 8, `periodSec` 40, `firstActiveSec` 20 |
| 5 | 4 | 2 | SealedNest | (없음) | `lockedSpecies` 4 (Plague), `unlockHpFraction` 0.55 |
| 5 | 5 | 1 | HunterTraps | 0.75 | `trapCount` 6, `ringDistance` 3.5, `trapRadius` 1.2, `lifeSec` 12, `rearmSec` 15, `monsterSpeedMul` 0.5, `damagePerSec` 10 |
| 5 | 5 | 2 | BossBulwark | 0.35 | `damageTakenMul` 0.6, `durationSec` 15 |

- 파라미터 값은 기존 승인값(Ch2/Ch3 덫, Ch1/Ch3/Ch4 결계·샘·안개)을 그대로 쓴다. 신규 파라미터 키 없음.
- `EMonster` 정수값: Wisp 0 · Wraith 1 · Reaper 2 · Hex 3 · Plague 4 · Phantom 5.
- 임계 이격: 트리거 0.75 / 0.35 는 패시브(0.9~0.1)·스킬(0.85/0.65/0.45)과 0.05 이상 이격(검증 통과값). 봉인 해제 0.60 은 패시브 임계와 같아 해제 효과(스폰 버스트)는 카드 팝업이 닫힌 뒤 처리(chapter-system E-4). 0.55 는 어느 임계와도 같지 않다(선례: 3-4 해제 0.5).
- 덫 감속 x0.5 와 성역 감속 x0.7 이 겹치면 x0.35(엣지 E-C5-4).

### 5.3 스테이지 표 (`Stage.json`, T1 = 130초 기준 도출, 모두 G0/G1 검증 후 확정)

도출식(chapter-system §0.4): `hpMultiplier = round2(targetClearSec x M_ref / T1 / (hpScale x ruleFactor))`, `eq 배수 = targetClearSec x M_ref / T1`. `hpScale` = 1.10. `M_ref` Ch5 = 1.31 / 1.32 / 1.33 / 1.34 / 1.35 (Ch4 보스 1.30 에서 스테이지당 +0.01).

> **M_ref 근거**: Ch4 가 남긴 몫은 1.35 - 1.30 = 0.05 이고 +0.01 x 5 로 정확히 소진된다. 5-5 의 1.35 는 상점 최대 효과(0.35)의 **100% 소진** 시점 = `M_max` 다. 즉 5-5 는 상점 만렙 플레이어 기준 목표 시간이며, `C(130)` = 270 x 1.35 / 130 = 2.80 에 대해 5-5 eq 2.70 은 여유 0.10(목표 260초 vs 상한 270초)이다.
>
> **raw `hpMultiplier` 가 4장과 다르게 움직이는 이유**: `hpScale` 이 광전사 1.25 에서 성기사 1.10 으로 내려 분모가 작아졌다. 난이도는 raw 배수가 아니라 eq 배수(4-5 2.55 -> 5-1 1.71 -> 5-5 2.70)로 판단한다. 5-3 과 5-4 의 raw 값이 1.95 로 같은 것은 5-4 의 `ruleFactor`(1.107)가 eq 증가분(+0.22)을 흡수했기 때문이며, eq 는 2.15 -> 2.37 로 오른다.

| 스테이지 | 스테이지명(id) | 규칙 | targetClearSec | M_ref | eq 배수 | ruleFactor | **hpMultiplier** | **powerMultiplier** | `ScaleMultiplier` |
|---|---|---|---|---|---|---|---|---|---|
| 5-1 | 얼어붙은 회랑 (1141) | 없음 | 170 | 1.31 | 1.71 | 1.000 | **1.56** | **1.40** | 1.00 |
| 5-2 | 침묵의 성가대석 (1142) | SealedNest(Hex) | 190 | 1.32 | 1.93 | 1.079 | **1.63** | **1.50** | 1.00 |
| 5-3 | 고해의 예배당 (1143) | DenseFog | 210 | 1.33 | 2.15 | 1.000 | **1.95** | **1.65** | 1.00 |
| 5-4 | 얼음 성물 보관소 (1144) | HolySpring + SealedNest(Plague) | 230 | 1.34 | 2.37 | 1.107 | **1.95** | **1.80** | 1.00 |
| 5-5 | 서리 대주교의 제단 (1145) | HunterTraps + BossBulwark | 260 | 1.35 | 2.70 | 1.092 | **2.25** | **1.95** | 1.30 |

**`ruleFactor` 산식** (dps-share 비중 chapter-system §4.1: Wisp 17.0% · Wraith 15.3% · Reaper 30.6% · Hex 18.3% · Plague 6.1% · Phantom 12.7%)

| 스테이지 | 산식 | 값 |
|---|---|---|
| 5-2 | Hex 18.3% x (1 - 0.60 = 40%) = 7.32% -> 1 / (1 - 0.0732) = 1 / 0.9268 = 1.07898 | 1.079 |
| 5-3 | 안개 단독은 미보정(1-3 · 2-3 선례: ruleFactor 1.000) | 1.000 |
| 5-4 | 샘: 개방 횟수 floor((230 - 20) / 40) + 1 = 5 + 1 = 6회. 6 x (0.4% x 8s = 3.2%) x 체류율 0.4 = 7.68% -> 1.0768. 체류율 0.4 근거: 중앙 끌림이 없어 기사(0.6)보다 낮고, 이동이 가장 느려 궁수(0.4, 카이팅)보다 샘을 빨리 벗어나지 않는다고 보고 궁수와 같은 값. 봉인: Plague 6.1% x (1 - 0.55 = 45%) = 2.745% -> 1 / 0.97255 = 1.02822. 곱 1.0768 x 1.02822 = 1.10719 | 1.107 |
| 5-5 | BossBulwark: 15s x 40% 경감 x 후반 피격 가중 1.3 / 목표 260s = 7.8 / 260 = 3.00% -> 1.030. HunterTraps 추정 6% -> 1.06 (2-5 · 3-5 선례). 곱 1.030 x 1.06 = 1.0918 | 1.092 |

> 5-5 의 ruleFactor 1.092 는 **추정치**다(정량 모델 없음). 결계가 0.35 에 오면 성역과 겹칠 수 있어 실효 경감이 커질 수 있다. G12/G13 실측 후 도출식으로 재산출한다.

**검산** (T1 = 130, round2 직전 비반올림 값)

| 스테이지 | eq 비반올림 | 분모 hpScale x ruleFactor | hpMultiplier 비반올림 | 역검산(1.10 x hpMult x ruleFactor) |
|---|---|---|---|---|
| 5-1 | 170 x 1.31 / 130 = 222.7 / 130 = 1.7131 | 1.10 x 1.000 = 1.1000 | 1.5573 -> 1.56 | 1.10 x 1.56 x 1.000 = 1.716 |
| 5-2 | 190 x 1.32 / 130 = 250.8 / 130 = 1.9292 | 1.10 x 1.079 = 1.1869 | 1.6254 -> 1.63 | 1.10 x 1.63 x 1.079 = 1.935 |
| 5-3 | 210 x 1.33 / 130 = 279.3 / 130 = 2.1485 | 1.10 x 1.000 = 1.1000 | 1.9531 -> 1.95 | 1.10 x 1.95 x 1.000 = 2.145 |
| 5-4 | 230 x 1.34 / 130 = 308.2 / 130 = 2.3708 | 1.10 x 1.107 = 1.2177 | 1.9469 -> 1.95 | 1.10 x 1.95 x 1.107 = 2.375 |
| 5-5 | 260 x 1.35 / 130 = 351.0 / 130 = 2.7000 | 1.10 x 1.092 = 1.2012 | 2.2478 -> 2.25 | 1.10 x 2.25 x 1.092 = 2.703 |

표의 eq 열은 위 비반올림 값의 round2(1.71 / 1.93 / 2.15 / 2.37 / 2.70)다. 절대 공격력 = `ScaleHeroStat(50, 0.85, powerMultiplier)`(§3.2).

`powerMultiplier` 톱니: 챕터 S1 1.00 / 1.10 / 1.20 / 1.30 -> **5-1 1.40**(+0.10), 챕터 내 증가폭 +0.10 / +0.15 / +0.15 / +0.15(Ch3·Ch4 와 같은 패턴) -> 1.40 / 1.50 / 1.65 / 1.80 / 1.95.

### 5.4 톱니형 곡선과 상한 점검

| 구간 | eq 배수 | 비고 |
|---|---|---|
| Ch4 S5 -> Ch5 S1 | 2.55 -> **1.71** | **-32.9% 하강** ((2.55 - 1.71) / 2.55 = 0.84 / 2.55 = 0.329). Ch4 진입(-34.7%)보다 약간 얕다 - 골의 높이가 계속 올라가는 방향 |
| Ch5 S1 -> S5 | 1.71 -> 1.93 -> 2.15 -> 2.37 -> 2.70 | 단조 증가 (+0.22 / +0.22 / +0.22 / +0.33: 보스 도약) |
| 챕터 S1 끼리 | 1.00 -> 1.25 -> 1.44 -> 1.60 -> 1.71 | 상승 (+0.25 / +0.19 / +0.16 / +0.11), 증가폭은 체감 |
| 챕터 보스 끼리 | 1.98 -> 2.28 -> 2.45 -> 2.55 -> 2.70 | 상승 (+0.30 / +0.17 / +0.10 / +0.15). 마지막 보스가 최고점 |

- 상한 점검: 최대 eq 2.70 <= chapter-system §0.4 의 Ch5 참고 상한 2.80 = `C(130)`. 분자 `hpScale x hpMultiplier` 최대 = 1.10 x 2.25 = 2.475 로 3.5 미만.
- 목표 시간: 5-5 260초는 보스 밴드 210~270초 안, 5분(300초) 제한까지 40초 여유.
- 모든 수치는 **G0 -> G1 -> G11 -> G12 -> G13 순 검증 후 확정**하며 어긋나면 위 도출식으로 `Stage.json` 을 일괄 재산출한다(규칙 구조·파라미터 불변).

### 5.5 시안 대비 달라진 값 (재도출 결과)

| 항목 | 시안 | 본 문서 | 이유 |
|---|---|---|---|
| 5-5 결계 트리거 | 0.70 | 0.35 (덫 0.75 앞으로) | 0.70 은 패시브 임계와 같아 `ChapterDatabase` 검증 오류. 3-5 복제 회피(§5.1) |
| 5-5 페이즈 순서 | 결계 -> 덫 | 덫 0.75 -> 결계 0.35 | 위와 같음 |
| 5-4 해제 | Plague 0.55 | 동일 | - |
| hpMultiplier | 1.60 / 1.72 / 1.92 / 2.02 / 2.40 | 1.56 / 1.63 / 1.95 / 1.95 / 2.25 | hpScale 1.10 · M_ref 1.31~1.35 · ruleFactor 반영 도출식 |
| powerMultiplier | 1.35 / 1.45 / 1.60 / 1.75 / 1.90 | 1.40 / 1.50 / 1.65 / 1.80 / 1.95 | 챕터 S1 톱니(+0.10) 연장 |
| 패시브 | 시한 디버프 지속 x0.6 (둔화·독·공포·약화) | Debuff 축 출처 한정, 시한 지속 x0.6 + 영구 감소분 x0.6, 출혈 포함 | §1.2 발견 1·2 |
| 정화의 빛 조건 | 디버프 3개 이상 | 대상 시한 디버프 2종 이상, 영구 미해제 | §1.2 발견 3 |
| 심판의 망치 대상 | 가장 가까운 군집 | 현재 표적 몬스터 위치(사거리 5.0) | 성기사는 군집 질의가 없는 단일 표적 AI - 별도 군집 탐색을 만들지 않음(YAGNI) |
| targetClearSec · 스탯 · 스킬 수치 · 스케일 1.30 | - | 시안 유지 | 도출식·선례와 정합 |

시안 HTML 의 STG/스킬 표 숫자는 합의용 초안으로 남기고, 구현의 단일 진실은 본 문서다.

---

## 6. 맵 배경 - "서리 대성당"

> 시안: `.mockups/scene-bg-ch5-frost.html` (4장 불꽃의 용광로와 나란히 비교). 캔버스 800x352, 팔레트 `TH[5]`.

### 6.1 에셋 규격과 키

- 파일 `Battle_Backdrop_FrostCathedral.png`, **1600x704** (시안 캔버스의 도트 2배 확대), PPU 48, Point 필터 · 무압축 · 밉맵 off. `Backdrop2D_SheetSpec.json` 엔트리 추가: `folder: Backdrop2D`, `size: [1600, 704]`, `pivot: [0.5, 0.5]`, `ppu: 48`, `content: [0, 0, 1600, 704]`.
- `EBackdrop.Battle_Backdrop_FrostCathedral` = `ChapterRow.backdropKey` 값명 = Addressables 주소 = 파일명.
- 정적 베이스는 시안의 `window.__ch5Baked()` 로 추출한다(4장의 `__ch4Static()` 대응).
- 공통 요소 유지: 상단 금 코니스(`#b9832a` / `#f7c64a`), 림 금 인레이 + 금 리벳 12개, 바깥 룬 점선, 비네트, 6개 스포너 자리는 어두운 금 링만(제단 본체 없음), HUD 가독성을 위해 가장자리·상단은 어둡게.

### 6.2 장별 비교와 5장 팔레트

| 항목 | 4장 불꽃의 용광로 | **5장 서리 대성당 (신규)** |
|---|---|---|
| 발광색 `GL` (rgb) | 엠버 주홍 `255,106,61` (14°) | **서리 청 `150,220,255` (200°)** |
| 보조 발광 `GL2` | 금 `247,198,74` | 금 `247,198,74` (UI 금 유지) |
| 석재 A / B / M | `#241a1c` / `#372729` / `#150d0e` | `#16202c` / `#223142` / `#0b1119` |
| 림 하이 / 로우 | `#7a5a52` / `#4d3a3a` | `#7d93ad` / `#4b5d74` |
| 배경색 / 측면 | `#0b0606` / `#201516` | `#05080d` / `#142030` |
| 바닥 중심 반사광 `LIFT` | `[130,58,28]` | `[90,150,190]` |

- 발광 색상각: 1장 155° · 2장 95° · 3장 260° · 4장 14° · **5장 200°**. UI 금(43°)과 구분된다. 4장(뜨거움)과 5장(차가움)이 정반대 온도라 연속 챕터의 분위기 전환이 크다.
- 영웅 대비: 성기사 A안 흰 몸은 어두운 청 배경에서 가장 밝은 물체다. 5-3 발광 `#7DD3FC`(199.4°)는 배경 발광(200°)과 같은 색이라 시안에서 겹쳐 확인한다(§3.5).

### 6.3 5장 연출 설계

| 층 | 정적 베이스 | 동적(애니메이션) |
|---|---|---|
| 상단 벽 | 서리 낀 석벽 + 금 코니스, 길이가 제각각인 **고드름**, 금 테 **장미창 3개** | 장미창이 은은하게 빛나고 빛이 바닥 쪽으로 비침. 7~10초마다 장미창 섬광 |
| 기둥 | 기둥 4개(얼음 피복 + 금 띠), 위쪽 서리 결정 | - |
| 양옆 | **얼음 폭포 2줄**, 아래 모서리 얼음 수정 군락 | 폭포 세로 결이 반짝임. 오른쪽은 시너지 패널 뒤라 밝기 0.6 |
| 투기장 바닥 | 푸른 석재 동심원 + 금 인레이 + 가장자리 성에 + 도트 **얼음 균열** | 균열을 따라 빛이 바깥으로 흐르며 맥동 |
| 중앙 | 6갈래 **눈꽃 문양 봉인진** + 바깥 룬 점선 | 봉인진 밝기가 회전하듯 이동 |
| 림 | 금 리벳 12개 + 림 위 **얼음 수정 6개** | 수정 반짝임 |
| 주변 | 눈 더미 고리, 아래쪽 얼어붙은 성배·촛대·부서진 석상 실루엣 | - |
| 입자 | - | 비스듬히 내리는 **눈송이**(크기 2종) |
| 대기 | - | 상단 **오로라** 띠 일렁임, 바닥 **냉기 안개** |
| 공통 | - | 비네트 |

- 시안과 PNG 의 관계: 시안은 합의용이다. 동적 연출은 Unity 측 기존 장별 연출 경로와 같은 방식으로 구현한다(방식은 gameplay-programmer 판단).

---

## 7. 에셋 요구 / 구현 요청사항 (gameplay-programmer 용 - 승인 후)

### 7.1 에셋 요구 (Claude 제작)

| 에셋 | 사양 |
|---|---|
| `Paladin_Sheet.png` + `Paladin_Sheet_S2~S5.png` (+ 각 `_Emission.png`) + `Paladin_SheetSpec.json` | 기존 4영웅 `*_SheetSpec.json` 과 같은 규격(§3.4). 시안 `hero-ch5-paladin.html` 은 부위 리그 시안이며 시트 베이크·실루엣 실측은 승인 후 별도 작업 |
| `HeroIcons/Paladin.png`, `Paladin_S2~S5.png` | 스테이지 초상 5장 (기존 초상 규격) |
| FX (프리팹 = `EVisual` 값명) | `HeroJudgmentHammerFx`(빛의 망치 낙하 + 반경 2.4 충격) `HeroPurifyingLightFx`(영웅 중심 정화 기둥) `HeroSanctuaryFx`(반경 2.6 바닥 성역 6.0초). 기본 공격 성광 링은 시트 프레임(F4)에 포함 - 별도 FX 없음. 규칙 FX 는 기존 `StageSealChainFx` `StageFogFx` `StageSpringFx` `StageBulwarkFx` 와 덫 FX 재사용 |
| 배경 `Battle_Backdrop_FrostCathedral.png` | §6. 정적 베이스는 `window.__ch5Baked()` 로 추출 |
| 사운드 | 이번 범위 제외(후속) |

### 7.2 Enum 값 (`CommonEnum.cs`, 카테고리별 분리 - Rule 02 §8, Rule 03 §2)

| Enum | 추가 값 | 비고 |
|---|---|---|
| `EHero` | `Paladin` | `Berserker` 뒤에 추가(기존 직렬화 정합) |
| `EHeroAi` | `Guardian` | `Charger` 뒤 |
| `EData` | `HeroArchetype_Guard`, `HeroArchetype_DebuffResist`, `HeroSkillLoadout_Paladin` | JSON/SO 파일명 = 값명 |
| `EBackdrop` | `Battle_Backdrop_FrostCathedral` | `ChapterRow.backdropKey` 값명 |
| `EVisual` | `HeroJudgmentHammerFx`, `HeroPurifyingLightFx`, `HeroSanctuaryFx` | 기존 값 뒤에 추가(int 직렬화 정합), 프리팹 `CHMPool` 대상 |

`EStageRule`·`EUI`·`ECardId` 신규 값 없음.

### 7.3 Interface (`CommonInterface*.cs`, Rule 02 §9)

- AI 설정 주입: `GuardSettings`(`HeroArchetype_GuardRow` 로부터), 패시브 설정 `DebuffResistSettings`(`HeroArchetype_DebuffResistRow`). 주입 방식·인터페이스 신설 여부는 gameplay-programmer 판단.
- 고정 요구 (방식은 gameplay-programmer 판단):
  1. 디버프 부착 시 **출처**(Debuff 축 카드 / 역병귀 적중 / 그 외)를 구분할 수 있어야 한다 - 패시브 대상 판정(§1.2) 단일 진실.
  2. 영웅이 "자신에게 대상 디버프를 건 몬스터"를 알 수 있어야 한다 - 응징 표적(§3.3).
  3. 두 부착 경로(`BattleContext.ApplyHeroAura` · `PlagueToxinOnHit` 직접 `Attach`, 2026-10-29 이전 `PlagueSlowOnHit`) 모두 패시브가 적용돼야 한다.
- 신규 스킬 효과 컨텍스트: 몬스터 기절(`JudgmentHammer`), 영웅 대상 시한 디버프 개수 조회·일괄 해제 + 최대 HP 비율 회복(`PurifyingLight`). 광전사 `IBerserkerSkillContext` 처럼 별도 컨텍스트로 둘지 기존 확장인지는 gameplay-programmer 판단.

### 7.4 에셋 키 / 데이터 파일 (파일명 = Enum 값명)

- JSON(`Assets/_Lair/Data/Json/`): `HeroArchetype_Guard.json`(1 row), `HeroArchetype_DebuffResist.json`(1 row), `Chapter.json`(row 5), `Stage.json`(5 row), `Stage_Rule.json` / `Stage_Rule_Param.json` / `Stage_Rule_Trigger.json`(§5.2), `HeroArchetype.json`(Paladin), `hero_skills.json`(스킬 3종 + loadout 3 row), `Strings_Ko.json`(1005 / 1015 / 1025 / 1141~1145).
- **스킬 SO 생성 방식**: 스킬 SO 는 메인이 `HeroSkillSyncer.Import` 를 직접 호출해 만든다. `HeroSkillSyncer` 가 있는 `Lair.Editor.JsonSync` 어셈블리는 자동 참조가 아니라서, 4장 때 일회용 빌더에서 호출하면 컴파일 에러가 났다.
- SO: `HeroSkillLoadout_Paladin.asset`, 스킬 SO `HeroSkill_JudgmentHammer.asset` `HeroSkill_PurifyingLight.asset` `HeroSkill_Sanctuary.asset`(`Art/Skills/`), `HeroStageVariantConfig.asset`(ChapterId=5 엔트리 5개: `Tier` 0~4, `UseEmission` false/false/true/true/true, `EmissionIntensity` 0/0/1.5/2.2/3.2, `ScaleMultiplier` 1/1/1/1/1.30).
- 프리팹: `Paladin.prefab`(`Art/Characters/`, Knight 프리팹과 같은 컴포넌트 구성, `_centerPullEnabled` off), `EVisual` 3종 FX(`Art/FX/`).
- 스프라이트·배경: §7.1, `Backdrop2D_SheetSpec.json` 엔트리 1개.
- 로드 검증(`ChapterDatabase`): 챕터당 스테이지 5개, 복합키 중복 0, FK(`ChapterRow.heroId` -> `HeroArchetype`, `HeroArchetype_Guard.heroId` -> aiType=Guardian 영웅, `HeroArchetype_DebuffResist.heroId` -> `HeroArchetype`), aiType=Guardian 영웅은 Guard row 필수(역방향 포함), `retributionLockSec` > 0, `durationMul` 0 초과 1 이하 · `penaltyMul` 0 이상 1 이하, 규칙 필수 키 일치, 트리거 hpFraction 이 스킬 게이트/패시브 임계와 불일치.

---

## 8. 영향 범위 체크리스트 (챕터 4 개방 기준)

> 작성 근거: **chapter-4.md §8 체크리스트 + 현행 코드 grep**(`ChapterGate.cs` · `StageProgress.cs` · `WorldMapViewModel.cs` · `BattleController.cs` · `ChapterDatabase.cs` · 테스트 폴더)으로 도출했다. 이후 메인이 `git show --stat 2eadb0d` 와 대조해 누락된 테스트 8개를 §8.1 에 보강했다.

| # | 대상 | 변경 | 근거 |
|---|---|---|---|
| 1 | `ChapterGate.PlayableChapterCount` | **4 -> 5** (`PlayableStageCount` 20 -> 25). **마지막 분할(d)에서만 올린다.** 그 전 (a) 에서 `Chapter.json` row 5 가 먼저 들어가면 `Chapter4OpenedWorldTests` 39행(`db.ChapterCount == PlayableChapterCount`, 5 != 4)이 깨진다 -> (a) 에서 "게이트 상향 전까지 `ChapterCount - 1`"로 임시 완화하고 (d) 에서 원복(Ch4 와 같은 처리) | `ChapterGate.cs` 주석 |
| 2 | `StageProgress.MaxStage` | `Chapter.json` row 수 기반이라 20 -> **25** 자동 (코드 변경 없음). `DefaultChapterCount`(2) 유지. 5챕터 x 5 = 25 가 구조 최대치 | `ConfigureChapterCount` |
| 3 | `MetaProfile.Version` / `MetaProfileMigration.CurrentVersion` | **6 유지 권장**(Ch4 D-C4-10 과 같은 근거: 스키마 필드 변경 없음, 범위만 0~25). 구버전(MaxStage 20) 클라이언트가 Cleared 21~25 를 복원하면 20 으로 잘림 -> 신버전 동시 배포 | `MetaProfileMigration.cs` 11행 |
| 4 | `StageLeaderboard.IsValidStage` | 1~25 (`MaxStage` 참조, 코드 변경 없음). `hero` 필드 = "Paladin" | chapter-system §2.3 |
| 5 | **사용자 작업(Firebase 콘솔)** | Firestore 보안 규칙 `stage` 허용 범위 1~20 -> **1~25**. 변경 전 21~25 제출은 서버 거부 + 클라이언트 조용히 실패 | D-C4-11 과 같은 성격 |
| 6 | `WorldMapViewModel` | `ChapterSlotCount` 5 = 출시 챕터 5 -> **자리표시자 탭 0개**(코드 변경 없음, row 5 로 실제 탭). (a)~(c) 기간엔 5번째 탭이 "준비 중"(1322) 잠금 탭으로 보인다. 5-5 상세 패널(규칙 0 + 페이즈 2건)은 4-5 와 같은 구조 | `WorldMapViewModel.cs` 51·216행 |
| 7 | 마지막 챕터 종점 | 5-5 클리어 후 다음 챕터가 없다. `MaxPlayableStage(25)` = 25 로 캐러셀·월드맵 기본 선택이 5-5 에 머무는지, `StageLabels.UnlockHint` · 챕터 잠금 안내(1314/1315)가 존재하지 않는 "6장"을 가리키지 않는지 확인 | `StageProgress.cs` 79행, `StageLabels.cs` |
| 8 | `VillageViewModel` / 마을 캐러셀 | ◀/▶ 1~25 순회, `STAGE 5-N` 표기, 쇼케이스 영웅 성기사, 영웅 표기 분기에 Paladin | `VillageViewModel.cs` 15행 |
| 9 | `BattleController` | ① `EHeroAi.Guardian` 분기(Guard 설정 주입, Charger 분기 675행 인접, row 없으면 전투 진입 중단 로그) ② `DebuffResist` 주입 ③ 로드아웃 맵(837행)에 `{ EHero.Paladin, EData.HeroSkillLoadout_Paladin }` ④ `WarmVisual`(1609행)에 Paladin FX 3종 ⑤ 챕터 5 영웅 프리팹 ⑥ 랭킹 제출 `hero` | `BattleController.cs` |
| 10 | `ChapterDatabase` / `ChapterDatabaseLoader` | `HeroArchetype_Guard` / `HeroArchetype_DebuffResist` 로드·FK·역방향 검증(`AddCharges` 패턴 복제), 로더 목록 2종 추가 | `ChapterDatabase.cs` 261~298행 |
| 11 | `AutoCombatAI` | Guardian 응징 표적 (다른 4영웅 경로 불변) | §3.3 |
| 12 | 디버프 부착 경로 | `HeroAuraRunner` / `BattleContext.ApplyHeroAura` / `PlagueToxinOnHit`(2026-10-29 이전 `PlagueSlowOnHit`) / `DebuffSynergyTier2` · `DebuffSynergyTier3` · `HeroAttackDownEffect` (영구 강도) | §1.2, §3.3 #4~6 |
| 13 | 스킬 | `JudgmentHammerSkillData` `PurifyingLightSkillData` `SanctuarySkillData` 신규 `$type`, 컨텍스트 확장 | §4 |
| 14 | `HeroStageVariantApplier` | 엔트리만 추가 (누락 시 로드 오류 E-11) | chapter-system §1.2 |
| 15 | `RankingPopup` / `RecordsPopup` | 스테이지 선택 1~25, 기록 행 25 | chapter-system §2.3 |
| 16 | 배경 | `Battle_Backdrop_FrostCathedral.png` + SheetSpec 엔트리 + Addressables 등록 | §6 |

### 8.1 영향받는 기존 테스트 (값 갱신 대상, 파일명 단위 - grep 결과)

| 파일 | 영향 |
|---|---|
| `Chapter4OpenedWorldTests` | `PlayableChapterCount == 4` / `PlayableStageCount == 20` 단정(37~39행)이 5 / 25 로 갱신되거나 "챕터 4 시점 스냅샷"으로 분리. (a) 기간 39행 임시 완화(§8 #1) |
| `Chapter3OpenedWorldTests` · `Chapter3ProgressAndPresentationTests` | 남아 있는 게이트·스테이지 수 단정 점검 |
| `ChapterGateAndStageLabelsTests` (게이트 참조 12건) | 5장이 플레이 가능 - "5장 잠금/준비 중" 단정은 제거(더 이상 잠긴 미출시 챕터가 없음) |
| `WorldMapViewModelTests` · `WorldMapViewModelBehaviorTests` · `WorldMapStringsAndPrefabTests` · `WorldMapRedesignTests` | 챕터 수 5, **미출시 탭 0개**(기존 "1개" 단정 반전), 노드 25 |
| `VillageCarouselAndRecordsLabelTests` · `VillageStageCarouselTests` · `VillageHudVisualPolishTests` · `RecordsVisibleStageCountTests` · `RecordsStageRowTests` | 캐러셀 상한 25, 기록 행 25 |
| `Chapter4DataValidationTests` · `ChapterDatabaseTests` · `ChapterDatabaseValidationTests` · `ChapterTestSupport` · `StageRuleFactoryAndDataTests` · `StageRuleTests` | 챕터 수·스테이지 수·규칙 row 수 기대값, 새 FK 검증 |
| `StageProgressEdgeTests` · `StageProgressChapterEdgeTests` · `ChapterProgressTests` · `MetaProfileMigrationTests` | 경계(20<->21, 25), `ResolveClearedStage` 상한 25, v6 로드 시 Cleared 21~25 보존 |
| `HeroStatTableRegressionTests` | Paladin `ScaleHeroStat(50, 0.85, 1.40)` = 60 · `(.., 1.80)` = 77 (.5 경계) |
| `Chapter4ArtIntegrityTests` · `Backdrop2DImportTests` · `DotSceneRegressionTests` | 신규 `Chapter5ArtIntegrityTests`(시트 5장 + 에미션 + 초상 + 배경)로 복제, 배경 임포트 목록에 FrostCathedral |
| `UiRedesignDataTests` | 챕터 수 의존 단정 점검 |
| `ArcherDataConsistencyTests` | 스킬 수·로드아웃 영웅 수 하드코딩 갱신: 12종 -> **15종**, 4영웅 -> **5영웅** (4장 때 9 -> 12, 3 -> 4) |
| `ArcherTestSupport` | FX 로그 격리 키 목록에 `HeroJudgmentHammerFx` · `HeroPurifyingLightFx` · `HeroSanctuaryFx` 추가 |
| `AssassinWorldIntegrationTests` | `HeroSkillSyncer.LoadoutPaths` 경로 수 4 -> **5** |
| `Chapter3DataValidationTests` | 챕터 범위로 규칙 row 수를 세는 단정 - 챕터 5 row 추가 후 범위·기대값 점검 |
| `Chapter4BerserkerLogicTests` | 갱신 대상은 아님. 신앙의 가호(부착 경로 보정)·Guardian 분기 추가 후 광전사 경로가 그대로인지 확인하는 회귀 |
| `Chapter4OpenedWorldTests` (전체) | 39행만이 아니라 게이트 4 / 20 단정 전체(37~39행 포함)를 5 / 25 로 갱신하거나 "챕터 4 시점 스냅샷"으로 분리 |
| `HeroStageVariantConfigTests` | 정본 엔트리 수 20 -> **25** (ChapterId=5 엔트리 5개) |
| `WorldMapTestSupport` | `ShippedDb` / `FullDb` 의 챕터 수 처리(5챕터 = `ChapterSlotCount` 와 같아 자리표시자 없는 DB) 점검 |

> 위 8개 파일은 메인이 `git show --stat 2eadb0d`(챕터 4 개방 커밋)와 대조해 확인한, 실제로 바뀐 파일이다.

---

## 9. 테스트 / QA 검증 항목

### 9.1 test-engineer (EditMode)

| 영역 | 항목 |
|---|---|
| `StageProgress` | 경계 20<->21, 25(종점), `IsUnlocked`(cleared 20 -> 21 해금, 22 잠금), `ResolveClearedStage` 상한 25, `MaxPlayableStage(25)` = 25, `ScaleHeroStat(50, 0.85, 1.40)` = 60 · `(50, 0.85, 1.80)` = 77 (.5 경계 2건) |
| 마이그레이션 | v6 값 보존 · `Version == 6`, Cleared 21~25 보존, `StageRecords` 26 이상 제거 |
| JSON 검증 | Guard / DebuffResist FK·범위, aiType=Guardian 의 Guard row 필수, 챕터 5 스테이지 5개, 트리거 0.75 / 0.35 검증 통과, **0.70 트리거 row 를 넣으면 검증 오류**(회귀) |
| 신앙의 가호 | (2026-11-01 현행: 역병귀 독 3 -> 1.8 · 독장판 항목 삭제 · `LingeringCurse` 와 곱 합성 1.75 x 0.6 = 1.05) 대상 8출처 지속/강도 보정(역병귀 1.5 -> 0.9, 독 5 -> 3, Fear 3 -> 1.8, Bleed 10 -> 6, Weaken 10 -> 6, `HeroAttackDown` 0.75 -> 0.85, T2 0.85 -> 0.91, T3 0.01 -> 0.006), **연장분도 x0.6**(같은 타입 재부착 - 현행 독은 연장이 아니라 갱신이라 갱신 지속 1.8초), `PlagueSlowOnHit`(현행 `PlagueToxinOnHit`) 직접 경로 적용, 제외 3종(`Slow` 10 · `TimeStop` 5 · `MarkOfDeath` 5) 불변, DebuffResist row 없는 영웅(기존 4명) 불변 |
| Guardian | 일반 최근접 + 고정 표적 회귀, 응징 발동(가해자 -> 즉시 표적), 2.0초 해제, 가해자 사망 해제, 재가해 시 갱신, 카드 출처 디버프는 미발동, FleeMode 우선, 중앙 끌림 없음, 다른 4영웅 경로 불변 |
| 스킬 | 망치: 사거리 5.0 밖 미시전, 반경 2.4 피해 110 + 기절 0.6초 후 복귀 / 정화: 대상 시한 디버프 1종이면 미시전, 2종이면 전부 해제 + 8% 회복, 영구 디버프 유지, 회복이 HP 임계를 재무장하지 않음 / 성역: 3마리 미만 미시전, 받는 피해 x0.6 · 구역 몬스터 x0.7 · 6.0초 후 복구 / 우선순위 정화 > 성역 > 망치 |

### 9.2 PlayMode / UnityMCP

- 월드맵: 마을 -> 지도 -> 챕터 5 탭(자리표시자 아님) -> 5-1 -> 출격 -> 결과 -> 5-2 해금. 4-5 클리어 후 5장 해금. 5-5 클리어 후 다음 해금 안내가 뜨지 않음.
- 5-5 보스까지 실제 플레이: 덫 75% / 결계 35% 발동, 스킬 게이트 85/65/45%, 카드 일시정지와 비중첩.

### 9.3 qa-simulator 게이트 (리포트 `docs/qa-reports/`)

| 게이트 | 대상 | 측정 | 통과 기준 |
|---|---|---|---|
| **G11** | 성기사 AI | 실효 DPS(스킬 제외), 교전 시간 비율, 표적 도달 시간, 응징 발동 후 가해 역병귀 처치까지 시간, 역병귀 독 커버율(독이 걸려 있는 시간 / 역병귀 생존 시간 - 2026-11-01 둔화 커버율에서 교체) | 실효 DPS 가 같은 인덱스 기사 DPS 의 95~120%(§3.2 중심 101~109% 를 감쌈), 교전 시간 비율 60% 이상, 응징 후 처치 평균 2.5초 이하, 독 커버율이 같은 빌드의 기사 대비 0.6배 이하 |
| **G12** | Ch5 곡선 | 스테이지별 평균 처치 시간 | 각 `targetClearSec` +-15%, 톱니(5-1 기대 시간 < 4-5 기대 시간), 4축 빌드별 편차 +-30초 |
| **G13** | Ch5 보스 | 5-5 첫 도전 승률, 보스 구간 시간, 페이즈 후 필드 평균 몬스터 수, 빌드별 승률, 정화의 빛 발동 횟수 | 승률 40~50%(상점 만렙 봇 기준, M_ref 1.35), 필드 점유 평균 12마리 이상, **Debuff 빌드 승률 < Tank·Dps·Swarm 각 빌드 승률**, **Debuff 빌드 승률 >= 전체 평균의 0.5배**(죽은 빌드 방지). 어긋나면 `durationMul`/`penaltyMul` 만 조정 |
| 보조 | 성수 샘(5-4) | 샘 개방 중 체류율 | 0.4 +-0.15 (`ruleFactor` 가정 검증) |
| 보조 | 정화의 빛 | 65% 이후 Debuff 빌드 1런당 발동 횟수 | 1회 이상. 0회면 `triggerDebuffCount` 를 1 로 낮추는 조정안 작성 |
| 보조 | 결계 x 성역 | 5-5 결계 15초 중 성역 겹침 비율, 마지막 35% 구간 시간 | 마지막 35% 구간 시간이 HP 비례 기대값 0.35 x 260 = 91초의 +20%(109초) 이하. 초과 시 결계 `durationSec` 조정안 |

---

## 10. 엣지 케이스

| # | 케이스 | 처리 |
|---|---|---|
| E-C5-1 | 역병귀 둔화와 Swarm `Slow` 카드가 같은 `SlowAura` 슬롯에 합쳐짐 | **(2026-11-01 현행) 해당 없음** - 역병귀가 `PlagueToxinAura`(별도 슬롯)를 쓰므로 `SlowAura` 는 Swarm `Slow` 카드 단독 슬롯이다. 정화는 `SlowAura` 를 대상 1종으로 세고 해제하는 규칙 그대로(아래 당시 기록 참고). 당시 기록: 패시브 배율은 부착 호출 단위(역병귀 0.9초, `Slow` 10초 그대로). 합쳐진 뒤에는 출처를 구분할 수 없으므로 정화는 `SlowAura` 슬롯을 **출처와 무관하게 대상 1종으로 세고 해제**한다(`Slow` 카드만으로 생긴 슬롯도 포함). Swarm 손실은 "다른 대상 디버프와 겹친 순간"에만 생기는 드문 경우로 수용, G13 Swarm 승률로 확인 |
| E-C5-2 | 응징 대상 역병귀가 적중 직후 넉백·이동으로 영웅 사거리(1.6) 밖으로 벗어남 | 이동 상태로 다가가 처치. 그 사이 다른 몬스터는 무시(2.0초 한정) |
| E-C5-3 | `BossBulwark`(x0.6)와 성역(x0.6) 동시 | 곱연산 0.36. 하한 없음, G13 보조 게이트로 확인 |
| E-C5-4 | `HunterTraps`(x0.5)와 성역 감속(x0.7) 동시 | 곱연산 0.35 |
| E-C5-5 | 정화의 빛 회복 8% | "최저 HP 비율"(패시브·스킬·보스 트리거 판정)은 회복으로 재무장하지 않는다(chapter-system E-1) |
| E-C5-6 | T3 영구 출혈 0.6% 가 반올림으로 0 | `RoundToInt(Max x 0.006)` 은 최대 HP 84 미만이면 0 이 된다. 성기사 최대 HP 는 앵커 x 1.10 x 1.56 이상이라 해당 없음을 G0 앵커 확정 시 확인 |
| E-C5-7 | 5-4 `SealedNest`(Plague) 동안 응징이 발동할 가해자가 없음 | 의도. 55% 해제 후부터 응징이 작동 |
| E-C5-8 | 5-5 클리어(cleared 25) 후 진행 | 다음 챕터 없음. 해금 안내·토스트가 "6장"을 가리키지 않는다(§8 #7) |
| E-C5-9 | 클라우드 복원 시 구버전(MaxStage 20) 클라이언트가 Cleared 21~25 를 복원 | 20 으로 잘림. 챕터 5 출시 시 신버전 동시 배포 |
| E-C5-10 | `DenseFog` 하한 0.8 이 영웅 충돌 거리보다 작음 | 충돌 거리 + 0.1 로 상향하고 보고(chapter-system E-7) |
| E-C5-11 | 망치 기절 중인 몬스터가 사망/봉인 처리 | 기절은 살아 있는 필드 몬스터에만, 사망 판정은 기존 `IDamageable` 경로 |

---

## 11. 미결 사항 · 사용자 승인 항목

| # | 항목 | 추천안 | 상태 |
|---|---|---|---|
| D-C5-1 | 챕터 5 범위 (CLAUDE.md §8 ⑦ 승격 완료), 신규 규칙 0종 | - | 승인 완료 (2026-10-18) |
| **D-C5-2** | 고유 패시브 정의 (§1.2): A 출처 한정 + 시한 지속 x0.6 + 영구 감소분 x0.6 / B 시한 지속만(시안) / C 타입 기준 전부 | **A** | **사용자 선택 필요** |
| **D-C5-3** | 팔레트 (§3.5): A 백은 + 금 / B 사파이어. A 의 5-3 발광이 배경과 동색 | **A** (묻히면 5-3 금 발광 변형) | **사용자 선택 필요** |
| **D-C5-4** | 데이터 구조표 (§2): 신규 스키마 `HeroArchetype_Guard` · `HeroArchetype_DebuffResist` 2종 분리 | 제안 구조 | **사용자 승인 필요** |
| **D-C5-5** | Guardian AI 디버프 반응 (§3.3): A 응징 표적 / B Melee 재사용 / C 방패 자세 | **A** | **사용자 선택 필요** |
| **D-C5-6** | 스킬 3종 신규 `$type` 및 신규 효과(몬스터 기절, 디버프 해제 + 회복), 정화 조건 2종 (§4) | 제안안 | **사용자 승인 필요** |
| D-C5-7 | 클립 이벤트 0.200s / 0.580s, `slash02`/`stab` = `slash01` 복제 (§3.4) | 채택 | 사용자 승인 필요 |
| D-C5-8 | 5단계 `ScaleMultiplier` 1.30 (§3.5) | 채택 | 사용자 승인 필요 |
| **D-C5-9** | 보스 페이즈 (§5.1): A 덫 0.75 + 결계 0.35 / B 3페이즈 / C 결계 0.75 + 덫 0.35. 시안 0.70 은 검증 불가 | **A** | **사용자 선택 필요** |
| D-C5-10 | `M_ref` 1.31~1.35(5-5 상점 100% 소진), 5-5 eq 2.70 (§5.3) | 채택 | 사용자 승인 필요 |
| D-C5-11 | `MetaProfile.Version` 6 유지 (§8 #3) | **6 유지** | 사용자 선택 필요 |
| D-C5-12 | 사용자 작업(Firebase 콘솔): Firestore `stage` 허용 범위 1~25 | - | 사용자 작업 |
| D-C5-13 | 수치 초안(스탯 §3.2, 스킬 §4, 스테이지 §5.3) - G0/G1 -> G11/G12/G13 검증 후 확정 | 초안 채택 | 사용자 승인 필요 |
| D-C5-14 | UI 목업: 신규 위젯 없음(5번째 탭은 데이터 구동) | 별도 목업 없음 | 확인만 필요 |

**수치 확정 방식**: 확정 순서는 G0(T1) -> G1(Ch1) -> G3(Ch2) -> G5~G7(Ch3) -> G8~G10(Ch4) -> G11(성기사 AI) -> G12(Ch5 곡선) -> G13(Ch5 보스). 결정 메트릭은 §9.3, 재산출 규칙은 chapter-system §0.4 도출식.

---

## 12. Self-Review

- **Placeholder 잔존**: 미정 마커·애매한 권유·두 갈래 위임(디자인 결정 영역)·어림 표현 없음. 미확정 수치는 "초안 - G0/G1 검증 후 확정" + §9.3 결정 메트릭. 선택 항목은 §11 "사용자 선택 필요". 구현 방식(인터페이스·통지 방식)만 gameplay-programmer 판단으로 위임(코드 구조 지정 금지 원칙).
- **내부 일관성**: 스탯(1.10/0.85/1.6/1.1/2.9), 패시브(0.6/0.6), Guard(2.0), 스킬 수치, 스테이지(hp 1.56/1.63/1.95/1.95/2.25, power 1.40/1.50/1.65/1.80/1.95, Scale 1.30), 트리거(0.75/0.35), 해제(0.60/0.55)가 §1~§11 에서 동일. 검산 §5.3.
- **명명 일관성**: `Paladin` / `Guardian` / `HeroArchetype_Guard`(`HeroArchetype_GuardRow`, `GuardSettings`) / `HeroArchetype_DebuffResist`(`HeroArchetype_DebuffResistRow`, `DebuffResistSettings`) / `retributionLockSec` / `durationMul` / `penaltyMul` / `HeroSkill_JudgmentHammer` · `HeroSkill_PurifyingLight` · `HeroSkill_Sanctuary`(`JudgmentHammerSkillData` · `PurifyingLightSkillData` · `SanctuarySkillData`) / `HeroJudgmentHammerFx` · `HeroPurifyingLightFx` · `HeroSanctuaryFx` / `Battle_Backdrop_FrostCathedral` / "신앙의 가호" 일관.
- **스코프**: 기획만. 시안은 이미 존재, 코드·커밋 없음.
- **구현 요청사항 완전성**: Enum / Interface / 에셋 키 / JSON·SO 스키마 §2·§7, 영향 범위 §8.
- **UI 목업**: 신규 UI 화면 없음. 배경·영웅 시안은 `.mockups/` 기존 파일.

### 구현 분할 제안

| 단위 | 내용 | 의존 |
|---|---|---|
| (a) 데이터/진행 | Chapter 5 row · Stage · 규칙 row, 스키마 2종 + 검증, Strings, `Chapter4OpenedWorldTests` 39행 임시 완화 (`ChapterGate` 는 올리지 않음) | 없음 |
| (b) 영웅 AI/패시브/스킬 | Guardian 응징 표적, 신앙의 가호(두 부착 경로 + 영구 강도), 스킬 3종 | (a) |
| (c) 아트 | 시트 5장 + 에미션 + 초상 5장 + FX 3종 + 배경 | 시안 승인 |
| (d) 개방 | 5번째 탭 실탭 확인(자리표시자 0), 5-5 상세 패널·종점(다음 챕터 없음) 검수, **마지막에 `ChapterGate.PlayableChapterCount` 5 상향** + 영향 테스트 갱신 + 임시 완화 원복 | (a)(b)(c) |
