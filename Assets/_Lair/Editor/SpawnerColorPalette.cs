using System.IO;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 스포너 디스크 색상 단일 진실 — EMonster→hex 6종 + Mat_Spawner_{type}.mat 생성/로드.
    //# LairSpawnerVisualBuilder 와 CircularSpawnerArrangerEditor 가 공유 (색상표 중복 정의 금지).
    public static class SpawnerColorPalette
    {
        private const string MaterialDir      = "Assets/_Lair/Art/Materials";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        //# EMonster 순서(0=Wisp, 1=Wraith, 2=Reaper, 3=Hex, 4=Plague, 5=Phantom)와 1:1 대응.
        private static readonly (EMonster Type, string ColorHex)[] ColorTable = new[]
        {
            (EMonster.Wisp,    "#22C55E"),
            (EMonster.Wraith,  "#6B7280"),
            (EMonster.Reaper,  "#EF4444"),
            (EMonster.Hex,     "#EAB308"),
            (EMonster.Plague,  "#A855F7"),
            (EMonster.Phantom, "#1F2937"),
        };

        //# EMonster 인덱스로 정렬된 머티리얼 배열 (SpawnerBody._materials 에 그대로 주입).
        public static int Count => ColorTable.Length;

        //# 6종 머티리얼 생성 또는 로드 — EMonster 순서 인덱스 배열로 반환 (idempotent).
        public static Material[] EnsureSpawnerMaterials()
        {
            if (Directory.Exists(MaterialDir) == false)
            {
                Directory.CreateDirectory(MaterialDir);
                AssetDatabase.Refresh();
            }

            Material[] mats = new Material[ColorTable.Length];
            for (int i = 0; i < ColorTable.Length; i++)
            {
                (EMonster type, string hex) = ColorTable[i];
                string matPath = $"{MaterialDir}/Mat_Spawner_{type}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find(UrpLitShaderName));
                    if (ColorUtility.TryParseHtmlString(hex, out Color color))
                    {
                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", color);
                        mat.color = color;
                    }
                    AssetDatabase.CreateAsset(mat, matPath);
                    Debug.Log($"[SpawnerColorPalette] 머티리얼 생성: {matPath}");
                }
                mats[i] = mat;
            }
            return mats;
        }
    }
}
