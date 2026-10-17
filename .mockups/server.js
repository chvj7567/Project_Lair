//# 怨듭슜 ?뺤쟻 ?쒕쾭 ??.mockups ?대뜑??HTML ?쒖븞??濡쒖뺄?먯꽌 誘몃━蹂닿린 ?꾪븳 ?⑸룄.
//# ?ъ슜: node .mockups/server.js  ?? http://localhost:8777/
//# ???섍꼍??python ? Microsoft Store ?ㅽ뀅?대씪 ?숈옉?섏? ?딆쑝誘濡?node 瑜??ъ슜?쒕떎.
//# /cardart/<Name>.png ???쒖븞 李멸퀬?⑹쑝濡?寃뚯엫??移대뱶 ?쇰윭?ㅽ듃(Assets/_Lair/Art/Sprites/CardArt)瑜??쎄린 ?꾩슜 ?쒕튃(蹂듭궗蹂??놁쓬).
const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 8777;
const ROOT = __dirname;
const CARD_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'CardArt');
//# POST /export/<Name> ???곸썒 ?쒖븞 ?쒗듃 異붿텧 ???湲고쉷??hero-2d-conversion 짠8 (e)). ???대뜑쨌???뚯씪紐낅쭔 ?곌린 ?덉슜.
const EXPORT_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'Heroes2D');
const EXPORT_NAMES = new Set([
  'Knight_Sheet.png', 'Knight_Sheet_Emission.png',
  'Knight_Sheet_S2.png', 'Knight_Sheet_S3.png', 'Knight_Sheet_S4.png', 'Knight_Sheet_S5.png',
  'Knight_Sheet_S2_Emission.png', 'Knight_Sheet_S3_Emission.png', 'Knight_Sheet_S4_Emission.png', 'Knight_Sheet_S5_Emission.png',
  'HeroGroundShadow.png', 'Knight_SheetSpec.json',
]);
//# POST /export/<Name> ??FX ?꾪듃 ?쒗듃(fx-dot-pixel.html). Art/Sprites/FX2D 濡쒕쭔 ??? ?뚯씪紐??붿씠?몃━?ㅽ듃.
const FX_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'FX2D');
const FX_NAMES = new Set([
  'DamagePopup_Sheet.png', 'PoisonAura_Sheet.png', 'TimeStopShield_Sheet.png', 'FearSkull_Sheet.png',
  'HeroDashConeFx_Sheet.png', 'HeroNovaFx_Sheet.png', 'HeroOrbitBladeFx_Sheet.png', 'FX2D_SheetSpec.json',
]);
//# POST /export/<Name> ????2D ?꾪듃 諛곌꼍쨌?μ떇(scene-2d-atmosphere.html, 湲고쉷??scene-2d-conversion 짠2.2쨌짠2.4). Art/Sprites/Backdrop2D 濡쒕쭔 ???
const BACKDROP_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'Backdrop2D');
const BACKDROP_NAMES = new Set([
  'Battle_Backdrop.png', 'Battle_Backdrop_DarkForest.png', 'Battle_Backdrop_ShadowCitadel.png', 'Flame_Soul8_Sheet.png',
  'Altar2D_Base.png', 'Altar2D_RingDim.png', 'Altar2D_Ring.png', 'Altar2D_Crystal.png', 'Altar2D_CrystalCore.png',
  'Village_Backdrop.png', 'Village_ThroneHalo.png', 'Village_PotionGlow.png', 'Village_CarpetShimmer.png', 'Village_CauldronGlow.png', 'Village_CauldronBubbles_Sheet.png', 'Village_CrownSparkle.png',
  'Village_ProjectorRings.png', 'Village_HoloScan_Sheet.png',
  'Flame_Warm7_Sheet.png', 'Flame_Warm3_Sheet.png', 'Flame_Warm3w1_Sheet.png', 'Shadow_R6.png', 'Shadow_R11.png',
  'Loading_Backdrop.png', 'Loading_DoorGlow.png', 'Loading_CircleRings.png', 'Loading_AxisRing.png',
  'Flame_Soul10_Sheet.png', 'Shadow_R7.png',
  'Dot_1x1.png', 'Dot_2x1.png', 'Light2D_Falloff.png',
  'Backdrop2D_SheetSpec.json',
]);
//# POST /export/<Name> ??媛숈? ?쒖븞??UI ?꾪듃 ?ㅽ봽?쇱씠??4?? Art/Sprites/UiDot 濡쒕쭔 ???
const UIDOT_DIR = path.join(ROOT, '..', 'Assets', '_Lair', 'Art', 'Sprites', 'UiDot');
const UIDOT_NAMES = new Set([
  'BossBar_Fill.png', 'BossBar_TickGem.png', 'Loading_BarFrame.png', 'Loading_BarFill.png',
  'Px_Minus.png', 'Px_Plus.png',
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
  //# 猷⑦듃 諛??묎렐 李⑤떒
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
