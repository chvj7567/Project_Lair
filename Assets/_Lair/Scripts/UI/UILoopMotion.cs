using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 자기 Transform 만 건드리는 공용 경량 루프 모션(외부 트윈 미사용). 프리팹 인스펙터 값으로 동작, 끄면 기준 포즈로 복귀.
    public class UILoopMotion : MonoBehaviour
    {
        public enum EMotionKind
        {
            Drift,
            Rotate,
            Nudge,
            Pulse,
        }

        [SerializeField] private EMotionKind _kind = EMotionKind.Drift;
        [SerializeField] private Vector2 _amplitude;            //# Drift/Nudge — 기준 위치 대비 이동량
        [SerializeField] private float _period = 1f;            //# Drift 편도 / Nudge·Pulse 주기(초)
        [SerializeField] private float _degreesPerSecond;       //# Rotate — z축 도/초 (음수 = 시계 방향)
        [SerializeField] private float _alphaFrom = 1f;         //# Pulse
        [SerializeField] private float _alphaTo = 1f;
        [SerializeField] private float _scaleFrom = 1f;
        [SerializeField] private float _scaleTo = 1f;
        [SerializeField] private Graphic _graphic;              //# Pulse 알파 대상

        private RectTransform _rect;
        private Vector2 _basePosition;
        private Quaternion _baseRotation;
        private Vector3 _baseScale;
        private float _startTime;
        private bool _running = true;
        private bool _active;

        //# 순수 곡선 함수 — 시간과 파라미터만 받는다(EditMode 검증용).
        public static float DriftFactor(float t, float oneWay)
        {
            if (oneWay <= 0f)
                return 0f;

            return Mathf.PingPong(t, oneWay) / oneWay;
        }

        public static float RotateAngle(float t, float degreesPerSecond)
        {
            return degreesPerSecond * t;
        }

        //# 주기 앞 절반은 기준(0), 뒤 절반은 진폭(1)인 2단 스텝.
        public static float NudgeFactor(float t, float period)
        {
            if (period <= 0f)
                return 0f;

            float phase = Mathf.Repeat(t, period) / period;
            return phase < 0.5f ? 0f : 1f;
        }

        //# t=0 에서 0, 주기 절반에서 1, 주기 끝에서 0 으로 돌아오는 사인 곡선.
        public static float PulseFactor(float t, float period)
        {
            if (period <= 0f)
                return 0f;

            return 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t / period);
        }

        public void SetRunning(bool running)
        {
            if (_running == running)
                return;

            _running = running;
            if (_active == false)
                return;

            if (running)
            {
                _startTime = Time.unscaledTime;
            }
            else
            {
                RestoreBasePose();
            }
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
        }

        private void OnEnable()
        {
            _active = true;
            _basePosition = _rect != null ? _rect.anchoredPosition : Vector2.zero;
            _baseRotation = transform.localRotation;
            _baseScale = transform.localScale;
            _startTime = Time.unscaledTime;
        }

        private void OnDisable()
        {
            RestoreBasePose();
            _active = false;
        }

        private void Update()
        {
            if (_running == false)
                return;

            float t = Time.unscaledTime - _startTime;
            switch (_kind)
            {
                case EMotionKind.Drift:
                    SetOffset(_amplitude * DriftFactor(t, _period));
                    break;
                case EMotionKind.Rotate:
                    transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, RotateAngle(t, _degreesPerSecond));
                    break;
                case EMotionKind.Nudge:
                    SetOffset(_amplitude * NudgeFactor(t, _period));
                    break;
                case EMotionKind.Pulse:
                    ApplyPulse(PulseFactor(t, _period));
                    break;
            }
        }

        private void SetOffset(Vector2 offset)
        {
            if (_rect != null)
            {
                _rect.anchoredPosition = _basePosition + offset;
            }
        }

        private void ApplyPulse(float factor)
        {
            transform.localScale = _baseScale * Mathf.Lerp(_scaleFrom, _scaleTo, factor);
            if (_graphic != null)
            {
                Color color = _graphic.color;
                color.a = Mathf.Lerp(_alphaFrom, _alphaTo, factor);
                _graphic.color = color;
            }
        }

        //# 정적 폴백 포즈 — 위치·회전·스케일은 기준값, Pulse 알파는 시작값(_alphaFrom).
        private void RestoreBasePose()
        {
            if (_active == false)
                return;

            if (_rect != null)
            {
                _rect.anchoredPosition = _basePosition;
            }
            transform.localRotation = _baseRotation;
            transform.localScale = _baseScale;
            if (_kind == EMotionKind.Pulse && _graphic != null)
            {
                Color color = _graphic.color;
                color.a = _alphaFrom;
                _graphic.color = color;
            }
        }
    }
}
