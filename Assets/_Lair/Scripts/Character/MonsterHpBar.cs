using UnityEngine;
using UnityEngine.UI;

namespace Lair.Character
{
    //# 빌더(AttachMonsterHpBar)가 몬스터 자식으로 생성하는 래퍼 GameObject 에 부착.
    //# 래퍼는 WorldSpace Canvas + 이 MonsterHpBar. 그 자식에 HpBar.prefab 인스턴스가 nest.
    //# _fill 은 nested HpBar.prefab 의 Background/Fill Image 를 빌더가 주입.
    //# 매 프레임 카메라를 향하도록 빌보드 회전 — 탑다운 45° 카메라에서 잘 보임.
    //# 캐릭터 프리팹의 nested 자식이라 풀 Pop/Push 에 자동 동행.
    public class MonsterHpBar : MonoBehaviour
    {
        [SerializeField] private Image _fill;   //# HpBar.prefab 내부 자식

        private IHealth _health;
        private Transform _cam;

        //# Rule 06 — 상위 캐릭터의 HP 를 인터페이스로 탐색 (구체 클래스 비참조).
        private void Awake()
        {
            _health = GetComponentInParent<IHealth>();
        }

        //# 풀 재사용 시 재구독 + 카메라 재캐시 + 현재 HP 반영.
        private void OnEnable()
        {
            var mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;

            if (_health == null) _health = GetComponentInParent<IHealth>();
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
            if (_cam != null) transform.rotation = _cam.rotation;
        }

        private void HandleChanged(int current, int max)
        {
            if (_fill != null)
                _fill.fillAmount = max > 0 ? (float)current / max : 0f;
        }
    }
}
