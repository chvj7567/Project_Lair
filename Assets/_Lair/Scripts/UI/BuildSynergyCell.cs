using System.Collections;
using ChvjUnityInfra;
using Lair.Card;
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
        //# UI 리디자인 — 도트 문장 아이콘 + 왼쪽 축 색띠 + 흐림 그룹. _axisIcon 이 배선되면 새 표시(사각 점 마커·축 이름만), 아니면 기존 표시.
        [SerializeField] private Image _axisIcon;
        [SerializeField] private Image _axisStrip;
        [SerializeField] private CanvasGroup _group;
        //# 시너지 진행도 — 1줄 우측 장수 텍스트(축 색) + 2줄 7칸 트랙. 리디자인 표시에서만 사용.
        [SerializeField] private CHText _countText;
        [SerializeField] private SynergyTrack _track;

        private bool RedesignMode => _axisIcon != null;

        private Color _axisColor;
        private Coroutine _pulseRoutine;

        //# 풀 재사용 시 코루틴/마커 리셋 — 이전 셀 마커 잔존 방지.
        private void OnEnable()
        {
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
            if (_tierMarkers == null) return;
            //# 리디자인 마커는 항상 보이는 점이라 풀 재사용 리셋에서 숨기지 않는다(Bind 가 색을 다시 칠함).
            if (RedesignMode) return;
            for (int i = 0; i < _tierMarkers.Length; ++i)
                ResetMarker(_tierMarkers[i]);
        }

        //# Panel.InitItem 호출 — 1축 데이터로 표시 갱신.
        public void Bind(BuildSynergyCellData data)
        {
            if (data == null) return;
            _axisColor = data.Color;

            if (RedesignMode)
            {
                BindRedesign(data);
                return;
            }

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

        //# 리디자인 표시(기획서 card-synergy-indicator §3.1) — 축 이름 + 장수 텍스트 + 7칸 트랙. 0장만 흐림, Tier 1 이상이면 왼쪽 축 색띠.
        //# 매 Bind 마다 전부 재계산 — 풀 재사용 시 이전 트랙·색띠·텍스트 잔상 금지.
        private void BindRedesign(BuildSynergyCellData data)
        {
            bool strip = SynergyProgress.HasStrip(data.Count);
            if (_text != null)
            {
                _text.SetText(data.Label);
                _text.SetColor(_axisColor);
            }
            if (_countText != null)
            {
                _countText.SetText(SynergyProgress.CountText(data.Count));
                _countText.SetColor(_axisColor);
            }
            _axisIcon.sprite = data.Icon;
            _axisIcon.enabled = data.Icon != null;
            if (_axisStrip != null)
            {
                _axisStrip.gameObject.SetActive(strip);
                _axisStrip.color = _axisColor;
            }
            if (_group != null)
            {
                _group.alpha = SynergyProgress.Opacity(data.Count);
            }
            if (_track != null)
            {
                _track.Bind(data.Count, _axisColor, data.GainedCell);
            }
        }

        //# 펄스 알파 적용 — 리디자인은 축 색띠, 기존은 배경.
        private void ApplyPulseAlpha(float alpha)
        {
            if (RedesignMode)
            {
                if (_axisStrip != null)
                    _axisStrip.color = new Color(_axisColor.r, _axisColor.g, _axisColor.b, alpha);
                return;
            }
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
                ApplyPulseAlpha(a);
                yield return null;
            }
            ApplyPulseAlpha(RedesignMode ? 1f : baseAlpha);
            _pulseRoutine = null;
        }
    }
}
