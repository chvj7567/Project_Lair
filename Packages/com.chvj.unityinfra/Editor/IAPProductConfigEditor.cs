#if UNITY_INFRA_IAP
using UnityEditor;
using UnityEngine;

namespace ChvjUnityInfra.Editor
{
    [CustomEditor(typeof(IAPProductConfig))]
    public class IAPProductConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "IAP 상품을 정의합니다.\n" +
                "- productName: 게임 코드에서 식별자로 사용 (예: \"RemoveAD\")\n" +
                "- productID: 스토어에 등록된 상품 ID (예: \"com.example.product1\")\n" +
                "- productType: Consumable(소모) / NonConsumable(영구) / Subscription(구독)\n" +
                "상품 정의 없이 CHMIAP.Init() 호출 시 경고만 표시되고 초기화는 건너뜁니다.",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
#endif
