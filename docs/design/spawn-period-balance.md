# 스폰 주기 BalanceConfig 이관 — Spawn Period Balance

> 단계: MVP / 프로토타입 범위
> 파이프라인: `start-develop-simple` (game-designer → gameplay-programmer → test-engineer)
> 입력 배경: 원형 스포너 배치 도구(`docs/superpowers/specs/2026-06-03-circular-spawner-arranger-design.md`) §6 — arranger 의 `List<EMonster>` 리스트는 주기를 Spawner 기본값(9초)으로 둔다(YAGNI). 본 기획서가 그 주기를 BalanceConfig 로 이관해 종별 자동 적용을 채운다.

---

## § 헤더

- **목표**: 스포너 스폰 주기를 몬스터 종별로 `BalanceConfig`(밸런스 단일 진실)에 데이터로 두고, 전투 시작 시 각 스포너의 출력 종에 맞는 주기를 런타임 자동 주입한다.
- **검증 가설**: 본 작업은 밸런스 가설 검증이 아니라 **데이터 단일화(SoT) 리팩터**다 — "스폰 주기가 씬 프리팹에 흩어져 있지 않고 BalanceConfig 한 곳에서 종별로 관리되며, 원형 arranger 의 `List<EMonster>` 만으로도 올바른 주기가 자동 따라오는가"를 확인한다.
- **현재 단계 범위 적합성**: 범위 내. MVP §8 (비주얼/사운드/메타/메인메뉴 금지) 위반 없음 — 데이터 스키마 + 주입 경로만 다룬다. 새 카드/시스템 없음.
- **핵심 메커니즘**: `MonsterStatRow` 에 `float SpawnPeriod` 형제 필드 추가 → `BattleController.BindSpawners()` 가 각 Spawner 의 `CurrentType` 으로 BalanceConfig 에서 base 주기를 조회해 주입 → 카드의 `ScalePeriod` 곱연산은 주입된 base 위에 적용.

---

## 1. 스키마 위치 결정 — `MonsterStatRow` 직속 `float SpawnPeriod`

### 대안 비교

| 안 | 위치 | trade-off |
|---|---|---|
| **A (권장)** | `MonsterStatRow.SpawnPeriod` (Stat 의 형제 필드) | 스폰 주기는 캐릭터 능력치가 아니라 **스포너 생산 설정**이다. `CharacterStat`(Hp/Power/Range/Cooldown/MoveSpeed)은 "이 종이 전투에서 어떻게 행동하는가"이고, 주기는 "이 종이 얼마나 자주 생산되는가" — 책임이 다름. `ApplyMonsterStats` 는 `CharacterStat` 만 캐릭터 인스턴스에 바르므로, 주기를 Stat 에 넣으면 캐릭터에 무의미하게 딸려 가고 스포너만 따로 꺼내 써야 하는 비대칭이 생긴다. 직속 필드면 조회 API 한 줄(`GetSpawnPeriod(key)`)로 분리. JsonSync DTO 도 `MonsterStatRowDto` 에 `spawnPeriod` 한 줄 추가로 끝. |
| B | `CharacterStat.SpawnPeriod` | 행(row) 구조 변경 없이 기존 Stat 직렬화 안에 흡수 → DTO 의 `CharacterStatDto` 한 곳만 수정. 그러나 `ApplyStats`/`ApplyMonsterStats` 가 Stat 을 캐릭터에 적용하는 경로에 스포너 전용 값이 섞여 의미 오염. 영웅(`_hero`)도 `CharacterStat` 인데 영웅에는 스폰 주기 개념이 없어 의미 없는 필드가 생긴다. |
| C | 별도 `SpawnPeriodRow[]` 신규 배열 | 가장 명확한 책임 분리지만, `MonsterStatRow` 와 키(EMonster) 1:1 중복 배열을 새로 만들게 되어 키 동기화 부담 2배. MVP 프로토타입에 과한 구조. |

### 결정: 안 A

`MonsterStatRow` 에 `public float SpawnPeriod;` 를 `CharacterStat Stat` 의 형제 필드로 추가한다.

```
[Serializable]
public class MonsterStatRow
{
    public EMonster      Key;
    public CharacterStat Stat;
    public float         SpawnPeriod;   //# 신규 — 이 종을 출력하는 스포너의 base 스폰 주기(초)
}
```

근거 요약: 주기는 스탯이 아니라 스포너 설정이므로 Stat 바깥, 그러나 키 중복 배열을 새로 만들 만큼 크지 않으므로 기존 행에 흡수 — 안 B(의미 오염)와 안 C(중복 배열) 사이의 최소 비용 지점.

조회 API: `BalanceConfig` 에 `public float GetSpawnPeriod(EMonster key)` 를 추가한다. 미발견 시 fallback 9f 반환 + `Debug.LogWarning` (기존 `GetMonster` 의 미발견 처리 패턴과 동일). fallback 9f 는 현행 `Spawner._spawnPeriod` 인스펙터 기본값과 동일해 회귀 시 동작 변화 0.

---

## 2. 기본값 확정

현재 씬 6개 스포너의 주기를 그대로 기본값으로 확정한다. 본 작업은 데이터 이관이므로 값 변경 없음.

| 몬스터(EMonster) | SpawnPeriod (초) |
|---|---|
| Wisp    | 9  |
| Reaper  | 12 |
| Phantom | 6  |
| Plague  | 10 |
| Wraith  | 20 |
| Hex     | 15 |

> EMonster 6종 = 컨셉 §11.3 MVP 몬스터 6종(위스프/레이스/리퍼/헥스/플레이그/팬텀)과 일치. 검산: 표 6행 = MVP 6종. 누락/초과 0.

### 밸런스 의미 검토 (컨셉 §8 대조)

- 컨셉 §8 밸런싱 기준: "영웅이 2~4분 사이에 죽도록 튜닝". 스폰 주기는 DPS 공급량을 직접 좌우하는 밸런스 변수이긴 하나, **본 작업은 현재 씬 값을 1:1 이관**하므로 전투 결과에 변화를 주지 않는다 → §8 기준과 어긋남 없음.
- 단, 데이터 관점 지적 1건 (조정 아님, 기록만): Phantom 6s(가장 빠름) ↔ Wraith 20s(가장 느림)의 3.3배 격차는 컨셉 §11.3 의도(팬텀=떼로 몰림, 레이스=강력한 벽)와 방향 일치 — 머릿수 종은 자주, 벽 종은 드물게. 의미상 정합하므로 이관 그대로 진행.
- 향후 이 주기들의 밸런스 적정성(영웅 2~4분 사망 곡선) 검증이 필요하면 별도 "밸런스 조정 흐름"(qa-simulator → game-designer)으로 분리한다. 본 기획서 범위 밖.

---

## 3. 자동 적용 경로

### 3.1 적용 지점 — `BattleController.BindSpawners()`

`BindSpawners()` 는 전투 시작 직후 1회, `_spawners` 를 순회하며 각 스포너에 `sp.Bind(this, _zone)` 를 호출한다. 여기에 base 주기 주입을 추가한다.

순회 시 각 스포너의 출력 종(`sp.CurrentType`)으로 `_balance.GetSpawnPeriod(...)` 를 조회해 `sp.SetBasePeriod(period)` 로 주입한다.

```
//# 의사 흐름 (구현은 gameplay-programmer)
foreach (Spawner sp in _spawners)
{
    if (sp == null) continue;
    sp.Bind(this, _zone);
    if (_balance != null)
        sp.SetBasePeriod(_balance.GetSpawnPeriod(sp.CurrentType));
}
```

- `sp.CurrentType` 는 `Spawner.OnEnable()` 에서 `_outputType` 으로 이미 초기화돼 있으므로 BindSpawners 시점에 유효하다. (OnEnable 은 BindSpawners 보다 먼저 — 씬 활성화 시점.)
- `_balance == null` 이면 주입을 건너뛴다 → 스포너는 인스펙터 `_spawnPeriod` 기본값(9f) 유지 (기존 동작 보전). `BattleController.Start` 가 이미 `_balance == null` 시 LogError 를 찍으므로 추가 경고 불필요.

### 3.2 Spawner 에 base period setter 추가 — `SetBasePeriod(float)`

`Spawner` 는 현재 `ScalePeriod(float mul)`(곱연산 누적)과 `SpawnPeriod`(read-only getter)만 노출한다. base 를 절대값으로 세팅하는 진입점이 없다. `SetBasePeriod(float period)` 를 신규 추가한다.

```
//# 의사 시그니처 (구현은 gameplay-programmer)
public void SetBasePeriod(float period)
{
    //# 음수/0 입력은 무시 (안전 가드). 최소 주기 0.05s 클램프 — ScalePeriod 와 동일 하한.
    if (period <= 0f) return;
    _spawnPeriod = Mathf.Max(0.05f, period);
}
```

### 3.3 적용 순서 — base 주입 → 카드 ScalePeriod 곱연산 (중요)

| 순서 | 시점 | 동작 | `_spawnPeriod` 결과 예시 (Phantom) |
|---|---|---|---|
| 1 | 씬 활성화 (`OnEnable`) | 인스펙터 기본값 | 9 (인스펙터 직렬화 값) |
| 2 | 전투 시작 (`BindSpawners`) | `SetBasePeriod(6)` 주입 | 6 (BalanceConfig 값으로 덮어씀) |
| 3 | 카드 픽 (`SpawnerHaste` ×0.8 등) | `ScalePeriod(0.8)` 곱연산 | 4.8 (= 6 × 0.8) |
| 4 | 추가 카드 (`FastBreeding` ×0.6 등) | `ScalePeriod(0.6)` 곱연산 | 2.88 (= 4.8 × 0.6) |

- **base 주입(2)은 반드시 카드 곱연산(3,4)보다 먼저**여야 한다. BindSpawners 는 전투 시작 직후 1회, 카드 픽은 그 이후(HP%/시간 트리거)이므로 시간 순서상 자연히 보장된다 — 별도 게이트 불필요.
- `SetBasePeriod` 는 절대 대입, `ScalePeriod` 는 곱연산 — 둘은 다른 연산이므로 충돌 없음. base 가 9→6 으로 바뀌어도 이후 누적된 카드 배율이 base 에 재적용되는 일은 없다(카드 효과는 1회성 곱연산이 그 시점 `_spawnPeriod` 에 즉시 반영되고 끝).
- **회귀 주의**: base 주입이 `ScalePeriod` 이후에 잘못 호출되면 카드 효과(곱연산 누적분)가 base 절대 대입으로 날아간다. BindSpawners 1회 + 전투 시작 직후라는 위치를 변경하지 않는 한 안전. gameplay-programmer 는 `SetBasePeriod` 호출을 BindSpawners 외 다른 경로(특히 카드 효과 처리 경로)에서 호출하지 않는다.

### 3.4 ReplaceOutput 시 base 주기 재조회 — 범위 제외 (명시)

융합 카드(`ReplaceSpawnerOutput`)로 스포너 출력 종이 바뀌면(예: Wisp 9s → Plague 10s) 새 종의 BalanceConfig 주기로 갱신할지 여부가 제기될 수 있다. **본 작업 범위 밖** — 사유:

- 현행 `ReplaceOutput` 은 `_spawnPeriod` 를 건드리지 않는다(종만 변경, 주기는 그 스포너 슬롯에 귀속 유지). 이 동작은 spec/카드 리뉴얼에서 의도된 것(스포너 슬롯의 누적 카드 효과 보존).
- 이관 작업이 기존 동작(종 변경 시 주기 유지)을 바꾸면 회귀가 된다. 데이터 이관 범위를 넘는 동작 변경이므로 제외.
- 향후 "종 교체 시 새 종 주기로 리셋" 이 필요하면 별도 기획으로 승격한다.

---

## 4. 딜레이 제외 확인

`Spawner._initialDelay` 는 현재 **비동작**이다 — `Spawner.cs` §주석(line 15~18) 및 `Tick`(line 102~108)에서 확인: 첫 스폰은 `_initialDelay` 와 무관하게 첫 Tick(t≈0)에 즉시 발사되고, `_initialDelay` 는 씬 직렬화 churn 방지를 위해 필드만 보존된 no-op 이다.

따라서 **스폰 시작 딜레이는 BalanceConfig 에 넣지 않는다.** 동작하지 않는 값을 단일 진실에 올리면 "데이터는 있는데 효과가 없다"는 거짓 SoT 가 된다. `_initialDelay` 가 향후 실제 위상 오프셋 기능으로 부활하면 그때 별도 이관 기획으로 다룬다. 본 기획서는 `SpawnPeriod`(주기) 단일 필드만 이관한다.

---

## 5. 구현 요청사항 (gameplay-programmer 용)

> 코드는 작성하지 않는다. 아래 명세를 gameplay-programmer 가 구현한다.

### 5.1 데이터 스키마 (`Assets/_Lair/Scripts/Data/BalanceConfig.cs`)

- `MonsterStatRow` 에 필드 추가: `public float SpawnPeriod;` (`CharacterStat Stat` 의 형제)
- `BalanceConfig` 에 조회 API 추가:
  - `public float GetSpawnPeriod(EMonster key)` — `_monsters` 순회로 `row.Key == key` 행의 `SpawnPeriod` 반환. 미발견 시 `9f` 반환 + `Debug.LogWarning($"[BalanceConfig] 스폰 주기 미발견: {key}")` (기존 `GetMonster` 패턴 일치).

### 5.2 Spawner setter (`Assets/_Lair/Scripts/Battle/Spawner.cs`)

- `public void SetBasePeriod(float period)` 추가 — `period <= 0f` 가드 후 `_spawnPeriod = Mathf.Max(0.05f, period)`. (절대 대입. 곱연산 `ScalePeriod` 와 별개.)

### 5.3 주입 경로 (`Assets/_Lair/Scripts/Battle/BattleController.cs`)

- `BindSpawners()` 의 `_spawners` 순회 루프에서 `sp.Bind(this, _zone)` 직후, `_balance != null` 이면 `sp.SetBasePeriod(_balance.GetSpawnPeriod(sp.CurrentType))` 호출.

### 5.4 SO 데이터 입력 (`Assets/_Lair/Data/BalanceConfig.asset`)

- 6개 `MonsterStatRow` 의 `SpawnPeriod` 에 §2 표 값 입력 (Wisp 9 / Reaper 12 / Phantom 6 / Plague 10 / Wraith 20 / Hex 15).
- (입력 후 씬의 6개 Spawner 인스펙터 `_spawnPeriod` 는 더 이상 단일 진실이 아니다 — BindSpawners 가 덮어쓰므로 씬 값은 fallback 표시용. 씬 값 변경/통일은 선택, 본 작업 필수 아님.)

### 5.5 JsonSync 갱신 (DTO + Syncer)

- `Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs` — `MonsterStatRowDto` 에 필드 추가:
  - `[JsonProperty("spawnPeriod")] public float SpawnPeriod;`
- `Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs`:
  - **Export** (`ExportToJson`): `MonsterStatRowDto` 생성 시 `SpawnPeriod = <해당 행의 SpawnPeriod>` 채움. 단, 현행 Export 는 `config.GetMonster(monster)` 로 Stat 만 조회하므로 주기 조회를 위해 `config.GetSpawnPeriod(monster)` 를 함께 호출해 채운다.
  - **Import** (`ApplyDto`): `monstersProp.GetArrayElementAtIndex(i)` 의 `row.FindPropertyRelative("SpawnPeriod").floatValue = rowDto.SpawnPeriod;` 한 줄 추가.
- JSON 파일(`Assets/_Lair/Data/Json/balance_config.json`)은 Export 1회 실행 시 자동 재생성되므로 수기 편집 불필요.

### Enum / Interface / 에셋 키

- **신규 Enum**: 없음 (기존 `EMonster` 6종 재사용).
- **신규 Interface**: 없음.
- **에셋 키**: 없음 (신규 프리팹/SO 없음). `BalanceConfig.asset` 데이터 필드 추가만.

---

## 6. Self-Review

- **스펙 커버리지**: 입력은 사용자 요구 5항목 직접 매핑 — ① 스키마 위치(§1) ② 값(§2) ③ 자동 적용 경로(§3) ④ 딜레이 제외(§4) ⑤ JsonSync 영향(§5.5). 갭 0.
- **내부 일관성**: SpawnPeriod 값(9/12/6/10/20/15)이 §2 표 = §5.4 입력 지시에서 동일. fallback 9f 가 §1·§5.1·§3.1 에서 일관.
- **시그니처 일관성**: `SetBasePeriod`(절대 대입), `ScalePeriod`(곱연산, 기존), `GetSpawnPeriod`(조회), `SpawnPeriod`(필드명 = read-only getter명 = DTO JsonProperty 의미) — 본문 전체에서 동일 표기. `_spawnPeriod`(private 필드) 표기 일관.
- **모호 표현**: "또는/적절히/TBD" 류 0건. 스키마 위치는 안 A 단일 확정, 값은 표 확정.
- **스코프**: 단일 구현 단위(스키마 1필드 + setter 1 + 주입 1 + DTO/Syncer + SO 데이터). 분할 불필요.
- 결과: 통과 (보강 0).
