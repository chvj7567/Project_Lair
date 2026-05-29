using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Lair.Card;

namespace Lair.EditorTools
{
    //# CardPool SO ↔ card_pools.json 양방향 동기화.
    public static class CardPoolSyncer
    {
        private const string JsonPath    = "Assets/_Lair/Data/Json/card_pools.json";
        private const string PassivePath = "Assets/_Lair/Art/Cards/CardPool_Passive.asset";
        private const string ActivePath  = "Assets/_Lair/Art/Cards/CardPool_Active.asset";
        private const string CardDir     = "Assets/_Lair/Art/Cards/Items";

        public static void Export()
        {
            CardPool passive = AssetDatabase.LoadAssetAtPath<CardPool>(PassivePath);
            CardPool active  = AssetDatabase.LoadAssetAtPath<CardPool>(ActivePath);

            CardPoolDto dto = new CardPoolDto
            {
                Passive = passive?.Cards.Where(c => c != null).Select(c => c.Id.ToString()).ToList() ?? new List<string>(),
                Active  = active?.Cards.Where(c => c != null).Select(c => c.Id.ToString()).ToList()  ?? new List<string>()
            };

            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(dto, JsonSyncSettings.Build()), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[CardPoolSyncer] Export → {JsonPath}");
        }

        public static void Import()
        {
            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            CardPoolDto dto = JsonConvert.DeserializeObject<CardPoolDto>(json);

            ApplyPool(PassivePath, dto.Passive);
            ApplyPool(ActivePath,  dto.Active);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardPoolSyncer] Import ← {JsonPath}");
        }

        private static void ApplyPool(string poolPath, List<string> cardIds)
        {
            CardPool pool = AssetDatabase.LoadAssetAtPath<CardPool>(poolPath);
            if (pool == null)
            {
                Debug.LogWarning($"[CardPoolSyncer] 풀 없음: {poolPath}");
                return;
            }

            SerializedObject so = new SerializedObject(pool);
            SerializedProperty listProp = so.FindProperty("_cards");
            listProp.arraySize = cardIds.Count;

            for (int i = 0; i < cardIds.Count; ++i)
            {
                string cardPath = $"{CardDir}/{cardIds[i]}.asset";
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(cardPath);
                if (card == null)
                {
                    Debug.LogWarning($"[CardPoolSyncer] 카드 없음: {cardIds[i]}");
                }
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = card;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pool);
        }

        private static void EnsureDir(string dir)
        {
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
