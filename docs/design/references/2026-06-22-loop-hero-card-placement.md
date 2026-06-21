# Reference Analysis — 2026-06-22 — Loop Hero / 카드 배치 조합

> 자동 생성 (매주 월 07:01 KST) — Project Lair Weekly Reference 루틴 (Rule 01 자동화 예외).
> Project Lair 에 적용 가능한 패턴 설계 목적. v0.2 포함 장기 계획 반영용.

## 0. 선정

- **게임**: Loop Hero (Four Quarters, 2021)
- **시스템**: 카드 배치 조합 (Tile Placement & Combination Synergy)
- **선정 이유**: Project Lair 는 5분 런 안에 최대 18회 카드 픽을 하는데, 현재 시너지는 "같은 축 N장 = 임계 버프"라는 카운트 기반이다. Loop Hero 의 타일 배치 조합은 "특정 타일 A 옆에 B 배치 → 전혀 다른 효과 C 발생" 이라는 페어 기반 변환이며, 이를 응용하면 카운트 임계 외에 '조합 변환' 이라는 새로운 빌드 축을 만들 수 있다. 또한 현재 Project Lair 의 Spawner Ring(§4.1) 이 공간적 배치 구조를 갖고 있어 Loop Hero 의 인접 타일 시너지와 직접 대응된다.
- **git log 과거 회차와 중복 없음 확인**: 4건 조회 (VS 패시브시너지 / RoR2 아이템중첩 / Hades 보우니레어리티 / StS 덱압축). Loop Hero 카드배치조합은 첫 등장.

---

## 1. 레퍼런스 메커니즘 분석

### 1.1 동작

Loop Hero 에서 플레이어는 직접 전투하지 않는다. 영웅이 자동으로 루프 경로를 순환하며 싸우는 동안, 플레이어는 맵 그리드에 "타일 카드"를 배치한다. 핵심은 타일이 독립적 효과를 갖는 동시에, **인접 타일 조합이 전혀 다른 세 번째 효과를 만든다**는 점이다. 예: 숲(Forest) 2칸 = 울창한 숲(Thicket) 발생 → 경험치 보너스, 숲 6칸 = 야영지(Campfire) 자동 생성 → 영웅 HP 회복. 산(Mountain) + 바위(Rock) = 봉우리(Peak) → 주변 몬스터 HP 상승 + 더 좋은 전리품. 이처럼 단일 타일을 조합하면 **질적으로 다른 새로운 상태**가 발생한다.

두 번째 핵심은 **누적 영속성**이다. StS 의 카드는 사용 후 버려지지만 Loop Hero 의 타일은 배치 후 런 내내 효과가 지속되고 쌓인다. 맵이 타일로 채워질수록 세계 자체가 변화한다. 세 번째는 **리스크/리워드 연동**이다. 타일이 많을수록 몬스터가 강해지지만 보상도 커진다 — 플레이어는 적의 강함과 자신의 성장 속도 사이에서 배치 타이밍을 선택한다.

### 1.2 왜 잘 될까

카드 픽이 "수치 증가"에 머물지 않고 "세계 변환"이 되기 때문이다. 플레이어는 단순히 강해지는 게 아니라 전장의 생태계를 구성하는 감각을 느낀다. "Forest 2장 더 놓으면 야영지가 생겨서 HP가 회복된다"는 지식이 플레이어 숙련의 핵심이 되고, 메타 지식이 쌓일수록 더 재밌어진다. 또한 인접 조합의 수가 경우의 수를 폭발적으로 늘리기 때문에, 동일한 타일 세트로도 배치 순서·위치에 따라 전혀 다른 런이 된다. 이것이 리플레이어빌리티를 만든다.

### 1.3 제약·한계

타일 배치 그리드가 있어야 공간적 인접이 정의된다. Project Lair 는 그리드가 없지만, **Spawner Ring이라는 순환 배열**이 있어 "인접 스포너" 개념은 정의 가능하다. 카드 픽은 공간 배치가 아니라 순서 선택이므로 "인접" 개념은 "동일 런에서 함께 픽됨"으로 재해석해야 한다. 카드 종류가 28장인 Project Lair MVP 에서 모든 페어 조합(최대 378가지)을 설계하는 것은 비현실적이므로, 소수의 고임팩트 페어만 큐레이션해야 한다.

---

## 2. Project Lair 적용 후보 (패턴 3개)

### 2.1 패턴 1: 스포너 링 인접 쌍 시너지 (Spawner Ring Neighbor Synergy)

- **적용 대상**: `CircularSpawnerArranger` / `SpawnerRingController` / `IBattleContext`
- **구현 스케치**: Loop Hero 의 "Forest 2장 = Thicket" 처럼, Spawner Ring 에서 **인접한 두 Spawner 종이 특정 페어를 이루면 영속적 존 효과**를 활성화한다. 예: Plague Spawner(독) 와 Wraith Spawner(강인함) 가 Ring 에서 이웃이면 → 두 스포너 사이 구역에 "독안개 지대(PoisonFog Zone)" 가 생겨 영웅이 통과할 때 둔화가 적용된다. `SpawnerRingController` 가 초기화 시 Ring 배열을 순회하며 이웃 페어를 탐지하고, `NeighborSynergyTable` (SO: `List<NeighborSynergyDef>`) 에서 해당 페어에 대응하는 `IZoneEffect` 를 `IBattleContext` 에 등록한다. Spawner 가 카드 픽으로 교체될 때마다 Ring 을 재계산한다.
- **구현 비용**: 3
- **MVP §11 호환**: O (Spawner Ring 구조 §4.1 위에 레이어 추가. 신규 씬·영웅·몬스터 종 없음)
- **적재 타이밍**: Alpha
- **의존 패턴**: `CircularSpawnerArranger` (이미 구현), `IBattleContext` Zone 등록 API, `NeighborSynergyDef` SO 스키마

---

### 2.2 패턴 2: 카드 페어 조합 변환 (Card Pick Pair Transform)

- **적용 대상**: `BattleViewModel` (or `CardSelectionViewModel`) / `CardComboPairRegistry` / 카드 HUD
- **구현 스케치**: Loop Hero 의 "Mountain + Rock → Peak" 처럼, **특정 두 카드를 같은 런에서 픽하면 즉시 '조합 효과(Combo Effect)' 가 발동**되고 이후 런 내내 지속된다. 이는 축 카운트 임계(3/5/7)와 독립적이며 **크로스 축 조합**도 가능하다. 예: `SpawnWraith`(Tank) + `SpawnPlagues`(Debuff) → "독 레이스 콤보" 활성 → 이후 스폰되는 Wraith 가 공격 시 Plague 의 둔화를 자동 적용. 구현: `CardComboPairRegistry` SO (`List<CardComboDef { ECardId A, ECardId B, IComboEffect }>`), `CardSelectionViewModel.OnCardPicked()` 에서 현재 픽 목록을 검사해 완성된 페어를 발화. HUD 에 "콤보 활성!" 미니 토스트. 페어 목록은 MVP 단계에서 3~5쌍으로 시작해 점진 확장.
- **구현 비용**: 3
- **MVP §11 호환**: O (기존 카드 픽 플로우 확장. 신규 카드 종 없음)
- **적재 타이밍**: Alpha
- **의존 패턴**: `CardSelectionViewModel.OnCardPicked` 훅, `IComboEffect` 인터페이스, 기존 `IBattleContext` 버프 등록 체계

---

### 2.3 패턴 3: 빌드 환경 누적 변환 (Build Environment Accumulation)

- **적용 대상**: `BattleEnvironmentState` (신규 Model) / `CardEffectDispatcher` / `BattleZoneController`
- **구현 스케치**: Loop Hero 의 타일이 런 내내 누적되어 세계 자체를 바꾸듯, **카드 픽이 '전장 환경 레벨'을 축적하고, 레벨 임계에서 전장 공간 자체에 영구 변화**를 준다. 예: Debuff 축 카드를 픽할 때마다 `BattleEnvironmentState.DebuffLevel++`. DebuffLevel 3 달성 시 전장 외곽 ring 에 "독안개 띠(PoisonBelt)" 가 영구 생성되어 영웅이 외곽으로 도주할 때 지속 데미지를 받는다. 이는 현재의 Debuff Tier1(SlowFactor 강화)과는 질적으로 다른 **공간적** 효과다. `BattleEnvironmentState` 는 카드 픽 이벤트를 구독해 각 축 레벨을 추적하고, 임계 도달 시 `BattleZoneController` 가 해당 Zone Prefab 을 Addressable 로 팝해 필드에 배치한다.
- **구현 비용**: 4
- **MVP §11 호환**: O (전장 공간 변형은 §4.1 Spawner Ring 위에서 동작, 신규 종 없음. `BattleEnvironmentState` 는 신규 모델이지만 기존 `IBattleContext` 와 연동 가능)
- **적재 타이밍**: Beta (전장 비주얼 Zone Prefab 준비 필요)
- **의존 패턴**: `CardEffectDispatcher` 이벤트 파이프라인, `CHMPool.Pop` (Zone Prefab 스폰), `BattleZoneController` (전장 공간 관리)

---

## 3. 우선순위 제안

**패턴 2 (카드 페어 조합 변환)** 를 먼저 고려 권장.

이유: 패턴 2 는 기존 카드 픽 플로우에 관찰(OnCardPicked 훅)과 SO 한 장만 추가하면 되므로 구현 비용이 가장 낮다. 동시에 "같은 카드 28장으로 새로운 빌드 조합을 발견하는 재미"를 크게 높이는데, 이는 현재 v0.3 에서 서버 연동 외에 기존 전투 루프의 깊이를 올릴 수 있는 가장 경제적인 방법이다. 피이크 임계(3/5/7) 와 카드 페어 조합이 공존하면 빌드업 의사결정이 두 레이어를 갖게 돼 런 중 전략 다양성이 극적으로 상승한다.

패턴 1 (스포너 인접 시너지) 은 패턴 2 다음으로 채택 권장. Spawner Ring 공간성을 활용한 독창적 메커니즘이며 타 게임과의 차별점을 만든다.

패턴 3 (환경 누적 변환) 은 비주얼 작업을 동반해야 하므로 Beta 단계로 미룬다.

---

## 4. 채택 흐름 제안

- **패턴 2 (Alpha 채택 시)**: 이 문서를 입력으로 game-designer 에게 `CardComboPairRegistry` 상세 기획 의뢰. MVP §11.3 기존 28장 카드로 크로스 축 페어 3~5쌍을 선정하고 효과 수치 정의 → gameplay-programmer 구현 → test-engineer 에서 각 페어 콤보 발동/미발동 엣지 테스트.
- **패턴 1 (Alpha 채택 시)**: `continuous-spawn-round.md` 와 함께 이 문서를 game-designer 에 전달. Spawner Ring 인접 정의(몇 칸 이내 = 이웃?) 확정 및 유효 페어 2~3쌍 기획.
- **패턴 3 (Beta backlog)**: v0.3 이후 비주얼 에셋 작업 시점에 재논의.

---

## 5. 쉬운 설명 (비개발자 요약)

루프 히어로는 영웅이 혼자 싸우는 동안 플레이어가 지도 위에 카드를 하나씩 놓는 게임이다. 재미있는 부분은, 같은 종류의 카드를 옆에 붙여 놓으면 예상치 못한 새로운 일이 벌어진다는 것이다 — 숲 카드 두 장을 나란히 놓으면 자동으로 야영지가 생겨 영웅이 회복을 받는 식이다.

Project Lair 도 비슷한 구조다. 영웅이 혼자 싸우고, 우리는 카드를 골라 몬스터들을 강화한다. 지금은 같은 계열 카드를 3·5·7장 모으면 보너스가 터지는데, 루프 히어로처럼 "이 카드 + 저 카드를 함께 고르면 완전히 새로운 효과가 생긴다"는 조합 규칙을 추가하면 훨씬 전략적인 재미가 생긴다. 예를 들어 "레이스 추가 소환" 카드와 "플레이그 소환 증가" 카드를 같은 판에 모두 고르면, 레이스가 자동으로 독을 묻히게 되는 식이다 — 미리 계획했다면 더 큰 보람, 우연히 발견했다면 "오, 이런 조합이 있었구나!" 하는 발견의 기쁨이 된다.

그래서 이번에 참고하려는 것은: 루프 히어로처럼 특정 카드 두 장을 함께 픽했을 때 예상치 못한 새로운 효과가 발동되는 "카드 페어 조합 변환" 시스템을 Project Lair 의 카드 픽 흐름에 얹는 패턴이다.
