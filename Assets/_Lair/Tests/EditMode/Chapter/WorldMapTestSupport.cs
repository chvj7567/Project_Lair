using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 월드맵(슬라이스 c) 테스트 공용 — shipped JSON 기반 ChapterDatabase 와 실제 Strings_Ko 조회.
    public static class WorldMapTestSupport
    {
        public const string JsonDir = "Assets/_Lair/Data/Json/";

        private static Dictionary<int, string> _strings;

        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));
        }

        //# 실서비스와 같이 로드 직후 진행 범위(MaxStage)를 챕터 수에 맞춘다(ChapterDatabaseLoader 동작) — 호출 테스트는 정적 상태 격리(ChapterStaticStateTestBase)로 원복한다.
        public static ChapterDatabase ShippedDb()
        {
            ChapterDatabase db = FullDb();
            StageProgress.ConfigureChapterCount(db.ChapterCount);
            return db;
        }

        //# shipped 데이터 전체 — ConfigureChapterCount 를 건드리지 않는다(정적 상태 불변).
        public static ChapterDatabase FullDb()
        {
            return ChapterDatabase.Build(
                Load<ChapterRow>("Chapter"), Load<StageRow>("Stage"), Load<Stage_RuleRow>("Stage_Rule"),
                Load<Stage_Rule_ParamRow>("Stage_Rule_Param"), Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"), Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"),
                Load<HeroArchetype_SkirmishRow>("HeroArchetype_Skirmish"),
                Load<HeroArchetype_ChargeRow>("HeroArchetype_Charge"), Load<HeroArchetype_CleaveRow>("HeroArchetype_Cleave"),
                Load<HeroArchetype_GuardRow>("HeroArchetype_Guard"), Load<HeroArchetype_DebuffResistRow>("HeroArchetype_DebuffResist"),
                ShippedBalance().PassiveThresholds,
                HeroSkillDatabase.GateFractionsOf(ShippedHeroSkillData()));
        }

        //# shipped BalanceConfig.json + MonsterStat.json 조립 — 로더와 같은 검증을 거친다 (so-to-json-migration §7.5).
        public static BalanceConfig ShippedBalance()
        {
            return BalanceConfig.Build(
                UnityEngine.JsonUtility.FromJson<BalanceConfigRow>(File.ReadAllText(JsonDir + "BalanceConfig.json")),
                Load<MonsterStatRow>("MonsterStat"));
        }

        //# shipped 메타 JSON 9종 묶음 — 엣지 테스트가 행을 바꿔 BuildMeta 로 다시 조립할 수 있게 조립 전 단계도 노출한다.
        public static MetaDataSet ShippedMetaData()
        {
            return new MetaDataSet
            {
                Config = UnityEngine.JsonUtility.FromJson<MetaConfigRow>(File.ReadAllText(JsonDir + "MetaConfig.json")),
                ShopItems = Load<ShopItemRow>("ShopItem"),
                Stats = Load<ShopItem_StatRow>("ShopItem_Stat"),
                Species = Load<ShopItem_SpeciesRow>("ShopItem_Species"),
                Muls = Load<ShopItem_MulRow>("ShopItem_Mul"),
                Descs = Load<ShopItem_DescRow>("ShopItem_Desc"),
                LordLevels = Load<LordLevelRow>("LordLevel"),
                LordRewards = Load<LordLevel_RewardRow>("LordLevel_Reward"),
                Achievements = Load<AchievementRow>("Achievement"),
            };
        }

        //# 엄격 조립 — Strings_Ko 에 없는 id 는 오류(로더의 provider-null 관용 없음), 재화는 Currency.json 행으로 판정한다.
        public static MetaConfig BuildMeta(MetaDataSet data)
        {
            HashSet<string> currencies = new HashSet<string>();
            foreach (CurrencyRow row in Load<CurrencyRow>("Currency"))
            {
                currencies.Add(row.currencyId);
            }
            return MetaConfig.Build(data, id => Strings().ContainsKey(id), Text, currency => currencies.Contains(currency.ToString()));
        }

        //# shipped 메타 JSON 9종 조립 — 호출마다 새 인스턴스라 테스트 간 공유 상태가 없다 (so-to-json-migration §7.5).
        public static MetaConfig ShippedMeta()
        {
            return BuildMeta(ShippedMetaData());
        }

        //# shipped 영웅 스킬 JSON 3종 묶음 — 엣지 테스트가 행을 바꿔 BuildHeroSkills 로 다시 조립할 수 있게 조립 전 단계도 노출한다.
        public static HeroSkillDataSet ShippedHeroSkillData()
        {
            return new HeroSkillDataSet
            {
                Skills = Load<HeroSkillRow>("HeroSkill"),
                Params = Load<HeroSkill_ParamRow>("HeroSkill_Param"),
                Phases = Load<HeroArchetype_SkillRow>("HeroArchetype_Skill"),
            };
        }

        //# 엄격 조립 — Strings_Ko 에 없는 id 는 오류, heroId FK·영웅 커버리지는 shipped 챕터 DB 로 판정한다.
        public static HeroSkillDatabase BuildHeroSkills(HeroSkillDataSet data)
        {
            return HeroSkillDatabase.Build(data, FullDb(), id => Strings().ContainsKey(id), Text);
        }

        public static HeroSkillDatabase ShippedHeroSkills()
        {
            return BuildHeroSkills(ShippedHeroSkillData());
        }

        //# shipped 영웅 스킬 한 종 — 클래스 이름(XxxSkillData)에서 스킬을 정해 출하 JSON 값으로 만든다.
        public static T ShippedSkill<T>() where T : HeroSkillData
        {
            HeroSkillDatabase db = ShippedHeroSkills();
            EHeroSkill id = (EHeroSkill)System.Enum.Parse(typeof(EHeroSkill), typeof(T).Name.Replace("SkillData", string.Empty));
            return (T)HeroSkillData.Create(db.GetSkill(id));
        }

        //# shipped 영웅 스킬 구간 — HeroSkillRunner.Bind 입력(구간 HP 비율 + 출하 값 스킬).
        public static List<HeroSkillPhase> ShippedSkillPhases(string heroId)
        {
            HeroSkillDatabase db = ShippedHeroSkills();
            List<HeroSkillPhase> phases = new List<HeroSkillPhase>();
            foreach (HeroSkillPhaseEntry entry in db.GetPhases(heroId))
            {
                phases.Add(new HeroSkillPhase(entry.HpFraction, HeroSkillData.Create(db.GetSkill(entry.Skill))));
            }
            return phases;
        }

        //# shipped 카드 JSON 묶음(Card·Card_Param) — 엣지 테스트가 행을 바꿔 BuildCards 로 다시 조립할 수 있게 조립 전 단계도 노출한다.
        public static CardDataSet ShippedCardData()
        {
            return new CardDataSet
            {
                Cards = Load<CardRow>("Card"),
                Params = Load<Card_ParamRow>("Card_Param"),
            };
        }

        //# shipped Card_Species 행 — Card.json 과의 FK 대조(SJ4)는 런타임이 아니라 테스트가 한다.
        public static Card_SpeciesRow[] ShippedCardSpecies()
        {
            return Load<Card_SpeciesRow>("Card_Species");
        }

        //# 카드 풀 SO 두 개(패시브·액티브)가 가진 카드 id — Card.json ↔ 풀 양방향 대조(SJ5, EditMode 전용)의 입력.
        public static List<ECardId> ShippedPoolCardIds()
        {
            List<ECardId> ids = new List<ECardId>();
            foreach (string pool in new[] { "CardPool_Passive", "CardPool_Active" })
            {
                foreach (CardData card in UnityEditor.AssetDatabase.LoadAssetAtPath<CardPool>("Assets/_Lair/Art/Cards/" + pool + ".asset").Cards)
                {
                    ids.Add(card.Id);
                }
            }
            return ids;
        }

        //# 엄격 조립 — Strings_Ko 에 없는 id 는 오류(로더의 provider-null 관용 없음).
        public static CardDatabase BuildCards(CardDataSet data)
        {
            return CardDatabase.Build(data, id => Strings().ContainsKey(id), Text);
        }

        public static CardDatabase ShippedCards()
        {
            return BuildCards(ShippedCardData());
        }

        public static IReadOnlyDictionary<int, string> Strings()
        {
            if (_strings == null)
            {
                Dictionary<int, string> table = new Dictionary<int, string>();
                foreach (StringEntry entry in Load<StringEntry>("Strings_Ko"))
                {
                    table[entry.id] = entry.text;
                }
                _strings = table;
            }
            return _strings;
        }

        //# 실제 Strings_Ko — id 누락이면 빈 문자열.
        public static string Text(int id)
        {
            return Strings().TryGetValue(id, out string text) ? text : string.Empty;
        }
    }
}
