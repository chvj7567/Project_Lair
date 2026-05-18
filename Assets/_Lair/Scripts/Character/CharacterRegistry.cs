using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅·몬스터 정적 레지스트리. 캐릭터의 OnEnable/OnDisable 에서 자기 등록.
    //# TryFindNearest 는 IsAlive 필터링 + 거리순 정렬 후 최근접 1개 반환.
    public static class CharacterRegistry
    {
        public class Entry
        {
            public Transform Transform;
            public IHealth Health;
        }

        public static readonly List<Entry> Heroes = new();
        public static readonly List<Entry> Monsters = new();

        public static void RegisterHero(Transform t, IHealth h)   => Add(Heroes, t, h);
        public static void UnregisterHero(Transform t)            => Remove(Heroes, t);
        public static void RegisterMonster(Transform t, IHealth h)=> Add(Monsters, t, h);
        public static void UnregisterMonster(Transform t)         => Remove(Monsters, t);

        public static bool TryFindNearestHero(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Heroes, from, out t, out h);
        public static bool TryFindNearestMonster(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Monsters, from, out t, out h);

        private static void Add(List<Entry> list, Transform t, IHealth h)
        {
            if (t == null) return;
            list.Add(new Entry { Transform = t, Health = h });
        }

        private static void Remove(List<Entry> list, Transform t)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (list[i].Transform == t) list.RemoveAt(i);
            }
        }

        private static bool TryFindNearest(
            List<Entry> list, Vector3 from, out Transform t, out IHealth h)
        {
            t = null; h = null;
            float best = float.MaxValue;
            foreach (var e in list)
            {
                if (e.Transform == null) continue;
                if (e.Health == null || e.Health.IsAlive == false) continue;
                float d = (e.Transform.position - from).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    t = e.Transform;
                    h = e.Health;
                }
            }
            return t != null;
        }
    }
}
