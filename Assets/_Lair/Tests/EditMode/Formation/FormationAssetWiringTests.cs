using System;
using System.IO;
using System.Reflection;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 프리팹 SerializeField 연결 + Addressables 등록 + 동적 라벨 _stringID 미사용 (Rule 03 §2·§3).
    public class FormationAssetWiringTests
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const BindingFlags Declared = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private static T Load<T>(string prefabName) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + prefabName + ".prefab");
            Assert.IsNotNull(prefab, prefabName + ".prefab 로드 실패");
            T component = prefab.GetComponentInChildren<T>(true);
            Assert.IsNotNull(component, prefabName + " 에 컴포넌트 없음");
            return component;
        }

        private static void AssertAllAssigned(object target)
        {
            foreach (FieldInfo field in target.GetType().GetFields(Declared))
            {
                if (field.GetCustomAttribute<SerializeField>() == null)
                    continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    Assert.IsTrue((UnityEngine.Object)field.GetValue(target) != null, $"{target.GetType().Name}.{field.Name} 참조가 비어 있음");
                }
                else if (field.FieldType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType.GetElementType()))
                {
                    Array array = (Array)field.GetValue(target);
                    Assert.Greater(array.Length, 0, $"{target.GetType().Name}.{field.Name} 배열이 비어 있음");
                    for (int i = 0; i < array.Length; ++i)
                    {
                        Assert.IsTrue((UnityEngine.Object)array.GetValue(i) != null, $"{target.GetType().Name}.{field.Name}[{i}] 참조가 비어 있음");
                    }
                }
            }
        }

        [Test]
        public void 프리팹_3종의_SerializeField가_모두_연결됐다()
        {
            AssertAllAssigned(Load<FormationPopup>("FormationPopup"));
            AssertAllAssigned(Load<FormationNestTile>("FormationNestTile"));
            AssertAllAssigned(Load<FormationSpeciesRow>("FormationSpeciesRow"));
        }

        [Test]
        public void 팝업은_둥지_칸_6과_종_행_6을_정적으로_가진다()
        {
            FormationPopup popup = Load<FormationPopup>("FormationPopup");
            Assert.AreEqual(6, popup.GetComponentsInChildren<FormationNestTile>(true).Length);
            Assert.AreEqual(6, popup.GetComponentsInChildren<FormationSpeciesRow>(true).Length);
        }

        [Test]
        public void 값이_코드로_채워지는_칸과_행_라벨은_stringID를_쓰지_않는다()
        {
            foreach (string name in new[] { "FormationNestTile", "FormationSpeciesRow" })
            {
                string yaml = File.ReadAllText(UiDir + name + ".prefab");
                StringAssert.DoesNotContain("_stringID: 13", yaml, name + " - 코드가 완성 문구를 넣는 라벨은 _stringID 가 -1 이어야 이중 포맷되지 않는다");
            }
        }

        [Test]
        public void 편성_JSON_5종과_팝업이_Addressables에_등록돼_있다()
        {
            string groups = File.ReadAllText("Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset")
                + File.ReadAllText("Assets/AddressableAssetsData/AssetGroups/Resource.asset");
            foreach (string address in new[] { "FormationConfig", "FormationMonster", "FormationSlot", "Card_Species", "Chapter_FormationHint", "FormationPopup" })
            {
                StringAssert.Contains("m_Address: " + address + "\n", groups.Replace("\r\n", "\n"), address);
            }
        }
    }
}
