using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

// IAP v5에서 IStoreListener/UnityPurchasing.Initialize 등이 deprecated 처리됨.
// 동작에는 문제 없으나 v6에서 제거될 가능성. 마이그레이션은 별도 작업.
#pragma warning disable 0618

namespace ChvjUnityInfra
{
    /// <summary>
    /// IAP 매니저. Init() 호출 시 IAPProductConfig를 Resources에서 자동 로드.
    /// 게임 측: CHMIAP.Instance.purchaseState += result => { ... };
    ///         CHMIAP.Instance.Init();
    ///         CHMIAP.Instance.Purchase("productName");
    /// </summary>
    public class CHMIAP : CHSingletonStatic<CHMIAP>, IStoreListener
    {
        public class PurchaseState
        {
            public string productName;
            public EPurchase state;
        }

        private List<IAPProductConfig.Product> _productList = new List<IAPProductConfig.Product>();
        private IStoreController _iStoreController;
        private IExtensionProvider _iExtensionProvider;

        public bool IsInitialized => _iStoreController != null && _iExtensionProvider != null;

        /// <summary>구매/초기화 결과 통지. 등록은 += 로 (event라 = 대입 불가).</summary>
        public event Action<PurchaseState> purchaseState;

        public bool IsConsumableType(string productName)
        {
            var product = _productList.Find(_ => _.productName == productName);
            if (product == null) return false;
            return product.productType == ProductType.Consumable;
        }

        public void Init()
        {
            if (IsInitialized) return;

            var config = Resources.Load<IAPProductConfig>("ChvjUnityInfra/IAPProductConfig");
            if (config == null)
            {
                Debug.LogWarning("[CHMIAP] IAPProductConfig 에셋을 찾을 수 없습니다. " +
                    "Tools/ChvjUnityInfra/Edit IAP Config 메뉴로 생성 후 상품을 정의하세요.");
                return;
            }

            if (config.Products == null || config.Products.Length == 0)
            {
                Debug.LogWarning("[CHMIAP] IAPProductConfig에 상품이 정의되지 않았습니다. Inspector에서 추가하세요.");
                return;
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            _productList.Clear();
            foreach (var p in config.Products)
            {
                _productList.Add(p);
                // 단일 productID를 양쪽 스토어에 동일하게 등록. 스토어별 ID가 다른 경우는
                // IAPProductConfig를 확장해 productID_apple 같은 필드를 추가하면 된다.
                builder.AddProduct(p.productName, p.productType, new IDs
                {
                    { p.productID, GooglePlay.Name },
                    { p.productID, AppleAppStore.Name },
                });
            }

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extension)
        {
            Debug.Log("[CHMIAP] 초기화 성공");
            _iStoreController = controller;
            _iExtensionProvider = extension;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[CHMIAP] 초기화 실패: {error}");
            purchaseState?.Invoke(new PurchaseState { productName = null, state = EPurchase.InitFailed });
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[CHMIAP] 초기화 실패: {error}\n{message}");
            purchaseState?.Invoke(new PurchaseState { productName = null, state = EPurchase.InitFailed });
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var id = args.purchasedProduct.definition.id;
            Debug.Log($"[CHMIAP] 구매 성공: {id}");

            purchaseState?.Invoke(new PurchaseState
            {
                productName = id,
                state = EPurchase.Success,
            });

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason error)
        {
            var id = product.definition.id;
            Debug.Log($"[CHMIAP] 구매 실패: {id} ({error})");

            purchaseState?.Invoke(new PurchaseState
            {
                productName = id,
                state = EPurchase.Failure,
            });
        }

        public void Purchase(string productName)
        {
            if (!IsInitialized) return;

            var product = GetProduct(productName);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[CHMIAP] 구매 시도: {product.definition.id}");
                _iStoreController.InitiatePurchase(product);
            }
            else
            {
                Debug.Log($"[CHMIAP] 구매 시도 불가: {productName}");
            }
        }

        public void RestorePurchase()
        {
            if (!IsInitialized) return;

            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.Log("[CHMIAP] 구매 복구 시도");

                var appleExt = _iExtensionProvider.GetExtension<IAppleExtensions>();
                appleExt.RestoreTransactions((success, error) =>
                    Debug.Log($"[CHMIAP] 구매 복구 결과: success={success} error={error}"));
            }
        }

        public bool HadPurchased(string productName)
        {
            if (!IsInitialized) return false;

            var product = GetProduct(productName);
            if (product == null) return false;

            return product.hasReceipt;
        }

        public Product GetProduct(string productName)
        {
            if (!IsInitialized) return null;
            return _iStoreController.products.WithID(productName);
        }

        public decimal GetPrice(string productID)
        {
            var product = GetProduct(productID);
            return product?.metadata.localizedPrice ?? 0;
        }

        public string GetPriceUnit(string productID)
        {
            var product = GetProduct(productID);
            return product?.metadata.isoCurrencyCode ?? "";
        }

        /// <summary>스토어 productID로 구매 가능 여부 조회. 비소모성이면 이미 구매한 경우 false.</summary>
        public bool CanBuyFromID(string productID)
        {
            var product = _productList.Find(_ => _.productID == productID);
            return product != null && CanBuyInternal(product);
        }

        /// <summary>게임 내 productName으로 구매 가능 여부 조회. 비소모성이면 이미 구매한 경우 false.</summary>
        public bool CanBuyFromName(string productName)
        {
            var product = _productList.Find(_ => _.productName == productName);
            return product != null && CanBuyInternal(product);
        }

        private bool CanBuyInternal(IAPProductConfig.Product product)
        {
            if (IsConsumableType(product.productName)) return true;

            if (HadPurchased(product.productName))
            {
                Debug.Log($"[CHMIAP] 이미 구매한 상품: {product.productName}");
                return false;
            }

            return true;
        }
    }
}
