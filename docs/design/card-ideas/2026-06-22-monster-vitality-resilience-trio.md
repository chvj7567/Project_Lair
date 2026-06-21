# Card Ideas — 2026-06-22 — 몬스터 생존력 3종 — 패시브 재생 × 방벽 부여

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 몬스터 생존력 — 3가지 서로 다른 회복 메커니즘. 기존 28장과 과거 24회차 전부가 HP 강화(×배율)·수 증가(스포너 출력)·사망 이벤트(분열/흡혈/OnDeath)에 집중된 반면, **패시브 시간 재생(HP/sec)** 과 **임시 방벽(Shield buffer)** 은 어느 회차·카드에서도 제안되지 않은 최초 메커니즘이다.
- **목록**: IronFlesh (강철의 살결 — Tank·Debuff 두 종 패시브 재생) / PlaguePersistence (역병의 끈질김 — Plague 패시브 재생) / GuardianShield (수호자의 방벽 — Wisp·Wraith 임시 방벽 액티브)
- **기존 28장 + 24회차 과거 루틴 전부와의 중복 회피 확인됨**
  - 기존 28장: 회복 카드는 BloodThirst(처치 시 인근 몬스터 HP +30, 이벤트 트리거) 단 1장. 패시브 재생(초당 HP 회복)·방벽(Shield) 없음 ✅
  - 06-10 tank-regen-division: Wisp 분열(사망 이벤트→스폰), Wraith 흡혈(공격 히트 이벤트→자가 회복), 거울 갑옷(피해 반사). 패시브 재생·방벽과 메커니즘 레이어 전혀 다름 ✅
  - 06-02 death-echo: PhantomBirth(사망→소환), SoulCurse(처치→역류), WraithRemnant(레이스 사망→위스프). 회복 카드 아님 ✅
  - 05-30 plague-poison-chain: Plague 사망→독 오라, Plague 생존 중 독 DPS 증폭, 독 즉발 폭발. 재생·방벽 없음 ✅
  - 06-18 spawner-diversity: HarmonyHeal(6종 유지 시 몬스터 회복) — 조건부·다양성 조건 전제. IronFlesh/PlaguePersistence는 무조건 패시브 재생 ✅
  - 06-16 wild-card: DoubleDraft·TacticalSurge·CurseResonance — 픽·전술·공명 메커니즘. 방벽 없음 ✅
  - 나머지 19회차 전부: 스탯 배율·스포너 조작·영웅 디버프·밀도 임계·타이머 연동·HP 조건 분기·영웅 전투력 잠식 등 — 패시브 재생·방벽 개념 없음 ✅

---

## 1. IronFlesh — 강철의 살결

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - Wisp 종 AND Wraith 종 모두: **초당 2 HP 패시브 재생** 영구 적용.
  - 픽 직후부터 런 내내 모든 필드 Wisp·Wraith(현재 + 이후 스폰)가 1초마다 2 HP 회복.
  - 회복은 MaxHP 이상으로 초과하지 않음.
  - 중첩 픽: 2픽 → 4 HP/sec, 3픽 → 6 HP/sec.
  - **수치 근거 (컨셉 §8)**:
    - Wisp (200 HP, 50 DPS hero, slow → ~4s TTK): 재생 2 HP/sec → 영웅 순 실효 DPS = 50-2 = **48**. Wisp TTK ≈ 4.17s (기본 4.0s vs +0.17s, 4%). 미미하나, 영웅이 여러 몬스터를 분산 공격할 때 의미 있음 — 25 HP/sec(분산 공격 시) - 2 = 23 DPS → TTK ≈ **8.7s** (기본 8s vs +8.5%).
    - Wraith (500 HP): 재생 2 HP/sec → 순 DPS 48 → TTK ≈ **10.4s** (기본 10s). 영웅이 여러 Wraith와 교전 시 재생 누적 효과 강화.
    - 2픽(4 HP/sec): 분산 공격 시 순 DPS 21 → Wisp TTK ≈ 9.5s. 점진적 강화.
    - 06-10의 레이스 흡혈(공격 히트 시 +20 HP, 1/sec = +20 HP/sec) 와 비교: IronFlesh(2 HP/sec 무조건)는 전투 밀도와 무관한 베이스라인 회복 — 두 카드 중첩 시 Wraith 회복량 22 HP/sec(흡혈 20 + 재생 2) → 영웅 순 DPS ≈ 28 → Wraith TTK **17.9s** (강력 시너지).
- **구현 패턴**:
  - `MonsterBuffService` 에 신규 API `RegisterPassiveRegen(EMonsterType[] types, float hpPerSec, bool permanent)` 추가.
  - 구현: 1초 코루틴 루프 — `CharacterRegistry.GetAliveMonsters(type)` 순회 → `monster.GetComponent<IHealth>().Heal(hpPerSec)`.
  - `IHealth.Heal(float amount)` 미지원 시 `SetHp(Mathf.Min(Current + amount, Max))` 로 대체.
  - `IronFleshEffect.Apply(IBattleContext ctx)` → `ctx.GetMonsterBuffService().RegisterPassiveRegen(new[]{ EMonsterType.Wisp, EMonsterType.Wraith }, 2f, permanent: true)`.
  - HeroPoisonAura(영웅 추적 독장판)의 틱 기반 DPS 구조를 "몬스터 대상 힐"로 역방향 적용 — 새 시스템이지만 기존 코루틴 패턴 재사용.
- **시너지 후크**:
  - **WispHpBoost(×1.5) + IronFlesh(2 HP/sec)**: Wisp HP 300, 재생으로 느린 소진 → Tank 전선 장기 유지
  - **Wraith 흡혈(06-10, +20 HP/hit) + IronFlesh**: Wraith 재생 합산 22 HP/sec → 순 피해 28/sec → TTK 17.9s (강력 복합 Tank 빌드)
  - **GuardianShield(60 HP 방벽, 20s) + IronFlesh**: 방벽 소진 후에도 재생으로 일부 HP 복원 — 액티브 창 이후에도 지속 효과
  - **HeroAttackDown(영웅 공격력 ×0.75)**: 영웅 순 DPS 37.5 → Wisp 재생 2 → 순 35.5 → TTK 5.6s (기본 5.3s보다 긴 생존)
- **구현 비용 추정**: 3 (MonsterBuffService.RegisterPassiveRegen 신규 API — 1초 코루틴 + CharacterRegistry 순회 + IHealth.Heal. 기존 코루틴 패턴 재사용 가능, 신규 시스템 없음)
- **중복 재검증**: 기존 28장 + 24회차 전부 — 패시브 HP 재생(초당 자동 회복) 카드 전무. 06-10 Wraith 흡혈(공격 히트 이벤트 트리거)·BloodThirst(처치 이벤트 트리거) 는 이벤트 트리거, IronFlesh 는 무조건 시간 기반 재생 — 메커니즘 레이어 다름 ✅

---

## 2. PlaguePersistence — 역병의 끈질김

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - Plague 종: **초당 1.5 HP 패시브 재생** 영구 적용.
  - 픽 직후부터 런 내내 모든 필드 Plague(현재 + 이후 스폰)가 1초마다 1.5 HP 회복.
  - 중첩 픽: 2픽 → 3 HP/sec, 3픽 → 4.5 HP/sec.
  - **수치 근거 (컨셉 §8)**:
    - Plague (50 HP, 5 DPS, 약함): 영웅 DPS 50 → 1타에 사망(1s). 재생 1.5 HP/sec 단독으로는 영웅 직격 1타 생존 불가.
    - 그러나 영웅이 Plague 를 직접 타깃하지 않는 구간(Wisp·Wraith 처리 중): 1.5 HP/sec × 5s = **7.5 HP 회복**. Plague HP 50 기준 15% 회복. 장기전에서 Plague 군단이 완전히 소진되지 않고 잔류 유지.
    - SpawnPlagues(+1 출력) 조합: 필드 Plague 동시 3~4마리 → 각자 1.5 HP/sec 재생 → 총 재생량 4.5~6 HP/sec (분산). 영웅이 Plague를 집중 공략하지 않으면 군단 유지.
    - HeroAttackDown(영웅 DPS ×0.75 = 37.5) 조합: Plague 생존 시간 50/37.5 = **1.33타(1.33s)** → 1.33s × 1.5 HP/sec ≈ 2 HP 재생. 미세하지만 다수 Plague가 동시 교전 시 분산 공격으로 재생 시간 확보 가능.
    - 밸런싱 의도: Plague는 원래 "낮은 HP → 빠른 사망" 의 희생 유닛. 이 카드는 그 취약점을 약화시켜 "영웅이 Plague를 빠르게 제거하지 못하면 느리게 쌓여가는 지속 압박"으로 변환. 단독으로 강하지 않고 SpawnPlagues·PlagueSlowBoost 와 삼위일체.
- **구현 패턴**:
  - `PlaguePersistenceEffect.Apply(ctx)` → `ctx.GetMonsterBuffService().RegisterPassiveRegen(new[]{ EMonsterType.Plague }, 1.5f, permanent: true)`.
  - **IronFlesh 와 동일 `RegisterPassiveRegen` API 재사용** — 타입·수치만 다름. 구현 비용 IronFlesh에서 API 완성 후 0 추가 비용.
- **시너지 후크**:
  - **SpawnPlagues(+1 출력) + PlaguePersistence(재생)**: 많은 Plague가 군단으로 유지 → 영웅 사방 슬로우 압박
  - **PlagueSlowBoost(SlowFactor ×0.75) + PlaguePersistence**: Plague가 더 강하게 느리게 하면서 더 오래 살아있음 → 영웅이 Plague 필드에서 탈출 어려워짐
  - **Debuff Tier1(Plague SlowFactor ×0.8 추가, 3픽) + PlaguePersistence**: 극한 둔화 Plague 군단 유지
  - **HeroPoisonAura(영웅 발밑 독장판 5 DPS) + PlaguePersistence**: 영웅이 느려진(Plague 슬로우) 상태에서 독장판 상시 밟음 → Plague 재생으로 영웅 포위 유지
- **구현 비용 추정**: 1 (IronFlesh `RegisterPassiveRegen` API 구현 완료 후 타입/수치 파라미터 변경만. 신규 코드 5줄 이하)
- **중복 재검증**: 05-30 plague-poison-chain(Plague 사망→독 오라 트리거, Plague 생존 중 독 증폭, 독 즉발 폭발) — 사망 이벤트 or 독 오라 강화 메커니즘. PlaguePersistence는 무조건 시간 기반 HP 재생 — 레이어 완전히 다름 ✅. 06-12(PlaguePowerBoost — 공격력 배율) 와도 다름 — PlaguePersistence는 재생(HP) ✅

---

## 3. GuardianShield — 수호자의 방벽

- **카테고리**: 액티브 버프 (Tank 축)
- **효과 모델**:
  - 발동 즉시 **20초간** 필드의 모든 Wisp AND Wraith 종에게 **60 HP 방벽(Shield)** 부여.
  - Shield는 실제 HP 앞에서 피해를 먼저 흡수한다. Shield가 먼저 소진된 후 HP가 감소.
  - Shield는 20초 후 잔량에 무관하게 제거 (회복 없이 시간 만료 소멸).
  - 20초 내에 Wisp·Wraith가 새로 스폰되면 새 개체도 즉시 60 HP 방벽 부여.
  - 중첩 픽(2번째 동일 액티브): 지속 시간 +15s 연장(총 35s) — Shield 수치는 동일.
  - **수치 근거 (컨셉 §8)**:
    - Wisp (200 HP): 방벽 60 HP → 실효 HP 260. 영웅 DPS 50 → TTK ≈ 5.2s (기본 4.0s, +30%).
    - Wraith (500 HP): 방벽 60 HP → 실효 HP 560 → TTK ≈ 11.2s (기본 10s, +12%).
    - 영웅이 Wisp 집중 공략 시 방벽 1.2s 추가 생존 — 짧지만 Plague 슬로우와 조합하면 체감 강함.
    - **GuardianRage(HP×2.0 + 데미지 ×0.5, 15s) 동시 발동 조합 (주의)**:
      - Wisp: HP 400 + 방벽 60 → 460, 영웅 실효 DPS 25(×0.5) → TTK ≈ **18.4s** (15s GuardianRage 창 중 Wisp 사실상 사망 불가).
      - 15s GuardianRage 만료 후: Wisp HP 400, 방벽 잔량(5s × 25 DPS → 방벽 125 소진 → 60 방벽 소진, Wisp HP 소모 65 → HP 335) → 방벽 없는 335 HP Wisp.
      - 밸런스 우려: 두 액티브 동시 발동 시 강력. 그러나 두 액티브 모두 30초 주기에서만 선택 가능 → 양쪽 다 픽하려면 별도 액티브 슬롯 필요. 전략적 비용 존재.
    - **IronWill(데미지 ×0.7, 15s) 와 비교**:
      - IronWill: 모든 몬스터 지속 피해 감소(비율 기반). GuardianShield: Tank 종 선행 흡수(절대량). IronWill이 대규모 교전에서 더 효율적, GuardianShield는 집중 공격 당하는 개별 Tank 보호에 더 효율적. 다른 사용 맥락.
- **구현 패턴**:
  - `GuardianShieldEffect.Apply(IBattleContext ctx)`:
    - `ctx.GetMonsterBuffService().ApplyShield(new[]{ EMonsterType.Wisp, EMonsterType.Wraith }, shieldAmount: 60, duration: 20f)`.
    - `ApplyShield` 구현: `IHealth` 에 `AddShield(int amount, float duration)` 추가. `MonsterHealthComponent` 내 `_shieldBuffer` 필드 추가, `TakeDamage(int dmg)` 에서 `_shieldBuffer` 선 차감 분기.
    - 20초 코루틴 만료 시 `_shieldBuffer = 0` 리셋.
    - 새 스폰 Wisp·Wraith: `MonsterBuffService` 가 활성 Shield 버프를 추적하고 스폰 직후 즉시 `AddShield` 적용 (기존 GlobalBuff 적용 패턴과 동일 진입점 활용).
  - `IronWillEffect`(몬스터 전체 피해 배율 ×0.7)와 비교: IronWill은 배율 인자, GuardianShield는 선행 버퍼 — 동일 `IHealth.TakeDamage` 내에서 처리 순서 정의 필요 (방벽 선 차감 → 배율 감소 적용 순서 또는 역순).
  - CHMPool 패턴 영향 없음 — `OnEnable` 리셋 시 `_shieldBuffer = 0` 으로 클리어.
- **시너지 후크**:
  - **IronFlesh(2 HP/sec 재생) + GuardianShield(60 HP 방벽 20s)**: 방벽이 먼저 데미지를 흡수하는 20s 동안 재생이 실제 HP를 꾸준히 회복 → 방벽 만료 후에도 HP 손실이 최소화되어 있음
  - **GuardianRage(HP×2 + 데미지 ×0.5, 15s) + GuardianShield(방벽 60, 20s)**: 중첩 시 Tank 극한 생존 창 (주의: 두 액티브 동시 사용 = 강력한 Tank 풀빌드 클라이맥스)
  - **SpawnWraith(Wraith 출력 +1) + GuardianShield**: 다수 Wraith가 전선에 있을 때 방벽 발동 → 전체 Wraith 라인이 일제히 60 HP 추가 방어력 획득
  - **WispHpBoost(HP ×1.5) + GuardianShield**: Wisp HP 300 + 방벽 60 = 360 실효 HP → 영웅 대비 TTK 7.2s (기본 Wisp 4s의 1.8배)
- **구현 비용 추정**: 3 (IHealth.AddShield 신규 메서드 + MonsterHealthComponent._shieldBuffer 필드 추가 + ApplyShield 서비스 메서드. 기존 GlobalBuff 진입점 재사용 가능, 신규 시스템이지만 작은 범위)
- **중복 재검증**:
  - IronWill(데미지 배율 ×0.7, 모든 몬스터, 15s): 비율 기반 감소 — GuardianShield는 절대량 선행 흡수 ✅
  - GuardianRage(HP×2.0 + 데미지 ×0.5, 15s): HP 직접 배율 + 피해 배율 조합 — GuardianShield는 추가 HP가 아닌 별도 방벽 버퍼 ✅
  - 06-10 거울 갑옷(피해 반사): 피해를 영웅에게 역반사 — GuardianShield는 피해 흡수(소멸) ✅
  - 과거 24회차 전부: 방벽(Shield buffer) 개념 없음 ✅

---

## 4. 공통 테마 고찰

오늘 3장은 **"기존 28장과 24회차 루틴이 건드리지 않은 두 가지 회복 메커니즘의 최초 카드화"** 라는 공통 이유로 묶인다:

| 카드 | 회복 메커니즘 | 축 | 최초 제안 여부 |
|---|---|---|---|
| IronFlesh | 패시브 시간 재생 (HP/sec 무조건) | Tank | 최초 — 이전 0장 |
| PlaguePersistence | 패시브 시간 재생 (HP/sec, Plague 특화) | Debuff | 최초 — 이전 0장 |
| GuardianShield | 임시 방벽 버퍼 (데미지 선흡수) | Tank | 최초 — 이전 0장 |

**기존 "회복" 계열 카드와의 차별화 매트릭스**:

| 카드 | 트리거 | 대상 | 회복 방식 |
|---|---|---|---|
| BloodThirst (기존) | 처치 이벤트 | 인근 몬스터 HP +30 | 이벤트 일회성 HP 추가 |
| Wraith 흡혈 (06-10 제안) | 공격 히트 이벤트 | 자신 HP +20 | 이벤트 일회성 HP 추가 |
| HarmonyHeal (06-18 제안) | 6종 유지 조건 지속 | 전체 몬스터 회복 | 조건부 주기 HP 회복 |
| **IronFlesh (오늘)** | 무조건 시간 기반 | Wisp·Wraith HP/sec | **무조건 연속 재생** |
| **PlaguePersistence (오늘)** | 무조건 시간 기반 | Plague HP/sec | **무조건 연속 재생** |
| **GuardianShield (오늘)** | 액티브 발동 | Wisp·Wraith 방벽 | **데미지 선흡수 버퍼** |

**왜 오늘 이 테마인가:**
- QA 리포트(2026-05-22)는 시뮬레이션 미실행(BLOCKED) — 픽률 데이터 없이 구조 분석으로 공백 도출.
- 현재 Tank 축은 "처음부터 크게" (HP 배율, 수 증가, 즉시 소환) 패턴에 집중. **"전투 중 능동적으로 회복"** 경로(재생·방벽)가 전혀 없음.
- Debuff 축의 Plague 유닛은 50 HP의 극취약 유닛 — 스탯 배율(PlaguePowerBoost 06-12)·수 증가(SpawnPlagues)·사망 트리거(05-30 독 오라)는 있지만 Plague 자체가 "오래 살아남는" 경로가 없다. PlaguePersistence가 이를 처음 채운다.
- GuardianShield는 IronWill·GuardianRage 이후 세 번째 Tank 액티브 카드가 되어 Tank 액티브 빌드의 깊이를 추가.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- **구현 전제 조건 및 순서**:
  1. **IronFlesh 선행** (비용 3): `MonsterBuffService.RegisterPassiveRegen` API 신규 설계·구현. Wisp·Wraith 타입 등록. `IHealth.Heal` 미지원 시 추가.
  2. **PlaguePersistence** (비용 1): IronFlesh API 완성 후 Plague 타입·수치 파라미터만 변경. 거의 무료.
  3. **GuardianShield** (비용 3): `IHealth.AddShield` + `MonsterHealthComponent._shieldBuffer` 추가. IronFlesh와 별도 PR 또는 함께 묶어 처리.
  - **추천 묶음**: IronFlesh + PlaguePersistence 를 같은 PR에 (패시브 재생 API 설계 공유), GuardianShield는 다음 PR.
- **v0.2 우선도 제안**: PlaguePersistence (가장 빠르게 구현, Plague 빌드 가치↑) → GuardianShield (Tank 액티브 다양성↑) → IronFlesh (중복 포함 시 API 무료 제공).
- v0.2 진입 전까지 backlog 보관.
- **시너지 빌드 예시 (채택 후 조합)**:
  - "철벽 Tank": WispHpBoost + IronFlesh(재생) + GuardianShield(방벽) + GuardianRage(HP×2·데미지 반감) → Wisp 가 4가지 생존력 레이어 확보
  - "역병 군단": SpawnPlagues + PlagueSlowBoost + PlaguePersistence(재생) → Plague 군단이 사방에서 느리게 해도 쉽게 제거 안 됨

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 몬스터들은 HP를 모두 잃으면 그냥 죽어버리고, 한 번 맞은 피해는 돌아오지 않습니다. 그래서 영웅이 몬스터를 하나씩 처치해 나가면 전선이 얇아지는 게 당연했어요. 오늘 제안하는 카드들은 이 흐름을 바꿉니다. 위스프·레이스는 가만히 있어도 초마다 조금씩 HP가 회복되고(철살), 플레이그도 느리게나마 스스로 회복하면서 계속 영웅을 느리게 만들고(역병의 끈질김), 그리고 수호자의 방벽 카드를 쓰면 20초 동안 모든 위스프·레이스에게 추가 보호막이 생겨 그 피해를 먼저 흡수합니다. 쉽게 말해, 영웅이 싸워도 싸워도 전장이 천천히 '버티고 회복하는' 던전이 되는 카드들입니다. 그래서 오늘 제안하는 카드 3장은: 탱커들이 조금씩 스스로 회복하는 '강철의 살결', 슬로우 유닛 플레이그가 잘 안 죽는 '역병의 끈질김', 그리고 탱커 몬스터에게 20초 한시 보호막을 부여하는 '수호자의 방벽'입니다.
