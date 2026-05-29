using Lair.Battle;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 zone 진입 단계 드라이버. AutoCombatAI 가 비활성화된 동안 영웅을 BattleZone.Center 로 이동시킨다.
    //# 도달 시 BattleZone.NotifyHeroReachedCenter() 호출 후 자기 비활성화. AutoCombatAI 활성화는 BattleController 가 담당.
    //# Rule 02 §5/§7 — IMover/IRotator/IHealth 인터페이스 의존 (구체 클래스 직접 참조 회피).
    public class HeroEntryDriver : MonoBehaviour
    {
        //# 도달 임계값 (m). spec §10 — 0.5m.
        [SerializeField] private float _arriveThreshold = 0.5f;

        private BattleZone _zone;
        private IMover _mover;
        private IRotator _rotator;
        private IHealth _health;
        private bool _notified;

        public void Bind(BattleZone zone)
        {
            _zone = zone;
            _notified = false;
        }

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _rotator = GetComponent<IRotator>();
            _health = GetComponent<IHealth>();
        }

        private void Update()
        {
            if (_zone == null) return;
            if (_notified) return;
            //# 영웅 사망 — march 중단 (현실적으로 발생 안 함, 안전망).
            if (_health != null && _health.IsAlive == false)
            {
                _mover?.Stop();
                return;
            }

            Vector3 center = _zone.Center;
            Vector3 dir = center - transform.position;
            //# Y 평면 거리만 — SimpleMover 가 Y=0 고정이라 일관성 유지.
            dir.y = 0f;
            float dist = dir.magnitude;

            if (dist <= _arriveThreshold)
            {
                _mover?.Stop();
                _notified = true;
                _zone.NotifyHeroReachedCenter();
                enabled = false;   //# 1회 동작 후 비활성화. BattleController 가 AutoCombatAI.enabled = true 로 전환.
                return;
            }

            _rotator?.FaceDirection(dir);
            _mover?.MoveTo(center);
        }
    }
}
