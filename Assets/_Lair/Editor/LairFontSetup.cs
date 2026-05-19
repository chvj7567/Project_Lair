using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Lair.EditorTools
{
    //# Slice B1 폴리시 — NotoSansKR-Regular.ttf 로 TMP_FontAsset SDF 자동 생성 + TMP default 교체.
    //# Dynamic 모드라 런타임에 사용되는 한글 글리프를 자동 SDF 생성. 별도 atlas pre-bake 불필요.
    public static class LairFontSetup
    {
        public const string SrcFontPath    = "Assets/TextMesh Pro/Fonts/Noto_Sans_KR/static/NotoSansKR-Regular.ttf";
        public const string FontAssetDir   = "Assets/_Lair/Data/Fonts";
        public const string FontAssetPath  = "Assets/_Lair/Data/Fonts/NotoSansKR SDF.asset";

        [MenuItem("Lair/Setup/Fix - Create NotoSansKR SDF")]
        public static void CreateNotoSansKRSDF()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SrcFontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[LairFontSetup] 폰트 미발견: {SrcFontPath}");
                return;
            }

            EnsureDir(FontAssetDir);

            //# 기존 자산이 있으면 atlas/material sub-asset 누락 시 재생성을 위해 삭제 후 새로 생성
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(FontAssetPath);
                Debug.Log("[LairFontSetup] 기존 NotoSansKR SDF 삭제 후 재생성");
            }

            //# Dynamic 모드 — 런타임에 새 글리프 자동 생성. 한글 어떤 글자든 OK.
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            //# Atlas Texture / Material 을 같은 .asset 의 sub-asset 으로 저장 — 도메인 리로드 후 reference 유지
            if (fontAsset.atlasTextures != null)
            {
                foreach (var tex in fontAsset.atlasTextures)
                {
                    if (tex != null && !AssetDatabase.Contains(tex))
                    {
                        tex.name = "Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
            }
            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = "Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath);
            Debug.Log($"[LairFontSetup] NotoSansKR SDF 생성 + Atlas/Material sub-asset 등록: {FontAssetPath}");

            //# TMP_Settings.defaultFontAsset 교체 (인스펙터에서 변경하는 것과 동등)
            var settings = TMP_Settings.instance;
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = fontAsset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(settings);
                    Debug.Log("[LairFontSetup] TMP_Settings.defaultFontAsset → NotoSansKR SDF");
                }
                else
                {
                    Debug.LogWarning("[LairFontSetup] TMP_Settings.m_defaultFontAsset 필드 미발견");
                }
            }
            else
            {
                Debug.LogWarning("[LairFontSetup] TMP_Settings.instance 가 null — TMP Essentials 가 import 됐는지 확인");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
