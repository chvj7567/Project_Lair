using UnityEngine;

namespace Lair.Character
{
    //# 광전사 분쇄 도약 (HP 85%, chapter-4 §4) — 최대 군집 무게중심이 minLeapDistance 보다 멀고 leapRange 안이면 그 지점으로 leapDurationSec 도약(무적),
    //# 착지 시 반경 slamRadius 에 damage + 넉백. 목적지는 전장 안으로 클램프.
    [CreateAssetMenu(fileName = "HeroSkill_CrushingLeap", menuName = "Lair/Hero Skills/Crushing Leap")]
    public class CrushingLeapSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 90;
        [SerializeField] private float _cooldown = 7f;
        [SerializeField] private float _leapRange = 8f;
        [SerializeField] private float _minLeapDistance = 3f;
        [SerializeField] private float _leapDurationSec = 0.3f;
        [SerializeField] private float _invulnerableSec = 0.3f;
        [SerializeField] private float _slamRadius = 2.6f;
        [SerializeField] private float _knockbackStrength = 1.5f;
        [SerializeField] private float _clusterRadius = 2.2f;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float LeapRange => _leapRange;
        public float MinLeapDistance => _minLeapDistance;
        public float LeapDurationSec => _leapDurationSec;
        public float InvulnerableSec => _invulnerableSec;
        public float SlamRadius => _slamRadius;
        public float KnockbackStrength => _knockbackStrength;
        public float ClusterRadius => _clusterRadius;

        //# 동시 조건 시 우선순위 — 사슬 갈고리(2) > 피의 광란(1) > 분쇄 도약(0).
        public override int CastPriority => 0;

        public override IHeroSkillRuntime CreateRuntime() => new CrushingLeapRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머 + 착지 대기(남은 시간·목적지).
    public class CrushingLeapRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;

        private readonly CrushingLeapSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private float _landRemain;
        private Vector3 _destination;
        private bool _missingContextLogged;

        public CrushingLeapRuntime(CrushingLeapSkillData data)
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
            //# TimeStop·사망 중엔 시전도 착지 진행도 멈춘다.
            if (berserker.IsHeroActive == false)
                return;

            _cooldownRemain -= dt;
            if (_landRemain > 0f)
            {
                _landRemain -= dt;
                if (_landRemain <= 0f)
                {
                    Land(berserker);
                }
            }

            _checkTimer += dt;
            if (_cooldownRemain > 0f || _landRemain > 0f || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            if (berserker.TryFindMonsterDensePoint(_data.LeapRange, _data.ClusterRadius, out Vector3 point) == false)
                return;
            float distance = PlanarDistance(berserker.HeroPosition, point);
            if (distance < _data.MinLeapDistance || distance > _data.LeapRange)
                return;   //# 이미 가깝거나 너무 멀다 — 보류

            _destination = berserker.ClampToArena(point);
            berserker.BeginLeap(_destination, _data.LeapDurationSec);
            berserker.GrantHeroInvulnerability(_data.InvulnerableSec);
            _landRemain = _data.LeapDurationSec;
            _cooldownRemain = _data.Cooldown;
        }

        public void OnDeactivate()
        {
            _landRemain = 0f;
        }

        private void Land(IBerserkerSkillContext berserker)
        {
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroCrushingLeapFx, _destination, 1f, Quaternion.identity);
            berserker.DamageMonstersInRing(0f, _data.SlamRadius, _data.Damage, _data.KnockbackStrength);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[CrushingLeapRuntime] IBerserkerSkillContext 미구현 컨텍스트 — 광전사 스킬 비활성");
        }
    }
}
