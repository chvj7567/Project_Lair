import { clamp, smooth, ease, rand, TAU, WHITE, band } from "../../core/index.js";
import { pal } from "./_lib.js";

export const meta = {
  name: "Assassin smoke bomb",
  desc: "A smoke bomb pops: a white flash, then a bank of grey-violet smoke swells to fill the circle, swirls slowly for three seconds and thins away.",
  tags: ["smoke", "aoe", "ground", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 3,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#15121c", "#3a3548", "#6a6280", "#a49cba", "#e6e0f4"], reach: 45, seed: 8 },
};

const N = 30;

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed * 3;
  const born = ease.outCubic(clamp(t / 0.5)), fade = 1 - smooth(2.2, 3, t);
  if (fade <= 0) return;
  //# faint area rim, so the zone reads through the smoke
  band(p, 0, 0, R * born, 1, 1.5, 0.55 * fade, [C[1], C[2], C[3], C[3]], () => true, false);
  //# puffs, drawn far to near by their y so they overlap like a bank
  const puffs = [];
  for (let i = 0; i < N; i++) {
    const a = rand(i, 1, S) * TAU, d = Math.sqrt(rand(i, 2, S)) * 0.7 * R, size = (7 + 8 * rand(i, 3, S));
    const sw = t * (0.5 + 0.4 * rand(i, 4, S)) * (rand(i, 5, S) < 0.5 ? 1 : -1);
    puffs.push({ x: Math.cos(a + sw * 0.35) * d * born + Math.sin(t * 1.3 + i) * 1.5, y: Math.sin(a + sw * 0.35) * d * born + Math.cos(t * 1.1 + i * 2) * 1.5, r: size * (0.35 + 0.65 * born) * (1 + 0.06 * Math.sin(t * 2 + i)) });
  }
  puffs.sort((a, b) => a.y - b.y);
  for (const q of puffs) {
    p.disc(q.x, q.y, q.r, C[1], 0.2 * fade);
    p.disc(q.x - q.r * 0.12, q.y - q.r * 0.14, q.r * 0.75, C[2], 0.16 * fade);
    p.disc(q.x - q.r * 0.26, q.y - q.r * 0.3, q.r * 0.42, C[3], 0.13 * fade);
  }
  if (t < 0.2) {
    const f = 1 - t / 0.2;
    p.circle(0, 0, 8 * (0.5 + 0.5 * f), f > 0.5 ? WHITE : C[4], 0.95);
    p.glow(0, 0, 26, C[4], 0.5 * f);
  }
}
