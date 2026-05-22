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
    //# 카드 SO 25장 (패시브 15 + 액티브 10) + CardPool 2개 자동 생성 + Addressables 등록.
    //# SerializeReference 의 ICardEffect 슬롯 주입은 managedReferenceValue 사용.
    public static class LairCardPrefabBuilder
    {
        public const string CardDir = "Assets/_Lair/Art/Cards/Items";
        public const string PoolDir = "Assets/_Lair/Art/Cards";
        public const string IconDir = "Assets/_Lair/Art/Sprites/CardIcons";
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

        //# 패시브 15장 (HP 10% 트리거)
        public static readonly Spec[] PassiveSpecs = new[]
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
            new Spec { Id = ECardId.ArcherRangeBoost, Category = ECardCategory.Enhance,
                       DisplayName = "궁수 정밀", Description = "모든 궁수 사거리 +40%",
                       EffectFactory = () => new ArcherRangeBoostEffect() },
            new Spec { Id = ECardId.SpiderSlowBoost, Category = ECardCategory.Enhance,
                       DisplayName = "독거미", Description = "거미 둔화 효과 강화",
                       EffectFactory = () => new SpiderSlowBoostEffect() },
            new Spec { Id = ECardId.BatMoveSpeedBoost, Category = ECardCategory.Enhance,
                       DisplayName = "흡혈박쥐 떼", Description = "모든 박쥐 이동속도 +50%",
                       EffectFactory = () => new BatMoveSpeedBoostEffect() },
            new Spec { Id = ECardId.SpawnSlimes, Category = ECardCategory.Spawn,
                       DisplayName = "슬라임 소환", Description = "영웅 근처에 슬라임 3마리",
                       EffectFactory = () => new SpawnSlimesEffect() },
            new Spec { Id = ECardId.SpawnGolem, Category = ECardCategory.Spawn,
                       DisplayName = "골렘 소환", Description = "영웅 근처에 골렘 1마리",
                       EffectFactory = () => new SpawnGolemEffect() },
            new Spec { Id = ECardId.SpawnOrcs, Category = ECardCategory.Spawn,
                       DisplayName = "오크 증원", Description = "영웅 근처에 오크 2마리",
                       EffectFactory = () => new SpawnOrcsEffect() },
            new Spec { Id = ECardId.SpawnSpiders, Category = ECardCategory.Spawn,
                       DisplayName = "거미 둥지", Description = "영웅 근처에 거미 2마리",
                       EffectFactory = () => new SpawnSpidersEffect() },
            new Spec { Id = ECardId.SpawnBats, Category = ECardCategory.Spawn,
                       DisplayName = "박쥐 무리", Description = "영웅 근처에 박쥐 5마리",
                       EffectFactory = () => new SpawnBatsEffect() },
            new Spec { Id = ECardId.ReplaceSlimesToGolem, Category = ECardCategory.Replace,
                       DisplayName = "융합", Description = "모든 슬라임 → 골렘 1마리",
                       EffectFactory = () => new ReplaceSlimesToGolemEffect() },
            new Spec { Id = ECardId.ReplaceOrcsToArchers, Category = ECardCategory.Replace,
                       DisplayName = "사격 훈련", Description = "모든 오크 → 궁수 (1:1)",
                       EffectFactory = () => new ReplaceOrcsToArchersEffect() },
            new Spec { Id = ECardId.HeroPoisonAura, Category = ECardCategory.Environment,
                       DisplayName = "독 안개", Description = "영웅 발 밑에 독 장판 (DPS 5)",
                       EffectFactory = () => new HeroPoisonAuraEffect() },
            new Spec { Id = ECardId.HeroAttackDown, Category = ECardCategory.Environment,
                       DisplayName = "약화의 저주", Description = "영웅 공격력 영구 -25%",
                       EffectFactory = () => new HeroAttackDownEffect() },
        };

        //# 액티브 10장 (30초 트리거)
        public static readonly Spec[] ActiveSpecs = new[]
        {
            new Spec { Id = ECardId.Fear, Category = ECardCategory.Environment,
                       DisplayName = "공포", Description = "영웅 3초간 도망",
                       EffectFactory = () => new FearEffect() },
            new Spec { Id = ECardId.Bleed, Category = ECardCategory.Environment,
                       DisplayName = "출혈", Description = "영웅 이동 시 HP 감소 (10초)",
                       EffectFactory = () => new BleedEffect() },
            new Spec { Id = ECardId.Weaken, Category = ECardCategory.Environment,
                       DisplayName = "무력화", Description = "영웅 데미지 -50% (10초)",
                       EffectFactory = () => new WeakenEffect() },
            new Spec { Id = ECardId.Slow, Category = ECardCategory.Environment,
                       DisplayName = "둔화", Description = "영웅 이동속도 -50% (10초)",
                       EffectFactory = () => new SlowEffect() },
            new Spec { Id = ECardId.Frenzy, Category = ECardCategory.Enhance,
                       DisplayName = "광폭화", Description = "모든 몬스터 공속 +50% (10초)",
                       EffectFactory = () => new FrenzyEffect() },
            new Spec { Id = ECardId.Multiply, Category = ECardCategory.Spawn,
                       DisplayName = "증식", Description = "최다 몬스터 종 즉시 2배",
                       EffectFactory = () => new MultiplyEffect() },
            new Spec { Id = ECardId.BloodThirst, Category = ECardCategory.Enhance,
                       DisplayName = "피의 갈증", Description = "처치 시 주변 몬스터 회복 (30초)",
                       EffectFactory = () => new BloodThirstEffect() },
            new Spec { Id = ECardId.IronWill, Category = ECardCategory.Enhance,
                       DisplayName = "강철 의지", Description = "몬스터 받는 데미지 -30% (15초)",
                       EffectFactory = () => new IronWillEffect() },
            new Spec { Id = ECardId.TimeStop, Category = ECardCategory.Environment,
                       DisplayName = "시간 정지", Description = "영웅 5초 멈춤",
                       EffectFactory = () => new TimeStopEffect() },
            new Spec { Id = ECardId.Berserk, Category = ECardCategory.Enhance,
                       DisplayName = "폭주", Description = "몬스터 HP -50%, 데미지 +200% (15초)",
                       EffectFactory = () => new BerserkEffect() },
        };

        //# 패시브 15 + 액티브 10 비파괴 재빌드. 기존 카드의 효과 튜닝값은 보존.
        [MenuItem("Lair/Setup/B3 - Rebuild All Cards")]
        public static void RebuildAllCards()
        {
            EnsureDir(CardDir);
            EnsureDir(PoolDir);
            EnsureDir(IconDir);
            RemoveStaleCards();
            BuildCardsAndPool(PassiveSpecs, EData.CardPool_Passive);
            BuildCardsAndPool(ActiveSpecs, EData.CardPool_Active);
            Debug.Log("[LairCardPrefabBuilder] 카드 25장 + 풀 2개 재빌드 완료 (비파괴)");
        }

        //# spec 목록에 없는 .asset 만 삭제 (폐기 ECardId). 유효 카드는 보존.
        private static void RemoveStaleCards()
        {
            var valid = new HashSet<string>();
            foreach (var s in PassiveSpecs) valid.Add(s.Id.ToString());
            foreach (var s in ActiveSpecs)  valid.Add(s.Id.ToString());

            foreach (var path in Directory.GetFiles(CardDir, "*.asset"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (valid.Contains(name) == false)
                {
                    AssetDatabase.DeleteAsset(path.Replace('\\', '/'));
                    Debug.Log($"[LairCardPrefabBuilder] stale 카드 삭제: {name}");
                }
            }
        }

        //# Spec 묶음 → CardData N장 + CardPool 1개 생성 + Addressables 등록.
        private static void BuildCardsAndPool(Spec[] specs, EData poolKey)
        {
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
                string path = $"{CardDir}/{spec.Id}.asset";
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                bool isNew = card == null;
                if (isNew) card = ScriptableObject.CreateInstance<CardData>();

                var so = new SerializedObject(card);
                so.FindProperty("_id").enumValueIndex       = (int)spec.Id;
                so.FindProperty("_category").enumValueIndex = (int)spec.Category;
                so.FindProperty("_displayName").stringValue = spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description;
                //# 빌드 패널 아이콘 — ECardId 이름 PNG 자동 배정 (없으면 null). _effect 와 달리 매번 재설정.
                so.FindProperty("_icon").objectReferenceValue = LoadCardIcon(spec.Id);

                //# 비파괴 — 기존 카드의 _effect(튜닝값) 보존. 신규/타입불일치 시에만 새 효과.
                var effectProp = so.FindProperty("_effect");
                var wanted = spec.EffectFactory();
                var existing = effectProp.managedReferenceValue;
                if (existing == null || existing.GetType() != wanted.GetType())
                    effectProp.managedReferenceValue = wanted;

                so.ApplyModifiedPropertiesWithoutUndo();

                if (isNew) AssetDatabase.CreateAsset(card, path);
                else       EditorUtility.SetDirty(card);
                RegisterAddressable(settings, group, path, spec.Id.ToString());

                createdCards.Add(card);
                Debug.Log($"[LairCardPrefabBuilder] CardData {(isNew ? "생성" : "갱신")}: {spec.Id}");
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

        //# ECardId 이름의 PNG 를 Sprite 로 로드. 미존재 시 null.
        //# PNG 의 textureType=Sprite / spriteImportMode=Single 임포트 설정을 보정.
        private static Sprite LoadCardIcon(ECardId id)
        {
            string path = $"{IconDir}/{id}.png";
            if (File.Exists(path) == false) return null;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite
                                || imp.spriteImportMode != SpriteImportMode.Single))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
