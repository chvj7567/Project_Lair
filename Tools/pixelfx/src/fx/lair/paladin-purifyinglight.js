import { clamp, smooth, ease, rand, TAU, WHITE, band } from "../../core/index.js";
import { pal, sheen, star } from "./_lib.js";

export const meta = {
  name: "Paladin purifying light",
  desc: "Soft white-gold light blooms round the paladin: two thin rings of light rise and widen, glints twinkle and motes drift up and out as the cleansing fades.",
  tags: ["holy", "cleanse", "ground", "one-shot"],
  size: [64, 64],
  origin: [32, 32],
  duration: 1.167,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#3a3010", "#a8841c", "#ffd84a", "#fff2a8", "#ffffff"], reach: 28, seed: 17 },
};

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed * 3;
  const fade = 1 - smooth(0.75, 1.167, t), born = clamp(t / 0.15);
  if (fade <= 0) return;
  sheen(p, R * ease.outCubic(born), (d) => [d > 0.5 ? C[3] : C[2], (0.2 + 0.4 * (1 - d) + 0.2 * d * d) * fade]);
  for (let i = 0; i < 2; i++) {
    const u = clamp((t - i * 0.25) / 0.55);
    if (u <= 0 || u >= 1) continue;
    band(p, 0, 0, R * (0.2 + 0.78 * ease.outCubic(u)), 1, 3, Math.min(1, (1.3 - u) * fade), [C[1], C[2], C[3], C[4]], () => true, u < 0.3);
  }
  //# motes drifting outward from the middle, twinkling
  for (let i = 0; i < 20; i++) {
    const ki = clamp((t - rand(i, 1, S) * 0.45) / 0.85);
    if (ki <= 0 || ki >= 1) continue;
    const a = rand(i, 2, S) * TAU + ki * 0.6, d = R * (0.15 + 0.75 * ease.outQuad(ki)) * (0.5 + 0.5 * rand(i, 3, S));
    const x = Math.cos(a) * d, y = Math.sin(a) * d;
    if (i % 3 === 0 && ki < 0.85) star(p, x, y, 2, C[3], WHITE);
    else p.dot(x, y, C[4], 1 - ki * 0.5);
    p.light(x, y, C[3], 0.25 * (1 - ki));
  }
  if (t < 0.25) {
    const f = 1 - t / 0.25;
    p.circle(0, 0, 8 * (0.4 + 0.6 * f), f > 0.5 ? WHITE : C[4], 0.9);
    p.glow(0, 0, 24, C[4], 0.55 * f);
  }
}
