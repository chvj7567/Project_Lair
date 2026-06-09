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

        //# 영웅 전용 — 선호 사분면(preferDirH AND preferDirV 둘 다 양의 방향)에 후보가 있으면 그중 최근접,
        //# 없으면 전체 최근접으로 폴백. 방향 벡터가 zero 면 해당 축 선호 없음 → 사실상 전체 최근접(카메라 null 안전).
        //# 카메라 viewport 판정은 HeroTargetProvider 가 수행해 월드 방향으로 변환 후 전달(레지스트리는 카메라 비의존).
        public static bool TryFindNearestMonster(
            Vector3 from, Vector3 preferDirH, Vector3 preferDirV, out Transform t, out IHealth h)
            => TryFindNearestPreferred(Monsters, from, preferDirH, preferDirV, out t, out h, requireEngaging: true);

        //# 공포 도주 centroid — TryFindNearest 와 동일한 필터(살아있음 + 몬스터 Engaging) 적용. 스킬용(HeroSkillContext) 유지.
        public static bool TryGetThreatCentroidHero(Vector3 from, float radius, out Vector3 centroid, out int count)
            => TryGetThreatCentroid(Heroes, from, radius, out centroid, out count, requireEngaging: false);
        public static bool TryGetThreatCentroidMonster(Vector3 from, float radius, out Vector3 centroid, out int count)
            => TryGetThreatCentroid(Monsters, from, radius, out centroid, out count, requireEngaging: true);

        //# 도주 안정화(A-1) 전용 — Engaging 토글 무관, 반경 내 살아있는 몬스터 전체로 centroid 집계.
        //# 교전 들락거림이 도주 방향을 뒤집던 진동원 제거. 타겟팅용 TryGetThreatCentroidMonster(engaging=true)는 불변.
        public static bool TryGetFleeCentroidMonster(Vector3 from, float radius, out Vector3 centroid, out int count)
            => TryGetThreatCentroid(Monsters, from, radius, out centroid, out count, requireEngaging: false);

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

        //# 선호 사분면 우선 최근접. 한 번의 순회로 (선호 사분면 최근접 / 전체 최근접) 동시 추적 후
        //# 사분면 후보가 있으면 그것, 없으면 전체 최근접 폴백. rel·preferDir 의 Dot>0 으로 사분면 판정.
        private static bool TryFindNearestPreferred(
            List<Entry> list, Vector3 from, Vector3 preferDirH, Vector3 preferDirV,
            out Transform t, out IHealth h, bool requireEngaging)
        {
            t = null; h = null;
            Transform prefT = null; IHealth prefH = null;
            float bestAll = float.MaxValue;
            float bestPref = float.MaxValue;
            foreach (Entry e in list)
            {
                if (e.Transform == null) continue;
                if (e.Health == null || e.Health.IsAlive == false) continue;
                if (requireEngaging && e.IsEngaging == false) continue;
                Vector3 rel = e.Transform.position - from;
                float d = rel.sqrMagnitude;
                if (d < bestAll)
                {
                    bestAll = d;
                    t = e.Transform;
                    h = e.Health;
                }
                //# 선호 사분면 — 수평·수직 둘 다 양의 방향(Dot>0). zero 방향은 Dot=0 이라 자동 미충족 → 폴백.
                if (Vector3.Dot(rel, preferDirH) > 0f && Vector3.Dot(rel, preferDirV) > 0f && d < bestPref)
                {
                    bestPref = d;
                    prefT = e.Transform;
                    prefH = e.Health;
                }
            }
            if (prefT != null)
            {
                t = prefT;
                h = prefH;
            }
            return t != null;
        }

        //# 반경 내 적들의 위치 평균(centroid)과 개수. TryFindNearest 와 동일 필터.
        private static bool TryGetThreatCentroid(
            List<Entry> list, Vector3 from, float radius, out Vector3 centroid, out int count, bool requireEngaging)
        {
            centroid = Vector3.zero;
            count = 0;
            float sqrRadius = radius * radius;
            Vector3 sum = Vector3.zero;
            foreach (Entry e in list)
            {
                if (e.Transform == null) continue;
                if (e.Health == null || e.Health.IsAlive == false) continue;
                if (requireEngaging && e.IsEngaging == false) continue;
                if ((e.Transform.position - from).sqrMagnitude > sqrRadius) continue;
                sum += e.Transform.position;
                ++count;
            }
            if (count == 0)
                return false;
            centroid = sum / count;
            return true;
        }
    }
}
