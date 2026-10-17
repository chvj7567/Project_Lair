using UnityEngine;

namespace Lair.Character
{
    //# 암살자 그림자 습격 (HP 85%, chapter-3-hero §4) — 위협 점수 최상위 몬스터가 minBlinkDistance 보다 멀고 blinkRange 안이면
    //# 표적 arriveOffset 앞으로 순간이동하며 damage 피해. 처치하면 남은 쿨다운의 killRefundFraction 만큼 환급. 순간이동 후 invulnerableSec 무적.
    [CreateAssetMenu(fileName = "HeroSkill_ShadowStrike", menuName = "Lair/Hero Skills/Shadow Strike")]
    public class ShadowStrikeSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 120;
        [SerializeField] private float _cooldown = 6f;
        [SerializeField] private float _blinkRange = 9f;
        [SerializeField] private float _minBlinkDistance = 2.5f;
        [SerializeField] private float _arriveOffset = 1.2f;
        [SerializeField] private float _invulnerableSec = 0.3f;
        [SerializeField] private float _killRefundFraction = 0.5f;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float BlinkRange => _blinkRange;
        public float MinBlinkDistance => _minBlinkDistance;
        public float ArriveOffset => _arriveOffset;
        public float InvulnerableSec => _invulnerableSec;
        public float KillRefundFraction => _killRefundFraction;

        //# 동시 조건 시 우선순위 — 연막 탄(2) > 그림자 습격(1) > 칼날 선풍(0).
        public override int CastPriority => 1;

        public override IHeroSkillRuntime CreateRuntime() => new ShadowStrikeRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머.
    public class ShadowStrikeRuntime : IHeroSkillRuntime
    {
        //# 트리거 검사 주기 — 명세 고정값(Evade 와 같은 0.25초).
        public const float CheckIntervalSec = 0.25f;

        private readonly ShadowStrikeSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private bool _missingContextLogged;

        public ShadowStrikeRuntime(ShadowStrikeSkillData data)
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

            IAssassinSkillContext assassin = ctx as IAssassinSkillContext;
            if (assassin == null)
            {
                LogMissingContext();
                return;
            }
            //# TimeStop·사망 중엔 순간이동도 멈춘다 — 정지 중 위치가 바뀌면 안 된다.
            if (assassin.IsHeroActive == false)
                return;
            if (assassin.TryFindTopThreat(_data.BlinkRange, out Vector3 targetPosition, out IHealth targetHealth) == false)
                return;

            Vector3 origin = assassin.HeroPosition;
            if (PlanarDistance(origin, targetPosition) < _data.MinBlinkDistance)
                return;   //# 이미 가까움 — 순간이동 낭비 방지, 보류

            Vector3 destination = assassin.ClampToArena(ComputeArrival(origin, targetPosition, _data.ArriveOffset));
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroShadowStrikeFx, origin, 1f, Quaternion.identity);
            assassin.TeleportHero(destination);
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroShadowStrikeFx, destination, 1f, Quaternion.identity);
            assassin.GrantHeroInvulnerability(_data.InvulnerableSec);

            bool killed = assassin.DamageMonster(targetHealth, _data.Damage);
            _cooldownRemain = ComputeCooldownAfterCast(_data.Cooldown, killed, _data.KillRefundFraction);
        }

        public void OnDeactivate() { }

        //# 도착 지점 — 표적에서 영웅 쪽으로 arriveOffset 앞(XZ). 겹치면 표적 위치.
        public static Vector3 ComputeArrival(Vector3 origin, Vector3 targetPosition, float arriveOffset)
        {
            Vector3 toHero = origin - targetPosition;
            toHero.y = 0f;
            if (toHero.sqrMagnitude < 0.0001f)
                return targetPosition;
            Vector3 arrival = targetPosition + toHero.normalized * arriveOffset;
            arrival.y = origin.y;
            return arrival;
        }

        //# 시전 후 남은 쿨다운 — 처치하면 refundFraction 만큼 환급(6.0 → 3.0).
        public static float ComputeCooldownAfterCast(float cooldown, bool killed, float refundFraction)
        {
            return killed ? cooldown * (1f - Mathf.Clamp01(refundFraction)) : cooldown;
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
            Debug.LogError("[ShadowStrikeRuntime] IAssassinSkillContext 미구현 컨텍스트 — 암살자 스킬 비활성");
        }
    }
}
