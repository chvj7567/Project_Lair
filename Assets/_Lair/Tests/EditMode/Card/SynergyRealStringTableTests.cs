using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lair.Card;
using Lair.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 실 Strings_Ko.json 을 실제 StringTableProvider(JsonArrayUtility 파싱)로 태워, 12 tier 각각의
    //# string.Format(GetString(DescriptionStringId), DescriptionArgs) 결과가 spec §6 기대 완성문자열과 일치하는지 검증.
    //# 기존 EditMode 는 fake IStringProvider 로만 돌아 실 json 오타·id 누락·placeholder 불일치(fake↔실 drift)를 못 잡았다 — 이걸 메운다.
    public class SynergyRealStringTableTests
    {
        private const string StringsJsonPath = "Assets/_Lair/Data/Json/Strings_Ko.json";

        private StringTableProvider _real;

        [OneTimeSetUp]
        public void 실json_로드()
        {
            //# Addressable 이라 EditMode 런타임 로드가 까다로움 → 파일 직접 읽어 실 StringTableProvider.Load 경로를 그대로 태운다.
            _real = new StringTableProvider();
            _real.Load(new TextAsset(ReadRawJson()));
        }

        private static string ReadRawJson()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", StringsJsonPath);
            Assert.IsTrue(File.Exists(fullPath), $"Strings_Ko.json 부재: {fullPath}");
            return File.ReadAllText(fullPath);
        }

        //# spec §6 기대 완성문자열 — json 이 아니라 spec/기준표에서 독립 전사(tautology 방지).
        //# 이 리터럴이 drift 를 잡는 1차 방어선이므로 실 json 을 복사하지 않는다.
        private static readonly object[] ExpectedCases =
        {
            new object[] { new TankSynergyTier1(),   "도깨비불·망령 HP ×1.3" },
            new object[] { new TankSynergyTier2(),   "도깨비불·망령 공격력 ×1.2" },
            new object[] { new TankSynergyTier3(),   "도깨비불·망령 HP ×1.4" },
            new object[] { new DpsSynergyTier1(),    "사신·저주술사 공격력 ×1.3" },
            new object[] { new DpsSynergyTier2(),    "사신·저주술사 공속 +25%" },
            new object[] { new DpsSynergyTier3(),    "사신·저주술사 사거리 ×1.3" },
            new object[] { new DebuffSynergyTier1(), "역병귀 둔화 ×0.8" },
            new object[] { new DebuffSynergyTier2(), "영웅 공격력 ×0.85" },
            new object[] { new DebuffSynergyTier3(), "출혈 영구 — 이동 시 1s당 HP -1%" },
            new object[] { new SwarmSynergyTier1(),  "환령·도깨비불 이동속도 ×1.3" },
            new object[] { new SwarmSynergyTier2(),  "모든 스포너 주기 ×0.85" },
            new object[] { new SwarmSynergyTier3(),  "모든 스포너 동시 출력 +1" },
        };

        //# 최우선 — 실 json 템플릿 + tier 유래 인자 조립 결과 == spec §6 완성문자열.
        [TestCaseSource(nameof(ExpectedCases))]
        public void 실json_조립_결과가_spec_기대문자열과_일치(IBuildSynergyTier tier, string expected)
        {
            string template = _real.GetString(tier.DescriptionStringId);
            Assert.IsFalse(string.IsNullOrEmpty(template),
                $"id {tier.DescriptionStringId} 템플릿이 실 json 에 비어있음");
            string assembled = string.Format(CultureInfo.InvariantCulture, template, tier.DescriptionArgs);
            Assert.AreEqual(expected, assembled);
        }

        //# fake provider(SynergyModalTestFakes) 조립 결과와 실 json 조립 결과가 동일 — 이중 안전망.
        //# (동일 오타는 못 잡으므로 위 spec 리터럴 테스트가 우선. 이건 fake 파일이 실 json 을 못 따라간 케이스 방어.)
        [TestCaseSource(nameof(AllTiers))]
        public void 실json_과_fake_provider_조립결과_동일(IBuildSynergyTier tier)
        {
            string realT = _real.GetString(tier.DescriptionStringId);
            string fakeT = Lair.Tests.UI.SynergyModalTestFakes.Strings.GetString(tier.DescriptionStringId);
            string realAsm = string.Format(CultureInfo.InvariantCulture, realT, tier.DescriptionArgs);
            string fakeAsm = string.Format(CultureInfo.InvariantCulture, fakeT, tier.DescriptionArgs);
            Assert.AreEqual(fakeAsm, realAsm, $"id {tier.DescriptionStringId} fake↔실 조립 drift");
        }

        //# 200~211 이 실 json 에 전부 non-empty 템플릿으로 존재 + {0} placeholder 보유(치환 실패 방지).
        [Test]
        public void id_200_211_전부_실json에_존재하고_placeholder_보유()
        {
            for (int id = 200; id <= 211; ++id)
            {
                string t = _real.GetString(id);
                Assert.IsFalse(string.IsNullOrEmpty(t), $"id {id} 템플릿 부재/빈값");
                Assert.IsTrue(t.Contains("{0}"), $"id {id} 에 {{0}} placeholder 없음: '{t}'");
            }
        }

        //# 실 json raw 기준 — 전체 id 중복 없음 + 200~211 각각 정확히 1번 등장(last-wins 로 dup 이 가려지지 않도록 raw 배열 직접 검사).
        //# 전체 유일성 검사가 200블록 vs 기존(<200) 충돌도 함께 잡는다.
        [Test]
        public void 실json_id_중복없고_200블록이_정확히_한번씩()
        {
            JArray arr = JArray.Parse(ReadRawJson());
            List<int> ids = arr.Select(t => (int)t["id"]).ToList();

            HashSet<int> seen = new HashSet<int>();
            foreach (int id in ids)
                Assert.IsTrue(seen.Add(id), $"실 json id {id} 중복");

            for (int id = 200; id <= 211; ++id)
                Assert.AreEqual(1, ids.Count(x => x == id), $"id {id} 등장 횟수");
        }

        //# 조립 대상 tier 12종 (cross-check TestCaseSource 용).
        private static readonly IBuildSynergyTier[] AllTiers =
        {
            new TankSynergyTier1(), new TankSynergyTier2(), new TankSynergyTier3(),
            new DpsSynergyTier1(),  new DpsSynergyTier2(),  new DpsSynergyTier3(),
            new DebuffSynergyTier1(), new DebuffSynergyTier2(), new DebuffSynergyTier3(),
            new SwarmSynergyTier1(), new SwarmSynergyTier2(), new SwarmSynergyTier3(),
        };
    }
}
