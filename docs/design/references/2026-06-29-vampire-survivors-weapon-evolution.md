# Reference Analysis — 2026-06-29 — Vampire Survivors / 무기 진화 (Weapon Evolution)

> 자동 생성 (매주 월 07:01 KST) — Project Lair Weekly Reference 루틴 (Rule 01 자동화 예외).
> Project Lair 에 적용 가능한 패턴 설계 목적. v0.2 포함 장기 계획 반영용.

## 0. 선정

- **게임**: Vampire Survivors
- **시스템**: 무기 진화 (Weapon Evolution)
- **선정 이유**: Project Lair의 카드 중첩 픽 시스템(같은 카드를 여러 번 선택 가능)과 4축 시너지 누적 구조는 VS의 무기 진화와 거의 같은 뼈대를 공유한다. 현재 Project Lair에는 중첩 픽에 대한 "질적 전환" 없이 선형 배율만 누적되는데(예: WispHpBoost ×3 = HP ×1.5 × 1.5 × 1.5), VS 진화 메커니즘은 이 "N번째 픽에서 별도 형태로 변신"하는 패턴을 구체적으로 보여준다. 또한 진화 조건 가시화(무기 레벨 바)는 Project Lair 의 카드 선택 팝업의 "빌드 축 카운트 바" 개선 방향과 직결된다.
- **git log 과거 회차와 중복 없음 확인**: 4건 조회 — VS/passive-synergy(2026-05-28), RoR2/item-stacking(2026-06-01), Hades/boon-rarity(2026-06-08), StS/deck-compression(2026-06-15). 이번 (VS, weapon-evolution) 조합은 신규.

---

## 1. 레퍼런스 메커니즘 분석

### 1.1 동작

Vampire Survivors의 무기 진화는 두 가지 경로가 있다.

**경로 A — 단일 무기 자체 진화**: 무기를 최대 레벨(Lv.8)까지 올리면, 보석함(Treasure Chest)에서 특정 수동 아이템과 함께 등장할 때 진화 버전을 획득한다. 예를 들어 `Whip Lv.8 + Hollow Heart` → `Bloody Tear`. 진화된 무기는 원본의 효과를 모두 포함하면서 완전히 새로운 시각 이펙트, 대폭 강화된 수치, 추가 특수 효과를 가진다. 무기 레벨 바가 항상 화면에 표시되므로 플레이어는 "진화까지 몇 레벨 남았다"를 직관적으로 파악한다.

**경로 B — 2무기 합성 진화 (Union)**: 두 가지 최대 레벨 무기를 동시에 보유할 때 특정 조건에서 Union 무기로 합성된다. `Peachone + Ebony Wings` → `Vandalier`. 두 무기의 특성이 하나로 녹아드는 개념.

진화한 무기는 원본 슬롯을 그대로 유지하면서 시각·수치·특수 기전이 변환된다. 픽업 순간 화면 전체에 연출이 발생해 "무언가 크게 달라졌다"는 느낌을 즉각 전달한다.

### 1.2 왜 잘 될까

**목표 지향적 루프**: 플레이어는 레벨업마다 단순히 "좋은 걸 고른다"가 아니라 "진화를 위해 이 무기를 계속 올린다"는 명확한 단기 목표를 가진다. 같은 선택지가 반복 등장해도 "진화까지 2레벨"이라는 상태가 있으면 항상 의미 있게 느껴진다. 이 단기 목표가 30분 런 내내 미세한 긴장을 유지시킨다.

**질적 전환의 명확한 연출**: 선형 강화(Lv.1→7은 10% 씩 증가)와 달리 Lv.8 진화 순간은 시각·청각적으로 크게 강조된다. 플레이어는 "10% 더 강해진 것"이 아니라 "다른 무기가 됐다"고 인식한다. 이 느낌의 차이가 중독성을 만든다.

**빌드 일관성 유도**: 진화 조건이 "같은 무기를 계속 올릴 것"을 강제하기 때문에 빌드가 자연스럽게 집중된다. 산만하게 모든 무기를 올리면 아무것도 진화하지 못한다 — 이 패널티가 암묵적 빌드 가이드 역할을 한다.

### 1.3 제약·한계

- 진화 조건이 불투명하면 초보 플레이어는 "왜 진화를 못 하는지"를 모른다. VS가 이를 극복한 방법은 레벨 바(진행 상태 가시화) + 진화 레시피 공개(게임 내 도감).
- 진화 무기 수가 너무 많아지면 "레시피 외우기" 게임이 되어 피로감을 준다. Project Lair는 카드 수가 28장으로 제한되어 있어 이 문제가 덜하다.
- 진화가 너무 강력하면 "진화만이 정답" 빌드로 수렴한다. Project Lair 에서는 진화 효과를 "질적 전환" 수준에서 조절하고 선형 중첩 vs 진화 간 선택 딜레마를 유지해야 한다.

---

## 2. Project Lair 적용 후보 (패턴 3개)

### 2.1 패턴 1: 패시브 카드 중첩 진화 — 3픽째 질적 전환

- **적용 대상**: `CardPickGenerator`, `CardEvolutionRegistry` (신규 SO), `ECardId` 추가 항목, `IBattleContext`
- **구현 스케치**: 패시브 카드를 동일한 것을 3번 픽하면 3번째 픽 시점에서 카드 선택지가 "진화 카드"로 교체된다. 진화 카드는 기존 카드와 동일한 `ECardId` 네임스페이스 내에 별도 enum 값(`WispHpBoost_Evo`)으로 정의하고, `CardEvolutionRegistry` ScriptableObject가 `(ECardId source, int pickCount=3) → ECardId evolved` 매핑을 보유한다. `CardPickGenerator`는 `BuildAxisCounter.GetPickCount(cardId) >= 2`일 때 해당 카드가 선택지에 등장하면 evolved 버전으로 교체한다. 진화 효과는 기존 효과 클래스를 상속한 `EvolvedXxxEffect`로 구현하되, 수치는 BalanceConfig에서 관리. 선택 팝업에 "EVOLVED" 특수 뱃지를 표시해 연출 차별화.
- **구현 비용**: 3 — CardEvolutionRegistry SO 신규 설계 + 효과 클래스 4~6개 + CardPickGenerator 조건 분기 + UI 뱃지 추가
- **MVP §11 호환**: O — 기존 카드 enum 구조를 유지하면서 evolved 항목만 추가. 선형 중첩 배율과 병존 가능.
- **적재 타이밍**: Alpha — 빌드 다양성 심화 단계에서 도입. 프로토타입에서는 진화 없이 선형 중첩만으로도 검증 가능하므로 Alpha 이후 추가.
- **의존 패턴**: 기존 `BuildAxisCounter`의 픽 횟수 추적 + `CardPickGenerator`의 3-pick 풀 생성 로직.

---

### 2.2 패턴 2: 패시브 + 액티브 교차 진화 — 두 카드 페어 합성

- **적용 대상**: `CardEvolutionPairRegistry` (신규 SO), `BattleContext.OnCardPicked`, `IBattleContext`, 신규 `PairEvolutionEffect` 클래스
- **구현 스케치**: 특정 패시브 카드 + 특정 액티브 카드를 모두 픽한 순간, 즉시 1회 발화하는 "교차 진화 효과"가 발동된다. 예시: `HeroAttackDown(P)` + `Weaken(A)` 동시 보유 → "완전 제압(CompleteSubjugation)": 영웅 공격력 영구 ×0.35 (기존 개별 효과보다 강한 복합 배율). `SpawnPhantoms(P)` + `TimeStop(A)` 동시 보유 → "유령 군단(PhantomLegion)": 다음 팬텀 스포너 스폰 웨이브 ×5 즉시 출력. `CardEvolutionPairRegistry` SO가 `(ECardId passive, ECardId active) → ICardEffect` 페어 테이블을 보유하고, `BattleContext`는 `OnCardPicked` 이벤트마다 테이블을 조회해 페어 조건 충족 여부를 확인, 충족 시 효과 즉시 실행. 발동 시 화면에 "교차 각성" 연출 팝업 1~2초 표시.
- **구현 비용**: 4 — PairEvolutionPairRegistry SO + 페어별 효과 클래스 다수(6~8개) + BattleContext 이벤트 구독 + 연출 팝업 + 페어 조합 설계 비용
- **MVP §11 호환**: O — 페어 조합은 기존 28장 카드의 조합이므로 신규 리소스 불필요. 단, 효과 수치 밸런싱이 복잡.
- **적재 타이밍**: Beta — 패턴 1(단일 카드 진화)이 안정화된 후 추가. 복잡도가 높아 프로토타입·알파에서는 생략.
- **의존 패턴**: 패턴 1의 진화 개념이 플레이어에게 익숙해진 뒤 도입해야 학습 부하가 낮다.

---

### 2.3 패턴 3: 진화 카운트다운 UI — 카드 픽 팝업 내 "N/3" 뱃지

- **적용 대상**: `CardSelectionPopup` (기존), `BuildAxisCounter.GetPickCount(ECardId)`, `CardEvolutionRegistry` (패턴 1 의존)
- **구현 스케치**: 카드 선택 팝업(3택 1)에서 플레이어가 이미 픽한 카드가 선택지에 다시 등장하면, 카드 패널 우상단에 소형 "×N/3" 카운트다운 뱃지를 표시한다. 뱃지 색상은 축 색상(예: Tank = 초록)과 동일하게 유지. N=2이면 "×2/3 → 진화 임박" 강조 표시(뱃지 테두리 반짝임). N=3이면 카드 패널 전체에 "EVOLVED" 오버레이가 표시되고 이 카드는 진화 버전으로 교체(패턴 1). 구현은 `CardSelectionPopup`에서 각 카드 데이터의 `ECardId`로 `BuildAxisCounter.GetPickCount(cardId)`를 조회하고 CHText 뱃지를 활성화/비활성화하는 방식으로, 핵심 로직은 패턴 1에 종속된다.
- **구현 비용**: 2 — 패턴 1이 이미 존재하면 UI 뱃지 추가만 필요. 독립적으로 구현하면 BuildAxisCounter 픽 카운트 추적만 필요(현재도 BuildAxisCounter가 있으므로 낮은 비용).
- **MVP §11 호환**: O — UI 변경만. 기존 기획서 §4.2 카드 팝업 구조에 뱃지 레이어 추가.
- **적재 타이밍**: Alpha — 패턴 1과 동시 도입. 진화가 없어도 "×N/3" 카운트 자체가 "이 카드를 계속 픽해야 하는가?" 의사결정에 도움을 준다.
- **의존 패턴**: 패턴 1의 `CardEvolutionRegistry`에 의존하지만, 단독으로도 동작 가능(카운트만 표시, 진화 미발생).

---

## 3. 우선순위 제안

**패턴 3 → 패턴 1 → 패턴 2** 순으로 도입 권장.

패턴 3(진화 카운트다운 UI)은 `BuildAxisCounter.GetPickCount()`를 읽기만 해도 즉시 구현 가능하고, 현재 "중첩 카드를 픽했을 때 배율이 누적되고 있는지 플레이어가 모른다"는 피드백 공백을 해소한다. 패턴 1(카드 진화)은 패턴 3이 UI를 구성한 이후 자연스럽게 확장이며, 질적 전환 연출이 가장 강한 긍정적 인상을 준다. 패턴 2(교차 진화)는 복잡도가 높고 밸런스 설계 비용도 크므로 Beta 이후 검토.

---

## 4. 채택 흐름 제안

- **패턴 3만 먼저 채택**: `game-designer` 에게 이 문서를 전달해 카드 팝업 뱃지 기획서 작성 요청 (MVP §11.3 카드 팝업 기획서 `docs/design/card-renewal.md` §8 업데이트 형태). 코드 변경은 `CardSelectionPopup` + `BuildAxisCounter` 조회 정도로 최소.
- **패턴 1 채택**: `CardEvolutionRegistry` SO 신규 설계 + 진화 카드 4~6종 기획이 선행되어야 하므로 game-designer → gameplay-programmer 전체 파이프라인 필요. Alpha 단계에서 `/start-develop` 흐름으로 진행.
- **패턴 2 채택 (backlog)**: 패턴 1 안정화 후 Beta에서 고려. 현재 backlog 등록 권장.

---

## 5. 쉬운 설명 (비개발자 요약)

뱀서(Vampire Survivors)에서는 같은 무기를 계속 올리다 보면 어느 순간 완전히 다른 강력한 무기로 "진화"한다. 단순히 조금 더 강해지는 게 아니라 생김새와 작동 방식이 통째로 바뀌면서 "드디어 터졌다!"는 쾌감을 준다. 또한 화면 한쪽에 "진화까지 몇 레벨 남았다"가 보이기 때문에 같은 선택지가 반복돼도 지루하지 않고 목표를 향해 계속 달리는 느낌이 든다. Project Lair에서도 같은 카드를 세 번 고르면 그 카드가 더 강력한 "각성 버전"으로 변신하게 하고, 카드 고르는 화면에 "이미 2번 골랐으니 한 번 더 고르면 각성!" 같은 표시를 붙이면, 플레이어가 5분 내내 작은 목표를 가지고 집중해서 게임을 즐길 수 있다. 그래서 이번에 참고하려는 것은: "중첩 카드 픽이 일정 횟수를 넘으면 질적으로 다른 단계로 변환되는 진화 시스템과, 그 진화까지의 카운트다운을 UI로 보여주는 방법"이다.
