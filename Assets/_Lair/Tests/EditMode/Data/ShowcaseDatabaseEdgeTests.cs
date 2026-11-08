using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# 쇼케이스 데이터 엣지·회귀(village-monster-showcase §4.3·§10, T-S10) - 시드(ShowcaseDatabaseTests)의 정상 로드·레인 밖 1건과 중복하지 않는다.
    //# 마을 경유점 17점 전수 검증, 레인 경계, 스키마 위반 거부, 조회 폴백.
    public class ShowcaseDatabaseEdgeTests
    {
        [Serializable] private class ActorWrap { public Showcase_ActorRow[] items; }
        [Serializable] private class PointWrap { public Showcase_Actor_WaypointRow[] items; }

        private static readonly string[] Species = { "Wisp", "Wraith", "Reaper", "Hex", "Plague", "Phantom" };

        private static string Read(string name) => "{\"items\":" + File.ReadAllText(Path.Combine(Application.dataPath, "_Lair/Data/Json", name + ".json")) + "}";

        //# ───────── 실제 데이터 ─────────

        [Test]
        public void 마을_경유점_17점은_전부_보행_레인_안이다()
        {
            Showcase_Actor_WaypointRow[] points = JsonUtility.FromJson<PointWrap>(Read("Showcase_Actor_Waypoint")).items;
            int village = 0;
            foreach (Showcase_Actor_WaypointRow row in points)
            {
                if (row.scene != "Village")
                    continue;
                village++;
                Assert.IsTrue(ShowcaseDatabase.IsInVillageLane(new Vector2(row.x, row.y)), $"{row.monster}#{row.index} ({row.x},{row.y}) 레인 밖");
            }
            Assert.AreEqual(17, village, "마을 경유점 수(기획서 §4.3 검산)");
        }

        [Test]
        public void 실제_데이터의_Wander_는_경유점_2개_이상이고_Stand_는_속도_0_이다()
        {
            Showcase_ActorRow[] actors = JsonUtility.FromJson<ActorWrap>(Read("Showcase_Actor")).items;
            Showcase_Actor_WaypointRow[] points = JsonUtility.FromJson<PointWrap>(Read("Showcase_Actor_Waypoint")).items;
            foreach (Showcase_ActorRow actor in actors)
            {
                int count = 0;
                foreach (Showcase_Actor_WaypointRow point in points)
                {
                    if (point.scene == actor.scene && point.monster == actor.monster)
                    {
                        count++;
                    }
                }
                if (actor.behavior == ShowcaseDatabase.BehaviorWander)
                {
                    Assert.GreaterOrEqual(count, 2, $"{actor.scene}/{actor.monster}");
                    Assert.Greater(actor.speedDots, 0f, $"{actor.scene}/{actor.monster} 속도");
                }
                else
                {
                    Assert.AreEqual(0f, actor.speedDots, $"{actor.scene}/{actor.monster}");
                    Assert.GreaterOrEqual(count, 1, $"{actor.scene}/{actor.monster}");
                }
            }
        }

        //# ───────── 레인 경계 ─────────

        [TestCase(112f, 154f, true)]
        [TestCase(190f, 154f, true)]
        [TestCase(190f, 199.9f, true)]
        [TestCase(191f, 154f, false)]
        [TestCase(191f, 199f, false)]
        [TestCase(191f, 200f, true)]
        [TestCase(289f, 199f, false)]
        [TestCase(290f, 154f, true)]
        [TestCase(340f, 232f, true)]
        [TestCase(341f, 232f, false)]
        [TestCase(200f, 160f, false)]
        [TestCase(111.9f, 220f, false)]
        [TestCase(250f, 233f, false)]
        [TestCase(250f, 232f, true)]
        [TestCase(250f, 199.9f, false)]
        [TestCase(250f, 200f, true)]
        [TestCase(150f, 153f, false)]
        public void 보행_레인_경계(float x, float y, bool expected)
        {
            Assert.AreEqual(expected, ShowcaseDatabase.IsInVillageLane(new Vector2(x, y)), $"({x},{y})");
        }

        //# ───────── 스키마 위반 ─────────

        private static Showcase_SceneRow[] Scenes()
        {
            return new[]
            {
                new Showcase_SceneRow { scene = "Village", fitMaxHeightDots = 94f, startWaitMaxSec = 2f, waitMinSec = 1f, waitMaxSec = 3.5f },
                new Showcase_SceneRow { scene = "Loading", fitMaxHeightDots = 94f },
            };
        }

        private static List<Showcase_ActorRow> Actors()
        {
            List<Showcase_ActorRow> rows = new List<Showcase_ActorRow>();
            foreach (string scene in new[] { "Village", "Loading" })
            {
                foreach (string monster in Species)
                {
                    rows.Add(new Showcase_ActorRow { scene = scene, monster = monster, behavior = ShowcaseDatabase.BehaviorStand });
                }
            }
            return rows;
        }

        private static List<Showcase_Actor_WaypointRow> Points()
        {
            List<Showcase_Actor_WaypointRow> rows = new List<Showcase_Actor_WaypointRow>();
            foreach (string scene in new[] { "Village", "Loading" })
            {
                foreach (string monster in Species)
                {
                    rows.Add(new Showcase_Actor_WaypointRow { scene = scene, monster = monster, index = 0, x = 150f, y = 210f });
                }
            }
            return rows;
        }

        private static ShowcaseDatabase Build(Showcase_SceneRow[] scenes, List<Showcase_ActorRow> actors, List<Showcase_Actor_WaypointRow> points)
            => ShowcaseDatabase.Build(scenes, actors.ToArray(), points.ToArray());

        private static Showcase_ActorRow ActorRow(List<Showcase_ActorRow> rows, string scene, string monster)
            => rows.Find(r => r.scene == scene && r.monster == monster);

        [Test]
        public void 기준_데이터는_검증을_통과한다()
        {
            ShowcaseDatabase db = Build(Scenes(), Actors(), Points());

            Assert.AreEqual(6, db.ActorsOf(EScene.Village).Count);
            Assert.AreEqual(6, db.ActorsOf(EScene.Loading).Count);
        }

        [Test]
        public void 로딩_경유점은_레인_검사를_받지_않는다()
        {
            List<Showcase_Actor_WaypointRow> points = Points();
            Showcase_Actor_WaypointRow loading = points.Find(r => r.scene == "Loading" && r.monster == "Wisp");
            loading.x = 379f;
            loading.y = 50f;

            Assert.DoesNotThrow(() => Build(Scenes(), Actors(), points));
        }

        [Test]
        public void Wander_인데_경유점이_1개면_검증_오류다()
        {
            List<Showcase_ActorRow> actors = Actors();
            ActorRow(actors, "Village", "Wisp").behavior = ShowcaseDatabase.BehaviorWander;
            ActorRow(actors, "Village", "Wisp").speedDots = 20f;

            Assert.Throws<ChapterDataException>(() => Build(Scenes(), actors, Points()));
        }

        [Test]
        public void Stand_인데_속도가_양수이거나_속도가_음수이면_검증_오류다()
        {
            List<Showcase_ActorRow> standMoving = Actors();
            ActorRow(standMoving, "Village", "Wisp").speedDots = 5f;
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), standMoving, Points()), "Stand 속도 > 0");

            List<Showcase_ActorRow> negative = Actors();
            ActorRow(negative, "Loading", "Hex").speedDots = -1f;
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), negative, Points()), "음수 속도");
        }

        [Test]
        public void 알_수_없는_behavior_는_검증_오류다()
        {
            List<Showcase_ActorRow> actors = Actors();
            ActorRow(actors, "Village", "Reaper").behavior = "Dance";

            Assert.Throws<ChapterDataException>(() => Build(Scenes(), actors, Points()));
        }

        [Test]
        public void 씬당_종족이_6행이_아니면_검증_오류다()
        {
            List<Showcase_ActorRow> actors = Actors();
            actors.Remove(ActorRow(actors, "Loading", "Plague"));

            ChapterDataException ex = Assert.Throws<ChapterDataException>(() => Build(Scenes(), actors, Points()));
            StringAssert.Contains("6", ex.Message);
        }

        [Test]
        public void 등급_몬스터나_복합키_중복_행은_검증_오류다()
        {
            List<Showcase_ActorRow> graded = Actors();
            ActorRow(graded, "Village", "Wisp").monster = "WispG2";
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), graded, Points()), "등급 몬스터는 쇼케이스 종족이 아님(기준 6종 고정)");

            List<Showcase_ActorRow> dup = Actors();
            dup.Add(new Showcase_ActorRow { scene = "Village", monster = "Wisp", behavior = ShowcaseDatabase.BehaviorStand });
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), dup, Points()), "복합키 중복");
        }

        [Test]
        public void 경유점이_없거나_인덱스가_비연속이거나_고아이면_검증_오류다()
        {
            List<Showcase_Actor_WaypointRow> none = Points();
            none.RemoveAll(r => r.scene == "Village" && r.monster == "Hex");
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), Actors(), none), "경유점 없음");

            List<Showcase_Actor_WaypointRow> gap = Points();
            gap.Find(r => r.scene == "Village" && r.monster == "Hex").index = 1;
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), Actors(), gap), "index 0 부터 연속이어야 함");

            List<Showcase_Actor_WaypointRow> orphan = Points();
            orphan.Add(new Showcase_Actor_WaypointRow { scene = "Village", monster = "WispG3", index = 0, x = 150f, y = 210f });
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), Actors(), orphan), "가리키는 Actor 행이 없는 경유점");

            List<Showcase_Actor_WaypointRow> badName = Points();
            badName[0].scene = "Nowhere";
            Assert.Throws<ChapterDataException>(() => Build(Scenes(), Actors(), badName), "EScene 값명 아님");
        }

        [Test]
        public void 경유점은_입력_순서와_무관하게_index_순으로_정렬된다()
        {
            List<Showcase_ActorRow> actors = Actors();
            ActorRow(actors, "Village", "Wisp").behavior = ShowcaseDatabase.BehaviorWander;
            ActorRow(actors, "Village", "Wisp").speedDots = 20f;
            List<Showcase_Actor_WaypointRow> points = Points();
            points.RemoveAll(r => r.scene == "Village" && r.monster == "Wisp");
            points.Add(new Showcase_Actor_WaypointRow { scene = "Village", monster = "Wisp", index = 2, x = 190f, y = 230f });
            points.Add(new Showcase_Actor_WaypointRow { scene = "Village", monster = "Wisp", index = 0, x = 140f, y = 222f });
            points.Add(new Showcase_Actor_WaypointRow { scene = "Village", monster = "Wisp", index = 1, x = 232f, y = 228f });

            ShowcaseActorData wisp = Build(Scenes(), actors, points).ActorOf(EScene.Village, EMonster.Wisp);

            Assert.AreEqual(new Vector2(140f, 222f), wisp.Waypoints[0]);
            Assert.AreEqual(new Vector2(232f, 228f), wisp.Waypoints[1]);
            Assert.AreEqual(new Vector2(190f, 230f), wisp.Waypoints[2]);
            Assert.IsTrue(wisp.IsWander);
            Assert.AreEqual(20f, wisp.SpeedDots);
        }

        [Test]
        public void 씬_행이_잘못되면_검증_오류다()
        {
            Showcase_SceneRow[] noFit = Scenes();
            noFit[0].fitMaxHeightDots = 0f;
            Assert.Throws<ChapterDataException>(() => Build(noFit, Actors(), Points()), "fit 0");

            Showcase_SceneRow[] reversed = Scenes();
            reversed[0].waitMinSec = 5f;
            reversed[0].waitMaxSec = 1f;
            Assert.Throws<ChapterDataException>(() => Build(reversed, Actors(), Points()), "대기 최소 > 최대");

            Showcase_SceneRow[] negStart = Scenes();
            negStart[0].startWaitMaxSec = -1f;
            Assert.Throws<ChapterDataException>(() => Build(negStart, Actors(), Points()), "음수 시작 대기");

            Showcase_SceneRow[] battle = Scenes();
            battle[1].scene = "Battle";
            Assert.Throws<ChapterDataException>(() => Build(battle, Actors(), Points()), "Village|Loading 만 허용");

            Showcase_SceneRow[] dup = Scenes();
            dup[1].scene = "Village";
            Assert.Throws<ChapterDataException>(() => Build(dup, Actors(), Points()), "씬 중복");

            Assert.Throws<ChapterDataException>(() => Build(new[] { Scenes()[0] }, Actors(), Points()), "Loading 행 누락");
        }

        [Test]
        public void 모든_입력이_null_이면_검증_오류다()
        {
            Assert.Throws<ChapterDataException>(() => ShowcaseDatabase.Build(null, null, null));
        }

        [Test]
        public void 여러_위반은_한_예외에_모인다()
        {
            Showcase_SceneRow[] scenes = Scenes();
            scenes[0].fitMaxHeightDots = 0f;
            List<Showcase_ActorRow> actors = Actors();
            ActorRow(actors, "Loading", "Wisp").behavior = "Dance";

            ChapterDataException ex = Assert.Throws<ChapterDataException>(() => Build(scenes, actors, Points()));

            StringAssert.Contains("Showcase_Scene.json", ex.Message);
            StringAssert.Contains("Showcase_Actor.json", ex.Message);
        }

        //# ───────── 조회 ─────────

        [Test]
        public void 쇼케이스_대상이_아닌_씬이나_몬스터는_조회가_비어_있다()
        {
            ShowcaseDatabase db = Build(Scenes(), Actors(), Points());

            Assert.AreEqual(0, db.ActorsOf(EScene.Battle).Count);
            Assert.IsNull(db.SceneRowOf(EScene.Battle));
            Assert.IsNull(db.ActorOf(EScene.Village, EMonster.WispG5), "등급 enum 으로 조회하면 null - 호출부가 기준 몬스터로 조회해야 함");
            Assert.IsNotNull(db.ActorOf(EScene.Village, EMonster.Wisp));
        }
    }
}
