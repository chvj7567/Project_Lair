# Slice B3 — 카드/몬스터 컨텐츠 확장 설계서

> Project Lair MVP 의 네 번째 수직 슬라이스 — 기획서 §11.3 컨텐츠 완성.
> 작성일: 2026-05-20
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
빌드 다양성을 기획서 목표치(몬스터 6종 / 패시브 15장 / 액티브 10장)까지 채워
**"빌드업의 재미"** 가설을 제대로 검증할 컨텐츠 폭을 확보.

### 0.2 In Scope (B3)
- 몬스터 3종 추가: 궁수 / 거미 / 박쥐 (총 6종)
- 패시브 카드 8장 추가 (총 15장)
- 액티브 카드 10장으로 전면 재구성 (B2 의 5장 → 기획서 10장 정렬)
- 신규 시스템: 몬스터 글로벌 버프 처리기 (`MonsterBuffService`)
- 인터페이스 확장: `IHealth.Heal` / `IMover.IsMoving` / `IAttacker.OnHit`
- 오버레이 스케일: `Health.DamageTakenScale` / `MeleeAttacker.CooldownScale` / `PowerScale`

### 0.3 Out of Scope
- 카드 등급/희귀도 / 진화 카테고리
- 메타 진행 / 사운드 / 아트
- 영웅 시야 기반 AI (시야 카드는 다른 효과로 대체 — §3.5)
- 투사체 물리 (궁수는 "긴 사거리 근접" 으로 단순화 — §2.2)

### 0.4 검증 가설
"몬스터 6종 + 카드 25장이면 한 판마다 다른 빌드가 나오는가." B3-M6 직후 사용자가 여러 판 플레이로 판단.

---

## 1. 프로젝트 룰 매핑

| 룰 | 본 설계에서의 적용 |
|---|---|
| 01 자동 커밋 + 한글 포맷 | 마일스톤별 커밋 메시지(안)만 |
| 02 주석 `//#` | 모든 신규 주석 |
| 03 종속성 최소화 | 카드 효과는 IBattleContext 만, 거미 특수능력은 IAttacker.OnHit 이벤트 구독 |
| 04 프리팹화 | 몬스터 3종 프리팹 + 카드 SO 18장 |
| 05 MVVM | CardSelectionPopup 재사용 — 변경 없음 |
| 06 상위 인터페이스 | IBattleContext 재사용 |
| 07 ChvjPackage | CHMResource/CHMUI/CHMPool 재사용 |
| 08 Enum 키 | EMonster 3종 추가 + ECardId 전체 재정의, 값명 = 파일명 |
| 09 CommonEnum | EMonster/ECardId 갱신 → CommonEnum.cs |
| 10 CommonInterface | IHealth/IMover/IAttacker 확장 → Character/CommonInterface.cs |
| 11 CHText/CHButton | 변경 없음 |
| 12 CHMPool 스폰 | 몬스터 3종 풀 워밍 추가, 소환 카드는 SpawnMonsterRuntime 재사용 |
| 13 UIArg 동일 파일 | 변경 없음 |

---

## 2. 몬스터 3종

### 2.1 스탯 (기획서 §11.3 / §11.4)

| 몬스터 | 메쉬 | 색 | Scale | HP | Power | Range | Cooldown | MoveSpeed |
|---|---|---|---|---|---|---|---|---|
| 궁수 Archer | Capsule | `#EAB308` | 0.8 | 60 | 30 | **5.0** | 1.0 | 2.0 |
| 거미 Spider | Cube | `#A855F7` | 0.5 (Y 0.3) | 50 | 5 | 1.0 | 1.0 | 2.0 |
| 박쥐 Bat | Sphere | `#1F2937` | 0.3 | 30 | 5 | 1.0 | 0.8 | 3.5 |

### 2.2 궁수 — 원거리
투사체 물리는 도입하지 않음. `Range = 5.0` 으로 길게 잡아 후방에서 공격하는 효과만 구현.
기존 `AutoCombatAI` 의 `dist <= _attacker.Range` 분기로 자연 처리 — 신규 코드 0.

### 2.3 거미 — 공격 시 영웅 둔화
신규 컴포넌트 `SpiderSlowOnHit` (거미 프리팹 전용):
- `Awake` 에서 자기 `IAttacker.OnHit` 구독
- `OnHit(IHealth target)` → target 이 영웅이면 그 Transform 의 `HeroAuraRunner` 에 `SlowAura(0.8)` 를 짧은 duration(1.5초)으로 Attach
- 거미가 계속 때리면 `HeroAuraRunner` 의 재부착 연장 로직으로 둔화 지속
- `OnHit` 은 §4.3 의 `IAttacker` 확장

### 2.4 박쥐 — 특수 없음
"시야 차단" 폐기 (§3.5). 박쥐는 저비용·고이동·다수 컨셉. 특수 컴포넌트 없음.

### 2.5 EMonster 확장
```csharp
public enum EMonster { Slime, Golem, Orc, Archer, Spider, Bat }
```
기존 3값 뒤에 append — int 시프트 없음. 프리팹 파일명 = 값명 (Rule 08).

---

## 3. 패시브 카드 +8장 (총 15장)

### 3.1 목록

| ECardId | 카테고리 | 카드명 | 효과 |
|---|---|---|---|
| ArcherRangeBoost | Enhance | 궁수 정밀 | 궁수 사거리 +40% |
| SpiderSlowBoost | Enhance | 독거미 | 거미 둔화 효과 강화 (0.8 → 0.6 배율) |
| BatMoveSpeedBoost | Enhance | 흡혈박쥐 떼 | 박쥐 이동속도 +50% |
| SpawnOrcs | Spawn | 오크 증원 | 오크 2마리 소환 |
| SpawnSpiders | Spawn | 거미 둥지 | 거미 2마리 소환 |
| SpawnBats | Spawn | 박쥐 무리 | 박쥐 5마리 소환 |
| ReplaceOrcsToArchers | Replace | 사격 훈련 | 오크 전부 → 궁수 (1:1) |
| HeroAttackDown | Environment | 약화의 저주 | 영웅 공격력 영구 -25% |

### 3.2 강화 카드 구현
기존 B1 강화 카드(`SlimeHpBoostEffect` 등) 패턴 그대로 — `ctx.GetMonsters(EMonster.X)` 순회 후 스탯 변경.
- 궁수 사거리: `MeleeAttacker` 의 `_range` 직접 ×1.4 (B1 `OrcAtkSpeedEffect` 가 cooldown 변경하는 패턴과 동일)
- 거미 둔화 강화: 거미의 `SpiderSlowOnHit` 의 slowFactor 를 더 강하게 — `SpiderSlowOnHit.SetSlowFactor(0.6f)` 메서드 노출
- 박쥐 이동속도: `IMover.Speed ×1.5`

### 3.3 소환 카드 구현
B1 `SpawnSlimesEffect` 패턴 — `ctx.SpawnMonster(EMonster.X, nearHero)` 반복.

### 3.4 교체 카드 구현
B1 `ReplaceSlimesToGolemEffect` 패턴 — 대상 몬스터 스냅샷 → Push → 같은 수만큼 신규 스폰.
`ReplaceOrcsToArchers` 는 1:1 (오크 N → 궁수 N).

### 3.5 시야 카드 대체
`HeroAttackDown` (#15 대체): 영웅 공격력 영구 -25%. `HeroPoisonAura` 와 같은 Environment 카테고리.
영구 효과이므로 `HeroAuraRunner` 에 무제한(duration < 0) `HeroAttackDownAura` Attach.
`HeroAttackDownAura.OnAttached` → 영웅 `IAttacker` 의 `PowerScale ×= 0.75` (§4.4 오버레이).

---

## 4. 인터페이스 / 오버레이 확장

advisor 점검 반영 — 모든 신규 메커니즘의 결합점을 여기서 못박는다.

### 4.1 `IHealth.Heal` — 액티브 #7 피의 갈증용
```csharp
public interface IHealth
{
    // ... 기존 멤버
    void Heal(int amount);   //# Max 초과 불가
}
```
`Health.Heal` / `FakeHealth.Heal` 구현. `Current = Min(Max, Current + amount)` + `OnChanged` 발행.

### 4.2 `IMover.IsMoving` — 액티브 #2 출혈용
```csharp
public interface IMover
{
    // ... 기존 멤버
    bool IsMoving { get; }
}
```
`SimpleMover` — `MoveTo` 호출된 프레임 이후 목적지 도달 전이면 true, `Stop` 시 false.
`FakeMover` — 테스트용 수동 토글 프로퍼티.

### 4.3 `IAttacker.OnHit` — 거미 특수능력용
```csharp
public interface IAttacker
{
    // ... 기존 멤버
    event Action<IHealth> OnHit;   //# TryAttack 성공 시 target 으로 발행
}
```
`MeleeAttacker.TryAttack` 성공 직후 `OnHit?.Invoke(target)`.
`FakeAttacker` — `event` 보유 + 테스트에서 수동 발행.

### 4.4 오버레이 스케일 — 글로벌 버프/디버프 결합점
직접 `[SerializeField] private` 필드를 건드리지 않고 런타임 배율로 처리:

| 컴포넌트 | 신규 프로퍼티 | 기본값 | 사용처 |
|---|---|---|---|
| `Health` | `float DamageTakenScale` | 1.0 | #8 강철 의지 (몬스터 받는 데미지 ×0.7) |
| `MeleeAttacker` | `float CooldownScale` | 1.0 | #5 광폭화 (몬스터 공속 — cooldown ×0.67) |
| `MeleeAttacker` | `float PowerScale` | 1.0 | #3 무력화, #10 폭주, #15 약화의 저주 |

- `Health.TakeDamage(amount)` → 내부에서 `amount = Mathf.RoundToInt(amount * DamageTakenScale)`
- `MeleeAttacker.TryAttack` → cooldown 비교에 `_cooldown * CooldownScale`, 데미지에 `_power * PowerScale`
- 오버레이는 **곱셈 합성** — 여러 버프가 겹치면 곱으로 누적

### 4.5 인터페이스 변경 영향 파일
`IHealth`/`IMover`/`IAttacker` 확장 → 구현체 전부 갱신:
- 프로덕션: `Health`, `SimpleMover`, `MeleeAttacker`
- 테스트 더블: `FakeHealth`, `FakeMover`, `FakeAttacker`

---

## 5. 몬스터 글로벌 버프 시스템

### 5.1 `MonsterBuffService` (POCO)
영웅 전용 `HeroAuraRunner` 의 형제. 모든 몬스터에 일괄 버프를 매 tick 강제 적용.

```csharp
public class MonsterBuffService
{
    //# 활성 버프 — 종류 + 남은 시간 + 적용 함수
    private readonly List<Buff> _buffs = new();

    public void AddBuff(EMonsterBuff type, float duration);   //# 같은 type 재부착 시 시간 연장
    public void Tick(float dt);                                //# BattleController.Update 가 호출
}
```

### 5.2 동작 모델 — "매 tick 재적용"
중간에 스폰된 몬스터도 자동 포함되도록 **상태 보존이 아닌 매 tick 강제** 방식:

```
Tick(dt):
  1) 만료된 버프 제거 (Remain -= dt)
  2) CharacterRegistry.Monsters 전체 순회
  3) 각 몬스터의 오버레이 스케일을 base(1.0) 로 리셋
  4) 활성 버프를 곱셈 누적 적용
```

- 새 몬스터가 스폰돼도 다음 tick 에 자동으로 활성 버프 적용
- 버프 만료 시 다음 tick 에 1.0 으로 자연 복원 (별도 OnDetached 불필요)
- 즉발 버프(#7 피의 갈증의 처치 트리거, #10 의 HP 변경)는 §5.4 별도 처리

### 5.3 `EMonsterBuff` enum
```csharp
public enum EMonsterBuff { Frenzy, IronWill, BerserkPower }
```
- `Frenzy` → 모든 몬스터 `MeleeAttacker.CooldownScale = 0.67`
- `IronWill` → 모든 몬스터 `Health.DamageTakenScale = 0.7`
- `BerserkPower` → 모든 몬스터 `MeleeAttacker.PowerScale = 3.0` (#10 폭주의 데미지 +200%)

### 5.4 즉발/특수 효과
지속 배율이 아닌 1회성:
- **#10 폭주의 HP -50%**: 효과 발동 순간 모든 몬스터 `Health` 를 현재값 절반으로 (`TakeDamage(Current/2)`). 데미지 +200% 부분만 `BerserkPower` 버프(15초).
- **#7 피의 갈증**: `BloodThirstService` — 30초간 몬스터 사망(`OnDied`) 구독, 사망 위치 주변 반경 내 몬스터에 `Heal(30)`.
- **#6 증식**: 즉발 — `CharacterRegistry.Monsters` 를 EMonster 별 집계 → 최다 종을 현재 수만큼 추가 스폰.

---

## 6. 액티브 카드 10장 (기획서 정렬)

### 6.1 목록

| ECardId | 카테고리 | 카드명 | 효과 | 구현 |
|---|---|---|---|---|
| Fear | 저주 | 공포 | 영웅 3초간 도망 | `FearAura` → AutoCombatAI flee |
| Bleed | 저주 | 출혈 | 영웅 이동 시 HP -2%/초 (10초) | `BleedAura` → IMover.IsMoving 체크 |
| Weaken | 저주 | 무력화 | 영웅 데미지 -50% (10초) | `WeakenAura` → PowerScale ×0.5 |
| Slow | 저주 | 둔화 | 영웅 이동속도 -50% (10초) | `SlowAura` (B2 재활용, 0.5/10초) |
| Frenzy | 버프 | 광폭화 | 모든 몬스터 공속 +50% (10초) | `MonsterBuffService.AddBuff(Frenzy)` |
| Multiply | 버프 | 증식 | 최다 몬스터 종 즉시 2배 | 즉발 스폰 (§5.4) |
| BloodThirst | 버프 | 피의 갈증 | 처치 시 주변 몬스터 HP +30 (30초) | `BloodThirstService` (§5.4) |
| IronWill | 버프 | 강철 의지 | 몬스터 받는 데미지 -30% (15초) | `MonsterBuffService.AddBuff(IronWill)` |
| TimeStop | 와일드 | 시간 정지 | 영웅 5초 멈춤 (이동+공격) | `TimeStopAura` → IMover.Stop + IAttacker.Enabled=false |
| Berserk | 와일드 | 폭주 | 몬스터 HP -50%, 데미지 +200% (15초) | 즉발 HP 절감 + `BerserkPower` 버프 (§5.4) |

### 6.2 영웅 디버프 오라 — `HeroAuraRunner` 재사용
`Fear`/`Bleed`/`Weaken`/`Slow`/`TimeStop` 모두 `IHeroAura` 구현, `ctx.ApplyHeroAura` 로 부착.

- **`FearAura`** — `OnAttached` 시 영웅 `AutoCombatAI.FleeMode = true`, `OnDetached` 시 false
- **`BleedAura`** — `Tick(hero, dt)` 마다 `ctx.GetHeroMover().IsMoving` 이면 누적 1초당 `hero.TakeDamage(Max×0.02)`
- **`WeakenAura`** — `OnAttached` 시 영웅 `IAttacker.PowerScale ×= 0.5`, `OnDetached` 복원
- **`SlowAura`** — B2 기구현 재활용 (둔화 수치 0.5 / 지속 10초로 카드 SO 설정)
- **`TimeStopAura`** — `OnAttached` 시 `IMover` 백업+`Speed=0`, `IAttacker.Enabled=false`. `OnDetached` 복원

### 6.3 `AutoCombatAI` flee 모드
```csharp
public bool FleeMode { get; set; }
```
`Update` 에서 `FleeMode == true` 면 nearest 타겟의 **반대 방향**으로 `MoveTo`, 공격 안 함.

---

## 7. B2 자산 폐기 + 카드 SO 재빌드

advisor 의 enum 시프트 경고 반영 — enum 정리와 전체 SO 재빌드를 **M5 한 마일스톤에 동시 수행**.

### 7.1 폐기 대상 (B2)
- 효과 클래스: `MonsterAoeDamageEffect`, `InstantSpawnGolemEffect`, `InstantSpawnSlimesEffect`
- `HeroSilenceEffect` — `TimeStop` 으로 흡수 (침묵≠시간정지, 별 카드로 폐기)
- `HeroSlowEffect` — `Slow` 로 재명명·재활용 유지
- B2 액티브 SO 5장 — M5 에서 전량 재생성

### 7.2 ECardId 최종형 (25개)
```csharp
public enum ECardId
{
    //# 패시브 15장
    SlimeHpBoost, GolemDamageBoost, OrcAtkSpeed,
    ArcherRangeBoost, SpiderSlowBoost, BatMoveSpeedBoost,
    SpawnSlimes, SpawnGolem, SpawnOrcs, SpawnSpiders, SpawnBats,
    ReplaceSlimesToGolem, ReplaceOrcsToArchers,
    HeroPoisonAura, HeroAttackDown,

    //# 액티브 10장
    Fear, Bleed, Weaken, Slow,
    Frenzy, Multiply, BloodThirst, IronWill,
    TimeStop, Berserk,
}
```

### 7.3 재빌드 순서 (M5)
1. ECardId 전체 재정의 + B2 효과 클래스 4개 삭제
2. `LairCardPrefabBuilder` 의 `PassiveSpecs`(15) / `ActiveSpecs`(10) 최종형으로 갱신
3. `Lair/Setup/B3 - Rebuild All Cards` 메뉴 1회 실행 → 25장 SO + 2 풀 전량 재생성
   - 모든 SO 가 새 enum 으로 다시 직렬화되므로 int 시프트 무관

---

## 8. 마일스톤

분할(B3a/B3b) 대신 6개 마일스톤으로 — 각 마일스톤이 컴파일·테스트 통과하는 동작 상태를 유지.

| 마일스톤 | 산출물 | 검증 |
|---|---|---|
| B3-M1 | 몬스터 3종 프리팹(특수능력 X) + EMonster 확장 + 캐릭터 빌더 갱신 | 에디터에서 3종 스폰 시각 확인 |
| B3-M2 | 인터페이스 3종 확장 + 오버레이 스케일 + 구현체/테스트더블 갱신 + SpiderSlowOnHit | EditMode 전체 PASS |
| B3-M3 | 패시브 효과 클래스 8개 + EditMode 테스트 | TDD (POCO) |
| B3-M4 | MonsterBuffService + BloodThirstService + 몬스터측 액티브 효과 4개 + 테스트 | TDD |
| B3-M5 | 영웅측 액티브 효과 6개 + 오라 5종 + AutoCombatAI flee + ECardId 재정의 + 카드 SO 25장 재빌드 + B2 폐기 | 메뉴 실행 → 25장 + 2풀 |
| B3-M6 | BattleController 통합 (풀 워밍 6종 + MonsterBuffService.Tick) + PlayMode 스모크 + 수동 검증 | 한 판 플레이 |

---

## 9. 위험 요소

| 위험 | 영향 | 완화 |
|---|---|---|
| 인터페이스 3종 확장이 모든 구현체·테스트더블에 파급 | 컴파일 에러 다발 | M2 를 인터페이스 전용 마일스톤으로 격리, 한 번에 전부 갱신 |
| `MonsterBuffService` 매 tick 전체 순회 비용 | 몬스터 수십 시 프레임 부담 | MVP 규모(수십)에선 무시 가능. 필요 시 dirty 플래그 최적화는 후속 |
| 오버레이 배율이 풀 재사용 시 잔존 | 다음 판 몬스터가 버프 상태로 시작 | `Health.OnEnable`/`MeleeAttacker.OnEnable` 에서 스케일 1.0 리셋 (Rule 12 패턴) |
| ECardId 재정의 중 기존 SO 깨짐 | M3~M4 동안 카드 플레이 검증 불가 | 효과 클래스는 EditMode 로 검증, SO/플레이 검증은 M5 이후로 일괄 |
| 거미 `OnHit` 이 풀 재사용 시 구독 누수 | 이벤트 핸들러 중복 | `SpiderSlowOnHit.OnEnable` 에서 재구독, `OnDisable` 에서 해제 |
| 궁수 사거리 5.0 이 화면 밖 공격 | 시각적으로 어색 | 전장 크기 대비 검증, 필요 시 4.0 으로 하향 (M1 수동 확인) |

---

## 10. 성공 기준 (사용자 검증)

- [ ] 궁수/거미/박쥐가 화면에 정상 스폰·전투
- [ ] 거미가 영웅을 때리면 영웅이 느려짐
- [ ] 패시브 15장 / 액티브 10장이 선택지에 등장
- [ ] 광폭화/강철 의지/폭주가 모든 몬스터(중간 스폰 포함)에 적용
- [ ] 공포 시 영웅이 도망, 시간 정지 시 영웅 완전 정지
- [ ] 영웅 사망 후 새 판에서 버프/디버프 잔존 X
- [ ] EditMode + PlayMode 테스트 전부 PASS
