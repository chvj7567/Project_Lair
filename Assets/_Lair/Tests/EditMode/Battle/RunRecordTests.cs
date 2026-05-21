using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# RunRecord 의 JsonUtility 직렬화/역직렬화 왕복 검증.
    public class RunRecordTests
    {
        [Test]
        public void Json_왕복_시_모든_필드_보존()
        {
            var original = new RunRecord
            {
                FinishedAt = "2026-05-21T10:00:00",
                Result = "Win",
                DeathTime = 184.5f,
                Picks = new List<string> { "SlimeHpBoost", "Frenzy" },
                SurvivingMonsters = 7,
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<RunRecord>(json);

            Assert.AreEqual("2026-05-21T10:00:00", restored.FinishedAt);
            Assert.AreEqual("Win", restored.Result);
            Assert.AreEqual(184.5f, restored.DeathTime, 0.001f);
            Assert.AreEqual(2, restored.Picks.Count);
            Assert.AreEqual("Frenzy", restored.Picks[1]);
            Assert.AreEqual(7, restored.SurvivingMonsters);
        }
    }
}
