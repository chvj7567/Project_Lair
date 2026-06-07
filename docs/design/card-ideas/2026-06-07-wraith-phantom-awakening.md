# Card Ideas — 2026-06-07 — 레이스와 팬텀의 각성: 빠진 스탯을 채우는 Tank·Swarm 심화 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 레이스(Tank)·팬텀(Swarm)의 빠진 스탯 채우기 — 레이스에는 Power·MoveSpeed 배율 카드가 전무하고, 팬텀에는 OnHit 효과가 없다. 오늘 3장이 이 공백을 정밀하게 채운다.
- **목록**: 레이스 공세 (Wraith Assault) / 레이스 가속 (Wraith Rush) / 팬텀 독침 (Phantom Venom)
- **기존 28장 + git log 과거 10회차와의 중복 회피 확인됨**
  - 기존 28장:
    - WraithDamageBoost = v0.6 에서 **HP** ×1.5 로 리뉴얼 (효과명과 달리 데미지→HP 변환 확정). Wraith Power 배율 카드는 전무.
    - PhantomMoveSpeedBoost = Phantom 이동속도만. Phantom Power / OnHit 효과 카드 없음.
    - Wraith MoveSpeed 카드: 없음 (컨셉 §11.3 "매우 느림" 상태 그대로).
  - 과거 10회차 전부:
    - 5/28 전장 상태 감지: 스냅샷 스케일링 / 5/29 종 간 연계: 공존 조건 ON/OFF
    - 5/30 Plague-독 생태계: Plague OnDied + 독 증폭 / 5/31 낙인 트리오: 픽마다 쌓이는 영구 스택
    - 6/01 리퍼·헥스 딜러 심화: Reaper Power + Hex 공속 + 혼합 즉시 소환
    - 6/02 죽음의 메아리: OnDeath 트리거 (PhantomBirth / SoulCurse / WraithRemnant)
    - 6/03 위스프 벽 포위: 위스프 공간 배치 / 6/04 DPS×Debuff 교차 사냥: 축 교차 시너지
    - 6/05 타이머 연동 압박: BattleClock 연동 / 6/06 군단 밀도 압박: 총 마릿수 실시간 임계
  - 오늘 3장: Wraith Power·MoveSpeed 배율 + Phantom OnHit 독침 — 단순 스탯 배율이지만 실존하는 공백이고 어느 과거 회차와도 메커니즘이 다름 ✅

---

## 1. 레이스 공세 (Wraith Assault) — 가칭

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - Wraith 종 공격력 글로벌 영구 ×1.5.
  - 기본 Wraith: HP 500, 공격력 20, "매우 느림". → 픽 후 공격력 30.
  - GuardianRage(액티브: Wisp·Wraith HP ×2.0 + 받는 데미지 ×0.5, 15s) 조합 시 Wraith 공격력 30 → **30 DPS 순수 딜** (15s 창). 이전엔 GuardianRage 가 생존력만 올렸지만, 이 카드 추가 시 Tank 가 실질적 딜러로 전환.
  - WraithDamageBoost(현재 HP ×1.5)와의 조합: HP 750 + 공격력 30 → Wraith 를 "탱킹 + 딜링" 겸용 유닛으로 완성.
  - 밸런스 근거 (컨셉 §8): WispHpBoostEffect 가 Wisp HP ×1.5, ReaperLethalStrike(6/01 제안)가 Reaper Power ×1.35. 이 카드는 ×1.5 로 동등~소폭 강함이지만, Wraith 기본 DPS(20)가 Reaper(40) 절반이므로 최종 DPS 30 은 여전히 Reaper(40)보다 낮아 밸런스 범위 내.
  - Wraith 평균 필드 생존 수 1~2마리 × DPS 30 = **30~60 DPS** 추가. 영웅 HP 1000 기준 약 17~33s 내 기여 기대.
- **구현 패턴**:
  - `WraithAssaultEffect.cs` — WispHpBoostEffect 의 구조 그대로, 타깃 종을 `EMonster.Wraith`, 대상 스탯을 `MeleeAttacker.PowerScale *= 1.5f` 로 교체.
  - `IBattleContext.GetMonsters(EMonster.Wraith)` 로 현재 필드 전체 순회 후 영구 배율 적용. 이후 스폰되는 Wraith 에는 MonsterBuffService 의 글로벌 스탯 재적용 루프 활용 (기존 WispHpBoostEffect 가 쓰는 패턴 동일).
- **시너지 후크**:
  - **WraithDamageBoost** (HP ×1.5) + 이 카드: Wraith가 최대 내구도·최대 딜을 동시에 보유하는 "완전체 Tank-딜러". Tank Tier3 시너지(글로벌 캡 +6)와 결합 시 필드에 더 많은 완전체 Wraith 유지 가능.
  - **GuardianRage** (15s HP×2 + 방어 버프) + 이 카드: GuardianRage 발동 시 Wraith DPS가 일시적으로 가장 높아지는 "Tank 폭발딜" 창 생성. 전략적 발동 타이밍 의미 상승.
  - **ReplaceWispsToWraith** (Wisp 스포너 → Wraith 출력으로 영구 변경): Wraith 수 극대화 후 이 카드로 전체 DPS 증폭 → Wraith 단일 빌드 완성 경로.
- **구현 비용 추정**: 1 (WispHpBoostEffect 구조 그대로 복사 후 종·스탯 교체만. 신규 패턴·API 없음)
- **중복 재검증**:
  - WraithDamageBoost(기존): v0.6 에서 효과가 HP ×1.5 로 리뉴얼 → Power 배율 카드가 아님. 이 카드와 용도 분리 명확.
  - 과거 6/01 리퍼 격살(Reaper Power ×1.35): Reaper 종만 대상, 이 카드는 Wraith — 종이 다름.
  - 기존 28장 중 Wraith Power 배율 카드: 없음 ✅

---

## 2. 레이스 가속 (Wraith Rush) — 가칭

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - Wraith 종 이동속도 글로벌 영구 ×1.5.
  - 컨셉 §11.3: Wraith = "매우 느림" (기본 이동속도 값은 Spawn-round 기획서에 정의, 상대적으로 Wisp보다 훨씬 느림). ×1.5 적용 후 일반 속도에 근접.
  - 게임플레이 변화: Wraith 가 기존에는 영웅 근처에 도달하기까지 느려서 "존재하지만 멀리 있는" 상태가 많았음. 이 카드 픽 후 Wraith 가 영웅에 꾸준히 붙어있어 지속 딜 기여.
  - 밸런스 근거 (컨셉 §8): PhantomMoveSpeedBoostEffect 가 Phantom ×1.5. 이 카드는 동일 배율이지만 Wraith 는 HP 500 의 고체력 유닛이므로 빨라지면 위협도 상승. 이동속도 단독으론 DPS 증가 없고 "접근 보장"만 — 독립적으론 약하지만 레이스 공세 조합 시 "반드시 붙어서 30 DPS"를 완성.
- **구현 패턴**:
  - `WraithRushEffect.cs` — PhantomMoveSpeedBoostEffect 구조 그대로, 타깃 종을 `EMonster.Wraith` 로 교체. `IMover.Speed *= 1.5f` 영구 적용 패턴 재사용.
  - 신규 API·시스템 없음.
- **시너지 후크**:
  - **레이스 공세 (이 세트 카드 1)**: 빠른 Wraith(이동속도) + 강한 Wraith(공격력) — 두 카드가 "속도-딜" 양 날개를 완성. 어느 한 장만 픽해도 의미 있고, 두 장 픽 시 완전한 딜러 Tank 완성.
  - **SpawnWraith** (Wraith 스포너 출력 +1): 더 많은 Wraith 가 빠르게 전장에 투입 → 가속 + 증원 콤보.
  - **Tank Tier1 시너지(Wisp·Wraith HP ×1.3)**: 빠르고 강한 Wraith 가 HP 버프까지 받음 → 탱킹 지속성과 딜 모두 향상.
- **구현 비용 추정**: 1 (PhantomMoveSpeedBoostEffect 구조 그대로 종만 교체. 신규 없음)
- **중복 재검증**:
  - PhantomMoveSpeedBoost(기존): Phantom 전용, Wraith 카드 없음 ✅
  - 기존 28장 중 Wraith 이동속도 배율 카드: 없음 ✅
  - 과거 10회차: 이동속도 배율은 Phantom(기존), Slow(영웅+몬스터), 5/28 연쇄 저주(영웅 이속) 범위 — Wraith 전용 이동속도는 미탐색 ✅

---

## 3. 팬텀 독침 (Phantom Venom) — 가칭

- **카테고리**: 패시브 강화 (Swarm 축)
- **효과 모델**:
  - Phantom 이 영웅을 공격할 때마다 영웅에게 출혈 오라(HP -1%/s, 이동 시 활성, 지속 2초)를 부착. 최대 **5 스택** 독립 운영. 같은 스택 한도 내에서 중복 부착 시 각 스택의 지속 시간 독립 갱신.
  - 팬텀 1마리 × 공속 1s = 약 0.5 hit/s(원거리 부재·근접 도달 시간 포함 추산) → 1마리가 1s 에 1회 부착. 10마리 × 0.5 hit/s = 5 스택 최대치 도달 예상 시간 ≈ 10초.
  - 5 스택 만개 시: HP -1% × 5 = **-5%/s** (이동 중). 영웅이 이동 중이면 약 5~6s 내 HP -25~30% 출혈 가중.
  - 밸런스 근거 (컨셉 §8): Bleed 액티브(2%/s, 10s)가 이동 중 총 -20%. 이 카드의 5 스택은 -5%/s × 이동 비율 60% × 지속 ≈ -3%/s 실효. Bleed 액티브보다 약하지만 영구 조건부로 무제한 재발동.
  - 팬텀의 기본 DPS(5)가 매우 낮아 팬텀은 지금까지 "머릿수로만 가치 있는 유닛". 이 카드로 "체력 갉아먹기" 역할이 추가 — Swarm 빌드의 실질 딜 기여 명확화.
- **구현 패턴**:
  - B3 §2.3 `SpiderSlowOnHit` 패턴 직접 재사용:
    ```
    PhantomVenomOnHit : MonoBehaviour
      Awake: _attacker = GetComponent<IAttacker>()
             _attacker.OnHit += OnHit
      OnDisable: _attacker.OnHit -= OnHit  //# 풀 재사용 구독 누수 방지
      OnHit(IHealth target):
        if (target != heroHealth) return
        if (heroAuraRunner.CountActiveAuraOfType<BleedAura>() >= 5) return
        heroAuraRunner.Attach(new BleedAura(damagePerSecPercent:0.01f, duration:2f))
    ```
  - `HeroAuraRunner.CountActiveAuraOfType<T>()` 는 현재 없는 메서드 → 기존 `_slots` 리스트를 LINQ 로 카운트하는 1~3줄 신규 추가 필요.
  - Phantom 프리팹에 `PhantomVenomOnHit` 컴포넌트를 `LairCharacterPrefabBuilder` 에서 자동 부착.
  - BleedAura 자체는 이미 Bleed 액티브 카드에서 구현 완료 (B3 §6.2) — 재사용.
- **시너지 후크**:
  - **PhantomMoveSpeedBoost** (Phantom 이속 ×1.5): 빠른 팬텀이 더 자주 영웅에 닿아 독침 부착 속도 상승 → 5 스택 도달 시간 단축.
  - **SpawnPhantoms + SpawnerHaste**: 팬텀 수 증가 + 스폰 주기 단축 → 더 빠른 독침 누적.
  - **Bleed 액티브 카드**: Bleed 가 이미 활성화된 상태에서 팬텀 독침 스택까지 → 총 출혈 -3~7%/s. 기존 Bleed 의 "일회성 10초 창"을 팬텀이 영구 유지.
  - **Swarm Tier3(모든 스포너 동시 출력 +1)**: 팬텀 수 급증 → 독침 5 스택을 수초 내 달성 → 영웅 출혈 상시화.
- **구현 비용 추정**: 3 (SpiderSlowOnHit 패턴 재사용으로 대부분 완성. HeroAuraRunner 카운트 메서드 신규 추가 + Phantom 프리팹 컴포넌트 부착 + BleedAura 스택 독립 관리 검증 필요)
- **중복 재검증**:
  - SpiderSlowOnHit(B3 §2.3): 거미 전용, 영웅 둔화 SlowAura. 팬텀 독침은 팬텀 전용, 영웅 BleedAura — 종·효과 모두 다름.
  - 기존 Bleed(액티브): 카드 픽 시 1회 10초 BleedAura. 이 카드는 팬텀 OnHit 시 스택형 2초 BleedAura 지속 부착 — 트리거와 지속 구조가 다름.
  - 과거 6/02 SoulCurse: 영웅이 처치(kill) 시 HP -3% 역류, 15s 액티브. 이 카드는 팬텀이 공격(hit) 시 BleedAura 부착, 영구 패시브 — 트리거 주체(영웅 vs 팬텀)·효과 타입(즉발 HP -% vs BleedAura 스택) 모두 다름 ✅

---

## 4. 공통 테마 고찰

### 왜 "레이스와 팬텀의 각성"인가

현행 28장을 축별로 스탯 커버리지 분석:

| 종(種) | HP 배율 | Power 배율 | Speed 배율 | OnHit 효과 | 소환 | 교체 |
|---|---|---|---|---|---|---|
| Wisp | ✅ WispHpBoost | ─ | ─ | ─ | ✅ SpawnWisps | ✅ →Wraith |
| **Wraith** | ✅ WraithDamageBoost | **❌ 없음** | **❌ 없음** | ─ | ✅ SpawnWraith | ─ |
| Reaper | ─ | ✅ (6/01 Lethal) | ✅ ReaperAtkSpeed | ─ | ✅ SpawnReapers | ✅ →Hex |
| Hex | ─ | ─ | ✅ (6/01 Rapid) | ─ | ─ | ← |
| Plague | ─ | ─ | ─ | ✅ SlowOnHit | ✅ SpawnPlagues | ─ |
| **Phantom** | ─ | **❌ 없음** | ✅ PhantomMoveSpeedBoost | **❌ 없음** | ✅ SpawnPhantoms | ─ |

오늘 3장이 채우는 공백: Wraith Power · Wraith Speed · Phantom OnHit.

### 3장이 만드는 새로운 빌드 경로

```
[Wraith 완전체 빌드]
  WraithDamageBoost (HP×1.5) + 레이스 공세 (Power×1.5) + 레이스 가속 (Speed×1.5)
  + GuardianRage (HP×2, def, 15s) + ReplaceWispsToWraith (Wisp 스포너 → Wraith)
  → HP 750, DPS 30, 정상 이동속도 Wraith 무한 군단
  → 현재 Tank 빌드의 "탱킹만 되는 느린 벽"에서 "탱킹+딜링 겸용 돌격대"로 전환

[Phantom 출혈 swarm 빌드]
  PhantomMoveSpeedBoost + SpawnPhantoms + SpawnerHaste + Phantom 독침
  + Bleed 액티브 + Slow 액티브
  → 빠른 팬텀 군단이 지속 독침 5 스택 + Bleed 액티브와 중첩
  → 영웅이 이동만 해도 8~9%/s 출혈 → 100초 이내 클리어 가능성
```

### 왜 오늘 이 테마인가

QA 리포트(2026-05-22.md)가 BLOCKED 상태로 카드별 픽률 데이터를 제공하지 못한다. 대신 스탯 커버리지 공백 분석으로 도출. Wraith 와 Phantom 은 각각 Tank · Swarm 축의 핵심 유닛임에도 스탯 테이블에 명백한 빈 칸이 있어, v0.2 에서 이 공백을 채우지 않으면 해당 종 전문 빌드 경로가 미완성으로 남는다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **구현 우선순위 제안**:
  1. **레이스 공세 + 레이스 가속** (비용 각 1): WispHpBoostEffect / PhantomMoveSpeedBoostEffect 코드 복사 후 종 교체만 — 동일 PR 에 묶어 30분 내 완성 가능. 먼저 Tank 빌드 체감 보강.
  2. **팬텀 독침** (비용 3): SpiderSlowOnHit 패턴 + HeroAuraRunner.CountActiveAuraOfType() 추가 포함. SpiderSlowOnHit 이 이미 구현된 이후 시점에 진행 권장.
- **밸런스 확인 포인트**: 팬텀 독침 5 스택 × Bleed 액티브 중첩 시 영웅 순간 출혈 속도. 필요 시 스택 상한을 3으로 하향 또는 독침 데미지를 0.7%로 감소.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 레이스(Wraith)는 굉장히 튼튼하지만 너무 느리고 공격력도 별로라서, 사실상 "그냥 두꺼운 방어벽" 역할만 합니다. 그리고 팬텀은 작고 많이 나오지만 공격력이 너무 약해서 혼자서는 영웅에게 거의 위협이 안 됩니다. 오늘 제안하는 카드 3장은 이 두 가지 약점을 정확히 건드립니다. 레이스를 더 강하고 빠르게 만들어서 진짜 위협적인 돌격대로 바꾸고, 팬텀이 영웅을 때릴 때마다 영웅에게 서서히 출혈을 쌓게 해서 "개미 떼처럼 조금씩, 하지만 끊임없이" 영웅의 체력을 갉아먹게 합니다. 마치 레이스는 뚫기 어려운 충격 보병으로 바꾸고, 팬텀은 독침 날리는 벌떼로 만드는 거라고 생각하면 됩니다. 그래서 오늘 제안하는 카드 3장은: 레이스 공격력을 1.5배로 올리는 '레이스 공세', 레이스 이동속도를 1.5배로 올리는 '레이스 가속', 그리고 팬텀이 영웅을 공격할 때마다 출혈 스택을 쌓는 '팬텀 독침'입니다.
