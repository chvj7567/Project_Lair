using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lair.Battle;
using Lair.Card;
using Lair.Data;
using Lair.Meta;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Lair.Tests.EditMode
{
    //# 던전 자유 편성 규칙 — 구레벨 이행·모드별 편성 분기·봉인 폴백 순서·ApplyFormation 빈 둥지·카드 추첨 독립 (dungeon-formation-free §1·§4·§5·§6).
    public class FormationFreeRulesTests
    {
        private FormationDatabase _db;
        private FormationDatabase _previousCurrent;
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
            _previousCurrent = FormationDatabase.Current;
        }

        [TearDown]
        public void 정리()
        {
            FormationDatabase.Current = _previousCurrent;
            foreach (Object o in _spawned)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }
            _spawned.Clear();
        }

        //# --- 구레벨 이행 (NormalizeProfile) ---

        private static MetaProfile WithLevel(EMonster species, int level, int souls = 1234)
        {
            MetaProfile profile = new MetaProfile { Souls = souls };
            profile.ShopLevels.Add(new ShopLevelEntry { ItemId = MonsterEnhanceTierRules.ItemId(species), Level = level });
            return profile;
        }

        private static int LevelOf(MetaProfile profile, EMonster species)
        {
            return profile.GetShopLevel(MonsterEnhanceTierRules.ItemId(species));
        }

        [TestCase(0, 0)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]
        [TestCase(5, 5)]
        public void 구레벨은_티어_대표값으로_이행된다(int stored, int expected)
        {
            MetaProfile profile = WithLevel(EMonster.Wisp, stored);

            MonsterEnhanceTierRules.NormalizeProfile(profile);

            Assert.AreEqual(expected, LevelOf(profile, EMonster.Wisp));
            Assert.AreEqual(1234, profile.Souls, "소울 불변");
        }

        [Test]
        public void 음수는_0으로_상한_초과는_5로_보정된다()
        {
            MetaProfile negative = WithLevel(EMonster.Reaper, -3);
            MetaProfile over = WithLevel(EMonster.Reaper, 99);

            MonsterEnhanceTierRules.NormalizeProfile(negative);
            MonsterEnhanceTierRules.NormalizeProfile(over);

            Assert.AreEqual(0, LevelOf(negative, EMonster.Reaper));
            Assert.AreEqual(5, LevelOf(over, EMonster.Reaper));
        }

        [Test]
        public void 중복_항목은_가장_높은_유효_값_하나만_남긴다()
        {
            MetaProfile high = WithLevel(EMonster.Hex, 3);
            high.ShopLevels.Insert(0, new ShopLevelEntry { ItemId = MonsterEnhanceTierRules.ItemId(EMonster.Hex), Level = 1 });
            MetaProfile highFirst = WithLevel(EMonster.Hex, 1);
            highFirst.ShopLevels.Insert(0, new ShopLevelEntry { ItemId = MonsterEnhanceTierRules.ItemId(EMonster.Hex), Level = 5 });

            MonsterEnhanceTierRules.NormalizeProfile(high);
            MonsterEnhanceTierRules.NormalizeProfile(highFirst);

            Assert.AreEqual(1, high.ShopLevels.Count);
            Assert.AreEqual(4, LevelOf(high, EMonster.Hex), "Lv1·Lv3 중 높은 Lv3 → T2 대표값 4");
            Assert.AreEqual(1, highFirst.ShopLevels.Count);
            Assert.AreEqual(5, LevelOf(highFirst, EMonster.Hex), "순서와 무관하게 최고값");
        }

        [Test]
        public void 이행은_두_번_적용해도_같고_6종을_독립으로_다룬다()
        {
            MetaProfile profile = new MetaProfile { Souls = 777 };
            EMonster[] species = MonsterGrades.BaseMonsters.ToArray();
            for (int i = 0; i < species.Length; ++i)
            {
                profile.ShopLevels.Add(new ShopLevelEntry { ItemId = MonsterEnhanceTierRules.ItemId(species[i]), Level = i });
            }
            int[] expected = { 0, 2, 2, 4, 4, 5 };

            MonsterEnhanceTierRules.NormalizeProfile(profile);
            for (int i = 0; i < species.Length; ++i)
            {
                Assert.AreEqual(expected[i], LevelOf(profile, species[i]), species[i].ToString());
            }
            string once = JsonUtility.ToJson(profile);
            MonsterEnhanceTierRules.NormalizeProfile(profile);

            Assert.AreEqual(once, JsonUtility.ToJson(profile), "멱등");
            Assert.AreEqual(777, profile.Souls);
        }

        [Test]
        public void 이행은_티어_항목이_아닌_상점_항목과_미구매_항목을_건드리지_않는다()
        {
            MetaProfile profile = WithLevel(EMonster.Wisp, 1);
            profile.ShopLevels.Add(new ShopLevelEntry { ItemId = "OtherShopItem", Level = 3 });

            MonsterEnhanceTierRules.NormalizeProfile(profile);

            Assert.AreEqual(3, profile.GetShopLevel("OtherShopItem"));
            Assert.AreEqual(0, LevelOf(profile, EMonster.Plague), "항목이 없는 종은 미구매 그대로");
            Assert.AreEqual(2, profile.ShopLevels.Count, "새 행을 만들지 않는다");
        }

        [Test]
        public void 이행은_null_프로필이나_null_행에도_예외가_없다()
        {
            Assert.DoesNotThrow(() => MonsterEnhanceTierRules.NormalizeProfile(null));
            MetaProfile profile = WithLevel(EMonster.Wisp, 1);
            profile.ShopLevels.Add(null);
            Assert.DoesNotThrow(() => MonsterEnhanceTierRules.NormalizeProfile(profile));
            Assert.AreEqual(2, LevelOf(profile, EMonster.Wisp));
        }

        //# --- 모드별 편성 분기 (챕터·탑 = 저장, 데일리 = 기본) ---

        private MetaProfile CustomProfile()
        {
            MetaProfile profile = new MetaProfile { ClearedStage = _db.Config.unlockClearedStage, HasCustomFormation = true };
            profile.Formation.Add(new FormationSlotEntry { Slot = 2, Monster = EMonster.Wisp.ToString() });
            return profile;
        }

        [Test]
        public void 챕터와_탑은_저장_편성을_쓰고_데일리만_종족별_기본_편성을_쓴다()
        {
            MetaProfile profile = CustomProfile();

            EMonster?[] chapterOrTower = FormationSave.Resolve(_db, profile, false);
            EMonster?[] daily = FormationSave.Resolve(_db, profile, true);

            Assert.AreEqual(1, chapterOrTower.Count(m => m.HasValue));
            Assert.AreEqual(EMonster.Wisp, chapterOrTower[1]);
            CollectionAssert.AreEqual(FormationTestKit.Default(), daily.Select(m => m.Value).ToArray());
        }

        [Test]
        public void 데일리_기본_편성은_보유_최고_등급을_따라간다()
        {
            MetaProfile profile = CustomProfile();
            profile.SetRaceGrade(ERace.Ignis, 3);

            Assert.AreEqual(EMonster.WispG3, FormationSave.Resolve(_db, profile, true)[0]);
        }

        [Test]
        public void 수동_0칸_저장도_챕터는_빈_편성이고_데일리는_기본_편성이다()
        {
            MetaProfile profile = CustomProfile();
            profile.Formation.Clear();

            EMonster?[] chapter = FormationSave.Resolve(_db, profile, false);

            Assert.AreEqual(0, chapter.Count(m => m.HasValue), "명시적 0칸과 기본(0행)을 구별");
            Assert.AreEqual(6, FormationSave.Resolve(_db, profile, true).Count(m => m.HasValue));
        }

        [Test]
        public void 수동_플래그가_없으면_행이_남아_있어도_기본_편성이다()
        {
            MetaProfile profile = CustomProfile();
            profile.HasCustomFormation = false;

            CollectionAssert.AreEqual(FormationTestKit.Default(), FormationSave.Resolve(_db, profile, false).Select(m => m.Value).ToArray());
        }

        [Test]
        public void 해금_전에는_수동_저장이_있어도_기본_편성이다()
        {
            MetaProfile profile = CustomProfile();
            profile.ClearedStage = _db.Config.unlockClearedStage - 1;

            Assert.AreEqual(6, FormationSave.Resolve(_db, profile, false).Count(m => m.HasValue));
        }

        [Test]
        public void 편성_데이터가_없으면_null이다()
        {
            Assert.IsNull(FormationSave.Resolve(null, CustomProfile(), true));
        }

        //# --- 봉인 폴백 순서 (PickSealedSlot) ---

        private static EMonster?[] Placed(EMonster? s0, EMonster? s1, EMonster? s2, EMonster? s3, EMonster? s4, EMonster? s5)
        {
            return new[] { s0, s1, s2, s3, s4, s5 };
        }

        [Test]
        public void 봉인_대상_종이_여러_곳이면_미봉인_채워진_최저_번호다()
        {
            EMonster?[] outputs = Placed(EMonster.Wisp, EMonster.Plague, EMonster.Hex, EMonster.Reaper, EMonster.PlagueG3, EMonster.Phantom);

            Assert.AreEqual(1, FormationRules.PickSealedSlot(outputs, EMonster.Plague, _db), "등급이 달라도 같은 종");
            Assert.AreEqual(4, FormationRules.PickSealedSlot(outputs, EMonster.Plague, _db, i => i != 1), "미봉인 후보만");
        }

        [Test]
        public void 봉인_대상_종이_없으면_그_종의_기본_위치가_채워진_미봉인일_때_그곳이다()
        {
            EMonster?[] outputs = Placed(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Wisp);

            Assert.AreEqual(3, FormationRules.PickSealedSlot(outputs, EMonster.Wraith, _db), "망령 기본 위치 = 4번");
        }

        [Test]
        public void 기본_위치가_비어_있으면_채워진_최저_번호로_넘어간다()
        {
            EMonster?[] outputs = Placed(null, null, EMonster.Hex, null, EMonster.Plague, EMonster.Phantom);

            Assert.AreEqual(2, FormationRules.PickSealedSlot(outputs, EMonster.Wraith, _db));
        }

        [Test]
        public void 기본_위치가_이미_봉인된_후보_밖이면_채워진_미봉인_최저_번호다()
        {
            EMonster?[] outputs = Placed(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.Wisp);

            Assert.AreEqual(0, FormationRules.PickSealedSlot(outputs, EMonster.Wraith, _db, i => i != 3));
            Assert.AreEqual(1, FormationRules.PickSealedSlot(outputs, EMonster.Wraith, _db, i => i != 3 && i != 0));
        }

        [Test]
        public void 채운_둥지가_없거나_전부_후보_밖이면_봉인하지_않는다()
        {
            EMonster?[] empty = new EMonster?[FormationDatabase.SlotCount];
            EMonster?[] filled = Placed(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);

            Assert.AreEqual(-1, FormationRules.PickSealedSlot(empty, EMonster.Wraith, _db));
            Assert.AreEqual(-1, FormationRules.PickSealedSlot(filled, EMonster.Wraith, _db, i => false));
        }

        [Test]
        public void 대상_종이_있으면_기본_위치보다_대상_종이_우선이다()
        {
            EMonster?[] outputs = Placed(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Phantom, EMonster.Plague, EMonster.WraithG2);

            Assert.AreEqual(5, FormationRules.PickSealedSlot(outputs, EMonster.Wraith, _db));
        }

        [Test]
        public void 편성_데이터가_없어도_폴백은_채워진_최저_번호다()
        {
            EMonster?[] outputs = Placed(null, EMonster.Reaper, null, EMonster.Hex, null, null);

            Assert.AreEqual(1, FormationRules.PickSealedSlot(outputs, EMonster.Wraith, null));
        }

        //# --- ApplyFormation 빈 둥지 처리 ---

        private Spawner MakeSpawner(EMonster output)
        {
            GameObject go = new GameObject("SpawnerApply");
            _spawned.Add(go);
            Spawner sp = go.AddComponent<Spawner>();
            typeof(Spawner).GetField("_outputType", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sp, output);
            typeof(Spawner).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sp, null);
            return sp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, field + " 필드 (production 시그니처 변경 감지)");
            info.SetValue(target, value);
        }

        private Spawner[] RunApply(EMonster?[] formation, Spawner[] spawners)
        {
            GameObject go = new GameObject("BC_ApplyFormation");
            go.SetActive(false);
            _spawned.Add(go);
            BattleController bc = go.AddComponent<BattleController>();
            SetPrivate(bc, "_formation", formation);
            SetPrivate(bc, "_spawners", spawners);
            FormationDatabase.Current = _db;
            MethodInfo apply = typeof(BattleController).GetMethod("ApplyFormation", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(apply, "BattleController.ApplyFormation");
            apply.Invoke(bc, null);
            return spawners;
        }

        private Spawner[] SceneSpawners()
        {
            return FormationTestKit.Default().Select(MakeSpawner).ToArray();
        }

        [Test]
        public void 빈_둥지는_출력이_꺼지고_봉인도_풀리며_나머지_번호는_그대로다()
        {
            Spawner[] spawners = SceneSpawners();
            spawners[2].Lock();
            EMonster?[] formation = Placed(EMonster.Wisp, null, null, EMonster.Wraith, null, EMonster.Phantom);

            RunApply(formation, spawners);

            Assert.IsFalse(spawners[1].HasOutput);
            Assert.IsFalse(spawners[2].HasOutput);
            Assert.IsFalse(spawners[2].IsLocked, "비운 둥지의 봉인은 해제");
            Assert.IsFalse(spawners[4].HasOutput);
            Assert.IsTrue(spawners[0].HasOutput);
            Assert.AreEqual(EMonster.Wraith, spawners[3].CurrentType, "번호가 당겨지지 않는다");
            Assert.AreEqual(EMonster.Phantom, spawners[5].CurrentType);
        }

        [Test]
        public void 전부_빈_편성이면_모든_스포너_출력이_꺼진다()
        {
            Spawner[] spawners = RunApply(new EMonster?[FormationDatabase.SlotCount], SceneSpawners());

            Assert.IsTrue(spawners.All(s => s.HasOutput == false));
        }

        [Test]
        public void 편성의_몬스터가_씬과_다르면_그_둥지의_출력이_바뀐다()
        {
            Spawner[] spawners = SceneSpawners();
            EMonster?[] formation = Placed(EMonster.WispG3, EMonster.Wisp, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);

            RunApply(formation, spawners);

            Assert.AreEqual(EMonster.WispG3, spawners[0].CurrentType);
            Assert.AreEqual(EMonster.Wisp, spawners[1].CurrentType, "같은 종 중복 배치도 절대 대입");
        }

        [Test]
        public void 편성이_null이면_씬_배치를_그대로_둔다()
        {
            Spawner[] spawners = RunApply(null, SceneSpawners());

            CollectionAssert.AreEqual(FormationTestKit.Default(), spawners.Select(s => s.CurrentType).ToArray());
            Assert.IsTrue(spawners.All(s => s.HasOutput));
        }

        [Test]
        public void 스포너_수가_슬롯_수와_다르면_편성을_적용하지_않는다()
        {
            Spawner[] five = SceneSpawners().Take(5).ToArray();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("편성 미적용"));

            RunApply(new EMonster?[FormationDatabase.SlotCount], five);

            Assert.IsTrue(five.All(s => s.HasOutput), "빈 편성이어도 적용 전 중단");
        }

        //# --- 카드 추첨은 편성·봉인과 무관 (제외 판정이 픽 상한만 읽는 순수 경로) ---

        private List<ECardId> DrawSequence(int seed)
        {
            List<CardData> cards = new List<CardData>();
            foreach (string name in FormationTestKit.Passives())
            {
                CardData card = FakeCardData.Create(FormationTestKit.Card(name));
                _spawned.Add(card);
                cards.Add(card);
            }
            CardDeck deck = new CardDeck(cards, seed);
            CardPickCounter counter = new CardPickCounter();
            List<ECardId> drawnIds = new List<ECardId>();
            for (int pick = 0; pick < 6; ++pick)
            {
                IReadOnlyList<CardData> drawn = deck.Draw(3, id => BattleController.IsCardExcluded(counter, id));
                drawnIds.AddRange(drawn.Select(c => c.Id));
                counter.RecordPick(drawn[0].Id);
            }
            return drawnIds;
        }

        [Test]
        public void 같은_난수와_선택_이력이면_0_1_6칸_편성과_봉인_유무를_바꿔도_카드_제안이_같다()
        {
            List<FormationSlotEntry>[] variants =
            {
                new List<FormationSlotEntry>(),
                FormationTestKit.Rows(EMonster.Wisp),
                FormationTestKit.Rows(FormationTestKit.Default()),
            };
            for (int seed = 1; seed <= 20; ++seed)
            {
                List<ECardId> baseline = DrawSequence(seed);
                foreach (List<FormationSlotEntry> rows in variants)
                {
                    MetaProfile profile = new MetaProfile { ClearedStage = 5, HasCustomFormation = true, Formation = rows };
                    EMonster?[] resolved = FormationSave.Resolve(_db, profile, false);
                    int sealedSlot = FormationRules.PickSealedSlot(resolved, EMonster.Wraith, _db);

                    CollectionAssert.AreEqual(baseline, DrawSequence(seed), $"seed {seed} 칸 {rows.Count} 봉인 {sealedSlot}");
                }
            }
        }
    }
}
