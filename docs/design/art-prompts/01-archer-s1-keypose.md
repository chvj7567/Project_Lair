# 단계 1-① 궁수 S1 — 스타일 확정용 키포즈 (가장 먼저)

목적: 전체 시트를 그리기 전에 "이 캐릭터 얼굴·비율·색"을 한 장으로 승인받는다. 승인된 이미지는 이후 모든 궁수 프롬프트의 참조 이미지가 된다.
첨부: ① 레퍼런스 스크린샷(치비 도트 RPG) ② `.mockups/art-style-v2.html` 스크린샷

## 프롬프트 ①-A (복붙)

```
STYLE (apply to everything):
Chibi high-density pixel art for a mobile RPG. 2.5-head-tall proportion, head about 40-45% of body height.
Thick dark 2px outline (dark navy/brown, NOT pure black). 3-tone cel shading plus one highlight, light from top-left.
High-saturation palette, max 48 colors per character. Crisp pixels: no anti-aliasing, no dithering, no blur, no gradients, no glow.
Top-down 2.5D game camera, character seen from slightly above, facing RIGHT.
NO text, NO labels, NO grid lines, NO frame numbers, NO cast shadow on the ground.
Background: flat solid pure magenta #FF00FF only (never use magenta inside the character).
Attach the reference images: use them for style, proportion and outline thickness only, do not copy their characters.

TASK:
Character: a novice adventurer ARCHER (the weakest tier, plain gear): green hooded cloak, brown leather vest, simple wooden short bow, small quiver on the back, brown boots. No metal armor, no ornaments.
Make ONE character design sheet on a 1024x1024 canvas, 4 poses in a 2x2 layout, same size and same baseline in each:
 1) idle, bow held low in left hand
 2) bow drawn, arrow nocked, about to release (side view, facing right)
 3) after release, bow arm extended
 4) hit reaction (leaning back, eyes shut)
Each pose about 400 px tall, centered in its own 512x512 quadrant, at least 40 px margin from the quadrant edge.
Character body (feet to head top) uses the same pixel size in all poses. Keep the face simple: two dot eyes, no mouth detail.
```

## 승인 체크 (제가 확인)
- 외곽선 2px 일정, 머리 비율 40~45%, 색 48개 이하, 마젠타 침범 없음
- 4포즈의 크기·발 위치 동일, 잘림 없음

## 다음 순서
승인 → ①-B 애니메이션 프레임(idle 8 → walk 8 → run 8 → 공격 9×3 → hit 6 → death 8 → spawn 16, 한 번에 4~6프레임씩) → mask/anchor/Tier.json
