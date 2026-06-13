# Content Audit — 2026-06-14 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 → BalanceConfig 이관

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (docs/design/project_lair_concept.md)
- 참조 spec/plan 수: 28개 (specs 28개 / plans 28개)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED 상태)
- 과거 감사 이력 (git log): 9건 (가장 최근: 2026-06-13 Seoul / UTC 2026-06-12)

---

## 1. 현황

| 카테고리 | 컨셉 | 실제 | 차이 |
|---|---|---|---|
| 영웅 | 1 (Knight) | 1 (Knight.prefab) | 0 ✓ |
| 몬스터 | 6종 | 6종 (Wisp·Wraith·Reaper·Hex·Plague·Phantom.prefab) | 0 ✓ |
| 패시브 카드 | 16장 (컨셉 §11.3 → card-renewal §3 재확정) | 16장 (.asset 16개 확인) | 0 ✓ |
| 액티브 카드 | 12장 | 12장 (.asset 12개 확인) | 0 ✓ |

### 계획 있으나 미구현

- **SwarmRush 효과 교체** — card-renewal §3.4 에서 Multiply(ECardId.Multiply, FastBreedingEffect) → SwarmRush(Phantom 6마리 즉발 소환) 로 교체 예정이나 현행 Multiply 잔존. (Jun 4 감사에서 교체 제안 완료)
- **EternalBleedAura (Debuff Tier3)** — card-renewal §4.5 "신규 표면 필요": `IBattleContext.ApplyHeroAura(EternalBleedAura, -1f)`. BalanceConfig 손잡이 없음.
- **IncrementGlobalMonsterCap (Tank Tier3 +6)** — card-renewal §4.5 "신규 표면 필요". BalanceConfig 손잡이 없음.
- **IncrementAllSpawnerOutputs (Swarm Tier3 모든 스포너 +1)** — card-renewal §4.5 "신규 표면 필요". Jun 3 감사에서 범위 Phantom·Wisp 한정 조정 제안 완료.
- **QA 자동 픽 훅 (DebugAutoPicker)** — QA 리포트 2026-05-22 §3 에서 `BattleController.DebugAutoPicker` 훅 추가를 gameplay-programmer 에게 요청했으나 미구현. 현재 시뮬레이션 전면 BLOCKED.
- **Swarm Tier2 `SwarmTier2PeriodMul` BalanceConfig 손잡이** — `SwarmSynergyTier2` 코드 내 `×0.85` 상수 하드코딩. village-meta-hub §3.3·§3.4 가 이 값의 조정 가능성을 게이트로 걸어뒀으나 이관 미완료.

### QA 권고 미해결

- **DebugAutoPicker 훅** — QA report 2026-05-22 §3 요청, 현재 전체 QA 시뮬레이션 BLOCKED 상태. 해결 시 card-renewal §4.3 수치 검증 가능.
- **village-meta-hub §3.4 만렙+SpawnerHaste 3픽+Swarm Tier2 복합 프로필 시뮬** — 판정 유보 상태. 실패 기준(캡 포화 도달 시간 ≥5s)이 명시됐으나 QA 인프라 미준비로 측정 불가.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 (Seoul) | SHA | subject 설명 |
|---|---|---|
| 2026-06-03 | fcbc975 | Swarm Tier3 출력+1 범위 축소 (전체→Phantom·Wisp 한정, 캡 제거 대응) |
| 2026-06-04 | 399560e | Multiply 액티브 카드 → SwarmRush(팬텀 즉발 소환) 교체 제안 (A#6 미구현 해소) |
| 2026-06-05 | c4f4215 | Tank Tier3 시너지 새 효과 수치 결정 (Wisp·Wraith HP ×1.5 영구, 캡 제거 공백) |
| 2026-06-08 | 307ec17 | Dps ReaperAtkSpeed 배율 재조정 (CD ×0.7→×0.75, Tier2 중첩 하한 없음 해소) |
| 2026-06-09 | 2002c8b | Debuff 출혈 카드(Bleed) 비율 재조정 제안 (2%→1%/s) |
| 2026-06-10 | 440794c | Dps HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 |
| 2026-06-11 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 |
| 2026-06-12 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 |
| 2026-06-13 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 BalanceConfig MinHeroAttackScale |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### [Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 → BalanceConfig 이관]

- **카테고리**: BalanceConfig 손잡이 추가 (빌드 시너지 Tier 수치 이관)
- **요지**: `SwarmSynergyTier2` 코드에 상수 `0.85f`로 박힌 스포너 주기 배율을 `BalanceConfig.SwarmTier2PeriodMul` 필드로 이관한다. village-meta-hub §3.3·§3.4 가 "메타 만렙(×0.927) + SpawnerHaste 3픽(×0.512) + Swarm Tier2(×0.85) 복합 = 총 ×0.403 / Phantom 2.42s — card-renewal §9.6 위험선(×0.435 / 2.61s) 하회"를 수치로 명기하고, 게이트 실패 시 보정값 ×0.92를 처방으로 박아뒀다. 현재 이 보정이 코드 수정이어야 하므로, 손잡이가 없으면 게이트 실패 후 gameplay-programmer 코드 수정→재시뮬 사이클이 강제된다.
- **검증/구현/시너지/데이터**: 4/2/5/4 → 종합 **15**
  - 검증가치 4: Tier2는 Swarm 5장 픽으로 도달 가능한 현실 시나리오. village-meta-hub §3.4 qa-simulator 게이트(만렙+SpawnerHaste 3픽+Tier2 프로필 100판)가 이 배율 값에 직접 의존한다.
  - 구현비용 2: `BalanceConfig.asset`에 `float SwarmTier2PeriodMul = 0.85f` 필드 추가 + `SwarmSynergyTier2.Activate` 한 줄에서 상수를 필드 조회로 교체.
  - 시너지폭 5: Swarm Tier2 × SpawnerHaste 카드(3픽 상한 ×0.512) × 메타 상점 SpawnerHasteUp(만렙 ×0.927) × Tank Tier3(캡 +6, 포화 지연) 4개 시스템이 정확히 이 배율 위에서 만난다.
  - 데이터근거 4: village-meta-hub §3.3이 위험선 하회를 수치로 명기하고 §3.4 게이트에 "실패 시 보정: 0.85→0.92"로 처방까지 박아뒀다. card-renewal §9.6도 스포너 주기 위험선 모니터링을 동일 맥락에서 다룬다.
- **근거**: village-meta-hub §3.3 (SpawnerHasteUp 교차 검산, ×0.403/2.42s), §3.4 (게이트 + 보정 처방 0.85→0.92), card-renewal §9.6 (위험선 ×0.435/2.61s), card-renewal §4.5 (Swarm Tier2 신규 표면 ScaleAllSpawnerPeriods)
- **MVP 범위**: 컨셉 §11.2 BalanceConfig 손잡이 추가 — 데이터 단일화·조정 용이성 범위. 신규 카드/몬스터/서버/메인메뉴 없음.

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**  
   플레이어가 한 런에서 Swarm 축(검정 테두리) 카드를 누적 5장 픽하는 순간 Swarm Tier2가 즉시 발동한다. 이 트리거는 HP 10%마다 또는 30초마다 뜨는 카드 선택지(최대 18회 의사결정) 중 5회 이상을 Swarm 축으로 채웠을 때 달성된다. 패시브 카드(SpawnPhantoms·SpawnerHaste 등)와 액티브 카드(TimeStop·Slow 등)가 모두 카운트에 합산된다.

2. **화면 변화**  
   BattleHud 좌측 Swarm 빌드 카운트 바가 `SWARM 5/7 ██` (Swarm 아이콘 2개) 표시로 갱신되고, 화면 상단 중앙에 "SWARM 시너지 Tier 2 발동!" 토스트가 1.5초 표시된다. 실제 게임 화면에서는 6개 스포너 모두가 이 시점부터 몬스터를 더 빠르게 내보내기 시작해 필드 밀도가 눈에 띄게 올라간다.

3. **입력 행동**  
   플레이어는 카드 선택 팝업(3택 1)에서 Swarm 축 카드를 의도적으로 5회 이상 선택해 Tier2를 유도한다. Tier2 도달 전에 이미 SpawnerHaste(던전의 박동, 주기 ×0.8 영구)를 1~3번 픽했다면, Tier2 발동 시점에 이미 스포너 주기가 단축된 상태 위에 ×0.85 배율이 추가로 곱산된다. 메타 상점에서 SpawnerHasteUp(깨어나는 둥지)을 미리 레벨업 해둔 상태라면 이 곱산은 `BalanceConfig.SpawnPeriod × 메타배율 × SpawnerHaste픽 × 0.85` 의 복합 식으로 동작한다.

4. **시스템 반응**  
   `BuildSynergyService`가 Swarm 카운트 5 감지 후 `SwarmSynergyTier2.Activate(context)` 호출. 내부에서 `IBattleContext.ScaleAllSpawnerPeriods(0.85f)`를 호출해 현재 활성 스포너 6개 전체의 `_spawnPeriod`에 ×0.85를 즉시 곱산한다. 현행 코드에서 이 `0.85f`는 하드코딩 상수이며 BalanceConfig를 참조하지 않는다 — 보정이 필요할 경우 코드 수정 후 재빌드가 강제된다.

5. **반복·재발생 패턴**  
   Swarm Tier2 효과는 한 라운드에 1회만 발동하며 5분 런이 끝날 때까지 영구 유지된다. 발동 이후 SpawnerHaste 카드 추가 픽(×0.8 곱산), Multiply 카드 픽(Phantom 스포너 한정 ×0.6 곱산), 메타 상점 SpawnerHasteUp 레벨이 높을수록 실질 Phantom 스폰 주기는 계속 단축된다. Tier3(7장) 도달 시 `ScaleAllSpawnerPeriods`와 독립적인 스포너 출력 +1이 추가로 발동해 주기 단축과 출력 증가가 동시에 작용한다.

6. **종료·해소 조건**  
   영웅 HP가 0이 되거나(승리) 5:00 타임오버가 발생하면(패배) 런 종료와 함께 모든 스포너 상태가 초기화된다. Swarm Tier2의 ×0.85 배율은 런 종료 전까지 해소되지 않으며, 다음 런 시작 시 BindSpawners가 BalanceConfig 기본 주기를 재주입해 초기 상태로 복원된다.

7. **다른 시스템과 상호작용**  
   ① SpawnerHaste 카드(×0.8, Swarm 패시브 P4): Tier2와 독립 곱산. Tier2 발동 전 픽했든 후에 픽했든 곱산 순서 무관 — 교환법칙 성립. ② 메타 상점 SpawnerHasteUp(만렙 ×0.927): `BindSpawners`(전투 시작)에서 base 주기에 주입 → 이후 SpawnerHaste·Tier2가 위에 중첩. ③ SpawnerHaste 3픽 ×0.512 + Tier2 ×0.85 + 메타 만렙 ×0.927 복합 시 총 배율 **×0.403** → Phantom 6s × 0.403 = **2.42s** (village-meta-hub §3.3) — card-renewal §9.6 위험선 2.61s **하회**. ④ Tank Tier3(캡 +6, 18→24): 캡이 확장되면 빠른 스폰으로 포화에 도달하는 시간이 늦춰져 "캡 포화 5초 내" 기준(§3.4 게이트)에는 유리하게 작용한다.

8. **엣지 케이스**  
   (a) 위험 시나리오 — SpawnerHaste 3픽 + Swarm Tier2 + 메타 SpawnerHasteUp 만렙: Phantom 2.42s 스폰. 글로벌 캡 18 포화 도달이 5초 내면 게이트 실패. 현재 보정값(0.85→0.92)이 코드 상수라 즉시 코드 수정이 필요하다. (b) Tier2 발동 시점에 이미 ScalePeriod 누적분이 있는 경우: 절대값 대입(SetBasePeriod)이 아니라 곱산(ScalePeriod) 방식이므로 누적이 유지된 채 ×0.85가 추가 곱산된다 — 순서 의존성 없음. (c) 글로벌 캡 포화 상태: 스폰 주기가 단축돼도 필드 밀도가 이미 18로 꽉 찼으면 스포너가 자연 백오프되어 체감 효과가 없다 — "Tier2 발동했는데 변화 없음"으로 느껴질 수 있다.

9. **유저 정보·피드백**  
   BattleHud Swarm 셀에 아이콘 2개 표시, 토스트로 발동 확인. 필드 몬스터가 더 자주 등장하는 것으로 Tier2 체감이 가능하다. 단, 글로벌 캡 포화 상태에서는 빈도 증가가 시각적으로 두드러지지 않아 Tier2 가치가 과소 체감될 수 있다 — 글로벌 캡 현황 UI가 없기 때문이며, 이는 Tier2 결과물이 아니라 별도 UX 개선 과제다.

---

### 보류

- **Debuff Tier3 EternalBleedAura BalanceConfig 손잡이** — card-renewal §4.5 신규 표면 필요. 직전 7일 이내 Debuff 축이 2회 나왔고(Bleed Jun 9, HeroAttackDown Jun 13), 특히 어제(Jun 13)가 Debuff였음. 차별 근거가 있으나 간격이 1일로 너무 짧아 보류.
- **QA DebugAutoPicker 훅** — QA 인프라 이슈로 "컨텐츠" 카테고리보다 개발 도구 카테고리. 가장 높은 검증가치이나 콘텐츠 감사 범위를 벗어남. 별도 슬랙/이슈 트래킹 권장.

---

## 3. 과거 감사 대비 차별성

git log 조회 9건 검토 완료. 가장 유사한 과거 커밋: `e4c765b` (2026-06-12 UTC / Seoul Jun 13) — "Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안"

**차별점**:
- Jun 12(UTC) 감사 = **액티브 카드 동작** (TimeStop 5s + Fear 3s가 중첩 픽 시 지속시간 가산되는 것의 상한 캡) — 카드 레벨 효과
- 오늘 제안 = **빌드 시너지 Tier2 효과** (Swarm 축 5장 누적 시 발화하는 영구 배율 — 카드 단위가 아닌 빌드 수준). 게다가 오늘의 핵심 근거는 village-meta-hub §3.3·§3.4의 "메타 만렙 복합 위험 시나리오" — Jun 12 감사는 메타 시스템을 전혀 다루지 않음.
- 구체적으로: 오늘 제안은 메타 상점(v0.2 신설, Jun 10 기획)과 인런 빌드 시너지가 교차하는 복합 상호작용을 처음 다룬다. Jun 12 감사는 카드 두 장의 단순 스택 동작이었다.

---

## 4. 제외 (범위 밖)

- **신규 몬스터 종 추가** — 컨셉 §11 "몬스터 6종" 확정, v0.3+ 범위. CLAUDE.md §8 금지.
- **신규 영웅·카드 리소스 제작** — CLAUDE.md §8 금지 (잠금 슬롯 + 더미만).
- **서버 연동 관련 밸런스** — CLAUDE.md §8 금지, v0.3+.
- **메인 메뉴 / 세팅 화면** — CLAUDE.md §8 금지.
- **QA 시뮬레이션 인프라 구축** — 본 감사 범주 외 (gameplay-programmer / test-engineer 영역). 단, DebugAutoPicker 훅이 구현되면 §3.4 게이트를 즉시 실행할 수 있으므로 강력히 추천.

---

## 5. 다음 단계 제안

- 채택 시 game-designer에게 정식 기획 요청:
  1. `BalanceConfig.asset`에 `SwarmTier2PeriodMul` 필드 추가 (기본값 0.85f)
  2. `SwarmSynergyTier2.Activate` 내 `0.85f` 상수 → `_balance.SwarmTier2PeriodMul` 참조 교체
  3. JsonSync DTO (`BalanceConfigDto`) 에 `swarmTier2PeriodMul` 필드 추가 (spawn-period-balance §5.5 패턴 동일)
  4. village-meta-hub §3.4 qa-simulator 게이트(만렙+SpawnerHaste 3픽+Tier2 복합 프로필 100판)를 DebugAutoPicker 훅 구현 후 함께 실행

---

## 6. 쉬운 설명 (비개발자 요약)

던전 주인인 플레이어가 한 판에서 "스웜(Swarm)" 계열 카드를 5장 이상 모으면, 게임이 모든 몬스터 소환 속도를 자동으로 15% 빠르게 해준다. 이 "15%"라는 숫자가 현재 코드 안에 딱 박혀 있어서, 나중에 바꾸려면 개발자가 코드를 뜯어고쳐야 한다. 문제는 마을 상점(v0.2 신설)에서 소환 속도 업그레이드를 다 사면, 거기다 스웜 카드를 3개씩 쌓으면, 거기다 이 15%까지 더해져서 팬텀이라는 몬스터가 2.42초마다 쏟아져 나오게 된다 — 설계자가 "위험하다"고 표시해 둔 선(2.61초)보다 더 빠르다. 그래서 이번에 제안하는 것은: 이 "15%"를 코드가 아닌 밸런스 설정 파일에서 바꿀 수 있게 옮겨두어, 나중에 수치를 조정할 때 코드 수정 없이 숫자 하나만 바꾸면 되도록 하자는 것이다.
