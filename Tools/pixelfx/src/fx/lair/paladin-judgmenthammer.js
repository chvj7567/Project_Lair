import { clamp, smooth, ease, rand, TAU, WHITE, band } from "../../core/index.js";
import { pal, sheen, star } from "./_lib.js";

export const meta = {
  name: "Paladin judgment hammer",
  desc: "A golden target circle contracts on the mark, then the hammer lands: blinding flash, a ring of light, glowing rays and cracks, sparks falling away.",
  tags: ["holy", "slam", "ground", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 1,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#3a2a08", "#a8761a", "#f2b830", "#ffe27a", "#fffbe8"], reach: 45, seed: 14 },
};

const HIT = 0.33;

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed * 3;
  const fade = 1 - smooth(0.55, 1, t);
  if (fade <= 0) return;
  if (t < HIT) {
    //# telegraph: the circle contracts onto the mark with a cross inside
    const u = ease.inQuad(t / HIT), r = R * (1 - 0.55 * u);
    band(p, 0, 0, r, 1, 2, 0.6 + 0.4 * u, [C[1], C[2], C[3], C[4]], () => true, false);
    for (let i = 0; i < 4; i++) {
      const a = (i / 4) * TAU + u * 0.8;
      p.line(Math.cos(a) * (r - 6), Math.sin(a) * (r - 6), Math.cos(a) * (r + 2), Math.sin(a) * (r + 2), C[4], 0.9);
    }
    p.glow(0, 0, 20 * u + 6, C[3], 0.4 * u);
    return;
  }
  const a = t - HIT, k = clamp(a / 0.25), r = R * ease.outExpo(k);
  sheen(p, r, (d) => [d > 0.6 ? C[3] : C[2], (0.12 + 0.36 * d * d) * fade]);
  band(p, 0, 0, r, 1, Math.max(1.5, 4.5 * (1 - k * 0.5)), fade, [C[1], C[2], C[3], C[4]], () => true, a < 0.15);
  //# rays of light and glowing cracks
  for (let i = 0; i < 12; i++) {
    const ang = (i / 12) * TAU + rand(i, 1, S) * 0.1, len = r * (i % 2 ? 0.55 : 0.9), glow = 1 - smooth(0, 0.35, a);
    p.line(Math.cos(ang) * 4, Math.sin(ang) * 4, Math.cos(ang) * len, Math.sin(ang) * len, i % 2 ? C[3] : C[4], fade > 0.5 ? 1 : fade * 2);
    if (glow > 0.3 && i % 2 === 0) p.light(Math.cos(ang) * len * 0.7, Math.sin(ang) * len * 0.7, C[3], 0.25 * glow);
  }
  //# sparks and glints
  for (let i = 0; i < 12; i++) {
    const ki = clamp((a - rand(i, 2, S) * 0.1) / 0.5);
    if (ki <= 0 || ki >= 1) continue;
    const ang = rand(i, 3, S) * TAU, d = R * (0.2 + 0.7 * rand(i, 4, S)) * ease.outCubic(ki);
    if (i % 3 === 0 && ki < 0.7) star(p, Math.cos(ang) * d, Math.sin(ang) * d, 2, C[4], WHITE);
    else p.px(Math.cos(ang) * d, Math.sin(ang) * d, ki < 0.5 ? C[4] : C[3]);
  }
  if (a < 0.17) {
    const f = 1 - a / 0.17;
    p.circle(0, 0, 14 * (0.5 + 0.5 * f), f > 0.5 ? WHITE : C[4], 0.97);
    p.glow(0, 0, 38, C[4], 0.65 * f);
  }
}
