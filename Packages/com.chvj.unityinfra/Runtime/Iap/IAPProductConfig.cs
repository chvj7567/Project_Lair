using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace ChvjUnityInfra
{
    /// <summary>
    /// IAP 상품 정의 ScriptableObject.
    /// 에셋 위치: Assets/Resources/ChvjUnityInfra/IAPProductConfig.asset
    /// CHMIAP가 Init 시 Resources.Load로 자동 로드.
    /// </summary>
    [CreateAssetMenu(fileName = "IAPProductConfig", menuName = "ChvjUnityInfra/IAP Product Config", order = 0)]
    public class IAPProductConfig : ScriptableObject
    {
        [Serializable]
        public class Product
        {
            [Tooltip("게임 내부에서 사용할 상품 이름 (식별자)")]
            public string productName;

            [Tooltip("스토어에 등록된 상품 ID (예: com.example.product1)")]
            public string productID;

            [Tooltip("Consumable=소모성, NonConsumable=영구, Subscription=구독")]
            public ProductType productType;
        }

        public Product[] Products;
    }
}
