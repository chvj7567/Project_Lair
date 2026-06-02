using System.Collections;
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 카드 리뉴얼 v0.6 — BuildSynergyPanel 의 1축 셀.
    //# 표시: [배경 = 축 색] AXIS  N/임계  [축아이콘 × ActiveTier] (우측 티어 마커).
    public class BuildSynergyCell : MonoBehaviour
    {
        [SerializeField] private Image _background;
        //# 우측 티어 마커 3칸 — 좌→우 순서. ActiveTier 만큼 활성, sprite = 축 아이콘.
        [SerializeField] private Image[] _tierMarkers;
        [SerializeField] private CHText _text;

        private Color _axisColor;
        private Coroutine _pulseRoutine;

        //# 풀 재사용 시 코루틴/마커 리셋 — 이전 셀 마커 잔존 방지.
        private void OnEnable()
        {
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
            if (_tierMarkers == null) return;
            for (int i = 0; i < _tierMarkers.Length; ++i)
                ResetMarker(_tierMarkers[i]);
        }

        //# Panel.InitItem 호출 — 1축 데이터로 표시 갱신.
        public void Bind(BuildSynergyCellData data)
        {
            if (data == null) return;
            _axisColor = data.Color;

            string thresholdText = data.NextThreshold > 0 ? $"{data.Count}/{data.NextThreshold}" : $"{data.Count}+";
            string composed = $"{data.Label}  {thresholdText}";
            if (_text != null)
            {
                _text.SetText(composed);
            }

            //# 티어 마커 — 매 Bind 마다 전부 재계산. early-return 금지(SetItemList 재바인딩에서
            //# Tier 하락 시 상위 마커 잔존 방지). Icon null 이면 모두 숨김.
            if (_tierMarkers != null)
            {
                for (int i = 0; i < _tierMarkers.Length; ++i)
                {
                    bool active = data.Icon != null && i < data.ActiveTier;
                    if (active == false)
                    {
                        ResetMarker(_tierMarkers[i]);
                        continue;
                    }
                    if (_tierMarkers[i] == null) continue;
                    _tierMarkers[i].sprite = data.Icon;
                    _tierMarkers[i].gameObject.SetActive(true);
                }
            }

            //# 배경 알파 — Tier 활성 = 50%, 미도달 = 30%.
            float alpha = data.ActiveTier > 0 ? 0.5f : 0.3f;
            if (_background != null)
                _background.color = new Color(_axisColor.r, _axisColor.g, _axisColor.b, alpha);
        }

        //# 단일 마커 비활성 + sprite null — 풀 리셋·Tier 하락·Icon null 공통 처리.
        private static void ResetMarker(Image marker)
        {
            if (marker == null) return;
            marker.sprite = null;
            marker.gameObject.SetActive(false);
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
