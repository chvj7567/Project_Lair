using System.Reflection;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 도감 셀 View 회귀 — CodexCell.Bind 후 _icon.color 를 (Unlocked / Icon유무 / 레벨) 케이스별로 단언.
    //# 리팩터가 재구성한 seam(base 색 계산 → EnhanceLevelVisual.Apply 라우팅)을 정적 추론이 아닌 실행으로 박제.
    //#  - 미해금 → SilhouetteColor(실루엣) / 해금+실아이콘 → white / 해금+색칩 lv0 → TintColor / 해금+색칩 lv>0 → Lerp(white,glow,tint[lv])
    //# 데이터 계약(TintColor 값·Unlocked·배열)은 CodexMonsterEnhance*Tests 담당 — 본 스위트는 View 픽셀 라우팅만.
    public class CodexCellIconColorTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        //# CodexCell + _icon(Image) 주입. 나머지 위젯(_glowOverlay/_levelBadge/_iconRect/_nameText/_background)은
        //# Apply·Bind 내부 null 가드로 안전 — 색 라우팅 검증에 _icon 하나면 충분(advisor 확인).
        private CodexCell MakeCellWithIcon(out Image icon)
        {
            _root = new GameObject("CodexCellIconColorTest");
            CodexCell cell = _root.AddComponent<CodexCell>();

            GameObject iconGo = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer));
            iconGo.transform.SetParent(_root.transform, false);
            icon = iconGo.AddComponent<Image>();

            FieldInfo field = typeof(CodexCell).GetField("_icon", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "_icon 필드 존재(리네임 시 이 테스트가 붉게 알림)");
            field.SetValue(cell, icon);
            return cell;
        }

        //# CodexCell.SilhouetteColor(private static) 를 반사로 취득 — 상수값을 하드코딩하지 않고 라우팅만 검증.
        private static Color SilhouetteColor()
        {
            FieldInfo f = typeof(CodexCell).GetField("SilhouetteColor", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(f, "SilhouetteColor 상수 존재");
            return (Color)f.GetValue(null);
        }

        private static Sprite MakeSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        }

        private static void AssertColorApprox(Color expected, Color actual, string label)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-3f, $"{label} R");
            Assert.AreEqual(expected.g, actual.g, 1e-3f, $"{label} G");
            Assert.AreEqual(expected.b, actual.b, 1e-3f, $"{label} B");
            Assert.AreEqual(expected.a, actual.a, 1e-3f, $"{label} A");
        }

        //# 케이스 1 — 미해금(unseen): enhanced=false → lv0 → unlit → base = SilhouetteColor(검정 실루엣, §6).
        [Test]
        public void 미해금_셀은_아이콘색이_실루엣색이다()
        {
            Image icon;
            CodexCell cell = MakeCellWithIcon(out icon);

            cell.Bind(new CodexCellData
            {
                DisplayName = "???",
                Unlocked = false,
                Species = EMonster.Reaper,
                EnhanceLevel = 4,   //# 미해금이면 레벨이 있어도 강화 무시(enhanced=Unlocked&&Species) → 실루엣 우선
                TintColor = SpawnerStatusCell.SpeciesColor(EMonster.Reaper),
            });

            AssertColorApprox(SilhouetteColor(), icon.color, "미해금 실루엣");
        }

        //# 케이스 2 — 해금 + 실제 Icon(스프라이트): lv0 → unlit → base = white(도감 원본 일러스트 원색, §6).
        [Test]
        public void 해금_실아이콘_셀은_아이콘색이_흰색이다()
        {
            Image icon;
            CodexCell cell = MakeCellWithIcon(out icon);

            cell.Bind(new CodexCellData
            {
                DisplayName = "도깨비불",
                Unlocked = true,
                Species = EMonster.Wisp,
                EnhanceLevel = 0,
                Icon = MakeSprite(),
                TintColor = SpawnerStatusCell.SpeciesColor(EMonster.Wisp),
            });

            AssertColorApprox(Color.white, icon.color, "해금 실아이콘 원색");
        }

        //# 케이스 3 — 해금 + 색칩(Icon 없음) lv0 → unlit → base = TintColor(종색 색칩, §6).
        //# base→TintColor 라우팅을 실제로 타는 유일한 케이스(lit 분기는 base 를 버림 — advisor).
        [Test]
        public void 해금_색칩_lv0_셀은_아이콘색이_종색이다()
        {
            Image icon;
            CodexCell cell = MakeCellWithIcon(out icon);

            Color tint = SpawnerStatusCell.SpeciesColor(EMonster.Hex);
            cell.Bind(new CodexCellData
            {
                DisplayName = "저주술사",
                Unlocked = true,
                Species = EMonster.Hex,
                EnhanceLevel = 0,
                Icon = null,   //# 색칩 fallback
                TintColor = tint,
            });

            AssertColorApprox(tint, icon.color, "해금 색칩 lv0 종색");
        }

        //# 케이스 4 — 해금 + 색칩 lv>0 → lit → 틴트가 Lerp(white, glow, tint[lv]) 로 이동(base 는 버려짐).
        //# 틴트 base 는 항상 흰색(도감 원본 동작 불변, §11) — 색칩 종색으로 Lerp 하지 않음(Design A 락).
        [Test]
        public void 해금_색칩_lv3_셀은_아이콘색이_white기준_glow_Lerp다()
        {
            Image icon;
            CodexCell cell = MakeCellWithIcon(out icon);

            const int lv = 3;
            cell.Bind(new CodexCellData
            {
                DisplayName = "저주술사",
                Unlocked = true,
                Species = EMonster.Hex,
                EnhanceLevel = lv,
                Icon = null,
                TintColor = SpawnerStatusCell.SpeciesColor(EMonster.Hex),   //# base 는 버려짐 — 이 값이 안 나와야 정상
            });

            Color glow = SpeciesVisual.SpeciesGlowColor(EMonster.Hex);
            Color expected = Color.Lerp(Color.white, glow, EnhanceLevelVisual.IconTintByLevel[lv]);
            AssertColorApprox(expected, icon.color, "해금 색칩 lv3 Lerp(white,glow,tint[3])");
        }
    }
}
