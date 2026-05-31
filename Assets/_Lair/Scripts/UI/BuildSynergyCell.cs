using System.Collections;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 카드 리뉴얼 v0.6 — BuildSynergyPanel 의 1축 셀.
    //# 표시: [색박스] AXIS  N/임계  ■■■ (Tier 마커).
    //# 동적 생성 패턴 — Create 정적 메서드로 RectTransform/Image/CHText 조립.
    public class BuildSynergyCell : MonoBehaviour
    {
        private Image _background;
        private CHText _text;
        private Color _axisColor;
        private string _axisLabel;
        private Coroutine _pulseRoutine;

        public int Count { get; private set; }

        //# 정적 팩토리 — parent 자식으로 GameObject 동적 생성 후 컴포넌트 부착.
        public static BuildSynergyCell Create(Transform parent, EBuildAxis axis, Color axisColor, string axisLabel)
        {
            GameObject go = new GameObject($"BuildSynergyCell_{axis}");
            go.transform.SetParent(parent, false);

            //# RectTransform 기본 — 폭 = 부모 expand, 높이 = 36.
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 36f);

            //# 배경 Image — 축 색 알파 30% (미도달 상태).
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(axisColor.r, axisColor.g, axisColor.b, 0.3f);

            //# 자식 TMP 텍스트 — 한 줄에 "AXIS  N/임계  ■■■".
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 0f);
            textRt.offsetMax = new Vector2(-8f, 0f);

            //# CHText 래퍼 (Rule 03 §3 — UI 컴포넌트 ChvjPackage 래퍼 우선).
            //# CHText 는 RequireComponent(TMP_Text) 로 자동 TextMeshProUGUI 부착.
            CHText chText = textGo.AddComponent<CHText>();

            BuildSynergyCell cell = go.AddComponent<BuildSynergyCell>();
            cell._background = bg;
            cell._text = chText;
            cell._axisColor = axisColor;
            cell._axisLabel = axisLabel;
            cell.SetCount(0, 3, 0);
            return cell;
        }

        //# count = 현재 픽 수 / nextThreshold = 다음 임계 (-1 면 최대 도달) / activeTier = 0~3.
        public void SetCount(int count, int nextThreshold, int activeTier)
        {
            Count = count;
            string markers = activeTier > 0 ? new string('■', activeTier) : "";
            string thresholdText = nextThreshold > 0 ? $"{count}/{nextThreshold}" : $"{count}+";
            string composed = $"{_axisLabel}  {thresholdText}  {markers}";
            if (_text != null) _text.SetText(composed);

            //# 배경 알파 — Tier 활성 = 50%, 미도달 = 30%.
            float alpha = activeTier > 0 ? 0.5f : 0.3f;
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
                //# 0→0.5→1 의 알파 진동 — sin 곡선으로 한 번 펄스.
                float a = Mathf.Lerp(baseAlpha, peak, Mathf.Sin(ratio * Mathf.PI));
                if (_background != null)
                    _background.color = new Color(_axisColor.r, _axisColor.g, _axisColor.b, a);
                yield return null;
            }
            //# 종료 시 활성 알파(0.5)로 복귀.
            if (_background != null)
                _background.color = new Color(_axisColor.r, _axisColor.g, _axisColor.b, baseAlpha);
            _pulseRoutine = null;
        }
    }
}
