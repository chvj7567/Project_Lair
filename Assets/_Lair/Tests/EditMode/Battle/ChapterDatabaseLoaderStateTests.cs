using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# ChapterDatabaseLoader 의 필수 묶음 불변식(so-to-json-migration §6.4) — 필수 묶음 4개(Balance·HeroSkill·Card·Meta)의 Current 는 항상 같이 null 로 되돌려진다.
    //# Addressables 를 거치는 정상 로드·동시 호출 공유는 PlayMode(ChapterDatabaseLoaderPlayTests)가 맡는다. 여기서는 로더가 CHMResource 에 닿기 전/실패하는 경로만 다룬다.
    //# 전역 정적(4개 Current, ChapterDatabase.Current, StageProgress 챕터 수, 로더 _loading, CHMResource 싱글톤 인스턴스)은 SetUp 에서 저장하고 TearDown 에서 원복한다.
    public class ChapterDatabaseLoaderStateTests : ChapterStaticStateTestBase
    {
        private BalanceConfig _balance;
        private HeroSkillDatabase _skills;
        private CardDatabase _cards;
        private MetaConfig _meta;
        private object _loading;
        private object _resourceInstance;

        //# CHMResource 싱글톤 슬롯 — 빈 인스턴스로 바꿔 끼우면(키 미등록) 모든 로드가 null 로 동기 완료돼 실패 경로가 테스트 순서와 무관하게 결정적이다.
        private static FieldInfo ResourceSlot()
        {
            FieldInfo field = typeof(CHSingletonStatic<CHMResource>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "CHSingletonStatic<CHMResource>._instance");
            return field;
        }

        private static FieldInfo LoadingField()
        {
            FieldInfo field = typeof(ChapterDatabaseLoader).GetField("_loading", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "ChapterDatabaseLoader._loading");
            return field;
        }

        [SetUp]
        public void 필수_묶음_전역값을_저장하고_비운다()
        {
            _balance = BalanceConfig.Current;
            _skills = HeroSkillDatabase.Current;
            _cards = CardDatabase.Current;
            _meta = MetaConfig.Current;
            _loading = LoadingField().GetValue(null);
            _resourceInstance = ResourceSlot().GetValue(null);
            ResourceSlot().SetValue(null, new CHMResource());
            ClearRequired();
            LoadingField().SetValue(null, null);
        }

        [TearDown]
        public void 필수_묶음_전역값을_원복한다()
        {
            BalanceConfig.Current = _balance;
            HeroSkillDatabase.Current = _skills;
            CardDatabase.Current = _cards;
            MetaConfig.Current = _meta;
            LoadingField().SetValue(null, _loading);
            ResourceSlot().SetValue(null, _resourceInstance);
        }

        private static void ClearRequired()
        {
            BalanceConfig.Current = null;
            HeroSkillDatabase.Current = null;
            CardDatabase.Current = null;
            MetaConfig.Current = null;
        }

        private static void PopulateAll()
        {
            BalanceConfig.Current = WorldMapTestSupport.ShippedBalance();
            HeroSkillDatabase.Current = WorldMapTestSupport.ShippedHeroSkills();
            CardDatabase.Current = WorldMapTestSupport.ShippedCards();
            MetaConfig.Current = WorldMapTestSupport.ShippedMeta();
            ChapterDatabase.Current = WorldMapTestSupport.FullDb();
        }

        private static void AssertRequiredAllNull(string label)
        {
            Assert.IsNull(BalanceConfig.Current, label + " BalanceConfig");
            Assert.IsNull(HeroSkillDatabase.Current, label + " HeroSkillDatabase");
            Assert.IsNull(CardDatabase.Current, label + " CardDatabase");
            Assert.IsNull(MetaConfig.Current, label + " MetaConfig");
        }

        //# --- ResetRequiredBundles ---

        [Test]
        public void ResetRequiredBundles는_필수_묶음_4개를_모두_null로_되돌리고_챕터_DB는_유지한다()
        {
            PopulateAll();
            ChapterDatabase chapters = ChapterDatabase.Current;
            MethodInfo reset = typeof(ChapterDatabaseLoader).GetMethod("ResetRequiredBundles", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(reset, "ChapterDatabaseLoader.ResetRequiredBundles");

            reset.Invoke(null, null);

            AssertRequiredAllNull("리셋 후");
            Assert.AreSame(chapters, ChapterDatabase.Current, "마을이 쓰는 챕터 DB 는 되돌리지 않는다(§6.4)");
        }

        [Test]
        public void ResetRequiredBundles는_일부만_채워져_있어도_나머지를_건드리지_않고_멱등이다()
        {
            BalanceConfig.Current = WorldMapTestSupport.ShippedBalance();
            MethodInfo reset = typeof(ChapterDatabaseLoader).GetMethod("ResetRequiredBundles", BindingFlags.NonPublic | BindingFlags.Static);

            reset.Invoke(null, null);
            reset.Invoke(null, null);

            AssertRequiredAllNull("두 번 리셋 후");
        }

        //# --- EnsureLoadedAsync 즉시 반환 ---

        [Test]
        public void 챕터와_필수_묶음이_모두_있으면_로드_없이_즉시_같은_챕터_DB를_돌려준다()
        {
            PopulateAll();
            ChapterDatabase chapters = ChapterDatabase.Current;
            BalanceConfig balance = BalanceConfig.Current;
            HeroSkillDatabase skills = HeroSkillDatabase.Current;
            CardDatabase cards = CardDatabase.Current;
            MetaConfig meta = MetaConfig.Current;

            Task<ChapterDatabase> task = ChapterDatabaseLoader.EnsureLoadedAsync();

            Assert.IsTrue(task.IsCompleted, "즉시 반환(로드를 시작하지 않음)");
            Assert.AreSame(chapters, task.Result);
            Assert.AreSame(balance, BalanceConfig.Current, "다시 로드되지 않음");
            Assert.AreSame(skills, HeroSkillDatabase.Current);
            Assert.AreSame(cards, CardDatabase.Current);
            Assert.AreSame(meta, MetaConfig.Current);
            Assert.IsNull(LoadingField().GetValue(null), "진행 중 로드가 만들어지지 않음");
        }

        //# --- 하나라도 비면 다시 로드를 시도하고 실패하면 4개가 같이 null ---
        //# SetUp 이 CHMResource 싱글톤을 빈 인스턴스로 바꿔 두므로 키 미등록 → 로드 null → 첫 묶음(밸런스) 실패가 테스트 순서와 무관하게 항상 재현된다.

        [TestCase("Balance")]
        [TestCase("HeroSkill")]
        [TestCase("Card")]
        [TestCase("Meta")]
        [TestCase("Chapter")]
        public void 다섯_개_중_하나라도_비어_있으면_즉시_반환하지_않고_재로드를_시도해_실패하면_필수_묶음이_전부_null이다(string missing)
        {
            PopulateAll();
            switch (missing)
            {
                case "Balance": BalanceConfig.Current = null; break;
                case "HeroSkill": HeroSkillDatabase.Current = null; break;
                case "Card": CardDatabase.Current = null; break;
                case "Meta": MetaConfig.Current = null; break;
                default: ChapterDatabase.Current = null; break;
            }
            LogAssert.Expect(LogType.Error, new Regex("밸런스 JSON 로드 실패"));

            Task<ChapterDatabase> task = ChapterDatabaseLoader.EnsureLoadedAsync();

            Assert.IsTrue(task.IsCompleted, "키 미등록 경로는 동기 완료");
            Assert.IsNull(task.Result, "로드 실패는 null");
            AssertRequiredAllNull(missing + " 누락 후");
            Assert.IsNull(ChapterDatabase.Current, "첫 묶음(밸런스) 실패는 챕터 DB 도 비운다");
            Assert.IsNull(LoadingField().GetValue(null), "끝난 로드는 _loading 에서 비워진다");
        }

        [Test]
        public void 로드에_실패한_뒤의_다음_호출은_다시_로드를_시도한다()
        {
            LogAssert.Expect(LogType.Error, new Regex("밸런스 JSON 로드 실패"));
            LogAssert.Expect(LogType.Error, new Regex("밸런스 JSON 로드 실패"));

            Task<ChapterDatabase> first = ChapterDatabaseLoader.EnsureLoadedAsync();
            Task<ChapterDatabase> second = ChapterDatabaseLoader.EnsureLoadedAsync();

            Assert.IsTrue(first.IsCompleted);
            Assert.IsTrue(second.IsCompleted);
            Assert.IsNull(first.Result);
            Assert.IsNull(second.Result, "두 번째 호출도 다시 시도해 로그를 한 번 더 남긴다");
            AssertRequiredAllNull("재시도 후");
        }

        [Test]
        public void LoadAsync_첫_묶음_실패는_이미_채워진_필수_묶음_4개와_챕터_DB를_모두_비운다()
        {
            PopulateAll();
            LogAssert.Expect(LogType.Error, new Regex("밸런스 JSON 로드 실패"));

            Task<ChapterDatabase> task = ChapterDatabaseLoader.LoadAsync();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsNull(task.Result);
            AssertRequiredAllNull("LoadAsync 실패 후");
            Assert.IsNull(ChapterDatabase.Current);
        }
    }
}
