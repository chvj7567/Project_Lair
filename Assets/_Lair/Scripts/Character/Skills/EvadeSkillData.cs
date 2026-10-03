using UnityEngine;

namespace Lair.Character
{
    //# 궁수 회피 도약 (HP 45%, chapter-system §5.4) — 반경 triggerRadius 안 몬스터가 triggerMonsterCount 마리 이상이면(checkIntervalSec 마다 검사)
    //# 위협 중심 반대로 leapDistance 도약하며 출발 지점에 피해+넉백. 갇힘 탈출 수단. 도약 동안 AI 이동 명령은 잠긴다(이동 소유권은 AutoCombatAI.BeginLeap).
    [CreateAssetMenu(fileName = "HeroSkill_Evade", menuName = "Lair/Hero Skills/Evade")]
    public class EvadeSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 30;
        [SerializeField] private float _cooldown = 10f;
        [SerializeField] private float _leapDistance = 4.5f;
        [SerializeField] private float _leapDurationSec = 0.25f;
        [SerializeField] private float _blastRadius = 2f;
        [SerializeField] private float _knockbackStrength = 2.5f;
        [SerializeField] private int _triggerMonsterCount = 3;
        [SerializeField] private float _triggerRadius = 2f;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float LeapDistance => _leapDistance;
        public float LeapDurationSec => _leapDurationSec;
        public float BlastRadius => _blastRadius;
        public float KnockbackStrength => _knockbackStrength;
        public int TriggerMonsterCount => _triggerMonsterCount;
        public float TriggerRadius => _triggerRadius;

        public override IHeroSkillRuntime CreateRuntime() => new EvadeRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머.
    public class EvadeRuntime : IHeroSkillRuntime
    {
        //# 트리거 검사 주기 — §5.4 "0.25초마다 검사"(파라미터 집합에 없는 명세 고정값).
        public const float CheckIntervalSec = 0.25f;

        private readonly EvadeSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private bool _missingContextLogged;

        public EvadeRuntime(EvadeSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            _cooldownRemain -= dt;
            _checkTimer += dt;
            if (_cooldownRemain > 0f || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            IArcherSkillContext archer = ctx as IArcherSkillContext;
            if (archer == null)
            {
                LogMissingContext();
                return;
            }
            if (archer.CountMonstersInRadius(_data.TriggerRadius) < _data.TriggerMonsterCount)
                return;

            Vector3 origin = archer.HeroPosition;
            Vector3 destination = ComputeLeapDestination(origin, archer.MonsterCentroid(_data.TriggerRadius), _data.LeapDistance, archer.ClampToArena);

            //# 출발 지점 폭발 → 도약. 폭발은 영웅이 떠나기 전 위치 기준.
            archer.DamageMonstersInRing(0f, _data.BlastRadius, _data.Damage, _data.KnockbackStrength);
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroEvadeFx, origin, _data.BlastRadius * 2f, Quaternion.identity);
            archer.BeginLeap(destination, _data.LeapDurationSec);
            _cooldownRemain = _data.Cooldown;
        }

        public void OnDeactivate() { }

        //# 도약 방향 = 위협 중심의 반대(XZ). 중심이 영웅 위치와 거의 같으면(대칭 포위) 원점 쪽으로 — 순수 헬퍼라 테스트 가능.
        public static Vector3 ComputeLeapDirection(Vector3 heroPosition, Vector3 threatCentroid)
        {
            Vector3 away = heroPosition - threatCentroid;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = new Vector3(-heroPosition.x, 0f, -heroPosition.z);
            }
            if (away.sqrMagnitude < 0.0001f)
            {
                away = Vector3.back;
            }
            return away.normalized;
        }

        //# 클램프 뒤 실이동이 leapDistance 의 이 비율 미만이면(가장자리에서 바깥으로 도약 → 제자리) 원점 쪽 방향을 섞어 재계산한다(E-8 탈출 취지).
        public const float MinLeapFraction = 0.5f;

        //# 도약 목적지 — 위협 반대 방향 + 클램프, 실이동이 부족하면 원점 방향을 가산한 방향으로 재계산해 더 멀리 가는 쪽을 택한다. clamp 는 컨텍스트의 아레나 클램프.
        public static Vector3 ComputeLeapDestination(Vector3 origin, Vector3 threatCentroid, float leapDistance, System.Func<Vector3, Vector3> clamp)
        {
            Vector3 dir = ComputeLeapDirection(origin, threatCentroid);
            Vector3 destination = clamp(origin + dir * leapDistance);
            if (PlanarDistance(destination, origin) >= leapDistance * MinLeapFraction)
                return destination;

            Vector3 toOrigin = new Vector3(-origin.x, 0f, -origin.z);
            if (toOrigin.sqrMagnitude < 0.0001f)
                return destination;
            toOrigin.Normalize();
            Vector3 blended = dir + toOrigin;
            Vector3 dir2 = blended.sqrMagnitude > 0.0001f ? blended.normalized : toOrigin;
            Vector3 destination2 = clamp(origin + dir2 * leapDistance);
            return PlanarDistance(destination2, origin) > PlanarDistance(destination, origin) ? destination2 : destination;
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
            Debug.LogError("[EvadeRuntime] IArcherSkillContext 미구현 컨텍스트 — 궁수 스킬 비활성");
        }
    }
}
