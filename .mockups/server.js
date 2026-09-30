//# 공용 정적 서버 — .mockups 폴더의 HTML 시안을 로컬에서 미리보기 위한 용도.
//# 사용: node .mockups/server.js  →  http://localhost:8777/
//# 이 환경의 python 은 Microsoft Store 스텁이라 동작하지 않으므로 node 를 사용한다.
//# /cardart/<Name>.png — 시안 참고용으로 게임의 카드 일러스트(Assets/_Lair/Art/Sprites/CardArt)를 읽기 전용 서빙(복사본 없음).
const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 8777;
const ROOT = __dirname;
const CARD_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'CardArt');
//# POST /export/<Name> — 영웅 시안 시트 추출 저장(기획서 hero-2d-conversion §8 (e)). 이 폴더·이 파일명만 쓰기 허용.
const EXPORT_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'Heroes2D');
const EXPORT_NAMES = new Set([
  'Knight_Sheet.png', 'Knight_Sheet_Emission.png',
  'Knight_Sheet_S2.png', 'Knight_Sheet_S3.png', 'Knight_Sheet_S4.png', 'Knight_Sheet_S5.png',
  'Knight_Sheet_S2_Emission.png', 'Knight_Sheet_S3_Emission.png', 'Knight_Sheet_S4_Emission.png', 'Knight_Sheet_S5_Emission.png',
  'HeroGroundShadow.png', 'Knight_SheetSpec.json',
]);
//# POST /export/<Name> — FX 도트 시트(fx-dot-pixel.html). Art/Sprites/FX2D 로만 저장, 파일명 화이트리스트.
const FX_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'FX2D');
const FX_NAMES = new Set([
  'DamagePopup_Sheet.png', 'PoisonAura_Sheet.png', 'TimeStopShield_Sheet.png', 'FearSkull_Sheet.png',
  'HeroDashConeFx_Sheet.png', 'HeroNovaFx_Sheet.png', 'HeroOrbitBladeFx_Sheet.png', 'FX2D_SheetSpec.json',
]);
//# POST /export/<Name> — 씬 2D 도트 배경·장식(scene-2d-atmosphere.html, 기획서 scene-2d-conversion §2.2·§2.4). Art/Sprites/Backdrop2D 로만 저장.
const BACKDROP_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'Backdrop2D');
const BACKDROP_NAMES = new Set([
  'Battle_Backdrop.png', 'Flame_Soul8_Sheet.png',
  'Village_Backdrop.png', 'Village_CauldronGlow.png', 'Village_CauldronBubbles_Sheet.png', 'Village_CrownSparkle.png',
  'Village_ProjectorRings.png', 'Village_HoloScan_Sheet.png',
  'Flame_Warm7_Sheet.png', 'Flame_Warm3_Sheet.png', 'Flame_Warm3w1_Sheet.png', 'Shadow_R6.png', 'Shadow_R11.png',
  'Loading_Backdrop.png', 'Loading_DoorGlow.png', 'Loading_CircleRings.png', 'Loading_AxisRing.png',
  'Flame_Soul10_Sheet.png', 'Shadow_R7.png',
  'Dot_1x1.png', 'Dot_2x1.png', 'Light2D_Falloff.png',
  'Backdrop2D_SheetSpec.json',
]);
//# POST /export/<Name> — 같은 시안의 UI 도트 스프라이트 4장. Art/Sprites/UiDot 로만 저장.
const UIDOT_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'UiDot');
const UIDOT_NAMES = new Set([
  'BossBar_Fill.png', 'BossBar_TickGem.png', 'Loading_BarFrame.png', 'Loading_BarFill.png',
]);
const EXPORT_MAX_BYTES = 20 * 1024 * 1024;

function exportDirOf(name) {
  if (EXPORT_NAMES.has(name)) return EXPORT_DIR;
  if (FX_NAMES.has(name)) return FX_DIR;
  if (BACKDROP_NAMES.has(name)) return BACKDROP_DIR;
  if (UIDOT_NAMES.has(name)) return UIDOT_DIR;
  return null;
}

function handleExport(req, res, name) {
  const dir = exportDirOf(name);
  if (dir === null) {
    res.writeHead(403, { 'Content-Type': 'text/plain; charset=utf-8' });
    res.end('Forbidden name: ' + name);
    return;
  }
  const chunks = [];
  let size = 0;
  let aborted = false;
  req.on('data', (c) => {
    size += c.length;
    if (size > EXPORT_MAX_BYTES) {
      aborted = true;
      res.writeHead(413);
      res.end('Too large');
      req.destroy();
      return;
    }
    chunks.push(c);
  });
  req.on('end', () => {
    if (aborted) return;
    fs.mkdir(dir, { recursive: true }, (mkErr) => {
      if (mkErr) {
        res.writeHead(500);
        res.end('mkdir failed');
        return;
      }
      const target = path.join(dir, name);
      fs.writeFile(target, Buffer.concat(chunks), (wErr) => {
        if (wErr) {
          res.writeHead(500);
          res.end('write failed');
          return;
        }
        console.log('exported: ' + target + ' (' + size + ' bytes)');
        res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({ ok: true, name: name, bytes: size }));
      });
    });
  });
}

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.svg': 'image/svg+xml',
};

function send(res, filePath) {
  fs.readFile(filePath, (err, data) => {
    if (err) {
      res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Not found');
      return;
    }
    const ext = path.extname(filePath).toLowerCase();
    res.writeHead(200, { 'Content-Type': MIME[ext] || 'application/octet-stream' });
    res.end(data);
  });
}

const server = http.createServer((req, res) => {
  let reqPath = decodeURIComponent(req.url.split('?')[0]);

  if (req.method === 'POST') {
    if (reqPath.startsWith('/export/')) {
      handleExport(req, res, reqPath.slice('/export/'.length));
      return;
    }
    res.writeHead(405);
    res.end('Method not allowed');
    return;
  }

  if (reqPath.startsWith('/cardart/')) {
    const name = reqPath.slice('/cardart/'.length);
    if (/^[A-Za-z0-9_]+\.png$/.test(name) === false) {
      res.writeHead(403);
      res.end('Forbidden');
      return;
    }
    send(res, path.join(CARD_DIR, name));
    return;
  }

  if (reqPath === '/') reqPath = '/index.html';
  const filePath = path.join(ROOT, reqPath);
  //# 루트 밖 접근 차단
  if (filePath.startsWith(ROOT) === false) {
    res.writeHead(403);
    res.end('Forbidden');
    return;
  }
  send(res, filePath);
});

server.listen(PORT, () => {
  console.log('mockup server: http://localhost:' + PORT + '/');
  console.log('serving: ' + ROOT + ' (+ /cardart/ -> ' + CARD_DIR + ')');
});
