using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 유물 프리팹 SerializeField 연결 · 아이콘 파일명 = ERelic 값명 · Addressables 주소 등록 (relics §12.4, Rule 03 §2, Rule 04).
    public class RelicAssetWiringTests
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string IconDir = "Assets/_Lair/Art/Sprites/Relic/";
        private const BindingFlags Declared = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private static readonly string[] JsonNames =
        {
            "Relic", "Relic_Effect", "Relic_Effect_Param", "Relic_Drop", "RelicRarity", "RelicStack", "RelicConfig",
            "Stage_Relic", "Difficulty_Relic", "Difficulty_Relic_Rarity",
        };

        private static bool IsMissing(object value)
        {
            if (value == null)
                return true;
            if (value is UnityEngine.Object obj)
                return obj == null;
            return false;
        }

        private static T LoadComponent<T>(string prefabName) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + prefabName + ".prefab");
            Assert.IsNotNull(prefab, prefabName + ".prefab 로드 실패");
            T component = prefab.GetComponentInChildren<T>(true);
            Assert.IsNotNull(component, $"{prefabName} 에 {typeof(T).Name} 없음");
            return component;
        }

        //# 대상 타입이 직접 선언한 직렬화 필드 중 UnityEngine.Object(및 그 배열) 참조가 모두 연결됐는지 — filter 로 이번 기능 필드만 고를 수 있다.
        private static int AssertAssigned(object target, string label, Func<FieldInfo, bool> filter)
        {
            int checkedCount = 0;
            foreach (FieldInfo field in target.GetType().GetFields(Declared))
            {
                bool serialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                if (serialized == false || (filter != null && filter(field) == false))
                    continue;

                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    Assert.IsFalse(IsMissing(field.GetValue(target)), $"{label}.{field.Name} 참조가 비어 있음");
                    checkedCount++;
                }
                else if (field.FieldType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType.GetElementType()))
                {
                    Array array = (Array)field.GetValue(target);
                    Assert.IsNotNull(array, $"{label}.{field.Name} 배열이 null");
                    Assert.Greater(array.Length, 0, $"{label}.{field.Name} 배열이 비어 있음");
                    for (int i = 0; i < array.Length; ++i)
                    {
                        Assert.IsFalse(IsMissing(array.GetValue(i)), $"{label}.{field.Name}[{i}] 참조가 비어 있음");
                    }
                    checkedCount++;
                }
            }
            return checkedCount;
        }

        private static object FieldValue(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, Declared);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} 필드 존재 — 시그니처 변경 감지");
            return field.GetValue(target);
        }

        //# --- 프리팹 ---

        [Test]
        public void RelicPopup_프리팹의_직렬화_참조가_모두_연결돼_있다()
        {
            RelicPopup popup = LoadComponent<RelicPopup>("RelicPopup");

            int checkedCount = AssertAssigned(popup, "RelicPopup", null);

            Assert.GreaterOrEqual(checkedCount, 10, "dim/close/scroll/detail/수집 텍스트 2/빈 결과 2/칩 배열 2");
        }

        [Test]
        public void RelicPopup_필터_칩은_종류_4개_등급_4개다()
        {
            RelicPopup popup = LoadComponent<RelicPopup>("RelicPopup");

            Assert.AreEqual(4, ((Array)FieldValue(popup, "_categoryChips")).Length, "전체/능력치/전투 효과/편의");
            Assert.AreEqual(4, ((Array)FieldValue(popup, "_rarityChips")).Length, "전체/일반/희귀/전설");
        }

        [Test]
        public void RelicPopup의_상세_뷰_참조가_모두_연결돼_있다()
        {
            RelicPopup popup = LoadComponent<RelicPopup>("RelicPopup");
            RelicDetailView detail = (RelicDetailView)FieldValue(popup, "_detailView");

            int checkedCount = AssertAssigned(detail, "RelicDetailView", null);

            Assert.GreaterOrEqual(checkedCount, 14);
        }

        [Test]
        public void RelicPopup의_풀링_스크롤뷰는_origin_셀이_연결되고_그_셀의_참조도_온전하다()
        {
            RelicPopup popup = LoadComponent<RelicPopup>("RelicPopup");
            RelicPoolingScrollView scroll = (RelicPoolingScrollView)FieldValue(popup, "_scrollView");
            FieldInfo origin = null;
            for (Type t = typeof(RelicPoolingScrollView); t != null && origin == null; t = t.BaseType)
            {
                origin = t.GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }
            Assert.IsNotNull(origin, "CHPoolingScrollView._origin 존재 — 시그니처 변경 감지");

            GameObject originObject = (GameObject)origin.GetValue(scroll);

            Assert.IsFalse(IsMissing(originObject), "_origin 이 비어 있음");
            RelicCell cell = originObject.GetComponent<RelicCell>();
            Assert.IsNotNull(cell, "origin 에 RelicCell 컴포넌트 없음(제거된 컴포넌트 의심)");
            AssertAssigned(cell, "RelicPopup.origin RelicCell", null);
        }

        [Test]
        public void RelicCell_프리팹의_직렬화_참조가_모두_연결돼_있다()
        {
            RelicCell cell = LoadComponent<RelicCell>("RelicCell");

            int checkedCount = AssertAssigned(cell, "RelicCell", null);

            Assert.GreaterOrEqual(checkedCount, 7);
        }

        [Test]
        public void ResultPopup_프리팹의_유물_띠_참조가_모두_연결돼_있다()
        {
            ResultPopup popup = LoadComponent<ResultPopup>("ResultPopup");

            int checkedCount = AssertAssigned(popup, "ResultPopup", f => f.Name.StartsWith("_relic") || f.Name == "_rowRelicMiss");

            Assert.AreEqual(12, checkedCount, "band/border/icon/frame/name/chip/rarity/new/stack/glow/missRow/missText");
        }

        [Test]
        public void BattleHud_프리팹의_2배속_토글_참조가_모두_연결돼_있다()
        {
            BattleHud hud = LoadComponent<BattleHud>("BattleHud");

            int checkedCount = AssertAssigned(hud, "BattleHud", f => f.Name.StartsWith("_speed"));

            Assert.AreEqual(3, checkedCount, "토글/라벨/On 테두리");
        }

        [Test]
        public void VillageHud_프리팹의_유물_버튼이_연결돼_있다()
        {
            VillageHud hud = LoadComponent<VillageHud>("VillageHud");

            int checkedCount = AssertAssigned(hud, "VillageHud", f => f.Name == "_relicButton");

            Assert.AreEqual(1, checkedCount);
        }

        [Test]
        public void 번개_FX_프리팹이_존재한다()
        {
            GameObject fx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/FX/RelicLightningFx.prefab");

            Assert.IsNotNull(fx);
            Assert.IsTrue(Enum.IsDefined(typeof(EVisual), "RelicLightningFx"), "EVisual 값명 = 프리팹 파일명");
        }

        //# --- 아이콘 ---

        [Test]
        public void 아이콘_PNG_파일명은_ERelic_값명과_일대일이다()
        {
            HashSet<string> files = new HashSet<string>();
            foreach (string path in Directory.GetFiles(IconDir, "*.png"))
            {
                files.Add(Path.GetFileNameWithoutExtension(path));
            }
            HashSet<string> names = new HashSet<string>(Enum.GetNames(typeof(ERelic)));

            Assert.AreEqual(16, files.Count);
            Assert.IsTrue(names.SetEquals(files), "ERelic 값명과 Art/Sprites/Relic/*.png 파일명이 달라지면 런타임 로드 실패 (Rule 03 §2)");
        }

        [Test]
        public void 아이콘은_모두_스프라이트로_임포트된다()
        {
            foreach (string name in Enum.GetNames(typeof(ERelic)))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + name + ".png");
                Assert.IsNotNull(sprite, name + ".png 가 Sprite 로 임포트되지 않음");
            }
        }

        [Test]
        public void 마을_타일_아이콘_Icon_Relic이_존재한다()
        {
            string[] guids = AssetDatabase.FindAssets("Icon_Relic t:Sprite");

            Assert.Greater(guids.Length, 0);
        }

        //# --- Addressables / Enum ---

        private static string AllGroupsText()
        {
            string text = string.Empty;
            foreach (string path in Directory.GetFiles("Assets/AddressableAssetsData/AssetGroups", "*.asset"))
            {
                text += File.ReadAllText(path) + "\n";
            }
            return text;
        }

        private static bool HasAddress(string groups, string address)
        {
            return Regex.IsMatch(groups, @"^\s*m_Address: " + Regex.Escape(address) + @"\s*$", RegexOptions.Multiline);
        }

        [Test]
        public void ERelic_아이콘_16종이_Addressables_주소로_등록돼_있다()
        {
            string groups = AllGroupsText();

            foreach (string name in Enum.GetNames(typeof(ERelic)))
            {
                Assert.IsTrue(HasAddress(groups, name), $"Addressables 주소 {name} 없음");
            }
        }

        [Test]
        public void 유물_JSON_10종은_파일이_있고_EData_값명과_Addressables_주소가_모두_맞다()
        {
            string groups = AllGroupsText();

            foreach (string name in JsonNames)
            {
                Assert.IsTrue(File.Exists(WorldMapTestSupport.JsonDir + name + ".json"), name + ".json 없음");
                Assert.IsTrue(Enum.IsDefined(typeof(EData), name), $"EData.{name} 없음");
                Assert.IsTrue(HasAddress(groups, name), $"Addressables 주소 {name} 없음");
            }
        }

        [Test]
        public void 도감_팝업_번개_FX_사운드_마을_아이콘의_Enum과_Addressables_주소가_등록돼_있다()
        {
            string groups = AllGroupsText();

            Assert.IsTrue(Enum.IsDefined(typeof(EUI), "RelicPopup"));
            Assert.IsTrue(Enum.IsDefined(typeof(EVisual), "RelicLightningFx"));
            Assert.IsTrue(Enum.IsDefined(typeof(EAudio), "RelicAcquire"));
            Assert.IsTrue(Enum.IsDefined(typeof(EAudio), "RelicThunder"));
            foreach (string address in new[] { "RelicPopup", "RelicLightningFx", "RelicAcquire", "RelicThunder", "Icon_Relic" })
            {
                Assert.IsTrue(HasAddress(groups, address), $"Addressables 주소 {address} 없음");
            }
        }

        [Test]
        public void 유물_Enum_값은_append_only_순서로_ERelic_16종_종류_3_등급_3_효과_12다()
        {
            Assert.AreEqual(16, Enum.GetNames(typeof(ERelic)).Length);
            Assert.AreEqual(3, Enum.GetNames(typeof(ERelicCategory)).Length);
            Assert.AreEqual(3, Enum.GetNames(typeof(ERelicRarity)).Length);
            Assert.AreEqual(12, Enum.GetNames(typeof(ERelicEffectKind)).Length);
            Assert.AreEqual(0, (int)ERelic.HeartOfStone);
            Assert.AreEqual(15, (int)ERelic.SpawnPrelude);
            Assert.AreEqual(0, (int)ERelicCategory.Stat, "RelicViewModel 필터 인덱스 = 값 + 1");
            Assert.AreEqual(0, (int)ERelicRarity.Common, "RelicViewModel 필터 인덱스 = 값 + 1 / 등급 굴림 순회 순서");
            Assert.AreEqual(2, (int)ERelicRarity.Legendary);
        }
    }
}
