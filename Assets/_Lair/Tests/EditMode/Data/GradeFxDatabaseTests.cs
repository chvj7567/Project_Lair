using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# 등급 연출 데이터 검증·조회(monster-grades §8) — 시드: gameplay-programmer. 정상(5등급 사양 합성) + 엣지(Fx 복합키 중복 거부).
    public class GradeFxDatabaseTests
    {
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

        [Test]
        public void 군주_사양은_크기_후광_오라_맥동_이펙트를_모두_합성한다()
        {
            GradeFxDatabase db = GradeFxDatabase.Build(
                Grades(),
                new[] { new Grade_GlowRow { grade = 5, widthCell = 0.075f } },
                new[] { new Grade_AuraRow { grade = 5, opacity = 1f, scale = 2f, offsetY = 0.075f } },
                new[] { new Grade_PulseRow { grade = 5, brightnessMin = 1.15f, brightnessMax = 1.45f, saturationMin = 1.5f, saturationMax = 1.8f, periodSec = 1.2f } },
                new[]
                {
                    new Grade_FxRow { grade = 5, index = 1, kind = "Sunburst", scale = 2.4f, opacity = 0.85f },
                    new Grade_FxRow { grade = 5, index = 0, kind = "Tornado", scale = 1f, opacity = 0.95f },
                },
                Colors());

            GradeFxSpec spec = db.SpecOf(EMonster.Wisp, 5);

            Assert.AreEqual(1.3f, spec.SizeMul, 0.0001f);
            Assert.IsTrue(spec.HasGlow && spec.HasAura && spec.Pulse != null);
            Assert.AreEqual(2, spec.Fx.Count);
            Assert.AreEqual(0, spec.Fx[0].index, "index 순 정렬");
            GradeFxSpec rookie = db.SpecOf(EMonster.Wisp, 1);
            Assert.IsFalse(rookie.HasGlow || rookie.HasAura || rookie.Pulse != null || rookie.Fx.Count > 0, "1등급은 연출 없음");
        }

        [Test]
        public void Fx_복합키가_중복되면_검증_오류다()
        {
            Assert.Throws<ChapterDataException>(() => GradeFxDatabase.Build(
                Grades(), null, null, null,
                new[]
                {
                    new Grade_FxRow { grade = 4, index = 0, kind = "Tornado", scale = 1f },
                    new Grade_FxRow { grade = 4, index = 0, kind = "Tornado", scale = 1f },
                },
                Colors()));
        }
    }
}
