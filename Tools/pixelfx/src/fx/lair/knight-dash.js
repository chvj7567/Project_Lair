import { fit, clamp, smooth, ease, rand, TAU, WHITE, hex, band } from "../../core/index.js";

export const meta = {
  name: "Knight dash",
  desc: "A whirl of slash arcs sweeps round a floor circle: a hot rim snaps out to the full radius, curved blade trails spin inside it and fade, sparks fly off the edge.",
  tags: ["dash", "slash", "ground", "one-shot"],
  size: [128, 96],
  origin: [64, 62],
  duration: 0.833,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#4a1200", "#a22c06", "#e8651a", "#ffb03a", "#fff0c0"], reach: 52, seed: 5 },
};

const SY = 0.5;

export function draw(p, t, o) {
  const C = fit(o.palette, 5).map((c) => (typeof c === "string" ? hex(c) : c)), R = o.reach, S = o.seed * 13;
  const fade = 1 - smooth(0.4, 0.83, t);
  const k = clamp(t / 0.2), r = R * ease.outExpo(k);
  if (r < 1) return;

  //# floor sheen, brighter toward the rim
  for (let y = Math.floor(-r * SY); y <= Math.ceil(r * SY); y++) {
    for (let x = Math.floor(-r); x <= Math.ceil(r); x++) {
      const d = Math.hypot((x + 0.5) / r, (y + 0.5) / (r * SY));
      if (d > 1) continue;
      p.blend(x, y, d > 0.7 ? C[3] : C[2], (0.08 + 0.26 * Math.pow(d, 3)) * fade);
    }
  }

  //# the rim: snaps out hot, thins as it settles
  band(p, 0, 0, r + 1, SY, Math.max(1.5, 4.5 * (1 - k * 0.5)), fade, [C[1], C[2], C[3], C[4]], () => true, t < 0.2);

  //# slash arcs: six curved blade trails whirling round at different radii, head bright, tail fading
  const spin = TAU * ease.outQuad(clamp(t / 0.75)) * 0.55;
  for (let i = 0; i < 6; i++) {
    const rr = r * (0.4 + 0.52 * ((i * 0.618) % 1)), a0 = (i / 6) * TAU + spin * (i % 2 ? 1 : 1.25), len = 1.3 + 0.5 * rand(i, 1, S);
    const born = clamp((t - 0.04 - 0.02 * i) / 0.1);
    if (born <= 0) continue;
    for (let j = 0; j <= 26; j++) {
      const u = j / 26, a = a0 - len * (1 - u), x = Math.cos(a) * rr, y = Math.sin(a) * rr * SY;
      const al = Math.min(1, Math.pow(u, 0.7) * born * (fade > 0.5 ? 1 : fade * 2) * 1.5);
      p.dot(x, y, u > 0.9 ? WHITE : u > 0.6 ? C[4] : u > 0.3 ? C[3] : C[2], al);
      if (u > 0.25) p.dot(x, y + 1, C[2], al);
      if (u > 0.6) p.dot(x, y - 1, C[3], al);
    }
    const hx = Math.cos(a0) * rr, hy = Math.sin(a0) * rr * SY;
    p.light(hx, hy, C[3], 0.4 * fade);
  }

  //# sparks off the rim
  for (let i = 0; i < 16; i++) {
    const ki = clamp((t - 0.03 - rand(i, 4, S) * 0.18) / 0.4);
    if (ki <= 0 || ki >= 1) continue;
    const a = rand(i, 5, S) * TAU, rd = R * (0.85 + 0.25 * ki), h = 6 * Math.sin(ki * Math.PI) * (0.6 + rand(i, 6, S));
    const x = Math.cos(a) * rd, y = Math.sin(a) * rd * SY - h;
    p.px(x, y, ki < 0.5 ? C[4] : C[3]);
    p.light(x, y, C[3], 0.3 * (1 - ki));
  }

  //# the launch flash at the centre
  if (t < 0.17) {
    const f = 1 - t / 0.17;
    p.ellipse(0, 0, 14 * (0.5 + 0.5 * f), 6 * (0.5 + 0.5 * f), f > 0.5 ? WHITE : C[3], 0.95);
    p.glow(0, -2, 30, C[3], 0.5 * f);
  }
}
