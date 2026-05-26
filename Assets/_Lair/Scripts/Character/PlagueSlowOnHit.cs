using Lair.Battle;
using Lair.Card;
using UnityEngine;

namespace Lair.Character
{
    //# 플레이그(역병귀) 전용 — 공격 적중 시 영웅에게 짧은 둔화 부착. IAttacker.OnHit 구독.
    //# 플레이그가 계속 때리면 HeroAuraRunner 의 재부착 연장으로 둔화 지속.
    [RequireComponent(typeof(MeleeAttacker))]
    public class PlagueSlowOnHit : MonoBehaviour
    {
        //# 플레이그 둔화 배율의 불변 baseline (지속 스폰). 컴파일타임 고정 —
        //# 직렬화·풀 재사용·SetSlowFactor 호출 어디에도 오염되지 않는다.
        //# ApplyMonsterStats 가 SetSlowFactor(BaseSlowFactor × SlowFactorMul) 로 매 Pop 갱신.
        public const float BaseSlowFactor = 0.8f;

        //# 런타임 적용값 캐시 — HandleHit 가 읽는 현재값. ApplyMonsterStats 가 Pop 마다 덮어쓴다.
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

        //# 강화 카드(PlagueSlowBoost)가 호출 — 둔화 배율을 더 강하게.
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
