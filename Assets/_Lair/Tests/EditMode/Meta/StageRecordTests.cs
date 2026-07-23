using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 스테이지별 전적 — 조회 폴백 / 집계 / 최단 갱신 / 직렬화 (spec §4).
    public class StageRecordTests
    {
        [Test]
        public void GetStageRecord_기록이_없는_스테이지는_0판_0승_최단없음을_돌려준다()
        {
            MetaProfile p = new MetaProfile();

            StageRecordEntry r = p.GetStageRecord(3);

            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.Runs);
            Assert.AreEqual(0, r.Wins);
            Assert.AreEqual(-1f, r.BestClearTime);
        }

        [Test]
        public void GetStageRecord_조회만으로는_엔트리가_생기지_않는다()
        {
            MetaProfile p = new MetaProfile();

            p.GetStageRecord(3);

            Assert.AreEqual(0, p.StageRecords.Count);
        }

        [Test]
        public void RecordStageRun_패배는_판수만_올리고_승수와_최단은_그대로다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(2, win: false, clearTime: 180f);

            StageRecordEntry r = p.GetStageRecord(2);
            Assert.AreEqual(1, r.Runs);
            Assert.AreEqual(0, r.Wins);
            Assert.AreEqual(-1f, r.BestClearTime);
        }

        [Test]
        public void RecordStageRun_승리는_판수와_승수를_올리고_최단을_기록한다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(2, win: true, clearTime: 180f);

            StageRecordEntry r = p.GetStageRecord(2);
            Assert.AreEqual(1, r.Runs);
            Assert.AreEqual(1, r.Wins);
            Assert.AreEqual(180f, r.BestClearTime);
        }

        [Test]
        public void RecordStageRun_최단은_더_빠를_때만_갱신된다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(1, win: true, clearTime: 150f);

            p.RecordStageRun(1, win: true, clearTime: 200f);

            Assert.AreEqual(150f, p.GetStageRecord(1).BestClearTime);

            p.RecordStageRun(1, win: true, clearTime: 120f);

            Assert.AreEqual(120f, p.GetStageRecord(1).BestClearTime);
        }

        [Test]
        public void RecordStageRun_같은_스테이지_반복은_엔트리를_늘리지_않는다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(4, win: false, clearTime: 300f);
            p.RecordStageRun(4, win: true, clearTime: 240f);

            Assert.AreEqual(1, p.StageRecords.Count);
            Assert.AreEqual(2, p.GetStageRecord(4).Runs);
        }

        [Test]
        public void RecordStageRun_스테이지가_다르면_엔트리가_각각_추가된다()
        {
            MetaProfile p = new MetaProfile();

            p.RecordStageRun(1, win: true, clearTime: 100f);
            p.RecordStageRun(5, win: false, clearTime: 300f);

            Assert.AreEqual(2, p.StageRecords.Count);
            Assert.AreEqual(1, p.GetStageRecord(1).Wins);
            Assert.AreEqual(0, p.GetStageRecord(5).Wins);
        }

        [Test]
        public void 구버전_세이브에_StageRecords_필드가_없어도_빈_리스트로_로드된다()
        {
            //# Version 2 시절 JSON — StageRecords 키 자체가 없다.
            string legacyJson = "{\"Version\":2,\"Souls\":500,\"TotalRuns\":40,\"TotalWins\":25}";

            MetaProfile p = JsonUtility.FromJson<MetaProfile>(legacyJson);

            Assert.IsNotNull(p.StageRecords);
            Assert.AreEqual(0, p.StageRecords.Count);
            Assert.AreEqual(25, p.TotalWins);
            Assert.AreEqual(0, p.GetStageRecord(1).Wins);
        }

        [Test]
        public void StageRecords_는_JSON_왕복으로_보존된다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(3, win: true, clearTime: 199.5f);

            MetaProfile round = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));

            Assert.AreEqual(1, round.GetStageRecord(3).Wins);
            Assert.AreEqual(199.5f, round.GetStageRecord(3).BestClearTime, 0.001f);
        }

        [Test]
        public void CopyFrom_은_스테이지_전적을_복원한다()
        {
            MetaProfile cloud = new MetaProfile();
            cloud.RecordStageRun(2, win: true, clearTime: 210f);
            MetaProfile local = new MetaProfile();

            local.CopyFrom(cloud);

            Assert.AreEqual(1, local.GetStageRecord(2).Wins);
            Assert.AreEqual(210f, local.GetStageRecord(2).BestClearTime);
        }

        [Test]
        public void 신규_프로필의_스키마_버전은_3이다()
        {
            Assert.AreEqual(3, new MetaProfile().Version);
        }

        [Test]
        public void 정산_계약_승리는_총계와_선택스테이지_전적이_함께_증가한다()
        {
            MetaProfile p = new MetaProfile();
            p.SelectedStage = 3;

            //# BattleController 정산 블록과 같은 순서 — 총계 가산 후 스테이지 집계.
            p.TotalRuns++;
            p.TotalWins++;
            p.RecordStageRun(p.SelectedStage, win: true, clearTime: 175f);

            Assert.AreEqual(1, p.TotalRuns);
            Assert.AreEqual(1, p.TotalWins);
            Assert.AreEqual(1, p.GetStageRecord(3).Runs);
            Assert.AreEqual(1, p.GetStageRecord(3).Wins);
            Assert.AreEqual(0, p.GetStageRecord(2).Runs);
        }

        [Test]
        public void 정산_계약_패배는_선택스테이지_판수만_증가한다()
        {
            MetaProfile p = new MetaProfile();
            p.SelectedStage = 5;

            p.TotalRuns++;
            p.RecordStageRun(p.SelectedStage, win: false, clearTime: 300f);

            Assert.AreEqual(0, p.TotalWins);
            Assert.AreEqual(1, p.GetStageRecord(5).Runs);
            Assert.AreEqual(0, p.GetStageRecord(5).Wins);
        }
    }
}
