import { clamp, smooth, ease, rand, TAU, WHITE, band } from "../../core/index.js";
import { pal, sheen } from "./_lib.js";

export const meta = {
  name: "Berserker crushing leap",
  desc: "A landing slam: white flash, hard orange shock ring, glowing cracks tearing out across the floor, rubble flung outward and a ring of dust.",
  tags: ["slam", "ground", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.833,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#2a0e08", "#7a2410", "#d4501a", "#ff9a3a", "#fff0c0"], reach: 45, seed: 12 },
};

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed * 7;
  const k = clamp(t / 0.28), r = R * ease.outExpo(k), fade = 1 - smooth(0.45, 0.83, t);
  if (r < 1) return;
  sheen(p, r, (d) => [d > 0.7 ? C[2] : C[1], (0.1 + 0.34 * d * d) * fade]);
  band(p, 0, 0, r, 1, Math.max(1.5, 4.5 * (1 - k * 0.55)), fade, [C[1], C[2], C[3], C[4]], () => true, t < 0.2);

  //# cracks: jagged polylines that race out, glowing then darkening
  for (let i = 0; i < 9; i++) {
    const a = (i / 9) * TAU + rand(i, 1, S) * 0.4, reach = r * (0.5 + 0.5 * rand(i, 2, S)), pts = [[0, 0]];
    let ang = a;
    for (let s = 1; s <= 6; s++) {
      ang += (rand(i, 10 + s, S) - 0.5) * 0.7;
      const d = (reach * s) / 6;
      pts.push([Math.cos(ang) * d, Math.sin(ang) * d]);
    }
    const glow = 1 - smooth(0.15, 0.7, t);
    p.polyline(pts, glow > 0.5 ? C[3] : C[1], fade > 0.3 ? 1 : fade * 3);
    if (glow > 0.3) p.light(pts[4][0], pts[4][1], C[3], 0.3 * glow);
  }

  //# rubble thrown outward
  for (let i = 0; i < 14; i++) {
    const ki = clamp((t - 0.04) / 0.5), a = rand(i, 3, S) * TAU, d = R * (0.3 + 0.75 * rand(i, 4, S)) * ease.outCubic(ki);
    if (ki <= 0 || ki >= 1) continue;
    const x = Math.cos(a) * d, y = Math.sin(a) * d;
    p.px(x, y, ki < 0.5 ? C[4] : C[3]);
    if (rand(i, 5, S) < 0.5) p.px(x + 1, y, C[2]);
  }
  //# dust puffs riding the front
  for (let i = 0; i < 10; i++) {
    const a = (i / 10) * TAU + rand(i, 6, S) * 0.5, rr = r * (0.78 + 0.14 * rand(i, 7, S));
    p.disc(Math.cos(a) * rr, Math.sin(a) * rr, 3 + 5 * ease.outQuad(k) * (0.6 + rand(i, 8, S) * 0.6), C[3], 0.3 * fade);
  }
  if (t < 0.2) {
    const f = 1 - t / 0.2;
    p.circle(0, 0, 12 * (0.5 + 0.5 * f), f > 0.5 ? WHITE : C[3], 0.95);
    p.glow(0, 0, 32, C[4], 0.55 * f);
  }
}
