using System.Collections.Generic;
using System.IO;
using Lair.Character;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;

namespace Lair.EditorTools.OneShot
{
    //# 일회용 에셋 빌더 (Rule 04 §3) — 궁수 영웅 Unity 에셋을 기사(Knight) 선례대로 구성한 뒤 삭제한다. 기획서 chapter-system §5.5·§5.6 (D-13·D-14·D-15).
    //# 1) 시트 10장 임포트 설정 = Knight 시트 설정 복제 + 72px 20x9 슬라이스(이름 "<시트명>_<row*20+col>") + 색 시트의 _EmissionMask 보조 텍스처 연결
    //# 2) Archer_SheetSpec.json 으로 클립 9종(+이벤트 베이크) → Archer_2D.controller (Knight_2D.controller 복제 후 모션만 교체)
    //# 3) Archer.prefab = Knight.prefab 의 Prefab Variant (시트·컨트롤러·티어 오버레이 프레임·공격 락 fallback·RangedHitFx 오버라이드) + Addressables "Archer"
    //# 4) 스테이지 초상 5장(HeroIcons/Archer*.png) 생성 + HeroStageVariantConfig Ch2 엔트리 Portrait 연결
    //# 기존 에셋은 덮어쓰지 않는다 — 대상이 이미 있으면 그 단계는 LogError 후 건너뛴다(시트 임포트 설정만 재실행 가능). Knight 에셋은 읽기만 한다.
    public static class ArcherAssetBuilder
    {
        private const string SheetDir = "Assets/_Lair/Art/Sprites/Heroes2D/";
        private const string IconDir = "Assets/_Lair/Art/Sprites/HeroIcons/";
        private const string ClipDir = "Assets/_Lair/Art/Animations/Heroes2D/";
        private const string KnightControllerPath = "Assets/_Lair/Art/Animations/Knight_2D.controller";
        private const string ArcherControllerPath = "Assets/_Lair/Art/Animations/Archer_2D.controller";
        private const string KnightPrefabPath = "Assets/_Lair/Art/Characters/Knight.prefab";
        private const string ArcherPrefabPath = "Assets/_Lair/Art/Characters/Archer.prefab";
        private const string SpecPath = SheetDir + "Archer_SheetSpec.json";
        private const string VariantConfigPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";
        private const string AddressableLabel = "Resource";

        //# 시트 규격 (Archer_SheetSpec.json 과 동일) — 72px 셀, 20열 x 9행, 중앙 커스텀 피벗, PPU 31.
        private const int Cell = 72;
        private const int Cols = 20;
        private const int Rows = 9;
        private const float PixelsPerUnit = 31f;
        private static readonly Vector2 Pivot = new Vector2(0.4236f, 0.2917f);

        //# 스테이지 시트 접미 — 0:"" 1:_S2 .. 4:_S5 (spec tiers). 초상 파일명 접미도 같다(Knight / Knight_S2 ..).
        private static readonly string[] TierSuffix = { "", "_S2", "_S3", "_S4", "_S5" };

        //# 초상 = 시트 idle 프레임0 에서 48x48 창을 잘라 x6 최근접 확대(288x288) — 기사 초상 5장에서 역산한 규칙(창 x0=5, 배율 6, 5장 공통).
        //# 기사 창 y0 은 8 이지만 궁수 5단계(사슴뿔 투구)는 셀 y=6 부터 시작해 y0=8 이면 머리가 잘려 y0=5 로 한다(5장 공통, 발 위치 정렬 유지).
        private const int PortraitWindow = 48;
        private const int PortraitWindowX = 5;
        private const int PortraitWindowY = 5;
        private const int PortraitScale = 6;

        //# 스프라이트명 → 클립 행 순서 (spec rowOrder).
        private static readonly string[] RowOrder = { "idle", "walk", "run", "slash01", "slash02", "stab", "hit", "death", "spawn" };

        [MenuItem("Lair/OneShot/Build Archer Assets")]
        public static void Run()
        {
            if (File.Exists(SpecPath) == false)
            {
                Debug.LogError($"[ArcherAssetBuilder] 시트 규격 파일 없음: {SpecPath} - 중단");
                return;
            }

            if (ImportSheets() == false)
                return;
            Dictionary<string, AnimationClip> clips = BuildClips();
            AnimatorController controller = BuildController(clips);
            BuildPortraits();
            BuildPrefab(controller);
            LinkPortraitsToVariantConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArcherAssetBuilder] 완료 - 결과 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# ---- 1) 시트 임포트 ----
        private static bool ImportSheets()
        {
            //# 보조 텍스처(Emission)를 먼저 임포트해야 색 시트가 참조할 수 있다.
            for (int tier = 0; tier < TierSuffix.Length; ++tier)
            {
                if (ConfigureSheet(tier, emission: true) == false)
                    return false;
            }
            for (int tier = 0; tier < TierSuffix.Length; ++tier)
            {
                if (ConfigureSheet(tier, emission: false) == false)
                    return false;
            }
            return true;
        }

        private static string SheetName(int tier, string prefix, bool emission)
        {
            return prefix + "_Sheet" + TierSuffix[tier] + (emission ? "_Emission" : string.Empty);
        }

        private static bool ConfigureSheet(int tier, bool emission)
        {
            string archerName = SheetName(tier, "Archer", emission);
            string archerPath = SheetDir + archerName + ".png";
            string knightPath = SheetDir + SheetName(tier, "Knight", emission) + ".png";

            TextureImporter archer = AssetImporter.GetAtPath(archerPath) as TextureImporter;
            TextureImporter knight = AssetImporter.GetAtPath(knightPath) as TextureImporter;
            if (archer == null || knight == null)
            {
                Debug.LogError($"[ArcherAssetBuilder] 시트 임포터를 찾지 못함: {archerPath} / {knightPath} - 중단");
                return false;
            }

            //# Knight 시트 설정 그대로 복제 — Sprite(Multiple)·Point·무압축·PPU 31·알파/sRGB(발광 시트는 sRGB off).
            TextureImporterSettings settings = new TextureImporterSettings();
            knight.ReadTextureSettings(settings);
            archer.SetTextureSettings(settings);
            archer.textureCompression = knight.textureCompression;
            archer.compressionQuality = knight.compressionQuality;
            archer.maxTextureSize = knight.maxTextureSize;
            archer.mipmapEnabled = false;

#pragma warning disable 618
            archer.spritesheet = BuildSlices(archerName);
#pragma warning restore 618

            //# 색 시트만 _EmissionMask 보조 텍스처로 같은 단계 발광 시트를 가진다(기사와 동일).
            if (emission == false)
            {
                Texture2D emissionTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetDir + SheetName(tier, "Archer", true) + ".png");
                if (emissionTexture == null)
                {
                    Debug.LogError($"[ArcherAssetBuilder] 발광 시트 로드 실패: tier {tier}");
                    return false;
                }
                archer.secondarySpriteTextures = new[]
                {
                    new SecondarySpriteTexture { name = "_EmissionMask", texture = emissionTexture },
                };
            }

            archer.SaveAndReimport();
            return true;
        }

        //# 72px 셀 20x9 슬라이스 — 이름 "<시트명>_<row*20+col>", 좌상단 셀이 0. 커스텀 피벗(0.4236, 0.2917).
        private static SpriteMetaData[] BuildSlices(string sheetName)
        {
            SpriteMetaData[] metas = new SpriteMetaData[Cols * Rows];
            int height = Rows * Cell;
            for (int index = 0; index < metas.Length; ++index)
            {
                int row = index / Cols;
                int col = index % Cols;
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"{sheetName}_{index}";
                meta.rect = new Rect(col * Cell, height - Cell * (row + 1), Cell, Cell);
                meta.alignment = (int)SpriteAlignment.Custom;
                meta.pivot = Pivot;
                metas[index] = meta;
            }
            return metas;
        }

        //# 시트의 스프라이트를 인덱스(이름 접미) 순으로 180개 반환.
        private static Sprite[] LoadSheetSprites(string sheetPath, string sheetName)
        {
            Sprite[] frames = new Sprite[Cols * Rows];
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                Sprite sprite = asset as Sprite;
                if (sprite == null || sprite.name.StartsWith(sheetName + "_") == false)
                    continue;
                string suffix = sprite.name.Substring(sheetName.Length + 1);
                if (int.TryParse(suffix, out int index) && index >= 0 && index < frames.Length)
                {
                    frames[index] = sprite;
                }
            }
            return frames;
        }

        //# ---- 2) 클립 + 컨트롤러 ----
        private static Dictionary<string, AnimationClip> BuildClips()
        {
            JObject spec = JObject.Parse(File.ReadAllText(SpecPath));
            JObject clipSpecs = (JObject)spec["clips"];
            Sprite[] frames = LoadSheetSprites(SheetDir + "Archer_Sheet.png", "Archer_Sheet");

            Dictionary<string, AnimationClip> result = new Dictionary<string, AnimationClip>();
            for (int row = 0; row < RowOrder.Length; ++row)
            {
                string key = RowOrder[row];
                string clipName = "Archer_" + char.ToUpperInvariant(key[0]) + key.Substring(1);
                string path = ClipDir + clipName + ".anim";

                AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (existing != null)
                {
                    Debug.LogError($"[ArcherAssetBuilder] 이미 존재 - 덮어쓰지 않고 재사용: {path}");
                    result[key] = existing;
                    continue;
                }

                JObject clipSpec = (JObject)clipSpecs[key];
                int frameCount = clipSpec["frames"].Value<int>();
                float fps = clipSpec["fps"].Value<float>();
                bool loop = clipSpec["loop"].Value<bool>();

                AnimationClip clip = new AnimationClip { name = clipName, frameRate = fps };
                ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameCount];
                for (int f = 0; f < frameCount; ++f)
                {
                    keys[f] = new ObjectReferenceKeyframe { time = f / fps, value = frames[row * Cols + f] };
                }
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keys);

                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                clipSettings.loopTime = loop;
                clipSettings.stopTime = frameCount / fps;   //# 기사 선례 — 마지막 프레임 길이까지 포함(frames/fps)
                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

                //# bakeEvents — slash01/slash02/stab 에 OnAttackStrike 0.300s · OnAttackEnd 0.580s (D-14). 나머지는 빈 목록.
                List<AnimationEvent> events = new List<AnimationEvent>();
                foreach (JToken bake in (JArray)clipSpec["bakeEvents"])
                {
                    events.Add(new AnimationEvent
                    {
                        functionName = bake["functionName"].Value<string>(),
                        time = bake["clipLocalSec"].Value<float>(),
                    });
                }
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());

                AssetDatabase.CreateAsset(clip, path);
                result[key] = clip;
            }
            return result;
        }

        //# Knight_2D.controller 를 복제해 모션만 Archer 클립으로 교체 — 상태·전이(ExitTime 1.0·Duration 0)·파라미터·블렌드 트리 구조는 그대로.
        private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ArcherControllerPath);
            if (existing != null)
            {
                Debug.LogError($"[ArcherAssetBuilder] 이미 존재 - 덮어쓰지 않고 재사용: {ArcherControllerPath}");
                return existing;
            }
            if (AssetDatabase.CopyAsset(KnightControllerPath, ArcherControllerPath) == false)
            {
                Debug.LogError($"[ArcherAssetBuilder] 컨트롤러 복제 실패: {KnightControllerPath}");
                return null;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ArcherControllerPath);
            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                ReplaceMotions(layer.stateMachine, clips);
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ReplaceMotions(AnimatorStateMachine machine, Dictionary<string, AnimationClip> clips)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                AnimatorState state = child.state;
                Motion motion = state.motion;
                if (motion is AnimationClip clip)
                {
                    state.motion = MapClip(clip, clips);
                }
                else if (motion is BlendTree tree)
                {
                    ChildMotion[] children = tree.children;
                    for (int i = 0; i < children.Length; ++i)
                    {
                        children[i].motion = MapClip(children[i].motion as AnimationClip, clips);
                    }
                    tree.children = children;
                }
                //# 궁수 클립은 전부 animatorSpeed 1 (Archer_SheetSpec) — 기사 stab 의 1.0204 보정은 가져오지 않는다.
                state.speed = 1f;
            }
            foreach (ChildAnimatorStateMachine sub in machine.stateMachines)
            {
                ReplaceMotions(sub.stateMachine, clips);
            }
        }

        private static Motion MapClip(AnimationClip knightClip, Dictionary<string, AnimationClip> clips)
        {
            if (knightClip == null)
                return null;
            //# "Knight_Idle" → "idle"
            string name = knightClip.name;
            int underscore = name.IndexOf('_');
            string key = underscore >= 0 ? name.Substring(underscore + 1).ToLowerInvariant() : name.ToLowerInvariant();
            if (clips.TryGetValue(key, out AnimationClip archerClip))
                return archerClip;
            Debug.LogError($"[ArcherAssetBuilder] 대응하는 궁수 클립 없음: {knightClip.name}");
            return knightClip;
        }

        //# ---- 3) 초상 ----
        private static void BuildPortraits()
        {
            TextureImporter knightIcon = AssetImporter.GetAtPath(IconDir + "Knight.png") as TextureImporter;
            if (knightIcon == null)
            {
                Debug.LogError("[ArcherAssetBuilder] Knight 초상 임포터 없음 - 초상 생성 건너뜀");
                return;
            }

            for (int tier = 0; tier < TierSuffix.Length; ++tier)
            {
                string iconAssetPath = IconDir + "Archer" + TierSuffix[tier] + ".png";
                string iconFilePath = Path.Combine(Directory.GetCurrentDirectory(), iconAssetPath);
                if (File.Exists(iconFilePath))
                {
                    Debug.LogError($"[ArcherAssetBuilder] 이미 존재 - 덮어쓰지 않고 건너뜀: {iconAssetPath}");
                    continue;
                }

                string sheetFilePath = Path.Combine(Directory.GetCurrentDirectory(), SheetDir + "Archer_Sheet" + TierSuffix[tier] + ".png");
                byte[] icon = CropPortrait(sheetFilePath);
                if (icon == null)
                    continue;
                File.WriteAllBytes(iconFilePath, icon);
                AssetDatabase.ImportAsset(iconAssetPath);

                TextureImporter importer = AssetImporter.GetAtPath(iconAssetPath) as TextureImporter;
                if (importer == null)
                    continue;
                TextureImporterSettings settings = new TextureImporterSettings();
                knightIcon.ReadTextureSettings(settings);
                importer.SetTextureSettings(settings);
                importer.textureCompression = knightIcon.textureCompression;
                importer.maxTextureSize = knightIcon.maxTextureSize;
                importer.SaveAndReimport();
            }
        }

        //# idle 프레임0(시트 좌상단 셀)에서 PortraitWindow 창을 잘라 PortraitScale 배 최근접 확대한 PNG 바이트.
        private static byte[] CropPortrait(string sheetFilePath)
        {
            Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (sheet.LoadImage(File.ReadAllBytes(sheetFilePath)) == false)
            {
                Debug.LogError($"[ArcherAssetBuilder] 시트 PNG 읽기 실패: {sheetFilePath}");
                Object.DestroyImmediate(sheet);
                return null;
            }

            Color32[] source = sheet.GetPixels32();
            int sheetWidth = sheet.width;
            int sheetHeight = sheet.height;
            int size = PortraitWindow * PortraitScale;
            Color32[] output = new Color32[size * size];
            bool clipped = false;
            for (int oy = 0; oy < size; ++oy)
            {
                //# 출력 행 oy 는 하단 원점 → 상단 기준 행으로 바꿔 시트 상단 기준 좌표에 대응.
                int topRow = PortraitWindowY + (size - 1 - oy) / PortraitScale;
                int sourceRow = sheetHeight - 1 - topRow;
                for (int ox = 0; ox < size; ++ox)
                {
                    int sourceColumn = PortraitWindowX + ox / PortraitScale;
                    output[oy * size + ox] = source[sourceRow * sheetWidth + sourceColumn];
                }
            }

            //# 창 밖으로 잘린 불투명 픽셀이 있는지 검사(셀 0 안에서 창 바깥 영역).
            for (int y = 0; y < Cell && clipped == false; ++y)
            {
                for (int x = 0; x < Cell; ++x)
                {
                    bool inside = x >= PortraitWindowX && x < PortraitWindowX + PortraitWindow
                        && y >= PortraitWindowY && y < PortraitWindowY + PortraitWindow;
                    if (inside)
                        continue;
                    if (source[(sheetHeight - 1 - y) * sheetWidth + x].a > 0)
                    {
                        clipped = true;
                        break;
                    }
                }
            }
            if (clipped)
            {
                Debug.LogWarning($"[ArcherAssetBuilder] 초상 창 밖으로 잘린 픽셀 있음: {sheetFilePath}");
            }

            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.SetPixels32(output);
            result.Apply();
            byte[] png = result.EncodeToPNG();
            Object.DestroyImmediate(sheet);
            Object.DestroyImmediate(result);
            return png;
        }

        //# ---- 4) 프리팹 (Knight Prefab Variant) ----
        private static void BuildPrefab(AnimatorController controller)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ArcherPrefabPath) != null)
            {
                Debug.LogError($"[ArcherAssetBuilder] 이미 존재 - 덮어쓰지 않고 건너뜀: {ArcherPrefabPath}");
                return;
            }
            GameObject knight = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath);
            if (knight == null || controller == null)
            {
                Debug.LogError("[ArcherAssetBuilder] Knight 프리팹 또는 Archer 컨트롤러 없음 - 프리팹 생성 중단");
                return;
            }

            Sprite[] bodyFrames = LoadSheetSprites(SheetDir + "Archer_Sheet.png", "Archer_Sheet");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(knight);
            instance.name = "Archer";
            try
            {
                MonsterVisual2D visual = instance.GetComponentInChildren<MonsterVisual2D>(true);
                MonsterTierOverlay overlay = instance.GetComponentInChildren<MonsterTierOverlay>(true);
                HeroAttackGate gate = instance.GetComponent<HeroAttackGate>();
                AutoCombatAI ai = instance.GetComponent<AutoCombatAI>();
                if (visual == null || overlay == null || gate == null || ai == null)
                {
                    Debug.LogError("[ArcherAssetBuilder] Knight 프리팹 구성 요소(MonsterVisual2D/MonsterTierOverlay/HeroAttackGate/AutoCombatAI)를 찾지 못함 - 중단");
                    return;
                }

                //# 몸(Visual2D) 스프라이트 + 애니메이터 컨트롤러
                SerializedObject bodyRenderer = new SerializedObject(visual.GetComponent<SpriteRenderer>());
                bodyRenderer.FindProperty("m_Sprite").objectReferenceValue = bodyFrames[0];
                bodyRenderer.ApplyModifiedProperties();
                SerializedObject animator = new SerializedObject(visual.GetComponent<Animator>());
                animator.FindProperty("m_Controller").objectReferenceValue = controller;
                animator.ApplyModifiedProperties();

                //# 강화 오버레이 — tier1~4 = S2~S5 시트 180프레임(기사와 같은 방식, 인덱스는 스프라이트명 접미로 매칭).
                SerializedObject overlayObject = new SerializedObject(overlay);
                SetSpriteArray(overlayObject, "_tier1Frames", LoadSheetSprites(SheetDir + "Archer_Sheet_S2.png", "Archer_Sheet_S2"));
                SetSpriteArray(overlayObject, "_tier2Frames", LoadSheetSprites(SheetDir + "Archer_Sheet_S3.png", "Archer_Sheet_S3"));
                SetSpriteArray(overlayObject, "_tier3Frames", LoadSheetSprites(SheetDir + "Archer_Sheet_S4.png", "Archer_Sheet_S4"));
                SetSpriteArray(overlayObject, "_tier4Frames", LoadSheetSprites(SheetDir + "Archer_Sheet_S5.png", "Archer_Sheet_S5"));
                overlayObject.ApplyModifiedProperties();

                //# 공격 락 fallback — D-14 OnAttackEnd(0.58s) 유실 안전망. 기사 1.8s 를 가져오면 DPS 가 24~36 으로 떨어진다.
                SerializedObject gateObject = new SerializedObject(gate);
                gateObject.FindProperty("_attackEndFallback").floatValue = 0.65f;
                gateObject.ApplyModifiedProperties();

                //# AI — 중앙 끌림 off, 타깃 고정 on (기사 프리팹 값과 동일하지만 계약이라 명시). KiteSettings 는 런타임 주입.
                SerializedObject aiObject = new SerializedObject(ai);
                aiObject.FindProperty("_centerPullEnabled").boolValue = false;
                aiObject.FindProperty("_targetHysteresisEnabled").boolValue = true;
                aiObject.ApplyModifiedProperties();

                //# 기본 공격 화살 FX(시각 전용, 판정과 분리) — 기본 키 ArcherArrowFx.
                if (instance.GetComponent<RangedHitFx>() == null)
                {
                    instance.AddComponent<RangedHitFx>();
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, ArcherPrefabPath);
                RegisterAddressable(saved, knight);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void SetSpriteArray(SerializedObject target, string field, Sprite[] sprites)
        {
            SerializedProperty property = target.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[ArcherAssetBuilder] 필드 없음: {field}");
                return;
            }
            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; ++i)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        //# Addressables — Knight 와 같은 그룹, 주소 "Archer"(= EHero.Archer 값명), 라벨 Resource.
        private static void RegisterAddressable(GameObject archerPrefab, GameObject knightPrefab)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[ArcherAssetBuilder] Addressables 설정 없음 - Archer 수동 등록 필요");
                return;
            }
            AddressableAssetEntry knightEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(KnightPrefabPath));
            AddressableAssetGroup group = knightEntry != null ? knightEntry.parentGroup : settings.FindGroup("Resource");
            if (group == null)
            {
                Debug.LogError("[ArcherAssetBuilder] Addressables 그룹을 찾지 못함 - Archer 수동 등록 필요");
                return;
            }
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(ArcherPrefabPath), group);
            entry.address = "Archer";
            entry.SetLabel(AddressableLabel, true);
            EditorUtility.SetDirty(settings);
        }

        //# ---- 5) HeroStageVariantConfig Ch2 엔트리 초상 연결 ----
        private static void LinkPortraitsToVariantConfig()
        {
            Object config = AssetDatabase.LoadMainAssetAtPath(VariantConfigPath);
            if (config == null)
            {
                Debug.LogError($"[ArcherAssetBuilder] HeroStageVariantConfig 없음: {VariantConfigPath}");
                return;
            }

            SerializedObject so = new SerializedObject(config);
            SerializedProperty stages = so.FindProperty("_stages");
            for (int i = 0; i < stages.arraySize; ++i)
            {
                SerializedProperty entry = stages.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("ChapterId").intValue != 2)
                    continue;
                int stageIndex = entry.FindPropertyRelative("StageIndex").intValue;
                SerializedProperty portrait = entry.FindPropertyRelative("Portrait");
                if (portrait.objectReferenceValue != null)
                    continue;   //# 이미 연결됨 — 덮어쓰지 않는다.

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + "Archer" + TierSuffix[stageIndex - 1] + ".png");
                if (sprite == null)
                {
                    Debug.LogError($"[ArcherAssetBuilder] 2-{stageIndex} 초상 스프라이트 로드 실패");
                    continue;
                }
                portrait.objectReferenceValue = sprite;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
