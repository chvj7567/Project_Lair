# 동시 몬스터 캡 제거 + 액티브 트리거 분단위 축소

- 작성일: 2026-06-03
- 단계: MVP
- 관련 컨셉서: `docs/design/project_lair_concept.md` §4 (코어 루프) · §8 (밸런싱)

## 1. 의도 / 배경

두 개의 독립된 배틀 플로우 변경을 한 사이클로 묶는다. 둘 다 "5분 자동전투 + 트리거 선택지" 코어 루프의 페이싱·물량 감각을 조정한다.

- **A. 동시 몬스터 캡 제거** — 현재 `BattleController._monsterCap = 18` 이 동시 활성 몬스터 수를 18로 막는다. 캡에 막혀 스포너/증식 카드의 출력이 truncate 되는 감각을 없애고, 물량이 자유롭게 누적되게 한다.
- **B. 액티브 카드 트리거 분단위 축소** — 액티브 카드 선택이 30초 단위 9회(30·60·…·270초) 발생한다. 분 단위 지점(60·120·180·240초) 4개를 제거해 5회({30·90·150·210·270})로 줄인다.

## 2. 범위 — 결정 락

### A. 캡 제거 (개념째 삭제)

| 항목 | 결정 |
|---|---|
| enforcement | `SpawnFromSpawner` / `SpawnMonsterRuntime` 의 `AliveMonsterCount() >= _monsterCap` 검사 전량 제거 |
| 필드/프로퍼티 | `BattleController._monsterCap`, `MonsterCap` 프로퍼티 삭제 |
| 캡 증가 API | `BattleController.IncrementGlobalMonsterCap`, `IBattleContext.IncrementGlobalMonsterCap`(Card/CommonInterface.cs), `BattleContext` 위임 모두 삭제 |
| Tank Tier3 카드 | 캡 +6 효과 → **Wisp+Wraith 추가 내구 버프로 교체** (테마 일관, Tier1/2 와 동일한 `RegisterMonsterTypeBuff` 구조). 구체 스탯·수치는 game-designer 가 §8 밸런스 맥락에서 설계 |
| 성능 안전장치 | **없음** — "매우 큰 값" 이 아닌 완전 제거로 확정. 무제한 누적 리스크 감수 (아래 §4) |

### B. 액티브 트리거 축소

| 항목 | 결정 |
|---|---|
| 새 임계점 집합 | `{30, 90, 150, 210, 270}` 초 (5개) |
| 제거 대상 | `60, 120, 180, 240` 초 (분 단위 4개) |
| 진실원 동기화 | 런타임 값 `BalanceConfig.asset._activeThresholds` + 코드 기본값 2곳(`BalanceConfig.cs` 기본 배열, `ActiveTriggerService.DefaultThresholds`) **모두** 새 집합으로 갱신 |
| 패시브 트리거 | 변경 없음 (HP% 9개 그대로) |

> 비고: "4분·3분·2분·1분" 은 카운트업/카운트다운 어느 해석이든 경과 {60,120,180,240}초로 동일하게 매핑된다.

## 3. 영향 받는 코드/에셋

**캡 (A)**
- `Assets/_Lair/Scripts/Battle/BattleController.cs` — 필드(L28)·프로퍼티(L561)·메서드(L529-533)·스폰 검사 4곳(L384,394,703,709)
- `Assets/_Lair/Scripts/Battle/BattleContext.cs` — `IncrementGlobalMonsterCap` 위임(L118-120)
- `Assets/_Lair/Scripts/Card/CommonInterface.cs` — `IBattleContext.IncrementGlobalMonsterCap`(L63)
- `Assets/_Lair/Scripts/Card/Synergy/TankSynergyTier3.cs` — 효과 교체
- `Assets/_Lair/Scripts/Battle/Spawner.cs` — §4.3 캡 관련 `//#` 주석 정리
- 캡을 언급하는 기타 `//#` 주석 정리

**트리거 (B)**
- `Assets/_Lair/Scripts/Data/BalanceConfig.cs` — `_activeThresholds` 기본 배열
- `Assets/_Lair/Scripts/Battle/ActiveTriggerService.cs` — `DefaultThresholds`
- `Assets/_Lair/Data/BalanceConfig.asset` — `_activeThresholds` 직렬화 값 (런타임 진실원, Rule 04)

**테스트**
- `Assets/_Lair/Tests/PlayMode/CardRenewalSpawnerIntegrationTest.cs` — `Tank_Tier3_…캡_24로_상승` 류를 새 Tier3 효과 검증으로 재작성
- 캡 무관 스폰 사이클 전량 발사 회귀 테스트 추가
- 액티브 트리거 5개 발화 검증 테스트 추가/수정

## 4. 리스크 (정직하게 명시)

- **성능**: 캡 제거로 5분간 몬스터 무제한 누적 가능. 특히 Swarm 시너지(출력+/주기단축)와 겹치면 렌더/물리/AI 비용이 선형 증가. 풀링은 생성 비용만 완화. 안전장치 없이 진행하는 것이 명시적 결정.
- **밸런스**: (A) Swarm 계열 체감 강화 + (B) 액티브 픽 9→5 감소가 동시에 적용 → 페이싱·난이도 변동 폭이 큼. 마무리 후 **qa-simulator 별도 검증을 제안**한다.

## 5. 성공 기준

- 캡 관련 식별자(`_monsterCap`/`MonsterCap`/`IncrementGlobalMonsterCap`)가 코드베이스에서 사라지고 컴파일·테스트 통과
- 스포너/증식 카드가 18 이상에서도 truncate 없이 스폰
- Tank Tier3 가 새 내구 효과로 동작 (game-designer 수치대로)
- 액티브 카드가 한 판에 정확히 5회({30,90,150,210,270}초)만 발생
