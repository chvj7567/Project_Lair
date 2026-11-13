using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 영웅 스킬 테스트 조립 — 스킬 클래스는 JSON 조립 결과(HeroSkillParams)로만 만들어진다(so-to-json-migration §7.3).
    //# 이 키트는 전환 전 코드 기본값을 명시 수치로 그대로 넘겨 기존 테스트의 기대값을 유지한다(출하 값이 아니다 — 출하 값은 HeroSkillShippedDataTests 가 검증).
    public static class HeroSkillTestKit
    {
        private static readonly Dictionary<EHeroSkill, (string key, float value)[]> Defaults = new Dictionary<EHeroSkill, (string key, float value)[]>
        {
            { EHeroSkill.DashStrike, new[] { ("damage", 80.0f), ("cooldown", 3.0f), ("dashLength", 7.0f), ("coneHalfAngle", 35.0f), ("knockbackStrength", 2.0f), ("centroidRadius", 8.0f) } },
            { EHeroSkill.AoeNova, new[] { ("damage", 100.0f), ("cooldown", 7.0f), ("radius", 3.5f), ("knockbackStrength", 3.0f) } },
            { EHeroSkill.OrbitingBlade, new[] { ("damage", 15.0f), ("hitInterval", 0.3f), ("orbitRadius", 1.4f), ("bladeSphereRadius", 0.9f), ("bladeCount", 3.0f), ("rotationSpeedDeg", 180.0f) } },
            { EHeroSkill.PiercingShot, new[] { ("damage", 70.0f), ("cooldown", 4.0f), ("lineLength", 9.0f), ("lineHalfWidth", 0.6f), ("knockbackStrength", 0.5f), ("centroidRadius", 8.0f) } },
            { EHeroSkill.ArrowVolley, new[] { ("damagePerTick", 40.0f), ("tickCount", 3.0f), ("tickIntervalSec", 0.5f), ("radius", 2.8f), ("cooldown", 8.0f), ("targetSearchRadius", 7.0f) } },
            { EHeroSkill.Evade, new[] { ("damage", 30.0f), ("cooldown", 10.0f), ("leapDistance", 4.5f), ("leapDurationSec", 0.25f), ("blastRadius", 2.0f), ("knockbackStrength", 2.5f), ("triggerMonsterCount", 3.0f), ("triggerRadius", 2.0f) } },
            { EHeroSkill.ShadowStrike, new[] { ("damage", 120.0f), ("cooldown", 6.0f), ("blinkRange", 9.0f), ("minBlinkDistance", 2.5f), ("arriveOffset", 1.2f), ("invulnerableSec", 0.3f), ("killRefundFraction", 0.5f) } },
            { EHeroSkill.BladeWhirl, new[] { ("damagePerTick", 35.0f), ("tickCount", 4.0f), ("tickIntervalSec", 0.3f), ("radius", 2.4f), ("cooldown", 9.0f), ("damageTakenMul", 0.7f), ("triggerMonsterCount", 2.0f), ("triggerRadius", 2.4f) } },
            { EHeroSkill.SmokeBomb, new[] { ("radius", 3.0f), ("durationSec", 3.0f), ("monsterSpeedMul", 0.6f), ("monsterRangeMul", 0.5f), ("monsterMinRange", 0.8f), ("cooldown", 12.0f), ("triggerMonsterCount", 4.0f), ("triggerRadius", 2.4f) } },
            { EHeroSkill.CrushingLeap, new[] { ("damage", 90.0f), ("cooldown", 7.0f), ("leapRange", 8.0f), ("minLeapDistance", 3.0f), ("leapDurationSec", 0.3f), ("invulnerableSec", 0.3f), ("slamRadius", 2.6f), ("knockbackStrength", 1.5f), ("clusterRadius", 2.2f) } },
            { EHeroSkill.ChainPull, new[] { ("damage", 40.0f), ("cooldown", 10.0f), ("pullRadius", 5.0f), ("maxTargets", 6.0f), ("pullToDistance", 1.5f), ("pullDurationSec", 0.4f), ("triggerMonsterCount", 4.0f), ("triggerRadius", 5.0f) } },
            { EHeroSkill.BloodFrenzy, new[] { ("durationSec", 6.0f), ("attackCooldownMul", 0.6f), ("damageTakenMul", 1.2f), ("cooldown", 18.0f), ("triggerMonsterCount", 3.0f), ("triggerRadius", 2.2f) } },
            { EHeroSkill.JudgmentHammer, new[] { ("damage", 110.0f), ("cooldown", 6.0f), ("radius", 2.4f), ("stunSec", 0.6f), ("castRange", 5.0f) } },
            { EHeroSkill.PurifyingLight, new[] { ("triggerDebuffCount", 2.0f), ("healPercent", 0.08f), ("cooldown", 12.0f) } },
            { EHeroSkill.Sanctuary, new[] { ("durationSec", 6.0f), ("damageTakenMul", 0.6f), ("radius", 2.6f), ("monsterSpeedMul", 0.7f), ("cooldown", 18.0f), ("triggerMonsterCount", 3.0f), ("triggerRadius", 2.6f) } },
        };

        //# 클래스 이름(XxxSkillData)에서 스킬을 정해 기본 수치로 만든다. 수치를 바꿔야 하는 테스트는 만든 뒤 TestReflection.SetField 로 필드를 덮어쓴다.
        public static T Make<T>() where T : HeroSkillData
        {
            EHeroSkill id = (EHeroSkill)Enum.Parse(typeof(EHeroSkill), typeof(T).Name.Replace("SkillData", string.Empty));
            return (T)HeroSkillData.Create(DefaultEntry(id));
        }

        public static HeroSkillEntry DefaultEntry(EHeroSkill id)
        {
            Dictionary<string, float> values = new Dictionary<string, float>();
            foreach ((string key, float value) pair in Defaults[id])
            {
                values[pair.key] = pair.value;
            }
            return new HeroSkillEntry { Id = id, DisplayName = string.Empty, Params = new HeroSkillParams(values) };
        }
    }
}
