using System.Collections.Generic;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using Lair.Tests.EditMode;
using Lair.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.UI
{
    //# 시너지 진행도 표시 위젯 단위 테스트(기획서 card-synergy-indicator §2.1·§3·§6, 수용 기준 A-2~A-7). View 는 리플렉션 주입으로 조립.
    public class SynergyIndicatorWidgetTests
    {
        private static readonly Color EmptyFill = new Color32(0x0B, 0x0E, 0x14, 0xFF);
        private static readonly Color FrameNormal = new Color32(0x07, 0x09, 0x0E, 0xFF);
        private static readonly Color FrameTierEmpty = new Color32(0xB9, 0xB0, 0x9A, 0xFF);
        private static readonly Color FrameTierFilled = new Color32(0xE8, 0xE1, 0xCF, 0xFF);
        private static readonly Color Axis = new Color32(0x5A, 0xA9, 0xFF, 0xFF);

        private readonly List<Object> _garbage = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _garbage.Count; ++i)
            {
                if (_garbage[i] != null)
                    Object.DestroyImmediate(_garbage[i]);
            }
            _garbage.Clear();
        }

        private GameObject NewGo(string name, Transform parent = null)
        {
            GameObject go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            else
                _garbage.Add(go);
            return go;
        }

        private Sprite NewSprite()
        {
            Texture2D tex = new Texture2D(12, 12);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f));
            _garbage.Add(sprite);
            _garbage.Add(tex);
            return sprite;
        }

        private class TrackParts
        {
            public SynergyTrack Track;
            public Image[] Frames;
            public Image[] Fills;
        }

        private TrackParts MakeTrack(Transform parent = null)
        {
            GameObject go = NewGo("Track", parent);
            SynergyTrack track = go.AddComponent<SynergyTrack>();
            Image[] frames = new Image[7];
            Image[] fills = new Image[7];
            for (int i = 0; i < 7; ++i)
            {
                frames[i] = NewGo($"Frame{i}", go.transform).AddComponent<Image>();
                fills[i] = NewGo($"Fill{i}", frames[i].transform).AddComponent<Image>();
            }
            TestReflection.SetField(track, "_frames", frames);
            TestReflection.SetField(track, "_fills", fills);
            return new TrackParts { Track = track, Frames = frames, Fills = fills };
        }

        private CHText MakeText(string name, Transform parent, out TextMeshProUGUI tmp)
        {
            GameObject go = NewGo(name, parent);
            tmp = go.AddComponent<TextMeshProUGUI>();
            return go.AddComponent<CHText>();
        }

        //# ===== SynergyTrack =====

        private void AssertTrack(TrackParts t, int count, Color axis)
        {
            int filled = SynergyProgress.FilledCells(count);
            for (int i = 0; i < 7; ++i)
            {
                bool isFilled = i < filled;
                Assert.AreEqual(isFilled ? axis : EmptyFill, t.Fills[i].color, $"count={count} 칸{i} 채움색");
                Color expectedFrame = SynergyProgress.IsTierCell(i) ? (isFilled ? FrameTierFilled : FrameTierEmpty) : FrameNormal;
                Assert.AreEqual(expectedFrame, t.Frames[i].color, $"count={count} 칸{i} 테두리색");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(15)]
        public void Track_Bind_장수별_채움과_Tier_테두리(int count)
        {
            TrackParts t = MakeTrack();

            t.Track.Bind(count, Axis, false);

            AssertTrack(t, count, Axis);
        }

        [Test]
        public void Track_Bind_음수_장수는_전부_빈칸()
        {
            TrackParts t = MakeTrack();

            t.Track.Bind(-4, Axis, false);

            AssertTrack(t, 0, Axis);
        }

        [Test]
        public void Track_Bind_축_색을_바꿔_재바인딩하면_새_색으로_칠함()
        {
            TrackParts t = MakeTrack();
            Color other = new Color32(0xFF, 0x6B, 0x5A, 0xFF);

            t.Track.Bind(4, Axis, false);
            t.Track.Bind(4, other, false);

            AssertTrack(t, 4, other);
        }

        [Test]
        public void Track_Bind_상위_단계에서_하위로_재바인딩해도_잔상_없음()
        {
            TrackParts t = MakeTrack();

            t.Track.Bind(9, Axis, false);
            t.Track.Bind(2, Axis, false);

            AssertTrack(t, 2, Axis);
            Assert.AreEqual(FrameTierEmpty, t.Frames[4].color, "5번째 칸 테두리 Tier 빈 상태 복귀");
            Assert.AreEqual(FrameTierEmpty, t.Frames[6].color, "7번째 칸 테두리 Tier 빈 상태 복귀");
        }

        [Test]
        public void Track_Bind_0장으로_리셋하면_전부_빈칸()
        {
            TrackParts t = MakeTrack();

            t.Track.Bind(7, Axis, false);
            t.Track.Bind(0, Axis, false);

            AssertTrack(t, 0, Axis);
        }

        [Test]
        public void Track_Bind_flash_true면_방금_채워진_칸이_흰색으로_시작()
        {
            TrackParts t = MakeTrack();
            t.Track.gameObject.SetActive(true);

            t.Track.Bind(4, Axis, true);

            Assert.AreEqual(Color.white, t.Fills[3].color, "새 칸 점멸 시작 — 흰색");
            Assert.AreEqual(Axis, t.Fills[2].color, "이전 칸은 축 색 유지");
        }

        [Test]
        public void Track_Bind_flash_중_재바인딩하면_점멸_정리되고_축색_복원()
        {
            TrackParts t = MakeTrack();

            t.Track.Bind(4, Axis, true);
            t.Track.Bind(4, Axis, false);

            AssertTrack(t, 4, Axis);
        }

        [Test]
        public void Track_Bind_flash_true여도_0장이면_점멸_없음()
        {
            TrackParts t = MakeTrack();

            t.Track.Bind(0, Axis, true);

            AssertTrack(t, 0, Axis);
        }

        [Test]
        public void Track_Bind_flash_true여도_8장_이상이면_점멸_없음()
        {
            TrackParts t = MakeTrack();

            t.Track.Bind(9, Axis, true);

            AssertTrack(t, 9, Axis);
        }

        [Test]
        public void Track_Bind_비활성_오브젝트에서는_점멸_시작_안함()
        {
            TrackParts t = MakeTrack();
            t.Track.gameObject.SetActive(false);

            t.Track.Bind(3, Axis, true);

            AssertTrack(t, 3, Axis);
        }

        [Test]
        public void Track_Bind_배열_미배선이어도_점멸없이는_예외없음()
        {
            GameObject go = NewGo("BareTrack");
            SynergyTrack track = go.AddComponent<SynergyTrack>();

            Assert.DoesNotThrow(() => track.Bind(5, Axis, false));
        }

        [Test]
        public void Track_Bind_배열_미배선이어도_점멸_요청시_예외없음()
        {
            GameObject go = NewGo("BareTrack");
            SynergyTrack track = go.AddComponent<SynergyTrack>();

            Assert.DoesNotThrow(() => track.Bind(5, Axis, true));
        }

        //# ===== BuildSynergyCell 리디자인 모드 =====

        private class CellParts
        {
            public BuildSynergyCell Cell;
            public Image AxisIcon;
            public Image Strip;
            public CanvasGroup Group;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Count;
            public CHText CountChText;
            public TrackParts Track;
        }

        private CellParts MakeRedesignCell(Transform parent = null)
        {
            GameObject go = NewGo("Cell", parent);
            BuildSynergyCell cell = go.AddComponent<BuildSynergyCell>();
            CellParts p = new CellParts { Cell = cell };
            p.Group = go.AddComponent<CanvasGroup>();
            p.AxisIcon = NewGo("Icon", go.transform).AddComponent<Image>();
            p.Strip = NewGo("Strip", go.transform).AddComponent<Image>();
            CHText label = MakeText("Label", go.transform, out p.Label);
            p.CountChText = MakeText("Count", go.transform, out p.Count);
            p.Track = MakeTrack(go.transform);
            TestReflection.SetField(cell, "_axisIcon", p.AxisIcon);
            TestReflection.SetField(cell, "_axisStrip", p.Strip);
            TestReflection.SetField(cell, "_group", p.Group);
            TestReflection.SetField(cell, "_text", label);
            TestReflection.SetField(cell, "_countText", p.CountChText);
            TestReflection.SetField(cell, "_track", p.Track.Track);
            return p;
        }

        private static BuildSynergyCellData Data(EBuildAxis axis, int count, Sprite icon = null, int prev = -1)
        {
            return BuildSynergyCellData.Create(axis, count, icon, prev < 0 ? count : prev);
        }

        [TestCase(0, "0/3", 0.55f, false)]
        [TestCase(1, "1/3", 1f, false)]
        [TestCase(2, "2/3", 1f, false)]
        [TestCase(3, "3/5 T1", 1f, true)]
        [TestCase(5, "5/7 T2", 1f, true)]
        [TestCase(7, "7+ T3", 1f, true)]
        [TestCase(9, "9+ T3", 1f, true)]
        public void Cell_Bind_장수별_텍스트_불투명도_색띠(int count, string text, float alpha, bool strip)
        {
            CellParts p = MakeRedesignCell();

            p.Cell.Bind(Data(EBuildAxis.Tank, count));

            Assert.AreEqual("TANK", p.Label.text);
            Assert.AreEqual(text, p.Count.text);
            Assert.AreEqual(alpha, p.Group.alpha, 0.0001f);
            Assert.AreEqual(strip, p.Strip.gameObject.activeSelf);
        }

        [Test]
        public void Cell_Bind_텍스트_색은_축_색()
        {
            CellParts p = MakeRedesignCell();

            p.Cell.Bind(Data(EBuildAxis.Dps, 4));

            Color dps = BuildSynergyPanel.AxisColor[EBuildAxis.Dps];
            Assert.AreEqual(dps, p.Label.color);
            Assert.AreEqual(dps, p.Count.color);
            Assert.AreEqual(dps, p.Strip.color);
        }

        [Test]
        public void Cell_Bind_트랙이_장수대로_채워짐()
        {
            CellParts p = MakeRedesignCell();

            p.Cell.Bind(Data(EBuildAxis.Tank, 5));

            AssertTrack(p.Track, 5, BuildSynergyPanel.AxisColor[EBuildAxis.Tank]);
        }

        [Test]
        public void Cell_Bind_풀_재사용_Tier3에서_0장으로_재바인딩해도_잔상_없음()
        {
            CellParts p = MakeRedesignCell();
            Color tank = BuildSynergyPanel.AxisColor[EBuildAxis.Tank];

            p.Cell.Bind(Data(EBuildAxis.Tank, 9));
            p.Cell.Bind(Data(EBuildAxis.Tank, 0));

            Assert.AreEqual("0/3", p.Count.text);
            Assert.AreEqual(0.55f, p.Group.alpha, 0.0001f);
            Assert.IsFalse(p.Strip.gameObject.activeSelf);
            AssertTrack(p.Track, 0, tank);
        }

        [Test]
        public void Cell_Bind_다른_축_재바인딩시_라벨과_색_교체()
        {
            CellParts p = MakeRedesignCell();

            p.Cell.Bind(Data(EBuildAxis.Tank, 3));
            p.Cell.Bind(Data(EBuildAxis.Swarm, 1));

            Color swarm = BuildSynergyPanel.AxisColor[EBuildAxis.Swarm];
            Assert.AreEqual("SWARM", p.Label.text);
            Assert.AreEqual("1/3", p.Count.text);
            Assert.AreEqual(swarm, p.Count.color);
            Assert.IsFalse(p.Strip.gameObject.activeSelf);
            AssertTrack(p.Track, 1, swarm);
        }

        [Test]
        public void Cell_Bind_아이콘_있으면_표시_없으면_숨김()
        {
            CellParts p = MakeRedesignCell();
            Sprite icon = NewSprite();

            p.Cell.Bind(Data(EBuildAxis.Tank, 1, icon));
            Assert.IsTrue(p.AxisIcon.enabled);
            Assert.AreSame(icon, p.AxisIcon.sprite);

            p.Cell.Bind(Data(EBuildAxis.Tank, 1, null));
            Assert.IsFalse(p.AxisIcon.enabled);
            Assert.IsNull(p.AxisIcon.sprite);
        }

        [Test]
        public void Cell_Bind_GainedCell_true면_트랙_새칸_점멸_시작()
        {
            CellParts p = MakeRedesignCell();

            p.Cell.Bind(Data(EBuildAxis.Tank, 2, null, 1));

            Assert.AreEqual(Color.white, p.Track.Fills[1].color);
        }

        [Test]
        public void Cell_Bind_GainedCell_false면_점멸_없음()
        {
            CellParts p = MakeRedesignCell();

            p.Cell.Bind(Data(EBuildAxis.Tank, 2, null, 2));

            AssertTrack(p.Track, 2, BuildSynergyPanel.AxisColor[EBuildAxis.Tank]);
        }

        [Test]
        public void Cell_Bind_null_데이터는_무시()
        {
            CellParts p = MakeRedesignCell();
            p.Cell.Bind(Data(EBuildAxis.Tank, 4));

            Assert.DoesNotThrow(() => p.Cell.Bind(null));
            Assert.AreEqual("4/5 T1", p.Count.text);
        }

        [Test]
        public void Cell_Bind_선택_위젯_미배선이어도_예외없음()
        {
            GameObject go = NewGo("MinimalCell");
            BuildSynergyCell cell = go.AddComponent<BuildSynergyCell>();
            Image icon = NewGo("Icon", go.transform).AddComponent<Image>();
            TestReflection.SetField(cell, "_axisIcon", icon);

            Assert.DoesNotThrow(() => cell.Bind(Data(EBuildAxis.Debuff, 6)));
        }

        //# ===== BuildSynergyCellData.Create =====

        [Test]
        public void CellData_Create_필드_매핑()
        {
            Sprite icon = NewSprite();

            BuildSynergyCellData d = BuildSynergyCellData.Create(EBuildAxis.Debuff, 4, icon, 4);

            Assert.AreEqual(EBuildAxis.Debuff, d.Axis);
            Assert.AreEqual(BuildSynergyPanel.AxisColor[EBuildAxis.Debuff], d.Color);
            Assert.AreEqual("DEBUFF", d.Label);
            Assert.AreSame(icon, d.Icon);
            Assert.AreEqual(4, d.Count);
            Assert.AreEqual(5, d.NextThreshold);
            Assert.AreEqual(1, d.ActiveTier);
        }

        [Test]
        public void CellData_Create_4축_모두_색과_라벨_매핑()
        {
            foreach (EBuildAxis axis in BuildSynergyPanel.AllAxes)
            {
                BuildSynergyCellData d = BuildSynergyCellData.Create(axis, 0, null, 0);
                Assert.AreEqual(BuildSynergyPanel.AxisColor[axis], d.Color, axis.ToString());
                Assert.AreEqual(BuildSynergyPanel.AxisLabel[axis], d.Label, axis.ToString());
            }
            Assert.AreEqual(4, BuildSynergyPanel.AllAxes.Length);
        }

        [Test]
        public void CellData_Create_AllAxes_순서는_TANK_SWARM_DPS_DEBUFF()
        {
            CollectionAssert.AreEqual(
                new[] { EBuildAxis.Tank, EBuildAxis.Swarm, EBuildAxis.Dps, EBuildAxis.Debuff },
                BuildSynergyPanel.AllAxes);
        }

        [TestCase(0, 1, true, false)]
        [TestCase(2, 3, true, true)]
        [TestCase(3, 3, false, false)]
        [TestCase(4, 5, true, true)]
        [TestCase(6, 7, true, true)]
        [TestCase(7, 8, true, false)]
        [TestCase(5, 2, false, false)]
        [TestCase(9, 0, false, false)]
        [TestCase(0, 0, false, false)]
        public void CellData_Create_GainedCell_JustCrossed(int prev, int count, bool gained, bool crossed)
        {
            BuildSynergyCellData d = BuildSynergyCellData.Create(EBuildAxis.Tank, count, null, prev);

            Assert.AreEqual(gained, d.GainedCell, "GainedCell");
            Assert.AreEqual(crossed, d.JustCrossed, "JustCrossed");
        }

        [Test]
        public void CellData_Create_7장_이상은_NextThreshold_음수_Tier3()
        {
            BuildSynergyCellData d = BuildSynergyCellData.Create(EBuildAxis.Swarm, 8, null, 7);

            Assert.AreEqual(-1, d.NextThreshold);
            Assert.AreEqual(3, d.ActiveTier);
        }

        [Test]
        public void CellData_Create_아이콘_null이면_null_유지()
        {
            Assert.IsNull(BuildSynergyCellData.Create(EBuildAxis.Tank, 1, null, 0).Icon);
        }

        //# ===== CardView.SetAxisIcon =====

        private class CardViewParts
        {
            public CardView View;
            public GameObject Root;
            public Image Border;
            public Image Icon;
        }

        private CardViewParts MakeCardView()
        {
            GameObject go = NewGo("CardView");
            CardView view = go.AddComponent<CardView>();
            GameObject root = NewGo("AxisIconRoot", go.transform);
            Image border = root.AddComponent<Image>();
            Image icon = NewGo("AxisIcon", root.transform).AddComponent<Image>();
            TestReflection.SetField(view, "_axisIconRoot", root);
            TestReflection.SetField(view, "_axisIconBorder", border);
            TestReflection.SetField(view, "_axisIconImage", icon);
            return new CardViewParts { View = view, Root = root, Border = border, Icon = icon };
        }

        private CardData MakeCard(EBuildAxis axis)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            TestReflection.SetField(card, "_axis", axis);
            _garbage.Add(card);
            return card;
        }

        private SynergyVisualConfig MakeConfig(params EBuildAxis[] axes)
        {
            SynergyVisualConfig config = ScriptableObject.CreateInstance<SynergyVisualConfig>();
            SynergyVisualConfig.Entry[] entries = new SynergyVisualConfig.Entry[axes.Length];
            for (int i = 0; i < axes.Length; ++i)
            {
                entries[i] = new SynergyVisualConfig.Entry { Axis = axes[i], Icon = NewSprite() };
            }
            TestReflection.SetField(config, "_entries", entries);
            _garbage.Add(config);
            return config;
        }

        [Test]
        public void CardView_SetAxisIcon_카드_축_아이콘과_테두리색_표시()
        {
            CardViewParts p = MakeCardView();
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm);
            p.Root.SetActive(false);

            p.View.SetAxisIcon(MakeCard(EBuildAxis.Debuff), config);

            Assert.IsTrue(p.Root.activeSelf);
            Assert.AreSame(config.GetIcon(EBuildAxis.Debuff), p.Icon.sprite);
            Assert.AreEqual(BuildSynergyPanel.AxisColor[EBuildAxis.Debuff], p.Border.color);
        }

        [Test]
        public void CardView_SetAxisIcon_축이_다른_카드는_서로_다른_아이콘()
        {
            CardViewParts a = MakeCardView();
            CardViewParts b = MakeCardView();
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm);

            a.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);
            b.View.SetAxisIcon(MakeCard(EBuildAxis.Swarm), config);

            Assert.AreNotSame(a.Icon.sprite, b.Icon.sprite);
            Assert.AreNotEqual(a.Border.color, b.Border.color);
        }

        [Test]
        public void CardView_SetAxisIcon_같은_축_카드는_같은_아이콘()
        {
            CardViewParts a = MakeCardView();
            CardViewParts b = MakeCardView();
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank);

            a.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);
            b.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);

            Assert.AreSame(a.Icon.sprite, b.Icon.sprite);
        }

        [Test]
        public void CardView_SetAxisIcon_SO_null이면_숨김()
        {
            CardViewParts p = MakeCardView();

            p.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), null);

            Assert.IsFalse(p.Root.activeSelf);
        }

        [Test]
        public void CardView_SetAxisIcon_카드_null이면_숨김()
        {
            CardViewParts p = MakeCardView();

            p.View.SetAxisIcon(null, MakeConfig(EBuildAxis.Tank));

            Assert.IsFalse(p.Root.activeSelf);
        }

        [Test]
        public void CardView_SetAxisIcon_SO에_해당_축_없으면_숨김()
        {
            CardViewParts p = MakeCardView();

            p.View.SetAxisIcon(MakeCard(EBuildAxis.Swarm), MakeConfig(EBuildAxis.Tank));

            Assert.IsFalse(p.Root.activeSelf);
        }

        [Test]
        public void CardView_SetAxisIcon_Sprite_null_항목이면_숨김()
        {
            CardViewParts p = MakeCardView();
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank);
            config.Entries[0].Icon = null;

            p.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);

            Assert.IsFalse(p.Root.activeSelf);
        }

        [Test]
        public void CardView_SetAxisIcon_표시_후_아이콘_없는_카드로_재호출하면_다시_숨김()
        {
            CardViewParts p = MakeCardView();
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank);

            p.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);
            Assert.IsTrue(p.Root.activeSelf);
            p.View.SetAxisIcon(MakeCard(EBuildAxis.Dps), config);

            Assert.IsFalse(p.Root.activeSelf);
        }

        [Test]
        public void CardView_SetAxisIcon_숨김_후_아이콘_카드로_재호출하면_다시_표시()
        {
            CardViewParts p = MakeCardView();
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank);

            p.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), null);
            p.View.SetAxisIcon(MakeCard(EBuildAxis.Tank), config);

            Assert.IsTrue(p.Root.activeSelf);
        }

        [Test]
        public void CardView_SetAxisIcon_루트_미배선이면_예외없이_무시()
        {
            GameObject go = NewGo("BareCardView");
            CardView view = go.AddComponent<CardView>();

            Assert.DoesNotThrow(() => view.SetAxisIcon(MakeCard(EBuildAxis.Tank), MakeConfig(EBuildAxis.Tank)));
        }

        [Test]
        public void CardView_SetAxisIcon_이미지_테두리_미배선이어도_루트만_토글()
        {
            GameObject go = NewGo("PartialCardView");
            CardView view = go.AddComponent<CardView>();
            GameObject root = NewGo("Root", go.transform);
            TestReflection.SetField(view, "_axisIconRoot", root);
            root.SetActive(false);

            Assert.DoesNotThrow(() => view.SetAxisIcon(MakeCard(EBuildAxis.Tank), MakeConfig(EBuildAxis.Tank)));
            Assert.IsTrue(root.activeSelf);
        }

        //# ===== CardSelectionPopup 칩 4개 바인딩 =====

        private CardSelectionPopup MakePopupWithChips(out CellParts[] chips)
        {
            GameObject go = NewGo("Popup");
            CardSelectionPopup popup = go.AddComponent<CardSelectionPopup>();
            BuildSynergyCell[] cells = new BuildSynergyCell[4];
            chips = new CellParts[4];
            for (int i = 0; i < 4; ++i)
            {
                chips[i] = MakeRedesignCell(go.transform);
                cells[i] = chips[i].Cell;
            }
            TestReflection.SetField(popup, "_synergyChips", cells);
            return popup;
        }

        private static void InvokeBindChips(CardSelectionPopup popup, CardSelectionArg arg)
        {
            MethodInfo m = typeof(CardSelectionPopup).GetMethod("BindSynergyChips", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "BindSynergyChips 미발견");
            m.Invoke(popup, new object[] { arg });
        }

        [Test]
        public void Popup_칩_4개가_AllAxes_순서로_장수와_아이콘_바인딩()
        {
            CardSelectionPopup popup = MakePopupWithChips(out CellParts[] chips);
            SynergyVisualConfig config = MakeConfig(EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm);
            Dictionary<EBuildAxis, int> counts = new Dictionary<EBuildAxis, int>
            {
                { EBuildAxis.Tank, 0 }, { EBuildAxis.Swarm, 2 }, { EBuildAxis.Dps, 5 }, { EBuildAxis.Debuff, 9 },
            };
            CardSelectionArg arg = new CardSelectionArg { SynergyVisual = config, BuildCountOf = axis => counts[axis] };

            InvokeBindChips(popup, arg);

            string[] labels = { "TANK", "SWARM", "DPS", "DEBUFF" };
            string[] texts = { "0/3", "2/3", "5/7 T2", "9+ T3" };
            for (int i = 0; i < 4; ++i)
            {
                EBuildAxis axis = BuildSynergyPanel.AllAxes[i];
                Assert.AreEqual(labels[i], chips[i].Label.text, $"칩{i} 라벨");
                Assert.AreEqual(texts[i], chips[i].Count.text, $"칩{i} 장수");
                Assert.AreSame(config.GetIcon(axis), chips[i].AxisIcon.sprite, $"칩{i} 아이콘");
                AssertTrack(chips[i].Track, counts[axis], BuildSynergyPanel.AxisColor[axis]);
            }
            Assert.AreEqual(0.55f, chips[0].Group.alpha, 0.0001f);
            Assert.AreEqual(1f, chips[1].Group.alpha, 0.0001f);
        }

        [Test]
        public void Popup_칩_BuildCountOf_null이면_전_축_0장()
        {
            CardSelectionPopup popup = MakePopupWithChips(out CellParts[] chips);

            InvokeBindChips(popup, new CardSelectionArg());

            for (int i = 0; i < 4; ++i)
            {
                Assert.AreEqual("0/3", chips[i].Count.text, $"칩{i}");
                Assert.AreEqual(0.55f, chips[i].Group.alpha, 0.0001f);
                Assert.IsFalse(chips[i].Strip.gameObject.activeSelf);
            }
        }

        [Test]
        public void Popup_칩_SynergyVisual_null이면_아이콘_숨김_텍스트와_트랙은_정상()
        {
            CardSelectionPopup popup = MakePopupWithChips(out CellParts[] chips);
            CardSelectionArg arg = new CardSelectionArg { BuildCountOf = axis => 4 };

            InvokeBindChips(popup, arg);

            for (int i = 0; i < 4; ++i)
            {
                Assert.IsFalse(chips[i].AxisIcon.enabled, $"칩{i} 아이콘 숨김");
                Assert.AreEqual("4/5 T1", chips[i].Count.text);
                AssertTrack(chips[i].Track, 4, BuildSynergyPanel.AxisColor[BuildSynergyPanel.AllAxes[i]]);
            }
        }

        [Test]
        public void Popup_칩_점멸_없음_prev_count_동일()
        {
            CardSelectionPopup popup = MakePopupWithChips(out CellParts[] chips);
            CardSelectionArg arg = new CardSelectionArg { BuildCountOf = axis => 3 };

            InvokeBindChips(popup, arg);

            for (int i = 0; i < 4; ++i)
            {
                Assert.AreNotEqual(Color.white, chips[i].Track.Fills[2].color, $"칩{i} 점멸 금지");
            }
        }

        [Test]
        public void Popup_칩_재오픈시_이전_장수_잔상_없음()
        {
            CardSelectionPopup popup = MakePopupWithChips(out CellParts[] chips);

            InvokeBindChips(popup, new CardSelectionArg { BuildCountOf = axis => 8 });
            InvokeBindChips(popup, new CardSelectionArg { BuildCountOf = axis => 1 });

            for (int i = 0; i < 4; ++i)
            {
                Assert.AreEqual("1/3", chips[i].Count.text);
                Assert.IsFalse(chips[i].Strip.gameObject.activeSelf);
                AssertTrack(chips[i].Track, 1, BuildSynergyPanel.AxisColor[BuildSynergyPanel.AllAxes[i]]);
            }
        }

        [Test]
        public void Popup_칩_배열이_null이거나_일부_null이어도_예외없음()
        {
            GameObject go = NewGo("Popup");
            CardSelectionPopup popup = go.AddComponent<CardSelectionPopup>();
            TestReflection.SetField(popup, "_synergyChips", null);
            Assert.DoesNotThrow(() => InvokeBindChips(popup, new CardSelectionArg()));

            CellParts one = MakeRedesignCell(go.transform);
            TestReflection.SetField(popup, "_synergyChips", new BuildSynergyCell[] { null, one.Cell });
            Assert.DoesNotThrow(() => InvokeBindChips(popup, new CardSelectionArg { BuildCountOf = axis => 3 }));
            Assert.AreEqual("3/5 T1", one.Count.text);
            Assert.AreEqual("SWARM", one.Label.text, "두 번째 칩 = AllAxes[1] = SWARM");
        }
    }
}
