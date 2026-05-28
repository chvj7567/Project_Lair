using UnityEngine;

namespace Lair.Battle
{
    //# Main Camera 에 부착. 마우스 휠로 카메라를 forward 축 방향으로 이동시켜 줌 효과를 낸다.
    //# Perspective + 하향 틸트 카메라에서 FOV 변경 대신 position translate 를 사용하는 이유:
    //# — FOV 변경은 원근 왜곡만 증가, 시점 중심 고정이 안 됨.
    //# — transform.position.y 만 이동하면 틸트 때문에 시야 중심이 Z 축으로 밀림.
    //# — forward 방향 이동은 카메라 바라보는 방향 그대로 가까워지고 멀어지므로 자연스러운 줌.
    [RequireComponent(typeof(Camera))]
    public class BattleCamera : MonoBehaviour
    {
        //# 최소 줌 거리 (카메라가 앵커 기준 forward 로 이동하는 최솟값).
        //# 기본값 = 현재 씬 설정과 동일한 거리 → Inspector 에서 맵 크기에 맞게 조정.
        [Tooltip("카메라가 앵커에서 forward 방향으로 떨어지는 최소 거리 (줌인 한계)")]
        [SerializeField] private float _minZoomDistance = 20f;
        //# 최대 줌 거리 — 이 값에서 스포너(맵 밖)가 보이는 범위까지 커버.
        [Tooltip("카메라가 앵커에서 forward 방향으로 떨어지는 최대 거리 (줌아웃 한계)")]
        [SerializeField] private float _maxZoomDistance = 50f;
        //# 마우스 휠 한 칸 당 줌 이동량.
        [Tooltip("마우스 휠 한 스크롤당 줌 이동 거리 (클수록 민감)")]
        [SerializeField] private float _zoomSpeed = 5f;
        //# 목표 줌 거리에 수렴하는 Lerp 속도 (단위: 1/s).
        [Tooltip("줌 스무딩 속도 (Lerp rate). 클수록 즉각 반응")]
        [SerializeField] private float _smoothSpeed = 10f;

        //# 카메라가 바라보는 월드 앵커 지점 (Start 시 한 번 계산).
        //# 앵커 = 카메라 forward 방향 레이가 y=0 평면과 만나는 지점.
        private Vector3 _worldAnchor;
        //# forward 단위벡터 (Start 시 캐싱 — 런타임 변하지 않음).
        private Vector3 _forward;
        //# 현재 줌 거리 (부드럽게 보간 중인 실제 거리).
        private float _currentDist;
        //# 목표 줌 거리 (휠 입력으로 갱신).
        private float _targetDist;

        private void Start()
        {
            _forward = transform.forward;

            //# 앵커 = 카메라 forward 가 y=0 과 만나는 지점.
            //# forward.y < 0 이어야 지면과 교차 — 틸트 없이 수평이면 fallback 으로 origin 사용.
            if (_forward.y < 0f)
            {
                float t = transform.position.y / (-_forward.y);
                _worldAnchor = transform.position + _forward * t;
            }
            else
            {
                _worldAnchor = Vector3.zero;
            }

            //# 기본 줌 = 최대 줌인으로 강제 시작 — 스포너가 화면 밖인 상태.
            _currentDist = _minZoomDistance;
            _targetDist = _minZoomDistance;
            transform.position = _worldAnchor + (-_forward) * _minZoomDistance;
        }

        private void Update()
        {
            HandleScrollInput();
            ApplyZoom();
        }

        //# 마우스 휠 입력을 목표 거리에 반영.
        //# 휠 업 = 줌인 (거리 감소), 휠 다운 = 줌아웃 (거리 증가).
        //# Input.GetAxis — Project Settings > Player > Active Input Handling 이 "Old" 또는 "Both" 여야 동작.
        //# "New Input System" 전용 설정이면 InputAction 기반으로 교체 필요.
        private void HandleScrollInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll == 0f) return;

            _targetDist -= scroll * _zoomSpeed;
            _targetDist = Mathf.Clamp(_targetDist, _minZoomDistance, _maxZoomDistance);
        }

        //# 현재 거리를 목표 거리로 스무딩 후 카메라 위치 적용.
        private void ApplyZoom()
        {
            if (Mathf.Approximately(_currentDist, _targetDist)) return;

            _currentDist = Mathf.Lerp(_currentDist, _targetDist, Time.unscaledDeltaTime * _smoothSpeed);

            //# 앵커에서 -forward 방향(카메라 뒤쪽)으로 distance 만큼 이동.
            transform.position = _worldAnchor + (-_forward) * _currentDist;
        }
    }
}
