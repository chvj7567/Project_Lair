using System;
using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# SO→JSON 이전 회귀 고정 (so-to-json-migration §4·§5·§8·§10 SJ7·§12.5) — 세이브 키·표시 순서·카드 풀 순서·문구 id 블록·잔여물.
    //# 정답 목록은 기획서에서 가져와 하드코딩한 것이다(production 표에서 파생하지 않는다). 수치 골든은 D8 에 따라 두지 않는다.
    public class SoToJsonRegressionTests
    {
        private static readonly string[] Species = { "Wisp", "Wraith", "Reaper", "Hex", "Plague", "Phantom" };
        private static readonly string[] AllCardIdNames =
        {
            "WispHpBoost", "WraithDamageBoost", "ReaperAtkSpeed", "HexRangeBoost", "PlagueSlowBoost", "PhantomMoveSpeedBoost",
            "SpawnWisps", "SpawnWraith", "SpawnReapers", "SpawnPlagues", "SpawnPhantoms", "ReplaceWispsToWraith", "ReplaceReapersToHex",
            "HeroPoisonAura", "HeroAttackDown", "Fear", "Bleed", "Weaken", "Slow", "Frenzy", "Multiply", "BloodThirst", "IronWill",
            "TimeStop", "Berserk", "WallOfWisps", "MarkOfDeath", "SpawnerHaste", "LingeringCurse",
        };
        private static readonly string[] PassivePool =
        {
            "WispHpBoost", "WraithDamageBoost", "SpawnWraith", "ReplaceWispsToWraith", "ReaperAtkSpeed", "HexRangeBoost", "SpawnReapers",
            "ReplaceReapersToHex", "PlagueSlowBoost", "SpawnPlagues", "LingeringCurse", "HeroAttackDown", "PhantomMoveSpeedBoost",
            "SpawnPhantoms", "SpawnWisps", "SpawnerHaste",
        };
        private static readonly string[] ActivePool =
        {
            "IronWill", "WallOfWisps", "Berserk", "Frenzy", "BloodThirst", "MarkOfDeath", "Fear", "Bleed", "Weaken", "TimeStop", "Multiply", "Slow",
        };

        //# §4.4 — 7(전종·주기) + 체급 6 + 각성 6 + 둥지 6 + 특성 6.
        private static List<string> ExpectedShopItemIds()
        {
            List<string> ids = new List<string>
            {
                "MonsterHpUp", "MonsterPowerUp", "MonsterAtkSpeedUp", "MonsterMoveSpeedUp", "MonsterRangeUp", "PlagueVenomUp", "SpawnerHasteUp",
            };
            foreach (string prefix in new[] { "Enhance_", "Awaken_", "Nest_", "Trait_" })
            {
                foreach (string species in Species)
                {
                    ids.Add(prefix + species);
                }
            }
            return ids;
        }

        private static readonly string[] ExpectedAchievementIds =
        {
            "FirstRun", "FirstWin", "Win120", "Win90", "Win60", "Wins5", "Wins10", "Wins25", "Wins50", "Runs10", "Runs30", "SynergyTier2", "SynergyTier3",
        };

        private static T[] Rows<T>(string name) => JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(WorldMapTestSupport.JsonDir + name + ".json"));

        //# --- 세이브 키 ---

        [Test]
        public void 상점_항목_id_31개는_세이브_키_목록과_JSON_행_순서까지_같다()
        {
            List<string> expected = ExpectedShopItemIds();

            List<string> fromJson = new List<string>();
            foreach (ShopItemRow row in Rows<ShopItemRow>("ShopItem"))
            {
                fromJson.Add(row.itemId);
            }
            List<string> fromMeta = new List<string>();
            foreach (ShopItemDef def in WorldMapTestSupport.ShippedMeta().ShopItems)
            {
                fromMeta.Add(def.Id);
            }

            Assert.AreEqual(31, expected.Count);
            CollectionAssert.AreEqual(expected, fromJson, "ShopItem.json 행 순서");
            CollectionAssert.AreEqual(expected, fromMeta, "MetaConfig.ShopItems 표시 순서");
        }

        [Test]
        public void 도전과제_id_13개는_세이브_키_목록과_JSON_행_순서까지_같다()
        {
            List<string> fromJson = new List<string>();
            foreach (AchievementRow row in Rows<AchievementRow>("Achievement"))
            {
                fromJson.Add(row.achievementId);
            }
            List<string> fromMeta = new List<string>();
            foreach (AchievementDef def in WorldMapTestSupport.ShippedMeta().Achievements)
            {
                fromMeta.Add(def.Id);
            }

            CollectionAssert.AreEqual(ExpectedAchievementIds, fromJson, "Achievement.json 행 순서");
            CollectionAssert.AreEqual(ExpectedAchievementIds, fromMeta, "MetaConfig.Achievements 표시 순서");
        }

        //# 세이브는 ECardId 값명 문자열을 쓰고 CardData._id 는 정수로 직렬화된다 — 값명과 정수값 둘 다 불변이어야 한다(추가는 끝에만).
        [Test]
        public void ECardId_값명과_정수값은_이전_목록과_같다()
        {
            string[] names = Enum.GetNames(typeof(ECardId));

            Assert.GreaterOrEqual(names.Length, AllCardIdNames.Length);
            for (int i = 0; i < AllCardIdNames.Length; i++)
            {
                Assert.AreEqual(AllCardIdNames[i], names[i], "인덱스 " + i);
                Assert.AreEqual(i, (int)Enum.Parse(typeof(ECardId), AllCardIdNames[i]), AllCardIdNames[i]);
            }
        }

        [Test]
        public void Card_JSON_행_순서는_ECardId_순서에서_폐기_자리만_뺀_28장이다()
        {
            List<string> expected = new List<string>(AllCardIdNames);
            expected.Remove("HeroPoisonAura");
            List<string> fromJson = new List<string>();
            foreach (CardRow row in Rows<CardRow>("Card"))
            {
                fromJson.Add(row.cardId);
            }
            List<string> fromDb = new List<string>();
            foreach (CardEntry entry in WorldMapTestSupport.ShippedCards().Cards)
            {
                fromDb.Add(entry.Id.ToString());
            }

            Assert.AreEqual(28, expected.Count);
            CollectionAssert.AreEqual(expected, fromJson, "Card.json 행 순서");
            CollectionAssert.AreEqual(expected, fromDb, "CardDatabase.Cards 순서");
        }

        //# --- 카드 풀 순서 (데일리 카드 시드가 의존) ---

        private static List<string> PoolOrder(string poolName)
        {
            CardPool pool = AssetDatabase.LoadAssetAtPath<CardPool>("Assets/_Lair/Art/Cards/" + poolName + ".asset");
            Assert.IsNotNull(pool, poolName);
            List<string> ids = new List<string>();
            foreach (CardData card in pool.Cards)
            {
                ids.Add(card.Id.ToString());
            }
            return ids;
        }

        [Test]
        public void 패시브_풀은_16장이_기획서_순서와_같다()
        {
            CollectionAssert.AreEqual(PassivePool, PoolOrder("CardPool_Passive"));
        }

        [Test]
        public void 액티브_풀은_12장이_기획서_순서와_같다()
        {
            CollectionAssert.AreEqual(ActivePool, PoolOrder("CardPool_Active"));
        }

        [Test]
        public void 두_풀의_합은_Card_JSON_28장과_집합이_같고_겹치지_않는다()
        {
            List<string> all = PoolOrder("CardPool_Passive");
            all.AddRange(PoolOrder("CardPool_Active"));
            HashSet<string> distinct = new HashSet<string>(all);

            Assert.AreEqual(28, all.Count);
            Assert.AreEqual(28, distinct.Count, "풀 사이 중복 없음");
            foreach (CardRow row in Rows<CardRow>("Card"))
            {
                Assert.IsTrue(distinct.Contains(row.cardId), row.cardId);
            }
        }

        [Test]
        public void 카드_SO_에셋은_28개이고_파일명과_id가_같으며_id_그림_외_필드가_없다()
        {
            string[] files = Directory.GetFiles("Assets/_Lair/Art/Cards/Items", "*.asset");
            HashSet<string> names = new HashSet<string>();

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(file.Replace('\\', '/'));
                string yaml = File.ReadAllText(file);

                Assert.IsNotNull(card, file);
                Assert.AreEqual(name, card.Id.ToString(), "파일명 = ECardId 값명");
                foreach (string stale in new[] { "_axis:", "_displayName:", "_description:", "_effect:" })
                {
                    StringAssert.DoesNotContain(stale, yaml, name + " 에 이전 SO 필드 잔여");
                }
                names.Add(name);
            }

            Assert.AreEqual(28, files.Length);
            foreach (CardRow row in Rows<CardRow>("Card"))
            {
                Assert.IsTrue(names.Contains(row.cardId), row.cardId + " SO 에셋 없음");
            }
        }

        //# --- 영주 트랙 ---

        [Test]
        public void 영주_트랙은_2에서_10까지_9칸이고_보상은_2_3_5_7_9만_있다()
        {
            MetaConfig meta = WorldMapTestSupport.ShippedMeta();
            int[] levels = { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            HashSet<int> rewarded = new HashSet<int> { 2, 3, 5, 7, 9 };

            Assert.AreEqual(levels.Length, meta.LordRewards.Count);
            for (int i = 0; i < levels.Length; i++)
            {
                Assert.AreEqual(levels[i], meta.LordRewards[i].Level, "표시 순서 " + i);
                Assert.AreEqual(rewarded.Contains(levels[i]) == false, meta.LordRewards[i].IsLockedDummy, "level " + levels[i]);
                if (meta.LordRewards[i].IsLockedDummy)
                {
                    Assert.AreEqual(0, meta.LordRewards[i].RewardSouls, "잠금 칸 소울");
                    Assert.AreEqual(string.Empty, meta.LordRewards[i].DisplayName, "잠금 칸 이름");
                    continue;
                }
                Assert.Greater(meta.LordRewards[i].RewardSouls, 0, "level " + levels[i]);
                Assert.IsNotEmpty(meta.LordRewards[i].DisplayName, "level " + levels[i]);
            }
        }

        //# --- 상점 항목 구조 규칙 (접두 → 효과·재화·종족) ---

        [Test]
        public void 상점_항목의_접두는_효과_종류_재화_종족과_일관된다()
        {
            MetaConfig meta = WorldMapTestSupport.ShippedMeta();
            Dictionary<string, (EShopEffectKind kind, ECurrency currency)> byPrefix = new Dictionary<string, (EShopEffectKind, ECurrency)>
            {
                { "Enhance_", (EShopEffectKind.MonsterSpecies, ECurrency.Soul) },
                { "Awaken_", (EShopEffectKind.MonsterAwaken, ECurrency.AwakenStone) },
                { "Nest_", (EShopEffectKind.NestMastery, ECurrency.MoonSeal) },
                { "Trait_", (EShopEffectKind.MonsterTrait, ECurrency.AbyssCrystal) },
            };
            int speciesItems = 0;

            foreach (ShopItemDef def in meta.ShopItems)
            {
                bool matched = false;
                foreach (KeyValuePair<string, (EShopEffectKind kind, ECurrency currency)> pair in byPrefix)
                {
                    if (def.Id.StartsWith(pair.Key) == false)
                        continue;
                    matched = true;
                    speciesItems++;
                    Assert.AreEqual(pair.Value.kind, def.EffectKind, def.Id + " effectKind");
                    Assert.AreEqual(pair.Value.currency, def.Currency, def.Id + " currency");
                    Assert.AreEqual(def.Id.Substring(pair.Key.Length), def.Species.ToString(), def.Id + " 종족");
                }
                if (matched == false)
                {
                    Assert.AreEqual(ECurrency.Soul, def.Currency, def.Id + " 전종 항목은 소울 결제");
                    Assert.IsTrue(def.EffectKind == EShopEffectKind.MonsterStat || def.EffectKind == EShopEffectKind.SpawnerPeriod, def.Id);
                }
            }

            Assert.AreEqual(24, speciesItems);
        }

        [Test]
        public void 설명_행은_육성_3축_중_둥지_특성_12행을_빼고_있다()
        {
            MetaConfig meta = WorldMapTestSupport.ShippedMeta();
            int withDescription = 0;

            foreach (ShopItemDef def in meta.ShopItems)
            {
                bool noDescription = def.Id.StartsWith("Nest_") || def.Id.StartsWith("Trait_");
                Assert.AreEqual(noDescription, string.IsNullOrEmpty(def.Description), def.Id);
                withDescription += noDescription ? 0 : 1;
            }

            Assert.AreEqual(19, withDescription);
            Assert.AreEqual(meta.FindShopItem("Enhance_Wisp").Description, meta.FindShopItem("Enhance_Phantom").Description, "체급 6행은 한 문구를 공유");
        }

        //# --- Strings_Ko 1541~1687 ---

        [Test]
        public void Strings_Ko_1541에서_1687은_각_id가_정확히_한_행이다()
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            foreach (StringEntry entry in Rows<StringEntry>("Strings_Ko"))
            {
                if (entry.id >= 1541 && entry.id <= 1687)
                {
                    counts[entry.id] = counts.TryGetValue(entry.id, out int count) ? count + 1 : 1;
                }
            }

            Assert.AreEqual(147, counts.Count);
            for (int id = 1541; id <= 1687; id++)
            {
                Assert.IsTrue(counts.ContainsKey(id), "id " + id + " 없음");
                Assert.AreEqual(1, counts[id], "id " + id + " 중복");
            }
        }

        private static void CountUse(Dictionary<int, int> uses, int id)
        {
            uses[id] = uses.TryGetValue(id, out int count) ? count + 1 : 1;
        }

        //# 신규 147개가 전부 쓰이고(고아 문구 없음) 같은 id 를 두 곳이 쓰는 것은 체급 설명 6행의 1579 하나뿐이다(§5.3).
        [Test]
        public void 신규_문구_147개는_모두_JSON에서_참조되고_공유는_1579_하나뿐이다()
        {
            Dictionary<int, int> uses = new Dictionary<int, int>();
            foreach (ShopItemRow row in Rows<ShopItemRow>("ShopItem"))
            {
                CountUse(uses, row.nameKey);
            }
            foreach (ShopItem_DescRow row in Rows<ShopItem_DescRow>("ShopItem_Desc"))
            {
                CountUse(uses, row.descKey);
            }
            foreach (LordLevel_RewardRow row in Rows<LordLevel_RewardRow>("LordLevel_Reward"))
            {
                CountUse(uses, row.nameKey);
            }
            foreach (AchievementRow row in Rows<AchievementRow>("Achievement"))
            {
                CountUse(uses, row.nameKey);
                CountUse(uses, row.descKey);
            }
            foreach (HeroSkillRow row in Rows<HeroSkillRow>("HeroSkill"))
            {
                CountUse(uses, row.nameKey);
            }
            foreach (CardRow row in Rows<CardRow>("Card"))
            {
                CountUse(uses, row.nameKey);
                CountUse(uses, row.descKey);
            }

            Assert.AreEqual(147, uses.Count, "참조된 서로 다른 id 수");
            for (int id = 1541; id <= 1687; id++)
            {
                Assert.IsTrue(uses.ContainsKey(id), "id " + id + " 를 참조하는 JSON 이 없음");
                Assert.AreEqual(id == 1579 ? 6 : 1, uses[id], "id " + id + " 사용 횟수");
            }
        }

        //# §5.1 블록 배정 — 행 순서와 id 가 1:1 로 대응한다.
        [Test]
        public void 문구_id_블록은_기획서_5_1_배정과_행_순서가_같다()
        {
            ShopItemRow[] items = Rows<ShopItemRow>("ShopItem");
            for (int i = 0; i < items.Length; i++)
            {
                Assert.AreEqual(1541 + i, items[i].nameKey, "상점 이름 " + items[i].itemId);
            }
            Dictionary<string, int> expectedDesc = new Dictionary<string, int>
            {
                { "MonsterHpUp", 1572 }, { "MonsterPowerUp", 1573 }, { "MonsterAtkSpeedUp", 1574 }, { "MonsterMoveSpeedUp", 1575 },
                { "MonsterRangeUp", 1576 }, { "PlagueVenomUp", 1577 }, { "SpawnerHasteUp", 1578 },
            };
            for (int i = 0; i < Species.Length; i++)
            {
                expectedDesc["Enhance_" + Species[i]] = 1579;
                expectedDesc["Awaken_" + Species[i]] = 1580 + i;
            }
            ShopItem_DescRow[] descs = Rows<ShopItem_DescRow>("ShopItem_Desc");
            Assert.AreEqual(expectedDesc.Count, descs.Length);
            foreach (ShopItem_DescRow row in descs)
            {
                Assert.AreEqual(expectedDesc[row.itemId], row.descKey, "상점 설명 " + row.itemId);
            }
            LordLevel_RewardRow[] rewards = Rows<LordLevel_RewardRow>("LordLevel_Reward");
            for (int i = 0; i < rewards.Length; i++)
            {
                Assert.AreEqual(1586 + i, rewards[i].nameKey, "영주 보상 " + rewards[i].level);
            }
            AchievementRow[] achievements = Rows<AchievementRow>("Achievement");
            for (int i = 0; i < achievements.Length; i++)
            {
                Assert.AreEqual(1591 + i, achievements[i].nameKey, "도전과제 이름 " + achievements[i].achievementId);
                Assert.AreEqual(1604 + i, achievements[i].descKey, "도전과제 설명 " + achievements[i].achievementId);
            }
            HeroSkillRow[] skills = Rows<HeroSkillRow>("HeroSkill");
            for (int i = 0; i < skills.Length; i++)
            {
                Assert.AreEqual(1617 + i, skills[i].nameKey, "스킬 이름 " + skills[i].skillId);
            }
            CardRow[] cards = Rows<CardRow>("Card");
            for (int i = 0; i < cards.Length; i++)
            {
                Assert.AreEqual(1632 + i, cards[i].nameKey, "카드 이름 " + cards[i].cardId);
                Assert.AreEqual(1660 + i, cards[i].descKey, "카드 설명 " + cards[i].cardId);
            }
        }

        //# --- EData · 에셋 키 ---

        [Test]
        public void EData_신규_16개는_기획서_순서로_연속하고_폐기_로드아웃_5개_자리는_남아_있다()
        {
            string[] added =
            {
                "BalanceConfig", "MonsterStat", "MetaConfig", "ShopItem", "ShopItem_Stat", "ShopItem_Species", "ShopItem_Mul", "ShopItem_Desc",
                "LordLevel", "LordLevel_Reward", "Achievement", "HeroSkill", "HeroSkill_Param", "HeroArchetype_Skill", "Card", "Card_Param",
            };
            int first = (int)Enum.Parse(typeof(EData), added[0]);

            for (int i = 0; i < added.Length; i++)
            {
                Assert.AreEqual(first + i, (int)Enum.Parse(typeof(EData), added[i]), added[i]);
                Assert.IsTrue(File.Exists(WorldMapTestSupport.JsonDir + added[i] + ".json"), added[i] + ".json");
            }
            foreach (string retired in new[] { "HeroSkillLoadout", "HeroSkillLoadout_Archer", "HeroSkillLoadout_Assassin", "HeroSkillLoadout_Berserker", "HeroSkillLoadout_Paladin" })
            {
                Assert.IsTrue(Enum.IsDefined(typeof(EData), retired), retired + " 자리 보존");
                Assert.Less((int)Enum.Parse(typeof(EData), retired), first, retired + " 은 신규 값보다 앞 정수");
            }
        }

        //# --- SJ7 잔여물 ---

        [Test]
        public void 삭제한_동기_사본_JSON_4개와_SO_에셋과_에디터_도구는_남아_있지_않다()
        {
            string[] gone =
            {
                WorldMapTestSupport.JsonDir + "balance" + "_config.json",
                WorldMapTestSupport.JsonDir + "hero" + "_skills.json",
                WorldMapTestSupport.JsonDir + "card" + "s.json",
                WorldMapTestSupport.JsonDir + "card" + "_pools.json",
                "Assets/_Lair/Data/BalanceConfig.asset",
                "Assets/_Lair/Data/MetaConfig.asset",
            };

            foreach (string path in gone)
            {
                Assert.IsFalse(File.Exists(path), path + " 가 남아 있음");
            }
            Assert.IsFalse(Directory.Exists("Assets/_Lair/Editor/Json" + "Sync"), "JSON Sync 에디터 폴더");
            Assert.IsFalse(Directory.Exists("Assets/_Lair/Editor/MetaTool"), "Meta Editor 폴더");
            Assert.IsFalse(Directory.Exists("Assets/_Lair/Art/Skills"), "스킬·로드아웃 SO 폴더");
        }

        [Test]
        public void 삭제한_SO_GUID는_씬과_프리팹에_남아_있지_않다()
        {
            string[] guids = { "f37d9d9829bfbdf4198bbf1e4215d0ca", "c667348905314e24f96b1b3e1f099b96" };
            List<string> files = new List<string>(Directory.GetFiles("Assets/_Lair", "*.unity", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles("Assets/_Lair", "*.prefab", SearchOption.AllDirectories));

            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                foreach (string guid in guids)
                {
                    StringAssert.DoesNotContain(guid, text, file);
                }
            }
            Assert.Greater(files.Count, 0);
        }

        [Test]
        public void 코드는_삭제한_동기_사본_파일명과_JSON_Sync_도구를_참조하지_않는다()
        {
            string[] tokens = { "balance" + "_config.json", "hero" + "_skills.json", "card" + "_pools.json", "Json" + "Sync" };
            List<string> files = new List<string>(Directory.GetFiles("Assets/_Lair/Scripts", "*.cs", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles("Assets/_Lair/Editor", "*.cs", SearchOption.AllDirectories));

            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                foreach (string token in tokens)
                {
                    StringAssert.DoesNotContain(token, text, file);
                }
            }
        }

        //# C1 — HeroStageVariant SO 에는 초상 참조와 엔트리 키만 남고 크기·티어·발광 직렬화 잔여가 없다(Stage.json 이 출처).
        [Test]
        public void 영웅_스테이지_외형_SO에는_초상과_키만_남고_Stage_JSON이_크기_티어를_가진다()
        {
            string yaml = File.ReadAllText("Assets/_Lair/Data/HeroStageVariantConfig.asset");
            foreach (string stale in new[] { "UseEmission", "EmissionColor", "EmissionIntensity", "ScaleMultiplier", "Tier:" })
            {
                StringAssert.DoesNotContain(stale, yaml, "이전 SO 필드 " + stale);
            }

            StageRow[] stages = Rows<StageRow>("Stage");
            Assert.AreEqual(25, stages.Length);
            foreach (StageRow stage in stages)
            {
                Assert.Greater(stage.heroScale, 0f, stage.chapterId + "-" + stage.stageIndex);
                Assert.That(stage.heroTier, Is.InRange(0, 4), stage.chapterId + "-" + stage.stageIndex);
            }
        }
    }
}
