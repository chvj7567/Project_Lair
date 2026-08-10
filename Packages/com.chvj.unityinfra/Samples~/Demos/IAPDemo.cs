#if UNITY_INFRA_IAP
using ChvjUnityInfra;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// CHMIAP 데모.
    /// 사전 준비:
    /// 1. Window > Package Manager에서 "In-App Purchasing" 패키지 설치
    /// 2. Tools > ChvjUnityInfra > Settings > IAP 탭 → Use IAP ✓
    /// 3. 같은 탭에서 IAPProductConfig 편집 → Products 배열에 상품 정의
    ///    - productName: 게임 식별자 (예: "RemoveAD")
    ///    - productID: 스토어 상품 ID (예: "com.yourgame.removead")
    ///    - productType: Consumable / NonConsumable / Subscription
    /// 4. Google Play Console / App Store Connect에 상품 등록 + 테스트 계정 셋업
    /// </summary>
    public class IAPDemo : MonoBehaviour
    {
        private const string PRODUCT_REMOVE_AD = "RemoveAD";
        private const string PRODUCT_GOLD_PACK = "GoldPack";

        private void Start()
        {
            // 결제 결과 콜백 등록 (Init 전에 미리)
            CHMIAP.Instance.purchaseState += OnPurchaseResult;

            // CHMIAP.Init은 통합 SDK에서 자동 호출됨
            CHMIAP.Instance.Init();
        }

        private void OnPurchaseResult(CHMIAP.PurchaseState result)
        {
            switch (result.state)
            {
                case EPurchase.Success:
                    Debug.Log($"[IAPDemo] 구매 성공: {result.productName}");
                    // 상품별 지급 로직
                    if (result.productName == PRODUCT_REMOVE_AD)
                    {
                        // PlayerPrefs.SetInt("AdRemoved", 1);
                    }
                    else if (result.productName == PRODUCT_GOLD_PACK)
                    {
                        // GameState.Instance.gold += 1000;
                    }
                    // Pending 패턴: 지급 처리 완료 후 ConfirmPending 호출 필수.
                    // 호출 전에는 다음 Init 시 ProcessPurchase가 재진입되어 콜백이 다시 발생한다.
                    CHMIAP.Instance.ConfirmPending(result.productName);
                    break;

                case EPurchase.Failure:
                    Debug.LogWarning($"[IAPDemo] 구매 실패: {result.productName}");
                    break;
            }
        }

        // UI 버튼 OnClick에 연결
        public void OnClickBuyRemoveAd()
        {
            if (CHMIAP.Instance.CanBuyFromName(PRODUCT_REMOVE_AD))
            {
                CHMIAP.Instance.Purchase(PRODUCT_REMOVE_AD);
            }
            else
            {
                Debug.Log("[IAPDemo] 이미 구매했거나 구매 불가");
            }
        }

        public void OnClickBuyGoldPack() => CHMIAP.Instance.Purchase(PRODUCT_GOLD_PACK);

        public void OnClickRestore() => CHMIAP.Instance.RestorePurchase();

        public void ShowPriceUI()
        {
            decimal price = CHMIAP.Instance.GetPrice(PRODUCT_REMOVE_AD);
            string unit = CHMIAP.Instance.GetPriceUnit(PRODUCT_REMOVE_AD);
            Debug.Log($"[IAPDemo] {PRODUCT_REMOVE_AD}: {price} {unit}");
        }
    }
}
#endif
