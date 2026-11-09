using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Lair.Character;
using Lair.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# hero-sheets.md §12 자동화 게이트 — X-H1(파일 규격)·X-H9(슬라이스·피벗)·U-H1(프리팹 참조)·U-H3(클립)·U-H6(변형 SO).
    //# 데이터 소스는 {Class}_SheetSpec.json. U-H4 는 OnAttackStrike 1개만 단언하고 OnAttackEnd 는 현행 값을 그대로 고정한다
    //# (기사 없음 / 나머지 4직업 공격 클립 0.58 존재 - 기획서 U-H4·U-H5 와 현행 데이터 불일치, game-designer 판정 대기).
    public class HeroSheetGateTests
    {
        private const string SheetDir = "Assets/_Lair/Art/Sprites/Heroes2D/";
        private const string IconDir = "Assets/_Lair/Art/Sprites/HeroIcons/";
        private const string AnimDir = "Assets/_Lair/Art/Animations/Heroes2D/";
        private const string PrefabDir = "Assets/_Lair/Art/Characters/";
        private const string VariantAssetPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";

        private static readonly string[] Classes = { "Knight", "Archer", "Assassin", "Berserker", "Paladin" };
        private static readonly string[] ClipRows = { "idle", "walk", "run", "slash01", "slash02", "stab", "hit", "death", "spawn" };
        private static readonly string[] AttackRows = { "slash01", "slash02", "stab" };
        private static readonly float[] StageScale = { 1.4f, 1.25f, 1.2f, 1.35f, 1.3f };

        private static string SheetName(string cls, int stage)
        {
            return stage == 1 ? cls + "_Sheet" : cls + "_Sheet_S" + stage;
        }

        private static JObject Spec(string cls)
        {
            return JObject.Parse(File.ReadAllText(SheetDir + cls + "_SheetSpec.json"));
        }

        private static string ClipName(string cls, string row)
        {
            return cls + "_" + char.ToUpperInvariant(row[0]) + row.Substring(1);
        }

        private static AnimationClip LoadClip(string cls, string row)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + ClipName(cls, row) + ".anim");
            Assert.IsNotNull(clip, ClipName(cls, row));
            return clip;
        }

        private static int SpriteRow(string row)
        {
            switch (row)
            {
                case "idle": return 0;
                case "walk":
                case "run": return 1;
                case "hit": return 3;
                case "death": return 4;
                case "spawn": return 5;
                default: return 2;
            }
        }

        //# 클립 길이 - float32 로 구워진 값(1.1333334 등). 4자리 반올림 값(1.1333)은 허용 오차 1e-6 밖이라 걸린다.
        private static float ExpectedLength(string cls, string row)
        {
            switch (row)
            {
                case "idle": return 1.0f;
                case "walk": return 0.8000333f;
                case "run": return 0.5714286f;
                case "hit": return 0.5f;
                case "death": return 0.8f;
                case "spawn": return 1.3333334f;
            }
            if (cls == "Knight")
                return row == "stab" ? 1.6666667f : 1.1333334f;
            return 0.6f;
        }

        //# 타격 시각 T(클립 로컬 초).
        private static float ExpectedStrike(string cls, string row)
        {
            if (cls == "Knight")
                return row == "stab" ? 0.8163f : 0.5f;
            return cls == "Archer" ? 0.3f : 0.2f;
        }

        private static void AssertNoBrokenReferences(GameObject root, string label)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), $"{label}/{child.name} Missing Script");
            }
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                Assert.IsNotNull(component, $"{label} null 컴포넌트");
                SerializedObject so = new SerializedObject(component);
                SerializedProperty property = so.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue == null
                        && property.objectReferenceInstanceIDValue != 0)
                    {
                        Assert.Fail($"{label}/{component.GetType().Name}.{property.propertyPath} 가 삭제된 에셋을 가리킴");
                    }
                }
            }
        }

        //# ===== X-H1 / X-H9 =====

        [Test]
        public void X_H1_시트_25장은_모두_1152x1152이고_초상_25장은_192x192이며_발광_마스크는_잔존하지_않는다()
        {
            int sheets = 0;
            int portraits = 0;
            foreach (string cls in Classes)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    string sheet = SheetName(cls, stage);
                    TextureImporter sheetImporter = (TextureImporter)AssetImporter.GetAtPath(SheetDir + sheet + ".png");
                    Assert.IsNotNull(sheetImporter, sheet);
                    sheetImporter.GetSourceTextureWidthAndHeight(out int w, out int h);
                    Assert.AreEqual(1152, w, sheet);
                    Assert.AreEqual(1152, h, sheet);
                    Assert.IsFalse(File.Exists(SheetDir + sheet + "_Emission.png"), sheet + "_Emission 잔존");
                    ++sheets;

                    string icon = stage == 1 ? cls : cls + "_S" + stage;
                    TextureImporter iconImporter = (TextureImporter)AssetImporter.GetAtPath(IconDir + icon + ".png");
                    Assert.IsNotNull(iconImporter, icon);
                    iconImporter.GetSourceTextureWidthAndHeight(out int iw, out int ih);
                    Assert.AreEqual(192, iw, icon);
                    Assert.AreEqual(192, ih, icon);
                    ++portraits;
                }
            }
            Assert.AreEqual(25, sheets);
            Assert.AreEqual(25, portraits);
        }

        [TestCaseSource(nameof(Classes))]
        public void X_H9_시트_5장은_36스프라이트_192셀_이름규칙_피벗_0_5_0_0833이고_PPU가_스펙과_같다(string cls)
        {
            JObject spec = Spec(cls);
            JArray stages = (JArray)spec["stages"];
            float heightWorld = (float)spec["heightWorld"];

            Assert.AreEqual(5, stages.Count);
            for (int stage = 1; stage <= 5; ++stage)
            {
                string sheet = SheetName(cls, stage);
                JToken stageSpec = stages[stage - 1];
                Assert.AreEqual(sheet, (string)stageSpec["sheet"]);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SheetDir + sheet + ".png");
                Assert.AreEqual(SpriteImportMode.Multiple, importer.spriteImportMode, sheet);
                Assert.AreEqual((float)stageSpec["ppu"], importer.spritePixelsPerUnit, 1e-3f, sheet + " PPU");
                Assert.AreEqual(heightWorld, (float)stageSpec["hPx"] / importer.spritePixelsPerUnit, 0.01f, sheet + " 월드 높이");

                HashSet<int> indices = new HashSet<int>();
                Regex pattern = new Regex("^" + sheet + "_(\\d+)$");
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(SheetDir + sheet + ".png"))
                {
                    Sprite sprite = asset as Sprite;
                    if (sprite == null)
                        continue;
                    Match match = pattern.Match(sprite.name);
                    Assert.IsTrue(match.Success, $"{sheet} 스프라이트 이름 규칙 위반: {sprite.name}");
                    Assert.IsTrue(indices.Add(int.Parse(match.Groups[1].Value)), $"{sprite.name} 중복");
                    Assert.AreEqual(192f, sprite.rect.width, sprite.name);
                    Assert.AreEqual(192f, sprite.rect.height, sprite.name);
                    Assert.AreEqual(0.5f, sprite.pivot.x / sprite.rect.width, 0.001f, sprite.name + " pivot.x");
                    Assert.AreEqual(0.0833f, sprite.pivot.y / sprite.rect.height, 0.001f, sprite.name + " pivot.y");
                }
                Assert.AreEqual(36, indices.Count, sheet);
                for (int i = 0; i < 36; ++i)
                {
                    Assert.IsTrue(indices.Contains(i), $"{sheet}_{i} 없음");
                }
            }
        }

        //# ===== U-H1 =====

        [TestCaseSource(nameof(Classes))]
        public void U_H1_프리팹은_끊어진_참조가_없고_오버레이_4단계가_각_36칸_해당_시트_스프라이트다(string cls)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + cls + ".prefab");
            Assert.IsNotNull(prefab, cls);
            AssertNoBrokenReferences(prefab, cls + ".prefab");

            MonsterTierOverlay overlay = prefab.GetComponentInChildren<MonsterTierOverlay>(true);
            Assert.IsNotNull(overlay, cls + " MonsterTierOverlay");
            SerializedObject so = new SerializedObject(overlay);
            for (int tier = 1; tier <= 4; ++tier)
            {
                string field = $"_tier{tier}Frames";
                string sheet = SheetName(cls, tier + 1);
                SerializedProperty frames = so.FindProperty(field);
                Assert.AreEqual(36, frames.arraySize, $"{cls}.{field}");

                HashSet<string> names = new HashSet<string>();
                for (int i = 0; i < frames.arraySize; ++i)
                {
                    Object sprite = frames.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(sprite, $"{cls}.{field}[{i}] null");
                    Assert.AreEqual(SheetDir + sheet + ".png", AssetDatabase.GetAssetPath(sprite), $"{cls}.{field}[{i}] 소속 시트");
                    names.Add(sprite.name);
                }
                Assert.AreEqual(36, names.Count, $"{cls}.{field} 칸 중복");
            }
        }

        //# ===== U-H3 / U-H4 =====

        [TestCaseSource(nameof(Classes))]
        public void U_H3_클립_9개는_키프레임_시각_프레임_인덱스_길이_루프가_스펙과_일치한다(string cls)
        {
            JObject clips = (JObject)Spec(cls)["clips"];

            foreach (string row in ClipRows)
            {
                AnimationClip clip = LoadClip(cls, row);
                string label = ClipName(cls, row);
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.AreEqual(1, bindings.Length, label);
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                JArray keyTimes = (JArray)clips[row]["keyTimes"];

                Assert.AreEqual(6, keys.Length, label);
                for (int i = 0; i < 6; ++i)
                {
                    Assert.AreEqual(SheetDir + cls + "_Sheet.png", AssetDatabase.GetAssetPath(keys[i].value), $"{label}[{i}]");
                    Assert.AreEqual($"{cls}_Sheet_{SpriteRow(row) * 6 + i}", keys[i].value.name, $"{label}[{i}]");
                    Assert.AreEqual((float)keyTimes[i], keys[i].time, 0.0006f, $"{label}[{i}] 시각");
                }
                Assert.AreEqual(ExpectedLength(cls, row), clip.length, 1e-6f, label + " 길이(float32)");
                Assert.AreEqual((bool)clips[row]["loop"], AnimationUtility.GetAnimationClipSettings(clip).loopTime, label + " 루프");
            }
        }

        [TestCaseSource(nameof(Classes))]
        public void U_H3_공격_클립_타격_프레임은_F3이고_시각_T는_직업별_현행_타격_시각이다(string cls)
        {
            JObject clips = (JObject)Spec(cls)["clips"];

            foreach (string row in AttackRows)
            {
                float t = ExpectedStrike(cls, row);
                JToken clipSpec = clips[row];
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(
                    LoadClip(cls, row), AnimationUtility.GetObjectReferenceCurveBindings(LoadClip(cls, row))[0]);

                Assert.AreEqual(3, (int)clipSpec["strikeFrame"], row);
                Assert.AreEqual(t, (float)clipSpec["strikeClipLocalSec"], 0.0006f, ClipName(cls, row) + " 스펙 T");
                Assert.AreEqual(t, keys[3].time, 0.0006f, ClipName(cls, row) + " F3 시각");
                Assert.AreEqual($"{cls}_Sheet_{12 + 3}", keys[3].value.name, ClipName(cls, row) + " F3 스프라이트");
            }
        }

        [TestCaseSource(nameof(Classes))]
        public void U_H4_공격_클립은_OnAttackStrike_1개를_T에_굽고_비공격_클립은_이벤트가_없다(string cls)
        {
            foreach (string row in ClipRows)
            {
                AnimationEvent[] events = LoadClip(cls, row).events;
                string label = ClipName(cls, row);
                bool isAttack = System.Array.IndexOf(AttackRows, row) >= 0;

                if (isAttack == false)
                {
                    Assert.AreEqual(0, events.Length, label);
                    continue;
                }
                int strikes = 0;
                foreach (AnimationEvent e in events)
                {
                    if (e.functionName != "OnAttackStrike")
                        continue;
                    ++strikes;
                    Assert.AreEqual(ExpectedStrike(cls, row), e.time, 0.0006f, label + " strike 시각");
                }
                Assert.AreEqual(1, strikes, label + " OnAttackStrike 개수");
                foreach (AnimationEvent e in events)
                {
                    Assert.AreNotEqual("OnSpawnAnimEnd", e.functionName, label);
                }
            }
        }

        //# 현행 값 고정(회귀) - 기사 공격 클립은 OnAttackEnd 없음, 궁수·암살자·광전사·성기사 공격 클립 12개는 0.58 에 OnAttackEnd 1개.
        [TestCaseSource(nameof(Classes))]
        public void U_H4_OnAttackEnd는_현행_값_불변이다_기사_없음_나머지_4직업_공격_클립_0_58(string cls)
        {
            foreach (string row in AttackRows)
            {
                List<AnimationEvent> ends = new List<AnimationEvent>();
                foreach (AnimationEvent e in LoadClip(cls, row).events)
                {
                    if (e.functionName == "OnAttackEnd")
                    {
                        ends.Add(e);
                    }
                }
                string label = ClipName(cls, row);
                if (cls == "Knight")
                {
                    Assert.AreEqual(0, ends.Count, label);
                    continue;
                }
                Assert.AreEqual(1, ends.Count, label);
                Assert.AreEqual(0.58f, ends[0].time, 0.0006f, label);
            }
        }

        //# ===== U-H6 =====

        [Test]
        public void U_H6_변형_SO_25엔트리는_발광이_꺼져있고_배수_Tier_초상이_유지된다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);
            Assert.IsNotNull(config);
            Assert.AreEqual(25, config.Stages.Count);

            for (int i = 0; i < 25; ++i)
            {
                HeroStageVariant v = config.Stages[i];
                int chapter = i / 5;
                int stage = i % 5;
                string label = $"{chapter + 1}-{stage + 1}";

                Assert.IsFalse(v.UseEmission, label + " UseEmission");
                Assert.AreEqual(stage == 4 ? StageScale[chapter] : 1f, v.ScaleMultiplier, 1e-4f, label + " ScaleMultiplier");
                Assert.AreEqual(stage, v.Tier, label + " Tier");
                Assert.IsNotNull(v.Portrait, label + " Portrait");
                StringAssert.Contains("HeroIcons/" + Classes[chapter], AssetDatabase.GetAssetPath(v.Portrait), label + " Portrait 소속 직업");
            }
        }
    }
}
