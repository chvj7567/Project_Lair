using System.Collections.Generic;
using System.Reflection;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# HeroSkillFx 의 "필수 FX 에셋 없음" LogError 는 키별 1회 정적 캐시(MissingLogged)로 걸러진다 — 실행 순서에 따라 어느 테스트가 로그를 맞을지 달라진다.
    //# FX 프리팹은 (d2) 소관이라 지금은 에셋이 없는 게 정상이므로, 테스트 동안 궁수 FX 키를 "이미 로그함" 으로 채워 로그 경로를 격리하고 끝나면 원복한다.
    public static class ArcherFxLogGuard
    {
        private static readonly EVisual[] ArcherFxKeys =
        {
            EVisual.HeroPiercingShotFx, EVisual.HeroArrowVolleyFx, EVisual.HeroEvadeFx, EVisual.ArcherArrowFx,
            //# 암살자 FX 5종 - 프리팹은 아트 슬라이스 소관이라 지금은 에셋 부재가 정상이다(같은 격리 방식).
            EVisual.HeroSlipFx, EVisual.HeroShadowStrikeFx, EVisual.HeroBladeWhirlFx, EVisual.HeroSmokeBombFx, EVisual.AssassinSlashFx,
        };

        private static HashSet<EVisual> _snapshot;

        private static HashSet<EVisual> Cache()
        {
            FieldInfo field = typeof(HeroSkillFx).GetField("MissingLogged", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "HeroSkillFx.MissingLogged 필드 존재 — 시그니처 변경 감지");
            return (HashSet<EVisual>)field.GetValue(null);
        }

        public static void Suppress()
        {
            HashSet<EVisual> cache = Cache();
            _snapshot = new HashSet<EVisual>(cache);
            foreach (EVisual key in ArcherFxKeys)
            {
                cache.Add(key);
            }
        }

        public static void Restore()
        {
            if (_snapshot == null)
                return;
            HashSet<EVisual> cache = Cache();
            cache.Clear();
            foreach (EVisual key in _snapshot)
            {
                cache.Add(key);
            }
            _snapshot = null;
        }
    }

    public struct LineCall
    {
        public Vector3 Direction;
        public float Length;
        public float HalfWidth;
        public int Amount;
        public float Knockback;
    }

    public struct RingCall
    {
        public float Inner;
        public float Outer;
        public int Amount;
        public float Knockback;
    }

    public struct SphereCall
    {
        public List<Vector3> Centers;
        public float Radius;
        public int Amount;
        public float Knockback;
    }

    public struct LeapCall
    {
        public Vector3 Destination;
        public float Duration;
    }

    //# IArcherSkillContext 호출 기록 더블 — 모든 멤버 구현(부분 더블 금지).
    public class RecordingArcherContext : IArcherSkillContext
    {
        public Vector3 HeroPosition { get; set; } = Vector3.zero;
        public Vector3 Centroid = new Vector3(4f, 0f, 0f);
        public bool HasDensePoint = true;
        public Vector3 DensePoint = new Vector3(3f, 0f, 1f);
        public int NearbyCount;
        public float ArenaRadius = 7f;

        public readonly List<LineCall> Lines = new List<LineCall>();
        public readonly List<RingCall> Rings = new List<RingCall>();
        public readonly List<SphereCall> Spheres = new List<SphereCall>();
        public readonly List<LeapCall> Leaps = new List<LeapCall>();
        public readonly List<float> CentroidRadii = new List<float>();
        public readonly List<float> CountRadii = new List<float>();
        public readonly List<KeyValuePair<float, float>> DenseSearches = new List<KeyValuePair<float, float>>();

        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
        {
            Rings.Add(new RingCall { Inner = innerRadius, Outer = outerRadius, Amount = amount, Knockback = knockbackStrength });
            return 0;
        }

        public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength)
        {
            return 0;
        }

        public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength)
        {
            Spheres.Add(new SphereCall { Centers = new List<Vector3>(sphereCenters), Radius = sphereRadius, Amount = amount, Knockback = knockbackStrength });
            return 0;
        }

        public Vector3 MonsterCentroid(float radius)
        {
            CentroidRadii.Add(radius);
            return Centroid;
        }

        public int DamageMonstersInLine(Vector3 direction, float length, float halfWidth, int amount, float knockbackStrength)
        {
            Lines.Add(new LineCall { Direction = direction, Length = length, HalfWidth = halfWidth, Amount = amount, Knockback = knockbackStrength });
            return 0;
        }

        public bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point)
        {
            DenseSearches.Add(new KeyValuePair<float, float>(searchRadius, clusterRadius));
            point = DensePoint;
            return HasDensePoint;
        }

        public int CountMonstersInRadius(float radius)
        {
            CountRadii.Add(radius);
            return NearbyCount;
        }

        public Vector3 ClampToArena(Vector3 position)
        {
            return KiteBehavior.ClampToRadius(position, ArenaRadius);
        }

        public void BeginLeap(Vector3 destination, float duration)
        {
            Leaps.Add(new LeapCall { Destination = destination, Duration = duration });
        }
    }

    //# IHeroSkillContext 만 구현한 더블 — 궁수 컨텍스트가 아님.
    public class PlainHeroSkillContext : IHeroSkillContext
    {
        public Vector3 HeroPosition => Vector3.zero;
        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength) { return 0; }
        public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
        public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { return 0; }
        public Vector3 MonsterCentroid(float radius) { return Vector3.zero; }
    }
}
