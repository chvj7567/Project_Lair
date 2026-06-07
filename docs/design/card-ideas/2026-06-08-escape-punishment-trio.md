# Card Ideas — 2026-06-08 — 부상당한 영웅, 이동도 카드도 발목을 잡는다

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 도주 처벌 (Escape Punishment) — 영웅이 HP를 잃을수록 더 느려지고, 패시브 카드를 받을 때마다 이동 페널티가 붙으며, 액티브 한 번으로 이동을 완전히 봉쇄하는 3종 세트. "부상당한 먹잇감"은 달리지 못한다.
- **목록**: 도주 추적 (Wounded Pursuit) / 절망의 무게 (Weight of Despair) / 탈출 봉쇄 (Siege Seal)
- **기존 28장 + 과거 11회차와의 중복 회피 확인됨**

  **기존 28장 관련 점검**:
  - Slow (기존): 영웅 이동속도 ×0.5, 10s **일시적 적용** — 이 카드들은 **영구·조건부·트리거 기반**으로 개념 구분됨
  - TimeStop (기존): 영웅+몬스터 5s 완전 정지 — 탈출 봉쇄는 **영웅 이동만 8s 금지, 영웅 공격 허용, 몬스터 정상 이동**으로 메커니즘이 다름
  - Bleed (기존): 이동 시 HP 손실 — 이 카드들은 이동속도 자체를 조작하며 HP 손실 없음

  **과거 11회차 점검**:
  - 5/28 전장 상태 감지: HP/시간 스냅샷 → **몬스터** 스탯 스케일링. 오늘은 **영웅** 이동속도를 동적으로 감소.
  - 5/29 종 간 연계: 몬스터 공존 조건 시너지 — 무관.
  - 5/30 독 생태계: Plague OnDeath 독 연쇄 — 무관.
  - 5/31 영구 낙인: 픽마다 쌓이는 영구 스택 브랜드 — 오늘 카드들은 스택이 아닌 조건부 오버라이드 방식.
  - 6/01 리퍼·헥스 심화: Reaper Power·Hex 공속 — 무관.
  - 6/02 죽음의 메아리: 몬스터 OnDeath 트리거(소환·역류 피해·종 교체) — 오늘은 OnDeath 없음. 절망의 무게는 카드 픽 트리거, 도주 추적은 HP 연속값 구독.
  - 6/03 위스프 포위: Wisp 공간 배치 — 무관.
  - 6/04 DPS×Debuff 교차: 둔화 영웅에게 딜러 추가 피해 — 오늘은 딜러 버프가 아닌 영웅 이동 자체를 봉쇄.
  - 6/05 타이머 연동: BattleClock 30초·잔여시간 기반 효과 — 오늘은 타이머 연동 없음.
  - 6/06 군단 밀도: 동시 생존 몬스터 수 임계 — 무관.
  - 6/07 레이스·팬텀 각성: Wraith Power/MoveSpeed, Phantom OnHit — 무관.

---

## 1. 도주 추적 (Wounded Pursuit) — 가칭

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - 영웅 이동속도 배율을 영구적으로 `0.5 + 0.5 × (currentHp / maxHp)` 로 설정한다.
  - HP 100%: ×1.0 (영향 없음, 픽 직후 무해)
  - HP 70%: ×0.85 (15% 감소)
  - HP 50%: ×0.75 (25% 감소)
  - HP 20%: ×0.6 (40% 감소)
  - HP 10%: ×0.55 (45% 감소, 상한 감소)
  - 효과는 HP 회복 시 자동 완화, HP 추가 손실 시 자동 강화 — 항상 현재 HP에 동기화.
  - **중첩 픽**: 2픽 시 공식 `0.3 + 0.7 × normalized` 로 하한이 0.3까지 내려감 (이동속도 최대 70% 감소).
  - **밸런스 근거 (컨셉 §8)**: 영웅 2~4분 사망 목표. HP 50% (약 1~2분 경과 예상) 시점에 ×0.75는 Plague 둔화(PlagueSlowBoost: ×0.75)와 동급이지만 이쪽은 영구·동적. HP 하락 시 자동 강화되므로 후반부에야 치명적이 됨.
- **구현 패턴**:
  - `WoundedPursuitEffect.cs` — `ICardEffect` 구현. `Apply(IBattleContext ctx)` 에서 `IHeroAura` 를 등록.
  - HeroPoisonAura·HeroAttackDown 이 이미 IHeroAura 패턴으로 영웅 상태를 구독 — 동일 구조.
  - 신규 포인트: `IHeroAura` 내부에서 `IBattleContext.Hero.OnHpChanged` 를 구독하여 매 HP 변화마다 `hero.MoveSpeedMultiplier` 를 재계산·재적용. 기존 IHeroAura 들은 Apply-once였으나 이 카드는 동적 업데이트가 첫 사례 — `OnDispose` 시 구독 해제 필수.
  ```csharp
  public class WoundedPursuitAura : IHeroAura, IDisposable
  {
      private IHeroStatus _hero;
      public void Attach(IHeroStatus hero)
      {
          _hero = hero;
          hero.OnHpNormalizedChanged += OnHpChanged;
          OnHpChanged(hero.HpNormalized);
      }
      private void OnHpChanged(float normalized)
          => _hero.SetMoveSpeedMultiplier("WoundedPursuit", 0.5f + 0.5f * normalized);
      public void Dispose() => _hero.OnHpNormalizedChanged -= OnHpChanged;
  }
  ```
- **시너지 후크**:
  - **Bleed** (이동 시 HP 소모): 이동 → HP 감소 → 이 카드 강화 → 더 느려짐 → 몬스터 접근 용이 → 악순환 루프 완성.
  - **탈출 봉쇄 (Card 3)**: Wounded Pursuit로 이미 느린 영웅에게 Siege Seal을 써서 이동 0으로 만들면 봉쇄 8s 동안 영웅 주변에 더 많은 몬스터가 도달.
  - **SpawnWraith (패시브)**: Wraith는 "매우 느림"이 약점 — 봉쇄+이 카드로 영웅도 느려지면 Wraith가 반드시 접근 가능해짐.
- **구현 비용 추정**: 2 (IHeroAura 패턴 기존 존재, 동적 HP 구독 업데이트는 신규지만 추가 로직 약 20줄)
- **중복 재검증**:
  - 기존 Slow: 일시적 ×0.5 — 이 카드는 영구·HP 연동·가변. 메커니즘 다름.
  - 5/28 전장 스케일링: 몬스터 버프 스케일 — 대상이 영웅 이동속도이며 공식도 완전히 다름. ✅

---

## 2. 절망의 무게 (Weight of Despair) — 가칭

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - 영웅이 **패시브 선택지를 고를 때마다** (HP 10% 단위 패시브 이벤트 해소 시) 15s간 영웅 이동속도 ×0.85 디버프 적용. 타이머 갱신 방식 (중첩 없이 15s 리셋).
  - 즉, 런 내내 패시브를 고를 때마다 15s씩 느려진다. HP가 많이 깎여 패시브를 자주 받을수록 총 느려지는 구간이 늘어난다.
  - 최대 9번 패시브 발생 → 이 카드 효과가 최대 9회 발동 → 15s 윈도우가 겹치면 사실상 런 중후반 내내 지속.
  - **중첩 픽**: 2픽 시 ×0.72 (두 중첩 곱), 20s로 연장.
  - **밸런스 근거 (컨셉 §8)**: 첫 패시브(HP 90%)에서는 영향이 미미 (15s만 느림). HP 50% 이하 (4번째 패시브~)부터 빈도가 높아져 "선택 압박과 이동 제약이 동시에" 발생. 패시브를 덜 받는 빠른 사망 시도엔 이 카드가 약하게 작동.
- **구현 패턴**:
  - `WeightOfDespairEffect.cs` — `ICardEffect.Apply` 에서 `IBattleContext.OnPassiveCardPicked` 이벤트 구독.
  - 이벤트 발생 시 `IHeroAura` 에 15s 슬로우 적용 (HeroPoisonAura·SlowEffect의 타이머 기반 패턴 참조).
  - 구현 전제: `IBattleContext` 에 `event Action OnPassiveCardPicked` 훅 노출 필요 — `BattleController.TryProcessNext` 의 패시브 픽 완료 시점에 발화. 기존 `OnCardPicked` 이벤트 유사 훅이 없다면 IBattleContext에 신규 추가 (~5줄).
  ```
  // BattleController.TryProcessNext 패시브 픽 완료 후:
  if (entry.SourceType == TriggerQueue.Source.Passive)
      ctx.RaiseOnPassiveCardPicked();
  ```
- **시너지 후크**:
  - **도주 추적 (Card 1)**: HP가 낮을수록 도주 추적이 강해지고, 그 낮은 HP 구간에서 패시브가 더 자주 발생해 이 카드도 더 자주 발동 → 두 카드가 시너지로 폭발.
  - **Bleed**: 이동 시 HP 소모 → 더 자주 패시브 트리거 → 이 카드 더 자주 발동.
  - **SpawnPlagues**: 많은 Plague → 더 강한 둔화 공격 → HP 더 빠르게 감소 → 패시브 주기 단축 → 이 카드 빈도 상승.
- **구현 비용 추정**: 2 (OnPassiveCardPicked 이벤트 추가 5줄 + 타이머 슬로우 적용 — 타이머 패턴 기존 존재)
- **중복 재검증**:
  - 5/31 낙인: 픽마다 쌓이는 영구 스택 — 이 카드는 영구 스택이 아닌 15s 타이머 갱신. 카드 픽 트리거라는 점이 유사해 보이나, 낙인은 "매 픽마다 강해지는 영구 스택"이고 이 카드는 "픽할 때마다 15s 한시 이동 페널티." 효과 대상(영웅 이동속도) · 지속 방식(시한부 타이머)이 모두 다름. ✅

---

## 3. 탈출 봉쇄 (Siege Seal) — 가칭

- **카테고리**: 액티브 저주 (Debuff 축)
- **효과 모델**:
  - 사용 즉시: **8s간 영웅 이동속도 0** (이동 불가). 영웅의 공격은 계속 가능. 몬스터는 정상 이동·공격.
  - TimeStop (기존, ECardId.TimeStop): 영웅 5s **완전 정지** (이동 0 + 공격 0 + 몬스터도 정지). 이 카드와 핵심 차이:
    | 항목 | TimeStop | 탈출 봉쇄 |
    |---|---|---|
    | 지속 | 5s | 8s |
    | 영웅 이동 | 0 | 0 |
    | 영웅 공격 | 0 (정지) | 정상 가능 |
    | 몬스터 이동·공격 | 0 (정지) | 정상 |
    | 실질 효과 | 양측 동결 | 영웅만 움직임 봉쇄 |
  - **게임플레이**: 몬스터들이 8s 동안 정상 이동해 영웅에게 수렴. 영웅은 공격할 수 있어 몬스터를 처치하지만 도망치지는 못함. 8s 종료 시 영웅은 이미 몬스터에게 포위된 상태.
  - 밸런스: Wraith DPS 20 × 1마리 + Reaper DPS 40 × 1마리 + Phantom DPS 5 × 3마리 = 75 DPS × 8s = 600 데미지 = 영웅 HP 60% (기본 몬스터 구성 기준). 영웅이 8s 동안 역으로 처치하는 몬스터 수 = 공격속도 1s/타, 공격력 50 → HP 200 Wisp 처치 4초 → 8s간 약 2마리 처치. 총 수지는 몬스터에게 유리.
  - **중첩 픽**: 2픽 시 지속 10s.
- **구현 패턴**:
  - `SiegeSealEffect.cs` — TimeStopEffect 구조 참조. 차이점:
    - TimeStopEffect: `Time.timeScale = 0` (또는 영웅+몬스터 모두 정지 API) → 전체 정지
    - SiegeSealEffect: `hero.SetMoveSpeedOverride(0f, 8f)` 만 호출. 몬스터 정지 없음. 영웅 공격 차단 없음.
  - 영웅 이동속도 오버라이드 API (`SetMoveSpeedOverride`)가 IBattleContext/HeroController에 있다면 구현 즉각 가능. 없으면 IHeroAura의 MoveSpeed 배율을 0으로 설정하는 방식.
  ```csharp
  public class SiegeSealEffect : ICardEffect
  {
      public void Apply(IBattleContext ctx)
      {
          ctx.Hero.ApplyMoveSpeedAura(multiplier: 0f, duration: 8f, id: "SiegeSeal");
      }
  }
  ```
- **시너지 후크**:
  - **도주 추적 (Card 1)** + **절망의 무게 (Card 2)**: 3장 동시 픽 시 영웅이 이미 느린 상태 → Siege Seal로 이동 0 → 8s 포위 → 봉쇄 종료 후에도 Card 1·2 효과 지속. "포위→느려짐→재포위" 루프.
  - **SpawnWraith**: Wraith의 치명적 약점은 "매우 느린 이동속도" — Siege Seal 8s 동안 Wraith도 영웅에게 확실히 도달 가능. Wraith 빌드의 접근 문제를 이 카드가 해결.
  - **Multiply (Phantom 번식)**: Phantom 스폰 주기 단축 → Siege Seal 8s 동안 영웅 위치에 더 많은 Phantom 수렴.
  - **GuardianRage (액티브)**: Siege Seal로 영웅 위치 고정 → GuardianRage로 Wisp·Wraith HP×2+방어 → 고정된 영웅을 강해진 탱커로 압박.
- **구현 비용 추정**: 2 (TimeStopEffect 참조 + 영웅 이동 오버라이드 API 있으면 1, 신규 API 추가 필요 시 3)
- **중복 재검증**:
  - TimeStop (기존): 전체 정지 vs 영웅 이동만 봉쇄. 영웅 공격 허용·몬스터 정상·8s 유지 — 모두 다름. ✅
  - 5/09~6/07 과거 11회차: 영웅 이동 봉쇄 전용 카드 없음. ✅

---

## 4. 공통 테마 고찰

**오늘 3장의 공통 철학**: 영웅이 "어디로든 달아날 수 있다"는 전제를 무너뜨린다.

현행 MVP의 Debuff 축은 출혈(Bleed: 이동 시 HP 손실), 공포(Fear: 강제 도주), 둔화(Slow: 속도 감소), 독(HeroPoisonAura) 등 영웅을 갉아내는 방식이다. 하지만 이 디버프들은 모두 "영웅이 능동적으로 이동하면서 저항할 수 있다"는 전제 위에 있다. 영웅이 공격 범위 밖으로 도망치거나, 포위를 깨고 나오거나, Fear로 강제 도주해 시간을 버는 전략이 여전히 존재한다.

오늘 3장은 이 전제를 공격한다:
- **도주 추적**: 부상당할수록 느려짐 → 탈출 성공률 하락
- **절망의 무게**: 선택지를 받을 때마다 잠시 느려짐 → 전략적 결정의 순간이 영웅에게도 부담
- **탈출 봉쇄**: 8s 완전 이동 봉쇄 → 포위 상태를 강제 완성

**왜 오늘 이 테마인가**: QA 데이터가 Blocked 상태(시뮬레이터 미구축)라 픽률 수치가 없음. 대신 설계 레벨 분석: Debuff 축 현행 카드(7장)는 HP 소모·공격력 감소·도주·약화에 집중되어 있고 **이동속도 영구 조작** 및 **조건부 이동 봉쇄**는 공백이다. Slow(액티브)가 유일한 이동속도 카드이지만 일시적·단순 배율 적용이며 v0.2에서 Debuff 축을 30~40장 규모로 확장할 때 "이동 통제" 서브 카테고리가 필요하다.

---

## 5. 채택 흐름 제안

- 채택 시 **game-designer** 호출, 이 문서를 입력으로 전달
- `WoundedPursuitEffect.cs` · `WeightOfDespairEffect.cs` · `SiegeSealEffect.cs` 는 Debuff 축 서브 카테고리 "이동 통제 (Movement Control)"로 분류
- IBattleContext에 `OnPassiveCardPicked` 이벤트 추가 (Card 2 구현 전제) — gameplay-programmer 협의 필요
- IHeroAura의 동적 HP 구독 패턴(Card 1) 최초 사례 — 패턴 문서화 권장
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 영웅은 다쳐도 발이 느려지지 않아요. 아무리 많이 맞아도 빠르게 뛰어다니며 몬스터를 피할 수 있어서, 플레이어 입장에서는 "이거 진짜 못 잡겠는데?"라는 느낌을 받을 수 있어요. 그 답답함을 해소하는 카드들이 오늘의 제안입니다.

첫 번째 카드는 "다칠수록 느려지는 발"이에요. 영웅이 체력을 잃으면 잃을수록 이동속도가 점점 떨어져서, 이미 많이 다친 상태에서는 느릿느릿 걷게 됩니다. 두 번째 카드는 "카드를 받을 때마다 순간적으로 발이 묶이는" 효과예요. 영웅이 새 능력을 선택하는 바로 그 순간에 잠깐 느려지니까, 선택의 순간이 곧 공격의 기회가 됩니다. 세 번째 카드는 "8초 동안 영웅을 그 자리에 묶는" 강력한 기술이에요. 영웅은 공격은 할 수 있지만 이동이 막히고, 그 사이 몬스터들이 사방에서 달려들게 됩니다.

그래서 오늘 제안하는 카드 3장은: 영웅이 부상당하고 선택지를 받고 버티면 버틸수록 탈출 가능성이 줄어드는 "포위 완성 3종 세트"입니다.
