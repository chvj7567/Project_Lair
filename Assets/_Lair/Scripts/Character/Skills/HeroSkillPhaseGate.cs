using System.Collections.Generic;

namespace Lair.Character
{
    //# 영웅 HP 비율을 폴링해 임계(HpFraction) 를 *처음* 하향 돌파한 페이즈 인덱스를 반환하는 순수 게이트.
    //# 한 번 활성된 인덱스는 다시 반환하지 않는다. 급락으로 여러 임계를 한 번에 넘으면 모두 반환.
    public class HeroSkillPhaseGate
    {
        private readonly float[] _fractions;
        private readonly bool[] _activated;

        public HeroSkillPhaseGate(IReadOnlyList<float> hpFractions)
        {
            _fractions = new float[hpFractions.Count];
            for (int i = 0; i < hpFractions.Count; ++i)
                _fractions[i] = hpFractions[i];
            _activated = new bool[_fractions.Length];
        }

        //# newlyActivated 를 clear 후, hpRatio <= fraction 인데 아직 미활성인 인덱스를 채운다.
        public void Poll(float hpRatio, List<int> newlyActivated)
        {
            newlyActivated.Clear();
            for (int i = 0; i < _fractions.Length; ++i)
            {
                if (_activated[i])
                    continue;
                if (hpRatio <= _fractions[i])
                {
                    _activated[i] = true;
                    newlyActivated.Add(i);
                }
            }
        }

        //# 풀 재사용/라운드 재시작 대비.
        public void Reset()
        {
            for (int i = 0; i < _activated.Length; ++i)
                _activated[i] = false;
        }
    }
}
