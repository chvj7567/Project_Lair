using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 한 종족(씬별)의 쇼케이스 동작 — Showcase_Actor + 경유점 합성.
    public sealed class ShowcaseActorData
    {
        public EMonster Monster;
        public bool IsWander;
        public float SpeedDots;
        public bool Flying;
        public bool StartFacingLeft;
        public int OrderBase;
        public Vector2[] Waypoints = Array.Empty<Vector2>();
    }

    //# 쇼케이스 JSON 3종 검증 + 조회(village-monster-showcase §10). 위반은 ChapterDataException 으로 모아 던진다. Current==null 이면 쇼케이스 비활성(기존 정적 배치 유지).
    public sealed class ShowcaseDatabase
    {
        public const string BehaviorWander = "Wander";
        public const string BehaviorStand = "Stand";
        public const int ActorsPerScene = 6;

        public static ShowcaseDatabase Current { get; set; }

        private readonly Dictionary<EScene, Showcase_SceneRow> _scenes = new Dictionary<EScene, Showcase_SceneRow>();
        private readonly Dictionary<EScene, List<ShowcaseActorData>> _actors = new Dictionary<EScene, List<ShowcaseActorData>>();

        private ShowcaseDatabase() { }

        //# 마을 보행 레인(도트, village-monster-showcase §4.3) — 경유점은 반드시 이 안. L: x112~190 y154~232 · R: x290~340 y154~232 · F: x112~340 y200~232.
        public static bool IsInVillageLane(Vector2 dot)
        {
            bool left = dot.x >= 112f && dot.x <= 190f && dot.y >= 154f && dot.y <= 232f;
            bool right = dot.x >= 290f && dot.x <= 340f && dot.y >= 154f && dot.y <= 232f;
            bool front = dot.x >= 112f && dot.x <= 340f && dot.y >= 200f && dot.y <= 232f;
            return left || right || front;
        }

        public static ShowcaseDatabase Build(Showcase_SceneRow[] scenes, Showcase_ActorRow[] actors, Showcase_Actor_WaypointRow[] waypoints)
        {
            List<string> errors = new List<string>();
            ShowcaseDatabase db = new ShowcaseDatabase();
            db.LoadScenes(scenes, errors);
            db.LoadActors(actors, waypoints, errors);
            if (errors.Count > 0)
                throw new ChapterDataException(string.Join("\n", errors));
            return db;
        }

        public Showcase_SceneRow SceneRowOf(EScene scene) => _scenes.TryGetValue(scene, out Showcase_SceneRow row) ? row : null;

        public IReadOnlyList<ShowcaseActorData> ActorsOf(EScene scene)
            => _actors.TryGetValue(scene, out List<ShowcaseActorData> list) ? list : (IReadOnlyList<ShowcaseActorData>)Array.Empty<ShowcaseActorData>();

        public ShowcaseActorData ActorOf(EScene scene, EMonster baseMonster)
        {
            foreach (ShowcaseActorData actor in ActorsOf(scene))
            {
                if (actor.Monster == baseMonster)
                    return actor;
            }
            return null;
        }

        private static bool IsShowcaseScene(EScene scene) => scene == EScene.Village || scene == EScene.Loading;

        private void LoadScenes(Showcase_SceneRow[] rows, List<string> errors)
        {
            foreach (Showcase_SceneRow row in rows ?? Array.Empty<Showcase_SceneRow>())
            {
                if (row == null || Enum.TryParse(row.scene, out EScene scene) == false || IsShowcaseScene(scene) == false || _scenes.ContainsKey(scene))
                {
                    errors.Add($"Showcase_Scene.json scene '{row?.scene}' 가 잘못됨(Village|Loading, 중복 금지)");
                    continue;
                }
                if (row.fitMaxHeightDots > 0f == false || row.startWaitMaxSec < 0f || row.waitMinSec < 0f || row.waitMaxSec < row.waitMinSec)
                {
                    errors.Add($"Showcase_Scene.json {scene} 값이 잘못됨(fit>0, 대기 시간 0 이상·최소<=최대)");
                    continue;
                }
                _scenes[scene] = row;
            }
            foreach (EScene scene in new[] { EScene.Village, EScene.Loading })
            {
                if (_scenes.ContainsKey(scene) == false)
                {
                    errors.Add($"Showcase_Scene.json 에 {scene} 행이 없음");
                }
            }
        }

        private void LoadActors(Showcase_ActorRow[] actors, Showcase_Actor_WaypointRow[] waypoints, List<string> errors)
        {
            Dictionary<(EScene, EMonster), List<Showcase_Actor_WaypointRow>> byActor = new Dictionary<(EScene, EMonster), List<Showcase_Actor_WaypointRow>>();
            foreach (Showcase_Actor_WaypointRow row in waypoints ?? Array.Empty<Showcase_Actor_WaypointRow>())
            {
                if (row == null || Enum.TryParse(row.scene, out EScene scene) == false || Enum.TryParse(row.monster, out EMonster monster) == false)
                {
                    errors.Add($"Showcase_Actor_Waypoint.json '{row?.scene}/{row?.monster}' 가 잘못됨(EScene·EMonster 값명)");
                    continue;
                }
                if (byActor.TryGetValue((scene, monster), out List<Showcase_Actor_WaypointRow> list) == false)
                {
                    list = new List<Showcase_Actor_WaypointRow>();
                    byActor[(scene, monster)] = list;
                }
                list.Add(row);
            }

            HashSet<(EScene, EMonster)> seen = new HashSet<(EScene, EMonster)>();
            foreach (Showcase_ActorRow row in actors ?? Array.Empty<Showcase_ActorRow>())
            {
                if (row == null || Enum.TryParse(row.scene, out EScene scene) == false || _scenes.ContainsKey(scene) == false
                    || Enum.TryParse(row.monster, out EMonster monster) == false || MonsterGrades.IsBase(monster) == false
                    || seen.Add((scene, monster)) == false)
                {
                    errors.Add($"Showcase_Actor.json '{row?.scene}/{row?.monster}' 가 잘못됨(씬 행 존재·기준 몬스터·복합키 중복 금지)");
                    continue;
                }
                bool wander = row.behavior == BehaviorWander;
                if (wander == false && row.behavior != BehaviorStand)
                {
                    errors.Add($"Showcase_Actor.json {scene}/{monster} behavior '{row.behavior}' 는 Wander|Stand 여야 함");
                    continue;
                }
                if (row.speedDots < 0f || (wander == false && row.speedDots > 0f))
                {
                    errors.Add($"Showcase_Actor.json {scene}/{monster} speedDots 가 잘못됨(0 이상, Stand 는 0)");
                }
                ShowcaseActorData data = BuildActor(scene, monster, row, wander, byActor, errors);
                if (data == null)
                    continue;
                if (_actors.TryGetValue(scene, out List<ShowcaseActorData> list) == false)
                {
                    list = new List<ShowcaseActorData>();
                    _actors[scene] = list;
                }
                list.Add(data);
            }
            foreach (EScene scene in _scenes.Keys)
            {
                int count = _actors.TryGetValue(scene, out List<ShowcaseActorData> list) ? list.Count : 0;
                if (count != ActorsPerScene)
                {
                    errors.Add($"Showcase_Actor.json {scene} 는 종족 {ActorsPerScene}행이어야 함(현재 {count})");
                }
            }
            foreach ((EScene, EMonster) key in byActor.Keys)
            {
                if (seen.Contains(key) == false)
                {
                    errors.Add($"Showcase_Actor_Waypoint.json {key.Item1}/{key.Item2} 가 가리키는 Showcase_Actor 행이 없음");
                }
            }
        }

        private static ShowcaseActorData BuildActor(EScene scene, EMonster monster, Showcase_ActorRow row, bool wander,
            Dictionary<(EScene, EMonster), List<Showcase_Actor_WaypointRow>> byActor, List<string> errors)
        {
            if (byActor.TryGetValue((scene, monster), out List<Showcase_Actor_WaypointRow> points) == false || points.Count == 0)
            {
                errors.Add($"Showcase_Actor_Waypoint.json 에 {scene}/{monster} 경유점이 없음");
                return null;
            }
            if (wander && points.Count < 2)
            {
                errors.Add($"Showcase_Actor {scene}/{monster} Wander 는 경유점 2개 이상이어야 함");
            }
            points.Sort((a, b) => a.index.CompareTo(b.index));
            Vector2[] result = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].index != i)
                {
                    errors.Add($"Showcase_Actor_Waypoint {scene}/{monster} index 는 0 부터 연속이어야 함");
                    return null;
                }
                result[i] = new Vector2(points[i].x, points[i].y);
                if (scene == EScene.Village && IsInVillageLane(result[i]) == false)
                {
                    errors.Add($"Showcase_Actor_Waypoint {scene}/{monster} #{i} ({points[i].x},{points[i].y}) 가 보행 레인 밖");
                }
            }
            return new ShowcaseActorData
            {
                Monster = monster,
                IsWander = wander,
                SpeedDots = row.speedDots,
                Flying = row.flying,
                StartFacingLeft = row.startFacingLeft,
                OrderBase = row.orderBase,
                Waypoints = result,
            };
        }
    }
}
