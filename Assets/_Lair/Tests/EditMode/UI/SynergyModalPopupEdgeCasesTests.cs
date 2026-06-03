using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.UI
{
    //# 시너지 모달 행 평탄화(BuildRows) 본격 스위트 — 경계·조합·문구·정합·회귀 망라.
    //# 기존 SynergyModalPopupBuildTests 는 정상 + 엣지 1 (빈 리스트 / Tank5Dps3 / 7+ / 정렬 / 12키 비어있지 않음) 수준.
    //# 본 파일은 그 너머: count 0~cap 전 경계, 4축 조합, 라벨 포맷 정확성, TierDesc 12건 문구 정본 일치, AxisColor/AxisLabel 정합.
    public class SynergyModalPopupEdgeCasesTests
    {
        private static Func<EBuildAxis, int> Counts(int tank, int dps, int debuff, int swarm)
        {
            Dictionary<EBuildAxis, int> map = new Dictionary<EBuildAxis, int>
            {
                { EBuildAxis.Tank, tank }, { EBuildAxis.Dps, dps },
                { EBuildAxis.Debuff, debuff }, { EBuildAxis.Swarm, swarm },
            };
            return a => map[a];
        }

        //# 한 축만 활성시키는 Counts (나머지 3축은 0).
        private static Func<EBuildAxis, int> Only(EBuildAxis axis, int count)
        {
            return a => a == axis ? count : 0;
        }

        //# 한 축 그룹(헤더 + 효과행들)에서 효과행만 추출.
        private static List<SynergyModalCellData> EffectsOf(List<SynergyModalCellData> rows)
        {
            List<SynergyModalCellData> effects = new List<SynergyModalCellData>();
            foreach (SynergyModalCellData r in rows)
                if (r.RowKind == SynergyModalCellData.Kind.Effect)
                    effects.Add(r);
            return effects;
        }

        //# ===== 1. count 경계 전수 — 단일 축 (Tank) 행 수 / 헤더 카운트 표기 =====

        //# 미달 경계 0·1·2 — 활성 티어 0 → 행 0개.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void 단일축_미달_0_1_2장_행_0개(int count)
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, count));
            Assert.AreEqual(0, rows.Count, $"{count}장 — 임계 3 미달 → 행 없음");
        }

        //# 임계 도달 경계별 (헤더1 + 효과 N) 정확한 행 수.
        //# 3·4 → Tier1 (효과1, 총2) / 5·6 → Tier1·2 (효과2, 총3) / 7·8·9·100 → Tier1·2·3 (효과3, 총4, cap).
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(6, 2)]
        [TestCase(7, 3)]
        [TestCase(8, 3)]
        [TestCase(9, 3)]
        [TestCase(100, 3)]
        public void 단일축_count별_효과행_수_경계(int count, int expectedEffectRows)
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, count));

            Assert.AreEqual(1 + expectedEffectRows, rows.Count,
                $"{count}장 — 헤더1 + 효과{expectedEffectRows}");
            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[0].RowKind, "첫 행은 헤더");
            Assert.AreEqual(expectedEffectRows, EffectsOf(rows).Count);
        }

        //# 헤더 카운트는 원 count 표기 (임계 7 로 고정되지 않음). 8·9·100 도 원값.
        [TestCase(3, "TANK (3장)")]
        [TestCase(7, "TANK (7장)")]
        [TestCase(9, "TANK (9장)")]
        [TestCase(100, "TANK (100장)")]
        public void 헤더_카운트는_원값_표기(int count, string expectedLabel)
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, count));
            Assert.AreEqual(expectedLabel, rows[0].Label);
        }

        //# Tier3 cap — count 가 아무리 커도 효과행은 최대 3 (Tier4 없음).
        [Test]
        public void count_100장이어도_효과행_최대_3_Tier4_없음()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Dps, 100));
            List<SynergyModalCellData> effects = EffectsOf(rows);

            Assert.AreEqual(3, effects.Count, "임계 3개 상한 → 효과행 최대 3");
            Assert.IsTrue(effects[0].Label.StartsWith("Tier1"));
            Assert.IsTrue(effects[1].Label.StartsWith("Tier2"));
            Assert.IsTrue(effects[2].Label.StartsWith("Tier3"));
            foreach (SynergyModalCellData e in effects)
                Assert.IsFalse(e.Label.StartsWith("Tier4"), "Tier4 행은 존재하지 않아야 함");
        }

        //# ===== 2. 4축 조합 =====

        //# 전부 미달 (모든 축 < 3) → 빈 리스트.
        [Test]
        public void 전부_미달이면_빈_리스트()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(2, 2, 2, 2));
            Assert.AreEqual(0, rows.Count);
        }

        //# 4축 동시 7+ → 산술 상한 16행 (4 × (헤더1 + 효과3)). 기획서 §6.3 / §4 레이아웃.
        [Test]
        public void four축_동시_7이상_16행_상한()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(7, 7, 7, 7));

            Assert.AreEqual(16, rows.Count, "4축 × (헤더1 + 효과3) = 16행 (산술 상한)");

            int headers = 0;
            int effects = 0;
            foreach (SynergyModalCellData r in rows)
            {
                if (r.RowKind == SynergyModalCellData.Kind.Header) ++headers;
                else ++effects;
            }
            Assert.AreEqual(4, headers, "헤더 4개 (축마다 1개)");
            Assert.AreEqual(12, effects, "효과 12개 (축마다 3개)");
        }

        //# 일부 축만 활성 — 미달 축은 헤더·효과 모두 스킵, 활성 축만 정렬 순서대로 등장.
        //# Tank 미달(2), Dps 활성(3), Debuff 미달(0), Swarm 활성(5).
        [Test]
        public void 일부축만_활성_미달축은_완전_스킵()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(2, 3, 0, 5));

            //# Dps 헤더1+효과1=2 , Swarm 헤더1+효과2=3 , 총 5행. Tank/Debuff 등장 X.
            Assert.AreEqual(5, rows.Count);
            Assert.AreEqual("DPS (3장)", rows[0].Label, "활성 첫 축은 Dps (Tank 스킵)");
            Assert.AreEqual("SWARM (5장)", rows[2].Label, "두 번째 활성 축은 Swarm (Debuff 스킵)");

            foreach (SynergyModalCellData r in rows)
            {
                Assert.IsFalse(r.Label.StartsWith("TANK"), "Tank 헤더 미등장");
                Assert.IsFalse(r.Label.StartsWith("DEBUFF"), "Debuff 헤더 미등장");
            }
        }

        //# 축 순서는 입력 카운트 크기와 무관하게 항상 Tank→Dps→Debuff→Swarm.
        //# Swarm 을 가장 많이 줘도 정렬은 고정.
        [Test]
        public void 축_순서는_count_크기와_무관하게_고정()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(3, 5, 7, 9));

            List<string> headerLabels = new List<string>();
            foreach (SynergyModalCellData r in rows)
                if (r.RowKind == SynergyModalCellData.Kind.Header)
                    headerLabels.Add(r.Label);

            Assert.AreEqual(4, headerLabels.Count);
            Assert.AreEqual("TANK (3장)",   headerLabels[0]);
            Assert.AreEqual("DPS (5장)",    headerLabels[1]);
            Assert.AreEqual("DEBUFF (7장)", headerLabels[2]);
            Assert.AreEqual("SWARM (9장)",  headerLabels[3]);
        }

        //# 축 내 효과 순서는 Tier1 → Tier2 → Tier3 오름차순.
        [Test]
        public void 축_내_효과는_Tier1_2_3_오름차순()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Debuff, 7));
            List<SynergyModalCellData> effects = EffectsOf(rows);

            Assert.AreEqual(3, effects.Count);
            Assert.IsTrue(effects[0].Label.StartsWith("Tier1"));
            Assert.IsTrue(effects[1].Label.StartsWith("Tier2"));
            Assert.IsTrue(effects[2].Label.StartsWith("Tier3"));
        }

        //# ===== 3. 라벨 포맷 정확성 =====

        //# 효과 라벨 = "Tier{n}" + 공백 정확히 2칸 + 문구. (1칸/3칸 회귀 방지)
        [Test]
        public void 효과_라벨_Tier뒤_공백_정확히_2칸()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, 3));
            SynergyModalCellData effect = EffectsOf(rows)[0];

            //# 정본: "Tier1  Wisp·Wraith HP ×1.3" — Tier1 다음 정확히 2 space.
            Assert.AreEqual("Tier1  Wisp·Wraith HP ×1.3", effect.Label);
            Assert.IsTrue(effect.Label.StartsWith("Tier1  "), "Tier1 + 공백 2칸");
            Assert.IsFalse(effect.Label.StartsWith("Tier1   "), "공백 3칸 아님");
            //# Tier 토큰과 문구 사이가 정확히 2 space 인지 직접 검증.
            int idx = effect.Label.IndexOf("Wisp");
            Assert.AreEqual("Tier1".Length + 2, idx, "Tier1 과 문구 사이 공백 2칸");
        }

        //# 헤더 라벨 = "{AxisLabel} ({count}장)" — 괄호·'장'·공백 1칸 포맷 전수.
        [TestCase(EBuildAxis.Tank,   "TANK (3장)")]
        [TestCase(EBuildAxis.Dps,    "DPS (3장)")]
        [TestCase(EBuildAxis.Debuff, "DEBUFF (3장)")]
        [TestCase(EBuildAxis.Swarm,  "SWARM (3장)")]
        public void 헤더_라벨_포맷_전수(EBuildAxis axis, string expected)
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(axis, 3));
            Assert.AreEqual(expected, rows[0].Label);
        }

        //# ===== 4. TierDesc 12건 문구 정본 일치 (기획서 §2 표) =====

        //# (axis, tier) → 기대 문구. 기획서 §2 마스터 표 정본 그대로.
        private static readonly Dictionary<(EBuildAxis, int), string> Expected = new()
        {
            { (EBuildAxis.Tank,   1), "Wisp·Wraith HP ×1.3" },
            { (EBuildAxis.Tank,   2), "Wisp·Wraith Power ×1.2" },
            { (EBuildAxis.Tank,   3), "필드 캡 +6 (18→24)" },
            { (EBuildAxis.Dps,    1), "Reaper·Hex Power ×1.3" },
            { (EBuildAxis.Dps,    2), "Reaper·Hex 공속 +25%" },
            { (EBuildAxis.Dps,    3), "Reaper·Hex Range ×1.3" },
            { (EBuildAxis.Debuff, 1), "Plague 둔화 ×0.8" },
            { (EBuildAxis.Debuff, 2), "영웅 공격력 ×0.85" },
            { (EBuildAxis.Debuff, 3), "출혈 영구 — 이동 시 1s당 HP -1%" },
            { (EBuildAxis.Swarm,  1), "Phantom·Wisp 이동속도 ×1.3" },
            { (EBuildAxis.Swarm,  2), "모든 스포너 주기 ×0.85" },
            { (EBuildAxis.Swarm,  3), "모든 스포너 동시 출력 +1" },
        };

        //# 단일 축 7장 시 효과행 3개 라벨이 "Tier{n}  {정본문구}" 와 글자 그대로 일치.
        [TestCase(EBuildAxis.Tank)]
        [TestCase(EBuildAxis.Dps)]
        [TestCase(EBuildAxis.Debuff)]
        [TestCase(EBuildAxis.Swarm)]
        public void TierDesc_정본_문구_축별_전수_일치(EBuildAxis axis)
        {
            List<SynergyModalCellData> effects = EffectsOf(SynergyModalPopup.BuildRows(Only(axis, 7)));
            Assert.AreEqual(3, effects.Count);

            for (int tier = 1; tier <= 3; ++tier)
            {
                string expectedLabel = $"Tier{tier}  {Expected[(axis, tier)]}";
                Assert.AreEqual(expectedLabel, effects[tier - 1].Label,
                    $"{axis} Tier{tier} 문구가 기획서 §2 정본과 정확히 일치해야 함");
            }
        }

        //# 한글화 토큰 3건 회귀 — 기획서 §2.1 의 플레이어 표기 채택분.
        //# Dps2 = 공속 +25% (Cooldown ×0.8 아님), Debuff1 = 둔화 (SlowFactor 아님), Swarm1 = 이동속도 (MoveSpeed 아님).
        [Test]
        public void 한글화_토큰_3건_플레이어_표기_채택()
        {
            string dps2    = EffectsOf(SynergyModalPopup.BuildRows(Only(EBuildAxis.Dps, 5)))[1].Label;
            string debuff1 = EffectsOf(SynergyModalPopup.BuildRows(Only(EBuildAxis.Debuff, 3)))[0].Label;
            string swarm1  = EffectsOf(SynergyModalPopup.BuildRows(Only(EBuildAxis.Swarm, 3)))[0].Label;

            Assert.IsTrue(dps2.Contains("공속 +25%"), "Dps Tier2 — 플레이어 표기 '공속 +25%'");
            Assert.IsFalse(dps2.Contains("Cooldown"), "Dps Tier2 — 시스템 표기 'Cooldown' 미사용");

            Assert.IsTrue(debuff1.Contains("둔화"), "Debuff Tier1 — '둔화'");
            Assert.IsFalse(debuff1.Contains("SlowFactor"), "Debuff Tier1 — 'SlowFactor' 미사용");

            Assert.IsTrue(swarm1.Contains("이동속도"), "Swarm Tier1 — '이동속도'");
            Assert.IsFalse(swarm1.Contains("MoveSpeed"), "Swarm Tier1 — 'MoveSpeed' 미사용");
        }

        //# 효과 문구는 절대 비지 않음 — "Tier{n}  " 로 끝나면(문구 공백) TierDesc 키 누락 회귀.
        [Test]
        public void 효과_문구_절대_비어있지_않음_4축7장()
        {
            List<SynergyModalCellData> effects = EffectsOf(SynergyModalPopup.BuildRows(Counts(7, 7, 7, 7)));
            Assert.AreEqual(12, effects.Count, "4축 × 효과3 = 12건 전수 확인");
            foreach (SynergyModalCellData e in effects)
            {
                Assert.IsFalse(e.Label.EndsWith("  "), $"문구 공백 (TierDesc 키 누락 의심): '{e.Label}'");
                Assert.IsFalse(e.Label.Trim() == "Tier1" || e.Label.Trim() == "Tier2" || e.Label.Trim() == "Tier3",
                    $"Tier 토큰만 있고 문구 없음: '{e.Label}'");
            }
        }

        //# ===== 5. AxisColor / AxisLabel 재사용 정합 =====

        //# 헤더·효과 행의 AxisColor 가 BuildSynergyPanel.AxisColor[axis] 와 정확히 동일 (중복 정의 아님, Rule 02 §5).
        [TestCase(EBuildAxis.Tank)]
        [TestCase(EBuildAxis.Dps)]
        [TestCase(EBuildAxis.Debuff)]
        [TestCase(EBuildAxis.Swarm)]
        public void 행_AxisColor가_BuildSynergyPanel_AxisColor와_동일(EBuildAxis axis)
        {
            Color expected = BuildSynergyPanel.AxisColor[axis];
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(axis, 7));

            foreach (SynergyModalCellData r in rows)
                Assert.AreEqual(expected, r.AxisColor,
                    $"{axis} 행(헤더/효과)의 색이 BuildSynergyPanel.AxisColor[{axis}] 와 동일해야 함");
        }

        //# 한 축 그룹 내에서 헤더 색 == 효과 행 색 (시각적으로 같은 축 그룹임을 잇는다, 기획서 §3.2).
        [Test]
        public void 헤더와_효과행_색이_같은_축이면_동일()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Swarm, 7));
            Color headerColor = rows[0].AxisColor;
            foreach (SynergyModalCellData r in rows)
                Assert.AreEqual(headerColor, r.AxisColor, "같은 축 헤더·효과 색 동일");
        }

        //# 헤더 라벨의 축 토큰이 BuildSynergyPanel.AxisLabel[axis] 와 동일 (재사용 정합).
        [TestCase(EBuildAxis.Tank)]
        [TestCase(EBuildAxis.Dps)]
        [TestCase(EBuildAxis.Debuff)]
        [TestCase(EBuildAxis.Swarm)]
        public void 헤더_축토큰이_AxisLabel과_동일(EBuildAxis axis)
        {
            string expectedLabel = BuildSynergyPanel.AxisLabel[axis];
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(axis, 3));
            Assert.IsTrue(rows[0].Label.StartsWith($"{expectedLabel} ("),
                $"헤더가 '{expectedLabel} (' 로 시작해야 함 — AxisLabel 재사용");
        }

        //# ===== 6. 회귀 / 방어 =====

        //# countOf == null 가드 — 예외 없이 빈 리스트 (구현 가드 회귀).
        [Test]
        public void countOf_null이면_예외없이_빈_리스트()
        {
            List<SynergyModalCellData> rows = null;
            Assert.DoesNotThrow(() => rows = SynergyModalPopup.BuildRows(null));
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        //# 음수 카운트 — >= 임계 비교상 활성 0 → 빈 리스트 (비정상 입력 방어).
        [Test]
        public void 음수_카운트는_빈_리스트()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, -5));
            Assert.AreEqual(0, rows.Count, "음수 count 는 임계 미달 취급");
        }

        //# 매 호출이 새 리스트 인스턴스 — 호출 간 공유/누수 없음 (순수 함수 회귀).
        [Test]
        public void 매_호출마다_새_리스트_인스턴스_반환()
        {
            List<SynergyModalCellData> a = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, 7));
            List<SynergyModalCellData> b = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, 7));
            Assert.AreNotSame(a, b, "호출마다 새 List 반환 (정적 캐시 공유 금지)");
            Assert.AreEqual(a.Count, b.Count);
        }

        //# 모든 행은 Header 또는 Effect 중 하나 (RowKind 누락/미정의 회귀).
        [Test]
        public void 모든_행은_Header_또는_Effect()
        {
            List<SynergyModalCellData> rows = SynergyModalPopup.BuildRows(Counts(3, 5, 7, 4));
            foreach (SynergyModalCellData r in rows)
                Assert.IsTrue(r.RowKind == SynergyModalCellData.Kind.Header
                           || r.RowKind == SynergyModalCellData.Kind.Effect,
                    "RowKind 는 Header/Effect 둘 중 하나");
        }
    }
}
