using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Character
{
    //# 티어 시트 로더(monster-tier-sheets §7) — 시드: gameplay-programmer. 정상(병렬 로드→조회→해제) + 엣지(실패 1회 재시도 후 폴백·경고 1줄).
    public class MonsterTierSheetLoaderTests
    {
        [Test]
        public async Task 필요한_티어만_로드하고_조회_후_해제한다()
        {
            MonsterTierSheetSet set = ScriptableObject.CreateInstance<MonsterTierSheetSet>();
            List<EMonsterTierSheet> loaded = new List<EMonsterTierSheet>();
            List<EMonsterTierSheet> unloaded = new List<EMonsterTierSheet>();
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => { loaded.Add(key); return Task.FromResult(set); },
                key => unloaded.Add(key),
                msg => Assert.Fail(msg));

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Wisp, 2) });

            Assert.AreEqual(new[] { EMonsterTierSheet.WispT2 }, loaded.ToArray(), "필요한 키 하나만 로드");
            Assert.AreSame(set, loader.Find(EMonster.Wisp, 2));
            Assert.IsNull(loader.Find(EMonster.Wisp, 1), "요청하지 않은 티어는 없음");
            loader.Release();
            Assert.AreEqual(new[] { EMonsterTierSheet.WispT2 }, unloaded.ToArray());
            Assert.IsNull(loader.Find(EMonster.Wisp, 2), "해제 후 조회 불가");
            Object.DestroyImmediate(set);
        }

        [Test]
        public async Task 로드가_두번_실패하면_폴백하고_경고는_한줄이다()
        {
            int calls = 0;
            List<string> warnings = new List<string>();
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(
                key => { calls++; return Task.FromResult<MonsterTierSheetSet>(null); },
                key => { },
                warnings.Add);

            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Hex, 3) });

            Assert.AreEqual(2, calls, "1회 재시도");
            Assert.IsNull(loader.Find(EMonster.Hex, 3), "풋내기 폴백 = 세트 없음");
            Assert.AreEqual(1, warnings.Count, "경고 1줄");
        }
    }
}
