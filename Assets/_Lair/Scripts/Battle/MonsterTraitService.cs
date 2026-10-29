using System;
using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 종족 고유 특성 6종 실행 (monster-growth-currencies §3.4) — 전투 시작에 확정한 종족별 단계로 동작. 단계 0 종족은 아무것도 하지 않는다.
    //# 시한 배율(원한 공격력·잔상 이속·처형)은 MonsterBuffService 의 매 틱 리셋 "뒤" Tick 에서 곱한다(같은 오버레이 표면, 곱연산 합성 E-19).
    public class MonsterTraitService : IDisposable
    {
        private class Hook
        {
            public Transform Transform;
            public EMonster Species;
            public Health Health;
            public MeleeAttacker Attacker;
            public SimpleMover Mover;
            public PlagueToxinOnHit Toxin;
            public Action Died;
            public Action Dodged;
            public Action<IHealth> Hit;
        }

        private struct Timed
        {
            public float Mul;
            public float Until;
        }

        private readonly GrowthDatabase _growth;
        private readonly Dictionary<EMonster, int> _tiers;
        private readonly Health _hero;
        private readonly Transform _heroTransform;
        private readonly HeroAuraRunner _heroRunner;
        private readonly MeleeAttacker _heroAttacker;
        private readonly Dictionary<GameObject, Hook> _hooks = new Dictionary<GameObject, Hook>();
        private readonly Dictionary<Transform, Timed> _grudge = new Dictionary<Transform, Timed>();
        private readonly Dictionary<Transform, Timed> _haste = new Dictionary<Transform, Timed>();
        private readonly List<Transform> _expired = new List<Transform>();
        private float _now;

        public MonsterTraitService(GrowthDatabase growth, Dictionary<EMonster, int> tiers, Health hero, HeroAuraRunner heroRunner, MeleeAttacker heroAttacker)
        {
            _growth = growth;
            _tiers = tiers;
            _hero = hero;
            _heroTransform = hero != null ? hero.transform : null;
            _heroRunner = heroRunner;
            _heroAttacker = heroAttacker;
            if (_heroAttacker != null && TierOf(EMonster.Wisp) > 0)
            {
                _heroAttacker.OnDamageDealt += HandleHeroDamageDealt;
            }
        }

        public bool HasAny
        {
            get
            {
                foreach (KeyValuePair<EMonster, int> pair in _tiers)
                {
                    if (pair.Value > 0)
                        return true;
                }
                return false;
            }
        }

        public int TierOf(EMonster species) => _tiers.TryGetValue(species, out int t) ? Mathf.Min(t, GrowthDatabase.TraitMaxTier) : 0;

        private float Param(EMonster species, string key) => _growth.TraitParam(species, TierOf(species), key);

        //# 스폰·소급마다 호출 — 인스턴스당 구독은 1회(풀 재사용 인스턴스는 그대로 재사용). spawned = 새로 나온 몬스터(잔상 회피 충전).
        public void OnMonsterConfigured(GameObject monster, EMonster species, bool spawned)
        {
            if (monster == null || TierOf(species) <= 0)
                return;
            if (_hooks.TryGetValue(monster, out Hook hook) == false)
            {
                hook = CreateHook(monster, species);
                _hooks[monster] = hook;
            }
            if (spawned && species == EMonster.Phantom && hook.Health != null)
            {
                hook.Health.GrantDodges(Mathf.RoundToInt(Param(EMonster.Phantom, "dodgeCount")));
            }
        }

        //# 1회성 캐싱(Rule 02 §5) — 구독 대상 컴포넌트를 훅 생성 시점에만 찾는다.
        private Hook CreateHook(GameObject monster, EMonster species)
        {
            Hook hook = new Hook
            {
                Transform = monster.transform,
                Species = species,
                Health = monster.GetComponent<Health>(),
                Attacker = monster.GetComponent<MeleeAttacker>(),
                Mover = monster.GetComponent<SimpleMover>(),
                Toxin = monster.GetComponent<PlagueToxinOnHit>(),
            };
            if (hook.Health != null && (species == EMonster.Wraith || species == EMonster.Plague))
            {
                hook.Died = () => HandleDied(hook);
                hook.Health.OnDied += hook.Died;
            }
            if (hook.Health != null && species == EMonster.Phantom)
            {
                hook.Dodged = () => HandleDodged(hook);
                hook.Health.OnDodged += hook.Dodged;
            }
            if (hook.Attacker != null && species == EMonster.Hex)
            {
                hook.Hit = target => HandleHexHit(target);
                hook.Attacker.OnHit += hook.Hit;
            }
            return hook;
        }

        //# 가시 껍질 — 영웅 기본 공격(범위 베기 대상마다)이 도깨비불에 맞으면 받은 피해 x 비율을 영웅에게 반사. 스킬 피해는 이 경로가 아니라 제외.
        private void HandleHeroDamageDealt(IHealth target, int amount)
        {
            if (_hero == null || _hero.IsAlive == false || target is not Health victim || victim == null)
                return;
            if (_hooks.TryGetValue(victim.gameObject, out Hook hook) == false || hook.Species != EMonster.Wisp)
                return;
            //# amount = 도깨비불이 실제로 잃은 HP (MeleeAttacker 가 TakeDamage 전후 차이로 통지).
            if (amount <= 0)
                return;
            _hero.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(amount * Param(EMonster.Wisp, "reflectRatio"))));
        }

        //# 저주 누적 — 저주술사 적중마다 영웅 받는 피해 +perStack/중첩, 마지막 적중 후 stackSec 유지.
        private void HandleHexHit(IHealth target)
        {
            if (_heroRunner == null || _hero == null || ReferenceEquals(target, _hero) == false)
                return;
            CurseStackAura aura = new CurseStackAura(Param(EMonster.Hex, "perStack"), Mathf.RoundToInt(Param(EMonster.Hex, "maxStack")));
            _heroRunner.Attach(aura, Param(EMonster.Hex, "stackSec"));
        }

        private void HandleDied(Hook hook)
        {
            if (hook.Transform == null)
                return;
            Vector3 pos = hook.Transform.position;
            if (hook.Species == EMonster.Wraith)
            {
                ApplyGrudge(pos);
            }
            else if (hook.Species == EMonster.Plague)
            {
                ApplyContagion(pos, hook.Toxin);
            }
        }

        //# 원한 — 반경 안 다른 몬스터 공격력 x powerMul, durationSec. 원한끼리는 중첩 없이 지속만 갱신(E-19).
        private void ApplyGrudge(Vector3 pos)
        {
            float radius = Param(EMonster.Wraith, "radius");
            Timed buff = new Timed { Mul = Param(EMonster.Wraith, "powerMul"), Until = _now + Param(EMonster.Wraith, "durationSec") };
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e?.Transform == null || e.Health == null || e.Health.IsAlive == false)
                    continue;
                if (FlatDistanceSq(e.Transform.position, pos) > radius * radius)
                    continue;
                _grudge[e.Transform] = buff;
            }
        }

        //# 전염 — 반경 안 영웅의 독 중첩 +addStacks, 지속 갱신. 독이 없으면 새로 건다(1중첩 피해 = 그 역병귀의 독 피해). 독 규칙은 §15.1 단일 진실.
        private void ApplyContagion(Vector3 pos, PlagueToxinOnHit toxin)
        {
            if (_heroRunner == null || _heroTransform == null || toxin == null)
                return;
            float radius = Param(EMonster.Plague, "radius");
            if (FlatDistanceSq(_heroTransform.position, pos) > radius * radius)
                return;
            int add = Mathf.RoundToInt(Param(EMonster.Plague, "addStacks"));
            _heroRunner.Attach(new PlagueToxinAura(toxin.PerStackDamage, add, toxin.MaxStack), toxin.Duration, toxin.transform, null);
        }

        //# 잔상 — 회피 직후 hasteSec 동안 이속 x hasteMul.
        private void HandleDodged(Hook hook)
        {
            if (hook.Transform == null)
                return;
            _haste[hook.Transform] = new Timed { Mul = Param(EMonster.Phantom, "hasteMul"), Until = _now + Param(EMonster.Phantom, "hasteSec") };
        }

        //# MonsterBuffService.Tick(오버레이 리셋) 뒤에 호출 — 시한 배율과 처형을 오버레이에 곱한다.
        public void Tick(float dt)
        {
            _now += dt;
            ApplyTimed(_grudge, true);
            ApplyTimed(_haste, false);
            ApplyExecute();
        }

        private void ApplyTimed(Dictionary<Transform, Timed> table, bool power)
        {
            _expired.Clear();
            foreach (KeyValuePair<Transform, Timed> pair in table)
            {
                if (pair.Key == null || pair.Value.Until <= _now || pair.Key.gameObject.activeInHierarchy == false)
                {
                    _expired.Add(pair.Key);
                    continue;
                }
                if (_hooks.TryGetValue(pair.Key.gameObject, out Hook hook) == false)
                {
                    hook = CachedComponents(pair.Key);
                }
                if (power && hook.Attacker != null)
                {
                    hook.Attacker.PowerScale *= pair.Value.Mul;
                }
                else if (power == false && hook.Mover != null)
                {
                    hook.Mover.SpeedScale *= pair.Value.Mul;
                }
            }
            foreach (Transform t in _expired)
            {
                table.Remove(t);
            }
        }

        //# 원한 대상은 특성 없는 종일 수도 있어 훅이 없으면 1회 캐싱 훅(구독 없음)을 만든다.
        private Hook CachedComponents(Transform t)
        {
            MonsterTag tag = t.GetComponent<MonsterTag>();
            Hook hook = new Hook
            {
                Transform = t,
                Species = tag != null ? tag.Key : (EMonster)(-1),
                Health = t.GetComponent<Health>(),
                Attacker = t.GetComponent<MeleeAttacker>(),
                Mover = t.GetComponent<SimpleMover>(),
            };
            _hooks[t.gameObject] = hook;
            return hook;
        }

        //# 처형 — 영웅 HP 비율이 임계 이하인 동안 사신의 피해 x damageMul.
        private void ApplyExecute()
        {
            if (TierOf(EMonster.Reaper) <= 0 || _hero == null || _hero.Ratio > Param(EMonster.Reaper, "hpThreshold"))
                return;
            float mul = Param(EMonster.Reaper, "damageMul");
            foreach (Hook hook in _hooks.Values)
            {
                if (hook.Species == EMonster.Reaper && hook.Attacker != null && hook.Transform != null && hook.Transform.gameObject.activeInHierarchy)
                {
                    hook.Attacker.PowerScale *= mul;
                }
            }
        }

        private static float FlatDistanceSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        public void Dispose()
        {
            if (_heroAttacker != null)
            {
                _heroAttacker.OnDamageDealt -= HandleHeroDamageDealt;
            }
            foreach (Hook hook in _hooks.Values)
            {
                if (hook.Health != null && hook.Died != null)
                {
                    hook.Health.OnDied -= hook.Died;
                }
                if (hook.Health != null && hook.Dodged != null)
                {
                    hook.Health.OnDodged -= hook.Dodged;
                }
                if (hook.Attacker != null && hook.Hit != null)
                {
                    hook.Attacker.OnHit -= hook.Hit;
                }
            }
            _hooks.Clear();
        }
    }
}
