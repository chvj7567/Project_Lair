# 몬스터 도감 UI 아트 세트 (GPT 생성, 도감 전용) - 제작 지시서 (전면 개정)

> **상태: 확정(2026-11-15) - "우선 확정, 세부는 다음에 수정"**. 36장(`Assets/_Lair/Art/Sprites/CodexUI/`)은 제작·반입 완료. S0 시트: `.mockups/assets/codex-s0/S0_popup.png`·`S0_elements.png`, 생성 모델 gpt-6-sol, 키잉 = 마젠타(`#FF00FF`) 크로마키 / 흰색 on 검정 -> 알파. 이 세트는 **공통 UI 전체 교체의 스타일 기준**이다(`docs/design/ui-art-refresh.md`). 후속 개선 목록은 문서 하단 §12.

> 상위 기획서: `docs/design/monster-codex.md` · 승인 레이아웃: `.mockups/monster-codex.html` (1280x720 기준)
> **개정 사유(2026-11-10 사용자 방침)**: 이전 판은 소형 도트 `Px_*` 를 재사용하고 3장만 신규로 뽑는 계획이었다. 새 방침은 "Claude 가 도트로 그린 UI 는 전부 GPT 아트로 바꾼다. 팝업 배경·아이콘·프레임·버튼 전부 GPT 가 뽑는다. **도감만 먼저**"이다. 따라서 도감 팝업에 쓰이는 모든 시각 요소를 **고해상도 GPT 아트 신규 세트**(`Codex*` 접두어)로 만든다.
> 범위 근거: CLAUDE.md §8 "몬스터 도감 개편" 항목 ⑤ (도감 전용 신규 UI 아트 세트). **공통 `Px_*`·`WorldMap*` 등은 건드리지 않고**, `Codex*` 스프라이트는 도감 팝업 안에서만 쓴다(다른 화면 사용 금지).

## 0. 결정 요약

| ID | 결정 | 근거 |
|---|---|---|
| A1 | 도감에 쓰이는 시각 요소 **36장**(§3)을 전부 신규 `Codex*` 스프라이트로 제작. 기존 `Px_*`·`WorldMap*` 참조는 도감 프리팹에서 제거 | 사용자 방침. 소형 도트 조각을 늘려 쓰는 방식 폐기 |
| A2 | 해상도 = **화면 표시 크기(1280x720 기준 px)의 3배**. 스프라이트 PPU **300**(Canvas 기준 PPU 100 -> Sliced 보더가 1/3 로 표시되어 화면 크기와 일치) | 사용자 요구 "2~4배 원본". 3배는 GPT 출력 해상도와 모바일 메모리의 절충(합계 약 1.64Mpx) |
| A3 | 아트 격자 = **원본 3px = 화면 1px**. 외곽선 굵기 화면 2px(원본 6px). 후처리에서 화면 해상도로 한 번 줄여 팔레트 스냅한 뒤 3배 Nearest 확대해 "또렷한 픽셀" 질감을 맞춘다(몬스터 v5 시트와 같은 결) | GPT 출력은 격자가 부정확해 후처리로 통일 필요 |
| A4 | **칸 프레임은 5등급 색별로 직접 그린 5장 + 미해금 1장 = 6장**(회색조 틴트 아님). 해금 펄스·링·스파클만 회색조 틴트 | §4 근거 |
| A5 | 종족 칩은 **중립 칩 2장(비선택/선택) + 회색조 색 띠 1장을 종족색으로 틴트**(6종족 색 변형 6장은 만들지 않음) | §4 근거 |
| A6 | 제작 순서 = **스타일 시트 1장 승인 -> 나머지 일괄**(§6). 생성은 Codex CLI 이미지 생성 사용량 한도 해제(**2026-11-10 오전 3:43 이후**)부터 | 사용자 지시 |
| A7 | 이전 3장 `Px_CodexCellFrame`·`Px_CodexGlow`·`Px_CodexSpark` 은 **새 세트로 대체·폐기**(`CodexCellFramePulse`·`CodexGlow`·`CodexSpark`). GUID 참조 0건 확인 후 삭제 | 이름·크기·임포트가 새 기준과 다름 |
| A8 | 공통 UI 전체 교체(`ui-art-refresh.md`)는 이 세트를 **스타일 기준**으로 삼는다. 그 문서의 "같은 크기 제자리 교체·소형 도트 유지" 전제는 후속 개정 대상(§9) | 사용자 방침 |

## 1. 아트 방향

몬스터 v4/v5 시트(`.mockups/assets/v5/*`, `Monster/v4`)와 같은 세계관의 **클린한 하이브리드 픽셀 아트**.

| 항목 | 기준 |
|---|---|
| 외곽선 | 굵은 near-black `#0a0810`, 화면 2px(원본 6px). 작은 요소는 화면 1px(원본 3px) |
| 음영 | 면마다 **2~3톤**(밝음/기본/어두움), 디더링·부드러운 그라데이션 금지(광원·후광 제외) |
| 베벨 | 위·왼쪽 밝은 선 1화면px, 아래·오른쪽 어두운 선 1화면px |
| 팔레트 | 석판 `#2a3552` / `#1a2132` / `#121827`, 베벨 `#4a5a80` / `#0e1320`, 금 `#f7c64a` / `#c8962a` / `#8a6a2c` + 하이라이트 `#ffe9a8`. 등급색 `#9aa5bd`(풋내기) `#5aa9ff`(견습) `#b07cff`(숙련) `#f7c64a`(정예) `#ff7a45`(군주), 의미색 소울 `#5ef0b4` / 블러드 `#e5484d` |
| 질감 | 석판 균열·리벳은 **프레임 보더 안쪽**에만. 늘어나는 가운데 영역은 평면 |
| 등급 표현 | 풋내기 = 가장 단순한 프레임, 군주 = 가장 장식적인 프레임(모서리 장식·금속 보강 점증) |
| 해상도·픽셀 | 화면 표시 크기 x 3, 격자 원본 3px |

## 2. 임포트 설정 (전 스프라이트 공통)

| 항목 | 값 |
|---|---|
| Texture Type | Sprite (2D and UI), Sprite Mode Single |
| Pixels Per Unit | **300** |
| Filter Mode | Bilinear (A3 후처리로 원본이 이미 3배 Nearest 확대본이라 모서리가 흐려지지 않음. 소수 배율 화면에서 점 깜빡임 방지) |
| Generate Mip Maps | 끔 |
| Compression | None (프레임류) / High Quality (`CodexPreviewBg`·`CodexGlow`) |
| alphaIsTransparency | 켬 |
| 9-slice | §3 표의 보더(Unity 순서 L,B,R,T). Image Type = Sliced, 보더가 있는 프레임은 fillCenter 켬/끔을 표에 명시 |
| 폴더 | `Assets/_Lair/Art/Sprites/CodexUI/` (신규 하위 폴더, Rule 04 §2 `Sprites/` 하위) |
| 로드 | Addressable 키가 아니라 프리팹 직접 참조(`CodexPopup` 의 기존 관례). 코드에서 스왑하는 `CodexCellFrameG1..G5`·`CodexCellFrameLocked` 는 `CodexMonsterCell` 의 `[SerializeField] Sprite` 필드로 받는다 |
| 메모리 | 합계 약 1.64Mpx. 비압축 RGBA 약 6.5MB, High Quality(ASTC 4x4, 1 byte/px) 약 1.6MB. 도감 팝업이 열릴 때만 로드되는 프리팹 의존이라 상시 점유 아님 |

## 3. 에셋 전수 (36장)

크기 = 화면 크기 x 3. 보더 = 화면 보더 x 3 (L,B,R,T). "틴트" = 회색조로 그려 런타임에서 `Image.color` 로 칠함.

### 3.1 팝업 틀

| # | 파일 | 화면 크기 -> 원본 크기 | 9-slice 보더 | 틴트 | 용도 |
|---|---|---|---|---|---|
| 1 | `CodexPanelFrame` | 팝업 전체 1136x640 (보더 36, 스프라이트 80x80 -> 240x240) | 108,108,108,108 (fillCenter 켬) | 아니오 | 팝업 배경 + 금테 이중 프레임 + 모서리 장식. 가운데는 어두운 석판 평면 |
| 2 | `CodexPanel` | 100x100 -> 300x300 | 48,48,48,48 (fillCenter 켬) | 아니오 | 진척 패널 200x178 · 상세 패널 352x472 · 내부 구획 바탕 |
| 3 | `CodexTitlePlaque` | 180x44 -> 540x132 | 90,0,90,0 (가로 늘림) | 아니오 | 제목판("도감") |
| 4 | `CodexDivider` | 320x2 -> 960x6 | 30,0,30,0 | 아니오 | 상세 패널 구분선(금빛이 옅어지는 선) |

### 3.2 탭·버튼·칩

| # | 파일 | 크기 | 9-slice 보더 | 틴트 | 용도 |
|---|---|---|---|---|---|
| 5 | `CodexTabOff` | 96x36 -> 288x108 | 36,36,36,36 | 아니오 | 몬스터/카드 탭 비선택 |
| 6 | `CodexTabOn` | 96x36 -> 288x108 | 36,36,36,36 | 아니오 | 탭 선택(금 강조). Toggle graphic |
| 7 | `CodexCloseBtn` | 36x36 -> 108x108 | 없음 | 아니오 | 닫기(X) 버튼 |
| 8 | `CodexChip` | 200x40 -> 600x120 | 48,48,48,48 | 아니오 | 종족 칩 비선택(중립 석판) |
| 9 | `CodexChipOn` | 200x40 -> 600x120 | 48,48,48,48 | 아니오 | 종족 칩 선택(금테 + 밝은 면). Toggle graphic |
| 10 | `CodexChipBand` | 8x40 -> 24x120 | 없음(세로 늘림) | **예**(종족색) | 칩 왼쪽 종족색 띠. 회색조 평면 + 외곽선 |

### 3.3 매트릭스 칸

| # | 파일 | 크기 | 9-slice 보더 | 틴트 | 용도 |
|---|---|---|---|---|---|
| 11 | `CodexCellBg` | 98x83 -> 294x249 | 36,36,36,36 (fillCenter 켬) | 아니오 | 해금 칸 바탕(밝은 석판). 카드 탭 셀 바탕도 재사용 |
| 12 | `CodexCellBgLocked` | 294x249 | 36,36,36,36 | 아니오 | 미해금 칸 바탕(어두운 석판, 실루엣이 잘 보이게 중간 톤 후광 없음) |
| 13 | `CodexCellFrameG1` | 294x249 | 36,36,36,36 (fillCenter **끔**) | 아니오 | 풋내기 프레임 `#9aa5bd` 계열, 가장 단순 |
| 14 | `CodexCellFrameG2` | 294x249 | 36,36,36,36 (끔) | 아니오 | 견습 `#5aa9ff` 계열 |
| 15 | `CodexCellFrameG3` | 294x249 | 36,36,36,36 (끔) | 아니오 | 숙련 `#b07cff` 계열, 모서리 보강 |
| 16 | `CodexCellFrameG4` | 294x249 | 36,36,36,36 (끔) | 아니오 | 정예 `#f7c64a` 계열, 금속 모서리·윗면 장식 |
| 17 | `CodexCellFrameG5` | 294x249 | 36,36,36,36 (끔) | 아니오 | 군주 `#ff7a45` 계열, 가장 장식적(모서리 불꽃 문양·금 리벳) |
| 18 | `CodexCellFrameLocked` | 294x249 | 36,36,36,36 (끔) | 아니오 | 미해금 프레임 `#232834` 계열, 무채색·장식 없음 |
| 19 | `CodexCellFramePulse` | 294x249 | 36,36,36,36 (끔) | **예**(등급색) | 해금 연출 테두리 펄스용 얇은 회색조 프레임(알파·스케일 애니) |
| 20 | `CodexRowHeader` | 98x83 -> 294x249 | 36,36,36,36 | 아니오 | 등급 행 머리글 바탕(평면, 등급색은 글자·별로) |
| 21 | `CodexTierHeader` | 98x24 -> 294x72 | 24,24,24,24 | 아니오 | 티어 열 머리글 · 상세 조건 줄 바탕 |
| 22 | `CodexTag` | 70x22 -> 210x66 | 24,24,24,24 | **예**(등급색 테두리/글자 느낌) | 상세 패널 등급 칩·티어 칩 바탕 |

### 3.4 배지·아이콘

| # | 파일 | 크기 | 9-slice 보더 | 틴트 | 용도 |
|---|---|---|---|---|---|
| 23 | `CodexStar` | 14x14 -> 42x42 | 없음 | 해금=금 그대로, 흐림=회색 곱 | 등급 별 아이콘 |
| 24 | `CodexNewBadge` | 24x14 -> 72x42 | 12,12,12,12 | 아니오 | NEW 배지(금) |
| 25 | `CodexNewDot` | 10x10 -> 30x30 | 없음 | 아니오 | 종족 칩 NEW 점 |
| 26 | `CodexCheck` | 18x18 -> 54x54 | 없음 | 아니오(소울 초록) | 해금 조건 충족 체크 |
| 27 | `CodexCross` | 18x18 -> 54x54 | 없음 | 아니오(블러드 빨강) | 해금 조건 미충족 X |
| 28 | `CodexLock` | 24x24 -> 72x72 | 없음 | 아니오 | 잠금 아이콘(잠긴 미리보기·미니 칸 모서리) |

### 3.5 바·후광·미리보기

| # | 파일 | 크기 | 9-slice 보더 | 틴트 | 용도 |
|---|---|---|---|---|---|
| 29 | `CodexProgressTrack` | 168x22 -> 504x66 | 21,21,21,21 | 아니오 | 진척 바 배경(파인 홈) |
| 30 | `CodexProgressFill` | 168x22 -> 504x66 (홈 안 채움은 안쪽 여백 포함해 한 장) | 없음(Filled Horizontal) | 아니오 | 진척 바 채움(금 줄무늬). Image Type Filled |
| 31 | `CodexPreviewBg` | 160x160 -> 480x480 | 없음 | 아니오 | 상세 큰 미리보기 배경(어두운 석판 바닥 + 중앙 조명 + 바닥 타원 그림자). 프레임은 `CodexCellFrameG*` 를 9-slice 로 위에 얹음 |
| 32 | `CodexGlow` | 64x64 -> 192x192 | 없음 | **예**(종족색) | 몸 뒤 후광 바탕(흰색 방사 + 알파 감쇠) |

### 3.6 연출·스크롤

| # | 파일 | 크기 | 9-slice 보더 | 틴트 | 용도 |
|---|---|---|---|---|---|
| 33 | `CodexUnlockRing` | 100x100 -> 256x256(약 2.56배, 확대 애니 전제) | 없음 | **예**(등급색) | 해금 연출 퍼지는 링 |
| 34 | `CodexSpark` | 32x32 -> 96x96 | 없음 | **예**(등급색) | 해금 연출 스파클(4방향 반짝임). RectTransform 으로 8~10px 로 표시 |
| 35 | `CodexScrollTrack` | 12x30 -> 36x90 | 18,18,18,18 | 아니오 | 스크롤바 홈(카드 탭 스크롤뷰) |
| 36 | `CodexScrollThumb` | 12x30 -> 36x90 | 18,18,18,18 | 아니오 | 스크롤 손잡이 |

검산: 3.1 4장 + 3.2 6장 + 3.3 12장 + 3.4 6장 + 3.5 4장 + 3.6 4장 = 36.

## 4. 설계 판단 (사용자 요청 "검토·권장안")

### 4.1 칸 프레임: 등급색 5장 직접 vs 회색조 틴트

| 안 | 장점 | 단점 |
|---|---|---|
| **직접 5장 + 미해금 1장 (권장)** | 등급마다 **장식 복잡도·금속 마감·모서리 문양이 다르게** 그려져 군주가 눈에 띔(몬스터 등급 연출의 "단계가 올라가는 느낌"과 일치). 색이 곱해지지 않아 `#f7c64a` 금·`#ff7a45` 주황이 칙칙해지지 않음 | 6장(원본 294x249) 추가 |
| 회색조 1장 + 틴트 | 1장으로 끝, 색 변경 쉬움 | 모든 등급이 같은 모양이라 등급 차이가 색뿐. 어두운 회색에 주황·보라를 곱하면 채도·명도가 죽음, 금속 하이라이트가 색에 묻힘 |

권장: **직접 그린 6장**. 단 해금 연출용 `CodexCellFramePulse`(#19)는 회색조 1장을 등급색으로 틴트(깜빡이는 효과에는 모양 차이가 불필요). 상세 미리보기 프레임·티어 미니 칸도 같은 6장을 9-slice 로 재사용한다(크기만 다름).

### 4.2 종족 칩: 6종족 색 변형 vs 중립 + 틴트 띠

권장: **중립 칩 2장(`CodexChip`·`CodexChipOn`) + 회색조 띠 `CodexChipBand` 를 종족색으로 틴트**. 근거: 종족색은 단색 띠 하나로 충분히 식별되고(현행 목업 동일), 6 x 2 = 12장을 더 만들면 세트가 38장에서 48장으로 늘지만 시각 차이는 띠 색뿐이다. 종족별 문양이 필요해지면(종족별 이펙트 차별화 후속) 그때 6장 추가.

## 5. 항목별 GPT 프롬프트

생성기: **Codex CLI 이미지 생성**. 모든 프롬프트는 영문, 공통 STYLE BLOCK + 항목 CONTENT + NEGATIVE + 참조 이미지 첨부 지시로 구성한다. 항목은 §6 의 시트(S0~S6)로 묶어 한 번에 여러 개를 생성한 뒤 잘라 쓴다.

### 5.1 공통 STYLE BLOCK (모든 프롬프트 맨 앞)

```
STYLE BLOCK: High-resolution clean pixel-art UI for a dark-fantasy game, in the same visual world as a chibi pixel-art monster sprite sheet: bold near-black outlines (#0a0810), flat colors with exactly 2 to 3 shading tones per surface, hard pixel edges aligned to a coarse grid, no anti-aliasing, no dithering, no blur, no photo texture, no gradients except where the item explicitly asks for a soft glow. Carved dark-blue stone slab material (#2a3552 light, #1a2132 base, #121827 dark), a 1-pixel bright bevel line on the top-left (#4a5a80) and a 1-pixel dark shade on the bottom-right (#0e1320), gold accents in three tones (#f7c64a, #c8962a, #8a6a2c) with a #ffe9a8 sparkle pixel. Ornament only inside the border ring, flat clean fills in stretch areas. Symmetrical, centered, crisp.
```

공통 NEGATIVE: `no text, no letters, no numbers, no watermark, no drop shadow outside the object, no extra objects, no perspective, no photo realism, no soft airbrush, no neon glow (unless asked), no anti-aliased edges`.

### 5.2 참조 이미지 첨부 지시

- **스타일 시트 승인 전(S0)**: 몬스터 시트 2~3장(`.mockups/assets/v5/Wisp_base.png`, `Hex_T2.png`, `Phantom_T3.png`)과 현재 도감 목업 캡처(`.mockups/monster-codex.html` 스크린샷)를 참조로 첨부하고 "match the outline weight, shading tone count, and palette feel of these monsters" 를 덧붙인다.
- **승인 후(S1~S6)**: 승인된 **스타일 시트 PNG 를 항상 첨부**하고 "Match the approved style sheet exactly: same outline thickness, same bevel, same gold tones, same corner ornament vocabulary" 를 덧붙인다.
- 배경: 투명 PNG 가 필요한 항목은 **마젠타 배경 키잉**(`#FF00FF` 단색 배경) 지시를 넣는다: "solid flat magenta background (#FF00FF), the object must not contain magenta".

### 5.3 항목별 CONTENT (STYLE BLOCK 뒤에 붙임)

| # | 파일 | CONTENT |
|---|---|---|
| 1 | `CodexPanelFrame` | Large 9-slice window frame, canvas 1024x1024, magenta outside the frame only. Layout: outer 36-unit-wide border ring on all sides carries: near-black outline, double gold frame (outer #c8962a band, 1px dark gap, inner #8a6a2c hairline), cut corners with ornate gold corner brackets and small rivets. The center is ONE flat dark slab fill (#121827) with absolutely no ornament so it can stretch to 1136x640. Edges between corners are plain straight bands (no ornament) |
| 2 | `CodexPanel` | 9-slice inset panel, canvas 1024x1024. 16-unit ring: near-black outline + subtle bevel + notched corners; center flat #1a2132. Slightly lighter than the window background so it reads as a raised plate |
| 3 | `CodexTitlePlaque` | Horizontal title plate for the word 'codex' (do NOT draw text), wide shape, gold-trimmed dark slab with small side wings; the middle is flat so it can stretch horizontally |
| 4 | `CodexDivider` | A thin horizontal gold line, 3 tones, brightest at the center fading to transparent at both ends (hard-stepped, no smooth gradient), tiny diamond at the center |
| 5 | `CodexTabOff` | A folder-tab-style button face, unselected: dark slab #1a2132, dim bevel, no gold |
| 6 | `CodexTabOn` | Same silhouette as CodexTabOff, selected: warm gold-trimmed top edge, lighter face #2a3552, bright gold underline |
| 7 | `CodexCloseBtn` | Small square close button: stone slab with a bold carved X mark in light bone color (#e8e1cf), gold trim corners |
| 8 | `CodexChip` | Wide pill-slab button (unselected): dark slab, flat center for text, left side leaves room for a color band, slightly notched ends |
| 9 | `CodexChipOn` | Same silhouette as CodexChip, selected: gold outline (#f7c64a), lighter face, bright bevel |
| 10 | `CodexChipBand` | A narrow vertical bar, GRAYSCALE ONLY (white-to-gray with a black outline), flat, to be tinted in engine |
| 11 | `CodexCellBg` | 9-slice cell background, unlocked: mid-dark slab #1a2132 with a faint lighter inner area, flat center |
| 12 | `CodexCellBgLocked` | 9-slice cell background, locked: very dark slab #10151f, no highlights, flat center |
| 13 | `CodexCellFrameG1` | Hollow 9-slice frame (center fully transparent), thin simple frame in cool gray #9aa5bd: 2px outline, 1px gray inner line, plain square corners, NO ornament |
| 14 | `CodexCellFrameG2` | Hollow frame in blue #5aa9ff, same outline weight, small corner notches |
| 15 | `CodexCellFrameG3` | Hollow frame in violet #b07cff, corner brackets in light violet, small rivets at the corners |
| 16 | `CodexCellFrameG4` | Hollow frame in gold #f7c64a with darker gold #c8962a lower side, metal corner caps, a small crest on the top edge center |
| 17 | `CodexCellFrameG5` | Hollow frame in fiery orange #ff7a45 with gold rivets, ornate flame-shaped corner brackets, glowing inner edge line, the most elaborate of the five; all ornament stays inside the 12-unit border ring |
| 18 | `CodexCellFrameLocked` | Hollow frame in dull dark gray #232834, no ornament, no highlights |
| 19 | `CodexCellFramePulse` | Hollow thin frame, GRAYSCALE ONLY (white), 2px, plain rounded-square corners, to be tinted and animated |
| 20 | `CodexRowHeader` | Flat dark slab plate for a row label, notched corners, flat center, subtle left-to-right bevel |
| 21 | `CodexTierHeader` | Flat low plate for a column header or text line, flat center, thin outline |
| 22 | `CodexTag` | Small flat pill tag, GRAYSCALE ONLY light-gray fill with a dark outline, to be tinted |
| 23 | `CodexStar` | A single chunky five-point star, gold three-tone (#f7c64a / #c8962a / #8a6a2c) with #ffe9a8 sparkle pixel, bold dark outline |
| 24 | `CodexNewBadge` | Small gold pill badge background (no text), notched corners, 3-tone gold, bold outline |
| 25 | `CodexNewDot` | A small round gold dot with a dark outline and one highlight pixel |
| 26 | `CodexCheck` | A bold green (#5ef0b4 tones) check mark, dark outline, chunky |
| 27 | `CodexCross` | A bold red (#e5484d tones) X mark, dark outline, chunky |
| 28 | `CodexLock` | A chunky padlock icon in dull gray-blue tones with a dark outline |
| 29 | `CodexProgressTrack` | Horizontal recessed bar trough, 9-slice, dark interior #0d1119, inner top-left shadow and bottom-right highlight so it reads sunken |
| 30 | `CodexProgressFill` | Horizontal bar fill that fits inside the trough: gold stripe with 3 tones, bright top line, rounded-square left end, plain right end, no text |
| 31 | `CodexPreviewBg` | Square diorama backdrop 1024x1024: dark stone floor with a soft elliptical shadow patch in the lower third and a soft spotlight cone from above, almost flat colors, stepped (banded) light, no objects, no characters |
| 32 | `CodexGlow` | A soft radial glow: pure white, brightest at the center, fading smoothly to fully transparent at the edge. Generate WHITE ON PURE BLACK, converted to alpha later |
| 33 | `CodexUnlockRing` | A thin clean circle ring, GRAYSCALE ONLY white, 3-tone soft edge, to be tinted and scaled up. Generate white on pure black, converted to alpha later |
| 34 | `CodexSpark` | A 4-point sparkle star, white, thin arms, bright 2x2 core. Generate white on pure black, converted to alpha later |
| 35 | `CodexScrollTrack` | Narrow vertical scrollbar groove, 9-slice, recessed dark slab with bevel |
| 36 | `CodexScrollThumb` | Narrow vertical scroll handle, 9-slice, raised stone-blue slab with a gold hairline in the center |

## 6. 제작 단계·일정

| 단계 | 내용 | 산출·게이트 | 일정 |
|---|---|---|---|
| S0 스타일 시트 | **스타일 시트 1장**: 도감 팝업 전체 시안 1장(1536x1024, 레이아웃 = 목업) + 대표 요소 6개(`CodexPanelFrame`·`CodexCellFrameG1`·`G3`·`G5`·`CodexCellFrameLocked`·`CodexChipOn`)를 한 장에 배치. 몬스터 시트와 나란히 비교 | **사용자 승인 게이트**(통과 전 S1 금지) | 사용량 한도 해제 후 **2026-11-10 오전 3:43 이후** |
| S1 틀 | `CodexPanelFrame`·`CodexPanel`·`CodexTitlePlaque`·`CodexDivider`·`CodexCloseBtn`·`CodexTabOff/On` (스타일 시트 PNG 첨부) | 육안 검수 | S0 승인 후 |
| S2 칩·칸 | `CodexChip`·`CodexChipOn`·`CodexChipBand`·`CodexCellBg`·`CodexCellBgLocked`·`CodexCellFrameG1..G5`·`CodexCellFrameLocked`·`CodexCellFramePulse`·`CodexRowHeader`·`CodexTierHeader`·`CodexTag` | 6프레임 나란히 비교(등급 상승이 한눈에 읽히는지) | S1 후 |
| S3 아이콘·배지 | `CodexStar`·`CodexNewBadge`·`CodexNewDot`·`CodexCheck`·`CodexCross`·`CodexLock` | 소형 가독성(실제 크기 14~24px) | S2 후 |
| S4 바·미리보기 | `CodexProgressTrack`·`CodexProgressFill`·`CodexPreviewBg`·`CodexScrollTrack`·`CodexScrollThumb` | 늘림 테스트 | S3 후 |
| S5 연출 | `CodexGlow`·`CodexUnlockRing`·`CodexSpark` (흰색-검정 생성 후 알파 변환) | 틴트·애니 확인 | S4 후 |
| S6 통합 | 프리팹 배선(§8), 인게임 스크린샷, 목업 대비 검수 | 사용자 최종 확인 | 마지막 |

Codex CLI 사용량 한도에 걸려 있으므로 **S0 는 한도 해제 시각(2026-11-10 오전 3:43) 이후에 시작**한다. 한 번에 시트 형태로 여러 요소를 뽑아 호출 횟수를 줄인다(시트 배치: 한 장에 3~6개, 요소 사이 간격 128px 이상 마젠타 배경).

## 7. 후처리·검수 절차

1. **크롭**: 시트에서 요소를 잘라낸다(여백 포함).
2. **키잉**: 마젠타(`#FF00FF`) 배경 제거(크로마키 + 가장자리 마젠타 잔상 제거 defringe). 흰-검정 생성물(#32·#33·#34)은 **밝기 -> 알파** 변환 후 순백 단색화.
3. **화면 해상도로 축소**: 최종 원본 크기 /3 (= 화면 크기)로 축소 - 보간 대신 Nearest(또는 박스 평균 후 팔레트 스냅).
4. **팔레트 스냅**: §1 팔레트로 색 양자화(광원류 #32·#33·#34 는 알파 그라데이션 유지, 색은 순백).
5. **알파 이진화**(경계 반투명 제거. 광원류 예외).
6. **3배 Nearest 확대** -> §3 의 원본 크기로 저장(PNG, 투명).
7. **9-slice 검증**: 보더 링 안에서만 장식이 있고 가운데 영역이 균일한가, 보더 값대로 임포트해 극단 크기(최소 / 팝업 크기)로 늘려 모서리가 깨지지 않는지(예: `CodexPanelFrame` 1136x640, `CodexCellFrameG*` 98x83 및 미리보기 160x160 / 미니 칸 76x72).
8. **틴트 검증**(#10·#19·#22·#32·#33·#34): 등급색·종족색 6종에서 색이 의도대로 나오는가(회색조여야 함).
9. 어두운 배경(`#10151f`)·밝은 배경 양쪽에서 가장자리 번짐 확인.
10. 실제 크기(1280x720 기준 화면)에서 가독성: 별 14px · NEW 점 10px · 체크 18px 이 알아볼 수 있는가.
11. 통과한 PNG 는 `Assets/_Lair/Art/Sprites/CodexUI/<파일명>.png` 로 저장 후 §2 임포트 설정 적용.

## 8. 프리팹 배선 변경 (gameplay-programmer 용)

코드 변경은 거의 없다 - 프리팹의 Image `Sprite` 연결과 `CodexMonsterCell` 의 `[SerializeField] Sprite` 필드(등급 프레임 6장 스왑) 정도다. 위젯 구조 자체는 `monster-codex.md` §8 을 따른다.

### 8.1 `CodexPopup.prefab`

| 위젯(Image/RawImage) | 스프라이트 | 타입 | 비고 |
|---|---|---|---|
| 팝업 루트 배경 | `CodexPanelFrame` | Sliced, fillCenter 켬 | 1136x640 |
| Dim(어두운 뒤 배경) | 스프라이트 없음(단색 알파) | Simple | 현행 유지 |
| 제목 바탕 | `CodexTitlePlaque` | Sliced | 180x44 |
| 몬스터 탭 / 카드 탭 배경 | `CodexTabOff` | Sliced | Toggle targetGraphic |
| 탭 선택 표시(자식) | `CodexTabOn` | Sliced | Toggle graphic(isOn 일 때 표시) |
| 닫기 버튼 | `CodexCloseBtn` | Simple | Button, ColorTint |
| 종족 칩 6개 배경 | `CodexChip` | Sliced | 200x40 |
| 종족 칩 선택 표시(자식) | `CodexChipOn` | Sliced | Toggle graphic |
| 종족 칩 색 띠 | `CodexChipBand` | Simple, 색 = 종족색 | 8x40 |
| 종족 칩 NEW 점 | `CodexNewDot` | Simple | 신규 종족 표시 |
| 진척 패널 | `CodexPanel` | Sliced | 200x178 |
| 진척 바 배경 / 채움 | `CodexProgressTrack` / `CodexProgressFill` | Sliced / Filled Horizontal | 168x22 |
| 티어 열 머리글 4개 | `CodexTierHeader` | Sliced | 98x24 |
| 상세 패널 | `CodexPanel` | Sliced | 352x472 |
| 상세 미리보기 배경 | `CodexPreviewBg` | Simple | 160x160, 아래에 RawImage(RenderTexture) |
| 상세 미리보기 후광 | `CodexGlow` | Simple, 색 = 종족색 | 몸 뒤 |
| 상세 미리보기 프레임 | `CodexCellFrameG1..G5` / `CodexCellFrameLocked` | Sliced, fillCenter 끔 | 등급·해금에 따라 스왑 |
| 등급 칩 / 티어 칩 | `CodexTag` | Sliced, 색 = 등급색/무채색 | 70x22 / 80x22 |
| 별 5개 | `CodexStar` | Simple | 해금 = 금, 미해금 = 회색 곱 |
| 티어 선택 미니 칸 4개 바탕 / 프레임 | `CodexCellBg`(`CodexCellBgLocked`) / `CodexCellFrameG*`(`Locked`) | Sliced | 76x72 |
| 구분선 | `CodexDivider` | Sliced | 320x2 |
| 해금 조건 줄 바탕 | `CodexTierHeader` | Sliced | 312x36 |
| 조건 충족 / 미충족 아이콘 | `CodexCheck` / `CodexCross` | Simple | 18x18 |
| 잠금 아이콘 | `CodexLock` | Simple | 잠긴 미리보기 모서리·잠긴 미니 칸 |
| 카드 탭 스크롤바 | `CodexScrollTrack` / `CodexScrollThumb` | Sliced | 기존 스크롤바 스프라이트 대체 |

### 8.2 `CodexMonsterCell.prefab`

| 위젯 | 스프라이트 | 비고 |
|---|---|---|
| 바탕 | `CodexCellBg`(해금) / `CodexCellBgLocked`(미해금) | `IsRowHeader` 면 `CodexRowHeader` |
| 프레임 | `CodexCellFrameG1..G5` / `CodexCellFrameLocked` | `CodexMonsterCell` 의 `[SerializeField] Sprite[] _gradeFrames`(5) + `_lockedFrame` 로 스왑 |
| 해금 연출 펄스 프레임(자식, 평소 비활성) | `CodexCellFramePulse` | 색 = `Grade_CodexUnlock.colorHex` |
| 후광(몸 뒤) | `CodexGlow` | 색 = 종족색, 알파 낮춤 |
| 해금 연출 링(자식, 평소 비활성) | `CodexUnlockRing` | 색 = 등급색 |
| 해금 연출 스파클 | `CodexSpark`(풀링 프리팹 `CodexSpark.prefab` 권장) | `CHMPool` 로 스폰(Rule 03 §4), 색 = 등급색 |
| NEW 배지 | `CodexNewBadge` | 24x14 |
| 행 머리글의 별 | `CodexStar` | 등급 수만큼 |

### 8.3 `CodexCell.prefab` (카드 탭 셀)

`_normalSprite` = `CodexCellBg`, `_dummySprite` = `CodexCellBgLocked`(탭 일관성, 선택 사항 아님 - 도감 팝업 안에서 한 세트로 보이게). 기존 `Px_Panel`/`Px_PanelDark` 참조는 이 두 프리팹에서 제거한다.

### 8.4 정리

- 이전 `Px_CodexCellFrame`·`Px_CodexGlow`·`Px_CodexSpark` 3장: GUID 참조 0건 확인 후 삭제(A7).
- 도감 프리팹에서 `Px_*`·`WorldMap*` 참조가 남지 않았는지 GUID Grep 으로 확인(검수 게이트 §10).

## 9. 공통 UI 전체 교체(`ui-art-refresh.md`)와의 관계

- 이번에 확정되는 **도감 세트가 공통 UI 세트의 스타일 기준**이다(외곽선 굵기·음영 톤 수·금 3톤·모서리 장식 어휘·등급색 프레임 방식).
- `docs/design/ui-art-refresh.md` 의 전제 중 **"같은 파일명·같은 크기·같은 9-slice/PPU 로 제자리 덮어쓰기"와 "소형 도트(PPU 25) 유지"는 이 방침(고해상도 신규 세트)과 다르므로 후속 개정 대상**이다. 이번에는 개정하지 않고 메모만 남긴다(`ui-art-refresh.md` 상단 메모 참조). 도감 세트 승인 후 공통 UI 도 "화면별 고해상도 신규 세트(화면 단위로 순차)" 로 방향을 재정리한다.
- 도감 세트는 `Codex*` 접두어로 도감에서만 쓰고 공통 `Px_*` 를 수정하지 않으므로 **충돌 없음**. 이후 공통 UI 세트가 만들어지면 도감 스프라이트 중 범용(`CodexPanel`·`CodexChip` 등)을 공통 이름으로 승격할지 그때 결정한다.

## 10. 검수 게이트

| ID | 종류 | 내용 | 담당 |
|---|---|---|---|
| UA1 | 육안 | S0 스타일 시트 승인(몬스터 시트 옆 비교) | 사용자 |
| UA2 | 자동 | `Codex*` 36장의 크기(§3)·보더·PPU 300·Filter·fillCenter 설정이 표와 일치 | test-engineer |
| UA3 | 자동 | `CodexPopup`·`CodexMonsterCell`·`CodexCell` 프리팹에 `Px_*`·`WorldMap*` GUID 참조 0건 | test-engineer |
| UA4 | 육안 | 6프레임이 등급 순으로 장식이 점증, 틴트 6종 색(종족)·등급색이 맞음, 극단 크기 늘림 시 모서리 정상 | 사용자 |
| UA5 | 육안 | 인게임 도감 스크린샷이 목업과 일치, 소형 아이콘 가독성 | 사용자 |
| UA6 | 프로파일 | 도감 오픈 시 텍스처 메모리·프레임(`monster-codex.md` CX7 와 합산) | qa-simulator / 사용자 |

## 12. 후속 개선 목록 (확정 이후 세부 수정)

| # | 항목 | 내용 | 우선 |
|---|---|---|---|
| F1 | 스크롤바 폭 | `CodexScrollTrack`/`CodexScrollThumb`(화면 12px)이 실제 스크롤뷰(카드 탭)에서 폭이 안 맞음 -> 폭 재조정 후 재생성·프리팹 `Scrollbar` 폭 정합 | 상 |
| F2 | 조건 줄 좌표 | 상세 패널 해금 조건 줄(체크/X 아이콘·글자·바탕 `CodexTierHeader`)의 좌표·간격이 목업(312x36, 간격 40)과 어긋남 -> `CodexPopup.prefab` 좌표 수정 | 상 |
| F3 | 늘림 테스트 | 36장 전부 극단 크기에서 모서리·보더 비율 재검수, 필요 시 해당 장만 재생성 | 중 |
| F4 | 소형 가독성 | 별 14px·NEW 점 10px·체크 18px 실제 크기 가독성 점검 | 중 |
| F5 | 공통 세트 이관 | F1·F2 완료 후 24장을 `CommonUI*` 로 이관(GUID 유지 이동·리네임, `ui-art-refresh.md` §4.2). **그 전에는 이동·리네임 금지** | 후 |
| F6 | 구 스프라이트 정리 | 도감 프리팹의 `Px_*`·`WorldMap*` 참조 0건 확인, 이전 `Px_CodexCellFrame`·`Px_CodexGlow`·`Px_CodexSpark` 삭제 | 후 |

## 11. 열린 질문

1. 스타일 시트(S0)에 도감 팝업 전체 시안 1장 + 대표 6요소를 한 번에 뽑는 것으로 진행해도 되는가. 추천: 그대로.
2. 등급 프레임 직접 6장(권장) 대신 회색조 틴트 1장으로 줄일 의향이 있는가. 추천: 직접 6장.
3. 원본 해상도 3배(PPU 300)로 확정해도 되는가(2배/4배 대안은 메모리·품질 절충). 추천: 3배.
