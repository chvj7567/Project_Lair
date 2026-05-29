# Card Ideas — 2026-05-30 — 플레이그-독 연쇄 시너지

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 플레이그(Plague)를 핵심 축으로 삼아 독 오라(HeroPoisonAura)를 단독 빌드 경로로 승격시키는 생태계 트리오
- **목록**: 부패의 신호 / 역병 증폭 / 독 폭발
- **기존 25장 + git log 과거 회차와의 중복 회피 확인됨**
  - 기존 25장: HeroPoisonAura(독 오라 부착)·PlagueSlowBoost(플레이그 둔화 강화) — 플레이그 사망 트리거, 독 동적 증폭, 독 조건부 즉발 폭발은 없음
  - 과거 1회차 (전장 상태 감지): "연쇄 저주" = HeroPoisonAura + HeroAttackDown 환경 빌드에서 둔화 효과 강화 (여러 디버프 스택 확장). 본 카드들은 단일 독 빌드 '심화'로 방향 상이
  - 과거 2회차 (종 간 연계 시너지): 탱커-딜러·유틸-딜러 공존 조건 버프. 독·플레이그 생태계와 무관

---

## 1. 부패의 신호 (Signal of Decay) — 가칭

- **카테고리**: 환경 (Environment) · 패시브
- **효과 모델**:
  - 플레이그가 사망할 때마다 영웅에게 **DPS 5 독 오라를 3초간** 부착한다.
  - 이미 독 오라가 활성화 중이면 **지속 시간만 3초로 갱신** (DPS 중복 없음).
  - 즉, 플레이그가 연속으로 처치될수록 독이 끊이지 않는다.
  - 밸런스 근거 (컨셉 §8): Plague HP = 50(낮음). 영웅이 1초에 1~3마리를 처치하는 구간에서 독 갱신 빈도 약 1~3회/초. DPS 5 × 최대 유지 상태 ≈ +5 DPS. HeroPoisonAura(기존 DPS 5)와 합산 시 독 빌드의 실질 DPS 10으로 이중화.
- **구현 패턴**: BloodThirstService 패턴 거의 동일 (B3 §5.4 OnDied 구독).
  ```
  PlagueDeathPoisonService:
    Start: CharacterRegistry.Monsters 의 Plague 종 OnDied 구독
    OnPlagueDied: ctx.ApplyHeroAura(new PoisonAura(5), 3f)
  ```
  - HeroAuraRunner.Attach 시 동일 타입 오라 지속 시간 갱신 로직 필요 (현재 미지원이면 +1 메서드 추가)
- **시너지 후크**:
  - SpawnPlagues (기존 추가 카드 #11) → 플레이그 수 증가 → 사망 트리거 빈도 증가
  - PlagueSlowBoost (기존 강화 카드 #5) → 플레이그 내구도 간접 강화 (둔화로 영웅 이동 억제 → 플레이그 생존 시간 연장)
  - 역병 증폭 (카드 2) → 플레이그 생존 중 독 DPS 증폭, 죽으면 부패의 신호가 독 유지
- **구현 비용 추정**: 2 (BloodThirstService 패턴 재사용, PoisonAura 갱신 로직 소폭 추가)
- **중복 재검증**: 기존 HeroPoisonAura = 영구 DPS 5 고정 단독 부착. 이 카드는 플레이그 사망 이벤트 조건부·단기 갱신 독 → 트리거 축이 다름. 과거 1회차 연쇄 저주 = 기존 환경 카드 2장 보유 시 새 저주 강화 (스택 확장 방식). 본 카드는 Plague 종 사망 이벤트 구독 방식 — 구분됨.

---

## 2. 역병 증폭 (Plague Amplification) — 가칭

- **카테고리**: 강화 (Enhance) · 패시브
- **효과 모델**:
  - HeroPoisonAura(독 오라)가 영웅에게 **이미 활성화된 상태**에서만 효과 발동.
  - 독 오라가 활성화 중일 때, 현재 필드에 생존한 플레이그 수 × **DPS +2**씩 독 오라의 DPS를 동적으로 증폭한다.
  - 플레이그 0마리 = 기본 DPS 5 유지. 플레이그 1마리 = DPS 7. 최대 5마리 = DPS 15.
  - 플레이그가 죽거나 추가 스폰되면 매 tick에 자동 재산출됨.
  - HeroPoisonAura가 없으면 이 카드는 효과 없음 (빈 픽 방지는 UI 경고로 처리 제안).
  - 밸런스 근거 (컨셉 §8): 목표 사망 밴드 120~240s. 독 DPS 15 × 180s = 2700 HP — 독 오라 단독으로 영웅 HP 1000 초과. 현실적으로 플레이그 5마리 유지는 영웅이 타 몬스터도 공격하므로 실효 유지 시간 < 100%. 평균 플레이그 유지 3마리 × 역병 증폭 = DPS 11, 라운드 2 Power 하향 후 밴드 내 동작 기대.
- **구현 패턴**: MonsterBuffService의 매 tick 재적용 구조 차용 + IHeroAura 조합.
  ```
  PlagueAmpService:
    Tick(dt):
      poisonAura = heroAuraRunner.GetAura<PoisonAura>()
      if poisonAura == null: return
      int count = ctx.GetMonsters(EMonster.Plague).Count()
      poisonAura.SetDps(BASE_DPS + count * 2)
  ```
  - `PoisonAura`에 `SetDps(float v)` 메서드 추가 (기존 생성자 고정값에서 변경 가능으로 확장)
  - `HeroAuraRunner.GetAura<T>()` 헬퍼 추가 (existing `_slots` 순회)
- **시너지 후크**:
  - HeroPoisonAura (기존 패시브 #14) — 전제 카드. 반드시 먼저 픽해야 의미 있음
  - SpawnPlagues (기존) + PlagueSlowBoost (기존) → 플레이그를 많이 살려두는 빌드의 독 DPS 배가
  - 부패의 신호 (카드 1) → 플레이그 죽을 때도 독 3초 갱신, 플레이그 전멸 순간 독 유지 브릿지 역할
- **구현 비용 추정**: 3 (PoisonAura.SetDps 확장 + HeroAuraRunner.GetAura 헬퍼 + PlagueAmpService 신규 POCO)
- **중복 재검증**: 기존 PlagueSlowBoost = 둔화 효과 강화 (SlowAura duration 연장). 이 카드는 독 DPS 동적 스케일링 — 완전히 다른 효과 차원. 과거 어느 회차와도 "독 동적 증폭" 아이디어 없음.

---

## 3. 독 폭발 (Toxic Burst) — 가칭

- **카테고리**: 저주 (Curse) · 액티브
- **효과 모델**:
  - [독 오라 활성 시] 영웅에게 **즉시 최대 HP의 10%** 피해를 준다. 쌓인 독을 한꺼번에 폭발시키는 이미지.
  - [독 오라 미활성 시] 영웅에게 **DPS 4 독 오라를 5초간** 부착한다 (약한 후속 옵션).
  - 즉, "독 빌드에서 강하고 일반 빌드에서는 평범한" 이분법 설계.
  - 밸런스 근거 (컨셉 §8): 라운드 2 진입 후 영웅 HP 1000 기준 최대 HP 10% = 100 즉발. 기존 액티브 저주 카드 Weaken(-50% 공격 10초) ≈ 누적 약 200~300 피해 환산. 독 폭발은 즉발이지만 독 빌드 전제 조건 → 보정 후 밴드 내 적정.
- **구현 패턴**: IBattleContext + HeroAuraRunner 조합.
  ```csharp
  [Serializable]
  public class ToxicBurstEffect : ICardEffect
  {
      public void Apply(IBattleContext ctx)
      {
          IHealth hero = ctx.GetHero();
          if (hero == null) return;
          Transform heroT = ctx.GetHeroTransform();
          HeroAuraRunner runner = heroT?.GetComponent<HeroAuraRunner>();
          bool hasPoisonAura = runner != null && runner.HasAura<PoisonAura>();
          if (hasPoisonAura)
              hero.TakeDamage(Mathf.RoundToInt(hero.Max * 0.10f));
          else
              ctx.ApplyHeroAura(new PoisonAura(4), 5f);
      }
  }
  ```
  - `HeroAuraRunner.HasAura<T>()` 헬퍼 (카드 2의 `GetAura<T>()` 와 동일 인프라 공유)
- **시너지 후크**:
  - HeroPoisonAura + 역병 증폭 활성 후 독 DPS 10~15인 상태에서 독 폭발 발동 = 10% 즉발 + 이후 고DPS 독 지속 → 최고 효율
  - IronWill (기존, 몬스터 피해 -30%) 와 함께 쓰면 독 폭발 즉발 + 몬스터 방어 강화로 이중 압박
  - 부패의 신호 활성화 후 플레이그가 죽기 시작한 시점에 독 폭발 사용 → 독 오라 갱신 + 즉발 폭발 시퀀스
- **구현 비용 추정**: 2 (조건 분기 + 기존 PoisonAura/HeroAuraRunner 재사용, HasAura 헬퍼 소폭 추가)
- **중복 재검증**: 기존 Bleed(출혈) = 이동 시 초당 HP -2% (지속 조건). 독 폭발 = 독 오라 유무 분기 즉발 또는 단기 독 — 트리거 조건·효과 타입 모두 다름.

---

## 4. 공통 테마 고찰

### 왜 오늘 이 테마인가

QA 3차 리포트(2026-05-26) 데이터가 두 가지 구조 문제를 명확히 드러낸다:

1. **HeroPoisonAura 픽률 최저 (12회 / 446회, 2.7%) + 하락 추세 (-7)**: 독 빌드 경로가 단독으로는 매력이 없다. 쌓인 독 DPS 5는 영웅 HP 1000 대비 너무 느리고, 다른 카드와의 시너지 경로가 없어 "항상 마지막 남은 카드"로 인식된다.
2. **4전략 분산 0.96% (span 0.30s)**: 어떤 빌드를 해도 결과가 비슷하다. 딜러 빌드·탱커 빌드·랜덤 빌드 모두 31.7~32.0s 사망. 카드 결정이 텍스처 차이를 만들 뿐 결과를 분리하지 못한다.

이 두 문제를 동시에 해결하는 방향이 "독 빌드 경로를 실질적으로 강력한 독립 아이덴티티로 만드는 것"이다.

- **부패의 신호** → HeroPoisonAura 없이도 "플레이그 중심 빌드"가 독을 생산하는 경로 제공
- **역병 증폭** → HeroPoisonAura + 플레이그 다수 생존 조건이 갖춰질 때 독 DPS를 3배 이상 끌어올림 → 독 빌드 단독 결과 분리 가능
- **독 폭발** → 독 상태를 즉발 자원으로 소비하는 새로운 의사결정 moment 추가 → "지금 폭발시킬까, 독을 계속 유지할까" 트레이드오프

세 카드가 함께 채택될 때 "SpawnPlagues → PlagueSlowBoost → 역병 증폭 → 부패의 신호 → 독 폭발"의 코히어런트 빌드 경로가 생긴다.

### 과거 회차와의 차별성 요약

| 회차 | 핵심 메커니즘 | 본 트리오 |
|---|---|---|
| 1회차 (전장 상태 감지) | 조건 충족 시 효과 강화 (디버프 다수 보유 → 3번째 디버프 강해짐) | 독 단일 경로 심화 (독 DPS 동적 증폭 + 이벤트 트리거) |
| 2회차 (종 간 연계 시너지) | 특정 종 쌍 공존 → 전체 공격속도·공격력 증폭 | 단일 종(플레이그) 중심 독 생태계 자급자족 구조 |

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- 우선순위 제안: 역병 증폭(구현 비용 3, 독 빌드 아이덴티티 핵심) → 독 폭발(비용 2, 액티브 저주 확장) → 부패의 신호(비용 2, 패시브 환경 다양화) 순
- PoisonAura.SetDps / HeroAuraRunner.GetAura<T>() 헬퍼는 세 카드가 공유하는 인프라 — 첫 카드 구현 시 한꺼번에 추가하면 나머지 비용 절감
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 독 장판(HeroPoisonAura)은 영웅에게 천천히 피해를 주는 카드인데, 너무 느려서 아무도 잘 안 고릅니다 — 마치 촛불 하나로 통나무에 불을 붙이려는 것과 비슷합니다. 오늘 제안하는 카드들은 그 촛불을 모닥불로 키우는 방법입니다. 플레이그(독뿌리는 보라색 몬스터)가 살아있을수록 독이 점점 세지고, 플레이그가 죽을 때도 마지막 독을 남기며, 쌓인 독을 한꺼번에 터뜨리는 폭발 카드도 추가됩니다. 그래서 오늘 제안하는 카드 3장은: "플레이그가 죽을 때 독을 남기는 '부패의 신호'", "플레이그를 많이 살려둘수록 독이 더 강해지는 '역병 증폭'", "독이 쌓인 상태라면 즉시 터뜨리는 '독 폭발'"입니다.
