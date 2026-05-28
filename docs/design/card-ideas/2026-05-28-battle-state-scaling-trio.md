# Card Ideas — 2026-05-28 — 전장 상태 감지 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 전장 상태 감지 — 픽 시점의 필드 상황(몬스터 종류 수 / 개별 HP 비율 / 영웅 오라 누적)이 효과 강도를 결정하는 카드 3장
- **목록**: 군중의 포효 (Active/버프) / 약자의 기백 (Passive/강화) / 연쇄 저주 (Active/저주)
- **기존 25장 + git log 과거 회차 중복 회피 확인**: git log 조회 결과 루틴 과거 커밋 0건 (첫 회차). 기존 25장 전원 효과 검토 후 개념적 중복 없음.

---

## 1. 군중의 포효 (Horde Roar) — 가칭

- **카테고리**: 액티브 버프
- **효과 모델**:
  - 발동 시점 필드에 살아있는 몬스터 종류(EMonster 값 기준) 수 × 12% 만큼, 모든 몬스터의 공격속도가 **15초간** 증가.
  - 1종 = +12% / 2종 = +24% / 3종 = +36% / 4종 = +48% / 5종 = +60% / 6종 = +72%
  - 발동 즉시 IBattleContext를 통해 종류 수를 스냅샷 → 배율 결정 → MonsterBuffService 전달.
  - 밸런스 근거 (컨셉 §8): 기준 카드 Berserk (+50% 공속 10초, QA pick 7%). 교체 카드를 자제해 3~4종을 유지한 빌드에서 +36~48% / 15초 = Berserk와 비등하거나 우세. 6종 유지 시 +72% / 15초로 최강 발동.

- **구현 패턴**: 기존 MonsterBuffService + IBattleContext 패턴 재사용.
  - IBattleContext에 `GetAliveMonsterKindCount()` 메서드 신규 추가 (기존 `GetMonsters()` 기반 Distinct LINQ 유도 가능).
  - `MonsterBuffService.AddBuff(EMonsterBuff.AttackSpeed, duration: 15f, scale: kindCount * 0.12f)` 호출.

- **시너지 후크**:
  - Spawn 계열 카드(SpawnWisps / SpawnReapers / SpawnPhantoms 등)로 다종 필드를 유지한 뒤 발동 시 최대 효율.
  - 교체 카드(ReplaceWispsToWraith / ReplaceReapersToHex)를 자제하는 "다양성 빌드" 전략을 처음으로 보상하는 카드.

- **구현 비용 추정**: **2** — IBattleContext 메서드 1개 신규 추가 + MonsterBuffService 기존 호출 패턴.

- **중복 재검증**: 기존 Berserk은 고정 +50% / 10초 단순 적용. 이 카드는 전략적 선택(다양성 유지 vs 교체 최적화)에 따라 강도가 달라지는 조건부 스케일링 구조 — 개념 차원이 다름.

---

## 2. 약자의 기백 (Wounded Pride) — 가칭

- **카테고리**: 패시브 강화
- **효과 모델**:
  - (영구 효과) 매 tick마다, HP가 자신 최대값의 **50% 이하**인 몬스터는 공격력 **+35%** 적용.
  - MonsterBuffService가 기존 "매 tick 재적용" 루프에서 개별 몬스터의 `IHealth.Ratio < 0.5f` 조건을 체크해 해당 몬스터에만 PowerScale 적용.
  - 밸런스 근거: WraithDamageBoost (레이스 +50% 데미지 영구) 와 비교 — 적용 대상이 전 종(種)으로 넓지만 HP 조건으로 발동 타이밍이 제한됨. 교전 중 평균 20~40% 몬스터가 50% 이하 HP 상태임을 가정하면 전체 DPS 기대 상승폭 약 +10~15%. 종(種) 강화보다 낮지만 단일 종 의존 없이 보편 적용.

- **구현 패턴**: MonsterBuffService 기존 "매 tick 재적용" 구조 자연 확장.
  - `MonsterBuffService.Tick()` 내부 몬스터 순회 시 `IHealth.Ratio < 0.5f` 조건 추가.
  - 조건 충족 몬스터에 `MeleeAttacker.PowerScale *= 1.35f` (tick마다 base 1.0 리셋 후 재적용).

- **시너지 후크**:
  - WispHpBoost(위스프 HP +50%)와 조합: 위스프가 더 오래 생존하다가 50% 선 아래로 떨어지는 순간 공격력 폭발 — 위스프를 "HP 버퍼 → 분노 돌입"으로 전환.
  - 장기전(2~4분 타겟 밴드 후반부)에서 필드 몬스터들이 점점 HP를 소모하면서 패시브 가치가 누적 상승.

- **구현 비용 추정**: **3** — MonsterBuffService.Tick() 내 개별 HP 조건 처리 추가. 기존 전체 재적용 로직과 조건부 개별 적용 로직을 함께 관리하는 설계 필요.

- **중복 재검증**: 기존 강화 패시브 6장(WispHpBoost / WraithDamageBoost / ReaperAtkSpeed / HexRangeBoost / PlagueSlowBoost / PhantomMoveSpeedBoost)은 모두 "특정 종(種)" 기반 무조건 영구 버프. 이 카드는 "HP 상태" 기반 조건 버프 — 트리거 축 자체가 다름.

---

## 3. 연쇄 저주 (Chain Curse) — 가칭

- **카테고리**: 액티브 저주
- **효과 모델**:
  - 영웅 이동속도 **-15% (기저)** + 발동 직전 활성화된 HeroAura 수 × **5% 추가 감소** (10초 지속).
  - 오라 0개: -15% / 1개: -20% / 2개: -25% / 3개: -30% / 4개: -35%
  - 발동 직전 HeroAuraRunner의 현재 오라 수 스냅샷 기준 — 이 카드 자신의 SlowAura는 카운트에서 제외.
  - 밸런스 근거 (컨셉 §8): 기준 카드 Slow (-50% 이속 10초, QA pick 18%). 단독에서는 -15%로 약하지만 환경 빌드(HeroPoisonAura + HeroAttackDown 이미 2개 활성)에서 -25% 추가로 영웅을 독 장판 위에 완전히 고정. 순수 Slow의 대안이 아닌 "환경 빌드 마무리" 포지션.

- **구현 패턴**: IHeroAura 기존 Attach 패턴 재사용.
  - IBattleContext에 `GetHeroActiveAuraCount()` 메서드 신규 추가 (HeroAuraRunner의 활성 슬롯 수 노출).
  - `HeroAuraRunner.Attach(new SlowAura(rate: 0.15f + auraCount * 0.05f, duration: 10f))`.

- **시너지 후크**:
  - HeroPoisonAura + HeroAttackDown이 이미 깔린 환경 빌드에서 발동 → 이속 -25% 추가 잠금 → 영웅이 독 장판 탈출 불가 → HeroPoisonAura DPS 대폭 상승.
  - Bleed 카드(이동 시 HP -2% / 10초)와 조합: 영웅 이속 감소로 탈출 빈도 줄이면서 이동 시 HP 손실 기회를 최대화.

- **구현 비용 추정**: **2** — IBattleContext 메서드 1개 신규 + HeroAuraRunner.Count 노출 + 기존 SlowAura 재활용.

- **중복 재검증**: 기존 Slow는 고정 -50% 단순 적용. 이 카드는 기저 -15%에서 시작해 기존 저주 누적으로 스케일링 — "저주 빌드의 마무리" 포지션으로 Slow와 용도 및 강도 구간이 다름.

---

## 4. 공통 테마 고찰

세 카드 모두 **"픽 시점의 전장 상태를 읽어 효과 강도를 결정"** 하는 새로운 축을 도입한다. 기존 25장은 효과가 픽 즉시 고정값으로 결정(종 강화 / 즉시 소환 / 교체 / 고정 디버프)이지만, 이 3장은 다음을 측정해 보상한다:

| 카드 | 읽는 상태 | 전략적 의미 |
|---|---|---|
| 군중의 포효 | 필드 몬스터 종류 수 | 교체 자제하는 다양성 빌드 보상 |
| 약자의 기백 | 개별 몬스터 HP 비율 | 장기전 후반 몬스터 HP 소모를 역이용 |
| 연쇄 저주 | 영웅 활성 오라 수 | 환경/저주 카드 중복 픽을 보상 |

**이 테마를 선택한 이유 — QA 데이터 기반:**
- QA 6차 리포트(2026-05-26): Berserk 7%, Fear 13%, Slow 18% — 단순 고정값 일회성 카드들이 최하위 pick.
- 이 카드들과 **같은 카테고리**(버프/강화/저주)이지만 조건부 스케일링으로 전략적 깊이를 추가 → v0.2 풀에서 해당 카테고리를 근본적으로 강화.
- 특히 군중의 포효는 QA §3.4에서 전략 분산이 span 1.9%로 수렴 현상을 보이는 원인 중 하나인 "단일 종 교체 집중" 전략에 처음으로 대안 보상을 제공, 전략 다양성 확대 기대.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **우선 채택 추천 순서**: 구현 비용 2인 군중의 포효 → 연쇄 저주 선행 구현 후, MonsterBuffService 확장이 필요한 약자의 기백(비용 3)을 후행 도입.
- 군중의 포효는 IBattleContext 메서드 패턴이 이미 확립되어 있어 신규 카드 효과 구현 중 가장 낮은 리스크.
