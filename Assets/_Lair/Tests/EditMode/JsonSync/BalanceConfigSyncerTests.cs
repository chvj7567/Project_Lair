using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Data;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class BalanceConfigSyncerTests
    {
        private BalanceConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BalanceConfig>();
            SerializedObject so = new SerializedObject(_config);

            SerializedProperty heroProp = so.FindProperty("_hero");
            heroProp.FindPropertyRelative("Hp").intValue          = 500;
            heroProp.FindPropertyRelative("Power").intValue       = 10;
            heroProp.FindPropertyRelative("Range").floatValue     = 3f;
            heroProp.FindPropertyRelative("Cooldown").floatValue  = 1f;
            heroProp.FindPropertyRelative("MoveSpeed").floatValue = 3f;

            so.FindProperty("_runDuration").floatValue = 300f;

            SerializedProperty passProp = so.FindProperty("_passiveThresholds");
            passProp.arraySize = 2;
            passProp.GetArrayElementAtIndex(0).floatValue = 0.9f;
            passProp.GetArrayElementAtIndex(1).floatValue = 0.8f;

            SerializedProperty activeProp = so.FindProperty("_activeThresholds");
            activeProp.arraySize = 2;
            activeProp.GetArrayElementAtIndex(0).floatValue = 30f;
            activeProp.GetArrayElementAtIndex(1).floatValue = 60f;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        //# Export → hero.hp 필드 포함
        [Test]
        public void ExportToJson_HeroHp포함()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);

            Assert.AreEqual(500, obj["hero"]?["hp"]?.Value<int>());
        }

        //# Export → hero 전체 스탯 (power/range/cooldown/moveSpeed) 포함
        [Test]
        public void ExportToJson_Hero전체스탯포함()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);
            JObject hero = obj["hero"] as JObject;

            Assert.IsNotNull(hero, "hero 키 없음");
            Assert.AreEqual(10,  hero["power"].Value<int>(),       "power");
            Assert.AreEqual(3f,  hero["range"].Value<float>(),     0.001f, "range");
            Assert.AreEqual(1f,  hero["cooldown"].Value<float>(),  0.001f, "cooldown");
            Assert.AreEqual(3f,  hero["moveSpeed"].Value<float>(), 0.001f, "moveSpeed");
        }

        //# Export → runDuration 포함
        [Test]
        public void ExportToJson_RunDuration포함()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);

            Assert.AreEqual(300f, obj["runDuration"]?.Value<float>(), 0.001f);
        }

        //# Export → passiveThresholds 배열 길이 및 값 포함
        [Test]
        public void ExportToJson_PassiveThresholds포함()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);
            JArray thresholds = obj["passiveThresholds"] as JArray;

            Assert.IsNotNull(thresholds, "passiveThresholds 키 없음");
            Assert.AreEqual(2, thresholds.Count);
            Assert.AreEqual(0.9f, thresholds[0].Value<float>(), 0.001f, "[0]");
            Assert.AreEqual(0.8f, thresholds[1].Value<float>(), 0.001f, "[1]");
        }

        //# Export → activeThresholds 배열 길이 및 값 포함
        [Test]
        public void ExportToJson_ActiveThresholds포함()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);
            JArray thresholds = obj["activeThresholds"] as JArray;

            Assert.IsNotNull(thresholds, "activeThresholds 키 없음");
            Assert.AreEqual(2, thresholds.Count);
            Assert.AreEqual(30f, thresholds[0].Value<float>(), 0.001f, "[0]");
            Assert.AreEqual(60f, thresholds[1].Value<float>(), 0.001f, "[1]");
        }

        //# Export → monsters 배열이 JSON 에 존재
        [Test]
        public void ExportToJson_Monsters배열존재()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);
            JToken monsters = obj["monsters"];

            Assert.IsNotNull(monsters, "monsters 키 없음");
            Assert.IsInstanceOf<JArray>(monsters);
        }

        //# ApplyDto → hero.hp 갱신
        [Test]
        public void ApplyDto_HeroHp갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 999, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>(),
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            Assert.AreEqual(999, _config.Hero.Hp);
        }

        //# ApplyDto → hero 전체 스탯 갱신 (Power/Range/Cooldown/MoveSpeed)
        [Test]
        public void ApplyDto_Hero전체스탯갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 20, Range = 5f, Cooldown = 0.5f, MoveSpeed = 4f },
                Monsters = new List<MonsterStatRowDto>(),
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            Assert.AreEqual(20,   _config.Hero.Power,           "Power");
            Assert.AreEqual(5f,   _config.Hero.Range,     0.001f, "Range");
            Assert.AreEqual(0.5f, _config.Hero.Cooldown,  0.001f, "Cooldown");
            Assert.AreEqual(4f,   _config.Hero.MoveSpeed, 0.001f, "MoveSpeed");
        }

        //# ApplyDto → runDuration 갱신
        [Test]
        public void ApplyDto_RunDuration갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>(),
                RunDuration = 600f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            Assert.AreEqual(600f, _config.RunDuration, 0.001f);
        }

        //# ApplyDto → passiveThresholds 배열 갱신 (크기 변경 포함)
        [Test]
        public void ApplyDto_PassiveThresholds배열갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>(),
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.7f, 0.5f, 0.3f, 0.1f }, //# 2 → 4 로 크기 변경
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            Assert.AreEqual(4,    _config.PassiveThresholds.Length, "배열 길이");
            Assert.AreEqual(0.7f, _config.PassiveThresholds[0], 0.001f, "[0]");
            Assert.AreEqual(0.5f, _config.PassiveThresholds[1], 0.001f, "[1]");
            Assert.AreEqual(0.3f, _config.PassiveThresholds[2], 0.001f, "[2]");
            Assert.AreEqual(0.1f, _config.PassiveThresholds[3], 0.001f, "[3]");
        }

        //# ApplyDto → activeThresholds 배열 갱신
        [Test]
        public void ApplyDto_ActiveThresholds배열갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>(),
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 60f, 120f, 180f } //# 2 → 3 으로 크기 변경
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            Assert.AreEqual(3,     _config.ActiveThresholds.Length, "배열 길이");
            Assert.AreEqual(60f,   _config.ActiveThresholds[0], 0.001f, "[0]");
            Assert.AreEqual(120f,  _config.ActiveThresholds[1], 0.001f, "[1]");
            Assert.AreEqual(180f,  _config.ActiveThresholds[2], 0.001f, "[2]");
        }

        //# ApplyDto → monsters 유효 행 갱신
        [Test]
        public void ApplyDto_Monsters유효행갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto
                    {
                        Key  = "Wisp",
                        Stat = new CharacterStatDto { Hp = 80, Power = 7, Range = 2f, Cooldown = 1f, MoveSpeed = 2f }
                    }
                },
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            BalanceConfig.CharacterStat wisp = _config.GetMonster(EMonster.Wisp);
            Assert.IsNotNull(wisp, "Wisp 스탯이 null");
            Assert.AreEqual(80, wisp.Hp,      "Wisp.Hp");
            Assert.AreEqual(7,  wisp.Power,   "Wisp.Power");
        }

        //# ApplyDto → 잘못된 EMonster 키는 skip, 유효 행만 반영 + LogWarning 출력 (회귀 고정)
        [Test]
        public void ApplyDto_잘못된MonsterKey_skip_LogWarning()
        {
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("EMonster 파싱 실패"));

            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>
                {
                    new MonsterStatRowDto
                    {
                        Key  = "InvalidMonster",
                        Stat = new CharacterStatDto { Hp = 999 }
                    },
                    new MonsterStatRowDto
                    {
                        Key  = "Wraith",
                        Stat = new CharacterStatDto { Hp = 200, Power = 15, Range = 2f, Cooldown = 2f, MoveSpeed = 1.5f }
                    }
                },
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            //# 유효 행(Wraith) 1개만 반영되어야 함 — InvalidMonster 는 배열에서 제외
            BalanceConfig.CharacterStat wraith = _config.GetMonster(EMonster.Wraith);
            Assert.IsNotNull(wraith, "Wraith 스탯이 null");
            Assert.AreEqual(200, wraith.Hp, "Wraith.Hp");
        }

        //# ApplyDto → passiveThresholds null 이어도 예외 없이 처리 (경계값)
        [Test]
        public void ApplyDto_PassiveThresholdsNull_예외없음()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>(),
                RunDuration = 300f,
                PassiveThresholds = null,
                ActiveThresholds  = new float[] { 30f }
            };

            Assert.DoesNotThrow(() => BalanceConfigSyncer.ApplyDto(dto, _config));
        }

        //# ApplyDto → activeThresholds null 이어도 예외 없이 처리 (경계값)
        [Test]
        public void ApplyDto_ActiveThresholdsNull_예외없음()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new List<MonsterStatRowDto>(),
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = null
            };

            Assert.DoesNotThrow(() => BalanceConfigSyncer.ApplyDto(dto, _config));
        }
    }
}
