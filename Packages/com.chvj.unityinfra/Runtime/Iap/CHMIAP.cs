using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#if UNITY_INFRA_IAP_VALIDATE
using UnityEngine.Purchasing.Security;
#endif

namespace ChvjUnityInfra
{
    /// <summary>
    /// IAP 매니저. Init() 호출 시 IAPProductConfig를 Resources에서 자동 로드.
    /// 게임 측: CHMIAP.Instance.purchaseState += result => { ... };
    ///         CHMIAP.Instance.Init();
    ///         CHMIAP.Instance.Purchase("productName");
    /// </summary>
    public class CHMIAP : CHSingletonStatic<CHMIAP>, IDetailedStoreListener
    {
        public class PurchaseState
        {
            public string productName;
            public EPurchase state;
        }

        private List<IAPProductConfig.Product> _productList = new List<IAPProductConfig.Product>();
        private IStoreController _iStoreController;
        private IExtensionProvider _iExtensionProvider;
        private bool _initInFlight;
#if UNITY_INFRA_IAP_VALIDATE
        private CrossPlatformValidator _validator;
#endif

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
            // IsInitialized는 OnInitialized 콜백 도착 후에만 true가 되므로,
            // 비동기 진행 중 재호출되면 UnityPurchasing.Initialize가 두 번 호출돼 리스너가 중복 등록된다.
            if (IsInitialized || _initInFlight) return;

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
                builder.AddProduct(p.productName, p.productType, new StoreSpecificIds
                {
                    { p.productID, GooglePlay.Name },
                    { p.productID, AppleAppStore.Name },
                });
            }

            EnsureValidator();

            // v5 신규 이벤트: Initialize 호출 전에 구독해야 경고 없음.
            // (UnityPurchasing.Initialize 내부에서 Connect/FetchPurchases 즉시 호출)
            UnityIAPServices.DefaultStore().OnStoreConnected += OnV5StoreConnected;
            UnityIAPServices.DefaultStore().OnStoreDisconnected += OnV5StoreDisconnected;
            UnityIAPServices.DefaultPurchase().OnPurchasesFetchFailed += OnV5PurchasesFetchFailed;

            _initInFlight = true;
            try
            {
                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception e)
            {
                // Initialize가 동기적으로 throw하면 콜백이 안 와 _initInFlight이 true로 남아 영구 잠김.
                _initInFlight = false;
                Debug.LogError($"[CHMIAP] UnityPurchasing.Initialize 예외: {e}");
                purchaseState?.Invoke(new PurchaseState { productName = null, state = EPurchase.InitFailed });
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extension)
        {
            Debug.Log("[CHMIAP] 초기화 성공");
            _iStoreController = controller;
            _iExtensionProvider = extension;
            _initInFlight = false;
        }

        // v5 IStoreService / IPurchaseService 이벤트 핸들러
        private void OnV5StoreConnected()
        {
            Debug.Log("[CHMIAP] 스토어 연결됨");
        }

        private void OnV5StoreDisconnected(StoreConnectionFailureDescription desc)
        {
            Debug.LogWarning($"[CHMIAP] 스토어 연결 끊김: {desc.message} (retryable={desc.isRetryable})");
        }

        private void OnV5PurchasesFetchFailed(PurchasesFetchFailureDescription desc)
        {
            Debug.LogWarning($"[CHMIAP] 기존 구매 내역 조회 실패: {desc.failureReason} {desc.message}");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[CHMIAP] 초기화 실패: {error}");
            _initInFlight = false;
            purchaseState?.Invoke(new PurchaseState { productName = null, state = EPurchase.InitFailed });
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[CHMIAP] 초기화 실패: {error}\n{message}");
            _initInFlight = false;
            purchaseState?.Invoke(new PurchaseState { productName = null, state = EPurchase.InitFailed });
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var id = args.purchasedProduct.definition.id;

#if UNITY_INFRA_IAP_VALIDATE
            // Tangle 파일이 있을 때만 검증. 검증 실패 시 Failure 통지 + Complete 반환(재시도 차단).
            if (_validator != null)
            {
                try
                {
                    _validator.Validate(args.purchasedProduct.receipt);
                    Debug.Log($"[CHMIAP] 영수증 검증 성공: {id}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CHMIAP] 영수증 검증 실패: {id} ({e.Message})");
                    purchaseState?.Invoke(new PurchaseState
                    {
                        productName = id,
                        state = EPurchase.Failure,
                    });
                    return PurchaseProcessingResult.Complete;
                }
            }
#endif

            // Pending 패턴: 게임 측 지급 처리 완료 후 ConfirmPending(productName) 호출 필요.
            // 호출 전까지는 다음 Init 때 ProcessPurchase가 재진입되어 지급 유실을 방지.
            Debug.Log($"[CHMIAP] 구매 성공 (Pending): {id}. 지급 완료 후 ConfirmPending 호출 필요.");
            purchaseState?.Invoke(new PurchaseState
            {
                productName = id,
                state = EPurchase.Success,
            });

            return PurchaseProcessingResult.Pending;
        }

        /// <summary>
        /// 지급 처리가 완료된 후 호출. 호출 전에는 다음 Init 시 ProcessPurchase가 재진입된다.
        /// </summary>
        public void ConfirmPending(string productName)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[CHMIAP] ConfirmPending 무시: 초기화되지 않음 ({productName})");
                return;
            }

            var product = GetProduct(productName);
            if (product == null)
            {
                Debug.LogWarning($"[CHMIAP] ConfirmPending: 상품을 찾을 수 없음 ({productName})");
                return;
            }

            Debug.Log($"[CHMIAP] ConfirmPending: {productName}");
            _iStoreController.ConfirmPendingPurchase(product);
        }

        /// <summary>
        /// CrossPlatformValidator 인스턴스 준비. Tangle 파일이 없으면 무동작.
        /// 디파인 UNITY_INFRA_IAP_VALIDATE + Editor에서 Tangle 생성(Window > Unity IAP > Receipt Validation Obfuscator)이 모두 필요.
        /// v5 이후 Apple 로컬 검증은 no-op(StoreKit 2) → Google 전용 생성자 사용.
        /// </summary>
        private void EnsureValidator()
        {
#if UNITY_INFRA_IAP_VALIDATE
            if (_validator != null) return;
            try
            {
                // v5 권장: Google 전용 생성자. Apple은 StoreKit 2에서 no-op이므로 AppleTangle 불필요.
                _validator = new CrossPlatformValidator(
                    GooglePlayTangle.Data(),
                    Application.identifier);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CHMIAP] CrossPlatformValidator 생성 실패. 영수증 검증 skip: {e.Message}");
                _validator = null;
            }
#endif
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason error)
        {
            // v5에서는 OnPurchaseFailed(Product, PurchaseFailureDescription)가 호출된다.
            // 인터페이스(IStoreListener) 계약 만족용으로만 남겨둔다.
            var id = product.definition.id;
            Debug.Log($"[CHMIAP] 구매 실패 (legacy 콜백): {id} ({error})");
            purchaseState?.Invoke(new PurchaseState
            {
                productName = id,
                state = EPurchase.Failure,
            });
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            var id = product.definition.id;
            Debug.Log($"[CHMIAP] 구매 실패: {id} ({failureDescription.reason}) {failureDescription.message}");
            purchaseState?.Invoke(new PurchaseState
            {
                productName = id,
                state = EPurchase.Failure,
            });
        }

        public void Purchase(string productName)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[CHMIAP] Purchase 무시: 초기화되지 않음 (productName={productName})");
                purchaseState?.Invoke(new PurchaseState
                {
                    productName = productName,
                    state = EPurchase.NotReady,
                });
                return;
            }

            var product = GetProduct(productName);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[CHMIAP] 구매 시도: {product.definition.id}");
                _iStoreController.InitiatePurchase(product);
            }
            else
            {
                Debug.Log($"[CHMIAP] 구매 시도 불가: {productName}");
                purchaseState?.Invoke(new PurchaseState
                {
                    productName = productName,
                    state = EPurchase.Failure,
                });
            }
        }

        public void RestorePurchase()
        {
            if (!IsInitialized) return;

            if (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.OSXPlayer)
            {
                Debug.LogWarning($"[CHMIAP] RestorePurchase는 iOS/OSX에서만 지원됩니다. (현재 플랫폼: {Application.platform})");
                return;
            }

            Debug.Log("[CHMIAP] 구매 복구 시도");
            var appleExt = _iExtensionProvider.GetExtension<IAppleExtensions>();
            appleExt.RestoreTransactions((success, error) =>
            {
                Debug.Log($"[CHMIAP] 구매 복구 결과: success={success} error={error}");
                purchaseState?.Invoke(new PurchaseState
                {
                    productName = null,
                    state = success ? EPurchase.Restored : EPurchase.RestoreFailed,
                });
            });
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

            // 구독은 만료되면 재구매 허용. hasReceipt만 보면 만료 후에도 true라 재구매 차단됨.
            if (product.productType == ProductType.Subscription)
            {
                return !IsSubscriptionActive(product.productName);
            }

            if (HadPurchased(product.productName))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 구독 활성 여부. SubscriptionManager로 만료 판단 시도, Unsupported/예외 시 hasReceipt fallback.
        /// </summary>
        private bool IsSubscriptionActive(string productName)
        {
            var product = GetProduct(productName);
            if (product == null) return false;
            if (!product.hasReceipt) return false;

            try
            {
                var info = new SubscriptionManager(product, null).getSubscriptionInfo();
                var result = info.isSubscribed();
                if (result == Result.True) return true;
                if (result == Result.False) return false;
                // Result.Unsupported → fallback
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CHMIAP] SubscriptionManager 조회 실패: {e.Message}. hasReceipt fallback.");
            }

            return product.hasReceipt;
        }
    }
}
