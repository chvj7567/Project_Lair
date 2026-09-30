# FX 2D 도트 전환 — 영웅 스킬·상태 FX 6종을 12fps 도트 시트로 교체 + 데미지 숫자 도트 연출

> 입력: 사용자 요청(메인 경유, 2026-09-30) "FX 이펙트도 적용해줘". 승인 완료 시안 `.mockups/fx-dot-pixel.html` + 추출 시트 `Assets/_Lair/Art/Sprites/FX2D/*_Sheet.png` 7장 + `FX2D_SheetSpec.json`.
> 선행 규약 재사용: `docs/design/monster-2d-conversion.md`(빌보드·`Mat_Monster2D`·PPU·풀 리셋), `docs/design/hero-2d-conversion.md`(피벗=발바닥 커스텀 피벗·임포트 규격·일회용 에디터 툴 Rule 04 §3).
> 범위 제외: 피격 임팩트 2종(`HitImpact`/`MonsterHitImpact`)은 시안에서 제외 → 기존 CFXR 유지(무변경).
> 원칙: **판정(게임플레이) 로직 0 변경, 시각 표현만 교체.** 모든 "현행" 값은 코드·프리팹 실측.

---

## § 헤더

- **목표**: 영웅 스킬 FX 3종(`HeroDashConeFx`/`HeroNovaFx`/`HeroOrbitBladeFx`)과 상태 FX 3종(`PoisonAura`/`TimeStopShield`/`FearSkull`)의 3D 프리미티브·CFXR 파티클 연출을 **도트 프레임 시트 재생**으로 교체하고, 데미지 숫자(`DamagePopup`)에는 도트 프레임 연출을 얹는다.
- **검증 가설**: (a) 영웅·몬스터가 2D 도트로 바뀐 전장에서 FX 도 도트로 통일되면 3D 파티클이 섞여 튀던 이질감이 사라지는가. (b) 시각 FX 의 크기·방향이 판정 반경·각도와 어긋나지 않아 "범위가 보이는 만큼 맞는다"는 가독성이 유지되는가. — 데미지·쿨다운·반경·개수 등 밸런스 값은 건드리지 않으므로 밸런스 가설은 없다.
- **현재 단계 범위 적합성**: **범위 내**. CLAUDE.md §8 "아트/에셋 작업 허용 — 실제 스프라이트 등 에셋으로 교체 가능". 신규 카드·영웅·몬스터·스킬 0, 서버 무관.
- **핵심 메커니즘**: 신규 컴포넌트 1개 `SpriteSheetFx`(프레임 배열 + 12fps + 루프/1회 + 종료 시 풀 반환)를 6개 FX 프리팹에 붙이고, 기존 스폰 코드(`HeroSkillFx`·각 Aura/Effect)가 정한 **위치·스케일 계약을 그대로 두고** 프리팹 내부만 스프라이트로 바꾼다. 지면 FX 3종은 바닥에 눕히고, 서 있는 FX 3종은 카메라 빌보드로 세운다. 데미지 숫자는 시트를 쓰지 않고 현행 TMP(Galmuri11 도트체)에 12fps 스텝 모션을 얹는다(§6).

---

## 1. 현행 실측 (코드·프리팹 확인값)

카메라(Battle): Orthographic size 7, 피치 50°(회전 x=0.4226=sin 25°), 위치 (0,10,−10) → 화면 위 = 월드 +Z, 지면 원의 화면 세로 압축비 = sin 50° = **0.766**. 바닥 `MapBackground` 는 SpriteRenderer(y=0.01, sortingOrder 기본 0). 캐릭터 몸 SpriteRenderer sortingOrder 0, 강화·스테이지 오버레이 1(Wisp/Knight 프리팹 실측).

| FX (`EVisual`) | 현재 프리팹 구성 | 스폰 코드 · 위치 · 크기 계약 | 수명 · 반환 | 풀 워밍(`BattleController`) |
|---|---|---|---|---|
| `DamagePopup` | 루트(`CHPoolable`+`DamagePopup`) + 자식 `Text`(TMP, 폰트 Galmuri11 SDF, m_fontSize 4, `CHText`) | `HitFeedbackSpawner.SpawnPopup` → `Play(pos, amount, color)`. 부상 1.2u(선형), 0.7s, 선형 알파 페이드, 카메라 회전 복사, 명도 분기 외곽선(#1A1A1A/#FFFFFF, 임계 L=128) | 0.7s 후 자체 `CHMPool.Push` | 40 |
| `PoisonAura` | 루트 스케일 (2.5, 0.1, 2.5) 원판 + `CHPoolable`(스크립트 GUID 부착), ReturnToPoolAfter 없음 | `PoisonAura.OnAttached` → `Pop` 후 `position = (hero.x, 0.05, hero.z)`, **스케일 미지정(프리팹 고정 2.5 = 지름 → 반경 1.25)**. 판정 반경 `_radius` 기본 1.25(`HeroPoisonAuraEffect`). 영웅 이동을 따라가지 않음 | 지속 5s(`_duration`). `OnDetached` 가 `Push` | 2 |
| `TimeStopShield` | 루트 `CHPoolable` + 중첩 CFXR `Shield Leaves A (Lit)`, 스케일 1 | `TimeStopAura.OnAttached` → 위치 `(hero.x, hero.y + 0.5, hero.z)`, 스케일 미지정. 영웅 이동·공격 정지 동안 유지 | 부착 5s. `OnDetached` 가 `Push` | 2 |
| `FearSkull` | 루트 `CHPoolable` + `ReturnToPoolAfter(1.6s)` + 중첩 CFXR `Skull Head Alt` | `FearEffect.Apply` → `HeroSkillFx.SpawnAttached(FearSkull, heroT, (0, **2.1**, 0), 1f)` — 영웅 자식으로 부착, 이동 추종. 지속 3s(`_duration`)와 별개인 1회성 | 1.6s 후 자동 `Push` | 2 |
| `HeroDashConeFx` | 루트 `CHPoolable`+`ReturnToPoolAfter(0.45s)`, MeshFilter(`HeroDashFx_Fan.mesh`)·MeshRenderer **비활성**, 자식 CFXR `Fire Explosion B`(스케일 1.4) | `DashStrikeRuntime` → `HeroSkillFx.SpawnCone(key, heroPos, dir, DashLength)`: 위치 `(hero.x, 0.1, hero.z)`, 회전 `LookRotation(dir(XZ))`(로컬 +Z=축), 스케일 = `DashLength`(**7**). 판정 `DamageMonstersInCone(dir, 7, ConeHalfAngle **35°**)` — 전체각 70°. 쿨다운 3s, 발동 즉시 판정(영웅은 이동하지 않음) | 0.45s 후 `Push` | 4 |
| `HeroNovaFx` | 루트 `CHPoolable`+`ReturnToPoolAfter(0.45s)`, MeshRenderer **비활성**(Sphere 메시), 자식 CFXR `Hit Light B (Air)` | `AoeNovaRuntime` → `HeroSkillFx.SpawnAt(key, heroPos, Radius*2)`: 위치 = `HeroPosition`, 스케일 = **7**(= Radius 3.5 × 2). 쿨다운 7s, 즉시 판정 `DamageMonstersInRing(0, 3.5)` | 0.45s 후 `Push` | 4 |
| `HeroOrbitBladeFx` | 루트 `CHPoolable`(ReturnToPoolAfter 없음), MeshRenderer **비활성**(Sphere), 자식 CFXR | `OrbitingBladeRuntime.UpdateBlades`: 블레이드 **3개**(`_bladeCount`=3)를 각각 `SpawnTracked` 로 Pop, 매 틱 `position = heroPos + (cos a, 0, sin a) × OrbitRadius(1.4)`(a = `_angleDeg` + 120°×i, 증가 = 화면 **반시계**), 스케일 = `BladeSphereRadius × 2` = 1.8. 공전 180°/s(1바퀴 2.000s). 판정 = 구 반경 0.9 × 3개, 0.3s 간격 | 스킬 비활성(`OnDeactivate`) 시 `Push` | 4 |

관찰: 세 스킬 FX 프리팹의 자체 MeshRenderer 는 이미 꺼져 있고 실제 시각은 중첩 CFXR 자식이 담당한다. 교체는 "자식 CFXR 제거 + 스프라이트 자식 추가"다.

---

## 2. 시트 스펙 → 월드 환산 (프리팹 배선 단일 진실 표)

시트 공통: 셀 128×96, **12fps**(프레임 1개 = 1÷12 = 0.0833s), 좌→우·위→아래, 프레임 수·루프는 `FX2D_SheetSpec.json` 값(아래와 동일, 어긋나면 JSON 이 진실).

**환산 규칙**: 기존 스폰 코드의 스케일 계약을 바꾸지 않는다. 각 FX 의 "시트 안 기준 길이(px)"를 판정 값에 맞춰 **PPU 를 역산**한다(수평 기준). PPU = 기준 px ÷ (기준 월드 길이 ÷ 루트 스케일).

| FX | 프레임 × fps = 길이 | 루프 | 시트 크기(열×행 → px) | 기준 px(시안 실측) | 판정·계약 값 | 루트 스케일(코드 계약) | **PPU** | 검산(월드 길이) | 피벗 = 발 기준점 (Unity 정규화) |
|---|---|---|---|---|---|---|---|---|---|
| `PoisonAura` | 16 × 12 = 1.333s | 루프 | 6×3 → 768×288 | 링 반지름 rx = 36 | 반경 1.25 | 2.5(프리팹 고정) | **72** | 36 ÷ 72 × 2.5 = 1.25 | (64,72) → **(0.5, 0.25)** |
| `TimeStopShield` | 16 × 12 = 1.333s | 루프 | 6×3 → 768×288 | 더미 영웅 높이 16 = 1u | 없음(영웅 몸 높이 1u 기준) | 영웅 lossyScale(§5.5) | **16** | 16 ÷ 16 = 1.0 | (64,86) → **(0.5, 0.104167)** |
| `FearSkull` | 14 × 12 = 1.167s | 1회 | 6×3 → 768×288 | 더미 영웅 높이 16 = 1u | 없음(영웅 자식이라 영웅 스케일 상속) | 1(코드 `1f`) | **16** | 16 ÷ 16 = 1.0 | (64,86) → **(0.5, 0.104167)** |
| `HeroDashConeFx` | 10 × 12 = 0.833s | 1회 | 6×2 → 768×192 | 부채꼴 도달 L = 92 | 길이 7 | 7(`DashLength`) | **92** | 92 ÷ 92 × 7 = 7.0 | (16,74) → **(0.125, 0.229167)** |
| `HeroNovaFx` | 10 × 12 = 0.833s | 1회 | 6×2 → 768×192 | 최대 링 반지름 6 + 50 = 56 | 반경 3.5 | 7(`Radius*2`) | **112** | 56 ÷ 112 × 7 = 3.5 | (64,62) → **(0.5, 0.354167)** |
| `HeroOrbitBladeFx` | 24 × 12 = 2.000s | 루프 | 6×4 → 768×384 | 궤도 rx = 40 | 궤도 반경 1.4 | 1.4(`OrbitRadius`, §5.6 변경) | **40** | 40 ÷ 40 × 1.4 = 1.4 | (64,66) → **(0.5, 0.3125)** |

- 피벗 산식: x = 발x ÷ 128, y = (96 − 발y) ÷ 96. 검산: Poison (96−72)÷96 = 0.25 · Nova (96−62)÷96 = 0.354167 · Orbit (96−66)÷96 = 0.3125 · Dash (96−74)÷96 = 0.229167 · TimeStop·Fear (96−86)÷96 = 0.104167.
- 부채꼴 각도 정합: 시안 반각 0.61rad = 34.95° ↔ 판정 35° → 차 0.05°(무시). 시안 부채꼴 도달 프레임: F3(4프레임 = 0.333s)에서 L 도달 — 판정은 발동 즉시 전 범위이므로 FX 확장이 판정보다 최대 0.333s 늦게 완성된다(§9 위험 R3).
- 궤도 회전 정합: 시트 1루프 24프레임 ÷ 12fps = 2.000s = 360° ÷ 180°/s ✓.
- 기준 px 의 사용처는 오직 이 표다. 시트에서 지면 링 반지름을 다시 재지 않는다.

---

## 3. 렌더 기하 결정 (핵심 — 2D 시트를 3D 카메라 위에 올리는 방법)

### 3.1 문제 두 가지 (실측 근거)

1. **빌보드를 바닥에 서게 하면 발 아래 절반이 바닥에 잘린다.** 카메라 정면 빌보드는 카메라 위 벡터 (0, 0.643, 0.766) 방향으로 서므로 피벗 아래 픽셀은 y < 0(바닥 아래)로 파고들어 깊이 테스트로 가려진다. 몬스터·영웅은 몸이 발 위에만 있어 무해했지만, 시트의 지면 링·궤도 앞쪽 블레이드·Fear 확장 링은 **발 기준점 아래에도 픽셀이 있다**(Poison 링 하단 +15px, TimeStop 링 +8px, Orbit 앞 블레이드 +22px).
2. **시트의 지면 원은 세로 압축비 0.5(Poison 0.4167)로 그려져 있다.** 실제 카메라의 압축비는 0.766. 빌보드 그대로 쓰면 지면 판정 원이 화면에서 시각 링보다 세로로 1.53배(Nova) 크다.

### 3.2 결정 — FX 를 두 부류로 나눈다

| 부류 | FX | 방식 | 근거 |
|---|---|---|---|
| **지면 FX** | `PoisonAura` · `HeroNovaFx` · `HeroDashConeFx` | **바닥에 눕힘**(스프라이트 자식 로컬 회전으로 XZ 평면, 그림 위쪽 = 월드 +Z) + **세로 보정 스케일**(그림 세로축 × k) | 눕히면 문제 1 이 없다(현행 부채꼴도 바닥 y=0.1 에 눕혀 스폰 — `HeroSkillFx.ConeFloorLiftY`). 보정 k = 1 ÷ 시트 압축비 → 눕힌 뒤 카메라가 0.766 을 적용해 **수평·수직 모두 판정 반경과 일치**(문제 2 해소). 부채꼴은 루트 yaw(`LookRotation(dir)`)로 **방향 자유 회전**(빌보드는 방향 회전 불가) |
| **서 있는 FX** | `TimeStopShield` · `FearSkull` · `HeroOrbitBladeFx` | **카메라 빌보드**(회전 복사) + 머티리얼 `ZTest Always` + sortingOrder 10 | 문제 1 은 깊이 테스트를 끄고 캐릭터보다 위에 그리는 것으로 해소. 이 셋은 지면 판정 원이 아니라 영웅 위·주위 표식이라 압축비 정합이 필요 없음(궤도는 §9 R2) |

- 보정 스케일 k(스프라이트 자식 로컬 Y 스케일): **Poison 2.4**(= 36 ÷ 15, 시안 ry=15) · **Nova 2.0**(= 1 ÷ 0.5) · **Dash 2.0**(= 1 ÷ 0.5, 시안 sq=0.5). 눕힌 그림의 위쪽 = 월드 +Z 이므로 Y 스케일 = 월드 Z 방향 확대.
- 부채꼴 눕힘 방향: 그림의 +X(부채꼴 축) → 루트 로컬 +Z(`dir`), 그림의 +Y → 루트 로컬 −X, 앞면이 위(+Y)를 향함. **검증**: `dir = 월드 +X` 로 발동했을 때 화면에서 원본 시트와 같은 방향(오른쪽으로 벌어짐)으로 보이고 그림이 좌우·상하 뒤집히지 않아야 한다. 회전 오일러 값은 gameplay-programmer 가 씬 뷰에서 이 검증으로 확정한다.
- 결과 픽셀 종횡비(화면): Nova·Dash 1 : 1.53, Poison 1 : 1.84(세로로 길어진 도트). 원인은 시안이 지면 원을 이미 눌러 그렸기 때문 — §9 D1 에서 top-down 재추출(픽셀 1 : 0.766, 방향·판정 완전 정합)을 개선안으로 분리.

### 3.3 sortingOrder · 머티리얼

| 대상 | sortingOrder | 머티리얼 | 근거 |
|---|---|---|---|
| 지면 FX 3종 | **−10** | `Mat_Monster2D`(재사용 — 알파 블렌드·ZWrite Off·Cull Off·Unlit 실측) | 캐릭터 몸 0·오버레이 1·바닥 `AuraShadow`/`Aura` 0 보다 아래 → "캐릭터 > 범위 > 맵" 유지 |
| `MapBackground`(Battle.unity) | **−20** | (기존) | 현재 0 이면 FX −10 이 맵 뒤로 숨는다. 씬 SpriteRenderer 1값 수정 |
| 서 있는 FX 3종 | **10** | 신규 `Mat_FX2D` = `Monster2DSprite` 셰이더 + `_ZTest`=Always(8) | 캐릭터·오버레이·HP 바 위에 그려짐. 시안의 "링이 영웅 뒤"는 재현되지 않고 링이 발 위에 얹힌다(수용, §9 R5) |
| 셰이더 변경 | — | `Monster2DSprite.shader` 에 프로퍼티 `_ZTest`(CompareFunction, 기본 4=LEqual) + `ZTest [_ZTest]` 1줄 추가 | 기존 머티리얼은 프로퍼티가 저장돼 있지 않아 셰이더 기본값 LEqual 로 동작 = **기존 몬스터·영웅 렌더 변화 0** |

- 가산(additive) 블렌드는 쓰지 않는다 — 시안 팔레트가 알파 도트 기준이고 `Mat_Monster2D` 로 충분(신규 셰이더 0).

---

## 4. 재생 방식 결정

### 4.1 기존 방식 조사

| 기존 방식 | 내용 | FX 재사용 가능? |
|---|---|---|
| 몬스터/영웅 클립 재생 | 스프라이트 클립 + `Animator`(상태 머신·`Speed/Attack/Hit/Dead` 파라미터) + Any State 전이 | ✗ 상태 머신이 필요 없다(FX 는 "0프레임 → 마지막 프레임" 1방향, 상태 0개). Animator 1개 = 클립 1 + 컨트롤러 1 + 풀 리셋 위험(Keep State On Disable) — FX 6종이면 에셋 12개 추가 |
| `MonsterVisual2D` | 카메라 회전 복사 + **`_root.forward.x` 로 flipX** + 오버레이 동기 | ✗ 컴포넌트 재사용 불가. flipX 는 부모 방향을 읽는데 `FearSkull` 은 영웅 자식이라 영웅이 좌우로 돌 때 해골이 뒤집힌다. **빌보드 개념(카메라 회전 복사)만 재사용** |
| `Mat_Monster2D` + `Monster2DSprite` 셰이더 | 알파 블렌드 스프라이트 | ✓ 지면 FX 에 그대로, 서 있는 FX 는 `_ZTest` 1줄 추가 변형(§3.3) |
| `ReturnToPoolAfter` | 고정 초 후 `Push` | △ 시트 길이(N÷12)와 초 값을 이중 관리하게 됨 → 1회성 FX 3종에서 제거하고 재생 종료로 대체 |

### 4.2 결정 — Animator 대신 코드 재생 신규 컴포넌트 1개 (`SpriteSheetFx`)

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| A. Animator 클립 ×6 + 컨트롤러 ×6 | 몬스터·영웅과 동일 방식 | 에셋 12개, 상태 없는 FX 에 컨트롤러 과잉, 풀 재사용 시 상태 누수 관리, 궤도 FX 의 위상 동기(§5.6)를 파라미터로 풀기 어려움 | ✗ |
| **B. `SpriteSheetFx`(프레임 배열 + fps + loop + 종료 이벤트)** | 신규 에셋 0(프리팹 필드만), 시트 스펙 JSON 값을 필드에 그대로 기록, `OnEnable` 리셋이 단순, 수동 위상(`SetLoopPhase`)으로 궤도 판정 동기 | 신규 컴포넌트 1개(최소) | ✅ 채택 |

**`SpriteSheetFx` 계약(구현 방식은 gameplay-programmer 판단, 아래는 동작 요구)**
1. 루트가 유일한 외부 진입점(Rule 02 §10): 하위 `SpriteRenderer`(자식 `AuraFx`)는 `[SerializeField] private`, 외부는 `ISpriteSheetFx` 로만 접근.
2. `OnEnable` 리셋(Rule 03 §4): 경과 시간 0, 프레임 0 표시, 종료 상태 해제, 카메라 재캐시.
3. 프레임 = `floor(경과 × fps)`(경과는 스케일 시간 — 기존 `ReturnToPoolAfter` 와 동일한 시간축). 루프면 `% frames`, 1회면 `frames` 도달 시 종료: `Finished` 이벤트 발행 → `_returnToPoolOnFinish` 가 true 면 `CHMPool.Push`(1회성 3종만 true).
4. `_billboard` true 면 매 `LateUpdate` `AuraFx` 회전 = 카메라 회전(서 있는 FX). false 면 프리팹에 저장된 눕힘 회전을 건드리지 않는다(지면 FX).
5. `_autoPlay` false 면 시간으로 진행하지 않고 `SetLoopPhase(0~1)` 로 프레임을 직접 지정(`floor(frac(p) × frames)`) — 궤도 FX 전용.
6. 프레임 계산은 순수 정적 함수로 분리해 EditMode 테스트 가능(§10).

---

## 5. FX 6종 상세 (현재 → 적용 후)

### 5.1 `HeroDashConeFx` — 돌진 부채꼴 (1회 · 지면)

| 항목 | 내용 |
|---|---|
| 적용 후 프리팹 | 루트(`CHPoolable`+`SpriteSheetFx`, `_billboard`=false, `_returnToPoolOnFinish`=true) → 자식 `AuraFx`(SpriteRenderer `Mat_Monster2D`, 눕힘, 로컬 Y 스케일 2.0, sortingOrder −10). 제거: 자식 CFXR 인스턴스, MeshFilter·MeshRenderer, `ReturnToPoolAfter` |
| 스폰 코드 | **변경 0줄**(`SpawnCone`: 위치 y=0.1 · yaw=`LookRotation(dir)` · 스케일=`DashLength`). 방향 회전은 루트 yaw 가 담당 → 부채꼴이 몬스터 무게중심 쪽으로 정확히 벌어짐 |
| 수명 | 0.45s → **0.833s**(10프레임). 재발동 최소 간격 3s(쿨다운) → 동시 최대 1개, 풀 4 유지 |
| 판정 정합 | 반각 34.95°(시안) ↔ 35°, 길이 7 ↔ 92px @PPU 92 × 스케일 7. 판정·넉백·데미지 코드 무변경 |
| 좌우 | 그림은 축 하나만 존재(방향 = yaw). flipX 사용 안 함. 시안의 영웅 잔상·더미 제외(투명) |

### 5.2 `HeroNovaFx` — AOE 노바 (1회 · 지면)

| 항목 | 내용 |
|---|---|
| 적용 후 프리팹 | 5.1 과 동일 구조, 로컬 Y 스케일 2.0 |
| 스폰 코드 | **변경 0줄**(`SpawnAt(..., Radius*2)` = 7, 위치 = `HeroPosition`) |
| 수명 | 0.45s → **0.833s**(10프레임), 쿨다운 7s → 동시 1개 |
| 판정 정합 | 링 반지름 56px @PPU 112 × 7 = 3.5 = `Radius`. 불꽃 알갱이는 링의 최대 1.1배(3.85u)까지 번짐(장식, 판정 아님) |

### 5.3 `PoisonAura` — 독 장판 (루프 · 지면)

| 항목 | 내용 |
|---|---|
| 적용 후 프리팹 | 루트 스케일 **(2.5, 2.5, 2.5)**(현행 y 0.1 → 2.5, 원판 두께가 사라지므로), `SpriteSheetFx`(`_loop`=true, `_returnToPoolOnFinish`=false) → 자식 `AuraFx`(눕힘, 로컬 Y 스케일 2.4, sortingOrder −10). 제거: 원판 메시·머티리얼 |
| 스폰 코드 | **변경 0줄**(위치 `(x, 0.05, z)`). 반환은 `OnDetached` 의 `Push`(현행) |
| 판정 정합 | 링 36px @PPU 72 × 2.5 = 1.25 = `_radius` 기본값. `_radius` 를 바꿔도 시각 반경이 따라가지 않는 점은 현행과 동일(프리팹 고정 스케일, 범위 밖) — 현재 SO 값 1.25 단일 |
| 루프 | 1.333s 루프. 5s 지속 동안 3.75회 반복 |

### 5.4 `FearSkull` — 공포 해골 (1회 · 서 있음, 영웅 자식)

| 항목 | 내용 |
|---|---|
| 적용 후 프리팹 | 루트(`SpriteSheetFx`, `_billboard`=true, `_returnToPoolOnFinish`=true) → 자식 `AuraFx`(빌보드, `Mat_FX2D`, sortingOrder 10). 제거: CFXR 자식, `ReturnToPoolAfter(1.6)` |
| 스폰 코드 | `FearEffect.FxLiftY` **2.1 → 0** 1곳. 사유: 해골 높이·발밑 확장 링이 시트에 이미 굽혀 있고 피벗이 영웅 발이므로 로컬 오프셋이 필요 없다(시트상 해골은 발 위 24~39px = 1.5~2.44u, 시안 더미 영웅 머리 위 0.5~1.4u — 옛 3D 스컬 중심 2.1u 와 같은 높이대) |
| 수명 | 1.6s → **1.167s**(14프레임). 공포 지속 3s 와 무관(현행과 동일) |
| 스케일 | 영웅 자식이라 영웅 스케일을 상속(S5 ×1.4 에서 함께 커짐). 코드의 `1f` 유지 |
| 좌우 | flipX 없음(해골·기운 좌우 대칭). 영웅이 좌우로 돌아도 빌보드 회전만 복사 |
| 피격 반전 간섭 | `HitFlash` 는 `Awake` 1회 수집이라 나중에 붙는 FX 는 수집되지 않는다. 그래도 렌더러 오브젝트 이름을 `AuraFx` 로 두어(접두 `Aura` 자동 제외) 재수집이 생겨도 반전되지 않게 한다 |

### 5.5 `TimeStopShield` — 시간 정지 아이콘 (루프 · 서 있음)

| 항목 | 내용 |
|---|---|
| 적용 후 프리팹 | 5.4 와 동일 구조, `_loop`=true, `_returnToPoolOnFinish`=false. 제거: CFXR 자식 |
| 스폰 코드 | `TimeStopAura.OnAttached`: 위치 `(x, y + 0.5, z)` → **`(x, y, z)`**(피벗이 영웅 발), 스케일 = 영웅 `lossyScale.x`(S1~S4 = 1, S5 = 1.4)를 `RequestVisualAt` 에 함께 전달. 반환은 `OnDetached`(현행) |
| 수명 | 5s 부착 동안 1.333s 루프 3.75회 |
| 시각 | 머리 위 시계 아이콘(분침 떨림) + 발밑 푸른 링. 영웅은 정지(이동·공격 정지 판정은 현행 로직 그대로) |

### 5.6 `HeroOrbitBladeFx` — 회전 블레이드 (루프 · 서 있음) — **구조 변경 필요**

시트는 **한 장에 블레이드 3개가 함께 도는 영상**이다. 현행 코드는 블레이드마다 FX 1개씩 3개를 각자 위치에 띄운다. 그대로는 못 쓴다.

| 항목 | 현행 | 적용 후 |
|---|---|---|
| FX 개수 | 3(`_blades[3]`, 각자 `_centers[i]` 추종) | **1**(영웅 위치 추종). 시트가 3개 블레이드를 포함 |
| 위치 · 스케일 | 각 `_centers[i]` · `BladeSphereRadius×2`=1.8 | `HeroPosition`(발) · `OrbitRadius`=**1.4**(PPU 40 검산 §2) |
| 프레임 진행 | 공전 각도를 코드가 직접 계산해 위치 갱신 | `_autoPlay`=false, 매 틱 `SetLoopPhase(**1 − frac(_angleDeg ÷ 360)**)` — 판정 구 위치(`ComputeCenters`)와 **같은 각도 변수에서 프레임을 뽑아 시각-판정 동기 보장**(자체 시계 드리프트·일시정지 어긋남 0) |
| 회전 방향 | 화면 반시계(월드 +Z = 화면 위, 각도 증가 = x→z) | 시안은 캔버스 y 가 아래로 자라 **시계 방향**으로 도는 영상 → 위상을 뒤집어 `1 − frac(...)` 로 재생해야 판정 구와 같은 쪽으로 돈다. 시작 위상: 각도 0 에서 블레이드 3개가 0°·120°·240°(시안 f=0 과 동일 집합) |
| 깊이(영웅 앞/뒤) | 3D 구가 실제 깊이로 정렬 | 시안은 영웅을 사이에 끼워 앞/뒤 블레이드를 나눠 그렸으나 영웅 더미가 제외돼 한 장으로 합쳐짐 → 시트 전체를 영웅 **위**(sortingOrder 10)에 그린다. 뒤쪽 블레이드 궤도(y 36 부근)는 영웅 몸(y 70~86)과 겹치지 않아 오류가 눈에 띄지 않음(§9 R2 확인 게이트) |
| 개수 정합 | `_bladeCount`(SO 3) | 시트는 3개 고정. SO 값이 3 이 아니면 시각·판정 개수 불일치 → EditMode 테스트로 SO=3 고정(§10) |
| 반환 | `OnDeactivate` 에서 각 `Push` | FX 1개를 `OnDeactivate` 에서 `Push`(현행 경로 유지) |

- 판정 코드(`ComputeCenters`·`DamageMonstersInSpheres`·`_hitInterval`·`_damage`) **변경 0줄**. 바뀌는 것은 `UpdateBlades`(시각)와 `_blades` 배열 → 단일 핸들뿐이다.
- 시각-판정 크기 불일치(블레이드 시각 반지름 9~12px = 0.32~0.42u ↔ 판정 구 0.9u)는 §9 D3 로 분리.

---

## 6. `DamagePopup` — 숫자 표시 방식

### 6.1 시안이 실제로 그리는 것

- 시트 `DamagePopup_Sheet.png` 는 **숫자가 구워진 영상**이다: 흰 "42"(스케일 2)와 노란 "128!"(스케일 3, 치명타 팝 바운스)가 시안 코드에 하드코딩된 채 12프레임짜리로 들어 있다(3×5 도트 폰트 `GL` 로 `text('42'…)`, `text('128!'…)`).
- 게임의 팝업은 **임의 정수 + 임의 색**(몬스터=종족색, 영웅=흰색, DoT=독색 등)이다. 구워진 시트로는 값을 표시할 수 없다 → 시트 직접 적용 불가.
- 게임에는 치명타 개념이 없다(`Play(pos, amount, color)` 에 플래그 없음) → 시안의 "!"·팝 바운스는 범위 밖.
- 이미 현행 TMP 폰트는 **Galmuri11(도트 한글 폰트, 전 UI 교체 완료 — 최근 커밋)** 이다.

### 6.2 대안 비교

| 안 | 내용 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| **A. TMP 유지 + 도트 연출만 얹기** | 값·색·외곽선 로직 그대로. 부상을 12fps 스텝(프레임 단위 계단)으로, 알파 페이드를 4단계 계단으로 바꾼다 | 값 표시 로직 0 변경(색 스탬프·명도 외곽선 유지), 신규 에셋 0, 이미 도트체, 시트 미사용 | 글자가 SDF 라 가장자리가 완전 도트 톱니는 아님 | ✅ 권장(§9 D2) |
| B. 도트 숫자 스프라이트 폰트 | 3×5 글리프 0~9 아틀라스 + 자릿수별 SpriteRenderer 조립 | 시안과 동일한 완전 도트 외형 | 신규 컴포넌트(자릿수 조립·풀링 자식), 색 틴트는 되나 **명도 분기 외곽선(밝은 색=검정 / 어두운 색=흰색)에 글리프 세트 2벌** 필요(몬스터 식별색에 #1F2937 등 어두운 색 존재), 재작업 크기 큼 | ✗ (이번 범위) |
| C. 보류 | 현행 그대로 | 무위험 | FX 통일감 없음 | ✗ |

### 6.3 A 안 동작 명세 (`DamagePopup.PlayCo` 의 시간 함수만 교체)

`n = floor(t × 12)`(t = 경과 초, 프레임 지수). 총 길이 0.7s·부상 1.2u 는 **불변**(`_duration`·`_rise` 필드 값 유지).

| 항목 | 현행 | 적용 후 |
|---|---|---|
| 부상 | `Up × 1.2 × (t ÷ 0.7)` 선형 | `Up × 1.2 × (1 − (1 − min(n ÷ 7, 1))³)` — 프레임 단위 ease-out(n=7 이후 1.2u 에 도달, 이후 정지) |
| 알파 | `1 − t ÷ 0.7` 선형 | n ≤ 4 → 1.00, n = 5 → 0.75, n = 6 → 0.50, n = 7 → 0.25, n ≥ 8 → 0.00 (계단 4단) |
| 위치 갱신 빈도 | 매 프레임 | n 이 바뀔 때만(12fps 계단) — 빌보드 회전 복사는 매 프레임 유지 |
| 색·외곽선·값·카메라 회전 | 유지 | 유지 |

검산: n=8 시작 = 8 ÷ 12 = 0.667s < 0.7s → 마지막 0.033s 는 투명(0.7s 시점 풀 반환 그대로). 시트(`DamagePopup_Sheet.png`)는 임포트·배선하지 않는다(파일은 참고 자료로 유지).

---

## 7. 시각·성능

### 7.1 텍스처 임포트 (6장, `DamagePopup_Sheet` 제외)

현재 6장은 기본 임포트(Sprite 모드 없음, PPU 100, 양선형 필터, 밉맵 on, 압축 on) 상태라 전부 재설정한다.

| 항목 | 값 |
|---|---|
| Texture Type / Sprite Mode | Sprite (2D and UI) / **Multiple** |
| Pixels Per Unit | §2 표의 FX 별 PPU(Poison 72 · TimeStop 16 · Fear 16 · Dash 92 · Nova 112 · Orbit 40) |
| Filter / Mip Maps / Compression | **Point** / **off** / **None** |
| sRGB / Alpha Is Transparency | on / on |
| Mesh Type | **Tight**(투명 여백 오버드로 절감) |
| Max Size | 1024 (최대 768×384) |
| 슬라이스 | Grid By Cell Size **128×96**, Padding 0, **Keep Empty Rects on**(빈 칸 포함 지수 = 행×열 위치 고정), 피벗 **Custom** = §2 표 피벗. 스프라이트 이름 = Unity 기본 `<시트명>_<지수>`(0 시작) |
| `_frames` 채움 | 지수 0 ~ (frames − 1) 을 지수 순으로. 남는 빈 칸(16f→18칸 중 2칸 등)은 제외 |

### 7.2 메모리·오버드로

- 시트 메모리(무압축 RGBA32): 768×288×4 = 884,736B × 3(Poison/TimeStop/Fear) + 768×192×4 = 589,824B × 2(Dash/Nova) + 768×384×4 = 1,179,648B(Orbit) = 2,654,208 + 1,179,648 + 1,179,648 = **5,013,504B (4.78MiB)**. 도트 원화라 압축 시 색 번짐이 나므로 무압축 유지.
- 동시 FX 상한: 지면 FX 는 스킬 쿨다운상 Dash 1 + Nova 1 + Poison 1(지속 5s) = 3, 서 있는 FX 는 Orbit 1 + TimeStop 1 + Fear 1 = 3, 데미지 팝업 최대 40. 지면 FX 3장이 겹치는 순간의 오버드로는 Tight 메시 기준 화면 절반 이하이므로 문제 없음 → 워밍 개수(`BattleController` 의 Poison 2 · TimeStop 2 · Fear 2 · Dash/Orbit/Nova 4)는 **변경 없음**.
- 이전 CFXR 파티클 시스템(파티클 수십 개·다수 머티리얼)이 스프라이트 1장으로 대체되므로 스킬 발동 프레임의 DrawCall 은 줄어든다(Profiler 게이트 §8).

---

## 8. 구현 요청사항 (gameplay-programmer 용)

### 8.1 Enum · Interface · 에셋 키 · 스키마

| 항목 | 내용 |
|---|---|
| Enum | **신규 없음.** `EVisual` 키(`PoisonAura`·`TimeStopShield`·`FearSkull`·`HeroDashConeFx`·`HeroNovaFx`·`HeroOrbitBladeFx`·`DamagePopup`)·프리팹 파일명·Addressable 주소 **전부 유지**(GUID 보존) |
| Interface | `ISpriteSheetFx`(`Lair.Character`, `CommonInterface.cs`): `int CurrentFrame { get; }` · `bool IsFinished { get; }` · `event Action Finished` · `void SetLoopPhase(float normalized)`. 소비자는 `OrbitingBladeRuntime`(`SetLoopPhase`)뿐이나 Rule 02 §10 에 따라 미리 정의 |
| 에셋 키 | 신규 Addressable 키 없음. 신규 비-Addressable: `Assets/_Lair/Art/Materials/Mat_FX2D.mat`(Rule 04 §2 Materials/) |
| SO / JSON 스키마 | **신규 없음.** `FX2D_SheetSpec.json`(기존)은 일회용 툴이 읽어 `_frames`·`_fps`·`_loop`·PPU·피벗을 설정하는 입력일 뿐 런타임 로드 대상 아님. 새 JSON row 클래스가 없으므로 Rule 00 데이터 구조 확인 게이트 **해당 없음** |

### 8.2 변경 파일

| 파일 | 변경 | 판정 영향 |
|---|---|---|
| `Scripts/Character/SpriteSheetFx.cs`(신규) | §4.2 계약 6항. 프레임 계산은 순수 정적 함수 분리. 필드: `_renderer` · `_frames`(`Sprite[]`) · `_fps`(12) · `_loop` · `_billboard` · `_autoPlay` · `_returnToPoolOnFinish` · `_poolable`(인스펙터 배선, 런타임 `GetComponent` 금지 Rule 02 §5) | 없음 |
| `Scripts/Character/CommonInterface.cs` | `ISpriteSheetFx` 추가 | 없음 |
| `Scripts/Character/DamagePopup.cs` | §6.3 시간 함수 교체(`PlayCo`) | 없음(시각) |
| `Scripts/Character/Skills/OrbitingBladeSkillData.cs` | `OrbitingBladeRuntime`: `_blades[]` → 단일 FX 핸들, 위치 `HeroPosition`·스케일 `OrbitRadius`, 매 틱 `SetLoopPhase(1 − frac(_angleDeg ÷ 360))`, `OnDeactivate` 에서 1개 `Push` | **판정 코드 0줄**(`ComputeCenters`·`DamageMonstersInSpheres`·`_hitInterval` 무변경) |
| `Scripts/Card/Auras/TimeStopAura.cs` | `RequestVisualAt`: y 오프셋 0.5 제거, 영웅 `lossyScale.x` 를 FX 스케일로 전달 | 없음 |
| `Scripts/Card/Effects/FearEffect.cs` | `FxLiftY` 2.1 → 0 | 없음 |
| `Art/Shaders/Monster2DSprite.shader` | `_ZTest` 프로퍼티 + `ZTest [_ZTest]`(§3.3) | 기존 머티리얼 동작 불변 |
| `Scenes/Battle.unity` | `MapBackground` SpriteRenderer sortingOrder −20 | 없음 |
| 일회용 에디터 툴(§8.4, 실행 후 삭제) | 임포트·프리팹 배선·머티리얼 생성 | — |
| `HeroSkillFx.cs`·`DashStrikeSkillData.cs`·`AoeNovaSkillData.cs`·`PoisonAura.cs`·`BattleController.cs` | **변경 없음** | — |

### 8.3 프리팹 배선 표 (기존 프리팹을 열어 수정 — 파일명·GUID·Addressable 유지)

| 프리팹 | 제거 | 추가·설정 |
|---|---|---|
| `HeroDashConeFx` | 자식 CFXR `Fire Explosion B` 인스턴스, MeshFilter(`HeroDashFx_Fan.mesh`)·MeshRenderer, `ReturnToPoolAfter` | 루트 `SpriteSheetFx`(`_fps`12, `_loop`false, `_billboard`false, `_autoPlay`true, `_returnToPoolOnFinish`true, `_frames`=`HeroDashConeFx_Sheet_0~9`, `_poolable`=루트 `CHPoolable`) · 자식 `AuraFx`(SpriteRenderer `Mat_Monster2D`, sortingOrder −10, 눕힘 회전 §3.2, 로컬 스케일 (1, 2.0, 1)) |
| `HeroNovaFx` | 자식 CFXR `Hit Light B (Air)`, MeshFilter·MeshRenderer, `ReturnToPoolAfter` | 위와 동일 구조, `_frames`=`HeroNovaFx_Sheet_0~9`, 눕힘 회전 Euler(90,0,0)(그림 위쪽 = 월드 +Z), 로컬 스케일 (1, 2.0, 1) |
| `PoisonAura` | 원판 메시·머티리얼, 루트 스케일 (2.5, 0.1, 2.5) | 루트 스케일 (2.5, 2.5, 2.5), `SpriteSheetFx`(`_loop`true, `_returnToPoolOnFinish`false, `_frames`=`PoisonAura_Sheet_0~15`), `AuraFx`(`Mat_Monster2D`, sortingOrder −10, Euler(90,0,0), 로컬 스케일 (1, 2.4, 1)) |
| `FearSkull` | 자식 CFXR `Skull Head Alt`, `ReturnToPoolAfter` | `SpriteSheetFx`(`_loop`false, `_billboard`true, `_returnToPoolOnFinish`true, `_frames`=`FearSkull_Sheet_0~13`), `AuraFx`(`Mat_FX2D`, sortingOrder 10, 로컬 스케일 1) |
| `TimeStopShield` | 자식 CFXR `Shield Leaves A (Lit)` | `SpriteSheetFx`(`_loop`true, `_billboard`true, `_returnToPoolOnFinish`false, `_frames`=`TimeStopShield_Sheet_0~15`), `AuraFx`(`Mat_FX2D`, sortingOrder 10) |
| `HeroOrbitBladeFx` | 자식 CFXR, MeshFilter·MeshRenderer | `SpriteSheetFx`(`_loop`true, `_billboard`true, **`_autoPlay`false**, `_returnToPoolOnFinish`false, `_frames`=`HeroOrbitBladeFx_Sheet_0~23`), `AuraFx`(`Mat_FX2D`, sortingOrder 10) |
| `DamagePopup` | — | **프리팹 무변경**(코드만) |
| `HitImpact`·`MonsterHitImpact` | — | **무변경** |

- 모든 `AuraFx` 스프라이트 초기값은 `_frames[0]`(프리팹 미리보기용). 풀 Pop 시 `OnEnable` 이 0 프레임으로 리셋.
- 지면 FX 눕힘 회전의 부채꼴 판(`HeroDashConeFx`)은 §3.2 방향 검증을 통과하는 오일러 값으로 저장.
- 잔존: `HeroDashFx_Fan.mesh`·중첩 CFXR 원본 프리팹 폴더는 삭제하지 않는다(다른 프리팹이 CFXR 사용, 부채꼴 메시는 사용처 0이 되지만 롤백 여지 — 육안 승인 후 별도 정리 커밋).

### 8.4 일회용 에디터 툴 (Rule 04 §3 — 실행 후 삭제)

- 메뉴: `Lair/FX2D/Apply Sheets To Prefabs`. 입력: `FX2D_SheetSpec.json`(frames·cols·rows·loop·foot·fps) + §2 PPU 표(툴 상수).
- 수행: ① 6장 임포트 설정·슬라이스·피벗 적용(§7.1) ② `Mat_FX2D.mat` 생성(셰이더 `Lair/Monster2DSprite`, `_ZTest`=8) ③ §8.3 표대로 6프리팹 수정 ④ `Battle.unity` `MapBackground` sortingOrder −20.
- 기대 결과: 콘솔에 프리팹 6개·시트 6장 처리 로그, `_frames.Length` = JSON `frames`, 예외 0건. 실행 후 툴 파일 삭제 + 프리팹·머티리얼·씬·텍스처 `.meta` 커밋.
- 완료 후 `Lair/Tests/Run EditMode Tests`(`project.md` editor_menu) → `Library/lair-test-result.json` 확인.

### 8.5 동작 요구 요약 (판정 불변식)

1. `DamageMonstersInCone`·`DamageMonstersInRing`·`DamageMonstersInSpheres`·쿨다운·데미지·반경·개수 값 **무변경**.
2. 스폰 위치·스케일 계약(§1 표)은 §5.6(Orbit)·§5.5(TimeStop)·§5.4(Fear) 3곳만 변경, 나머지 무변경.
3. 1회성 FX 는 재생 종료 시 `CHMPool.Push`(이중 반환 방지: `ReturnToPoolAfter` 제거), 루프 FX 는 기존 `OnDetached`/`OnDeactivate` 가 반환.
4. Pop 재사용 시 이전 프레임·종료 상태가 남지 않는다(Rule 03 §4 State Reset).

---

## 9. 검증 게이트

| 구분 | 항목 | 통과 기준 | 담당 |
|---|---|---|---|
| 자동 | EditMode 전체 | §10 신규 테스트 통과 + 기존 회귀(`PoisonAuraTests`·`HitFeedbackTests`·`OrbitingBlade*Tests` 등) 통과 | test-engineer |
| 자동 | PlayMode 회귀 | `HeroSkillFxAttachPlayTests`·`HitFeedbackPlayTests`·`HeroSkillRunnerPhasePlayTests` 통과(Orbit 단일 FX 반영 갱신 포함) | test-engineer |
| 육안(Battle 플레이 스크린샷) | ① 돌진: HP 90% 이하 진입 후 첫 발동 시점, 발동 후 0.083s(F0)·0.25s(F3)·0.75s(F9) 3장 | 부채꼴이 몬스터 무게중심 방향으로 벌어짐, F3 에서 길이 7u·반각 35° 와 화면상 일치, 바닥 잘림 없음 | 메인 → 사용자 |
| | ② 노바: HP 30% 이하 진입 후 첫 발동 F0~F9 | 링 바깥 가장자리가 반경 3.5u 판정 원과 일치(가로·세로) | 〃 |
| | ③ 회전 블레이드: HP 60% 이하 진입 후 2초 | 블레이드 3개가 판정 구가 도는 방향(화면 반시계)으로 돈다, 몬스터 피격 위치와 시각 블레이드가 같은 쪽 | 〃 |
| | ④ 독 장판: `HeroPoisonAura` 카드 적용 직후 + 3초 | 링이 반경 1.25u 원과 일치, 영웅이 안에서 밖으로 나가면 데미지 중단과 시각이 맞음 | 〃 |
| | ⑤ 시간 정지·공포: 각 카드 적용 직후 | 발 아래 링이 바닥에 잘리지 않음, 아이콘·해골이 영웅 머리 위, S5(×1.4) 영웅에서도 비례 유지 | 〃 |
| | ⑥ 데미지 숫자: 몬스터 다수 피격 장면 | 12fps 계단 부상·4단 알파, 색·외곽선 현행 유지 | 〃 |
| 성능 | 스킬 3종 + 카드 3종 동시 발동 프레임 | DrawCall·SetPass 가 CFXR 시절 이하(Profiler) | gameplay-programmer |
| qa-simulator | **불필요** — 판정·수치·타이밍 코드 변경 0(Orbit 은 시각 핸들만 변경). 단 §8.5-1 불변식 테스트가 실패하면 재검토 | — | — |

---

## 10. 테스트 관점 (test-engineer, EditMode)

| 대상 | 검증 |
|---|---|
| 프레임 계산 순수 함수 | fps 12, frames 16, 루프: t=0 → 0, t=1÷12 → 1, t=16÷12 → 0(루프 회귀). 1회 14프레임: t=13÷12 → 13(종료 아님), t=14÷12 → 종료. `SetLoopPhase`: p=0 → 0, p=0.999 → frames−1, p=1.25 → frac 0.25 → 지수 floor(0.25×24)=6(음수·1 초과 입력 안전) |
| 스펙 JSON ↔ 프리팹 | `FX2D_SheetSpec.json` 의 각 FX 에 대해 프리팹 `SpriteSheetFx` 의 `_frames.Length == frames`, `_loop == loop`, `_fps == 12`, 스프라이트 PPU·피벗 == §2 표 |
| 스펙 ↔ 수명 | 1회 FX 길이(frames ÷ 12): Dash 0.833 · Nova 0.833 · Fear 1.167. 루프 FX 는 `_returnToPoolOnFinish == false` |
| 재생/종료/풀 리셋 | Pop → 종료 → `Finished` 1회 발행 → Push. 재 Pop 시 `CurrentFrame == 0`·`IsFinished == false`. 1회성 3종에서 `ReturnToPoolAfter` 부재(이중 Push 방지) |
| 궤도 위상 | `_angleDeg` = 0 → 프레임 0, 90 → frame floor((1−0.25)×24) = 18, 180 → 12, 270 → 6(반시계 정합). `_bladeCount == 3` 고정 |
| 부채꼴 각도 정합 | `DashStrikeSkillData` 반각 35° ↔ 시안 0.61rad(34.95°) 허용오차 0.1° |
| 판정 불변 | 기존 `OrbitingBladeSkillTests`·`OrbitingBladeBoundaryTests`·Dash/Nova 스킬 테스트 무수정 통과(핸들 변경 반영 외 판정 assert 변경 0) |
| 데미지 팝업 | n=0 → 부상 0·알파 1.00, n=4 → 알파 1.00, n=5 → 0.75, n=7 → 부상 1.2·알파 0.25, n=8 → 알파 0. 색·값 텍스트 불변 |
| 셰이더 회귀 | `Mat_Monster2D` 에 `_ZTest` 프로퍼티가 저장되지 않아 기본 LEqual 로 동작(기존 렌더 회귀 0) |

---

## 11. 위험 · 열린 질문 (사용자 결정 필요 분리)

### 11.1 사용자 결정 필요

| ID | 질문 | 대안 | 권장 · 이유 | 결정 전 진행 방식 |
|---|---|---|---|---|
| **D1** | 지면 FX 3종(Poison/Nova/Dash)의 픽셀 종횡비. 시안이 지면 원을 이미 눌러 그려(0.5/0.4167) 판정 정합을 위해 눕힌 뒤 세로 보정(2.0/2.4/2.0)하면 화면 도트가 세로로 1.53~1.84배 길어진다 | (1) 현 시트 + 보정 스케일 (2) 3장을 **top-down(압축비 1)으로 재추출** — 도트 정사각(원근 압축만 남아 1 : 0.766), 보정 스케일 1, 부채꼴 방향·판정 완전 정합. 시안 JS 의 3개 압축 상수만 변경, 파일명 동일 교체(코드·프리팹 무영향) | **(1)로 먼저 배선 → 스크린샷 게이트에서 종횡비가 거슬리면 (2)**. 이유: 이미 승인된 시안·추출물로 즉시 진행 가능하고, (2)는 시트 교체만으로 언제든 전환된다 | (1) 로 진행 |
| **D2** | 데미지 숫자 방식 | A(TMP 유지 + 도트 연출) · B(도트 숫자 스프라이트 폰트) · C(보류) | **A**. 이유: 값 표시·색 스탬프·명도 외곽선 로직 0 변경, 폰트가 이미 Galmuri11 도트체, 시안 시트는 숫자가 구워져 있어 값 표시 불가(§6.1). 시안의 치명타 표현은 게임에 치명타가 없어 제외 | A 로 진행 |
| **D3** | 회전 블레이드 시각 반지름(0.32~0.42u)이 판정 구 반지름 0.9u 의 1/2 미만 — 현행 3D 는 판정과 같은 크기의 구를 그렸다 | (a) 수용(시안 그대로) (b) 시안의 블레이드·글로우를 키워 재추출 (c) 블레이드 주변에 판정 반경 표시용 옅은 링 추가 | **(a)로 진행 후 게이트 ③에서 "맞았는데 안 닿아 보인다"면 (b)**. 이유: 시트 교체는 코드 무영향. 플레이 체감 확인 없이 아트를 다시 그리는 비용을 먼저 쓰지 않는다 | (a) 로 진행 |
| **D4** | 시안의 "링이 영웅 뒤로 감기는" 표현 상실(서 있는 FX 는 영웅 위에 그려짐) | 수용(영웅 더미를 넣은 재추출은 영웅 외형이 스테이지마다 달라 불가) | 수용. 링 두께 1px·알파 0.6~0.8 이라 영웅 하체 위 겹침이 작음 | 수용 |
| **D5** | `DamagePopup_Sheet.png`(미사용)와 `HeroDashFx_Fan.mesh`·미사용 CFXR 참조 정리 시점 | 즉시 삭제 · 육안 승인 후 별도 정리 | 승인 후 정리(몬스터·영웅 2D 전환 §7.5·§10-4 선례와 동일) | 삭제하지 않음 |

### 11.2 위험 (결정 불요, 게이트로 확인)

| ID | 위험 | 완화 · 확인 |
|---|---|---|
| R1 | 돌진 판정 각도 ≠ 시트 각도가 될 가능성: 시트 반각은 34.95°로 고정이고 SO `_coneHalfAngle` 이 바뀌면 시각이 따라가지 못한다 | 시각만 시트에 고정하고 **판정은 SO 값을 그대로** 사용(판정 불변 원칙). 각도 변경 시 시트 재추출이 필요하다는 점을 §10 부채꼴 각도 테스트가 알린다(35° 이탈 시 실패) |
| R2 | 궤도 앞/뒤 깊이(영웅 사이 정렬) 미표현, 위상 뒤집기 방향 오류 가능성 | 게이트 ③ 스크린샷. 방향이 반대로 보이면 `SetLoopPhase` 인자에서 `1 −` 제거 1줄 수정 |
| R3 | 부채꼴 FX 가 4프레임(0.333s)에 걸쳐 확장 → 판정(즉시 전 범위)보다 시각이 늦게 완성 | 현행 CFXR 폭발(0.45s)도 즉시 판정과 같은 구조라 수용. 체감 문제면 시트 앞 프레임을 줄이는 재추출 |
| R4 | 서 있는 FX 의 `ZTest Always` 가 다른 오브젝트(HP 바 캔버스 등) 뒤에 가려져야 할 때도 위에 그려짐 | sortingOrder 10 으로 의도한 표식이라 수용. 게이트 ⑤ 확인 |
| R5 | `Battle.unity` 외 씬(Village 등)에 같은 FX 프리팹이 스폰되는 경우 `MapBackground` 별도 씬의 sortingOrder 가 −10 보다 높으면 지면 FX 가 가려짐 | 현재 FX 스폰은 전투 씬(`BattleController`·카드 효과) 한정. 마을 씬에 지면 FX 사용처가 생기면 그 씬의 바닥 order 를 −20 으로 |
| R6 | 영웅 S5(×1.4)에서 TimeStop 아이콘 스케일 = `lossyScale.x` 전달이 누락되면 상대적으로 작아 보임 | 게이트 ⑤ + §10 확인 |
| R7 | 발 기준점을 영웅 루트(`p.y`)로 가정 — 영웅 루트 y 가 발바닥이 아니면 지면 FX 가 뜸 | 현행 Fear 주석 "피벗=발밑", Poison 이 영웅 x,z 만 사용하는 계약과 동일. 게이트 ①~⑤ 스크린샷에서 확인 |

### 11.3 답변 요약 (요청 7-(a)~(d))

- (a) 데미지 숫자: D2 = A 권장. (b) 돌진 각도 불일치: 시각만 시트에 맞추고 판정 불변(R1, 35° 대 34.95° 는 사실상 일치). (c) 영웅 좌우/회전: 서 있는 FX 는 좌우 대칭이라 flipX 를 쓰지 않고, 부채꼴만 루트 yaw 로 방향 회전. (d) 새 JSON 스키마: **없음**.

---

## 12. Self-Review

- **Placeholder 잔존**: 0건(`미정`·`TBD`·`적절히`·`대략` 등 금지 표현 없음). 결정이 갈리는 곳은 D1~D5 로 "진행 방식"을 명시했고 각 결정은 코드 무영향(시트 교체)임을 밝혔다.
- **내부 일관성**: PPU·피벗·스케일이 §2 표 하나를 단일 진실로 두고 §5·§7.1·§8.3·§10 이 그 값을 인용. 수명(0.833·1.167·1.333·2.000s)이 §2·§5·§10 동일. `_billboard`·`_autoPlay`·`_returnToPoolOnFinish`·`SetLoopPhase`·`ISpriteSheetFx`·`Finished`·`AuraFx`·`Mat_FX2D` 표기 통일.
- **스코프**: 단일 구현 단위(컴포넌트 1 + 머티리얼 1 + 프리팹 6 + 스크립트 5곳 소수정). 판정 코드 0 변경.
- **UI 목업**: 해당 없음(승인된 `.mockups/fx-dot-pixel.html` 재사용, 신규 UI 화면 없음).
