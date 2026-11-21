import { clamp, smooth, ease, rand, TAU, WHITE, band } from "../../core/index.js";
import { pal, sheen } from "./_lib.js";

export const meta = {
  name: "Assassin blade whirl",
  desc: "Four pulses of spinning steel: curved blade arcs whirl round the floor circle at two radii in opposite directions, the rim flaring on every pulse.",
  tags: ["whirl", "blade", "aoe", "ground", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 1.167,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#14182a", "#3a4a7a", "#7f95d0", "#c4d6ff", "#ffffff"], reach: 45, seed: 6 },
};

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed * 5;
  const born = clamp(t / 0.1), fade = 1 - smooth(0.8, 1.167, t);
  if (fade <= 0) return;
  const R0 = R * ease.outCubic(born);
  //# pulse: four beats; the rim and sheen flare on each
  const beat = (t % 0.25) / 0.25, flare = t < 1.0 ? Math.pow(1 - beat, 2) : 0;
  sheen(p, R0, (d) => [d > 0.7 ? C[3] : C[2], (0.14 + 0.2 * d * d + 0.22 * flare * d) * fade]);
  band(p, 0, 0, R0, 1, 2 + 2.5 * flare, fade, [C[1], C[2], C[3], C[4]], () => true, flare > 0.6);

  //# blade arcs: outer set clockwise, inner set counter-clockwise, spinning up then easing
  const spin = TAU * 2.2 * ease.outQuad(clamp(t / 1.0));
  const sets = [{ n: 4, rr: 0.78, dir: 1, len: 1.1 }, { n: 3, rr: 0.5, dir: -1, len: 0.9 }];
  for (const s of sets) {
    for (let i = 0; i < s.n; i++) {
      const head = (i / s.n) * TAU + spin * s.dir, rr = R * s.rr;
      for (let j = 0; j <= 24; j++) {
        const u = j / 24, a = head - s.dir * s.len * (1 - u);
        const al = Math.min(1, Math.pow(u, 0.7) * fade * 1.5);
        const x = Math.cos(a) * rr, y = Math.sin(a) * rr;
        p.dot(x, y, u > 0.9 ? WHITE : u > 0.6 ? C[4] : u > 0.3 ? C[3] : C[2], al);
        if (u > 0.15) { p.dot(x * 1.04, y * 1.04, C[3], al); p.dot(x * 0.96, y * 0.96, C[2], al); }
        if (u > 0.55) { p.dot(x * 1.08, y * 1.08, C[2], al); p.dot(x * 0.92, y * 0.92, C[2], al); }
      }
      p.light(Math.cos(head) * rr, Math.sin(head) * rr, C[3], 0.4 * fade);
    }
  }
  if (t < 0.17) {
    const f = 1 - t / 0.17;
    p.circle(0, 0, 8 * (0.5 + 0.5 * f), f > 0.5 ? WHITE : C[3], 0.95);
    p.glow(0, 0, 24, C[3], 0.5 * f);
  }
}
