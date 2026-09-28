# 몬스터 2D 전환 — 3D LittleGhost 모델 → 2D 픽셀 스프라이트 애니메이션

> 입력: 사용자 요청(메인 경유, 2026-09-28) "몬스터를 3D → 2D 로 전환, 일단 몬스터 쪽만". 영웅·카드·UI 는 범위 밖.
> 본 문서의 모든 "현행" 값은 코드/에셋 실측이다(§1). 추정치는 쓰지 않는다.
> 시안(승인 게이트 자료): `.mockups/monster-2d-conversion.html` — 6종 픽셀 스프라이트·상태별 애니메이션·전장 프리뷰·강화 발광·피격 플래시를 실제로 재생한다.

---

## § 헤더

- **목표**: 몬스터 6종(Wisp/Wraith/Reaper/Hex/Plague/Phantom)의 전투 비주얼을 3D LittleGhost 모델(단일 둥실 모션)에서 **종족색·실루엣이 분명한 2D 픽셀 스프라이트 + 상태별 프레임 애니메이션(대기/이동/공격/피격/사망)** 으로 교체한다.
- **검증 가설**: (a) 몸 색 = 종족 식별색, 종족마다 다른 돌출 실루엣을 주면 탑다운 전장에서 종족이 **바닥 원판(Aura) 없이도 한눈에** 구분되는가. (b) 현재 없는 공격·피격·사망 모션이 생기면 자동전투의 "누가 누구를 때렸고 누가 죽었나"가 모션으로 읽혀 전투 가독성·타격감이 오르는가. — **전투 판정·수치·타이밍은 불변**(§5.3·§5.5 불변식)이므로 밸런스 가설은 건드리지 않는다.
- **현재 단계 범위 적합성**: **범위 내**. CLAUDE.md §8 "아트/에셋 작업 허용 — 실제 스프라이트·모델 등 에셋으로 교체 가능". 기존 6종의 **표현 교체**이며 신규 종족·신규 카드·신규 영웅 리소스 제작 0(§8 "신규 영웅·몬스터·카드 리소스 제작 금지" 비저촉). 영웅(Knight 3D)·카드·UI 아이콘은 이번 범위에서 제외(§10 후속 분할).
- **핵심 메커니즘**: 몬스터 프리팹의 3D 모델 자리를 카메라 정면 빌보드 스프라이트(`Visual2D`)로 바꾸고, 이미 영웅용으로 존재하는 애니메이션 파이프(`CharacterAnimationDriver → CharacterAnimationController → AnimatorSink` 의 Speed/Attack/Hit/Dead 파라미터)를 몬스터에도 연결한다. 공격 데미지는 현행 **즉시 판정** 그대로 두고 공격 클립의 **히트 프레임을 인덱스 1(트리거 후 83~100ms)** 에 둬 시각만 맞춘다. 기존 연출(피격 반전·공격 백색 번쩍·스케일 펀치·강화 발광·데미지 팝업·HP바·바닥 원판)은 동작 규칙을 그대로 유지하고 스프라이트 셰이더가 같은 결과를 내게 한다.

---

## 1. 현행 실측 (코드·에셋 확인값)

### 1.1 종족 정체성

출처: `CommonEnum.cs`(EMonster·EBuildAxis 주석), `SpeciesVisual.cs`(한글명·발광색), `SpawnerStatusCell.SpeciesColor`(식별색), `BalanceConfig.asset`(MonsterStatRow Key 0~5), 각 `Art/Characters/<종>.prefab`(루트 스케일·3D 소스 프리팹), `MonsterIcons/<종>.png`(3D 외형 렌더).

| EMonster | 한글명 | Enum 주석 역할 | 빌드 축 | HP | Power | Range | Cooldown(s) | MoveSpeed | SpawnPeriod(s) | 루트 스케일 | 3D 소스 · 외형 | SpeciesColor | SpeciesGlowColor |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Wisp | 도깨비불 | 기본 잡몹 | Tank (Swarm 카드 SpawnWisps·Swarm T1 보조) | 200 | 5 | 1 | 1.0 | 1.0 | 9 | 0.6 | LittleGhost_N1 · 무장식 흰 시트 유령 | #22C55E | #28E66E |
| Wraith | 망령 | 보스급 탱커 | Tank | 500 | 10 | 1.3 | 1.0 | 0.8 | 20 | 1.3 | LittleGhost_V1 · 뿔 달린 얼룩 흰 유령 | #6B7280 | #C0CCE6 |
| Reaper | 사신 | 근접 광룡 | Dps | 100 | 6 | 1 | 0.5 | 1.5 | 12 | 0.9 | LittleGhost_H1 · 흰 유령 + 붉은 도끼 | #EF4444 | #E64141 |
| Hex | 저주술사 | 원거리 캐스터 | Dps | 60 | 9 | 5 | 1.0 | 1.4 | 15 | 0.8 | LittleGhost_M1 · 파란 마녀모자 유령 | #EAB308 | #E6AF08 |
| Plague | 역병귀 | 둔화 디버퍼 | Debuff | 50 | 2 | 1 | 1.0 | 1.3 | 10 | 0.5 | LittleGhost_V2 · 노란 얼룩 뿔 유령 | #A855F7 | #9C4FE6 |
| Phantom | 환령 | 스웜 | Swarm | 30 | 2 | 1 | 0.8 | 2.4 | 6 | 0.4 | LittleGhost_N2 · 주황 유령 | #1F2937 | #82ABE6 |

- **공격 방식**: 6종 모두 `MeleeAttacker`(거리 기반) — Hex 도 발사체 없이 사거리 5 에서 **즉시 데미지**. 6종 프리팹 모두 `_deferStrike: 0`(즉시 경로 `TryAttack`).
- **Plague 고유**: `PlagueSlowOnHit` — 적중 시 영웅 둔화 부착(SlowFactor 0.8, 1.5s).

### 1.2 현행 애니메이션·연출 파이프

| 항목 | 현행 | 출처 |
|---|---|---|
| 몬스터 애니메이터 | `Ghost_Kid.controller` — 상태 1개 `idle_updown`(둥실 루프), **파라미터 0개**. 6종 공용 | `Art/Animations/Ghost_Kid.controller` |
| 애니 드라이버 | 몬스터 프리팹에 `CharacterAnimationDriver`·`CharacterAttackStrikeRelay` **미부착**(부착처 = Knight.prefab 뿐, GUID 검색) | 스크립트 GUID 검색 |
| 드라이버 파라미터 계약 | `Speed`(Float) · `Attack`(Trigger) · `AttackVariant`(Int, 0→1→2 순환) · `Hit`(Trigger) · `Dead`(Bool) · `Spawn`(Trigger) | `AnimatorSink.cs` |
| 드라이버 기본값 | `_walkSpeed 1` · `_runSpeed 2` · `_hitReactionCooldown 0.4` · `_attackSuppressWindow 0.5` | `CharacterAnimationDriver.cs` |
| 몬스터 공격 애니 트리거 | 게이트(IAttackGate) 없음 → `IAttacker.OnHit`(데미지 적용 직후) → `OnAttack` → `TriggerAttack` | `CharacterAnimationDriver.HandleAttackHit` |
| 스폰 게이트 | 몬스터는 무시(`DeferStrike == false` → 즉시 교전) | `AutoCombatAI.IsSpawnGatePassed` |
| 공격자 연출 | `AttackJuice` — 루트 스케일 펀치 ×1.15 · 0.12s(sin 반원) + 몸체색 → 흰색 0.6 lerp 번쩍(동일 0.12s, `HitFlash.FlashAttack` 위임) | `AttackJuice.cs`, Wisp.prefab |
| 피격 연출 | `HitFlash` — 자식 Renderer 색 **반전** 0.1s(`_duration 0.1`), 이름 `Aura`/`HpBar` 접두 제외 | `HitFlash.cs` |
| 데미지 숫자 | `DamageFeedback → DamagePopup` — 콜라이더 상단에서 1.2u 부상 · 0.7s 선형 페이드, 몬스터 공격 숫자색 = SpeciesColor, 명도 분기 외곽선(#1A1A1A / #FFFFFF) | `DamagePopup.cs`, `AttackJuice.CacheRepColor` |
| 피격 임팩트 | 피격자=몬스터 → `EVisual.MonsterHitImpact`, 피격자=영웅 → `EVisual.HitImpact` | `DamageFeedback.cs` |
| 사망 | `DespawnOnDeath._delay 0` → 사망 즉시 풀 반환(시체 연출 없음), 반환 직전 `MonsterDied` 발행 | `DespawnOnDeath.cs` |
| 강화 발광 | `MonsterEnhancementVisual` — `_EMISSION` 키워드 + `_EmissionColor = SpeciesGlowColor × _emissionByLevel[Lv-1]`, `[1.5, 1.9, 2.3, 2.7, 3.2]`, Lv0 off | `MonsterEnhancementVisual.cs` |
| 레벨 배지 | 전장 월드엔 없음. 도감·상점·인게임 상태 셀 UI 에만 "Lv N" 칩(#0F1520 α0.85, Lv0 숨김) | `monster-codex-prefab-icon-enhancement.md` §5 |
| HP 바 | `MonsterHpBar` WorldSpace 캔버스, 래퍼 로컬 y 1.2(6종 동일), 카메라 회전 복사 빌보드, 텍스트 숨김 | 6종 prefab `HpBarWrapper` |
| 바닥 원판 | 자식 `Aura` — 실린더 로컬 0.47 × 0.01, 머티리얼 `Mat_<종>_Aura` = SpeciesColor | 6종 prefab, `Art/Materials/` |
| 회전 | `SimpleRotator` yaw 540°/s 보간 | `SimpleRotator.cs` |
| 콜라이더 | Capsule 반지름 0.5 · 높이 1.8 · 중심 0.9(6종 동일, 루트 스케일 적용) | 6종 prefab |
| 카메라(Battle) | Orthographic size 7, 회전 x=0.4226 → 피치 50°(=2·asin 0.4226), 위치 (0,10,−10) | `Battle.unity` Main Camera |

### 1.3 관찰 — 2D 전환이 푸는 문제

- **3D 외형과 식별색 불일치**: Phantom 몸은 주황(식별색 #1F2937 검정), Plague 몸은 노랑(식별색 #A855F7 보라), Wisp·Reaper 몸은 흰색. 현재 종족 식별은 발밑 `Aura` 원판 색에 의존한다. 2D 전환에서 **몸 주색 = SpeciesColor** 로 정렬하면 카드 테두리색·데미지 숫자색·원판색·몸색이 하나로 묶인다(컨셉 §11.4 "내가 픽한 카드 색 = 내가 키우는 몬스터 색" 학습과 일치).
- **모션 부재**: 몬스터는 둥실 루프 1개뿐이라 공격·피격·사망이 스케일 펀치·색 번쩍·즉시 소멸로만 표현된다.

---

## 2. 시점 — 3/4 탑다운 정면 스프라이트 + 카메라 정면 빌보드 + 좌우 반전 (확정)

| 안 | 내용 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| **A. 3/4 탑다운 정면형 1방향 + 좌우 반전** | 살짝 위에서 본 정면-우측 3/4 원화 1세트, 좌측 이동 시 flipX. 카메라 정면 빌보드 | 얼굴(눈 = 발광 마스크)이 항상 보임, 방향 수 1 → 제작량 최소, 피치 50° 카메라와 원근감 일치 | 화면 위쪽으로 멀어지는 이동도 정면을 보임 | ✅ 채택 |
| B. 2D 사이드뷰 1방향 + 좌우 반전 | 측면 원화 | 실루엣이 가장 또렷, 제작량 A 와 동일 | 몬스터는 링 사방에서 중앙으로 수렴(컨셉 §4.1) → 접근 각도가 균등하면 세로 성분이 가로보다 큰 이동이 50%(45°~135° + 225°~315° = 180° / 360°)인데 측면 모습으로 위아래 이동 = "게걸음"으로 읽힘. 피치 50° 바닥 위에 종이 인형처럼 섬 | ✗ |
| C. 탑다운 3방향(정면/측면/후면) + 측면 반전 | 이동 방향별 원화 | 방향 정확 | 원화 3세트 → 프레임 수 ×3(150 → 450), 둥실 떠다니는 유령 테마에서 이득 작음 | ✗ (비용) |

- **빌보드 방식 = 카메라 회전 복사(전체 빌보드)** — HP 바·데미지 팝업과 같은 방식. Y축만 도는 직립 빌보드는 피치 50° 카메라에서 화면 높이가 `h × cos 50° = 0.643h` 로 눌려 보이므로 기각. 전체 빌보드는 화면 높이 = h.
- **바닥 접지**: 스프라이트 피벗 = 셀 하단 중앙(발 밑 = 월드 루트 위치). 기존 `Aura` 원판이 그림자 역할로 접지감을 준다.
- **떠다니는 유령 테마의 이점**: 다리가 없어 이동 속도와 발 모션을 맞출 필요(풋 슬라이딩 보정)가 없다 → 이동 클립 재생 속도 고정(§5.2).

---

## 3. 아트 스타일·해상도 규격

### 3.1 스타일 — 픽셀아트 프레임 애니메이션 (확정)

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| **픽셀아트 프레임 애니** | 작은 화면 크기(최소 19px)에서 실루엣이 선명, 프레임당 제작 비용 최저, 구매 가능한 픽셀 몬스터 에셋 풀이 넓음(§8 선택지 확장) | 비정수 배율에서 픽셀 크기 불균일(피치 카메라라 픽셀 퍼펙트 불가) | ✅ 채택 |
| HD 2D + 본 리깅(Unity 2D Animation) | 부드러운 모션, 프레임 수 무관 | 종족마다 리깅 비용, 19px 급 소형 종에서 디테일 손실 | ✗ |
| 기존 3D 모델을 스프라이트로 프리렌더 | 현 외형 유지 | 3D 쪽에 공격·피격·사망 모션이 없어 3D 애니 제작이 선행돼야 함 → 비용 절감 없음, 식별색 불일치(§1.3) 그대로 | ✗ |

### 3.2 PPU — 48 (6종 공통, 확정)

화면 1유닛 픽셀 = 세로 해상도 ÷ (2 × orthographic size 7): 1080p → 1080 ÷ 14 = 77.1px/u, 720p → 720 ÷ 14 = 51.4px/u.

| PPU | 1080p 텍셀→화면 | 720p 텍셀→화면 | Phantom(0.4u) 원화 높이 | 판정 |
|---|---|---|---|---|
| 32 | 2.41px | 1.61px | 0.4×32 = 12.8 → 13px | ✗ 귀·눈·날개 3요소를 13px 에 넣으면 요소당 2~3px → 소형종 식별 한계 |
| **48** | **1.61px** | **1.07px** | **0.4×48 = 19.2 → 19px** | ✅ 모바일 720p 에서 1:1 에 가장 가까움(선명), PC 1080p 에서 텍셀당 1.61px |
| 64 | 1.21px | 0.80px | 25.6 → 26px | ✗ 720p 에서 텍셀 < 화면 픽셀 → 포인트 필터 시 픽셀 누락·시머링 |

- **6종 동일 PPU = 동일 픽셀 밀도**(픽셀아트 규칙: 한 화면에 픽셀 크기가 섞이지 않게). 이를 위해 `Visual2D` 자식의 **월드 스케일 = 1**(루트 스케일 상쇄, §11 수치 표)로 두고, 종족 간 크기 차이는 원화 픽셀 수로만 낸다.

### 3.3 종족별 크기·셀 규격

기준: 현행 루트 스케일 = 스프라이트 몸 월드 높이(로컬 1.0 = HP바 로컬 1.2 아래 0.2 여유). 몸 px = 루트 스케일 × 48 반올림.

| 종족 | 루트 스케일 | 몸 높이 px (산식) | 셀 N×N | 셀 여유 용도 | 시트(6열×5행) | 스프라이트 최상단 월드 (부유 2 + 몸 + 대기 상승 1) ÷ 48 | **HP바 하단 목표 월드** (+0.08u) |
|---|---|---|---|---|---|---|---|
| Wisp | 0.6 | 0.6×48 = 28.8 → **29** | 48 | 몸통 박치기 전진 4px + 불꽃 스파크 | 288×240 | 32 ÷ 48 = 0.667 | **0.747** |
| Wraith | 1.3 | 1.3×48 = 62.4 → **62** | 88 | 소매 치켜듦·바닥 충격 먼지 | 528×440 | 65 ÷ 48 = 1.354 | **1.434** |
| Reaper | 0.9 | 0.9×48 = 43.2 → **43** | 72 | 낫 휘두름 궤적(피벗 반경 33px) | 432×360 | 46 ÷ 48 = 0.958 | **1.038** |
| Hex | 0.8 | 0.8×48 = 38.4 → **38** | 56 | 떠 있는 구슬 + 시전 고리(반경 8px) | 336×280 | 41 ÷ 48 = 0.854 | **0.934** |
| Plague | 0.5 | 0.5×48 = **24** | 40 | 독 방울 발사 경로 | 240×200 | 27 ÷ 48 = 0.563 | **0.643** |
| Phantom | 0.4 | 0.4×48 = 19.2 → **19** | 32 | 좌우 날개 폭 | 192×160 | 22 ÷ 48 = 0.458 | **0.538** |

- **몸 높이 px** = 대기 F0 기준 불투명 픽셀의 세로 범위(무기·뿔·모자·귀 포함, 공격 FX 제외).
- **HP 바 여유 0.08u = 3.84px(PPU 48)**. 공격 프레임의 순간 상승(Phantom 공격 F0 −2px 가 대기 상승 1px 보다 1px 더 높음 = 0.02u)은 이 여유 안이다.
- 데미지 팝업 시작 높이는 현행대로 콜라이더 상단(1.8 × 루트 스케일)이며 6종 모두 HP 바 목표보다 위(예: Phantom 1.8×0.4 = 0.72 > 0.538) → 변경 없음.

### 3.4 공통 원화 규칙

| 항목 | 규칙 |
|---|---|
| 피벗 | 셀 하단 중앙 (0.5, 0) |
| 부유 | 몸 최하단(밑단)은 셀 바닥에서 2px 위 — 유령이 떠 있음. 대기 루프는 ±1px 상하 |
| 기본 방향 | 정면-우측 3/4. 공격 모션은 화면 +X(오른쪽)로 뻗는다. 좌측 대상은 flipX |
| 광원 | 좌상단. 각 부위 4톤(외곽선 · 그림자 · 기본 · 하이라이트) |
| 외곽선 | 1px `#0A0D14`. **Phantom 만 1px `#82ABE6` 림**(몸 식별색 #1F2937 이 어두운 전장 바닥에 묻히므로) |
| 몸 주색 | SpeciesColor 그대로(§1.1) — 데미지 숫자색·Aura·카드 테두리와 동일 색 |
| 발광 부위 | 종족별 지정(§6.1) — 별도 발광 마스크 시트에 흰색으로 그린다 |

---

## 4. 종족별 2D 캐릭터 컨셉

테마는 현행 "LittleGhost(영혼/유령)" 계승(`CommonEnum.EMonster` 주석). 3D 의 뿔·모자·무기 같은 식별 소품은 이어받되, **몸 주색을 종족색으로 바꾸고 소품을 실루엣 돌출부로 키운다**.

### 4.1 실루엣 구분 매트릭스

| 종족 | 기본 도형 | 돌출 실루엣 | 높이 순위 | 몸 주색 |
|---|---|---|---|---|
| Wisp 도깨비불 | 원(●) | 머리 위로 솟는 불꽃 꼬리 | 4위 (29px) | #22C55E |
| Wraith 망령 | 넓은 종(鐘)형 수의 | 뿔 2개 · 양옆 소매 덩어리 · 쇠사슬 | 1위 (62px) | #6B7280 |
| Reaper 사신 | 세로 두건 망토 | 머리 위 가로 초승달 낫 날 | 2위 (43px) | #EF4444 |
| Hex 저주술사 | 세로 로브 | 뾰족 마녀모자(▲) · 옆에 떠 있는 구슬 | 3위 (38px) | #EAB308 |
| Plague 역병귀 | 가로로 납작한 덩어리(▬) | 등 위 기포 3개 · 작은 뿔 · 흘러내리는 독 | 5위 (24px) | #A855F7 |
| Phantom 환령 | 작은 유령 | 좌우 박쥐 날개(V) · 뾰족 귀 | 6위 (19px) | #1F2937 + 림 #82ABE6 |

6종의 **기본 도형이 모두 다르고**(원/종/세로망토/세로로브+삼각/가로덩어리/날개), 높이 순위가 역할(보스급 탱커 최대 → 스웜 최소)과 일치한다. 흑백 실루엣만으로 구분 가능한지는 §9 육안 게이트로 확인.

### 4.2 종족별 상세

**Wisp 도깨비불 — 기본 잡몹 / Tank**
- 형태: 동그란 몸통 + 정수리에서 위로 흔들리는 녹색 불꽃. 큰 검은 눈 2개(흰 하이라이트 1px), 작은 입.
- 팔레트: 몸 #22C55E / 하이라이트 #86EFAC / 그림자 #15803D · 불꽃 #4ADE80 / #BBF7D0 / #16A34A · 불꽃 심지 #28E66E(발광).
- 3D 계승: 무장식 시트 유령의 담백함 → 장식 없이 불꽃 하나로 "도깨비불" 이름을 시각화.
- 공격: 몸을 움츠렸다가 앞으로 몸통 박치기(불꽃이 길게 늘어남) + 녹색 스파크.

**Wraith 망령 — 보스급 탱커 / Tank**
- 형태: 6종 중 가장 크고 넓은 종형 수의. 뿔 2개(3D V1 뿔 계승), 좌우로 튀어나온 소매 덩어리 끝에 쇠사슬. 움푹한 눈구멍 속 냉백색 눈, 가슴에 빈 마름모 룬.
- 팔레트: 몸 #6B7280 / #9CA3AF / #4B5563 · 뿔 #E7D8C9 / #F5EDE4 / #B8A99A · 눈구멍 #111827 · 눈·룬 #C0CCE6(발광) · 사슬 #9CA3AF.
- 공격: 양 소매를 치켜들었다가 내리찍기 + 발밑 먼지 충격. 대기·이동 FPS 를 낮춰(6·8fps) 묵직함.

**Reaper 사신 — 근접 광룡 / Dps**
- 형태: 붉은 두건 망토, 두건 속 검은 얼굴과 붉은 눈. 뒤쪽 손으로 쥔 긴 자루 + 머리 위를 가로지르는 초승달 낫 날(3D 도끼 → 2D 에선 실루엣이 더 특징적인 낫으로 변경, "사신" 이름과 일치).
- 팔레트: 망토 #EF4444 / #FCA5A5 / #B91C1C · 두건 속 #0A0D14 · 눈 #F87171 · 자루 #8B5A2B / #A47148 / #5C3A1A · 날 #D1D5DB / #F3F4F6 / #9CA3AF · 날 안쪽 가장자리 #E64141(발광).
- 공격: 낫을 뒤로 젖혔다가(윈드업) 앞-아래로 크게 베기 + 분홍 궤적. 쿨다운 0.5s 에 맞춰 공격 클립 4프레임(0.333s).

**Hex 저주술사 — 원거리 캐스터 / Dps**
- 형태: 노란 로브 유령 + 끝이 휜 뾰족 마녀모자(3D M1 모자 계승, 색은 종족색 계열로) + 몸 오른쪽에 떠 있는 금빛 구슬. 모자 띠에 보석.
- 팔레트: 로브 #EAB308 / #FDE047 / #A16207 · 모자 #A16207 / #CA8A04 / #713F12 · 띠 #1C1917 · 보석 #E6AF08(발광) · 구슬 #FEF08A / #FFFBEB / #CA8A04(발광) · 시전 고리 #FDE047.
- 공격: 구슬을 들어 올려 번쩍 + 고리 확산. 발사체는 없다 — 현행 판정이 사거리 5 즉시 데미지이므로 영웅 쪽 `HitImpact` 로 "맞았다"를 표시(§10 후속에 트레이서 검토).

**Plague 역병귀 — 둔화 디버퍼 / Debuff**
- 형태: 가로로 납작한 보라 덩어리(컨셉 §11.4 "납작하게" 계승), 작은 뿔 2개(3D V2 뿔 계승), 튀어나온 큰 눈, 등 위로 올라가다 터지는 기포 3개, 밑으로 떨어지는 독 방울.
- 팔레트: 몸 #A855F7 / #C084FC / #7E22CE · 뿔 #E7D8C9 계열 · 눈 흰자 #F3E8FF / 동공 #3B0764 · 기포 #D8B4FE / #F3E8FF / #9C4FE6(발광) · 독 방울 #9C4FE6(발광).
- 공격: 몸을 부풀렸다가 입에서 독 방울 발사(3프레임에 걸쳐 앞으로 날아감).

**Phantom 환령 — 스웜 / Swarm**
- 형태: 가장 작은 검은 유령 + 좌우 박쥐 날개 + 뾰족 귀. 청회색 눈, 날개 끝이 빛남. 외곽선만 청회 림.
- 팔레트: 몸 #1F2937 / #374151 / #111827 · 날개 #111827 / #1F2937 / #030712 · 림 외곽선 #82ABE6 · 눈·날개 끝 #82ABE6(발광) · 송곳니 #F3F4F6.
- 공격: 날개를 들었다가 앞으로 급강하 물기(송곳니 노출). 대기 4프레임 10fps 날갯짓.

---

## 5. 애니메이션 세트

### 5.1 상태 목록

| 상태 | 형식 | 트리거(현행 드라이버) |
|---|---|---|
| 대기 Idle | 스프라이트 클립, 루프 | `Speed < 0.5` |
| 이동 Move | 스프라이트 클립, 루프 | `Speed > 0.5` (몬스터는 이동 중 1.0) |
| 공격 Attack | 스프라이트 클립, 1회 | `Attack` 트리거 (몬스터 = `OnHit` 직후) |
| 피격 Hit | 스프라이트 클립, 1회 | `Hit` 트리거 (드라이버 억제 규칙 §5.4) |
| 사망 Death | 스프라이트 클립, 1회, 마지막 프레임 유지 | `Dead = true` |
| 등장 Spawn | **클립 없음 — 트윈**(§5.6) | 풀 Pop(`OnEnable`) |

### 5.2 종족별 프레임 규격

형식: 프레임 수 × FPS = 길이. 이동 클립은 재생 속도 고정(유령이라 발 동기화 불필요, §2).

| 종족 | 대기 (루프) | 이동 (루프) | 공격 (1회) · 히트 프레임 | 피격 (1회) | 사망 (1회) | 합계 |
|---|---|---|---|---|---|---|
| Wisp | 6 × 8 = 0.750s | 6 × 12 = 0.500s | 5 × 12 = 0.417s · **F1 (+83ms)** | 3 × 12 = 0.250s | 6 × 12 = 0.500s | 26 |
| Wraith | 6 × 6 = 1.000s | 6 × 8 = 0.750s | 5 × 10 = 0.500s · **F1 (+100ms)** | 3 × 12 = 0.250s | 6 × 12 = 0.500s | 26 |
| Reaper | 6 × 8 = 0.750s | 6 × 12 = 0.500s | 4 × 12 = 0.333s · **F1 (+83ms)** | 3 × 12 = 0.250s | 6 × 12 = 0.500s | 25 |
| Hex | 6 × 8 = 0.750s | 6 × 12 = 0.500s | 5 × 12 = 0.417s · **F1 (+83ms)** | 3 × 12 = 0.250s | 6 × 12 = 0.500s | 26 |
| Plague | 6 × 8 = 0.750s | 6 × 12 = 0.500s | 5 × 12 = 0.417s · **F1 (+83ms)** | 3 × 12 = 0.250s | 6 × 12 = 0.500s | 26 |
| Phantom | 4 × 10 = 0.400s | 4 × 12 = 0.333s | 4 × 12 = 0.333s · **F1 (+83ms)** | 3 × 12 = 0.250s | 6 × 12 = 0.500s | 21 |

- **총 프레임** = 26 + 26 + 25 + 26 + 26 + 21 = **150**.
- 프레임 번호는 0부터(F0 = 첫 프레임). 히트 프레임 "+Nms" = 히트 프레임 인덱스 ÷ FPS(1 ÷ 12 = 83ms, 1 ÷ 10 = 100ms).

**프레임 내용(6종 공통 골격, 종족 특화는 §4.2)**

| 상태 | F0 | F1 | F2 | F3 | F4 | F5 |
|---|---|---|---|---|---|---|
| 대기 | 기준 | +1px 상승 | +1px | 기준 | −1px 하강(눈 깜박임, Wraith 제외) | −1px |
| 이동 | 앞으로 기울임(상단 전방 쏠림) + 밑단 물결 가속 | ← 반복, 상하 1px 튐 | | | | |
| 공격 | 예비 동작(움츠림/치켜듦/윈드업) | **타격(히트)** | 팔로스루 | 복귀 | 기준(Reaper·Phantom 은 F3 까지) | — |
| 피격 | 뒤로 2px 밀림 + 가로 1.12·세로 0.88 찌그러짐, 찡그린 눈 | 반동 늘어남 | 기준 | — | — | — |
| 사망 | 충격(X 눈) | 위로 1px, 분해 시작 | 분해 진행 · 영혼 입자 상승 | ← | ← (불투명 픽셀 F0 대비 30% 이하) | 거의 소멸(불투명 픽셀 F0 대비 10% 이하) |

- 사망 페이드는 **픽셀 분해로 원화에 굽는다**(클립은 스프라이트 교체만, 색·알파 커브 없음 — 등장 트윈이 쓰는 SpriteRenderer 알파와 충돌 방지).

### 5.3 공격 타이밍 — 판정과 히트 프레임 연결 (확정: 즉시 판정 유지)

| 안 | 내용 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| **A. 즉시 판정 유지 + 히트 프레임 F1** | 데미지는 현행대로 `TryAttack` 순간. 공격 클립은 그 직후 재생되고 F1 에 타격 포즈 | 전투 판정·DPS·타이밍 **불변** → 밸런스 재시뮬 불필요, `CharacterAnimationDriver` 몬스터 경로(`OnHit → OnAttack`)를 그대로 사용 | 데미지 숫자·임팩트가 타격 포즈보다 83~100ms 먼저 뜬다 / 윈드업이 데미지 이전에 올 수 없음 | ✅ 채택 |
| B. 영웅식 지연 판정 | `_deferStrike` on + `CharacterAttackStrikeRelay` + 클립에 `OnAttackStrike` 애니 이벤트 → 윈드업 뒤 데미지 | 모션과 데미지가 프레임 단위로 일치 | 첫 타 지연·헛스윙(대상 이탈 시)으로 6종 DPS·교전 타이밍이 바뀜 → qa-simulator 재검증 필요, 스폰 게이트(`_spawnGateFallback`)가 몬스터에도 걸려 교전 시작 지연 | ✗ (이번 범위는 비주얼 교체) |

- **83~100ms 선행 허용 근거**: 히트 프레임을 인덱스 1 로 고정해 선행을 최대 1프레임(Wraith 100ms, 나머지 83ms)으로 묶는다. 윈드업(F0)은 "데미지 직후의 짧은 반동 준비"로 연출되며 타격 포즈는 데미지 숫자가 떠오르는 0.7s 부상 구간 초반에 겹친다.
- **`CharacterAttackStrikeRelay` 는 몬스터에 부착하지 않고, 몬스터 클립에 애니메이션 이벤트(`OnAttackStrike`/`OnAttackEnd`/`OnSpawnAnimEnd`)를 넣지 않는다** — 넣으면 즉시 경로와 이중 적용 위험.
- **재트리거 규칙**: 공격 클립이 실효 쿨다운보다 길어지는 경우(예: Reaper 0.5s × ReaperAtkSpeed 0.7 × Dps T2 0.8 ÷ Frenzy 1.5 = 0.187s < 클립 0.333s)에는 **새 공격 트리거가 오면 F0 부터 즉시 재시작**한다(Any State → Attack, Can Transition To Self on). 그래서 히트 프레임은 항상 데미지 후 1프레임 안에 보인다.
- `AttackVariant`(0→1→2 순환)은 몬스터 컨트롤러에서 **미사용** — 공격 클립 1개. 파라미터는 경고 방지를 위해 선언만 한다(§5.7).

### 5.4 피격 반응 규칙 (현행 컨트롤러 그대로)

`CharacterAnimationController.OnDamaged`: 사망 중 무시 · 최근 공격 0.5s(`_attackSuppressWindow`) 내 무시 · 최근 피격 리액션 0.4s(`_hitReactionCooldown`) 내 무시. 피격 클립 0.25s < 0.4s → 피격 클립끼리 겹치지 않고, 공격 클립 최장 0.5s(Wraith) ≤ 0.5s → 공격 모션을 피격이 끊지 않는다. **피격 반전 플래시(`HitFlash`)는 이 억제와 무관하게 매 데미지마다** 나간다(현행).

### 5.5 사망 — 제자리 사망 클립 + 풀 반환 0.5s 지연 (확정)

| 안 | 내용 | 판정 |
|---|---|---|
| **A. 제자리 사망 클립 → 0.5s 뒤 풀 반환** | `Dead=true` 로 사망 클립 재생, `DespawnOnDeath._delay = 0.5`(= 사망 클립 길이 6 ÷ 12) | ✅ 채택 — 기존 `SetDead` 경로·풀 반환 경로 재사용, 방향·발광 연속성 유지 |
| B. 즉시 풀 반환 + 사망 FX 프리팹 분리 | 반환은 현행대로, 종족별 사망 연출을 별도 `EVisual` 풀 FX 로 | ✗ — 종족·방향·강화 발광을 FX 에 다시 넘겨야 해 연결부가 늘어남 |

**사망 연출 0.5s 동안의 불변식(게임플레이 영향 0 보장 — test-engineer 검증 대상)**:
1. `DespawnOnDeath.MonsterDied` 발행 시각 = **사망 순간**(현행과 동일). 0.5s 지연에 끌려가지 않는다(피의 갈증 카드 회복 타이밍 불변).
2. 사망한 몬스터는 영웅 타겟 선정·피격·다른 캐릭터 이동 차단 대상에서 **사망 순간** 제외.
3. 사망 순간 HP 바 숨김. 데미지 팝업·임팩트는 현행대로 사망 타격까지만.
4. 풀 재사용(Pop) 시 Idle 상태·flipX 기본(오른쪽)·등장 트윈으로 초기화.

### 5.6 등장 — 트윈 (클립 없음, 확정)

- 풀 Pop 시 `Visual2D` 에 **0.3s** 동안 알파 0 → 1, 스케일 0.6 → 1.0(ease-out cubic `1 − (1 − t)³`).
- 몬스터는 스폰 게이트가 없어 태어나자마자 이동하므로 등장을 이동과 겹쳐 재생할 수 있는 트윈으로 둔다(클립으로 두면 등장 중 이동 모션이 가려짐). 원화 제작량 0.
- 스케일 트윈 대상은 `Visual2D`(루트 아님) — 루트 스케일은 `AttackJuice` 펀치가 쓰므로 충돌하지 않게 분리.

### 5.7 Animator 상태 머신 (`Monster2D.controller`, 6종 공용 + 종족별 Override)

| 파라미터 | 타입 | 사용 |
|---|---|---|
| `Speed` | Float | Idle ↔ Move |
| `Attack` | Trigger | → Attack |
| `AttackVariant` | Int | 선언만(미사용) |
| `Hit` | Trigger | → Hit |
| `Dead` | Bool | → Death |
| `Spawn` | Trigger | 선언만(전이 없음 — 등장은 트윈) |

| 전이 | 조건 | Has Exit Time | Duration | 비고 |
|---|---|---|---|---|
| Any State → Death | `Dead == true` | off | 0 | 우선순위 1, Can Transition To Self off |
| Any State → Attack | `Attack`, `Dead == false` | off | 0 | 우선순위 2, **Can Transition To Self on**(§5.3 재시작) |
| Any State → Hit | `Hit`, `Dead == false` | off | 0 | 우선순위 3, Can Transition To Self off |
| Idle → Move | `Speed > 0.5` | off | 0 | |
| Move → Idle | `Speed < 0.5` | off | 0 | |
| Attack → Idle | — | on (1.0) | 0 | 복귀 후 Speed 로 Move 재진입 |
| Hit → Idle | — | on (1.0) | 0 | |
| Death | 출구 없음 | — | — | 마지막 프레임 유지 |

- 모든 전이 Duration 0(스프라이트 교체는 블렌드 불가). 클립 샘플레이트 = 해당 상태 FPS(§5.2). Loop Time: Idle·Move on, 나머지 off.
- Animator **Keep Animator State On Disable = off** — 풀 재사용 시 기본 상태(Idle)에서 시작.
- 종족별 `<EMonster>_2D.overrideController` 가 5개 클립만 교체.

### 5.8 좌우 방향

- 회전 시스템(`SimpleRotator` yaw 540°/s)은 **그대로 둔다**. `Visual2D` 는 카메라 회전을 복사하므로 루트 yaw 가 스프라이트를 돌리지 않는다.
- 시각 방향 = 루트 전방 벡터의 월드 X 성분(카메라 yaw 0 → 월드 +X = 화면 오른쪽): `forward.x > 0.1` → 오른쪽(flipX off), `forward.x < −0.1` → 왼쪽(flipX on), 그 사이(화면 세로 이동)는 **직전 방향 유지**(데드존 0.1 — 세로로 수렴할 때 좌우 깜빡임 방지).
- flipX 는 발광 마스크(보조 텍스처)에도 동일 UV 로 적용된다.

---

## 6. 기존 연출 유지 매핑

| 연출 | 현행 규칙 (불변) | 2D 에서의 결과 | 변경점 |
|---|---|---|---|
| 피격 플래시 | 색 **반전** 0.1s | 스프라이트 RGB → 1 − RGB(알파 유지) 0.1s | 곱셈 틴트로는 반전이 안 됨(흰 틴트 반전 = 검정 실루엣) → 셰이더 `_FlashInvert` 로 구현(§6.2) |
| 공격 번쩍 | 흰색 쪽 0.6 lerp, 0.12s sin 반원, 피격 플래시 우선 | 스프라이트 색 → 흰색 lerp 최대 0.6 | 곱셈 틴트로는 흰색보다 밝아질 수 없음 → 셰이더 `_FlashWhite` |
| 스케일 펀치 | 루트 ×1.15, 0.12s | 동일(루트) | 없음 |
| 강화 발광 | `_EMISSION` + `_EmissionColor = SpeciesGlowColor × [1.5, 1.9, 2.3, 2.7, 3.2]`, Lv0 off | 발광 마스크 부위만 HDR 가산 → 블룸 | `MonsterEnhancementVisual` 속성 계약 유지, `_renderers` 를 SpriteRenderer 로 재배선 |
| 레벨 배지 "Lv N" | UI 셀 전용(도감·상점·상태 셀), 전장 월드엔 없음 | 동일 | 없음(UI 아이콘 교체는 §10 후속) |
| 데미지 팝업 | 콜라이더 상단 · 1.2u 부상 · 0.7s · 몬스터 숫자색 SpeciesColor · 명도 분기 외곽선 | 동일 | 없음 — 몸색 = SpeciesColor 가 되어 숫자색과 몸색이 일치 |
| 피격 임팩트 | `MonsterHitImpact` / `HitImpact` | 동일 | 없음 |
| HP 바 | 월드 캔버스 빌보드, 텍스트 숨김 | 동일 | 래퍼 높이를 §3.3 "HP바 하단 목표"로 종족별 재조정 |
| 바닥 원판 Aura | 종족색 원판, 플래시 제외 | 동일 — 그림자·접지 역할 | 없음 |
| Plague 둔화 | `PlagueSlowOnHit` | 동일 | 없음 |

### 6.1 강화 발광 방식 — 발광 마스크 (확정)

| 안 | 내용 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| **A. 발광 마스크 보조 텍스처** | 종족별 발광 부위만 흰색으로 칠한 마스크 시트(원화와 동일 레이아웃) × `_EmissionColor` | 원화 색·형태 보존, 부위가 종족 개성(눈·불꽃·낫날), `MonsterEnhancementVisual` 계약 그대로 | 마스크 시트 6장 추가 제작(원화에서 부위 선택 → 1채널, 내부 제작 가능) | ✅ 채택 |
| B. 전신 가산 | 마스크 없이 스프라이트 전체에 발광 가산 | 추가 아트 0 | Lv5(×3.2)에서 몸 전체가 흰 덩어리 → 형태 소실(도감 틴트 t=1.0 기각 사유와 동일) | ✗ |
| C. 몸 뒤 라디얼 글로우 | 기존 `UISoftGlow.png` 를 몸 뒤에 종족색으로 | 추가 아트 0, 도감 표현과 유사 | 스웜 밀집 시 원판이 겹쳐 노이즈, 발광이 "몸이 빛남"이 아니라 "뒤가 빛남" | ✗ |

**발광 마스크 부위**:

| 종족 | 마스크 부위 |
|---|---|
| Wisp | 불꽃 심지(정수리 안쪽 불꽃) |
| Wraith | 눈 2 · 가슴 룬 |
| Reaper | 눈 2 · 낫 날 안쪽 가장자리 1px |
| Hex | 모자 보석 · 떠 있는 구슬 · 시전 고리 |
| Plague | 기포 3 · 독 방울(드립·발사) |
| Phantom | 눈 2 · 날개 끝 |

- **마스크 면적 규칙**: 각 프레임에서 마스크 픽셀 = 불투명 픽셀의 8~20%. 20% 초과 시 Lv5 에서 형태가 번지고, 8% 미만이면 Lv1 발광이 점으로만 보인다.
- **가시성 게이트**: `monster-species-enhancement.md` §4.1 규칙 그대로 — 실제 씬 블룸에서 6종 각각 Lv1(1.5)이 육안 발광하는지 확인, 한 종이라도 미달이면 `_emissionByLevel` 하한을 6종 공통으로 올린다(곡선은 6종 동일 유지, 보스 최종 3.2 이하).

### 6.2 스프라이트 머티리얼 속성 계약

신규 셰이더 1개(`Monster2DSprite`, URP Shader Graph, Unlit) + 공용 머티리얼 1개. 몬스터 6종 공용.

| 속성 | 타입 | 기본값 | 용도 |
|---|---|---|---|
| `_MainTex` | Texture2D | 스프라이트 | 원화 |
| `_EmissionMask` | Texture2D (스프라이트 보조 텍스처명) | 검정 | 발광 부위 |
| `_EmissionColor` | Color (HDR) | 검정 | 강화 발광색 × 세기 — `MonsterEnhancementVisual` 이 기록 |
| 키워드 `_EMISSION` | Keyword | off | 발광 on/off — `MonsterEnhancementVisual` 이 토글 |
| `_FlashWhite` | Float 0~1 | 0 | 공격 번쩍: `rgb = lerp(rgb, 1, _FlashWhite)` |
| `_FlashInvert` | Float 0~1 | 0 | 피격 반전: `rgb = lerp(rgb, 1 − rgb, _FlashInvert)` |

- 최종 색 = `flash(원화 × 정점색) + _EmissionColor × 마스크 × 알파`. 렌더: Transparent, ZWrite Off, Cull Off, 카메라 거리 역순 정렬(URP 투명 기본). 조명 영향 없음(Unlit) — 3D 조명 변화에도 원화 색 유지.
- `HitFlash`·`AttackJuice` 가 스프라이트 렌더러에서 위 두 Float 로 같은 결과(반전 0.1s / 흰색 최대 0.6 · 0.12s sin)를 내게 하는 방식은 gameplay-programmer 판단.

---

## 7. 에셋 규격

### 7.1 파일·폴더·명명

| 에셋 | 경로 | 파일명 | 수량 |
|---|---|---|---|
| 스프라이트 시트 | `Assets/_Lair/Art/Sprites/Monsters2D/` | `<EMonster>_Sheet.png` (예: `Wisp_Sheet.png`) | 6 |
| 발광 마스크 시트 | `Assets/_Lair/Art/Sprites/Monsters2D/` | `<EMonster>_Sheet_Emission.png` — 시트의 보조 텍스처 `_EmissionMask` 로 등록 | 6 |
| 애니메이션 클립 | `Assets/_Lair/Art/Animations/Monsters2D/` | `<EMonster>_<State>.anim`, State ∈ `Idle`/`Move`/`Attack`/`Hit`/`Death` | 30 |
| 공용 컨트롤러 | `Assets/_Lair/Art/Animations/` | `Monster2D.controller` | 1 |
| 종족 오버라이드 | `Assets/_Lair/Art/Animations/Monsters2D/` | `<EMonster>_2D.overrideController` | 6 |
| 셰이더 | `Assets/_Lair/Art/Shaders/` (신규 폴더) | `Monster2DSprite.shadergraph` | 1 |
| 머티리얼 | `Assets/_Lair/Art/Materials/` | `Mat_Monster2D.mat` | 1 |
| 몬스터 프리팹 | `Assets/_Lair/Art/Characters/` | 기존 `<EMonster>.prefab` **수정**(파일명·GUID·Addressable 주소 유지) | 6 |

- **Rule 03 §2**: Addressable 로 Enum 키 로드되는 에셋은 몬스터 프리팹뿐이며 이름(`Wisp.prefab` ↔ `EMonster.Wisp`)과 주소를 그대로 둔다. 시트·클립·컨트롤러는 프리팹이 직접 참조하므로 Addressable 등록 대상이 아니다. 시트 파일명에 `_Sheet` 접미사를 붙여 프리팹 주소 `Wisp`·도감 아이콘 `MonsterIcons/Wisp.png` 와 이름이 겹치지 않게 한다.
- **Rule 04 §2**: 이미지 → `Sprites/`, 클립·컨트롤러 → `Animations/`(기존 `Knight.controller` 와 같은 폴더), 머티리얼 → `Materials/`, 셰이더는 새 에셋 타입이라 `Art/Shaders/` 신설.

### 7.2 시트 레이아웃

- **6열 × 5행 고정**, 셀 N×N(§3.3), 여백·간격 0. 행 순서: 0 대기 · 1 이동 · 2 공격 · 3 피격 · 4 사망. 열 = 프레임 인덱스(F0~F5). 프레임이 6개 미만인 행의 남는 칸은 투명.
- 발광 마스크 시트는 **같은 크기·같은 칸 배치**, 발광 부위 = 흰색(#FFFFFF), 나머지 = 검정(#000000).

### 7.3 텍스처 임포트 설정 (시트·마스크 공통)

| 항목 | 값 |
|---|---|
| Texture Type / Sprite Mode | Sprite (2D and UI) / Multiple |
| Pixels Per Unit | **48** |
| Filter Mode | Point (no filter) |
| Compression | None |
| Generate Mip Maps | off |
| sRGB | 시트 on / 마스크 off |
| Alpha Is Transparency | 시트 on |
| Max Size | 1024 (최대 시트 528×440) |
| 슬라이스 | Grid By Cell Size N×N, Offset 0, Padding 0, **Keep Empty Rects on**(인덱스 = 행 × 6 + 열 고정), Pivot Bottom Center |
| 보조 텍스처 | 시트 → Secondary Textures 에 `_EmissionMask` = `<EMonster>_Sheet_Emission` |

### 7.4 프리팹 구성 변경 (6종 공통)

- 제거: LittleGhost 중첩 프리팹 인스턴스(3D 모델 + `Ghost_Kid.controller` Animator).
- 추가: 자식 `Visual2D` — SpriteRenderer(`Mat_Monster2D`) + Animator(`<EMonster>_2D.overrideController`). 이름은 `Aura`/`HpBar` 접두로 시작하지 않는다(플래시 제외 규칙에 걸리지 않게).
- 유지: 콜라이더·Rigidbody·AI·공격·체력·`Aura`·`HpBarWrapper`·루트 스케일(판정 불변).

### 7.5 3D 잔존 에셋

`LittleGhost_{N1,N2,V1,V2,H1,M1}.prefab` · `LittleGhost_LP.fbx` · `Ghost_Kid.controller` 의 GUID 참조처 = 몬스터 6프리팹 + LittleGhost 계열 자신뿐(2026-09-28 검색). 이번 구현에서는 **삭제하지 않는다** — 2D 전환 육안 승인 후 별도 정리 커밋(§10-4)에서 관련 텍스처와 함께 삭제한다(롤백 여지 유지).

---

## 8. 아트 소스 — **사용자 선택 필요**

규격(§3·§5·§7)은 소스와 무관하게 고정이다. 파일명·레이아웃이 같으면 소스를 나중에 바꿔도 코드·프리팹 변경이 없다.

| 선택지 | 내용 | 장점 | 단점 |
|---|---|---|---|
| (a) 외부 제작(외주) | §4 컨셉·§5 프레임표·§7 규격을 발주서로 사용 | 규격·컨셉 100% 일치, 마스크 동시 납품 | 견적·기간 필요 |
| (b) 에셋 구매 | 픽셀 몬스터 팩 구매 후 규격에 맞춤 | 즉시 확보, 저비용 | 6종 컨셉·셀 크기·프레임 수 불일치 가능 → 재채색·리사이즈·마스크 내부 제작 필요, 종족색 정렬(§1.3) 작업 발생 |
| (c) 임시 플레이스홀더 | 시안(`.mockups/monster-2d-conversion.html`)의 **"시트 PNG 저장"** 으로 규격 그대로의 절차 생성 시트·마스크 12장을 내보내 임포트 | 비용 0, 규격 100% 일치 → 파이프라인(애니메이터·셰이더·발광·연출)을 먼저 검증 | 품질은 절차 생성 픽셀 수준 — 출시 품질 아님 |

권장 진행 순서: (c)로 파이프라인을 먼저 검증한 뒤 최종 아트를 (a)·(b) 중 사용자가 고른다. 최종 소스 결정은 사용자 몫이다.

---

## 9. 검증 게이트

| 구분 | 항목 | 통과 기준 | 담당 |
|---|---|---|---|
| 불변식 | 몬스터 데미지 적용 시점 | 공격 트리거 = 데미지 적용 같은 프레임(현행 즉시 경로) | test-engineer |
| 불변식 | `MonsterDied` 발행 시각 | 사망 순간(지연 0) | test-engineer |
| 불변식 | 사망 연출 중 대상 제외 | 사망 순간부터 타겟 선정·피격·이동 차단 0건 | test-engineer |
| 불변식 | 풀 재사용 초기화 | Pop 시 Idle · flipX off · 등장 트윈 재생 · 발광 = 현재 강화 Lv | test-engineer |
| 불변식 | Animator 파라미터 | 6종 재생 중 "Parameter does not exist" 경고 0건 | test-engineer |
| 불변식 | 강화 발광 | Lv0 `_EMISSION` off, Lv1~5 `_EmissionColor` = SpeciesGlowColor × [1.5, 1.9, 2.3, 2.7, 3.2] | test-engineer |
| 육안 | 실루엣 식별 | 실제 화면 스케일, 흑백 실루엣만으로 6종 구분 | 사용자 |
| 육안 | 발광 가시성 | 6종 각각 Lv1 육안 발광(§6.1) | 사용자 |
| 육안 | 스웜 밀집 | Phantom 다수 밀집 시 림으로 개체 식별 | 사용자 |
| 성능 | 드로우 부하 | 같은 씬·같은 몬스터 수에서 2D 전환 후 SetPass·Draw Call ≤ 3D 현행(Profiler) | gameplay-programmer |

- **qa-simulator 불필요** — 전투 판정·수치·타이밍 불변(§5.3 A안, §5.5 불변식). 불변식 테스트가 하나라도 실패하면 본 판단을 재검토한다.

---

## 10. 후속 분할 제안 (이번 범위 밖)

1. **UI 아이콘 2D 교체** — 도감·상점·인게임 상태 셀이 쓰는 `MonsterIcons/{6종}.png` 를 2D 대기 F0 기반으로 재생성(같은 경로 덮어쓰기 → GUID 유지). 최종 아트(§8) 확정 후 진행.
2. **영웅 2D 전환 여부** — 이번엔 몬스터만 2D, 영웅은 3D Knight 유지 → 한 화면에 3D·2D 가 섞이는 톤 리스크. 몬스터 2D 육안 승인 뒤 사용자가 결정.
3. **Hex 원거리 공격 가독성** — 사거리 5 즉시 판정이라 발사체가 없다. 시전→영웅 사이를 잇는 트레이서 FX 는 새 `EVisual` 신설이 필요한 별도 기획.
4. **3D 잔존 에셋 정리** — §7.5 목록 삭제.

---

## 11. 구현 요청사항 (gameplay-programmer 용)

### Enum (Rule 02 §8 — `CommonEnum.cs`)
- **신규 없음.** `EMonster` 6값·순서·에셋 키 그대로.

### Interface (Rule 02 §9)
- **신규 없음.** 기존 `IAnimatorSink`(Speed/Attack/AttackVariant/Hit/Dead/Spawn) 계약을 몬스터 컨트롤러가 그대로 받는다.

### 에셋 키 (Rule 03 §2)
- Addressable 키 변경 없음: `Wisp` / `Wraith` / `Reaper` / `Hex` / `Plague` / `Phantom` 프리팹.
- 신규 비-Addressable 에셋은 §7.1 표의 경로·파일명 그대로.

### 데이터 스키마
- **신규 JSON row 스키마 없음**(데이터 구조 확인 게이트 대상 아님). 아래 값은 프리팹 직렬화 필드·에셋 설정(정적 비주얼 배선).

### 직렬화 필드 / 수치

| 대상 | 필드 | 값 |
|---|---|---|
| `DespawnOnDeath` (6종) | `_delay` | **0.5** |
| `CharacterAnimationDriver` (6종 신규 부착) | `_animator` / `_walkSpeed` / `_runSpeed` / `_hitReactionCooldown` / `_attackSuppressWindow` | `Visual2D` Animator / 1 / 2 / 0.4 / 0.5 |
| `MonsterEnhancementVisual` (6종) | `_renderers` / `_emissionByLevel` | `[Visual2D SpriteRenderer]` / `[1.5, 1.9, 2.3, 2.7, 3.2]` 유지 |
| `HitFlash` (6종) | `_duration` | 0.1 유지 |
| `AttackJuice` (6종) | `_punchScale` / `_punchDuration` / `_flashWhiteLerp` | 1.15 / 0.12 / 0.6 유지 |
| `Visual2D` localScale (월드 스케일 1) | 1 ÷ 루트 스케일 | Wisp 1.6667 · Wraith 0.7692 · Reaper 1.1111 · Hex 1.25 · Plague 2.0 · Phantom 2.5 |
| `HpBarWrapper` (6종) | HP 바 **하단** 월드 높이 | Wisp 0.747 · Wraith 1.434 · Reaper 1.038 · Hex 0.934 · Plague 0.643 · Phantom 0.538 (§3.3) |
| 등장 트윈 | 길이 / 스케일 / 알파 / 이징 | 0.3s / 0.6 → 1.0 / 0 → 1 / ease-out cubic (§5.6) |
| 좌우 반전 | 데드존 | `|forward.x| ≤ 0.1` 이면 직전 방향 유지 (§5.8) |
| 빌보드 | 회전 | `Visual2D` 월드 회전 = 메인 카메라 회전 (HP 바와 동일 방식) |

### 동작 요구 (구현 방식은 gameplay-programmer 판단)
1. 몬스터 공격은 즉시 판정 경로 유지, `CharacterAttackStrikeRelay` 미부착, 몬스터 클립에 애니메이션 이벤트 없음(§5.3).
2. 사망 연출 0.5s 동안 §5.5 불변식 1~4 충족.
3. 스프라이트 렌더러에서 피격 = RGB 반전 0.1s, 공격 = 흰색 쪽 최대 0.6 lerp · 0.12s sin 반원, 피격 우선(§6, §6.2 속성 계약).
4. Animator 상태 머신 §5.7 그대로.

### 테스트 포인트 (test-engineer 참고)
- §9 "불변식" 6항목.

---

## 12. Self-Review

- **Placeholder 잔존 0**: 미정 마커·애매한 권유·두 갈래 위임·본문 비움 참조·검산 누락 점검 완료. 아트 소스만 "사용자 선택 필요"로 명시(§8, 요청 사항).
- **내부 일관성**: 프레임표(§5.2) 합계 150 = 26+26+25+26+26+21 검산. 사망 길이 6÷12 = 0.5s = `_delay 0.5`(§5.5·§11). HP바 목표 = 최상단 + 0.08(§3.3·§11 동일 값). PPU 48(§3.2·§7.3). 히트 프레임 F1(§5.2·§5.3). 발광 곡선 [1.5…3.2](§1.2·§6·§9·§11).
- **명명 일관성**: `Visual2D`, `Monster2D.controller`, `<EMonster>_2D.overrideController`, `<EMonster>_Sheet.png`, `<EMonster>_Sheet_Emission.png`, `_EmissionMask`, `_FlashWhite`, `_FlashInvert`, `Monster2DSprite.shadergraph`, `Mat_Monster2D.mat` — 문서 전체 동일 표기.
- **스코프**: 몬스터 비주얼 교체 단일 단위. 아이콘·영웅·Hex 트레이서·3D 정리는 §10 분할.
- **UI 목업**: `.mockups/monster-2d-conversion.html` — 6종 픽셀 스프라이트를 §3.3 셀 규격·§4 팔레트·§5.2 프레임표 그대로 절차 생성해 재생. 발광은 블룸 근사(SVG 블러), 영웅은 회색 자리표시.
