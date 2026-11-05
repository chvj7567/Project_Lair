using UnityEngine;

namespace Lair.UI
{
    //# HudRoot 에 붙여 Screen.safeArea ∩ 게임 카메라 pixelRect 를 앵커로 반영한다(battle-wide-screen-fit §4.1).
    //# 캔버스가 카메라 rect 에 붙어 있으므로 앵커는 카메라 pixelRect 기준으로 정규화한다.
    [ExecuteAlways]
    [DefaultExecutionOrder(-40)]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private Camera _gameCamera;
        [SerializeField] private RectTransform _target;

        private Rect _lastSafe;
        private Rect _lastPixel;
        private bool _applied;

        private void OnEnable()
        {
            _applied = false;
        }

        private void Update()
        {
            if (_gameCamera == null || _target == null)
                return;

            Rect safe = Screen.safeArea;
            Rect pixel = _gameCamera.pixelRect;
            if (_applied && safe == _lastSafe && pixel == _lastPixel)
                return;

            _applied = true;
            _lastSafe = safe;
            _lastPixel = pixel;
            ComputeAnchors(safe, pixel, out Vector2 anchorMin, out Vector2 anchorMax);
            _target.anchorMin = anchorMin;
            _target.anchorMax = anchorMax;
            _target.offsetMin = Vector2.zero;
            _target.offsetMax = Vector2.zero;
        }

        //# safe ∩ pixel 을 pixel 기준 0~1 로 정규화. 교집합이 비면 전체(0,0)~(1,1).
        public static void ComputeAnchors(Rect safe, Rect pixel, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;
            if (pixel.width <= 0f || pixel.height <= 0f)
                return;

            float xMin = Mathf.Max(safe.xMin, pixel.xMin);
            float yMin = Mathf.Max(safe.yMin, pixel.yMin);
            float xMax = Mathf.Min(safe.xMax, pixel.xMax);
            float yMax = Mathf.Min(safe.yMax, pixel.yMax);
            if (xMax <= xMin || yMax <= yMin)
                return;

            anchorMin = new Vector2((xMin - pixel.xMin) / pixel.width, (yMin - pixel.yMin) / pixel.height);
            anchorMax = new Vector2((xMax - pixel.xMin) / pixel.width, (yMax - pixel.yMin) / pixel.height);
        }
    }
}
