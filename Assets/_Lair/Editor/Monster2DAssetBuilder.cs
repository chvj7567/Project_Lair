using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lair.Character;
using Lair.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Lair.EditorTools
{
    //# 일회용 에디터 툴(Rule 04 §3) — monster-2d-conversion.md §7·§11 규격대로
    //# 텍스처 임포트 + 애니메이션 클립/컨트롤러 + 프리팹 배선을 1회 실행한다.
    //# 실행 후 정상 생성 확인되면 이 스크립트는 삭제한다(단일 진실 = 생성된 에셋).
    public static class Monster2DAssetBuilder
    {
        private const string MenuPath = "Lair/Build Monster 2D Assets (Run Once, Then Delete)";

        private const string SheetFolder = "Assets/_Lair/Art/Sprites/Monsters2D";
        private const string AnimMonsterFolder = "Assets/_Lair/Art/Animations/Monsters2D";
        private const string ShaderFolder = "Assets/_Lair/Art/Shaders";
        private const string ShaderPath = "Assets/_Lair/Art/Shaders/Monster2DSprite.shader";
        private const string MaterialPath = "Assets/_Lair/Art/Materials/Mat_Monster2D.mat";
        private const string ControllerPath = "Assets/_Lair/Art/Animations/Monster2D.controller";
        private const string CharactersFolder = "Assets/_Lair/Art/Characters";

        //# §5.2 프레임 규격 — 종별 상태별 (프레임수, FPS).
        private struct FrameSpec
        {
            public int frameCount;
            public int fps;
        }

        private struct SpeciesSpec
        {
            public int cellSize;
            public FrameSpec idle;
            public FrameSpec move;
            public FrameSpec attack;
            public FrameSpec hit;
            public FrameSpec death;
            public float[] hpBarHeightByTier;
        }

        private static FrameSpec F(int frameCount, int fps) => new FrameSpec { frameCount = frameCount, fps = fps };

        //# §3.3 셀 규격 + §5.2 프레임표 + §6.3.7 HP바 표 — 실측값 그대로.
        private static readonly Dictionary<EMonster, SpeciesSpec> SpeciesTable = new Dictionary<EMonster, SpeciesSpec>
        {
            { EMonster.Wisp, new SpeciesSpec
                {
                    cellSize = 48,
                    idle = F(6, 8), move = F(6, 12), attack = F(5, 12), hit = F(3, 12), death = F(6, 12),
                    hpBarHeightByTier = new[] { 0.747f, 0.809f, 0.872f, 0.934f },
                }
            },
            { EMonster.Wraith, new SpeciesSpec
                {
                    cellSize = 88,
                    idle = F(6, 6), move = F(6, 8), attack = F(5, 10), hit = F(3, 12), death = F(6, 12),
                    hpBarHeightByTier = new[] { 1.434f, 1.559f, 1.663f, 1.726f },
                }
            },
            { EMonster.Reaper, new SpeciesSpec
                {
                    cellSize = 72,
                    idle = F(6, 8), move = F(6, 12), attack = F(4, 12), hit = F(3, 12), death = F(6, 12),
                    hpBarHeightByTier = new[] { 1.038f, 1.080f, 1.080f, 1.101f },
                }
            },
            { EMonster.Hex, new SpeciesSpec
                {
                    cellSize = 56,
                    idle = F(6, 8), move = F(6, 12), attack = F(5, 12), hit = F(3, 12), death = F(6, 12),
                    hpBarHeightByTier = new[] { 0.934f, 1.018f, 1.080f, 1.143f },
                }
            },
            { EMonster.Plague, new SpeciesSpec
                {
                    cellSize = 40,
                    idle = F(6, 8), move = F(6, 12), attack = F(5, 12), hit = F(3, 12), death = F(6, 12),
                    hpBarHeightByTier = new[] { 0.643f, 0.747f, 0.747f, 0.788f },
                }
            },
            { EMonster.Phantom, new SpeciesSpec
                {
                    cellSize = 40,
                    idle = F(4, 10), move = F(4, 12), attack = F(4, 12), hit = F(3, 12), death = F(6, 12),
                    hpBarHeightByTier = new[] { 0.538f, 0.622f, 0.684f, 0.747f },
                }
            },
        };

        //# 상태 → 시트 행 인덱스(§7.2: 0 대기·1 이동·2 공격·3 피격·4 사망). 열 = 프레임(왼쪽부터).
        private static readonly string[] StateNames = { "Idle", "Move", "Attack", "Hit", "Death" };
        private static readonly bool[] StateLoops = { true, true, false, false, false };

        [MenuItem(MenuPath)]
        public static void BuildAll()
        {
            EnsureFolder(ShaderFolder);
            EnsureFolder(AnimMonsterFolder);

            EnsureShaderAndMaterial();
            ImportSigilTexture();

            AnimatorController baseController = BuildSharedController(out Dictionary<string, AnimationClip> templates);

            int clipCount = 0;
            int prefabCount = 0;

            EMonster[] species = (EMonster[])System.Enum.GetValues(typeof(EMonster));
            foreach (EMonster monster in species)
            {
                SpeciesSpec spec = SpeciesTable[monster];
                ImportSpeciesTextures(monster, spec.cellSize);

                Dictionary<string, Sprite> bodySprites = BuildSpriteDict($"{SheetFolder}/{monster}_Sheet.png");
                Dictionary<string, AnimationClip> speciesClips = BuildSpeciesClips(monster, spec, bodySprites);
                clipCount += speciesClips.Count;

                AnimatorOverrideController overrideController =
                    BuildOverrideController(monster, baseController, templates, speciesClips);

                RewirePrefab(monster, spec, overrideController, bodySprites);
                prefabCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Monster2DAssetBuilder] 완료 — 프리팹 {prefabCount}개 재배선, 클립 {clipCount}개(+템플릿 5), " +
                "Monster2D.controller 1개, 종족별 OverrideController 6개, Mat_Monster2D.mat 1개, EnhanceSigil 슬라이스 완료. " +
                "정상 확인 후 이 스크립트(Monster2DAssetBuilder.cs)를 삭제하세요(Rule 04 §3).");
        }

        //# ---- 셰이더 / 머티리얼 ----

        private static void EnsureShaderAndMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) == null)
            {
                File.WriteAllText(ShaderPath, Monster2DShaderSource);
                AssetDatabase.ImportAsset(ShaderPath);
            }

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("[Monster2DAssetBuilder] Monster2DSprite.shader 임포트 실패 — 머티리얼 생성 중단");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null)
                return;

            Material mat = new Material(shader) { name = "Mat_Monster2D" };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }

        //# monster-2d-conversion.md §7.1 이탈 — ShaderGraph 대신 ShaderLab 텍스트로 대체(헤드리스 세션 제약).
        private const string Monster2DShaderSource = @"Shader ""Lair/Monster2DSprite""
{
    Properties
    {
        [PerRendererData] _MainTex (""Sprite Texture"", 2D) = ""white"" {}
        [PerRendererData] _EmissionMask (""Emission Mask"", 2D) = ""black"" {}
        [HDR] _EmissionColor (""Emission Color"", Color) = (0,0,0,1)
        [Toggle(_EMISSION)] _EmissionToggle (""Emission Enabled"", Float) = 0
        _FlashWhite (""Flash White"", Range(0,1)) = 0
        _FlashInvert (""Flash Invert"", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            ""Queue"" = ""Transparent""
            ""RenderType"" = ""Transparent""
            ""RenderPipeline"" = ""UniversalPipeline""
            ""IgnoreProjector"" = ""True""
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _EMISSION

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMask); SAMPLER(sampler_EmissionMask);
            float4 _MainTex_ST;
            half4 _EmissionColor;
            half _FlashWhite;
            half _FlashInvert;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 rgb = tex.rgb * IN.color.rgb;
                rgb = lerp(rgb, 1h - rgb, _FlashInvert);
                rgb = lerp(rgb, 1h, _FlashWhite);
                half alpha = tex.a * IN.color.a;

                #if defined(_EMISSION)
                half emissionMask = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, IN.uv).r;
                rgb += _EmissionColor.rgb * emissionMask * alpha;
                #endif

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
";

        //# ---- 텍스처 임포트(§7.3) ----

        private static void ImportSpeciesTextures(EMonster species, int cellSize)
        {
            string prefix = species.ToString();
            Vector2 bottomCenter = new Vector2(0.5f, 0f);

            //# 마스크 먼저(본 텍스처가 참조할 Texture2D 필요) — sRGB off, alpha off.
            ConfigureSpriteSheet(
                $"{SheetFolder}/{prefix}_Sheet_Emission.png", 6, 5, cellSize, bottomCenter,
                sRGB: false, alphaTransparency: false, namePrefix: $"{prefix}_Sheet_Emission", secondaryMaskPath: null);
            ConfigureSpriteSheet(
                $"{SheetFolder}/{prefix}_Sheet.png", 6, 5, cellSize, bottomCenter,
                sRGB: true, alphaTransparency: true, namePrefix: $"{prefix}_Sheet",
                secondaryMaskPath: $"{SheetFolder}/{prefix}_Sheet_Emission.png");

            string[] tiers = { "T1", "T2", "T3" };
            foreach (string tier in tiers)
            {
                ConfigureSpriteSheet(
                    $"{SheetFolder}/{prefix}_Sheet_{tier}_Emission.png", 6, 5, cellSize, bottomCenter,
                    sRGB: false, alphaTransparency: false, namePrefix: $"{prefix}_Sheet_{tier}_Emission", secondaryMaskPath: null);
                ConfigureSpriteSheet(
                    $"{SheetFolder}/{prefix}_Sheet_{tier}.png", 6, 5, cellSize, bottomCenter,
                    sRGB: true, alphaTransparency: true, namePrefix: $"{prefix}_Sheet_{tier}",
                    secondaryMaskPath: $"{SheetFolder}/{prefix}_Sheet_{tier}_Emission.png");
            }
        }

        private static void ImportSigilTexture()
        {
            ConfigureSpriteSheet(
                $"{SheetFolder}/EnhanceSigil.png", 3, 6, 80, new Vector2(0.5f, 0.5f),
                sRGB: true, alphaTransparency: true, namePrefix: "EnhanceSigil", secondaryMaskPath: null);
        }

        //# 그리드 슬라이스 — 인덱스 = 행×열수 + 열(§7.3 "Keep Empty Rects" 취지: 항상 cols×rows 전부 생성).
        //# 행 0 = 이미지 상단(절차 생성 원화가 캔버스 top-down 으로 그려짐 — 실제와 다르면 y 산식 뒤집기).
        private static void ConfigureSpriteSheet(
            string path, int cols, int rows, int cellSize, Vector2 pivot,
            bool sRGB, bool alphaTransparency, string namePrefix, string secondaryMaskPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Monster2DAssetBuilder] TextureImporter 미발견: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 48f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = sRGB;
            importer.alphaIsTransparency = alphaTransparency;
            importer.maxTextureSize = 1024;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
#pragma warning disable 0618
                    SpriteMetaData meta = new SpriteMetaData
                    {
                        name = $"{namePrefix}_{r * cols + c}",
                        rect = new Rect(c * cellSize, (rows - r - 1) * cellSize, cellSize, cellSize),
                        pivot = pivot,
                        alignment = (int)SpriteAlignment.Custom,
                    };
#pragma warning restore 0618
                    metas.Add(meta);
                }
            }
#pragma warning disable 0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618

            if (string.IsNullOrEmpty(secondaryMaskPath) == false)
            {
                Texture2D maskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(secondaryMaskPath);
                if (maskTex != null)
                {
                    importer.secondarySpriteTextures = new[]
                    {
                        new SecondarySpriteTexture { name = "_EmissionMask", texture = maskTex },
                    };
                }
                else
                {
                    Debug.LogWarning($"[Monster2DAssetBuilder] 마스크 텍스처 미발견(보조 텍스처 미연결): {secondaryMaskPath}");
                }
            }

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static Dictionary<string, Sprite> BuildSpriteDict(string path)
        {
            Dictionary<string, Sprite> dict = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    dict[sprite.name] = sprite;
                }
            }
            return dict;
        }

        private static Sprite[] LoadSlicedSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        //# ---- Animator 컨트롤러(§5.7) ----

        private static AnimatorController BuildSharedController(out Dictionary<string, AnimationClip> templates)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Spawn", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            templates = new Dictionary<string, AnimationClip>();
            Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();

            for (int i = 0; i < StateNames.Length; i++)
            {
                string stateName = StateNames[i];
                AnimationClip template = new AnimationClip { frameRate = 12f };
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(template);
                settings.loopTime = StateLoops[i];
                AnimationUtility.SetAnimationClipSettings(template, settings);
                CreateOrReplaceAsset(template, $"{AnimMonsterFolder}/_Template_{stateName}.anim");
                templates[stateName] = template;

                AnimatorState state = sm.AddState(stateName, new Vector3(300f, i * 100f, 0f));
                state.motion = template;
                states[stateName] = state;
            }

            sm.defaultState = states["Idle"];

            //# Any State 우선순위: 1 Death · 2 Attack · 3 Hit(§5.7 표).
            AnimatorStateTransition deathT = sm.AddAnyStateTransition(states["Death"]);
            ConfigureInstant(deathT);
            deathT.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

            AnimatorStateTransition attackT = sm.AddAnyStateTransition(states["Attack"]);
            ConfigureInstant(attackT);
            attackT.canTransitionToSelf = true;
            attackT.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            attackT.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

            AnimatorStateTransition hitT = sm.AddAnyStateTransition(states["Hit"]);
            ConfigureInstant(hitT);
            hitT.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            hitT.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

            AnimatorStateTransition idleToMove = states["Idle"].AddTransition(states["Move"]);
            ConfigureInstant(idleToMove);
            idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");

            AnimatorStateTransition moveToIdle = states["Move"].AddTransition(states["Idle"]);
            ConfigureInstant(moveToIdle);
            moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");

            AnimatorStateTransition attackToIdle = states["Attack"].AddTransition(states["Idle"]);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0f;

            AnimatorStateTransition hitToIdle = states["Hit"].AddTransition(states["Idle"]);
            hitToIdle.hasExitTime = true;
            hitToIdle.exitTime = 1f;
            hitToIdle.duration = 0f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureInstant(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.exitTime = 0f;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
        }

        //# ---- 애니메이션 클립(§5.2) ----

        private static Dictionary<string, AnimationClip> BuildSpeciesClips(
            EMonster species, SpeciesSpec spec, Dictionary<string, Sprite> bodySprites)
        {
            string prefix = $"{species}_Sheet";
            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>
            {
                { "Idle", BuildClip(species, "Idle", prefix, bodySprites, 0, spec.idle, true) },
                { "Move", BuildClip(species, "Move", prefix, bodySprites, 1, spec.move, true) },
                { "Attack", BuildClip(species, "Attack", prefix, bodySprites, 2, spec.attack, false) },
                { "Hit", BuildClip(species, "Hit", prefix, bodySprites, 3, spec.hit, false) },
                { "Death", BuildClip(species, "Death", prefix, bodySprites, 4, spec.death, false) },
            };
            return clips;
        }

        private static AnimationClip BuildClip(
            EMonster species, string stateName, string namePrefix, Dictionary<string, Sprite> sprites,
            int row, FrameSpec spec, bool loop)
        {
            AnimationClip clip = new AnimationClip { frameRate = spec.fps };
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[spec.frameCount];
            for (int i = 0; i < spec.frameCount; i++)
            {
                int index = row * 6 + i;
                sprites.TryGetValue($"{namePrefix}_{index}", out Sprite sprite);
                keys[i] = new ObjectReferenceKeyframe { time = (float)i / spec.fps, value = sprite };
            }

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            CreateOrReplaceAsset(clip, $"{AnimMonsterFolder}/{species}_{stateName}.anim");
            return clip;
        }

        private static AnimatorOverrideController BuildOverrideController(
            EMonster species, AnimatorController baseController,
            Dictionary<string, AnimationClip> templates, Dictionary<string, AnimationClip> speciesClips)
        {
            AnimatorOverrideController oc = new AnimatorOverrideController(baseController) { name = $"{species}_2D" };

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            oc.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                foreach (KeyValuePair<string, AnimationClip> kv in templates)
                {
                    if (kv.Value != original)
                    {
                        continue;
                    }
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, speciesClips[kv.Key]);
                    break;
                }
            }
            oc.ApplyOverrides(overrides);

            CreateOrReplaceAsset(oc, $"{AnimMonsterFolder}/{species}_2D.overrideController");
            return oc;
        }

        //# ---- 프리팹 배선(§7.4·§6.3.8·§11) ----

        private static void RewirePrefab(
            EMonster species, SpeciesSpec spec, AnimatorOverrideController overrideController,
            Dictionary<string, Sprite> bodySprites)
        {
            string path = $"{CharactersFolder}/{species}.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[Monster2DAssetBuilder] 프리팹 로드 실패: {path}");
                return;
            }

            float rootScale = root.transform.localScale.x;
            float inverseScale = Mathf.Approximately(rootScale, 0f) ? 1f : 1f / rootScale;

            //# 3D LittleGhost 중첩 인스턴스 제거(§7.4).
            Transform oldVisual = root.transform.Find("Visual");
            if (oldVisual != null)
            {
                Object.DestroyImmediate(oldVisual.gameObject, true);
            }

            Material mat2D = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            //# Visual2D — 몸 스프라이트 + Animator + MonsterVisual2D 루트(Rule 02 §10).
            GameObject visual2D = new GameObject("Visual2D", typeof(SpriteRenderer), typeof(Animator), typeof(MonsterVisual2D));
            visual2D.transform.SetParent(root.transform, false);
            visual2D.transform.localPosition = Vector3.zero;
            visual2D.transform.localRotation = Quaternion.identity;
            visual2D.transform.localScale = Vector3.one * inverseScale;

            SpriteRenderer bodyRenderer = visual2D.GetComponent<SpriteRenderer>();
            bodyRenderer.sharedMaterial = mat2D;
            bodySprites.TryGetValue($"{species}_Sheet_0", out Sprite idleFrame0);
            bodyRenderer.sprite = idleFrame0;
            bodyRenderer.sortingOrder = 0;

            Animator animator = visual2D.GetComponent<Animator>();
            animator.runtimeAnimatorController = overrideController;
            animator.applyRootMotion = false;
            animator.keepAnimatorStateOnDisable = false;

            //# Enhance — 강화 오버레이(§6.3.6), 몸보다 sortingOrder +1.
            GameObject enhance = new GameObject("Enhance", typeof(SpriteRenderer), typeof(MonsterTierOverlay));
            enhance.transform.SetParent(visual2D.transform, false);
            enhance.transform.localPosition = Vector3.zero;
            enhance.transform.localRotation = Quaternion.identity;
            enhance.transform.localScale = Vector3.one;

            SpriteRenderer enhanceRenderer = enhance.GetComponent<SpriteRenderer>();
            enhanceRenderer.sharedMaterial = mat2D;
            enhanceRenderer.sortingOrder = 1;

            MonsterTierOverlay tierOverlay = enhance.GetComponent<MonsterTierOverlay>();
            SetObjectField(tierOverlay, "_overlayRenderer", enhanceRenderer);
            SetObjectArrayField(tierOverlay, "_tier1Frames", LoadSlicedSprites($"{SheetFolder}/{species}_Sheet_T1.png"));
            SetObjectArrayField(tierOverlay, "_tier2Frames", LoadSlicedSprites($"{SheetFolder}/{species}_Sheet_T2.png"));
            SetObjectArrayField(tierOverlay, "_tier3Frames", LoadSlicedSprites($"{SheetFolder}/{species}_Sheet_T3.png"));

            //# AuraSigil — 바닥 강화 문장, 루트 자식(§6.3.6·§7.4). Aura 접두 → HitFlash/AttackJuice 플래시 제외.
            GameObject sigil = new GameObject("AuraSigil", typeof(SpriteRenderer), typeof(MonsterEnhanceSigil));
            sigil.transform.SetParent(root.transform, false);
            sigil.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            sigil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sigil.transform.localScale = Vector3.one * inverseScale;

            MonsterEnhanceSigil sigilComp = sigil.GetComponent<MonsterEnhanceSigil>();
            SetObjectField(sigilComp, "_renderer", sigil.GetComponent<SpriteRenderer>());
            SetObjectArrayField(sigilComp, "_stageSprites", LoadSpeciesSigilStages(species));
            SetFloatField(sigilComp, "_rotationSpeedDegPerSec", 45f);

            //# CharacterAnimationDriver — 몬스터 신규 부착(§1.2·§11 표).
            CharacterAnimationDriver driver = root.AddComponent<CharacterAnimationDriver>();
            SetObjectField(driver, "_animator", animator);
            SetFloatField(driver, "_walkSpeed", 1f);
            SetFloatField(driver, "_runSpeed", 2f);
            SetFloatField(driver, "_hitReactionCooldown", 0.4f);
            SetFloatField(driver, "_attackSuppressWindow", 0.5f);

            //# MonsterEnhancementVisual — 4채널 재배선(§6.3.8).
            MonsterEnhancementVisual enhancementVisual = root.GetComponent<MonsterEnhancementVisual>();
            if (enhancementVisual != null)
            {
                SetObjectArrayField(enhancementVisual, "_renderers", new Object[] { bodyRenderer, enhanceRenderer });
                SetFloatArrayField(enhancementVisual, "_emissionByLevel", new[] { 1.5f, 1.9f, 2.3f, 2.7f, 3.2f });
                SetObjectField(enhancementVisual, "_tierOverlay", tierOverlay);
                SetObjectField(enhancementVisual, "_sigil", sigilComp);

                Transform hpBarWrapper = root.transform.Find("HpBarWrapper");
                MonsterHpBar hpBar = hpBarWrapper != null ? hpBarWrapper.GetComponent<MonsterHpBar>() : null;
                if (hpBar == null)
                {
                    Debug.LogWarning($"[Monster2DAssetBuilder] {species}: HpBarWrapper/MonsterHpBar 미발견");
                }
                SetObjectField(enhancementVisual, "_hpBar", hpBar);
                SetFloatArrayField(enhancementVisual, "_hpBarHeightByTier", spec.hpBarHeightByTier);
            }
            else
            {
                Debug.LogWarning($"[Monster2DAssetBuilder] {species}: MonsterEnhancementVisual 미발견 — 강화 외형 배선 스킵");
            }

            //# MonsterVisual2D 자체 참조.
            MonsterVisual2D visualComp = visual2D.GetComponent<MonsterVisual2D>();
            SetObjectField(visualComp, "_body", bodyRenderer);
            SetObjectField(visualComp, "_tierOverlay", tierOverlay);

            //# 사망 연출 0.5s(§5.5·§11) — DespawnOnDeath._delay 0 → 0.5.
            DespawnOnDeath despawn = root.GetComponent<DespawnOnDeath>();
            if (despawn != null)
            {
                SetFloatField(despawn, "_delay", 0.5f);
            }
            else
            {
                Debug.LogWarning($"[Monster2DAssetBuilder] {species}: DespawnOnDeath 미발견 — _delay 갱신 스킵");
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        //# EnhanceSigil.png 행 순서 = EMonster 선언 순서(Wisp..Phantom, §7.1) → 행 인덱스 = (int)species.
        //# 열 0=링1·1=링2·2=링2+파편(§6.3.1). 이미지 상단이 행 0 이라는 가정(§ImportSigilTexture 주석)과 동일.
        private static Sprite[] LoadSpeciesSigilStages(EMonster species)
        {
            Dictionary<string, Sprite> dict = BuildSpriteDict($"{SheetFolder}/EnhanceSigil.png");
            int rowIndex = (int)species;
            Sprite[] stages = new Sprite[3];
            for (int col = 0; col < 3; col++)
            {
                int index = rowIndex * 3 + col;
                dict.TryGetValue($"EnhanceSigil_{index}", out stages[col]);
            }
            return stages;
        }

        //# ---- 공용 헬퍼 ----

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
                return;
            if (AssetDatabase.IsValidFolder(parent) == false)
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void CreateOrReplaceAsset(Object asset, string path)
        {
            Object existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        //# SerializedObject 경유 private 필드 주입 — LairSpawnerVisualBuilder 와 동일 패턴(§ 기존 관례).
        private static void SetObjectField(Object target, string fieldName, Object value)
        {
            if (target == null)
                return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Monster2DAssetBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArrayField(Object target, string fieldName, Object[] values)
        {
            if (target == null)
                return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Monster2DAssetBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatArrayField(Object target, string fieldName, float[] values)
        {
            if (target == null)
                return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Monster2DAssetBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).floatValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatField(Object target, string fieldName, float value)
        {
            if (target == null)
                return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Monster2DAssetBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
