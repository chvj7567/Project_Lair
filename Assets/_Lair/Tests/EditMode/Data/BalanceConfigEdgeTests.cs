using System;
using System.Text.RegularExpressions;
using Lair.Data;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# BalanceConfig.Build 검증 경계·조회 API (so-to-json-migration §3.2·§3.3·§10 SJ3~SJ5·§12.5-3). 합성 row 만 쓴다 — 출하 JSON 에 의존하지 않는다.
    public class BalanceConfigEdgeTests
    {
        private static readonly string[] BaseNames = { "Wisp", "Wraith", "Reaper", "Hex", "Plague", "Phantom" };

        private static BalanceConfigRow Config()
        {
            return new BalanceConfigRow
            {
                heroHp = 4000,
                heroPower = 50,
                runDuration = 300f,
                passiveThresholds = new[] { 0.9f, 0.5f, 0.1f },
                activeThresholds = new[] { 30f, 90f, 150f },
            };
        }

        //# 종마다 값이 모두 달라 "다른 종의 값이 섞이는" 조립 오류를 잡는다.
        private static MonsterStatRow[] Monsters()
        {
            MonsterStatRow[] rows = new MonsterStatRow[BaseNames.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = new MonsterStatRow
                {
                    monster = BaseNames[i],
                    hp = 100 + i,
                    power = 10 + i,
                    range = 1f + 0.5f * i,
                    cooldown = 0.5f + 0.1f * i,
                    moveSpeed = 1f + 0.25f * i,
                    spawnPeriod = 5f + i,
                };
            }
            return rows;
        }

        private static void AssertBuilds(BalanceConfigRow config, MonsterStatRow[] monsters = null)
        {
            Assert.DoesNotThrow(() => BalanceConfig.Build(config, monsters ?? Monsters()));
        }

        private static void AssertFails(BalanceConfigRow config, MonsterStatRow[] monsters, params string[] fragments)
        {
            BuildEdgeTestKit.AssertFails(() => BalanceConfig.Build(config, monsters), fragments);
        }

        //# --- 정상 조립 ---

        [Test]
        public void 정상_입력은_모든_값이_종별로_섞이지_않고_조립된다()
        {
            BalanceConfigRow config = Config();
            MonsterStatRow[] rows = Monsters();

            BalanceConfig balance = BalanceConfig.Build(config, rows);

            Assert.AreEqual(4000, balance.Hero.Hp);
            Assert.AreEqual(50, balance.Hero.Power);
            Assert.AreEqual(300f, balance.RunDuration, 0f);
            CollectionAssert.AreEqual(config.passiveThresholds, balance.PassiveThresholds);
            CollectionAssert.AreEqual(config.activeThresholds, balance.ActiveThresholds);
            for (int i = 0; i < rows.Length; i++)
            {
                EMonster monster = (EMonster)Enum.Parse(typeof(EMonster), rows[i].monster);
                BalanceConfig.CharacterStat stat = balance.GetMonster(monster);
                Assert.AreEqual(rows[i].hp, stat.Hp, rows[i].monster + " hp");
                Assert.AreEqual(rows[i].power, stat.Power, rows[i].monster + " power");
                Assert.AreEqual(rows[i].range, stat.Range, 0f, rows[i].monster + " range");
                Assert.AreEqual(rows[i].cooldown, stat.Cooldown, 0f, rows[i].monster + " cooldown");
                Assert.AreEqual(rows[i].moveSpeed, stat.MoveSpeed, 0f, rows[i].monster + " moveSpeed");
                Assert.AreEqual(rows[i].spawnPeriod, balance.GetSpawnPeriod(monster), 0f, rows[i].monster + " spawnPeriod");
            }
        }

        [Test]
        public void 몬스터_행_순서가_뒤섞여도_조립된다()
        {
            MonsterStatRow[] rows = Monsters();
            Array.Reverse(rows);

            BalanceConfig balance = BalanceConfig.Build(Config(), rows);

            Assert.AreEqual(100, balance.GetMonster(EMonster.Wisp).Hp);
            Assert.AreEqual(105, balance.GetMonster(EMonster.Phantom).Hp);
        }

        //# --- 필수 입력 ---

        [Test]
        public void 설정_객체가_null이면_비어_있음_오류다()
        {
            AssertFails(null, Monsters(), "BalanceConfig.json 이 비어 있음");
        }

        [Test]
        public void 몬스터_배열이_null이어도_예외없이_기준_6종_누락을_모두_보고한다()
        {
            ChapterDataException e = BuildEdgeTestKit.Fails(() => BalanceConfig.Build(Config(), null));

            foreach (string name in BaseNames)
            {
                StringAssert.Contains(name + " 행이 없음", e.Message);
            }
            Assert.AreEqual(BaseNames.Length, BuildEdgeTestKit.Violations(e).Length);
        }

        [Test]
        public void 몬스터_배열이_비어_있으면_기준_6종_누락을_모두_보고한다()
        {
            ChapterDataException e = BuildEdgeTestKit.Fails(() => BalanceConfig.Build(Config(), new MonsterStatRow[0]));

            Assert.AreEqual(BaseNames.Length, BuildEdgeTestKit.Violations(e).Length);
        }

        [Test]
        public void 기준_6종_중_하나만_빠지면_그_종만_보고한다()
        {
            MonsterStatRow[] rows = Monsters();
            Array.Resize(ref rows, 5);

            ChapterDataException e = BuildEdgeTestKit.Fails(() => BalanceConfig.Build(Config(), rows));

            StringAssert.Contains("Phantom 행이 없음", e.Message);
            Assert.AreEqual(1, BuildEdgeTestKit.Violations(e).Length);
        }

        [TestCase("passive")]
        [TestCase("active")]
        public void 임계_배열이_null이거나_비어_있으면_가_비어_있음_오류다(string which)
        {
            foreach (float[] bad in new float[][] { null, new float[0] })
            {
                BalanceConfigRow config = Config();
                if (which == "passive")
                {
                    config.passiveThresholds = bad;
                }
                else
                {
                    config.activeThresholds = bad;
                }

                AssertFails(config, Monsters(), which + "Thresholds 가 비어 있음");
            }
        }

        //# --- 값 범위 경계 ---

        [TestCase(1, 1, true)]
        [TestCase(0, 50, false)]
        [TestCase(-1, 50, false)]
        [TestCase(4000, 0, false)]
        [TestCase(4000, -5, false)]
        [TestCase(int.MaxValue, int.MaxValue, true)]
        public void 영웅_앵커는_둘_다_1_이상이어야_한다(int hp, int power, bool ok)
        {
            BalanceConfigRow config = Config();
            config.heroHp = hp;
            config.heroPower = power;

            if (ok)
            {
                AssertBuilds(config);
                return;
            }
            AssertFails(config, Monsters(), "heroHp·heroPower 는 1 이상");
        }

        [TestCase(0f, false)]
        [TestCase(-1f, false)]
        [TestCase(0.0001f, true)]
        [TestCase(300f, true)]
        public void 런_길이는_0보다_커야_한다(float runDuration, bool ok)
        {
            BalanceConfigRow config = Config();
            config.runDuration = runDuration;
            config.activeThresholds = new[] { runDuration > 0.001f ? runDuration * 0.5f : 0.00005f };

            if (ok)
            {
                AssertBuilds(config);
                return;
            }
            AssertFails(config, Monsters(), "runDuration 은 0 보다 커야 함");
        }

        [TestCase(new[] { 0.9f, 0.5f, 0.1f }, true)]
        [TestCase(new[] { 0.5f }, true)]
        [TestCase(new[] { 0.9999f }, true)]
        [TestCase(new[] { 0.0001f }, true)]
        [TestCase(new[] { 0f }, false)]
        [TestCase(new[] { 1f }, false)]
        [TestCase(new[] { 1.0001f }, false)]
        [TestCase(new[] { -0.1f }, false)]
        [TestCase(new[] { 0.5f, 0.5f }, false)]
        [TestCase(new[] { 0.1f, 0.9f }, false)]
        [TestCase(new[] { 0.9f, 0.5f, 0.7f }, false)]
        public void 패시브_임계는_0_초과_1_미만_엄격_내림차순이다(float[] thresholds, bool ok)
        {
            BalanceConfigRow config = Config();
            config.passiveThresholds = thresholds;

            if (ok)
            {
                AssertBuilds(config);
                return;
            }
            AssertFails(config, Monsters(), "passiveThresholds[");
        }

        [TestCase(new[] { 30f, 90f, 150f }, true)]
        [TestCase(new[] { 0.0001f }, true)]
        [TestCase(new[] { 299.99f }, true)]
        [TestCase(new[] { 0f }, false)]
        [TestCase(new[] { 300f }, false)]
        [TestCase(new[] { 301f }, false)]
        [TestCase(new[] { -1f }, false)]
        [TestCase(new[] { 90f, 30f }, false)]
        [TestCase(new[] { 30f, 30f }, false)]
        public void 액티브_임계는_0_초과_런_길이_미만_엄격_오름차순이다(float[] thresholds, bool ok)
        {
            BalanceConfigRow config = Config();
            config.activeThresholds = thresholds;

            if (ok)
            {
                AssertBuilds(config);
                return;
            }
            AssertFails(config, Monsters(), "activeThresholds[");
        }

        //# --- 몬스터 행 ---

        [TestCase("Ghost")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("wisp")]
        [TestCase("WispG2")]
        [TestCase("PhantomG5")]
        public void 몬스터_이름이_기준_6종_값명이_아니면_오류다(string name)
        {
            MonsterStatRow[] rows = Monsters();
            rows[0].monster = name;

            ChapterDataException e = BuildEdgeTestKit.Fails(() => BalanceConfig.Build(Config(), rows));

            StringAssert.Contains("는 EMonster 기준 6종 값명이어야 함", e.Message);
            StringAssert.Contains("Wisp 행이 없음", e.Message);
        }

        [Test]
        public void 몬스터_행에_null이_섞여도_예외없이_위반으로_보고한다()
        {
            MonsterStatRow[] rows = Monsters();
            rows[2] = null;

            ChapterDataException e = BuildEdgeTestKit.Fails(() => BalanceConfig.Build(Config(), rows));

            StringAssert.Contains("기준 6종 값명이어야 함", e.Message);
            StringAssert.Contains("Reaper 행이 없음", e.Message);
        }

        [Test]
        public void 같은_종이_두_번_나오면_중복_오류다()
        {
            MonsterStatRow[] rows = Monsters();
            rows[3].monster = "Wisp";

            AssertFails(Config(), rows, "monster Wisp 중복", "Hex 행이 없음");
        }

        [TestCase("hp", 0)]
        [TestCase("hp", -1)]
        [TestCase("power", 0)]
        [TestCase("power", -1)]
        public void 몬스터_hp_power_는_1_이상이어야_한다(string field, int value)
        {
            MonsterStatRow[] rows = Monsters();
            typeof(MonsterStatRow).GetField(field).SetValue(rows[1], value);

            AssertFails(Config(), rows, "Wraith hp·power 는 1 이상이어야 함");
        }

        [Test]
        public void 몬스터_hp_power_가_1이면_통과한다()
        {
            MonsterStatRow[] rows = Monsters();
            rows[0].hp = 1;
            rows[0].power = 1;

            AssertBuilds(Config(), rows);
        }

        [TestCase("range", 0f)]
        [TestCase("range", -0.5f)]
        [TestCase("cooldown", 0f)]
        [TestCase("cooldown", -1f)]
        [TestCase("moveSpeed", 0f)]
        [TestCase("moveSpeed", -1f)]
        [TestCase("spawnPeriod", 0f)]
        [TestCase("spawnPeriod", -9f)]
        public void 몬스터_range_cooldown_moveSpeed_spawnPeriod_는_0보다_커야_한다(string field, float value)
        {
            MonsterStatRow[] rows = Monsters();
            typeof(MonsterStatRow).GetField(field).SetValue(rows[4], value);

            AssertFails(Config(), rows, "Plague range·cooldown·moveSpeed·spawnPeriod 는 0 보다 커야 함");
        }

        [Test]
        public void 몬스터_실수_필드가_0에_가까운_양수면_통과한다()
        {
            MonsterStatRow[] rows = Monsters();
            rows[5].range = 0.0001f;
            rows[5].cooldown = 0.0001f;
            rows[5].moveSpeed = 0.0001f;
            rows[5].spawnPeriod = 0.0001f;

            AssertBuilds(Config(), rows);
        }

        //# --- 위반 수집 ---

        [Test]
        public void 설정_위반과_몬스터_위반이_여러_개면_메시지에_전부_담긴다()
        {
            BalanceConfigRow config = Config();
            config.heroHp = 0;
            config.runDuration = 0f;
            config.passiveThresholds = new[] { 0.1f, 0.9f };
            config.activeThresholds = null;
            MonsterStatRow[] rows = Monsters();
            rows[0].hp = 0;
            rows[1].monster = "Ghost";
            rows[2].range = 0f;

            ChapterDataException e = BuildEdgeTestKit.Fails(() => BalanceConfig.Build(config, rows));

            StringAssert.Contains("heroHp·heroPower", e.Message);
            StringAssert.Contains("runDuration 은 0 보다 커야 함", e.Message);
            StringAssert.Contains("passiveThresholds[1]", e.Message);
            StringAssert.Contains("activeThresholds 가 비어 있음", e.Message);
            StringAssert.Contains("Wisp hp·power", e.Message);
            StringAssert.Contains("'Ghost'", e.Message);
            StringAssert.Contains("Reaper range·cooldown", e.Message);
            StringAssert.Contains("Wraith 행이 없음", e.Message);
        }

        //# --- 조회 API ---

        [Test]
        public void 등급_몬스터_24종은_기준_종의_같은_행과_같은_스폰_주기를_돌려준다()
        {
            BalanceConfig balance = BalanceConfig.Build(Config(), Monsters());
            int graded = 0;

            foreach (EMonster monster in Enum.GetValues(typeof(EMonster)))
            {
                string name = monster.ToString();
                bool isGrade = name.Length > 2 && name[name.Length - 2] == 'G' && char.IsDigit(name[name.Length - 1]);
                string baseName = isGrade ? name.Substring(0, name.Length - 2) : name;
                EMonster baseMonster = (EMonster)Enum.Parse(typeof(EMonster), baseName);

                Assert.AreSame(balance.GetMonster(baseMonster), balance.GetMonster(monster), name);
                Assert.AreEqual(balance.GetSpawnPeriod(baseMonster), balance.GetSpawnPeriod(monster), 0f, name);
                graded += isGrade ? 1 : 0;
            }
            Assert.AreEqual(24, graded, "등급 몬스터 수");
        }

        [Test]
        public void 범위_밖_몬스터_값은_null과_경고를_돌려준다()
        {
            BalanceConfig balance = BalanceConfig.Build(Config(), Monsters());
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("몬스터 스탯 미발견"));
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("몬스터 스탯 미발견"));

            Assert.IsNull(balance.GetMonster((EMonster)(-1)));
            Assert.IsNull(balance.GetMonster((EMonster)999));
        }

        [Test]
        public void 범위_밖_몬스터_값의_스폰_주기는_폴백_9를_돌려준다()
        {
            BalanceConfig balance = BalanceConfig.Build(Config(), Monsters());
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("스폰 주기 미발견"));
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("스폰 주기 미발견"));

            Assert.AreEqual(9f, balance.GetSpawnPeriod((EMonster)(-1)), 0f);
            Assert.AreEqual(9f, balance.GetSpawnPeriod((EMonster)999), 0f);
        }

        //# 검증 없는 공개 생성자 — 일부 행만 있는 모델에서 미발견 폴백 경로를 재현한다(§12.5-3).
        [Test]
        public void 일부_행만_있는_모델은_없는_종에_null과_폴백_9를_돌려주고_등급은_기준_행을_따른다()
        {
            MonsterStatRow wisp = Monsters()[0];
            BalanceConfig balance = new BalanceConfig(Config(), new[] { wisp });
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("몬스터 스탯 미발견: Wraith"));
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("몬스터 스탯 미발견: WraithG3"));
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("스폰 주기 미발견: Wraith"));

            Assert.IsNull(balance.GetMonster(EMonster.Wraith));
            Assert.IsNull(balance.GetMonster(EMonster.WraithG3));
            Assert.AreEqual(9f, balance.GetSpawnPeriod(EMonster.Wraith), 0f);
            Assert.AreEqual(wisp.hp, balance.GetMonster(EMonster.WispG5).Hp);
            Assert.AreEqual(wisp.spawnPeriod, balance.GetSpawnPeriod(EMonster.WispG2), 0f);
        }

        [Test]
        public void 공개_생성자는_null_몬스터_배열과_null_행과_모르는_이름을_건너뛴다()
        {
            MonsterStatRow wisp = Monsters()[0];
            MonsterStatRow ghost = Monsters()[1];
            ghost.monster = "Ghost";

            BalanceConfig empty = new BalanceConfig(Config(), null);
            BalanceConfig partial = new BalanceConfig(Config(), new[] { null, ghost, wisp });

            Assert.AreEqual(4000, empty.Hero.Hp);
            Assert.AreEqual(wisp.hp, partial.GetMonster(EMonster.Wisp).Hp);
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex("몬스터 스탯 미발견: Wraith"));
            Assert.IsNull(partial.GetMonster(EMonster.Wraith));
        }
    }
}
