using System.Collections.Generic;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public struct ZoneCall
    {
        public Vector3 Center;
        public float Radius;
        public float SpeedMul;
        public float RangeMul;
        public float MinRange;
    }

    //# IAssassinSkillContext 호출 기록 더블 - 모든 멤버 구현(부분 더블 금지).
    public class RecordingAssassinContext : IAssassinSkillContext
    {
        public Vector3 HeroPosition { get; set; } = Vector3.zero;
        public bool IsHeroActive { get; set; } = true;
        public int Nearby;
        public bool HasThreat = true;
        public Vector3 ThreatPosition = new Vector3(6f, 0f, 0f);
        public bool KillOnDamage;
        public float ArenaRadius = 7f;

        public readonly List<float> TopThreatRanges = new List<float>();
        public readonly List<float> CountRadii = new List<float>();
        public readonly List<int> MonsterDamage = new List<int>();
        public readonly List<Vector3> Teleports = new List<Vector3>();
        public readonly List<float> Invulns = new List<float>();
        public readonly List<float> TakenMuls = new List<float>();
        public readonly List<RingCall> Rings = new List<RingCall>();
        public readonly List<ZoneCall> Zones = new List<ZoneCall>();
        public int SlipRequests;
        public int ZoneClears;

        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
        {
            Rings.Add(new RingCall { Inner = innerRadius, Outer = outerRadius, Amount = amount, Knockback = knockbackStrength });
            return 0;
        }

        public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
        public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { return 0; }
        public Vector3 MonsterCentroid(float radius) { return Vector3.zero; }

        public int CountMonstersInRadius(float radius)
        {
            CountRadii.Add(radius);
            return Nearby;
        }

        public bool TryFindTopThreat(float maxRange, out Vector3 position, out IHealth health)
        {
            TopThreatRanges.Add(maxRange);
            position = ThreatPosition;
            health = null;
            return HasThreat;
        }

        public bool DamageMonster(IHealth target, int amount)
        {
            MonsterDamage.Add(amount);
            return KillOnDamage;
        }

        public void TeleportHero(Vector3 destination)
        {
            Teleports.Add(destination);
            HeroPosition = destination;
        }

        public Vector3 ClampToArena(Vector3 position)
        {
            return KiteBehavior.ClampToRadius(position, ArenaRadius);
        }

        public void GrantHeroInvulnerability(float seconds) { Invulns.Add(seconds); }
        public void MultiplyHeroDamageTaken(float mul) { TakenMuls.Add(mul); }
        public void RequestHeroSlip() { ++SlipRequests; }

        public void ApplyMonsterSlowZone(Vector3 center, float radius, float speedMul, float rangeMul, float minRange)
        {
            Zones.Add(new ZoneCall { Center = center, Radius = radius, SpeedMul = speedMul, RangeMul = rangeMul, MinRange = minRange });
        }

        public void ClearMonsterSlowZone() { ++ZoneClears; }
    }
}
