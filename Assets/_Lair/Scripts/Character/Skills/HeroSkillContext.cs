using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# IHeroSkillContext 실구현 — CharacterRegistry.Monsters(살아있음+교전)를 SkillGeometry 로 필터링해 데미지/넉백.
    public class HeroSkillContext : IArcherSkillContext, IAssassinSkillContext, IBerserkerSkillContext, IPaladinSkillContext
    {
        private readonly Transform _hero;

        //# 영웅 공격 배율 소스 — 스킬 데미지에 PowerScale 을 입혀 근접과 일관. 생성자 1회 캐싱.
        private readonly IAttacker _heroAttacker;

        //# 회피 도약 이동 소유자 — 영웅 AutoCombatAI(없으면 위치를 직접 옮긴다). 생성자 1회 캐싱(Rule 02 §5).
        private readonly AutoCombatAI _heroAi;

        //# 암살자 스킬용 — 영웅 Health(무적·받는 피해 배율)·생존 판정·충돌 반경. 생성자 1회 캐싱.
        private readonly Health _heroHealth;
        private readonly IInvulnerable _heroInvulnerable;
        private readonly float _heroColliderRadius;

        //# 광전사 스킬용 — 영웅 공격 쿨다운 배율(MeleeAttacker.CooldownScale)·진행 중 당김 목록. 생성자 1회 캐싱.
        private readonly MeleeAttacker _heroMelee;
        private readonly List<PullState> _pulls = new List<PullState>();

        private sealed class PullState
        {
            public Transform Transform;
            public IHealth Health;
            public Vector3 Start;
            public Vector3 Destination;
            public float Elapsed;
            public float Duration;
        }

        //# 성기사 스킬용 - 영웅 디버프 슬롯 제어(HeroAuraRunner 는 스폰 시점에 추가될 수 있어 첫 사용 때 1회 캐싱)·기절 대상 AI 캐시.
        private IHeroDebuffControl _debuffs;
        private readonly Dictionary<Transform, AutoCombatAI> _stunCache = new Dictionary<Transform, AutoCombatAI>();

        //# 연막 지대 영향권 — 매 프레임 갱신. Transform → (이동, 공격자, 충돌 반경) 1회 캐시, 영향받는 몬스터 목록.
        private readonly Dictionary<Transform, SlowTargets> _slowCache = new Dictionary<Transform, SlowTargets>();
        private readonly List<SlowTargets> _slowed = new List<SlowTargets>();

        private sealed class SlowTargets
        {
            public SimpleMover Mover;
            public MeleeAttacker Attacker;
            public float ColliderRadius;
        }

        //# 타겟 스냅샷 재사용 버퍼 — 데미지로 몬스터 사망 시 Monsters 리스트가 수정되므로
        //# 열거(수집)와 변형(데미지/넉백)을 분리한다. 메인스레드 전용 → 재진입 없음.
        private readonly List<CharacterRegistry.Entry> _buffer = new();

        public HeroSkillContext(Transform hero)
        {
            _hero = hero;
            _heroAttacker = _hero != null ? _hero.GetComponent<LairCharacter>()?.Get<IAttacker>() : null;
            _heroAi = _hero != null ? _hero.GetComponent<AutoCombatAI>() : null;
            _heroHealth = _hero != null ? _hero.GetComponent<Health>() : null;
            _heroInvulnerable = _heroHealth;
            _heroMelee = _hero != null ? _hero.GetComponent<MeleeAttacker>() : null;
            _heroColliderRadius = _hero != null ? ColliderRadiusOf(_hero) : 0f;
        }

        public Transform HeroTransform => _hero;

        public bool TryGetHeroTargetPosition(float maxRange, out Vector3 position)
        {
            position = HeroPosition;
            if (_heroAi == null || _heroAi.TryGetCurrentTarget(out Transform target) == false)
                return false;
            Vector3 delta = target.position - HeroPosition;
            delta.y = 0f;
            if (delta.magnitude > maxRange)
                return false;
            position = target.position;
            return true;
        }

        public int DamageAndStunMonstersInSphere(Vector3 center, float radius, int amount, float stunSec)
        {
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) && SkillGeometry.InSphere(e.Transform.position, center, radius))
                {
                    _buffer.Add(e);
                }
            }
            int hit = ApplyAll(HeroPosition, amount, 0f);
            //# 피해로 죽은 몬스터는 기절 불필요 - 생존자만.
            foreach (CharacterRegistry.Entry e in _buffer)
            {
                if (e.Health.IsAlive == false || e.Transform == null)
                    continue;
                if (_stunCache.TryGetValue(e.Transform, out AutoCombatAI ai) == false)
                {
                    ai = e.Transform.GetComponent<AutoCombatAI>();
                    _stunCache[e.Transform] = ai;
                }
                ai?.Stun(stunSec);
            }
            return hit;
        }

        private IHeroDebuffControl Debuffs
        {
            get
            {
                if (_debuffs == null && _hero != null)
                {
                    _debuffs = _hero.GetComponent<Lair.Battle.HeroAuraRunner>();
                }
                return _debuffs;
            }
        }

        public int CountTimedDebuffs() => Debuffs != null ? Debuffs.CountTimedDebuffs() : 0;

        public int ClearTimedDebuffs() => Debuffs != null ? Debuffs.ClearTimedDebuffs() : 0;

        public void HealHeroPercent(float percent)
        {
            if (_heroHealth == null || _heroHealth.IsAlive == false)
                return;
            _heroHealth.Heal(Mathf.RoundToInt(_heroHealth.Max * percent));
        }

        public Vector3 HeroPosition => _hero != null ? _hero.position : Vector3.zero;

        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                if (SkillGeometry.InRing(e.Transform.position, origin, innerRadius, outerRadius) == false)
                    continue;
                _buffer.Add(e);
            }
            return ApplyAll(origin, amount, knockbackStrength);
        }

        public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                if (SkillGeometry.InCone(e.Transform.position, origin, direction, length, halfAngleDeg) == false)
                    continue;
                _buffer.Add(e);
            }
            return ApplyAll(origin, amount, knockbackStrength);
        }

        public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            if (sphereCenters == null || sphereCenters.Count == 0)
                return 0;
            //# 한 몬스터가 여러 구에 동시에 들어도 _buffer 에 1회만 add → union dedup.
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                if (InAnySphere(e.Transform.position, sphereCenters, sphereRadius) == false)
                    continue;
                _buffer.Add(e);
            }
            return ApplyAll(origin, amount, knockbackStrength);
        }

        private static bool InAnySphere(Vector3 p, IReadOnlyList<Vector3> centers, float radius)
        {
            for (int i = 0; i < centers.Count; ++i)
            {
                if (SkillGeometry.InSphere(p, centers[i], radius))
                    return true;
            }
            return false;
        }

        public int DamageMonstersInLine(Vector3 direction, float length, float halfWidth, int amount, float knockbackStrength)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                if (SkillGeometry.InLine(e.Transform.position, origin, direction, length, halfWidth) == false)
                    continue;
                _buffer.Add(e);
            }
            return ApplyAll(origin, amount, knockbackStrength);
        }

        public bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) && SkillGeometry.InSphere(e.Transform.position, origin, searchRadius))
                {
                    _buffer.Add(e);
                }
            }

            point = origin;
            int bestCount = 0;
            float bestOriginDist = float.MaxValue;
            for (int i = 0; i < _buffer.Count; ++i)
            {
                Vector3 candidate = _buffer[i].Transform.position;
                int count = 0;
                Vector3 sum = Vector3.zero;
                for (int j = 0; j < _buffer.Count; ++j)
                {
                    Vector3 other = _buffer[j].Transform.position;
                    if (SkillGeometry.InSphere(other, candidate, clusterRadius))
                    {
                        ++count;
                        sum += other;
                    }
                }
                //# 밀집도 우선, 동률이면 영웅에 가까운 지점.
                float originDist = (candidate - origin).sqrMagnitude;
                if (count > bestCount || (count == bestCount && originDist < bestOriginDist))
                {
                    bestCount = count;
                    bestOriginDist = originDist;
                    point = sum / count;
                }
            }
            return bestCount > 0;
        }

        public bool IsHeroActive
        {
            get
            {
                if (_heroHealth != null && _heroHealth.IsAlive == false)
                    return false;
                return _heroAttacker == null || _heroAttacker.Enabled;
            }
        }

        public bool TryFindTopThreat(float maxRange, out Vector3 position, out IHealth health)
        {
            Vector3 origin = HeroPosition;
            position = origin;
            health = null;
            float bestScore = -1f;
            float bestDist = float.MaxValue;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) == false)
                    continue;
                float dist = (e.Transform.position - origin).magnitude;
                if (dist > maxRange)
                    continue;
                float score = SkirmishBehavior.ThreatScore(e.Attacker, e.Health);
                if (score > bestScore || (Mathf.Approximately(score, bestScore) && dist < bestDist))
                {
                    bestScore = score;
                    bestDist = dist;
                    position = e.Transform.position;
                    health = e.Health;
                }
            }
            return health != null;
        }

        public bool DamageMonster(IHealth target, int amount)
        {
            if (target == null || target.IsAlive == false)
                return false;
            float scale = _heroAttacker != null ? _heroAttacker.PowerScale : 1f;
            int scaledAmount = Mathf.RoundToInt(amount * scale);
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e == null || e.Health != target)
                    continue;
                Apply(e, HeroPosition, scaledAmount, 0f);
                break;
            }
            return target.IsAlive == false;
        }

        public void TeleportHero(Vector3 destination)
        {
            if (_heroAi != null)
            {
                _heroAi.Teleport(destination);
            }
            else if (_hero != null)
            {
                _hero.position = destination;
            }
        }

        public void GrantHeroInvulnerability(float seconds)
        {
            _heroInvulnerable?.GrantInvulnerability(seconds);
        }

        public void MultiplyHeroDamageTaken(float mul)
        {
            if (_heroHealth != null)
            {
                _heroHealth.DamageTakenScale *= mul;
            }
        }

        public void MultiplyHeroAttackCooldown(float mul)
        {
            if (_heroMelee != null)
            {
                _heroMelee.CooldownScale *= mul;
            }
        }

        public void SuspendHeroRetarget(float seconds)
        {
            _heroAi?.SuspendRetarget(seconds);
        }

        public int BeginMonsterPull(float radius, int maxTargets, float pullToDistance, float durationSec, int damage)
        {
            Vector3 origin = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) && SkillGeometry.InSphere(e.Transform.position, origin, radius))
                {
                    _buffer.Add(e);
                }
            }
            _buffer.Sort((a, b) => (a.Transform.position - origin).sqrMagnitude.CompareTo((b.Transform.position - origin).sqrMagnitude));
            if (_buffer.Count > maxTargets)
            {
                _buffer.RemoveRange(maxTargets, _buffer.Count - maxTargets);
            }

            //# 피해(영웅 공격 배율) 후 생존자만 당긴다 — 이미 pullToDistance 안쪽이면 제자리.
            float scale = _heroAttacker != null ? _heroAttacker.PowerScale : 1f;
            int scaledAmount = Mathf.RoundToInt(damage * scale);
            int pulled = 0;
            foreach (CharacterRegistry.Entry e in _buffer)
            {
                if (Valid(e) == false)
                    continue;
                Vector3 start = e.Transform.position;
                Apply(e, origin, scaledAmount, 0f);
                ++pulled;
                if (e.Health.IsAlive == false)
                    continue;
                Vector3 away = start - origin;
                away.y = 0f;
                if (away.magnitude <= pullToDistance)
                    continue;
                _pulls.Add(new PullState
                {
                    Transform = e.Transform,
                    Health = e.Health,
                    Start = start,
                    Destination = new Vector3(origin.x, start.y, origin.z) + away.normalized * pullToDistance,
                    Duration = Mathf.Max(0.01f, durationSec),
                });
            }
            return pulled;
        }

        public void TickMonsterPull(float dt)
        {
            for (int i = _pulls.Count - 1; i >= 0; --i)
            {
                PullState pull = _pulls[i];
                //# 사망/풀 반환된 몬스터는 목록에서 제거.
                if (pull.Transform == null || pull.Transform.gameObject.activeInHierarchy == false || pull.Health.IsAlive == false)
                {
                    _pulls.RemoveAt(i);
                    continue;
                }
                pull.Elapsed += dt;
                pull.Transform.position = Vector3.Lerp(pull.Start, pull.Destination, Mathf.Clamp01(pull.Elapsed / pull.Duration));
                if (pull.Elapsed >= pull.Duration)
                {
                    _pulls.RemoveAt(i);
                }
            }
        }

        public void RequestHeroSlip()
        {
            _heroAi?.RequestSlip();
        }

        public void ApplyMonsterSlowZone(Vector3 center, float radius, float speedMul, float rangeMul, float minRange)
        {
            //# 파괴/비활성(풀 반환) 몬스터는 추적에서 제거 — 풀 OnEnable 이 스케일을 리셋한다.
            for (int i = _slowed.Count - 1; i >= 0; --i)
            {
                SlowTargets t = _slowed[i];
                if (t.Mover == null || t.Mover.isActiveAndEnabled == false)
                {
                    _slowed.RemoveAt(i);
                }
            }

            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e == null || e.Transform == null || e.Health == null || e.Health.IsAlive == false)
                    continue;
                SlowTargets targets = SlowTargetsOf(e.Transform);
                if (targets.Mover == null)
                    continue;

                if (SkillGeometry.InSphere(e.Transform.position, center, radius))
                {
                    targets.Mover.SkillSpeedScale = speedMul;
                    if (targets.Attacker != null)
                    {
                        float floor = Mathf.Max(minRange, _heroColliderRadius + targets.ColliderRadius + Lair.Battle.MonsterRangeModifier.ClearanceMargin);
                        targets.Attacker.SetRangeScale(rangeMul, floor);
                    }
                    if (_slowed.Contains(targets) == false)
                    {
                        _slowed.Add(targets);
                    }
                }
                else if (_slowed.Remove(targets))
                {
                    Restore(targets);
                }
            }
        }

        public void ClearMonsterSlowZone()
        {
            foreach (SlowTargets targets in _slowed)
            {
                Restore(targets);
            }
            _slowed.Clear();
        }

        private static void Restore(SlowTargets targets)
        {
            if (targets.Mover != null)
            {
                targets.Mover.SkillSpeedScale = 1f;
            }
            if (targets.Attacker != null)
            {
                targets.Attacker.SetRangeScale(1f, 0f);
            }
        }

        //# Transform → 컴포넌트 1회 캐시(Rule 02 §5) — 풀 재사용 인스턴스는 같은 Transform 이라 안전.
        private SlowTargets SlowTargetsOf(Transform t)
        {
            if (_slowCache.TryGetValue(t, out SlowTargets targets) == false)
            {
                targets = new SlowTargets
                {
                    Mover = t.GetComponent<SimpleMover>(),
                    Attacker = t.GetComponent<MeleeAttacker>(),
                    ColliderRadius = ColliderRadiusOf(t),
                };
                _slowCache[t] = targets;
            }
            return targets;
        }

        //# 캡슐 콜라이더 반경 x 월드 스케일(XZ 최대) — 없으면 0(하한이 minRange 로만 걸린다).
        private static float ColliderRadiusOf(Transform t)
        {
            CapsuleCollider capsule = t.GetComponentInChildren<CapsuleCollider>();
            if (capsule == null)
                return 0f;
            Vector3 scale = capsule.transform.lossyScale;
            return capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        public int CountMonstersInRadius(float radius)
        {
            Vector3 origin = HeroPosition;
            int count = 0;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (Valid(e) && SkillGeometry.InSphere(e.Transform.position, origin, radius))
                {
                    ++count;
                }
            }
            return count;
        }

        public Vector3 ClampToArena(Vector3 position)
        {
            return _heroAi != null ? _heroAi.ClampToArena(position) : position;
        }

        public void BeginLeap(Vector3 destination, float duration)
        {
            if (_heroAi != null)
            {
                _heroAi.BeginLeap(destination, duration);
            }
            else if (_hero != null)
            {
                _hero.position = destination;
            }
        }

        public Vector3 MonsterCentroid(float radius)
        {
            if (CharacterRegistry.TryGetThreatCentroidMonster(HeroPosition, radius, out Vector3 c, out int n) && n > 0)
                return c;
            return HeroPosition;
        }

        //# 스냅샷 버퍼를 순회하며 데미지/넉백 적용. 이 루프에서 몬스터가 죽어 Monsters 가 수정돼도
        //# 순회 대상은 _buffer 라 안전. 반환값 = 실제 데미지를 적용한 수.
        private int ApplyAll(Vector3 origin, int amount, float knockbackStrength)
        {
            //# 적용 시점 live read — 부착된 디버프(HeroAttackDown/Weaken)가 자동 반영. null 이면 배율 1.
            //# 근접(MeleeAttacker)과 동일한 RoundToInt 라운딩. 넉백은 무변경.
            float scale = _heroAttacker != null ? _heroAttacker.PowerScale : 1f;
            int scaledAmount = Mathf.RoundToInt(amount * scale);
            int hit = 0;
            foreach (CharacterRegistry.Entry e in _buffer)
            {
                //# 수집~적용 사이 다른 효과로 죽었을 수 있으므로 재확인.
                if (Valid(e) == false)
                    continue;
                Apply(e, origin, scaledAmount, knockbackStrength);
                ++hit;
            }
            return hit;
        }

        private static bool Valid(CharacterRegistry.Entry e)
            => e != null && e.Transform != null && e.Health != null && e.Health.IsAlive && e.IsEngaging;

        private static void Apply(CharacterRegistry.Entry e, Vector3 origin, int amount, float knockback)
        {
            //# 데미지색 스탬프 — TakeDamage 가 OnChanged 를 동기 발행하기 전에 찍어야 DamageFeedback 이 팝업/임팩트를 띄운다(스킬=영웅 흰색, 근접과 일관).
            //# Valid(e) 가 Transform 비null 보장. GetComponent 는 스킬 발동(쿨다운 게이트) 시점·대상가변이라 캐싱 불가 → 1회성 허용(MeleeAttacker 와 동일).
            e.Transform.GetComponent<LairCharacter>()?.Get<IDamageColorSink>()?.StampDamageColor(Color.white);
            e.Health.TakeDamage(amount);
            //# 망령 각성 — 받는 넉백 세기 배율 (monster-growth-currencies §3.2). 넉백 스킬이 모두 이 지점을 거친다.
            if (e.Health is Health health)
            {
                knockback *= health.KnockbackTakenMul;
            }
            if (knockback > 0f)
            {
                Vector3 away = e.Transform.position - origin;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                {
                    e.Transform.position += away.normalized * knockback;
                }
            }
        }
    }
}
