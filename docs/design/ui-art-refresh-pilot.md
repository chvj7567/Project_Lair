# 공통 UI 아트 교체 - 파일럿 6장 제작 지시서

> **폐기(2026-11-15)**: 5~9px 소형 도트 제자리 덮어쓰기 + 파일럿 6장 계획은 폐기됐다. 새 계획(화면 단위 전용 고해상도 GPT 아트 세트, 도감 세트가 스타일 기준)은 `docs/design/ui-art-refresh.md` 를 따른다. 이 문서는 이력 보존용이며 따르지 않는다. 관련 목업 `.mockups/ui-art-refresh.html` 도 폐기 대상이다.

> 상위 기획서: `docs/design/ui-art-refresh.md` (D5 확정: 파일럿 6장 -> 사용자 승인 -> B1~B5 일괄) · 비교 목업: `.mockups/ui-art-refresh.html`
> 이 문서는 파일럿 6장만 다룬다. **승인 전에는 `Assets` 의 PNG 를 덮어쓰지 않는다.** 시안은 `.mockups/assets/ui-pilot/` 에만 저장해 비교 목업에서 확인한다.

## 1. 파일럿 6장 사양

| # | 파일 | 경로 | PPU | 9-slice 보더 (L,B,R,T) | 현재 픽셀 크기 | 임포트 | 틴트 | 대표 용도 |
|---|---|---|---|---|---|---|---|---|
| 1 | `Px_PanelDark` | `Assets/_Lair/Art/Sprites/UiDot/Px_PanelDark.png` | 25 | 3,3,3,3 | **실측 필요** | Sprite Single, Point, 알파 투명 | 일부 화면에서 Image.color 로 곱함(코드 `.color=` 사용 화면 확인) | 어두운 석판 바탕: 영웅선택 셀·데일리·편성·탑·전투 HUD·월드맵·마을 HUD 등 12파일 19건 |
| 2 | `Px_Btn` | `Assets/_Lair/Art/Sprites/UiDot/Px_Btn.png` | 25 | 2,3,2,2 | **실측 필요** | 동일 | 일반 버튼(필요 시 색 곱) | 편성·월드맵·월드맵 챕터탭 3파일 |
| 3 | `Px_BtnGold` | `Assets/_Lair/Art/Sprites/UiDot/Px_BtnGold.png` | 25 | 2,3,2,2 | **실측 필요** | 동일 | 금색이 그림에 포함(색 곱 최소) | 데일리·탑·편성·월드맵·상점 아이템셀·챕터탭 |
| 4 | `Px_BarBg` | `Assets/_Lair/Art/Sprites/UiDot/Px_BarBg.png` | 25 | 2,2,2,2 | **실측 필요** | 동일 | 바 배경 | 편성·마을 HUD·HpBar 3파일 |
| 5 | `WorldMapFrame` | `Assets/_Lair/Art/Sprites/WorldMapFrame.png` | 100 | 9,9,9,9 | **실측 필요** | Sprite Single, 알파 투명 (필터는 현행 값 유지) | 없음(금테 포함). Sliced, **fillCenter 끔** | 거의 모든 모달의 금테 이중 프레임 35건·16파일 |
| 6 | `Px_Badge_Gold` | `Assets/_Lair/Art/Sprites/UiDot/Px_Badge_Gold.png` | 25 | 2,2,2,2 | **실측 필요** | Point, 알파 투명 | 없음 | 배지(현재 프리팹 직접 참조 0건 - 도감 개편 NEW 배지 사용 예정) |

**현재 픽셀 크기**: Unity `.meta` 에 단일 스프라이트의 픽셀 크기가 없고 이 에이전트는 PNG 를 측정할 수 없어 "실측 필요"로 둔다. Phase 0 덤프(또는 이미지 뷰어/`file` 명령)로 6장의 `W x H` 를 확인한 뒤 아래 "4배 캔버스"를 계산한다. **제약**: 가로 >= 좌+우 보더 + 1, 세로 >= 상+하 보더 + 1(9-slice 가운데 영역이 1px 이상 남아야 함).

## 2. 4배 생성 -> 축소 -> 스냅 규칙

1. GPT 캔버스 = **현재 크기 W x H 의 4배**(4W x 4H). 보더 영역도 4배(예: 3px 보더 -> 12px).
2. 생성물을 **Nearest Neighbor** 로 W x H 로 축소(보간·Lanczos 금지).
3. §3 팔레트로 **색 스냅**(가장 가까운 팔레트 색으로 양자화).
4. 알파 이진화(경계 반투명 제거).
5. 9-slice 검증: 가운데 영역이 균일한 색·반복 가능 패턴, 장식은 보더 안에만.
6. 어두운 배경(`#10151f`)·밝은 배경에서 가장자리 번짐 확인 -> 비교 목업(`.mockups/assets/ui-pilot/<파일명>.png`)에 저장.

## 3. 팔레트 (docs/design/ui-art-refresh.md §4 기준)

| 용도 | 색 |
|---|---|
| 외곽선 | `#0a0810` |
| 석판 면 3톤 | `#2a3552`(밝음) / `#1a2132`(기본) / `#121827`(어두움) |
| 베벨 | 위·왼쪽 `#4a5a80`, 아래·오른쪽 `#0e1320` |
| 버튼 면(일반) | `#4a5a80` / `#3a4664` / `#2a3350` |
| 금 3톤 | `#f7c64a` / `#c8962a` / `#8a6a2c`, 하이라이트 점 `#ffe9a8` |
| 바 트러프 | `#0d1119`(안) / 외곽선 `#0a0810` / 베벨 `#1e2638` |

## 4. GPT 프롬프트

### 4.1 공통 화풍 블록 (모든 프롬프트 맨 앞)

```
STYLE BLOCK: Dark-fantasy chunky pixel-art UI element, hard pixel edges, no anti-aliasing, no gradients, no dithering, no blur, no photo texture. A bold near-black outline (#0a0810) 2 pixels thick, flat colors with exactly 3 shading tones per surface (light, base, dark), a 1-pixel top-left highlight and 1-pixel bottom-right shade for a carved-stone-slab bevel. Palette: slab blues #2a3552 / #1a2132 / #121827, bevel #4a5a80 and #0e1320, gold accents #f7c64a / #c8962a / #8a6a2c with a #ffe9a8 sparkle pixel. Clean, centered, symmetrical where applicable, transparent PNG background (alpha 0 outside the object). Same visual language as a chibi pixel-art monster sheet: thick dark outlines, clean simple shapes.
```

공통 NEGATIVE: `no text, no drop shadow outside the outline, no glow, no background fill, no gradient, no anti-aliasing, no extra objects, no perspective, no rounded blur edges`.

### 4.2 항목별 프롬프트 (캔버스 4W x 4H 는 실측 후 대입)

**#1 `Px_PanelDark`**
```
[STYLE BLOCK]
TARGET: Px_PanelDark - a dark carved stone slab panel used as a UI background that is stretched to many sizes. Canvas 4W x 4H pixels (W x H = current file size, to be downscaled with nearest-neighbor).
LAYOUT: 9-slice. The outer 12-pixel ring (3 px at 1x) carries the 2px(at 1x) near-black outline, a notched/cut corner, and the 1px top-left highlight (#4a5a80) and 1px bottom-right shade (#0e1320). The entire center area is ONE flat repeatable fill of the darkest slab tone (#121827) with no ornament, no cracks, no gradient so it can stretch freely.
CONTENT: a dark stone slab panel, flat front view, three tones only (the center uses #121827, the inner edge band #1a2132).
NEGATIVE: [common negative]
```

**#2 `Px_Btn`**
```
[STYLE BLOCK]
TARGET: Px_Btn - a standard blue-gray stone button face, stretched horizontally. Canvas 4W x 4H pixels.
LAYOUT: 9-slice, borders at 1x: left 2, bottom 3, right 2, top 2 (multiply by 4). The thicker bottom border is a darker 'pressed-lip' band (#2a3350 then outline). Corners carry the notch; the center is one flat repeatable fill.
CONTENT: a raised button: top face light #4a5a80, middle #3a4664, bottom band #2a3350, 1px top-left highlight #6a7aa0, 1px bottom-right shade #0e1320, 2px near-black outline. No text, no icon.
NEGATIVE: [common negative]
```

**#3 `Px_BtnGold`**
```
[STYLE BLOCK]
TARGET: Px_BtnGold - the gold call-to-action version of Px_Btn, identical silhouette, borders and proportions to Px_Btn. Canvas 4W x 4H pixels.
LAYOUT: 9-slice, borders at 1x: left 2, bottom 3, right 2, top 2 (multiply by 4); center is one flat repeatable fill.
CONTENT: a raised gold button: top face #f7c64a, middle #c8962a, bottom lip #8a6a2c, 1px top-left highlight #ffe9a8, 1px bottom-right shade #6a4a14, 2px near-black outline. No text, no icon.
NEGATIVE: [common negative]
```

**#4 `Px_BarBg`**
```
[STYLE BLOCK]
TARGET: Px_BarBg - the empty trough of a horizontal progress bar, stretched horizontally (a separate fill sprite is drawn on top). Canvas 4W x 4H pixels.
LAYOUT: 9-slice with 2 px (1x) borders on all sides (multiply by 4); center is one flat repeatable fill #0d1119 with no ornament.
CONTENT: a recessed trough: 2px near-black outline, inner top-left shade darker (#06080c) and inner bottom-right highlight #1e2638 to read as sunken, flat dark interior.
NEGATIVE: [common negative]
```

**#5 `WorldMapFrame`**
```
[STYLE BLOCK]
TARGET: WorldMapFrame - a hollow double gold frame drawn around modal windows, stretched to many sizes. Canvas 4W x 4H pixels.
LAYOUT: 9-slice with 9 px (1x) borders on all sides (36 px at 4x). ONLY the border ring is drawn; the entire center and everything outside the frame are fully transparent (alpha 0). Edges between corners are plain repeatable straight bands with no ornament so they can stretch; any ornament stays inside the 9 px corner squares.
CONTENT: an outer 2px near-black outline, then a thin gold band (#c8962a with a #f7c64a highlight line), a 1px dark gap (#0a0810), then an inner thin gold hairline (#8a6a2c), then a 1px inner outline. Corners are squared with a tiny notch. Gold only - no blue.
NEGATIVE: [common negative], no filled center
```

**#6 `Px_Badge_Gold`**
```
[STYLE BLOCK]
TARGET: Px_Badge_Gold - a small gold pill-shaped badge background (for 'NEW' style labels), stretched horizontally. Canvas 4W x 4H pixels.
LAYOUT: 9-slice with 2 px (1x) borders on all sides (multiply by 4); center one flat repeatable gold fill. Corners are cut (notched) instead of round.
CONTENT: gold badge: fill #f7c64a, 1px top-left highlight #ffe9a8, 1px bottom-right shade #8a6a2c, 2px near-black outline. No text.
NEGATIVE: [common negative]
```

## 5. 파일럿 산출·승인 절차

1. 6장을 4배로 생성 -> 후처리(§2) -> `.mockups/assets/ui-pilot/<파일명>.png` 에 저장(`Assets` 덮어쓰기 금지).
2. `.mockups/ui-art-refresh.html` 의 파일럿 "시안 자리" 6칸에 PNG 를 연결하고, 몬스터 시트(`.mockups/assets/v5/*`) 옆에 둔 비교 화면을 만든다(현재 / 시안 / 몬스터 옆).
3. 사용자 승인 기준: ① 외곽선·음영이 몬스터 시트와 어울린다 ② 늘렸을 때 모서리가 깨지지 않는다 ③ 금 포인트가 과하지 않다 ④ 팔레트 수치(`ui-art-refresh.md` §4) 확정.
4. 승인되면 B1~B5 일괄 생성(`ui-art-refresh.md` §5.1 Phase 2~6). 불합격이면 §3 팔레트·프롬프트 서술만 조정해 6장 재생성.

## 6. 파일럿 한계

- 틴트 용도가 일부 있는 `Px_PanelDark`·`Px_Btn` 은 색이 그림에 들어가므로 코드가 `Image.color` 로 곱하는 화면에서 색이 변할 수 있다. 파일럿 승인 전에 해당 화면(영웅선택·데일리·편성·탑·전투 HUD)을 스크린샷 비교해 곱 결과가 허용 범위인지 확인한다(문제 시 해당 파일만 회색조로 재생성).
- 현재 크기 미측정이라 4배 캔버스·보더 비율은 Phase 0 실측 후 확정한다.
