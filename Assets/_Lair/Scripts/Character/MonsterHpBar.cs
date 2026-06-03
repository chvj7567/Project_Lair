using UnityEngine;

namespace Lair.Character
{
    //# 빌더(AttachMonsterHpBar)가 몬스터 자식으로 생성하는 래퍼 GameObject 에 부착.
    //# 래퍼는 WorldSpace Canvas + 이 MonsterHpBar. 그 자식에 HpBar.prefab 인스턴스가 nest.
    public class MonsterHpBar : MonoBehaviour
    {
        [SerializeField] private HpBarView _hpBar;   //# nest 된 HpBar.prefab 인스턴스의 View

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
            Camera mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;

            //# 몬스터 바는 현재/최대 텍스트를 숨긴다 (영웅 HUD 와 공유하는 prefab 이라 런타임 토글).
            if (_hpBar != null)
                _hpBar.SetTextVisible(false);

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
            if (_hpBar != null) _hpBar.SetHp(current, max);
        }
    }
}
