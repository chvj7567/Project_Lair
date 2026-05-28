using System;
using System.Collections.Generic;
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
    //# Art/Json/Strings_Ko.json (Addressable) 을 JsonArrayUtility 로 파싱해 id → text 테이블 구축.
    public class StringTableProvider : IStringProvider
    {
        private readonly Dictionary<int, string> _table = new();

        public void Load(TextAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("[StringTableProvider] asset 이 null");
                return;
            }
            StringEntry[] entries = JsonArrayUtility.FromJsonArray<StringEntry>(asset.text);
            foreach (StringEntry entry in entries)
                _table[entry.id] = entry.text;
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
