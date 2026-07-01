# Card Ideas — 2026-07-02 — 스포너 아키텍처 재설계 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 스포너 동작 구조 자체를 영구 개조하는 3종 — 출력 배가 / 스포너 희생 가속 / 캡 도달 시 교체 순환
- **목록**: TwinSpawn / SacrificedSpawner / RelentlessCycle
- 기존 25장 + git log 과거 34회차(구포맷 15건 포함)와의 중복 회피 확인됨

---

## 1. TwinSpawn — 이중 스폰

- **카테고리**: 패시브 추가 (Add)
- **효과 모델**: 무작위 스포너 1개가 이후 매 스폰 사이클마다 동종 몬스터를 1마리 대신 **2마리 동시 배출**한다. 글로벌 캡이 허용하는 범위 내에서만 동작 (캡 도달 시 1마리만 배출 또는 백오프). 영구 효과.
  - 수치: 선택된 Spawner.SpawnCount = 2 (영구)
  - 적용 대상: 무작위 Spawner 1개 (픽 시점 랜덤 결정, 이후 고정)
- **구현 패턴**: `Spawner` 컴포넌트에 `int SpawnCount` 필드 추가 (기본 1). 스폰 루프에서 `for (int i = 0; i < SpawnCount; i++)` 구조로 확장. Spawner 접근 경로는 `SpawnerHasteEffect` 기존 패턴(IBattleContext → SpawnerController) 재사용.
- **시너지 후크**:
  - Swarm Tier3 (글로벌 캡 +6 → 이중 스폰 공간 확보)
  - SpawnerHaste (주기 단축 × 배수 배출 = 복합 출력 가속)
  - Multiply 액티브 (현재 최다 종 2배 스폰과 시너지 — 이중 스폰 종을 타겟으로 선정 시 4배 스폰 효과)
- **구현 비용 추정**: 2 (Spawner SpawnCount 필드 추가 + 스폰 루프 for 변환 — SpawnerHaste 접근 패턴 재사용)
- **중복 재검증**: `SpawnerHaste`(전체 스포너 주기 단축)와 다름 — TwinSpawn은 "출력 수량 증가"이며 주기 불변. Run 12/13의 스포너 가속·다양성 제안은 주기 조작·종 다양성 유지이므로 구조적으로 구분됨.

---

## 2. SacrificedSpawner — 희생된 스포너

- **카테고리**: 패시브 환경 (Environment)
- **효과 모델**: 무작위 스포너 1개를 **영구 비활성화** (해당 종의 신규 스폰 중단, 필드 잔존 몬스터는 유지). 대신 나머지 5개 스포너의 스폰 주기가 영구 **×0.8** (25% 가속). "6종 → 5종으로 집중하되 더 빠르게" 전략.
  - 수치: 비활성 Spawner.Active = false (영구) + 나머지 Spawner.CooldownScale × 0.8 (영구 누적, SpawnerHaste 와 곱연산)
  - 적용 대상: 픽 시점 무작위 1개 비활성. 나머지 5개 일괄 가속.
- **구현 패턴**: `Spawner` 컴포넌트에 `bool Active` 플래그 추가 → `Active == false` 시 스폰 루프 전체 스킵. 나머지 5개는 `SpawnerHasteEffect` 의 `CooldownScale × 0.8` 방식과 동일 경로 적용.
- **시너지 후크**:
  - ReplaceWispsToWraith 후 이 카드로 Wisp 스포너 비활성화 → 순수 Wraith 집중 빌드 가속 가능
  - SpawnerHaste 와 곱연산 — 비활성화 후 5개에 SpawnerHaste 적용 시 × (0.5 × 0.8) = ×0.4 주기
  - SpawnerDiversityTrio(Run 13, 2026-06-18)의 "다양성 보상"과 정반대 전략 축 형성 (다양성 vs 집중)
- **구현 비용 추정**: 3 (Spawner.Active 플래그 신설 + 조건부 스킵 + 나머지 CooldownScale 배율 적용)
- **중복 재검증**: Run 13 DiverseHaste(6종 유지 보상)·Run 12 ReaperOverflow 등(종별 개별 가속)과 방향이 반대. "스포너 비활성화를 통한 역설적 가속"은 어느 회차에도 없음.

---

## 3. RelentlessCycle — 무자비한 순환

- **카테고리**: 패시브 환경 (Environment)
- **효과 모델**: 글로벌 캡 도달 시 자동 백오프(스폰 대기) 대신, 현재 필드에서 **HP% 가장 낮은 몬스터 1마리를 즉시 제거**하고 해당 스포너 위치에서 동종 **신규 몬스터 1마리를 즉시 스폰**. 필드가 항상 "상대적으로 건강한" 몬스터로 갱신됨.
  - 수치: 캡 도달 감지마다 발동. 제거 대상 = CharacterRegistry.Monsters 전체 중 min(HP / MaxHP) 1마리.
  - 신규 스폰은 해당 스포너의 종과 기본 스탯 (글로벌 버프 적용됨).
- **구현 패턴**: SpawnerController 스폰 시도 시 캡 도달 조건 분기 추가 → `IBattleContext.GetAllMonsters()` 순회, `IHealth.Current / IHealth.Max` 비교 → 최솟값 1마리 `CHMPool.Push()` → 해당 스포너에서 `ctx.SpawnMonster(spawnerType, spawnerPosition)`. SpawnMonster는 기존 SpawnXxxEffect 패턴 재사용.
- **시너지 후크**:
  - Tank 빌드 (Wisp·Wraith HP가 높아 오래 생존 → 교체 빈도 낮고 안정적 전선 유지)
  - IronWill (DamageTakenScale ×0.7 → HP 소진 느림 → RelentlessCycle 발동 빈도 감소 = 안정적 고HP 필드)
  - BloodThirst 액티브 (교체로 새로 스폰된 몬스터도 처치 시 회복 대상)
  - Debuff 빌드 (영웅이 Slow·Bleed 로 느리면 몬스터 피해 느려져 HP% 내려가기 전에 교체 덜 발생)
- **구현 비용 추정**: 3 (HP% 최솟값 탐색 + 캡 도달 조건 분기 + CHMPool.Push + SpawnMonster 조합 — 각 조각은 기존 패턴)
- **중복 재검증**: "density-tide-pressure-trio"(2026-06-06)는 필드 밀도로 영웅 압박하는 영웅 디버프 계열. RelentlessCycle은 캡 백오프 로직 자체를 교체하는 스포너 행동 계층 변경으로 완전히 다른 레이어. 어느 회차도 "글로벌 캡 백오프를 교체 스폰 메커니즘으로 전환"을 제안하지 않음.

---

## 4. 공통 테마 고찰

세 카드 모두 **스포너의 동작 규칙(Architecture)** 을 영구 변경하는 메타-레이어 카드다. 기존 카드들이 몬스터 스탯, 영웅 디버프, 즉시 버스트 스폰에 집중한 것과 달리, 이 세 카드는 5분 내내 작동하는 스폰 엔진 자체를 뜯어 고친다.

**왜 이 테마인가?** 6개 스포너 구조는 게임의 핵심 인프라지만 현 25장 중 스포너 동작을 건드리는 카드는 SpawnerHaste 단 1장뿐이다. 스포너 설계 공간이 크게 비어 있었으며 세 카드는 각각 다른 차원을 건드린다:
- **TwinSpawn**: 출력 수량 (1회 스폰당 마릿수)
- **SacrificedSpawner**: 스포너 개수 (6→5 + 가속)
- **RelentlessCycle**: 캡 도달 시 행동 (백오프 → 교체 순환)

특히 SacrificedSpawner는 "희생으로 나머지를 강화"하는 트레이드오프 카드로, 현재 25장 중 명시적 트레이드오프를 가진 카드가 없다는 점에서도 독창적이다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- IBattleContext에 `GetAllMonsters()` / `GetSpawners()` API 노출 여부를 gameplay-programmer 와 선확인 권장
- TwinSpawn(비용 2) → RelentlessCycle(비용 3) → SacrificedSpawner(비용 3) 순으로 구현 권장
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 Project Lair에서 몬스터들은 6개의 "생산 라인(스포너)"에서 계속 쏟아져 나온다. 지금까지 대부분의 카드는 그 몬스터들을 더 강하게 만들거나 영웅을 더 약하게 만드는 방식이었다. 오늘 제안하는 카드 3장은 **생산 라인 자체의 규칙을 바꾸는** 전혀 다른 접근이다. 하나는 어떤 생산 라인이 물건을 두 개씩 내보내게 하고, 다른 하나는 생산 라인 하나를 폐쇄해서 남은 다섯 개를 더 빠르게 돌리고, 마지막은 공장이 꽉 찼을 때 가장 지친 직원을 새 직원으로 자동 교체해 항상 기운찬 팀을 유지한다. 그래서 오늘 제안하는 카드 3장은: "몬스터를 강하게 만드는 게 아니라, 몬스터를 만드는 방식 자체를 개조하는 공장 혁신 카드"다.
