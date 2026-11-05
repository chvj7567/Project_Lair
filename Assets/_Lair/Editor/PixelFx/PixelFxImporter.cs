using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lair.Editor
{
    //# Tools/pixelfx/tools/export-unity.mjs 가 만든 PNG+JSON(Aseprite 형식)을 슬라이스하고 프리팹으로 만든다.
    public class PixelFxImporter : AssetPostprocessor
    {
        private const string SheetDir = "Assets/_Lair/Art/Sprites/PixelFx";
        private const string PrefabDir = "Assets/_Lair/Art/FX/PixelFx";
        private const float PixelsPerUnit = 16f;

        [Serializable] private class Rect4 { public int x, y, w, h; }
        [Serializable] private class Frame { public Rect4 frame; public int duration; }
        [Serializable] private class PixelFxMeta { public float fps; public bool loop; public int[] pivot; }
        [Serializable] private class SheetMeta { public PixelFxMeta pixelfx; }
        [Serializable] private class Sheet { public Frame[] frames; public SheetMeta meta; }

        private static Sheet Load(string pngPath)
        {
            string jsonPath = Path.ChangeExtension(pngPath, ".json");
            if (File.Exists(jsonPath) == false)
                return null;

            return JsonUtility.FromJson<Sheet>(File.ReadAllText(jsonPath));
        }

        private static bool IsSheet(string path) => path.StartsWith(SheetDir) && path.EndsWith(".png");

        private void OnPreprocessTexture()
        {
            if (IsSheet(assetPath) == false)
                return;

            Sheet sheet = Load(assetPath);
            if (sheet == null)
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            //# 시트 높이 = 마지막 프레임 줄의 바닥. Unity 는 y 가 아래→위라 뒤집는다.
            int texH = 0;
            foreach (Frame f in sheet.frames)
            {
                texH = Mathf.Max(texH, f.frame.y + f.frame.h);
            }

            int[] p = sheet.meta.pixelfx.pivot;
            SpriteMetaData[] metas = new SpriteMetaData[sheet.frames.Length];
            for (int i = 0; i < metas.Length; i++)
            {
                Rect4 r = sheet.frames[i].frame;
                metas[i] = new SpriteMetaData
                {
                    name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{i}",
                    rect = new Rect(r.x, texH - r.y - r.h, r.w, r.h),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2((float)p[0] / r.w, 1f - (float)p[1] / r.h),
                };
            }

#pragma warning disable CS0618 //# ISpriteEditorDataProvider 는 2D Sprite 패키지 필요 — 의존 없이 구 API 사용
            importer.spritesheet = metas;
#pragma warning restore CS0618
        }

        //# JSON 만 바뀌어도 슬라이스가 갱신되도록 PNG 를 재임포트.
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (path.StartsWith(SheetDir) && path.EndsWith(".json"))
                {
                    AssetDatabase.ImportAsset(Path.ChangeExtension(path, ".png"), ImportAssetOptions.ForceUpdate);
                }
            }
        }

        [MenuItem("Lair/PixelFx/Build Prefabs")]
        public static void BuildPrefabs()
        {
            Directory.CreateDirectory(PrefabDir);
            int made = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SheetDir }))
            {
                string png = AssetDatabase.GUIDToAssetPath(guid);
                Sheet sheet = Load(png);
                if (sheet == null)
                    continue;

                string name = Path.GetFileNameWithoutExtension(png);
                Sprite[] frames = new Sprite[sheet.frames.Length];
                foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(png))
                {
                    if (o is Sprite s && int.TryParse(s.name.Substring(name.Length + 1), out int idx) && idx < frames.Length)
                    {
                        frames[idx] = s;
                    }
                }

                GameObject go = new GameObject(name);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                PixelFxPlayer player = go.AddComponent<PixelFxPlayer>();
                SerializedObject so = new SerializedObject(player);
                so.FindProperty("_renderer").objectReferenceValue = sr;
                so.FindProperty("_loop").boolValue = sheet.meta.pixelfx.loop;
                SerializedProperty fr = so.FindProperty("_frames");
                SerializedProperty du = so.FindProperty("_durations");
                fr.arraySize = frames.Length;
                du.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                {
                    fr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                    du.GetArrayElementAtIndex(i).floatValue = sheet.frames[i].duration / 1000f;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
                UnityEngine.Object.DestroyImmediate(go);
                made++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFx] 프리팹 {made}개 생성/갱신 -> {PrefabDir}");
        }
    }
}
