using UnityEngine;

namespace Lair.Stage
{
    //# 스프라이트 알파 맥동 — 알파 = clamp01(DotWave). Time.time 기준이라 일시정지 중 멈춘다(§7.3).
    public class DotAlphaPulse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _base = 0.55f;
        [SerializeField] private float[] _amps = { 0.25f };
        [SerializeField] private float[] _omegas = { 3f };
        [SerializeField] private float[] _phases = { 0f };

        private void Update()
        {
            if (_renderer == null)
                return;
            Color c = _renderer.color;
            c.a = Mathf.Clamp01(DotWave.Evaluate(_base, _amps, _omegas, _phases, Time.time));
            _renderer.color = c;
        }
    }
}
