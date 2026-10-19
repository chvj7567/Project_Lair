using System;
using System.Collections.Generic;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 난이도 선택 — 월드맵 ViewModel(세그먼트·토스트·클램프·확정) · 결과창 행 · 영웅 제목 · 프리팹 참조 (difficulty-select §5).
    public class DifficultyWorldMapAndResultTests : DifficultyStaticStateTestBase
    {
        private const string ResultPrefabPath = "Assets/_Lair/Art/UI/ResultPopup.prefab";
        private const string WorldMapPrefabPath = "Assets/_Lair/Art/UI/WorldMapPopup.prefab";
        private const string RecordsCellPrefabPath = "Assets/_Lair/Art/UI/RecordsStageCell.prefab";

        private static WorldMapViewModel NewVm(MetaProfile profile, Action<int> onConfirm = null)
        {
            return new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), profile, WorldMapTestSupport.Text, onConfirm, DifficultyTestSupport.Table());
        }

        private static MetaProfile Profile(int cleared, int selected, int selectedDifficulty = 0, params (int stage, int max)[] rows)
        {
            MetaProfile p = new MetaProfile { ClearedStage = cleared, SelectedStage = selected, SelectedDifficulty = selectedDifficulty };
            foreach ((int stage, int max) row in rows)
            {
                p.DifficultyClears.Add(new DifficultyClearEntry { Stage = row.stage, MaxDifficulty = row.max });
            }
            return p;
        }

        private static EDifficultySegmentState[] States(WorldMapViewModel vm)
        {
            EDifficultySegmentState[] states = new EDifficultySegmentState[vm.DifficultySegments.Count];
            for (int i = 0; i < states.Length; ++i)
            {
                states[i] = vm.DifficultySegments[i].State;
            }
            return states;
        }

        //# --- 세그먼트 상태 ---

        [Test]
        public void 세그먼트는_3칸이고_라벨은_Easy_Normal_Hard다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2));

            Assert.AreEqual(3, vm.DifficultySegments.Count);
            Assert.AreEqual("Easy", vm.DifficultySegments[0].Label);
            Assert.AreEqual("Normal", vm.DifficultySegments[1].Label);
            Assert.AreEqual("Hard", vm.DifficultySegments[2].Label);
            Assert.IsTrue(vm.DifficultyEnabled);
        }

        [Test]
        public void 클리어한_스테이지는_행이_없으면_Normal까지_열리고_Hard는_잠긴다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2));

            CollectionAssert.AreEqual(
                new[] { EDifficultySegmentState.Selected, EDifficultySegmentState.Available, EDifficultySegmentState.Locked }, States(vm));
            Assert.AreEqual(0, vm.DisplayDifficulty);
        }

        [Test]
        public void Normal을_클리어한_스테이지는_Hard까지_열린다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2, 0, (2, 1)));

            CollectionAssert.AreEqual(
                new[] { EDifficultySegmentState.Selected, EDifficultySegmentState.Available, EDifficultySegmentState.Available }, States(vm));
        }

        [Test]
        public void 미클리어_스테이지는_Easy만_열리고_Normal_Hard는_잠긴다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 4));

            CollectionAssert.AreEqual(
                new[] { EDifficultySegmentState.Selected, EDifficultySegmentState.Locked, EDifficultySegmentState.Locked }, States(vm));
            Assert.IsTrue(vm.DifficultyEnabled, "선택 가능한 스테이지라 박스는 활성");
        }

        [Test]
        public void 저장된_선택_난이도가_초기_표시_난이도가_된다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2, 1));

            Assert.AreEqual(1, vm.DisplayDifficulty);
            CollectionAssert.AreEqual(
                new[] { EDifficultySegmentState.Available, EDifficultySegmentState.Selected, EDifficultySegmentState.Locked }, States(vm));
        }

        [Test]
        public void 해금_초과_저장_난이도는_표시에서_클램프되고_프로필은_수정되지_않는다()
        {
            MetaProfile profile = Profile(3, 2, 2);

            WorldMapViewModel vm = NewVm(profile);

            Assert.AreEqual(1, vm.DisplayDifficulty);
            Assert.AreEqual(2, profile.SelectedDifficulty, "닫기만 하면 저장하지 않는다");
        }

        [Test]
        public void 음수_저장_난이도는_Easy로_표시된다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2, -4));

            Assert.AreEqual(0, vm.DisplayDifficulty);
        }

        [Test]
        public void 저장된_스테이지가_잠겨_있으면_보정된_스테이지의_상한으로_난이도를_표시한다()
        {
            MetaProfile profile = Profile(1, 9, 2);

            WorldMapViewModel vm = NewVm(profile);

            Assert.AreEqual(2, vm.SelectedStageIndex, "cleared 1 → MaxPlayable 2 (미클리어)");
            Assert.AreEqual(0, vm.DisplayDifficulty);
        }

        //# --- 클릭: 선택 / 잠금 토스트 ---

        [Test]
        public void 선택_가능한_세그먼트를_누르면_표시가_바뀌고_효과_문구와_OnChanged가_갱신된다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2, 0, (2, 1)));
            int changed = 0;
            vm.OnChanged += () => ++changed;

            Assert.AreEqual("영웅 HP ×1 · 공격력 ×1", vm.DifficultyEffectText);
            Assert.IsTrue(vm.SelectDifficulty(1));
            Assert.AreEqual(1, vm.DisplayDifficulty);
            Assert.AreEqual("영웅 HP ×1.15 · 공격력 ×1.1", vm.DifficultyEffectText);
            Assert.IsTrue(vm.SelectDifficulty(2));
            Assert.AreEqual("영웅 HP ×1.3 · 공격력 ×1.22", vm.DifficultyEffectText);
            Assert.AreEqual(2, changed);
        }

        [Test]
        public void 잠긴_Normal을_누르면_Easy_클리어_안내_토스트만_나오고_표시는_그대로다()
        {
            MetaProfile profile = Profile(3, 4);
            WorldMapViewModel vm = NewVm(profile);
            string notice = null;
            int changed = 0;
            vm.OnNotice += message => notice = message;
            vm.OnChanged += () => ++changed;

            Assert.IsFalse(vm.SelectDifficulty(1));

            Assert.AreEqual("Easy 클리어 시 해금", notice);
            Assert.AreEqual(0, vm.DisplayDifficulty);
            Assert.AreEqual(0, profile.SelectedDifficulty);
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void 잠긴_Hard를_누르면_Normal_클리어_안내_토스트가_나온다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2));
            string notice = null;
            vm.OnNotice += message => notice = message;

            Assert.IsFalse(vm.SelectDifficulty(2));

            Assert.AreEqual("Normal 클리어 시 해금", notice);
            Assert.AreEqual(0, vm.DisplayDifficulty);
        }

        [Test]
        public void 잠긴_Hard를_누르면_구조화_잠금_이벤트가_토스트보다_먼저_발행된다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2));
            List<string> order = new List<string>();
            WorldMapDifficultyLockData lockData = null;
            vm.OnDifficultyLocked += data => { lockData = data; order.Add("lock"); };
            vm.OnNotice += message => order.Add("notice");

            Assert.IsFalse(vm.SelectDifficulty(2));

            CollectionAssert.AreEqual(new[] { "lock", "notice" }, order);
            Assert.AreEqual(2, lockData.Difficulty);
            Assert.AreEqual(1, lockData.ClearedMax);   //# 스테이지 2 는 클리어됨 → Normal 까지 선택 가능
            Assert.AreEqual(3, lockData.StepCount);
            Assert.AreEqual("Hard 잠김", lockData.TitleText);
            Assert.AreEqual("Normal 클리어 시 해금", lockData.HintText);
        }

        [Test]
        public void 선택_가능한_난이도를_누르면_잠금_이벤트는_발행되지_않는다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2, 0, (2, 1)));
            int fired = 0;
            vm.OnDifficultyLocked += data => ++fired;

            Assert.IsTrue(vm.SelectDifficulty(1));

            Assert.AreEqual(0, fired);
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(99)]
        public void 범위_밖_세그먼트는_무반응이고_토스트도_없다(int difficulty)
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2, 0, (2, 1)));
            string notice = null;
            vm.OnNotice += message => notice = message;

            Assert.IsFalse(vm.SelectDifficulty(difficulty));

            Assert.IsNull(notice);
            Assert.AreEqual(0, vm.DisplayDifficulty);
        }

        //# --- 노드 이동: _wantedDifficulty 클램프 / 복원 ---

        [Test]
        public void 노드를_옮기면_표시는_그_스테이지_상한으로_클램프되고_돌아오면_선택값이_복원된다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 1, 0, (1, 2)));

            Assert.IsTrue(vm.SelectDifficulty(2), "스테이지 1: Hard 까지 열림");
            Assert.IsTrue(vm.SelectNode(2));
            Assert.AreEqual(1, vm.DisplayDifficulty, "스테이지 2 는 Normal 까지");
            Assert.AreEqual(EDifficultySegmentState.Selected, vm.DifficultySegments[1].State);
            Assert.IsTrue(vm.SelectNode(4));
            Assert.AreEqual(0, vm.DisplayDifficulty, "스테이지 4 는 미클리어");
            Assert.IsTrue(vm.SelectNode(1));
            Assert.AreEqual(2, vm.DisplayDifficulty, "선택값 Hard 복원");
        }

        [Test]
        public void 클램프된_스테이지에서_새로_고른_난이도가_이전_선택값을_대체한다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 1, 0, (1, 2)));
            vm.SelectDifficulty(2);
            vm.SelectNode(2);

            Assert.IsTrue(vm.SelectDifficulty(0));
            vm.SelectNode(1);

            Assert.AreEqual(0, vm.DisplayDifficulty, "명시 선택 Easy 가 Hard 를 덮는다");
        }

        [Test]
        public void 잠긴_노드를_눌러_이동이_거부되면_표시_난이도가_그대로다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 1, 1, (1, 1)));

            Assert.IsFalse(vm.SelectNode(5));

            Assert.AreEqual(1, vm.DisplayDifficulty);
        }

        [Test]
        public void 챕터를_옮겨도_선택값은_새_스테이지_상한으로_클램프된다()
        {
            WorldMapViewModel vm = NewVm(Profile(7, 5, 0, (5, 2)));
            vm.SelectDifficulty(2);

            Assert.IsTrue(vm.SelectChapter(2));

            Assert.AreEqual(1, vm.DisplayDifficulty, "2챕터 첫 스테이지: 행 없는 클리어 → Normal 까지");
        }

        //# --- 확정 / 미확정 ---

        [Test]
        public void 세그먼트_선택과_노드_이동만으로는_프로필_SelectedDifficulty가_바뀌지_않는다()
        {
            MetaProfile profile = Profile(3, 1, 0, (1, 2));
            WorldMapViewModel vm = NewVm(profile);

            vm.SelectDifficulty(2);
            vm.SelectNode(2);
            vm.SelectNode(1);

            Assert.AreEqual(0, profile.SelectedDifficulty, "확정 없이 닫으면 저장하지 않는다");
        }

        [Test]
        public void 확정하면_표시_난이도가_SelectedDifficulty로_저장되고_onConfirm_시점에는_이미_반영돼_있다()
        {
            MetaProfile profile = Profile(3, 1, 0, (1, 2));
            int seenDifficulty = -1;
            int seenLinear = -1;
            WorldMapViewModel vm = NewVm(profile, linear =>
            {
                seenLinear = linear;
                seenDifficulty = profile.SelectedDifficulty;
            });
            vm.SelectDifficulty(2);

            Assert.IsTrue(vm.Confirm());

            Assert.AreEqual(2, profile.SelectedDifficulty);
            Assert.AreEqual(2, seenDifficulty, "호출자가 이어서 Save 하므로 onConfirm 전에 기록돼야 한다");
            Assert.AreEqual(1, seenLinear);
        }

        [Test]
        public void 상한이_낮은_스테이지로_옮겨_확정하면_클램프된_난이도가_정규화_저장된다()
        {
            MetaProfile profile = Profile(3, 1, 0, (1, 2));
            int confirmed = -1;
            WorldMapViewModel vm = NewVm(profile, linear => confirmed = linear);
            vm.SelectDifficulty(2);
            vm.SelectNode(4);

            Assert.IsTrue(vm.Confirm());

            Assert.AreEqual(4, confirmed);
            Assert.AreEqual(0, profile.SelectedDifficulty);
        }

        [Test]
        public void 잠긴_노드_이동이_거부돼도_직전_선택_스테이지와_난이도로_확정된다()
        {
            MetaProfile profile = Profile(3, 1, 1, (1, 2));
            WorldMapViewModel vm = NewVm(profile);
            vm.SelectDifficulty(2);
            vm.SelectNode(5);

            //# 노드 5 는 잠금이라 이동되지 않았고 선택 스테이지(1)는 그대로다.
            Assert.IsTrue(vm.Confirm());
            Assert.AreEqual(2, profile.SelectedDifficulty);
        }

        //# --- 난이도 표 미로드 (Easy 만 존재) ---

        [Test]
        public void 표_미로드면_난이도_박스가_비활성이고_클릭도_저장도_하지_않는다()
        {
            DifficultyTable.Current = null;
            MetaProfile profile = Profile(3, 2, 2, (2, 1));
            WorldMapViewModel vm = new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), profile, WorldMapTestSupport.Text, null);
            string notice = null;
            vm.OnNotice += message => notice = message;

            Assert.IsFalse(vm.DifficultyEnabled);
            Assert.AreEqual(0, vm.DifficultySegments.Count);
            Assert.AreEqual(string.Empty, vm.DifficultyEffectText);
            Assert.IsFalse(vm.SelectDifficulty(1));
            Assert.IsNull(notice);
            Assert.IsFalse(vm.HasBestDifficulty, "표가 없으면 최고 난이도 표시도 숨김");

            Assert.IsTrue(vm.Confirm());
            Assert.AreEqual(2, profile.SelectedDifficulty, "표 미로드 확정은 난이도 값을 건드리지 않는다");
        }

        [Test]
        public void 생성자_인자를_생략하면_DifficultyTable_Current를_쓴다()
        {
            DifficultyTable.Current = DifficultyTestSupport.Table();

            WorldMapViewModel vm = new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), Profile(3, 2), WorldMapTestSupport.Text, null);

            Assert.AreEqual(3, vm.DifficultySegments.Count);
        }

        //# --- 상세 카드 최고 난이도 ---

        [Test]
        public void 최고_난이도는_행이_있는_스테이지에서만_표시된다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 1, 0, (2, 1)));

            Assert.IsFalse(vm.HasBestDifficulty);
            Assert.AreEqual(string.Empty, vm.BestDifficultyText);

            vm.SelectNode(2);
            Assert.IsTrue(vm.HasBestDifficulty);
            Assert.AreEqual(1, vm.BestDifficulty);
            Assert.AreEqual("최고 난이도 Normal", vm.BestDifficultyText);

            vm.SelectNode(3);
            Assert.IsFalse(vm.HasBestDifficulty, "Easy 만 클리어한 스테이지는 숨김");
        }

        //# --- 결과창 행 (ResultPopup.BuildRows) ---

        private static ResultPopupArg Arg(BattleResult result, int difficulty, int unlocked)
        {
            return new ResultPopupArg { Result = result, ClearTime = 83.4f, HeroHpRatio = 0.4f, Difficulty = difficulty, DifficultyUnlocked = unlocked };
        }

        [Test]
        public void Easy_판은_난이도_줄이_없다()
        {
            ResultRowsData rows = ResultPopup.BuildRows(Arg(BattleResult.Win, 0, 0));

            Assert.IsFalse(rows.ShowDifficultyRow);
            Assert.IsFalse(rows.ShowUnlockBanner);
            Assert.IsTrue(rows.ShowClearRow);
        }

        [TestCase(1, "Normal")]
        [TestCase(2, "Hard")]
        public void Normal_Hard_판은_승리_패배_모두_난이도_줄과_라벨이_표시된다(int difficulty, string label)
        {
            ResultRowsData win = ResultPopup.BuildRows(Arg(BattleResult.Win, difficulty, 0));
            ResultRowsData lose = ResultPopup.BuildRows(Arg(BattleResult.Lose, difficulty, 0));

            Assert.IsTrue(win.ShowDifficultyRow);
            Assert.AreEqual(label, win.DifficultyText);
            Assert.AreEqual(difficulty, win.DifficultyIndex);
            Assert.IsTrue(lose.ShowDifficultyRow);
            Assert.AreEqual(label, lose.DifficultyText);
            Assert.IsTrue(lose.ShowHeroHpRow);
            Assert.IsFalse(lose.ShowClearRow);
        }

        [Test]
        public void 라벨은_난이도_표가_로드되면_Strings_Ko_문구를_쓴다()
        {
            DifficultyTable.Current = DifficultyTestSupport.Table();
            CHText.StringProvider = new DifficultyTestSupport.RealStrings();

            ResultRowsData rows = ResultPopup.BuildRows(Arg(BattleResult.Win, 2, 0));

            Assert.AreEqual("Hard", rows.DifficultyText);
        }

        [TestCase(1, "Normal 해금!")]
        [TestCase(2, "Hard 해금!")]
        public void 승리하며_새_난이도가_열리면_해금_배너가_뜬다(int unlocked, string expected)
        {
            CHText.StringProvider = new DifficultyTestSupport.RealStrings();

            ResultRowsData rows = ResultPopup.BuildRows(Arg(BattleResult.Win, 0, unlocked));

            Assert.IsTrue(rows.ShowUnlockBanner);
            Assert.AreEqual(expected, rows.UnlockBannerText);
            Assert.IsFalse(rows.ShowDifficultyRow, "Easy 로 이긴 판은 배너만, 난이도 줄은 없음");
        }

        [Test]
        public void 문구_provider가_없어도_배너는_기본_문구로_표시된다()
        {
            CHText.StringProvider = null;

            ResultRowsData rows = ResultPopup.BuildRows(Arg(BattleResult.Win, 1, 2));

            Assert.AreEqual("Hard 해금!", rows.UnlockBannerText);
        }

        [Test]
        public void 패배이거나_열린_난이도가_없으면_배너가_없다()
        {
            Assert.IsFalse(ResultPopup.BuildRows(Arg(BattleResult.Lose, 1, 2)).ShowUnlockBanner, "패배 화면 배너 없음");
            Assert.IsFalse(ResultPopup.BuildRows(Arg(BattleResult.Win, 1, 0)).ShowUnlockBanner);
            Assert.IsFalse(ResultPopup.BuildRows(Arg(BattleResult.Win, 2, 0)).ShowUnlockBanner);
        }

        [Test]
        public void 난이도_색은_Easy_소울_Normal_금_Hard_피로_서로_다르다()
        {
            Assert.AreEqual(UiDotPalette.Soul, UiDotPalette.DifficultyColor(0));
            Assert.AreEqual(UiDotPalette.Gold, UiDotPalette.DifficultyColor(1));
            Assert.AreEqual(UiDotPalette.Blood, UiDotPalette.DifficultyColor(2));
            Assert.AreEqual(UiDotPalette.Soul, UiDotPalette.DifficultyColor(-1));
            Assert.AreEqual(UiDotPalette.Blood, UiDotPalette.DifficultyColor(9));
        }

        //# --- 전투 HUD 영웅 제목 ---

        [Test]
        public void 영웅_제목은_Easy면_기존_표기이고_Normal_Hard면_난이도_접미가_붙는다()
        {
            Assert.AreEqual("기사 · 3단계", BattleViewModel.ComposeHeroTitle("기사", 3));
            Assert.AreEqual("기사 · 3단계", BattleViewModel.ComposeHeroTitle("기사", 3, string.Empty));
            Assert.AreEqual("기사 · 3단계", BattleViewModel.ComposeHeroTitle("기사", 3, null));
            Assert.AreEqual("기사 · 3단계 · Normal", BattleViewModel.ComposeHeroTitle("기사", 3, "Normal"));
            Assert.AreEqual("기사 · 3단계 · Hard", BattleViewModel.ComposeHeroTitle("기사", 3, "Hard"));
        }

        //# --- 프리팹 SerializeField 연결 ---

        private static bool IsMissing(object value)
        {
            if (value == null)
                return true;
            UnityEngine.Object obj = value as UnityEngine.Object;
            return value is UnityEngine.Object && obj == null;
        }

        private static object Field(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} 필드 없음 — 시그니처 변경 감지");
            return field.GetValue(target);
        }

        private static void AssertWired(object target, params string[] names)
        {
            foreach (string name in names)
            {
                Assert.IsFalse(IsMissing(Field(target, name)), $"{target.GetType().Name}.{name} 참조가 비어 있음");
            }
        }

        [Test]
        public void ResultPopup_프리팹의_난이도_줄과_해금_배너_참조가_연결돼_있다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath);
            Assert.IsNotNull(prefab, "ResultPopup.prefab 로드 실패");
            ResultPopup popup = prefab.GetComponent<ResultPopup>();
            Assert.IsNotNull(popup);

            AssertWired(popup, "_rowDifficulty", "_difficultyText", "_unlockBanner", "_unlockBannerText");
        }

        [Test]
        public void ResultPopup_프리팹의_난이도_줄_라벨은_CHText_stringID_1334로_연결돼_있다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath);
            ResultPopup popup = prefab.GetComponent<ResultPopup>();
            GameObject row = (GameObject)Field(popup, "_rowDifficulty");

            List<int> ids = new List<int>();
            foreach (CHText text in row.GetComponentsInChildren<CHText>(true))
            {
                ids.Add((int)Field(text, "_stringID"));
            }

            CollectionAssert.Contains(ids, 1334, "정적 라벨 '난이도'(Rule 03 §3)");
        }

        [Test]
        public void WorldMapPopup_프리팹의_난이도_세그먼트_3칸과_효과_문구_최고_난이도_참조가_연결돼_있다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldMapPrefabPath);
            WorldMapPopup popup = prefab.GetComponent<WorldMapPopup>();
            Assert.IsNotNull(popup);

            AssertWired(popup, "_difficultyBestText", "_hpChip", "_powerChip", "_lockNotice", "_detailBestDifficultyText", "_bottomText");
            WorldMapDifficultySegmentView[] segments = (WorldMapDifficultySegmentView[])Field(popup, "_difficultySegments");
            Assert.AreEqual(3, segments.Length);
            for (int i = 0; i < segments.Length; ++i)
            {
                Assert.IsFalse(IsMissing(segments[i]), $"_difficultySegments[{i}] 비어 있음");
                AssertWired(segments[i], "_button", "_background", "_labelText", "_multiplierText", "_icon");
            }
        }

        [Test]
        public void RecordsStageCell_프리팹의_최고_난이도_텍스트_참조가_연결돼_있다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RecordsCellPrefabPath);
            Assert.IsNotNull(prefab, "RecordsStageCell.prefab 로드 실패");
            RecordsStageCell cell = prefab.GetComponent<RecordsStageCell>();
            Assert.IsNotNull(cell);

            AssertWired(cell, "_bestDifficultyText");
        }
    }
}
