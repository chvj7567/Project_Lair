using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# IStageRuleContext 실구현 — 영웅 Health·스포너·몬스터 레지스트리·FX 풀을 규칙에 대신 조작해 준다 (chapter-system §3.1).
    //# BattleController 가 규칙이 있는 스테이지에서만 1개 생성(규칙 없는 스테이지는 만들지 않아 기존 동작 불변).
    public class StageRuleContext : IStageRuleContext
    {
        private readonly Health _heroHealth;
        private readonly Transform _hero;
        private readonly IReadOnlyList<Spawner> _spawners;
        private readonly BattleZone _zone;
        private readonly MonsterRangeModifier _range;
        private readonly Action _reapplyMonsterStats;
        private readonly Action<string> _notify;
        private readonly IStringProvider _strings;

        //# 스냅샷 재사용 버퍼 — 데미지로 몬스터가 죽어 Monsters 가 수정돼도 열거와 변형을 분리. 메인스레드 전용.
        private readonly List<CharacterRegistry.Entry> _buffer = new List<CharacterRegistry.Entry>();
        private readonly Dictionary<EMonster, List<Spawner>> _locked = new Dictionary<EMonster, List<Spawner>>();
        //# Transform → SimpleMover 1회 캐시(Rule 02 §5) — 풀 재사용 인스턴스는 같은 Transform 이라 안전.
        private readonly Dictionary<Transform, SimpleMover> _moverCache = new Dictionary<Transform, SimpleMover>();
        //# Transform → IDamageColorSink 1회 캐시(null 포함) — 틱마다 GetComponent 를 반복하지 않는다.
        private readonly Dictionary<Transform, IDamageColorSink> _sinkCache = new Dictionary<Transform, IDamageColorSink>();
        private readonly List<SimpleMover> _slowed = new List<SimpleMover>();
        private readonly HashSet<EVisual> _missingFxLogged = new HashSet<EVisual>();

        public StageRuleContext(
            Health heroHealth, Transform hero, IReadOnlyList<Spawner> spawners, BattleZone zone,
            MonsterRangeModifier range, Action reapplyMonsterStats, Action<string> notify, IStringProvider strings)
        {
            _heroHealth = heroHealth;
            _hero = hero;
            _spawners = spawners ?? Array.Empty<Spawner>();
            _zone = zone;
            _range = range;
            _reapplyMonsterStats = reapplyMonsterStats;
            _notify = notify;
            _strings = strings;
        }

        public Vector3 ArenaCenter => _zone != null ? _zone.Center : Vector3.zero;
        public Vector3 HeroPosition => _hero != null ? _hero.position : Vector3.zero;
        public int HeroMaxHp => _heroHealth != null ? _heroHealth.EffectiveMaxHp : 0;
        public float HeroHpRatio => _heroHealth != null ? _heroHealth.Ratio : 1f;

        public void HealHero(int amount)
        {
            if (_heroHealth != null)
            {
                _heroHealth.Heal(amount);
            }
        }

        public void MultiplyHeroDamageTaken(float mul)
        {
            if (_heroHealth != null)
            {
                _heroHealth.DamageTakenScale *= mul;
            }
        }

        public void SetMonsterRangeRule(float mul, float minRange)
        {
            _range.Set(mul, minRange);
            _reapplyMonsterStats?.Invoke();
        }

        public void ClearMonsterRangeRule()
        {
            _range.Clear();
            _reapplyMonsterStats?.Invoke();
        }

        public Vector3[] LockSpawners(EMonster species)
        {
            List<Spawner> targets = new List<Spawner>();
            foreach (Spawner sp in _spawners)
            {
                if (sp != null && sp.CurrentType == species && sp.IsLocked == false)
                {
                    sp.Lock();
                    targets.Add(sp);
                }
            }
            _locked[species] = targets;

            Vector3[] positions = new Vector3[targets.Count];
            for (int i = 0; i < targets.Count; ++i)
            {
                positions[i] = targets[i].transform.position;
            }
            return positions;
        }

        public void UnlockSpawners(EMonster species)
        {
            if (_locked.TryGetValue(species, out List<Spawner> targets) == false)
                return;
            foreach (Spawner sp in targets)
            {
                sp.Unlock();
            }
            _locked.Remove(species);
        }

        public int DamageMonstersInSpheres(IReadOnlyList<Vector3> centers, float radius, int amount)
        {
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (ValidTarget(e) && InAnySphere(e.Transform.position, centers, radius))
                {
                    _buffer.Add(e);
                }
            }

            int hit = 0;
            foreach (CharacterRegistry.Entry e in _buffer)
            {
                //# 수집~적용 사이 다른 효과로 죽었을 수 있다.
                if (ValidTarget(e) == false)
                    continue;
                //# 데미지색 스탬프 — TakeDamage 가 OnChanged 를 동기 발행하기 전에 찍어야 DamageFeedback 이 팝업을 띄운다.
                SinkOf(e.Transform)?.StampDamageColor(Color.white);
                e.Health.TakeDamage(amount);
                ++hit;
            }
            return hit;
        }

        public bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point)
        {
            Vector3 heroPos = HeroPosition;
            _buffer.Clear();
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (ValidTarget(e) && SkillGeometry.InSphere(e.Transform.position, heroPos, searchRadius))
                {
                    _buffer.Add(e);
                }
            }

            point = heroPos;
            int bestCount = 0;
            float bestHeroDist = float.MaxValue;
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
                float heroDist = (candidate - heroPos).sqrMagnitude;
                if (count > bestCount || (count == bestCount && heroDist < bestHeroDist))
                {
                    bestCount = count;
                    bestHeroDist = heroDist;
                    point = sum / count;
                }
            }
            return bestCount > 0;
        }

        public void SetMonsterSpeedScaleInSpheres(IReadOnlyList<Vector3> centers, float radius, float scale)
        {
            //# 파괴/비활성(풀 반환) 몬스터는 추적에서 제거 — 풀 OnEnable 이 RuleSpeedScale 을 리셋한다.
            for (int i = _slowed.Count - 1; i >= 0; --i)
            {
                if (_slowed[i] == null || _slowed[i].isActiveAndEnabled == false)
                {
                    _slowed.RemoveAt(i);
                }
            }

            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e == null || e.Transform == null || e.Health == null || e.Health.IsAlive == false)
                    continue;
                SimpleMover mover = MoverOf(e.Transform);
                if (mover == null)
                    continue;

                if (InAnySphere(e.Transform.position, centers, radius))
                {
                    mover.RuleSpeedScale = scale;
                    if (_slowed.Contains(mover) == false)
                    {
                        _slowed.Add(mover);
                    }
                }
                else if (_slowed.Remove(mover))
                {
                    mover.RuleSpeedScale = 1f;
                }
            }
        }

        public void ClearMonsterSpeedScale()
        {
            foreach (SimpleMover mover in _slowed)
            {
                if (mover != null)
                {
                    mover.RuleSpeedScale = 1f;
                }
            }
            _slowed.Clear();
        }

        public Vector3 ClampToArena(Vector3 position)
        {
            return _zone != null ? _zone.ClampInside(position) : position;
        }

        public IStageFx ShowFx(EVisual key, Vector3 position, float scale)
        {
            PooledStageFx fx = new PooledStageFx(position, scale);
            CHMResource.Instance.Load<GameObject>(key, prefab =>
            {
                if (prefab == null)
                {
                    LogMissingFx(key);
                    return;
                }
                fx.Attach(prefab);
            });
            return fx;
        }

        public void ShowNotice(int stringId, params object[] args)
        {
            if (_strings == null)
            {
                Debug.LogError($"[StageRuleContext] 문자열 테이블 미등록 — 알림 {stringId} 표시 불가");
                return;
            }
            string format = _strings.GetString(stringId);
            string text = args != null && args.Length > 0 ? string.Format(format, args) : format;
            _notify?.Invoke(text);
        }

        //# 에셋 부재는 조용히 건너뛰지 않는다(E-11 취지) — 키별 1회만 LogError.
        private void LogMissingFx(EVisual key)
        {
            if (_missingFxLogged.Add(key))
            {
                Debug.LogError($"[StageRuleContext] 스테이지 규칙 FX 에셋 없음: {key} — Addressables 등록 필요(Art/FX)");
            }
        }

        private IDamageColorSink SinkOf(Transform t)
        {
            if (_sinkCache.TryGetValue(t, out IDamageColorSink sink) == false)
            {
                sink = t.GetComponent<LairCharacter>()?.Get<IDamageColorSink>();
                _sinkCache[t] = sink;
            }
            return sink;
        }

        private SimpleMover MoverOf(Transform t)
        {
            if (_moverCache.TryGetValue(t, out SimpleMover mover) == false)
            {
                mover = t.GetComponent<SimpleMover>();
                _moverCache[t] = mover;
            }
            return mover;
        }

        private static bool ValidTarget(CharacterRegistry.Entry e)
        {
            return e != null && e.Transform != null && e.Health != null && e.Health.IsAlive && e.IsEngaging;
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

        //# 풀링 FX 핸들 — 프리팹 로드 완료 후 Pop, Release 시 Push. 로드 전 Release 면 생성 취소.
        private sealed class PooledStageFx : IStageFx
        {
            private Vector3 _position;
            private readonly float _scale;
            private CHPoolable _poolable;
            private bool _released;

            public PooledStageFx(Vector3 position, float scale)
            {
                _position = position;
                _scale = scale;
            }

            public void Attach(GameObject prefab)
            {
                if (_released)
                    return;
                _poolable = CHMPool.Instance.Pop(prefab, null);
                if (_poolable == null)
                    return;
                _poolable.transform.position = _position;
                _poolable.transform.localScale = Vector3.one * _scale;
            }

            public void SetPosition(Vector3 position)
            {
                _position = position;
                if (_poolable != null)
                {
                    _poolable.transform.position = position;
                }
            }

            public void Release()
            {
                if (_released)
                    return;
                _released = true;
                if (_poolable != null)
                {
                    CHMPool.Instance.Push(_poolable);
                    _poolable = null;
                }
            }
        }
    }
}
