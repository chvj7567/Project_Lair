using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Character
{
    //# 티어 시트 로더 엣지·회귀(monster-tier-sheets §7·§10 U-T4·U-T5) — 시드(MonsterTierSheetLoaderTests)와 중복 없이 키 규칙·재시도·예외·시간 초과·해제 멱등을 고정.
    //# LoadMonsterTierSheets(티어 산출)의 조각인 TierOfLevel·BaseOf·TryKeyOf 도 여기서 고정한다(BattleController 본체는 Addressables 의존이라 PlayMode 영역).
    public class MonsterTierSheetLoaderEdgeTests
    {
        private readonly List<MonsterTierSheetSet> _sets = new List<MonsterTierSheetSet>();

        private MonsterTierSheetSet NewSet()
        {
            MonsterTierSheetSet set = ScriptableObject.CreateInstance<MonsterTierSheetSet>();
            _sets.Add(set);
            return set;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (MonsterTierSheetSet set in _sets)
            {
                Object.DestroyImmediate(set);
            }
            _sets.Clear();
        }

        //# ───────── 티어 산출 (BattleController.LoadMonsterTierSheets 가 쓰는 표) ─────────

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        public void 상점_강화_레벨은_티어_0_1_1_2_2_3_으로_변환된다(int level, int expectedTier)
        {
            Assert.AreEqual(expectedTier, MonsterEnhancementVisual.TierOfLevel(level));
        }

        [TestCase(-1, 0)]
        [TestCase(-100, 0)]
        [TestCase(6, 3)]
        [TestCase(99, 3)]
        public void 범위_밖_레벨은_양끝_티어로_클램프된다(int level, int expectedTier)
        {
            Assert.AreEqual(expectedTier, MonsterEnhancementVisual.TierOfLevel(level));
        }

        [Test]
        public void 기준_몬스터_6종_x_티어_1_3_의_키는_모두_존재한다()
        {
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                for (int tier = 1; tier <= 3; tier++)
                {
                    Assert.IsTrue(MonsterTierSheetLoader.TryKeyOf(monster, tier, out EMonsterTierSheet key), $"{monster} T{tier} 키");
                    Assert.AreEqual($"{monster}T{tier}", key.ToString(), "값명 = <기준 몬스터>T<티어>");
                }
            }
        }

        [Test]
        public void 티어_0과_4_이상은_키가_없다()
        {
            Assert.IsFalse(MonsterTierSheetLoader.TryKeyOf(EMonster.Wisp, 0, out EMonsterTierSheet _));
            Assert.IsFalse(MonsterTierSheetLoader.TryKeyOf(EMonster.Wisp, 4, out EMonsterTierSheet _));
            Assert.IsFalse(MonsterTierSheetLoader.TryKeyOf(EMonster.Wisp, -1, out EMonsterTierSheet _));
        }

        [Test]
        public void 등급_몬스터를_그대로_넘기면_키가_없고_기준_몬스터로_바꾸면_있다()
        {
            Assert.IsFalse(MonsterTierSheetLoader.TryKeyOf(EMonster.WispG2, 1, out EMonsterTierSheet _), "등급 enum 으로는 키 없음 - 호출부가 BaseOf 로 환산해야 함");
            foreach (EMonster baseMonster in MonsterGrades.BaseMonsters)
            {
                for (int grade = 2; grade <= 5; grade++)
                {
                    EMonster graded = MonsterGrades.Of(baseMonster, grade);
                    EMonster root = MonsterGrades.BaseOf(graded);
                    Assert.AreEqual(baseMonster, root, $"{graded} 의 기준");
                    Assert.IsTrue(MonsterTierSheetLoader.TryKeyOf(root, 3, out EMonsterTierSheet _), $"{graded} → {root} T3 키");
                }
            }
        }

        //# ───────── 로더 엣지 ─────────

        [Test]
        public async Task 키가_없는_요청은_로드_0건_경고_한줄로_건너뛴다()
        {
            int calls = 0;
            List<string> warnings = new List<string>();
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => { calls++; return Task.FromResult(NewSet()); }, key => { }, warnings.Add);

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Wisp, 0), (EMonster.WispG3, 2) });

            Assert.AreEqual(0, calls, "키가 없으면 로드 호출 없음");
            Assert.AreEqual(2, warnings.Count, "키 없는 요청마다 경고 1줄");
            Assert.IsNull(loader.Find(EMonster.Wisp, 0));
            Assert.IsNull(loader.Find(EMonster.WispG3, 2));
        }

        [Test]
        public async Task 요청_목록이_비어_있으면_아무_일도_하지_않는다()
        {
            int calls = 0;
            int warnCount = 0;
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => { calls++; return Task.FromResult(NewSet()); }, key => { }, msg => warnCount++);

            await loader.LoadAsync(new List<(EMonster, int)>());

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, warnCount);
        }

        [Test]
        public async Task 첫_시도가_실패하고_재시도가_성공하면_등록하고_경고는_없다()
        {
            MonsterTierSheetSet set = NewSet();
            int calls = 0;
            int warnCount = 0;
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => { calls++; return Task.FromResult(calls == 1 ? null : set); }, key => { }, msg => warnCount++);

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Reaper, 1) });

            Assert.AreEqual(2, calls, "실패 1회 + 재시도 1회");
            Assert.AreSame(set, loader.Find(EMonster.Reaper, 1));
            Assert.AreEqual(0, warnCount, "재시도 성공이면 경고 없음");
        }

        [Test]
        public async Task 로드가_예외를_던지면_null_취급으로_재시도_후_폴백한다()
        {
            int calls = 0;
            List<string> warnings = new List<string>();
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => { calls++; throw new System.InvalidOperationException("boom"); }, key => { }, warnings.Add);

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Plague, 2) });

            Assert.AreEqual(2, calls, "예외도 1회 재시도");
            Assert.IsNull(loader.Find(EMonster.Plague, 2), "풋내기 폴백");
            Assert.AreEqual(1, warnings.Count, "경고 1줄");
        }

        [Test]
        public async Task 여러_종족은_각자_로드되어_자기_세트로_조회된다()
        {
            Dictionary<EMonsterTierSheet, MonsterTierSheetSet> byKey = new Dictionary<EMonsterTierSheet, MonsterTierSheetSet>();
            List<EMonsterTierSheet> unloaded = new List<EMonsterTierSheet>();
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key =>
                {
                    MonsterTierSheetSet set = NewSet();
                    byKey[key] = set;
                    return Task.FromResult(set);
                },
                key => unloaded.Add(key),
                msg => Assert.Fail(msg));

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Wisp, 1), (EMonster.Hex, 3), (EMonster.Phantom, 2) });

            Assert.AreEqual(3, byKey.Count);
            Assert.AreSame(byKey[EMonsterTierSheet.WispT1], loader.Find(EMonster.Wisp, 1));
            Assert.AreSame(byKey[EMonsterTierSheet.HexT3], loader.Find(EMonster.Hex, 3));
            Assert.AreSame(byKey[EMonsterTierSheet.PhantomT2], loader.Find(EMonster.Phantom, 2));
            Assert.IsNull(loader.Find(EMonster.Wisp, 2), "다른 티어는 없음");
            Assert.IsNull(loader.Find(EMonster.Hex, 1), "다른 티어는 없음");

            loader.Release();
            Assert.AreEqual(3, unloaded.Count, "로드한 키 전부 해제");
            Assert.IsNull(loader.Find(EMonster.Hex, 3));
        }

        [Test]
        public async Task 일부만_실패해도_성공한_종족은_등록되고_실패_종족만_경고한다()
        {
            MonsterTierSheetSet ok = NewSet();
            List<string> warnings = new List<string>();
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => Task.FromResult(key == EMonsterTierSheet.WispT1 ? ok : null), key => { }, warnings.Add);

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Wisp, 1), (EMonster.Wraith, 1) });

            Assert.AreSame(ok, loader.Find(EMonster.Wisp, 1));
            Assert.IsNull(loader.Find(EMonster.Wraith, 1));
            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("Wraith", warnings[0]);
        }

        [Test]
        public async Task Release_는_두번_호출해도_해제는_한번만_일어난다()
        {
            int unloadCount = 0;
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => Task.FromResult(NewSet()), key => unloadCount++, msg => { });
            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Wisp, 1) });

            loader.Release();
            loader.Release();

            Assert.AreEqual(1, unloadCount, "재진입 방지 - 이중 Unload 없음");
        }

        [Test]
        public void 로드_전에_Release_를_호출해도_예외가_없다()
        {
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(key => Task.FromResult<MonsterTierSheetSet>(null), key => { }, msg => { });

            Assert.DoesNotThrow(() => loader.Release());
            Assert.IsNull(loader.Find(EMonster.Wisp, 1));
        }

        [Test]
        public async Task Release_후_재로드하면_다시_조회된다()
        {
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => Task.FromResult(NewSet()), key => { }, msg => { });
            List<(EMonster, int)> needs = new List<(EMonster, int)> { (EMonster.Reaper, 2) };

            await loader.LoadAsync(needs);
            loader.Release();
            await loader.LoadAsync(needs);

            Assert.IsNotNull(loader.Find(EMonster.Reaper, 2), "재도전 시 재로드(U-T4)");
        }

        [Test]
        public async Task 시간_초과한_종족은_폴백하고_늦게_도착한_세트는_해제한다()
        {
            MonsterTierSheetSet fast = NewSet();
            MonsterTierSheetSet late = NewSet();
            TaskCompletionSource<MonsterTierSheetSet> hang = new TaskCompletionSource<MonsterTierSheetSet>();
            List<EMonsterTierSheet> unloaded = new List<EMonsterTierSheet>();
            List<string> warnings = new List<string>();
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => key == EMonsterTierSheet.WispT1 ? Task.FromResult(fast) : hang.Task,
                key => unloaded.Add(key),
                warnings.Add);

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Wisp, 1), (EMonster.Hex, 2) }, 50);

            Assert.AreSame(fast, loader.Find(EMonster.Wisp, 1), "제때 온 종족은 등록");
            Assert.IsNull(loader.Find(EMonster.Hex, 2), "시간 초과 종족은 풋내기");
            Assert.AreEqual(1, warnings.Count);

            hang.SetResult(late);
            for (int i = 0; i < 200 && unloaded.Count == 0; i++)
            {
                await Task.Delay(10);
            }

            Assert.AreEqual(new[] { EMonsterTierSheet.HexT2 }, unloaded.ToArray(), "늦게 도착한 세트는 즉시 해제");
            Assert.IsNull(loader.Find(EMonster.Hex, 2), "늦게 와도 등록되지 않음");
        }
    }
}
