using Lair.Battle;
using Lair.Card;
using UnityEngine;

namespace Lair.Character
{
    //# 거미 전용 — 공격 적중 시 영웅에게 짧은 둔화 부착. IAttacker.OnHit 구독.
    //# 거미가 계속 때리면 HeroAuraRunner 의 재부착 연장으로 둔화 지속.
    [RequireComponent(typeof(MeleeAttacker))]
    public class SpiderSlowOnHit : MonoBehaviour
    {
        [SerializeField] private float _slowFactor = 0.8f;
        [SerializeField] private float _duration = 1.5f;

        private IAttacker _attacker;

        private void Awake() => _attacker = GetComponent<IAttacker>();

        //# 풀 재사용 시 구독 누수 방지 — OnEnable 구독 / OnDisable 해제.
        private void OnEnable()
        {
            if (_attacker != null) _attacker.OnHit += HandleHit;
        }

        private void OnDisable()
        {
            if (_attacker != null) _attacker.OnHit -= HandleHit;
        }

        //# 강화 카드(SpiderSlowBoost)가 호출 — 둔화 배율을 더 강하게.
        public void SetSlowFactor(float factor) => _slowFactor = factor;

        private void HandleHit(IHealth target)
        {
            //# 영웅만 대상 — HeroAuraRunner 보유 여부로 판별.
            if (target is not MonoBehaviour mb || mb == null) return;
            var runner = mb.GetComponent<HeroAuraRunner>();
            if (runner == null) return;
            runner.Attach(new SlowAura(mb.GetComponent<IMover>(), _slowFactor), _duration);
        }
    }
}
