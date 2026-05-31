# Spec — 카드 전체 리뉴얼 (4축 빌드 + 2-Layer 시너지)

> 작성: superpowers:brainstorming (메인 오케스트레이터)
> 작성일: 2026-05-31
> 단계: brainstorming → writing-plans 전 단계 spec (의도·범위·메커니즘 윤곽 + 결정 락)
> 후속: writing-plans → game-designer → ... (start-develop 표준 흐름)

---

## 0. 컨텍스트

- **프로젝트**: Project Lair (MVP)
- **참고 문서**:
  - 컨셉서 `docs/design/project_lair_concept.md` (v0.5, §4.2 · §5.2 · §11.3 · §11.4)
  - Content Audit `docs/design/content-audit/2026-05-30-plague-spawner-passive-unlock.md`
  - QA 6차 리포트 `docs/qa-reports/2026-05-26-continuous-spawn-6th-validation.md`
  - 지속 스폰 기획서 `docs/design/continuous-spawn-round.md`
- **현재 구현된 카드**: `Assets/_Lair/Art/Cards/Items/*.asset` 25장 (패시브 15 + 액티브 10)

---

## 1. 동기 — 왜 리뉴얼인가

| 문제 | 근거 |
|---|---|
| **Dead Card 2장** | SpawnPlagues · PlagueSlowBoost — Plague Spawner 부재로 픽해도 no-op (`continuous-spawn-round.md` §7) |
| **Berserk 자살 구조** | "몬스터 HP −50% + 데미지 +200% (15s)" — 영웅 처치 못하면 그냥 몬스터가 죽음. QA 6차 픽률 7% (전체 최하위) |
| **자살에 가까운 Multiply** | "최다 몬스터 종 즉시 2배" — 광역 빌드에 절대적이라 다른 액티브 선택지를 압살. 삭제 결정 (사용자 의사) |
| **빌드 다양성 부재** | 카드 카테고리(강화/추가/교체/환경/저주/버프/와일드)가 "어떤 식으로 작동하는가" 만 분류하고 "어떤 승리 패턴인가" 를 안내하지 않음. 픽 패턴이 단일 종 강화로 수렴 |
| **액티브 카드 픽률 하락** | Fear 13% · Bleed/Slow 18% · Weaken/IronWill 19% · TimeStop 20% — 액티브 카드 전반이 약함 |
| **클리어율 100%** | 액티브 카드의 영웅 방해력이 약해 빌드 결과와 무관하게 영웅 처치 |

---

## 2. 1순위 목표 (결정 락)

**빌드 다양성 확장.** 카드 28장이 4개의 명확한 빌드 축 위에 배치되고, 같은 축 카드를 모을수록 점점 강해지는 시너지가 발현되도록 한다. 카드 계열에 따라 다른 승리 패턴이 명확해진다.

부수 목표(자원 충돌 시 빌드 다양성 우선):
- Dead card · 자살 구조 해소
- 액티브 카드의 영웅 방해력 회복

---

## 3. 결정 락

### 3.1 범위
- 패시브 15장 + 액티브 10장 = 기존 25장 **전체** 리뉴얼
- 코드(`Effects/*.cs`) · ScriptableObject(`Cards/Items/*.asset`) · json(`Data/Json/cards.json`, `card_pools.json`) · 테스트 · 기획서(`continuous-spawn-round.md`, `project_lair_concept.md` §11.3·§11.4) · QA 리포트 (참고 인용만) 영향

### 3.2 4개 빌드 축 (새 카테고리)
기존 7카테고리(강화/추가/교체/환경/저주/버프/와일드)를 **폐지**하고 4축을 새 카테고리로 채택.

| 축 | 핵심 몬스터 | 빌드 컨셉 |
|---|---|---|
| **탱커/포위** | Wisp · Wraith | HP · 길막 · 진로 방해. 영웅을 못 움직이게 |
| **DPS** | Reaper · Hex | 깡딜 · 원거리 사격. 짧은 시간 안에 처치 |
| **디버프 누적** | Plague + 액티브 저주 콤보 | 둔화 · 출혈 · 공포 · 약화. 영웅을 갉아냄. **둔화/속박은 이 축에 포함** |
| **수적 압박** | Phantom | 작고 빠른 다수가 둘러쌈. 수치 자체보다 머릿수로 압도 |

### 3.3 카드 수 · 패시브/액티브 비율
- **총 28장** (기존 25 → 28)
- **패시브 16장 + 액티브 12장** (HP 10% / 30s 트리거 구분은 컨셉 §4.2 그대로 유지)
- **한 축당 7장 균등 분배** = 패시브 4 + 액티브 3

### 3.4 환경 카드 처리
- 별도 카테고리 폐지. 독장판·시야감소 같은 환경 효과는 4축에 흡수.
- 예: 독장판 → 디버프 누적 축 / 시야감소 → 수적 압박 축 (해석은 game-designer 단계)

### 3.5 2-Layer 시너지 메커니즘

**Layer 1 — 빌드 시너지 (같은 축 카운트 기반)**
- 한 축의 카드를 N장 픽하면 단계적 보너스 발동
- 임계: **3장 / 5장 / 7장** (Tier 1·2·3)
- 효과 형태: 해당 축 핵심 스탯 추가 보정 (예: 탱커 3장 → 탱커 몬스터 HP +α%)
- 발동 시점: **임계 도달 시 즉시 발동** (해당 카드 픽 결과로 임계를 넘는 순간 적용)
- 수치 · 효과 디테일은 game-designer

**Layer 2 — 카드 중첩 (같은 카드 누적 기반)**
- 같은 카드를 K번 픽하면 그 카드 자체 효과가 강화
- 기존 패시브 강화(`WispHpBoost +50% × 2픽 = ×1.5²` 곱연산 누적) 정책을 이 시스템으로 일반화
- 액티브 카드의 중첩 정책(효과량 ↑? 지속시간 ↑?)은 game-designer 단계에서 카드별 결정
- 4축 새 카테고리와 무관하게 독립적으로 작동

두 Layer는 독립. 같은 카드 K번 픽은 빌드 시너지 카운트와 카드 중첩 카운트를 동시에 올린다.

### 3.6 Plague Spawner 추가 (구조 결함 해소)
디버프 누적 축이 작동하려면 Plague가 필드에 등장해야 한다. `continuous-spawn-round.md §3.1`의 Spawner 구성에서 Wisp 스포너 2개 중 하나를 Plague 스포너로 전환 (구체 슬롯·주기는 game-designer 결정). content-audit 2026-05-30의 권고를 이 리뉴얼에 흡수.

### 3.7 Multiply 카드 삭제
"최다 몬스터 종 즉시 2배" 카드는 4축 중 수적 압박 축에 자연스럽게 흡수 또는 대체. 카드 자체는 폐기. 폐기 시점은 신 카드 28장 구현이 완료된 일괄 마이그레이션 시점.

---

## 4. 보존되는 도구 (메커니즘 풀)

기존 카드 작동 메커니즘은 카테고리 라벨로 쓰이지 않을 뿐, 카드 효과의 도구로는 **유효하게 보존**한다. game-designer 가 한 카드를 설계할 때 다음 중 하나 이상을 자유 조합:

| 메커니즘 | 기존 명명 | 4축 안에서 용도 예 |
|---|---|---|
| 종(種) 영구 글로벌 스탯 버프 | 강화 | 탱커/DPS 축의 기본 보정 |
| Spawner 동시 출력 +1 (영구) | 추가 | 수적 압박 축의 핵심 도구 |
| Spawner 출력 종 영구 변경 | 교체 | 빌드 전환 카드 (한 축에서 다른 축으로 갈아탈 때) |
| 영웅에게 영구 환경 효과 | 환경 | 디버프 누적 축의 영구 디버프 |
| 영웅 일시 저주 (지속시간형) | 저주 | 디버프 누적 축의 액티브 도구 |
| 몬스터 일시 버프 (지속시간형) | 버프 | 모든 축의 액티브 도구 |
| 시간 정지 등 특수 일회성 | 와일드 | 수적 압박/디버프 축의 액티브 트럼프 |

---

## 5. 영향 범위

### 5.1 코드 (gameplay-programmer 작업)
| 파일 | 변경 종류 |
|---|---|
| `Assets/_Lair/Scripts/Card/Effects/*.cs` | 25개 중 대다수 효과 신규/리뉴얼/삭제. `MultiplyEffect.cs` 삭제 |
| `Assets/_Lair/Scripts/Card/` (시너지 시스템) | **새 시스템** — 빌드 카운트 추적 + 임계 시너지 적용 |
| `Assets/_Lair/Scripts/Battle/IBattleContext` | 카드 카테고리(4축) 카운트 조회·시너지 발동 API 신설 |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `ECardCategory`(7카테고리) → `EBuildAxis`(4축) 교체 (Enum 이름·값) |
| `Assets/_Lair/Data/Json/cards.json` | 카드 정의 (28장으로 변경 + 카테고리 필드 4축) |
| `Assets/_Lair/Data/Json/card_pools.json` | 카드 풀 구성 갱신 |
| `Assets/_Lair/Editor/LairCardPrefabBuilder.cs` | 카테고리 라벨/색상 매핑 갱신 |

### 5.2 데이터 (SO · Addressable)
| 파일 | 변경 종류 |
|---|---|
| `Assets/_Lair/Art/Cards/Items/*.asset` | 25개 → 28개. 일부 신규, 일부 리네임, Multiply 삭제 |
| `Assets/_Lair/Art/Cards/CardPool_Passive.asset` | 15장 → 16장 |
| `Assets/_Lair/Art/Cards/CardPool_Active.asset` | 10장 → 12장 |
| BalanceConfig | 시너지 임계 보너스 수치 추가 가능성 |

### 5.3 테스트
- `Assets/_Lair/Tests/EditMode/` — 카드 효과 단위 테스트 갱신·신규
- `Assets/_Lair/Tests/PlayMode/SimPickStrategy.cs` — 4축 우선 픽 전략으로 갱신
- `Assets/_Lair/Tests/PlayMode/ContinuousSpawnIntegrationTest.cs` — Plague Spawner 통합 시나리오 추가

### 5.4 기획서 / 문서
- `docs/design/project_lair_concept.md` §11.3 (카드 정의) · §11.4 (테두리 색) · §5.2 (시너지 방향성) — 4축으로 갱신
- `docs/design/continuous-spawn-round.md` §3.1 / §7 — Plague Spawner 슬롯 추가 반영
- `docs/design/card-renewal-2026-05-31.md` (또는 game-designer 정한 이름) — 신규 기능 기획서

---

## 6. 비결정 / 위임 사항 (game-designer 채움)

- 각 축의 7장(패시브 4 + 액티브 3) 구체 라인업과 효과 수치
- 시너지 임계 보너스의 구체 수치 (Tier 1·2·3 강도)
- 시너지 효과 표시 UI (카드 픽 화면에서 현재 빌드 카운트 노출 방법)
- Plague Spawner 의 구체 슬롯 위치(0°~300° 중 어느 #) · 주기 · 초기 지연
- 액티브 카드 중첩 정책 (효과량 누적 vs 지속시간 연장 vs 둘 다)
- Multiply 의 대체/흡수 카드 (수적 압박 축 안에 어떻게 변환할지)
- 카드 ID 재할당 정책 (기존 0~24 → 신규 0~27)
- 카드 명·설명 텍스트
- 컨셉서 §11.4 카드 테두리 4색 매핑

---

## 7. 비범위 (Out of Scope)

- **메타 진행** (소울 / 도감 / 해금) — MVP §11 외
- **신규 몬스터 추가** — 6종 고정
- **신규 영웅 추가** — 1명 고정
- **사운드 / 아트** — MVP 제약
- **메인 메뉴 / 세팅** — MVP 제약
- **신규 시너지 축 추가 (5축 이상)** — 4축 고정
- **트리거 자체 변경** (HP 10% / 30s) — 컨셉 §4.2 유지
- **카드 풀 외부 카드 수급 시스템** — 없음, 모든 카드는 28장 풀에서

---

## 8. 다음 단계

1. **사용자 spec 리뷰** (이 문서) → 승인
2. **writing-plans** → `docs/superpowers/plans/2026-05-31-card-renewal.md` 작성 (파일 경로·시그니처·TDD·verification gate 단계화)
3. 실행 방식 선택 게이트 — 슈퍼파워 실행 vs start-develop 파이프라인 계속
4. (start-develop 계속 시) game-designer → design-reviewer → 사용자 승인 → gameplay-programmer → code-reviewer → test-engineer
5. 마무리 — `git add` + 한글 커밋 메시지(안)

---

## 9. 결정 락 요약 (writing-plans·game-designer 가 따를 핵심)

| # | 결정 락 |
|---|---|
| D1 | 범위 = 25장 전체 리뉴얼 + 신규 3장 추가 (총 28장) |
| D2 | 1순위 목표 = 빌드 다양성 확장 |
| D3 | 새 카테고리 = 4축 (탱커/포위 · DPS · 디버프 누적 · 수적 압박). 기존 7카테고리 폐지 |
| D4 | 둔화/속박은 디버프 누적 축에 통합 |
| D5 | 카드 수 = 패시브 16 + 액티브 12 = 28장, 한 축당 7장 (패시브 4 + 액티브 3) 균등 |
| D6 | Layer 1 시너지 = 같은 축 3·5·7장 임계 스탯 보너스, **임계 도달 시 즉시 발동** |
| D7 | Layer 2 시너지 = 같은 카드 K번 픽 시 능력 강화 (기존 패시브 곱연산 정책 일반화) |
| D8 | 환경 카드 = 4축 안에 흡수 (별도 카테고리 X) |
| D9 | Plague Spawner 추가 (디버프 축 작동을 위한 구조 결함 해소) |
| D10 | Multiply 카드 삭제 (수적 압박 축으로 흡수/대체) |
| D11 | 컨셉서 §11.3·§11.4·§5.2 갱신, §4.2 유지 |
