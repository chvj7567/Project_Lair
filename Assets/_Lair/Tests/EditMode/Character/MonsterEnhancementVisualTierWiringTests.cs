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
    }
}
