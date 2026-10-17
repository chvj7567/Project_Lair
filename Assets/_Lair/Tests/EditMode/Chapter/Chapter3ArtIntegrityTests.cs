using System.Collections.Generic;
using System.IO;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 챕터 3 아트 에셋 무결성 스모크 (chapter-3-hero §3.4·§3.5·§6) - 암살자 프리팹/시트/클립/컨트롤러, 스킬 FX 프리팹 5종, 그림자 성채 배경, Addressables 주소, 초상.
    //# 에셋 파일만 읽는다(씬·풀·렌더러 호출 없음). Archer/Knight 선례와 같은 SheetSpec 정합 방식.
    public class Chapter3ArtIntegrityTests : ChapterStaticStateTestBase
    {
        private const string AnimDir = "Assets/_Lair/Art/Animations/Heroes2D/";
        private const string SheetDir = "Assets/_Lair/Art/Sprites/Heroes2D/";
        private const string SpecPath = "Assets/_Lair/Art/Sprites/Heroes2D/Assassin_SheetSpec.json";
        private const string PrefabPath = "Assets/_Lair/Art/Characters/Assassin.prefab";
        private const string ControllerPath = "Assets/_Lair/Art/Animations/Assassin_2D.controller";
        private const string FxDir = "Assets/_Lair/Art/FX/";
        private const string VariantAssetPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";
        private const string AddressGroupPath = "Assets/AddressableAssetsData/AssetGroups/Resource.asset";

        private static readonly string[] ClipRows = { "idle", "walk", "run", "slash01", "slash02", "stab", "hit", "death", "spawn" };
        private static readonly string[] FxNames = { "AssassinSlashFx", "HeroSlipFx", "HeroShadowStrikeFx", "HeroBladeWhirlFx", "HeroSmokeBombFx" };
        private static readonly string[] SheetNames = { "Assassin_Sheet", "Assassin_Sheet_S2", "Assassin_Sheet_S3", "Assassin_Sheet_S4", "Assassin_Sheet_S5" };

        private static JObject Spec()
        {
            return JObject.Parse(File.ReadAllText(SpecPath));
        }

        private static string ClipAssetName(string row)
        {
            return "Assassin_" + char.ToUpperInvariant(row[0]) + row.Substring(1);
        }

        private static AnimationClip LoadClip(string row)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + ClipAssetName(row) + ".anim");
            Assert.IsNotNull(clip, $"클립 로드 실패: {ClipAssetName(row)}");
            return clip;
        }

        //# 끊어진 오브젝트 참조(에셋이 삭제돼 InstanceID 만 남은 속성)와 Missing Script 를 찾는다 - 필수/선택 구분 없이 어떤 경우에도 오류인 것만.
        private static void AssertNoBrokenReferences(GameObject root, string label)
        {
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), $"{label} 루트 Missing Script");
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), $"{label}/{child.name} Missing Script");
            }
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                Assert.IsNotNull(component, $"{label} 에 null 컴포넌트(Missing Script)");
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

        //# ===== 클립 / SheetSpec =====

        [Test]
        public void SheetSpec의_클립_행_9개가_모두_클립_에셋으로_존재한다()
        {
            JObject clips = (JObject)Spec()["clips"];

            foreach (string row in ClipRows)
            {
                Assert.IsTrue(clips.ContainsKey(row), $"SheetSpec 에 {row} 없음");
                LoadClip(row);
            }
            Assert.AreEqual(9, clips.Count);
        }

        [Test]
        public void 클립_샘플레이트는_SheetSpec_fps와_같다()
        {
            JObject clips = (JObject)Spec()["clips"];

            foreach (string row in ClipRows)
            {
                Assert.AreEqual((float)clips[row]["fps"], LoadClip(row).frameRate, 0.001f, row);
            }
        }

        [Test]
        public void 클립_길이는_프레임_수_나누기_fps다()
        {
            JObject clips = (JObject)Spec()["clips"];

            foreach (string row in ClipRows)
            {
                float expected = (float)clips[row]["frames"] / (float)clips[row]["fps"];
                Assert.AreEqual(expected, LoadClip(row).length, 0.01f, row);
            }
        }

        [Test]
        public void 클립_루프_설정은_SheetSpec과_같다()
        {
            JObject clips = (JObject)Spec()["clips"];

            foreach (string row in ClipRows)
            {
                Assert.AreEqual((bool)clips[row]["loop"], AnimationUtility.GetAnimationClipSettings(LoadClip(row)).loopTime, row);
            }
        }

        [Test]
        public void 스프라이트_키프레임_수는_프레임_수와_같고_모두_암살자_기본_시트_스프라이트다()
        {
            JObject clips = (JObject)Spec()["clips"];

            foreach (string row in ClipRows)
            {
                AnimationClip clip = LoadClip(row);
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.AreEqual(1, bindings.Length, $"{row} 스프라이트 커브 1개");
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);

                Assert.AreEqual((int)clips[row]["frames"], keys.Length, $"{row} 키프레임 수");
                foreach (ObjectReferenceKeyframe key in keys)
                {
                    Assert.IsNotNull(key.value, $"{row} 키프레임 스프라이트 null");
                    Assert.AreEqual(SheetDir + "Assassin_Sheet.png", AssetDatabase.GetAssetPath(key.value), row);
                }
            }
        }

        [TestCase("Slash01")]
        [TestCase("Slash02")]
        [TestCase("Stab")]
        public void 공격_클립은_타격_0_200초와_종료_0_580초_이벤트_두_개를_굽는다(string name)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "Assassin_" + name + ".anim");
            Assert.IsNotNull(clip);
            AnimationEvent[] events = clip.events;

            Assert.AreEqual(2, events.Length, name);
            Assert.AreEqual("OnAttackStrike", events[0].functionName);
            Assert.AreEqual(0.200f, events[0].time, 0.0005f);
            Assert.AreEqual("OnAttackEnd", events[1].functionName);
            Assert.AreEqual(0.580f, events[1].time, 0.0005f);
        }

        [Test]
        public void 공격_클립_이벤트는_SheetSpec_clipEvents와_일치한다()
        {
            JObject clipEvents = (JObject)Spec()["clipEvents"];

            foreach (KeyValuePair<string, JToken> pair in clipEvents)
            {
                AnimationEvent[] events = LoadClip(pair.Key).events;
                JArray expected = (JArray)pair.Value;

                Assert.AreEqual(expected.Count, events.Length, pair.Key);
                for (int i = 0; i < expected.Count; ++i)
                {
                    Assert.AreEqual((string)expected[i]["functionName"], events[i].functionName, $"{pair.Key}[{i}]");
                    Assert.AreEqual((float)expected[i]["clipLocalSec"], events[i].time, 0.0005f, $"{pair.Key}[{i}]");
                }
            }
        }

        [TestCase("Idle")]
        [TestCase("Walk")]
        [TestCase("Run")]
        [TestCase("Hit")]
        [TestCase("Death")]
        [TestCase("Spawn")]
        public void 공격이_아닌_클립은_이벤트가_없다(string name)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "Assassin_" + name + ".anim");

            Assert.IsNotNull(clip);
            Assert.AreEqual(0, clip.events.Length, name);
        }

        [Test]
        public void 암살자_공격_종료_시각은_궁수와_같고_타격_시각은_암살자_0_200_궁수_0_300이다()
        {
            AnimationEvent[] archer = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "Archer_Stab.anim").events;
            AnimationEvent[] assassin = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "Assassin_Stab.anim").events;

            Assert.AreEqual(2, archer.Length);
            Assert.AreEqual(2, assassin.Length);
            Assert.AreEqual("OnAttackStrike", archer[0].functionName);
            Assert.AreEqual("OnAttackStrike", assassin[0].functionName);
            Assert.AreEqual(0.300f, archer[0].time, 0.0005f, "궁수 타격");
            Assert.AreEqual(0.200f, assassin[0].time, 0.0005f, "암살자 타격(D-C3-8)");
            Assert.AreEqual("OnAttackEnd", archer[1].functionName);
            Assert.AreEqual("OnAttackEnd", assassin[1].functionName);
            Assert.AreEqual(archer[1].time, assassin[1].time, 0.0005f, "공격 종료 시각은 같다");
            Assert.AreEqual(0.580f, assassin[1].time, 0.0005f);
        }

        [Test]
        public void SheetSpec_기본값은_셀_72_20열_9행_PPU_31_시트_1440x648이다()
        {
            JObject spec = Spec();

            Assert.AreEqual(72, (int)spec["cellSize"]);
            Assert.AreEqual(20, (int)spec["cols"]);
            Assert.AreEqual(9, (int)spec["rows"]);
            Assert.AreEqual(31, (int)spec["pixelsPerUnit"]);
            Assert.AreEqual(1440, (int)spec["sheetSize"][0]);
            Assert.AreEqual(648, (int)spec["sheetSize"][1]);
            Assert.AreEqual(9, ((JArray)spec["rowOrder"]).Count);
            Assert.AreEqual(5, ((JObject)spec["tiers"]).Count);
        }

        //# ===== 시트 임포트 =====

        [Test]
        public void 시트_5단계_이미지는_스펙_크기와_Point_무압축_밉맵_없음_PPU_31이다()
        {
            foreach (string name in SheetNames)
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SheetDir + name + ".png");
                Assert.IsNotNull(importer, name);
                importer.GetSourceTextureWidthAndHeight(out int w, out int h);

                Assert.AreEqual(1440, w, name);
                Assert.AreEqual(648, h, name);
                Assert.AreEqual(FilterMode.Point, importer.filterMode, name);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, name);
                Assert.IsFalse(importer.mipmapEnabled, name);
                Assert.AreEqual(31f, importer.spritePixelsPerUnit, 1e-4f, name);
                Assert.AreEqual(SpriteImportMode.Multiple, importer.spriteImportMode, name);
            }
        }

        [Test]
        public void 시트_5단계마다_발광_시트가_짝으로_존재한다()
        {
            foreach (string name in SheetNames)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(SheetDir + name + "_Emission.png"), name + "_Emission");
            }
        }

        [Test]
        public void 시트는_180개_스프라이트로_슬라이스되고_피벗이_스펙_공통값이다()
        {
            JObject pivot = (JObject)Spec()["pivot"];
            float px = (float)pivot["x"];
            float py = (float)pivot["y"];

            foreach (string name in SheetNames)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SheetDir + name + ".png");
                int sprites = 0;
                foreach (Object asset in assets)
                {
                    Sprite sprite = asset as Sprite;
                    if (sprite == null)
                        continue;
                    ++sprites;
                    Assert.AreEqual(px, sprite.pivot.x / sprite.rect.width, 0.01f, $"{sprite.name} pivot.x");
                    Assert.AreEqual(py, sprite.pivot.y / sprite.rect.height, 0.01f, $"{sprite.name} pivot.y");
                }
                Assert.AreEqual(180, sprites, name);
            }
        }

        [Test]
        public void 영웅_초상_5단계_이미지가_존재하고_영웅_재스킨_초상이_모두_배선돼_있다()
        {
            foreach (string name in new[] { "Assassin", "Assassin_S2", "Assassin_S3", "Assassin_S4", "Assassin_S5" })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Lair/Art/Sprites/HeroIcons/" + name + ".png"), name);
            }

            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);
            HashSet<Sprite> chapter3 = new HashSet<Sprite>();
            for (int linear = 1; linear <= 15; ++linear)
            {
                Sprite portrait = config.GetStage(linear).Portrait;
                Assert.IsNotNull(portrait, $"linear {linear} 초상");
                if (linear >= 11)
                {
                    StringAssert.Contains("HeroIcons/Assassin", AssetDatabase.GetAssetPath(portrait), $"linear {linear}");
                    chapter3.Add(portrait);
                }
            }
            Assert.AreEqual(5, chapter3.Count, "챕터 3 초상 5단계는 서로 다르다");
        }

        [Test]
        public void 챕터1_2_초상은_각각_기사_궁수_이미지다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);

            for (int linear = 1; linear <= 5; ++linear)
            {
                StringAssert.Contains("HeroIcons/Knight", AssetDatabase.GetAssetPath(config.GetStage(linear).Portrait), $"linear {linear}");
            }
            for (int linear = 6; linear <= 10; ++linear)
            {
                StringAssert.Contains("HeroIcons/Archer", AssetDatabase.GetAssetPath(config.GetStage(linear).Portrait), $"linear {linear}");
            }
        }

        //# ===== 컨트롤러 / 프리팹 =====

        [Test]
        public void 암살자_컨트롤러의_상태는_모두_모션이_있고_9개_클립을_전부_참조한다()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller);

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                foreach (ChildAnimatorState child in layer.stateMachine.states)
                {
                    Assert.IsNotNull(child.state.motion, $"상태 {child.state.name} 모션 없음");
                }
            }
            HashSet<string> referenced = new HashSet<string>();
            foreach (AnimationClip clip in controller.animationClips)
            {
                referenced.Add(clip.name);
            }
            foreach (string row in ClipRows)
            {
                Assert.IsTrue(referenced.Contains(ClipAssetName(row)), $"{ClipAssetName(row)} 가 컨트롤러에서 참조되지 않음");
            }
        }

        [Test]
        public void 암살자_프리팹은_로드되고_전투_컴포넌트와_원거리_연출_FX_키를_가진다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath);

            Assert.IsNotNull(prefab.GetComponent<LairCharacter>());
            Assert.IsNotNull(prefab.GetComponent<AutoCombatAI>());
            Assert.IsNotNull(prefab.GetComponent<Health>());
            Assert.IsNotNull(prefab.GetComponent<SimpleMover>());
            Assert.IsNotNull(prefab.GetComponent<HeroTargetProvider>());
            Assert.IsNotNull(prefab.GetComponent<HeroSkillRunner>());
            Assert.IsNotNull(prefab.GetComponent<HeroAttackGate>());
            MeleeAttacker attacker = prefab.GetComponent<MeleeAttacker>();
            Assert.IsNotNull(attacker);
            Assert.IsTrue(attacker.DeferStrike, "영웅은 개시/strike 2분할");

            RangedHitFx fx = prefab.GetComponent<RangedHitFx>();
            Assert.IsNotNull(fx);
            Assert.AreEqual((int)EVisual.AssassinSlashFx, new SerializedObject(fx).FindProperty("_fxKey").intValue);
        }

        [Test]
        public void 암살자_프리팹의_애니메이터는_암살자_컨트롤러를_쓴다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            Assert.IsNotNull(animator);
            Assert.AreSame(controller, animator.runtimeAnimatorController);
        }

        [Test]
        public void 암살자_프리팹은_기사_프리팹과_같은_베이스의_변형이고_궁수와_같은_컴포넌트_구성이다()
        {
            GameObject assassin = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject archer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/Characters/Archer.prefab");

            HashSet<System.Type> assassinTypes = new HashSet<System.Type>();
            HashSet<System.Type> archerTypes = new HashSet<System.Type>();
            foreach (Component c in assassin.GetComponentsInChildren<Component>(true))
            {
                assassinTypes.Add(c.GetType());
            }
            foreach (Component c in archer.GetComponentsInChildren<Component>(true))
            {
                archerTypes.Add(c.GetType());
            }

            Assert.IsTrue(assassinTypes.SetEquals(archerTypes), "챕터 영웅 프리팹은 같은 베이스 변형이라 컴포넌트 타입 집합이 같다");
            Assert.AreEqual(PrefabUtility.GetCorrespondingObjectFromSource(archer), PrefabUtility.GetCorrespondingObjectFromSource(assassin), "같은 소스 프리팹");
        }

        [Test]
        public void 암살자_프리팹에_끊어진_참조와_Missing_Script가_없다()
        {
            AssertNoBrokenReferences(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), "Assassin.prefab");
        }

        //# ===== FX 프리팹 =====

        [TestCaseSource(nameof(FxNames))]
        public void FX_프리팹은_SpriteSheetFx_12fps_1회_자동풀반환이고_ReturnToPoolAfter가_없다(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FxDir + name + ".prefab");
            Assert.IsNotNull(prefab, name);
            SpriteSheetFx comp = prefab.GetComponentInChildren<SpriteSheetFx>(true);
            Assert.IsNotNull(comp, name + " SpriteSheetFx");
            SerializedObject so = new SerializedObject(comp);

            Assert.AreEqual(12f, so.FindProperty("_fps").floatValue, 1e-6f, name);
            Assert.IsFalse(so.FindProperty("_loop").boolValue, name);
            Assert.IsTrue(so.FindProperty("_returnToPoolOnFinish").boolValue, name);
            Assert.IsNull(prefab.GetComponentInChildren<ReturnToPoolAfter>(true), name + " 이중 Push 방지");
        }

        [TestCaseSource(nameof(FxNames))]
        public void FX_프레임은_모두_배선되고_서로_다른_스프라이트이며_자기_시트_소속이다(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FxDir + name + ".prefab");
            SerializedProperty frames = new SerializedObject(prefab.GetComponentInChildren<SpriteSheetFx>(true)).FindProperty("_frames");
            HashSet<Object> unique = new HashSet<Object>();

            Assert.Greater(frames.arraySize, 0, name);
            for (int i = 0; i < frames.arraySize; ++i)
            {
                Object sprite = frames.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(sprite, $"{name} _frames[{i}]");
                Assert.AreEqual("Assets/_Lair/Art/Sprites/FX2D/" + name + "_Sheet.png", AssetDatabase.GetAssetPath(sprite), $"{name}[{i}]");
                unique.Add(sprite);
            }
            Assert.AreEqual(frames.arraySize, unique.Count, name + " 프레임 중복");
        }

        [TestCaseSource(nameof(FxNames))]
        public void FX_프리팹에_끊어진_참조가_없다(string name)
        {
            AssertNoBrokenReferences(AssetDatabase.LoadAssetAtPath<GameObject>(FxDir + name + ".prefab"), name);
        }

        [TestCaseSource(nameof(FxNames))]
        public void FX_이름은_EVisual_값명과_정확히_일치한다(string name)
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(EVisual), name), name);
        }

        //# ===== Addressables / 배경 =====

        [Test]
        public void 암살자_프리팹_FX_5종_로드아웃_배경이_Addressables에_주소로_등록돼_있다()
        {
            string groups = File.ReadAllText(AddressGroupPath);
            List<string> addresses = new List<string> { "Assassin", "HeroSkillLoadout_Assassin", "Battle_Backdrop_ShadowCitadel" };
            addresses.AddRange(FxNames);

            foreach (string address in addresses)
            {
                StringAssert.Contains($"m_Address: {address}\n", groups.Replace("\r\n", "\n"), address);
            }
        }

        [Test]
        public void 암살자_주소는_EHero_값명과_프리팹_파일명이_같다()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(EHero), "Assassin"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/Characters/" + EHero.Assassin + ".prefab"));
        }

        [Test]
        public void 모든_챕터_배경_키는_Backdrop2D_이미지와_Addressables_주소를_가진다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            string groups = File.ReadAllText(AddressGroupPath).Replace("\r\n", "\n");

            foreach (ChapterRow chapter in db.Chapters)
            {
                string key = chapter.backdropKey;
                Assert.IsTrue(System.Enum.IsDefined(typeof(EBackdrop), key), $"챕터 {chapter.chapterId} {key}");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Lair/Art/Sprites/Backdrop2D/{key}.png"), $"{key}.png");
                //# 기본 배경(Battle_Backdrop)은 씬에 배선돼 있어 주소가 없다 - 챕터 전용 배경만 Addressables 로 교체 로드한다.
                if (key != EBackdrop.Battle_Backdrop.ToString())
                {
                    StringAssert.Contains($"m_Address: {key}\n", groups, key);
                }
            }
        }

        [Test]
        public void 그림자_성채_배경은_다른_전투_배경과_같은_크기_PPU의_SheetSpec_항목이다()
        {
            JObject files = (JObject)JObject.Parse(File.ReadAllText("Assets/_Lair/Art/Sprites/Backdrop2D/Backdrop2D_SheetSpec.json"))["files"];
            JToken citadel = files["Battle_Backdrop_ShadowCitadel.png"];
            JToken forest = files["Battle_Backdrop_DarkForest.png"];

            Assert.IsNotNull(citadel);
            Assert.AreEqual((int)forest["size"][0], (int)citadel["size"][0]);
            Assert.AreEqual((int)forest["size"][1], (int)citadel["size"][1]);
            Assert.AreEqual((int)forest["ppu"], (int)citadel["ppu"]);
            Assert.AreEqual((int)forest["frames"], (int)citadel["frames"]);
        }
    }
}
