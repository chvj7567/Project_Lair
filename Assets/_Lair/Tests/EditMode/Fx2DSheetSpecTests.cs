using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Card;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    //# FX2D_SheetSpec.json ↔ 6개 FX 프리팹 SpriteSheetFx 설정 정합 + 이번 변경으로 고정된 값(기획서 fx-2d-conversion §10).
    public class Fx2DSheetSpecTests
    {
        private const string SpecPath = "Assets/_Lair/Art/Sprites/FX2D/FX2D_SheetSpec.json";
        private const string FxFolder = "Assets/_Lair/Art/FX";
        private const string MatFx2D = "Assets/_Lair/Art/Materials/Mat_FX2D.mat";
        private const string MatMonster2D = "Assets/_Lair/Art/Materials/Mat_Monster2D.mat";
        private const string Shader2D = "Assets/_Lair/Art/Shaders/Monster2DSprite.shader";
        private const string OrbitSkillAsset = "Assets/_Lair/Art/Skills/HeroSkill_OrbitingBlade.asset";
        private const string DashSkillAsset = "Assets/_Lair/Art/Skills/HeroSkill_DashStrike.asset";

        private static readonly string[] SheetFxNames =
        {
            "PoisonAura", "TimeStopShield", "FearSkull", "HeroDashConeFx", "HeroNovaFx", "HeroOrbitBladeFx",
        };

        private static JObject LoadFx(string name)
        {
            Assert.IsTrue(File.Exists(SpecPath), $"스펙 JSON 누락: {SpecPath}");
            JObject root = JObject.Parse(File.ReadAllText(SpecPath));
            JToken fx = root["fx"]?[name];
            Assert.IsNotNull(fx, $"스펙 JSON 에 FX 항목 없음: {name}");
            return (JObject)fx;
        }

        private static SerializedObject LoadSpriteSheetFx(string name)
        {
            string path = $"{FxFolder}/{name}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"FX 프리팹 누락: {path}");
            SpriteSheetFx comp = prefab.GetComponentInChildren<SpriteSheetFx>(true);
            Assert.IsNotNull(comp, $"프리팹에 SpriteSheetFx 컴포넌트 없음: {path}");
            return new SerializedObject(comp);
        }

        [Test]
        public void 스펙_fps는_12이다()
        {
            Assert.IsTrue(File.Exists(SpecPath), $"스펙 JSON 누락: {SpecPath}");
            JObject root = JObject.Parse(File.ReadAllText(SpecPath));
            Assert.AreEqual(12, (int)root["fps"]);
        }

        [TestCaseSource(nameof(SheetFxNames))]
        public void 프리팹_프레임수가_스펙과_일치한다(string name)
        {
            JObject spec = LoadFx(name);
            SerializedObject so = LoadSpriteSheetFx(name);
            Assert.AreEqual((int)spec["frames"], so.FindProperty("_frames").arraySize, $"{name} 프레임 수 불일치");
        }

        [TestCaseSource(nameof(SheetFxNames))]
        public void 프리팹_프레임_스프라이트가_모두_배선되어있다(string name)
        {
            SerializedObject so = LoadSpriteSheetFx(name);
            SerializedProperty frames = so.FindProperty("_frames");
            for (int i = 0; i < frames.arraySize; ++i)
            {
                Assert.IsNotNull(frames.GetArrayElementAtIndex(i).objectReferenceValue, $"{name} _frames[{i}] 스프라이트 미배선");
            }
        }

        [TestCaseSource(nameof(SheetFxNames))]
        public void 프리팹_loop가_스펙과_일치한다(string name)
        {
            JObject spec = LoadFx(name);
            SerializedObject so = LoadSpriteSheetFx(name);
            Assert.AreEqual((bool)spec["loop"], so.FindProperty("_loop").boolValue, $"{name} loop 불일치");
        }

        [TestCaseSource(nameof(SheetFxNames))]
        public void 프리팹_fps는_12이다(string name)
        {
            SerializedObject so = LoadSpriteSheetFx(name);
            Assert.AreEqual(12f, so.FindProperty("_fps").floatValue, 1e-6f, $"{name} fps");
        }

        [TestCaseSource(nameof(SheetFxNames))]
        public void 루프_FX는_자동풀반환을_켜지않고_1회_FX는_켠다(string name)
        {
            SerializedObject so = LoadSpriteSheetFx(name);
            bool loop = so.FindProperty("_loop").boolValue;
            bool ret = so.FindProperty("_returnToPoolOnFinish").boolValue;
            Assert.AreEqual(loop == false, ret, $"{name} loop={loop} returnToPool={ret}");
        }

        [TestCaseSource(nameof(SheetFxNames))]
        public void 프리팹에_ReturnToPoolAfter가_없다_이중Push_방지(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FxFolder}/{name}.prefab");
            Assert.IsNotNull(prefab, $"FX 프리팹 누락: {name}");
            Assert.IsNull(prefab.GetComponentInChildren<ReturnToPoolAfter>(true), $"{name} 에 ReturnToPoolAfter 잔존");
        }

        [TestCase("HeroDashConeFx", 0.833f)]
        [TestCase("HeroNovaFx", 0.833f)]
        [TestCase("FearSkull", 1.167f)]
        public void 일회성_FX_수명은_프레임수_나누기_12이다(string name, float expected)
        {
            SerializedObject so = LoadSpriteSheetFx(name);
            float life = so.FindProperty("_frames").arraySize / so.FindProperty("_fps").floatValue;
            Assert.AreEqual(expected, life, 0.001f);
        }

        [Test]
        public void 궤도_블레이드_SO는_3개로_고정이다()
        {
            OrbitingBladeSkillData so = AssetDatabase.LoadAssetAtPath<OrbitingBladeSkillData>(OrbitSkillAsset);
            Assert.IsNotNull(so, $"에셋 누락: {OrbitSkillAsset}");
            Assert.AreEqual(3, so.BladeCount, "시트는 블레이드 3개 고정 — 시각·판정 개수 정합");
        }

        [Test]
        public void 대시_판정은_전방위_반각180이고_FX는_반경3_원형_시트이다()
        {
            DashStrikeSkillData so = AssetDatabase.LoadAssetAtPath<DashStrikeSkillData>(DashSkillAsset);
            Assert.IsNotNull(so, $"에셋 누락: {DashSkillAsset}");
            Assert.AreEqual(180f, so.ConeHalfAngle, 1e-4f, "돌진 반각이 바뀜 — FX 시트·크기 재검토 필요");
            Assert.AreEqual(3f, so.DashLength, 1e-4f, "돌진 반경이 바뀜 — FX 크기(루트 스케일 3, PPU 52) 재검토 필요");
        }

        [Test]
        public void 대시_시트_스펙은_원형_규격이다()
        {
            JObject spec = LoadFx("HeroDashConeFx");
            Assert.AreEqual(10, (int)spec["frames"]);
            Assert.AreEqual(6, (int)spec["cols"]);
            Assert.AreEqual(2, (int)spec["rows"]);
            Assert.AreEqual(false, (bool)spec["loop"]);
            Assert.AreEqual(64, (int)spec["foot"][0], "foot.x");
            Assert.AreEqual(62, (int)spec["foot"][1], "foot.y");
        }

        [Test]
        public void 대시_프리팹_AuraFx는_노바와_같이_바닥에_눕고_스케일y2_PPU52이다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FxFolder}/HeroDashConeFx.prefab");
            Assert.IsNotNull(prefab, "HeroDashConeFx 프리팹 누락");
            Transform aura = prefab.transform.Find("AuraFx");
            Assert.IsNotNull(aura, "AuraFx 자식 없음");
            Assert.Less(Quaternion.Angle(aura.localRotation, Quaternion.Euler(90f, 0f, 0f)), 0.1f, "AuraFx 회전은 X축 90도여야 한다");
            Assert.AreEqual(1f, Mathf.Abs(aura.forward.y), 1e-3f, "AuraFx forward 가 월드 ±Y 가 아님(눕지 않음)");
            Assert.AreEqual(2f, aura.localScale.y, 1e-4f, "AuraFx 로컬 스케일 y");

            GameObject nova = AssetDatabase.LoadAssetAtPath<GameObject>($"{FxFolder}/HeroNovaFx.prefab");
            Assert.IsNotNull(nova, "HeroNovaFx 프리팹 누락");
            Transform novaAura = nova.transform.Find("AuraFx");
            Assert.IsNotNull(novaAura, "노바 AuraFx 없음");
            Assert.Less(Quaternion.Angle(aura.localRotation, novaAura.localRotation), 0.1f, "노바와 회전 불일치");

            TextureImporter imp = AssetImporter.GetAtPath("Assets/_Lair/Art/Sprites/FX2D/HeroDashConeFx_Sheet.png") as TextureImporter;
            Assert.IsNotNull(imp, "돌진 시트 임포터 없음");
            Assert.AreEqual(52f, imp.spritePixelsPerUnit, 1e-4f, "돌진 시트 PPU");
        }

        [Test]
        public void FearEffect_FxLiftY는_0이다()
        {
            FieldInfo f = typeof(FearEffect).GetField("FxLiftY", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(f, "FearEffect.FxLiftY 상수 없음(리네임/제거?)");
            Assert.AreEqual(0f, (float)f.GetRawConstantValue(), 1e-6f);
        }

        [Test]
        public void Mat_FX2D_ZTest는_Always_8이다()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatFx2D);
            Assert.IsNotNull(mat, $"머티리얼 누락: {MatFx2D}");
            Assert.IsTrue(mat.HasProperty("_ZTest"), "Mat_FX2D 셰이더에 _ZTest 없음");
            Assert.AreEqual(8f, mat.GetFloat("_ZTest"), 1e-6f);
        }

        [Test]
        public void Monster2DSprite_셰이더에_ZTest_프로퍼티가_있고_기본은_LEqual_4이다()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Shader2D);
            Assert.IsNotNull(shader, $"셰이더 누락: {Shader2D}");
            int idx = shader.FindPropertyIndex("_ZTest");
            Assert.GreaterOrEqual(idx, 0, "셰이더에 _ZTest 프로퍼티 없음");
            Assert.AreEqual(4f, shader.GetPropertyDefaultFloatValue(idx), 1e-6f);
        }

        [Test]
        public void Mat_Monster2D는_ZTest를_저장하지_않아_기본_LEqual로_동작한다()
        {
            //# 기존 몬스터·영웅 렌더 회귀 0 — 머티리얼에 _ZTest 가 직렬화되면 기본값이 덮인다.
            string yaml = File.ReadAllText(MatMonster2D);
            StringAssert.DoesNotContain("_ZTest", yaml);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatMonster2D);
            Assert.IsNotNull(mat);
            Assert.AreEqual(4f, mat.GetFloat("_ZTest"), 1e-6f);
        }

        [Test]
        public void Mat_FX2D는_몬스터_머티리얼과_같은_셰이더를_쓴다()
        {
            Material fx = AssetDatabase.LoadAssetAtPath<Material>(MatFx2D);
            Material mon = AssetDatabase.LoadAssetAtPath<Material>(MatMonster2D);
            Assert.IsNotNull(fx);
            Assert.IsNotNull(mon);
            Assert.AreSame(mon.shader, fx.shader);
        }
    }
}
