# Card Ideas — 2026-06-04 — Dps × Debuff 교차 사냥 전략

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: Dps × Debuff 교차 사냥 전략 — 둔화·독·약화로 약해진 영웅에게 딜러(리퍼·헥스)가 폭발적으로 반응
- **목록**: 사냥꾼의 본능 (HuntersInstinct) / 독날 채찍 (VenomStrike) / 절박한 분노 (DespairFrenzy)
- 기존 28장 + git log 과거 7회차와의 중복 회피 확인됨
  - 기존 28장: 둔화 영웅에게 딜러 추가 피해 없음, 리퍼 공격 연동 독 없음, HP 비율 스케일 버프 없음
  - 과거 루틴: 낙인(브랜드) / 리퍼헥스 딜러 심화(격살·연사·처형 부대) / 죽음의 메아리 등과 개념적으로 무관

---

## 1. 사냥꾼의 본능 (HuntersInstinct) — 가칭

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - 영구 글로벌 효과. 리퍼·헥스가 영웅을 공격하는 순간, 영웅 이동속도가 기준값(3.0)보다 낮으면(둔화 상태) 해당 공격의 기본 피해에 추가로 **+60% 즉시 데미지**를 한 번 더 적용.
  - 예시: 리퍼 Power 40, 영웅 둔화 상태 → 기본 40 + 추가 24 = 총 64 피해
  - 수치 근거: Frenzy(공속 +50%)가 최강 단기 버프인데, HuntersInstinct는 조건부 영구 적용이므로 강도를 60%로 설정해 "조건 달성 시에만 강력" 구조 유지
- **구현 패턴**: `SpiderSlowOnHit`의 역방향. `HuntersInstinctEffect.Apply` → `ctx.GetMonsters(EMonster.Reaper)` + `ctx.GetMonsters(EMonster.Hex)` 순회 → 각 몬스터 GameObject에 `HuntersInstinctBuff` 컴포넌트 추가. `HuntersInstinctBuff.Awake` → `IAttacker.OnHit` 구독 → `OnHit(hero)` 시 `heroMover.Speed < 3.0f` 이면 `hero.TakeDamage(power * 0.6f)`. OnEnable/OnDisable 구독 관리 (풀 재사용 안전)
- **시너지 후크**:
  - **Debuff 축 패시브**: `PlagueSlowBoost` + `SpawnPlagues` → 플레이그가 항상 영웅을 둔화 → HuntersInstinct 발동 확률 최대화
  - **Swarm 액티브**: `Slow`(영웅 이속 ×0.5, 10s) → 확실한 둔화 보장 → 딜러와 연계
  - **Debuff Tier1 시너지**: Plague SlowFactor ×0.8 강화 → 더 강한 둔화 → 조건 달성 쉬워짐
- **구현 비용 추정**: 3 (SpiderSlowOnHit 패턴 재활용, IAttacker.OnHit + IMover.Speed 체크. 신규 시스템 없음)
- **중복 재검증**: 기존 28장 중 "공격 조건부 추가 피해" 카드 없음 ✅. 과거 7회차 루틴(낙인/죽음의메아리/위스프벽 등)과 테마·효과 모두 무관 ✅

---

## 2. 독날 채찍 (VenomStrike) — 가칭

- **카테고리**: 액티브 저주 (Debuff 축)
- **효과 모델**:
  - 발동 후 **10초간**, 리퍼가 영웅을 공격할 때마다 영웅에게 `PoisonBladeAura`를 **재부착** (duration 2.0초, 1s마다 HP -1.5%).
  - 리퍼 공속이 1.0s 쿨다운이면 2.0초 안에 재타격 → 독 오라가 연속 갱신되어 실질 지속. 공격 없이는 2초 후 자동 소멸.
  - 수치 근거: Bleed(이동 시 HP -2%/s, 10s)보다 발동 조건(리퍼 명중)이 엄격하므로 DPS는 비슷하게 설정. 리퍼 공격이 끊기면 효과도 끊기는 "집중 딜 보상" 구조.
- **구현 패턴**: `VenomStrikeEffect.Apply` → `_buffActive = true` 플래그 10초 코루틴 or MonsterBuffService에 VenomStrike 버프 추가 → Tick마다 리퍼 OnHit 발생 감지 or VenomStrike 버프 활성 중 리퍼의 OnHit 이벤트에서 `ctx.ApplyHeroAura(new PoisonBladeAura(dps: 15f), 2.0f)` 호출. `PoisonBladeAura`는 기존 `PoisonAura` 클래스에 dps 파라미터만 다르게 재사용
- **시너지 후크**:
  - **Dps 패시브**: `ReaperAtkSpeed`(리퍼 쿨다운 ×0.7) → 공격 빈도 증가 → 독 재부착 빈도 ↑, 실질 독 DPS ↑
  - **HuntersInstinct**: 리퍼가 독으로 둔화된 영웅(기본값 미만 속도)을 치면 HuntersInstinct 추가 피해까지 발동 → 두 카드 콤보로 단타 대미지 극대화
  - **SpawnReapers**: 리퍼 출력 +1 → 독 부착 확률 두 배로 상승
- **구현 비용 추정**: 3 (BleedEffect + SpiderSlowOnHit 패턴 결합. PoisonAura 재사용으로 신규 Aura 클래스 불필요)
- **중복 재검증**: 기존 `HeroPoisonAura`는 독 장판(영웅 위치 기반), `Bleed`는 이동 시 출혈 — VenomStrike는 "리퍼 공격 명중 시 독 갱신"으로 트리거가 완전히 다름 ✅. 과거 루틴의 "plague-poison-chain(낙인 트리오)"은 브랜드/낙인 계열로 독 채찍과 무관 ✅

---

## 3. 절박한 분노 (DespairFrenzy) — 가칭

- **카테고리**: 액티브 와일드
- **효과 모델**:
  - 발동 시점 영웅 HP 비율을 측정해 강도가 달라지는 15초 광폭화:
    - HP ≥ 60%: 모든 몬스터 공격속도 +15%
    - HP < 60% · ≥ 30%: 공격속도 +35%
    - HP < 30%: 공격속도 +55% + 이동속도 +20%
  - Frenzy(공속 +50%, 10s)보다 조건부 강함/약함이 교차하여 선택 딜레마 형성. "지금 쓸까, 영웅 HP를 더 낮추고 쓸까?"가 핵심 결정지점
  - 수치 근거: HP < 30% 조건(= 영웅이 이미 많이 쳐진 상태)에서야 Frenzy를 초과하는 강도로 설계 → 위험 감수 보상
- **구현 패턴**: `DespairFrenzyEffect.Apply` → `float ratio = ctx.GetHero().Current / (float)ctx.GetHero().Max` → ratio에 따라 CooldownScale과 MoveSpeedScale 결정 → `MonsterBuffService.AddBuff(DespairFrenzy, 15f, cooldownScale, speedScale)`. MonsterBuffService 에 스케일 파라미터 수용 확장 필요 (기존 enum 단순 버프에서 float 페이로드 버프로 확장, 구현 비용에 포함)
- **시너지 후크**:
  - **Debuff 축 전체**: Bleed·Weaken·Fear로 영웅 HP를 30% 이하까지 빠르게 낮추면 +55% 공속 발동 → Debuff→DespairFrenzy 연계가 핵심 라인
  - **HuntersInstinct**: HP 30% 이하 영웅이 둔화 상태일 때 리퍼가 치면 HuntersInstinct까지 발동 → 3카드 콤보 클리어 루트
  - **Dps 시너지 Tier2**: Reaper·Hex Cooldown ×0.8과 DespairFrenzy +55% 공속 중복 → 최소 쿨다운 수렴
- **구현 비용 추정**: 3 (IBattleContext HP 비율 체크 단순. MonsterBuffService float 페이로드 확장은 내부 리팩터이므로 외부 인터페이스 변경 없음)
- **중복 재검증**: 기존 `Frenzy`(공속 +50%, 10s 단일 강도)와 달리 HP 비율 스케일링 구조 ✅. 과거 루틴 `battle-state-scaling-trio`(전투 상태 스케일링)와 유사해 보이지만, 그 루틴은 HP%/시간 트리거 기반 패시브 강화였고 DespairFrenzy는 "발동 시점 상태에 따른 강도 분기 액티브"로 카테고리·시점·메커니즘이 다름 ✅

---

## 4. 공통 테마 고찰

세 카드는 **"약해진 영웅에게 딜러가 더 강하게 반응"** 이라는 축으로 묶인다:
- HuntersInstinct: 둔화 영웅 → 딜러 즉시 추가 피해 (공격 시 조건부 활성화)
- VenomStrike: 리퍼 공격 → 독 부착 → 영웅 체력 지속 소모 (딜러와 도트의 연결)
- DespairFrenzy: 영웅 HP 낮을수록 더 강한 공속 버프 → 이미 몰린 영웅을 더 빠르게 압박

**왜 이 테마를 오늘 골랐는가:**
- QA 리포트(`2026-05-22.md`)가 BLOCKED로 픽률 데이터가 없어, 카드 풀의 공백을 구조적으로 분석.
- 현재 28장은 단일 축 내부 강화(예: 리퍼 공속 ×0.7, 헥스 사거리 ×1.4)가 주를 이루며, **Dps축과 Debuff축이 상호 작용하는 교차 카드가 전무**.
- 실전에서 "플레이그로 둔화 → 리퍼로 집중" 전략이 직관적으로 유효하나 이를 보상하는 카드가 없어 빌드 선택의 시너지 보상이 약함.
- 이 세 카드로 "Debuff 빌드 → Dps 빌드 연계"라는 멀티 축 전략선이 추가됨.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **우선순위 제안**: DespairFrenzy > HuntersInstinct > VenomStrike 순서. DespairFrenzy는 MonsterBuffService 내부 확장만으로 낮은 리스크에 결정지점(decision point)이 명확해 게임성 기여가 즉각적임. HuntersInstinct는 SpiderSlowOnHit 패턴 재사용으로 안정적 구현. VenomStrike는 OnHit 이벤트 + Aura 조합으로 가장 복잡하지만 기존 패턴 안에 있음.
- 세 카드 모두 채택 시 Dps/Debuff 시너지 Tier 달성 임계(3/5/7) 계산에 포함되므로 기존 시너지 카운터(`card-renewal.md §4`)와 정합 필요.

---

## 6. 쉬운 설명 (비개발자 요약)

지금까지 몬스터들은 영웅을 "열심히 때리기만" 했는데, 오늘 제안하는 카드들은 "영웅이 약해진 상태일 때 더 영리하게 반응"하는 능력을 줍니다. 예를 들어, 리퍼라는 빠른 몬스터가 느려진 영웅을 칠 때 갑자기 두 배로 세게 때린다거나, 독을 발라서 영웅이 점점 체력을 잃게 만든다거나, 영웅이 이미 많이 다쳤을 때 모든 몬스터가 훨씬 빠르게 공격하는 식입니다. 지금 카드 풀에는 "플레이그로 영웅을 느리게 만들고 → 리퍼로 집중 공격" 같은 연계 전략이 있는데도 이를 보상해주는 카드가 없었는데, 이 세 장이 그 빈틈을 채웁니다. 그래서 오늘 제안하는 카드 3장은: 약해진 영웅에게 더 강하게 반응하는 "사냥꾼의 본능(HuntersInstinct)", 리퍼 공격에 독을 실어 체력을 갉아먹는 "독날 채찍(VenomStrike)", 그리고 영웅이 위기에 처할수록 더 강해지는 "절박한 분노(DespairFrenzy)"입니다.
