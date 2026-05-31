using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 Phase 1 Task 1 — EBuildAxis Enum 정합 회귀.
    //# 값 순서·이름이 변경되면 CardData._axis (int 직렬화) · BattleController·UI 와 desync.
    public class EBuildAxisMigrationTests
    {
        [Test]
        public void EBuildAxis_네_값_순서_고정()
        {
            Assert.AreEqual(0, (int)EBuildAxis.Tank);
            Assert.AreEqual(1, (int)EBuildAxis.Dps);
            Assert.AreEqual(2, (int)EBuildAxis.Debuff);
            Assert.AreEqual(3, (int)EBuildAxis.Swarm);
        }

        [Test]
        public void EBuildAxis_값_정확히_네개()
        {
            Assert.AreEqual(4, System.Enum.GetValues(typeof(EBuildAxis)).Length);
        }

        //# 카드 리뉴얼 Phase 2 Task 6 — ECardId 신규 3장이 정확히 25/26/27 위치에 추가.
        //# 기존 0~24 자리 보존 (Multiply=20, Berserk=24 enum 값 유지 — SO/풀 ref 만 폐기/교체).
        [Test]
        public void ECardId_기존_25개_순서_보존()
        {
            Assert.AreEqual(0, (int)ECardId.WispHpBoost);
            Assert.AreEqual(20, (int)ECardId.Multiply);
            Assert.AreEqual(24, (int)ECardId.Berserk);
        }

        //# 카드 리뉴얼 v0.6 Phase 2 BLOCKER W1 후속 — SwarmRush 는 enum 자리 신설 대신 Multiply (값 20) 자리를 그대로 재사용.
        //# Berserk → GuardianRage 와 동일 패턴: enum 값명·SO 파일명 보존, 효과/displayName 만 리뉴얼.
        [Test]
        public void ECardId_신규_3장_25_26_27_위치()
        {
            Assert.AreEqual(25, (int)ECardId.WallOfWisps);
            Assert.AreEqual(26, (int)ECardId.MarkOfDeath);
            Assert.AreEqual(27, (int)ECardId.SpawnerHaste);
        }

        //# 카드 리뉴얼 Phase 2 — 총 28 ECardId (기존 25 + 신규 3).
        //# Multiply enum 자리는 보존, SO 파일 Multiply.asset 도 SwarmRush 효과로 재사용 (BLOCKER W1).
        [Test]
        public void ECardId_총_28개_기존25_신규3()
        {
            Assert.AreEqual(28, System.Enum.GetValues(typeof(ECardId)).Length);
        }
    }
}
