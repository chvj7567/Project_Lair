using UnityEngine;

namespace Lair.Character
{
    //# 자동전투 행동 — 인터페이스 4개 조합으로만 동작.
    //# 영웅/몬스터 공통. ITargetProvider 구현체로 진영이 결정됨.
    [RequireComponent(typeof(SimpleMover))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(MeleeAttacker))]
    public class AutoCombatAI : MonoBehaviour
    {
        private IMover _mover;
        private IHealth _health;
        private IAttacker _attacker;
        private ITargetProvider _targetProvider;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _health = GetComponent<IHealth>();
            _attacker = GetComponent<IAttacker>();
            _targetProvider = GetComponent<ITargetProvider>();
        }

        private void Update()
        {
            if (_health == null || _health.IsAlive == false)
            {
                _mover?.Stop();
                return;
            }
            if (_targetProvider == null) return;

            if (_targetProvider.TryFindNearest(transform.position, out var t, out var th) == false)
            {
                _mover.Stop();
                return;
            }

            float dist = Vector3.Distance(transform.position, t.position);
            if (dist <= _attacker.Range)
            {
                _mover.Stop();
                _attacker.TryAttack(th, transform.position, t.position, Time.time);
            }
            else
            {
                _mover.MoveTo(t.position);
            }
        }
    }
}
