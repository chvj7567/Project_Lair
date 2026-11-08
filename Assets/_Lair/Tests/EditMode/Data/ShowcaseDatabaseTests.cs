using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# 쇼케이스 데이터(village-monster-showcase §10, 게이트 T-S10) — 시드: gameplay-programmer. 정상(실제 JSON 3종 로드·레인 검사) + 엣지(레인 밖 경유점 거부).
    public class ShowcaseDatabaseTests
    {
        [Serializable] private class SceneWrap { public Showcase_SceneRow[] items; }
        [Serializable] private class ActorWrap { public Showcase_ActorRow[] items; }
        [Serializable] private class PointWrap { public Showcase_Actor_WaypointRow[] items; }

        private static string Read(string name) => "{\"items\":" + File.ReadAllText(Path.Combine(Application.dataPath, "_Lair/Data/Json", name + ".json")) + "}";

        private static ShowcaseDatabase BuildFromFiles(Func<Showcase_Actor_WaypointRow[], Showcase_Actor_WaypointRow[]> mutate)
        {
            Showcase_SceneRow[] scenes = JsonUtility.FromJson<SceneWrap>(Read("Showcase_Scene")).items;
            Showcase_ActorRow[] actors = JsonUtility.FromJson<ActorWrap>(Read("Showcase_Actor")).items;
            Showcase_Actor_WaypointRow[] points = JsonUtility.FromJson<PointWrap>(Read("Showcase_Actor_Waypoint")).items;
            return ShowcaseDatabase.Build(scenes, actors, mutate(points));
        }

        [Test]
        public void 실제_JSON_3종이_검증을_통과하고_씬마다_6종을_가진다()
        {
            ShowcaseDatabase db = BuildFromFiles(p => p);

            Assert.AreEqual(6, db.ActorsOf(EScene.Village).Count);
            Assert.AreEqual(6, db.ActorsOf(EScene.Loading).Count);
            Assert.AreEqual(94f, db.SceneRowOf(EScene.Village).fitMaxHeightDots, 0.001f);
            ShowcaseActorData phantom = db.ActorOf(EScene.Village, EMonster.Phantom);
            Assert.IsTrue(phantom.Flying && phantom.IsWander);
            Assert.AreEqual(4, phantom.Waypoints.Length);
            Assert.IsFalse(db.ActorOf(EScene.Village, EMonster.Wraith).IsWander, "망령은 제자리");
        }

        [Test]
        public void 마을_경유점이_보행_레인_밖이면_검증_오류다()
        {
            Assert.Throws<ChapterDataException>(() => BuildFromFiles(points =>
            {
                points[0].x = 10f;
                points[0].y = 10f;
                return points;
            }));
        }
    }
}
