import { fit, TAU, WHITE, hex, clamp } from "../../core/index.js";

export const meta = {
  name: "Knight orbit blades",
  desc: "Three steel-cyan blades circle the hero on a tilted ring, each with a comet trail and a glint; far blades dim and small, near blades large.",
  tags: ["orbit", "loop", "blade"],
  size: [128, 96],
  origin: [64, 56],
  duration: 2,
  loop: true,
  fps: 12,
  bg: "ground",
  options: { palette: ["#06283a", "#0e6a8c", "#26b4e0", "#7fe6ff", "#e2fbff"], rx: 40, ry: 24 },
};

const N = 3;

function blade(p, x, y, ang, s, C, a) {
  //# a kite-shaped blade pointing along ang: long tip, short tail, bright centre line
  const ca = Math.cos(ang), sa = Math.sin(ang), pt = (u, v) => [x + ca * u * s - sa * v * s, y + sa * u * s + ca * v * s];
  p.poly([pt(9, 0), pt(-1, 3.2), pt(-6, 0), pt(-1, -3.2)], C[2], a);
  p.poly([pt(9, 0), pt(-1, 1.4), pt(-4, 0), pt(-1, -1.4)], C[3], a);
  const [x0, y0] = pt(-4, 0), [x1, y1] = pt(8, 0);
  p.line(x0, y0, x1, y1, WHITE, a);
  p.glow(x, y, 11 * s, C[3], 0.35 * a);
}

export function draw(p, t, o) {
  const C = fit(o.palette, 5).map((c) => (typeof c === "string" ? hex(c) : c));
  const ph = (t / meta.duration) * TAU;
  const items = [];
  for (let i = 0; i < N; i++) items.push(ph + (i * TAU) / N);
  //# far blades first so the near ones overlap them
  items.sort((a, b) => Math.sin(a) - Math.sin(b));
  for (const a of items) {
    const depth = 0.5 + 0.5 * Math.sin(a), s = 0.8 + 0.4 * depth, al = 0.7 + 0.3 * depth;
    //# comet trail behind the blade along the ring
    for (let j = 22; j >= 1; j--) {
      const b = a - j * 0.045, d2 = 0.5 + 0.5 * Math.sin(b), f = 1 - j / 22;
      const x = Math.cos(b) * o.rx, y = Math.sin(b) * o.ry;
      p.dot(x, y, f > 0.55 ? C[4] : f > 0.25 ? C[3] : C[2], f * f * 1.1 * (0.6 + 0.4 * d2));
      if (f > 0.5) p.dot(x, y + 1, C[2], f * 0.7);
    }
    const x = Math.cos(a) * o.rx, y = Math.sin(a) * o.ry;
    //# travel direction on the tilted ring
    const ang = Math.atan2(Math.cos(a) * o.ry, -Math.sin(a) * o.rx);
    blade(p, x, y, ang, s, C, al);
  }
}
