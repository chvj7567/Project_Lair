//# 씬 2D 분위기 시안 빌드 — scene-2d-atmosphere.src.html 의 @@sheet:<경로> 를 게임 스프라이트 base64 로 인라인.
//# 사용: node .mockups/build-scene-2d.js  →  .mockups/scene-2d-atmosphere.html (단일 파일, 서버 없이 열림)
const fs = require('fs');
const path = require('path');

const ROOT = __dirname;
const SPRITES = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites');
const src = fs.readFileSync(path.join(ROOT, 'scene-2d-atmosphere.src.html'), 'utf8');

const out = src.replace(/@@sheet:([A-Za-z0-9_\/]+\.png)/g, (m, rel) => {
  const file = path.join(SPRITES, rel);
  const b64 = fs.readFileSync(file).toString('base64');
  return 'data:image/png;base64,' + b64;
});

fs.writeFileSync(path.join(ROOT, 'scene-2d-atmosphere.html'), out);
console.log('scene-2d-atmosphere.html', (out.length / 1024).toFixed(0) + 'KB');
