using UnityEngine;

namespace Lair.Stage
{
    //# 도트 배경이 어떤 화면 비율에서도 화면을 덮도록 직교 카메라 크기를 맞춘다(마을·로딩) — 좌우 빈 띠 방지.
    //# 와이드에선 위아래를 조금 잘라 확대, 좁은 화면에선 좌우를 자른다. 16:9 에선 기준 크기(2.8125) 그대로.
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class DotStageCoverFit : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        //# 배경 캔버스의 도트 크기(마을·로딩 480×270)
        [SerializeField] private Vector2 _referenceDots = new Vector2(480f, 270f);

        private float _lastAspect;

        //# 보이는 영역(가로 2·size·aspect·PPU × 세로 2·size·PPU)이 배경 안에 들어가는 가장 큰 직교 크기.
        public static float OrthoSizeForCover(Vector2 referenceDots, float aspect, float ppu)
        {
            float byHeight = referenceDots.y * 0.5f / ppu;
            if (aspect <= 0f)
                return byHeight;
            float byWidth = referenceDots.x * 0.5f / (ppu * aspect);
            return Mathf.Min(byHeight, byWidth);
        }

        //# 같은 오브젝트 카메라 1회 캐싱(Rule 02 §5 — 인스펙터 미배선 대비)
        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
        }

        private void OnEnable()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
            _lastAspect = 0f;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (_camera == null || _camera.orthographic == false)
                return;
            float aspect = _camera.aspect;
            if (Mathf.Approximately(aspect, _lastAspect))
                return;
            _lastAspect = aspect;
            _camera.orthographicSize = OrthoSizeForCover(_referenceDots, aspect, DotStageMapping.Ppu);
        }
    }
}
