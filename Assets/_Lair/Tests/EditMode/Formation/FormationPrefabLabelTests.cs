using System.Collections.Generic;
using System.Reflection;
using ChvjUnityInfra;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 프리팹 라벨 규약 (Rule 03 §3) - 모든 TMP_Text 에 CHText, 정적 문구 stringID 는 Strings_Ko 1393~1412/1303 안, 코드가 채우는 라벨은 -1.
    //# SerializeField 연결/6칸 6행/Addressables/'_stringID: 13' 이중 포맷 검사는 FormationAssetWiringTests 소유 - 여기서는 중복하지 않는다.
    public class FormationPrefabLabelTests
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";

        //# 팝업이 정적 문구로 쓰는 id - 던전 편성(1393) 제목/출격/기본 편성/영웅 성향 + 스테이지 규칙(1303, 기존 문구 재사용).
        private static readonly HashSet<int> PopupStaticIds = new HashSet<int> { 1303, 1393, 1395, 1396, 1407 };

        private static GameObject Load(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + prefabName + ".prefab");
            Assert.IsNotNull(prefab, prefabName + ".prefab 로드 실패");
            return prefab;
        }

        private static int StringIdOf(CHText text)
        {
            FieldInfo field = typeof(CHText).GetField("_stringID", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "CHText._stringID (패키지 시그니처 변경 감지)");
            return (int)field.GetValue(text);
        }

        [Test]
        public void 프리팹_3종의_모든_TMP_텍스트는_CHText를_함께_가진다()
        {
            foreach (string name in new[] { "FormationPopup", "FormationNestTile", "FormationSpeciesRow" })
            {
                GameObject prefab = Load(name);
                TMP_Text[] labels = prefab.GetComponentsInChildren<TMP_Text>(true);
                Assert.Greater(labels.Length, 0, name);
                foreach (TMP_Text label in labels)
                {
                    Assert.IsNotNull(label.GetComponent<CHText>(), $"{name}/{label.name} - 정적 라벨도 CHText 필수");
                }
            }
        }

        [Test]
        public void 둥지_칸과_종_행의_모든_라벨은_stringID가_음수_1이다()
        {
            foreach (string name in new[] { "FormationNestTile", "FormationSpeciesRow" })
            {
                foreach (CHText text in Load(name).GetComponentsInChildren<CHText>(true))
                {
                    Assert.AreEqual(-1, StringIdOf(text), $"{name}/{text.name} - 코드가 완성 문구를 넣는 라벨");
                }
            }
        }

        [Test]
        public void 팝업의_정적_라벨은_편성_문구_범위와_스테이지_규칙_제목만_쓰고_나머지는_음수_1이다()
        {
            int dynamicCount = 0;
            HashSet<int> used = new HashSet<int>();
            foreach (CHText text in Load("FormationPopup").GetComponentsInChildren<CHText>(true))
            {
                int id = StringIdOf(text);
                if (id == -1)
                {
                    ++dynamicCount;
                    continue;
                }
                Assert.IsTrue(PopupStaticIds.Contains(id), $"{text.name} 의 stringID {id} 는 허용 목록 밖");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"{text.name} 의 stringID {id} 가 Strings_Ko 에 없음");
                used.Add(id);
            }
            Assert.Greater(dynamicCount, 0, "코드가 값을 넣는 라벨이 있어야 한다");
            CollectionAssert.IsSubsetOf(new[] { 1393, 1395, 1396, 1407 }, used, "제목/출격/기본 편성/성향 제목 라벨 (부제 1394 는 삭제됨)");
            CollectionAssert.DoesNotContain(used, 1394, "삭제된 부제 라벨 잔재");
        }

        [Test]
        public void 팝업은_출격_버튼과_기본_편성_버튼을_CHButton으로_가진다()
        {
            GameObject popup = Load("FormationPopup");
            Assert.GreaterOrEqual(popup.GetComponentsInChildren<CHButton>(true).Length, 2, "출격 + 기본 편성(+ 닫기)");
            foreach (UnityEngine.UI.Button button in popup.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                Assert.IsNotNull(button.GetComponent<CHButton>(), $"{button.name} - 단일 Button 직접 사용 금지 (Rule 03 §3)");
            }
        }
    }
}
