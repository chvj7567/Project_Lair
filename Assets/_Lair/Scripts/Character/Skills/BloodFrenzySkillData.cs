using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Character
{
    //# 광전사 피의 광란 (HP 45%, chapter-4 §4) — triggerRadius 안 몬스터가 triggerMonsterCount 마리 이상이면 durationSec 동안
    //# 영웅 공격 쿨다운 x attackCooldownMul · 받는 피해 x damageTakenMul. 쿨다운은 시전 시점부터 흐른다.
    [CreateAssetMenu(fileName = "HeroSkill_BloodFrenzy", menuName = "Lair/Hero Skills/Blood Frenzy")]
    public class BloodFrenzySkillData : HeroSkillData
    {
        [SerializeField] private float _durationSec = 6f;
        [SerializeField] private float _attackCooldownMul = 0.6f;
        [SerializeField] private float _damageTakenMul = 1.2f;
        [SerializeField] private float _cooldown = 18f;
        [SerializeField] private int _triggerMonsterCount = 3;
        [SerializeField] private float _triggerRadius = 2.2f;

        public float DurationSec => _durationSec;
        public float AttackCooldownMul => _attackCooldownMul;
        public float DamageTakenMul => _damageTakenMul;
        public float Cooldown => _cooldown;
        public int TriggerMonsterCount => _triggerMonsterCount;
        public float TriggerRadius => _triggerRadius;

        //# 동시 조건 시 우선순위 — 사슬 갈고리(2) > 피의 광란(1) > 분쇄 도약(0).
        public override int CastPriority => 1;

        public override IHeroSkillRuntime CreateRuntime() => new BloodFrenzyRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머 + 진행 중 광란(남은 시간·배율 적용 여부·오라 FX 핸들).
    public class BloodFrenzyRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;

        private readonly BloodFrenzySkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private float _frenzyRemain;
        private bool _applied;
        private IBerserkerSkillContext _activeCtx;
        private CHPoolable _auraFx;
        private bool _missingContextLogged;

        public BloodFrenzyRuntime(BloodFrenzySkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            IBerserkerSkillContext berserker = ctx as IBerserkerSkillContext;
            if (berserker == null)
            {
                LogMissingContext();
                return;
            }
            //# TimeStop·사망 중엔 시전도 지속 시간도 멈춘다.
            if (berserker.IsHeroActive == false)
                return;

            _cooldownRemain -= dt;
            if (_applied)
            {
                _frenzyRemain -= dt;
                if (_frenzyRemain <= 0f)
                {
                    EndFrenzy();
                }
            }

            _checkTimer += dt;
            if (_cooldownRemain > 0f || _applied || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            if (berserker.CountMonstersInRadius(_data.TriggerRadius) < _data.TriggerMonsterCount)
                return;

            _activeCtx = berserker;
            _cooldownRemain = _data.Cooldown;
            _frenzyRemain = _data.DurationSec;
            _applied = true;
            berserker.MultiplyHeroAttackCooldown(_data.AttackCooldownMul);
            berserker.MultiplyHeroDamageTaken(_data.DamageTakenMul);
            //# 루프 FX — 영웅을 따라다니고 지속이 끝나면 EndFrenzy 가 풀에 반환한다. 풀/프리팹이 없으면 null(FX 만 생략).
            _auraFx = HeroSkillFx.SpawnAttached(Lair.Data.EVisual.HeroBloodFrenzyFx, berserker.HeroTransform, Vector3.zero, 1f);
        }

        public void OnDeactivate()
        {
            EndFrenzy();
        }

        //# 배율 원복(시전 때 곱한 값의 역수) + 오라 FX 반환. 중복 호출 안전.
        private void EndFrenzy()
        {
            if (_applied == false)
                return;
            _applied = false;
            if (_data.AttackCooldownMul > 0f)
            {
                _activeCtx?.MultiplyHeroAttackCooldown(1f / _data.AttackCooldownMul);
            }
            if (_data.DamageTakenMul > 0f)
            {
                _activeCtx?.MultiplyHeroDamageTaken(1f / _data.DamageTakenMul);
            }
            _activeCtx = null;
            if (_auraFx != null && CHMPool.Instance != null)
            {
                CHMPool.Instance.Push(_auraFx);
            }
            _auraFx = null;
        }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[BloodFrenzyRuntime] IBerserkerSkillContext 미구현 컨텍스트 — 광전사 스킬 비활성");
        }
    }
}
