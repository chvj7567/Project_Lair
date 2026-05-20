using System.Collections.Generic;
using System.IO;
using Lair.Card;
using Lair.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# B1 — 카드 SO 7장 + CardPool_Passive SO 자동 생성 + Addressables 등록.
    //# SerializeReference 의 ICardEffect 슬롯 주입은 managedReferenceValue 사용.
    public static class LairCardPrefabBuilder
    {
        public const string CardDir = "Assets/_Lair/Data/Cards/Cards";
        public const string PoolDir = "Assets/_Lair/Data/Cards";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";

        public class Spec
        {
            public ECardId Id;
            public ECardCategory Category;
            public string DisplayName;
            public string Description;
            public System.Func<ICardEffect> EffectFactory;
        }

        public static readonly Spec[] AllSpecs = new[]
        {
            new Spec { Id = ECardId.SlimeHpBoost, Category = ECardCategory.Enhance,
                       DisplayName = "끈질긴 슬라임", Description = "모든 슬라임 HP +50%",
                       EffectFactory = () => new SlimeHpBoostEffect() },
            new Spec { Id = ECardId.GolemDamageBoost, Category = ECardCategory.Enhance,
                       DisplayName = "강철 골렘", Description = "모든 골렘 데미지 +50%",
                       EffectFactory = () => new GolemDamageBoostEffect() },
            new Spec { Id = ECardId.OrcAtkSpeed, Category = ECardCategory.Enhance,
                       DisplayName = "광폭 오크", Description = "모든 오크 공격속도 +30%",
                       EffectFactory = () => new OrcAtkSpeedEffect() },
            new Spec { Id = ECardId.SpawnSlimes, Category = ECardCategory.Spawn,
                       DisplayName = "슬라임 소환", Description = "영웅 근처에 슬라임 3마리",
                       EffectFactory = () => new SpawnSlimesEffect() },
            new Spec { Id = ECardId.SpawnGolem, Category = ECardCategory.Spawn,
                       DisplayName = "골렘 소환", Description = "영웅 근처에 골렘 1마리",
                       EffectFactory = () => new SpawnGolemEffect() },
            new Spec { Id = ECardId.ReplaceSlimesToGolem, Category = ECardCategory.Replace,
                       DisplayName = "융합", Description = "모든 슬라임 → 골렘 1마리",
                       EffectFactory = () => new ReplaceSlimesToGolemEffect() },
            new Spec { Id = ECardId.HeroPoisonAura, Category = ECardCategory.Environment,
                       DisplayName = "독 안개", Description = "영웅 발 밑에 독 장판 (DPS 5)",
                       EffectFactory = () => new HeroPoisonAuraEffect() },
        };

        //# B2 — 액티브 5장
        public static readonly Spec[] ActiveSpecs = new[]
        {
            new Spec { Id = ECardId.MonsterAoeDamage, Category = ECardCategory.Environment,
                       DisplayName = "전체 데미지", Description = "모든 몬스터에 50 데미지",
                       EffectFactory = () => new MonsterAoeDamageEffect() },
            new Spec { Id = ECardId.HeroSlow, Category = ECardCategory.Environment,
                       DisplayName = "영웅 둔화", Description = "영웅 이동속도 40% 감소 5초",
                       EffectFactory = () => new HeroSlowEffect() },
            new Spec { Id = ECardId.HeroSilence, Category = ECardCategory.Environment,
                       DisplayName = "영웅 침묵", Description = "영웅 공격 5초 정지",
                       EffectFactory = () => new HeroSilenceEffect() },
            new Spec { Id = ECardId.InstantSpawnGolem, Category = ECardCategory.Spawn,
                       DisplayName = "골렘 즉시 소환", Description = "골렘 1마리 즉시 소환",
                       EffectFactory = () => new InstantSpawnGolemEffect() },
            new Spec { Id = ECardId.InstantSpawnSlimes, Category = ECardCategory.Spawn,
                       DisplayName = "슬라임 떼", Description = "슬라임 3마리 즉시 소환",
                       EffectFactory = () => new InstantSpawnSlimesEffect() },
        };

        [MenuItem("Lair/Setup/B1 - Build Card Assets")]
        public static void BuildAllCards()
        {
            BuildCardsAndPool(AllSpecs, EData.CardPool_Passive);
        }

        [MenuItem("Lair/Setup/B2 - Build Active Cards")]
        public static void BuildActiveCards()
        {
            BuildCardsAndPool(ActiveSpecs, EData.CardPool_Active);
        }

        //# Spec 묶음 → CardData N장 + CardPool 1개 생성 + Addressables 등록.
        private static void BuildCardsAndPool(Spec[] specs, EData poolKey)
        {
            EnsureDir(CardDir);
            EnsureDir(PoolDir);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                LairSetup.EnsureAddressablesSetup();
                settings = AddressableAssetSettingsDefaultObject.Settings;
            }
            var group = settings.FindGroup(ResourceGroup);

            var createdCards = new List<CardData>();

            foreach (var spec in specs)
            {
                var card = ScriptableObject.CreateInstance<CardData>();
                var so = new SerializedObject(card);
                so.FindProperty("_id").enumValueIndex       = (int)spec.Id;
                so.FindProperty("_category").enumValueIndex = (int)spec.Category;
                so.FindProperty("_displayName").stringValue = spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description;
                so.FindProperty("_effect").managedReferenceValue = spec.EffectFactory();
                so.ApplyModifiedPropertiesWithoutUndo();

                string path = $"{CardDir}/{spec.Id}.asset";
                AssetDatabase.CreateAsset(card, path);
                RegisterAddressable(settings, group, path, spec.Id.ToString());

                createdCards.Add(card);
                Debug.Log($"[LairCardPrefabBuilder] CardData 생성: {spec.Id}");
            }

            var pool = ScriptableObject.CreateInstance<CardPool>();
            var poolSo = new SerializedObject(pool);
            var listProp = poolSo.FindProperty("_cards");
            listProp.arraySize = createdCards.Count;
            for (int i = 0; i < createdCards.Count; ++i)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = createdCards[i];
            }
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            string poolPath = $"{PoolDir}/{poolKey}.asset";
            AssetDatabase.CreateAsset(pool, poolPath);
            RegisterAddressable(settings, group, poolPath, poolKey.ToString());
            Debug.Log($"[LairCardPrefabBuilder] {poolKey} 생성 + {createdCards.Count}장 등록");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RegisterAddressable(AddressableAssetSettings settings,
            AddressableAssetGroup group, string assetPath, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);
        }

        private static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }
    }
}
