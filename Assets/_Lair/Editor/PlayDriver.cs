using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# Play 모드 자동 조작 헬퍼. editor_invoke_method 는 인자를 못 넘기므로 인자는 Temp/PlayDriverArg.txt 로 전달한다.
    //# 직접 호출 가능한 오버로드: Click(string) / ClickIndex(int). MCP 용: ClickArg() / ClickIndexArg().
    public static class PlayDriver
    {
        private const int MaxList = 60;
        private const string NotPlaying = "NOT PLAYING";
        private const string ArgFile = "Temp/PlayDriverArg.txt";

        public static string GetState()
        {
            if (Application.isPlaying == false)
                return NotPlaying;

            List<Button> buttons = CollectButtons();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"scene={SceneManager.GetActiveScene().name} timeScale={Time.timeScale} buttons={buttons.Count}");
            sb.AppendLine($"popups={string.Join(", ", CollectPopups())}");
            int count = Math.Min(buttons.Count, MaxList);
            for (int i = 0; i < count; i++)
            {
                Button b = buttons[i];
                sb.AppendLine($"[{i}] {PathOf(b.transform)} | \"{TextOf(b)}\" | CHButton={HasCHButton(b)}");
            }

            if (buttons.Count > MaxList)
                sb.AppendLine($"... {buttons.Count - MaxList} more (ClickIndex 는 전체 목록 인덱스 사용 가능하나 표시는 {MaxList}개까지)");

            return sb.ToString();
        }

        public static string ClickArg() => Click(ReadArg());

        public static string ClickIndexArg()
        {
            int index;
            if (int.TryParse(ReadArg(), out index) == false)
                return "ARG NOT INT: " + ReadArg();

            return ClickIndex(index);
        }

        public static string Click(string pathOrText)
        {
            if (Application.isPlaying == false)
                return NotPlaying;

            if (string.IsNullOrEmpty(pathOrText))
                return "EMPTY ARG";

            string key = pathOrText.Trim();
            List<Button> matches = new List<Button>();
            foreach (Button b in CollectButtons())
            {
                if (PathOf(b.transform).EndsWith(key, StringComparison.Ordinal) || TextOf(b) == key)
                    matches.Add(b);
            }

            if (matches.Count == 1)
                return Invoke(matches[0]);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(matches.Count == 0 ? $"NO MATCH for '{key}'" : $"AMBIGUOUS '{key}' ({matches.Count}) - not clicked");
            foreach (Button b in matches)
            {
                sb.AppendLine($"  {PathOf(b.transform)} | \"{TextOf(b)}\"");
            }

            return sb.ToString();
        }

        public static string ClickIndex(int index)
        {
            if (Application.isPlaying == false)
                return NotPlaying;

            List<Button> buttons = CollectButtons();
            if (index < 0 || index >= buttons.Count)
                return $"INDEX OUT OF RANGE {index} (count={buttons.Count})";

            return Invoke(buttons[index]);
        }

        private static string Invoke(Button button)
        {
            string path = PathOf(button.transform);
            button.onClick.Invoke();
            return "CLICKED " + path;
        }

        private static string ReadArg()
        {
            if (File.Exists(ArgFile) == false)
                return string.Empty;

            return File.ReadAllText(ArgFile).Trim();
        }

        //# 인덱스 안정성을 위해 경로+형제순서로 정렬. GetState 와 ClickIndex 가 같은 순서를 쓴다.
        private static List<Button> CollectButtons()
        {
            List<Button> result = new List<Button>();
            foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (b.gameObject.activeInHierarchy && b.interactable)
                    result.Add(b);
            }

            result.Sort((x, y) => string.CompareOrdinal(SortKey(x.transform), SortKey(y.transform)));
            return result;
        }

        private static string SortKey(Transform t)
        {
            StringBuilder sb = new StringBuilder();
            Stack<Transform> chain = new Stack<Transform>();
            for (Transform c = t; c != null; c = c.parent)
            {
                chain.Push(c);
            }

            foreach (Transform c in chain)
            {
                sb.Append(c.GetSiblingIndex().ToString("D4")).Append('/');
            }

            return sb.ToString();
        }

        private static string PathOf(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }

            return sb.ToString();
        }

        private static string TextOf(Button b)
        {
            TMP_Text text = b.GetComponentInChildren<TMP_Text>(false);
            if (text == null)
                return string.Empty;

            return text.text.Replace("\n", " ").Trim();
        }

        private static bool HasCHButton(Button b)
        {
            foreach (MonoBehaviour mb in b.GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == "CHButton")
                    return true;
            }

            return false;
        }

        //# UIBase 파생 컴포넌트를 가진 활성 오브젝트를 팝업 루트로 본다 (패키지 asmdef 비참조 - 이름으로 판별).
        private static List<string> CollectPopups()
        {
            List<string> names = new List<string>();
            foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.gameObject.activeInHierarchy && IsUIBase(mb.GetType()))
                    names.Add(mb.gameObject.name);
            }

            return names;
        }

        private static bool IsUIBase(Type type)
        {
            for (Type t = type.BaseType; t != null; t = t.BaseType)
            {
                if (t.Name == "UIBase")
                    return true;
            }

            return false;
        }
    }
}
