import { TAU, WHITE, band, rand } from "../../core/index.js";
import { pal, sheen, star } from "./_lib.js";

export const meta = {
  name: "Paladin sanctuary",
  desc: "A consecrated circle of gold: double rim with tick marks, a slowly turning hexagram of light, soft glow breathing inside and glints twinkling, seamless.",
  tags: ["holy", "loop", "aoe", "ground"],
  size: [96, 96],
  origin: [48, 48],
  duration: 2,
  loop: true,
  fps: 12,
  bg: "ground",
  options: { palette: ["#3a2a08", "#a8761a", "#f2b830", "#ffe27a", "#fffbe8"], reach: 45, seed: 19 },
};

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed, ph = t / meta.duration;
  const breath = 0.5 + 0.5 * Math.sin(ph * TAU);
  sheen(p, R, (d) => [d > 0.7 ? C[3] : C[2], 0.07 + 0.12 * d * d + 0.07 * breath * (1 - d)]);
  band(p, 0, 0, R, 1, 2.5, 1, [C[1], C[2], C[3], C[4]], () => true, false);
  band(p, 0, 0, R - 6, 1, 1, 0.8, [C[1], C[2], C[3], C[3]], () => true, false);
  //# tick marks between the rims, turning one twelfth of a turn per loop (seamless: 12 ticks)
  for (let i = 0; i < 12; i++) {
    const a = ph * (TAU / 12) + (i / 12) * TAU;
    p.line(Math.cos(a) * (R - 5), Math.sin(a) * (R - 5), Math.cos(a) * (R - 1), Math.sin(a) * (R - 1), i % 3 === 0 ? WHITE : C[4], 1);
  }
  //# hexagram: two triangles, turning a sixth of a turn per loop (seamless)
  const base = -ph * (TAU / 6), rr = R * 0.72;
  for (let k = 0; k < 2; k++) {
    for (let i = 0; i < 3; i++) {
      const a0 = base + k * (TAU / 6) + (i / 3) * TAU, a1 = base + k * (TAU / 6) + ((i + 1) / 3) * TAU;
      p.line(Math.cos(a0) * rr, Math.sin(a0) * rr, Math.cos(a1) * rr, Math.sin(a1) * rr, C[3], 0.85);
    }
  }
  for (let i = 0; i < 6; i++) {
    const a = base + (i / 6) * TAU;
    p.px(Math.cos(a) * rr, Math.sin(a) * rr, WHITE);
    p.light(Math.cos(a) * rr, Math.sin(a) * rr, C[3], 0.3);
  }
  p.glow(0, 0, R * 0.55, C[3], 0.12 + 0.1 * breath);
  //# glints twinkling on a cycle
  for (let i = 0; i < 9; i++) {
    const u = (ph + rand(i, 1, S)) % 1, a = rand(i, 2, S) * TAU, d = R * (0.2 + 0.55 * rand(i, 3, S));
    const s = Math.sin(u * Math.PI);
    if (s > 0.35) star(p, Math.cos(a) * d, Math.sin(a) * d, s > 0.8 ? 2 : 1, C[4], WHITE);
  }
}
