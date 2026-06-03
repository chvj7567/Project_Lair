using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.UI
{
    //# 2026-06-03 델타 — BuildRows(iconOf) 헤더 아이콘 경로 본격 스위트 (순수 EditMode).
    //# 기존 SynergyModalPopupIconTests 는 정상 + 엣지 1 (Tank7 헤더 set/효과 null / iconOf 미지정 헤더 null) 수준.
    //# 본 파일은 그 너머: 축 선택적 stub / 4축 1:1 identity / iconOf 호출 횟수 / 효과 행 null 망라 /
    //# 활성축에 null 반환하는 iconOf 가드 / iconOf 추가가 기존 행 구조(수·순서·RowKind·Label)를 깨지 않는 회귀.
    //# BuildSynergy 관례(Service↔EdgeCases 분리 + 아이콘 전용 파일)를 따라 IconTests 와 별도 파일로 분리.
    public class SynergyModalPopupIconEdgeCasesTests
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

        private static Func<EBuildAxis, int> Only(EBuildAxis axis, int count)
        {
            return a => a == axis ? count : 0;
        }

        //# 축마다 고유 sentinel Sprite — identity(AreSame) 비교로 1:1 매핑 검증용.
        private static Sprite NewSprite()
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        }

        //# 헤더 행만 추출.
        private static List<SynergyModalCellData> HeadersOf(List<SynergyModalCellData> rows)
        {
            List<SynergyModalCellData> headers = new List<SynergyModalCellData>();
            foreach (SynergyModalCellData r in rows)
                if (r.RowKind == SynergyModalCellData.Kind.Header)
                    headers.Add(r);
            return headers;
        }

        //# 생성된 sprite 정리 — 테스트 간 누수 방지.
        private readonly List<Sprite> _created = new List<Sprite>();

        private Sprite Track(Sprite s)
        {
            _created.Add(s);
            return s;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Sprite s in _created)
                if (s != null)
                    UnityEngine.Object.DestroyImmediate(s);
            _created.Clear();
        }

        //# ===== 1. 효과 행 Icon null 망라 (헤더 set + 효과 전부 null) =====

        //# 단일 축 — count 별로 헤더 Icon set, 효과행 전부 null. Tier1/2/3 모든 경계.
        [TestCase(EBuildAxis.Tank, 3)]
        [TestCase(EBuildAxis.Tank, 5)]
        [TestCase(EBuildAxis.Tank, 7)]
        [TestCase(EBuildAxis.Dps, 7)]
        [TestCase(EBuildAxis.Debuff, 5)]
        [TestCase(EBuildAxis.Swarm, 3)]
        public void 헤더만_Icon_채우고_효과행은_전부_null(EBuildAxis axis, int count)
        {
            Sprite icon = Track(NewSprite());
            List<SynergyModalCellData> rows =
                SynergyModalPopup.BuildRows(Only(axis, count), a => a == axis ? icon : null);

            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[0].RowKind);
            Assert.AreSame(icon, rows[0].Icon, "헤더 Icon 은 iconOf(axis) 결과");
            for (int i = 1; i < rows.Count; ++i)
            {
                Assert.AreEqual(SynergyModalCellData.Kind.Effect, rows[i].RowKind);
                Assert.IsNull(rows[i].Icon, $"효과 행 {i} 의 Icon 은 항상 null (기획서 §3.2)");
            }
        }

        //# 4축 동시 7+ (16행) — 효과 행 12개 전부 Icon null, 헤더 4개만 Icon set.
        [Test]
        public void four축_동시_효과12행_전부_Icon_null_헤더4개만_set()
        {
            Dictionary<EBuildAxis, Sprite> map = new Dictionary<EBuildAxis, Sprite>
            {
                { EBuildAxis.Tank, Track(NewSprite()) },
                { EBuildAxis.Dps, Track(NewSprite()) },
                { EBuildAxis.Debuff, Track(NewSprite()) },
                { EBuildAxis.Swarm, Track(NewSprite()) },
            };
            List<SynergyModalCellData> rows =
                SynergyModalPopup.BuildRows(Counts(7, 7, 7, 7), a => map[a]);

            int headerWithIcon = 0;
            int effectNull = 0;
            foreach (SynergyModalCellData r in rows)
            {
                if (r.RowKind == SynergyModalCellData.Kind.Header)
                {
                    Assert.IsNotNull(r.Icon, "헤더는 Icon 채워짐");
                    ++headerWithIcon;
                }
                else
                {
                    Assert.IsNull(r.Icon, "효과 행은 Icon null");
                    ++effectNull;
                }
            }
            Assert.AreEqual(4, headerWithIcon, "헤더 4개 Icon set");
            Assert.AreEqual(12, effectNull, "효과 12개 Icon null");
        }

        //# ===== 2. 4축 1:1 identity 매핑 — 각 헤더 Icon == iconOf(해당 축) =====

        //# 4축 활성 시 각 축 헤더 Icon 이 iconOf(axis) 결과와 정확히 같은 인스턴스 (뒤섞임/오매핑 회귀).
        [Test]
        public void four축_각_헤더_Icon이_iconOf_결과와_1대1_identity()
        {
            Dictionary<EBuildAxis, Sprite> map = new Dictionary<EBuildAxis, Sprite>
            {
                { EBuildAxis.Tank, Track(NewSprite()) },
                { EBuildAxis.Dps, Track(NewSprite()) },
                { EBuildAxis.Debuff, Track(NewSprite()) },
                { EBuildAxis.Swarm, Track(NewSprite()) },
            };
            Func<EBuildAxis, Sprite> iconOf = a => map[a];

            List<SynergyModalCellData> headers =
                HeadersOf(SynergyModalPopup.BuildRows(Counts(3, 3, 3, 3), iconOf));

            //# 헤더 순서 = 축 순서 Tank→Dps→Debuff→Swarm.
            Assert.AreEqual(4, headers.Count);
            Assert.AreSame(map[EBuildAxis.Tank], headers[0].Icon, "0번 헤더(Tank) Icon");
            Assert.AreSame(map[EBuildAxis.Dps], headers[1].Icon, "1번 헤더(Dps) Icon");
            Assert.AreSame(map[EBuildAxis.Debuff], headers[2].Icon, "2번 헤더(Debuff) Icon");
            Assert.AreSame(map[EBuildAxis.Swarm], headers[3].Icon, "3번 헤더(Swarm) Icon");
        }

        //# 모든 sentinel 이 서로 다른 인스턴스인지(테스트 자체의 identity 비교 유효성) 사전 확인.
        [Test]
        public void sentinel_Sprite들은_서로_다른_인스턴스()
        {
            Sprite a = Track(NewSprite());
            Sprite b = Track(NewSprite());
            Assert.AreNotSame(a, b, "축별 sentinel 이 달라야 identity 비교가 유효");
        }

        //# ===== 3. 축 선택적 iconOf stub — 일부 축만 sprite, 나머지 null =====

        //# iconOf 가 Tank·Debuff 만 sprite 반환, Dps·Swarm 은 null 반환.
        //# → Tank·Debuff 헤더만 Icon set, Dps·Swarm 헤더 Icon null. (행 수·순서는 불변.)
        [Test]
        public void iconOf_일부축만_sprite_나머지_null_반환시_해당축_헤더만_Icon()
        {
            Sprite tankIcon = Track(NewSprite());
            Sprite debuffIcon = Track(NewSprite());
            //# Dps·Swarm 은 null (인스펙터 미할당 가정).
            Func<EBuildAxis, Sprite> iconOf = a =>
            {
                if (a == EBuildAxis.Tank) return tankIcon;
                if (a == EBuildAxis.Debuff) return debuffIcon;
                return null;
            };

            List<SynergyModalCellData> headers =
                HeadersOf(SynergyModalPopup.BuildRows(Counts(3, 3, 3, 3), iconOf));

            Assert.AreEqual(4, headers.Count, "4축 모두 활성 — 헤더 수는 아이콘 유무와 무관");
            Assert.AreSame(tankIcon, headers[0].Icon, "Tank 헤더 Icon set");
            Assert.IsNull(headers[1].Icon, "Dps 헤더 Icon null (iconOf 가 null 반환)");
            Assert.AreSame(debuffIcon, headers[2].Icon, "Debuff 헤더 Icon set");
            Assert.IsNull(headers[3].Icon, "Swarm 헤더 Icon null (iconOf 가 null 반환)");
        }

        //# 활성 축에 iconOf 가 null 을 반환해도 예외 없이 헤더 Icon=null (셀이 아이콘 숨김, 기획서 §3.1 가드).
        [Test]
        public void 활성축에_iconOf가_null반환해도_예외없이_헤더_Icon_null()
        {
            Func<EBuildAxis, Sprite> allNull = a => null;
            List<SynergyModalCellData> rows = null;

            Assert.DoesNotThrow(() => rows = SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, 7), allNull));
            Assert.AreEqual(4, rows.Count, "헤더1 + 효과3 — 행 수는 불변");
            Assert.IsNull(rows[0].Icon, "iconOf 가 null 반환 시 헤더 Icon 도 null");
        }

        //# ===== 4. iconOf 호출 횟수 / 호출 대상 =====

        //# iconOf 는 활성 축 헤더에 대해서만, 축당 정확히 1회 호출 (효과 행/스킵 축엔 호출 안 함).
        [Test]
        public void iconOf는_활성축당_정확히_1회_효과행엔_미호출()
        {
            Dictionary<EBuildAxis, int> calls = new Dictionary<EBuildAxis, int>
            {
                { EBuildAxis.Tank, 0 }, { EBuildAxis.Dps, 0 },
                { EBuildAxis.Debuff, 0 }, { EBuildAxis.Swarm, 0 },
            };
            Func<EBuildAxis, Sprite> iconOf = a =>
            {
                ++calls[a];
                return null;
            };

            //# Tank 7(헤더1+효과3), Dps 3(헤더1+효과1) 활성 / Debuff 2, Swarm 0 스킵.
            SynergyModalPopup.BuildRows(Counts(7, 3, 2, 0), iconOf);

            Assert.AreEqual(1, calls[EBuildAxis.Tank], "Tank 활성 — 헤더 1회 (효과 3행엔 미호출)");
            Assert.AreEqual(1, calls[EBuildAxis.Dps], "Dps 활성 — 헤더 1회");
            Assert.AreEqual(0, calls[EBuildAxis.Debuff], "Debuff 미달(2장) — 헤더 미생성 → iconOf 미호출");
            Assert.AreEqual(0, calls[EBuildAxis.Swarm], "Swarm 0장 — iconOf 미호출");
        }

        //# 전부 미달이면 iconOf 가 단 한 번도 호출되지 않음 (헤더 0개).
        [Test]
        public void 전부_미달이면_iconOf_미호출()
        {
            int callCount = 0;
            Func<EBuildAxis, Sprite> iconOf = a =>
            {
                ++callCount;
                return null;
            };
            SynergyModalPopup.BuildRows(Counts(2, 2, 2, 2), iconOf);
            Assert.AreEqual(0, callCount, "활성 티어 0 → 헤더 0 → iconOf 미호출");
        }

        //# ===== 5. iconOf 추가가 기존 행 구조를 깨지 않는 회귀 (1-arg vs 2-arg 동치) =====

        //# 동일 count 에서 iconOf 주입 유무와 무관하게 행 수·순서·RowKind·Label 불변 (Icon 만 차이).
        [Test]
        public void iconOf_주입해도_행수_순서_RowKind_Label_불변_1arg와_동치()
        {
            Func<EBuildAxis, int> counts = Counts(5, 3, 7, 4);

            List<SynergyModalCellData> without = SynergyModalPopup.BuildRows(counts);
            List<SynergyModalCellData> with =
                SynergyModalPopup.BuildRows(counts, a => Track(NewSprite()));

            Assert.AreEqual(without.Count, with.Count, "iconOf 유무로 행 수 불변");
            for (int i = 0; i < without.Count; ++i)
            {
                Assert.AreEqual(without[i].RowKind, with[i].RowKind, $"{i}행 RowKind 불변");
                Assert.AreEqual(without[i].Label, with[i].Label, $"{i}행 Label 불변");
                Assert.AreEqual(without[i].AxisColor, with[i].AxisColor, $"{i}행 AxisColor 불변");
            }
        }

        //# 1-arg 경로(iconOf 미지정)는 모든 헤더 Icon 이 null (기존 26곳 호출부 무회귀 박제).
        [Test]
        public void one_arg_경로는_모든_헤더_Icon_null()
        {
            List<SynergyModalCellData> headers =
                HeadersOf(SynergyModalPopup.BuildRows(Counts(7, 7, 7, 7)));

            Assert.AreEqual(4, headers.Count);
            foreach (SynergyModalCellData h in headers)
                Assert.IsNull(h.Icon, "1-arg 호출 시 헤더 Icon 은 null (기획서 §8 무회귀)");
        }

        //# 헤더 Label/카운트 표기는 iconOf 주입과 무관 — 아이콘 추가가 라벨 포맷을 건드리지 않음.
        [Test]
        public void iconOf_주입해도_헤더_라벨_포맷_그대로()
        {
            Sprite icon = Track(NewSprite());
            List<SynergyModalCellData> rows =
                SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, 5), a => icon);

            Assert.AreEqual("TANK (5장)", rows[0].Label, "아이콘 추가가 헤더 라벨 포맷을 바꾸지 않음");
            Assert.AreSame(icon, rows[0].Icon);
        }
    }
}
