using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 규칙이 띄운 FX 한 개의 기록 — Release 횟수까지 세어 이중 반환을 잡는다.
    public class StageFxRecord : IStageFx
    {
        public EVisual Key;
        public Vector3 Position;
        public float Scale;
        public int ReleaseCount;
        public int SetPositionCount;

        public void SetPosition(Vector3 position)
        {
            Position = position;
            ++SetPositionCount;
        }

        public void Release()
        {
            ++ReleaseCount;
        }
    }

    public struct DamageCall
    {
        public List<Vector3> Centers;
        public float Radius;
        public int Amount;
    }

    public struct SpeedCall
    {
        public List<Vector3> Centers;
        public float Radius;
        public float Scale;
    }

    public struct NoticeCall
    {
        public int StringId;
        public object[] Args;
    }

    //# 규칙 엔진 단일 seam(IStageRuleContext)의 호출 기록용 더블 — 모든 멤버를 구현한다.
    public class RecordingStageRuleContext : IStageRuleContext
    {
        public Vector3 ArenaCenter { get; set; } = Vector3.zero;
        public Vector3 HeroPosition { get; set; } = Vector3.zero;
        public int HeroMaxHp { get; set; } = 4000;
        public float HeroHpRatio { get; set; } = 1f;

        //# 전장 반경 — 0 이하면 클램프 없음.
        public float ArenaClampRadius;
        //# LockSpawners 가 돌려줄 스포너 위치 — 기본 1개.
        public Vector3[] SpawnerPositions = { Vector3.zero };
        public bool HasDensePoint;
        public Vector3 DensePoint;

        public readonly List<int> Heals = new List<int>();
        public readonly List<float> DamageMultiplies = new List<float>();
        public float DamageScale = 1f;
        public readonly List<KeyValuePair<float, float>> RangeRuleSets = new List<KeyValuePair<float, float>>();
        public int RangeRuleClears;
        public readonly List<EMonster> Locks = new List<EMonster>();
        public readonly List<EMonster> Unlocks = new List<EMonster>();
        public readonly List<DamageCall> Damages = new List<DamageCall>();
        public readonly List<SpeedCall> SpeedSets = new List<SpeedCall>();
        public int SpeedClears;
        public int DenseFindCalls;
        public float LastSearchRadius;
        public float LastClusterRadius;
        public readonly List<StageFxRecord> Fxs = new List<StageFxRecord>();
        public readonly List<NoticeCall> Notices = new List<NoticeCall>();

        public int TotalHealed
        {
            get
            {
                int sum = 0;
                foreach (int heal in Heals)
                {
                    sum += heal;
                }
                return sum;
            }
        }

        public int LiveFxCount
        {
            get
            {
                int live = 0;
                foreach (StageFxRecord fx in Fxs)
                {
                    if (fx.ReleaseCount == 0)
                    {
                        ++live;
                    }
                }
                return live;
            }
        }

        public void HealHero(int amount)
        {
            Heals.Add(amount);
        }

        public void MultiplyHeroDamageTaken(float mul)
        {
            DamageMultiplies.Add(mul);
            DamageScale *= mul;
        }

        public void SetMonsterRangeRule(float mul, float minRange)
        {
            RangeRuleSets.Add(new KeyValuePair<float, float>(mul, minRange));
        }

        public void ClearMonsterRangeRule()
        {
            ++RangeRuleClears;
        }

        public Vector3[] LockSpawners(EMonster species)
        {
            Locks.Add(species);
            return SpawnerPositions;
        }

        public void UnlockSpawners(EMonster species)
        {
            Unlocks.Add(species);
        }

        public int DamageMonstersInSpheres(IReadOnlyList<Vector3> centers, float radius, int amount)
        {
            Damages.Add(new DamageCall { Centers = new List<Vector3>(centers), Radius = radius, Amount = amount });
            return 0;
        }

        public bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point)
        {
            ++DenseFindCalls;
            LastSearchRadius = searchRadius;
            LastClusterRadius = clusterRadius;
            point = DensePoint;
            return HasDensePoint;
        }

        public void SetMonsterSpeedScaleInSpheres(IReadOnlyList<Vector3> centers, float radius, float scale)
        {
            SpeedSets.Add(new SpeedCall { Centers = new List<Vector3>(centers), Radius = radius, Scale = scale });
        }

        public void ClearMonsterSpeedScale()
        {
            ++SpeedClears;
        }

        public Vector3 ClampToArena(Vector3 position)
        {
            if (ArenaClampRadius <= 0f)
                return position;
            Vector3 offset = position - ArenaCenter;
            offset.y = 0f;
            if (offset.magnitude <= ArenaClampRadius)
                return position;
            Vector3 clamped = offset.normalized * ArenaClampRadius;
            return new Vector3(ArenaCenter.x + clamped.x, position.y, ArenaCenter.z + clamped.z);
        }

        public IStageFx ShowFx(EVisual key, Vector3 position, float scale)
        {
            StageFxRecord fx = new StageFxRecord { Key = key, Position = position, Scale = scale };
            Fxs.Add(fx);
            return fx;
        }

        public void ShowNotice(int stringId, params object[] args)
        {
            Notices.Add(new NoticeCall { StringId = stringId, Args = args });
        }
    }

    //# IHealth 더블 — Current 만 조작하면 Ratio/IsAlive 가 따라온다(Max 100).
    public class StageRuleFakeHero : IHealth
    {
        public int Max => 100;
        public int Current { get; set; } = 100;
        public float Ratio => (float)Current / Max;
        public bool IsAlive => Current > 0;
        public event Action<int, int> OnChanged;
        public event Action OnDied;

        public void TakeDamage(int amount)
        {
            Current = Math.Max(0, Current - amount);
            OnChanged?.Invoke(Current, Max);
            if (Current == 0)
            {
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            Current = Math.Min(Max, Current + amount);
            OnChanged?.Invoke(Current, Max);
        }

        public void SetMax(int max, bool resetCurrent = true) { }
    }

    //# 러너 검증용 규칙 더블 — 활성/틱/비활성 순서와 전달 인자를 공유 로그에 남긴다.
    public class StageProbeRule : IStageRule
    {
        public readonly string Name;
        private readonly List<string> _log;
        public int ActivateCount;
        public int TickCount;
        public int DeactivateCount;
        public float LastElapsed;
        public float LastDt;

        public StageProbeRule(string name, List<string> log)
        {
            Name = name;
            _log = log;
        }

        public EStageRule Id => EStageRule.BossBulwark;
        public bool IsActive { get; private set; }

        public void Activate(IStageRuleContext ctx)
        {
            IsActive = true;
            ++ActivateCount;
            _log.Add("on:" + Name);
        }

        public void Tick(float elapsed, float dt)
        {
            ++TickCount;
            LastElapsed = elapsed;
            LastDt = dt;
            _log.Add("tick:" + Name);
        }

        public void Deactivate()
        {
            IsActive = false;
            ++DeactivateCount;
            _log.Add("off:" + Name);
        }
    }

    //# StageRuleData 조립 헬퍼.
    public static class StageRuleDataFactory
    {
        public static StageRuleData Make(EStageRule ruleId, float? trigger, params (string key, float value)[] ps)
        {
            Dictionary<string, float> map = new Dictionary<string, float>();
            foreach ((string key, float value) in ps)
            {
                map[key] = value;
            }
            return new StageRuleData { RuleId = ruleId, Params = map, HasTrigger = trigger.HasValue, TriggerHpFraction = trigger ?? 0f };
        }

        public static StageRuleParams Params(params (string key, float value)[] ps)
        {
            Dictionary<string, float> map = new Dictionary<string, float>();
            foreach ((string key, float value) in ps)
            {
                map[key] = value;
            }
            return new StageRuleParams(map);
        }

        //# shipped 수치(Stage_Rule_Param.json)와 같은 값 묶음.
        public static StageRuleParams HolySpring(float healPercent = 0.4f, float radius = 3f, float cx = 0f, float cz = 0f)
        {
            return Params(("centerX", cx), ("centerZ", cz), ("radius", radius), ("healPercentPerSec", healPercent),
                ("activeSec", 8f), ("periodSec", 40f), ("firstActiveSec", 20f));
        }

        public static StageRuleParams DenseFog()
        {
            return Params(("monsterRangeMul", 0.7f), ("minRange", 0.8f));
        }

        public static StageRuleParams SealedNest(float species = 1f, float unlock = 0.6f)
        {
            return Params(("lockedSpecies", species), ("unlockHpFraction", unlock));
        }

        public static StageRuleParams BossBulwark()
        {
            return Params(("damageTakenMul", 0.6f), ("durationSec", 15f));
        }

        public static StageRuleParams ArrowRain()
        {
            return Params(("intervalSec", 10f), ("searchRadius", 8f), ("radius", 3.5f), ("tickCount", 4f),
                ("tickIntervalSec", 0.5f), ("damagePerTick", 50f));
        }

        public static StageRuleParams HunterTraps()
        {
            return Params(("trapCount", 6f), ("ringDistance", 3.5f), ("trapRadius", 1.2f), ("lifeSec", 12f),
                ("rearmSec", 15f), ("monsterSpeedMul", 0.5f), ("damagePerSec", 10f));
        }
    }
}
