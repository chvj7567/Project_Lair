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
    //# 카드 SO 28장 (패시브 16 + 액티브 12) + CardPool 2개 자동 생성 + Addressables 등록.
    //# 카드 리뉴얼 v0.6 (2026-05-31) — 4축(Tank/Dps/Debuff/Swarm) × (P4 + A3) 균등 분배.
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
            //# 카드 리뉴얼 v0.6 — Category(구 4종 카테고리 enum) → Axis(EBuildAxis).
            //# Phase 1 임시 매핑 (Enhance=Tank, Spawn=Dps, Replace=Debuff, Environment=Swarm).
            //# Phase 2 Task 13 에서 기획서 §3 4축 라인업대로 재할당.
            public EBuildAxis Category;
            public string DisplayName;
            public string Description;
            public System.Func<ICardEffect> EffectFactory;
        }

        //# 카드 리뉴얼 v0.6 — 패시브 16장 (4축 × 4장 균등). 기획서 §3 표 매핑 그대로.
        public static readonly Spec[] PassiveSpecs = new[]
        {
            //# Tank P4
            new Spec { Id = ECardId.WispHpBoost, Category = EBuildAxis.Tank,
                       DisplayName = "끈질긴 위스프", Description = "모든 위스프 HP +50%",
                       EffectFactory = () => new WispHpBoostEffect() },
            new Spec { Id = ECardId.WraithDamageBoost, Category = EBuildAxis.Tank,
                       DisplayName = "망령의 압박", Description = "모든 레이스 HP +50%",
                       EffectFactory = () => new WraithDamageBoostEffect() },
            new Spec { Id = ECardId.SpawnWraith, Category = EBuildAxis.Tank,
                       DisplayName = "더 많은 망령", Description = "레이스 Spawner 출력 +1",
                       EffectFactory = () => new SpawnWraithEffect() },
            new Spec { Id = ECardId.ReplaceWispsToWraith, Category = EBuildAxis.Tank,
                       DisplayName = "망령으로 진화", Description = "위스프 Spawner → 레이스 생산",
                       EffectFactory = () => new ReplaceWispsToWraithEffect() },
            //# Dps P4
            new Spec { Id = ECardId.ReaperAtkSpeed, Category = EBuildAxis.Dps,
                       DisplayName = "신속한 사신", Description = "모든 리퍼 공격속도 +30%",
                       EffectFactory = () => new ReaperAtkSpeedEffect() },
            new Spec { Id = ECardId.HexRangeBoost, Category = EBuildAxis.Dps,
                       DisplayName = "저주의 시야", Description = "모든 헥스 사거리 +40%",
                       EffectFactory = () => new HexRangeBoostEffect() },
            new Spec { Id = ECardId.SpawnReapers, Category = EBuildAxis.Dps,
                       DisplayName = "사신 떼거리", Description = "리퍼 Spawner 출력 +1",
                       EffectFactory = () => new SpawnReapersEffect() },
            new Spec { Id = ECardId.ReplaceReapersToHex, Category = EBuildAxis.Dps,
                       DisplayName = "헥스로 진화", Description = "리퍼 Spawner → 헥스 생산",
                       EffectFactory = () => new ReplaceReapersToHexEffect() },
            //# Debuff P4
            new Spec { Id = ECardId.PlagueSlowBoost, Category = EBuildAxis.Debuff,
                       DisplayName = "역병의 손길", Description = "플레이그 둔화 효과 강화",
                       EffectFactory = () => new PlagueSlowBoostEffect() },
            new Spec { Id = ECardId.SpawnPlagues, Category = EBuildAxis.Debuff,
                       DisplayName = "역병 증식", Description = "플레이그 Spawner 출력 +1",
                       EffectFactory = () => new SpawnPlaguesEffect() },
            new Spec { Id = ECardId.HeroPoisonAura, Category = EBuildAxis.Debuff,
                       DisplayName = "독장판", Description = "영웅 발 밑에 독 장판 (DPS 5)",
                       EffectFactory = () => new HeroPoisonAuraEffect() },
            new Spec { Id = ECardId.HeroAttackDown, Category = EBuildAxis.Debuff,
                       DisplayName = "약화의 저주", Description = "영웅 공격력 영구 -25%",
                       EffectFactory = () => new HeroAttackDownEffect() },
            //# Swarm P4
            new Spec { Id = ECardId.PhantomMoveSpeedBoost, Category = EBuildAxis.Swarm,
                       DisplayName = "환령의 발걸음", Description = "모든 팬텀 이동속도 +50%",
                       EffectFactory = () => new PhantomMoveSpeedBoostEffect() },
            new Spec { Id = ECardId.SpawnPhantoms, Category = EBuildAxis.Swarm,
                       DisplayName = "환령 떼", Description = "팬텀 Spawner 출력 +1",
                       EffectFactory = () => new SpawnPhantomsEffect() },
            new Spec { Id = ECardId.SpawnWisps, Category = EBuildAxis.Swarm,
                       DisplayName = "위스프 떼", Description = "위스프 Spawner 출력 +1",
                       EffectFactory = () => new SpawnWispsEffect() },
            new Spec { Id = ECardId.SpawnerHaste, Category = EBuildAxis.Swarm,
                       DisplayName = "던전의 박동", Description = "모든 스포너 주기 -20% (영구)",
                       EffectFactory = () => new SpawnerHasteEffect() },
        };

        //# 카드 리뉴얼 v0.6 — 액티브 12장 (4축 × 3장 균등). 기획서 §3 표 매핑 그대로.
        public static readonly Spec[] ActiveSpecs = new[]
        {
            //# Tank A3
            new Spec { Id = ECardId.IronWill, Category = EBuildAxis.Tank,
                       DisplayName = "강철 의지", Description = "모든 몬스터 받는 데미지 -30% (15초)",
                       EffectFactory = () => new IronWillEffect() },
            new Spec { Id = ECardId.WallOfWisps, Category = EBuildAxis.Tank,
                       DisplayName = "위스프 장벽", Description = "영웅 주변 4방위에 위스프 4마리 즉시 소환",
                       EffectFactory = () => new WallOfWispsEffect() },
            new Spec { Id = ECardId.Berserk, Category = EBuildAxis.Tank,
                       DisplayName = "수호자의 분노", Description = "위스프·레이스 받는 데미지 -50% (15초)",
                       EffectFactory = () => new GuardianRageEffect() },
            //# Dps A3
            new Spec { Id = ECardId.Frenzy, Category = EBuildAxis.Dps,
                       DisplayName = "광폭화", Description = "모든 몬스터 공속 +50% (10초)",
                       EffectFactory = () => new FrenzyEffect() },
            new Spec { Id = ECardId.BloodThirst, Category = EBuildAxis.Dps,
                       DisplayName = "피의 갈증", Description = "처치 시 주변 몬스터 회복 (30초)",
                       EffectFactory = () => new BloodThirstEffect() },
            new Spec { Id = ECardId.MarkOfDeath, Category = EBuildAxis.Dps,
                       DisplayName = "죽음의 표식", Description = "다음 5초간 영웅이 받는 데미지 +50%",
                       EffectFactory = () => new MarkOfDeathEffect() },
            //# Debuff A3
            new Spec { Id = ECardId.Fear, Category = EBuildAxis.Debuff,
                       DisplayName = "공포", Description = "영웅 3초간 도망",
                       EffectFactory = () => new FearEffect() },
            new Spec { Id = ECardId.Bleed, Category = EBuildAxis.Debuff,
                       DisplayName = "출혈", Description = "영웅 이동 시 HP 감소 (10초)",
                       EffectFactory = () => new BleedEffect() },
            new Spec { Id = ECardId.Weaken, Category = EBuildAxis.Debuff,
                       DisplayName = "무력화", Description = "영웅 데미지 -50% (10초)",
                       EffectFactory = () => new WeakenEffect() },
            //# Swarm A3
            new Spec { Id = ECardId.TimeStop, Category = EBuildAxis.Swarm,
                       DisplayName = "시간 정지", Description = "영웅 5초 멈춤",
                       EffectFactory = () => new TimeStopEffect() },
            //# 카드 리뉴얼 v0.6 BLOCKER W1 — SwarmRush 효과는 ECardId.Multiply 자리 (값 20) 의 SO 파일 Multiply.asset 으로 재사용.
            //# Berserk → GuardianRage 패턴과 동일: enum 값명·SO 파일명 보존, 효과/displayName 만 리뉴얼.
            new Spec { Id = ECardId.Multiply, Category = EBuildAxis.Swarm,
                       DisplayName = "스웜 러시", Description = "팬텀 6마리 영웅 근처 즉시 소환",
                       EffectFactory = () => new SwarmRushEffect() },
            new Spec { Id = ECardId.Slow, Category = EBuildAxis.Swarm,
                       DisplayName = "던전의 점성", Description = "영웅 -50% 이동속도, 모든 몬스터 +30% 이동속도 (10초)",
                       EffectFactory = () => new SlowEffect() },
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
            Debug.Log("[LairCardPrefabBuilder] 카드 리뉴얼 v0.6 — 카드 28장 + 풀 2개 재빌드 완료 (비파괴)");
        }

        //# spec 목록에 없는 .asset 만 삭제 (폐기 ECardId). 유효 카드는 보존.
        private static void RemoveStaleCards()
        {
            HashSet<string> valid = new HashSet<string>();
            foreach (Spec s in PassiveSpecs) valid.Add(s.Id.ToString());
            foreach (Spec s in ActiveSpecs)  valid.Add(s.Id.ToString());

            foreach (string path in Directory.GetFiles(CardDir, "*.asset"))
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
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairCardPrefabBuilder] Addressables 미설정 — Window > Asset Management > Addressables Groups 로 초기화 필요");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup(ResourceGroup);

            List<CardData> createdCards = new List<CardData>();

            foreach (Spec spec in specs)
            {
                string path = $"{CardDir}/{spec.Id}.asset";
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                bool isNew = card == null;
                if (isNew) card = ScriptableObject.CreateInstance<CardData>();

                SerializedObject so = new SerializedObject(card);
                so.FindProperty("_id").enumValueIndex       = (int)spec.Id;
                //# 카드 리뉴얼 v0.6 — _category → _axis 필드명 갱신.
                so.FindProperty("_axis").enumValueIndex = (int)spec.Category;
                so.FindProperty("_displayName").stringValue = spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description;
                //# 빌드 패널 아이콘 — ECardId 이름 PNG 자동 배정 (없으면 null). _effect 와 달리 매번 재설정.
                so.FindProperty("_icon").objectReferenceValue = LoadCardIcon(spec.Id);

                //# 비파괴 — 기존 카드의 _effect(튜닝값) 보존. 신규/타입불일치 시에만 새 효과.
                SerializedProperty effectProp = so.FindProperty("_effect");
                ICardEffect wanted = spec.EffectFactory();
                object existing = effectProp.managedReferenceValue;
                if (existing == null || existing.GetType() != wanted.GetType())
                    effectProp.managedReferenceValue = wanted;

                so.ApplyModifiedPropertiesWithoutUndo();

                if (isNew) AssetDatabase.CreateAsset(card, path);
                else       EditorUtility.SetDirty(card);
                RegisterAddressable(settings, group, path, spec.Id.ToString());

                createdCards.Add(card);
                Debug.Log($"[LairCardPrefabBuilder] CardData {(isNew ? "생성" : "갱신")}: {spec.Id}");
            }

            CardPool pool = ScriptableObject.CreateInstance<CardPool>();
            SerializedObject poolSo = new SerializedObject(pool);
            SerializedProperty listProp = poolSo.FindProperty("_cards");
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
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);
        }

        //# ECardId 이름의 PNG 를 Sprite 로 로드. 미존재 시 null.
        //# PNG 의 textureType=Sprite / spriteImportMode=Single 임포트 설정을 보정.
        private static Sprite LoadCardIcon(ECardId id)
        {
            string path = $"{IconDir}/{id}.png";
            if (File.Exists(path) == false) return null;

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
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
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }
    }
}
