//# ui-redesign.src.html → ui-redesign.html 빌드. @@<Sprites 하위경로>.png 는 data URI 로, @@glyph:<name> 은 도트 SVG 로 치환한다.
//# 사용: node .mockups/build-ui-redesign.js  (게임 스프라이트는 읽기만 하고 복사하지 않는다)
const fs = require('fs');
const path = require('path');

const ROOT = __dirname;
const SPRITES = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites');

//# 12x12 도트 아이콘 — 문자: . 투명 / k 윤곽 / 그 외 팔레트
const PAL = { k: '#07090e', b: '#e8e1cf', s: '#b9b09a', g: '#f7c64a', o: '#b9832a', t: '#5ef0b4', d: '#1f9e74', r: '#e5484d', p: '#b58cff', w: '#8e98ad' };
const GLYPHS = {
  castle: [
    'k.k.kkkk.k.k',
    'kbkbkbbkbkbk',
    'kbbbkbbkbbbk',
    'kkbbbbbbbbkk',
    '.kbbbbbbbbk.',
    '.kbkkbbkkbk.',
    '.kbkkbbkkbk.',
    '.kbbbbbbbbk.',
    '.kbbbkkbbbk.',
    '.kbbkggkbbk.',
    '.kbbkggkbbk.',
    '.kkkkkkkkkk.',
  ],
  shop: [
    '....kkkk....',
    '...kgggok...',
    '..kggkkgok..',
    '..kgk..kgk..',
    '.kkkkkkkkkk.',
    '.kgggggggok.',
    'kgggkkggggok',
    'kggkggkgggok',
    'kgggggkggook',
    'kggkkkggoook',
    '.kgggggoook.',
    '..kkkkkkkk..',
  ],
  book: [
    '.kkkkkkkkkk.',
    'kppppkppppkk',
    'kppppkppppks',
    'kpbbpkpbbpks',
    'kppppkppppks',
    'kpbbpkpbbpks',
    'kppppkppppks',
    'kppppkppppks',
    'kpppkkkpppks',
    'kkkkksskkkks',
    '.ssssssssss.',
    '.kkkkkkkkkk.',
  ],
  scroll: [
    '.kkkkkkkkk..',
    'kbbbbbbbbbk.',
    'kskkkkkkkbk.',
    '.kbbbbbbbbk.',
    '.kbkkkkkbbk.',
    '.kbbbbbbbbk.',
    '.kbkkkkbbbk.',
    '.kbbbbbbbbk.',
    '.kbkkkbrrbk.',
    '.kbbbbrrrbk.',
    'kbbbbbbrbbbk',
    '.kkkkkkkkkk.',
  ],
  tomb: [
    '...kkkkkk...',
    '..kwwwwwwk..',
    '.kwwwkkwwwk.',
    '.kwwkkkkwwk.',
    '.kwwwkkwwwk.',
    '.kwwwkkwwwk.',
    '.kwwwwwwwwk.',
    '.kwkkkkkkwk.',
    '.kwwwwwwwwk.',
    '.kwwwwwwwwk.',
    'kkkkkkkkkkkk',
    'kddttddttddk',
  ],
  crown: [
    '............',
    'k....kk....k',
    'kk..kgok..kk',
    'kgk.kgok.kgk',
    'kgokggookgok',
    'kggggoggggok',
    'kgrggogtggok',
    'kggggoggggok',
    'kkkkkkkkkkkk',
    'kgggggggoook',
    'kkkkkkkkkkkk',
    '............',
  ],
  shield: [
    'kkkkkkkkkkkk',
    'kbbbbbbbbbsk',
    'kbwwwwwwwwsk',
    'kbwwwbbwwwsk',
    'kbwwwbbwwwsk',
    'kbwbbbbbbwsk',
    'kbwwwbbwwwsk',
    '.kbwwbbwwsk.',
    '.kbwwwwwwsk.',
    '..kbwwwwsk..',
    '...kbwwsk...',
    '....kkkk....',
  ],
  sword: [
    '.........kkk',
    '........kbbk',
    '.......kbbsk',
    '......kbbsk.',
    '.....kbbsk..',
    '.k..kbbsk...',
    'kgk.kbsk....',
    '.kgkbsk.....',
    '..kgkk......',
    '.kokgk......',
    'kok.kgk.....',
    'kk...k......',
  ],
  swarm: [
    '..kkk.......',
    '.kttdk..kkk.',
    '.ktkdk.kttdk',
    '.kttdk.ktkdk',
    '..kkk..kttdk',
    '.......kkkk.',
    '...kkk......',
    '..kttdk.kkk.',
    '..ktkdkkttdk',
    '..kttdkktkdk',
    '..kdkdkkttdk',
    '...k.k..kkk.',
  ],
  curse: [
    '...kkkkkk...',
    '..kppppppk..',
    '.kppppppppk.',
    '.kpkkppkkpk.',
    '.kpkbppkbpk.',
    '.kppppppppk.',
    '..kpppkppk..',
    '...kpppppk..',
    '...kpkpkpk..',
    '....kkkkk...',
    '.k........k.',
    'kpk......kpk',
  ],
};

function glyphSvg(name) {
  const rows = GLYPHS[name];
  if (rows === undefined)
    throw new Error('unknown glyph ' + name);
  let rects = '';
  rows.forEach((row, y) => {
    [...row].forEach((ch, x) => {
      if (ch === '.')
        return;
      rects += `<rect x="${x}" y="${y}" width="1" height="1" fill="${PAL[ch]}"/>`;
    });
  });
  return `<svg viewBox="0 0 12 12" width="100%" height="100%" shape-rendering="crispEdges" xmlns="http://www.w3.org/2000/svg">${rects}</svg>`;
}

const cache = new Map();
function dataUri(rel) {
  if (cache.has(rel))
    return cache.get(rel);
  const buf = fs.readFileSync(path.join(SPRITES, rel));
  const uri = 'data:image/png;base64,' + buf.toString('base64');
  cache.set(rel, uri);
  return uri;
}

let html = fs.readFileSync(path.join(ROOT, 'ui-redesign.src.html'), 'utf8');
html = html.replace(/@@glyph:([a-z]+)/g, (_, n) => glyphSvg(n));
html = html.replace(/@@([A-Za-z0-9_\/]+\.png)/g, (_, rel) => dataUri(rel));
fs.writeFileSync(path.join(ROOT, 'ui-redesign.html'), html);
console.log('ui-redesign.html', (html.length / 1024).toFixed(0) + 'KB', 'sprites:', cache.size);
