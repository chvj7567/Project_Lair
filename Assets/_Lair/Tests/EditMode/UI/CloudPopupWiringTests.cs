using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 계정 팝업 — 공통 모달 정합(account-ranking-ui-alignment) 후 프리팹 와이어링 회귀 가드.
    //# 코드 0건·재부모만 한 변경이라 단위 로직 대상은 없다 → 이 세션의 VillageHud 재직렬화 클로버 전례에 대비한
    //# "프리팹이 깨지면 빨개지는" 트립와이어. 깨지면 계정 팝업의 복원·표시명·닫기 동선이 죽는다.
    public class CloudPopupWiringTests
    {
        private const string PrefabPath = "Assets/_Lair/Art/UI/CloudPopup.prefab";

        private static CloudPopup LoadCloudPopup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 미발견: {PrefabPath}");
            CloudPopup popup = prefab.GetComponent<CloudPopup>();
            Assert.IsNotNull(popup, "루트에 CloudPopup 컴포넌트 누락 — 프리팹 정체성 손상");
            return popup;
        }

        [Test]
        public void 계정팝업_공통모달_프레임노드_Dim_ModalBody_가_존재한다()
        {
            //# 정합 핵심 — 루트 밑에 Dim + ModalBody 가 형제로 추가되고 콘텐츠가 ModalBody 로 재부모됐는지.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 미발견: {PrefabPath}");

            Transform dim = prefab.transform.Find("Dim");
            Transform modalBody = prefab.transform.Find("ModalBody");
            Assert.IsNotNull(dim, "Dim 노드 누락 — 공통 모달 정합 미적용/롤백");
            Assert.IsNotNull(modalBody, "ModalBody 노드 누락 — 공통 모달 정합 미적용/롤백");
        }

        [Test]
        public void 계정팝업_닫기슬롯_BackgroundButton_BackButton_이_와이어링돼있다()
        {
            //# 최고가치 가드 — 이 두 슬롯은 정합 시 Dim/코너X 로 능동 재배선됐다(§6.6).
            //# §6.4 가 _backgroundButton 댕글링 리스크를 명시 → 끊기면 딤클릭/코너X 닫기 회귀.
            CloudPopup popup = LoadCloudPopup();
            PopupWiringAsserts.AssertWired(popup, "_backgroundButton", "CloudPopup");
            PopupWiringAsserts.AssertWired(popup, "_backButton", "CloudPopup");
        }

        [Test]
        public void 계정팝업_기능_SerializeField_참조가_재부모후에도_살아있다()
        {
            //# 재부모는 m_Father 만 바꾸므로 참조는 보존돼야 한다 — 클로버 시 null 로 빠지는 걸 박제.
            CloudPopup popup = LoadCloudPopup();
            string[] fields =
            {
                "_connectionText", "_displayNameText", "_changeNameButton",
                "_nameInput", "_nameConfirmButton", "_nameCancelButton", "_nameEditGroup",
                "_restoreButton", "_conflictGroup", "_conflictText",
                "_conflictRestoreButton", "_conflictLaterButton", "_conflictDot",
            };
            foreach (string field in fields)
                PopupWiringAsserts.AssertWired(popup, field, "CloudPopup");
        }
    }
}
