using System.Collections;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode.Character
{
    //# MonsterEnhancementVisual 발광 엣지 (monster-species-enhancement §4.1·§4.3) — 레벨별 세기 매핑·잘못된 레벨 가드·종족별 색 일치.
    //# 기존 MonsterEnhancementVisualPlayTests(Lv0 off / Lv3 on / 풀 리셋)와 비중복.
    //# 세기 절대값은 material 파이프 상수 스케일에 의존하므로 인접 레벨 비율(intensity 비)로 검증(상수 상쇄).
    public class MonsterEnhancementVisualEdgePlayTests
    {
        private static readonly float[] Intensity = { 1.5f, 1.9f, 2.3f, 2.7f, 3.2f };

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Material> _materials = new List<Material>();

        [TearDown]
        public void 정리()
        {
            foreach (Material m in _materials)
            {
                if (m != null)
                    Object.DestroyImmediate(m);
            }
            _materials.Clear();
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private MonsterEnhancementVisual NewVisual(out Renderer rd)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad); //# 테스트 전용 new (Rule 03 예외)
            _spawned.Add(go);
            rd = go.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _materials.Add(mat);
            rd.material = mat;
            MonsterEnhancementVisual v = go.AddComponent<MonsterEnhancementVisual>();
            v.SetRenderersForTest(new[] { rd });
            v.SetEmissionByLevelForTest(Intensity);
            return v;
        }

        //# 레벨 1~5 발광 세기가 _emissionByLevel 비율([1.5,1.9,2.3,2.7,3.2])을 따른다 — 인접 레벨 세기 비 == 주입 세기 비.
        [UnityTest]
        public IEnumerator 레벨별_발광세기가_주입_세기비율을_따른다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);

            float[] measured = new float[5];
            for (int level = 1; level <= 5; level++)
            {
                v.ApplyLevel(level, EMonster.Wisp);
                yield return null;
                measured[level - 1] = rd.material.GetColor("_EmissionColor").maxColorComponent;
                Assert.Greater(measured[level - 1], 0f, $"Lv{level} 발광 세기 > 0");
            }

            //# 비율로 검증 — material 상수 스케일이 있어도 상쇄된다.
            for (int i = 1; i < 5; i++)
            {
                float measuredRatio = measured[i] / measured[0];
                float expectedRatio = Intensity[i] / Intensity[0];
                Assert.AreEqual(expectedRatio, measuredRatio, 0.02f, $"Lv{i + 1}/Lv1 세기 비율");
            }
        }

        //# 세기가 단조 증가 — 레벨이 오를수록 발광이 밝아진다(정점감).
        [UnityTest]
        public IEnumerator 레벨이_오를수록_발광세기가_단조증가한다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);

            float prev = -1f;
            for (int level = 1; level <= 5; level++)
            {
                v.ApplyLevel(level, EMonster.Wisp);
                yield return null;
                float cur = rd.material.GetColor("_EmissionColor").maxColorComponent;
                Assert.Greater(cur, prev, $"Lv{level} > Lv{level - 1}");
                prev = cur;
            }
        }

        //# 잘못된 레벨 가드 — 음수는 발광 off.
        [UnityTest]
        public IEnumerator 음수_레벨은_발광이_꺼진다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(-1, EMonster.Wisp);
            yield return null;
            Assert.IsFalse(rd.material.IsKeywordEnabled("_EMISSION"));
        }

        //# 잘못된 레벨 가드 — 세기 배열 길이 초과 레벨은 발광 off(인덱스 초과 방지).
        [UnityTest]
        public IEnumerator 세기배열_길이_초과레벨은_발광이_꺼진다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(6, EMonster.Wisp);   //# 배열 길이 5 초과
            yield return null;
            Assert.IsFalse(rd.material.IsKeywordEnabled("_EMISSION"));
        }

        //# 경계 — 배열 마지막 레벨(길이와 동일한 5)은 발광 on.
        [UnityTest]
        public IEnumerator 마지막_레벨_5는_발광이_켜진다()
        {
            MonsterEnhancementVisual v = NewVisual(out Renderer rd);
            v.ApplyLevel(5, EMonster.Wisp);
            yield return null;
            Assert.IsTrue(rd.material.IsKeywordEnabled("_EMISSION"));
        }

        //# 종족 인자별 발광색이 SpeciesGlowColor(species) 방향과 일치 — "메뉴 색 = 전장 색" 구조 보장.
        [UnityTest]
        public IEnumerator 종족별_발광색이_SpeciesGlowColor_방향과_일치한다()
        {
            foreach (EMonster species in System.Enum.GetValues(typeof(EMonster)))
            {
                MonsterEnhancementVisual v = NewVisual(out Renderer rd);
                v.ApplyLevel(3, species);
                yield return null;

                Color e = rd.material.GetColor("_EmissionColor");
                Color glow = SpeciesVisual.SpeciesGlowColor(species);
                //# 방향 비교 — 각자의 최대성분으로 정규화 후 채널별 일치(세기 상수 상쇄).
                float em = e.maxColorComponent;
                float gm = glow.maxColorComponent;
                Assert.Greater(em, 0f, $"{species} 발광 세기 > 0");
                Assert.AreEqual(glow.r / gm, e.r / em, 0.02f, $"{species} R 방향");
                Assert.AreEqual(glow.g / gm, e.g / em, 0.02f, $"{species} G 방향");
                Assert.AreEqual(glow.b / gm, e.b / em, 0.02f, $"{species} B 방향");
            }
        }
    }
}
