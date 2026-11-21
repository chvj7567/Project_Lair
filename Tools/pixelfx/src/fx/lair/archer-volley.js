import { fit, clamp, smooth, ease, rand, TAU, WHITE, hex, band } from "../../core/index.js";

export const meta = {
  name: "Archer arrow volley",
  desc: "A green-gold target circle on the floor takes three volleys of arrows: each streaks in, sticks with a flash and a dust ring, and the stuck arrows fade out.",
  tags: ["arrow", "aoe", "ground", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 1.5,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#133a16", "#3f8f2a", "#9bd143", "#e0e86a", "#fffbd0"], reach: 44, seed: 4 },
};

const VOLLEYS = [0, 0.5, 1.0], PER = 9;

//# arrow stuck in the floor, seen from above: shaft toward the up-left, fletching tail, bright head dot
function arrow(p, x, y, C, a, big) {
  const dx = -0.5, dy = -0.87;
  p.line(x, y, x + dx * 6, y + dy * 6, C[2], a);
  p.px(x + dx * 7, y + dy * 7, C[4]);
  p.px(x + dx * 6 - 1, y + dy * 6, C[3]);
  p.px(x + dx * 6 + 1, y + dy * 6, C[3]);
  p.dot(x, y, big ? WHITE : C[4], a);
}

export function draw(p, t, o) {
  const C = fit(o.palette, 5).map((c) => (typeof c === "string" ? hex(c) : c)), R = o.reach, S = o.seed * 3;
  const born = clamp(t / 0.12), R0 = R * ease.outCubic(born);
  const end = 1 - smooth(1.15, 1.5, t);
  if (end <= 0) return;

  //# the target circle: sheen and a pulsing rim that flares on each volley
  let pulse = 0;
  for (const v of VOLLEYS) pulse = Math.max(pulse, clamp(1 - (t - v) / 0.18) * (t >= v ? 1 : 0));
  for (let y = -Math.ceil(R0); y <= Math.ceil(R0); y++) {
    for (let x = -Math.ceil(R0); x <= Math.ceil(R0); x++) {
      const d = Math.hypot(x + 0.5, y + 0.5) / R0;
      if (d > 1) continue;
      p.blend(x, y, C[2], (0.05 + 0.14 * d * d + 0.12 * pulse) * end);
    }
  }
  band(p, 0, 0, R0, 1, 2.5 + 1.5 * pulse, end, [C[1], C[2], C[3], C[4]], () => true, pulse > 0.4);

  VOLLEYS.forEach((v, vi) => {
    for (let i = 0; i < PER; i++) {
      const seed = vi * 20 + i, a = rand(seed, 1, S) * TAU, rr = Math.sqrt(rand(seed, 2, S)) * R * 0.82;
      const x = Math.cos(a) * rr, y = Math.sin(a) * rr, land = v + 0.02 + rand(seed, 3, S) * 0.16, age = t - land;
      //# the streak coming in, then the stuck arrow
      if (age > -0.07 && age < 0) {
        const f = (age + 0.07) / 0.07;
        p.line(x + 0.5 * 18 * (1 - f), y + 0.87 * 18 * (1 - f), x + 0.5 * 6, y + 0.87 * 6, C[4], 0.9);
      }
      if (age < 0) continue;
      const stay = 1 - smooth(0.5, 1.4, t - v) * 0.0;
      arrow(p, x, y, C, end * stay, age < 0.1);
      if (age < 0.28) {
        const f = age / 0.28;
        p.ring(x, y, 2 + 5 * ease.outCubic(f), 1.2, C[3], (1 - f) * 0.9);
        if (age < 0.08) p.glow(x, y, 7, C[4], 0.6);
      }
    }
  });
}
