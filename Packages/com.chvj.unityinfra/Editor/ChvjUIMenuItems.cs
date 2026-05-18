using UnityEditor;
using UnityEngine;

namespace ChvjUnityInfra.Editor
{
    /// <summary>
    /// GameObject > UI > Chvj 서브메뉴 — CHText/CHButton/CHToggle 생성.
    /// Unity 기본 UI 생성 메뉴(TMP 버전)를 호출 + 해당 CH 컴포넌트 부착.
    /// </summary>
    public static class ChvjUIMenuItems
    {
        private const int Priority = 11;

        [MenuItem("GameObject/UI/Chvj/CHText", false, Priority)]
        public static void CreateCHText(MenuCommand command)
        {
            // Unity 기본 TMP Text 생성 → 선택된 GameObject가 새 UI
            EditorApplication.ExecuteMenuItem("GameObject/UI/Text - TextMeshPro");
            var go = Selection.activeGameObject;
            if (go == null) return;

            go.name = "CHText";
            Undo.AddComponent<CHText>(go);
        }

        [MenuItem("GameObject/UI/Chvj/CHButton", false, Priority)]
        public static void CreateCHButton(MenuCommand command)
        {
            EditorApplication.ExecuteMenuItem("GameObject/UI/Button - TextMeshPro");
            var go = Selection.activeGameObject;
            if (go == null) return;

            go.name = "CHButton";
            Undo.AddComponent<CHButton>(go);
        }

        [MenuItem("GameObject/UI/Chvj/CHToggle", false, Priority)]
        public static void CreateCHToggle(MenuCommand command)
        {
            EditorApplication.ExecuteMenuItem("GameObject/UI/Toggle");
            var go = Selection.activeGameObject;
            if (go == null) return;

            go.name = "CHToggle";
            Undo.AddComponent<CHToggle>(go);
        }
    }
}
