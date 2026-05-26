using System;
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

        //# IAttacker.Enabled — MonoBehaviour 의 enabled 와 매핑.
        //# false 면 AutoCombatAI 의 Update 가 정지하므로 공격 시도 자체가 일어나지 않음.
        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        //# B3 — 오버레이 배율. 글로벌 버프(광폭화)/디버프(무력화·약화)가 곱셈 합성. 기본 1.0.
        public float CooldownScale { get; set; } = 1f;
        public float PowerScale { get; set; } = 1f;

        //# B3 — 공격 적중 시 발행. 플레이그 PlagueSlowOnHit 가 구독.
        public event Action<IHealth> OnHit;

        private float _lastAttackTime = float.NegativeInfinity;

        //# CHMPool 재사용 시 쿨다운 기록 + 오버레이 배율 리셋.
        private void OnEnable()
        {
            _lastAttackTime = float.NegativeInfinity;
            CooldownScale = 1f;
            PowerScale = 1f;
        }

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
            if (now - _lastAttackTime < _cooldown * CooldownScale) return false;

            target.TakeDamage(Mathf.RoundToInt(_power * PowerScale));
            _lastAttackTime = now;
            OnHit?.Invoke(target);
            return true;
        }
    }
}
