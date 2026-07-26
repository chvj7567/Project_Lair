using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 상태 셀 강화 표현 — 셀(MonoBehaviour+MetaSession)을 직접 단위테스트하는 대신,
    //# 셀이 호출하는 공유 계약 EnhanceLevelVisual.Apply + 레벨 조회 키 형식을 검증 (plan Task 2 Step 1).
    //# 실제 픽셀/발광 렌더는 프리팹·육안(Task 3 목업 게이트) 소관.
    public class SpawnerCellEnhanceLevelTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("EnhanceVisualTest");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        private Image MakeImage(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<Image>();
        }

        private CHText MakeBadge()
        {
            GameObject go = new GameObject("badge");
            go.transform.SetParent(_root.transform, false);
            //# CHText 는 RequireComponent(TMP_Text) — TMP 먼저 부착.
            go.AddComponent<TextMeshProUGUI>();
            return go.AddComponent<CHText>();
        }

        private static void AssertColorApprox(Color expected, Color actual, string label)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-3f, $"{label} R");
            Assert.AreEqual(expected.g, actual.g, 1e-3f, $"{label} G");
            Assert.AreEqual(expected.b, actual.b, 1e-3f, $"{label} B");
            Assert.AreEqual(expected.a, actual.a, 1e-3f, $"{label} A");
        }

        //# 정상 — Lv0(미강화): 발광/배지 off·스케일 1·아이콘은 쉬는 색(baseIconColor) 그대로.
        [Test]
        public void Lv0은_발광배지off_스케일1_아이콘은_baseIconColor다()
        {
            Image icon = MakeImage("icon");
            Image glow = MakeImage("glow");
            CHText badge = MakeBadge();
            RectTransform iconRect = icon.rectTransform;
            Color baseColor = new Color(0.9f, 0.1f, 0.2f, 1f);   //# 흰색 아닌 뚜렷한 색

            EnhanceLevelVisual.Apply(0, EMonster.Wisp, icon, glow, badge, iconRect, baseColor);

            Assert.IsFalse(glow.gameObject.activeSelf, "Lv0 발광 오버레이 off");
            Assert.IsFalse(badge.gameObject.activeSelf, "Lv0 배지 off");
            Assert.AreEqual(1f, iconRect.localScale.x, 1e-4f, "Lv0 스케일 항등");
            AssertColorApprox(baseColor, icon.color, "Lv0 아이콘");
        }

        //# 정상 — Lv5(만렙): 발광 α0.90(종족색 RGB)·배지 "Lv 5"·스케일 1.10.
        //# 아이콘 틴트 base 는 항상 흰색(도감 원본 동작 불변, 기획서 §11) — baseIconColor 를 흰색 아닌 색으로 줘도 Lerp(white,glow,0.64).
        [Test]
        public void Lv5는_발광α0_90_배지Lv5_스케일1_10_틴트는_white기준Lerp다()
        {
            Image icon = MakeImage("icon");
            Image glow = MakeImage("glow");
            CHText badge = MakeBadge();
            RectTransform iconRect = icon.rectTransform;
            Color baseColor = new Color(0.1f, 0.2f, 0.3f, 1f);   //# 흰색 아님 — Design A 락(base 로 틴트하면 실패)

            EnhanceLevelVisual.Apply(5, EMonster.Wisp, icon, glow, badge, iconRect, baseColor);

            Assert.IsTrue(glow.gameObject.activeSelf, "Lv5 발광 오버레이 on");
            Color speciesGlow = SpeciesVisual.SpeciesGlowColor(EMonster.Wisp);
            Assert.AreEqual(speciesGlow.r, glow.color.r, 1e-3f, "발광 RGB=종족색 R");
            Assert.AreEqual(speciesGlow.g, glow.color.g, 1e-3f, "발광 RGB=종족색 G");
            Assert.AreEqual(speciesGlow.b, glow.color.b, 1e-3f, "발광 RGB=종족색 B");
            Assert.AreEqual(0.90f, glow.color.a, 1e-3f, "Lv5 발광 α=0.90");

            Assert.IsTrue(badge.gameObject.activeSelf, "Lv5 배지 on");
            Assert.AreEqual(1.10f, iconRect.localScale.x, 1e-4f, "Lv5 스케일 1.10");

            Color expectedTint = Color.Lerp(Color.white, speciesGlow, 0.64f);
            AssertColorApprox(expectedTint, icon.color, "Lv5 틴트 Lerp(white,glow,0.64)");
        }

        //# 엣지 — 변조 레벨: 만렙 초과(99)는 Clamp→5, 음수(-3)는 Clamp→0(off). 인덱싱 throw 없음.
        [Test]
        public void 변조레벨은_Clamp되어_인덱싱_throw없이_상한하한을_지킨다()
        {
            Image icon = MakeImage("icon");
            Image glow = MakeImage("glow");
            RectTransform iconRect = icon.rectTransform;

            Assert.DoesNotThrow(() =>
                EnhanceLevelVisual.Apply(99, EMonster.Reaper, icon, glow, null, iconRect, Color.white));
            Assert.AreEqual(1.10f, iconRect.localScale.x, 1e-4f, "99 → Clamp→5 상한 스케일");
            Assert.IsTrue(glow.gameObject.activeSelf, "99 → lit");

            Assert.DoesNotThrow(() =>
                EnhanceLevelVisual.Apply(-3, EMonster.Reaper, icon, glow, null, iconRect, Color.white));
            Assert.AreEqual(1f, iconRect.localScale.x, 1e-4f, "-3 → Clamp→0 항등 스케일");
            Assert.IsFalse(glow.gameObject.activeSelf, "-3 → off");
        }

        //# 위젯 null 가드 — 부분 배선(프리팹 미배선)에서도 NRE 없이 배선된 것만 적용.
        [Test]
        public void 위젯이_null이면_NRE없이_배선된것만_적용된다()
        {
            RectTransform iconRect = MakeImage("iconOnly").rectTransform;

            Assert.DoesNotThrow(() =>
                EnhanceLevelVisual.Apply(3, EMonster.Hex, null, null, null, iconRect, Color.white));
            Assert.AreEqual(EnhanceLevelVisual.ScaleByLevel[3], iconRect.localScale.x, 1e-4f, "iconRect 만 적용");
        }

        //# 계약 — 상태 셀·도감이 공유하는 레벨 조회 키 형식 "Enhance_"+EMonster (SoT 정합, drift 시 붉게).
        [Test]
        public void 레벨조회_키는_Enhance_접두어와_종족이름이다()
        {
            Assert.AreEqual("Enhance_Wisp", "Enhance_" + EMonster.Wisp);
            Assert.AreEqual("Enhance_Phantom", "Enhance_" + EMonster.Phantom);
        }
    }
}
