using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lair.Stage
{
    //# 2D 조명 세기 깜빡임 — intensity = max(0, DotWave). Time.time 기준이라 일시정지 중 멈춘다(§7.3).
    public class DotLightFlicker : MonoBehaviour
    {
        [SerializeField] private Light2D _light;
        [SerializeField] private float _base = 0.8f;
        [SerializeField] private float[] _amps = { 0.07f };
        [SerializeField] private float[] _omegas = { 10f };
        [SerializeField] private float[] _phases = { 0f };

        private void Update()
        {
            if (_light == null)
                return;
            _light.intensity = Mathf.Max(0f, DotWave.Evaluate(_base, _amps, _omegas, _phases, Time.time));
        }
    }
}
