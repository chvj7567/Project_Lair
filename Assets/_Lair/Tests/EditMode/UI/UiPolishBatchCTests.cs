using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Lair.Net;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 비주얼 폴리시 묶음 C(QuestPopup/Cell · LordLevelPopup · LordRewardCell · RankingPopup/Cell · CloudPopup) 스위트(ui-polish-batch-c §12) —
    //# Missing Script · 신규 필드 연결 · 셀 상태 전환 · 순위 메달 · 연결 발광 · origin 보존 · 레이캐스트 · 일회용 빌더 부재.
    public class UiPolishBatchCTests
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string QuestPopupPath = UiDir + "QuestPopup.prefab";
        private const string QuestCellPath = UiDir + "QuestCell.prefab";
        private const string LordPopupPath = UiDir + "LordLevelPopup.prefab";
        private const string LordCellPath = UiDir + "LordRewardCell.prefab";
        private const string RankingPopupPath = UiDir + "RankingPopup.prefab";
        private const string RankingCellPath = UiDir + "RankingCell.prefab";
        private const string CloudPopupPath = UiDir + "CloudPopup.prefab";

        private static readonly Color PodiumGold = new Color32(0xFF, 0xD2, 0x4A, 0xFF);
        private static readonly Color PodiumSilver = new Color32(0xC9, 0xCD, 0xD2, 0xFF);
        private static readonly Color PodiumBronze = new Color32(0xD0, 0x8B, 0x5B, 0xFF);

        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();

        [TearDown]
        public void 생성한_오브젝트를_정리한다()
        {
            foreach (UnityEngine.Object obj in _spawned)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            _spawned.Clear();
        }

        //# --- 공용 헬퍼 ---

        private static FieldInfo FieldOf(object target, string name)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null)
                    return field;
            }
            Assert.Fail($"{target.GetType().Name}.{name} 필드 없음 — 시그니처 변경 감지");
            return null;
        }

        private static object Field(object target, string name)
        {
            return FieldOf(target, name).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldOf(target, name).SetValue(target, value);
        }

        //# 리플렉션 값이 null 이면 NRE 대신 Assert 로 먼저 실패시킨다.
        private static T Required<T>(object target, string name) where T : class
        {
            object value = Field(target, name);
            if (value is UnityEngine.Object unityObj)
            {
                Assert.IsTrue(unityObj != null, $"{target.GetType().Name}.{name} 비어 있음");
            }
            else
            {
                Assert.IsNotNull(value, $"{target.GetType().Name}.{name} 비어 있음");
            }
            return (T)value;
        }

        private static void InvokeNonPublic(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{method} 없음");
            info.Invoke(target, null);
        }

        private static GameObject LoadPrefabAsset(string path)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(root, path);
            return root;
        }

        private GameObject NewInstance(string path)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(LoadPrefabAsset(path)) as GameObject;
            Assert.IsNotNull(instance, "프리팹 인스턴스 " + path);
            _spawned.Add(instance);
            return instance;
        }

        private static T Comp<T>(GameObject root) where T : Component
        {
            T comp = root.GetComponentInChildren<T>(true);
            Assert.IsTrue(comp != null, $"{root.name} 에 {typeof(T).Name} 없음");
            return comp;
        }

        private QuestCell NewQuestCell()
        {
            return Comp<QuestCell>(NewInstance(QuestCellPath));
        }

        private RankingCell NewRankingCell()
        {
            return Comp<RankingCell>(NewInstance(RankingCellPath));
        }

        private LordRewardCell NewLordCell()
        {
            return Comp<LordRewardCell>(NewInstance(LordCellPath));
        }

        private CloudPopup NewCloudPopup()
        {
            return Comp<CloudPopup>(NewInstance(CloudPopupPath));
        }

        private static void AssertRgb(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.005f, message + " r");
            Assert.AreEqual(expected.g, actual.g, 0.005f, message + " g");
            Assert.AreEqual(expected.b, actual.b, 0.005f, message + " b");
        }

        private static string FullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static GameObject OriginOf(Component popup, string scrollViewField)
        {
            Component scrollView = Required<Component>(popup, scrollViewField);
            return Required<GameObject>(scrollView, "_origin");
        }

        private static string SourcePrefabPath(GameObject originInstance)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(originInstance);
            Assert.IsTrue(source != null, originInstance.name + " 은 프리팹 인스턴스가 아님");
            return AssetDatabase.GetAssetPath(source);
        }

        private static QuestCellData QuestData(bool achieved, bool hasProgress, int current = 3, int target = 10)
        {
            return new QuestCellData
            {
                DisplayName = "테스트 도전과제",
                Description = "설명",
                RewardText = "+30 소울",
                Achieved = achieved,
                HasProgress = hasProgress,
                Current = current,
                Target = target,
            };
        }

        private static RankingRowEntry RankEntry(long rank, bool mine = false)
        {
            return new RankingRowEntry
            {
                Row = new RankingRowDto { rank = rank, displayName = "테스터", clearTimeMs = 92500, hero = "Knight", uid = "uid" },
                IsMine = mine,
            };
        }

        private static LordRewardCellData LordData(bool reached, bool isCurrent, bool dummy = false)
        {
            return new LordRewardCellData
            {
                LevelText = "Lv 3",
                DisplayName = "보상",
                RewardText = dummy ? string.Empty : "+50 소울",
                Reached = reached,
                IsCurrent = isCurrent,
                IsLockedDummy = dummy,
                SubText = isCurrent ? "현재 레벨" : string.Empty,
            };
        }

        //# --- ① Missing Script ---

        [TestCase(QuestPopupPath)]
        [TestCase(QuestCellPath)]
        [TestCase(LordPopupPath)]
        [TestCase(LordCellPath)]
        [TestCase(RankingPopupPath)]
        [TestCase(RankingCellPath)]
        [TestCase(CloudPopupPath)]
        public void 프리팹_YAML에_Missing_Script_참조가_없다(string path)
        {
            string full = FullPath(path);
            Assert.IsTrue(File.Exists(full), full);
            MatchCollection missing = Regex.Matches(File.ReadAllText(full), @"m_Script:\s*\{fileID:\s*0\}");
            Assert.AreEqual(0, missing.Count, path + " 의 m_Script fileID 0 (Missing Script)");
        }

        [TestCase(QuestPopupPath)]
        [TestCase(QuestCellPath)]
        [TestCase(LordPopupPath)]
        [TestCase(LordCellPath)]
        [TestCase(RankingPopupPath)]
        [TestCase(RankingCellPath)]
        [TestCase(CloudPopupPath)]
        public void 프리팹_모든_오브젝트에_Missing_Script와_null_컴포넌트가_없다(string path)
        {
            foreach (Transform t in LoadPrefabAsset(path).GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), $"{path} {t.name} Missing Script 수");
                foreach (Component c in t.GetComponents<Component>())
                {
                    Assert.IsTrue(c != null, $"{path} {t.name} 에 null 컴포넌트");
                }
            }
        }

        //# --- ② 신규 필드 연결 · 기존 필드 유지 ---

        [Test]
        public void QuestCell_achievedDecor가_DecoAchieved로_연결되고_프리팹_기본_비활성이다()
        {
            QuestCell cell = Comp<QuestCell>(LoadPrefabAsset(QuestCellPath));

            GameObject decor = Required<GameObject>(cell, "_achievedDecor");

            Assert.AreEqual("DecoAchieved", decor.name);
            Assert.IsFalse(decor.activeSelf, "기본 비활성");
            Assert.IsTrue(decor.transform.IsChildOf(cell.transform), "셀 하위");
        }

        [Test]
        public void QuestCell_기존_위젯_필드가_유지된다()
        {
            QuestCell cell = Comp<QuestCell>(LoadPrefabAsset(QuestCellPath));
            string[] names =
            {
                "_background", "_nameText", "_descText", "_rewardText", "_achievedBadge",
                "_progressRoot", "_progressFill", "_progressText", "_normalSprite", "_badgeBg",
            };

            foreach (string name in names)
            {
                Required<UnityEngine.Object>(cell, name);
            }
        }

        [Test]
        public void RankingCell_rankMedalRoot가_DecoRankMedal이고_기본_비활성이다()
        {
            RankingCell cell = Comp<RankingCell>(LoadPrefabAsset(RankingCellPath));

            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");

            Assert.AreEqual("DecoRankMedal", root.name);
            Assert.IsFalse(root.activeSelf, "기본 비활성");
        }

        [Test]
        public void RankingCell_rankMedalTinted가_길이_2와_Glow_Disc_순서_프리팹_알파로_연결된다()
        {
            RankingCell cell = Comp<RankingCell>(LoadPrefabAsset(RankingCellPath));
            Image[] tinted = Required<Image[]>(cell, "_rankMedalTinted");
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");
            string[] names = { "DecoMedalGlow", "DecoMedalDisc" };
            float[] alphas = { 0.50f, 1.0f };

            Assert.AreEqual(2, tinted.Length, "_rankMedalTinted 길이");
            for (int i = 0; i < tinted.Length; ++i)
            {
                Assert.IsTrue(tinted[i] != null, $"_rankMedalTinted[{i}] 비어 있음");
                Assert.AreEqual(names[i], tinted[i].name, $"_rankMedalTinted[{i}] 이름");
                Assert.AreEqual(alphas[i], tinted[i].color.a, 0.01f, names[i] + " 프리팹 알파");
                Assert.IsTrue(tinted[i].transform.IsChildOf(root.transform), names[i] + " 은 DecoRankMedal 하위");
            }
        }

        [Test]
        public void RankingCell_기존_위젯_필드가_유지된다()
        {
            RankingCell cell = Comp<RankingCell>(LoadPrefabAsset(RankingCellPath));
            string[] names =
            {
                "_background", "_rankText", "_nameText", "_timeText", "_heroText",
                "_normalSprite", "_mineSprite", "_mineRing",
            };

            foreach (string name in names)
            {
                Required<UnityEngine.Object>(cell, name);
            }
        }

        [Test]
        public void LordRewardCell_기존_위젯_필드가_유지된다()
        {
            LordRewardCell cell = Comp<LordRewardCell>(LoadPrefabAsset(LordCellPath));
            string[] names =
            {
                "_background", "_levelText", "_nameText", "_rewardText", "_reachedBadge", "_subText",
                "_stateRing", "_normalSprite", "_pendingSprite", "_badgeBg", "_badgeSoulSprite", "_badgeGoldSprite",
            };

            foreach (string name in names)
            {
                Required<UnityEngine.Object>(cell, name);
            }
        }

        [Test]
        public void CloudPopup_connectionGlow가_DecoConnectionGlow로_ConnectionDot_바로_앞_형제에_연결된다()
        {
            CloudPopup popup = Comp<CloudPopup>(LoadPrefabAsset(CloudPopupPath));
            Image glow = Required<Image>(popup, "_connectionGlow");
            Image dot = Required<Image>(popup, "_connectionDot");

            Assert.AreEqual("DecoConnectionGlow", glow.name);
            Assert.AreSame(glow.transform.parent, dot.transform.parent, "같은 부모");
            Assert.AreEqual(dot.transform.GetSiblingIndex() - 1, glow.transform.GetSiblingIndex(), "ConnectionDot 바로 앞");
        }

        [Test]
        public void CloudPopup_기존_기능_필드가_유지된다()
        {
            CloudPopup popup = Comp<CloudPopup>(LoadPrefabAsset(CloudPopupPath));
            string[] names =
            {
                "_connectionText", "_displayNameText", "_changeNameButton", "_nameInput", "_nameConfirmButton",
                "_nameCancelButton", "_nameEditGroup", "_restoreButton", "_conflictGroup", "_conflictText",
                "_conflictRestoreButton", "_conflictLaterButton", "_conflictDot", "_connectionDot", "_conflictCompare",
                "_conflictLocalText", "_conflictCloudText", "_backgroundButton", "_backButton",
            };

            foreach (string name in names)
            {
                Required<UnityEngine.Object>(popup, name);
            }
        }

        [Test]
        public void QuestPopup_LordLevelPopup_RankingPopup_기존_필드가_유지된다()
        {
            QuestPopup quest = Comp<QuestPopup>(LoadPrefabAsset(QuestPopupPath));
            foreach (string name in new[] { "_dimButton", "_closeButton", "_scrollView" })
            {
                Required<UnityEngine.Object>(quest, name);
            }

            LordLevelPopup lord = Comp<LordLevelPopup>(LoadPrefabAsset(LordPopupPath));
            foreach (string name in new[] { "_dimButton", "_closeButton", "_lordLevelText", "_xpFill", "_xpNextText", "_scrollView" })
            {
                Required<UnityEngine.Object>(lord, name);
            }

            RankingPopup ranking = Comp<RankingPopup>(LoadPrefabAsset(RankingPopupPath));
            foreach (string name in new[] { "_scrollView", "_emptyText", "_myRankContainer", "_myRankCell", "_overallTab", "_backgroundButton", "_backButton" })
            {
                Required<UnityEngine.Object>(ranking, name);
            }
            Toggle[] tabs = Required<Toggle[]>(ranking, "_stageTabs");
            Assert.AreEqual(5, tabs.Length, "_stageTabs 길이");
            for (int i = 0; i < tabs.Length; ++i)
            {
                Assert.IsTrue(tabs[i] != null, $"_stageTabs[{i}] 비어 있음");
            }
        }

        //# --- ③ QuestCell 상태 전환 ---

        [Test]
        public void QuestCell_진행중_Bind는_달성_장식과_배지를_끄고_진행바를_켠다()
        {
            QuestCell cell = NewQuestCell();
            GameObject decor = Required<GameObject>(cell, "_achievedDecor");
            GameObject badge = Required<GameObject>(cell, "_badgeBg");
            GameObject progress = Required<GameObject>(cell, "_progressRoot");
            Image fill = Required<Image>(cell, "_progressFill");

            cell.Bind(QuestData(false, true));

            Assert.IsFalse(decor.activeSelf, "달성 장식");
            Assert.IsFalse(badge.activeSelf, "배지");
            Assert.IsTrue(progress.activeSelf, "진행 바");
            Assert.AreEqual(0.3f, fill.fillAmount, 0.001f, "fillAmount");
        }

        [Test]
        public void QuestCell_달성_Bind는_달성_장식과_배지를_켜고_진행바를_끄며_루트를_틴트한다()
        {
            QuestCell cell = NewQuestCell();
            GameObject decor = Required<GameObject>(cell, "_achievedDecor");
            GameObject badge = Required<GameObject>(cell, "_badgeBg");
            GameObject progress = Required<GameObject>(cell, "_progressRoot");
            Image background = Required<Image>(cell, "_background");

            cell.Bind(QuestData(true, false));

            Assert.IsTrue(decor.activeSelf, "달성 장식");
            Assert.IsTrue(badge.activeSelf, "배지");
            Assert.IsFalse(progress.activeSelf, "진행 바");
            AssertRgb(new Color(0.72f, 1f, 0.84f), background.color, "달성 틴트");
        }

        [Test]
        public void QuestCell_달성_후_진행중_재_Bind하면_달성_장식_잔상과_틴트가_남지_않는다()
        {
            QuestCell cell = NewQuestCell();
            GameObject decor = Required<GameObject>(cell, "_achievedDecor");
            Image background = Required<Image>(cell, "_background");

            cell.Bind(QuestData(true, false));
            cell.Bind(QuestData(false, true));

            Assert.IsFalse(decor.activeSelf, "달성 장식 잔상");
            AssertRgb(Color.white, background.color, "루트 색 복귀");
        }

        [Test]
        public void QuestCell_달성과_진행중을_반복_Bind해도_장식이_항상_상태를_따른다()
        {
            QuestCell cell = NewQuestCell();
            GameObject decor = Required<GameObject>(cell, "_achievedDecor");
            bool[] sequence = { true, false, false, true, true, false, true, false };

            foreach (bool achieved in sequence)
            {
                cell.Bind(QuestData(achieved, achieved == false));
                Assert.AreEqual(achieved, decor.activeSelf, $"achieved={achieved}");
            }
        }

        [Test]
        public void QuestCell_진행바_없는_미달성_단발은_진행바_배지_장식이_모두_꺼진다()
        {
            QuestCell cell = NewQuestCell();
            GameObject decor = Required<GameObject>(cell, "_achievedDecor");
            GameObject badge = Required<GameObject>(cell, "_badgeBg");
            GameObject progress = Required<GameObject>(cell, "_progressRoot");

            cell.Bind(QuestData(true, false));
            cell.Bind(QuestData(false, false));

            Assert.IsFalse(decor.activeSelf, "장식");
            Assert.IsFalse(badge.activeSelf, "배지");
            Assert.IsFalse(progress.activeSelf, "진행 바");
        }

        [Test]
        public void QuestCell_Bind_null이면_장식_상태를_건드리지_않는다()
        {
            QuestCell cell = NewQuestCell();
            GameObject decor = Required<GameObject>(cell, "_achievedDecor");
            cell.Bind(QuestData(true, false));

            Assert.DoesNotThrow(() => cell.Bind(null));

            Assert.IsTrue(decor.activeSelf, "null Bind 는 조기 반환");
        }

        [Test]
        public void QuestCell_achievedDecor가_비어_있어도_Bind가_예외없다()
        {
            QuestCell cell = NewQuestCell();
            SetField(cell, "_achievedDecor", null);

            Assert.DoesNotThrow(() => cell.Bind(QuestData(true, false)));
            Assert.DoesNotThrow(() => cell.Bind(QuestData(false, true)));
        }

        [Test]
        public void QuestCell_위젯_필드가_전부_비어_있어도_Bind가_예외없다()
        {
            GameObject go = new GameObject("EmptyQuestCell");
            _spawned.Add(go);
            QuestCell cell = go.AddComponent<QuestCell>();

            Assert.DoesNotThrow(() => cell.Bind(QuestData(true, false)));
            Assert.DoesNotThrow(() => cell.Bind(QuestData(false, true)));
            Assert.DoesNotThrow(() => cell.Bind(null));
        }

        //# --- ④ RankingCell 메달 ---

        [TestCase(1L)]
        [TestCase(2L)]
        [TestCase(3L)]
        public void RankingCell_1_3위는_메달을_켜고_순위색_rgb로_틴트하며_알파를_보존한다(long rank)
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");
            Image[] tinted = Required<Image[]>(cell, "_rankMedalTinted");
            float[] alphaBefore = { tinted[0].color.a, tinted[1].color.a };
            Color expected = rank == 1 ? PodiumGold : rank == 2 ? PodiumSilver : PodiumBronze;

            cell.Bind(RankEntry(rank));

            Assert.IsTrue(root.activeSelf, "메달 켜짐");
            for (int i = 0; i < tinted.Length; ++i)
            {
                AssertRgb(expected, tinted[i].color, $"rank {rank} {tinted[i].name}");
                Assert.AreEqual(alphaBefore[i], tinted[i].color.a, 0.001f, tinted[i].name + " 알파 보존");
            }
            Assert.AreEqual(0.50f, tinted[0].color.a, 0.01f, "DecoMedalGlow 알파 0.50");
            Assert.AreEqual(1.0f, tinted[1].color.a, 0.01f, "DecoMedalDisc 알파 1.0");
        }

        [TestCase(4L)]
        [TestCase(5L)]
        [TestCase(100L)]
        [TestCase(0L)]
        [TestCase(-1L)]
        public void RankingCell_4위_이하와_범위_밖_순위는_메달이_꺼진다(long rank)
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");

            cell.Bind(RankEntry(rank));

            Assert.IsFalse(root.activeSelf, $"rank {rank}");
        }

        [Test]
        public void RankingCell_1위_후_4위로_재_Bind하면_메달_잔상이_없다()
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");

            cell.Bind(RankEntry(1));
            Assert.IsTrue(root.activeSelf, "사전: 1위 메달");
            cell.Bind(RankEntry(4));

            Assert.IsFalse(root.activeSelf, "4위 재사용");
        }

        [Test]
        public void RankingCell_순위를_반복_전환해도_메달_틴트가_마지막_순위를_따른다()
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");
            Image[] tinted = Required<Image[]>(cell, "_rankMedalTinted");

            cell.Bind(RankEntry(1));
            cell.Bind(RankEntry(2));
            AssertRgb(PodiumSilver, tinted[1].color, "2위");
            cell.Bind(RankEntry(7));
            Assert.IsFalse(root.activeSelf, "7위");
            cell.Bind(RankEntry(3));
            Assert.IsTrue(root.activeSelf, "3위");
            AssertRgb(PodiumBronze, tinted[0].color, "3위 Glow");
            AssertRgb(PodiumBronze, tinted[1].color, "3위 Disc");
            Assert.AreEqual(0.50f, tinted[0].color.a, 0.001f, "Glow 알파");
            Assert.AreEqual(1.0f, tinted[1].color.a, 0.001f, "Disc 알파");
        }

        [Test]
        public void RankingCell_1위_직후_Row가_null인_entry를_Bind해도_메달이_남지_않는다()
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");

            cell.Bind(RankEntry(1));
            Assert.IsTrue(root.activeSelf, "사전: 1위 메달");
            cell.Bind(new RankingRowEntry { Row = null });

            Assert.IsFalse(root.activeSelf, "Row==null 경로");
        }

        [Test]
        public void RankingCell_1위_직후_미등재_entry를_Bind하면_메달이_꺼진다()
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");

            cell.Bind(RankEntry(1));
            cell.Bind(new RankingRowEntry { Unranked = true, UnrankedName = "나", IsMine = true });

            Assert.IsFalse(root.activeSelf, "미등재");
        }

        [Test]
        public void RankingCell_entry가_null이면_메달_상태를_건드리지_않는다()
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");
            cell.Bind(RankEntry(2));

            Assert.DoesNotThrow(() => cell.Bind(null));

            Assert.IsTrue(root.activeSelf, "null Bind 는 조기 반환");
        }

        [Test]
        public void RankingCell_OnEnable_풀_재사용_리셋은_메달을_끈다()
        {
            RankingCell cell = NewRankingCell();
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");

            //# EditMode 에서는 OnEnable 이 자동 호출되지 않아 리플렉션으로 직접 호출한다.
            cell.Bind(RankEntry(1));
            Assert.IsTrue(root.activeSelf, "사전: 1위 메달");
            InvokeNonPublic(cell, "OnEnable");

            Assert.IsFalse(root.activeSelf, "OnEnable 리셋");
        }

        [Test]
        public void RankingCell_내_행_여부에_따라_MineRing이_토글되고_메달과_독립이다()
        {
            RankingCell cell = NewRankingCell();
            Image ring = Required<Image>(cell, "_mineRing");
            GameObject root = Required<GameObject>(cell, "_rankMedalRoot");

            cell.Bind(RankEntry(1, true));
            Assert.IsTrue(ring.gameObject.activeSelf, "내 행 링");
            Assert.IsTrue(root.activeSelf, "내 행 1위 메달");
            AssertRgb(UiDotPalette.Soul, ring.color, "링 색");
            cell.Bind(RankEntry(1, false));

            Assert.IsFalse(ring.gameObject.activeSelf, "남의 행 링");
            Assert.IsTrue(root.activeSelf, "남의 행 1위 메달");
        }

        [Test]
        public void RankingCell_신규_필드가_비어_있어도_Bind와_OnEnable이_예외없다()
        {
            RankingCell cell = NewRankingCell();
            SetField(cell, "_rankMedalRoot", null);
            SetField(cell, "_rankMedalTinted", null);

            Assert.DoesNotThrow(() => InvokeNonPublic(cell, "OnEnable"), "OnEnable");
            Assert.DoesNotThrow(() => cell.Bind(RankEntry(1)), "1위");
            Assert.DoesNotThrow(() => cell.Bind(RankEntry(9)), "9위");
            Assert.DoesNotThrow(() => cell.Bind(new RankingRowEntry { Unranked = true }), "미등재");
        }

        [Test]
        public void RankingCell_메달_틴트_배열에_null_원소가_있어도_나머지는_칠한다()
        {
            RankingCell cell = NewRankingCell();
            Image[] original = Required<Image[]>(cell, "_rankMedalTinted");
            SetField(cell, "_rankMedalTinted", new Image[] { null, original[1] });

            Assert.DoesNotThrow(() => cell.Bind(RankEntry(1)));

            AssertRgb(PodiumGold, original[1].color, "null 을 건너뛴 Disc");
            SetField(cell, "_rankMedalTinted", new Image[0]);
            Assert.DoesNotThrow(() => cell.Bind(RankEntry(2)), "빈 배열");
        }

        [Test]
        public void RankingCell_위젯_필드가_전부_비어_있어도_Bind가_예외없다()
        {
            GameObject go = new GameObject("EmptyRankingCell");
            _spawned.Add(go);
            RankingCell cell = go.AddComponent<RankingCell>();

            Assert.DoesNotThrow(() => cell.Bind(RankEntry(1)));
            Assert.DoesNotThrow(() => cell.Bind(new RankingRowEntry { Row = null }));
            Assert.DoesNotThrow(() => cell.Bind(new RankingRowEntry { Unranked = true }));
            Assert.DoesNotThrow(() => cell.Bind(null));
        }

        //# --- ⑤ LordRewardCell 배지 색 ---

        [Test]
        public void LordRewardCell_현재_레벨_배지는_Gold다()
        {
            LordRewardCell cell = NewLordCell();
            Image badge = Required<Image>(cell, "_badgeBg");

            cell.Bind(LordData(true, true));

            Assert.IsTrue(badge.gameObject.activeSelf, "배지 켜짐");
            AssertRgb(UiDotPalette.Gold, badge.color, "배지");
            Assert.AreEqual(1f, badge.color.a, 0.001f, "배지 알파");
        }

        [Test]
        public void LordRewardCell_수령_완료_배지는_Soul이다()
        {
            LordRewardCell cell = NewLordCell();
            Image badge = Required<Image>(cell, "_badgeBg");

            cell.Bind(LordData(true, false));

            Assert.IsTrue(badge.gameObject.activeSelf, "배지 켜짐");
            AssertRgb(UiDotPalette.Soul, badge.color, "배지");
            Assert.AreEqual(1f, badge.color.a, 0.001f, "배지 알파");
        }

        [Test]
        public void LordRewardCell_현재_레벨에서_수령_완료로_재_Bind하면_배지가_Soul로_복귀하고_링이_꺼진다()
        {
            LordRewardCell cell = NewLordCell();
            Image badge = Required<Image>(cell, "_badgeBg");
            Image ring = Required<Image>(cell, "_stateRing");

            cell.Bind(LordData(true, true));
            AssertRgb(UiDotPalette.Gold, badge.color, "사전: 현재");
            Assert.IsTrue(ring.gameObject.activeSelf, "사전: 링");
            cell.Bind(LordData(true, false));

            AssertRgb(UiDotPalette.Soul, badge.color, "복귀");
            Assert.IsFalse(ring.gameObject.activeSelf, "링 꺼짐");
        }

        [Test]
        public void LordRewardCell_미도달과_잠금_더미는_배지가_꺼진다()
        {
            LordRewardCell cell = NewLordCell();
            Image badge = Required<Image>(cell, "_badgeBg");

            cell.Bind(LordData(true, true));
            cell.Bind(LordData(false, false));
            Assert.IsFalse(badge.gameObject.activeSelf, "미도달");

            cell.Bind(LordData(true, true));
            cell.Bind(LordData(false, false, true));
            Assert.IsFalse(badge.gameObject.activeSelf, "잠금 더미");

            cell.Bind(LordData(true, false, true));
            Assert.IsFalse(badge.gameObject.activeSelf, "Reached 더미");
        }

        [Test]
        public void LordRewardCell_배지_스프라이트_슬롯이_비어도_배지_색은_칠해진다()
        {
            LordRewardCell cell = NewLordCell();
            Image badge = Required<Image>(cell, "_badgeBg");
            SetField(cell, "_badgeSoulSprite", null);
            SetField(cell, "_badgeGoldSprite", null);

            Assert.DoesNotThrow(() => cell.Bind(LordData(true, true)));

            AssertRgb(UiDotPalette.Gold, badge.color, "스프라이트 없이도 색");
        }

        [Test]
        public void LordRewardCell_배지_위젯이_비어_있어도_Bind가_예외없다()
        {
            LordRewardCell cell = NewLordCell();
            SetField(cell, "_badgeBg", null);

            Assert.DoesNotThrow(() => cell.Bind(LordData(true, true)));
            Assert.DoesNotThrow(() => cell.Bind(LordData(true, false)));
            Assert.DoesNotThrow(() => cell.Bind(null));
        }

        [Test]
        public void LordRewardCell_위젯_필드가_전부_비어_있어도_Bind가_예외없다()
        {
            GameObject go = new GameObject("EmptyLordCell");
            _spawned.Add(go);
            LordRewardCell cell = go.AddComponent<LordRewardCell>();

            Assert.DoesNotThrow(() => cell.Bind(LordData(true, true)));
            Assert.DoesNotThrow(() => cell.Bind(LordData(false, false, true)));
        }

        //# --- ⑥ CloudPopup 연결 발광 ---

        [Test]
        public void CloudPopup_연결됨이면_점과_발광이_Soul_rgb이고_발광_알파가_보존된다()
        {
            CloudPopup popup = NewCloudPopup();
            Image dot = Required<Image>(popup, "_connectionDot");
            Image glow = Required<Image>(popup, "_connectionGlow");
            float alphaBefore = glow.color.a;

            popup.InitUI(new CloudPopupArg { IsConnected = true, DisplayName = "영주" });

            AssertRgb(UiDotPalette.Soul, dot.color, "점");
            AssertRgb(UiDotPalette.Soul, glow.color, "발광");
            AssertRgb(dot.color, glow.color, "점과 발광 rgb 동일");
            Assert.AreEqual(alphaBefore, glow.color.a, 0.001f, "발광 알파 보존");
            Assert.AreEqual(0.50f, glow.color.a, 0.01f, "발광 알파 0.50");
        }

        [Test]
        public void CloudPopup_오프라인이면_점과_발광이_Blood_rgb이고_발광_알파가_보존된다()
        {
            CloudPopup popup = NewCloudPopup();
            Image dot = Required<Image>(popup, "_connectionDot");
            Image glow = Required<Image>(popup, "_connectionGlow");
            float alphaBefore = glow.color.a;

            popup.InitUI(new CloudPopupArg { IsConnected = false, DisplayName = "영주" });

            AssertRgb(UiDotPalette.Blood, dot.color, "점");
            AssertRgb(UiDotPalette.Blood, glow.color, "발광");
            Assert.AreEqual(alphaBefore, glow.color.a, 0.001f, "발광 알파 보존");
        }

        [Test]
        public void CloudPopup_연결과_오프라인을_반복_전환해도_발광_알파가_변하지_않는다()
        {
            CloudPopup popup = NewCloudPopup();
            Image glow = Required<Image>(popup, "_connectionGlow");
            float alphaBefore = glow.color.a;
            bool[] sequence = { true, false, false, true, false };

            foreach (bool connected in sequence)
            {
                popup.InitUI(new CloudPopupArg { IsConnected = connected, DisplayName = "영주" });
                AssertRgb(connected ? UiDotPalette.Soul : UiDotPalette.Blood, glow.color, $"connected={connected}");
                Assert.AreEqual(alphaBefore, glow.color.a, 0.001f, $"connected={connected} 알파");
            }
        }

        [Test]
        public void CloudPopup_충돌_대기_여부에_따라_충돌_그룹과_점이_토글된다()
        {
            CloudPopup popup = NewCloudPopup();
            GameObject group = Required<GameObject>(popup, "_conflictGroup");
            GameObject dot = Required<GameObject>(popup, "_conflictDot");
            Image groupImage = group.GetComponent<Image>();

            popup.InitUI(new CloudPopupArg { IsConnected = true, ConflictPending = true });
            Assert.IsTrue(group.activeSelf, "충돌 그룹 켜짐");
            Assert.IsTrue(dot.activeSelf, "충돌 점 켜짐");
            Assert.IsTrue(groupImage != null, "ConflictGroup Image");
            Assert.AreEqual(1f, groupImage.color.a, 0.001f, "ConflictGroup 불투명 유지");

            popup.InitUI(new CloudPopupArg { IsConnected = true, ConflictPending = false });
            Assert.IsFalse(group.activeSelf, "충돌 그룹 꺼짐");
            Assert.IsFalse(dot.activeSelf, "충돌 점 꺼짐");
        }

        //# --- ⑩ 빈 필드 안전 (Cloud) ---

        [Test]
        public void CloudPopup_connectionGlow가_비어_있어도_InitUI가_예외없고_점은_칠해진다()
        {
            CloudPopup popup = NewCloudPopup();
            Image dot = Required<Image>(popup, "_connectionDot");
            SetField(popup, "_connectionGlow", null);

            Assert.DoesNotThrow(() => popup.InitUI(new CloudPopupArg { IsConnected = false }));

            AssertRgb(UiDotPalette.Blood, dot.color, "점");
        }

        [Test]
        public void CloudPopup_connectionDot이_비어_있어도_발광은_독립적으로_칠해진다()
        {
            CloudPopup popup = NewCloudPopup();
            Image glow = Required<Image>(popup, "_connectionGlow");
            SetField(popup, "_connectionDot", null);

            Assert.DoesNotThrow(() => popup.InitUI(new CloudPopupArg { IsConnected = true }));

            AssertRgb(UiDotPalette.Soul, glow.color, "발광");
        }

        [Test]
        public void CloudPopup_arg가_null이어도_InitUI가_예외없다()
        {
            CloudPopup popup = NewCloudPopup();

            Assert.DoesNotThrow(() => popup.InitUI(null));
        }

        [Test]
        public void CloudPopup_위젯_필드가_전부_비어_있어도_InitUI가_예외없다()
        {
            GameObject go = new GameObject("EmptyCloudPopup");
            _spawned.Add(go);
            CloudPopup popup = go.AddComponent<CloudPopup>();

            Assert.DoesNotThrow(() => popup.InitUI(new CloudPopupArg { IsConnected = true }));
            Assert.DoesNotThrow(() => popup.InitUI(new CloudPopupArg { IsConnected = false, ConflictPending = true }));
        }

        //# --- ⑦ CHPoolingScrollView _origin 보존 · 셀 오버라이드 없음 · m_RemovedComponents ---

        [Test]
        public void QuestPopup_origin이_QuestCell_프리팹_인스턴스를_가리킨다()
        {
            QuestPopup popup = Comp<QuestPopup>(LoadPrefabAsset(QuestPopupPath));

            GameObject origin = OriginOf(popup, "_scrollView");

            Assert.IsTrue(origin.GetComponent<QuestCell>() != null, "origin 에 QuestCell");
            Assert.AreEqual(QuestCellPath, SourcePrefabPath(origin), "origin 원본 프리팹");
        }

        [Test]
        public void LordLevelPopup_origin이_LordRewardCell_프리팹_인스턴스를_가리킨다()
        {
            LordLevelPopup popup = Comp<LordLevelPopup>(LoadPrefabAsset(LordPopupPath));

            GameObject origin = OriginOf(popup, "_scrollView");

            Assert.IsTrue(origin.GetComponent<LordRewardCell>() != null, "origin 에 LordRewardCell");
            Assert.AreEqual(LordCellPath, SourcePrefabPath(origin), "origin 원본 프리팹");
        }

        [Test]
        public void RankingPopup_origin이_RankingCell_프리팹_인스턴스를_가리키고_내_순위_셀과_다른_인스턴스다()
        {
            RankingPopup popup = Comp<RankingPopup>(LoadPrefabAsset(RankingPopupPath));

            GameObject origin = OriginOf(popup, "_scrollView");
            RankingCell myCell = Required<RankingCell>(popup, "_myRankCell");

            Assert.IsTrue(origin.GetComponent<RankingCell>() != null, "origin 에 RankingCell");
            Assert.AreEqual(RankingCellPath, SourcePrefabPath(origin), "origin 원본 프리팹");
            Assert.AreEqual(RankingCellPath, SourcePrefabPath(myCell.gameObject), "내 순위 셀 원본 프리팹");
            Assert.AreNotSame(origin, myCell.gameObject, "origin 과 내 순위 셀은 별도 인스턴스");
        }

        [TestCase(QuestPopupPath, 1)]
        [TestCase(LordPopupPath, 1)]
        [TestCase(RankingPopupPath, 2)]
        public void 팝업_프리팹의_셀_인스턴스는_m_RemovedComponents가_비어_있다(string path, int instanceCount)
        {
            string yaml = File.ReadAllText(FullPath(path));

            Assert.AreEqual(instanceCount, Regex.Matches(yaml, @"^PrefabInstance:", RegexOptions.Multiline).Count, path + " PrefabInstance 수");
            Assert.AreEqual(instanceCount, Regex.Matches(yaml, @"m_RemovedComponents:").Count, path + " m_RemovedComponents 키 수");
            Assert.AreEqual(0, Regex.Matches(yaml, @"m_RemovedComponents:\s*\r?\n\s*-").Count, path + " 제거된 컴포넌트 존재(참조 null 위험)");
            Assert.AreEqual(0, Regex.Matches(yaml, @"m_RemovedGameObjects:\s*\r?\n\s*-").Count, path + " 제거된 GameObject 존재");
        }

        [TestCase(QuestPopupPath)]
        [TestCase(LordPopupPath)]
        [TestCase(RankingPopupPath)]
        public void 팝업_프리팹의_셀_인스턴스에_스프라이트_색_필드_오버라이드와_추가_컴포넌트가_없다(string path)
        {
            string yaml = File.ReadAllText(FullPath(path));

            Assert.AreEqual(0, Regex.Matches(yaml, @"propertyPath:\s*(m_Sprite|m_Color|m_Material|m_Type|m_FillAmount|_)").Count, path + " 셀 시각 값 오버라이드(셀 변경은 셀 프리팹에만)");
            Assert.AreEqual(0, Regex.Matches(yaml, @"m_AddedComponents:\s*\r?\n\s*-").Count, path + " 추가 컴포넌트 오버라이드");
            Assert.AreEqual(0, Regex.Matches(yaml, @"m_AddedGameObjects:\s*\r?\n\s*-").Count, path + " 추가 GameObject 오버라이드");
        }

        //# --- ⑧ Deco raycastTarget ---

        [TestCase(QuestPopupPath)]
        [TestCase(QuestCellPath)]
        [TestCase(LordPopupPath)]
        [TestCase(LordCellPath)]
        [TestCase(RankingPopupPath)]
        [TestCase(RankingCellPath)]
        [TestCase(CloudPopupPath)]
        public void Deco로_시작하는_오브젝트의_Graphic은_raycastTarget이_꺼져_있다(string path)
        {
            int decoCount = 0;
            foreach (Graphic graphic in LoadPrefabAsset(path).GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.name.StartsWith("Deco") == false)
                    continue;

                ++decoCount;
                Assert.IsFalse(graphic.raycastTarget, $"{path} {graphic.name} raycastTarget");
            }
            Assert.Greater(decoCount, 0, path + " Deco Graphic 수집 확인");
        }

        [TestCase(QuestPopupPath)]
        [TestCase(LordPopupPath)]
        [TestCase(RankingPopupPath)]
        public void 팝업의_Scrollbar와_Handle과_Dim_닫기버튼은_raycastTarget이_켜져_있다(string path)
        {
            int scrollbars = 0;
            int handles = 0;
            int dims = 0;
            int closes = 0;
            foreach (Graphic graphic in LoadPrefabAsset(path).GetComponentsInChildren<Graphic>(true))
            {
                switch (graphic.name)
                {
                    case "Scrollbar":
                        ++scrollbars;
                        Assert.IsTrue(graphic.raycastTarget, path + " Scrollbar raycastTarget");
                        break;
                    case "Handle":
                        ++handles;
                        Assert.IsTrue(graphic.raycastTarget, path + " Handle raycastTarget");
                        break;
                    case "Dim":
                        ++dims;
                        Assert.IsTrue(graphic.raycastTarget, path + " Dim raycastTarget");
                        break;
                    case "CloseButton":
                        ++closes;
                        Assert.IsTrue(graphic.raycastTarget, path + " CloseButton raycastTarget");
                        break;
                }
            }
            Assert.AreEqual(1, scrollbars, "Scrollbar Graphic 수");
            Assert.AreEqual(1, handles, "Handle Graphic 수");
            Assert.AreEqual(1, dims, "Dim Graphic 수");
            Assert.AreEqual(1, closes, "CloseButton Graphic 수");
        }

        [Test]
        public void 계정_팝업의_Dim과_CloseButton은_raycastTarget이_켜져_있다()
        {
            int dims = 0;
            int closes = 0;
            foreach (Graphic graphic in LoadPrefabAsset(CloudPopupPath).GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.name == "Dim")
                {
                    ++dims;
                    Assert.IsTrue(graphic.raycastTarget, "Dim raycastTarget");
                }
                else if (graphic.name == "CloseButton")
                {
                    ++closes;
                    Assert.IsTrue(graphic.raycastTarget, "CloseButton raycastTarget");
                }
            }
            Assert.AreEqual(1, dims, "Dim Graphic 수");
            Assert.AreEqual(1, closes, "CloseButton Graphic 수");
        }

        //# --- ⑨ 일회용 빌더 없음 ---

        [Test]
        public void 일회용_장식_빌더_스크립트가_레포에_없다()
        {
            string editorDir = Path.GetFullPath(Path.Combine(Application.dataPath, "_Lair", "Editor"));
            Assert.IsTrue(Directory.Exists(editorDir), editorDir);

            string[] polishC = Directory.GetFiles(editorDir, "PolishC*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, polishC.Length, "PolishC 에디터 스크립트 잔존: " + string.Join(", ", polishC));
            string[] decoBuilders = Directory.GetFiles(editorDir, "*Deco*Builder*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, decoBuilders.Length, "Deco 빌더 잔존: " + string.Join(", ", decoBuilders));
        }
    }
}
