using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 스킬 페이즈 정의 — {HP비율, 스킬} 순서 리스트. CHMResource 로 로드(EData.HeroSkillLoadout).
    [CreateAssetMenu(fileName = "HeroSkillLoadout", menuName = "Lair/Hero Skill Loadout")]
    public class HeroSkillLoadout : ScriptableObject
    {
        [System.Serializable]
        public class Phase
        {
            [Range(0f, 1f)] public float HpFraction = 1f;
            public HeroSkillData Skill;
        }

        [SerializeField] private List<Phase> _phases = new();
        public IReadOnlyList<Phase> Phases => _phases;
    }
}
