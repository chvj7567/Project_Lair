using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Data
{
    [Serializable]
    public class StringEntry
    {
        public int id;
        public string text;
    }

    //# CHText.StringProvider 에 등록하는 구현체.
    //# StreamingAssets/strings_ko.json 을 JsonArrayUtility 로 파싱해 id → text 테이블 구축.
    public class StringTableProvider : IStringProvider
    {
        private readonly Dictionary<int, string> _table = new();

        public void Load(string fileName = "strings_ko.json")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (File.Exists(path) == false)
            {
                Debug.LogError($"[StringTableProvider] 파일 없음: {path}");
                return;
            }
            string json = File.ReadAllText(path, Encoding.UTF8);
            StringEntry[] entries = JsonArrayUtility.FromJsonArray<StringEntry>(json);
            foreach (StringEntry entry in entries)
            {
                _table[entry.id] = entry.text;
            }
        }

        public string GetString(int stringID)
        {
            if (_table.TryGetValue(stringID, out string text))
                return text;
            Debug.LogWarning($"[StringTableProvider] ID {stringID} 없음");
            return string.Empty;
        }
    }
}
