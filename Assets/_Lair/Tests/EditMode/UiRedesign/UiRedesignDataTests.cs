using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# UI 도트 던전 리디자인 1단계(데이터/로직) 자체 테스트 — 항목별 정상 1 + 엣지 1. 엣지 망라·회귀는 test-engineer.
    public class UiRedesignDataTests
    {
        //# ---- 카드 픽 횟수 (제안 7) ----

        [Test]
        public void 같은_카드를_여러번_픽하면_횟수가_누적된다()
        {
            MetaProfile p = new MetaProfile();
            p.AddCardPick("TimeStop");
            p.AddCardPick("TimeStop", 2);
            Assert.AreEqual(3, p.GetCardPickCount("TimeStop"));
            Assert.AreEqual(0, p.GetCardPickCount("Fear"));
        }

        [Test]
        public void 픽_횟수_동률이면_카드ID_사전순_첫번째가_대표다()
        {
            MetaProfile p = new MetaProfile();
            p.AddCardPick("Zeta", 2);
            p.AddCardPick("Alpha", 2);
            Assert.AreEqual("Alpha", p.GetMostPickedCard().CardId);
            Assert.IsNull(new MetaProfile().GetMostPickedCard());
        }

        [Test]
        public void 카드_픽_횟수는_CopyFrom과_JSON_왕복에서_보존된다()
        {
            MetaProfile src = new MetaProfile();
            src.AddCardPick("Fear", 4);
            MetaProfile dst = new MetaProfile();
            dst.CopyFrom(JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(src)));
            Assert.AreEqual(4, dst.GetCardPickCount("Fear"));
        }

        [Test]
        public void 구버전_세이브는_카드_픽_횟수가_빈_리스트로_로드된다()
        {
            MetaProfile p = JsonUtility.FromJson<MetaProfile>("{\"Version\":3,\"Souls\":10}");
            Assert.IsNotNull(p.CardPickCounts);
            Assert.AreEqual(0, p.CardPickCounts.Count);
        }

        //# ---- 결과 팝업 (제안 4) ----

        [Test]
        public void 이전_기록이_없거나_더_빠르면_신기록이고_패배나_느린_기록은_아니다()
        {
            MetaProfile p = new MetaProfile();
            Assert.IsTrue(p.IsNewStageBest(1, true, 200f));
            Assert.IsFalse(p.IsNewStageBest(1, false, 100f));
            p.RecordStageRun(1, true, 200f);
            Assert.IsTrue(p.IsNewStageBest(1, true, 199f));
            Assert.IsFalse(p.IsNewStageBest(1, true, 200f));
        }

        [Test]
        public void 결과_행은_승리면_클리어시간과_NEW_패배면_영웅HP를_보인다()
        {
            ResultRowsData win = ResultPopup.BuildRows(new ResultPopupArg { Result = BattleResult.Win, HasMeta = true, ClearTime = 222.15f, IsNewBest = true, SoulsGained = 180, XpGained = 60 });
            Assert.IsTrue(win.ShowClearRow);
            Assert.AreEqual("3:42.1", win.ClearTimeText);
            Assert.IsTrue(win.ShowNewBadge);
            Assert.IsFalse(win.ShowHeroHpRow);

            ResultRowsData lose = ResultPopup.BuildRows(new ResultPopupArg { Result = BattleResult.Lose, HeroHpRatio = 0.18f, IsNewBest = true });
            Assert.IsTrue(lose.ShowHeroHpRow);
            Assert.AreEqual("18%", lose.HeroHpText);
            Assert.IsFalse(lose.ShowNewBadge);
            Assert.IsFalse(lose.ShowSoulsRow);
        }

        //# ---- 토스트 (제안 6) / 카드 부제 (제안 9) / 타이머 / 빌드 바 ----

        [Test]
        public void 토스트_종류별_점_색이_다르다()
        {
            Assert.AreEqual(UiDotPalette.Soul, UiDotPalette.ToastDot(EToastKind.Info));
            Assert.AreEqual(UiDotPalette.Gold, UiDotPalette.ToastDot(EToastKind.Warning));
            Assert.AreEqual(UiDotPalette.Blood, UiDotPalette.ToastDot(EToastKind.Error));
        }

        [Test]
        public void 카드_부제는_패시브면_HP구간_액티브면_주기를_보인다()
        {
            Assert.AreEqual("패시브 · 영웅 HP 60% 도달", CardSelectionPopup.BuildSubtitle(true, 60, 60));
            Assert.AreEqual("액티브 · 60초 주기", CardSelectionPopup.BuildSubtitle(false, 0, 60));
        }

        [Test]
        public void 트리거_임계점에서_HP퍼센트와_액티브_주기를_구한다()
        {
            Assert.AreEqual(60, PassiveTriggerService.ResolveHpPercent(new[] { 0.9f, 0.8f, 0.7f, 0.6f }, 3));
            Assert.AreEqual(90, PassiveTriggerService.ResolveHpPercent(null, 0));
            Assert.AreEqual(0, PassiveTriggerService.ResolveHpPercent(null, 99));
            Assert.AreEqual(60, ActiveTriggerService.ResolvePeriodSeconds(null));
            Assert.AreEqual(30, ActiveTriggerService.ResolvePeriodSeconds(new[] { 30f }));
        }

        [Test]
        public void 타이머는_남은_30초_이하에서_경고이고_총시간_0이면_경고하지_않는다()
        {
            Assert.IsFalse(BattleViewModel.IsTimerWarning(269f, 300f));
            Assert.IsTrue(BattleViewModel.IsTimerWarning(270f, 300f));
            Assert.IsFalse(BattleViewModel.IsTimerWarning(0f, 0f));
        }

        [Test]
        public void 빌드_바는_최소_슬롯까지_빈칸으로_채우고_넘으면_늘리지_않는다()
        {
            List<BattleViewModel.BuildEntry> two = new List<BattleViewModel.BuildEntry>
            {
                new BattleViewModel.BuildEntry(), new BattleViewModel.BuildEntry(),
            };
            List<BattleViewModel.BuildEntry> padded = BuildPanel.PadWithEmptySlots(two, BuildPanel.MinPassiveSlots, true);
            Assert.AreEqual(9, padded.Count);
            Assert.IsNull(padded[8].Card);
            Assert.AreEqual(0, BuildPanel.EmptySlotCount(10, BuildPanel.MinPassiveSlots));
            Assert.AreEqual(5, BuildPanel.MinActiveSlots);
        }

        //# ---- 시너지 다음 단계 (제안 5) ----

        [Test]
        public void 시너지_헤더는_단계_배지와_다음_단계_미리보기를_가진다()
        {
            List<SynergyModalCellData> rows = Lair.Tests.UI.SynergyModalTestFakes.BuildRows(a => a == EBuildAxis.Tank ? 5 : 0);
            Assert.AreEqual("2/3", rows[0].TierBadgeText);
            Assert.IsFalse(rows[0].IsMaxTier);
            Assert.IsTrue(rows[0].NextText.StartsWith("다음: "));

            List<SynergyModalCellData> max = Lair.Tests.UI.SynergyModalTestFakes.BuildRows(a => a == EBuildAxis.Tank ? 7 : 0);
            Assert.AreEqual("3/3", max[0].TierBadgeText);
            Assert.IsTrue(max[0].IsMaxTier);
            Assert.AreEqual("", max[0].NextText);
        }

        //# ---- 마을 전적 패널 (제안 1) / 기록 타일 / 계정 비교 ----

        [Test]
        public void 마을_전적_패널은_선택_스테이지의_전적을_보이고_기록이_없으면_대시다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 4 };
            p.RecordStageRun(5, true, 222.15f);
            p.RecordStageRun(5, false, 0f);
            VillageViewModel vm = new VillageViewModel(p, null);
            Assert.AreEqual("기사 · 5단계", vm.IntruderText);
            Assert.AreEqual("3:42.1", vm.BestDefenseText);
            Assert.AreEqual("1승 · 2판", vm.StageRecordText);

            vm.MoveStage(-1);
            Assert.AreEqual("-", vm.BestDefenseText);
            Assert.AreEqual("0승 · 0판", vm.StageRecordText);
        }

        [Test]
        public void 기록_통계_타일은_총계와_가장_많이_픽한_카드를_보이고_없으면_대시다()
        {
            MetaProfile p = new MetaProfile { TotalRuns = 42, TotalWins = 27, BestClearTime = 178f };
            RecordsStatTilesData none = RecordsPopup.BuildStatTiles(p, null);
            Assert.AreEqual("42", none.RunsText);
            Assert.AreEqual("64%", none.RateText);
            Assert.AreEqual("2:58", none.BestText);
            Assert.AreEqual("-", none.TopCardText);

            p.AddCardPick("TimeStop", 31);
            Assert.AreEqual("TimeStop ×31", RecordsPopup.BuildTopCardText(p, null));
        }

        [Test]
        public void 세이브_요약은_영주_레벨과_소울을_표기하고_프로필이_없으면_null이다()
        {
            SaveSummary s = new SaveSummary { LordLevel = 7, Souls = 1240 };
            StringAssert.Contains("영주 Lv 7", s.ToDisplayText());
            Assert.IsNull(SaveSummary.From(null, null));
        }

        [Test]
        public void 영웅_선택_셀은_해금이면_N단계_잠금이면_잠김_보조줄을_가진다()
        {
            List<HeroSelectCellData> list = HeroSelectPopup.BuildCellData(new MetaProfile { ClearedStage = 1 }, null);
            Assert.AreEqual("1단계", list[0].SubText);
            Assert.AreEqual("2단계", list[1].SubText);
            Assert.AreEqual("잠김", list[2].SubText);
        }

        [Test]
        public void 도감_수집_문구는_더미를_제외하고_해금만_센다()
        {
            List<CodexCellData> cells = new List<CodexCellData>
            {
                new CodexCellData { Unlocked = true },
                new CodexCellData { Unlocked = false },
                new CodexCellData { IsLockedDummy = true },
            };
            Assert.AreEqual("수집 1 / 2", CodexPopup.BuildCollectedText(cells));
            Assert.AreEqual("수집 0 / 0", CodexPopup.BuildCollectedText(null));
        }

        [Test]
        public void 영주_다음_레벨_문구는_설정이_없으면_최대_레벨이다()
        {
            Assert.AreEqual("최대 레벨", LordLevelPopup.BuildXpNextText(100, null));
            Assert.AreEqual(0, LordLevelService.XpForLevel(1, null));
        }

        //# ---- 스테이지 랭킹 (제안 2) ----

        [Test]
        public void 스테이지_랭킹_문서ID와_갱신_규칙을_따른다()
        {
            Assert.AreEqual("3_abc", StageLeaderboard.DocId(3, "abc"));
            Assert.IsTrue(StageLeaderboard.ShouldReplace(0, 100000));
            Assert.IsTrue(StageLeaderboard.ShouldReplace(120000, 100000));
            Assert.IsFalse(StageLeaderboard.ShouldReplace(100000, 100000));
            Assert.IsFalse(StageLeaderboard.IsValidStage(6));
        }

        [Test]
        public void 랭킹_클라이언트는_스테이지_제출과_조회를_스테이지와_함께_전달한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient();
            fake.StageTopToReturn.Add(new RankingRowDto { rank = 1, clearTimeMs = 60000 });
            RankingClient client = new RankingClient(fake);

            Assert.IsTrue(client.SubmitStageAsync(2, 90000, "Knight", "A").GetAwaiter().GetResult());
            Assert.AreEqual(2, fake.LastSubmittedStage);
            Assert.AreEqual(90000, fake.LastSubmittedStageMs);
            Assert.AreEqual(1, client.GetStageTopAsync(4, 100).GetAwaiter().GetResult().Count);
            Assert.AreEqual(4, fake.LastTopStage);
        }
    }
}
