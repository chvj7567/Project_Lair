import { clamp, smooth, ease, rand, TAU, WHITE, band } from "../../core/index.js";
import { pal, sheen } from "./_lib.js";

export const meta = {
  name: "Assassin shadow strike",
  desc: "A violet shadow poof where the assassin blinks: a dark core collapses, a hard ring snaps out and curling wisps spiral away.",
  tags: ["blink", "shadow", "ground", "one-shot"],
  size: [64, 64],
  origin: [32, 32],
  duration: 0.833,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#0a0614", "#2a1650", "#5b34a8", "#a678f0", "#efe0ff"], reach: 27, seed: 2 },
};

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed * 9;
  const k = clamp(t / 0.3), r = R * ease.outExpo(k), fade = 1 - smooth(0.4, 0.83, t);
  if (r < 1) return;
  sheen(p, r, (d) => [d > 0.6 ? C[2] : C[1], (0.3 + 0.45 * d * d) * fade]);
  //# the dark core shrinks away
  const core = (1 - smooth(0, 0.45, t)) * 10;
  if (core > 0.5) p.circle(0, 0, core, C[0], 0.9);
  band(p, 0, 0, r, 1, Math.max(2, 5 * (1 - k * 0.5)), fade, [C[1], C[2], C[3], C[4]], () => true, t < 0.2);
  //# wisps curling outward
  for (let i = 0; i < 7; i++) {
    const a0 = (i / 7) * TAU + rand(i, 1, S) * 0.6, born = clamp((t - 0.03 * i) / 0.45);
    if (born <= 0) continue;
    for (let j = 0; j <= 14; j++) {
      const u = j / 14, rr = r * (0.25 + 0.7 * u * born), a = a0 + u * 1.3 * (1 + 0.4 * rand(i, 2, S));
      const al = Math.pow(1 - u, 0.8) * fade * 1.4;
      p.dot(Math.cos(a) * rr, Math.sin(a) * rr, u < 0.3 ? C[4] : u < 0.6 ? C[3] : C[2], al);
      if (u < 0.6) p.dot(Math.cos(a) * rr + 1, Math.sin(a) * rr, C[2], al); p.dot(Math.cos(a) * rr, Math.sin(a) * rr + 1, C[3], al);
    }
  }
  if (t < 0.17) {
    const f = 1 - t / 0.17;
    //# crossing slash marks in the flash
    p.line(-12 * f - 4, -12 * f - 4, 12 * f + 4, 12 * f + 4, WHITE, 0.95);
    p.line(12 * f + 4, -12 * f - 4, -12 * f - 4, 12 * f + 4, C[4], 0.95);
    p.glow(0, 0, 22, C[3], 0.5 * f);
  }
}
