# 지속 스폰 라운드 — 기능 기획서

> 작성: game-designer · 2026-05-26
> 대상 버전: MVP

---

## 1. 개요

씬에 배치된 Spawner 6개가 한 판 내내 고정 주기로 몬스터를 흘려보낸다.
영웅은 링 중앙에서 시작하고, 몬스터는 사방에서 수렴해온다.

- **승리**: 영웅 HP 0
- **패배**: 5분 타임오버
- **MVP 범위**: 몬스터 6종, 패시브 15장, 액티브 10장

---

## 2. 영웅 스탯

| 항목 | 값 |
|---|---|
| 시작 위치 | (0, 0, 0) |
| HP | 4000 |
| 공격력 | 50 |
| 공격 쿨다운 | 1.0s |

---

## 3. Ring 배치

Spawner 6개를 반지름 **14.0 유닛** ring에 60° 간격으로 균등 배치한다.

### 3.1 Spawner 상세

| # | 각도 | 위치 (x, z) | 종 | 스폰 주기 | 초기 지연 |
|---|---|---|---|---|---|
| 1 | 0° | (14.0, 0.0) | Slime | 9.0s | 0.0s |
| 2 | 60° | (7.0, 12.124) | Orc | 12.0s | 0.5s |
| 3 | 120° | (-7.0, 12.124) | Bat | 6.0s | 1.0s |
| 4 | 180° | (-14.0, 0.0) | Slime | 9.0s | 1.5s |
| 5 | 240° | (-7.0, -12.124) | Golem | 20.0s | 2.0s |
| 6 | 300° | (7.0, -12.124) | Archer | 15.0s | 2.5s |

초기 지연은 Spawner 간 첫 스폰 시점을 분산시켜 시작 직후 몰림을 방지한다.

---

## 4. 몬스터 스탯

`BalanceConfig.asset` 기준값.

| 종 | HP | Power | MoveSpeed | Cooldown | Range |
|---|---|---|---|---|---|
| Slime | 200 | 5 | 1.0 | 1.0s | 1.5 |
| Golem | 500 | 10 | 0.8 | 1.0s | 1.5 |
| Orc | 100 | 6 | 1.5 | 0.5s | 1.5 |
| Archer | 60 | 9 | 1.4 | 1.0s | 5.0 |
| Spider | 80 | 2 | 1.3 | 1.0s | 1.5 |
| Bat | 30 | 2 | 2.4 | 1.0s | 1.5 |

---

## 5. 필드 몬스터 캡

- **글로벌 하드 캡: 18마리**
- 캡 초과 시 해당 Spawner는 해당 사이클을 skip (백오프)
- 액티브 증식 카드(Multiply) 소환도 캡 초과분 truncate

---

## 6. 카드 시스템

### 6.1 강화 카드 (6장) — 영구 글로벌 타입 버프

픽 시 해당 종의 글로벌 스탯 버프가 영구 등록된다.
이후 스폰되는 동일 종 전부와 현재 필드에 있는 동일 종에도 즉시 소급 적용된다.
중첩 픽은 곱연산으로 누적된다.

| 카드 | 종 | 적용 스탯 | 배율 |
|---|---|---|---|
| SlimeHpBoost | Slime | 최대 HP | ×1.5 |
| GolemDamageBoost | Golem | 공격력 | ×1.5 |
| OrcAtkSpeed | Orc | 공격 쿨다운 | ×0.7 |
| ArcherRangeBoost | Archer | 사거리 | ×1.4 |
| BatMoveSpeedBoost | Bat | 이동속도 | ×1.5 |
| SpiderSlowBoost | Spider | SlowFactorMul | ×0.75 |

**거미 둔화 계산**: 기준값 `BaseSlowFactor = 0.8`에 `SlowFactorMul`을 곱연산.
1픽 시 0.8 × 0.75 = 0.6 (이동속도 60%로 감속).

### 6.2 추가 소환 카드 (5장) — Spawner 동시 출력 +1

픽 시 해당 종을 출력 중인 Spawner의 동시 출력 수를 각각 +1 (영구 누적).
해당 종을 출력하는 Spawner가 없으면 no-op.

| 카드 | 효과 |
|---|---|
| SpawnSlimes | Slime 출력 Spawner 전부 동시 출력 +1 |
| SpawnGolem | Golem 출력 Spawner 전부 동시 출력 +1 |
| SpawnOrcs | Orc 출력 Spawner 전부 동시 출력 +1 |
| SpawnSpiders | Spider 출력 Spawner 전부 동시 출력 +1 |
| SpawnBats | Bat 출력 Spawner 전부 동시 출력 +1 |

### 6.3 교체(융합) 카드 (2장) — Spawner 출력 종 영구 변경

픽 시 매칭 Spawner의 출력 종을 대상 종으로 영구 변경한다.
매칭 Spawner가 없으면 no-op. 이미 필드에 있는 몬스터는 변경되지 않는다.

강화 버프는 종에 귀속되고, 동시 출력 수·출력 종은 Spawner에 귀속된다.

| 카드 | 효과 |
|---|---|
| ReplaceSlimesToGolem | Slime 출력 Spawner → Golem 출력으로 변경 |
| ReplaceOrcsToArchers | Orc 출력 Spawner → Archer 출력으로 변경 |

### 6.4 환경 카드 (2장)

HeroPoisonAura, HeroAttackDown — 별도 기획서 참조.

### 6.5 카드 트리거 타이밍

- **패시브**: 영웅 HP 10%마다 3택 1 (한 판 최대 9픽)
- **액티브**: 30초마다 3택 1 (한 판 최대 9픽)

---

## 7. 거미(Spider) 특이사항

스타터 Spawner 6개에 Spider는 포함되지 않는다.
`SpawnSpiders`, `SpiderSlowBoost` 카드는 풀에 포함되지만,
Spawner가 없으면 no-op이다. 이는 의도된 설계다.

---

## 8. Spawner 비주얼 (MVP)

작은 Cylinder (Y 스케일 0.1, 회색) — 씬 사전 배치 정적 오브젝트.

---

## 9. 구현 요청사항

### Enum

`CommonEnum.cs` 의 `ECardId` (또는 기존 카드 Enum)에 아래 값 확인/추가:

```
SlimeHpBoost, GolemDamageBoost, OrcAtkSpeed, ArcherRangeBoost,
BatMoveSpeedBoost, SpiderSlowBoost,
SpawnSlimes, SpawnGolem, SpawnOrcs, SpawnSpiders, SpawnBats,
ReplaceSlimesToGolem, ReplaceOrcsToArchers
```

### Spawner SO 스키마

`SpawnerConfig.asset` (ScriptableObject) — Spawner당 1개:

| 필드 | 타입 | 설명 |
|---|---|---|
| monsterType | EMonster | 출력 종 (런타임에 교체 카드로 변경 가능) |
| spawnInterval | float | 스폰 주기 (초) |
| initialDelay | float | 초기 지연 (초) |
| spawnCount | int | 동시 출력 수 (기본 1, 추가 소환 카드로 +1) |
| position | Vector3 | 월드 위치 (씬 배치용 참고값) |

### 글로벌 버프 적용 규칙

- 버프는 종(EMonster) 키로 관리되는 딕셔너리에 저장
- 픽 시 필드에 살아있는 동일 종 전부에 즉시 소급 적용
- 이후 스폰되는 동일 종은 스폰 시점에 누적 버프 반영

### 필드 캡 적용 규칙

- 글로벌 카운터로 현재 필드 몬스터 수 추적
- 스폰 직전 캡(18) 초과 여부 검사, 초과 시 skip
- 몬스터 사망 시 카운터 -1
