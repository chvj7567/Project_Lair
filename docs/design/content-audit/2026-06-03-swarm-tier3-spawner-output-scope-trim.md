# Content Audit — 2026-06-03 — Swarm Tier3 스포너 출력+1 적용 범위 축소 (전체→Phantom·Wisp 한정, 캡 제거 대응)

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.6 (2026-05-31 카드 전체 리뉴얼 + 2026-06-02 카드 아이콘/시너지 패널 승격 반영)
- 참조 spec/plan 수: 18개 (`docs/superpowers/specs/` 18개 + `docs/superpowers/plans/` 18개)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED, 시뮬레이션 미실행)
- 과거 감사 이력 (git log): 5건 (가장 최근: 2026-06-02, SHA 9cf39cc)

---

## 1. 현황

| 카테고리 | 컨셉 §11 목표 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 없음 ✅ |
| 몬스터 | 6종 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom) | 없음 ✅ |
| 패시브 카드 | 16장 (4축×4) | 16장 (SO 28개 중 패시브 16 완비) | 없음 ✅ |
| 액티브 카드 | 12장 (4축×3) | 12장 (SO 28개 중 액티브 12 완비) | 없음 ✅ |
| 카드 효과 클래스 | 28종 | 28종 (`Assets/_Lair/Scripts/Card/Effects/` 확인) | 없음 ✅ |

### 계획 있으나 미구현

- **SwarmRush 미구현**: `card-renewal.md §3.4 #6` 에서 Multiply(FastBreedingEffect, 팬텀 스포너 주기 ×0.6 영구)를 SwarmRush(팬텀 6마리 즉시 소환)으로 교체 예정이나 현행 에셋에 SwarmRush.asset · SwarmRushEffect.cs 부재. Multiply가 잔존.
- **Swarm Tier3 + 캡 제거 상호작용 QA 미수행**: spec 2026-06-03 `docs/superpowers/specs/2026-06-03-monster-cap-removal-active-trigger-trim-design.md` 가 글로벌 몬스터 캡(18) 전면 제거를 결정. Swarm Tier3(7장 임계, 모든 스포너 출력+1 영구) 는 이 캡이 사실상 안전망 역할을 했으나, 제거 이후 무제한 누적 리스크가 노출됨. `docs/design/tank-tier3-renewal.md §4` 에서 "Swarm 계열 체감 강화 + 액티브 픽 9→5 감소가 동시에 적용 → 페이싱·난이도 변동 폭 큼" 경고.
- **액티브 트리거 9→5 전환 미적용**: spec 2026-06-03 §2.B 에서 `{30,90,150,210,270}` 5개로 축소 결정. `BalanceConfig.asset` 직렬화 값 및 코드 기본값 갱신 필요 (plan Task 5~6).

### QA 권고 미해결

- **QA 2022-05-22**: BLOCKED — 카드 픽 자동화 훅 미구현으로 헤드리스 시뮬레이션 불가. `BattleController.DebugAutoPicker` 훅 추가 요청 상태로 미실행.
- **QA 5차 이전 미해결 ④⑤**: 클리어율 ≤80% · 5분 타임오버 ≥1판 미달 상태. v0.6 카드 리뉴얼 + HP 4600 조정 이후 미재측.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 (UTC) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-01 | 9cf39cc | Debuff Tier3 EternalBleedAura 효과량 상향 (-1%/s → -1.5%/s) |
| 2026-05-31 | cce7243 | 패시브 카드 재조정 — SpawnerHaste 중첩 상한 3픽 캡 도입 (Swarm 패시브) |
| 2026-05-30 | 666d39f | BalanceConfig 영웅 HP 조정 — 4000→4600 (QA 6차 권고 ③ 통과) |
| 2026-05-29 | 531ad9d | 패시브 카드 실효성 회복 — Plague 스포너 배치로 SpawnPlagues·PlagueSlowBoost 활성화 |
| 2026-05-28 | 586dfde | 저주 카드 4종 효과값·지속시간 재조정 — 픽률 하위권 해소 |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Swarm Tier3 스포너 출력+1 적용 범위 축소 — 전체 6종 스포너 → Phantom·Wisp 스포너 2종 한정

- **카테고리**: Swarm 시너지 (Layer 1 Tier3 효과 재조정)
- **요지**: 글로벌 몬스터 캡(18) 제거(spec 2026-06-03) 이후, 현행 Swarm Tier3(7장 임계 — 모든 스포너 동시 출력+1 영구)가 6종 전체 스포너에 적용되면 렌더·물리·AI 성능을 직격하는 무제한 누적 리스크가 발생한다. 적용 범위를 Swarm 축 정체성("머릿수로 압도") 의 핵심 종인 Phantom·Wisp 스포너 2종으로 좁혀, 위험 범위를 기존 6/6 → 2/6(약 67% 감축)로 줄이면서 Swarm Tier3 보상 의미는 유지한다.
- **검증/구현/시너지/데이터**: 5/2/4/4 → 종합 **17**
- **근거**:
  - spec `docs/superpowers/specs/2026-06-03-monster-cap-removal-active-trigger-trim-design.md` §4 "성능: 캡 제거로 5분간 몬스터 무제한 누적 가능. 특히 Swarm 시너지(출력+/주기단축)와 겹치면 렌더/물리/AI 비용이 선형 증가. 안전장치 없이 진행" 명시.
  - `docs/design/tank-tier3-renewal.md §4` "Swarm 계열 체감 강화 + 액티브 픽 9→5 감소가 동시에 적용 → 페이싱·난이도 변동 폭 큼. 위험 시나리오는 상쇄가 아니라 분포 양극화" — game-designer 가 구체적 Swarm 리스크를 비구속 코멘트로 이미 포착.
  - `docs/design/card-renewal.md §4.2` Swarm Tier3 효과: "모든 스포너 동시 출력 +1 (영구)". 6 스포너 전부에 +1 = 6마리/주기 추가 스폰 → 캡 없이 5분 누적하면 최악 수백 마리 이상.
  - Tank Tier3 교체 선례(`tank-tier3-renewal.md §1`): "캡 +6 → Wisp·Wraith HP ×1.4" — 캡과 연동된 Tier3 효과가 이미 한 번 재설계됨. 같은 캡 제거 사이클에서 Swarm Tier3 재설계 미완이 asym gap.
- **MVP 범위**: 컨셉 §11.2 — "시너지 검증 ✅". 몬스터 종수·카드 매수 불변, Tier3 효과 수치/범위 조정만.

#### 유저 플로우

1. **노출 시점·트리거**: 한 라운드에서 Swarm 축 카드 7장(누적 픽 카운트 기준, 중복 픽 포함)을 쌓으면 즉시 1회 발화. 액티브 트리거 5회 체제(30/90/150/210/270초) 전환 후엔, Swarm 7장 달성이 가능한 타이밍은 패시브 트리거(HP 80~30% 구간 4회) + 액티브(90~210초 사이 3회)가 교차하는 1분 30초~2분 30초 구간이 가장 일반적이다. 픽 직후 즉시 `SwarmSynergyTier3.Apply` 가 호출된다.

2. **화면 변화**: 팝업 닫힘 직후 좌상단 BuildSynergyPanel Swarm 행 배경이 0.3초 펄스(알파 30→100→30%), SWARM 아이콘이 3개로 증가. 화면 중앙 상단 토스트 "Swarm 시너지 Tier3 발동!" 1.5초 표시(card-renewal.md §8.4). 적용 범위 변경 후에는 Phantom·Wisp 스포너만 출력 카운터가 1 증가—이전 전체 6종 대비 화면 체감 변화는 팬텀·위스프의 밀집도가 두드러지게 증가하는 형태로 나타난다.

3. **입력 행동**: 카드 3택 팝업에서 Swarm 축 카드 7번째를 클릭(CHButton). Swarm 7장 진성 빌드의 전형적 경로는 패시브 4종(PhantomMoveSpeedBoost·SpawnPhantoms·SpawnWisps·SpawnerHaste)을 모두 픽 + 액티브 3종(TimeStop·Multiply·Slow) 각 1픽 이상. 3픽 캡으로 단일 카드 반복만으로는 Tier1(3장)까지만 단독 도달, Tier3는 서로 다른 Swarm 카드 최소 3종 조합 필요.

4. **시스템 반응**: `SwarmSynergyTier3.Apply(IBattleContext ctx)` → 현행은 `ctx.IncrementAllSpawnerOutputs(1)` (6종 전체 +1). 제안 변경 후: Phantom·Wisp 스포너만 `_outputCount += 1`(IBattleContext 또는 Spawner 직접 필터). 변경 즉시 해당 스포너 다음 Tick부터 2마리/주기 스폰. 기존 필드에 있는 Phantom·Wisp는 영향 없고, 이후 스폰분부터 적용.

5. **반복·재발생 패턴**: Tier3는 Layer 1 발화이므로 라운드당 1회만(card-renewal.md §4.1). 한 번 발화한 뒤 Swarm 카드를 추가 픽해도 Tier3 재발화 없음. 그러나 출력+1은 영구 버프이므로 발화 이후 라운드 종료까지 Phantom·Wisp 스포너가 2마리/주기로 계속 스폰한다. SpawnPhantoms 3픽(+3 출력) + Tier3 +1 = Phantom 스포너 최대 4마리/주기.

6. **종료·해소 조건**: 영웅 사망 또는 5분 타임오버(BattleClock 300초). `BuildSynergyService.Reset`은 런(라운드) 종료 시점에만 호출되므로 스포너 출력 변경이 다음 런으로 이월되지 않음. 런 중에는 Tier3 효과가 한 번 걸리면 해소 불가 — 영구 레일.

7. **다른 시스템과 상호작용**: SpawnPhantoms(패시브, 가산 누적 최대 +3) + Tier3(+1) 조합 시 Phantom 스포너 4마리/주기. SpawnerHaste 3픽(×0.512) + Swarm Tier2(×0.85) 조합 시 Phantom 스포너 주기 6s × 0.512 × 0.85 ≈ 2.6s → 4마리/2.6s = 초당 1.5마리. 캡 없이 5분 = 최대 450마리(이론 상한). 이는 `tank-tier3-renewal.md §4` 가 지적한 분포 양극화의 핵심 Swarm 시나리오다. 반면 Debuff·Tank·Dps 빌드는 Tier3 개입 없이 상대적으로 페이싱 유지.

8. **엣지 케이스**: Wisp 스포너가 Plague 스포너로 전환된 경우(Spawner #4가 현행 Plague로 고정 — continuous-spawn-round.md §3.1), Wisp 스포너는 Spawner #1 한 개뿐. Tier3 Wisp+1이 실제로는 1개 스포너만 증가 → 체감은 Phantom+1보다 작음. ReplaceWispsToWraith 픽 없이도 Wisp 스포너가 1개뿐이라, Swarm Tier3의 실질 이득은 Phantom+1에 편중. 이 비대칭은 제안 변경 후에도 동일하게 유지됨.

9. **유저 정보·피드백**: 좌상단 BuildSynergyPanel Swarm 아이콘 3개 + 토스트로 Tier3 달성 인지. 이후 필드에서 팬텀(검정 작은 구체)과 위스프(초록 작은 구체)가 이전보다 밀집되는 것을 시각적으로 확인. "Swarm 7장 모으면 팬텀·위스프 두 배 나온다"는 직관적 학습이 가능(색 = 몬스터 대표색 = 축 카드 테두리색, card-renewal.md §2). 반면 리퍼·헥스·플레이그·레이스는 Tier3 후에도 스폰 수가 변하지 않아, 플레이어가 빌드 축의 의도(Swarm = 팬텀·위스프로 압도)를 직접 체감한다.

### 보류

- **Multiply → SwarmRush 구현**: 미구현 상태이나 현행 Multiply(FastBreedingEffect)가 Phantom 스포너 주기 ×0.6 영구로 대체 기능 중. 긴급도 낮음. 향후 Swarm 빌드 페이싱 정착 후 별도 사이클.
- **MarkOfDeath + TimeStop 조합 검토**: Dps 액티브 MarkOfDeath(5s 받피 ×1.5) + Swarm 액티브 TimeStop(5s 정지) 동시 발동 시 5초간 완전 제압+150% 피해. 캡 제거·액티브 5회 변경 이후 영향 미계측. Swarm Tier3 재조정 후 qa-simulator 캠페인 데이터로 후속 판단 권장.

---

## 3. 과거 감사 대비 차별성

git log 조회 5건 검토 완료.

- **9cf39cc (2026-06-02)**: Debuff Tier3 EternalBleedAura 효과량(−1%/s → −1.5/s). 카테고리=Debuff, 대상=Tier3 효과량 수치 → 본 후보(Swarm, 적용 범위 축소)와 축·내용 모두 다름.
- **cce7243 (2026-05-31)**: SpawnerHaste 중첩 상한 3픽 캡 도입. 카테고리=Swarm 패시브(개별 카드 픽 캡), 근거=중첩 천장 → 본 후보는 카테고리=Swarm 시너지 Tier3(빌드 누적 보상), 근거=캡 제거 이후 물량 리스크. **같은 Swarm 축이지만 대상 레이어(개별 카드 픽 정책 vs 시너지 임계 효과) 와 발생 원인(픽 반복 천장 vs 글로벌 캡 제거) 이 완전히 다름.**
- 나머지 3건(666d39f·531ad9d·586dfde): BalanceConfig HP·Plague 구조·저주 카드 효과값. 카테고리·요지·근거 모두 상이.

→ 중복 없음. 가장 유사했던 과거 커밋: cce7243 (SpawnerHaste 3픽 캡, Swarm 패시브) — 차별점: 대상이 개별 카드 픽 상한이 아니라 7장 시너지 임계 효과의 적용 종 범위이며, 촉발 원인이 spec 2026-06-03 캡 제거라는 새로운 구조적 변화임.

---

## 4. 제외 (범위 밖)

- **신규 몬스터 종 추가**: 컨셉 §11.2 "몬스터 6종" lock — 제외.
- **SwarmRush 신규 카드 구현**: 새 SO·effect class 생성 필요, 이번 사이클 범위 외. 보류.
- **사운드·아트 추가**: CLAUDE.md §8 명시 금지.
- **메타 진행·서버**: 컨셉 §11.2 제외 항목.
- **타임오버 방지용 영웅 HP 추가 상향**: QA 데이터 없이 임의 상향 → 검증 가설 훼손 우려. 별도 밸런스 사이클.

---

## 5. 다음 단계 제안

1. **채택 시**: game-designer 에게 정식 기획 요청 → `SwarmSynergyTier3.Apply` 의 `IncrementAllSpawnerOutputs(1)` 호출을 Phantom·Wisp 2종 한정 `_outputCount += 1` 로 교체하는 기획서 작성.
2. **병행 권장**: spec 2026-06-03 구현(캡 제거 + 액티브 5회) 완료 직후 qa-simulator 1회 캠페인 — 핵심 메트릭: 빌드축별 평균 사망 시각 분산, 타임오버 발생률, Swarm 7장 빌드의 사망 시각 vs 다른 축. 이 데이터가 나오면 Swarm Tier3 재조정 필요성이 정량적으로 확정됨.
3. **card-renewal.md 동기화**: Tier3 효과 변경 확정 시 `docs/design/card-renewal.md §4.2` Tier 효과 표와 §4.4 설계 의도 갱신 필요(tank-tier3-renewal.md §6 패턴 참조).

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 플레이어는 5분 동안 몬스터를 카드로 강화하며 영웅을 쓰러뜨린다. "스웜 7장 빌드"는 팬텀(작은 검은 구체)과 위스프(초록 구체)를 쏟아내는 전략인데, 지금까지는 필드에 동시에 존재할 수 있는 몬스터 수가 18마리로 제한되어 있어 너무 많아지지 않았다. 그런데 최근 이 제한을 아예 없애기로 했다. 제한이 사라진 상태에서 스웜 7장 보상(모든 스포너에서 한 번에 두 배씩 나오기)이 6종 몬스터 전체에 적용되면, 이론상 5분 동안 수백 마리가 쏟아져 게임이 버벅거리거나 너무 쉬워질 수 있다. 그래서 이번에 제안하는 것은: "7장 보상이 팬텀과 위스프 두 종류에만 두 배 효과를 주도록 좁히자"는 것이다.