using System;
using System.Collections.Generic;
using System.IO;
using Lair.Card;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.UI
{
    //# 실 Strings_Ko.json + 실 tier 인스턴스(SynergyModalTestFakes.TierOf)로 BuildRows 를 태워, 티어 행 Label 이
    //# "Tier{n}  {실json 완성설명}" 인지 통합 검증 — fake 템플릿이 아니라 실 데이터를 조립까지 관통한다(행수·RowKind·Tier접두 회귀 포함).
    public class SynergyModalRealStringIntegrationTests
    {
        private const string StringsJsonPath = "Assets/_Lair/Data/Json/Strings_Ko.json";
        private StringTableProvider _real;

        [OneTimeSetUp]
        public void 실json_로드()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", StringsJsonPath);
            Assert.IsTrue(File.Exists(fullPath), $"Strings_Ko.json 부재: {fullPath}");
            _real = new StringTableProvider();
            _real.Load(new TextAsset(File.ReadAllText(fullPath)));
        }

        private static Func<EBuildAxis, int> Counts(int tank, int dps, int debuff, int swarm)
        {
            Dictionary<EBuildAxis, int> map = new Dictionary<EBuildAxis, int>
            {
                { EBuildAxis.Tank, tank }, { EBuildAxis.Dps, dps },
                { EBuildAxis.Debuff, debuff }, { EBuildAxis.Swarm, swarm },
            };
            return a => map[a];
        }

        //# Tank 7 → 헤더1 + Tier1/2/3 효과3. 배율 표기 3종을 실 json 조립으로 확인(Tier3 는 구 "필드 캡 +6" stale 교정 반영).
        [Test]
        public void Tank7_실json_Tier1_2_3_완성라벨()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(
                Counts(7, 0, 0, 0), SynergyModalTestFakes.TierOf, _real);

            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[0].RowKind);
            Assert.AreEqual("TANK (7장)", rows[0].Label);
            Assert.AreEqual(SynergyModalCellData.Kind.Effect, rows[1].RowKind);
            Assert.AreEqual("Tier1  도깨비불·망령 HP ×1.3", rows[1].Label);
            Assert.AreEqual("Tier2  도깨비불·망령 공격력 ×1.2", rows[2].Label);
            Assert.AreEqual("Tier3  도깨비불·망령 HP ×1.4", rows[3].Label);
        }

        //# Dps 7 → 파생 표기(Dps2 쿨다운→공속 +25%) 를 실 json 조립으로 확인.
        [Test]
        public void Dps7_실json_공속_사거리_완성라벨()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(
                Counts(0, 7, 0, 0), SynergyModalTestFakes.TierOf, _real);

            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("Tier1  사신·저주술사 공격력 ×1.3", rows[1].Label);
            Assert.AreEqual("Tier2  사신·저주술사 공속 +25%", rows[2].Label);
            Assert.AreEqual("Tier3  사신·저주술사 사거리 ×1.3", rows[3].Label);
        }

        //# Debuff 7 → Debuff3 파생 표기(비율→%/s) + em-dash 완성라벨을 실 json 조립으로 확인.
        [Test]
        public void Debuff7_실json_출혈영구_완성라벨()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(
                Counts(0, 0, 7, 0), SynergyModalTestFakes.TierOf, _real);

            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("Tier1  역병귀 둔화 ×0.8", rows[1].Label);
            Assert.AreEqual("Tier2  영웅 공격력 ×0.85", rows[2].Label);
            Assert.AreEqual("Tier3  출혈 영구 — 이동 시 1s당 HP -1%", rows[3].Label);
        }

        //# Swarm 7 → 스포너 3종 완성라벨(정수 인자 +1 포함) 을 실 json 조립으로 확인.
        [Test]
        public void Swarm7_실json_스포너_완성라벨()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(
                Counts(0, 0, 0, 7), SynergyModalTestFakes.TierOf, _real);

            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("Tier1  환령·도깨비불 이동속도 ×1.3", rows[1].Label);
            Assert.AreEqual("Tier2  모든 스포너 주기 ×0.85", rows[2].Label);
            Assert.AreEqual("Tier3  모든 스포너 동시 출력 +1", rows[3].Label);
        }

        //# 엣지(보강) — 실 provider 지만 한 축(Swarm) tier 가 미바인딩(null)이면 그 효과행만 빈 설명, 다른 축은 정상. 예외 없음.
        //# 기존 provider_null / 전체 미바인딩 테스트와 달리 "일부 축만 미바인딩 + 실 provider" 혼합 경로를 커버.
        [Test]
        public void 실provider_일부축_미바인딩이면_그축만_빈설명()
        {
            Func<EBuildAxis, int, IBuildSynergyTier> partial =
                (axis, threshold) => axis == EBuildAxis.Swarm
                    ? null
                    : SynergyModalTestFakes.TierOf(axis, threshold);

            List<SynergyModalCellData> rows = null;
            Assert.DoesNotThrow(() =>
                rows = SynergyModalPopup.BuildRows(Counts(3, 0, 0, 3), partial, _real));

            //# Tank 헤더+효과1, Swarm 헤더+효과1 = 4행.
            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("TANK (3장)", rows[0].Label);
            Assert.AreEqual("Tier1  도깨비불·망령 HP ×1.3", rows[1].Label);
            Assert.AreEqual("SWARM (3장)", rows[2].Label);
            Assert.IsTrue(rows[3].Label.EndsWith("  "), $"미바인딩 축은 빈 설명이어야 함: '{rows[3].Label}'");
        }
    }
}
