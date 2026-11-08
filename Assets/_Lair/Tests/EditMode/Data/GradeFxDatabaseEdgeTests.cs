using System.IO;
using ChvjUnityInfra;
using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# 등급 연출 데이터 검증·조회 엣지·회귀(monster-grades §8, T2) - 시드(GradeFxDatabaseTests)의 합성·고아 grade·frameOffset<0·복합키 중복과 중복하지 않는다.
    //# 마지막 구간은 실제 Data/Json 6종을 그대로 빌드해 §8 표와 대조한다(읽기 전용).
    public class GradeFxDatabaseEdgeTests
    {
        private const string JsonDir = "Assets/_Lair/Data/Json/";

        private static GradeRow[] Grades()
        {
            GradeRow[] rows = new GradeRow[5];
            float[] size = { 1f, 1.075f, 1.15f, 1.225f, 1.3f };
            for (int i = 0; i < 5; i++)
            {
                rows[i] = new GradeRow { grade = i + 1, sizeMul = size[i], hpMul = 1f, atkMul = 1f };
            }
            return rows;
        }

        private static Monster_GradeFxColorRow[] Colors()
        {
            Monster_GradeFxColorRow[] rows = new Monster_GradeFxColorRow[6];
            for (int i = 0; i < 6; i++)
            {
                rows[i] = new Monster_GradeFxColorRow { monster = ((EMonster)i).ToString(), glowColor = "#3FCF86" };
            }
            return rows;
        }

        private static Grade_FxRow Fx(int grade, int index, string kind = "Tornado", float scale = 1f)
        {
            return new Grade_FxRow { grade = grade, index = index, kind = kind, scale = scale, opacity = 0.9f };
        }

        private static GradeFxDatabase BuildFx(params Grade_FxRow[] fx) => GradeFxDatabase.Build(Grades(), null, null, null, fx, Colors());

        //# ───────── Grade.json (필수 1:1) ─────────

        [Test]
        public void Grade_sizeMul_이_0_이하이거나_NaN_이면_검증_오류다()
        {
            foreach (float bad in new[] { 0f, -1f, float.NaN })
            {
                GradeRow[] grades = Grades();
                grades[2].sizeMul = bad;
                ChapterDataException ex = Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(grades, null, null, null, null, Colors()), $"sizeMul {bad}");
                StringAssert.Contains("sizeMul", ex.Message);
            }
        }

        [Test]
        public void Grade_JSON_자체가_null_이면_검증_오류다()
        {
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(null, null, null, null, null, Colors()));
        }

        [Test]
        public void 등록되지_않은_grade_의_크기_배율은_1_이다()
        {
            GradeFxDatabase db = GradeFxDatabase.Build(Grades(), null, null, null, null, Colors());

            Assert.AreEqual(1f, db.SizeMulOf(0));
            Assert.AreEqual(1f, db.SizeMulOf(6));
            Assert.AreEqual(1f, db.SizeMulOf(-3));
            Assert.AreEqual(1.15f, db.SizeMulOf(3), 0.0001f);
        }

        //# ───────── 선택적 관계 행 검증 ─────────

        [Test]
        public void Glow_는_1등급_폭_0_중복_고아_행을_모두_거부한다()
        {
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), new[] { new Grade_GlowRow { grade = 1, widthCell = 0.01f } }, null, null, null, Colors()), "1등급");
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), new[] { new Grade_GlowRow { grade = 2, widthCell = 0f } }, null, null, null, Colors()), "폭 0");
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), new[] { new Grade_GlowRow { grade = 6, widthCell = 0.1f } }, null, null, null, Colors()), "고아 grade");
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(),
                new[] { new Grade_GlowRow { grade = 2, widthCell = 0.1f }, new Grade_GlowRow { grade = 2, widthCell = 0.2f } }, null, null, null, Colors()), "중복");
        }

        [Test]
        public void Aura_는_1등급_불투명도_0_스케일_0_중복_고아_행을_모두_거부한다()
        {
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, new[] { new Grade_AuraRow { grade = 1, opacity = 1f, scale = 1f } }, null, null, Colors()), "1등급");
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, new[] { new Grade_AuraRow { grade = 2, opacity = 0f, scale = 1f } }, null, null, Colors()), "불투명도 0");
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, new[] { new Grade_AuraRow { grade = 2, opacity = 1f, scale = 0f } }, null, null, Colors()), "스케일 0");
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, new[] { new Grade_AuraRow { grade = 7, opacity = 1f, scale = 1f } }, null, null, Colors()), "고아 grade");
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null,
                new[] { new Grade_AuraRow { grade = 3, opacity = 1f, scale = 1f }, new Grade_AuraRow { grade = 3, opacity = 1f, scale = 1f } }, null, null, Colors()), "중복");
        }

        [Test]
        public void Pulse_는_주기_0_최소_최대_역전_고아_중복_행을_거부한다()
        {
            Grade_PulseRow Good() => new Grade_PulseRow { grade = 5, brightnessMin = 1.15f, brightnessMax = 1.45f, saturationMin = 1.5f, saturationMax = 1.8f, periodSec = 1.2f };

            Grade_PulseRow zero = Good();
            zero.periodSec = 0f;
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, new[] { zero }, null, Colors()), "주기 0");

            Grade_PulseRow brightness = Good();
            brightness.brightnessMax = 1f;
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, new[] { brightness }, null, Colors()), "밝기 최소 > 최대");

            Grade_PulseRow saturation = Good();
            saturation.saturationMax = 1f;
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, new[] { saturation }, null, Colors()), "채도 최소 > 최대");

            Grade_PulseRow orphan = Good();
            orphan.grade = 9;
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, new[] { orphan }, null, Colors()), "고아 grade");

            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, new[] { Good(), Good() }, null, Colors()), "중복");
        }

        [Test]
        public void Pulse_최소와_최대가_같아도_허용한다_맥동_없음과_같은_평평한_값()
        {
            Grade_PulseRow flat = new Grade_PulseRow { grade = 5, brightnessMin = 1f, brightnessMax = 1f, saturationMin = 1f, saturationMax = 1f, periodSec = 1f };

            GradeFxDatabase db = GradeFxDatabase.Build(Grades(), null, null, new[] { flat }, null, Colors());

            Assert.IsNotNull(db.SpecOf(EMonster.Wisp, 5).Pulse);
        }

        [Test]
        public void Fx_는_소문자_kind_음수_index_스케일_0_1등급을_거부한다()
        {
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(4, 0, "tornado")), "소문자 kind");
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(4, 0, "")), "빈 kind");
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(4, 0, null)), "null kind");
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(4, -1)), "음수 index");
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(4, 0, "Tornado", 0f)), "스케일 0");
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(1, 0)), "1등급");
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(6, 0)), "고아 grade");
        }

        [Test]
        public void Fx_복합키는_grade_가_다르면_같은_index_를_허용한다()
        {
            GradeFxDatabase db = BuildFx(Fx(4, 0), Fx(5, 0), Fx(5, 1, "Sunburst", 2.4f));

            Assert.AreEqual(1, db.SpecOf(EMonster.Wisp, 4).Fx.Count);
            Assert.AreEqual(2, db.SpecOf(EMonster.Wisp, 5).Fx.Count);
        }

        [Test]
        public void Fx_는_입력_순서와_무관하게_index_순으로_정렬된다()
        {
            GradeFxDatabase db = BuildFx(Fx(5, 3), Fx(5, 0), Fx(5, 2), Fx(5, 1));

            GradeFxSpec spec = db.SpecOf(EMonster.Wisp, 5);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, spec.Fx[i].index);
            }
        }

        [Test]
        public void Fx_배열_안의_null_행은_검증_오류다()
        {
            Assert.Throws<ChapterDataException>(() => BuildFx(Fx(4, 0), null));
        }

        //# ───────── 색 ─────────

        [Test]
        public void 기준_몬스터_색이_하나라도_빠지면_검증_오류다()
        {
            Monster_GradeFxColorRow[] colors = Colors();
            System.Array.Resize(ref colors, 5);

            ChapterDataException ex = Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, null, null, colors));
            StringAssert.Contains("Phantom", ex.Message);
        }

        [Test]
        public void 색은_알_수_없는_몬스터_잘못된_색_중복을_거부한다()
        {
            Monster_GradeFxColorRow[] unknown = Colors();
            unknown[0].monster = "Dragon";
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, null, null, unknown), "알 수 없는 몬스터");

            Monster_GradeFxColorRow[] badColor = Colors();
            badColor[1].glowColor = "#ZZZZZZ";
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, null, null, badColor), "잘못된 색");

            Monster_GradeFxColorRow[] dup = Colors();
            dup[2].monster = dup[1].monster;
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, null, null, dup), "중복 몬스터");
        }

        [Test]
        public void 색_배열이_null_이면_모든_기준_몬스터가_빠진_것이라_검증_오류다()
        {
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(Grades(), null, null, null, null, null));
        }

        //# ───────── 오류 집계 / 조회 ─────────

        [Test]
        public void 여러_파일의_위반은_하나의_예외에_모두_모인다()
        {
            GradeRow[] grades = Grades();
            grades[2].sizeMul = 0f;

            ChapterDataException ex = Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(
                grades,
                new[] { new Grade_GlowRow { grade = 2, widthCell = 0f } },
                new[] { new Grade_AuraRow { grade = 2, opacity = 0f, scale = 1f } },
                new[] { new Grade_PulseRow { grade = 5, periodSec = 0f } },
                new[] { Fx(4, 0, "bad") },
                Colors()));

            StringAssert.Contains("Grade.json", ex.Message);
            StringAssert.Contains("Grade_Glow.json", ex.Message);
            StringAssert.Contains("Grade_Aura.json", ex.Message);
            StringAssert.Contains("Grade_Pulse.json", ex.Message);
            StringAssert.Contains("Grade_Fx.json", ex.Message);
        }

        [Test]
        public void SpecOf_가_돌려준_Fx_목록을_수정해도_DB_는_변하지_않는다()
        {
            GradeFxDatabase db = BuildFx(Fx(4, 0), Fx(4, 1));
            GradeFxSpec first = db.SpecOf(EMonster.Wisp, 4);

            first.Fx.Clear();

            Assert.AreEqual(2, db.SpecOf(EMonster.Wisp, 4).Fx.Count, "스펙은 복사본");
        }

        [Test]
        public void SpecOf_는_기준_몬스터의_색을_쓰고_색이_없는_몬스터는_흰색이다()
        {
            Monster_GradeFxColorRow[] colors = Colors();
            colors[0].glowColor = "#FF0000";
            GradeFxDatabase db = GradeFxDatabase.Build(Grades(), null, null, null, null, colors);

            Color wisp = db.SpecOf(EMonster.Wisp, 3).GlowColor;
            Color wispGraded = db.SpecOf(EMonster.WispG3, 3).GlowColor;

            Assert.AreEqual("FF0000", ColorUtility.ToHtmlStringRGB(wisp));
            Assert.AreEqual("FFFFFF", ColorUtility.ToHtmlStringRGB(wispGraded), "색 표는 기준 6종 키 - 호출부(BattleController)가 BaseOf 로 환산해야 함");
        }

        [Test]
        public void 연출_행이_없는_등급은_크기만_있고_나머지는_비어_있다()
        {
            GradeFxDatabase db = GradeFxDatabase.Build(Grades(), null, null, null, null, Colors());

            GradeFxSpec spec = db.SpecOf(EMonster.Reaper, 3);

            Assert.AreEqual(1.15f, spec.SizeMul, 0.0001f);
            Assert.IsFalse(spec.HasGlow || spec.HasAura);
            Assert.IsNull(spec.Pulse);
            Assert.AreEqual(0, spec.Fx.Count);
        }

        //# ───────── 실제 JSON (회귀 고정) ─────────

        private static T[] Load<T>(string fileName)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(JsonDir + fileName));
        }

        private static GradeFxDatabase BuildReal()
        {
            return GradeFxDatabase.Build(
                Load<GradeRow>("Grade.json"),
                Load<Grade_GlowRow>("Grade_Glow.json"),
                Load<Grade_AuraRow>("Grade_Aura.json"),
                Load<Grade_PulseRow>("Grade_Pulse.json"),
                Load<Grade_FxRow>("Grade_Fx.json"),
                Load<Monster_GradeFxColorRow>("Monster_GradeFxColor.json"));
        }

        [Test]
        public void 실제_JSON_6종은_검증을_통과한다()
        {
            Assert.DoesNotThrow(() => BuildReal());
        }

        [Test]
        public void 실제_JSON_크기_배율은_기획서_8_표와_같다()
        {
            GradeFxDatabase db = BuildReal();
            float[] expected = { 1f, 1.075f, 1.15f, 1.225f, 1.3f };

            for (int grade = 1; grade <= 5; grade++)
            {
                Assert.AreEqual(expected[grade - 1], db.SizeMulOf(grade), 0.0001f, $"{grade}등급 sizeMul");
            }
        }

        [Test]
        public void 실제_JSON_1등급은_연출이_없고_2_5등급은_후광과_오라가_있다()
        {
            GradeFxDatabase db = BuildReal();
            float[] glow = { 0.0125f, 0.025f, 0.04375f, 0.075f };
            float[] opacity = { 0.45f, 0.65f, 0.85f, 1f };
            float[] scale = { 1.1f, 1.4f, 1.7f, 2f };

            GradeFxSpec rookie = db.SpecOf(EMonster.Wisp, 1);
            Assert.IsFalse(rookie.HasGlow || rookie.HasAura || rookie.Pulse != null || rookie.Fx.Count > 0, "1등급 무효과");

            for (int grade = 2; grade <= 5; grade++)
            {
                GradeFxSpec spec = db.SpecOf(EMonster.Wisp, grade);
                Assert.IsTrue(spec.HasGlow && spec.HasAura, $"{grade}등급");
                Assert.AreEqual(glow[grade - 2], spec.GlowWidthCell, 0.00001f, $"{grade}등급 후광 폭");
                Assert.AreEqual(opacity[grade - 2], spec.AuraOpacity, 0.00001f, $"{grade}등급 오라 불투명도");
                Assert.AreEqual(scale[grade - 2], spec.AuraScale, 0.00001f, $"{grade}등급 오라 스케일");
                Assert.AreEqual(0.075f, spec.AuraOffsetY, 0.00001f, $"{grade}등급 오라 offsetY");
            }
        }

        [Test]
        public void 실제_JSON_이펙트_개수는_2_3등급_0_4등급_기둥2_5등급_기둥4_후광1이다()
        {
            GradeFxDatabase db = BuildReal();

            Assert.AreEqual(0, db.SpecOf(EMonster.Wisp, 2).Fx.Count);
            Assert.AreEqual(0, db.SpecOf(EMonster.Wisp, 3).Fx.Count);

            GradeFxSpec elite = db.SpecOf(EMonster.Wisp, 4);
            Assert.AreEqual(2, elite.Fx.Count);
            Assert.AreEqual(-elite.Fx[1].offsetX, elite.Fx[0].offsetX, 0.00001f, "좌우 대칭");

            GradeFxSpec lord = db.SpecOf(EMonster.Wisp, 5);
            Assert.AreEqual(5, lord.Fx.Count);
            int tornado = 0;
            int sunburst = 0;
            foreach (Grade_FxRow row in lord.Fx)
            {
                if (row.kind == GradeFxDatabase.KindTornado)
                {
                    tornado++;
                }
                else if (row.kind == GradeFxDatabase.KindSunburst)
                {
                    sunburst++;
                }
            }
            Assert.AreEqual(4, tornado);
            Assert.AreEqual(1, sunburst);
        }

        [Test]
        public void 실제_JSON_맥동은_군주_한_등급에만_있고_값이_기획서와_같다()
        {
            GradeFxDatabase db = BuildReal();

            for (int grade = 1; grade <= 4; grade++)
            {
                Assert.IsNull(db.SpecOf(EMonster.Wisp, grade).Pulse, $"{grade}등급은 맥동 없음");
            }
            Grade_PulseRow pulse = db.SpecOf(EMonster.Wisp, 5).Pulse;
            Assert.IsNotNull(pulse);
            Assert.AreEqual(1.15f, pulse.brightnessMin, 0.00001f);
            Assert.AreEqual(1.45f, pulse.brightnessMax, 0.00001f);
            Assert.AreEqual(1.5f, pulse.saturationMin, 0.00001f);
            Assert.AreEqual(1.8f, pulse.saturationMax, 0.00001f);
            Assert.AreEqual(1.2f, pulse.periodSec, 0.00001f);
        }

        [TestCase(EMonster.Wisp, "3FCF86")]
        [TestCase(EMonster.Wraith, "CFD4E8")]
        [TestCase(EMonster.Reaper, "FF5A4A")]
        [TestCase(EMonster.Hex, "FFC531")]
        [TestCase(EMonster.Plague, "C765FF")]
        [TestCase(EMonster.Phantom, "4FB0FF")]
        public void 실제_JSON_종족_이펙트_색은_시안_값이다(EMonster monster, string expectedHex)
        {
            GradeFxDatabase db = BuildReal();

            Assert.AreEqual(expectedHex, ColorUtility.ToHtmlStringRGB(db.SpecOf(monster, 5).GlowColor));
        }
    }
}
