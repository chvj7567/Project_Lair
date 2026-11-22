using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 한 마리의 "외형 한 벌" — 둥지(슬롯) 하나의 편성 몬스터·기준 몬스터·등급·강화 Lv·티어(village-monster-showcase §2).
    public struct MonsterLook
    {
        public int Slot;
        public EMonster Monster;
        public EMonster Base;
        public int Grade;
        public int Level;
        public int Tier;
    }

    //# 전투(BattleController)와 마을·로딩 쇼케이스가 함께 쓰는 순수 계산 — 유효 편성·종족별 강화 Lv·필요한 티어 시트 목록.
    //# 같은 입력이면 같은 결과를 내므로 둘 사이 외형이 어긋나지 않는다(게이트 T-S2). Unity 오브젝트 비의존.
    public static class MonsterLooks
    {
        //# 유효 편성(둥지별 몬스터). 편성 데이터가 없으면 null — 전투는 "씬 기본 배치", 외형 계산은 기준 6종으로 간주.
        public static EMonster?[] ResolveFormation(MetaProfile profile, bool useDefault = false)
        {
            return FormationSave.Resolve(FormationDatabase.Current, profile, useDefault, Debug.LogWarning);
        }

        //# 종족(기준 몬스터) → 상점 강화 Lv. Id 문자열 파싱 없이 config 항목 순회. config 가 없으면 빈 표(전부 Lv0).
        public static Dictionary<EMonster, int> SpeciesLevels(MetaProfile profile, MetaConfig config)
        {
            Dictionary<EMonster, int> levels = new Dictionary<EMonster, int>();
            if (config == null || profile == null)
                return levels;
            foreach (ShopItemDef item in config.ShopItems)
            {
                if (item == null || item.EffectKind != EShopEffectKind.MonsterSpecies)
                    continue;
                levels[item.Species] = Mathf.Clamp(profile.GetShopLevel(item.Id), 0, item.MaxLevel);
            }
            return levels;
        }

        //# 유효 편성 → 슬롯별 MonsterLook. formation 이 null 이면 기준 6종(편성 미사용과 같은 취급).
        public static MonsterLook[] Compute(IReadOnlyList<EMonster> formation, IReadOnlyDictionary<EMonster, int> speciesLevels)
        {
            IReadOnlyList<EMonster> placed = formation != null ? formation : MonsterGrades.BaseMonsters;
            MonsterLook[] looks = new MonsterLook[placed.Count];
            for (int i = 0; i < placed.Count; i++)
            {
                looks[i] = CreateLook(i, placed[i], speciesLevels);
            }
            return looks;
        }

        public static MonsterLook[] ComputeSlots(IReadOnlyList<EMonster?> formation, IReadOnlyDictionary<EMonster, int> speciesLevels)
        {
            if (formation == null)
                return Compute(null, speciesLevels);
            List<MonsterLook> looks = new List<MonsterLook>();
            for (int i = 0; i < formation.Count; ++i)
            {
                if (formation[i].HasValue)
                    looks.Add(CreateLook(i, formation[i].Value, speciesLevels));
            }
            return looks.ToArray();
        }

        private static MonsterLook CreateLook(int slot, EMonster monster, IReadOnlyDictionary<EMonster, int> speciesLevels)
        {
            EMonster root = MonsterGrades.BaseOf(monster);
            int level = speciesLevels != null && speciesLevels.TryGetValue(root, out int lv) ? lv : 0;
            return new MonsterLook
            {
                Slot = slot,
                Monster = monster,
                Base = root,
                Grade = MonsterGrades.GradeOf(monster),
                Level = level,
                Tier = MonsterEnhancementVisual.TierOfLevel(level),
            };
        }

        //# 로딩 화면 고정 외형 — 로딩 시작부터 끝까지 기준 6종 전부 군주(5등급) + 강화 Lv 최대(상점 MaxLevel 이내).
        public static MonsterLook[] LoadingShowcase(MetaConfig config)
        {
            Dictionary<EMonster, int> levels = new Dictionary<EMonster, int>();
            foreach (EMonster species in MonsterGrades.BaseMonsters)
            {
                levels[species] = MonsterEnhancementVisual.MaxLevel;
            }
            if (config != null)
            {
                foreach (ShopItemDef item in config.ShopItems)
                {
                    if (item != null && item.EffectKind == EShopEffectKind.MonsterSpecies)
                    {
                        levels[item.Species] = Mathf.Min(MonsterEnhancementVisual.MaxLevel, item.MaxLevel);
                    }
                }
            }
            EMonster[] formation = new EMonster[MonsterGrades.BaseMonsters.Count];
            for (int i = 0; i < formation.Length; i++)
            {
                formation[i] = MonsterGrades.Of(MonsterGrades.BaseMonsters[i], MonsterGrades.MaxGrade);
            }
            return Compute(formation, levels);
        }

        //# 로드해야 할 (기준 몬스터, 티어) — 티어 > 0 인 종족만, 같은 종족은 한 번.
        public static List<(EMonster, int)> TierNeeds(IReadOnlyList<MonsterLook> looks)
        {
            List<(EMonster, int)> needs = new List<(EMonster, int)>();
            HashSet<EMonster> seen = new HashSet<EMonster>();
            foreach (MonsterLook look in looks)
            {
                if (look.Tier > 0 && seen.Add(look.Base))
                {
                    needs.Add((look.Base, look.Tier));
                }
            }
            return needs;
        }
    }

    //# 전투와 쇼케이스가 공유하는 외형 적용 — 등급 연출(Configure) → 강화 시각(티어 치환·발광·HP바). 순서가 중요(sizeMul 이 HP바 높이에 합성됨).
    public static class MonsterLookApplier
    {
        //# configureGrade: 스폰/배치 때만 true(재적용에서는 이펙트 위상이 튀지 않게 false). 등급 Variant 가 아니면 등급 연출은 건너뛴다.
        public static void Apply(GameObject character, EMonster key, EMonster species, int level, bool configureGrade, IMonsterTierSheetProvider tiers)
        {
            IMonsterGradeVisual gradeVisual = null;
            int grade = MonsterGrades.GradeOf(key);
            if (grade > MonsterGrades.MinGrade && GradeFxDatabase.Current != null)
            {
                LairCharacter lair = character.GetComponent<LairCharacter>();
                gradeVisual = lair != null ? lair.Get<IMonsterGradeVisual>() : null;
                if (configureGrade && gradeVisual != null)
                {
                    gradeVisual.Configure(GradeFxDatabase.Current.SpecOf(species, grade));
                }
            }

            MonsterEnhancementVisual visual = character.GetComponent<MonsterEnhancementVisual>();
            if (visual != null)
            {
                visual.SetTierSheetProvider(tiers);
                visual.SetGradeVisual(gradeVisual);
                visual.ApplyLevel(level, species);
            }
        }
    }
}
