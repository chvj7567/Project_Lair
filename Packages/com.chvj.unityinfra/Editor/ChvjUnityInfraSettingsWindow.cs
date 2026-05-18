using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ChvjUnityInfra.Editor
{
    /// <summary>
    /// ChvjUnityInfra 패키지의 모듈 설정 윈도우.
    /// Tools/ChvjUnityInfra/Settings 메뉴로 열림.
    /// 탭: Ads / IAP / Social — 각 탭에서 모듈 토글 + Config 편집 + 사용 가이드.
    /// </summary>
    public class ChvjUnityInfraSettingsWindow : EditorWindow
    {
        private const string ADS_DEFINE = "UNITY_INFRA_ADS";
        private const string IAP_DEFINE = "UNITY_INFRA_IAP";
        private const string SOCIAL_DEFINE = "UNITY_INFRA_SOCIAL";

        private const string AD_CONFIG_PATH = "Assets/Resources/ChvjUnityInfra/AdConfig.asset";
        private const string IAP_CONFIG_PATH = "Assets/Resources/ChvjUnityInfra/IAPProductConfig.asset";

        private static readonly string[] TabLabels = { "Ads", "IAP", "Social" };

        private const string TabIndexKey = "ChvjUnityInfra.SettingsWindow.TabIndex";

        private int _tabIndex;
        private Vector2 _scroll;

        [MenuItem("Tools/ChvjUnityInfra/Settings", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<ChvjUnityInfraSettingsWindow>("ChvjUnityInfra");
            window.minSize = new Vector2(500, 480);
            window.Show();
        }

        private void OnEnable()
        {
            _tabIndex = SessionState.GetInt(TabIndexKey, 0);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            int next = GUILayout.Toolbar(_tabIndex, TabLabels, GUILayout.Height(28));
            if (next != _tabIndex)
            {
                _tabIndex = next;
                SessionState.SetInt(TabIndexKey, _tabIndex);
            }
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tabIndex)
            {
                case 0: DrawAdsTab(); break;
                case 1: DrawIapTab(); break;
                case 2: DrawSocialTab(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        // ────────── Ads ──────────

        private void DrawAdsTab()
        {
            EditorGUILayout.LabelField("AdMob 광고 모듈", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawToggle("Use Admob", ADS_DEFINE);

            EditorGUILayout.Space();

#if UNITY_INFRA_ADS
            if (GUILayout.Button("AdConfig 에셋 편집", GUILayout.Height(24)))
            {
                OpenOrCreateConfig<AdConfig>(AD_CONFIG_PATH);
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "사용 스텝:\n" +
                "1. 'Use Admob' 체크 (이미 켜져 있음)\n" +
                "2. 'AdConfig 에셋 편집' 클릭 → Inspector에서 프로덕션 광고 ID 3종 입력\n" +
                "3. 게임 부팅 코드에 추가:\n" +
                "   #if UNITY_INFRA_ADS\n" +
                "   CHMAdmob.Instance.Init();\n" +
                "   #endif\n" +
                "4. 광고 표시:\n" +
                "   CHMAdmob.Instance.ShowBanner(AdPosition.Bottom);\n" +
                "   CHMAdmob.Instance.ShowInterstitialAd();\n" +
                "   CHMAdmob.Instance.ShowRewardedAd();\n" +
                "\n" +
                "안전장치:\n" +
                "- 에디터 빌드는 항상 테스트 광고\n" +
                "- 프로덕션 ID 비어있으면 디바이스 빌드에서도 테스트 광고로 자동 fallback\n" +
                "- AdConfig 에셋 없으면 경고 + 기본 테스트 광고",
                MessageType.Info);
#else
            EditorGUILayout.HelpBox(
                "Admob 모듈이 꺼져 있습니다.\n" +
                "'Use Admob' 체크 → 컴파일 완료 후 AdConfig 편집 + 사용 가이드가 표시됩니다.\n" +
                "전제: Google Mobile Ads Unity 패키지 임포트 필요.",
                MessageType.Warning);
#endif
        }

        // ────────── IAP ──────────

        private void DrawIapTab()
        {
            EditorGUILayout.LabelField("In-App Purchasing 모듈", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawToggle("Use IAP", IAP_DEFINE);

            EditorGUILayout.Space();

#if UNITY_INFRA_IAP
            if (GUILayout.Button("IAPProductConfig 에셋 편집", GUILayout.Height(24)))
            {
                OpenOrCreateConfig<IAPProductConfig>(IAP_CONFIG_PATH);
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "사용 스텝:\n" +
                "1. 'Use IAP' 체크 (이미 켜져 있음)\n" +
                "2. 'IAPProductConfig 에셋 편집' 클릭 → Inspector에서 Products 배열 추가\n" +
                "   - productName: 게임 코드 식별자 (예: \"RemoveAD\")\n" +
                "   - productID: 스토어 상품 ID (예: \"com.yourgame.removead\")\n" +
                "   - productType: Consumable / NonConsumable / Subscription\n" +
                "3. 게임 부팅 코드에 추가:\n" +
                "   #if UNITY_INFRA_IAP\n" +
                "   CHMIAP.Instance.purchaseState += OnPurchaseResult;\n" +
                "   CHMIAP.Instance.Init();\n" +
                "   #endif\n" +
                "4. 구매 호출:\n" +
                "   CHMIAP.Instance.Purchase(\"RemoveAD\");\n" +
                "\n" +
                "주의: Ads와 달리 IAP는 fallback 없음.\n" +
                "Products 비어있으면 Init이 경고만 띄우고 초기화 안 함.\n" +
                "실제 결제 시험은 Google Play/App Store 상품 등록 + 테스트 계정 필요.",
                MessageType.Info);
#else
            EditorGUILayout.HelpBox(
                "IAP 모듈이 꺼져 있습니다.\n" +
                "'Use IAP' 체크 → 컴파일 완료 후 IAPProductConfig 편집 + 사용 가이드가 표시됩니다.\n" +
                "전제: Window > Package Manager에서 'In-App Purchasing' 패키지 설치 필요.",
                MessageType.Warning);
#endif
        }

        // ────────── Social ──────────

        private void DrawSocialTab()
        {
            EditorGUILayout.LabelField("Google Play Games Services (Android)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawToggle("Use GPGS", SOCIAL_DEFINE);

            EditorGUILayout.Space();

#if UNITY_INFRA_SOCIAL
            EditorGUILayout.HelpBox(
                "사용 스텝:\n" +
                "1. 'Use GPGS' 체크 (이미 켜져 있음)\n" +
                "2. Google Play Console에서 게임 등록 + 업적/리더보드/이벤트 ID 발급\n" +
                "3. GPGS SDK 셋업 (Window > Google Play Games > Setup > Android setup)\n" +
                "4. 게임 부팅 코드에 추가 (Android 한정):\n" +
                "   #if UNITY_INFRA_SOCIAL && UNITY_ANDROID\n" +
                "   CHMGPGS.Instance.Login((success, user) => { ... });\n" +
                "   #endif\n" +
                "5. 기능 호출:\n" +
                "   CHMGPGS.Instance.SaveCloud(fileName, data, ...);\n" +
                "   CHMGPGS.Instance.UnlockAchievement(\"gpgs_id\");\n" +
                "   CHMGPGS.Instance.ReportLeaderboard(\"gpgs_id\", score);\n" +
                "\n" +
                "주의: Android 플랫폼에서만 동작. iOS Game Center는 별도 구현 필요.\n" +
                "Config 에셋은 없음 — GPGS ID들은 게임 코드/스트링 테이블에 직접 입력.",
                MessageType.Info);
#else
            EditorGUILayout.HelpBox(
                "GPGS 모듈이 꺼져 있습니다.\n" +
                "'Use GPGS' 체크 → 컴파일 완료 후 사용 가이드가 표시됩니다.\n" +
                "전제: Google Play Games Plugin for Unity 임포트 필요 + Android 플랫폼 빌드.",
                MessageType.Warning);
#endif
        }

        // ────────── 공통 위젯 ──────────

        private static void DrawToggle(string label, string define)
        {
            bool current = IsDefineEnabled(define);
            bool next = EditorGUILayout.ToggleLeft(new GUIContent($"  {label}", $"Scripting Define Symbol: {define}"), current, GUILayout.Height(22));
            if (next != current)
            {
                SetDefine(define, next);
            }
        }

        private static void OpenOrCreateConfig<T>(string path) where T : ScriptableObject
        {
            EnsureResourcesFolder();

            var config = AssetDatabase.LoadAssetAtPath<T>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"[ChvjUnityInfra] {typeof(T).Name} 에셋 생성: {path}");
            }

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/ChvjUnityInfra"))
                AssetDatabase.CreateFolder("Assets/Resources", "ChvjUnityInfra");
        }

        // ────────── Define 유틸 ──────────

        private static NamedBuildTarget CurrentTarget()
        {
            return NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        }

        private static bool IsDefineEnabled(string define)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbols(CurrentTarget());
            return symbols.Split(';').Any(s => s.Trim() == define);
        }

        private static void SetDefine(string define, bool enabled)
        {
            var target = CurrentTarget();
            string symbols = PlayerSettings.GetScriptingDefineSymbols(target);
            List<string> list = symbols.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            bool changed = false;
            if (enabled && !list.Contains(define))
            {
                list.Add(define);
                changed = true;
            }
            else if (!enabled && list.Contains(define))
            {
                list.Remove(define);
                changed = true;
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
            }
        }
    }
}
