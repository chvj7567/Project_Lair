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

        //# 영웅 한정 흐름 역전 스위치 (§B.2). 영웅 프리팹만 true. SerializeField 라 OnEnable 리셋 불요.
        [SerializeField] private bool _deferStrike;

        public float Range => _range;
        public float Cooldown => _cooldown;
        public int Power => _power;
        public bool DeferStrike => _deferStrike;

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

        //# 데미지 숫자 대표색. AttackJuice 가 OnEnable 에 몸체색(영웅은 흰색) 주입. 기본 백색.
        public Color DamageColor { get; set; } = Color.white;

        private float _lastAttackTime = float.NegativeInfinity;

        //# 영웅 흐름 역전 — TryBeginAttack 가 캐싱한 strike 대상. TryApplyStrike 가 소비.
        private IHealth _pendingTarget;
        private Vector3 _pendingSelfPos;
        private Vector3 _pendingTargetPos;

        //# CHMPool 재사용 시 쿨다운 기록 + 오버레이 배율 리셋.
        private void OnEnable()
        {
            _lastAttackTime = float.NegativeInfinity;
            CooldownScale = 1f;
            PowerScale = 1f;
            _pendingTarget = null;
        }

        //# 테스트 또는 런타임 동적 설정.
        public void Configure(float range, float cooldown, int power)
        {
            _range = range;
            _cooldown = cooldown;
            _power = power;
        }

        //# 몬스터(DeferStrike=false) 즉시 경로 — 현행 100% 보존. 검사+데미지+OnHit 일체.
        public bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)
        {
            if (target == null || target.IsAlive == false) return false;
            float dist = Vector3.Distance(selfPos, targetPos);
            if (dist > _range) return false;
            if (now - _lastAttackTime < _cooldown * CooldownScale) return false;

            _lastAttackTime = now;
            ApplyDamage(target);
            return true;
        }

        //# 영웅 흐름 역전 — 개시 판정 (§2.4·§2.4-a). 사거리·쿨다운[CooldownScale 개시시점]·타겟 유효 검사.
        //# 통과 시 쿨다운 기록 + 타겟 캐싱. 데미지 적용 안 함.
        public bool TryBeginAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now)
        {
            if (target == null || target.IsAlive == false) return false;
            float dist = Vector3.Distance(selfPos, targetPos);
            if (dist > _range) return false;
            if (now - _lastAttackTime < _cooldown * CooldownScale) return false;

            _lastAttackTime = now;
            _pendingTarget = target;
            _pendingSelfPos = selfPos;
            _pendingTargetPos = targetPos;
            return true;
        }

        //# 영웅 흐름 역전 — strike 프레임 (§2.4·§2.4-a·§2.5). 캐싱 타겟 사거리·생존 재검사 후 데미지[PowerScale strike시점].
        //# 재검사 실패(이탈/사망)면 헛스윙 — 데미지 없이 false. 쿨다운은 이미 개시 시점 기록되어 다음 공격 정상.
        public bool TryApplyStrike(float now)
        {
            IHealth target = _pendingTarget;
            _pendingTarget = null;
            if (target == null || target.IsAlive == false) return false;
            float dist = Vector3.Distance(_pendingSelfPos, _pendingTargetPos);
            if (dist > _range) return false;

            ApplyDamage(target);
            return true;
        }

        //# 데미지 색 스탬프 + TakeDamage[PowerScale] + OnHit. 즉시/strike 공통.
        //# PowerScale 은 호출 시점에 읽힘 — strike 경로면 strike 순간 배율(§2.4-a).
        private void ApplyDamage(IHealth target)
        {
            //# 데미지 숫자 색 스탬프 — TakeDamage 가 OnChanged 를 동기 발행하기 전에 찍어야 한다.
            if (target is Component tc && tc != null)
                tc.GetComponent<IDamageColorSink>()?.StampDamageColor(DamageColor);

            target.TakeDamage(Mathf.RoundToInt(_power * PowerScale));
            OnHit?.Invoke(target);
        }
    }
}
