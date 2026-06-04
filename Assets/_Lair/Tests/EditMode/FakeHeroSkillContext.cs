using System.Collections.Generic;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# IHeroSkillContext 테스트 더블 — 데미지 호출을 기록한다.
    //# 각 Damage* 메서드는 미리 설정한 HitCount 를 반환하고, 호출 파라미터를 로그에 남긴다.
    public class FakeHeroSkillContext : IHeroSkillContext
    {
        public Vector3 HeroPosition { get; set; } = Vector3.zero;

        //# 다음 Damage* 호출이 반환할 피격 수.
        public int NextHitCount = 0;
        //# centroid 반환값.
        public Vector3 CentroidResult = Vector3.zero;

        public struct RingCall { public float Inner, Outer; public int Amount; public float Knockback; }
        public struct ConeCall { public Vector3 Dir; public float Length, HalfAngleDeg; public int Amount; public float Knockback; }
        public struct SpheresCall { public Vector3[] Centers; public float Radius; public int Amount; public float Knockback; }

        public readonly List<RingCall> RingCalls = new List<RingCall>();
        public readonly List<ConeCall> ConeCalls = new List<ConeCall>();
        public readonly List<SpheresCall> SpheresCalls = new List<SpheresCall>();

        public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
        {
            RingCalls.Add(new RingCall { Inner = innerRadius, Outer = outerRadius, Amount = amount, Knockback = knockbackStrength });
            return NextHitCount;
        }

        public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength)
        {
            ConeCalls.Add(new ConeCall { Dir = direction, Length = length, HalfAngleDeg = halfAngleDeg, Amount = amount, Knockback = knockbackStrength });
            return NextHitCount;
        }

        public int DamageMonstersInSpheres(System.Collections.Generic.IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength)
        {
            Vector3[] copy = new Vector3[sphereCenters.Count];
            for (int i = 0; i < sphereCenters.Count; ++i)
                copy[i] = sphereCenters[i];
            SpheresCalls.Add(new SpheresCall { Centers = copy, Radius = sphereRadius, Amount = amount, Knockback = knockbackStrength });
            return NextHitCount;
        }

        public Vector3 MonsterCentroid(float radius) => CentroidResult;
    }
}
