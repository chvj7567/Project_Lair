using System.Collections;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode.Character
{
    //# 강화 레벨 → 발광 세기 적용 + 풀 재사용 리셋 검증 (monster-species-enhancement §4.3).
    //# 색은 SpeciesGlowColor(species) SoT 에서 취득 — ApplyLevel(int, EMonster) 시그니처(기획서 delta).
    public class MonsterEnhancementVisualPlayTests
    {
        private static MonsterEnhancementVisual NewVisual(out Renderer rd)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad); //# 테스트 전용 new (Rule 03 예외)
            rd = go.GetComponent<Renderer>();
            rd.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            MonsterEnhancementVisual v = go.AddComponent<MonsterEnhancementVisual>();
            v.SetRenderersForTest(new[] { rd });               //# 테스트 주입 API
            v.SetEmissionByLevelForTest(new[] { 1.5f, 1.9f, 2.3f, 2.7f, 3.2f });
            return v;
        }

        [UnityTest]
        public IEnumerator Lv0은_발광이_꺼진다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(0, EMonster.Wisp);
            yield return null;
            Assert.IsFalse(rd.material.IsKeywordEnabled("_EMISSION"));
        }

        [UnityTest]
        public IEnumerator Lv3은_해당_종족색의_발광이_적용된다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(3, EMonster.Wisp);
            yield return null;
            Assert.IsTrue(rd.material.IsKeywordEnabled("_EMISSION"));
            Color e = rd.material.GetColor("_EmissionColor");
            Assert.Greater(e.maxColorComponent, 0f);
            //# 종족색(초록) 색조 유지 — G 성분이 R 성분보다 크다.
            Assert.Greater(e.g, e.r);
        }

        //# 엣지 — 풀 재사용: Lv3 발광 후 OnDisable→OnEnable 재활성 시 발광이 off 로 리셋된다.
        [UnityTest]
        public IEnumerator 풀재사용_OnEnable에서_발광이_리셋된다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(3, EMonster.Wisp);
            yield return null;
            Assert.IsTrue(rd.material.IsKeywordEnabled("_EMISSION"));

            v.gameObject.SetActive(false);
            v.gameObject.SetActive(true);   //# OnEnable → ApplyLevel(0) 리셋
            yield return null;
            Assert.IsFalse(rd.material.IsKeywordEnabled("_EMISSION"));
        }
    }
}
