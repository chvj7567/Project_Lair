using UnityEngine;
using UnityEngine.UI;

namespace Lair.Character
{
    //# 몬스터 머리 위 WorldSpace HP 바. Health.OnChanged 구독해 fill 갱신.
    //# 매 프레임 카메라를 향하도록 빌보드 회전 — 탑다운 45° 카메라에서 잘 보임.
    //# 몬스터 프리팹 자식이라 풀 Pop/Push 에 자동 동행 — 별도 풀 불필요.
    public class MonsterHpBar : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Image _fill;
        [SerializeField] private Transform _barRoot;   //# 빌보드 회전 대상 (HpBar Canvas)

        private Transform _cam;

        //# 풀 재사용 시 재구독 + 카메라 재캐시 + 현재 HP 반영.
        private void OnEnable()
        {
            var mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;

            if (_health == null) return;
            _health.OnChanged += HandleChanged;
            HandleChanged(_health.Current, _health.Max);
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnChanged -= HandleChanged;
        }

        //# 빌보드 — HP 바가 카메라 정면을 향하게.
        private void LateUpdate()
        {
            if (_barRoot != null && _cam != null)
                _barRoot.rotation = _cam.rotation;
        }

        private void HandleChanged(int current, int max)
        {
            if (_fill != null)
                _fill.fillAmount = max > 0 ? (float)current / max : 0f;
        }
    }
}
