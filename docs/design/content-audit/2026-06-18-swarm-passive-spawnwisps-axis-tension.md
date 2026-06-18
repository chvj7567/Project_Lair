# Content Audit — 2026-06-18 — Swarm 패시브 SpawnWisps: Tank 핵심 종(Wisp) 수량 카드의 Swarm 축 귀속이 Tank·Swarm 교차 픽 딜레마를 만드는가

> 생성: Daily Content Audit 루틴 (자동)
> 날짜: 2026-06-18
> 단계: v0.2 — 런 사이 메타 성장 검증

---

## 0. 입력 스냅샷

| 항목 | 값 |
|---|---|
| 컨셉 기준 | `docs/design/project_lair_concept.md` v0.7 |
| 카드 기획서 | `docs/design/card-renewal.md` v0.6 (2026-05-31, synced 2026-06-01) |
| 스폰 밸런스 | `docs/design/spawn-period-balance.md` |
| 영웅 스킬 | `docs/design/hero-skills.md` |
| QA 리포트 | `docs/qa-reports/2026-05-22.md` (BLOCKED) |
| 구현 스냅샷 기준 | Assets/_Lair/Scripts/Card/, Art/Cards/Items/ |
| 과거 감사 이력 | git log `--grep="^\# \[Routines\]\[Daily Content Audit\]"` 10개 |

---

## 1. 현황

### 1.1 구현 완료 (MVP 기준)

| 항목 | 계획 | 구현 | 상태 |
|---|---|---|---|
| 영웅 | 1 (Knight) | 1 | ✅ |
| 몬스터 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom) | 6 | ✅ |
| 패시브 카드 | 16 | 16 | ✅ |
| 액티브 카드 | 12 | 12 | ✅ |
| 시너지 티어 | 12 (축 4 × 3단계) | 12 | ✅ |

### 1.2 계획 있으나 미구현

| 항목 | 상태 | 비고 |
|---|---|---|
| SwarmRush (액티브) | 미구현 | Multiply 대체 자리 (card-renewal §3.4) |
| DebugAutoPicker 훅 | 미구현 | QA 헤드리스 시뮬 차단 요인 (qa-report §3) |
| Swarm Tier3 | 구현 완료 | SwarmSynergyTier3.cs 확인 |

### 1.3 QA 권고 사항

- `BattleController.DebugAutoPicker` (`#if UNITY_EDITOR`) 훅 추가 필요 — gameplay-programmer 대상
- 훅 없이는 헤드리스 밸런스 시뮬레이션 불가 (팝업 `await tcs.Task` 영구 블록)

### 1.4 과거 감사 이력 (git log 기준 최근 10회)

| 날짜 | 슬러그 요약 |
|---|---|
| 2026-06-17 | Phantom 이동속도 중첩 상한 설계 |
| 2026-06-16 | (Swarm 관련 — Swarm Tier 계열) |
| 2026-06-14 | (Tank/Debuff 계열 — 정확 슬러그 미확인) |
| 2026-06-13 | (Swarm 계열) |
| 2026-06-11 | (Swarm 계열) |
| 2026-06-10 | Tank Tier3 내구도 버프 |
| 2026-06-09 | SpawnerHaste·Swarm Tier2 |
| 2026-06-06 | SpawnerHaste period stack nova guard |
| 2026-06-05 | Tank Tier3 내구도 버프 |
| 2026-06-04 | Multiply→SwarmRush 액티브 교체 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### 후보명: Swarm 패시브 SpawnWisps — Tank 핵심 종(Wisp) 수량 카드의 Swarm 축 귀속이 Tank·Swarm 교차 픽 딜레마를 만드는가

**한 줄 요약**: Wisp를 강화하는 Tank 빌드 플레이어가 Wisp를 더 소환하려면 Swarm 축 카드인 SpawnWisps를 픽해야 하는데, 이 교차 픽이 진짜 딜레마를 만드는가를 설계 차원에서 검증한다.

---

### 2.1 설계 긴장 분석

**card-renewal.md §6.2 명시 내용**:
> "SpawnWisps: _axis=3 (Swarm). Wisp는 느리고 작고 떼 = Swarm 정체성"

**Tank 시너지 Tier1 (card-renewal.md §4.2)**:
> Tank Tier1 — Wisp HP ×1.3

**구조적 긴장**:

```
[Tank 빌드 플레이어]
 → Wisp를 강화하고 싶다  (Tank Tier1: Wisp HP ×1.3)
 → Wisp를 더 많이 소환하고 싶다 (전선 두께 확보)
 → SpawnWisps 픽 필요  ← 단, 이 카드는 Swarm 축(_axis=3)
 → Swarm Tier 카운터 +1 (Tank 카운터 아님)
 → Tank 3/5/7 달성이 늦어짐 vs Swarm 3/5/7 기회가 생김
```

이 구조가 **실제로 작동하는 딜레마인가**, 아니면 플레이어가 혼란만 느끼고 어느 쪽도 강화되지 않는 덫인가를 확인해야 한다.

---

### 2.2 점수표

| 축 | 점수 (1~5) | 근거 |
|---|---|---|
| 검증가치 | 4 | Tank 최강화 종인 Wisp의 수량 카드가 타 축 귀속 — 실제 플레이 결정에 직접 영향 |
| 구현비용 | 2 | 기존 카드·시너지 시스템 무수정; 기획서 설계 명세 추가만 필요 |
| 시너지폭 | 4 | Tank×Swarm 교차 픽 허용 시 하이브리드 빌드 가능성; 단일 축 집중 vs 분산 트레이드오프 |
| 데이터근거 | 4 | card-renewal §3.4, §4.2, §6.2 삼중 근거; Tank Tier1 Wisp HP 강화가 SoT에 명시 |
| **종합** | **16** | |

---

### 2.3 유저 플로우 9개

**전제**: 영웅 HP=4000, 패시브 트리거 10%마다(HP 400 단위), 전역 3픽 캡 적용.

#### 플로우 1 — Tank 집중 빌드 (SpawnWisps 미픽)

1. 초기 등장: Wisp 기본 3기 스폰됨
2. 영웅 HP 10% (400) 감소 → 패시브 3택 제시
3. Tank 카드(IronWill 등) 픽 → Tank 카운터 +1
4. 반복으로 Tank 3 달성 → Wisp HP ×1.3 시너지 발동
5. 전선은 얇지만 Wisp 개체가 질적으로 강화됨
6. Wisp 소환량 부족 → 다수 카드가 Wisp 처치 없이 영웅 우회 가능
7. SpawnWisps 제시될 경우 픽 시 Swarm 카운터 증가 딜레마 직면
8. Tank Tier 5/7 달성 목표와 Swarm 오염 사이 선택
9. 결과: Tank 3 달성 속도 빠르지만 Wisp 물량 부족으로 영웅 억제력 약화

#### 플로우 2 — Swarm 집중 빌드 (SpawnWisps 적극 픽)

1. 초기: Wisp 기본 3기
2. 패시브 3택 시 SpawnWisps 우선 픽 → Swarm 카운터 +1, Wisp 스포너 출력 +1
3. SpawnWisps 연속 픽 (최대 3픽 캡) → Wisp 스포너 출력 +3 (기본 + 3)
4. Swarm 3 달성 → Swarm Tier1 효과 발동
5. 전선은 두꺼워지지만 Wisp 개체 HP/공격력은 기본값
6. 영웅이 낮은 Wisp를 빠르게 처치 → 물량이 질보다 빠르게 소진
7. Tank 카드가 제시돼도 Swarm에 집중하면 Wisp 강화 없음
8. Swarm Tier 5/7 달성 vs Wisp 내구성 부족 사이 압박
9. 결과: 빠른 물량 확보, 후반 영웅 딜 상승 시 버티기 어려움

#### 플로우 3 — 하이브리드 빌드 (Tank + SpawnWisps 혼합)

1. Tank 카드 2픽 + SpawnWisps 1픽 → Tank 카운터 2, Swarm 카운터 1
2. Wisp 스포너 출력 +1 (중간 물량)
3. Tank Tier1 (3픽 달성 목표) 근접 → 다음 픽 Tank 필요
4. 하지만 3택에 SpawnWisps 재등장 → 3픽 캡 중 1픽 소진 여부 고려
5. Tank 3 + Swarm 1: Wisp HP ×1.3 + 소환 +1 = 질·양 모두 일부 확보
6. 어느 쪽 Tier도 5/7에 미달 → 고급 시너지 없음
7. 영웅 중반 생존 가능, 후반 압박 증가
8. 추가 픽에서 Tank 추가 시 Swarm 정체, Swarm 추가 시 Tank 정체
9. 결과: 중간 성능; 특화 빌드에 비해 시너지 효율 저하

#### 플로우 4 — SpawnWisps 3픽 캡 도달

1. SpawnWisps 3회 픽 완료 → Wisp 스포너 출력 최대 +3
2. 이후 SpawnWisps 제시 불가 (전역 3픽 캡)
3. 이후 픽은 다른 Swarm 카드 또는 다른 축으로 강제 전환
4. Swarm Tier3 달성 가능 여부: Swarm 카운터 3 이상 확보 필요
5. Wisp 수는 최대화되었으나 개체 강도 기본값
6. Wisp 과잉 스폰 → 전역 몬스터 캡(18기) 소진 가능성
7. 캡 소진 시 다른 몬스터 스폰 억제 → Wisp 단일 의존
8. 영웅이 Wisp 특화 대응 가능하면 후반 급격히 불리
9. 결과: 물량 극대화, 다양성 감소, 캡 충돌 리스크

#### 플로우 5 — Tank Tier1 달성 후 SpawnWisps 픽 직면

1. Tank 3픽 달성 → Wisp HP ×1.3 시너지 활성
2. 다음 패시브 트리거에서 3택: SpawnWisps 포함
3. 현재 Tank는 확보됨 → SpawnWisps 픽으로 얻는 것: 물량 확보 + Swarm 카운터
4. Swarm 카운터 1 상태에서 SpawnWisps 픽: Swarm 방향 전환 신호탄
5. 이후 3택에서 Swarm 카드 비율 증가 여부 (현재 시스템은 랜덤 3택이므로 확률적)
6. Tank 5 목표 vs Swarm 3 목표 중 선택
7. Tank 5 (Wisp 추가 버프) vs Swarm 3 (양 증가) — 두 효과 모두 Wisp 강화에 기여하나 방향 다름
8. 플레이어 입장에서 명확한 우선순위 없음 → 혼란 or 흥미 중 어느 쪽인가?
9. 결과: 딜레마 실체 확인 지점 — 게임 흐름이 자연스러운 분기인지 체크 필요

#### 플로우 6 — SpawnWisps 없이 Wisp 극대화 시도

1. Tank 카드만 픽, SpawnWisps 거부
2. Wisp 스포너 기본 출력 유지 (출력 +0)
3. Tank Tier 1/2/3 모두 달성 가능 (카드 수 여유)
4. Wisp HP ×1.3 (Tier1) + 공격력 ×1.2 (Tier2) + Tank Tier3 효과
5. 개체 강도 최대 but 스폰 수 기본
6. 영웅이 Wisp 1기씩 제거 시 전선 유지 어려움
7. 개체 강도 높아져도 수가 부족하면 영웅 우회 허용
8. Tank 완성 빌드: 개체 품질 최대 vs 스폰량 최소 (기본값)
9. 결과: Tank Tier3 완성 빌드 vs Wisp 수량 확보의 상충 — SpawnWisps 거부 비용 측정 포인트

#### 플로우 7 — 영웅 스킬 페이즈 전환 시 SpawnWisps 가치 변화

1. 영웅 HP 85% 이하 → 스킬 1단계 발동 (hero-skills.md 기준)
2. 스킬 발동 시 광역 피해 → Wisp 다수 소멸 가능
3. SpawnWisps 픽이 많을수록 회복 속도 빠름 (스포너 출력 ↑)
4. 영웅 HP 65%, 45% 스킬 페이즈 → 광역 피해 강도 상승
5. SpawnWisps 미픽 빌드: 영웅 스킬에 Wisp 전멸 후 재충전 느림
6. SpawnWisps 3픽 빌드: 소멸 후 빠른 재충전 → 지속 압박 가능
7. 반면 Tank 빌드: 개체 내구성 높아 스킬 1회 피해에도 생존 가능
8. 스킬 페이즈가 SpawnWisps vs IronWill 계열 선택 기준을 바꾸는가?
9. 결과: 영웅 스킬 광역 대응으로 SpawnWisps 가치가 Tank 카드와 경쟁하는 후반 시나리오

#### 플로우 8 — Swarm Tier 오염 후 Tank Tier 실패 시나리오

1. 초반 SpawnWisps 2픽 + Tank 1픽 → Swarm 카운터 2, Tank 카운터 1
2. 다음 3택에 SpawnWisps 재등장: 3픽 캡 중 마지막 픽
3. SpawnWisps 3번째 픽 → Swarm 3 달성 (Swarm Tier1 발동)
4. Tank 카운터 1 → 이후 Tank 카드 2픽 더 필요해야 Tier1 달성
5. 남은 트리거 횟수 제한(영웅 HP 10% 단위, 최대 10회 = 영웅 HP 전부)으로 Tank Tier1 달성 불가 시나리오
6. Swarm 물량 + 개체 강도 기본값 → 영웅 후반 스킬 앞에 취약
7. 플레이어 인식: "SpawnWisps를 너무 많이 픽했다" → 후회 포인트
8. 단, Swarm Tier 달성으로 다른 효과 보상이 있다면 허용 가능한 트레이드오프
9. 결과: 교차 픽 리스크의 최악 시나리오 — 어떤 구제 수단이 필요한가 검토 포인트

#### 플로우 9 — 전역 몬스터 캡(18기) 충돌 시나리오

1. SpawnWisps 3픽 + Swarm Tier 달성 → Wisp 스포너 출력 최대
2. Wisp 대량 스폰 진행 중 전역 캡 18기 도달
3. 다른 몬스터 스포너(Reaper·Hex 등)가 스폰 차단됨
4. 전투 화면: Wisp만 가득 → Reaper·Hex 등 특화 몬스터 부재
5. Debuff/Dps 계열 카드 픽 가치 하락 (대상 몬스터가 없음)
6. 영웅이 Wisp 집중 처치 → 캡 일시 해제 → 타 몬스터 스폰 재개
7. SpawnWisps 물량이 전략 다양성을 억제하는 역할 수행
8. 플레이어 입장: "Wisp 너무 많이 소환했더니 다른 몬스터가 못 나온다" → 인식 필요
9. 결과: 캡 충돌 시 SpawnWisps 과잉 픽 페널티 자연 발생 여부 검증 포인트

---

### 2.4 기획 결정 사항 (game-designer 대상)

다음 항목을 명시적으로 결정해야 한다.

| 결정 사항 | 옵션 A | 옵션 B |
|---|---|---|
| SpawnWisps Swarm 귀속 유지 여부 | 유지 (딜레마 그대로) | Tank 축으로 이동 (긴장 해소) |
| 하이브리드 빌드 허용 의도 | 허용 — 양 축 부분 달성 가능 | 불허 — 집중 빌드에만 보상 |
| SpawnWisps 3픽 캡 충돌 대응 | 자연 페널티로 수용 | 추가 보상(Swarm 캡 해제 등) 설계 |
| Wisp 수량 vs 품질 교환비 설명 | UI 힌트 추가 | 플레이어 자연 학습에 맡김 |

---

### 2.5 보류

현재 SwarmRush 액티브가 미구현 상태 (Multiply 자리 교체 예정 — card-renewal §3.4). SpawnWisps와 SwarmRush의 상호작용(패시브 물량 축적 → 액티브 돌진 폭발)은 SwarmRush 구현 이후 별도 감사 대상.

---

## 3. 과거 감사 대비 차별성

| 기준 | 본 회차 | 과거 유사 항목 |
|---|---|---|
| 카드 대상 | SpawnWisps (패시브, Swarm 축) | 과거 Swarm 항목: Phantom 이동속도, SpawnerHaste, Swarm Tier2 — 모두 다른 카드/시스템 |
| 핵심 질문 | Wisp(Tank 강화 대상)의 수량 카드가 Swarm 축인 것이 딜레마를 만드는가 | Tank Tier 내구도, SpawnerHaste 스택, Phantom 속도 중첩 — 교차 축 귀속 질문 없음 |
| 설계 긴장 유형 | 축 간 정체성 귀속 충돌 (같은 몬스터를 놓고 두 축이 경쟁) | 개별 카드 수치 조정, 시너지 달성 조건 — 교차 긴장 미분석 |
| 데이터 근거 | card-renewal §3.4 + §4.2 + §6.2 삼중 명시 | 각 기획서 독립 섹션 |

---

## 4. 제외 (범위 밖)

| 항목 | 제외 이유 |
|---|---|
| 신규 Wisp 전용 카드 추가 | v0.2 신규 카드 제작 금지 (CLAUDE.md §8) |
| SwarmRush 구현 | card-renewal §3.4 기획 미완 — SwarmRush 설계 전 SpawnWisps 딜레마 명확화가 선행 |
| 서버 연동 통계 수집 | v0.2 서버 연동 금지 |
| Tank·Swarm 시너지 수치 조정 | 설계 의도 확인 전 수치 변경 불가 — 이번 감사는 설계 검증 단계 |
| DebugAutoPicker 훅 구현 | gameplay-programmer 영역 (QA 리포트 §3 의뢰 사항) |

---

## 5. 다음 단계 제안

1. **game-designer**: §2.4 결정 사항 검토 — SpawnWisps Swarm 귀속 유지 의도 확정
2. **game-designer**: 하이브리드 빌드 보상 구조 명세 (Tank 2 + Swarm 2 상태에서 어느 쪽도 Tier 미달일 때 플레이어 경험)
3. **게임플레이 플레이테스트**: 플로우 5(Tank Tier1 달성 후 SpawnWisps 직면)와 플로우 8(오염 후 Tank Tier 실패)을 중점 관찰
4. **qa-simulator**: DebugAutoPicker 훅 구현 후 Tank 집중 빌드 vs SpawnWisps 포함 하이브리드 빌드 승률 비교 시뮬레이션 (qa-report §3 선행 요청)
5. **gameplay-programmer**: qa-simulator 의뢰 대로 `BattleController.DebugAutoPicker` (`#if UNITY_EDITOR`) 구현 — 약 10줄, 프로덕션 경로 불변

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 플레이어는 작은 유령(위스프)을 포함한 몬스터 떼를 이용해 영웅을 막는다. 위스프를 더 강하게 만드는 카드들은 "탱크" 계열인데, 위스프를 더 많이 불러오는 카드는 "떼" 계열로 분류되어 있다. 즉, 위스프를 강하게도 하고 많이도 하려면 서로 다른 두 종류의 카드를 골고루 골라야 한다. 이게 재미있는 선택지가 되는지, 아니면 그냥 헷갈리기만 한지를 확인해야 한다. 그래서 이번에 제안하는 것은: 위스프 강화(탱크 계열)와 위스프 증가(떼 계열) 사이의 선택이 플레이어에게 진짜 고민을 주는지 기획 차원에서 검증하고, 두 계열을 섞어 골랐을 때 게임이 어떻게 흘러가는지를 정의하는 것이다.
