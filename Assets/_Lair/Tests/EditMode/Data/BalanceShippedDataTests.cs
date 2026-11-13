using System.IO;
using System.Text.RegularExpressions;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# 출하 데이터 회귀 (so-to-json-migration §10) — 밸런스 JSON 2종이 로드 검증(SJ3~SJ5)을 통과하고 에셋 키 규약(SJ6)을 지킨다.
    public class BalanceShippedDataTests
    {
        private const string GroupDir = "Assets/AddressableAssetsData/AssetGroups";
        private static readonly EData[] Keys = { EData.BalanceConfig, EData.MonsterStat };

        [Test]
        public void 출하_밸런스_JSON은_검증을_통과하고_기준_6종을_모두_가진다()
        {
            BalanceConfig balance = WorldMapTestSupport.ShippedBalance();

            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                Assert.IsNotNull(balance.GetMonster(monster), monster.ToString());
            }
        }

        [Test]
        public void 몬스터_행이_없고_패시브_임계가_오름차순이면_위반을_모아_던진다()
        {
            BalanceConfigRow config = new BalanceConfigRow
            {
                heroHp = 1,
                heroPower = 1,
                runDuration = 300f,
                passiveThresholds = new[] { 0.1f, 0.9f },
                activeThresholds = new[] { 30f },
            };

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => BalanceConfig.Build(config, new MonsterStatRow[0]));

            StringAssert.Contains("passiveThresholds[1]", e.Message);
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                StringAssert.Contains(monster + " 행이 없음", e.Message);
            }
        }

        [Test]
        public void 밸런스_JSON_2종은_EData_값명_파일로_Addressables에_Resource_라벨로_한_번만_등록돼_있다()
        {
            string groups = string.Empty;
            foreach (string path in Directory.GetFiles(GroupDir, "*.asset"))
            {
                groups += File.ReadAllText(path).Replace("\r\n", "\n") + "\n";
            }

            foreach (EData key in Keys)
            {
                string jsonPath = WorldMapTestSupport.JsonDir + key + ".json";
                Assert.IsTrue(File.Exists(jsonPath), jsonPath);
                string entry = "  - m_GUID: " + AssetDatabase.AssetPathToGUID(jsonPath) + "\n    m_Address: " + key
                    + "\n    m_ReadOnly: 0\n    m_SerializedLabels:\n    - Resource\n";
                StringAssert.Contains(entry, groups, key.ToString());
                Assert.AreEqual(1, Regex.Matches(groups, "m_Address: " + key + "\n").Count, key + " 주소 중복");
            }
        }
    }
}
