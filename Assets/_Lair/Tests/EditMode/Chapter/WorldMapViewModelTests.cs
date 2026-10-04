using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 월드맵 ViewModel(순수 로직) 자체 스모크 — shipped Chapter JSON + 실제 Strings_Ko 로 노드 상태·탭·상세·확정·출격 불가 게이트를 확인한다.
    //# 본격 스위트(엣지·회귀)는 test-engineer.
    public class WorldMapViewModelTests : ChapterStaticStateTestBase
    {
        private const string JsonDir = "Assets/_Lair/Data/Json/";

        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + name + ".json"));
        }

        private static ChapterDatabase Db()
        {
            return ChapterDatabase.Build(
                Load<ChapterRow>("Chapter"), Load<StageRow>("Stage"), Load<Stage_RuleRow>("Stage_Rule"),
                Load<Stage_Rule_ParamRow>("Stage_Rule_Param"), Load<Stage_Rule_TriggerRow>("Stage_Rule_Trigger"),
                Load<HeroArchetypeRow>("HeroArchetype"), Load<HeroArchetype_KiteRow>("HeroArchetype_Kite"));
        }

        //# 실제 Strings_Ko — id 누락이 있으면 빈 문자열이 되어 단언이 실패한다.
        private static string Text(int id)
        {
            Dictionary<int, string> table = new Dictionary<int, string>();
            foreach (StringEntry entry in JsonArrayUtility.FromJsonArray<StringEntry>(File.ReadAllText(JsonDir + "Strings_Ko.json")))
            {
                table[entry.id] = entry.text;
            }
            return table.TryGetValue(id, out string text) ? text : string.Empty;
        }

        private static WorldMapViewModel NewVm(MetaProfile profile, System.Action<int> onConfirm = null)
        {
            return new WorldMapViewModel(Db(), profile, Text, onConfirm);
        }

        [Test]
        public void 노드_상태는_클리어_해금_잠금으로_나뉘고_선택_스테이지는_표시된다()
        {
            WorldMapViewModel vm = NewVm(new MetaProfile { ClearedStage = 3, SelectedStage = 2 });

            Assert.AreEqual(1, vm.SelectedChapter);
            Assert.AreEqual(2, vm.SelectedStageIndex);
            Assert.AreEqual(EWorldMapNodeState.Cleared, vm.Nodes[0].State);
            Assert.AreEqual(EWorldMapNodeState.Cleared, vm.Nodes[2].State);
            Assert.AreEqual(EWorldMapNodeState.Open, vm.Nodes[3].State);
            Assert.AreEqual(EWorldMapNodeState.Locked, vm.Nodes[4].State);
            Assert.IsTrue(vm.Nodes[4].IsBoss);
            Assert.IsTrue(vm.Nodes[1].IsSelected);
            Assert.AreEqual("1-2", vm.Nodes[1].LabelText);
            Assert.AreEqual("잠금", vm.Nodes[4].LabelText);
            Assert.AreEqual("???", vm.Nodes[4].NameText);
        }

        [Test]
        public void 챕터_탭은_5칸이고_2장은_1장_보스를_깨야_열린다()
        {
            WorldMapViewModel locked = NewVm(new MetaProfile { ClearedStage = 4 });
            WorldMapViewModel opened = NewVm(new MetaProfile { ClearedStage = 5 });

            Assert.AreEqual(5, opened.Tabs.Count);
            Assert.AreEqual("1장 지하 묘지", opened.Tabs[0].Label);
            Assert.IsFalse(opened.Tabs[0].IsLocked);
            Assert.AreEqual("2장 어둠의 숲", opened.Tabs[1].Label);
            Assert.IsTrue(locked.Tabs[1].IsLocked);
            Assert.AreEqual("잠금", locked.Tabs[1].LockTagText);
            Assert.IsFalse(opened.Tabs[1].IsLocked, "ChapterGate 가 2장까지 열려 있다");
            Assert.AreEqual(string.Empty, opened.Tabs[1].LockTagText);
            Assert.IsTrue(opened.Tabs[2].IsPlaceholder);
            Assert.AreEqual("3장 ???", opened.Tabs[2].Label);
        }

        [Test]
        public void 잠긴_챕터_탭을_누르면_이동하지_않고_열리는_조건을_안내하고_열리면_이동한다()
        {
            WorldMapViewModel vm = NewVm(new MetaProfile { ClearedStage = 4 });
            string notice = null;
            vm.OnNotice += message => notice = message;

            Assert.IsFalse(vm.SelectChapter(2));
            Assert.AreEqual(1, vm.SelectedChapter);
            Assert.AreEqual("2장은 1장 보스를 클리어하면 열립니다", notice);

            WorldMapViewModel opened = NewVm(new MetaProfile { ClearedStage = 5 });
            Assert.IsTrue(opened.SelectChapter(2));
            Assert.AreEqual(2, opened.SelectedChapter);
            Assert.AreEqual("2-1", opened.DetailNumberText);
        }

        [Test]
        public void 잠긴_노드를_누르면_선택되지_않고_선행_조건_안내가_나온다()
        {
            WorldMapViewModel vm = NewVm(new MetaProfile { ClearedStage = 3, SelectedStage = 2 });
            string notice = null;
            vm.OnNotice += message => notice = message;

            Assert.IsFalse(vm.SelectNode(5));

            Assert.AreEqual(2, vm.SelectedStageIndex);
            Assert.AreEqual("스테이지 1-4 클리어 필요", notice);
        }

        [Test]
        public void 선택_확정은_선형_번호를_넘기고_잠금_스테이지는_확정되지_않는다()
        {
            int confirmed = -1;
            bool closed = false;
            WorldMapViewModel vm = NewVm(new MetaProfile { ClearedStage = 3, SelectedStage = 1 }, linear => confirmed = linear);
            vm.OnConfirmed += () => closed = true;

            Assert.IsTrue(vm.SelectNode(4));
            Assert.IsTrue(vm.Confirm());
            Assert.AreEqual(4, confirmed);
            Assert.IsTrue(closed);
            Assert.IsTrue(vm.SelectEnabled);

            Assert.IsFalse(vm.SelectNode(5), "5 는 잠금");
            Assert.AreEqual(4, vm.SelectedStageIndex);
        }

        [Test]
        public void 저장된_선택이_잠겨_있으면_최대_플레이_가능_스테이지로_보정한다()
        {
            WorldMapViewModel vm = NewVm(new MetaProfile { ClearedStage = 1, SelectedStage = 9 });

            Assert.AreEqual(1, vm.SelectedChapter);
            Assert.AreEqual(2, vm.SelectedStageIndex, "cleared 1 → MaxPlayable 2");
        }

        [Test]
        public void 규칙_스테이지는_규칙_목록을_보스_스테이지는_보스_페이즈를_보여_준다()
        {
            WorldMapViewModel vm = NewVm(new MetaProfile { ClearedStage = 10, SelectedStage = 2 });

            StringAssert.Contains("성수 샘", vm.DetailRulesText);
            StringAssert.Contains("0.4%", vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);

            vm.SelectNode(5);
            Assert.IsTrue(vm.DetailIsBoss);
            Assert.IsTrue(vm.HasBossPhases);
            StringAssert.Contains("수호 결계", vm.DetailBossPhaseText);
            StringAssert.Contains("55%", vm.DetailBossPhaseText);
            Assert.AreEqual("특수 규칙 없음", vm.DetailRulesText, "보스 스테이지의 상시 규칙은 없다");

            vm.SelectNode(1);
            Assert.AreEqual("특수 규칙 없음", vm.DetailRulesText);
        }

        [Test]
        public void 봉인된_둥지_규칙문구는_종족명과_해제_HP를_담는다()
        {
            WorldMapViewModel vm = NewVm(new MetaProfile { ClearedStage = 10, SelectedStage = 4 });

            StringAssert.Contains("망령", vm.DetailRulesText);
            StringAssert.Contains("60%", vm.DetailRulesText);
        }

        [Test]
        public void 기록이_있으면_최단과_승패를_없으면_기록_없음을_보인다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 3, SelectedStage = 1 };
            p.RecordStageRun(2, true, 222.15f);
            p.RecordStageRun(2, false, 0f);
            WorldMapViewModel vm = NewVm(p);

            Assert.AreEqual("기록 없음", vm.DetailRecordText);
            vm.SelectNode(2);
            Assert.AreEqual("최단 3:42.1 / 1승 2판", vm.DetailRecordText);
        }

        [Test]
        public void 규칙_문구_헬퍼는_봉인_종족의_해제_HP_퍼센트를_돌려준다()
        {
            ChapterDatabase db = Db();
            StageRuleData nest = db.GetRules(1, 4)[0];

            Assert.AreEqual(60, StageRuleText.SealUnlockHpPercent(nest, EMonster.Wraith));
            Assert.AreEqual(0, StageRuleText.SealUnlockHpPercent(nest, EMonster.Wisp), "다른 종은 0");
            Assert.AreEqual(0, StageRuleText.SealUnlockHpPercent(db.GetRules(1, 2)[0], EMonster.Wraith), "봉인 규칙이 아니면 0");
        }

        [Test]
        public void 마을_캐러셀은_출격_가능_스테이지까지만_이동하고_표기는_챕터_인덱스다()
        {
            VillageViewModel vm = new VillageViewModel(new MetaProfile { ClearedStage = 10 }, null, Db(), Text);

            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.SelectedStage, "cleared 10 이어도 출격 가능 상한으로 클램프");
            Assert.AreEqual("STAGE 2-5", vm.StageIndicatorText);
            Assert.AreEqual("★★★★★", vm.StageThreatText);
            Assert.IsFalse(vm.CanMoveNext);

            vm.SelectStage(99);
            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.SelectedStage);
            vm.SelectStage(3);
            Assert.AreEqual("STAGE 1-3", vm.StageIndicatorText);
            Assert.AreEqual("기사 · 3단계", vm.IntruderText);
            vm.SelectStage(7);
            Assert.AreEqual("STAGE 2-2", vm.StageIndicatorText);
            Assert.AreEqual("궁수 · 2단계", vm.IntruderText);
        }

        [Test]
        public void 마을_캐러셀_잠금_안내는_챕터_인덱스_문구다()
        {
            VillageViewModel vm = new VillageViewModel(new MetaProfile { ClearedStage = 0 }, null, Db(), Text);

            vm.SelectStage(3);

            Assert.AreEqual("스테이지 1-2 클리어 필요", vm.StageLockHintText);
        }

        [Test]
        public void ChapterGate는_출격_가능_챕터_수로_챕터와_스테이지를_판정한다()
        {
            Assert.IsTrue(ChapterGate.IsChapterPlayable(1));
            Assert.IsFalse(ChapterGate.IsChapterPlayable(ChapterGate.PlayableChapterCount + 1));
            Assert.IsTrue(ChapterGate.IsStagePlayable(ChapterGate.PlayableStageCount));
            Assert.IsFalse(ChapterGate.IsStagePlayable(ChapterGate.PlayableStageCount + 1));
            Assert.IsFalse(ChapterGate.IsStagePlayable(0));
        }
    }
}
