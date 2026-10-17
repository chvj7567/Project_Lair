using UnityEngine;

namespace Lair.Data
{
    //# 시너지 축 아이콘 공유 SO — Sprite 참조를 담으므로 SO(Rule 02 §11 예외). EData.SynergyVisualConfig 로 로드.
    [CreateAssetMenu(fileName = "SynergyVisualConfig", menuName = "Lair/Synergy Visual Config")]
    public class SynergyVisualConfig : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public EBuildAxis Axis;
            public Sprite Icon;
        }

        //# 축마다 정확히 1개(Tank/Dps/Debuff/Swarm).
        [SerializeField] private Entry[] _entries = new Entry[0];
        public Entry[] Entries => _entries;

        //# 축 → 아이콘. 항목이 없거나 Sprite 미할당이면 null.
        public Sprite GetIcon(EBuildAxis axis)
        {
            if (_entries == null)
                return null;
            for (int i = 0; i < _entries.Length; ++i)
            {
                if (_entries[i] != null && _entries[i].Axis == axis)
                    return _entries[i].Icon;
            }
            return null;
        }
    }
}
