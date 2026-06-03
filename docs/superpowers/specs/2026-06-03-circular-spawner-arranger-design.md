# 원형 스포너 배치 — Circular Spawner Arranger (Spec)

- 날짜: 2026-06-03
- 단계: MVP / 프로토타입 범위
- 파이프라인: `start-develop-simple` (game-designer → gameplay-programmer → test-engineer, 리뷰·시뮬 생략)

## 1. 의도 / 한 줄

중앙을 기준으로 스포너를 **원형으로 균등 배치**하는 도구. 반지름과 몬스터 리스트를 입력하면, 리스트 개수만큼 스포너가 원주 위에 균등 각도로 배치되고 각 스포너는 지정 몬스터 색상을 띤다.

기존 "메쉬 존 + 12 스폰지점" 정적 구조를 대체한다.

## 2. 범위 (결정 락)

| 결정 | 값 |
|---|---|
| 스크립트 형태 | 런타임 `MonoBehaviour` + 에디터 "Rebuild" 버튼 |
| 배치 대상 | 실제 기능 `Spawner` (`_outputType` 설정) + 색상 `SpawnerBody` 디스크 |
| 기존 처리 | Rebuild 시 관리 대상 스포너 **전면 교체** (구 12 스폰지점 구조 폐기) |
| 색상 소스 | 기존 `LairSpawnerVisualBuilder` 의 `EMonster→색상 hex` 테이블 재사용 (공유 헬퍼로 추출) |
| 각도 규약 | +Z(위)에서 시작, 균등 분배 (N개 → 360/N°) |

## 3. 구성요소

### 3.1 `CircularSpawnerArranger : MonoBehaviour` (런타임, `Lair.Battle`)

원의 중심 역할 — 컴포넌트의 `transform.position` 이 원 중심.

인스펙터 직렬화:
- `[SerializeField] float _radius = 13f` — 반지름(입력값 그대로 사용)
- `[SerializeField] List<EMonster> _monsters` — 각 항목 = 스포너 1개. 리스트로 늘림. 중복 허용
- `[SerializeField] float _startAngleDeg = 90f` — 첫 스포너 시작각(기본 +Z=위)
- (관리 추적용) 생성된 스포너 참조 — 자식 탐색 또는 `[SerializeField] Spawner[] _managed` 중 택1 (구현 재량)

순수 정적 헬퍼 (Unity 비의존 — 테스트 대상):
- `static float AngleStep(int count)` → `count <= 0 ? 0 : 360f / count`
- `static Vector3 PositionOnCircle(Vector3 center, float radius, float angleDeg)` → `center + (cos, 0, sin) * radius`
- `static Vector3[] ComputePositions(Vector3 center, float radius, int count, float startDeg)` → count 개 균등 배치 좌표

### 3.2 `CircularSpawnerArrangerEditor : Editor` (Editor asmdef)

커스텀 인스펙터에 "Rebuild" 버튼. GameObject 생성을 Editor asmdef 에 둬 Rule 03 §4(런타임 `CreatePrimitive`/`Instantiate` 금지)를 위반하지 않는다.

Rebuild 동작:
1. 이전에 만든 관리 스포너 자식 전부 제거 (전면 교체, idempotent)
2. `_monsters[i]` 마다:
   - `Spawner` GameObject 생성 (`Spawner_{type}_{i}`), 컴포넌트 부착
   - `_outputType = type` 설정 (SerializedObject)
   - `transform.position = ComputePositions(...)[i]` (y=0 평면)
   - `SpawnerBody` 색 디스크 자식 부착 — 색상/머티리얼은 공유 헬퍼로
3. **`BattleController._spawners` 를 새 배열로 재와이어링** (SerializedObject) — 이게 빠지면 스포너가 Tick 안 됨
4. 씬 dirty + 저장

### 3.3 색상 헬퍼 공유

`LairSpawnerVisualBuilder` 의 `SpawnerColorTable` + `EnsureSpawnerMaterials` 를 공유 가능한 형태로 추출(예: editor util static class)해 두 빌더가 같은 테이블을 쓰도록 한다. 색상표 중복 정의 금지.

## 4. 각도 규약

- +Z(탑다운 뷰 "위")에서 시작, `_startAngleDeg` 만큼 회전한 지점이 첫 스포너
- N개 → 360/N° 간격 균등 분배
- 검증: 2개 → 90°/270° (180° 간격), 3개 → 120° 간격, 4개 → 90° 간격

## 5. 엣지 케이스

- 빈 리스트 → 관리 스포너 전부 제거, 스포너 0개
- 1개 → 시작각에 스포너 1개 (360/1)
- 중복 몬스터 → 각 항목이 독립 스포너 (허용)
- count ≤ 0 → `AngleStep` 0 반환, 위치 배열 빈 배열

## 6. 프로토타입 한계 (범위 밖 — 명시만)

- 스포너 상태 HUD 는 기존 6셀 가정 — 임의 개수에선 매핑이 깔끔하지 않을 수 있음 (이번 범위 밖)
- `_spawnPeriod` 는 `Spawner` 기본값(9초) 사용 — arranger 에서 주기 설정 미포함 (YAGNI)
- 영웅 진입 지점(`_heroEntryPoint`) / `BattleZone` 경계 클램프는 이번 작업에서 변경하지 않음

## 7. 테스트 (test-engineer)

순수 헬퍼 EditMode 테스트:
- `AngleStep(2)=180`, `AngleStep(3)=120`, `AngleStep(4)=90`, `AngleStep(0/음수)=0`
- `PositionOnCircle` — 반지름·각도별 좌표 정확도 (대표 각 0/90/180/270)
- `ComputePositions` — 개수 일치, 균등 간격(인접 점 각거리 동일), 모든 점이 center 에서 radius 거리
- 엣지 — count 0 → 빈 배열, count 1 → 1개

## 8. 룰 준수

- Rule 02: `//#` 주석, `var` 금지, `!` 금지, 가드 절, MVVM(해당 시)
- Rule 03 §4: 런타임 asmdef 에서 `CreatePrimitive`/`Instantiate` 금지 → GameObject 생성은 Editor asmdef
- Rule 04: 에셋/머티리얼은 `Art/Materials/`, 프리미티브 도형 고정
- MVP §8: 비주얼은 프리미티브, 아트·사운드·메타 작업 없음
