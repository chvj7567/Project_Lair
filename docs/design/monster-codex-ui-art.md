# 몬스터 도감 개편 - UI 스프라이트 목록 (GPT 이미지 생성용)

> 상위 기획서: `docs/design/monster-codex.md` · 승인 목업: `.mockups/monster-codex.html`
> 목적: 이번 개편에서 **새로 만들어야 하는 UI 스프라이트만** 골라 GPT 이미지 생성 지시서로 정리한다. 기존 에셋으로 충분한 것은 "재사용"으로 표시해 생성하지 않는다.
> 범위: UI 스프라이트(프레임·글로우·스파클)만. 몬스터 몸 시트·등급 이펙트 시트는 신규 제작 금지(CLAUDE.md §8 도감 개편 항목, 기존 시트 재사용).

## 1. 핵심 판단 - 등급색 프레임은 "5장"이 아니라 "1장 + 틴트"

목업의 등급색(풋내기 `#9aa5bd` / 견습 `#5aa9ff` / 숙련 `#b07cff` / 정예 `#f7c64a` / 군주 `#ff7a45`)과 미해금 테두리(`#232834`)는 **회색조 프레임 1장을 런타임에 `Image.color` 로 틴트**해 만든다. 색별 5+1장을 따로 생성하지 않는다(생성 비용·색 불일치 방지). 해금 연출의 테두리 펄스·링·스파클 색도 같은 틴트 값(`Grade_CodexUnlock.colorHex`)을 쓴다.
단, 틴트가 먹으려면 프레임 스프라이트가 **흰색~밝은 회색 + 검정 외곽선**으로만 그려져야 한다(프롬프트에 명시).

## 2. 현행 도감 UI 화풍 (참고 위치)

| 항목 | 값·위치 |
|---|---|
| 기존 도트 UI 스프라이트 폴더 | `Assets/_Lair/Art/Sprites/UiDot/` (`Px_*`, Point 필터, 압축 없음, `alphaIsTransparency` 켬, PPU 25) |
| 도감 셀이 쓰는 스프라이트 | `CodexCell` 의 `_normalSprite` = `Px_Panel`(9-slice 보더 3), `_dummySprite` = `Px_PanelDark`(보더 3) - 프리팹 `Assets/_Lair/Art/UI/CodexCell.prefab`, 팝업 `CodexPopup.prefab` |
| 금테 프레임·모서리·그라데이션 | `Assets/_Lair/Art/Sprites/WorldMapFrame.png`, `WorldMapCornerGold.png`(28x28), `WorldMapGradientH/V.png`, `BattleHudSmallFrame.png` - `docs/design/build-modal-visual-polish.md` 의 모달 규격 |
| 색·톤 | 어두운 남색 석판 + 검정 2~3px 외곽선 + 얇은 금/청회색 하이라이트. 팔레트: 외곽선 `#07090e`, 베벨 `#3a4764`, 하이라이트 `#6a7aa0`, 패널 `#1a2132`→`#121827`, 금 `#f7c64a` |
| 로드 방식 | 도감 스프라이트는 Addressable 키가 아니라 **인스펙터 직접 참조**(`CodexPopup` 의 아이콘 관례) - 별도 Enum 불필요. 파일명은 `Px_` 접두어 + 파스칼케이스 |

공통 후처리(생성 후 사람이 수행): GPT 출력물은 픽셀 격자가 정확하지 않으므로 ① 지정 크기로 **Nearest Neighbor** 축소 ② 위 팔레트로 색 스냅 ③ 투명 배경 확인 ④ 어두운 배경(`#10151f`)과 밝은 배경 양쪽에서 가장자리 번짐 확인. 이 문서의 "픽셀 크기"는 최종 파일 크기이고, GPT 에는 4배(예: 24 -> 96)로 생성시킨 뒤 축소한다.

## 3. 전체 목록 (재사용 vs 신규)

| # | 용도 | 판정 | 에셋 |
|---|---|---|---|
| 1 | 칸 프레임 - 풋내기·견습·숙련·정예·군주 5색 + 미해금 | **신규 1장(틴트)** | `Px_CodexCellFrame` |
| 2 | 칸 배경(해금 / 미해금) | 재사용 | `Px_Panel` / `Px_PanelDark` (기존 `CodexCell` 값) |
| 3 | 등급 행 라벨 배경(70x83) | 재사용 | `Px_PanelDark` + 글자색 등급색 (필요 시 `Px_CodexCellFrame` 틴트를 위에 얹음) |
| 4 | 티어 열 헤더(106x24) | 재사용 | `Px_PanelSunk`(보더 2) |
| 5 | 등급 별 아이콘 | 재사용 | `Px_Star` (금색, 미해금은 `#59617a` 틴트) |
| 6 | 매트릭스 패널·상세 패널 프레임 | 재사용 | `Px_Panel` 바탕 + `WorldMapFrame`(금테 이중, Sliced, fillCenter 끔) |
| 7 | 종족 칩 배경(200x40) | 재사용 | `Px_Btn`(기본) / `Px_BtnGold`(선택) + 왼쪽 8px 띠는 `Px_Solid` 종족색 틴트 |
| 8 | NEW 배지(24x14) | 재사용 | `Px_Badge_Gold`(보더 2) + 글자 "NEW" |
| 9 | 선택 칸 금색 외곽선 | 재사용 | `Px_CardRing`(보더 3) 금색 틴트 |
| 10 | 진척 바 | 재사용 | `Px_BarBg` / `Px_BarFill` |
| 11 | 상세 미리보기 배경 | 재사용 | `Px_PanelSunk` + 아래 #12 글로우 |
| 12 | 칸·상세 몸 뒤 은은한 후광 바탕(종족색 틴트) | **신규** | `Px_CodexGlow` |
| 13 | 해금 연출 링 | 재사용 | `Px_Ring`(종족/등급색 틴트, 확대 애니) |
| 14 | 해금 연출 스파클 | **신규** | `Px_CodexSpark` (`Px_Dot` 로 대체 가능 - 아래 참고) |
| 15 | 해금 연출 테두리 펄스 | 재사용 | #1 `Px_CodexCellFrame` 를 같은 자리에 겹쳐 알파·스케일 애니 |
| 16 | 미해금 몸 실루엣 | 에셋 없음 | 몸 Image 에 검정 틴트(런타임). 옅은 외곽선은 #12 와 별개로 셰이더/틴트 처리 |

생성 대상은 **신규 3장**(#1, #12, #14)뿐이다. 모든 프롬프트는 영문이며, GPT 이미지 생성(투명 배경 PNG 요청 가능한 모델/모드)을 전제로 한다.

## 4. 신규 스프라이트 상세

### 4.1 `Px_CodexCellFrame` (#1, 매트릭스 칸·티어 미니 칸·상세 미리보기 테두리 공용)

| 항목 | 값 |
|---|---|
| 경로 | `Assets/_Lair/Art/Sprites/UiDot/Px_CodexCellFrame.png` |
| 최종 크기 | 24x24 px (GPT 생성은 96x96) |
| 9-slice | 예 - 보더 left/top/right/bottom = 6 (바깥 검정 2px + 안쪽 밝은 라인 1px + 베벨 3px). `Image Type = Sliced`, **fillCenter 끔**(안은 완전 투명) |
| 투명 PNG | 예(바깥 모서리 바깥과 안쪽 중앙 모두 투명) |
| 색 | 회색조 전용: 검정 외곽선 `#07090e` + 흰색~밝은 회색(`#ffffff`~`#c8ccd8`) 하이라이트·베벨. 컬러 금지(런타임 틴트용) |
| 임포트 | Sprite(2D and UI), Single, PPU 25, Filter Point, Compression None, Mesh Type Full Rect, alphaIsTransparency 켬 |
| 사용 | 칸 98x83(풀링 항목 균일 크기, `monster-codex.md` §2) / 티어 미니 칸 76x72 / 미리보기 160x160 에 Sliced 로 늘려 사용. 색 = 등급색(해금) 또는 `#232834`(미해금) |

GPT 프롬프트:
```
A square 9-slice UI frame sprite for a dark fantasy pixel-art game, front view, flat 2D. Hollow rectangular border only, the center and everything outside the border are fully transparent. Chunky pixel-art style, hard pixel edges, no anti-aliasing, no gradients, no blur. The border is made of: a 2-pixel solid near-black outline (#07090e) on the outermost edge, then a 1-pixel bright white inner highlight line, then a 3-pixel light-grey beveled stone edge (#c8ccd8) with a slightly darker bottom/right side (#8e98ad) for a carved-stone-slab look. Plain squared corners with a tiny 1-pixel notch at each corner, no ornaments, no gold, no color. Pure grayscale/white tones only so it can be tinted in-engine. Symmetrical, centered, 96x96 canvas, transparent PNG background.
```
부정 프롬프트: `no text, no color, no gold, no drop shadow, no glow, no background, no gradient, no anti-aliasing, no rounded blur edges`.

### 4.2 `Px_CodexGlow` (#12, 몸 뒤 후광 바탕)

| 항목 | 값 |
|---|---|
| 경로 | `Assets/_Lair/Art/Sprites/UiDot/Px_CodexGlow.png` |
| 최종 크기 | 64x64 px (GPT 생성은 256x256) |
| 9-slice | 아니오(보더 0, Simple) |
| 투명 PNG | 예(가장자리로 갈수록 알파 0으로 사라지는 원형) |
| 색 | 흰색 단색 + 알파 그라데이션(런타임에 종족색 틴트, 알파 0.27 근처로 사용) |
| 임포트 | PPU 25, Filter **Bilinear**(이 한 장만 부드러운 그라데이션이라 Point 금지), Compression None, alphaIsTransparency 켬 |
| 사용 | 칸 몸 뒤 52px 원형 바탕, 상세 미리보기 뒤 160px 바탕. 이펙트(오라·후광)와는 별개의 정적 배경 |

GPT 프롬프트:
```
A soft radial glow sprite on a fully transparent background: a perfectly circular pure white light that is brightest at the center and fades smoothly to completely transparent (alpha 0) at the circle's edge, with a quadratic falloff. No hard edge, no ring, no sparkles, no texture, no color tint, no noise. Single centered circle filling the canvas, 256x256, transparent PNG.
```
부정 프롬프트: `no color, no star shape, no outline, no hard edge, no background fill`.

### 4.3 `Px_CodexSpark` (#14, 해금 스파클)

| 항목 | 값 |
|---|---|
| 경로 | `Assets/_Lair/Art/Sprites/UiDot/Px_CodexSpark.png` |
| 최종 크기 | 8x8 px (GPT 생성은 64x64) |
| 9-slice | 아니오 |
| 투명 PNG | 예 |
| 색 | 흰색 단색(런타임에 등급색 틴트) |
| 임포트 | PPU 25, Filter Point, Compression None, alphaIsTransparency 켬 |
| 사용 | 해금 연출 스파클 입자(등급별 4~18개, 칸에서는 최대 8개). 기존 `Px_Dot` 로 대체 가능하나 4방향 반짝임 모양이 있으면 해금 느낌이 더 뚜렷함 |

GPT 프롬프트:
```
A tiny 4-point sparkle star sprite in chunky pixel-art style on a transparent background: a plus-shaped twinkle made of a 1-pixel-wide vertical and horizontal cross with a brighter 2x2 pixel core at the center, arms tapering by one pixel at the tips. Pure white only (so it can be tinted in-engine), hard pixel edges, no anti-aliasing, no glow, no gradient. Centered, symmetrical, 64x64 canvas (to be downscaled to 8x8 with nearest-neighbor), transparent PNG.
```
부정 프롬프트: `no color, no blur, no halo, no background, no outline`.

## 5. 적용 규칙·검수

| 항목 | 규칙 |
|---|---|
| 등급색 틴트 | `Grade_CodexUnlock.colorHex` 5값 + 미해금 `#232834` 를 `Px_CodexCellFrame` 에 적용. 틴트 후 어두운 칸 배경(`#10151f`)에서 풋내기 회색(`#9aa5bd`)과 미해금 회색(`#232834`)이 구분되는지 육안 확인 |
| 파일·참조 | 3개 파일을 `UiDot/` 에 추가하고 `CodexMonsterCell.prefab`·`CodexPopup.prefab` 이 인스펙터로 참조. 새 Addressable 키·Enum 없음 |
| 기존 불변 | 재사용 스프라이트(`Px_*`, `WorldMap*`)는 수정·리네임·삭제 금지(다른 화면이 참조) |
| 이펙트 시트 | 등급 이펙트(오라·`_g4`·`_g5`) 시트는 이 문서 범위 밖(기존 18장 재사용) |
| 검수 | 사용자 육안 확인(목업 `.mockups/monster-codex.html` 과 칸 비교). 통과 못 하면 프롬프트의 두께·베벨 서술만 조정해 재생성 |

## 6. 결정 필요

- #14 스파클은 생성 없이 `Px_Dot` 으로 대체해도 기능상 동일하다. **추천: 생성(8x8 한 장이라 비용이 작고 해금 연출 품질이 오름).** 대체하려면 #14 만 생략.
