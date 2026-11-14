using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    //# ChapterDatabaseLoader 의 Addressables 경유 경로(so-to-json-migration §6.2·§6.4) — 씬을 로드하지 않는 로더 단독 통합 테스트.
    //# 전투 씬 PlayMode 의 Start 레이스와 섞이지 않게 씬은 건드리지 않는다. 로더가 덮어쓰는 전역 정적은 SetUp 에서 저장하고 TearDown 에서 전부 원복한다.
    public class ChapterDatabaseLoaderPlayTests : BattlePlayTestBase
    {
        private ChapterDatabase _chapters;
        private BalanceConfig _balance;
        private HeroSkillDatabase _skills;
        private CardDatabase _cards;
        private MetaConfig _meta;
        private DifficultyTable _difficulty;
        private RelicDatabase _relic;
        private FormationDatabase _formation;
        private FormationCardSpecies _cardSpecies;
        private GradeFxDatabase _gradeFx;
        private CodexDatabase _codex;
        private ShowcaseDatabase _showcase;
        private TowerDatabase _tower;
        private DailyDatabase _daily;
        private GrowthDatabase _growth;
        private int _chapterCount;
        private object _growthValidated;
        private object _loading;
        private IStringProvider _stringProvider;

        private static FieldInfo LoaderField(string name)
        {
            FieldInfo field = typeof(ChapterDatabaseLoader).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "ChapterDatabaseLoader." + name);
            return field;
        }

        [SetUp]
        public void 로더가_덮어쓰는_전역값을_저장하고_게이트_5개를_비운다()
        {
            _chapters = ChapterDatabase.Current;
            _balance = BalanceConfig.Current;
            _skills = HeroSkillDatabase.Current;
            _cards = CardDatabase.Current;
            _meta = MetaConfig.Current;
            _difficulty = DifficultyTable.Current;
            _relic = RelicDatabase.Current;
            _formation = FormationDatabase.Current;
            _cardSpecies = FormationCardSpecies.Current;
            _gradeFx = GradeFxDatabase.Current;
            _codex = CodexDatabase.Current;
            _showcase = ShowcaseDatabase.Current;
            _tower = TowerDatabase.Current;
            _daily = DailyDatabase.Current;
            _growth = GrowthDatabase.Current;
            _chapterCount = StageProgress.ChapterCount;
            _growthValidated = LoaderField("_growthShopValidatedFor").GetValue(null);
            _loading = LoaderField("_loading").GetValue(null);
            _stringProvider = CHText.StringProvider;

            //# 이전 테스트가 남긴 문구 공급자가 있으면 Strings_Ko 존재 검증이 로더와 무관한 이유로 실패한다 — null 이면 검증은 통과하고 표시 문구만 빈 값이다.
            CHText.StringProvider = null;
            ChapterDatabase.Current = null;
            BalanceConfig.Current = null;
            HeroSkillDatabase.Current = null;
            CardDatabase.Current = null;
            MetaConfig.Current = null;
            LoaderField("_loading").SetValue(null, null);
        }

        [TearDown]
        public void 로더가_덮어쓴_전역값을_원복한다()
        {
            ChapterDatabase.Current = _chapters;
            BalanceConfig.Current = _balance;
            HeroSkillDatabase.Current = _skills;
            CardDatabase.Current = _cards;
            MetaConfig.Current = _meta;
            DifficultyTable.Current = _difficulty;
            RelicDatabase.Current = _relic;
            FormationDatabase.Current = _formation;
            FormationCardSpecies.Current = _cardSpecies;
            GradeFxDatabase.Current = _gradeFx;
            CodexDatabase.Current = _codex;
            ShowcaseDatabase.Current = _showcase;
            TowerDatabase.Current = _tower;
            DailyDatabase.Current = _daily;
            GrowthDatabase.Current = _growth;
            StageProgress.ConfigureChapterCount(_chapterCount);
            LoaderField("_growthShopValidatedFor").SetValue(null, _growthValidated);
            LoaderField("_loading").SetValue(null, _loading);
            CHText.StringProvider = _stringProvider;
        }

        private static IEnumerator WaitFor(Task task, float timeoutSec = 60f)
        {
            float start = Time.realtimeSinceStartup;
            while (task.IsCompleted == false && Time.realtimeSinceStartup - start < timeoutSec)
            {
                yield return null;
            }
            Assert.IsTrue(task.IsCompleted, "로드가 " + timeoutSec + "초 안에 끝나지 않음");
        }

        private static void AssertRequiredAllLoaded(string label)
        {
            Assert.IsNotNull(BalanceConfig.Current, label + " BalanceConfig");
            Assert.IsNotNull(HeroSkillDatabase.Current, label + " HeroSkillDatabase");
            Assert.IsNotNull(CardDatabase.Current, label + " CardDatabase");
            Assert.IsNotNull(MetaConfig.Current, label + " MetaConfig");
        }

        [UnityTest]
        public IEnumerator 로드가_성공하면_필수_묶음_4개와_챕터_DB가_모두_채워지고_진행_범위가_챕터_수에_맞춰진다()
        {
            yield return EnsureCHMReady();

            Task<ChapterDatabase> task = ChapterDatabaseLoader.EnsureLoadedAsync();
            yield return WaitFor(task);

            Assert.IsNotNull(task.Result, "로드 실패 — Console 의 [ChapterDatabaseLoader] 오류 확인");
            AssertRequiredAllLoaded("로드 후");
            Assert.AreSame(task.Result, ChapterDatabase.Current);
            Assert.AreEqual(task.Result.ChapterCount, StageProgress.ChapterCount);
            Assert.IsNull(LoaderField("_loading").GetValue(null), "끝난 로드는 _loading 에서 비워진다");
        }

        [UnityTest]
        public IEnumerator 로드가_끝난_뒤_재호출은_새_로드를_시작하지_않고_같은_인스턴스를_즉시_돌려준다()
        {
            yield return EnsureCHMReady();
            Task<ChapterDatabase> first = ChapterDatabaseLoader.EnsureLoadedAsync();
            yield return WaitFor(first);
            Assert.IsNotNull(first.Result);
            BalanceConfig balance = BalanceConfig.Current;
            HeroSkillDatabase skills = HeroSkillDatabase.Current;
            CardDatabase cards = CardDatabase.Current;
            MetaConfig meta = MetaConfig.Current;

            Task<ChapterDatabase> again = ChapterDatabaseLoader.EnsureLoadedAsync();

            Assert.IsTrue(again.IsCompleted, "재호출은 즉시 반환");
            Assert.AreSame(first.Result, again.Result);
            Assert.AreSame(balance, BalanceConfig.Current, "다시 로드되지 않음");
            Assert.AreSame(skills, HeroSkillDatabase.Current);
            Assert.AreSame(cards, CardDatabase.Current);
            Assert.AreSame(meta, MetaConfig.Current);
        }

        //# 같은 Task 를 공유하지 않고 각자 LoadAsync 를 돌리면 서로의 Current 를 비워 서로 다른 ChapterDatabase 인스턴스가 나온다.
        [UnityTest]
        public IEnumerator 동시_호출은_진행_중인_로드를_공유하고_같은_챕터_DB를_돌려준다()
        {
            yield return EnsureCHMReady();
            //# 이전 테스트가 캐시해 둔 핸들이 있으면 로드가 동기 완료돼 공유 검증이 공허해진다 — 첫 묶음 키의 캐시를 비운다.
            CHMResource.Instance.Unload(EData.BalanceConfig);
            CHMResource.Instance.Unload(EData.MonsterStat);

            Task<ChapterDatabase> first = ChapterDatabaseLoader.EnsureLoadedAsync();
            Assert.IsFalse(first.IsCompleted, "첫 호출은 비동기 로드 중이어야 한다(Addressables)");
            object loadingAfterFirst = LoaderField("_loading").GetValue(null);
            Task<ChapterDatabase> second = ChapterDatabaseLoader.EnsureLoadedAsync();
            object loadingAfterSecond = LoaderField("_loading").GetValue(null);

            Assert.IsNotNull(loadingAfterFirst, "진행 중 로드가 _loading 에 보관된다");
            Assert.AreSame(loadingAfterFirst, loadingAfterSecond, "두 번째 호출이 새 로드를 시작하지 않는다");
            yield return WaitFor(first);
            yield return WaitFor(second);
            Assert.IsNotNull(first.Result, "로드 실패 — Console 의 [ChapterDatabaseLoader] 오류 확인");
            Assert.AreSame(first.Result, second.Result, "두 호출이 같은 로드 결과를 받는다");
            Assert.AreSame(first.Result, ChapterDatabase.Current);
            AssertRequiredAllLoaded("동시 호출 후");
            Assert.IsNull(LoaderField("_loading").GetValue(null), "끝난 로드는 _loading 에서 비워진다");
        }

        [UnityTest]
        public IEnumerator 필수_묶음_하나가_비면_재호출이_다시_로드해_채운다()
        {
            yield return EnsureCHMReady();
            Task<ChapterDatabase> first = ChapterDatabaseLoader.EnsureLoadedAsync();
            yield return WaitFor(first);
            Assert.IsNotNull(first.Result);
            CardDatabase.Current = null;

            Task<ChapterDatabase> again = ChapterDatabaseLoader.EnsureLoadedAsync();
            yield return WaitFor(again);

            Assert.IsNotNull(again.Result, "카드 묶음이 비면 즉시 반환하지 않고 다시 로드한다");
            Assert.IsNotNull(CardDatabase.Current);
            AssertRequiredAllLoaded("재로드 후");
        }
    }
}
