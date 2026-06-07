using UnityEngine;

namespace Lair.Battle
{
    //# 디바이스 종횡비와 무관하게 게임 화면을 16:9 로 고정(레터박스/필러박스)한다.
    //# 모든 UI 레이아웃이 1280×720 단일 좌표계를 전제(기획서 spawner-status-ui §2.8 등)하므로,
    //# 캔버스의 논리 공간을 항상 16:9 로 가둬 가변 종횡비로 인한 UI 잔상/오버플로를 전역 차단한다.
    //# ExecuteAlways — Play 전 편집 모드 Game 뷰에서도 16:9 레터박스를 미리보기한다.
    [ExecuteAlways]
    [DefaultExecutionOrder(-50)]
    public class LetterboxFramer : MonoBehaviour
    {
        //# 고정 종횡비 (16:9). 1280/720 과 동일.
        [SerializeField] private float _targetAspect = 16f / 9f;

        //# 16:9 박스로 렌더할 게임플레이 카메라 (World + ScreenSpaceCamera UI).
        [SerializeField] private Camera _gameCamera;
        //# 16:9 박스 바깥(레터박스 띠)을 검게 채우는 배경 카메라. depth 가 _gameCamera 보다 작아야 한다.
        [SerializeField] private Camera _backgroundCamera;
        //# 16:9 박스에 맞춰 ScreenSpaceCamera 로 렌더할 UI 캔버스.
        [SerializeField] private Canvas _uiCanvas;

        //# 마지막으로 적용한 화면 크기 — 변동 시에만 재계산.
        private int _lastWidth;
        private int _lastHeight;

        private void OnEnable()
        {
            //# 풀/씬 재진입 시 강제 1회 재적용 위해 캐시 무효화.
            _lastWidth = 0;
            _lastHeight = 0;
            ConfigureBackgroundCamera();
            Apply();
        }

        //# 레터박스 띠를 채우는 배경 카메라 설정을 스크립트가 직접 소유한다.
        //# 에디터 자동화(UnityMCP)로 빌트인 Camera 속성을 못 만지므로 수동 씬 구성 의존을 제거 — 빈 Camera 만 있으면 된다.
        private void ConfigureBackgroundCamera()
        {
            if (_backgroundCamera == null)
                return;

            //# 표준 Camera API 만 사용 — URP/빌트인 무관하게 검은 클리어 전용으로 동작.
            _backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            _backgroundCamera.backgroundColor = Color.black;
            _backgroundCamera.cullingMask = 0;
            _backgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);

            //# depth 가 작을수록 먼저 렌더 → 배경을 게임 카메라보다 먼저 그려 띠를 깐다.
            if (_gameCamera != null)
            {
                _backgroundCamera.depth = _gameCamera.depth - 1f;
            }
            else
            {
                _backgroundCamera.depth = -2f;
            }
        }

        private void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight)
                return;
            Apply();
        }

        private void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            Rect rect = ComputeViewportRect(Screen.width, Screen.height, _targetAspect);

            if (_gameCamera != null)
            {
                _gameCamera.rect = rect;
            }

            if (_uiCanvas != null && _gameCamera != null)
            {
                //# ScreenSpaceCamera 로 두면 캔버스가 카메라 viewport(=16:9 박스) 안에만 그려진다.
                //# → CHMUI 가 풀스트레치로 붙이는 팝업도 16:9 안으로 제한 → 가변 종횡비 오버플로 차단.
                _uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                _uiCanvas.worldCamera = _gameCamera;
                _uiCanvas.planeDistance = 1f;
            }
        }

        //# 화면(w×h) 안에서 targetAspect 박스를 중앙 정렬한 정규화 viewport rect 를 계산.
        //# 화면이 더 넓으면 좌우 필러박스, 더 좁으면 상하 레터박스.
        public static Rect ComputeViewportRect(int screenWidth, int screenHeight, float targetAspect)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || targetAspect <= 0f)
                return new Rect(0f, 0f, 1f, 1f);

            float windowAspect = (float)screenWidth / screenHeight;
            float scaleHeight = windowAspect / targetAspect;

            if (scaleHeight < 1f)
            {
                //# 화면이 16:9 보다 세로로 길다 → 상하 레터박스.
                return new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
            }

            //# 화면이 16:9 보다 가로로 넓다 → 좌우 필러박스.
            float scaleWidth = 1f / scaleHeight;
            return new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
        }
    }
}
