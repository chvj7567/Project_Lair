using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Lair.Card;
using Lair.Data;

namespace Lair.EditorTools
{
    //# CardData SO ↔ cards.json 양방향 동기화.
    public static class CardDataSyncer
    {
        private const string JsonPath = "Assets/_Lair/Data/Json/cards.json";
        private const string CardDir  = "Assets/_Lair/Art/Cards/Items";

        //# CardData SO 목록 → JSON 문자열
        public static string ExportToJson(IEnumerable<CardData> cards)
        {
            List<CardDataDto> dtos = new List<CardDataDto>();
            foreach (CardData card in cards)
            {
                dtos.Add(new CardDataDto
                {
                    Id          = card.Id.ToString(),
                    //# 카드 리뉴얼 v0.6 — card.Category → card.Axis.
                    //# json 의 "Category" 키명은 유지 (호환). 값은 EBuildAxis 의 enum 명("Tank"/"Dps"/...) 출력.
                    Category    = card.Axis.ToString(),
                    DisplayName = card.DisplayName,
                    Description = card.Description,
                    Effect      = card.Effect
                });
            }
            return JsonConvert.SerializeObject(dtos, JsonSyncSettings.Build());
        }

        //# DTO 를 기존 CardData SO 에 적용 (SerializedObject 사용, LairCardPrefabBuilder 동일 패턴).
        //# _icon 필드는 건드리지 않음 — LairCardPrefabBuilder 가 관리.
        //# Enum 파싱 실패 시 false 반환 — Import 에서 SO 생성/저장을 skip.
        public static bool ApplyDto(CardDataDto dto, CardData card)
        {
            if (Enum.TryParse(dto.Id, out ECardId cardId) == false)
            {
                Debug.LogWarning($"[CardDataSyncer] ECardId 파싱 실패 — skip: {dto.Id}");
                return false;
            }
            //# 카드 리뉴얼 v0.6 — 구 카테고리 → EBuildAxis. json 의 "Category" 키 값으로 axis enum 명 파싱.
            //# 기존 json ("Enhance/Spawn/Replace/Environment") 은 본 시점에 파싱 실패 → skip (의도된 결과 — Phase 2 Task 14 에서 json 갱신).
            if (Enum.TryParse(dto.Category, out EBuildAxis axis) == false)
            {
                Debug.LogWarning($"[CardDataSyncer] EBuildAxis 파싱 실패 — skip: {dto.Category} (카드 ID: {dto.Id})");
                return false;
            }

            SerializedObject so = new SerializedObject(card);
            so.FindProperty("_id").enumValueIndex          = (int)cardId;
            so.FindProperty("_axis").enumValueIndex    = (int)axis;
            so.FindProperty("_displayName").stringValue    = dto.DisplayName;
            so.FindProperty("_description").stringValue    = dto.Description;
            so.FindProperty("_effect").managedReferenceValue = dto.Effect;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);
            return true;
        }

        //# AssetDatabase 에서 전체 CardData 로드 → JSON 파일 저장
        public static void Export()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { CardDir });
            List<CardData> cards = new List<CardData>();
            foreach (string guid in guids)
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, ExportToJson(cards), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[CardDataSyncer] {cards.Count}장 Export → {JsonPath}");
        }

        //# JSON 파일 → CardData SO 생성/갱신. ApplyDto false 반환 시 해당 카드만 skip.
        public static void Import()
        {
            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            List<CardDataDto> dtos = JsonConvert.DeserializeObject<List<CardDataDto>>(json, JsonSyncSettings.Build());

            int successCount = 0;
            int skipCount = 0;
            foreach (CardDataDto dto in dtos)
            {
                string assetPath = $"{CardDir}/{dto.Id}.asset";
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                bool isNew = card == null;
                if (isNew)
                {
                    card = ScriptableObject.CreateInstance<CardData>();
                }

                bool applied = ApplyDto(dto, card);
                if (applied == false)
                {
                    if (isNew)
                    {
                        UnityEngine.Object.DestroyImmediate(card);
                    }
                    skipCount++;
                    continue;
                }

                if (isNew)
                {
                    AssetDatabase.CreateAsset(card, assetPath);
                    Debug.Log($"[CardDataSyncer] 신규 생성: {dto.Id}");
                }
                successCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardDataSyncer] {successCount}장 Import ← {JsonPath} (skip: {skipCount})");
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
