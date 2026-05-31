using System.Collections;
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 카드 리뉴얼 v0.6 — BuildSynergyPanel 의 1축 셀.
    //# 표시: [배경 = 축 색] AXIS  N/임계  ■■■ (Tier 마커).
    public class BuildSynergyCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private CHText _text;

        private Color _axisColor;
        private Coroutine _pulseRoutine;

        //# 풀 재사용 시 코루틴/배경 알파 리셋.
        private void OnEnable()
        {
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }

        //# Panel.InitItem 호출 — 1축 데이터로 표시 갱신.
        public void Bind(BuildSynergyCellData data)
        {
            if (data == null) return;
            _axisColor = data.Color;

            string markers = data.ActiveTier > 0 ? new string('■', data.ActiveTier) : "";
            string thresholdText = data.NextThreshold > 0 ? $"{data.Count}/{data.NextThreshold}" : $"{data.Count}+";
            string composed = $"{data.Label}  {thresholdText}  {markers}";
            if (_text != null) _text.SetText(composed);

            //# 배경 알파 — Tier 활성 = 50%, 미도달 = 30%.
            float alpha = data.ActiveTier > 0 ? 0.5f : 0.3f;
            if (_background != null)
                _background.color = new Color(_axisColor.r, _axisColor.g, _axisColor.b, alpha);
        }

        //# 임계 도달 펄스 — 0.3s 동안 배경 알파 50→100→50.
        public void Pulse()
        {
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            const float duration = 0.3f;
            const float peak = 1.0f;
            const float baseAlpha = 0.5f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(t / duration);
                float a = Mathf.Lerp(baseAlpha, peak, Mathf.Sin(ratio * Mathf.PI));
                if (_background != null)
                    _background.color = new Color(_axisColor.r, _axisColor.g, _axisColor.b, a);
                yield return null;
            }
            if (_background != null)
                _background.color = new Color(_axisColor.r, _axisColor.g, _axisColor.b, baseAlpha);
            _pulseRoutine = null;
        }
    }
}
