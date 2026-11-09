# 영웅 25종 전신 애니메이션 시트 적용 (Hero Sheets) - 기능 기획서

> 작성: game-designer · 2026-11-09 · 단계: v0.3 · 승격: **불필요**(CLAUDE.md §8 "아트/에셋 작업 허용" + "챕터 구조 허용"의 챕터별 영웅 아트 범위. 신규 영웅 직업·신규 몬스터·신규 UI 화면 0, 전투 수치·타이밍 변화 0)
> 입력: `C:\Users\Tae\Desktop\Art\Hero\v2\{Knight,Archer,Assassin,Berserker,Paladin}_S{1..5}_ver2.png` 25장(각 1254x1254 투명 PNG, 6열 x 6행, 셀 209, 행 순서 Idle / Attack / Death / Spawn / Move / Hit, 행당 6프레임) · `hero-2d-conversion.md`(§5.3 라이브 타이밍·§5.3.1 락/게이트·§6.4 오버레이·§7 에셋 규격) · `monster-tier-sheets.md`·`monster-sheet-v4.md`(선례) · `HeroStageVariantConfig`·`HeroStageVariantApplier`·`MonsterTierOverlay`·`AnimatorSink`·`HeroAttackGate`·`HeroEntryDriver`·`CharacterAttackStrikeRelay`·`Heroes2D/*_SheetSpec.json`
> **사용자용 가이드**: `docs/design/hero-sheets.html` · **UI 목업**: `.mockups/hero-sheets.html`(메인이 로컬 서버로 제공, 영웅 원본 이미지는 서버의 읽기 전용 경로 `/herosrc/` 로 서빙)
> **대체 관계**: `hero-2d-conversion.md` §5.2·§5.3(프레임 수·fps)·§7.1~7.3(72px 도트 규격)·§4(도트 규격)는 본 문서가 **대체**한다. §5.3.1(락·게이트 1.8s 고정)·§6.4(오버레이 합성 구조)·§9 불변식은 **그대로 유효**하다.

---

## § 헤더

- **목표**: 영웅 5직업 x 5단계 = 25종의 외형을 도트 시트(셀 72, 9행 x 20열)에서 새 일러스트 시트(6행 x 6열)로 교체한다. 영웅 전투 동작·타이밍·수치는 그대로이고 **그림만 바뀐다**.
- **검증 가설**: (1) 새 그림이 몬스터 v4 화풍과 맞아 한 화면에서 어색하지 않은가. (2) 한 직업의 5단계가 같은 크기로 서고 단계가 한눈에 구분되는가. (3) 공격 그림이 6프레임뿐이어도 타격 순간(데미지 시각)이 지금과 정확히 같게 유지되는가.
- **현재 단계 범위 적합성**: **범위 안**(§8 "아트/에셋 작업 허용", 챕터별 영웅 아트). 신규 몬스터·신규 규칙·신규 화면 0. 영웅 능력치·스킬·AI·공격 주기 불변. (불변이 깨지는 항목이 생기면 즉시 밸런스 조정 흐름 - qa-simulator - 로 전환, §12.2 T-H 게이트)
- **핵심 메커니즘**: 현행 구조(1단계 = 몸 스프라이트, 2~5단계 = 몸을 끄고 오버레이 렌더러가 같은 칸 번호의 완전 합성 원화를 표시)를 **그대로 쓰고 시트 이미지·슬라이스·클립 내용만 교체**한다. 컨트롤러·상태·이벤트 이름·코드 변경은 없다(§6).

---

## 0. 결정

### 0.1 확정 입력

| # | 항목 | 값 |
|---|---|---|
| D1 | 소스 | 25장, 1254x1254, 6x6, 셀 209, 행 Idle / Attack / Death / Spawn / Move / Hit, 모두 **오른쪽을 본다**(flipX 규칙 불변) |
| D2 | 직업 ↔ 챕터 | 1 Knight · 2 Archer · 3 Assassin · 4 Berserker · 5 Paladin. 단계 S1~S5 = 스테이지 x-1~x-5 (`HeroStageVariant.Tier` 0~4 와 1:1, 불변) |
| D3 | 불변식 | strike 0.5/0.8/0.3/0.2 초 · 공격 락 1.8 초 · 스폰 게이트 1.8 초 · 루트 스케일 곱(콜라이더 포함) · 이벤트 이름 `OnAttackStrike` · 컨트롤러 파라미터(Speed/Attack/AttackVariant/Hit/Dead/Spawn) |

### 0.2 설계 결정 (모두 근거 있는 추천안. 사용자 확인이 필요한 H1·H2·H7·H9(배수)·H8·H6·H11·E1·H10 은 HTML "결정해 주세요" 1~9 번에 같은 순서로 제시)

| # | 항목 | 확정안 | 대안 | 근거 |
|---|---|---|---|---|
| H1 | 셀 규격 | **192** (209 -> 192, x0.9187), 6열 x 6행 = **1152 x 1152** | 209 그대로(1254x1254) | 몬스터 v4 와 같은 셀·같은 변환 도구 재사용. 압축 메모리 시트당 0.75MB vs 0.93MB(+24%) |
| H2 | Spawn 행 | **유지**(6행) | 삭제(5행, 1152x960) | 영웅에는 `Spawn` 트리거·스폰 게이트 1.8s 가 있고 새 Spawn 행("땅에서 솟아오름")이 입장 연출에 맞다. 삭제하면 1.8초 동안 대기 자세로 서 있게 되어 현행(등장 모션 1.33s)보다 퇴보. 메모리 차이는 시트당 0.125MB(-16.7%), 직업당 0.625MB |
| H3 | 행 순서(게임 포맷) | **Idle 0~5 · Move 6~11 · Attack 12~17 · Hit 18~23 · Death 24~29 · Spawn 30~35**, 인덱스 = 행 x 6 + 열 | 소스 순서 유지 | 몬스터 v4 의 6행 포맷과 같다(추출 도구 재사용). 소스 -> 게임 행 매핑은 §3.2 |
| H4 | 시트 구조 | 현행 유지: S1 = 몸 렌더러, S2~S5 = 오버레이(`_tier1Frames`~`_tier4Frames`)가 같은 인덱스를 표시. 영웅 경로 코드 불변 | 몬스터식 온디맨드 세트 SO | §7 로딩 결정(H8)과 묶임 |
| H5 | 전투 타이밍 | **불변**. 공격 6프레임을 직업별 키프레임 시각표로 고정해 타격 프레임이 현행 strike 시각에 떨어진다(§5) | fps 균일 | 균일 fps 면 타격 시각이 직업마다 달라진다 |
| H6 | 공격 모션 | 소스는 공격 행이 **하나**다. 베기1·베기2·찌르기(Knight) 모양 구분은 사라지고, 세 상태가 **같은 6프레임**을 쓴다(상태 이름·Animator Speed·스윙 효과음 variant 0/1/2 는 유지) | 공격 행 추가 제작 | 아트 추가 없이 타이밍·소리 구분 유지 |
| H7 | 발광 | **제거**: 25 단계 `UseEmission = false`. 새 발광 마스크는 만들지 않고 기존 마스크 25장은 삭제 | 마스크 새로 생성(S3~S5 15장, 몬스터 P7 방식) | 새 그림은 금·보석·빛 장식이 그림 안에 이미 있고(단계 구분이 그림으로 충분), 곱연산 발광(1.5/2.2/3.2)을 금색에 얹으면 과노출 위험이 크다 |
| H8 | 로딩 | **직업 프리팹이 5단계 시트 5장을 직접 참조**(현행과 같은 방식, 신규 로더 없음). 한 번에 올라가는 영웅 시트 = 그 직업 5장 | 단계 시트 25종을 SO 래퍼 + Addressable 25키로 온디맨드 | 마을 쇼케이스 캐러셀이 스테이지를 넘기며 25단계 외형을 즉시 바꾸므로 비동기 로드는 캐러셀을 깨뜨린다. 메모리 3.75MB 로 충분히 작다 |
| H9 | 크기 | 단계마다 **대기 F0 불투명 bbox 높이를 그 직업 S1 의 월드 높이로 정규화**(시트별 PPU). `HeroStageVariant.ScaleMultiplier` 는 **불변**(1.4 / 1.25 / 1.2 / 1.35 / 1.3, 5단계만) | 배수 재조정 | 루트 스케일이 캡슐 콜라이더에 곱해져(반경 0.7·높이 2.52 @1.4) 게임플레이에 영향. 배수를 바꾸면 피격 판정이 변한다. 정규화로 새 그림이 커도 최종 표시 높이는 현행과 같다 |
| H10 | 초상 | `HeroIcons/{Class}[_S2..S5].png` 25장을 새 시트 Idle F0 로 **교체**(같은 파일명 덮어쓰기, GUID 유지) | 유지 | 마을·기록 UI 에 옛 그림이 남으면 영웅이 둘로 보인다 |
| H11 | 대기 정지감 | 이번엔 **그대로**(추가 숨쉬기 모션 없음) | Visual2D 에 호흡 스케일 | YAGNI. 게이트 V-H5 에서 사용자가 보고 판단 |
| H12 | 임포트 | Sprite Multiple, Bilinear, 밉맵 on, 모바일 ASTC 6x6, Max 2048, Pivot Custom = §3.3 | Point(현행) | 새 그림은 도트가 아니다. 몬스터와 같은 설정 |

---

## 1. 소스 분석 (육안 확인 결과)

| 항목 | 관찰 | 처리 |
|---|---|---|
| 프레임 정렬 | 셀마다 캐릭터가 거의 같은 위치. Idle 6프레임이 거의 같은 그림(미세 차이) | 대기 모션 정지감(H11, V-H5). 6프레임 그대로 재생 |
| Attack 행 | 6프레임 = F0 대기 유사 -> F1 윈드업 -> F2 최고점·시위 당김 -> **F3 타격 접촉**(Knight 칼 전방, Archer 시위 놓음, Assassin 단검 찌름, Berserker 도끼 내려침, Paladin 철퇴 휘두름) -> F4 낮은 팔로스루 -> F5 복귀 | **타격 프레임 = F3(행 내 인덱스 3), 5직업 공통** (게임 인덱스 15) |
| Spawn 행 | F0 = 점 몇 개, F1 = 작은 잔해, F2~F5 = 솟아오름 -> 정상 자세 | 사용(H2). F0 불투명 픽셀 0 금지(X-H6) |
| Death 행 | 쓰러짐 -> 마지막 F5 = 작은 파편 | 사용. 마지막 프레임 유지(비반복) |
| 칸 넘침·가장자리 | Berserker Attack F2 도끼가 셀 상단에 닿음, Knight Attack F2 칼날 끝이 셀 가장자리 근처, Paladin 날개·후광이 좌우 폭을 크게 차지 | §4 Q-E. 균일 축소(s_fit)·교차 칸 번짐 귀속 |
| 빨간 테두리 | Knight S5·Paladin S5 외곽에 붉은 반투명 번짐·점 | §4 Q-F |
| 폭 | Paladin S5 는 날개로 폭이 가장 넓다 | 폭 상한(§3.4) |

---

## 2. 에셋 목록

### 2.1 신규·교체 (경로 `Assets/_Lair/Art/Sprites/Heroes2D/`)

| 에셋 | 파일명 | 수량 | 크기 | 비고 |
|---|---|---|---|---|
| 몸 시트 S1 | `{Class}_Sheet.png` | 5 | 1152 x 1152 | 기존 파일명 덮어쓰기(GUID 유지). 프리팹 몸 렌더러·클립이 참조 |
| 오버레이 시트 S2~S5 | `{Class}_Sheet_S2.png` ~ `_S5.png` | 20 | 1152 x 1152 | 기존 파일명 덮어쓰기. 프리팹 `MonsterTierOverlay` 의 `_tier1Frames`~`_tier4Frames` 가 참조 |
| 시트 규격 기록 | `{Class}_SheetSpec.json` | 5 | - | 덮어쓰기. 구조는 §10 |
| 초상 | `Sprites/HeroIcons/{Class}.png`, `{Class}_S2.png` ~ `_S5.png` | 25 | 192 x 192 | Idle F0 의 불투명 bbox 중심 정사각 크롭(가장자리 여백 6% = 11px), 덮어쓰기 |
| **이미지 합계** | | **50**(시트 25 + 초상 25) | | |

- 스프라이트 이름: S1 `{Class}_Sheet_{0~35}`, S2~S5 `{Class}_Sheet_S{n}_{0~35}`. 인덱스 = 행 x 6 + 열. `MonsterTierOverlay.ParseFrameIndex`(마지막 `_` 뒤 정수) 가 몸·오버레이 같은 칸을 자동 매칭하므로 **S1~S5 를 같은 슬라이스 설정으로 만든다**.
- 소스 -> 게임 파일 매핑: `{Class}_S1_ver2.png` -> `{Class}_Sheet.png`, `{Class}_S{n}_ver2.png` -> `{Class}_Sheet_S{n}.png` (n = 2~5).

### 2.2 클립 (경로 `Assets/_Lair/Art/Animations/Heroes2D/`) - **이름·GUID 유지, 내용만 재작성**

5직업 x 9클립 = 45개를 같은 파일명으로 덮어쓴다(`Idle · Walk · Run · Slash01 · Slash02 · Stab · Hit · Death · Spawn`). 컨트롤러 5개(`{Class}_2D.controller`)는 **수정 없음**. 시각표는 §5.

### 2.3 삭제 (신규 에셋 확인 후 삭제)

| 대상 | 수량 | 사유 |
|---|---|---|
| `Heroes2D/{Class}_Sheet_Emission.png` · `{Class}_Sheet_S{2..5}_Emission.png` | 25 | H7 발광 제거. 시트의 Secondary Textures(`_EmissionMask`) 연결도 함께 제거 |
| 기존 도트 시트 25장(72px) | - | 같은 파일명에 신규 이미지로 덮어쓰므로 별도 삭제 없음 |
| `Heroes2D/*_SheetSpec.json` 의 도트 규격 필드 | 5 | §10 새 구조로 교체 |

- 유지: `HeroGroundShadow.png`(그림자 스프라이트, 24x8), `Knight.controller`·`KnightWalkLoop.controller`·3D 잔존 에셋(`hero-2d-conversion.md` §7.5 방침 유지, 이번 범위 밖).
- `.mockups/server.js` 의 `EXPORT_NAMES` 중 영웅 시트·마스크 이름은 옛 도트 내보내기 도구용이며 이번에는 쓰지 않는다(정리는 범위 밖).

---

## 3. 변환 파이프라인 (`monster-sheet-v4.md` P0~P10 재사용 + 영웅 차이)

### 3.1 단계

| 단계 | 내용 |
|---|---|
| P0~P1 | 소스 6x6 분할(셀 209). 칸 경계를 넘어 번진 연결성분은 중심이 속한 칸에 귀속(칸 넘침 방지) |
| P2 정리 | 알파 24 미만 제거, 외곽 반투명 잔색을 윤곽선 색으로 치환(Q-F), 고립 성분 제거(Q-J) |
| P3 축소 | 209 -> 192(x0.9187), 시트 전체 동일 비율. `s_fit` 적용 시 추가 균일 축소 |
| P4 흔들림 | Idle·Move 행의 바닥 y 편차 ±2px 이내로 보정(칸 단위 정수 이동) |
| P5 PPU·피벗 | §3.3 산식 |
| P6 출력 | 1152 x 1152, 행 순서 §3.2 |
| P7 | (발광 마스크 생성 없음, H7) |
| P10 | `{Class}_SheetSpec.json` 기록(§10) + 게이트 X-H1~X-H9 출력 |

### 3.2 소스 행 -> 게임 행

| 게임 행(0~5) | 상태 | 소스 행(0~5) | 인덱스 범위 |
|---|---|---|---|
| 0 | Idle | 0 (Idle) | 0~5 |
| 1 | Move | 4 (Move) | 6~11 |
| 2 | Attack | 1 (Attack) | 12~17 |
| 3 | Hit | 5 (Hit) | 18~23 |
| 4 | Death | 2 (Death) | 24~29 |
| 5 | Spawn | 3 (Spawn) | 30~35 |

### 3.3 크기·PPU·피벗 (H9)

기호: `H_class` = 직업 S1 의 목표 월드 높이(u) = 현행 `bodyHeightDots / 31`. Knight 31/31 = **1.0000**, Archer 31/31 = **1.0000**, Assassin 31/31 = **1.0000**, Berserker 33/31 = **1.0645**, Paladin 30/31 = **0.9677**. `hPx` = 시트 Idle F0 의 불투명(알파 ≥ 24) bbox 높이(192 변환·`s_fit` 적용 후, px).

1. **시트별 PPU** = `hPx / H_class` (S1~S5 모두 같은 `H_class` 를 쓴다). 결과: 5단계 모두 대기 F0 의 월드 높이 = `H_class`. 따라서 5단계의 최종 표시 높이 = `H_class x ScaleMultiplier`(현행과 동일).
2. **이동**: 시트 전체를 정수 px 로 옮겨 Idle F0 의 **발 중심**이 셀 좌표 (96, 16) 에 오게 한다. 발 중심 x = bbox 하단 6px 띠의 불투명 픽셀 x 평균, 발 바닥 y = bbox 하단.
3. **피벗(전 슬라이스 공통, 몸·오버레이 동일)**: (0.5, 16 / 192) = **(0.5, 0.0833)**. 현행 피벗(0.4236, 0.2917)도 발 높이 기준이라 `Visual2D` 로컬 오프셋은 유지한다.
4. `s_fit`: 모든 프레임의 불투명 픽셀이 셀 가장자리 8px 이상 안쪽이 되도록 미달 시 시트 전체를 균일 축소(기본 1.00, 최소 0.88). 0.88 미만이면 해당 시트 **재생성 요청**. `s_fit` 는 `hPx` 에 반영되므로 PPU 도 같은 비율로 낮아져 **월드 크기는 변하지 않는다**.
5. 몸 높이 추정(육안 기준, 실측이 우선): S1 Knight 대기 높이 161px 로 눈대중(209 셀 175px x 0.9187, 실측 전 가정값) -> PPU 161 로 가정. **추출 도구가 25장 전부를 실측해 SheetSpec 에 기록**하며 목업(`.mockups/hero-sheets.html`)이 같은 산식을 브라우저에서 실시간 계산해 보여준다.

### 3.4 정합 규칙

| 규칙 | 값 | 위반 시 |
|---|---|---|
| 같은 직업 S1~S5 대기 F0 월드 높이 | 모두 `H_class` (오차 0.01u 이내) | 도구 버그, 재실행 |
| 월드 폭(대기 F0 bbox) | ≤ 그 직업 S1 폭 x 1.60 | 사용자 보고(Paladin S5 날개 유력). 재생성 요청 / 사용자 승인 중 사용자 선택 필요 |
| 모든 프레임 가장자리 여백 | 불투명 픽셀이 가장자리 8px 이상 안쪽 | `s_fit`(0.88 하한), 미달 재생성 |
| 발 접지 | Idle·Move 프레임의 발 바닥 y 편차 ≤ 2px, Death 프레임 바닥 y 편차 ≤ 6px | P4 보정 / 사용자 보고 |

---

## 4. 품질 이슈 처리 기준

| # | 대상 | 관찰된 문제 | 통과 기준 / 처리 |
|---|---|---|---|
| Q-E | Berserker Attack F2 · Knight Attack F2 등 | 도끼·칼이 칸 가장자리에 닿거나 이웃 칸으로 번짐 | P1 귀속으로 이웃 칸 번짐 제거 후, 가장자리 8px 여백 확보를 위해 시트 단위 `s_fit` 적용. 0.88 미만이면 재생성(해당 직업·단계 목록을 사용자에게 보고) |
| Q-F | Knight S5 · Paladin S5 | 외곽 붉은 반투명 번짐·잔점 | 알파 24 컷 후 외곽 1px 띠에서 윤곽선 색(어두운 굵은 외곽)과 색 거리 40 이상인 채도 0.5 이상 붉은 픽셀 -> 윤곽선 색으로 치환. 떨어진 붉은 고립 성분 전부 삭제. 잔여 0 |
| Q-J | 25장 전반 | 프레임 단위 잡티·고립 점 | Idle·Move·Attack·Hit 행에서 면적 12px(192 셀) 이하 고립 성분 0. Death·Spawn 행은 연출용 파편이라 면적 6px 이하만 삭제(최대 성분에서 떨어진 것) |
| Q-W | Paladin S5 | 날개로 폭이 큼 | §3.4 폭 상한. 초과 시 사용자 승인 필요(재생성 / 허용) |
| Q-D | Death 마지막 F5 | 작은 파편만 남음 | 유지(연출). 마지막 프레임 불투명 픽셀 ≥ 6px(바닥 파편이 완전히 비지 않을 것) |
| Q-S | Spawn F0 | 점 몇 개 | 불투명 픽셀 ≥ 6px(완전 투명 프레임 금지) |
| Q-I | Idle | 6프레임이 거의 같은 그림 | 그대로 사용(H11). 인접 프레임 전부 동일이면 정지 영상이므로, Idle 6프레임 중 서로 다른 프레임이 2개 이상일 것(X-H8) |
| Q-M | 수동 | 자동으로 못 지우는 잡티 | **수동 제거 허용**(몬스터 선례). 입력 파일(프레임·사각형·삭제)을 도구가 받는다. 레포 커밋 대상 아님, 적용 건수를 SheetSpec `measured.manualEdits` 에 기록 |

---

## 5. 클립·타이밍 (불변식 - 핵심)

### 5.1 상태 -> 클립 -> 소스 행

| 현행 상태 | 클립 파일 | 새 소스 행 | 프레임 | 재생 | 이벤트 |
|---|---|---|---|---|---|
| Idle | `{Class}_Idle` | Idle | 6 (인덱스 0~5) | 반복, 6fps = 1.0s | 없음 |
| Move(블렌드 트리 두 모션) | `{Class}_Walk` | Move | 6 (6~11) | 반복, 7.5fps = 0.8s | 없음 |
| | `{Class}_Run` | Move | 6 (6~11) | 반복, 10.5fps = 0.5714s | 없음 |
| Slash01 · Slash02 · Stab | `{Class}_Slash01` · `_Slash02` · `_Stab` | Attack | 6 (12~17) | 비반복, §5.2 시각표 | `OnAttackStrike` 1개 |
| Hit | `{Class}_Hit` | Hit | 6 (18~23) | 비반복, 12fps = 0.5s | 없음 |
| Death | `{Class}_Death` | Death | 6 (24~29) | 비반복, 7.5fps = 0.8s | 없음 |
| Spawn | `{Class}_Spawn` | Spawn | 6 (30~35) | 비반복, 4.5fps = 1.3333s | 없음(`OnSpawnAnimEnd` 를 굽지 않음, 게이트는 fallback 1.8s, 불변) |

현행 클립 길이와 같다: Idle 8f@8 = 1.0s, Walk 8f@10 = 0.8s, Run 8f@14 = 0.5714s, Hit 6f@12 = 0.5s, Death 8f@10 = 0.8s, Spawn 16f@12 = 1.3333s.

### 5.2 공격 클립 키프레임 시각표 (타격 프레임 F3 가 현행 strike 시각에 오도록)

규칙: 클립 로컬 초 기준. `T` = 직업·상태별 타격 시각(클립 로컬 초). F0 = 0, F1 = T/3, F2 = 2T/3, **F3 = T(타격 프레임)**, F4 = T + 0.0833, F5 = T + 0.1667, 이후 클립 끝 `L` 까지 F5 유지. `OnAttackStrike` 이벤트 시각 = `T`. Animator 상태 Speed 는 **현행 값 그대로**.

| 클립 | T (로컬 초) | State Speed | 실시간 strike (T / Speed) | L (로컬 초) | 실시간 길이 | F1 | F2 | F3 | F4 | F5 |
|---|---|---|---|---|---|---|---|---|---|---|
| Knight Slash01 | 0.5000 | 1.0000 | **0.500s** | 1.1333 | 1.1333s | 0.1667 | 0.3333 | 0.5000 | 0.5833 | 0.6667 |
| Knight Slash02 | 0.5000 | 1.0000 | **0.500s** | 1.1333 | 1.1333s | 0.1667 | 0.3333 | 0.5000 | 0.5833 | 0.6667 |
| Knight Stab | 0.8163 | 1.0204 | **0.800s** (0.8163 / 1.0204) | 1.6667 | 1.6333s | 0.2721 | 0.5442 | 0.8163 | 0.8996 | 0.9830 |
| Archer 3클립 | 0.3000 | 1.0000 | **0.300s** | 0.6000 | 0.6s | 0.1000 | 0.2000 | 0.3000 | 0.3833 | 0.4667 |
| Assassin · Berserker · Paladin 각 3클립 | 0.2000 | 1.0000 | **0.200s** | 0.6000 | 0.6s | 0.0667 | 0.1333 | 0.2000 | 0.2833 | 0.3667 |

- 현행 클립 길이 L 과 Speed 는 값 그대로다(Knight 17f@15 = 1.1333, Stab 20f@12 = 1.6667 / Speed 1.0204, Archer·Assassin·Berserker·Paladin 9f@15 = 0.6). 클립이 끝난 뒤 락 해제(1.8s)까지 대기 자세로 서 있는 현행 동작도 같다(`hero-2d-conversion.md` §5.3.1 안 A).
- 데이터 단일 진실 = 각 `{Class}_SheetSpec.json` 의 `clips.<state>.keyTimes`·`strikeClipLocalSec`·`animatorSpeed`. 에디터 툴이 이를 읽어 클립을 만든다(숫자 하드코딩 금지).

### 5.3 전투 타이밍 불변 선언

| 항목 | 현행 | 변경 후 |
|---|---|---|
| strike 실시간 | Knight 0.5 / 0.5 / 0.8s · Archer 0.3s · 나머지 0.2s | **동일**(§5.2 실시간 열) |
| 공격 락 | 1.8s (`HeroAttackGate._attackEndFallback`) | **동일**(코드·값 변경 0) |
| 스폰 게이트 | 1.8s (`HeroEntryDriver`/`AutoCombatAI._spawnGateFallback`) | **동일** |
| 스윙음 | variant 0/1/2 순환 -> `Slash01`/`Slash02`/`Stab` | **동일**(세 상태가 같은 그림을 쓸 뿐, `AttackVariant` 파라미터·소리 불변) |
| 루트 스케일·콜라이더 | 5단계 x1.4 / 1.25 / 1.2 / 1.35 / 1.3 | **동일**(H9) |
| 영웅 능력치·스킬·AI | - | **동일** |

위 표 중 하나라도 깨지면 이 기획은 밸런스 변경이 되며 qa-simulator 시뮬을 거친다(게이트 T-H1~T-H4).

---

## 6. 프리팹·코드 영향 범위

| 대상 | 변경 | 비고 |
|---|---|---|
| 영웅 프리팹 5종(`Knight`·`Archer`·`Assassin`·`Berserker`·`Paladin`) | 몸 렌더러의 초기 스프라이트를 새 S1 로 재연결, `MonsterTierOverlay` 의 `_tier1Frames`~`_tier4Frames`(각 36개)를 새 S2~S5 로 재충전, `AuraShadow`·`HeroLight2D` 위치·크기를 새 그림 폭에 맞춰 재정렬 | 일회용 에디터 툴(Rule 04 §3, 실행 후 삭제). 파일명·GUID·Addressable 주소 유지 |
| 컨트롤러 5종 | **변경 없음** | 상태·전이·파라미터·Speed 불변 |
| 클립 45개 | 내용 재작성(§5), 이름·GUID 유지 | `OnAttackStrike` 이벤트 재베이크 |
| `HeroStageVariantConfig.asset` | 값만 변경: 25 엔트리 `UseEmission = false`. `ScaleMultiplier`·`Tier`·`Portrait` 참조 불변(초상은 같은 파일 덮어쓰기라 참조 유지) | 스키마 변경 없음 |
| `HeroStageVariantApplier` | **코드 변경 없음**. 발광 끔 경로(`DisableKeyword`)로 동작 | |
| `MonsterTierOverlay` | **코드 변경 없음**. 현재 몬스터 쪽 프리팹은 Enhance 자식이 없고(`monster-tier-sheets` §6) 영웅이 사실상 유일한 사용처다. 정리·이름 변경은 이번 범위 밖 | 영웅 오버레이 회귀 필수 |
| `CharacterAttackStrikeRelay`·`HeroAttackGate`·`HeroEntryDriver`·`AutoCombatAI`·`AnimatorSink`·`HitFlash`·`AttackJuice` | 변경 없음 | |
| `Mat_*` 영웅 머티리얼 | `_EmissionMask` 보조 텍스처 참조 제거만 | 발광 끔 |
| `HeroHologramMode`(마을 투영진 홀로그램) | 코드 변경 없음, 새 그림에서도 반투명 푸른 톤이 읽히는지 육안 확인(V-H4) | |
| 마을 쇼케이스 캐러셀(`VillageController`) | 코드 변경 없음. 5단계 시트가 프리팹에 상주(H8)라 즉시 전환 유지 | |
| 무한의 탑·데일리 영웅 외형 | 코드 변경 없음. `HeroStageVariantConfig.GetStage(선형 번호)` 선택 로직 불변이라 25단계 시트 어느 것이든 동일 경로로 표시 | |

---

## 7. 로딩·메모리

### 7.1 로딩 (H8)

- 영웅 프리팹이 5단계 시트(몸 S1 + 오버레이 S2~S5)를 직접 참조한다. 영웅 프리팹이 로드될 때 그 직업 5장이 함께 올라오고, 프리팹 해제(현행 수명) 때 함께 내려간다. **신규 Addressable 키·Enum·로더·레지스트리 없음**(Rule 03 §2: 영웅은 기존 `EHero` 키로 프리팹만 로드, 시트는 프리팹 의존).
- 한 판에 한 직업만 쓰므로 전투 중 영웅 시트 = 5장(25장 전체가 아니다). 마을은 쇼케이스 영웅 1직업(5장).
- 온디맨드(단계 시트 25종 SO) 안은 대안으로 기록(§0.2 H8): 절감량이 직업당 3.0MB(5장 -> 1장)이지만 캐러셀 비동기 로드가 필요해 이번엔 채택하지 않는다. 총량 게이트 U-H7 이 초과하면 재검토한다.

### 7.2 메모리 산식 (밉맵 포함 ASTC 6x6, 0.444 byte/px x 4/3)

- 시트 1장 = 1152 x 1152 = 1,327,104px -> x 0.444 = 589,824 byte = **0.5625MB**, 밉맵 포함 x 4/3 = **0.75MB**.
- 영웅 직업당 5장 = **3.75MB**(전투 중 영웅 시트 총량). 25장 전체를 동시에 올리는 경우는 없다.
- 현행(참고, 기본 플랫폼 임포트 설정 Compression None·밉맵 off 기준 환산): 시트 1440 x 648 = 933,120px x 4 byte(RGBA32) = 3.73MB x (시트 5 + 마스크 5) = 37.3MB. 플랫폼 오버라이드가 압축을 켜고 있으면 실제는 이보다 작으므로 U-H7 에서 실측으로 덮어쓴다. 새 방식은 압축·마스크 제거로 직업당 3.75MB, **-90%**.
- 빌드 용량: 새 시트 25장 x 0.5625MB = 14.06MB(밉맵 제외 ASTC).
- 합산: 몬스터 최악 29.1MB(`monster-tier-sheets.md` §7.6) + 영웅 3.75MB = **32.85MB**(몬스터·이펙트 상한 35MB 안).

---

## 8. 초상 (H10)

- `HeroStageVariant.Portrait` 가 소비하는 마을(`HeroSelectCell`·`WorldMapPopup`·`VillageHud`·`DailyPopup`·`RecordsPopup`·`RecordsStageCell`)의 영웅 얼굴 그림 25장을 새 시트 Idle F0 로 교체한다.
- 규격: 192 x 192 정사각, Idle F0 불투명 bbox 중심을 정사각 중심에 두고 가장자리 여백 6%(11px). Single 스프라이트, Bilinear, 밉맵 off(UI), 기존 임포트 설정 유지.
- 같은 파일명 덮어쓰기라 `HeroStageVariantConfig` 참조·UI 프리팹 변경 없음. UI 코드 변경 없음, 틴트(`PortraitTint`) 불변.

---

## 9. 엣지 케이스

| # | 상황 | 처리 |
|---|---|---|
| E1 | 시트 한 장이 §3.4 위반(폭·여백)으로 재생성 대기 | 해당 단계만 옛 도트 시트 유지는 **금지**(화풍 혼재). 25장이 모두 통과할 때까지 교체를 일괄 보류, 직업 단위로 부분 교체는 사용자 승인 시에만 |
| E2 | 풀 재사용(영웅 재스폰) | 오버레이 `OnEnable -> SetTier(0)` 후 Applier 가 재적용(현행 경로) |
| E3 | flipX(왼쪽 이동) | 몸·오버레이 동기(현행). 소스가 오른쪽을 보므로 규칙 불변 |
| E4 | 오버레이 단계에서 몸 렌더러 끔 | 현행(`_body.enabled = Tier == 0`). 이벤트 수신(`CharacterAttackStrikeRelay`)은 몸 Visual2D 의 Animator 가 계속 구동하므로 데미지 중복 없음 |
| E5 | 스테이지 5 루트 배수(x1.4 등) | 불변. 정규화로 표시 높이 = `H_class x 배수` |
| E6 | 스폰 시 첫 프레임이 거의 투명 | 입장 연출. 스폰 게이트 1.8s 는 클립 길이(1.3333s)와 무관하게 유지 |
| E7 | 데일리·탑에서 영웅 직업·단계가 챕터와 다름 | 선택 로직 불변. 25단계 시트가 모든 직업 프리팹에 있으므로 누락 없음 |
| E8 | 마을 홀로그램(조명 안 받는 머티리얼) | 새 그림도 같은 머티리얼로 표시. 발광 제거로 홀로그램 색 충돌 없음 |

---

## 10. 데이터 구조표 (Rule 00 데이터 구조 확인 게이트)

- **게임 JSON 스키마 변경: 없음.** 신규 데이터는 아트 매니페스트(편집기 전용, 레포 커밋은 `{Class}_SheetSpec.json` 5개). `HeroStageVariantConfig`(SO)는 **값만** 바꾼다(스키마 불변).
- 정규화 점검(Rule 02 §11-1): 핵심 엔티티 = "직업 한 개의 시트 규격". 단계(S1~S5)·클립(9)은 그 직업의 **필수 1:N 그룹**이라 부모 파일 안에 배열로 둔다(복합 키 = `stage` + `state`). 선택적 관계 없음, sentinel 값 없음.

```
{Class}_SheetSpec.json (직업당 1, 5개 - 아트 매니페스트)
 |-- doc, class, cell 192, cols 6, rows 6, sheetSize [1152, 1152], indexRule "row*6+col"
 |-- rowOrder ["idle","move","attack","hit","death","spawn"]
 |-- source { fileSuffix "_ver2", cell 209, srcRowOrder ["idle","attack","death","spawn","move","hit"] }
 |-- heightWorld  (= bodyHeightDots / 31, §3.3)
 |-- pivot { x 0.5, y 0.0833 }
 |-- stages[5]  "단계 하나 (필수 1:1 시트)"
 |     |-- stage (1~5), sheet "<Class>_Sheet[_S<n>]", s_fit, offset [dx,dy], ppu, hPx
 |     +-- measured { idleWorldHeight, idleWorldWidth, widthRatioToS1, minEdgeMargin, removedSpecks, redFringePixels, manualEdits }
 +-- clips{9}   "상태 하나"  (키 = idle/walk/run/slash01/slash02/stab/hit/death/spawn)
       |-- frames 6, row (소스 게임 행), loop, animatorSpeed
       |-- keyTimes [6]   (로컬 초, 각 프레임 시작 시각)
       |-- length         (로컬 초, 클립 끝 L)
       +-- strikeFrame 3, strikeClipLocalSec, strikeRealSec, lockReleaseRealSec 1.8   (공격 클립만)
```

| 필드 | 값(예) |
|---|---|
| `clips.slash01.keyTimes` (Knight) | [0, 0.1667, 0.3333, 0.5000, 0.5833, 0.6667] |
| `clips.stab.keyTimes` (Knight) | [0, 0.2721, 0.5442, 0.8163, 0.8996, 0.9830] |
| `clips.stab.animatorSpeed` (Knight) | 1.0204 |

- 삭제된 필드: `cellSize 72`, `cols 20`, `rows 9`, `pixelsPerUnit 31`, 도트 `bodyHeightDots`(값은 `heightWorld` 산식 입력으로만 사용), `tiers`(S1~S5 매핑은 `stages[]` 로 이동), `edgeClipFrames`, `anchors`.

---

## 11. 위험

| # | 위험 | 가능성 | 완화 |
|---|---|---|---|
| R1 | 공격 모양이 하나로 줄어 Knight 의 베기/찌르기 구분 상실 | 확정 | H6. 소리·타이밍은 유지. 사용자 확인(결정 8) |
| R2 | Paladin S5 날개 폭·후광이 크게 보이고 콜라이더 배수(x1.3)와 합쳐 위압적 | 중 | §3.4 폭 상한 + V-H2 비교, 초과 시 재생성 요청 |
| R3 | 칸 넘침 때문에 `s_fit` 가 과도해 몸이 작아 보임 | 중 | 정규화가 월드 높이를 보정하므로 월드 크기는 불변, 화면 해상도 손해만 있음. 0.88 하한 |
| R4 | 발광 제거로 3~5단계의 "점점 강해지는 느낌"이 약해짐 | 중 | 새 그림 자체(왕관·망토·날개·후광)가 단계 구분을 담당. V-H1 에서 5단계 구분 판정, 부족하면 마스크 생성을 후속으로 승격(몬스터 P7 재사용) |
| R5 | 클립 이벤트가 프레임 경계에 정확히 떨어져 샘플링 순서에 따라 1프레임 어긋남 | 낮음 | T-H1 실측 편차 기준(기존 테스트값 불변). 어긋나면 키프레임을 1/60s 앞당기는 보정을 사용자 보고 |
| R6 | 대기 모션 정지감 | 확정(소스 특성) | H11, V-H5 |
| R7 | 옛 테스트가 도트 규격·클립 이름·프레임 수를 직접 단언 | 높음 | §12.3 영향 테스트 목록, test-engineer 갱신 |
| R8 | 한 시트라도 재생성 지연되면 일괄 교체 지연 | 중 | E1, 직업 단위 부분 교체는 사용자 승인 |

---

## 12. 검증 게이트

### 12.1 추출·임포트 게이트

| ID | 항목 | 통과 기준 |
|---|---|---|
| X-H1 | 파일 | 시트 25장 전부 **1152 x 1152**, 초상 25장 192 x 192, 삭제 마스크 25장 잔존 0 |
| X-H2 | 월드 높이 | 직업별 S1~S5 Idle F0 월드 높이 = `H_class`(오차 ≤ 0.01u) |
| X-H3 | 폭 | 월드 폭 ≤ S1 폭 x 1.60 (초과 목록 보고) |
| X-H4 | 가장자리 | 모든 프레임 불투명 픽셀이 가장자리 8px 이상 안쪽, `s_fit` ≥ 0.88 |
| X-H5 | 잡티·붉은 번짐 | §4 Q-F·Q-J 잔여 0 |
| X-H6 | 빈 프레임 | 36프레임 전부 불투명 픽셀 ≥ 6px (Spawn F0·Death F5 포함) |
| X-H7 | 접지 | Idle·Move 발 바닥 y 편차 ≤ 2px, Death ≤ 6px |
| X-H8 | Idle 변화 | Idle 6프레임 중 서로 다른 프레임 ≥ 2 |
| X-H9 | 피벗 | 25장 모두 (0.5, 0.0833), 슬라이스 192x192, 인덱스 0~35, 이름 규칙 §2.1 |

### 12.2 Unity 게이트 (EditMode 단위 + PlayMode 통합)

| ID | 항목 | 통과 기준 |
|---|---|---|
| U-H1 | 슬라이스·참조 | 시트 25장 36 스프라이트, 프리팹 5·클립 45·컨트롤러 5 Missing 0, 이전 인덱스 36 이상 참조 0 |
| U-H2 | 오버레이 동기 | 영웅 5직업 x 단계 2~5 에서 모든 클립 프레임의 오버레이 스프라이트 이름 끝 숫자 = 몸 인덱스, flipX 동기(`MonsterTierOverlaySyncTests` 계열 유지·통과) |
| U-H3 | 클립 키프레임 | 45클립 전부 §5.1·§5.2 키프레임 시각·프레임 인덱스·길이와 일치(`HeroClip2DEventBakeTests` 갱신) |
| U-H4 | 이벤트 | 공격 클립 15개에 `OnAttackStrike` 1개(시각 = T)·그 외 클립 이벤트 0. `OnAttackEnd`·`OnSpawnAnimEnd` 베이크 0 |
| U-H5 | 설정 값 고정 | `_attackEndFallback == 1.8`, `_spawnGateFallback == 1.8`(HeroEntryDriver·AutoCombatAI), 컨트롤러 파라미터 6종 이름·상태 이름 불변, State Speed(Knight Stab 1.0204, 그 외 1.0) |
| U-H6 | 변형 SO | 25 엔트리 `UseEmission == false`, `ScaleMultiplier` 값 불변(0 단계 1.0 ... 5단계 1.4/1.25/1.2/1.35/1.3), `Tier` 0~4, Portrait 25개 null 아님 |
| U-H7 | 메모리 | 영웅 프리팹 로드 시 영웅 텍스처 압축 메모리(밉맵 포함) ≤ 4.5MB(예산 3.75MB + 여유), 몬스터 포함 총량 ≤ 35MB |
| U-H8 | 풀 리셋 | 영웅 재스폰 시 Tier 값대로 재적용, 이전 단계 외형 잔존 0 |
| U-H9 | 홀로그램·쇼케이스 | 마을 캐러셀 25단계 순회에서 NRE·Missing 0 |
| U-H10 | 영웅 회귀 | 영웅 Applier·오버레이 기존 테스트 전부 통과 |

### 12.3 타이밍 게이트 (**불변식, 게이트 T-H**)

| ID | 항목 | 통과 기준 |
|---|---|---|
| T-H1 | strike 발행 시각 | 5직업 x 3공격 상태: BeginAttack 기준 실시간 Knight 0.500 / 0.500 / 0.800s, Archer 0.300s, Assassin·Berserker·Paladin 0.200s. 허용 편차는 기존 테스트(`HeroKnight2DAttackTimingPlayTests`·`HeroAttackTimingProbePlayTests`) 기준을 **그대로** 적용 |
| T-H2 | 공격 락 | `IsAttacking` 해제 1.800s(60fps)/1.833s(30fps) 불변 |
| T-H3 | 스폰 게이트 | 행진 시작 1.8s 불변 |
| T-H4 | 데미지·DPS | 같은 시드·같은 편성에서 영웅 처치 시각·받은 피해 분포가 변경 전과 동일(qa-simulator 헤드리스 소규모 N판, 통계 차이가 아니라 **동일 값**이어야 함) |

영향받는 기존 테스트(갱신 대상, test-engineer): `HeroClip2DEventBakeTests`, `HeroKnight2DAttackTimingPlayTests`, `HeroAttackTimingProbePlayTests`, `Chapter3ArtIntegrityTests`(영웅 시트 규격), `CharacterAnimationController*Tests`(클립 이름 참조 시), `MonsterTierOverlaySyncTests`, `HeroStageVariantApplierTests`·`HeroStageVariantConfigTests`·`HeroStageVariantConfigEdgeTests`(발광 값), `AttackJuiceStage5ScaleRegressionTests`(배수 불변 확인), `FleeStabilizePlayTests`.

### 12.4 육안 게이트

| ID | 항목 | 통과 기준 |
|---|---|---|
| V-H1 | 단계 구분 | 직업별 5단계를 나란히 놓았을 때 단계 1~5 순서가 한눈에 구분됨 |
| V-H2 | 크기 | 같은 직업 5단계가 같은 높이로 서고(5단계는 배수만큼 큼), Paladin S5 날개가 과하지 않음 |
| V-H3 | 선명도 | 실제 화면 x3 확대 캡처에서 번짐·잡티·붉은 테두리 없음 |
| V-H4 | 접지·그림자·홀로그램 | 발이 바닥에 붙어 보이고 `AuraShadow`·`HeroLight2D`·발사체 시작점(Archer)이 새 그림에 맞음, 마을 홀로그램 색 읽힘 |
| V-H5 | 대기·공격 체감 | 대기 정지감이 허용 범위인지(사용자 판정), 공격 타격 프레임에서 무기가 사거리에 닿는 포즈 |

---

## 13. 구현 요청사항 (gameplay-programmer 용)

### Enum / Interface / 에셋 키 / SO
- **Enum**: 신규 없음(`EHero` 불변). **Interface**: 신규 없음. **Addressable 신규 키**: 없음(영웅 프리팹 주소 유지).
- **SO 스키마**: 변경 없음. `HeroStageVariantConfig.asset` 값만 변경(§6).
- **JSON 스키마**: 게임 데이터 변경 없음. 아트 매니페스트 `{Class}_SheetSpec.json`(§10).

### 할 일
1. **추출 도구**(JS, `.mockups/` 인근, 커밋 대상 아님): 소스 25장 -> §3 파이프라인 -> 시트 25 + 초상 25 + SheetSpec 5 출력, 게이트 X-H1~X-H9 출력. 몬스터 v4 추출 도구 재사용.
2. **일회용 에디터 툴**(Rule 04 §3, 실행 후 삭제): 시트 25장 임포트(6x6 슬라이스 192, 공통 피벗, PPU, ASTC·밉맵·Bilinear, Secondary Textures 제거) -> 클립 45개 재작성(키프레임·이벤트·루프) -> 프리팹 5종 재연결(몸 스프라이트·`_tier1~4Frames`·`AuraShadow`·`HeroLight2D`) -> `HeroStageVariantConfig` 25 엔트리 `UseEmission = false` -> 옛 마스크 25장 삭제.
3. 런타임 코드 변경: **없음**(영웅 경로 불변). 단, 도구 실행 중 코드 변경이 필요해지면 기획 변경으로 간주해 보고.
4. test-engineer: U-H1~U-H10, T-H1~T-H3 갱신·실행. qa-simulator: T-H4.
5. 후속 문서 개정(메인): `hero-2d-conversion.md` 상단에 "본 문서 `hero-sheets.md` 로 시트 규격 대체" 안내 추가.
