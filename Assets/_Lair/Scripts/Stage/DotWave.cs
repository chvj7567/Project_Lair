using UnityEngine;

namespace Lair.Stage
{
    //# 사인 합 파형(정적 순수) — 기획서 scene-2d-conversion §7.3. base + Σ amp_k·sin(ω_k·t + φ_k).
    public static class DotWave
    {
        public static float Evaluate(float baseValue, float[] amps, float[] omegas, float[] phases, float t)
        {
            if (amps == null || omegas == null || phases == null)
                return baseValue;

            int n = Mathf.Min(amps.Length, Mathf.Min(omegas.Length, phases.Length));
            float v = baseValue;
            for (int k = 0; k < n; k++)
            {
                v += amps[k] * Mathf.Sin(omegas[k] * t + phases[k]);
            }
            return v;
        }
    }
}
