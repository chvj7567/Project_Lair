# 인게임 상태 셀 강화 레벨 표현 (도감 동일) Design Spec

- **작성일**: 2026-07-26
- **단계**: v0.3 (기존 강화 시각·아이콘 재사용 — 신규 기능 아님, 범위 내)
- **분류**: UI (인게임 하단 상태 셀)
- **문서 성격**: spec — 골격 + 결정 락. 레이아웃·배지 위치·스케일 헤드룸 등은 game-designer 기획서/목업이 확정.

---

## 1. 의도

인게임 하단 6칸 상태 패널의 각 셀(`SpawnerStatusCell`)에 있는 몬스터 아이콘이, 그 종족의 **강화 레벨을 시각적으로 반영**하게 한다 — **도감(작업 L, `CodexCell`)과 동일한 표현**. 전투에서 실제 몬스터가 이미 발광하는데, 상태 셀 아이콘도 같은 색·밝기·배지로 호응해 "내가 키운 종족"이 UI에서도 즉시 읽히게 한다.

## 2. 범위

### 포함
- `SpawnerStatusCell` 중앙 아이콘에 **도감과 동일한 4채널** 적용: 발광 오버레이(레벨별 알파) + 아이콘 틴트(`SpeciesGlowColor` lerp) + 스케일 + "Lv N" 배지. Lv0 = 담백한 원본.
- 도감의 레벨→시각 매핑을 **공유 SoT로 추출**해 도감·상태 셀이 동일 매핑 사용.

### 비포함
- 강화 밸런스·레벨 곡선 변경(작업 I·L 그대로).
- 신규 몬스터/아이콘/리소스 제작(기존 `MonsterIcons`·`SpeciesGlowColor`·소프트글로우 스프라이트 재사용).
- 상태 셀의 다른 요소(진행바·×N·이름·초) 변경.

## 3. 핵심 결정 (락)

1. **4채널 도감 동일** — 발광 오버레이 + 틴트 + 스케일 + 배지. (brainstorming A)
2. **레벨 소스** = 그 종족의 강화 레벨. `MetaProfile.GetShopLevel("Enhance_<EMonster>")` (전투 중 고정값). 셀 전달 배선(스냅샷 필드 vs 바인드 시 조회)은 plan 확정.
3. **시각 매핑 공유 SoT** — 작업 L에서 `CodexCell`에 둔 `IconTintByLevel`·`GlowOverlayAlphaByLevel`·`ScaleByLevel` 배열을 공유 위치로 추출(도감·상태 셀 공유). `SpeciesVisual.SpeciesGlowColor`도 공유. 두 셀의 레벨 표현이 영원히 일치(drift 방지).
4. **Lv0 = 표현 off** — 발광/배지/스케일 항등, 아이콘 원본. 미강화 종은 담백.
5. **풀 재사용 리셋** — 셀 `Bind/RebindSnapshot`이 매번 4채널을 전부 재설정(잔상 방지 — RecordsStageCell 교훈: OnEnable 리셋 대신 바인드 소유).

## 4. 아키텍처

- 현 `SpawnerStatusCell`: `_icon`(SpeciesSprite resolver → MonsterIcons), `_border`(종색), `_speciesText`, `_countText`(×N), `_progressFill`, `_periodText`. 아이콘·이름은 이미 있음.
- 추가: 발광 오버레이 `Image`(아이콘 뒤, UISoftGlow) + "Lv N" 배지 `CHText` + 아이콘 `RectTransform`(스케일 대상). 신규 위젯은 `[SerializeField] private`(§6.1).
- 레벨 시각 적용: 공유 매핑을 읽어 `_icon.color`(틴트)·발광 오버레이 알파(`SpeciesGlowColor`)·아이콘 스케일·배지 텍스트 설정. Lv0이면 오버레이/배지 off·스케일 1·틴트 white.
- 매핑 추출 위치(공유 SoT)와 apply 헬퍼 공유 여부는 plan/gameplay-programmer 결정(최소: 매핑 배열 공유. 옵션: apply 헬퍼도 공유).

## 5. ⚠️ game-designer + 목업이 확정할 것 (작고 빽빽한 셀이라 핵심)

- **발광 오버레이·"Lv N" 배지 배치** — 기존 요소(테두리·이름·×N·진행바·초)와 안 겹치게. 배지는 ×N(노랑 카운트)과 시각적으로 구분.
- **스케일 헤드룸** — 아이콘 확대(만렙 ~1.10)가 이름/진행바/이웃 셀을 침범 안 하게 pivot·여백 (도감에서 pivot (0.5,1) 필요했던 것과 유사). 셀이 작으면 스케일 상한을 도감보다 낮출지 여부.
- **발광 세기/크기** — 작은 셀에서 발광 오버레이 크기·알파가 과하지 않게(도감 값 재사용 vs 셀 크기 맞춤 조정).
- **레벨 소스 배선** — 스냅샷에 레벨을 실을지, 바인드 시 MetaProfile 조회할지(전투 중 불변이라 어느 쪽도 정합).

## 6. 테스트 관점

- 레벨 소스 매핑: 종족별 강화 레벨이 셀 시각(틴트/발광α/스케일/배지)에 정확히 반영(Lv0/중간/만렙, 미강화 종은 off).
- 공유 매핑 SoT: 도감·상태 셀이 같은 배열을 읽는지(회귀 — 도감 값 무변경).
- 풀 재사용: 종족 전이 시 잔상 없음(데이터/계약 수준). 실제 렌더는 프리팹·육안(§Task 목업 게이트).
- 회귀: 진행바·×N·이름·초·클릭 무변경.
