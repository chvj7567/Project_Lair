using System.Collections.Generic;
using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 Phase 2 Task 12 — CardPool SO 28장 4축 균등 분배 회귀 보장.
    //# Passive 16 = 4×4, Active 12 = 4×3. 기획서 §3.5 라인업 통계 표.
    public class CardPoolDistributionTests
    {
        private const string PassivePath = "Assets/_Lair/Art/Cards/CardPool_Passive.asset";
        private const string ActivePath  = "Assets/_Lair/Art/Cards/CardPool_Active.asset";

        [Test]
        public void CardPool_Passive_16장_4축_각4()
        {
            CardPool pool = AssetDatabase.LoadAssetAtPath<CardPool>(PassivePath);
            Assert.IsNotNull(pool, $"CardPool_Passive 로드 실패: {PassivePath}");
            Assert.AreEqual(16, pool.Cards.Count, "패시브 풀 16장 (4축 × 4)");

            Dictionary<EBuildAxis, int> dist = CountByAxis(pool);
            Assert.AreEqual(4, dist[EBuildAxis.Tank],   "Tank 패시브 4장");
            Assert.AreEqual(4, dist[EBuildAxis.Dps],    "Dps 패시브 4장");
            Assert.AreEqual(4, dist[EBuildAxis.Debuff], "Debuff 패시브 4장");
            Assert.AreEqual(4, dist[EBuildAxis.Swarm],  "Swarm 패시브 4장");
        }

        [Test]
        public void CardPool_Active_12장_4축_각3()
        {
            CardPool pool = AssetDatabase.LoadAssetAtPath<CardPool>(ActivePath);
            Assert.IsNotNull(pool, $"CardPool_Active 로드 실패: {ActivePath}");
            Assert.AreEqual(12, pool.Cards.Count, "액티브 풀 12장 (4축 × 3)");

            Dictionary<EBuildAxis, int> dist = CountByAxis(pool);
            Assert.AreEqual(3, dist[EBuildAxis.Tank],   "Tank 액티브 3장");
            Assert.AreEqual(3, dist[EBuildAxis.Dps],    "Dps 액티브 3장");
            Assert.AreEqual(3, dist[EBuildAxis.Debuff], "Debuff 액티브 3장");
            Assert.AreEqual(3, dist[EBuildAxis.Swarm],  "Swarm 액티브 3장");
        }

        //# 카드 리뉴얼 v0.6 BLOCKER W1 — Multiply enum 자리·SO 파일은 보존되고 SwarmRush 효과 (팬텀 6마리 즉시 소환) 로 재사용.
        //# 따라서 Active 풀에는 ECardId.Multiply 가 정확히 1장 포함 (Swarm 축).
        [Test]
        public void CardPool_Active_Multiply_정확히_1장_Swarm축()
        {
            CardPool pool = AssetDatabase.LoadAssetAtPath<CardPool>(ActivePath);
            int multiplyCount = 0;
            foreach (CardData c in pool.Cards)
            {
                if (c.Id == ECardId.Multiply)
                {
                    multiplyCount++;
                    Assert.AreEqual(EBuildAxis.Swarm, c.Axis, "Multiply (SwarmRush 효과) 는 Swarm 축");
                }
            }
            Assert.AreEqual(1, multiplyCount, "Multiply 는 Active 풀에 1장 포함 (SwarmRush 효과로 재사용)");
        }

        private static Dictionary<EBuildAxis, int> CountByAxis(CardPool pool)
        {
            Dictionary<EBuildAxis, int> dist = new Dictionary<EBuildAxis, int>
            {
                { EBuildAxis.Tank, 0 }, { EBuildAxis.Dps, 0 },
                { EBuildAxis.Debuff, 0 }, { EBuildAxis.Swarm, 0 },
            };
            foreach (CardData c in pool.Cards)
            {
                if (c == null) continue;
                dist[c.Axis]++;
            }
            return dist;
        }
    }
}
