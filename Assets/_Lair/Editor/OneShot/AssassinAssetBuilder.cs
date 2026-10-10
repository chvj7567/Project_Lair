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
    //# 일회용 에셋 빌더 (Rule 04 §3) — 암살자 영웅 Unity 에셋을 궁수(Archer) 선례대로 구성한 뒤 삭제한다. 기획서 chapter-3-hero §3.4·§3.5·§6·§7 (D-C3-8).
    //# 실행: Unity -batchmode -nographics -quit -projectPath <경로> -executeMethod Lair.EditorTools.OneShot.AssassinAssetBuilder.Run
    //#       (시트 임포트만 먼저 돌리려면 ...AssassinAssetBuilder.ImportSheetsOnly — Run 도 같은 임포트를 멱등으로 포함)
    //# 1) 시트 10장 임포트 설정 = Knight 시트 설정 복제 + 72px 20x9 슬라이스(이름 "<시트명>_<row*20+col>") + 색 시트의 _EmissionMask 보조 텍스처 연결
    //# 2) Assassin_SheetSpec.json 으로 클립 9종(+이벤트 베이크) → Assassin_2D.controller (Knight_2D.controller 복제 후 모션만 교체)
    //# 3) Assassin.prefab = Knight.prefab 의 Prefab Variant (시트·컨트롤러·티어 오버레이 프레임·공격 락 fallback·RangedHitFx 오버라이드) + Addressables "Assassin"
    //# 4) 스테이지 초상 5장(HeroIcons/Assassin*.png) 생성 + HeroStageVariantConfig Ch3 엔트리 Portrait 연결
    //# 기존 에셋은 덮어쓰지 않는다 — 대상이 이미 있으면 그 단계는 LogError 후 건너뛴다(시트 임포트 설정만 재실행 가능). Knight 에셋은 읽기만 한다.
    public static class AssassinAssetBuilder
    {
        private const string SheetDir = "Assets/_Lair/Art/Sprites/Heroes2D/";
        private const string IconDir = "Assets/_Lair/Art/Sprites/HeroIcons/";
        private const string ClipDir = "Assets/_Lair/Art/Animations/Heroes2D/";
        private const string KnightControllerPath = "Assets/_Lair/Art/Animations/Knight_2D.controller";
        private const string AssassinControllerPath = "Assets/_Lair/Art/Animations/Assassin_2D.controller";
        private const string KnightPrefabPath = "Assets/_Lair/Art/Characters/Knight.prefab";
        private const string AssassinPrefabPath = "Assets/_Lair/Art/Characters/Assassin.prefab";
        private const string SpecPath = SheetDir + "Assassin_SheetSpec.json";
        private const string VariantConfigPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";
        private const string AddressableLabel = "Resource";
        private const int ChapterId = 3;

        //# 시트 규격 (Assassin_SheetSpec.json 과 동일) — 72px 셀, 20열 x 9행, 중앙 커스텀 피벗, PPU 31.
        private const int Cell = 72;
        private const int Cols = 20;
        private const int Rows = 9;
        private static readonly Vector2 Pivot = new Vector2(0.4236f, 0.2917f);

        //# 스테이지 시트 접미 — 0:"" 1:_S2 .. 4:_S5 (spec tiers). 초상 파일명 접미도 같다(Assassin / Assassin_S2 ..).
        private static readonly string[] TierSuffix = { "", "_S2", "_S3", "_S4", "_S5" };

        //# 초상 = 시트 idle 프레임0 에서 48x48 창을 잘라 x6 최근접 확대(288x288). 창 위치는 5단계 전부의 불투명 영역 합집합을 기준으로 계산한다
        //# (5장 공통 창 → 발 위치 정렬 유지, 5단계 칼날 왕관이 잘리지 않게 머리를 우선). 합집합이 창보다 크면 경고.
        private const int PortraitWindow = 48;
        private const int PortraitScale = 6;
        private const int FeetMargin = 3;

        private static readonly string[] RowOrder = { "idle", "walk", "run", "slash01", "slash02", "stab", "hit", "death", "spawn" };

        [MenuItem("Lair/OneShot/Build Assassin Assets")]
        public static void Run()
        {
            if (File.Exists(SpecPath) == false)
            {
                Debug.LogError($"[AssassinAssetBuilder] 시트 규격 파일 없음: {SpecPath} - 중단");
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
            Debug.Log("[AssassinAssetBuilder] 완료 - 결과 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# 시트 임포트 단계만 — 새 PNG 의 .meta 를 먼저 만들고 슬라이스/보조 텍스처를 구성한다.
        [MenuItem("Lair/OneShot/Import Assassin Sheets")]
        public static void ImportSheetsOnly()
        {
            if (ImportSheets())
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[AssassinAssetBuilder] 시트 임포트 완료");
            }
        }

        //# ---- 1) 시트 임포트 ----
        private static bool ImportSheets()
        {
            AssetDatabase.Refresh();
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
            string sheetName = SheetName(tier, "Assassin", emission);
            string sheetPath = SheetDir + sheetName + ".png";
            string knightPath = SheetDir + SheetName(tier, "Knight", emission) + ".png";

            TextureImporter assassin = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
            TextureImporter knight = AssetImporter.GetAtPath(knightPath) as TextureImporter;
            if (assassin == null || knight == null)
            {
                Debug.LogError($"[AssassinAssetBuilder] 시트 임포터를 찾지 못함: {sheetPath} / {knightPath} - 중단");
                return false;
            }

            //# Knight 시트 설정 그대로 복제 — Sprite(Multiple)·Point·무압축·PPU 31·알파/sRGB(발광 시트는 sRGB off).
            TextureImporterSettings settings = new TextureImporterSettings();
            knight.ReadTextureSettings(settings);
            assassin.SetTextureSettings(settings);
            assassin.textureCompression = knight.textureCompression;
            assassin.compressionQuality = knight.compressionQuality;
            assassin.maxTextureSize = knight.maxTextureSize;
            assassin.mipmapEnabled = false;

#pragma warning disable 618
            assassin.spritesheet = BuildSlices(sheetName);
#pragma warning restore 618

            //# 색 시트만 _EmissionMask 보조 텍스처로 같은 단계 발광 시트를 가진다(기사·궁수와 동일).
            if (emission == false)
            {
                Texture2D emissionTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetDir + SheetName(tier, "Assassin", true) + ".png");
                if (emissionTexture == null)
                {
                    Debug.LogError($"[AssassinAssetBuilder] 발광 시트 로드 실패: tier {tier}");
                    return false;
                }
                assassin.secondarySpriteTextures = new[]
                {
                    new SecondarySpriteTexture { name = "_EmissionMask", texture = emissionTexture },
                };
            }

            assassin.SaveAndReimport();
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
            Sprite[] frames = LoadSheetSprites(SheetDir + "Assassin_Sheet.png", "Assassin_Sheet");

            Dictionary<string, AnimationClip> result = new Dictionary<string, AnimationClip>();
            for (int row = 0; row < RowOrder.Length; ++row)
            {
                string key = RowOrder[row];
                string clipName = "Assassin_" + char.ToUpperInvariant(key[0]) + key.Substring(1);
                string path = ClipDir + clipName + ".anim";

                AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (existing != null)
                {
                    Debug.LogError($"[AssassinAssetBuilder] 이미 존재 - 덮어쓰지 않고 재사용: {path}");
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
                clipSettings.stopTime = frameCount / fps;   //# 기사·궁수 선례 — 마지막 프레임 길이까지 포함(frames/fps)
                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

                //# bakeEvents — slash01/slash02/stab 에 OnAttackStrike 0.200s · OnAttackEnd 0.580s (D-C3-8). 나머지는 빈 목록.
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

        //# Knight_2D.controller 를 복제해 모션만 Assassin 클립으로 교체 — 상태·전이(ExitTime 1.0·Duration 0)·파라미터·블렌드 트리 구조는 그대로.
        private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssassinControllerPath);
            if (existing != null)
            {
                Debug.LogError($"[AssassinAssetBuilder] 이미 존재 - 덮어쓰지 않고 재사용: {AssassinControllerPath}");
                return existing;
            }
            if (AssetDatabase.CopyAsset(KnightControllerPath, AssassinControllerPath) == false)
            {
                Debug.LogError($"[AssassinAssetBuilder] 컨트롤러 복제 실패: {KnightControllerPath}");
                return null;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssassinControllerPath);
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
                //# 암살자 클립은 전부 animatorSpeed 1 (Assassin_SheetSpec) — 기사 stab 의 1.0204 보정은 가져오지 않는다.
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
            if (clips.TryGetValue(key, out AnimationClip assassinClip))
                return assassinClip;
            Debug.LogError($"[AssassinAssetBuilder] 대응하는 암살자 클립 없음: {knightClip.name}");
            return knightClip;
        }

        //# ---- 3) 초상 ----
        private static void BuildPortraits()
        {
            TextureImporter knightIcon = AssetImporter.GetAtPath(IconDir + "Knight.png") as TextureImporter;
            if (knightIcon == null)
            {
                Debug.LogError("[AssassinAssetBuilder] Knight 초상 임포터 없음 - 초상 생성 건너뜀");
                return;
            }

            //# 5장 공통 창 — 5단계 시트 idle 프레임0 불투명 영역의 합집합으로 계산.
            RectInt union = ComputeOpaqueUnion();
            if (union.width <= 0)
            {
                Debug.LogError("[AssassinAssetBuilder] 시트 idle 프레임0 에서 불투명 픽셀을 찾지 못함 - 초상 생성 건너뜀");
                return;
            }
            Vector2Int window = ComputeWindow(union);
            Debug.Log($"[AssassinAssetBuilder] 초상 창 x0={window.x} y0={window.y} (불투명 합집합 x={union.x}..{union.xMax - 1} y={union.y}..{union.yMax - 1}, 상단 기준 셀 좌표)");

            for (int tier = 0; tier < TierSuffix.Length; ++tier)
            {
                string iconAssetPath = IconDir + "Assassin" + TierSuffix[tier] + ".png";
                string iconFilePath = Path.Combine(Directory.GetCurrentDirectory(), iconAssetPath);
                if (File.Exists(iconFilePath))
                {
                    Debug.LogError($"[AssassinAssetBuilder] 이미 존재 - 덮어쓰지 않고 건너뜀: {iconAssetPath}");
                    continue;
                }

                string sheetFilePath = Path.Combine(Directory.GetCurrentDirectory(), SheetDir + "Assassin_Sheet" + TierSuffix[tier] + ".png");
                byte[] icon = CropPortrait(sheetFilePath, window);
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

        //# 5단계 시트 셀 0(상단 기준 좌표, y 아래로 증가)의 불투명 픽셀 경계 상자 합집합.
        private static RectInt ComputeOpaqueUnion()
        {
            int minX = Cell;
            int minY = Cell;
            int maxX = -1;
            int maxY = -1;
            for (int tier = 0; tier < TierSuffix.Length; ++tier)
            {
                string sheetFilePath = Path.Combine(Directory.GetCurrentDirectory(), SheetDir + "Assassin_Sheet" + TierSuffix[tier] + ".png");
                Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (sheet.LoadImage(File.ReadAllBytes(sheetFilePath)) == false)
                {
                    Object.DestroyImmediate(sheet);
                    continue;
                }
                Color32[] source = sheet.GetPixels32();
                int sheetWidth = sheet.width;
                int sheetHeight = sheet.height;
                for (int y = 0; y < Cell; ++y)
                {
                    for (int x = 0; x < Cell; ++x)
                    {
                        if (source[(sheetHeight - 1 - y) * sheetWidth + x].a == 0)
                            continue;
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
                Object.DestroyImmediate(sheet);
            }
            return maxX < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        //# 창 좌상단(셀 상단 기준) — 가로는 합집합 중앙, 세로는 발 아래 FeetMargin 여유를 두되 머리(합집합 상단)는 절대 자르지 않는다.
        private static Vector2Int ComputeWindow(RectInt union)
        {
            int x0 = Mathf.Clamp(union.x + union.width / 2 - PortraitWindow / 2, 0, Cell - PortraitWindow);
            int feetAligned = Mathf.Clamp(union.yMax + FeetMargin - PortraitWindow, 0, Cell - PortraitWindow);
            int y0 = Mathf.Min(feetAligned, union.y);
            if (union.width > PortraitWindow || union.yMax > y0 + PortraitWindow)
            {
                Debug.LogWarning($"[AssassinAssetBuilder] 불투명 합집합({union.width}x{union.height})이 {PortraitWindow}px 창보다 커 초상이 잘림");
            }
            return new Vector2Int(x0, y0);
        }

        //# idle 프레임0(시트 좌상단 셀)에서 PortraitWindow 창을 잘라 PortraitScale 배 최근접 확대한 PNG 바이트.
        private static byte[] CropPortrait(string sheetFilePath, Vector2Int window)
        {
            Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (sheet.LoadImage(File.ReadAllBytes(sheetFilePath)) == false)
            {
                Debug.LogError($"[AssassinAssetBuilder] 시트 PNG 읽기 실패: {sheetFilePath}");
                Object.DestroyImmediate(sheet);
                return null;
            }

            Color32[] source = sheet.GetPixels32();
            int sheetWidth = sheet.width;
            int sheetHeight = sheet.height;
            int size = PortraitWindow * PortraitScale;
            Color32[] output = new Color32[size * size];
            for (int oy = 0; oy < size; ++oy)
            {
                //# 출력 행 oy 는 하단 원점 → 상단 기준 행으로 바꿔 시트 상단 기준 좌표에 대응.
                int topRow = window.y + (size - 1 - oy) / PortraitScale;
                int sourceRow = sheetHeight - 1 - topRow;
                for (int ox = 0; ox < size; ++ox)
                {
                    int sourceColumn = window.x + ox / PortraitScale;
                    output[oy * size + ox] = source[sourceRow * sheetWidth + sourceColumn];
                }
            }

            //# 창 밖으로 잘린 불투명 픽셀 검사(셀 0 안에서 창 바깥 영역).
            bool clipped = false;
            for (int y = 0; y < Cell && clipped == false; ++y)
            {
                for (int x = 0; x < Cell; ++x)
                {
                    bool inside = x >= window.x && x < window.x + PortraitWindow && y >= window.y && y < window.y + PortraitWindow;
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
                Debug.LogWarning($"[AssassinAssetBuilder] 초상 창 밖으로 잘린 픽셀 있음: {sheetFilePath}");
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
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AssassinPrefabPath) != null)
            {
                Debug.LogError($"[AssassinAssetBuilder] 이미 존재 - 덮어쓰지 않고 건너뜀: {AssassinPrefabPath}");
                return;
            }
            GameObject knight = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath);
            if (knight == null || controller == null)
            {
                Debug.LogError("[AssassinAssetBuilder] Knight 프리팹 또는 Assassin 컨트롤러 없음 - 프리팹 생성 중단");
                return;
            }

            Sprite[] bodyFrames = LoadSheetSprites(SheetDir + "Assassin_Sheet.png", "Assassin_Sheet");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(knight);
            instance.name = "Assassin";
            try
            {
                MonsterVisual2D visual = instance.GetComponentInChildren<MonsterVisual2D>(true);
                MonsterTierOverlay overlay = instance.GetComponentInChildren<MonsterTierOverlay>(true);
                HeroAttackGate gate = instance.GetComponent<HeroAttackGate>();
                AutoCombatAI ai = instance.GetComponent<AutoCombatAI>();
                MeleeAttacker attacker = instance.GetComponent<MeleeAttacker>();
                if (visual == null || overlay == null || gate == null || ai == null || attacker == null)
                {
                    Debug.LogError("[AssassinAssetBuilder] Knight 프리팹 구성 요소(MonsterVisual2D/MonsterTierOverlay/HeroAttackGate/AutoCombatAI/MeleeAttacker)를 찾지 못함 - 중단");
                    return;
                }
                if (instance.GetComponentInChildren<CapsuleCollider>(true) == null)
                {
                    Debug.LogError("[AssassinAssetBuilder] Knight 프리팹에 CapsuleCollider 없음 - 연막 탄 사거리 하한(충돌 거리)이 minRange 로만 걸림");
                }
                if (instance.GetComponent<HeroTargetProvider>() == null || instance.GetComponent<HeroSkillRunner>() == null)
                {
                    Debug.LogError("[AssassinAssetBuilder] Knight 프리팹에 HeroTargetProvider/HeroSkillRunner 없음 - 암살자 AI·스킬이 동작하지 않음");
                }

                //# 몸(Visual2D) 스프라이트 + 애니메이터 컨트롤러
                SerializedObject bodyRenderer = new SerializedObject(visual.GetComponent<SpriteRenderer>());
                bodyRenderer.FindProperty("m_Sprite").objectReferenceValue = bodyFrames[0];
                bodyRenderer.ApplyModifiedProperties();
                SerializedObject animator = new SerializedObject(visual.GetComponent<Animator>());
                animator.FindProperty("m_Controller").objectReferenceValue = controller;
                animator.ApplyModifiedProperties();

                //# 강화 오버레이 — tier1~4 = S2~S5 시트 180프레임(기사·궁수와 같은 방식, 인덱스는 스프라이트명 접미로 매칭).
                SerializedObject overlayObject = new SerializedObject(overlay);
                SetSpriteArray(overlayObject, "_tier1Frames", LoadSheetSprites(SheetDir + "Assassin_Sheet_S2.png", "Assassin_Sheet_S2"));
                SetSpriteArray(overlayObject, "_tier2Frames", LoadSheetSprites(SheetDir + "Assassin_Sheet_S3.png", "Assassin_Sheet_S3"));
                SetSpriteArray(overlayObject, "_tier3Frames", LoadSheetSprites(SheetDir + "Assassin_Sheet_S4.png", "Assassin_Sheet_S4"));
                SetSpriteArray(overlayObject, "_tier4Frames", LoadSheetSprites(SheetDir + "Assassin_Sheet_S5.png", "Assassin_Sheet_S5"));
                overlayObject.ApplyModifiedProperties();

                //# 공격 락 fallback — D-C3-8 OnAttackEnd(0.58s) 유실 안전망. 기사 1.8s 를 가져오면 공격 주기가 1.8s 로 늘어 DPS 가 61% 하락한다.
                SerializedObject gateObject = new SerializedObject(gate);
                gateObject.FindProperty("_attackEndFallback").floatValue = 0.65f;
                gateObject.ApplyModifiedProperties();

                //# AI — 중앙 끌림 off, 거리 기준 타깃 고정 off(암살자 표적은 점수 기준 히스테리시스를 AI 가 직접 수행). SkirmishSettings 는 런타임 주입.
                SerializedObject aiObject = new SerializedObject(ai);
                aiObject.FindProperty("_centerPullEnabled").boolValue = false;
                aiObject.FindProperty("_targetHysteresisEnabled").boolValue = false;
                aiObject.ApplyModifiedProperties();

                //# 흐름 역전(공격 개시/strike 분할) — 영웅 계약이라 명시.
                SerializedObject attackerObject = new SerializedObject(attacker);
                attackerObject.FindProperty("_deferStrike").boolValue = true;
                attackerObject.ApplyModifiedProperties();

                //# 기본 공격 베기 FX(시각 전용, 판정과 분리) — RangedHitFx 규약(영웅→타깃 직선), 키 AssassinSlashFx.
                RangedHitFx hitFx = instance.GetComponent<RangedHitFx>();
                if (hitFx == null)
                {
                    hitFx = instance.AddComponent<RangedHitFx>();
                }
                SerializedObject hitFxObject = new SerializedObject(hitFx);
                SerializedProperty fxKey = hitFxObject.FindProperty("_fxKey");
                int enumIndex = System.Array.IndexOf(fxKey.enumNames, "AssassinSlashFx");
                if (enumIndex < 0)
                {
                    Debug.LogError("[AssassinAssetBuilder] EVisual.AssassinSlashFx 를 찾지 못함 - RangedHitFx 키 미설정");
                }
                else
                {
                    fxKey.enumValueIndex = enumIndex;
                    hitFxObject.ApplyModifiedProperties();
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, AssassinPrefabPath);
                RegisterAddressable();
                if (saved == null)
                {
                    Debug.LogError("[AssassinAssetBuilder] 프리팹 저장 실패");
                }
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
                Debug.LogError($"[AssassinAssetBuilder] 필드 없음: {field}");
                return;
            }
            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; ++i)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        //# Addressables — Knight 와 같은 그룹, 주소 "Assassin"(= EHero.Assassin 값명), 라벨 Resource.
        private static void RegisterAddressable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AssassinAssetBuilder] Addressables 설정 없음 - Assassin 수동 등록 필요");
                return;
            }
            AddressableAssetEntry knightEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(KnightPrefabPath));
            AddressableAssetGroup group = knightEntry != null ? knightEntry.parentGroup : settings.FindGroup("Resource");
            if (group == null)
            {
                Debug.LogError("[AssassinAssetBuilder] Addressables 그룹을 찾지 못함 - Assassin 수동 등록 필요");
                return;
            }
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(AssassinPrefabPath), group);
            entry.address = "Assassin";
            entry.SetLabel(AddressableLabel, true);
            EditorUtility.SetDirty(settings);
        }

        //# ---- 5) HeroStageVariantConfig Ch3 엔트리 초상 연결 ----
        private static void LinkPortraitsToVariantConfig()
        {
            Object config = AssetDatabase.LoadMainAssetAtPath(VariantConfigPath);
            if (config == null)
            {
                Debug.LogError($"[AssassinAssetBuilder] HeroStageVariantConfig 없음: {VariantConfigPath}");
                return;
            }

            SerializedObject so = new SerializedObject(config);
            SerializedProperty stages = so.FindProperty("_stages");
            for (int i = 0; i < stages.arraySize; ++i)
            {
                SerializedProperty entry = stages.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("ChapterId").intValue != ChapterId)
                    continue;
                int stageIndex = entry.FindPropertyRelative("StageIndex").intValue;
                SerializedProperty portrait = entry.FindPropertyRelative("Portrait");
                if (portrait.objectReferenceValue != null)
                    continue;   //# 이미 연결됨 — 덮어쓰지 않는다.

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + "Assassin" + TierSuffix[stageIndex - 1] + ".png");
                if (sprite == null)
                {
                    Debug.LogError($"[AssassinAssetBuilder] 3-{stageIndex} 초상 스프라이트 로드 실패");
                    continue;
                }
                portrait.objectReferenceValue = sprite;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
