# Card Ideas — 2026-07-04 — Phantom 시야 압박: 팬텀의 어둠을 무기로

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: Phantom 의 "시야 차단" 특성을 카드 효과의 조건·증폭으로 활용 — 팬텀이 존재하거나 영웅이 어두울 때 다른 축까지 덩달아 강해지는 크로스 시너지
- **목록**: ShadowVeil (패시브·Swarm) / DarkPursuit (패시브·Dps) / PanicCloud (액티브·Debuff)
- **기존 25장 + git log 과거 35회차와의 중복 회피 확인됨**: 팬텀의 시야 차단 "특성 자체"를 카드 조건으로 쓰는 방식은 이번이 최초. PhantomMoveSpeedBoost·SpawnPhantoms 는 팬텀 수/속도를 올리지만 시야 차단 상태를 *트리거*로 쓰지는 않음. 2026-06-23 presence-aura-leader-trio 는 팬텀 필드 생존 조건부였으나 시야 차단 연계 없음. 개념적 중복 없음.

---

## 1. ShadowVeil (쉐도우 베일) — 패시브 / Swarm 축

- **카테고리**: 패시브 강화 (팬텀 특성 심화)
- **효과 모델**:
  - Phantom 종의 시야 차단 반경 ×1.5 (영구 글로벌 — 이후 스폰 팬텀 전체 소급)
  - 영웅이 시야 차단 상태(필드에 팬텀이 1마리 이상 영웅 근방에 있어 차단 중)인 동안, 필드 내 모든 팬텀 이동속도 +20% (조건부 글로벌, 매 프레임 조건 재평가)
- **구현 패턴**:
  - `MonsterBuffService.ApplyGlobalMultiplier(EMonster.Phantom, StatType.ViewBlockRadius, 1.5f)` — ViewBlockRadius 는 Phantom 컴포넌트 확장 필요 (신규 StatType, 구현 비용 +1)
  - `IBattleContext.IsHeroBlindfolded` bool 을 Update 에서 체크 → true 시 `MonsterBuffService.SetConditionalSpeed(EMonster.Phantom, 1.2f)` 호출, 조건 해제 시 제거
- **시너지 후크**:
  - PhantomMoveSpeedBoost (팬텀 기본 이속 ×1.5) 과 중첩 → ShadowVeil 조건부 +20% 까지 합산, 팬텀이 영웅을 추격하며 시야 차단 지속 시간 극대화
  - SpawnPhantoms (팬텀 스포너 동시 출력 +1) 과 조합 → 필드에 팬텀 수 ↑ → 시야 차단 확률·면적 동시 증가
  - Swarm Tier2 (전 스포너 주기 ×0.85) 와 조합하면 팬텀이 더 자주 스폰되어 시야 차단 상태가 끊기지 않음
- **구현 비용 추정**: 3 (ViewBlockRadius StatType 신설 필요, 나머지는 기존 패턴)
- **중복 재검증**: PhantomMoveSpeedBoost 는 이속만 올리고 시야 차단 반경·조건부 가속 없음. 완전 차별화.

---

## 2. DarkPursuit (다크 퍼슈트) — 패시브 / Dps 축

- **카테고리**: 패시브 강화 (크로스 축 조건부 — Swarm 시야 차단 → Dps 공속)
- **효과 모델**:
  - 영웅이 시야 차단 상태인 동안, Reaper·Hex 종 공격 쿨다운 ×0.85 (공속 +약 18%) (조건부 글로벌, 매 프레임 재평가)
  - 시야 차단 상태 해제 시 즉시 원래 쿨다운으로 복귀 (한시적 버프, 영구 아님)
- **구현 패턴**:
  - `IBattleContext.IsHeroBlindfolded` → true 시 `MonsterBuffService.SetConditionalCooldown(EMonster.Reaper, 0.85f)` + `MonsterBuffService.SetConditionalCooldown(EMonster.Hex, 0.85f)`
  - false 시 조건부 배율 제거 — MonsterBuffService 의 조건부 버프 레이어 분리 (기존 영구 배율과 스택 독립)
- **시너지 후크**:
  - **ShadowVeil 과 핵심 콤보**: ShadowVeil 이 팬텀 시야 차단 반경을 넓히고 차단 지속 시간이 늘어날수록, DarkPursuit 의 Dps 공속 상승 구간도 늘어남. Swarm·Dps 두 축을 묶는 브리지 카드.
  - ReaperAtkSpeed (Reaper 쿨다운 ×0.7, 영구) 와 중첩 → 시야 차단 중 ×0.7 × ×0.85 ≈ ×0.595 (공속 +68%), 순간 화력 폭발
  - HexRangeBoost 와 조합 → 원거리 Hex 가 시야 차단 중 더 빠르게 사격, 영웅이 안 보이는 상태에서 원거리 포화
  - Dps Tier1 (Reaper·Hex Power ×1.3) 과 시너지 → 시야 차단 구간에 공속+데미지 동시 상승
- **구현 비용 추정**: 3 (MonsterBuffService 의 조건부 쿨다운 레이어 분리 필요, 조건 bool 은 ShadowVeil 과 공유)
- **중복 재검증**: ReaperAtkSpeed 는 영구 쿨다운 감소이고 조건부가 아님. Frenzy (액티브, 전체 공속 10s) 는 시야 차단 조건 없는 즉발 버프. 완전 차별화.

---

## 3. PanicCloud (패닉 클라우드) — 액티브 / Debuff 축

- **카테고리**: 액티브 저주 (영웅 시야 차단 + 이동 방해)
- **효과 모델**:
  - 20초 동안 영웅의 이동속도 ×0.8 (둔화)
  - 동시에 영웅 위치 기준 반경 3.0 유닛 안을 완전히 시야 차단 (안개 장판 이펙트 — 팬텀 없이도 `IBattleContext.IsHeroBlindfolded = true` 강제)
  - 영웅이 이동해도 안개 장판이 영웅 위치를 따라가지 않음 (설치형) → 영웅이 안개에서 도망치면 시야 차단 해제되지만 이동속도 둔화는 유지
- **구현 패턴**:
  - `IHeroAura` 패턴: 영웅 위치에 시야 차단 오브젝트(CHMPool.Pop) 설치, `ViewBlockZone` 컴포넌트 → `IBattleContext.IsHeroBlindfolded = true` 등록
  - 20초 후 CHMPool.Push 로 회수, `IsHeroBlindfolded` 소스 제거
  - 이동속도 둔화: `MonsterBuffService` 아닌 영웅 측 `HeroStatusService.ApplyDebuff(DebuffType.SlowFactor, 0.8f, 20f)` — BleedEffect 와 동일 패턴
- **시너지 후크**:
  - **ShadowVeil + DarkPursuit 삼각 콤보**: PanicCloud 가 강제로 시야 차단 → ShadowVeil 의 팬텀 가속 + DarkPursuit 의 Dps 공속 부스트 동시 발동. 팬텀이 없어도 이 두 패시브를 트리거 가능.
  - PlagueSlowBoost (Plague 둔화 ×0.75) 와 중첩 → PanicCloud 의 이속 ×0.8 + Plague 공격 시 추가 둔화, 영웅 거의 정지 수준
  - Bleed (출혈 — 이동 시 HP -2%/s) 와 조합 → 영웅이 안개를 피해 이동하려 할수록 출혈 데미지 누적
- **구현 비용 추정**: 2 (팬텀 시야 차단 이펙트 시스템 재사용, HeroStatusService 둔화는 기존 패턴)
- **중복 재검증**: Fear (3s 도주) 는 영웅 제어지만 시야 차단 없음. Slow (액티브 — 영웅 이속 ×0.5 + 몬스터 이속 ×1.3) 는 시야 차단 없음. PanicCloud 는 시야 차단 + 설치형 안개 + 이속 감소 조합으로 완전 차별화.

---

## 4. 공통 테마 고찰

세 카드의 핵심은 **"Phantom 의 시야 차단 = 필드 상태 조건"** 이다. 현재 기존 25장은 종(種) 수치 강화, 스포너 조작, 영웅 HP/공격 조작을 다루지만, "현재 영웅이 앞이 안 보이는가?" 라는 필드 상태를 조건으로 쓰는 카드가 전혀 없다.

**왜 오늘 이 테마인가?**
1. **QA 리포트 부재 → 직관적 공백 분석**: QA 리포트가 BLOCKED 상태라 픽률 데이터 없음. 그러나 Phantom 전용 패시브가 2장(PhantomMoveSpeedBoost·SpawnPhantoms)뿐인 데 비해 다른 종은 3~5장씩 보유. Phantom 의 정체성(시야 차단)을 활용하는 카드가 전무한 것이 가장 큰 설계 공백.
2. **크로스 축 시너지 공백**: 현재 4축은 대부분 자기 축 내부에서 시너지가 닫힌다. Swarm↔Dps 크로스 연결이 약하고, DarkPursuit 이 이 브리지 역할을 하면 두 축을 동시 채택하는 빌드 동기가 생긴다.
3. **PanicCloud 의 시스템 보완**: ShadowVeil·DarkPursuit 는 팬텀이 있을 때 조건 발동이지만, PanicCloud 는 팬텀 없이도 시야 차단 조건을 강제로 만들어주어 "Phantom 없는 빌드도 두 패시브를 활용 가능"하게 함 — 빌드 유연성 향상.

---

## 5. 채택 흐름 제안

- **채택 시**: 이 문서를 game-designer 호출 입력으로 전달 → 수치 조정(시야 차단 반경 구체값, 조건 판정 주기, DarkPursuit 공속 배율) 및 ViewBlockRadius 신규 스탯 정의 포함 기획서 작성
- **필수 선행 결정**: `IBattleContext.IsHeroBlindfolded` 의 판정 기준 명확화 (팬텀 몇 마리? 거리 얼마?) — ShadowVeil·DarkPursuit·PanicCloud 세 카드 공통 조건이므로 한 번에 정의 필요
- **v0.2 진입 전까지 backlog 보관** (현재 v0.3 단계, MVP §11 28장 lock 유지)

---

## 6. 쉬운 설명 (비개발자 요약)

현재 던전에는 팬텀이라는 작고 까만 몬스터가 있는데, 이 몬스터는 영웅 주변을 가득 채워서 영웅의 시야를 흐리게 만드는 특기가 있습니다. 지금까지는 팬텀이 그냥 빠르게 뛰거나 숫자가 늘어나는 방식으로 강화됐는데, 팬텀이 "영웅의 눈을 가리는 바로 그 순간"을 트리거로 활용하는 카드가 한 장도 없었습니다. 예를 들어 영웅이 앞을 못 볼 때 리퍼와 헥스가 더 빠르게 공격한다거나, 팬텀이 더 넓은 어둠을 만들면서 동시에 더 빨리 달려온다거나 하는 연계를 지금은 아무것도 건드리지 않고 있습니다. 또한 PanicCloud 카드는 팬텀이 없어도 인위적으로 영웅 주변에 안개를 깔아서 이 연계를 억지로 발동시킬 수 있어, 팬텀이 적은 빌드도 이 시스템을 활용할 수 있습니다. 그래서 오늘 제안하는 카드 3장은: **팬텀의 '어둠 만들기'를 단순한 방해 수단이 아닌 던전 전체의 연쇄 반응 조건으로 바꾸는 세 장**입니다.
