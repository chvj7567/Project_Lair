using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 결과화면 배경 (Result_Screen 스프라이트) — 프리팹 직렬화 정합 검증 (gameplay-programmer 자체 스모크).
    //# V2a 빌더 주입 결과를 박제 — 깨지면 결과 팝업 배경이 사라지거나 콘텐츠를 가린다.
    public class ResultPopupBackgroundTests
    {
        private const string PrefabPath = "Assets/_Lair/Art/UI/ResultPopup.prefab";
        private const string SpriteName = "Result_Screen";

        [Test]
        public void 결과팝업_Background_노드가_ResultScreen_스프라이트를_표시한다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 미발견: {PrefabPath}");

            Transform background = prefab.transform.Find("Background");
            Assert.IsNotNull(background, "ResultPopup 에 Background 노드 누락 — V2a 빌더 재실행 필요");

            Image image = background.GetComponent<Image>();
            Assert.IsNotNull(image, "Background 노드에 Image 컴포넌트 누락");
            Assert.IsNotNull(image.sprite, "Background 스프라이트 참조 누락");
            Assert.AreEqual(SpriteName, image.sprite.name, "Background 스프라이트가 Result_Screen 이 아님");
        }

        [Test]
        public void 결과팝업_Background_는_Dim_위_콘텐츠_아래에_그려진다()
        {
            //# 엣지 — sibling 순서가 틀리면 배경이 텍스트/버튼을 가리거나 Dim 에 묻힌다.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 미발견: {PrefabPath}");

            Transform background = prefab.transform.Find("Background");
            Assert.IsNotNull(background, "Background 노드 누락");

            Assert.AreEqual(1, background.GetSiblingIndex(), "Background 는 Dim(0) 바로 위, 콘텐츠 아래(1)여야 함");

            Image image = background.GetComponent<Image>();
            Assert.IsNotNull(image, "Background 노드에 Image 컴포넌트 누락");
            Assert.IsFalse(image.raycastTarget, "Background 가 버튼 클릭을 가로채면 안 됨 (raycastTarget off)");
        }
    }
}
