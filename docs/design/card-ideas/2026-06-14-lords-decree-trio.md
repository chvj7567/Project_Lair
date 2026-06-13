# Card Ideas — 2026-06-14 — 영주의 칙령 3종 (전 종 동시 영구 강화 패시브)

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 영주의 칙령 (Lord's Decree) — 특정 종(種) 한 가지만 찍는 기존 패시브 강화 카드와 달리, 던전 영주가 전 부대에 칙령을 내려 **모든 몬스터 종을 동시에 소폭 영구 강화**하는 패시브 3장. 배율은 종-특정 카드(×1.4~1.5)보다 낮지만(×1.15~1.25), 픽 1장으로 Wisp·Wraith·Reaper·Hex·Plague·Phantom 전부에 즉시 소급 적용된다.
- **목록**: LordsBulwark (전 종 HP ×1.25 — Tank 축) / LordsWrath (전 종 공격력 ×1.2 — Dps 축) / LordsTide (전 종 이동속도 ×1.15 — Swarm 축)
- **기존 28장 + git log 과거 17회차와의 중복 회피 확인됨**
  - 기존 28장: `WispHpBoost`·`WraithDamageBoost`·`ReaperAtkSpeed`·`PhantomMoveSpeedBoost` 등 종-특정 강화 카드만 존재. `IronWill`(전 종 피해 ×0.7, 15s)·`Frenzy`(전 종 공속 +50%, 10s)는 **임시 액티브**로 영구 패시브 없음. `SpawnerHaste`(전 스포너 주기 ×0.8 영구)는 스포너 대상이지 몬스터 스탯이 아님.
  - 과거 17회차 테마 요약: 스냅샷 스케일링(05-28)·종간 공존 조건(05-29)·Plague 독 생태계(05-30)·영구 낙인 이중 효과(05-31)·Dps 공백 보완(06-01)·OnDeath 소환(06-02)·Wisp 벽 전술(06-03)·Dps×Debuff 교차(06-04)·타이머 연동(06-05)·군단 밀도 압박(06-06)·Wraith·Phantom 각성(06-07)·도주 처벌(06-08)·킬 카운터 처벌(06-09)·Tank 재생·분열(06-10)·크로스 축 교체(06-11)·스탯 공백 채우기(06-12)·즉시 소환 4축 완성(06-13) — **모든 회차에서 "무조건 영구 전 종 스탯 배율 패시브" 테마 부재** ✅

---

## 1. LordsBulwark — 영주의 방벽 (가칭)

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - 모든 몬스터 종의 HP 기준치 ×1.25 (영구). 픽 즉시 필드 생존 몬스터에 소급, 이후 스폰되는 모든 몬스터에도 자동 적용.
  - 수치 적용 예시:

    | 종 | 기존 HP | 적용 후 HP | 영웅(50 DPS) 처치 소요 시간 |
    |---|---|---|---|
    | Wisp | 200 | 250 | 4.0s → 5.0s (+1.0s) |
    | Wraith | 500 | 625 | 10.0s → 12.5s (+2.5s) |
    | Reaper | 100 | 125 | 2.0s → 2.5s (+0.5s) |
    | Hex | 60 | 75 | 1.2s → 1.5s (+0.3s) |
    | Plague | 50 | 62 | 1.0s → 1.24s (+0.24s) |
    | Phantom | 30 | 37 | 0.6s → 0.74s (+0.14s) |

  - 수치 근거: `WispHpBoost`(Wisp만 ×1.5) 대비 배율을 ×1.25로 낮추되 대상을 전 종으로 확장. 총 처치 부담 증가는 "종당 처치 시간 평균 +0.7s × 필드 평균 10~18마리" ≒ 7~12s 추가 압박. 2~4분 전투(컨셉 §8 기준)에서 유의미한 생존력 상승.
  - 중복 픽 시: 1.25 × 1.25 = 1.5625 → 사실상 WispHpBoost ×1.5 수준을 전 종에 적용. 2픽째는 "종-특정 강화를 이미 받은 종"에게도 추가 상승.
- **구현 패턴**: `LordsBulwarkEffect.cs` — `WispHpBoostEffect` 와 동일 내부 로직, 대상 Enum 을 단일 값 대신 `System.Enum.GetValues(typeof(EMonster))` 전체 순회로 교체.

  ```csharp
  //# 기존 WispHpBoostEffect 패턴 → EMonster 전체 반복
  public void Apply(IBattleContext ctx)
  {
      foreach (EMonster type in System.Enum.GetValues(typeof(EMonster)))
          ctx.MonsterBuffService.ApplyGlobalTypeMultiplier(type, MonsterStat.MaxHp, 1.25f);
  }
  ```

- **시너지 후크**:
  - `WispHpBoost` + LordsBulwark: Wisp HP = 200 × 1.5 × 1.25 = 375 — Tank 빌드의 Wisp가 사실상 소형 레이스급 내구력 획득.
  - `WraithDamageBoost`(Wraith HP ×1.5) + LordsBulwark: Wraith HP = 500 × 1.5 × 1.25 = 937. 필드를 거의 영구히 막는 대형 탱커 완성.
  - `GuardianRage`(Wisp·Wraith HP ×2.0, 15s) + LordsBulwark: 15s 동안 Wisp 750 HP, Wraith 1875 HP. 순간적으로 영웅 타격이 무의미해지는 수준.
  - 전 종 빌드(Tank+Dps+Swarm 혼합): LordsBulwark 1픽으로 모든 종의 생존력이 오르므로, 축을 섞어 쓰는 하이브리드 빌드에서 "잡화 생존력 패키지"로 가장 높은 유연성.
- **구현 비용 추정**: 2 (WispHpBoostEffect 에서 단일 foreach 루프 추가. Enum 열거 패턴이 기존 코드에 없어 약간 새로우나, 로직 자체는 단순)
- **중복 재검증**: 기존 28장에 "전 종 영구 HP 배율" 없음. 과거 17회차 중 전 종 HP 조작 카드: 05-28 "약자의 기백"(HP 낮은 몬스터만 조건 버프, 영구 아님)·06-09 "거울 갑옷"(반사 메카닉) — 모두 무조건·영구·전 종·HP 배율과 다름. 완전 신규.

---

## 2. LordsWrath — 영주의 분노 (가칭)

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - 모든 몬스터 종의 공격력(Power) ×1.2 (영구). 픽 즉시 필드 소급 + 이후 스폰 전부.
  - 수치 적용 예시:

    | 종 | 기존 DPS | 적용 후 DPS | 증가량 |
    |---|---|---|---|
    | Reaper | 40 | 48 | +8 |
    | Wraith | 20 | 24 | +4 |
    | Hex | 30 | 36 | +6 |
    | Wisp | 10 | 12 | +2 |
    | Plague | 5 | 6 | +1 |
    | Phantom | 5 | 6 | +1 |

  - 수치 근거: `ReaperAtkSpeed`(Reaper 공격속도 ×0.7 = DPS 환산 +43%)와 비교해, LordsWrath 는 Reaper에 +20% 만 주지만 전 종에 적용. 필드 평균 몬스터 구성(각 종 2~3마리) 전체 DPS 합 기준: 기존 ≈ 170 DPS → 적용 후 ≈ 204 DPS (+34 DPS 순증). 영웅 HP 1000 기준 전투 시간 ≈ 30~60s 단축 효과.
  - 중복 픽 시: 1.2 × 1.2 = 1.44 → 단일 종 특화 카드 수준에 근접.
- **구현 패턴**: `LordsWrathEffect.cs` — LordsBulwark 동일 구조, `MonsterStat.Power` 로 대상 스탯 교체.

  ```csharp
  public void Apply(IBattleContext ctx)
  {
      foreach (EMonster type in System.Enum.GetValues(typeof(EMonster)))
          ctx.MonsterBuffService.ApplyGlobalTypeMultiplier(type, MonsterStat.Power, 1.2f);
  }
  ```

- **시너지 후크**:
  - `ReaperAtkSpeed`(공속 ×0.7 = DPS×1.43) + LordsWrath(×1.2): Reaper 실효 DPS = 40 × 1.43 × 1.2 = 68.6. 리퍼 한 마리가 영웅 HP 1000 을 1초당 68.6 씩 깎음.
  - `MarkOfDeath`(영웅 받는 피해 ×1.5, 5s) + LordsWrath: 5s 동안 전 종 공격이 ×1.5 × 1.2 = ×1.8 배 피해. 필드 전체 DPS 204 × 1.8 = 367 DPS burst 창.
  - `Frenzy`(공속 +50%, 10s) + LordsWrath(공격력 ×1.2 영구): 공속·공격력이 동시 상승 → 10s 동안 실효 DPS = 원래 DPS × 1.2 × 1.5 = ×1.8.
  - 혼합 빌드: 어떤 종을 많이 뽑아도 LordsWrath 1픽으로 전부 강해지므로, 4축 고루 쓰는 빌드의 "딜 밀도 보장 카드".
- **구현 비용 추정**: 2 (LordsBulwarkEffect 에서 MonsterStat Enum 값만 교체)
- **중복 재검증**: 기존 28장에 "전 종 영구 공격력 배율" 없음 (`Frenzy`=공속·임시·액티브, `IronWill`=피해 방어·임시·액티브). 과거 17회차 중 전 종 공격력 강화: 05-28 "군중의 포효"(공속·임시·종 수 조건), 06-06 "DensityTide"(총 수 조건). 모두 무조건·영구·공격력 배율 아님. 완전 신규.

---

## 3. LordsTide — 영주의 조류 (가칭)

- **카테고리**: 패시브 강화 (Swarm 축)
- **효과 모델**:
  - 모든 몬스터 종의 이동속도 ×1.15 (영구). 픽 즉시 필드 소급 + 이후 스폰 전부.
  - 수치 근거: `PhantomMoveSpeedBoost`(Phantom 이속 ×1.5 = +50%)보다 낮은 ×1.15(+15%). Wraith(매우 느림)에도 +15% → 체감 가속 ≈ 0.5~1.0 유닛/s. Phantom(빠름)에 +15% → 이미 빠른데 더 빨라져 도주 대응이 어려워짐. Wisp/Plague 같은 중간 속도 종이 영웅을 더 빠르게 포위 형성. 전 종 +15% 가속이 미치는 포위 압박 완성 시간: 평균 -0.5~1.5s 단축 추정.
  - 주의: ×1.15 는 Slow 카드(영웅 이속 ×0.5 + 몬스터 이속 ×1.3, 10s) 의 임시 ×1.3 보다 낮다. 그러나 영구이므로 중복 픽 가능: ×1.15 × ×1.15 = ×1.32 ≈ Slow 의 일시 버프 수준을 영구로.
- **구현 패턴**: `LordsTideEffect.cs` — LordsBulwark 동일 구조, `MonsterStat.MoveSpeed` 로 교체.

  ```csharp
  public void Apply(IBattleContext ctx)
  {
      foreach (EMonster type in System.Enum.GetValues(typeof(EMonster)))
          ctx.MonsterBuffService.ApplyGlobalTypeMultiplier(type, MonsterStat.MoveSpeed, 1.15f);
  }
  ```

- **시너지 후크**:
  - `PhantomMoveSpeedBoost`(Phantom ×1.5) + LordsTide(×1.15): Phantom = ×1.5 × ×1.15 = ×1.725. 팬텀이 전력 질주로 영웅 근처에 순식간에 접근.
  - `Slow`(몬스터 이속 ×1.3, 10s) + LordsTide(×1.15 영구): 중첩 시 몬스터 이속 ×1.15 × ×1.3 = ×1.495. Slow 종료 후에도 영구 ×1.15 잔존.
  - `SpawnerHaste`(스포너 주기 ×0.8) + LordsTide: 새로 스폰된 몬스터가 더 빠른 주기 + 더 빠른 이속으로 영웅에게 수렴 → 압박 밀도 × 속도 동시 상승.
  - `TimeStop`(영웅 5s 정지) + LordsTide: 영웅 정지 상태에서 빨라진 몬스터 전군이 둘러싸기 완성. 포위망 형성 속도 대폭 단축.
  - Swarm Tier2(모든 스포너 주기 ×0.85) + LordsTide: 빠른 보충 + 빠른 수렴 = 영웅 입장에서 "파도가 쉬지 않고 몰려오는" 심리 압박.
- **구현 비용 추정**: 2 (LordsBulwarkEffect 에서 MonsterStat 교체)
- **중복 재검증**: 기존 28장에 "전 종 영구 이속 배율" 없음 (`Slow` 의 몬스터 이속 ×1.3 = 임시 10s). 과거 17회차 중 전 종 이속 강화: 05-28 "군중의 포효"(공속), 06-05 "CurseOfTime"(30초마다 +4% 누적·타이머 트리거), 06-06 "DensityTide"(밀도 조건 on/off) — 모두 조건부·임시·타이머 누적으로 픽 즉시 무조건 영구 전 종 이속 배율과 다름. 완전 신규.

---

## 4. 공통 테마 고찰

세 카드는 **"영주의 칙령 — 종을 가리지 않는 광역 영구 강화"** 라는 설계 공백을 채운다:

| 카드 | 축 | 대상 스탯 | 배율 | 종-특정 대응 카드 | 차별점 |
|---|---|---|---|---|---|
| LordsBulwark | Tank | HP | ×1.25 | WispHpBoost(×1.5, Wisp만) | 배율↓, 대상 전 종↑ |
| LordsWrath | Dps | Power | ×1.20 | ReaperAtkSpeed(쿨다운, Reaper만) | 스탯 다름, 전 종↑ |
| LordsTide | Swarm | MoveSpeed | ×1.15 | PhantomMoveSpeedBoost(×1.5, Phantom만) | 배율↓, 대상 전 종↑ |

**왜 이 테마를 오늘 골랐는가:**

1. **설계 공백 구조 분석**: 기존 28장 패시브 강화 카드는 모두 종-특정. 영구 "全종 스탯 배율" 패시브가 완전히 비어 있다.
2. **과거 17회차 전부 검토**: 일시적 전 종 버프(IronWill·Frenzy·Slow)는 이미 존재하지만 영구 패시브 버전은 없음. 가장 단순하면서도 아직 채워지지 않은 패턴.
3. **빌드 다양성 기여**: 종-특정 카드는 단일 축 빌드를 심화하지만, 칙령 3장은 **하이브리드 빌드** 의 "전 종 기반 체력" 역할을 한다. "내 몬스터가 여러 종이어서 개별 강화 카드가 애매할 때" 선택할 수 있는 카드군이 처음 생긴다.
4. **구현 단가**: 3장 모두 기존 단일 종 강화 이펙트에서 `foreach EMonster` 반복 1줄 추가. 구현 비용 2(×3장 = 총 6공수)로 낮다.
5. **QA 공백 대응**: QA 리포트가 BLOCKED 상태이므로 픽률 데이터 없음. 구조적 공백(종-특정만 있고 전-종 영구 패시브 없음)을 근거로 삼았다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- ECardId 후보: `LordsBulwark`, `LordsWrath`, `LordsTide`
- v0.2 진입 전까지 backlog 보관
- **채택 우선순위**: LordsWrath > LordsBulwark > LordsTide.
  - LordsWrath(공격력)은 "전 종 DPS 보장" 이라는 명확한 유스케이스가 있어 게임플레이 기여가 가장 즉각적.
  - LordsBulwark(HP)은 생존력 보장으로 '개념이 직관적'이고 밸런스 영향 예측이 쉬움.
  - LordsTide(이속)는 이동속도 스택이 지나치게 강해질 가능성 — 밸런스 검증 후 수치 조정 권장. 수치를 ×1.10 으로 보수적으로 낮추는 것도 검토할 것.
- **주의**: `LordsTide` ×1.15 × `PhantomMoveSpeedBoost` ×1.5 × `Slow` 몬스터 ×1.3 = 전체 ×2.24. Phantom 이미 고속인데 ×2.24 배가 되면 영웅 회피 불가 수준이 될 수 있음. 수치 캡 논의 필요.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 카드를 고르면 대부분 "이 몬스터 종류만" 강해집니다. 위스프만 더 튼튼해지거나, 팬텀만 더 빨라지거나 하는 식이죠. 마치 축구 감독이 수비수 한 명에게만 훈련을 몰아주는 것처럼요. 그런데 여러 종류의 몬스터를 섞어 쓰는 전략을 택하면, 한 종만 강화하는 카드가 큰 도움이 안 됩니다. 마치 "나는 수비수도 쓰고 공격수도 쓰는데, 수비수 훈련 카드만 나와서 곤란한" 상황이죠. 오늘 제안하는 카드 3장은 이런 상황을 해결합니다 — "모든 팀원에게 동시에 약간의 훈련 보너스"를 주는 카드로, 어떤 몬스터를 쓰든 전부 조금씩 튼튼해지거나, 조금씩 세지거나, 조금씩 빨라집니다. 그래서 오늘 제안하는 카드 3장은: 모든 몬스터 종의 체력을 한꺼번에 25% 늘려주는 카드, 모든 종의 공격력을 20% 올려주는 카드, 모든 종의 이동속도를 15% 높여주는 카드입니다.
