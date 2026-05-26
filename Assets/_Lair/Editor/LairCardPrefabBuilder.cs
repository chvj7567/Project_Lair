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
            new Spec { Id = ECardId.WispHpBoost, Category = ECardCategory.Enhance,
                       DisplayName = "끈질긴 위스프", Description = "모든 위스프 HP +50%",
                       EffectFactory = () => new WispHpBoostEffect() },
            new Spec { Id = ECardId.WraithDamageBoost, Category = ECardCategory.Enhance,
                       DisplayName = "강철 레이스", Description = "모든 레이스 데미지 +50%",
                       EffectFactory = () => new WraithDamageBoostEffect() },
            new Spec { Id = ECardId.ReaperAtkSpeed, Category = ECardCategory.Enhance,
                       DisplayName = "광폭 리퍼", Description = "모든 리퍼 공격속도 +30%",
                       EffectFactory = () => new ReaperAtkSpeedEffect() },
            new Spec { Id = ECardId.HexRangeBoost, Category = ECardCategory.Enhance,
                       DisplayName = "헥스 정밀", Description = "모든 헥스 사거리 +40%",
                       EffectFactory = () => new HexRangeBoostEffect() },
            new Spec { Id = ECardId.PlagueSlowBoost, Category = ECardCategory.Enhance,
                       DisplayName = "독성 플레이그", Description = "플레이그 둔화 효과 강화",
                       EffectFactory = () => new PlagueSlowBoostEffect() },
            new Spec { Id = ECardId.PhantomMoveSpeedBoost, Category = ECardCategory.Enhance,
                       DisplayName = "흡혈 팬텀 떼", Description = "모든 팬텀 이동속도 +50%",
                       EffectFactory = () => new PhantomMoveSpeedBoostEffect() },
            new Spec { Id = ECardId.SpawnWisps, Category = ECardCategory.Spawn,
                       DisplayName = "위스프 소환", Description = "위스프 Spawner 출력 +1",
                       EffectFactory = () => new SpawnWispsEffect() },
            new Spec { Id = ECardId.SpawnWraith, Category = ECardCategory.Spawn,
                       DisplayName = "레이스 소환", Description = "레이스 Spawner 출력 +1",
                       EffectFactory = () => new SpawnWraithEffect() },
            new Spec { Id = ECardId.SpawnReapers, Category = ECardCategory.Spawn,
                       DisplayName = "리퍼 증원", Description = "리퍼 Spawner 출력 +1",
                       EffectFactory = () => new SpawnReapersEffect() },
            new Spec { Id = ECardId.SpawnPlagues, Category = ECardCategory.Spawn,
                       DisplayName = "플레이그 둥지", Description = "플레이그 Spawner 출력 +1",
                       EffectFactory = () => new SpawnPlaguesEffect() },
            new Spec { Id = ECardId.SpawnPhantoms, Category = ECardCategory.Spawn,
                       DisplayName = "팬텀 무리", Description = "팬텀 Spawner 출력 +1",
                       EffectFactory = () => new SpawnPhantomsEffect() },
            new Spec { Id = ECardId.ReplaceWispsToWraith, Category = ECardCategory.Replace,
                       DisplayName = "융합", Description = "위스프 Spawner → 레이스 생산",
                       EffectFactory = () => new ReplaceWispsToWraithEffect() },
            new Spec { Id = ECardId.ReplaceReapersToHex, Category = ECardCategory.Replace,
                       DisplayName = "주술 훈련", Description = "리퍼 Spawner → 헥스 생산",
                       EffectFactory = () => new ReplaceReapersToHexEffect() },
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
                Debug.LogError("[LairCardPrefabBuilder] Addressables 미설정 — Window > Asset Management > Addressables Groups 로 초기화 필요");
                return;
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
