using System.Collections.Generic;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# PlayMode 러너 테스트용 스킬 조립 — 스킬 클래스는 JSON 조립 결과(HeroSkillParams)로만 만들어진다(so-to-json-migration §7.3).
    //# 전환 전 코드 기본값을 명시 수치로 넘겨 기존 기대값을 유지한다. 필요한 수치는 만든 뒤 리플렉션으로 덮어쓴다.
    public static class HeroSkillPlayKit
    {
        private static HeroSkillParams Params(params (string key, float value)[] pairs)
        {
            Dictionary<string, float> values = new Dictionary<string, float>();
            foreach ((string key, float value) pair in pairs)
            {
                values[pair.key] = pair.value;
            }
            return new HeroSkillParams(values);
        }

        public static DashStrikeSkillData MakeDash()
        {
            return new DashStrikeSkillData(string.Empty, Params(("damage", 80.0f), ("cooldown", 3.0f), ("dashLength", 7.0f), ("coneHalfAngle", 35.0f), ("knockbackStrength", 2.0f), ("centroidRadius", 8.0f)));
        }

        public static OrbitingBladeSkillData MakeOrbit()
        {
            return new OrbitingBladeSkillData(string.Empty, Params(("damage", 15.0f), ("hitInterval", 0.3f), ("orbitRadius", 1.4f), ("bladeSphereRadius", 0.9f), ("bladeCount", 3.0f), ("rotationSpeedDeg", 180.0f)));
        }
    }
}
