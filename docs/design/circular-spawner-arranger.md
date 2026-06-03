# 원형 스포너 배치 — Circular Spawner Arranger (기획서)

> 작성: game-designer · 2026-06-03
> 대상 버전: MVP / 프로토타입 범위
> 파이프라인: `start-develop-simple` (game-designer → gameplay-programmer → test-engineer, 리뷰·시뮬 생략)
> 입력: spec `docs/superpowers/specs/2026-06-03-circular-spawner-arranger-design.md` · plan `docs/superpowers/plans/2026-06-03-circular-spawner-arranger.md`

---

## § 헤더

- **목표**: 중앙 기준으로 스포너를 원형으로 균등 배치하는 에디터 도구. 반지름 + 몬스터 리스트를 입력하면 리스트 개수만큼 스포너가 원주 위 균등 각도로 배치되고, 각 스포너는 출력 몬스터 종의 색을 띤다.
- **검증 가설**: 기존 "메쉬 존 + 12 정적 스폰지점" 구조를 **임의 개수·임의 반지름의 원형 배치**로 대체하면, 스포너 구성 변경 비용이 낮아져 자동전투 밸런스 실험(스포너 종/개수/거리 조합) 반복이 빨라지는가.
- **현재 단계 범위 적합성**: **범위 내**. MVP §8 준수 — 비주얼은 프리미티브(Cylinder 디스크), 새 아트/사운드/메타 작업 없음. 기존 색상 테이블 재사용으로 컨셉 §11.4 비주얼 매핑 유지.
- **핵심 메커니즘**: 런타임 컴포넌트 `CircularSpawnerArranger` 는 설정값(반지름·몬스터 리스트·시작각) + 순수 각도 헬퍼만 보유. 실제 GameObject 생성·배치·색상·`BattleController` 재와이어링은 에디터 "Rebuild" 버튼이 수행 (Rule 03 §4 런타임 생성 금지 준수). 각도 분배는 시작각에서 360/N° 균등.

---

## 1. 좌표·각도 규약

탑다운 2.5D 뷰 기준. 원은 XZ 평면(y=0) 위에 그려진다.

| 항목 | 결정값 | 근거 |
|---|---|---|
| 원 중심 | `arranger.transform.position` | 컴포넌트가 붙은 GameObject 위치 = 원 중심 |
| 평면 | XZ (y = 중심의 y, 사실상 0) | 바닥 평면. 영웅·몬스터와 동일 평면 |
| 각도 0° 기준 | +X 축 (cos→x, sin→z) | 수학 표준 단위원. `90° = +Z = 탑다운 뷰 "위"` |
| 시작각 기본 | **90° (= +Z = 위)** | 첫 스포너가 화면 위쪽 중앙에 오게 — 직관적 시각 기준점 |
| 분배 방향 | 시작각에서 반시계(각도 증가) 방향으로 360/N° 씩 | 단위원 표준. 검증 표(§3)로 좌표 고정 |

좌표 산식 (단위원):
```
PositionOnCircle(center, radius, angleDeg)
  = center + (radius·cos(angleDeg), 0, radius·sin(angleDeg))
```
검산: `angleDeg = 90°` → `(cos90, sin90) = (0, 1)` → `center + (0, 0, radius)` = 중심 바로 위(+Z). 의도와 일치.

---

## 2. 반지름

| 항목 | 결정값 | 근거 |
|---|---|---|
| 기본값 `_radius` | **13** | 기존 `BattleZone` 경계 ±12 의 **1m 바깥**. 기존 12 스폰지점도 ±13 에 배치되어 있어 시각·전투 위상 동일 유지 |
| 권장 범위 | **10 ~ 16** | 하한 10 = 경계(12) 안쪽 → 스포너가 존 내부에 들어와 몬스터가 즉시 교전 상태로 스폰됨(원치 않는 위상). 상한 16 = 경계에서 4m 바깥 → 몬스터 진입 거리가 길어져 초반 압박 약화. 이 범위에서 기존 페이싱 유지 |
| 입력 처리 | 입력값 그대로 사용 (클램프 없음) | 프로토타입 — 실험을 위해 범위 밖 값도 허용. 권장 범위는 가이드일 뿐 강제 안 함 |

> **밸런스 곡선 재설계 아님** — 반지름은 "기존 ±13 위상 재현 + 실험 여지"가 목적. 13 을 기본으로 두면 기존 자동전투 페이싱이 그대로 유지된다. 권장 범위 밖 값의 페이싱 영향 측정은 본 도구 범위 밖(필요 시 별도 qa-simulator 사이클).

---

## 3. 균등 분배 규칙 — 360/N

스포너 개수 N 마다 각 간격 = `AngleStep(N) = 360/N` (N≤0 이면 0).
`i` 번째 스포너 각 = `시작각(90°) + 360/N × i`.

검증표 (시작각 90° 기준, 좌표는 반지름 r):

| N | 각 간격 | 각 스포너 각도 | 좌표 (cos, sin)·r 요지 |
|---|---|---|---|
| 1 | 360° | 90° | 위 1개: (0, r) |
| 2 | 180° | 90°, 270° | 위 (0, r), 아래 (0, -r) |
| 3 | 120° | 90°, 210°, 330° | 위 / 좌하 / 우하 (정삼각형) |
| 4 | 90° | 90°, 180°, 270°, 0° | 위 / 좌 / 아래 / 우 (정사각형) |
| 6 | 60° | 90°,150°,210°,270°,330°,30° | 정육각형 — 기존 6 스포너 구성 재현 가능 |

검산(N=4): 간격 360/4 = 90°. 시작 90° → 90, 180, 270, 360(=0). 네 점이 90° 등간격, 모두 중심에서 r 거리. 균등 사각형 일치.

균등성 정의: 인접 두 스포너 사이 각거리가 모든 쌍에서 동일(= 360/N), 모든 스포너가 중심에서 정확히 `_radius` 거리. (test-engineer 검증 항목과 일치 — plan §7.)

---

## 4. 색상 테이블 — 기존 6종 재사용 (새 색 정의 금지)

각 스포너 디스크 색 = 그 스포너의 출력 종(`_outputType`) 색. 기존 `LairSpawnerVisualBuilder.cs` 의 `SpawnerColorTable` 를 그대로 재사용한다. **새 색을 정의하지 않는다.**

| EMonster | Hex | 컨셉 §11.4 색 |
|---|---|---|
| Wisp | `#22C55E` | 초록 |
| Wraith | `#6B7280` | 회색 |
| Reaper | `#EF4444` | 빨강 |
| Hex | `#EAB308` | 노랑 |
| Plague | `#A855F7` | 보라 |
| Phantom | `#1F2937` | 검정(어두운 회색) |

이 테이블은 spec §3.3 에 따라 두 빌더(`LairSpawnerVisualBuilder`, `CircularSpawnerArrangerEditor`)가 공유하도록 `SpawnerColorPalette` static class 로 추출한다(plan Task 3). 색상표는 단 한 곳에만 정의 — 중복 정의 0건.

머티리얼: 기존과 동일 경로·동일 idempotent 생성 규칙 — `Assets/_Lair/Art/Materials/Mat_Spawner_{type}.mat` 6종, URP Lit 셰이더 `_BaseColor`.

디스크 형태: 기존 `SpawnerBody` 와 동일 — Cylinder, 스케일 (2.0, 0.05, 2.0), Collider 제거(전투 충돌 무영향). spawner-visual 기획서 §2.1 과 일치.

---

## 5. 엣지 케이스

| 입력 | 동작 | 근거 |
|---|---|---|
| 빈 리스트 (N=0) | 관리 스포너 자식 전부 제거 → 스포너 0개. `AngleStep(0)=0`, `ComputePositions` 빈 배열. `BattleController._spawners` = 빈 배열로 재와이어링 | 비어 있으면 전투에 스포너가 없는 상태(영웅이 무저항 돌파). 유효한 실험 상태로 허용 |
| 1개 (N=1) | 시작각(90°=위) 1개. `AngleStep(1)=360` 이나 항목 1개라 1점만 배치 | 단일 스포너 실험 |
| 중복 몬스터 (예: [Wisp, Wisp, Reaper]) | 각 리스트 항목이 **독립 스포너**. 같은 종 2개면 같은 색 디스크 2개가 다른 각도에 배치 | 같은 종 다중 스포너 = 물량 실험. 허용 |
| 음수/0 count 헬퍼 호출 | `AngleStep ≤0 → 0`, `ComputePositions ≤0 → 빈 배열` | 방어적 — Rebuild 가 빈 리스트와 동일 처리 |

Rebuild 는 **idempotent** — 같은 입력으로 여러 번 눌러도 결과 동일(매번 관리 스포너 전면 제거 후 재생성).

---

## 6. 스폰 주기 — Spawner 기본값 사용 (arranger 미포함)

arranger 는 **스폰 주기를 설정하지 않는다.** 생성되는 각 `Spawner` 는 컴포넌트 기본값 `_spawnPeriod = 9f` (초) 를 그대로 쓴다.

- 근거(YAGNI): 본 도구의 검증 가설은 "배치"(개수·거리·종)이지 "주기"가 아니다. 주기 튜닝은 기존 카드 효과(SpawnerHaste 등 `BattleController` 의 `_spawnPeriod ×mul` 경로)와 BalanceConfig 가 이미 담당한다. arranger 에 주기 필드를 더하면 단일 진실이 둘로 갈린다.
- 기본 9초는 기존 씬 스포너와 동일 → 배치 도구로 교체해도 스폰 페이싱 변화 없음.
- 주기 실험이 필요해지면 별도 기획으로 승격(현재 범위 밖).

---

## 7. 프로토타입 한계 (범위 밖 — 명시만, 이번에 다루지 않음)

spec §6 을 그대로 박제한다.

1. **스포너 상태 HUD 6셀 가정** — 기존 BattleHud 는 `Spawners` 배열을 받아 스포너 상태를 표시하며 6셀 레이아웃을 가정한다(`BattleController` 가 `Spawners = _spawners` 로 전달). arranger 가 6개가 아닌 임의 개수를 배치하면 HUD 셀 매핑이 깔끔하지 않을 수 있다(셀 부족/과잉). HUD 동적 셀 대응은 이번 범위 밖 — N=6 구성에서 가장 안전.
2. **영웅 진입 지점·존 클램프 불변** — `BattleZone._heroEntryPoint` / `_spawnPoints` / `ClampInside` 경계(±12)는 이번 작업에서 변경하지 않는다. arranger 는 스포너만 다룬다. 반지름을 경계 안쪽(<12)으로 주면 스포너가 존 내부에 들어가는 위상 변화가 생기므로 §2 권장 범위(10~16, 실질 13 기본) 준수 권장.
3. **임시값 허용** — 프로토타입 범위이므로 반지름·시작각은 임시값. 정밀 밸런스·시너지 표는 본 기획서에서 생략(plan Task 1 허용).

---

## 8. 구현 요청사항 (gameplay-programmer 용)

> 코드 구조·시그니처는 plan Task 2~4 에 이미 박제되어 있다. 본 절은 도메인 결정값 + 명명 계약만 확정한다.

**Enum**
- 신규 Enum 값 추가 **없음**. 출력 종은 기존 `Lair.Data.EMonster` (Wisp/Wraith/Reaper/Hex/Plague/Phantom) 를 그대로 사용.
- arranger 는 씬에 사전 배치/에디터 생성되는 정적 오브젝트 → Addressables 키(EVisual 등) 추가 없음.

**Interface**
- 신규 Interface **없음**. arranger 는 설정 + 순수 정적 헬퍼만 보유. 생성되는 `Spawner` 는 기존 계약(`ISpawnerProgress` 등) 그대로.

**컴포넌트 / 파일** (plan File Structure 와 동일)
| 파일 | 책임 | namespace / asmdef |
|---|---|---|
| `Assets/_Lair/Scripts/Battle/CircularSpawnerArranger.cs` (신규) | 설정(`_radius`/`_monsters`/`_startAngleDeg`) + 순수 정적 헬퍼 | `Lair.Battle` / Lair |
| `Assets/_Lair/Editor/SpawnerColorPalette.cs` (신규) | `EMonster→hex` 테이블 + 머티리얼 생성/로드 공유 헬퍼 (기존 빌더에서 추출) | Editor asmdef |
| `Assets/_Lair/Editor/LairSpawnerVisualBuilder.cs` (수정) | 색상표·머티리얼을 `SpawnerColorPalette` 참조로 변경 (중복 정의 제거) | Editor asmdef |
| `Assets/_Lair/Editor/CircularSpawnerArrangerEditor.cs` (신규) | 커스텀 인스펙터 "Rebuild" — 스포너 생성/배치/색상/`BattleController._spawners` 재와이어링 | Editor asmdef |

> 참고: 현재 `LairSpawnerVisualBuilder.cs` 는 namespace `Lair.EditorTools` 다. spec/plan 표기(`Editor`)는 asmdef(Editor) 를 가리키며 namespace 와 무관 — 추출 `SpawnerColorPalette` 는 기존 빌더와 동일 namespace(`Lair.EditorTools`) 에 두어 양쪽이 참조하게 한다.

**직렬화 필드 (CircularSpawnerArranger) — 도메인 기본값 확정**
| 필드 | 타입 | 기본값 | 의미 |
|---|---|---|---|
| `_radius` | `float` | **13f** | 원 반지름(§2) |
| `_monsters` | `List<EMonster>` | 빈 리스트 | 항목 1개 = 스포너 1개, 중복 허용(§5) |
| `_startAngleDeg` | `float` | **90f** | 첫 스포너 시작각(+Z=위, §1) |

**순수 정적 헬퍼 — 명명 계약 (본문 전체 동일 표기)**
- `static float AngleStep(int count)` → `count<=0 ? 0 : 360/count`
- `static Vector3 PositionOnCircle(Vector3 center, float radius, float angleDeg)` → 단위원 (cos→x, sin→z)
- `static Vector3[] ComputePositions(Vector3 center, float radius, int count, float startDeg)` → count 개, `startDeg + AngleStep(count)·i`. count≤0 빈 배열
- 읽기 전용 프로퍼티: `Radius` / `Monsters` (`IReadOnlyList<EMonster>`) / `StartAngleDeg`

**Rebuild 동작 계약 (에디터)**
1. 관리 스포너 자식 전면 제거 (idempotent)
2. `_monsters[i]` 마다 `Spawner` GameObject 생성(`Spawner_{type}_{i}`) + `_outputType=type`(SerializedObject) + 위치 `ComputePositions(center, _radius, count, _startAngleDeg)[i]` + `SpawnerBody` Cylinder 디스크(2.0,0.05,2.0)·`SpawnerColorPalette` 머티리얼
3. `BattleController._spawners` 를 새 배열로 재와이어링(SerializedObject, `FindFirstObjectByType<BattleController>`) — 누락 시 스포너 Tick 안 됨
4. 씬 dirty + 저장

**SO 스키마 / 수치**
- 새 SO **없음**. 수치(반지름 13·시작각 90)는 컴포넌트 직렬화 필드 기본값으로 인라인.
- 스폰 주기 필드 **없음** — Spawner 기본 9초 사용(§6).

**머티리얼**
- `Mat_Spawner_{type}.mat` 6종, `Assets/_Lair/Art/Materials/`, URP Lit `_BaseColor`. 기존 idempotent 생성 규칙 동일. 새 색 정의 없음(§4).

---

## 9. MVP 범위 확인

| 항목 | 범위 |
|---|---|
| 원형 스포너 배치 도구(런타임 설정 + 에디터 Rebuild) | MVP 내 — 실험 도구, 기존 정적 구조 대체 |
| 프리미티브 Cylinder 디스크 비주얼 | MVP 내 (컨셉 §11.4 프리미티브 방침) |
| 기존 6종 색상 재사용 | MVP 내 (새 색·새 아트 없음) |
| HUD 동적 셀(임의 개수 대응) | MVP 외 — §7 한계로 명시, 별도 기획 |
| 스폰 주기 설정 | MVP 외 — §6 YAGNI |
| 영웅 진입/존 클램프 변경 | MVP 외 — §7 한계 |
| 사운드 / 새 아트 에셋 / 메타 | MVP 외 — §8 비작업 |

---

## 변경 이력

- **v0.1 (2026-06-03)**: 초안. spec/plan 기준 도메인 값 확정 — 반지름 기본 13(권장 10~16), 시작각 90°(+Z=위), 360/N 균등 분배, 기존 6종 색상 재사용, 엣지(0/1/중복), 스폰 주기 Spawner 기본 9초(arranger 미포함), 프로토타입 한계(HUD 6셀 가정·영웅/존 불변) 명시.
