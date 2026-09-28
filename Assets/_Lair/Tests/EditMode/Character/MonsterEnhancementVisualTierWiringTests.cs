using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Character
{
    //# ApplyLevel 이 티어 오버레이·바닥 문장·HP바 높이 세 채널을 함께 파생시킨다(monster-2d-conversion.md §6.3.1·§6.3.8).
    //# 시드: gameplay-programmer. "정상(Lv3) + 엣지(OnEnable 리셋)" 만 — 종별 전 레벨 매핑은 test-engineer.
    public class MonsterEnhancementVisualTierWiringTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private MonsterEnhancementVisual NewVisual(out MonsterTierOverlay overlay, out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar)
        {
            _root = new GameObject("MonsterRoot");
            _root.transform.localScale = Vector3.one;

            GameObject overlayGo = new GameObject("Enhance");
            overlayGo.transform.SetParent(_root.transform, false);
            overlayGo.AddComponent<SpriteRenderer>();
            overlay = overlayGo.AddComponent<MonsterTierOverlay>();

            GameObject sigilGo = new GameObject("AuraSigil");
            sigilGo.transform.SetParent(_root.transform, false);
            sigilGo.AddComponent<SpriteRenderer>();
            sigil = sigilGo.AddComponent<MonsterEnhanceSigil>();

            GameObject hpBarGo = new GameObject("HpBarWrapper");
            hpBarGo.transform.SetParent(_root.transform, false);
            hpBar = hpBarGo.AddComponent<MonsterHpBar>();

            MonsterEnhancementVisual visual = _root.AddComponent<MonsterEnhancementVisual>();
            visual.SetEmissionByLevelForTest(new[] { 1.5f, 1.9f, 2.3f, 2.7f, 3.2f });
            visual.SetTierOverlayForTest(overlay);
            visual.SetSigilForTest(sigil);
            visual.SetHpBarForTest(hpBar);
            visual.SetHpBarHeightByTierForTest(new[] { 0.747f, 0.809f, 0.872f, 0.934f });
            return visual;
        }

        [Test]
        public void Lv3은_티어2_문장1단계_해당_HP바높이를_적용한다()
        {
            MonsterEnhancementVisual visual = NewVisual(out MonsterTierOverlay overlay, out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar);

            visual.ApplyLevel(3, EMonster.Wisp);

            Assert.AreEqual(2, overlay.CurrentTierForTest, "Lv3 → 티어[0,1,1,2,2,3][3]=2");
            Assert.AreEqual(1, sigil.StageForTest, "Lv3 → 문장[0,0,1,1,2,3][3]=1");
            Assert.AreEqual(0.872f, hpBar.transform.localPosition.y, 0.0001f, "티어2 HP바 높이(루트 스케일 1)");
        }

        //# 엣지 — 풀 재사용(OnEnable 재호출) 시 세 채널 모두 레벨 0(티어0·문장0·HP바 티어0 높이)으로 리셋된다.
        [Test]
        public void 풀재사용_OnEnable에서_세채널_모두_레벨0으로_리셋된다()
        {
            MonsterEnhancementVisual visual = NewVisual(out MonsterTierOverlay overlay, out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar);
            visual.ApplyLevel(5, EMonster.Wisp);
            Assert.AreEqual(3, overlay.CurrentTierForTest);

            visual.gameObject.SetActive(false);
            visual.gameObject.SetActive(true);   //# OnEnable → 레벨0 리셋

            Assert.AreEqual(0, overlay.CurrentTierForTest, "리셋 후 티어 0");
            Assert.AreEqual(0, sigil.StageForTest, "리셋 후 문장 숨김");
            Assert.AreEqual(0.747f, hpBar.transform.localPosition.y, 0.0001f, "리셋 후 티어0 HP바 높이");
        }

        //# ───────── 이하 test-engineer 보강분 — ② 4채널(발광 포함) 동시 적용·리셋 + 레벨 전 구간 매핑 + 클램프/null 안전 ─────────

        //# 기존 NewVisual(3채널) 에 실제 Renderer+Material 을 더해 발광까지 포함한 4채널 조립을 만든다.
        private MonsterEnhancementVisual NewVisualWithRenderer(out Renderer rd, out MonsterTierOverlay overlay,
            out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar, out Material mat)
        {
            MonsterEnhancementVisual visual = NewVisual(out overlay, out sigil, out hpBar);

            GameObject bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(_root.transform, false);
            rd = bodyGo.AddComponent<MeshRenderer>();
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rd.material = mat;
            visual.SetRenderersForTest(new[] { rd });

            return visual;
        }

        [Test]
        public void Lv5은_발광_티어_문장_HP바_네채널을_동시에_적용한다()
        {
            MonsterEnhancementVisual visual = NewVisualWithRenderer(out Renderer rd, out MonsterTierOverlay overlay,
                out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar, out Material mat);

            visual.ApplyLevel(5, EMonster.Wisp);

            Assert.IsTrue(mat.IsKeywordEnabled("_EMISSION"), "Lv5 발광 on");
            Assert.Greater(mat.GetColor("_EmissionColor").maxColorComponent, 0f, "Lv5 발광 세기 > 0");
            Assert.AreEqual(3, overlay.CurrentTierForTest, "Lv5 → 티어[0,1,1,2,2,3][5]=3");
            Assert.AreEqual(3, sigil.StageForTest, "Lv5 → 문장[0,0,1,1,2,3][5]=3(링2+파편)");
            Assert.AreEqual(0.934f, hpBar.transform.localPosition.y, 0.0001f, "Lv5 → 티어3 HP바 높이");

            Object.DestroyImmediate(mat);
        }

        //# ② 핵심 — 풀 재사용 시 발광·티어·문장·HP바 4채널이 "전부 동시에" 레벨0 으로 리셋된다.
        [Test]
        public void 풀재사용시_발광채널도_티어_문장_HP바와_함께_동시에_리셋된다()
        {
            MonsterEnhancementVisual visual = NewVisualWithRenderer(out Renderer rd, out MonsterTierOverlay overlay,
                out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar, out Material mat);
            visual.ApplyLevel(5, EMonster.Wisp);
            Assert.IsTrue(mat.IsKeywordEnabled("_EMISSION"), "리셋 전 발광 on 확인");

            visual.gameObject.SetActive(false);
            visual.gameObject.SetActive(true);   //# OnEnable → 4채널 모두 레벨0 리셋

            Assert.IsFalse(mat.IsKeywordEnabled("_EMISSION"), "리셋 후 발광 off");
            Assert.AreEqual(0, overlay.CurrentTierForTest, "리셋 후 티어 0");
            Assert.AreEqual(0, sigil.StageForTest, "리셋 후 문장 숨김");
            Assert.AreEqual(0.747f, hpBar.transform.localPosition.y, 0.0001f, "리셋 후 티어0 HP바 높이");

            Object.DestroyImmediate(mat);
        }

        //# 레벨 0~5 전 구간이 기획서 §6.3.1 표(티어[0,1,1,2,2,3]·문장[0,0,1,1,2,3])와 일치하는지 데이터 주도로 검증.
        [TestCase(0, 0, 0)]
        [TestCase(1, 1, 0)]
        [TestCase(2, 1, 1)]
        [TestCase(3, 2, 1)]
        [TestCase(4, 2, 2)]
        [TestCase(5, 3, 3)]
        public void 레벨별_티어와_문장_매핑이_기획서_표와_일치한다(int level, int expectedTier, int expectedSigilStage)
        {
            MonsterEnhancementVisual visual = NewVisual(out MonsterTierOverlay overlay, out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar);

            visual.ApplyLevel(level, EMonster.Wraith);

            Assert.AreEqual(expectedTier, overlay.CurrentTierForTest, $"Lv{level} → 티어");
            Assert.AreEqual(expectedSigilStage, sigil.StageForTest, $"Lv{level} → 문장단계");
        }

        //# 엣지 — 유효 범위(0~5)를 넘는 레벨이 들어와도 티어/문장은 Lv5 값으로 클램프되어 안전하다(방어적 가드).
        [Test]
        public void 레벨이_최대치를_초과해도_티어와_문장은_Lv5값으로_클램프된다()
        {
            MonsterEnhancementVisual visual = NewVisual(out MonsterTierOverlay overlay, out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar);

            visual.ApplyLevel(99, EMonster.Wisp);

            Assert.AreEqual(3, overlay.CurrentTierForTest, "레벨 상한 클램프 → 티어3(Lv5 취급)");
            Assert.AreEqual(3, sigil.StageForTest, "레벨 상한 클램프 → 문장3(Lv5 취급)");
        }

        //# 엣지 — 음수 레벨은 티어/문장이 0으로 클램프된다.
        [Test]
        public void 음수_레벨은_티어와_문장이_0으로_클램프된다()
        {
            MonsterEnhancementVisual visual = NewVisual(out MonsterTierOverlay overlay, out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar);

            visual.ApplyLevel(-5, EMonster.Wisp);

            Assert.AreEqual(0, overlay.CurrentTierForTest, "음수 레벨 → 티어0 으로 클램프");
            Assert.AreEqual(0, sigil.StageForTest, "음수 레벨 → 문장0 으로 클램프");
        }

        //# 엣지 — 하위 컴포넌트(티어 오버레이·문장·HP바)가 전부 미배선(2D 전환 전 구형 프리팹 호환)이어도 예외 없이 동작.
        [Test]
        public void 하위컴포넌트가_모두_null이어도_ApplyLevel이_예외없이_동작한다()
        {
            GameObject go = new GameObject("MonsterRootOnly");
            MonsterEnhancementVisual visual = go.AddComponent<MonsterEnhancementVisual>();
            visual.SetEmissionByLevelForTest(new[] { 1.5f, 1.9f, 2.3f, 2.7f, 3.2f });

            Assert.DoesNotThrow(() => visual.ApplyLevel(5, EMonster.Wisp));

            Object.DestroyImmediate(go);
        }

        //# 엣지 — HP바 높이 배열이 조회하려는 티어 인덱스보다 짧으면 예외 없이 무시(이전 값 유지).
        [Test]
        public void HP바높이배열이_티어_인덱스보다_짧으면_예외없이_무시된다()
        {
            MonsterEnhancementVisual visual = NewVisual(out MonsterTierOverlay overlay, out MonsterEnhanceSigil sigil, out MonsterHpBar hpBar);
            visual.SetHpBarHeightByTierForTest(new[] { 0.5f });   //# 인덱스 0(티어0)만 존재
            hpBar.SetHeightAboveRoot(0.5f);                        //# 기준값 세팅

            Assert.DoesNotThrow(() => visual.ApplyLevel(5, EMonster.Wisp), "티어3 조회가 배열 길이를 넘어도 예외 없음");
            Assert.AreEqual(0.5f, hpBar.transform.localPosition.y, 0.0001f, "배열 범위 밖이면 HP바 높이는 이전 값 그대로 유지");
        }
    }
}
