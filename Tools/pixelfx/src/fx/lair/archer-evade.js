import { fit, clamp, smooth, ease, rand, TAU, WHITE, hex, band } from "../../core/index.js";

export const meta = {
  name: "Archer evade blast",
  desc: "A violet shock ring bursts out from where the archer leapt away: white flash, hard bright rim, puffs of dust riding the front, radial streaks.",
  tags: ["blast", "ring", "ground", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.667,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#1c1038", "#4a2a8a", "#8a5ae0", "#c9a8ff", "#f4ecff"], reach: 44, seed: 9 },
};

//# top-down view: the cell is a true circle, so the ring is round
export function draw(p, t, o) {
  const C = fit(o.palette, 5).map((c) => (typeof c === "string" ? hex(c) : c)), R = o.reach, S = o.seed * 7;
  const k = clamp(t / 0.4), r = R * ease.outExpo(k);
  const fade = 1 - smooth(0.33, 0.667, t);
  if (r < 1) return;

  //# soft violet sheen inside the front
  for (let y = -Math.ceil(r); y <= Math.ceil(r); y++) {
    for (let x = -Math.ceil(r); x <= Math.ceil(r); x++) {
      const d = Math.hypot(x + 0.5, y + 0.5) / r;
      if (d > 1) continue;
      p.blend(x, y, d > 0.65 ? C[3] : C[2], (0.06 + 0.3 * Math.pow(d, 3)) * fade);
    }
  }
  band(p, 0, 0, r, 1, Math.max(1.5, 4 * (1 - k * 0.6)), fade, [C[1], C[2], C[3], C[4]], () => true, t < 0.2);

  //# radial streaks from the centre, thrown outward with the front
  for (let i = 0; i < 14; i++) {
    const a = (i / 14) * TAU + rand(i, 1, S) * 0.3, lo = r * (0.35 + 0.2 * rand(i, 2, S)), hi = r * 0.92;
    if (k >= 1 && fade < 0.4) continue;
    p.line(Math.cos(a) * lo, Math.sin(a) * lo, Math.cos(a) * hi, Math.sin(a) * hi, (u) => (u > 0.6 ? C[4] : C[3]), (0.9 - 0.4 * (1 - k)) * fade);
  }

  //# dust puffs on the front, swelling then thinning
  for (let i = 0; i < 10; i++) {
    const a = (i / 10) * TAU + rand(i, 3, S) * 0.5, rr = r * (0.8 + 0.12 * rand(i, 4, S));
    const sz = 3 + 4 * ease.outQuad(k) * (0.6 + rand(i, 5, S) * 0.6);
    p.disc(Math.cos(a) * rr, Math.sin(a) * rr, sz, C[3], 0.35 * fade);
  }

  if (t < 0.17) {
    const f = 1 - t / 0.17;
    p.circle(0, 0, 9 * (0.5 + 0.5 * f), f > 0.5 ? WHITE : C[3], 0.95);
    p.glow(0, 0, 26, C[4], 0.5 * f);
  }
}
