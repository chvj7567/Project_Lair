# BattleZone 시스템 + 영웅 entry / 몬스터 march — 기능 기획서

> 작성: game-designer · 2026-05-29
> 대상 버전: MVP
> 입력 스펙: `docs/superpowers/specs/2026-05-29-battlezone-design.md` (결정 락, B1/B2/B3 정정 반영)
> 입력 플랜: `docs/superpowers/plans/2026-05-29-battlezone.md` (파일·시그니처·Task 단계, B1/B2/B3 정정 반영)
> 현재 문서 버전: **v1.2** (design-reviewer 3차 BLOCKER 정정 — (1) §3.3 / §4.2 / §13 sync 표의 spawn 좌표 ±14 잔존 표현을 ±14.4 단일 단정으로 통일, (2) §13 sync 표에 spec 본문 v1 인비저블 벽 잔존 결정의 일괄 갱신 완료 후속 작업 한 줄 명시. v1.1 의 B1/B2/B3 정정 결과 (§6 영웅 차단 = SimpleMover._clampZone, BattleZone 본체 단순화) 는 그대로 유지)

---

## § 헤더

- **목표**: 카메라 가시영역과 전장 경계를 `BattleZone` BoxCollider 한 곳에 묶고, 몬스터는 zone 밖 spawn point 에서 태어나 zone 진입 후에만 영웅 AI 타겟 후보가 되며, 영웅은 zone 밖 한 지점에서 등장해 중심까지 행진 → 도달 즉시 5분 타이머 + 모든 Spawner Tick 이 한 박자에 시작한다.
- **검증 가설**: "스폰 비가시 + 영웅 등장 행진 + 5분 타이머 동기 시작" 이 던전 주인 시점에 침입자가 들어오는 한 호흡을 만들고, 카드 픽 페이싱 (컨셉 §2.4 위상 오프셋) 의 첫 박자(0초)를 또렷하게 한다.
- **현재 단계 범위 적합성**: MVP 범위 내 (컨셉 §11 — 5분 자동전투 코어 / 프리미티브 비주얼 유지 / 사운드·메타·메인메뉴 비작업). Marching 전용 VFX·SFX 는 본 기획서 범위 외 (스펙 §2.2 후속).
- **핵심 메커니즘**:
  1. **단일 BattleZone**: 씬에 BoxCollider(isTrigger) 1개 + 자식 Transform (spawn points × 12 + HeroEntryPoint × 1). 인비저블 벽 / Rigidbody 자동 생성 없음 (v1.1 B1/B2 정정).
  2. **몬스터 상태머신**: `Marching → Engaging` 단방향. `BattleZone.OnTriggerEnter` 가 `MonsterTag` 컬라이더 진입 시 `CharacterRegistry.SetMonsterEngaging(true)` 호출. 영웅 AI 의 nearest-target 검색은 `IsEngaging == true` 만 후보.
  3. **영웅 entry 시퀀스**: 영웅이 `HeroEntryPoint` 에 스폰 → `HeroEntryDriver` 가 `BattleZone.Center` 까지 이동 → 도달 시 `OnHeroReachedCenter` 발행 → `BattleClock.Start()` + Spawner Tick 게이트 활성.
  4. **영웅 zone 차단**: 영웅 `SimpleMover._clampZone` 런타임 주입 (`BattleController.SpawnHero` 의 Pop 직후 `SimpleMover.BindClampZone(_zone)` 호출 — §12.B). `FixedUpdate` 의 next 좌표를 `BattleZone.ClampInside` 로 X/Z 클램프. 몬스터는 `_clampZone` 미주입 (null) → 무동작 (v1.1 B1 정정).

---

## 1. 개요

스펙과 플랜이 골격(파일·시그니처·Task)을 락한다. 본 기획서는 다음 도메인 결정을 채운다.

| 결정 영역 | 본 기획서 § | 비고 |
|---|---|---|
| Zone BoxCollider 크기 | §2 | 카메라 frustum 산출 + 사용자 결정 게이트 |
| Spawn point 거리 (zone 밖 m) | §3 | 몬스터 moveSpeed 기준 결정 (max 채택) |
| Spawn point 개수 / 좌표 | §4 | 12 채택 + 그 근거 |
| Hero entry 거리 / 동선 / 페이싱 | §5 | 영웅 moveSpeed 기준 행진 시간 |
| 영웅 zone-clamp (SimpleMover._clampZone) | §6 | v1.1 B1 정정 — 인비저블 벽 자동 생성 폐기, SimpleMover 옵션 채택 |
| BattleClock 시작 페이싱 (entry → 첫 spawn) | §7 | 컨셉 §2.4 위상 오프셋과 정합 |
| 디버그 Gizmo | §8 | 본 기획서 권장 (스펙 §3.4 — 플랜 미커버, MVP 편의) |

스펙이 락한 결정 (BattleZone 본체 BoxCollider 직접 부착 / OnTriggerEnter 만으로 상태 전환 / 영웅 entry 4단계 / Marching 중 데미지 수용) 은 변경하지 않는다. v1.1 정정으로 "4면 자동 벽" 결정은 폐기 — §6 참조.

---

## 2. Zone BoxCollider 크기 — 카메라 frustum 산출

### 2.1 카메라 사양 (확인)

`Assets/_Lair/Scenes/Battle.unity` 의 Main Camera 직접 확인:

| 항목 | 값 |
|---|---|
| projection | Perspective |
| FOV (vertical) | 60° |
| Position (local) | (0, 22, -20) |
| Rotation (Euler) | (50°, 0°, 0°) (Quaternion (0.4226, 0, 0, 0.9063) = sin 25°/cos 25° → 2×25=50° X 회전) |
| Aspect (16:9 가정) | 1.778 |
| `BattleCamera._minZoomDistance` | 20 (forced 시작 줌인 한계) |

### 2.2 지면 가시영역 산출

`BattleCamera.Start` 가 카메라 위치를 앵커 + (-forward) × _minZoomDistance 로 강제 → 줌인 한계에서 카메라가 항상 동일 위치 보장. 그 위치 기준 지면 가시영역 계산:

- forward = (0, -sin 50°, cos 50°) ≈ (0, -0.766, 0.643)
- 앵커(forward 가 y=0 과 만나는 지점) = (0, 0, -1.54) ← 카메라 pos (0, 22, -20) + forward × t (t = 22 / 0.766 ≈ 28.72)
- 줌인 한계 카메라 실제 pos = 앵커 + (-forward) × 20 = (0, 15.32, -14.40)
- 수직 frustum half-angle = FOV/2 = 30° → 상단 ray = 화면 중심 ray 보다 30° 위, 하단 ray = 30° 아래
- 카메라 ray 의 지면 hit (화면 중심) = 앵커 (0, 0, -1.54)
- 상단 ray 가 지면과 만나는 Z 좌표 (수평선에 더 가까움): Z ≈ +27.7
- 하단 ray 가 지면과 만나는 Z 좌표 (카메라에 더 가까움): Z ≈ −11.7
- 지면 가시 Z 범위 ≈ [−11.7, +27.7], 길이 ≈ 39.4m. **trapezoid**(원근 — 가까운 쪽이 좁고 먼 쪽이 넓다).

**폭 계산** — 지면 hit 의 카메라까지 거리(forward 방향 투영 길이, depth) × tan(horizontal half-FOV).

- horizontal half-FOV (16:9 기준) = atan(tan(30°) × 16/9) ≈ 45.74°
- 가까운 쪽 hit (0, 0, -11.7) — 카메라(0, 15.32, -14.40)에서 vector = (0, -15.32, 2.70). forward 방향 투영 = dot((0,-15.32,2.70), (0,-0.766,0.643)) = 15.32×0.766 + 2.70×0.643 ≈ 11.74 + 1.74 = **13.48m**. half-width = 13.48 × tan 45.74° ≈ **13.85m** → **폭 ≈ 27.7m**
- 먼 쪽 hit (0, 0, +27.7) — 카메라에서 vector = (0, -15.32, 42.10). forward 방향 투영 = 15.32×0.766 + 42.10×0.643 ≈ 11.74 + 27.07 = **38.81m**. half-width = 38.81 × tan 45.74° ≈ **39.84m** → **폭 ≈ 79.7m**

> 즉 줌인 한계에서 지면 가시 영역은 사다리꼴: **(Z = −11.7 폭 ≈ 27.7m) ~ (Z = +27.7 폭 ≈ 79.7m)**, 깊이 약 39m.

### 2.3 Zone 크기 결정

스펙 §10 — "Zone trigger 크기 = 카메라 가시영역의 ~80~90%". 위 산출은 trapezoid 이지만 zone 은 직사각형이므로 **inscribed rectangle (가까운 쪽 폭 27.7m 으로 묶인 사각형)** 기준이 정확.

inscribed rectangle = 폭 27.7m × 깊이 39.4m. 80% = 폭 22.2 × 깊이 31.5, 90% = 폭 25.0 × 깊이 35.5.

플랜 잠정값 (16, 1, 16) 은 폭 **58%** × 깊이 **41%** = inscribed area 의 **~24%** — 스펙 §10 의 "80~90%" 와 큰 격차. 옵션 비교:

| 옵션 | Zone size | inscribed 폭 비율 | inscribed 깊이 비율 | spawn 비가시 보장 | 평가 |
|---|---|---|---|---|---|
| 옵션 A | (30, 1, 30) | **108%** (overflow) | 76% | — | **폭 30 > 가까운쪽 가시 폭 27.7 → zone 동/서 가장자리가 화면 밖** — 스펙 §1.1 "카메라 = 내 영역" 침해. 채택 불가 |
| 옵션 B (플랜 잠정) | (16, 1, 16) | 58% | 41% | 보장 X (spawn point 모두 카메라 안) | zone 이 화면의 절반도 안 채워 시각 답답. 스펙 §10 와 어긋남 |
| **옵션 B' (권장)** | **(24, 1, 24)** | **87%** | **61%** | **부분 보장** (E/W spawn X=±14.4 는 가까운쪽 폭 ±13.85 약간 초과 → 살짝 비가시 가능. N spawn Z=+14.4 는 가시, S spawn Z=−14.4 는 카메라 하단 Z=−11.7 밖 → 비가시) | 폭이 spec §10 의 "80~90%" 정확 부합 (87%). 깊이 61% 는 trapezoid 의 깊이 39m 대비 다소 작지만 zone 정사각 유지 (컨셉 §4.1 ring 균등) |
| 옵션 C | (24, 1, 30) | 87% | 76% | 옵션 B' 와 유사 | 폭 단축 24 + 깊이 확장 30 — Z 방향 비가시 spawn 영역 확대. 비대칭이라 ring 컨셉과 어긋남 |

> **옵션 A 채택 시 발생하는 시각 문제**: 폭 30m 의 zone 본체가 화면 좌우 밖으로 일부 잘림. v1.1 결정 (SimpleMover._clampZone — §6.1) 적용 시 영웅이 zone 가장자리에서 클램프되므로 영웅이 화면 밖으로 잘리는 시각 사고 발생 (zone 폭 30 > 가시 폭 27.7). 본 기획서는 옵션 A 를 권장 안에서 배제한다.

**결정**: 본 기획서는 **(24, 1, 24)** 채택. 폭 비율 87% 가 스펙 §10 부합. 깊이 비율 61% 는 ring 균등(4 edge 동일 거리) 유지 우선으로 정사각 채택.

> qa-simulator 검증 후 결정 가능 항목: 5분 세션의 "spawn 가시 빈도" — 옵션 B' (24×24) 에서 E/W spawn point 가 카메라 가장자리에 있어 미세 가시 가능. 메트릭 — 5분 평균 가시 spawn 발생 비율 < 20% 면 B' 통과, > 50% 면 옵션 C (24, 1, 30) 로 재산정.

### 2.4 Zone 위치

Zone 의 BoxCollider center 를 카메라 앵커 (0, 0, -1.54) 에 정확히 맞추면 화면 정중앙이 zone 중심이 된다. 그러나 카메라 앵커가 약간 -Z 쪽이라 zone 중심 (0, 0, 0) 에 두면 화면 중심이 zone 의 X=0/Z=+1.5 부근으로 약간 위로 치우침 — 카메라가 영웅을 약간 화면 하단에서 보게 되어 위쪽 진행 공간이 더 많이 보이는 자연스러운 탑다운 구도 (영웅이 던전 안쪽을 들여다보는 시점).

**결정**: Zone center = (0, 0, 0). 위치 보정 없이 기존 원점 좌표 유지 — 6 Spawner 가 이미 원점 기준 배치되어 있어 호환 (Spawner 위치 자체는 §4.2 의 spawn point 와 별개 — 본 절은 zone center 결정).

---

## 3. Spawn point 거리 (zone 밖 m) — 몬스터 moveSpeed 기준

### 3.1 BalanceConfig 의 moveSpeed 값 (확인)

| EMonster | Key | moveSpeed (`BalanceConfig.asset` 확인) |
|---|---|---|
| Wisp | 0 | 1.0 |
| Wraith | 1 | 0.8 |
| Reaper | 2 | 1.5 |
| Hex | 3 | 1.4 |
| Plague | 4 | 1.3 |
| Phantom | 5 | 2.4 |
| **평균** | | **1.4** |
| **최저** | | **0.8** (Wraith) |
| **최고** | | **2.4** (Phantom) |

### 3.2 결정 — 최고 (Phantom 2.4m) 기준

스펙 §10 "spawn 거리 = moveSpeed × 1.0초". moveSpeed 가 6종 다른데 어느 기준으로 spawn point 까지의 거리를 잡을지가 본 결정.

| 기준 | spawn 거리 | 효과 | trade-off |
|---|---|---|---|
| 평균 1.4 → **1.4m** | 평균 1.0s march | Phantom 은 0.58s 만에 등장 (너무 빠름) / Wraith 는 1.75s (적정) | 빠른 종이 "툭 튀어나옴"으로 보임 |
| 최고 2.4 → **2.4m** (권장) | Phantom 1.0s, Wraith 3.0s, Reaper 1.6s, 평균 1.7s | 모든 종이 최소 1초 march 보장 → "행진해서 들어왔다" 시각 신호 보장 | 느린 종(Wraith) 은 3초 march — zone 밖 시각 visible 시간 증가 |
| 최저 0.8 → **0.8m** | Phantom 0.33s, Wraith 1.0s | 사실상 spawn 즉시 zone 내부 | spawn 비가시 의도 자체가 거의 무효 |

**결정**: **2.4m (최고 기준)**. 근거:
- 컨셉 §4.1 "Spawner 는 ring 둘레에서 태어나 안쪽으로 수렴" — march 단계가 시각적으로 명확해야 ring 모델이 읽힘.
- Phantom(스웜 종) 이 가장 빠르고 가장 많이 등장 → Phantom 의 시각이 "툭 튀어나옴"이면 가장 심한 위화감 — 그 종 기준으로 잡으면 모든 종이 자연스러움.
- Wraith 가 3초 march 는 느린 만큼 "묵직한 등장" 으로 컨셉 §5.1 (보스급 탱커) 와 어울림.

### 3.3 거리 적용

Zone (24, 1, 24) 기준 — zone 가장자리 = ±12. spawn point = 가장자리 + 2.4m = **±14.4** (정수 절삭 없이 그대로 단정 — Phantom moveSpeed 2.4 × 1.0초의 정확한 산출값. 1초 march 보장 = "스워밍" 페이싱 명확. plan / spec 도 ±14.4 기준으로 정합).

> 옵션 A(30,1,30) 채택 시 spawn point = ±17.4m, 옵션 B(16,1,16) 채택 시 ±10.4m 로 §2.3 결정과 함께 갱신.

---

## 4. Spawn point 개수 / 좌표 — 12 채택

### 4.1 개수 결정 — 12 vs 16 vs 20

**5분 세션 spawn 횟수 추정**: 6 Spawner × 5분 × 평균 cooldown 3초 = 평균 100~120 spawn / 세션. spawn point 개수별 평균 재사용 횟수:

| 개수 | edge 당 | 평균 재사용 | 평가 |
|---|---|---|---|
| 12 | 3 | ~10회 | RNG 분산 충분. 같은 spot 에서 1분에 1.5회 → 중복 출현 가시. **MVP 권장** |
| 16 | 4 | ~7회 | 중복 가시성 줄어듦. 와이어링 수 33% 증가 |
| 20 | 5 | ~6회 | RNG 분산 가장 좋음. 와이어링 비용 67% 증가 |

**결정**: **12 (edge 당 3)**.

근거:
- MVP 단계의 wireup 비용 최소화 (스펙 §13 후속 — 다중 BattleZone 으로 확장 시 prefab variant 가 더 깔끔).
- 5분 압축 세션에서 동일 spot 의 반복 spawn 은 "버그가 아닌 컨셉 §4.1 의 ring 라인 흐름 시각화" — 플레이어가 어느 방향에서 몬스터가 자주 오는지 학습할 수 있는 정보가 됨 (advisor 지적 — "repetition is a feature").
- 16 vs 12 의 RNG 차이 (10회 vs 7회 재사용) 는 시각적으로 큰 차이 없음.

### 4.2 좌표 (Zone (24, 1, 24) 기준)

Zone center=(0, 0, 0), edge X·Z = ±12. spawn point = edge + 2.4m → **±14.4** (정수 절삭 없이 그대로 단정 — §3.3 참조). edge 당 3개 균등 분산:

| 이름 | Local Position (X, Y, Z) | edge | 비고 |
|---|---|---|---|
| SpawnPoint_N1 | (-6, 0, 14.4) | North (Z+) | edge 폭 24m 의 좌측 1/4 지점 |
| SpawnPoint_N2 | (0, 0, 14.4) | North | edge 중심 |
| SpawnPoint_N3 | (6, 0, 14.4) | North | edge 우측 1/4 |
| SpawnPoint_S1 | (-6, 0, -14.4) | South (Z-) | |
| SpawnPoint_S2 | (0, 0, -14.4) | South | |
| SpawnPoint_S3 | (6, 0, -14.4) | South | |
| SpawnPoint_E1 | (14.4, 0, -6) | East (X+) | |
| SpawnPoint_E2 | (14.4, 0, 0) | East | |
| SpawnPoint_E3 | (14.4, 0, 6) | East | |
| SpawnPoint_W1 | (-14.4, 0, -6) | West (X-) | |
| SpawnPoint_W2 | (-14.4, 0, 0) | West | |
| SpawnPoint_W3 | (-14.4, 0, 6) | West | |

**좌표 검산**: edge 폭 24m, 3개 spawn point 가 ±6, 0 분산 = edge 의 25%/50%/75% 지점 (균등). 가장자리 모서리(X=±12, Z=±12) 의 8m 안쪽까지만 spawn → 모서리 중복 spawn 방지. Z(또는 X) 방향 spawn 거리 ±14.4 는 zone edge ±12 에서 2.4m 바깥 — Phantom moveSpeed 2.4 × 1.0초 정확값.

---

## 5. Hero entry 거리 / 동선 / 페이싱

### 5.1 동선 — 서쪽 → 동쪽 진입 (컨셉 정합)

**결정**: 영웅이 zone **서쪽 (X-)** 밖에서 등장하여 zone 중심(0, 0, 0) 을 향해 동쪽으로 행진.

대안 비교:

| 동선 | 시각 효과 | trade-off |
|---|---|---|
| **서 → 동 (권장)** | 카메라가 50° down + 약간 -Z 앵커라 화면 좌측에서 우중심으로 들어오는 동선이 자연스럽게 보임. 영웅이 던전 입구(서쪽 = 외부) 에서 안쪽 으로 진입하는 RPG 관용 동선 | — |
| 남 → 북 | 화면 하단(카메라 가까운 쪽 Z=-11.7) 에서 등장 → 카메라 가까이서 큰 모습으로 등장 — "보스 등장" 느낌. 단 화면 하단에 보스 UI/카드 픽 공간이 있어 시각 충돌 가능 | UI 침범 |
| 북 → 남 | 화면 상단 깊은 곳에서 등장 → 카메라까지 행진 길 길어짐 (가시 trapezoid 의 먼 쪽은 Z=+27.7 까지) | 행진 시간이 의도보다 길어짐 |
| 동 → 서 | 시각 효과는 서→동 의 미러. 컨셉 정합도(외부에서 안쪽) 동일 | 좌우 차이 미미, 우측을 카드 픽 UI 가 차지하므로 서→동 이 안정 |

서→동 채택. 영웅이 화면 좌측 외부에서 행진하여 zone 중심에 도달 → 카드 픽 UI 의 우측 컬럼 (기획서 `spawner-status-ui.md` §2.8 — 화면 우측 1/3 의 빌드 패널) 과 동선 충돌 없음.

### 5.2 거리 / 페이싱

영웅 `BalanceConfig.Hero.MoveSpeed = 3`. zone 서쪽 가장자리 X=-12.

| 거리 (X=) | edge 까지 m | 총 행진 m (HeroEntryPoint → Center) | 행진 시간 | 평가 |
|---|---|---|---|---|
| -14 | 2m | 14m | 4.67s | march 시간 너무 길어 첫 카드 픽 직전(0:00) 까지 지루함 |
| **-15** (권장) | **3m** | **15m** | **5.0s** | march 시간 5초 — "등장 → 자리 잡음" 호흡으로 적정. 컨셉 §4.1 의 5분 세션 대비 1.6% — 무시 가능 |
| -16 | 4m | 16m | 5.33s | 5초 약간 초과. 큰 차이 없음 |
| -18 | 6m | 18m | 6.0s | 6초는 다소 길어 — MVP 의 프리미티브 영웅(파란 캡슐)이 6초 동안 걸어오면 답답 |
| -20 | 8m | 20m | 6.67s | 너무 김. 영웅 등장 효과보다 지연감 |

플랜 잠정값 X=-12 (= zone edge 거리 4m → 행진 16m → 5.33s) 는 본 분석의 권장 X=-15 와 가까움 — 단 **플랜의 X=-12 는 zone size (16,1,16) 기준** (zone edge X=-8 + 4m = -12), 본 기획서의 zone (24,1,24) 기준으로 재계산.

**결정**: HeroEntryPoint = **(-15, 0, 0)**. 행진 거리 15m, 영웅 moveSpeed 3 → **행진 시간 5.0초**.

> Zone 옵션 A (30,1,30) 채택 시 X=-18 (edge=-15 +3m), 옵션 B (16,1,16) 채택 시 X=-11 (edge=-8 +3m, 행진 11m=3.67s) 로 §5.2 재계산.

### 5.3 영웅 시선 / 회전

영웅이 X- 에서 X+ 로 행진 → `IRotator.FaceDirection(dir)` 호출 시 dir = (1, 0, 0) → 영웅이 X+ 방향(동쪽)을 바라본 채 이동 (자연스러운 진입 자세). `HeroEntryDriver.Update` 가 매 프레임 `_rotator?.FaceDirection(dir)` 호출 — Center 도달 시점에 dir.magnitude < threshold 되어 호출 안 됨 → 도달 위치에서 마지막 회전 상태(동쪽) 유지. 도달 후 `AutoCombatAI` 활성화되며 nearest-target 방향으로 다시 회전.

### 5.4 도달 임계값

스펙 §10 `_arriveThreshold = 0.5m`. 영웅 moveSpeed 3 / 60fps 기준 1프레임 이동 거리 = 0.05m → 0.5m 는 약 10프레임 (0.17s) 안에 진입 보장. 임계값 변경 불필요 — **0.5m 유지**.

---

## 6. 영웅 zone-clamp (SimpleMover._clampZone)

> **v1.1 정정 (2026-05-29 — design-reviewer BLOCKER B1/B2)**: 본 § 의 결정이 v1.0 의 "인비저블 벽 자동 생성 + 영웅 CapsuleCollider 부착 + BattleZone Kinematic Rigidbody" 3중 결정에서 **SimpleMover._clampZone 옵션 단일 결정**으로 교체됨. v1.0 의 §6 결정 (인비저블 벽 / Knight Collider / BattleZone Rigidbody) 은 모두 폐기.

### 6.0 v1.0 결정 폐기 사유 (B1/B2)

v1.0 에서 채택했던 "BattleZone Awake 시 4면 인비저블 벽 자동 생성 + BattleZone 본체에 Kinematic Rigidbody 부착" 결정에는 다음 결함이 발견됐다:

- **코드 현실 — 몬스터 6종 프리팹은 이미 Dynamic Rigidbody 부착** (Wisp.prefab 직접 확인 — RigidbodyConstraints, isKinematic=0). v1.0 §6.3 "몬스터에 Rigidbody 없다" 단정은 사실 오류였다.
- **충돌 매트릭스 부작용**: BattleZone 본체에 Kinematic RB + 4면 compound BoxCollider 가 생성되면, Dynamic RB 를 가진 몬스터가 zone 밖에서 zone 안으로 들어올 때 **인비저블 벽(non-trigger Collider)에 물리적으로 막힌다**. 결과: 몬스터가 zone 진입 못 함 → `OnTriggerEnter` 발화 안 됨 → `Marching → Engaging` 상태 전환 불가 → **영웅 AI 의 nearest-target 후보가 영원히 0** → 전 시스템 마비.
- v1.0 §6.2 한계 박스에서 PlayMode 검증 위임으로 적었던 "벽이 영웅을 못 막을 수 있음" 보다 훨씬 치명적인 부작용 — 즉 옵션 1 (벽) 은 영웅 차단 실패의 폴백 옵션 3 (SimpleMover clamp) 조차 적용해도 본질적인 "몬스터 zone 진입 차단" 문제가 남는다.

→ **인비저블 벽 자동 생성 + BattleZone Rigidbody 부착 둘 다 채택 안 함**. 영웅 차단은 SimpleMover 의 옵션 필드로 수행.

### 6.1 결정 — SimpleMover._clampZone 옵션

**결정**: 영웅 `SimpleMover` 에 `[SerializeField] private BattleZone _clampZone` 필드 신설. `FixedUpdate` 의 next 좌표를 `_clampZone.ClampInside(next)` 로 X/Z 평면 클램프.

- **영웅 프리팹 인스펙터 필드 1개 와이어링**으로 영웅 한정 적용 — 몬스터 6종 프리팹에서는 `_clampZone` 미할당 (null) 이라 클램프 무동작.
- 몬스터는 zone 진입 후 영웅을 추적하므로 자연히 zone 안에 머무름. 예외적 이탈은 인비저블 벽 부재로 막을 수 없음 — MVP 에선 허용 (드물고, 비치명적). 스펙 §11.6 명시.
- BattleZone 측 추가 컴포넌트 (Rigidbody) 없음. BattleZone 본체 = BoxCollider(isTrigger) + BattleZone 컴포넌트 2개만.

### 6.2 Clamp 동작 명세

`BattleZone.ClampInside(Vector3 worldPos)`:
- `_zoneTrigger.bounds` 의 `min.x / max.x` 로 worldPos.x 클램프 — `Mathf.Clamp(x, min.x, max.x)`.
- 동일하게 worldPos.z 클램프.
- worldPos.y 는 입력 그대로 (`SimpleMover` 가 어차피 Y=0 고정).
- `_zoneTrigger == null` 폴백: worldPos 그대로 반환 (안전 가드).

`SimpleMover.FixedUpdate`:
- `Vector3.MoveTowards(transform.position, _target, _speed * Time.fixedDeltaTime)` 으로 next 계산
- next.y = 0 고정
- `_clampZone != null` 이면 `next = _clampZone.ClampInside(next)`
- 최종 위치 적용 (`_rigidbody.MovePosition(next)` 또는 `transform.position = next`)

→ 영웅이 zone 가장자리에 다가가도 가장자리 안쪽으로 매 FixedUpdate 자동 클램프 → 영웅이 zone 밖으로 나가는 케이스 0건.

### 6.3 대안 검토 (재정리)

| 옵션 | 방식 | 평가 |
|---|---|---|
| ~~옵션 1 (v1.0 채택)~~ | 인비저블 벽 자동 생성 + 영웅 CapsuleCollider | **폐기** — 몬스터 Dynamic RB 와 충돌 매트릭스 충돌로 zone 진입 차단 위험 (§6.0) |
| 옵션 2 | HeroEntryDriver 가 zone center 까지만 이동 + AutoCombatAI 에 zone leash | 스펙 §2.2 — leash 는 명시적 후속 작업. 본 작업 범위 외 |
| **옵션 3 (v1.1 채택)** | **SimpleMover._clampZone 옵션** | 영웅 인스펙터 필드 1개 + ClampInside 수치 클램프. 몬스터 무영향. 가장 단순하면서 부작용 없음 |

옵션 3 채택. 옵션 1 폴백이 아닌 **단일 1차 결정**.

### 6.4 몬스터 OnTriggerEnter 발화 보장 (Rigidbody 조합)

`BattleZone._zoneTrigger.OnTriggerEnter(Collider)` 발화 조건 (Unity 충돌 매트릭스):
- 진입측 또는 zone trigger 측 어느 하나에 Rigidbody 존재 + 양쪽 모두 Collider 부착.

**코드 현실 검증**: 몬스터 6종 프리팹은 **이미 Dynamic Rigidbody + CapsuleCollider 부착** (Wisp.prefab 확인 — RigidbodyConstraints / collider 모두 존재). zone 측은 BoxCollider(isTrigger) 단독.

→ 진입측 (몬스터) 에 Rigidbody 가 이미 있으므로 **BattleZone 본체에 Rigidbody 추가 불필요**. 기존 프리팹 변경 없이 OnTriggerEnter 자연 발화.

**결정**: BattleZone 본체에 Rigidbody 추가 안 함. 몬스터 프리팹 무변경.

### 6.5 영웅 OnTriggerEnter 무영향

영웅 (Knight.prefab) 은 MonsterTag 부재 → BattleZone.OnTriggerEnter 의 `other.GetComponent<MonsterTag>() == null` 분기로 자동 무시 (스펙 §4.2). 영웅 Collider 유무와 무관 — 영웅이 zone trigger 를 지나가더라도 IsEngaging 상태 전환 대상 아님.

---

## 7. BattleClock 시작 페이싱 — entry → 첫 spawn → 첫 카드 픽

### 7.1 흐름

```
T = 0.0s   Battle 씬 진입
T = 0.0s   BattleController.Start() → SpawnHero (영웅이 HeroEntryPoint (-15,0,0) 에 등장)
T ≈ 0.0s   영웅 AutoCombatAI.enabled = false, HeroEntryDriver.enabled = true
T = 0.0s   BattleClock 미시작 — Tick 호출돼도 no-op
T = 0.0s   Spawner Tick 게이트 false — 어떤 Spawner 도 Tick 안 함
T ≈ 5.0s   영웅이 Center (0,0,0) 도달 → BattleZone.NotifyHeroReachedCenter()
T = 5.0s   BattleController.HandleHeroReachedCenter()
            ├ _spawnersActive = true
            ├ 영웅 AutoCombatAI.enabled = true
            ├ HeroEntryDriver.enabled = false
            └ _clock.Start()         ← 5분 카운트다운 개시
T = 5.0s+  Spawner Tick 시작 — 6 Spawner 각자의 InitialDelay 위상 오프셋에 따라 첫 spawn
```

### 7.2 컨셉 §2.4 위상 오프셋과 정합

`docs/design/continuous-spawn-round.md` (확인된 기존 문서) 의 §2.4 — Spawner 각자의 `InitialDelay` 가 위상 오프셋. 본 기획서는 이 InitialDelay 의 **0 기준점** 을 명시한다:

**결정**: Spawner 의 `InitialDelay` 는 **`OnHeroReachedCenter` 발행 시점(T=5.0s) 기준의 상대 시간**. 즉 `BattleController.HandleHeroReachedCenter` 가 호출된 그 프레임부터 각 Spawner 의 첫 spawn 까지 InitialDelay 초가 흐름.

기존 `Spawner.Tick(dt)` 는 dt 누적 방식이라 게이트 false 동안 Tick 자체가 호출되지 않으면 자연히 0 부터 시작. 즉 본 기획서의 결정은 **추가 코드 없음** — 게이트 플래그 하나로 자동 만족.

### 7.3 첫 카드 픽 페이싱과의 정합

컨셉 §4.2 — 액티브 카드 30초마다, 첫 발화는 0:30. 패시브 카드는 영웅 HP 90% 도달 시점.

| Event | 절대 시간 (씬 진입 기준) | 상대 시간 (BattleClock 기준) |
|---|---|---|
| 영웅 등장 | T = 0.0s | — (Clock 미시작) |
| 영웅 Center 도달 + BattleClock Start | T = 5.0s | T = 0.0s |
| 첫 액티브 카드 (BalanceConfig._activeThresholds[0] = 30s) | T = 35.0s | T = 30.0s |
| 첫 패시브 카드 (HP 90% 도달) | 영웅 HP 깎인 시점 | (HP 기반) |
| 마지막 액티브 카드 (270s) | T = 275.0s | T = 270.0s |
| 게임 종료 (300s) | T = 305.0s | T = 300.0s |

→ 씬 진입 후 BattleClock 이 5초 늦게 시작하므로 5분 전체 세션의 실제 카메라 시간은 **5분 5초**. 컨셉 §4.4 "5:00 도달 = 패배" 의 5:00 은 BattleClock 기준이라 그대로 유지.

### 7.4 영웅 등장 동안 카드 시스템

영웅 entry march 5초 동안 패시브 카드는 발화 불가 (HP 100% 유지 — 몬스터 0마리). 액티브 카드는 시간 기준이지만 BattleClock 미시작이라 발화 불가. → entry 동안 **어떤 카드 UI 도 뜨지 않음** — 컨셉 §4.3 일시정지+큐 와 무관 (큐 자체가 비어있음).

**결정**: 영웅 entry 동안 카드 시스템은 자연 비활성. 별도 가드 코드 불필요.

---

## 8. 디버그 Gizmo (MVP 편의)

스펙 §3.4 — OnDrawGizmos 로 zone 경계 / spawn points / hero entry 표시 (v1.0 의 "인비저블 벽" Gizmo 항목은 §6 결정 변경으로 폐기 — v1.1). 플랜은 미커버 (§9 Self-Review §3.4 미커버 명시).

**결정 — 본 기획서는 Gizmo 를 포함** (MVP 디자이너 편의):

| 요소 | 색 | 모양 |
|---|---|---|
| Zone trigger 경계 | 녹색 `(0, 1, 0)` | `Gizmos.DrawWireCube(_zoneTrigger.bounds.center, _zoneTrigger.bounds.size)` |
| Spawn points | 노랑 `(1, 1, 0)` | `Gizmos.DrawSphere(sp.position, 0.3f)` per spawn point |
| Hero entry point | 빨강 `(1, 0, 0)` | `Gizmos.DrawSphere(_heroEntryPoint.position, 0.4f)` |

> v1.1 — v1.0 의 "Inscribed wall (자홍색)" 행 폐기. 인비저블 벽 자체가 폐기됨.

근거: zone 크기 조정·spawn point 위치 조정이 본 기획서의 핵심 결정인데, Scene 뷰에서 시각 피드백 없으면 Inspector 만으로 조정 어려움. 코드 5~10줄 추가로 디자이너 작업 효율↑.

---

## 9. 데이터 / 수치 요약 (단일 진실)

| 항목 | 값 | 단위 | 근거 (§) |
|---|---|---|---|
| Zone BoxCollider size | (24, 1, 24) | m | §2.3 (잠정 — A/B 옵션 사용자 결정 필요) |
| Zone center (local) | (0, 0, 0) | m | §2.4 |
| Zone trigger isTrigger | true | bool | 스펙 §3.1 |
| Spawn point 거리 (zone 가장자리 +) | 2.4 | m | §3.2 — 몬스터 max moveSpeed (Phantom 2.4) × 1.0s |
| Spawn point 개수 | 12 (edge 당 3) | 개 | §4.1 |
| Spawn point 좌표 | §4.2 표 | m | §4.2 |
| HeroEntryPoint Local Position | (-15, 0, 0) | m | §5.2 |
| 영웅 entry 행진 거리 | 15 | m | §5.2 (HeroEntryPoint → Center) |
| 영웅 entry 행진 시간 | 5.0 | s | §5.2 (15m ÷ moveSpeed 3) |
| HeroEntryDriver._arriveThreshold | 0.5 | m | §5.4 |
| 영웅 SimpleMover._clampZone | 런타임 주입된 BattleZone 인스턴스 (영웅 한정, BindClampZone 메서드) | reference | §6.1 / §12.B (몬스터는 미주입 null) |
| BattleZone 본체 추가 컴포넌트 | BoxCollider(isTrigger) + BattleZone 2개만 (Rigidbody / 인비저블 벽 없음) | — | §6.3 / §6.4 (v1.1 정정) |
| BattleClock 시작 시점 | OnHeroReachedCenter | event | §7.1 |
| Spawner InitialDelay 0 기준 | OnHeroReachedCenter | event | §7.2 |

---

## 10. 시너지 가시성 / 페이싱 영향

### 10.1 시너지 가시성

- **Marching 몬스터가 영웅 AI 후보에서 제외** → 영웅이 zone 가장자리에서 막 등장한 Phantom 떼를 무시하고 zone 안의 Wraith 를 먼저 패는 패턴이 자연 발생. 플레이어가 "내 Phantom 떼는 zone 진입 직후 영웅을 둘러싼다" 라는 컨셉 §5.2 시너지(스웜) 를 시각으로 학습.
- 동시에 **Marching 중 데미지 수용** (스펙 §4.1) → 영웅이 zone 안에서 AoE 카드(독 장판, 광역 발화)를 쓰면 zone 가장자리 진입 직전 Marching Phantom 까지도 데미지를 받음 → AoE 카드의 효과 가시.

### 10.2 페이싱

- 영웅 등장 → 5초 행진 → 자리 잡음 → Spawner Tick → 첫 몬스터 등장 → 영웅이 첫 타겟 잡음 → 30초 후 첫 액티브 카드 → ...
- 5초 행진 동안 화면이 "조용한 시작" → 첫 액티브 카드 (T = 35s 절대 / T = 30s clock) 까지 30초 → 페이싱 충돌 없음.
- 영웅 행진 5초 → 영웅 자리잡음 → 30초의 "스폰 보고 다 보기" 단계 → 첫 카드 픽 — MVP 가설 §11.1 "5분 자동전투 + 카드 트리거 선택지 재미" 검증의 첫 호흡으로 적정.

> **컨셉서 §4.1 후속 갱신 필요** (v1.1 — W2): 컨셉서 §4.1 "영웅이 던전 정중앙에서 등장" 서술은 본 결정 (영웅 zone 서쪽 밖 X=-15 에서 등장 → 5초 행진 → Center 도달 후 게임 시작) 과 모순. 본 기획서 결정이 우선 (도메인 가시성 + 페이싱 우위) — **컨셉서 §4.1 의 등장 위치 서술을 별도 작업으로 갱신** 필요. 책임자: 본 기능 PR 머지 직후 game-designer 의 후속 차순위 작업 (별도 PR).

---

## 11. 엣지 케이스

스펙 §11 의 6 케이스를 본 기획서 결정 기준으로 확장:

| # | 케이스 | 본 기획서 대응 |
|---|---|---|
| 1 | BattleZone 미할당 | 스펙 — `_heroSpawn` fallback. 본 기획서 권장: Awake 시 `_zoneTrigger == null && _spawnPoints == null` 모두 시 LogError + return → 폴백 동작은 플랜 Task 8 의 `if (_zone == null)` 분기로 보장 |
| 2 | spawnPoints 0 개 | `GetRandomSpawn` null → Spawner 가 `_spawnPoint` fallback. 본 기획서 §4.2 에서 12개 명시 — 사고 방지 |
| 3 | 풀 재사용 누락 | 플랜 Task 2 (MonsterTag.OnEnable) 가 보장 |
| 4 | 영웅 entry 중 사망 | 본 기획서 §5 — entry 동안 몬스터 0마리라 발생 안 함. 안전망으로 `HeroEntryDriver.Update` 가 `Health.IsAlive == false` 체크 후 `_mover.Stop()` |
| 5 | (폐기 — v1.1) | v1.0 의 "Zone trigger vs 인비저블 벽 충돌" 케이스. 인비저블 벽 폐기로 자연 해소 |
| 6 | (폐기 — v1.1) | v1.0 의 "영웅 collider 누락 → 벽 통과" 케이스. 벽 자체 폐기로 자연 해소 |
| 7 (재정의) | 영웅이 zone 경계에 부딪힘 | **본 기획서 §6.1 — SimpleMover._clampZone 가 매 FixedUpdate X/Z 클램프**. 영웅이 가장자리에 접근해도 안쪽 한계 위치로 잡힘. 모서리 끼임 (코너 케이스) 도 X/Z 각 축 독립 클램프라 자유 영역 유지 — 영웅 가장자리 평행 이동 자연 |
| 8 (재정의) | 몬스터가 zone 진입 trigger 발화 못함 | **본 기획서 §6.4 — 몬스터 프리팹이 이미 Dynamic Rigidbody 부착이라 OnTriggerEnter 자연 발화**. BattleZone 본체에 Rigidbody 추가 불필요. v1.0 의 "kinematic Rigidbody 추가" 결정은 폐기 (B2) |
| 9 (폐기 — v1.1) | v1.0 의 "인비저블 벽이 영웅을 실제로 못 막을 가능성" 케이스. 벽 폐기로 자연 해소 — SimpleMover._clampZone 은 수치 클램프라 물리 우회 위험 없음 |
| 10 신규 | 영웅 SimpleMover 가 `_clampZone` 미주입 (런타임 주입 누락) | `_clampZone == null` 분기로 클램프 무동작 → 영웅이 zone 밖으로 자유 이동 (시각 버그). 주입 누락 방지 — 본 기획서 §12.B 의 `BindClampZone` 단일 진입점 + BattleController.SpawnHero 의 Pop 직후 호출 한 단계. PlayMode smoke test (plan Task 9 Step 7) 에서 영웅이 zone 경계에 부딪히는지 확인 |

---

## 12. 구현 요청사항 (gameplay-programmer 용)

### A. Enum / Interface 추가

본 기획서는 **신규 Enum / Interface 없음**. BattleZone 은 단일 인스턴스 컴포넌트라 Enum 키 불필요. 인터페이스 의존은 스펙·플랜이 락한 기존 IMover / IRotator / IHealth / ISpawnerHost 그대로 사용.

### B. 영웅 SimpleMover._clampZone 런타임 주입 (v1.1 — 핵심 결정)

> **v1.1 정정**: v1.0 의 "Knight 프리팹에 CapsuleCollider 추가" 결정은 폐기 (§6.0 사유). 대신 **영웅 SimpleMover 의 `_clampZone` 을 런타임 주입**으로 와이어링.

**전제 — 영웅은 풀 스폰**: 영웅 Knight 는 `BattleController.SpawnHero` 안에서 `CHMResource.Instance.LoadAsync<GameObject>(EHero.Knight)` 로 prefab 을 로드한 뒤 `CHMPool.Instance.Pop(prefab, transform)` 으로 Pop 한다 (Rule 03 §4). 즉 씬에 사전 배치된 정적 GameObject 가 **아니다**. 풀 Pop 한 prefab clone 은 씬 안의 객체 참조 필드를 직접 들고 있을 수 없으므로 **씬 인스펙터 와이어링 방식은 작동하지 않는다**.

**결정 — 런타임 주입 단일 갈래**:

1. `SimpleMover` 에 public 메서드 `public void BindClampZone(BattleZone zone)` 추가. 메서드는 내부 `_clampZone` private 필드에 인자를 그대로 대입한다.
2. `BattleController.SpawnHero` 의 영웅 Pop 직후 (기존 `p.transform.position = ...` 다음 줄) 한 단계 추가:
   ```
   if (_zone != null)
   {
       SimpleMover mover = p.GetComponent<SimpleMover>();
       if (mover != null) mover.BindClampZone(_zone);
   }
   ```
3. `SimpleMover._clampZone` 의 `[SerializeField]` 선언은 유지 — 인스펙터 노출 자체는 보존 (디버깅 시 런타임에 어떤 zone 이 바인딩됐는지 확인 가능). 단 prefab asset 의 `_clampZone` 은 항상 null 로 직렬화된 상태로 유지된다 (씬 참조 불가).
4. Knight prefab 자체 변경 0건 — `Assets/_Lair/Art/Characters/Knight.prefab` 수정 필요 없음.

**다른 갈래는 배제**:
- 씬 인스펙터 와이어링 → 풀 Pop 메커니즘과 충돌, 작동 불가 (위 전제).
- SimpleMover 필드를 `public BattleZone _clampZone` 로 노출 → Rule 02 §6 (View/Model 캡슐화 우선) 위반 + 외부에서 임의로 변경 가능해 위험.
- Reflection 주입 → 가독성·유지보수성 손실, 본 케이스에서 굳이 사용할 이유 없음.

> 누락 시 시각 버그 (§11 #10) — 영웅이 zone 밖으로 자유 이동. plan Task 9 Step 7 의 PlayMode smoke test 가 트리거.
>
> **plan delta 필요 사항** (gameplay-programmer 또는 메인 오케스트레이터가 plan 갱신 단계에서 처리):
> - plan Task 4 Step 5 의 `SimpleMover.cs` 코드 블록에 `BindClampZone(BattleZone zone)` public 메서드 추가.
> - plan Task 4 의 SimpleMoverClampTests 에서 `fi.SetValue(mover, clampZone)` reflection 호출을 `mover.BindClampZone(clampZone)` public 메서드 호출로 단순화 (선택 — 기존 reflection 방식도 동작).
> - plan Task 8 Step 2 의 `SpawnHero` 코드 블록 끝 (Pop 후 `p.transform.position = spawnPos;` 다음 줄) 에 위 3번 항목의 BindClampZone 주입 코드 추가.
> - plan Task 9 Step 4-1 (씬 인스펙터 와이어링) 폐기 — 풀 스폰이라 작동 불가. 대신 "런타임 주입은 BattleController.SpawnHero 에서 처리 (Task 8)" 한 줄로 교체.

### C. BattleZone 씬 오브젝트 (플랜 Task 9 와 정합)

**대상 파일**: `Assets/_Lair/Scenes/Battle.unity`

신규 GameObject 1개 + 자식 13개 추가:

```
BattleZone (GameObject, position = (0,0,0))
├ Component: BoxCollider (isTrigger=true, size=(24,1,24), center=(0,0,0))   ← §2.3
├ Component: BattleZone (Lair.Battle)                                        ← §6 (Rigidbody / 인비저블 벽 없음)
│            _spawnPoints[12] 와이어링 (아래 자식 12개), _heroEntryPoint 와이어링
├ Children: SpawnPoint_N1/N2/N3/S1/S2/S3/E1/E2/E3/W1/W2/W3 (Transform only)  ← §4.2 좌표
└ Child: HeroEntryPoint (Transform only, position = (-15, 0, 0))             ← §5.2
```

**v1.1 정정**: BattleZone 본체에 Rigidbody 추가 안 함 (§6.4 — 몬스터 프리팹의 Dynamic Rigidbody 가 trigger 발화 보장). BattleZone 컴포넌트의 `_wallThickness` / `_wallHeight` 필드 없음 (인비저블 벽 자동 생성 폐기 — §6.0).

**BattleController 와이어링**:
- `_zone` ← BattleZone GameObject (인스펙터 드래그)
- `_heroSpawn` 는 fallback 으로만 사용 (스펙 §11.1) — 빈 채로 두거나 기존값 유지 가능

**영웅 SimpleMover 와이어링** (§B 참조): 풀 Pop 한 영웅 인스턴스의 `SimpleMover._clampZone` 은 `BattleController.SpawnHero` 가 Pop 직후 `SimpleMover.BindClampZone(_zone)` 호출로 런타임 주입. 씬 인스펙터 와이어링 방식은 풀 스폰이라 작동 불가 (§B 전제).

### D. 에셋 키

본 기획서는 신규 Enum 키 없음 — BattleZone GameObject 의 자식 Transform 들은 인스펙터 배열로 와이어링 (Addressables 키 불필요).

### E. SO 스키마

본 기획서는 신규 ScriptableObject 없음. 모든 수치 (§9 표) 가 BattleZone / SimpleMover 컴포넌트 인스펙터 필드로 직접 노출:

| 컴포넌트 / 필드 | 본 기획서 값 |
|---|---|
| `BattleZone._zoneTrigger` | 본체 BoxCollider |
| `BattleZone._spawnPoints` | 12개 자식 Transform 배열 |
| `BattleZone._heroEntryPoint` | 자식 HeroEntryPoint Transform |
| `SimpleMover._clampZone` (영웅) | 런타임 주입 — `BattleController.SpawnHero` 의 Pop 직후 `BindClampZone(_zone)` 호출 (§B). prefab asset 의 직렬화 값은 null. |
| `SimpleMover._clampZone` (몬스터 6종) | null (미할당 — 클램프 무동작) |
| `SimpleMover.BindClampZone(BattleZone)` | public 메서드 — `_clampZone` 주입 단일 진입점 (§B) |

`HeroEntryDriver._arriveThreshold` 는 컴포넌트 default 0.5 유지 (영웅 프리팹 부착 시 인스펙터 노출).

### F. 디버그 Gizmo 추가 (스펙 §3.4 = 본 기획서 §8)

**대상 파일**: `Assets/_Lair/Scripts/Battle/BattleZone.cs` (플랜 Task 3 의 skeleton 에 추가)

`OnDrawGizmos` 메서드 추가 — §8 표의 색·모양 4종. 본 기획서 §8 명세 그대로 구현.

> **v1.1 정정**: §8 표의 "Inscribed wall (런타임 생성)" 행 (자홍색 Gizmo) 은 인비저블 벽 폐기로 자연 무효. 본 §F 구현 시 자홍색 Gizmo 항목 제외 — zone trigger (녹색) / spawn points (노랑) / hero entry (빨강) 3종만 그린다.

---

## 13. 사용자 결정 필요 / 후속 검증 사항

본 기획서가 단정하지 않고 사용자 검토 또는 PlayMode 검증에 위임한 항목:

| # | 항목 | 본 기획서 결정 | 후속 검증 트리거 | 결정 시점 |
|---|---|---|---|---|
| 1 | Zone 크기 (스펙 §10 80~90% 가설 정합) — §2.3 | (24, 1, 24) — 폭 87% 부합 | qa-simulator 의 E/W spawn 가시 비율 > 50% 면 옵션 C (24,1,30) 로 재산정 | gameplay-programmer 구현 전 사용자 검토 가능. qa-simulator 후 데이터 기반 갱신 |
| 2 | (해소 — v1.1) | v1.0 의 "인비저블 벽이 영웅을 실제로 막는가" 검증 항목 | — | **2026-05-29 B1 정정으로 해소** — 벽 자체 폐기, SimpleMover._clampZone 수치 클램프로 대체 |
| 3 | 컨셉서 §4.1 등장 위치 서술 갱신 — §10.2 (W2) | 본 기획서가 단정 (영웅이 zone 서쪽 밖 X=-15 에서 등장) | 컨셉서 §4.1 의 "정중앙 등장" 서술과 충돌 → 컨셉서 별도 PR 갱신 | 본 기능 PR 머지 직후 game-designer 의 후속 차순위 작업 |
| 4 | 영웅 SimpleMover._clampZone 런타임 주입 실제 동작 — §11 #10 | BattleController.SpawnHero 의 Pop 직후 `SimpleMover.BindClampZone(_zone)` 호출로 단정 (§12.B) | plan Task 9 Step 7 — PlayMode smoke test 에서 영웅이 zone 경계에 부딪히는지 확인 | gameplay-programmer 의 Task 8 (BattleController.SpawnHero 변경) + Task 9 (씬 와이어링) 후 즉시 |

다른 결정 (spawn point 거리/개수/좌표, hero entry 거리/페이싱, BattleClock 시작 시점, 영웅 차단 방식 = SimpleMover._clampZone, BattleZone 본체 추가 컴포넌트 = BoxCollider + BattleZone 2개만) 은 본 기획서가 단정.

### Spec / Plan / 기획서 sync 상태 (v1.2, 2026-05-29)

본 기획서 §2~§5 수치 (zone 24, spawn ±14.4, HeroEntry -15) 와 §6 결정 (SimpleMover._clampZone, BattleZone 본체 단순화) 이 다음과 일치 — 2026-05-29 B1/B2/B3 정정 완료:

| 문서 | 일치 § / Task |
|---|---|
| spec `docs/superpowers/specs/2026-05-29-battlezone-design.md` | §3.2 (SimpleMover._clampZone) / §3.3 (영웅 zone-clamp) / §8 (파일 변경) / §10 (데이터 수치) / §11.5·§11.6 (엣지) / §13 (결정 락) |
| plan `docs/superpowers/plans/2026-05-29-battlezone.md` | 사전 의사결정 (B1/B2/B3 정정 박스) / Task 3 (BattleZone skeleton — Rigidbody/벽 없음) / Task 4 (SimpleMover._clampZone) / Task 9 Step 1~3 (zone size 24, spawn ±14.4, HeroEntry -15 좌표) |

**sync 검증 후속 작업** — spec 본문 §2.1/§2.2/§3.1/§3.4/§4.2/§12.1 의 v1 인비저블 벽 잔존 결정을 SimpleMover._clampZone 으로 일괄 갱신 완료 (2026-05-29 메인 오케스트레이터). v1.1 시점의 잠재 모순(§13 표는 SimpleMover._clampZone 일치 단정 vs spec 본문 일부에 인비저블 벽 잔존) 해소됨. spawn 좌표 ±14 / ±14.4 혼재 또한 본 v1.2 에서 ±14.4 단일 단정으로 통일 (§3.3 / §4.2 / 본 sync 표) — Phantom moveSpeed 2.4 × 1.0초 정확값 기준.

### Plan Delta 필요 사항 (v1.1 — advisor 2차 review)

본 기획서 §12.B 의 **런타임 주입 결정**이 plan 의 다음 Step 과 어긋남 — gameplay-programmer 실행 전 plan 갱신 필요:

| Plan 위치 | 현재 plan 내용 | v1.1 결정 (본 기획서 §12.B) |
|---|---|---|
| Task 4 Step 5 | `SimpleMover._clampZone` private SerializeField 만 추가 | + `public void BindClampZone(BattleZone zone) { _clampZone = zone; }` 추가 |
| Task 4 Step 3 (SimpleMoverClampTests) | `fi.SetValue(mover, clampZone)` reflection 으로 필드 주입 | (호환 — 그대로 둠) 또는 `mover.BindClampZone(clampZone)` public 호출로 단순화 (선택) |
| Task 8 Step 2 (SpawnHero) | Pop 후 transform.position 만 설정 | + `if (_zone != null) { SimpleMover mover = p.GetComponent<SimpleMover>(); if (mover != null) mover.BindClampZone(_zone); }` 한 블록 추가 |
| Task 9 Step 4-1 | "Knight 영웅 프리팹 SimpleMover._clampZone 와이어링 (수동)" — 씬 인스펙터 와이어링 | **폐기** — 풀 스폰이라 작동 불가. "런타임 주입은 BattleController.SpawnHero 에서 처리 (Task 8)" 한 줄로 교체 |

이 delta 는 메인 오케스트레이터가 plan 갱신 단계 또는 gameplay-programmer 가 구현 시작 전에 plan 파일에 반영해야 한다. 본 기획서 §12.B 가 단일 진실 — plan 과 다를 경우 본 기획서 우선.

**향후 zone 크기 변경 시 sync 책임**: Zone 크기를 옵션 A (30,1,30) 또는 옵션 C (24,1,30) 로 변경하면 본 기획서 §4.2 spawn 좌표 / §5.2 HeroEntry 좌표 / plan Task 9 Step 2·3 의 좌표를 일괄 재산정해야 한다. 책임자 — 변경 트리거 단계의 작업자 (qa-simulator 가 데이터 기반 변경 권유 시 game-designer, 사용자 직접 변경 시 game-designer 가 sync 작업 1건 추가).

---

## 14. Self-Review

- **Placeholder 잔존 (5 카테고리)**: 0건 — "사용자 결정 필요" 항목은 §13 표로 명시 위임. "qa-simulator 검증 후 결정" 항목은 §2.3 의 옵션 B' 채택 메트릭 명시.
- **스펙 / 플랜 커버리지**:

| 스펙 § | 항목 | 본 기획서 § |
|---|---|---|
| §3.1 | BattleZone GameObject 구조 | §12.C |
| §3.2 | 공개 API (ClampInside 포함) | §6.2 (ClampInside 동작 명세) / §12.E (SimpleMover._clampZone 와이어링) |
| §3.3 | 영웅 zone-clamp (v1.1 — 인비저블 벽 폐기 후 SimpleMover._clampZone) | §6.1 + §6.2 + §11 #7·#10 |
| §3.4 | OnDrawGizmos | §8 (본 기획서가 채움 — 플랜 §13 후속이었음) |
| §4 | 몬스터 상태머신 | §10.1 시너지 가시성 분석 |
| §5 | 영웅 entry 시퀀스 | §5, §7 |
| §6 | Spawner 통합 | §7.2 InitialDelay 0 기준 |
| §7 | 영웅 AI 타겟 필터 | §10.1 분석 |
| §10 | 데이터·수치 (영웅 zone-clamp 포함) | §2, §3, §5.4, §6.1, §9 |
| §11.5 | 영웅 zone 밖 이탈 시도 | §6.1 + §11 #7 (SimpleMover._clampZone 이 매 FixedUpdate 클램프) |
| §11.6 | 몬스터 zone 밖 이탈 | §11 (MVP 허용 — 드물고 비치명적) |
| §13 결정 락 — 영웅 zone 차단 | SimpleMover._clampZone 옵션 (v1.1) | §6 전체 |

| 플랜 Task | 본 기획서 |
|---|---|
| Task 1~6 | 본 기획서 §9 수치를 받아들이는 인터페이스 그대로 — 변경 없음 |
| Task 7 (HeroEntryDriver) | §5.4 _arriveThreshold 0.5 유지 |
| Task 8 (BattleController) | §7 BattleClock 시작 시점 + InitialDelay 0 기준 |
| Task 9 (씬 와이어링) | §12.C — 본 기획서가 좌표·size 수치 단정 |

- **내부 일관성**: §9 표의 모든 수치가 §2~§8 본문 및 §12 구현 요청사항과 동일. Zone size (24, 1, 24) 가 §2.3 / §3.3 / §4.2 / §5.2 / §9 / §12.C 에서 글자 그대로 동일. v1.1 정정으로 §6 (SimpleMover._clampZone) / §9 (BattleZone 본체 추가 컴포넌트 없음) / §11 (#5·#6·#9 폐기, #7·#8 재정의, #10 신규) / §12.B (Knight 변경 0건, SimpleMover 와이어링) / §13 (항목 2 해소, 3·4 신규) 가 일관.
- **시그니처/명명 일관성**: 스펙·플랜이 락한 식별자 (BattleZone, HeroEntryDriver, OnHeroReachedCenter, SetMonsterEngaging, GetRandomSpawn, ClampInside, IsInside, SimpleMover._clampZone) 본문 전체에서 변형 없음. v1.1 신규 식별자 `BattleZone.ClampInside` / `SimpleMover._clampZone` 가 §6.1 / §6.2 / §9 / §12.B / §12.E / §13 / spec §3.2 §3.3 / plan Task 3 Task 4 와 글자 그대로 동일.
- **모호 표현**: "잠정", "권장" 등의 단어 사용 — 모두 §13 사용자 결정 표로 회수. "약", "대략", "충분히" 검산 어림 표현은 §2.2 / §3.3 / §5.2 의 수치 산출 후 정수 단정으로 종결.
- **스코프**: 단일 BattleZone 인스턴스 + entry 시퀀스 + 상태머신 + 영웅 zone-clamp — 4개 결합 시스템이지만 단일 구현 단위. 분할 불필요.
- **구현 요청사항 완전성**: §12 A~F — Enum/Interface(없음 명시) / SimpleMover._clampZone 런타임 주입 (BindClampZone, v1.1 — Knight 프리팹 변경 0건) / 씬 오브젝트 구조 / 에셋 키(없음) / SO 스키마 (BattleZone + SimpleMover._clampZone + BindClampZone) / Gizmo 누락 없음.

**Self-Review 결과**: 통과 (v1.2 = v1.1 4항목 + v1.2 2항목 보강 후 통과. v1.2 보강 — (a) §3.3 / §4.2 / §2.4 / §13 sync 표의 spawn 좌표 ±14 표현을 모두 ±14.4 단일 단정으로 통일. §3.3 의 "정수 단정 ±14m" 문구 제거. 옵션 갱신 단서도 ±17/±10 → ±17.4/±10.4. (b) §13 sync 표 끝에 spec 본문 일괄 갱신 완료 (2026-05-29 메인) 후속 작업 한 줄 명시. v1.1 보강은 다음 4항목 — (1) design-reviewer BLOCKER B1 정정 — §6 전체 인비저블 벽 → SimpleMover._clampZone 으로 교체, §11 엣지 #5·#6·#9 폐기 + #7·#8 재정의 + #10 신규, §12.B Knight CapsuleCollider 부착 항목 폐기 → SimpleMover 와이어링으로 교체, §12.F Gizmo 자홍색 항목 폐기, §13 항목 2 해소 표시. (2) design-reviewer BLOCKER B2 정정 — §6.0 코드 현실 검증 (몬스터 6종 Dynamic RB 부착 확인) + §6.4 BattleZone Rigidbody 추가 불필요 결정, §9 데이터 표 BattleZone Rigidbody 행 / 인비저블 벽 두께·높이 행 제거, §12.C BattleZone 씬 오브젝트 구조에서 Rigidbody 줄 제거. (3) design-reviewer BLOCKER B3 정정 — §13 끝에 spec/plan/기획서 sync 상태 명시 + 향후 zone 크기 변경 시 sync 책임자 명시. (4) advisor 2차 review 후 § 헤더 핵심 메커니즘 #1 의 "Awake 시 4면 인비저블 벽 자동 생성" 문구 제거 + #4 영웅 zone 차단 추가, §1 개요 표 §6 행 라벨 갱신, §11 #7 (벽 모서리 끼임) 도 SimpleMover 클램프 기준으로 재정의, §10.2 페이싱 박스에 컨셉서 §4.1 후속 갱신 필요 명시 (W2 트래킹), 상단 버전 v1.0 → v1.1 / §15 v1.1 항목 추가). v1.0 작성 시 보강 내역 (Knight Collider / advisor 1차 frustum 정정 / 옵션 3 폴백) 은 §15 변경 이력의 v1.0 줄에 보존.

---

## 15. 변경 이력

- **v1.0 (2026-05-29)**: 초안. 스펙 + 플랜 입력 기반으로 도메인 결정 채움. Zone 크기 산출(§2 — 카메라 frustum 정밀 계산 후 inscribed 폭 27.7m × 깊이 39.4m, zone (24,1,24) 가 폭 87% 부합)·spawn 거리 결정(§3 — 몬스터 max moveSpeed 2.4)·spawn point 12개(§4)·hero entry 15m / 5초 행진(§5)·영웅 Collider 누락 발견 후 §12.B 신규(§6.2 + 옵션 3 폴백 명시)·BattleZone Rigidbody 결정(§6.3)·BattleClock 시작 시점 명세(§7)·Gizmo 포함(§8) 정리. §13 사용자 결정 2건 위임 (Zone 크기 PlayMode 후 옵션 C 갱신 가능 + 벽 차단 실패 시 폴백).

- **v1.1 (2026-05-29)** — design-reviewer BLOCKER B1/B2/B3 정정. v1.0 의 영웅 차단 결정 (인비저블 벽 + Knight CapsuleCollider + BattleZone Kinematic Rigidbody 3중) 폐기. 폐기 사유 — 몬스터 6종 프리팹의 Dynamic Rigidbody 가 인비저블 벽(non-trigger BoxCollider)에 물리적으로 막혀 zone 진입 불가 → OnTriggerEnter 발화 안 됨 → Marching/Engaging 전환 마비 → 영웅 AI nearest-target 후보 0 → 전 시스템 마비.
  - **B1 정정** — 영웅 zone 차단을 **SimpleMover._clampZone 인스펙터 옵션 (영웅 한정)** 으로 단정. `BattleZone.ClampInside(Vector3)` 메서드 추가. 영웅 프리팹 Knight.prefab 직접 변경 0건 (§12.B 항목 교체).
  - **B2 정정** — BattleZone 본체 Rigidbody 추가 안 함. 코드 현실 검증 (Wisp.prefab 등 몬스터 6종이 이미 Dynamic Rigidbody 부착) 후 trigger 발화 자연 보장 확인 (§6.4).
  - **B3 정정** — spec / plan / 기획서 3 문서 sync 상태 §13 끝에 명시. 향후 zone 크기 변경 시 sync 책임자 명시.
  - **W2 처리** — 컨셉서 §4.1 "정중앙 등장" 서술과 본 결정 (zone 서쪽 밖 X=-15 등장) 충돌 트래킹 → §10.2 페이싱 박스 + §13 항목 3 에 컨셉서 별도 PR 갱신 필요 명시.
  - **advisor 2차 review 후 보강** — 영웅이 풀 스폰 (`CHMPool.Instance.Pop`) 이라 씬 인스펙터 와이어링이 작동 불가하다는 메커니즘 충돌 발견 → §12.B 를 "런타임 주입 단일 갈래" 로 단정 (SimpleMover.BindClampZone(BattleZone) public 메서드 추가 + BattleController.SpawnHero 의 Pop 직후 호출). §13 끝에 Plan Delta 필요 표 (Task 4 Step 5 / Task 8 Step 2 / Task 9 Step 4-1) 신규.
  - **부수 갱신** — § 헤더 핵심 메커니즘 #1·#4 / §1 개요 표 §6 행 라벨 / §8 Gizmo 표 (자홍색 행 폐기) / §9 데이터 요약표 (인비저블 벽 두께·높이·Rigidbody·Knight Collider 행 제거, SimpleMover._clampZone 행 추가) / §11 엣지 케이스 (#5·#6·#9 폐기, #7·#8 재정의, #10 신규) / §12.B (Knight 프리팹 변경 → SimpleMover.BindClampZone 런타임 주입 교체) / §12.C (Rigidbody / 인비저블 벽 두께·높이 줄 제거, 영웅 와이어링은 런타임 주입 단서) / §12.E (SimpleMover._clampZone 행 + BindClampZone 행 추가) / §12.F (Gizmo 자홍색 항목 폐기 단서) / §14 Self-Review 스펙 커버리지 표 §3.3 행 갱신.

- **v1.2 (2026-05-29)** — design-reviewer 3차 BLOCKER 2건 정정 (3회 한도 도달).
  - **BLOCKER #1 정정** — §13 sync 표 끝에 "spec 본문 일괄 갱신 완료 (2026-05-29 메인)" 후속 작업 한 줄 명시. v1.1 시점에서 §13 표는 "spec/plan 일치" 단정했으나 spec 본문 §2.1/§2.2/§3.1/§3.4/§4.2/§12.1 에 v1 인비저블 벽 잔존 결정이 남아있던 모순을, 메인 오케스트레이터가 spec 본문을 SimpleMover._clampZone 으로 일괄 갱신하여 해소. 본 기획서는 sync 결과를 §13 sync 표 후속 박스에 명시 기록.
  - **BLOCKER #2 정정** — spawn point 좌표를 **±14.4 단일 단정**으로 통일. v1.1 §3.3 마지막 줄 "정수 단정 ±14m" 문구 + §4.2 표 12행 ±14 좌표 + §2.4 "(±14, 0, 0)" 호환 서술 + §3.3 옵션 갱신 단서 (±17 / ±10) 를 모두 ±14.4 (옵션 갱신은 ±17.4 / ±10.4) 기준으로 갱신. 근거 — Phantom moveSpeed 2.4 × 1.0초의 정확한 산출값, plan / spec 도 ±14.4 기준으로 정합, "1초 march 보장 = 스워밍 페이싱" 시각 신호 명확.
  - **3 문서 일치 단정 수치** (v1.2 sync 결과) — zone size = (24, 1, 24) / spawn point 거리 = 2.4m (zone edge ±12 → spawn ±14.4) / hero entry = (-15, 0, 0).
