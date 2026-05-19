# Rule 12 — 모든 스폰은 ChvjPackage 풀(`CHMPool`) 사용

## 룰
런타임에 GameObject 를 생성하는 **모든 코드** 는 `ChvjUnityInfra.CHMPool` 의 `Pop` / `Push` 로 처리한다.
`UnityEngine.Object.Instantiate` 및 `GameObject.CreatePrimitive` 직접 호출 금지(아래 예외 참조).

## 대상
- 캐릭터 (영웅/몬스터) 스폰
- 카드 효과로 추가 소환되는 몬스터 (예: `SpawnSlimesEffect` → `CHMPool.Pop`)
- 시각 이펙트 (예: `PoisonAura_Visual`, 향후 폭발/이펙트 등)
- 발사체, 데미지 텍스트, 픽업 아이템 등 반복 스폰되는 모든 것

## 워크플로

### 1) 사전 워밍 (권장)
`BattleController.Start` 등 진입점에서 알려진 풀 대상은 미리 `CreatePool` 로 인스턴스 비축.
```csharp
var prefab = await CHMResource.Instance.LoadAsync<GameObject>(EMonster.Slime);
if (prefab != null) CHMPool.Instance.CreatePool(prefab, count: 5);
```
첫 Pop 시 lazy Create 의 spike 를 방지.

### 2) Pop 으로 꺼냄
```csharp
var poolable = CHMPool.Instance.Pop(prefab, parent);
var go = poolable.gameObject;
go.transform.position = somePosition;
```

### 3) Push 로 반환
사망/만료 시 `Destroy` 대신 `Push`:
```csharp
var poolable = gameObject.GetComponent<CHPoolable>();
if (poolable != null) CHMPool.Instance.Push(poolable);
else                  Destroy(gameObject);   //# fallback
```

### 4) State Reset (필수)
풀에서 재사용된 GameObject 는 이전 상태를 보존. `OnEnable` / `OnDisable` 으로 명시 reset:
- `Health.OnEnable` — Current ≤ 0 이면 Max 로 복원
- `MeleeAttacker.OnEnable` — `_lastAttackTime` 리셋
- `HitFlash.OnEnable` — `_lastHp` 재캐시, 코루틴 정리, 색상 원복
- `HeroAuraRunner.OnDisable` — `_slots` 비우기 + 각 IHeroAura.OnDetached 호출
- 향후 신규 컴포넌트도 동일 패턴 적용

## 자식 GameObject 도 풀 사용
프리팹의 자식이 아닌 **동적 자식** 도 풀 사용. 예: `PoisonAura` 가 영웅에 부착되는 디스크 → `EVisual.PoisonAura` 프리팹 + `CHMPool.Pop` / `Push`.

## 예외 (제한적)
다음은 풀 비사용 허용:
- **씬에 사전 배치된 정적 오브젝트** (Floor, Light, UICanvas 등) — 스폰 아님
- **에디터 전용 디버그 GameObject** — 런타임 외
- **테스트 한정 GameObject** — `new GameObject("test")` 같은 단위 테스트 셋업

위 예외 이외엔 **모든 스폰은 CHMPool 사용**.

## 금지 예시
```csharp
//# (X) Instantiate 직접 호출
var go = Object.Instantiate(prefab, parent);

//# (X) CreatePrimitive 직접 (시각 이펙트 등)
var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
```

## 권장 예시
```csharp
//# (O) CHMPool.Pop / Push
var poolable = CHMPool.Instance.Pop(prefab, parent);
//# ... use ...
CHMPool.Instance.Push(poolable);   //# 사망/만료 시
```

## Rule 07 / 11 과의 관계
- Rule 07: ChvjPackage 기존 API 사용
- Rule 11: UI 는 CHText / CHButton / CHToggle
- Rule 12: **런타임 스폰** 은 CHMPool

## 워밍 가이드 (참고)
| 대상 | 권장 count |
|---|---|
| 영웅 | 1 |
| 자연 스폰 몬스터 | 3 + α (카드로 추가 소환되는 종은 +5) |
| 시각 이펙트 | 1~2 (동시 표시 가능성 따라) |
| 발사체 | 10~20 (DPS 높을수록 더 많이) |
