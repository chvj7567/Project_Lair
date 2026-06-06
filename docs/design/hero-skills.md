# 영웅 스킬 시스템 (Hero Skills) — 기능 기획서

> 작성일: 2026-06-04 · 작성: game-designer
> 입력 spec: `docs/superpowers/specs/2026-06-04-hero-skills-design.md`
> 입력 plan: `docs/superpowers/plans/2026-06-04-hero-skills.md`
> 관련 컨셉: `docs/design/project_lair_concept.md` §8(밸런싱)·§11(MVP 범위) — 본 기획서가 정식 annotate (§9 참조)
> 데이터 SoT: `Assets/_Lair/Data/BalanceConfig.asset`(영웅·몬스터 스탯) · `docs/design/continuous-spawn-round.md`(스포너 주기·글로벌 캡)

---

## § 헤더

- **목표**: 영웅(적 모험가)이 HP 3페이즈(85%/65%/45%)로 점진 획득하는 Survivor.io식 자동 스킬(돌진 → AOE 노바 → 회전 블레이드)로 몰려오는 몬스터 무리를 쓸어담게 한다. 영웅을 능동적 위협(역서바이버)으로 만들어 카드 선택의 무게를 키운다.
- **검증 가설**: 영웅이 무리를 쓸어담는 능동적 위협이 되면 (1) 카드 선택의 무게가 커지는가, (2) 컨셉 §8 "영웅 2~4분 처치" 페이싱이 영웅 스킬이 켜진 상태에서도 유지되는가.
- **현재 단계 범위 적합성**: **범위 밖이었으나 사용자 명시 승격(2026-06-04)으로 §11 에 정식 추가됨.** 본 기획서가 컨셉 §11/§8 을 v0.7 로 annotate (§9). 비주얼은 §11.4 프리미티브 + 4색 원칙 유지, 사운드·메타·서버는 여전히 비작업.
- **핵심 메커니즘**: HP% 게이트 3페이즈로 스킬을 누적 획득(이전 페이즈 스킬 유지). 각 스킬은 쿨다운/인터벌마다 자동 시전 — 돌진은 무리 무게중심 방향 **부채꼴(cone)** 관통, 회전 블레이드는 영웅 주위를 공전하는 **3D 구(sphere) N개**가 각 구 반경에 닿은 몬스터만 지속 데미지(per-sphere overlap), 노바는 영웅 중심 디스크 일괄 데미지 + 넉백(비주얼 구). 데이터드리븐 폴리모픽 SO(plan §3) 로 수치는 인스펙터 필드.

> **[형태 변경 v0.8 — 2026-06-04 사용자 확정]** P1 직선 띠 → 부채꼴(각도 configurable), P2 링밴드 → 구 N개 공전 + per-sphere 히트, P3 비주얼 실린더 → 구. 변경 상세·수치 재산정·plan sync 노트는 §12. 본 헤더 외 §2.1·§2.2·§2.3·§3·§5.3·§8·§9 가 형태 변경을 반영해 갱신됨.

---

## §0. 데이터 출처 / plan sync 노트 (먼저 읽을 것)

본 기획서의 모든 수치는 **live `BalanceConfig.asset` 의 실제 값**을 단일 진실(SoT)로 산정했다 (Rule 00·본 기획서 §7: 밸런싱 데이터 SoT 는 `BalanceConfig.asset`). live 데이터와 어긋나는 출처가 3건 있어 명시한다.

| 값 | 어긋난 출처 | live BalanceConfig (SoT) | 본 기획서 채택 | 영향 |
|---|---|---|---|---|
| 영웅 HP | task 지시문/컨셉 §11.3 = 1000 | **4000** | **4000** | §6 페이싱·데스스파이럴 서사에만 영향 (킬카운트 표는 무관 — 아래) |
| Plague HP | `continuous-spawn-round.md` §6 = 80 (stale) | **50** | **50** | §3 킬카운트 표 (Plague 처치 타수). 컨셉 §11.3 표(=50)는 BalanceConfig 과 일치 |
| 몬스터 DPS | 컨셉 §11.3 표 (Wisp 10 등) | 다름 (Wisp 5 등) | live | 본 기획 deliverable 아님 — §6 영웅 압박 산식에서만 참조 |

> **Plague HP=50 채택 근거**: BalanceConfig.asset(Plague HP=50)과 컨셉 §11.3 표(=50)가 일치한다. 80 은 `continuous-spawn-round.md` §6 의 몬스터 스탯 스냅샷 값이나, 그 문서는 헤더(본 기획서 line 7)에서 **스포너 주기·글로벌 캡** 에 대해서만 SoT 로 선언됐고 몬스터 HP 는 그 권한 밖이다 — 따라서 그 80 은 stale 한 값으로 채택하지 않는다. (1차 작성 시 이 문서의 80 을 잘못 채택했던 오류를 정정함 — §11 참조.)

**핵심 단순화**: 스킬 데미지 수치(§2 수치표)와 "몇 타에 어느 종이 죽는가"(§3)는 **몬스터 HP 에만** 의존한다 — 영웅 HP 1000/4000 논쟁과 무관하다. 영웅 HP 4000 은 §6 페이싱/데스스파이럴 서사에만 들어간다. HP 게이트 85/65/45% 는 분수이므로 영웅 HP 크기와 독립이다.

**plan 정합**: EVisual 키(`HeroDashFx`/`HeroOrbitBladeFx`/`HeroNovaFx`), EData 키(`HeroSkillLoadout`), FX 색(노바 #FBBF24 / 돌진 #93C5FD / 궤도 #E5E7EB), 파일명을 **그대로 따른다**. ⚠️ **단 (a) 페이즈 게이트·페이즈↔스킬 매핑, (b) 형태 변경 v0.8(§12) 로 일부 SO 필드명·인터페이스·기하·FX 메시는 plan 과 의도적으로 어긋난다.** (a) 페이즈: plan/spec 은 0.9/0.6/0.3 + P2 궤도 / P3 노바였으나 **shipped `HeroSkillLoadout.asset` 은 0.85/0.65/0.45 + P2 노바 / P3 궤도** 다 — 본 기획서는 shipped 에셋을 SoT 로 채택해 정정한다(상세·근거 §13 변경 이력). (b) — `_halfWidth`→`_coneHalfAngle`, `_bandHalfThickness` 제거 + `_bladeSphereRadius` 신설, `DamageMonstersInCone`/`InSpheres` 추가, FX Cylinder/Cube→Sphere/부채꼴 mesh. 이 어긋남은 §12 plan sync 노트 7항목에 전부 명시했고 구현 시 plan 동시 갱신 대상이다. **형태 변경 외 식별자는 어긋남 0건** (§8 참조).

**기준 데이터 스냅샷** (live):

| 종 | HP | Power(공격력/타) | Cooldown | DPS(=Power/CD) | 스포너 주기 |
|---|---|---|---|---|---|
| Wisp | 200 | 5 | 1.0s | 5.0 | 9.0s |
| Wraith | 500 | 10 | 1.0s | 10.0 | 20.0s |
| Reaper | 100 | 6 | 0.5s | **12.0** | 12.0s |
| Hex | 60 | 9 (원거리 range 5) | 1.0s | 9.0 | 15.0s |
| Plague | 50 | 2 (+영웅 둔화) | 1.0s | 2.0 | 10.0s |
| Phantom | 30 | 2 | 0.8s | 2.5 | 6.0s |

- 영웅: HP 4000, Power 50/타, Cooldown 1.0s(공속 1초, 단일 근접), MoveSpeed 3, Range 1.5
- 글로벌 필드 캡: **18마리** (캡 도달 시 스포너 백오프)
- 영웅 기본 근접 공격(`MeleeAttacker`, 50/타 단일)은 스킬 페이즈 중에도 **유지**된다 — spec §2 "페이즈는 스킬만 추가". 스킬은 기본 공격에 **가산**.

---

## §1. 페이즈 ↔ 스킬 매핑 (SoT = shipped `HeroSkillLoadout.asset` — spec §2 와 게이트값·순서가 어긋나 정정, §13)

| 페이즈 | HP 게이트 | 새로 추가 스킬 | 누적 활성 스킬 | 영웅 잔여 HP(4000 기준) |
|---|---|---|---|---|
| P0 | 100%~85% | 없음 (기본 근접만) | 근접 | 4000~3400 |
| P1 | **85%** | Dash Strike (돌진/관통) | 근접 + 돌진 | 3400 |
| P2 | **65%** | Aoe Nova (AOE 노바) | 근접 + 돌진 + 노바 | 2600 |
| P3 | **45%** | Orbiting Blade (회전 블레이드) | 근접 + 돌진 + 노바 + 궤도 | 1800 |

> ⚠️ **페이즈↔스킬 매핑은 shipped `HeroSkillLoadout.asset` 순서를 SoT 로 한다.** 에셋 `_phases` 는 `0.85 → DashStrike`, `0.65 → AoeNova`, `0.45 → OrbitingBlade` 순이다(asset+meta GUID 1:1 확인). 따라서 **P2 가 노바, P3 가 궤도 블레이드**다 — spec/plan 의 "P2 궤도 / P3 노바" 와 순서가 뒤바뀌어 있다. §2.x 의 스킬별 수치·형태는 스킬 고유 속성이라 변하지 않으나, "어느 페이즈에서 켜지는가"는 본 표가 단일 진실이다. 잔여 HP: 85%×4000=3400, 65%×4000=2600, 45%×4000=1800.

**HP 게이트는 일부러 패시브 픽과 어긋나게 배치했다 (충돌 회피 오프셋).** 카드 픽 트리거(HP 10% 패시브, BalanceConfig `_passiveThresholds` 0.9·0.8·0.7·…·0.1)는 0.05 단위가 아니라 0.1 단위에서 발생한다. 스킬 게이트를 패시브 임계와 **정확히 겹치게** 두면, 스킬 해금 컷인(시간정지 + 카메라 쿵 + 화면 중앙 배너 — `skill-unlock-cutscene.md`)과 패시브 카드픽 일시정지가 **같은 HP 순간에 동시 발동**해 두 정지 연출이 충돌한다. 게이트를 패시브 임계(0.9/0.8/0.7/0.6/0.5)에서 **각 0.05 아래로 오프셋**(0.85/0.65/0.45)하면 스킬 해금이 패시브 픽 사이의 빈 HP 구간(예 0.85 는 0.9 픽과 0.8 픽 사이)에서 발동해 두 연출이 같은 순간에 겹치지 않는다. **즉 세 페이즈는 패시브 픽과 동기화되는 것이 아니라, 의도적으로 비켜 있다.** (이 충돌 회피 의도는 어느 문서에도 명문화돼 있지 않은 **에셋 값 + git 커밋 `688a649` 로 역추론한 의도** 다 — §13 참조. 컷인 기획서 `skill-unlock-cutscene.md` 자체는 0.9/0.7/0.5 가정 하에 "겹침 허용 + 배너 위치로 회피" 를 택하고 있어, shipped 의 0.05 오프셋과는 별개의 회피 레이어다.)

---

## §2. 스킬 수치표 (SO 인스펙터 필드 전부)

> 모든 필드는 plan §SO 필드명과 1:1 대응. 단위: 데미지 = 정수 HP, 시간 = 초(s), 거리/반경 = Unity 유닛(스포너 ring 반경 14.0, 몬스터 스케일 0.3~1.2 기준).

### 2.1 Dash Strike (P1, HP 85%) — `DashStrikeSkillData`

> **형태: 부채꼴(cone / angular sector).** 영웅 중심에서 돌진 방향(무게중심 방향) 기준 ± `_coneHalfAngle` 도 + 사거리 `_dashLength` 안의 몬스터를 타격. 히트 판정·비주얼 모두 부채꼴. 기존 직선 띠(`_halfWidth`)는 폐기 — `_coneHalfAngle` 로 대체.

| 필드 | 값 | 근거 |
|---|---|---|
| `_damage` | **80** | Phantom(30)·Plague(50)·Hex(60) 1타 처치, Reaper(100) 2타, Wisp(200) 3타, Wraith(500) 7타. 스웜(Phantom)을 전방에서 즉살하되 탱커는 못 뚫음 — Swarm 부분 카운터, Tank 면역(§4). 형태 변경과 무관(데미지=HP 의존). |
| `_cooldown` | **3.0** | 영웅 기본 공속 1.0s 의 3배. 돌진은 "가끔 터지는 부채꼴 휘두르기" 로 위치한다. Phantom 스포너 주기 6s 의 0.5배라 무리를 완전히 비우진 못함. 형태 변경과 무관. |
| `_dashLength` | **7.0** | ring 반경 14.0 의 0.5. 영웅 중심에서 무리가 수렴하는 중간 거리까지 관통. 화면 절반을 가르는 체감. (필드명 유지 — `_coneLength` 로 개명하지 않음. plan churn 최소화.) |
| `_coneHalfAngle` | **35** (도) | **신규.** 영웅 앞 방향 ± 35° = 전체 부채꼴 70°. 산정 근거는 아래 "부채꼴 vs 직선 띠 커버리지" 검산. 30~45° 후보 중 35° 채택 — 근접 무리 sweep 폭 확보(넓게)하되 60°+ 광역으로 가면 노바(P2, §1) 역할과 겹쳐 돌진의 "방향성 선긋기" 정체성이 무너짐(§4 "사방 포위 전체는 못 쓸음, 방향성 유지" 와 정합). SerializeField configurable. |
| `_knockbackStrength` | **2.0** | 부채꼴 안 몬스터를 바깥으로 2유닛 밀어 직후 재진입 지연. 돌진 본질은 데미지라 넉백은 보조. 형태 변경과 무관. |
| `_centroidRadius` | **8.0** | 무게중심(돌진 방향) 수집 반경. ring 반경 14.0 보다 작아 "가까이 붙은 무리" 쪽으로만 돌진 — 막 스폰된 먼 몬스터에 헛돌진 방지. 형태 변경과 무관. |

**부채꼴 vs 직선 띠 커버리지 검산** — 형태 변경의 핵심.

| 거리(영웅 중심부터) | 기존 직선 띠 폭 (halfWidth 1.5 → 전폭 3.0, 거리 무관 일정) | 신규 부채꼴 전폭 (35° → 2·d·tan35° ≈ 1.40·d) | 비교 |
|---|---|---|---|
| 근접 d=1.5 | 3.0 | **2.10** | 부채꼴이 더 좁음 |
| 중간 d=3.0 | 3.0 | **4.20** | 부채꼴이 더 넓음 (교차점 d≈2.14) |
| 원거리 d=5.0 | 3.0 | **7.00** | 부채꼴이 훨씬 넓음 |
| 끝 d=7.0 | 3.0 | **9.80** | 부채꼴이 압도적으로 넓음 |

- **면적**: 직선 띠 = 2·halfWidth·length = 3.0×7.0 = **21.0 유닛²**. 부채꼴 = ½·(2·θ_rad)·L² = θ_rad·L² = 0.6109·7.0² = **29.9 유닛²** (35°=0.6109 rad). 부채꼴이 **약 1.42배 넓은 면적** — 영웅 앞에서 수렴하는 무리를 더 넓게 쓸음.
- **형태 의도**: 부채꼴은 **근접은 좁고 먼 곳은 넓다** — 직선 띠와 반대 프로파일. 영웅 코앞(d<2.14)에 붙은 1~2마리는 직선 띠보다 덜 잡지만, 영웅을 향해 수렴해오는 중·원거리 무리(부채꼴이 벌어지는 구간)를 더 많이 쓸어담는다. "다가오는 무리를 부채꼴로 베어내는" 체감 — 직선 관통보다 군집 sweep 에 강하다.
- ⚠️ **모델 일관성 주의 (radial 기준)**: 위 표의 "전폭 2·d·tan35°" 는 영웅 정면 수직선 기준 직선 wedge 폭이다. 그러나 면적 산식(½·α·L²)과 실제 히트 판정(`InCone` radial, §8)은 **radial sector(방사형 부채꼴)** 기준이다. radial 기준 호 폭은 2·d·sin35° 이므로 직선 띠 전폭 3.0 과의 교차점은 2·sin35°·d = 3.0 → d = 3.0/(2·0.5736) ≈ **2.5** 다(tan 모델의 d≈2.14 와 약간 다름). **35° 채택 결론은 두 모델 모두에서 불변** — 교차점이 melee standoff(~1.5) 바깥이고, 면적 비교(radial 29.9 유닛² vs 직선 21.0)도 radial 기준 산식이다. 표는 직선 wedge 근사라 교차점만 radial 기준 ~2.5 로 읽는다.
- **35° 근거**: 교차점 d≈2.5(radial) 가 melee 몬스터 standoff(영웅 Range 1.5 ≈ 몬스터 정지 거리)보다 바깥 → 영웅 코앞 1마리는 직선 띠 수준, 그 너머 다가오는 무리는 부채꼴 우위. 45° 면 면적 38.5 유닛²로 너무 광역(노바 디스크 38.5 와 동급) → 돌진≈노바 역할 중복. 30° 면 면적 25.6 으로 직선 띠 대비 이득이 작음. 35° 가 "직선 대비 명확히 넓되 노바와 구분되는" 중간값.

**검산** — Dash 1회 기대 처치(부채꼴 29.9 유닛² 안 평균 2~4마리, 글로벌 캡 18 중 영웅 전방 밀집 가정): Phantom·Plague·Hex 면 2~4킬, Reaper·Wisp 면 0~1킬(2~3타 필요). 3s 쿨 → 분당 약 20회 발동 → 분당 Phantom 환산 최대 ~60킬(직선 띠 50 대비 면적비 1.42 증가분 반영). Phantom 스포너 6s 주기 = 분당 10마리 공급 → **돌진 단독으론 스웜을 못 따라잡음**(의도 유지, §4). 부채꼴 광역화에도 쿨 3s·방향성·탱커 면역이 카운터 폭주를 막는다.

### 2.2 Orbiting Blade (P3, HP 45%) — `OrbitingBladeSkillData`

> **형태: 3D 구(sphere) N개가 영웅 주위를 공전.** 각 구는 반경 `_bladeSphereRadius` 의 실제 구이며, **각 구 중심에서 `_bladeSphereRadius` 안에 들어온 몬스터만** 타격(per-sphere overlap). 기존 "링밴드 근사(반경 ± 밴드 안 전부 타격)" 는 폐기 — `_bandHalfThickness` 필드 제거, `_bladeSphereRadius` 신설. 비주얼도 구(Sphere 프리미티브).

| 필드 | 값 | 근거 |
|---|---|---|
| `_damage` | **15** (유지) | 인터벌당 per-contact 데미지. 한 구에 닿은 몬스터에 15. **per-contact** Phantom(30)=2틱, Plague(50)=4틱에 처치(킬 "틱 수"는 §3 표 유지 — 단 벽시계 시간은 overlap 이 연속이 아니라 가변, §3 주석 참조). 1틱 즉살 없음 — 노바와 역할 분리(노바=폭발, 궤도=지속). |
| `_hitInterval` | **0.3** (0.4→0.3) | 초당 약 3.3틱. **하향 조정 근거**: per-sphere 는 구가 한 몬스터 위를 지나는 통과 시간이 짧다(shell-정렬 몬스터 기준 ≈ 0.21s, 아래 검산). 틱 0.4s 면 통과(0.21s)보다 길어 한 통과에 0틱 또는 1틱으로 갈려 per-monster DPS 가 위상(phase)에 따라 깜빡인다. 0.3s 로 줄여 틱 간격을 통과 시간에 가깝게 당겨 통과당 틱 안정성·전체 duty-cycle 을 평탄화한다(틱 밀도 ↑). |
| `_orbitRadius` | **1.4** (2.0→1.4) | **하향 조정 근거**: melee 몬스터는 자기 Range(1.0)·영웅 Range(1.5) 경계에서 정지 → 영웅 중심부터 약 1.0~1.5 유닛 거리에 밀집한다. 기존 궤도 반경 2.0 은 이 밀집 shell 바깥이라 구가 melee 무리를 *스치지 못할* 위험이 있었다. 1.4 로 낮춰 구 중심 궤도를 melee standoff shell 위에 정렬 → 구가 밀착 무리를 실제로 통과한다. |
| `_bladeSphereRadius` | **0.9** | **신규.** 각 공전 구의 반경(= 히트 반경 = 비주얼 반경). 0.9 면 구 중심 궤도 1.4 기준 타격 도달 반경 [0.5, 2.3](= 1.4 ± 0.9) — 영웅 밀착 shell(1.0~1.5) 을 확실히 덮되, 원거리 Hex(range 5)·먼 무리엔 무효("근접 방어막" 위치 유지). 1.0 초과로 키우면 구가 영웅을 삼켜 시각상 "공전"이 안 보임 → 0.9 상한. |
| `_bladeCount` | **3** (2→3) | **gameplay-load-bearing 로 격상.** per-sphere 에선 구 개수가 곧 순간 각도 커버리지다(각 구 통과 arc 37.5° → 3개 ≈ 112.5°/360° ≈ 31% shell 하한, 아래 검산). 2개(≈25%)는 sweep 공백이 너무 커 "근접 방어막" 체감이 약함. 3개로 대칭(120° 간격) 배치해 커버리지·회전 가독성 확보. (시각·데미지 모두 영향.) |
| `_rotationSpeedDeg` | **180** (유지) | 초당 반바퀴 = ω. 한 구가 한 점 위를 지나는 통과 arc(Δθ = 2·arcsin(r/2R))를 ω 로 나눠 통과 시간 산정(아래 검산). 너무 빠르면 깜빡임, 180°/s 가 프리미티브 구로 회전 인지 적정. |

**⚠️ 밸런스 핵심 — per-sphere 로 순간 커버리지 급감 (형태 변경의 최대 영향)**:

기존 ring-band(같은 궤도 반경 기준 가상의 ± 밴드)는 **둘레 전체(360°) 동시 판정** → 영웅을 둘러싼 모든 밀착 몬스터가 매 틱 피격(이론 37.5 DPS 연속). per-sphere 는 **각 구가 덮는 arc 만** 판정한다:

- 한 구가 **궤도 shell 위(반경 R) 몬스터**를 덮는 통과 arc Δθ = 2·arcsin(`_bladeSphereRadius` / (2·`_orbitRadius`)) = 2·arcsin(0.9/2.8) = 2·18.75° = **37.5°**. 3개 × 37.5° = 112.5°/360° ≈ **약 31% 순간 커버리지** (shell-정렬 기준, **보수 하한**).
- **shell 안쪽 몬스터는 커버리지가 더 넓다**: melee 무리는 영웅 중심 1.0~1.5(궤도 1.4 의 안쪽~근처)에 밀집한다. 구가 그 점에 더 가까이 지나가므로 도달 arc 가 shell 값(37.5°)보다 넓어진다 — 실제 유효 커버리지는 31%(하한)~상당히 높은 값 사이. 정확한 유효 커버리지는 밀집 분포 종속이라 **분석 단정 불가 → qa-simulator 검증**(결정 메트릭 아래).
- ⚠️ **이전 안(r=0.6, R=2.0, 2개) 대비**: 그 조합은 통과 arc 2·arcsin(0.6/4.0)≈17.2°, 2개×17.2°≈9.5% 로 ring-band(100%) 의 1/10 로 붕괴했다. 본 안(r=0.9, R=1.4, 3개)은 구를 키우고 궤도를 줄이고 개수를 늘려 shell 하한을 ~31% 로 끌어올렸다 — 그래도 100% 인 ring-band 보다 낮은 것이 §4·§6 의도와 정합.

**중복 데미지 정책 (필수 명시)**: 한 몬스터가 한 인터벌에 **2개 이상 구 안에 동시에 들어가도 데미지는 1회(`_damage` 만큼)만** 적용한다(union dedup). per-monster 당 인터벌당 최대 1틱으로 상한.

> **현 수치에선 비활성 — forward-compat 방어**: 현 수치(궤도 R=1.4, 구반경 r=0.9, 120° 간격 3구)에선 인접 두 구의 중심거리 = 2·R·sin60° = 2·1.4·0.8660 = **2.42 > 2r = 1.8** 이라 **구끼리 서로 겹치지 않는다**(틈 ≈ 0.62 유닛). 따라서 "한 몬스터가 2구에 동시 진입해 ×3 폭딜 스파이크" 시나리오는 **현 수치에선 기하적으로 발생 불가** — dedup 정책이 실제로 발동할 케이스가 없다. 또한 궤도 1.4·구반경 0.9 면 각 구는 hero 로부터 반경 [0.5, 2.3] shell 만 덮어 **중심부(반경 < 0.5)는 어느 구도 커버하지 않는다** — 따라서 "중심부에서 구 3개 겹쳐 ×3" 라는 이전 근거도 성립하지 않는다. dedup 은 향후 R/r/`_bladeCount` 를 구가 서로 겹치도록 조정(예: R 축소·r 확대·구 개수 증가)할 경우의 ×N 스파이크를 막는 **forward-compat 방어 규약**으로 유지한다(그때 활성화되면 데스스파이럴 가드 §6 와 연결). 중심부 미커버는 melee 교전거리(영웅 Range 1.5·몬스터 standoff ~1.0~1.5)가 shell [0.5, 2.3] 안에 들어와 **실용상 무해**하다 — 밀착 무리는 어차피 shell 위에 밀집하므로 미커버 중심 구멍에 몬스터가 멈춰 있지 않는다.

**검산 — per-sphere 통과 시간 / duty-cycle** (단위·산식 명시):
- 통과 arc Δθ = 2·arcsin(r/(2R)) = 2·arcsin(0.9/2.8) = **37.5°** (shell-정렬 몬스터). 통과 시간 = Δθ / ω = 37.5° / 180°·s⁻¹ ≈ **0.21s**. (shell 안쪽 밀집 몬스터는 도달 arc 가 넓어 통과 시간 ↑ — 이 0.21s 는 보수 하한.)
- 틱 0.3s vs 통과 0.21s(하한): shell-정렬이면 통과당 0~1틱(위상 종속), shell 안쪽(통과 시간 길어짐)이면 통과당 1틱 이상 — 0.3s 틱이 0.4s 보다 위상 깜빡임을 줄인다. (이전 셀의 "0.27s" 표기는 구 형태 이전 stale 값 — 본 검산 0.21s 로 정정.)
- 한 구가 같은 점을 다시 통과하는 주기(3구 120° 간격) = 120° / ω = 120° / 180°·s⁻¹ ≈ **0.667s**. per-monster 피격은 약 0.667s 마다 한 번의 통과(통과당 0~수틱) 패턴 → **연속이 아니다.**
- 따라서 "연속 37.5 DPS 유지" 는 per-sphere 에서 **성립하지 않으며 주장하지 않는다**(§5.3·§12 반영). 지속 클리어율은 위상·밀집 분포 종속이라 분석으로 단정하지 않고 **qa-simulator 게이트로 검증**한다(결정 메트릭 아래).

**DPS 의도 보정 — 결정 메트릭 (분석 단정 불가 항목)**:
- 목표: Phantom-인접(영웅 밀착) 처치 시간 ≤ 약 1.5s 유지(ring-band 의 Phantom 0.8s 보다 다소 느려진 것은 §4·§6 의도와 정합 — 스웜 하드카운터 완화·데스스파이럴 가드에 **부합하는 feature**). 
- 결정 메트릭(qa-simulator): 밀착 Phantom 평균 처치 시간 측정. **> 1.5s 면** 다음 순서로 보정 — ① `_bladeCount` 3 → 4 (커버리지 ↑), ② `_hitInterval` 0.3 → 0.2 (틱 밀도 ↑). `_damage` 상향은 per-contact 처치 타수(§3)를 흔들어 후순위. **< 0.6s(과도하게 빠름)면** `_bladeCount` 3 → 2 또는 `_bladeSphereRadius` 0.9 → 0.7.
- per-sphere 커버리지 손실은 **§4(스웜 비-하드카운터)·§6(데스스파이럴 가드) 를 직접 강화하는 의도된 효과** — 결함이 아니라 설계 정합.

### 2.3 Aoe Nova (P2, HP 65%) — `AoeNovaSkillData`

> **형태: 히트는 그대로 원형 디스크(반경 `_radius`), 비주얼만 원기둥(Cylinder) → 구(Sphere) 3D.** 수치 변화 없음 — SO 필드·데미지·쿨·반경·넉백 전부 유지. 구 스케일이 히트 반경과 정합하도록 명시: **구 지름 = 2 × `_radius` = 2 × 3.5 = 7.0** (균일 스케일 7.0). 구는 영웅 중심에 반경 3.5 의 반투명 돔으로 떠올라 디스크 범위를 3D 로 가시화한다(영웅을 감싸는 형태 — 의도된 룩).

| 필드 | 값 | 근거 |
|---|---|---|
| `_damage` | **100** | Phantom(30)·Plague(50)·Hex(60) 1타 즉살, Reaper(100) 1타 즉살, Wisp(200) 2타, Wraith(500) 5타. 피니시 라인 폭발 — 근접 무리를 한 번에 비움. 탱커(Wraith)는 못 즉살(§4 Tank 잔존). |
| `_cooldown` | **7.0** | **§5 가드레일 핵심값.** Phantom 스포너 base 주기 6.0s 보다 **길게**(7.0s). 노바 직후 무리가 다시 찰 틈 보장. 산정 근거는 §5.1. |
| `_radius` | **3.5** | 영웅 중심 디스크 반경. 영웅 근접 포위(스케일 1.0~1.2 몬스터가 붙는 거리 2~3유닛)를 덮되, ring 반경 14.0 의 0.25 라 화면 전체는 안 비움. 궤도 반경 1.4 보다 커서 궤도 밖 무리까지 포함. |
| `_knockbackStrength` | **3.0** | 디스크 내 전체를 3유닛 바깥으로 밀어냄. 노바=폭발 연출의 핵심. 밀려난 무리가 재진입하는 동안 노바 쿨(7s) 회복 → 데스스파이럴 완화(§6). |

**검산** — 노바 1회: 디스크 반경 3.5(면적 ≈ 38.5 유닛²) 내 근접 무리 전부 즉살(Phantom~Reaper) + 넉백. 글로벌 캡 18 중 근접 도달분 최대 ~8마리 동시 클리어. 7s 쿨 → 분당 약 8.5회. **분당 최대 클리어 ≈ 8.5 × 8 ≈ 68마리** (이론 상한, 실제는 근접 밀집도에 종속). 전 스포너 합산 공급량(아래 §5.1 = 분당 약 31마리)보다 크므로 노바는 강력 — 단 넉백 + 쿨 7s + 탱커 잔존이 폭주를 막는다(§4·§6).

---

## §3. 킬카운트 표 (스킬 데미지 → 종별 처치 타수)

> live 몬스터 HP 기준. "타수" = 해당 스킬이 그 종을 처치하는 데 필요한 발동 횟수(궤도는 틱 수).

| 종 (HP) | Dash 80 (부채꼴) | Orbit 15/틱 (구 통과 중) | Nova 100 | 영웅 근접 50/타 (참고) |
|---|---|---|---|---|
| Phantom (30) | **1타** | 2틱 | **1타** | 1타 |
| Plague (50) | **1타** | 4틱 | **1타** | 1타 |
| Hex (60) | **1타** | 4틱 | **1타** | 2타 |
| Reaper (100) | 2타 | 7틱 | **1타** | 2타 |
| Wisp (200) | 3타 | 14틱 | 2타 | 4타 |
| Wraith (500) | 7타 | 34틱 | 5타 | 10타 |

> **Orbit 틱 수 vs 벽시계 시간 (per-sphere 형태 변경 반영)**: Orbit 열의 "틱 수"는 **몬스터가 구에 닿아 있는 동안의 누적 피격 횟수** 다 — per-contact 데미지이므로 종별 처치 타수(Phantom 2틱 등)는 형태 변경과 무관하게 유지된다. 다만 ring-band(연속 360° 판정)와 달리 per-sphere 는 구가 몬스터 위를 *통과할 때만* 틱이 들어가 **벽시계 처치 시간은 연속이 아니다**(구 회전 위상에 종속). 그래서 이전 표의 "(0.8s)/(1.6s)" 같은 초 환산은 더 이상 단정할 수 없어 제거했다 — 실제 처치 시간은 §2.2 결정 메트릭(qa-simulator)으로 검증.

**설계 의도 요약**:
- **Swarm(Phantom 30)**: 세 스킬 모두 1~2틱/타 처치 → 스웜이 영웅 스킬의 주 표적. 단 스킬 각도(부채꼴·구 arc)/반경/쿨 제한으로 무리 *전체*는 못 비움 → 부분 카운터(§4).
- **Tank(Wisp 200·Wraith 500)**: 어느 스킬도 1발 즉살 불가. Wraith 는 노바 5타·돌진 7타. **탱커가 영웅 스킬의 천적** — Tank 빌드는 영웅 스킬에 강하다(빌드 다양성 보존).
- **Dps(Reaper 100·Hex 60)**: 노바 1타 즉살. Dps 빌드는 영웅 스킬에 취약하나, 원거리 Hex(range 5)는 궤도(반경 1.4) 밖에서 안전 → 일부 생존.

---

## §4. Swarm 축 하드카운터 완화 (deliverable 3)

**문제**: 영웅 스킬은 본질적으로 스웜 클리어형 → 4축 중 Swarm(Phantom HP 30, 떼) 을 직접 카운터. 28장 카드 세트가 §8 "2~4분 처치" 에 맞춰 튜닝돼 있어 영웅 클리어력 추가가 이를 깰 위험.

**입장**: 영웅 스킬은 Swarm 을 **부분 카운터(soft counter)** 하되 **하드카운터(무력화)하지 않는다.** "단일 스킬은 maxed Swarm 빌드를 못 따라잡는다 — 무리가 영웅을 압도하는 것이 Swarm 빌드의 의도된 승리 경로다."

**수치 근거**:

1. **공급 vs 클리어 — base Swarm**: Phantom 스포너 base 주기 6.0s = 분당 10마리. 돌진 단독(분당 ~50킬 이론, 실제 근접 밀집 종속)은 base 1스포너는 따라잡지만, Swarm 빌드는 스포너를 강화한다(아래 2).

2. **maxed Swarm 빌드의 Phantom 공급** (card-renewal §4 Swarm Tier 누적):
   - `SpawnerHaste`(주기 ×0.8, **1픽 가정** — 3픽 캡 ×0.512 는 보수적으로 미반영, card-renewal §3.4 #4) + Swarm Tier2(모든 스포너 주기 ×0.85) → Phantom 주기 6.0 × 0.8 × 0.85 = **4.08s**
   - `SpawnPhantoms`(동시 출력 +1) + Swarm Tier3(전 스포너 동시 출력 +1) → 사이클당 최대 3마리
   - 유효 공급 ≈ 3마리 / 4.08s = 분당 약 44마리 (글로벌 캡 18 로 상한)
   - + Phantom 이동속도 **×1.95** (= `PhantomMoveSpeedBoost` 단독 ×1.5 [card-renewal §3.4 #1] × Swarm Tier1 ×1.3 [card-renewal §4.2]) → ring 14 횡단 시간 단축(speed 2.4 → 4.68, 14/4.68 ≈ **3.0s**) → 영웅 도달 회전율 ↑

3. **결론**: maxed Swarm 의 유효 공급(분당 44, 캡 18 유지)은 노바(분당 ~68 이론 상한)에 근접하나, **노바 넉백(3유닛) + 쿨 7s + 돌진/궤도의 방향·반경 제약**으로 영웅이 무리를 *영구히* 비울 수 없다. 글로벌 캡 18 이 항상 차 있는 상태가 유지 → 영웅은 끊임없이 포위된다. **Swarm 빌드는 영웅 스킬이 켜진 상태에서도 살아남는다** (검증은 qa-simulator, §7).

**조정 가드** (스웜 무력화 신호 시): 우선 `Aoe Nova _cooldown` 을 7.0 → 8.0 으로(공급 틈 확대), 그래도 부족하면 `_radius` 3.5 → 3.0. 돌진/궤도는 방향·근접 제약이 이미 있어 후순위.

---

## §5. §5 밸런스 가드레일 — 수치 확정 (deliverable 2)

### 5.1 노바 쿨다운 > 스웜 리필 시간 → **`_cooldown` = 7.0s 확정**

- **스웜 리필 시간 정의**: 노바가 근접 무리를 비운 직후, 무리가 다시 영웅을 포위하기까지 걸리는 시간.
- **base 리필**: 가장 빠른 공급원 Phantom 스포너 주기 **6.0s**. + ring 반경 14 에서 영웅까지 횡단(speed 2.4 → 약 5.8s)이지만, 노바 넉백은 3유닛만 밀므로 재진입은 근거리(약 1~2s). 따라서 base 리필 체감 ≈ 스포너 주기 6.0s 가 지배항.
- **확정**: 노바 쿨 **7.0s > base 리필 6.0s**. 노바가 base 공급보다 느리게 발동 → 매 노바 사이 무리가 반드시 다시 찬다.
- **maxed Swarm 은 의도적으로 미보장**: maxed Swarm 리필(주기 4.08s)은 노바 쿨 7.0s 보다 빠르다 — 이는 §4 의 "단일 스킬은 maxed Swarm 을 못 따라잡는다" 의도와 정합. 가드레일은 **base 리필** 을 보장 대상으로 한다.

> 산정 검산: 노바 쿨 7.0 = Phantom base 주기 6.0 × 1.167. base 공급 기준 항상 1마리 이상 신규 공급 후 노바 발동.

### 5.2 페이즈는 스킬만 추가, 기본 스탯 동시 램프 금지 → **명시 확정**

- 페이즈 전환은 **스킬 1종 추가만** 한다. 영웅 HP(4000 고정)·Power(50 고정)·공속(1.0s 고정)·MoveSpeed(3 고정)는 페이즈와 무관하게 **불변**이다.
- 구현 보증: `HeroSkillRunner`(plan A10)는 `HeroSkillLoadout` 의 스킬만 활성화한다 — 스탯 필드를 건드리지 않는다. BalanceConfig `_hero` 블록은 런타임 페이즈 로직과 분리.
- 이유: HP 에스컬레이션 + 스탯 램프의 이중 강화는 종반 폭주(데스스파이럴)를 일으킨다. 스킬만 추가하면 강화 곡선이 선형(3계단)으로 예측 가능.

### 5.3 초당 클리어 상한(clear-per-second cap) → **별도 cap 불필요로 판단**

- **판단**: 별도 clear-per-second cap 을 **두지 않는다.** 근거:
  - **글로벌 필드 캡 18** 이 순간 AOE 클리어량을 이미 상한한다 — 노바/돌진은 필드에 18마리 이상 존재할 수 없으므로 1회 클리어가 18 을 못 넘는다.
  - 돌진(쿨 3.0s)·노바(쿨 7.0s)는 쿨다운 게이트 → 본질적으로 발동 빈도 제한됨.
  - 유일한 **지속(continuous)** 데미지원은 **궤도 블레이드**다. **per-sphere 형태 변경으로 궤도의 지속 클리어력은 ring-band 대비 크게 낮아졌다** — 각 구가 덮는 arc 만 판정하고(순간 ~31% shell 하한, §2.2) 한 점 통과 시간(≈0.21s)·회전 위상에 종속이라 **"연속 37.5 DPS / 순간 150 DPS" 는 더 이상 성립하지 않는다**(이전 안의 ring-band 가정에서만 유효했던 수치 — §12 정정). 구 궤도 반경 1.4·sphereRadius 0.9 의 근접 한정 + arc 공백 + union dedup(1×/틱)이 자체 cap 역할을 한다.
- **조건부 cap 트리거**: qa-simulator 에서 궤도 단독으로 분당 60마리 초과 클리어가 관측되면, 궤도에 한해 `_bladeCount` 3 → 2 또는 `_bladeSphereRadius` 0.9 → 0.7 (커버리지 ↓) 로 조정. `_damage` 하향은 per-contact 처치 타수(§3)를 흔들어 후순위. 별도 cap 시스템 추가는 YAGNI.

---

## §6. 종반 데스스파이럴 vs 클라이맥스 (deliverable 4)

**문제**: HP 에스컬레이션은 "플레이어가 영웅 HP 를 깎을수록 영웅이 강해지는" 구조. 가장 강한 광역 폭발인 노바가 이미 P2(HP 65%)에서 켜지고, 최종 P3(HP 45%, 궤도 블레이드 추가)에서 영웅이 3스킬 전부를 갖춰 가장 강해진 순간 = 처치 직전 → "역전 불가 좌절"(feel-bad) 위험.

**디자인 입장**: **클라이맥스("코너에 몰린 보스의 발악")로 설계하며, 데스스파이럴이 되지 않도록 수치로 가드한다.** P3 는 *긴장의 정점*이지 *처치 불가 벽*이 아니다.

**근거 — 잔여 HP 1800 은 처치 가능**:
- P3 진입 = HP 45% = 영웅 잔여 **1800 HP**(4000 기준).
- 평균 빌드 영웅 압박(필드 캡 18 중 근접 교전분 ~8마리, 종 혼합 평균 DPS-on-hero ≈ 6~7/마리 — §0 DPS=Power/CD 가중) ≈ **50~56 DPS** → 1800 HP 를 약 32~36s 에 처치. P3 구간이 액티브 카드 1~2회(30s 주기) 안에 결판나는 클라이맥스 길이. (노바가 P2 에서 이미 켜져 있으므로 영웅 압박 완화는 P3 진입 전부터 작동 — P3 에서 추가되는 건 궤도의 근접 지속 방어막뿐.)
- 노바(P2 부터)가 근접 무리를 비워 영웅 피격을 줄이지만, 넉백된 무리가 재진입(1~2s) + base 리필(6s) 로 영웅 압박이 끊기지 않음 → 영웅이 무한 생존하지 못함.

**데스스파이럴 가드레일**:
1. **노바는 즉살 아닌 넉백 동반** — Tank(Wisp 200·Wraith 500)는 노바 1발 생존(§3) → Tank 빌드는 P2·P3 에서도 영웅을 계속 때림(클리어 무효화 방지).
2. **궤도는 근접 한정(per-sphere, shell [0.5, 2.3])** — P3 에서 추가되는 궤도 블레이드는 무리 전체가 아닌 밀착분만 친다(§2.2) → 원거리/외곽 무리는 영웅 압박 지속.
3. **스탯 램프 금지(§5.2)** — P3 에서 강해지는 건 클리어력뿐, 영웅 맷집(HP)·딜(Power)은 그대로 → 플레이어 누적 빌드가 여전히 우세.
4. **에스컬레이션 상한 = 3계단** — P3 이후 추가 강화 없음. 4번째 페이즈/스케일링 없음(YAGNI).

**검증 분기** (qa-simulator §7): P3 진입 후 영웅 처치율이 평균 빌드에서 60% 미만으로 떨어지면 데스스파이럴 신호 → 노바 `_radius` 3.5 → 3.0 또는 `_cooldown` 7.0 → 8.0 우선 조정.

---

## §7. 검증 (qa-simulator 권장)

본 파이프라인(start-develop)은 qa-simulator 를 자동 단계로 포함하지 않으나, 영웅 스킬은 게임플레이 영향이 큰 케이스다. **구현 완료 후 qa-simulator 별도 호출을 강력 권장**한다. 핵심 측정 메트릭:

| 메트릭 | 목표 | 실패 신호 → 조정 |
|---|---|---|
| 평균 빌드 영웅 처치 시간 | 2~4분(컨셉 §8) | <2분(영웅 너무 약함, 스킬 무의미) / >4분(영웅 클리어 폭주) |
| Swarm 빌드 영웅 처치율 | base 28장 튜닝 대비 −10%p 이내 | Swarm 무력화 시 §4 가드 적용 |
| P3 진입 후 처치율 | ≥60% | <60% → §6 데스스파이럴 가드 |
| 궤도 단독 분당 클리어 | <60마리 | 초과 시 §5.3 처방대로 `_bladeCount` 3 → 2 또는 `_bladeSphereRadius` 0.9 → 0.7 (커버리지 ↓). `_damage` 하향은 per-contact 처치 타수(§3)를 흔들어 후순위 |
| 필드 캡(18) 점유율 | 영웅 스킬 켜진 상태에서도 평균 12+ 유지 | 지속 <8 이면 영웅이 포위를 비움 = 카운터 과함 |

---

## §8. 구현 요청사항 (gameplay-programmer 용)

> plan §파일 구조·시그니처가 단일 진실. 본 절은 plan 과 정합되는 도메인 값을 명세한다. **plan 과 어긋남 0건** — 아래는 plan 의 SO 필드를 본 기획 §2 수치로 채우는 매핑이다.

### Enum 값 (plan A7 — `CommonEnum.cs`)
- `EVisual`: `HeroDashFx`, `HeroOrbitBladeFx`, `HeroNovaFx` 추가 (plan 그대로)
- `EData`: `HeroSkillLoadout` 추가 (plan 그대로)

### Interface (plan A1 — `CommonInterface.HeroSkill.cs`)
- `ISkillTarget`, `IHeroSkillContext`, `IHeroSkillRuntime` (plan 그대로)
- ⚠️ **형태 변경에 따른 신규 메서드 (plan sync 필요, §12)**:
  - `IHeroSkillContext.DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength)` — 부채꼴 히트 (P1). 기존 `DamageMonstersInLine` 은 더 이상 영웅 스킬에서 사용 안 함(다른 호출처 없으면 제거 가능 — gameplay-programmer 판단).
  - `IHeroSkillContext.DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength)` — per-sphere union 히트 (P2). 한 몬스터가 여러 구에 동시에 들어가도 **1회만** 데미지(union dedup, §2.2 중복 정책). 피격 수 반환은 dedup 후 고유 몬스터 수.
  - `SkillGeometry.InCone(Vector3 p, Vector3 origin, Vector3 dir, float length, float halfAngleDeg)` 및 `SkillGeometry.InSphere(Vector3 p, Vector3 center, float radius)` (XZ 평면 순수 기하). 기존 `InRing` 은 P3 디스크(`InRing(…,0,radius)`)에 계속 사용 — 제거하지 않음. `InLine` 은 P1 전용이었으므로 미사용 시 제거.
  - ⚠️ **`InCone` 은 radial(방사형) 부채꼴 판정 — `InLine` 의 축-투영 방식을 미러하지 말 것**: `(p−origin)` 의 **반경 거리 ≤ length** AND **dir 과의 각도 ≤ halfAngleDeg** 를 둘 다 만족해야 hit. (`InLine` 처럼 축 투영 길이 ≤ length 로 자르면 잘린 쐐기(truncated wedge)가 되어 부채꼴 비주얼(팬 mesh)과 어긋난다 — 사용자 "히트·비주얼 모두 부채꼴" 요구 위반 + §2.1 면적 산식 29.9 = ½·α·L² (radial 가정) 과 불일치.) 비주얼 팬 mesh(중심 + 호)와 동일한 radial 영역.
  - 구 중심 좌표는 런타임(`OrbitingBladeRuntime`)이 회전 각도로 계산해 `DamageMonstersInSpheres` 에 전달.

### JSON Sync (plan Phase E — 사용자 추가 요청 2026-06-04)
- 카드/밸런스와 동일하게 스킬 SO 3종 + 로드아웃을 `hero_skills.json` 으로 양방향 동기화 (`Lair > JSON Sync` 창에 "Hero Skills" 섹션 추가). §2 수치표가 JSON 의 SoT 가 되어 인스펙터 대신 JSON 으로도 튜닝 가능. 상세는 plan Phase E.

### 에셋 키 (파일명 = Enum 값명, Rule 03 §2)
- FX 프리팹: `HeroDashFx.prefab` / `HeroOrbitBladeFx.prefab` / `HeroNovaFx.prefab` (`Art/FX/`)
- 스킬 SO: `HeroSkill_DashStrike.asset` / `HeroSkill_OrbitingBlade.asset` / `HeroSkill_AoeNova.asset` (`Art/Skills/`)
- 로드아웃 SO: `HeroSkillLoadout.asset` (`Art/Skills/`, Addressable address = `HeroSkillLoadout`)

### SO 스키마 / 수치 필드 — `.asset` 채울 값 (§2 단일 진실)

**`HeroSkill_DashStrike.asset`** (`DashStrikeSkillData`) — ⚠️ `_halfWidth` 제거 · `_coneHalfAngle` 신설 (plan sync 필요, §12):
| 필드 | 값 |
|---|---|
| `_displayName` | "돌진 강타" |
| `_damage` | 80 |
| `_cooldown` | 3.0 |
| `_dashLength` | 7.0 |
| `_coneHalfAngle` | 35 |
| `_knockbackStrength` | 2.0 |
| `_centroidRadius` | 8.0 |

**`HeroSkill_OrbitingBlade.asset`** (`OrbitingBladeSkillData`) — ⚠️ `_bandHalfThickness` 제거 · `_bladeSphereRadius` 신설 · `_orbitRadius`/`_hitInterval`/`_bladeCount` 값 변경 (plan sync 필요, §12):
| 필드 | 값 |
|---|---|
| `_displayName` | "궤도 블레이드" |
| `_damage` | 15 |
| `_hitInterval` | 0.3 |
| `_orbitRadius` | 1.4 |
| `_bladeSphereRadius` | 0.9 |
| `_bladeCount` | 3 |
| `_rotationSpeedDeg` | 180 |

**`HeroSkill_AoeNova.asset`** (`AoeNovaSkillData`) — 수치 변화 없음(비주얼만 구로):
| 필드 | 값 |
|---|---|
| `_displayName` | "파멸의 노바" |
| `_damage` | 100 |
| `_cooldown` | 7.0 |
| `_radius` | 3.5 |
| `_knockbackStrength` | 3.0 |

**`HeroSkillLoadout.asset`** (`HeroSkillLoadout`) — Phase 리스트 (순서, **shipped 에셋 SoT**):
| 순서 | `HpFraction` | `Skill` |
|---|---|---|
| 0 | 0.85 | `HeroSkill_DashStrike` |
| 1 | 0.65 | `HeroSkill_AoeNova` |
| 2 | 0.45 | `HeroSkill_OrbitingBlade` |

> ⚠️ HpFraction 은 패시브 픽 임계(0.9·0.8·…·0.1)에서 각 0.05 아래로 오프셋한 값(§1 충돌 회피 reasoning). 순서 index 1 이 노바, index 2 가 궤도 블레이드임에 주의 — spec/plan 의 순서(궤도→노바)와 반대다.

---

## §9. 비주얼 (deliverable 6 — MVP §11.4 프리미티브 준수)

**색은 plan A11 을 그대로 유지**(사용자 확정: 색 불변). **메시는 형태 변경 v0.8 로 교체**(아래 표 — Cylinder→Sphere, Cube→Sphere ×3, Cube→부채꼴 mesh). 메시 변경은 plan A11 과 어긋나므로 §12 plan sync 노트로 명시.

| 스킬 | 메시 (형태 변경 v0.8) | 색 (유지) | 알파 | 비고 |
|---|---|---|---|---|
| Aoe Nova | **Sphere**(반경 3.5 = 지름 7.0, 영웅 감싸는 돔) ← Cylinder | `#FBBF24` (앰버) | 0.5 반투명 | 폭발·열기 연상. 반투명 구로 디스크 범위를 3D 가시화. 구 지름 = 2×히트반경(§2.3 정합). |
| Dash Strike | **평면 부채꼴 mesh**(절차 생성, ± `_coneHalfAngle` ·반경 `_dashLength`) ← Cube | `#93C5FD` (연파랑) | 1.0 | 영웅 색의 밝은 변형. 부채꼴로 히트 영역과 비주얼 정합. |
| Orbiting Blade | **Sphere ×3**(각 반경 `_bladeSphereRadius` 0.9 = 지름 1.8) ← Cube | `#E5E7EB` (밝은 회색) | 1.0 | 금속 구 블레이드. 영웅 주위를 공전, 각 구 반경 = 히트 반경 정합. |

**색 충돌 점검**: 4축 카드 테두리색(Tank 초록 #22C55E / Dps 빨강 #EF4444 / Debuff 보라 #A855F7 / Swarm 검정 #1F2937)·몬스터색(§11.4)과 스킬 3색(앰버/연파랑/회색)은 모두 다른 색역 → 혼동 없음. **색은 형태 변경과 무관하게 전부 유지**(사용자 확정). 영웅 스킬은 영웅 계열(파랑) 또는 중립(회색/앰버)으로 묶여 "영웅 것" 가독.

**프리미티브 원칙 정합 + 부채꼴 처방**:
- **Nova(구)·Orbit(구)**: Unity 기본 `PrimitiveType.Sphere` 프리미티브 사용 — §11.4 프리미티브 원칙 그대로 충족. 빌더(`LairVisualPrefabBuilder.BuildHeroSkillFx`)에서 `PrimitiveType.Cylinder`/`Cube` → `Sphere` 로 교체. Orbit 은 구 1개 프리팹을 런타임에 `_bladeCount` 개 Pop(현 plan 의 SpawnTracked ×N 패턴 유지). 구 균일 스케일: Nova = `_radius`×2, Orbit 블레이드 = `_bladeSphereRadius`×2.
- **Dash(부채꼴)** — ⚠️ **유일한 mesh-gen 필요 지점**: Unity 기본 프리미티브에 부채꼴(angular sector)이 없다. 늘린 Cube 는 직사각 box 라 부채꼴 히트와 시각이 어긋난다(사용자 요구 "히트·비주얼 모두 부채꼴" 위반). **권장안: 빌더에 평평한 부채꼴 mesh 를 절차 생성**(중심점 1 + 호 분할 정점 N개로 삼각 팬, XZ 평면, 단색 머티리얼). 프리미티브급 단순 도형(단색·flat)이라 §11.4 "프리미티브 + 단색" 정신에 부합 — 정식 아트가 아니다. 대안(부채꼴을 여러 가는 Cube 로 근사)은 정점 절약 이점이 없고 시각이 거칠어 비권장. mesh 파라미터(halfAngle·length)는 SO 값과 동기화해 빌드 시 반영하거나 런타임에 스케일/회전으로 맞춘다(gameplay-programmer 판단 — mesh 자체 생성은 필수).

> **plan sync**: 위 mesh 변경(Cylinder→Sphere ×2, Cube→부채꼴 mesh)은 plan A11 `BuildHeroSkillFx` 의 `PrimitiveType` 인자·프리팹 구성과 어긋난다. 구현 시 plan A11 갱신 필요(§12).

**⚠️ Orbiting Blade(P3) 구 가독성 게이트 (구현 후 화면 확인 필요 — 밸런스와 별개 축)**: `_bladeSphereRadius` 0.9 는 **커버리지(밸런스) 우선**으로 잡은 값이다. 그러나 궤도 1.4·구 지름 1.8(영웅 지름의 약 2배) 3개가 영웅 근처에 모이면 화면상 "구 3개 공전"이 아니라 **영웅을 감싼 덩어리(blob)** 로 뭉쳐 보일 수 있다 — `_bladeCount` 3 으로 노린 "회전 가독성"과 충돌하는 trade-off. 이 축은 qa-simulator(밸런스)로 안 잡히므로 **구현 후 사용자/디자이너 화면 확인**으로 게이트한다. 신호·조정: 구가 뭉쳐 회전이 안 보이면 ① `_bladeSphereRadius` 0.9 → 0.7(시각 분리 ↑, 단 커버리지 ↓ → ② 로 보상) ② `_bladeCount` 3 → 4(분리 + 커버리지 동시 보강). r↔커버리지↔가독성은 묶인 노브라 0.9 는 **확정 기본값이되 화면 확인 전까지 미세조정 여지 있는 값**으로 명시한다.

---

## §10. 컨셉서 §11/§8 갱신 (deliverable 5) — 본 기획서가 수행

아래 갱신을 `docs/design/project_lair_concept.md` 에 적용한다 (별도 편집 — 본 기획서와 동시 커밋):
- §8 밸런싱 기준에 "영웅 스킬이 켜진 상태의 2~4분 타깃" 보강.
- §11.2 포함/제외 표에 "영웅 액티브 스킬 3종" 행 추가(✅, 사용자 승격).
- §11.3 영웅 항목에 스킬 3페이즈 요약 추가.
- 변경 이력 v0.7 추가.

(실제 적용 내용은 컨셉서 파일에 반영됨 — 본 기획서 §1·§9 가 단일 진실, 컨셉서는 요약 + 참조 링크.)

---

## §12. 형태 변경 v0.8 — 변경 이력 + plan sync 노트 (2026-06-04 사용자 확정)

사용자가 영웅 스킬 3종의 **형태(geometry/visual)** 를 확정 변경했다. 데미지·쿨·HP 게이트·색 등 형태 외 밸런스 골격은 유지. 변경 요지 (스킬명 기준 — 페이즈 매핑은 §1 표 SoT 참조: Dash=P1·Nova=P2·Orbit=P3):

| 스킬 | 기존 형태 | 신규 형태 | 히트 변화 | 비주얼 변화 |
|---|---|---|---|---|
| Dash Strike | 직선 띠 (halfWidth 1.5, length 7) | **부채꼴** (± `_coneHalfAngle` 35°, length 7) | 직선 → 부채꼴(근접 좁고 원거리 넓음, 면적 ×1.42) | Cube → 평면 부채꼴 mesh |
| Orbiting Blade | 링밴드 근사 (반경 2.0 ± 0.6 전체 360° 판정) | **구 3개 공전 + per-sphere overlap** (구 반경 0.9, 궤도 1.4) | 360° 연속 → 각 구 arc 만(순간 ~31% shell 하한, dedup 1×/틱) | Cube → Sphere ×3 |
| Aoe Nova | 디스크 히트 + Cylinder 비주얼 | **디스크 히트 유지 + 구 비주얼** | 변화 없음 | Cylinder → Sphere (지름 7.0) |

### 확정 수치 (형태 변경분만 — 스킬명 기준)
- **Dash Strike**: `_coneHalfAngle` = **35°** (신규, ± 반각 → 전체 70°). `_halfWidth` 폐기. `_dashLength` 7.0 유지(개명 안 함).
- **Orbiting Blade**: `_bladeSphereRadius` = **0.9** (신규). `_orbitRadius` 2.0 → **1.4**, `_hitInterval` 0.4 → **0.3**, `_bladeCount` 2 → **3**. `_bandHalfThickness` 폐기. `_damage` 15 유지. 중복 데미지 = **1회/틱 union dedup**.
- **Aoe Nova**: 수치 변화 없음. 비주얼 구 지름 = 2 × `_radius` 3.5 = **7.0**.

### plan sync 필요 항목 (구현 시 `docs/superpowers/plans/2026-06-04-hero-skills.md` 동시 갱신)
1. **SO 필드 (plan Task A6/B1)**: `DashStrikeSkillData` — `_halfWidth` → `_coneHalfAngle`. `OrbitingBladeSkillData` — `_bandHalfThickness` 제거, `_bladeSphereRadius` 추가, `_orbitRadius`/`_hitInterval`/`_bladeCount` 기본값 변경.
2. **인터페이스 (plan A1)**: `IHeroSkillContext` 에 `DamageMonstersInCone`·`DamageMonstersInSpheres` 추가(§8). `DamageMonstersInLine` 은 P3 가 아닌 P1 전용이었으므로 호출처 없어지면 제거 검토.
3. **기하 (plan A2 `SkillGeometry`)**: `InCone`·`InSphere` 추가, 테스트 추가. `InLine` 은 미사용 시 제거. `InRing` 은 P3 디스크용으로 유지.
4. **런타임 (plan A6 `DashStrikeRuntime` / B1 `OrbitingBladeRuntime`)**: Dash 는 `DamageMonstersInLine` → `DamageMonstersInCone`. Orbit 은 `DamageMonstersInRing(inner,outer)` 단일 호출 → 구 중심 N개 계산 후 `DamageMonstersInSpheres` (union dedup). 회전 각도로 구 중심 좌표 산출 로직이 데미지 경로에 편입(기존엔 비주얼 전용이었음).
4a. **실구현 `IHeroSkillContext` (plan A9 `HeroSkillContext`)**: 신규 인터페이스 메서드 `DamageMonstersInCone`·`DamageMonstersInSpheres` 의 실구현을 추가한다. `DamageMonstersInCone` 은 `CharacterRegistry` 순회 + `SkillGeometry.InCone`(radial 판정, §8) 으로 필터. `DamageMonstersInSpheres` 는 구 중심 N개 각각 `SkillGeometry.InSphere` 로 모은 몬스터 집합을 **union dedup(HashSet 등)** 후 1회만 데미지/넉백 적용(§2.2 중복 정책) — 반환은 dedup 후 고유 몬스터 수. (plan A9 는 현재 `DamageMonstersInRing/InLine` 만 구현하므로 이 두 메서드 추가가 강제된다.)
4b. **테스트 더블 `FakeHeroSkillContext` (plan A5)**: 현재 `RingCalls`/`LineCalls` 만 기록한다. 신규 메서드 대응 기록 컬렉션 `ConeCalls`(Dir/Length/HalfAngle/Amount/Knockback)·`SpheresCalls`(SphereCenters/SphereRadius/Amount/Knockback) 를 추가해 `DamageMonstersInCone`/`DamageMonstersInSpheres` 호출을 기록하도록 확장한다 — 추가하지 않으면 A1 인터페이스 변경으로 컴파일 실패. (인터페이스 ↔ Fake ↔ 실구현 시그니처 일치는 plan "타입 일관성" 게이트 대상.)
5. **FX 빌더 (plan A11 `BuildHeroSkillFx`)**: `HeroNovaFx` Cylinder → Sphere, `HeroOrbitBladeFx` Cube → Sphere, `HeroDashFx` Cube → 절차 생성 부채꼴 mesh(빌더에 mesh-gen 헬퍼 신설). 스케일 공식: Nova `_radius`×2, Orbit `_bladeSphereRadius`×2.
6. **JSON Sync (plan Phase E)**: `hero_skills.json` DTO 의 Dash/Orbit 필드명 변경 동기화(`halfWidth`→`coneHalfAngle`, `bandHalfThickness` 제거 + `bladeSphereRadius` 추가).
7. **테스트 (plan A6/B1 EditMode)**: 부채꼴 각도 경계·per-sphere union dedup(한 몬스터 2구 동시 = 1데미지) 테스트 케이스 추가. ⚠️ "Orbit 런타임이 올바른 구 중심 N개로 `DamageMonstersInSpheres` 를 호출하는가"(회전 각도 → 구 중심 좌표 검증) 테스트는 **4b 의 `FakeHeroSkillContext.SpheresCalls` 기록이 선행조건**이다 — Fake 가 호출을 기록하지 못하면 이 런타임 테스트를 작성할 수 없으므로 4b 를 먼저 반영한다. (현재 dedup 자체 검증은 실구현 PlayMode 테스트 plan D1 영역 — `HeroSkillContext.DamageMonstersInSpheres` 가 2구 동시 진입 몬스터에 1회만 데미지 주는지.)

### v0.8.1 권장 보강 (2026-06-04 design-reviewer 검토 반영 — 수치 변경 없음, 문구·근거·목록 정확도)
design-reviewer 가 지적한 권장수정 3건을 in-place 반영했다 (수치 불변 — 문구·근거·목록만):
1. **§2.2 union dedup 근거 정정**: 현 수치(R=1.4, r=0.9, 120° 3구)에선 인접 구 중심거리 2.42 > 2r=1.8 이라 구끼리 안 겹침(틈 0.62) → "2구 동시 진입 ×3 스파이크" 는 현 수치에서 기하적 발생 불가. 중심부(반경<0.5) 미커버이나 melee 교전거리가 shell [0.5,2.3] 안이라 실용상 무해. dedup 은 "현 수치 비활성, R/r/count 향후 조정 대비 forward-compat 방어" 로 문구 정정.
2. **§7 vs §5.3 처방 모순 정정**: §7 표 "궤도 분당>60" 처방을 §5.3(보정 우선순위 `_bladeCount`/`_bladeSphereRadius` 우선, `_damage` 후순위)과 글자 일치시킴 (기존 "`_damage` 하향" → bladeCount/sphereRadius 우선).
3. **§12 plan sync 목록 보강**: 신규 인터페이스가 강제하는 두 implementer 변경 추가 — (a) 실구현 `HeroSkillContext`(plan A9, 항목 4a), (b) 테스트 더블 `FakeHeroSkillContext`(plan A5, 항목 4b — ConeCalls/SpheresCalls 추가). §12-7 런타임 테스트(Orbit→올바른 구 중심으로 InSpheres 호출)는 Fake 의 SpheresCalls 기록이 선행조건임을 명시.
4. (선택) §2.1 커버리지 비교의 tan(wedge) ↔ radial sector 모델 혼용을 radial 기준으로 보정 — 교차점 ~2.5(radial), 35° 결론 불변.

### 정정 — 형태 변경으로 무효화된 이전 분석
- **§2.2 검산(링밴드 면적 15.1 → 4마리 150 DPS)**: ring-band 가정 전용 → per-sphere 로 무효, §2.2 에서 재작성.
- **§5.3 "유일 지속원 궤도 = 37.5 DPS 연속"**: per-sphere arc·통과 위상 종속 → 연속 DPS 단정 불가로 정정.
- **§3 Orbit 열 초 환산(0.8s/1.6s 등)**: per-sphere 는 통과 시 틱이라 벽시계 비연속 → 초 표기 제거(틱 수만 유지).

---

## §11. Self-Review

> **[v0.8 형태 변경 self-review — 2026-06-04]** 아래는 형태 변경(§12) 반영 후 재점검 결과다.

- **Placeholder 잔존**: 0건. 모든 SO 필드 단정값(`_coneHalfAngle` 35 / `_bladeSphereRadius` 0.9 / `_orbitRadius` 1.4 / `_hitInterval` 0.3 / `_bladeCount` 3). **per-sphere 지속 DPS 는 "미정 방치"가 아니라 분석으로 단정 불가한 항목**(위상·duty-cycle 종속) → No-Placeholders 규약대로 "qa-simulator 결정 메트릭(Phantom-인접 처치 ≤1.5s) + 보정 순서(bladeCount→hitInterval)" 로 명시 치환(§2.2). 두 갈래 위임(`또는`)은 결정 메트릭의 임계 분기에만 사용(허용 — 조건부 분기).
- **스펙 커버리지**: 본 갱신은 spec 이 아닌 **사용자 직접 요구(2026-06-04 형태 3종)** 입력. 요구 매핑(스킬명 기준 — 페이즈는 §1 SoT) — Dash 부채꼴→§2.1·§12 / Orbit 구 per-sphere→§2.2·§12 / Nova 구 비주얼→§2.3·§9·§12 / 신규 SO 필드→§8 / 비주얼 프리미티브 처방→§9 / 변경 이력+plan sync→§12. 갭 0.
- **plan 정합 (형태 변경분)**: SO 필드명 변경·신규 인터페이스·기하·런타임·FX·JSON 7개 항목 모두 §12 plan sync 노트로 명시(어긋남을 숨기지 않고 "구현 시 plan 동시 갱신" 으로 기록). 변경 안 한 식별자(EVisual/EData 키·파일명)는 plan 과 글자 일치 유지. ⚠️ **단 페이즈 게이트·페이즈↔스킬 매핑은 plan(0.9/0.6/0.3 + P2 궤도/P3 노바)과 어긋난다 — shipped 에셋 정합 정정(v0.9, §13)으로 0.85/0.65/0.45 + P2 노바/P3 궤도 채택.** 이 어긋남은 §13 변경 이력에 명시.
- **내부 일관성**: §2.1 `_coneHalfAngle` 35 = §8 = §12 동일. §2.2 `_orbitRadius` 1.4 / `_bladeSphereRadius` 0.9 / `_bladeCount` 3 / `_hitInterval` 0.3 = §8 = §9(구 지름 1.8) = §12 동일. P3 `_radius` 3.5 → 구 지름 7.0 = §2.3 = §9 = §12 동일. §3 Orbit 초 환산 제거 = §2.2·§12 정정과 정합. §5.3 "37.5 DPS 연속" 무효화 = §2.2·§12 정합. 색 불변 = §9 전체 일치.
- **검산 수치 일관성 (2026-06-04 2차 보강)**: P2 통과 arc·시간·커버리지·revisit 을 단일 산식(Δθ = 2·arcsin(r/2R))으로 통일 — 통과 arc 37.5°, 통과 시간 0.21s, shell 커버리지 ~31%(하한), revisit 0.667s. (초안의 0.44s/0.27s/67%/0.56s 불일치를 정정 — stale 구 형태값·산식 혼용 제거. §2.2·§12 동일값.) 단위 명시(°, s, °·s⁻¹) + 산식 한 줄 동반.
- **시그니처/명명 일관성**: `_coneHalfAngle`·`_bladeSphereRadius`·`DamageMonstersInCone`·`DamageMonstersInSpheres`·`InCone`·`InSphere` 가 §2·§8·§9·§12 전반에서 글자 동일. 변형 표기 0건.
- **모호 표현**: 0건. mesh 생성 방식의 "런타임 스케일 vs 빌드 반영" 만 gameplay-programmer 판단 위임(구현 디테일 — 디자인 결정 아님, "mesh 자체 생성은 필수" 로 디자인 결정은 단정).
- **스코프**: 형태 변경 in-place 갱신 — 단일 구현 단위 유지. 분할 불필요.
- **구현 요청사항 완전성**: 신규 Enum 없음, 신규 Interface 2종·기하 2종 명세(§8), 에셋 키 불변, SO 스키마 신규/제거 필드 전부 명세(§8·§12).

> **[정정 이력 — 2026-06-04 design-reviewer 검토 반영]**: 1차 작성본은 **Plague HP 를 80 으로 잘못 채택**했다(stale `continuous-spawn-round.md` §6 값을 BalanceConfig 보다 우선시한 데이터 오류). live `BalanceConfig.asset`(=50)·컨셉 §11.3(=50)이 SoT 이므로 **Plague HP=50 으로 정정**했다. 영향: §0 출처표/스냅샷, §2.1·§2.2·§2.3 데미지 근거, §3 킬카운트(Orbit 6틱→4틱·근접 2타→1타), §4 Phantom 이동속도 배율(×1.3→×1.95, card-renewal §3.4·§4.2 실수치)·횡단시간(4.5s→3.0s). **설계 구조(스킬 데미지·쿨·반경·가드레일·결론)는 전부 유지된다** — Plague(50)은 Dash 80/Nova 100 으로 여전히 1타 즉살, Orbit 만 4틱으로 더 빨라졌고, §4 분당 44 공급 결론은 이동속도와 무관하므로 불변.

> **[v0.8.1 권장 보강 self-review — 2026-06-04]** 수치 변경 0건(문구·근거·목록만). 점검: ① §2.2 dedup 근거 정정 — 현 수치 구 비-겹침(중심거리 2.42 > 2r 1.8) 명시, "현 수치 비활성 + forward-compat 방어" 로 재서술. union dedup **정책 자체는 유지**(§8 line 267·§12 항목 1·5·7 와 정합) — 근거만 교체. ② §7 처방 칸을 §5.3(line 216) 와 **글자 일치**(`_bladeCount`/`_bladeSphereRadius` 우선, `_damage` 후순위) — 명명/처방 일관성 게이트 통과. ③ §12 plan sync 항목 4a(A9 실구현)·4b(A5 Fake)·7(런타임 테스트 선행조건) 추가 — 신규 인터페이스 2종이 강제하는 implementer 변경 누락 0. ④ §2.1 radial 보정(교차점 ~2.5) — 35° 결론·면적 산식 불변. 내부 일관성 재확인: dedup 정책 unchanged·처방 §5.3=§7 동일·plan sync 목록 신규 항목이 §8 인터페이스 정의와 정합. **v0.8.1: 4항목 in-place 보강, placeholder 0 / 모순 0 / 명명 변형 0건 — 통과.**

**Self-Review (v0.8 형태 변경): 2항목 보강 후 통과** (placeholder 0 / 요구 매핑 갭 0 / 명명 변형 0건). 보강 내역: ① P2 검산 수치 불일치(통과 시간 0.44/0.27 혼재, 커버리지 67%, revisit 0.56) 를 단일 산식으로 정정(0.21s / ~31% 하한 / 0.667s) ② P2 구 가독성 게이트(§9)·InCone radial 판정 명시(§8) 추가. plan 어긋남은 형태 변경 본질상 불가피 — §12 plan sync 노트 7항목으로 전부 명시(숨김 0). 핵심 밸런스 영향(P2 per-sphere 순간 커버리지 ~31% shell 하한으로 감소, 지속 DPS 분석 단정 불가)은 §2.2·§5.3·§12 에 정직하게 반영하고 qa-simulator 결정 메트릭 + 화면 가독성 게이트로 검증.

> **[v0.1 Plague HP 정정 이력 — 보존]** (아래는 형태 변경 이전 정정 기록 — 그대로 유지)

---

## §13. 변경 이력 — v0.9 HP 게이트·페이즈 매핑 shipped 정합 정정 (2026-06-06)

**무엇을 정정했나**: 본 기획서 전반의 HP 게이트가 **90/60/30%(0.9/0.6/0.3)** 로, 페이즈↔스킬 매핑이 **P2 궤도 블레이드 / P3 노바** 로 적혀 있었다. 이 값은 spec(`2026-06-04-hero-skills-design.md`)·plan(`2026-06-04-hero-skills.md`)의 1차 확정값을 그대로 옮긴 것이나, **shipped `Assets/_Lair/Art/Skills/HeroSkillLoadout.asset` 과 한 번도 일치한 적이 없다.** 기획서를 shipped 에셋(=실제 게임 동작)에 재정합한다.

**shipped 진실 (SoT — 에셋 + meta GUID 1:1 확인)**:

| 페이즈 | `HpFraction` | Skill (GUID) | 스킬 |
|---|---|---|---|
| P1 | **0.85** | `e45d864a…` | `HeroSkill_DashStrike` (돌진 강타) |
| P2 | **0.65** | `025ab35e…` | `HeroSkill_AoeNova` (파멸의 노바) |
| P3 | **0.45** | `447c923b…` | `HeroSkill_OrbitingBlade` (궤도 블레이드) |

- HP 게이트: 90/60/30 → **85/65/45** (각 페이즈를 패시브 임계에서 0.05 아래로 오프셋).
- 페이즈 순서: spec/plan 의 P2 궤도 / P3 노바 → **shipped 의 P2 노바 / P3 궤도** (순서 뒤바뀜).

**정정 범위 (HP 게이트·페이즈 매핑·관련 reasoning 만 — 데미지·기하 수치 불변)**:
- §헤더 목표(85/65/45 + 스킬 순서), §0 핵심 단순화·plan 정합 노트, §1 페이즈 표 + 잔여 HP(3400/2600/1800), §1 충돌 회피 reasoning 전면 재작성, §2.1·§2.2·§2.3 헤더 페이즈 라벨, §6 데스스파이럴 서사(잔여 HP 1200→1800, 노바=P2/궤도=P3 반영), §8 부록 로드아웃 표, §12 형태 변경 표 페이즈 라벨, §11 self-review.
- §2.x 의 데미지·쿨·기하 수치(부채꼴 35°, 구반경 0.9, 노바 반경 3.5 등)는 **건드리지 않았다** — 스킬 고유 속성이라 페이즈가 어디든 동일.

**git 이력 근거 (의도 역추론)**:
- 컷인 커밋 `688a649` "영웅이 스킬을 해금하는 순간 시간 정지 + 카메라 쿵 + 화면 중앙 배너 컷인" 이전 에셋 값 = **0.9 / 0.7 / 0.5** (패시브 임계 0.9·0.7·0.5 와 정렬).
- 같은 커밋이 게이트를 0.9/0.7/0.5 → **0.85/0.65/0.45 로 각 −0.05 오프셋** 하고, `docs/design/skill-unlock-cutscene.md`(시간정지 + 배너 컷인 연출)를 신규 추가했다.
- **역추론된 의도**: 패시브 카드픽은 HP 0.9·0.8·…·0.1 에서 발생. 스킬 해금을 패시브 임계와 정확히 겹치게 두면 컷인(시간정지+배너)과 카드픽 일시정지가 같은 HP 순간에 충돌한다. 0.05 씩 어긋내면 해금이 패시브 픽 사이 빈 구간에서 발동돼 충돌을 피한다.

> ⚠️ **이 충돌 회피 의도는 어느 문서에도 명문화돼 있지 않다 — 에셋 값 + git 커밋 `688a649` 의 동시 변경으로 역추론한 것**이다. 오히려 `skill-unlock-cutscene.md` 자체는 (별개로) 90/70/50 또는 90/60/30 가정 하에 "패시브 픽과 겹침 허용 + 배너 위치(하단 자막)로 회피" 를 택하고 있어, shipped 의 0.05 오프셋과는 다른 레이어의 회피책이다. 추후 컷인/게이트를 다시 손볼 때 이 두 회피 레이어(① 게이트 오프셋 ② 배너 위치)가 모두 의도된 것인지 사용자 확인을 권장한다. (`skill-unlock-cutscene.md` 의 임계 표기 90/70/50 가 shipped 85/65/45 와 어긋났던 건은 **정정 완료(2026-06-06 후속 커밋)** — 컷인 문서가 85/65/45 + P1 돌진/P2 노바/P3 궤도 로 본 §1 표와 1:1 정합됐고, 위 두 회피 레이어 관계도 그 문서 §3.1 정정 노트로 모순 없이 기술됨.)

**왜 spec/plan 이 아니라 에셋을 SoT 로 골랐나**: 기획서·spec·plan 이 모두 90/60/30 으로 일치하더라도, **실제 빌드에서 플레이어가 겪는 동작은 에셋 값(85/65/45)** 이다. 기획서는 "게임이 실제로 어떻게 동작하는가"를 기술해야 하므로 shipped 에셋을 진실로 채택했다. spec/plan 의 90/60/30 표기는 정정하지 않고 남겨 둔다(과거 결정 기록으로서의 가치 + 본 §13 이 그 어긋남을 명시하므로 추적 가능).
