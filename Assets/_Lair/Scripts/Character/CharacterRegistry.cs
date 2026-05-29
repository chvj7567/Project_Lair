using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅·몬스터 정적 레지스트리. 캐릭터의 OnEnable/OnDisable 에서 자기 등록.
    //# TryFindNearest 는 IsAlive 필터링 + 거리순 정렬 후 최근접 1개 반환.
    //# 몬스터는 추가로 IsEngaging=true 만 영웅 AI 의 타겟 후보 (BattleZone 진입 후 true).
    public static class CharacterRegistry
    {
        public class Entry
        {
            public Transform Transform;
            public IHealth Health;
            //# Marching → Engaging 상태. 영웅은 무관 (Hero 검색은 무시).
            //# 풀 Pop 직후엔 false (MonsterTag.OnEnable 이 보장). BattleZone trigger 진입 시 true.
            public bool IsEngaging;
        }

        public static readonly List<Entry> Heroes = new();
        public static readonly List<Entry> Monsters = new();

        public static void RegisterHero(Transform t, IHealth h)   => Add(Heroes, t, h);
        public static void UnregisterHero(Transform t)            => Remove(Heroes, t);
        public static void RegisterMonster(Transform t, IHealth h)=> Add(Monsters, t, h);
        public static void UnregisterMonster(Transform t)         => Remove(Monsters, t);

        //# 몬스터의 Marching/Engaging 상태 전환 — BattleZone.OnTriggerEnter 또는 MonsterTag.OnEnable 이 호출.
        public static void SetMonsterEngaging(Transform t, bool engaging)
        {
            if (t == null) return;
            foreach (Entry e in Monsters)
            {
                if (e.Transform == t)
                {
                    e.IsEngaging = engaging;
                    return;
                }
            }
        }

        public static bool TryFindNearestHero(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Heroes, from, out t, out h, requireEngaging: false);
        public static bool TryFindNearestMonster(Vector3 from, out Transform t, out IHealth h)
            => TryFindNearest(Monsters, from, out t, out h, requireEngaging: true);

        private static void Add(List<Entry> list, Transform t, IHealth h)
        {
            if (t == null) return;
            //# 신규 등록 — IsEngaging 기본 false. 몬스터는 MonsterTag.OnEnable 이 재호출.
            list.Add(new Entry { Transform = t, Health = h, IsEngaging = false });
        }

        private static void Remove(List<Entry> list, Transform t)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (list[i].Transform == t) list.RemoveAt(i);
            }
        }

        private static bool TryFindNearest(
            List<Entry> list, Vector3 from, out Transform t, out IHealth h, bool requireEngaging)
        {
            t = null; h = null;
            float best = float.MaxValue;
            foreach (Entry e in list)
            {
                if (e.Transform == null) continue;
                if (e.Health == null || e.Health.IsAlive == false) continue;
                //# 몬스터 검색은 Engaging 만 후보 — 영웅 검색(requireEngaging=false)엔 무영향.
                if (requireEngaging && e.IsEngaging == false) continue;
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
