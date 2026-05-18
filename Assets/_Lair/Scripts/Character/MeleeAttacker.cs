using UnityEngine;

namespace Lair.Character
{
    //# 근접 공격. 인스펙터 또는 Configure 로 스탯 설정.
    //# now 는 외부 주입 (Time.time 직접 참조 금지) — 테스트 가능성 확보.
    public class MeleeAttacker : MonoBehaviour, IAttacker
    {
        [SerializeField] private float _range = 1.5f;
        [SerializeField] private float _cooldown = 1.0f;
        [SerializeField] private int _power = 50;

        public float Range => _range;
        public float Cooldown => _cooldown;
        public int Power => _power;

        private float _lastAttackTime = float.NegativeInfinity;

        //# 테스트 또는 런타임 동적 설정.
        public void Configure(float range, float cooldown, int power)
        {
            _range = range;
            _cooldown = cooldown;
            _power = power;
        }

        public bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)
        {
            if (target == null || target.IsAlive == false) return false;
            float dist = Vector3.Distance(selfPos, targetPos);
            if (dist > _range) return false;
            if (now - _lastAttackTime < _cooldown) return false;

            target.TakeDamage(_power);
            _lastAttackTime = now;
            return true;
        }
    }
}
