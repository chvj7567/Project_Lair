using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChvjUnityInfra;
using Lair.Character;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Lair.EditorTools
{
    //# 일회용 에디터 툴(Rule 04 §3) — fx-2d-conversion.md §7.1·§8.3·§8.4 규격대로
    //# 시트 임포트 + Mat_FX2D + 프리팹 6종 배선 + MapBackground 정렬을 1회 실행한다(멱등).
    //# 실행 후 정상 확인되면 이 스크립트는 삭제한다(단일 진실 = 생성된 에셋).
    public static class Fx2DApplyTool
    {
        private const string MenuPath = "Lair/FX2D/Apply Sheets To Prefabs";

        private const string SheetFolder = "Assets/_Lair/Art/Sprites/FX2D";
        private const string SpecPath = "Assets/_Lair/Art/Sprites/FX2D/FX2D_SheetSpec.json";
        private const string FxFolder = "Assets/_Lair/Art/FX";
        private const string ShaderPath = "Assets/_Lair/Art/Shaders/Monster2DSprite.shader";
        private const string MatGroundPath = "Assets/_Lair/Art/Materials/Mat_Monster2D.mat";
        private const string MatStandingPath = "Assets/_Lair/Art/Materials/Mat_FX2D.mat";
        private const string BattleScenePath = "Assets/_Lair/Scenes/Battle.unity";

        private const int GroundOrder = -10;
        private const int StandingOrder = 10;
        private const int MapBackgroundOrder = -20;
        private const int CompareAlways = 8;   //# UnityEngine.Rendering.CompareFunction.Always

        //# §2 표 — PPU · 종류 · 세로 보정 · 눕힘 오일러 · 루트 스케일. 프레임/루프/발 기준점은 SheetSpec JSON 이 진실.
        private struct FxRow
        {
            public string name;
            public float ppu;
            public bool ground;          //# true = 눕힘 지면 FX(Mat_Monster2D, −10) / false = 빌보드(Mat_FX2D, 10)
            public float yScale;         //# 지면 FX 그림 세로축 보정 k
            public Vector3 euler;        //# 지면 FX 눕힘 회전
            public float rootScale;      //# 0 = 루트 스케일 미변경
            public bool autoPlay;
        }

        private static readonly FxRow[] Rows =
        {
            new FxRow { name = "PoisonAura",       ppu = 72f,  ground = true,  yScale = 2.4f, euler = new Vector3(90f, 0f, 0f),   rootScale = 2.5f, autoPlay = true },
            new FxRow { name = "HeroNovaFx",       ppu = 112f, ground = true,  yScale = 2.0f, euler = new Vector3(90f, 0f, 0f),   rootScale = 0f,   autoPlay = true },
            //# 부채꼴: 그림 +X → 루트 로컬 +Z(dir), 그림 +Y → 루트 −X, 앞면 위. Ry(−90)·Rx(90). 메인이 dir=+X 발동으로 검증.
            new FxRow { name = "HeroDashConeFx",   ppu = 92f,  ground = true,  yScale = 2.0f, euler = new Vector3(90f, -90f, 0f), rootScale = 0f,   autoPlay = true },
            new FxRow { name = "HeroOrbitBladeFx", ppu = 40f,  ground = false, yScale = 1f,   euler = Vector3.zero,              rootScale = 0f,   autoPlay = false },
            new FxRow { name = "TimeStopShield",   ppu = 16f,  ground = false, yScale = 1f,   euler = Vector3.zero,              rootScale = 0f,   autoPlay = true },
            new FxRow { name = "FearSkull",        ppu = 16f,  ground = false, yScale = 1f,   euler = Vector3.zero,              rootScale = 0f,   autoPlay = true },
        };

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            JObject spec = JObject.Parse(File.ReadAllText(SpecPath));
            float fps = spec.Value<float>("fps");
            JArray cell = (JArray)spec["cell"];
            int cellW = cell[0].Value<int>();
            int cellH = cell[1].Value<int>();

            int sheetCount = 0;
            foreach (FxRow row in Rows)
            {
                JObject fx = (JObject)spec["fx"][row.name];
                ConfigureSheet(row, fx, cellW, cellH);
                sheetCount++;
            }
            AssetDatabase.Refresh();

            Material matGround = AssetDatabase.LoadAssetAtPath<Material>(MatGroundPath);
            Material matStanding = EnsureStandingMaterial();
            if (matGround == null || matStanding == null)
            {
                Debug.LogError("[Fx2DApplyTool] 머티리얼 준비 실패 — 프리팹 배선 중단");
                return;
            }

            int prefabCount = 0;
            foreach (FxRow row in Rows)
            {
                JObject fx = (JObject)spec["fx"][row.name];
                if (RewirePrefab(row, fx, fps, row.ground ? matGround : matStanding))
                    prefabCount++;
            }

            bool sceneOk = SetMapBackgroundOrder();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[Fx2DApplyTool] 완료 — 시트 {sheetCount}장, 프리팹 {prefabCount}개, Mat_FX2D 준비, MapBackground order {(sceneOk ? "적용" : "실패")}. " +
                "정상 확인 후 이 스크립트(Fx2DApplyTool.cs)를 삭제하세요(Rule 04 §3).");
        }

        //# ---- 텍스처 임포트 · 슬라이스 (§7.1) ----

        private static void ConfigureSheet(FxRow row, JObject fx, int cellW, int cellH)
        {
            string path = $"{SheetFolder}/{row.name}_Sheet.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Fx2DApplyTool] TextureImporter 미발견: {path}");
                return;
            }

            int cols = fx.Value<int>("cols");
            int rows = fx.Value<int>("rows");
            JArray foot = (JArray)fx["foot"];
            Vector2 pivot = new Vector2(foot[0].Value<float>() / cellW, (cellH - foot[1].Value<float>()) / cellH);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = row.ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);

            //# 인덱스 = 행×열수 + 열, 행 0 = 이미지 상단. 빈 칸도 포함(Keep Empty Rects).
            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
#pragma warning disable 0618
                    metas.Add(new SpriteMetaData
                    {
                        name = $"{row.name}_Sheet_{r * cols + c}",
                        rect = new Rect(c * cellW, (rows - r - 1) * cellH, cellW, cellH),
                        pivot = pivot,
                        alignment = (int)SpriteAlignment.Custom,
                    });
#pragma warning restore 0618
                }
            }
#pragma warning disable 0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null && (tex.width != cols * cellW || tex.height != rows * cellH))
                Debug.LogWarning($"[Fx2DApplyTool] {row.name} 시트 크기 불일치: {tex.width}x{tex.height} (기대 {cols * cellW}x{rows * cellH})");
        }

        //# ---- 머티리얼 (§3.3) ----

        private static Material EnsureStandingMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("[Fx2DApplyTool] Monster2DSprite.shader 미발견");
                return null;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatStandingPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = "Mat_FX2D" };
                AssetDatabase.CreateAsset(mat, MatStandingPath);
            }
            mat.shader = shader;
            mat.SetFloat("_ZTest", CompareAlways);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        //# ---- 프리팹 배선 (§8.3) — 기존 프리팹을 열어 수정(파일·GUID 유지) ----

        private static bool RewirePrefab(FxRow row, JObject fx, float fps, Material mat)
        {
            string path = $"{FxFolder}/{row.name}.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[Fx2DApplyTool] 프리팹 로드 실패: {path}");
                return false;
            }

            //# 기존 시각(CFXR 인스턴스·이전 실행분 AuraFx) 자식 전부 제거.
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject, true);

            //# Transform·CHPoolable·SpriteSheetFx 외 컴포넌트(MeshFilter/MeshRenderer/ReturnToPoolAfter 등) 제거.
            bool removed = true;
            while (removed)
            {
                removed = false;
                foreach (Component comp in root.GetComponents<Component>())
                {
                    if (comp is Transform || comp is CHPoolable || comp is SpriteSheetFx)
                        continue;
                    Object.DestroyImmediate(comp, true);
                    removed = true;
                    break;
                }
            }

            if (row.rootScale > 0f)
                root.transform.localScale = Vector3.one * row.rootScale;

            GameObject aura = new GameObject("AuraFx", typeof(SpriteRenderer));
            aura.transform.SetParent(root.transform, false);
            aura.transform.localPosition = Vector3.zero;
            aura.transform.localRotation = Quaternion.Euler(row.euler);
            aura.transform.localScale = new Vector3(1f, row.yScale, 1f);

            int frameCount = fx.Value<int>("frames");
            Sprite[] frames = LoadFrames(row.name, frameCount);
            SpriteRenderer sr = aura.GetComponent<SpriteRenderer>();
            sr.sharedMaterial = mat;
            sr.sortingOrder = row.ground ? GroundOrder : StandingOrder;
            sr.sprite = frames.Length > 0 ? frames[0] : null;

            SpriteSheetFx sheetFx = root.GetComponent<SpriteSheetFx>();
            if (sheetFx == null)
                sheetFx = root.AddComponent<SpriteSheetFx>();

            bool loop = fx.Value<bool>("loop");
            SerializedObject so = new SerializedObject(sheetFx);
            so.FindProperty("_renderer").objectReferenceValue = sr;
            so.FindProperty("_fps").floatValue = fps;
            so.FindProperty("_loop").boolValue = loop;
            so.FindProperty("_billboard").boolValue = row.ground == false;
            so.FindProperty("_autoPlay").boolValue = row.autoPlay;
            so.FindProperty("_returnToPoolOnFinish").boolValue = loop == false;
            so.FindProperty("_poolable").objectReferenceValue = root.GetComponent<CHPoolable>();
            SerializedProperty framesProp = so.FindProperty("_frames");
            framesProp.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[Fx2DApplyTool] {row.name}: frames {frames.Length}/{frameCount}, loop {loop}, ppu {row.ppu}");
            return frames.Length == frameCount;
        }

        //# 지수 0 ~ frames−1 을 지수 순으로(빈 칸 제외).
        private static Sprite[] LoadFrames(string fxName, int frameCount)
        {
            string path = $"{SheetFolder}/{fxName}_Sheet.png";
            Dictionary<string, Sprite> dict = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);
            List<Sprite> list = new List<Sprite>();
            for (int i = 0; i < frameCount; i++)
            {
                if (dict.TryGetValue($"{fxName}_Sheet_{i}", out Sprite sprite))
                    list.Add(sprite);
                else
                    Debug.LogError($"[Fx2DApplyTool] 스프라이트 미발견: {fxName}_Sheet_{i}");
            }
            return list.ToArray();
        }

        //# ---- Battle.unity MapBackground sortingOrder −20 (§3.3) ----

        private static bool SetMapBackgroundOrder()
        {
            Scene scene = SceneManager.GetSceneByPath(BattleScenePath);
            bool openedByTool = false;
            if (scene.isLoaded == false)
            {
                scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
                openedByTool = true;
            }

            SpriteRenderer target = null;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
            {
                foreach (SpriteRenderer sr in rootGo.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.gameObject.name == "MapBackground")
                    {
                        target = sr;
                        break;
                    }
                }
                if (target != null)
                    break;
            }

            bool ok = target != null;
            if (ok)
            {
                target.sortingOrder = MapBackgroundOrder;
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                Debug.LogError("[Fx2DApplyTool] Battle.unity 에서 MapBackground SpriteRenderer 미발견");
            }

            if (openedByTool)
                EditorSceneManager.CloseScene(scene, true);
            return ok;
        }
    }
}
