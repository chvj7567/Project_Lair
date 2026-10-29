using Lair.Battle;
using Lair.Card;
using UnityEngine;

namespace Lair.Character
{
    //# 역병귀 전용 — 공격 적중 시 영웅에게 독 1중첩 부착 (monster-growth-currencies §15.1). IAttacker.OnHit 구독.
    //# 1중첩 피해 = 건 역병귀의 최종 공격력 x _toxinPowerRatio x PoisonDamage 배율. 지속·상한은 ApplyMonsterStats 가 Pop 마다 덮어쓴다.
    [RequireComponent(typeof(LairCharacter))]
    [RequireComponent(typeof(MeleeAttacker))]
    public class PlagueToxinOnHit : MonoBehaviour
    {
        //# 불변 baseline (지속 스폰 복리 방지) — 런타임 값은 Configure 가 baseline x 배율로 매번 절대 대입한다.
        public const float BaseDuration = 3f;
        public const int BaseMaxStack = 3;

        [SerializeField] private float _toxinPowerRatio = 1f;
        [SerializeField] private float _duration = BaseDuration;
        [SerializeField] private int _maxStack = BaseMaxStack;

        private float _damageMul = 1f;
        private float _runtimeDuration = BaseDuration;
        private int _runtimeMaxStack = BaseMaxStack;
        private IAttacker _attacker;
        private IHealth _self;
        private MeleeAttacker _melee;

        public float PerStackDamage => _melee != null ? _melee.Power * _melee.PowerScale * _toxinPowerRatio * _damageMul : 0f;
        public float Duration => _runtimeDuration;
        public int MaxStack => _runtimeMaxStack;

        private void Awake()
        {
            LairCharacter character = GetComponent<LairCharacter>();
            _attacker = character.Get<IAttacker>();
            _self = character.Get<IHealth>();
            _melee = GetComponent<MeleeAttacker>();
        }

        //# 풀 재사용 시 구독 누수 방지 — OnEnable 구독 / OnDisable 해제.
        private void OnEnable()
        {
            if (_attacker != null) _attacker.OnHit += HandleHit;
        }

        private void OnDisable()
        {
            if (_attacker != null) _attacker.OnHit -= HandleHit;
        }

        //# 스폰·소급마다 호출 — damageMul = PoisonDamage 누적 배율, durationMul = PoisonDuration 배율, maxStackBonus = Debuff Tier1 (+2).
        public void Configure(float damageMul, float durationMul, int maxStackBonus)
        {
            _damageMul = damageMul;
            _runtimeDuration = _duration * durationMul;
            _runtimeMaxStack = _maxStack + maxStackBonus;
        }

        private void HandleHit(IHealth target)
        {
            //# 영웅만 대상 — HeroAuraRunner 보유 여부로 판별.
            if (target is not MonoBehaviour mb || mb == null) return;
            HeroAuraRunner runner = mb.GetComponent<HeroAuraRunner>();
            if (runner == null) return;
            //# Debuff 축 출처 + 가해자 통지 - 성기사 패시브(지속 x0.6)·응징 표적이 이 경로를 쓴다.
            runner.Attach(new PlagueToxinAura(PerStackDamage, 1, _runtimeMaxStack), _runtimeDuration, transform, _self);
        }
    }
}
