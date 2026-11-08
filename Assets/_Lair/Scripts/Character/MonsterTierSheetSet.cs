using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 강화 전신 시트 1벌(종족 x 티어) — 스프라이트 30개(인덱스 = 행*6+열) + 티어 HP바 높이(monster-tier-sheets §8).
    //# 스프라이트 참조가 필요해 JSON 불가(Rule 02 §11 SO 예외). 프리팹은 이 SO 를 직접 참조하지 않고 전투 시작에 Addressables 로 불러온다.
    [CreateAssetMenu(fileName = "MonsterTierSheetSet", menuName = "Lair/Monster Tier Sheet Set")]
    public class MonsterTierSheetSet : ScriptableObject
    {
        public const int FrameCount = 30;

        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _hpBarHeightWorld;

        public int Count => _frames != null ? _frames.Length : 0;
        public float HpBarHeightWorld => _hpBarHeightWorld;

        //# 범위 밖·미할당이면 null(호출부는 몸 스프라이트를 그대로 둔다).
        public Sprite GetFrame(int index)
        {
            if (_frames == null || index < 0 || index >= _frames.Length)
                return null;
            return _frames[index];
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetForTest(Sprite[] frames, float hpBarHeightWorld)
        {
            _frames = frames;
            _hpBarHeightWorld = hpBarHeightWorld;
        }
#endif
    }
}
