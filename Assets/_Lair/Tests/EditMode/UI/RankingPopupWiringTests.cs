using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 랭킹 팝업 — 공통 모달 정합(account-ranking-ui-alignment) 후 프리팹 와이어링 회귀 가드.
    //# 코드 0건·재부모 + 루트 리네임만 한 변경 → 트립와이어. 깨지면 Top100 조회/빈안내/닫기가 죽는다.
    public class RankingPopupWiringTests
    {
        private const string PrefabPath = "Assets/_Lair/Art/UI/RankingPopup.prefab";

        private static RankingPopup LoadRankingPopup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 미발견: {PrefabPath}");
            RankingPopup popup = prefab.GetComponent<RankingPopup>();
            Assert.IsNotNull(popup, "루트에 RankingPopup 컴포넌트 누락 — 프리팹 정체성 손상");
            return popup;
        }

        [Test]
        public void 랭킹팝업_루트이름이_RankingPopup_으로_정합됐다()
        {
            //# 정합 시 루트 GO 이름 LeaderboardPopup → RankingPopup 으로 변경(§3.2). 회귀 가드.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 미발견: {PrefabPath}");
            Assert.AreEqual("RankingPopup", prefab.name, "루트 GO 이름이 RankingPopup 이 아님 — 리네임 롤백 의심");
        }

        [Test]
        public void 랭킹팝업_공통모달_프레임노드_Dim_ModalBody_가_존재한다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 미발견: {PrefabPath}");

            Transform dim = prefab.transform.Find("Dim");
            Transform modalBody = prefab.transform.Find("ModalBody");
            Assert.IsNotNull(dim, "Dim 노드 누락 — 공통 모달 정합 미적용/롤백");
            Assert.IsNotNull(modalBody, "ModalBody 노드 누락 — 공통 모달 정합 미적용/롤백");
        }

        [Test]
        public void 랭킹팝업_닫기슬롯_BackgroundButton_BackButton_이_와이어링돼있다()
        {
            //# 최고가치 가드 — 정합 시 Dim Button(_backgroundButton 인계) + 신규 코너X(_backButton)로 능동 재배선(§3.4·§6.6).
            //# 루트 Button 제거 시 _backgroundButton 댕글링 리스크 → 끊기면 닫기 회귀.
            RankingPopup popup = LoadRankingPopup();
            PopupWiringAsserts.AssertWired(popup, "_backgroundButton", "RankingPopup");
            PopupWiringAsserts.AssertWired(popup, "_backButton", "RankingPopup");
        }

        [Test]
        public void 랭킹팝업_기능_SerializeField_참조가_재부모후에도_살아있다()
        {
            //# _scrollView/_emptyText 가 재부모 후에도 보존돼야 Top100 조회·빈/오프라인 안내가 동작.
            RankingPopup popup = LoadRankingPopup();
            PopupWiringAsserts.AssertWired(popup, "_scrollView", "RankingPopup");
            PopupWiringAsserts.AssertWired(popup, "_emptyText", "RankingPopup");
        }
    }
}
