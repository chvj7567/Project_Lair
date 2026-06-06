# Content Audit — 2026-06-06 — Swarm Tier2 × SpawnerHaste 3픽 복합 스폰 주기 스택 — Nova 쿨 가드레일 재검토

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.6 (2026-05-31), 영웅 스킬 승격 주석 포함(2026-06-04 사용자 확정)
- 참조 spec/plan 수: 26개(specs) + 26개(plans) = 52개
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED)
- 과거 감사 이력 (git log): 3건 (가장 최근: 2026-06-04)

---

## 1. 현황

| 카테고리 | 컨셉 §11 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (`Knight.prefab`) | 일치 |
| 몬스터 | 6종 | 6종 (Wisp·Wraith·Reaper·Hex·Plague·Phantom) + LittleGhost 아트 변형 6개 | ✓ (MVP 6종 완비) |
| 패시브 카드 | 16장 | 16장 SO (`Assets/_Lair/Art/Cards/Items/`) | 일치 |
| 액티브 카드 | 12장 | 12장 SO (`Multiply` 잔존 포함) | 일치 |
| 카드 Effect 클래스 | 28개 | 28개 `.cs` (`Assets/_Lair/Scripts/Card/Effects/`) | 일치 |

### 계획 있으나 미구현

- `SwarmRush` 카드 (`card-renewal.md §3.4`): `Multiply.asset` (`FastBreedingEffect`) 잔존, SwarmRush 미구현. 2026-06-03 감사에서 이미 제안됨.
- `EternalBleedAura` (Debuff Tier3 효과, `card-renewal.md §4.5`): "신규 표면 필요: `ApplyHeroAura(new EternalBleedAura, -1f)`" 명시, 구현 여부 미확인.
- `BattleController.DebugAutoPicker` 훅 (`docs/qa-reports/2026-05-22.md §3`): qa-simulator 가드 미구현, 시뮬레이션 BLOCKED 상태.
- 영웅 스킬 시스템 (`hero-skills.md`): 2026-06-04 사용자 승격 확정, SO + 코드 구현 여부 미확인.

### QA 권고 미해결

- **BLOCKED**: qa-simulator 1건(2026-05-22). 핵심 미해결 = `DebugAutoPicker` 훅 구현. 실측 밸런스 데이터 전무.

### 과거 감사 후보 (git log 조회)

| 날짜 | SHA | subject 설명 |
|---|---|---|
| 2026-06-04 | c4f4215 | Tank Tier3 수치 결정 (Wisp·Wraith HP ×1.5, 캡 제거 공백 해소) |
| 2026-06-03 | 399560e | Multiply → SwarmRush(팬텀 즉발 소환) 교체 제안 (Swarm A#6 미구현 해소) |
| 2026-06-02 | fcbc975 | Swarm Tier3 스포너 출력+1 적용 범위 축소 (전체→Phantom·Wisp 한정) |

> 비고: `docs/design/content-audit/` 폴더에 위 3건 외 파일 6건(2026-05-28~2026-06-02)이 추가로 존재하나, git log에서 검출되지 않음. 구 포맷 커밋 또는 미커밋 추정. 본 루틴 규칙상 git log 기준으로만 중복 회피 처리.

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Swarm Tier2 × SpawnerHaste 3픽 복합 스폰 주기 스택 — Nova 쿨 가드레일 재검토

- **카테고리**: 액티브 카드 효과값 재조정 / BalanceConfig 손잡이 (Nova 쿨다운)
- **요지**: `hero-skills.md §4` 는 maxed Swarm 분석에서 SpawnerHaste 3픽 상한(×0.512)을 "보수적으로 미반영"했다고 명시했다. Swarm Tier2(×0.85) + SpawnerHaste 3픽(×0.512) 복합 스택 시 Phantom 스포너 주기는 6.0s × 0.512 × 0.85 ≈ **2.61s** 로 단축된다. Nova 쿨 7s 가드레일의 근거("Phantom base 주기 6.0s보다 길게 — hero-skills.md §5.1")가 이 시나리오에서 실효를 잃는다. 이 시나리오를 기획서에 명시하고, Nova 쿨다운 7s → **8s** 조정을 검토하도록 제안한다.
- **검증/구현/시너지/데이터**: 4/2/5/5 → 종합 **18**
- **근거**:
  - `docs/design/hero-skills.md §4` — "SpawnerHaste(주기 ×0.8, **1픽 가정** — 3픽 캡 ×0.512는 보수적으로 미반영)" 명시적 갭
  - `docs/design/hero-skills.md §5.1` — Nova 쿨 7s 가드레일 근거: "Phantom base 주기 6.0s"
  - `docs/design/card-renewal.md §4.2` — Swarm Tier2: "모든 스포너 주기 ×0.85 영구"
  - `docs/design/card-renewal.md §3.4 #4` — SpawnerHaste: 전역 3픽 캡으로 ×0.8³ = ×0.512 상한
  - `docs/design/continuous-spawn-round.md §3.1` — Phantom 스포너 base 주기 6.0s
- **MVP 범위**: 컨셉 §11.2 — 액티브 카드 효과값 재조정 + BalanceConfig SO 필드(`HeroSkill_AoeNova.asset._cooldown`)

#### 핵심 수치 검산

```
Phantom base 주기:       6.0s
SpawnerHaste 3픽 상한:  ×0.512  → 3.07s
Swarm Tier2 추가:       ×0.85   → 2.61s  ← 복합 최하 주기

Nova 쿨 7s 동안 공급:   7s / 2.61s ≈ 2.68 사이클
사이클당 최대 3마리:     (기본1 + SpawnPhantoms +1 + Tier3 +1)
7s 내 최대 공급:         약 8마리

hero-skills.md §5.1 기준 = "Nova 쿨 7s > base 리필 6s" → ✓
복합 스택 기준 =          "Nova 쿨 7s >> 복합 리필 2.61s" → ✗ (7s 동안 2.68사이클 공급)

Nova 쿨 8s 시:           8s / 2.61s ≈ 3.07 사이클 공급 (구조적 해소 아님, 방향성 신호)
                         → SpawnerHaste 1픽 기준 4.08s 대비: 8s / 4.08s ≈ 1.96 (≈2 사이클)
```

SpawnerHaste 1픽(hero-skills.md §4 기준) 포함 최소 시나리오에서도 Nova 쿨 8s는 "2사이클 이내"로 리필 제한. 기존 7s에서는 1.7사이클. 8s 조정은 SpawnerHaste 1픽 시나리오 대비 체감 차는 작으나, 3픽 상한 시나리오(2.61s) 방어 방향성을 정량화하는 데 의미 있음.

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**: Swarm 7장 진성 빌드(Tier3 달성) + SpawnerHaste 3픽이 쌓인 후반 전투(약 3~4분대). 패시브 9픽 중 SpawnerHaste 3회 + 타 Swarm 카드 4회 조합으로 한 라운드 안에 달성 가능. 영웅 HP 30% 미만(P3 Nova 활성 후)에 가장 극단적으로 나타남.

2. **화면 변화**: spawner-status-ui(기획서 존재)가 Phantom 스포너 주기 ~2.6s를 표시. 사방에서 Phantom이 빠르게 리필됨. 영웅 P3 Nova가 근접 무리를 일소해도 곧바로 Phantom 2~4마리가 재공급. Swarm 빌드 카운트 바(card-renewal.md §8)에 Tier3 아이콘 3개 표시. 화면이 끊임없이 Phantom으로 채워지는 시각.

3. **입력 행동**: 플레이어는 SpawnerHaste 3픽을 소진한 상태. 남은 패시브 픽에서 타 축(Tank/Dps/Debuff) 카드 선택 가능. 추가 선택 없이 Phantom 압박이 자동으로 지속. 별도 유저 입력 불필요.

4. **시스템 반응**: Phantom 스포너가 2.61s마다 공급. 영웅 Nova(쿨 7s) 발동 후 넉백 3유닛 → 1~2s 후 Phantom 재진입 시작 → 7s 동안 최대 8마리 재공급(Tier3·SpawnPhantoms 중첩 기준). 글로벌 캡 18 범위 내에서 Phantom 점유율이 높게 유지됨. Swarm 빌드 의도("머릿수로 압도") 기술적 충족.

5. **반복·재발생 패턴**: 영웅 P3 이후 — Nova 발동 → Phantom 일소 → 2.61s 간격 재공급 → Nova 발동(7s 후) 루프. Wraith(기본 주기 20.0s × 0.85 × 0.512 ≈ 8.7s)·Hex(15.0s → 6.5s) 등 타 스포너도 Tier2 적용으로 전반적 공급 증가. 모든 스포너가 가속된 상태에서 Phantom만 특히 빠름.

6. **종료·해소 조건**: 영웅 HP 0(승리) 또는 5:00 타임오버(패배). Phantom 공격력(DPS 2.5)은 낮아 Phantom 단독으로는 영웅 압박이 약하지만, Wraith·Reaper·Hex와 함께 포위하며 지속 압박. 영웅이 스킬로 Phantom을 정리해도 바로 채워지므로 스킬 에너지가 Phantom에 쏠려 타 종 처치 여유가 줄어듦.

7. **다른 시스템과 상호작용**: hero-skills.md §5.1 가드레일("Nova 쿨 > Phantom base 주기 6.0s")은 base 기준으로만 성립. 복합 스택 시 가드레일 전제가 뒤집힘. Slow 카드(Swarm A, 몬스터 이동속도 ×1.3, 10s)와 중첩 시 Phantom 도달 속도도 증폭되어 재진입이 더 빨라짐. Multiply(FastBreeding, 팬텀 스포너 추가 ×0.6 곱연산) 픽 시: 2.61s × 0.6 = **1.57s** → 캡 18이 상시 포화에 근접.

8. **엣지 케이스**: SpawnerHaste 3픽 + Swarm Tier2 + Swarm Tier3(출력+1) + SpawnPhantoms(출력+1) + Multiply(FastBreeding ×0.6) 모두 발동 시: Phantom 주기 1.57s × 사이클당 3마리 = 분당 약 115마리(캡 18 상한으로 실제 제한되나 리필 속도 극도로 빠름). 반면 Phantom Power는 2/타, 캡 18 내 Phantom만 있어도 총 DPS = 2 × 18 = 36 — 영웅 HP 4000 기준 약 111초, 다른 종의 DPS까지 더하면 처치 가속. 이 극단 시나리오가 컨셉 §8 "2~4분 처치" 범위를 이탈하는지 수치만으로 단정 불가.

9. **유저 정보·피드백**: spawner-status-ui에서 Phantom 주기 2.61s 또는 1.57s를 플레이어가 읽을 수 있다면, "얼마나 빠른 무리인지"를 직접 인지. 현재 빌드 카운트 바(Swarm Tier3 아이콘 3개) + SpawnerHaste 패시브 픽 수 배지(×3)가 조합 강도 신호로 작용. Nova 이후 즉각 재공급되는 Phantom을 시각적으로 확인해 "끊임없이 몰린다"는 Swarm 빌드 체감이 최대치로 구현됨.

### 보류

- **후보 E (Tank 받데미지 감소 3중 스택 + 영웅 스킬)**: IronWill(×0.7)+ToughHide(×0.75)+GuardianRage(×0.5) 3중 스택 시 DamageTakenScale 0.2625 → 영웅 스킬 실효 데미지 급감 문제. 검증가치 높으나 hero-skills.md §3에서 "Tank = 영웅 스킬 천적(빌드 다양성 보존)"으로 의도 명시됨. 기획서 §3 킬카운트 표가 base Wraith 기준임을 명시·보강하는 형태로 다음 감사에서 재검토 권장(카테고리·요지 비중첩).
- **후보 H (Debuff Tier3 EternalBleedAura 구현 갭)**: `card-renewal.md §4.5` 신규 표면 필요 항목, 구현 미확인. `docs/design/content-audit/` 폴더에 `2026-06-02-debuff-tier3-eternal-bleed-aura-balance.md` 파일 존재(git log 미포함) — 카테고리 유사 중복 가능성으로 회피.

---

## 3. 과거 감사 대비 차별성

git log 조회 3건 검토 완료. 가장 유사했던 과거 커밋: **fcbc975** ("Swarm Tier3 스포너 출력+1 적용 범위 축소") — 차별점: 해당 커밋은 Tier3 출력 스코프(전체→Phantom·Wisp 한정)를 다뤘고, 본 후보는 Tier2(주기 ×0.85) + SpawnerHaste 3픽(주기 ×0.512) 복합 스택의 정량 검토이며 영웅 스킬 Nova 쿨다운 가드레일 상호작용이 핵심. 다루는 메커니즘(출력 수 범위 vs 주기 복합 스택), 관련 기획서(tank-tier3-renewal.md 배경 vs hero-skills.md §4·§5.1), 제안 수치(스코프 제한 vs 쿨 조정)가 모두 다름.

---

## 4. 제외 (범위 밖)

- **영웅 스킬 자체 수치 재조정**: hero-skills.md 이미 설계. qa-simulator 결과 후 별도 밸런스 사이클 예정.
- **Swarm Tier3 재조정**: 2026-06-02 감사에서 다뤄짐.
- **SwarmRush 신규 구현**: 2026-06-03 감사에서 제안됨.
- **EternalBleedAura 구현**: 구현비용 높음, 별도 기획 사이클 필요.
- **글로벌 캡 복원**: tank-tier3-renewal.md §1에서 캡 제거로 확정됨.
- **메타 진행 / 서버 / 메인 메뉴**: CLAUDE.md §8 금지.

---

## 5. 다음 단계 제안

- **채택 시**: game-designer 에게 정식 기획 요청 — "Swarm Tier2 × SpawnerHaste 3픽 복합 스택 정량 검토 + Nova 쿨다운 7s → 8s 조정 여부 결정"
- **선행 과제**: qa-simulator `DebugAutoPicker` 훅 구현(2026-05-22 QA 리포트 §3) — 실측 데이터 없이 본 제안의 영향을 수치로 검증 불가

---

## 6. 쉬운 설명 (비개발자 요약)

플레이어가 "유령 떼(Phantom) 전략"으로 모든 카드를 몰아 픽하면, 유령이 2~3초마다 계속 나타나게 된다. 영웅은 강력한 범위 폭발(Nova)로 유령을 쓸어내지만, 지금 설계는 폭발이 식을 때쯤 새 유령이 다시 차오르는 것을 보장하도록 만들어져 있다. 문제는 최강 전략 빌드에서 유령이 폭발 쿨타임보다 무려 2.7배 빠르게 나온다는 계산이 기획서에서 "나중에 확인하자"로만 메모되어 있고, 구체 검토가 없다는 것이다. 그래서 이번에 제안하는 것은: 그 "나중"을 지금 검토하고 폭발 간격을 7초에서 8초로 늘릴지 판단하자.
