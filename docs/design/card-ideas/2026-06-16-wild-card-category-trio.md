# Card Ideas — 2026-06-16 — 와일드 카드 신설: 빈 카테고리를 채우는 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 와일드 카드 신설 — 컨셉서 §4.2 가 "액티브 이벤트 풀 = 저주 1 + 버프 1 + **와일드 1**" 로 명시했음에도 현행 28장에 와일드 카드가 한 장도 없는 구조적 공백을 채운다. 카드 선택 시스템 자체·영웅 디버프 시너지·빌드 축 피드백을 각각 건드리는 와일드 3종.
- **목록**: 이중 영감 (DoubleDraft) / 전술 급등 (TacticalSurge) / 저주 공명 (CurseResonance)
- **기존 28장 + git log 과거 18회차 (2026-05-28 ~ 2026-06-15) 와의 중복 회피 확인됨**
  - 와일드 카테고리 자체가 18회 전 기간 동안 한 번도 제안된 적 없음 (각 회차 slug 및 목록 대조).
  - 기존 28장: Tank/Dps/Debuff/Swarm 축 카드만 존재. 와일드 0장.
  - QA 리포트는 BLOCKED 상태이므로 구조적 공백 분석을 근거로 삼음.

---

## 1. 이중 영감 (DoubleDraft) — 가칭

- **카테고리**: 액티브 와일드
- **효과 모델**: 픽 즉시 "다음 번 카드 선택 화면"(패시브든 액티브든 불문)에서 3장 → **5장** 제시. 단 1회만 적용 후 초기화. 이후 선택 화면은 평소 3장으로 복귀.
  - 수치 근거: 28장 풀에서 특정 카드 1장 발견 확률 — 3장: ≈10.7%, 5장: ≈17.4%. Tier 임계(3·5·7장) 1장 전에 픽하면 필요 축 카드 접근 확률 ×1.63 향상. 중첩 픽 가능 (2픽 = 2회 연속 +2, 3픽 = 3회). 단 연속 중첩 시 다음 두 선택 화면에 순차 적용.
- **구현 패턴**: `DoubleDraftEffect.Apply()` → `IBattleContext.SetNextDrawBonus(2)` (1회성 상태값 저장) → `CardDeck.Draw(count)` 내부에서 `count = 3 + context.ConsumeNextDrawBonus()` 로 참조 후 초기화. 기존 `Draw(3)` 호출부 수정 없이 context 경유로만 처리 가능.
- **시너지 후크**: Tier 임계 직전(예: Dps 4카드로 Tier2 = 5장 1장 전)에 픽 → 5장 화면에서 축 카드 발견 확률 대폭 향상. `TacticalSurge`(아래)와 연계: DoubleDraft로 원하는 축 카드 확보 → 축 카운트 증가 → TacticalSurge 발동 강도 상승.
- **구현 비용 추정**: 2 (IBattleContext 에 NextDrawBonus 상태 1개 + CardDeck.Draw 수정 1곳. 기존 트리거 파이프라인 무수정)
- **중복 재검증**: 기존 28장에 "선택지 수 조작" 카드 없음. 과거 18회차 전 기간에 걸쳐 draw count 조작을 제안한 파일 없음 (slug 목록 전수 대조). 카드 선택 메타에 직접 개입하는 최초 제안. ✓

---

## 2. 전술 급등 (TacticalSurge) — 가칭

- **카테고리**: 액티브 와일드
- **효과 모델**: 픽 즉시 현재 빌드 카운트 최대 축(Tank·Dps·Debuff·Swarm 중 픽 수 1위)의 소속 몬스터 종 전체에 **HP ×1.4, Power ×1.4, MoveSpeed ×1.2, 10초** 임시 버프 적용. 동점 시 마지막으로 픽한 카드의 축 우선.
  - 수치 근거: Dps 축 최대 예시. 필드 Reaper(HP 100, DPS 40) → HP 140, DPS 56. Hex(HP 60, DPS 30) → HP 84, DPS 42. 10초 기준 Reaper 2마리 × 56 DPS = 1120 HP 잠재 피해 (영웅 HP 1000의 112%). MarkOfDeath(영웅 피해 ×1.5, 5s)와 중첩 구간 Reaper DPS 56 × 1.5 = 84. Tank 축 예시: Wisp·Wraith HP ×1.4 — IronWill(받는 데미지 ×0.7)과 조합 시 10초간 사실상 무적에 가까운 Tank 라인.
  - 픽 타이밍 전략: 축 카운트가 1~2장인 초반보단 5~7장 집중된 중·후반에 픽할수록 강함. "빌드가 만들어진 만큼 돌아오는" 자기 피드백형.
  - 중첩 픽 가능: 2픽 → 타이머 독립 적용 (10s + 10s 순차 or 첫 10s에 ×1.4×1.4=×1.96 중첩 — 밸런스 검토 필요, 기획서 확정 시 결정).
- **구현 패턴**: `TacticalSurgeEffect.Apply()` → `IBattleContext.GetDominantAxis()` (빌드 카운트 배열에서 ArgMax, 동점 = 마지막 픽 축) → `MonsterBuffService.ApplyMultiStatBuff(axis, hpMul: 1.4f, powerMul: 1.4f, speedMul: 1.2f, duration: 10f)`. `GetDominantAxis()`는 기존 BattleViewModel 의 빌드 카운트(축별 픽 수) 구조 재사용.
- **시너지 후크**: 모든 "축 집중 빌드" 카드들과 자연 시너지 — Tank 7장 Tier3 + TacticalSurge = 캡 +6 + Wisp·Wraith 임시 ×1.4 버프 동시 활용; Swarm 5장 Tier2 (스포너 주기 ×0.85 영구) + TacticalSurge = 빠른 스폰으로 가득 찬 필드에 Phantom·Wisp 전체 이속·체력 급등.
- **구현 비용 추정**: 2 (GetDominantAxis = 빌드 카운트 max — 단순 로직; ApplyMultiStatBuff = 기존 GuardianRageEffect·IronWillEffect 패턴 조합)
- **중복 재검증**: 기존 28장에 "현재 빌드 상태를 읽어 동적으로 강화하는" 카드 없음. 과거 18회차: 05-28(battle-state-scaling)은 영웅 HP 잔여량 기반 스케일링, 06-06(density-tide)은 밀도 압박 — 모두 "현재 픽 빌드 카운트를 읽어 축 몬스터를 즉발 강화"하는 개념이 아님. 최초 제안. ✓

---

## 3. 저주 공명 (CurseResonance) — 가칭

- **카테고리**: 액티브 와일드
- **효과 모델**: 픽 즉시 영웅에게 현재 활성 중인 모든 시한부 디버프 효과의 **남은 지속시간 +8초 연장**. 활성 디버프가 0개(아무 저주 없음)이면 폴백: 영웅 이동속도 ×0.6, 5초.
  - 수치 근거: Fear(3s 도주) → 11초. Bleed(이동 시 HP -2%, 10s) → 18초. Weaken(ATK ×0.5, 10s) → 18초. Slow(이속 ×0.5, 10s) → 18초. 단일 Bleed 18초 — 영웅 평균 이속 가정, 10s 구간 대비 이동 감속 없이 추가 이동 시 HP -2% × 8회 추가 = 약 80 HP 추가 피해. 3개 동시 연장 시 18초 공포+출혈+약화 = 사실상 영웅이 반격 불가 구간 대폭 연장.
  - 폴백(디버프 없을 때) 수치 근거: 이속 ×0.6, 5초 — Slow(×0.5, 10s)보다 약한 버전으로 낭비 방지.
  - 중첩 픽 가능: 2픽 시 남은 시간 +8 → +16s. Weaken 10s 상태에서 2픽 CurseResonance = 26s Weaken.
- **구현 패턴**: `CurseResonanceEffect.Apply()` → `IHeroAura.GetActiveDebuffs()` (현재 시한부 효과 컬렉션) → foreach `debuff.ExtendDuration(8f)`. 폴백은 기존 `SlowEffect` 유사 패턴으로 `HeroAura.ApplySpeedMultiplier(0.6f, 5f)`. `IHeroAura.GetActiveDebuffs()`가 없으면 `HeroAura` 내부 리스트를 순회하는 메서드 1개 추가로 해결.
- **시너지 후크**: Debuff 축 저주 카드 전체와 직접 시너지 — Fear + Bleed + Weaken 모두 건 뒤 CurseResonance 픽 = 세 저주 동시 연장으로 저주 지옥 유지; ExhaustionCurse(06-15 제안, 공격할수록 ATK 감소, 12s) + CurseResonance = 20s 고갈 저주로 영웅 ATK 를 오래 바닥으로 유지; Debuff Tier3(영구 출혈, 이동 시 1s당 HP -1%) + Bleed(임시, -2%) 동시 활성 중 CurseResonance → Tier3 출혈은 영구이므로 패스, Bleed 임시만 연장 (구현 시 영구 효과 제외 로직 확인 필요).
- **구현 비용 추정**: 3 (IHeroAura 에 GetActiveDebuffs + ExtendDuration API 추가 가능성. 단 debuff 목록을 이미 관리하고 있다면 2로 감소)
- **중복 재검증**: 기존 28장에 "활성 효과 연장" 카드 없음. 과거 18회차: 05-31(active-permanent-brand)은 "액티브로 영구 표식 부여" 개념이나, 영구화 ≠ 연장 (다른 방향). 06-05(time-surge)는 시간 압박 주제이나 타이머·트리거 관련, 효과 연장 아님. 06-15(ExhaustionCurse) 는 "공격할수록 ATK 감소" — CurseResonance 와 기계적으로 다름. ✓

---

## 4. 공통 테마 고찰

세 카드는 **"와일드 카드 카테고리 최초 신설"** 이라는 하나의 구조 공백을 채운다.

컨셉서 §4.2 는 "액티브 이벤트 풀: 저주 1 + 버프 1 + **와일드 1**"로 규정했지만, 현행 28장에는 와일드 카테고리가 **0장**이다. 즉 실제 액티브 선택 화면에서 세 번째 슬롯이 늘 비어 있거나 임시 처리로 채워진다. 오늘 제안은 이 세 번째 슬롯을 처음으로 채우는 시도다.

| 카드 | 효과 방향 | 기존 카테고리 대비 차별점 |
|---|---|---|
| DoubleDraft | 선택 메타 조작 (다음 화면 5장) | 저주도 버프도 아닌 "빌드 찾기 보조" |
| TacticalSurge | 빌드 상태 피드백 강화 | 축 방향을 스스로 읽어 해당 몬스터 강화 — 조건부 버프 |
| CurseResonance | 기존 저주 시너지 증폭 | 저주 자체를 주는 게 아니라 "걸린 저주를 연장" — 메타 저주 |

**왜 오늘 이 테마를 골랐는가:**
- QA 리포트가 BLOCKED 상태라 픽률 데이터 대신 **구조 분석**을 근거로 삼음.
- 18회차 내내 와일드 카드를 한 번도 제안하지 않았음이 역으로 "가장 미탐색된 영역"임을 시사.
- 와일드 슬롯이 0장이면 결국 플레이어는 "저주 vs 버프" 두 방향만 고르는 구조 — 세 번째 선택지가 생기면 선택 깊이 자체가 달라짐.
- DoubleDraft (구현 비용 2) / TacticalSurge (2) / CurseResonance (3) — 세 카드 모두 기존 IBattleContext / IHeroAura / MonsterBuffService 패턴 안에서 처리 가능.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- **채택 우선순위**: DoubleDraft ≥ CurseResonance > TacticalSurge
  - DoubleDraft: 구현 비용 2, 효과 명쾌, "빌드 보정 도구"로 플레이어 스트레스 완화에 직결.
  - CurseResonance: Debuff 빌드를 완성하는 핵심 시너지 잠금 해제 카드. 기존 저주 6장(Fear·Bleed·Weaken·Slow·PlagueSlowBoost·HeroPoisonAura)이 모두 강화됨.
  - TacticalSurge: 빌드 완성도 높을 때 강력하지만 "동점 시 처리" 등 엣지케이스 게임 디자인 고려 필요.
- `IHeroAura.GetActiveDebuffs()` / `ExtendDuration()` API 존재 여부를 gameplay-programmer 와 사전 확인 권장 (CurseResonance 구현 비용 2↔3 분기점).
- v0.2 풀 확장 진입 전까지 backlog 보관 — 풀 확장 시 와일드 카테고리 3장 일괄 추가로 자연스럽게 편입.

---

## 6. 쉬운 설명 (비개발자 요약)

던전 주인은 30초마다 카드를 한 장 골라야 하는데, 지금까지 모든 카드는 영웅을 괴롭히는 "저주" 아니면 몬스터를 강하게 만드는 "버프" 둘 중 하나였습니다. 그런데 게임 설계에는 원래 세 번째 칸, 이 두 가지 중 어느 쪽도 아닌 "특별한 카드"(와일드) 슬롯이 있었는데, 지금까지 그 슬롯이 빈 채로 남아 있었습니다. 오늘 제안하는 세 장은 그 빈 슬롯을 처음으로 채우는 카드들입니다. 하나는 다음 선택지를 더 많이 보여줘서 원하는 카드를 찾기 쉽게 해 주고, 하나는 내가 지금까지 어떤 방향으로 키워왔는지를 읽어서 그 방향 몬스터들을 10초간 폭발적으로 강화해 주고, 하나는 이미 영웅에게 걸린 저주를 8초씩 연장해 저주 효과가 끊기지 않게 이어줍니다. 그래서 오늘 제안하는 카드 3장은: 게임의 세 번째 선택지 슬롯을 채워 던전 주인에게 완전히 새로운 전략 방향을 여는 카드들입니다.
