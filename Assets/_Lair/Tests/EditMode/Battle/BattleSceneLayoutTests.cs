using Lair.Tests.EditMode;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.Tests.Battle
{
    //# 배틀 씬 실제 스포너 배치 — 기획서 scene-2d-conversion §3.2 · §3.4 · §3.5 · G2 · G5.
    //# 순수 식이 아니라 Battle.unity 에 직렬화된 실제 위치를 도트로 환산해 검증한다.
    public class BattleSceneLayoutTests
    {
        //# 전장(HUD 가 비워 두는 영역) — §3.4
        private const float FieldLeft = 16.67f;
        private const float FieldRight = 1045.2f;
        private const float FieldTop = 111.2f;
        private const float FieldBottom = 613.3f;

        private static readonly Dictionary<EMonster, Vector2> ExpectedDot = new Dictionary<EMonster, Vector2>
        {
            { EMonster.Wisp, new Vector2(1013.36f, 366.38f) },
            { EMonster.Reaper, new Vector2(813.68f, 165.29f) },
            { EMonster.Hex, new Vector2(414.32f, 165.29f) },
            { EMonster.Wraith, new Vector2(214.64f, 366.38f) },
            { EMonster.Plague, new Vector2(414.32f, 567.47f) },
            { EMonster.Phantom, new Vector2(813.68f, 567.47f) },
        };

        private static readonly Dictionary<EMonster, float> ExpectedDelay = new Dictionary<EMonster, float>
        {
            { EMonster.Wisp, 11.680f },
            { EMonster.Reaper, 8.753f },
            { EMonster.Hex, 9.378f },
            { EMonster.Wraith, 14.600f },
            { EMonster.Plague, 10.099f },
            { EMonster.Phantom, 5.470f },
        };

        private Scene _scene;
        private List<Spawner> _spawners;

        [OneTimeSetUp]
        public void OpenScene()
        {
            _scene = DotSceneTestUtil.Open(DotSceneTestUtil.BattleScene);
            _spawners = DotSceneTestUtil.FindAll<Spawner>(_scene);
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            DotSceneTestUtil.Close(_scene);
        }

        private static EMonster TypeOf(Spawner s)
        {
            SerializedObject so = new SerializedObject(s);
            return (EMonster)so.FindProperty("_outputType").enumValueIndex;
        }

        [Test]
        public void 스포너는_정확히_6개이고_종이_모두_다르다()
        {
            Assert.AreEqual(6, _spawners.Count);
            HashSet<EMonster> types = new HashSet<EMonster>();
            foreach (Spawner s in _spawners)
            {
                types.Add(TypeOf(s));
            }
            Assert.AreEqual(6, types.Count);
        }

        [Test]
        public void 스포너_6개의_실제_위치가_기획서_도트_좌표와_같다()
        {
            foreach (Spawner s in _spawners)
            {
                EMonster type = TypeOf(s);
                Vector2 dot = DotStageMapping.BattleGroundToDot(s.transform.position);
                Assert.AreEqual(ExpectedDot[type].x, dot.x, 0.05f, type + " dx");
                Assert.AreEqual(ExpectedDot[type].y, dot.y, 0.05f, type + " dy");
            }
        }

        [Test]
        public void 스포너_6개는_모두_지면_Y_0에_있다()
        {
            foreach (Spawner s in _spawners)
            {
                Assert.AreEqual(0f, s.transform.position.y, 1e-4f, TypeOf(s).ToString());
            }
        }

        [Test]
        public void 제단_외곽_상자가_HUD_영역과_겹치지_않는다()
        {
            foreach (Spawner s in _spawners)
            {
                Vector2 d = DotStageMapping.BattleGroundToDot(s.transform.position);
                string n = TypeOf(s).ToString();
                Assert.GreaterOrEqual(d.x - 22f, FieldLeft, n + " left");
                Assert.LessOrEqual(d.x + 22f, FieldRight, n + " right");
                Assert.GreaterOrEqual(d.y - 29f, FieldTop, n + " top");
                Assert.LessOrEqual(d.y + 9f, FieldBottom, n + " bottom");
            }
        }

        [Test]
        public void 제단_앵커는_투기장_타원_안이다()
        {
            foreach (Spawner s in _spawners)
            {
                Vector2 d = DotStageMapping.BattleGroundToDot(s.transform.position);
                float nx = (d.x - 614f) / 624f;
                float ny = (d.y - 366f) / 258f;
                Assert.Less(nx * nx + ny * ny, 1f, TypeOf(s).ToString());
            }
        }

        [Test]
        public void 위아래_제단은_투기장_테두리_안쪽이다()
        {
            //# 결정 꼭대기(앵커 −29)가 위 테두리 아래, 링 아랫끝(앵커 +9)이 아래 테두리 위 — §10
            foreach (Spawner s in _spawners)
            {
                Vector2 d = DotStageMapping.BattleGroundToDot(s.transform.position);
                if (Mathf.Abs(d.y - 366.38f) < 1f)
                    continue;
                float half = 258f * Mathf.Sqrt(1f - Mathf.Pow((d.x - 614f) / 624f, 2f));
                Assert.Greater(d.y - 29f, 366f - half, TypeOf(s) + " top edge");
                Assert.Less(d.y + 9f, 366f + half, TypeOf(s) + " bottom edge");
            }
        }

        [Test]
        public void 모든_제단은_영웅_진입점에서_46도트_이상_떨어진다()
        {
            BattleZone zone = DotSceneTestUtil.Find<BattleZone>(_scene);
            Transform entry = zone.transform.Find("HeroEntryPoint");
            Vector2 entryDot = DotStageMapping.BattleGroundToDot(entry.position);
            foreach (Spawner s in _spawners)
            {
                Vector2 d = DotStageMapping.BattleGroundToDot(s.transform.position);
                Assert.GreaterOrEqual(Vector2.Distance(entryDot, d), 46f, TypeOf(s).ToString());
            }
        }

        [Test]
        public void Wraith_제단과_진입점의_가로_간격은_63_36도트다()
        {
            BattleZone zone = DotSceneTestUtil.Find<BattleZone>(_scene);
            Vector2 entryDot = DotStageMapping.BattleGroundToDot(zone.transform.Find("HeroEntryPoint").position);
            foreach (Spawner s in _spawners)
            {
                if (TypeOf(s) != EMonster.Wraith)
                    continue;
                Vector2 d = DotStageMapping.BattleGroundToDot(s.transform.position);
                Assert.AreEqual(63.36f, entryDot.x - d.x, 0.05f);
            }
        }

        [Test]
        public void 영웅_진입점_도트는_278_366_38이다()
        {
            BattleZone zone = DotSceneTestUtil.Find<BattleZone>(_scene);
            Vector2 entryDot = DotStageMapping.BattleGroundToDot(zone.transform.Find("HeroEntryPoint").position);
            Assert.AreEqual(278f, entryDot.x, 0.05f);
            Assert.AreEqual(366.38f, entryDot.y, 0.05f);
        }

        [Test]
        public void BattleZone_중심은_월드_0_0_마이너스2다()
        {
            BattleZone zone = DotSceneTestUtil.Find<BattleZone>(_scene);
            Assert.AreEqual(0f, zone.transform.position.x, 1e-4f);
            Assert.AreEqual(0f, zone.transform.position.y, 1e-4f);
            Assert.AreEqual(-2f, zone.transform.position.z, 1e-4f);
        }

        [Test]
        public void 스포너_링_각도_배치는_현행_0_60_120_180_240_300이다()
        {
            CircularSpawnerArranger arranger = DotSceneTestUtil.Find<CircularSpawnerArranger>(_scene);
            SerializedObject so = new SerializedObject(arranger);
            Assert.AreEqual(0f, so.FindProperty("_startAngleDeg").floatValue, 1e-4f);
            Vector3 center = new Vector3(0f, 0f, -2f);
            float[] angles = { 0f, 60f, 120f, 180f, 240f, 300f };
            SerializedProperty monsters = so.FindProperty("_monsters");
            Assert.AreEqual(6, monsters.arraySize);
            EMonster[] order = { EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom };
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual((int)order[i], monsters.GetArrayElementAtIndex(i).enumValueIndex, "monster " + i);
                Vector3 expected = CircularSpawnerArranger.PositionOnEllipse(center, 8.32f, 6.3149f, angles[i]);
                Spawner s = _spawners.Find(x => TypeOf(x) == order[i]);
                Assert.IsNotNull(s, order[i].ToString());
                Assert.AreEqual(expected.x, s.transform.position.x, 0.002f, "x " + order[i]);
                Assert.AreEqual(expected.z, s.transform.position.z, 0.002f, "z " + order[i]);
            }
        }

        [Test]
        public void 스포너마다_제단_컴포넌트가_정확히_1개씩_있다()
        {
            foreach (Spawner s in _spawners)
            {
                SpawnerAltar2D[] altars = s.GetComponentsInChildren<SpawnerAltar2D>(true);
                Assert.AreEqual(1, altars.Length, TypeOf(s).ToString());
            }
        }

        [Test]
        public void 제단은_ISpawnerAltar로_루트_계약을_노출한다()
        {
            foreach (Spawner s in _spawners)
            {
                ISpawnerAltar altar = s.GetComponentInChildren<ISpawnerAltar>(true);
                Assert.IsNotNull(altar, TypeOf(s).ToString());
            }
        }

        [Test]
        public void 씬_값으로_계산한_첫_스폰_지연이_기획서_표와_같다()
        {
            BalanceConfig cfg = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Lair/Data/BalanceConfig.asset");
            BattleZone zone = DotSceneTestUtil.Find<BattleZone>(_scene);
            Vector3 center = zone.transform.position;
            foreach (Spawner s in _spawners)
            {
                EMonster type = TypeOf(s);
                BalanceConfig.CharacterStat stat = cfg.GetMonster(type);
                Vector3 d = s.transform.position - center;
                float distance = new Vector2(d.x, d.z).magnitude;
                float delay = SpawnTravelCompensation.FirstSpawnDelay(
                    SpawnTravelCompensation.LegacyRadius, distance, stat.Range, stat.MoveSpeed, SpawnTravelCompensation.DelayScale);
                Assert.AreEqual(ExpectedDelay[type], delay, 0.01f, type.ToString());
            }
        }
    }
}
