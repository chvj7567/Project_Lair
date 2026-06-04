# Content Audit — 2026-06-05 — Tank Tier3 시너지 효과 수치 결정 (Wisp·Wraith HP ×1.5 영구)

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

---

## 0. 입력 스냅샷

- 컨셉서 버전: v0.6 (2026-05-31)
- 참조 spec/plan 수: 23개 spec / 23개 plan
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED 상태, 시뮬레이션 미실행)
- 과거 감사 이력 (git log `[Routines][Daily Content Audit]`): 4건 (가장 최근: 2026-06-03)
  - 보조 검색 (`[docs] - 컨텐츠 감사` 포맷): 0건
  - 폴더 파일 확인 결과 2026-05-28~2026-05-30 파일 3건 추가 존재하나 git log 미매칭 (구 포맷 또는 수동 생성으로 추정)

---

## 1. 현황

| 카테고리 | 컨셉 §11 목표 | 실제 에셋 수 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 0 |
| 몬스터 | 6 | 6 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 0 |
| 패시브 카드 | 16 | 16 (Cards/Items/*.asset 중 P 분류 16장) | 0 |
| 액티브 카드 | 12 | 12 (Cards/Items/*.asset 중 A 분류 12장) | 0 |
| 카드 효과 클래스 | 28 | 28개 중 SwarmRush 미구현 (Multiply/FastBreedingEffect 잔존) | Swarm A#6 공백 잔존 |

### 계획 있으나 미구현

| 항목 | 근거 문서 | 상태 |
|---|---|---|
| **Tank Tier3 시너지 효과** | `specs/2026-06-03-monster-cap-removal…` §2A — "구체 스탯·수치는 game-designer 가 §8 밸런스 맥락에서 설계" | 공백 (캡 제거로 기존 효과 무효, 새 효과 미결정) |
| SwarmRush (`ECardId.Multiply` 자리) | `docs/design/card-renewal.md` §3.4 — 원안 SwarmRush 신설 미실현 | Multiply/FastBreedingEffect 잔존 (구감사 2026-06-03 제안) |
| 영웅 스킬 3종 수치 (DashStrike/OrbitingBlade/AoeNova) | `specs/2026-06-04-hero-skills-design.md` §8 — "각 스킬 수치 game-designer에 위임" | spec 완료, game-designer 단계 미진입 |
| Spawner 스폰 주기 BalanceConfig 이관 | `docs/design/spawn-period-balance.md` — gameplay-programmer 구현 대기 | 기획 완료, 미구현 |
| 동시 몬스터 캡 제거 + 액티브 트리거 5회 축소 | `specs/2026-06-03-monster-cap-removal…` | 기획 완료, 미구현 |

### QA 권고 미해결

| 권고 | 출처 | 상태 |
|---|---|---|
| `BattleController.DebugAutoPicker` 훅 추가 (게임플레이 영향 없는 에디터 전용) | QA 2026-05-22 §3 | 미구현 — 의사결정 보류 중 |
| 시뮬레이션 실행 방식 결정 (대화형 에디터 vs `[UnityTest]` 래핑) | QA 2026-05-22 §4.1 | 사용자 결정 필요 |

### 과거 감사 후보 (git log 조회 결과)

| 날짜(커밋) | SHA | subject 설명 |
|---|---|---|
| 2026-05-31 | cce7243 | 패시브 카드 재조정 — SpawnerHaste 중첩 상한 3픽 캡 도입 |
| 2026-06-01 | 9cf39cc | Debuff Tier3 EternalBleedAura 효과량 상향 (-1%/s → -1.5%/s) |
| 2026-06-02 | fcbc975 | Swarm Tier3 스포너 출력+1 적용 범위 축소 (전체→Phantom·Wisp 한정, 캡 제거 대응) |
| 2026-06-03 | 399560e | Multiply 액티브 카드 → SwarmRush(팬텀 즉발 소환) 교체 제안 (Swarm A#6 미구현 해소) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Tank Tier3 시너지 새 효과 수치 결정 — Wisp·Wraith HP ×1.5 영구 글로벌

- **카테고리**: 시너지 (Layer 1 Tier3 수치 결정)
- **요지**: 동시 몬스터 캡 제거(spec 2026-06-03)로 기존 Tank Tier3 효과("필드 캡 +6")가 무효화됐고, 교체 효과 수치가 **미결정 상태**다. spec은 "Wisp+Wraith 추가 내구 버프, RegisterMonsterTypeBuff 구조, game-designer 가 수치 설계"로만 위임했다. 본 감사는 그 공백을 **Wisp·Wraith HP ×1.5 영구 글로벌**로 채울 것을 제안한다.
- **검증가치**: 5 / **구현비용**: 2 / **시너지폭**: 4 / **데이터근거**: 5 → 종합 **18점**
- **근거**: `docs/superpowers/specs/2026-06-03-monster-cap-removal-active-trigger-trim-design.md` §2A — "Tank Tier3 캡 +6 효과 → Wisp+Wraith 추가 내구 버프로 교체. 구체 스탯·수치는 game-designer 가 §8 밸런스 맥락에서 설계" (명시적 공백)
- **MVP 범위**: 컨셉 §11.2 — 카드 매수 lock 불변. `card-renewal.md` §4.2 Layer 1 시너지 Tier 표 수치 결정. 메타·서버·사운드·아트 미작업.

#### 수치 제안

| 시너지 단계 | 현행 | 제안 (이 감사) |
|---|---|---|
| Tank Tier1 (3장) | Wisp·Wraith HP ×1.3 (영구) | 변경 없음 |
| Tank Tier2 (5장) | Wisp·Wraith Power ×1.2 (영구) | 변경 없음 |
| **Tank Tier3 (7장)** | **미결정** (기존 캡 +6 무효화, 새 효과 공백) | **Wisp·Wraith HP ×1.5 (영구, RegisterMonsterTypeBuff)** |

적용 후 Tank 7장 빌드에서 Wisp·Wraith HP 누적 배율:
- Tier1 × Tier3 = ×1.3 × ×1.5 = **×1.95** (기본값의 약 2배)
- `WispHpBoost` 카드 1픽 추가 시: ×1.95 × ×1.5 = **×2.925** — 강하지만 Tank 진성 빌드의 진성 보상으로 설계 의도와 부합

수치 근거 (컨셉 §8 대조):
- §8 밸런스 기준 "영웅이 2~4분 사이에 죽도록 튜닝". HP ×1.95 Wisp(390, 기본 200)·Wraith(975, 기본 500)는 영웅 DPS 50/타 × 1초 공속을 고려 시 Wisp가 7.8초, Wraith가 19.5초 생존 가능(기본 4초·10초 대비 약 2배). 빌드 다양성을 열면서 §8 "2~4분" 범위를 크게 이탈하지 않는 수치.
- Tier2 Power ×1.2와 동일 크기의 "두 단계 누적" 느낌 유지. Tier1·2·3 모두 ×1.2~×1.5 범위로 일관된 step 크기.

구현 표면:
```
//# TankSynergyTier3 의 IncrementGlobalMonsterCap(6) 을 아래로 교체 (game-designer 수치 확정 후)
ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.HpMax, 1.5f);
ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.HpMax, 1.5f);
```
> 코드 구현은 gameplay-programmer 영역. 본 제안은 수치 단정만.

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**
   Tank 축 카드를 7장 이상 누적 픽했을 때 발동한다. 패시브(HP 10%마다)·액티브(30초마다)를 가리지 않고 같은 축 픽이 7장째 누적되는 순간 즉시 발화한다. 액티브 트리거가 5회로 줄어든 현행 기준으로도 패시브 9회 + 액티브 5회 = 최대 14픽 기회가 있어 Tank 7장은 달성 가능 범위다.

2. **화면 변화**
   BattleHud 좌상단의 빌드 시너지 패널 Tank 행이 "TANK 7+" 로 갱신되고 TANK 아이콘이 3개로 채워진다. 동시에 화면 상단 중앙에 토스트 텍스트 "Tank 시너지 Tier 3 발동!" (Tank 축 키 색 `#22C55E`)이 1.5초 노출된다. 0.3초 펄스 애니메이션이 Tank 패널 셀 배경에서 실행된다.

3. **입력 행동**
   플레이어의 별도 입력 없이 자동 발화한다. 7번째 Tank 카드를 픽한 카드 선택 팝업 닫힘과 동시에 즉각 적용된다. 플레이어가 확인할 수 있는 것은 토스트 메시지와 패널 아이콘 3개뿐이며, 별도 확인·승인 없이 효과가 영구 적용된다.

4. **시스템 반응**
   `IBattleContext.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.HpMax, 1.5f)`와 동일한 Wraith 호출이 각 1회 실행된다. 현재 필드에 살아있는 모든 Wisp·Wraith의 최대 HP가 ×1.5 배율로 즉시 소급 적용되고, 이후 스폰되는 Wisp·Wraith도 동일 배율을 갖고 태어난다. Tier1의 HP ×1.3과 곱연산 누적되어 최종 배율은 ×1.95가 된다.

5. **반복·재발생 패턴**
   같은 임계(7장)는 런당 1회만 발화한다(`card-renewal.md` §4.1). 8번째, 9번째 Tank 카드를 픽해도 Tier3는 재발화하지 않는다. 단 Tier1(3장)·Tier2(5장)·Tier3(7장)의 세 효과는 모두 영구 유지되며 서로 곱연산 누적된다.

6. **종료·해소 조건**
   전투 종료(영웅 처치 승리 또는 5분 타임오버 패배) 시 BattleController.Restart 흐름에서 모든 글로벌 버프가 초기화된다. 효과는 런 단위이며 메타 진행에 영향을 주지 않는다(메타 미구현, 컨셉 §11.2).

7. **다른 시스템과 상호작용**
   - `WispHpBoostEffect` 카드(Wisp HP ×1.5, 최대 3픽 = ×3.375 상한): Tier3 ×1.5와 곱연산 → Wisp HP 최대 ×3.375 × ×1.95 ≈ ×6.6 (3픽 카드 + Tier3 동시) — 강한 Tank 빌드의 천장.
   - `WallOfWisps`(ToughHide, 받피×0.75 영구)·`Berserk`(GuardianRage, 받피×0.5 15초): Tier3는 HP이고 ToughHide/GuardianRage는 DamageTakenScale이라 별도 축에서 중첩 → Tank 7장 빌드는 HP ×1.95 + 상시 받피 ×0.75 + 버스트 받피 ×0.5를 동시에 가질 수 있어 생존력이 극적으로 높아진다.
   - `WraithDamageBoostEffect` 카드(Wraith HP ×1.5): Tier3 HP ×1.5와 곱연산 → Wraith HP ×1.5 × ×1.95 = ×2.925. Wraith 기본 HP 500 → 약 1463. 레이스가 극단적인 벽이 된다.
   - SpawnerHaste / Multiply(FastBreeding)와의 상호작용 없음 (다른 축 효과).

8. **엣지 케이스**
   - Wisp 스포너가 없는 빌드: Tier3 적용 시 현재 필드 Wisp가 0마리이면 효과가 "소급"할 개체가 없지만 이후 스폰 시 자동 반영된다(`card-renewal.md` §9.1 안전 처리 — "RegisterMonsterTypeBuff 는 이후 스폰 + 현재 필드 소급이므로 스포너가 없을 때 무영향, 다시 스폰되면 자동 반영"). 별도 처리 불요.
   - `SpawnWisps` 카드로 Wisp 출력이 증가한 상태: Wisp 개체 수가 늘어난 만큼 HP ×1.95 효과의 "질량"도 커져 영웅 입장에서 처리 부담이 선형 증가한다. 설계 의도와 일치.
   - `WispHpBoost` 3픽 상한(전역 3픽 캡, `card-3pick-cap.md`) 후 Tier3 발화: 카드 픽과 시너지 발화는 독립적 표면이므로 충돌 없음.

9. **유저 정보·피드백**
   플레이어는 토스트 "Tank 시너지 Tier 3 발동!" 텍스트로 발화를 즉각 인지한다. 시너지 패널의 Tank 행에 TANK 아이콘 3개가 채워지며, 이후 픽 팝업 상단 빌드 카운트 바에서 "TANK 7+" 표기가 유지돼 적용 중임을 지속 확인할 수 있다. 수치 변화(HP가 올랐음)는 팝업이나 HUD에서 직접 노출되지 않으나, 필드 몬스터가 더 오래 버티는 체감으로 전달된다.

### 보류

| 후보 | 사유 |
|---|---|
| 액티브 트리거 5회 축소 후 카드 효과 재조정 | 카테고리 다르나 spec이 이제 막 완성됐고 QA 데이터가 없어 수치 근거 약함(종합 13점). 캡 제거 + 트리거 축소가 구현된 뒤 qa-simulator 후 판단이 적절. |
| 영웅 스킬 3종 수치 (DashStrike·OrbitingBlade·AoeNova) | MVP §11 밖 범위(사용자 명시 승격 항목). game-designer가 정식 파이프라인에서 다루게 됨. 중복 불가. |
| Multiply → SwarmRush 교체 | 직전 감사(2026-06-03, SHA 399560e)와 카테고리·요지·근거 모두 겹침. 중복 회피 원칙에 따라 제외. |
| Debuff Tier3 / Swarm Tier3 추가 조정 | 직전 7일 이내 각각 2026-06-01(Debuff Tier3), 2026-06-02(Swarm Tier3) 에 등장. QA 데이터 없이 재조정하면 근거 약함. |

---

## 3. 과거 감사 대비 차별성

git log 조회 4건 검토 완료.

| 비교 기준 | 과거 감사 (가장 유사) | 이번 감사 |
|---|---|---|
| 카테고리 | Debuff Tier3 (9cf39cc), Swarm Tier3 (fcbc975) — 시너지 Tier 효과 조정 | **Tank Tier3** — 다른 축 |
| 요지 | 기존 효과 수치 상향/범위 조정 | **새 효과 수치 결정 (공백 채우기)** — 기존 효과가 캡 제거로 무효화된 공백 |
| 근거 | 밸런스 데이터 또는 spec 권장 | spec 2026-06-03 §2A 에 "game-designer 수치 설계" 명시 위임 |

가장 유사했던 과거 커밋: `fcbc975` (Swarm Tier3 범위 축소) — 차별점: Swarm Tier3는 기존 효과의 적용 범위를 좁히는 조정이었고, 이번 Tank Tier3는 효과 자체가 공백인 상태에서 새 스탯·수치를 결정하는 것. "조정"이 아닌 "신규 결정"이라는 본질적 차이.

폴더에서 확인된 2026-05-28~2026-05-30 파일 3건은 git log 미매칭 — Tank Tier3 관련 파일명 없음 (spawn-interval/curse-rebalance/plague-spawner 주제).

---

## 4. 제외 (범위 밖)

| 항목 | 제외 사유 |
|---|---|
| 영웅 스킬 수치 결정 | 컨셉 §11 밖 (사용자 명시 승격 2026-06-04). game-designer 파이프라인 별도 진행. |
| 서버 연동 / 메타 진행 | 컨셉 §11.2 ❌ (v0.2 항목) |
| 사운드 추가 | CLAUDE.md §8 절대 금지 |
| 메인 메뉴 / 세팅 화면 | CLAUDE.md §8 절대 금지 |
| 몬스터 신규 추가 | 컨셉 §11.2 ❌ (MVP 6종 고정) |

---

## 5. 다음 단계 제안

- **채택 시**: `game-designer` 에게 Tank Tier3 공식 수치 기획 요청. 이 감사의 제안 수치(HP ×1.5)를 초안으로 삼아 §8 밸런스 검토 + card-renewal.md §4.2 Tier 표 갱신.
- **구현 후**: `TankSynergyTier3.cs` 의 `IncrementGlobalMonsterCap(6)` 을 `RegisterMonsterTypeBuff` 2행으로 교체 (gameplay-programmer).
- **검증**: qa-simulator `DebugAutoPicker` 훅이 구현되면 Tank 7장 빌드 시뮬 후 영웅 평균 사망 시간과 §8 기준(2~4분) 정합 확인 권장.
- **연계**: monster-cap-removal + active-trigger-trim 구현 완료 후, 새 Tank Tier3 포함해 전체 밸런스 qa-simulator 재검증을 제안.

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 플레이어가 같은 계열의 카드를 7장 이상 고르면 특별 보너스(시너지 Tier3)가 터지도록 설계되어 있다. "탱크" 계열 카드를 7장 모으면 위스프와 레이스가 특별 강화를 받아야 하는데, 며칠 전 게임 구조를 바꾸면서 원래 있던 보너스 내용이 통째로 사라졌고 새 보너스가 아직 정해지지 않은 상태다. 마치 레벨업 보너스 칸이 텅 비어있는 것과 같다. 그래서 이번에 제안하는 것은: 탱크 7장 달성 시 위스프와 레이스의 HP를 50% 추가로 올려줘서, 누적 효과로 HP가 거의 2배가 되도록 보너스 내용을 채우자는 것이다.
