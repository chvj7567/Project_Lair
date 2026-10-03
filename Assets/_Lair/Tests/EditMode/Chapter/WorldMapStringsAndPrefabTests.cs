using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 월드맵 문구 테이블(Strings_Ko 1301~1326·id 11 등) 무결성과 WorldMapPopup 프리팹 필수 참조 스모크 (chapter-system §6.5).
    public class WorldMapStringsAndPrefabTests : ChapterStaticStateTestBase
    {
        private const string PopupPrefabPath = "Assets/_Lair/Art/UI/WorldMapPopup.prefab";

        //# --- Strings_Ko ---

        [Test]
        public void 문구_id는_오름차순이고_중복이_없다()
        {
            StringEntry[] entries = JsonArrayUtility.FromJsonArray<StringEntry>(File.ReadAllText(WorldMapTestSupport.JsonDir + "Strings_Ko.json"));

            for (int i = 1; i < entries.Length; ++i)
            {
                Assert.Less(entries[i - 1].id, entries[i].id, $"위치 {i}: {entries[i - 1].id} → {entries[i].id}");
            }
        }

        [Test]
        public void id_11은_지도다()
        {
            Assert.AreEqual("지도", WorldMapTestSupport.Text(11));
        }

        [Test]
        public void 월드맵_문구_1301부터_1326까지가_모두_존재하고_비어_있지_않다()
        {
            for (int id = 1301; id <= 1326; ++id)
            {
                Assert.IsTrue(WorldMapTestSupport.Strings().ContainsKey(id), $"id {id} 없음");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id} 비어 있음");
            }
        }

        [Test]
        public void 규칙명_1201_1206과_설명_1221_1226과_봉인_알림_1231이_존재한다()
        {
            for (int id = 1201; id <= 1206; ++id)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"규칙명 {id}");
            }
            for (int id = 1221; id <= 1226; ++id)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"규칙 설명 {id}");
            }
            Assert.IsNotEmpty(WorldMapTestSupport.Text(SealedNestRule.UnlockNoticeStringId));
        }

        [Test]
        public void 규칙_알림_문구_id는_모두_존재한다()
        {
            Assert.IsNotEmpty(WorldMapTestSupport.Text(BossBulwarkRule.NoticeStringId));
            Assert.IsNotEmpty(WorldMapTestSupport.Text(ArrowRainRule.NoticeStringId));
            Assert.IsNotEmpty(WorldMapTestSupport.Text(HunterTrapsRule.NoticeStringId));
        }

        [Test]
        public void 월드맵_ViewModel이_쓰는_문구_id_상수는_전부_존재한다()
        {
            int[] ids =
            {
                WorldMapViewModel.StrChapterTitle, WorldMapViewModel.StrChapterPlaceholder, WorldMapViewModel.StrSelectButton,
                WorldMapViewModel.StrCleared, WorldMapViewModel.StrLocked, WorldMapViewModel.StrNoRules,
                WorldMapViewModel.StrNoRecord, WorldMapViewModel.StrChapterLockedHint, WorldMapViewModel.StrSelectNote,
                WorldMapViewModel.StrThemeHero, WorldMapViewModel.StrHeroName, WorldMapViewModel.StrNotReady,
                WorldMapViewModel.StrNotReadyHint, WorldMapViewModel.StrRecord, WorldMapViewModel.StrStageLockedNote,
                StageLabels.HintPreviousStageStringId, StageLabels.HintChapterBossStringId,
            };

            foreach (int id in ids)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id}");
            }
        }

        [Test]
        public void 서식_문구는_필요한_인자_수로_Format해도_예외가_없고_자리표시자가_남지_않는다()
        {
            (int id, object[] args)[] cases =
            {
                (1311, new object[] { 1, "A" }), (1312, new object[] { 3 }), (1313, new object[] { 1, 2 }),
                (1314, new object[] { 1 }), (1315, new object[] { 2, 1 }), (1317, new object[] { 60 }),
                (1318, new object[] { "A" }), (1319, new object[] { 60 }), (1320, new object[] { "A", "B" }),
                (1321, new object[] { "A" }), (1324, new object[] { "0:00.0", 1, 2 }),
                (1221, new object[] { 1, 2, 3 }), (1222, new object[] { 1, 2 }), (1223, new object[] { "A", 2 }),
                (1224, new object[] { 1, 2, 3 }), (1225, new object[] { 1, 2, 3, 4 }), (1226, new object[] { 1, 2, 3, 4 }),
                (1231, new object[] { "A" }),
            };

            foreach ((int id, object[] args) in cases)
            {
                string formatted = null;
                Assert.DoesNotThrow(() => formatted = string.Format(WorldMapTestSupport.Text(id), args), $"id {id}");
                StringAssert.DoesNotContain("{", formatted, $"id {id}");
            }
        }

        [Test]
        public void 챕터_스테이지_영웅_데이터가_참조하는_문구는_모두_존재한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            foreach (ChapterRow chapter in db.Chapters)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(chapter.nameKey), $"챕터 {chapter.chapterId} 이름");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(chapter.themeTextKey), $"챕터 {chapter.chapterId} 테마");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(db.GetHero(chapter.heroId).nameKey), $"챕터 {chapter.chapterId} 영웅");
                for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
                {
                    Assert.IsNotEmpty(WorldMapTestSupport.Text(db.GetStage(chapter.chapterId, index).nameKey), $"스테이지 {chapter.chapterId}-{index}");
                }
            }
        }

        //# --- 프리팹 ---

        private static bool IsNullRef(object value)
        {
            if (value == null)
                return true;
            UnityEngine.Object obj = value as UnityEngine.Object;
            return obj != null ? false : value is UnityEngine.Object;
        }

        private static void AssertAllSerializedRefsAssigned(object target, string label)
        {
            FieldInfo[] fields = target.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            int checkedCount = 0;
            foreach (FieldInfo field in fields)
            {
                bool serialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                if (serialized == false || typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) == false)
                    continue;
                //# 스프라이트는 선택 슬롯(미지정이면 배경 스프라이트로 폴백) — 위젯 참조만 필수로 본다.
                if (field.FieldType == typeof(Sprite))
                    continue;

                Assert.IsFalse(IsNullRef(field.GetValue(target)), $"{label}.{field.Name} 참조가 비어 있음");
                ++checkedCount;
            }
            Assert.Greater(checkedCount, 0, $"{label} 에 검사할 참조 필드가 없음");
        }

        [Test]
        public void 팝업_프리팹이_로드되고_WorldMapPopup_컴포넌트를_가진다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);

            Assert.IsNotNull(prefab, "WorldMapPopup.prefab 로드 실패");
            Assert.IsNotNull(prefab.GetComponent<WorldMapPopup>(), "WorldMapPopup 컴포넌트 없음");
            Assert.IsTrue(Enum.IsDefined(typeof(EUI), "WorldMapPopup"), "EUI 값명 = 프리팹 파일명(Rule 03 §2)");
        }

        [Test]
        public void 팝업_프리팹의_필수_SerializeField_참조가_모두_연결돼_있다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
            WorldMapPopup popup = prefab.GetComponent<WorldMapPopup>();

            FieldInfo[] fields = typeof(WorldMapPopup).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.GetCustomAttribute<SerializeField>() == null)
                    continue;
                if (field.FieldType.IsArray)
                    continue;
                Assert.IsFalse(IsNullRef(field.GetValue(popup)), $"WorldMapPopup.{field.Name} 참조가 비어 있음");
            }
        }

        [Test]
        public void 팝업_프리팹은_챕터_탭_5개와_스테이지_노드_5개가_배치돼_있고_각_뷰의_참조가_연결돼_있다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
            WorldMapPopup popup = prefab.GetComponent<WorldMapPopup>();
            FieldInfo tabsField = typeof(WorldMapPopup).GetField("_tabs", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo nodesField = typeof(WorldMapPopup).GetField("_nodes", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(tabsField, "_tabs 필드 존재 — 시그니처 변경 감지");
            Assert.IsNotNull(nodesField, "_nodes 필드 존재 — 시그니처 변경 감지");

            WorldMapChapterTabView[] tabs = (WorldMapChapterTabView[])tabsField.GetValue(popup);
            WorldMapNodeView[] nodes = (WorldMapNodeView[])nodesField.GetValue(popup);

            Assert.AreEqual(WorldMapViewModel.ChapterSlotCount, tabs.Length);
            Assert.AreEqual(StageProgress.StagesPerChapter, nodes.Length);
            for (int i = 0; i < tabs.Length; ++i)
            {
                Assert.IsFalse(IsNullRef(tabs[i]), $"_tabs[{i}] 비어 있음");
                AssertAllSerializedRefsAssigned(tabs[i], $"_tabs[{i}]");
            }
            for (int i = 0; i < nodes.Length; ++i)
            {
                Assert.IsFalse(IsNullRef(nodes[i]), $"_nodes[{i}] 비어 있음");
                AssertAllSerializedRefsAssigned(nodes[i], $"_nodes[{i}]");
            }
        }

        [Test]
        public void 탭과_노드_프리팹_자체도_뷰_컴포넌트와_참조가_연결돼_있다()
        {
            GameObject tabPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/UI/WorldMapChapterTab.prefab");
            GameObject nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/UI/WorldMapNode.prefab");

            Assert.IsNotNull(tabPrefab);
            Assert.IsNotNull(nodePrefab);
            AssertAllSerializedRefsAssigned(tabPrefab.GetComponent<WorldMapChapterTabView>(), "WorldMapChapterTab");
            AssertAllSerializedRefsAssigned(nodePrefab.GetComponent<WorldMapNodeView>(), "WorldMapNode");
        }

        [Test]
        public void 팝업_프리팹은_Addressables_주소로_등록돼_있다()
        {
            string group = File.ReadAllText("Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset");

            StringAssert.Contains("m_Address: WorldMapPopup", group);
        }
    }
}
