using System;
using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Character;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools.OneShot
{
    //# 일회용 FX 빌더 (Rule 04 §3) — 궁수 4종 + 스테이지 규칙 6종 FX 프리팹을 한 번 만든 뒤 삭제한다. 기획서 chapter-system §3.4·§5.4.
    //# 도트 시트는 코드로 절차 생성(Texture2D 픽셀 드로잉 → PNG → Point/무압축 슬라이스 임포트, Art/Sprites/FX2D/<이름>_Sheet.png),
    //# 프리팹은 기존 FX(HeroNovaFx·TimeStopShield) 구조 그대로 — 루트(CHPoolable + SpriteSheetFx) + 자식 AuraFx(SpriteRenderer, Mat_Monster2DUnlit).
    //# 기존 에셋은 덮어쓰지 않는다(프리팹/시트 PNG 가 이미 있으면 LogError 후 건너뜀). Resources/·Instantiate 직접 호출 없음 — 런타임은 CHMPool.
    //#
    //# 스케일 규약(소비 코드와 일치):
    //#  - 지면 원형 FX(Volley/Evade/Spring/ArrowRain/Trap): 스프라이트 폭 = 1 월드 단위(PPU = 셀 폭) → 호출부가 루트 스케일 = 지름(반경 x 2)으로 균일 스케일.
    //#  - 직선 FX(ArcherArrow/PiercingShot): 로컬 +Z 길이 1 단위, 폭은 월드 단위 고정 → HeroSkillFx.SpawnLineRequired 가 루트 스케일 (1, 1, length).
    //#  - 고정 크기 FX(Fog/SealChain/Bulwark): 크기를 프리팹 자식 스케일에 굽고 호출부는 루트 스케일 1.
    public static class ArcherFxBuilder
    {
        private const string FxDir = "Assets/_Lair/Art/FX/";
        private const string SheetDir = "Assets/_Lair/Art/Sprites/FX2D/";
        private const string MaterialPath = "Assets/_Lair/Art/Materials/Mat_Monster2DUnlit.mat";
        private const string ReferencePrefabPath = FxDir + "HeroNovaFx.prefab";
        private const string ReferenceSheetPath = SheetDir + "HeroNovaFx_Sheet.png";
        private const string AddressableLabel = "Resource";

        //# 배치 방식.
        private enum ELayout
        {
            FlatUnit,    //# 지면에 눕힘, 폭 1 단위 (호출부가 지름으로 루트 스케일)
            FlatFixed,   //# 지면에 눕힘, 자식 스케일에 크기 고정
            Line,        //# 지면에 눕힘, 로컬 +Z 길이 1 단위(피벗 하단 중앙), 폭 고정
            Billboard,   //# 카메라 정면(SpriteSheetFx._billboard), 자식 스케일/오프셋 고정
        }

        private sealed class FxSpec
        {
            public string Name;
            public int CellW;
            public int CellH;
            public float Ppu;
            public Vector2 Pivot = new Vector2(0.5f, 0.5f);
            public int Frames;
            public bool Loop;
            public bool AutoReturn;
            public ELayout Layout;
            public float ChildScaleX = 1f;
            public float ChildScaleY = 1f;
            public float ChildOffsetY;
            public int SortingOrder;
            public Action<Canvas, int, int> Draw;
        }

        //# 팔레트 — 챕터 2 어둠의 숲(황혼 보라/이끼 녹색) + 영웅 아트 A안. 전장 위 몬스터와 구분되도록 밝은 림 우선.
        private static readonly Color32 Moss = new Color32(163, 230, 53, 255);
        private static readonly Color32 DeepMoss = new Color32(77, 124, 15, 255);
        private static readonly Color32 Lavender = new Color32(196, 181, 253, 255);
        private static readonly Color32 Violet = new Color32(124, 58, 237, 255);
        private static readonly Color32 Amber = new Color32(251, 191, 36, 255);
        private static readonly Color32 Gold = new Color32(253, 224, 71, 255);
        private static readonly Color32 White = new Color32(248, 250, 252, 255);
        private static readonly Color32 Teal = new Color32(94, 234, 212, 255);
        private static readonly Color32 Water = new Color32(125, 211, 252, 255);
        private static readonly Color32 Danger = new Color32(248, 113, 113, 255);
        private static readonly Color32 Iron = new Color32(148, 163, 184, 255);
        private static readonly Color32 DarkIron = new Color32(51, 65, 85, 255);
        private static readonly Color32 FogGrey = new Color32(156, 176, 160, 255);
        private static readonly Color32 FogDusk = new Color32(150, 128, 184, 255);

        [MenuItem("Lair/OneShot/Build Archer FX")]
        public static void Run()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError($"[ArcherFxBuilder] 머티리얼 없음: {MaterialPath} - 중단");
                return;
            }
            TextureImporter referenceImporter = AssetImporter.GetAtPath(ReferenceSheetPath) as TextureImporter;
            if (referenceImporter == null)
            {
                Debug.LogError($"[ArcherFxBuilder] 기준 시트 임포터 없음: {ReferenceSheetPath} - 중단");
                return;
            }

            AddressableAssetGroup group = FindAddressableGroup();
            foreach (FxSpec spec in BuildSpecs())
            {
                BuildOne(spec, material, referenceImporter, group);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArcherFxBuilder] 완료 - 프리팹 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# ---- FX 목록 + 규약 ----
        private static List<FxSpec> BuildSpecs()
        {
            return new List<FxSpec>
            {
                //# 영웅→타깃 직선 화살 — 1회 0.25s, 폭 0.25 단위. RangedHitFx 가 HeroSkillFx.SpawnLineRequired(길이=영웅~타깃 거리).
                new FxSpec { Name = "ArcherArrowFx", CellW = 8, CellH = 32, Ppu = 32f, Pivot = new Vector2(0.5f, 0f), Frames = 3,
                    AutoReturn = true, Layout = ELayout.Line, SortingOrder = 4, Draw = DrawArrow },
                //# 관통 사격 빔 — 1회 0.5s, 폭 0.75 단위. SpawnLineRequired(길이 9.0).
                new FxSpec { Name = "HeroPiercingShotFx", CellW = 24, CellH = 32, Ppu = 32f, Pivot = new Vector2(0.5f, 0f), Frames = 6,
                    AutoReturn = true, Layout = ELayout.Line, SortingOrder = 4, Draw = DrawPiercing },
                //# 화살 소나기 — 1회 1.5s(3타 0.5s 간격 + 여운), 루트 스케일 = 반경 2.8 x 2 = 5.6.
                new FxSpec { Name = "HeroArrowVolleyFx", CellW = 96, CellH = 96, Ppu = 96f, Frames = 18,
                    AutoReturn = true, Layout = ELayout.FlatUnit, SortingOrder = -10, Draw = DrawVolley },
                //# 회피 도약 먼지 — 1회 0.67s, 루트 스케일 = 반경 2.0 x 2 = 4.0.
                new FxSpec { Name = "HeroEvadeFx", CellW = 96, CellH = 96, Ppu = 96f, Frames = 8,
                    AutoReturn = true, Layout = ELayout.FlatUnit, SortingOrder = -10, Draw = DrawEvade },
                //# 성수 샘 — 루프, 개방 구간 동안 규칙이 Release. 루트 스케일 = 반경 3.0 x 2 = 6.0.
                new FxSpec { Name = "StageSpringFx", CellW = 96, CellH = 96, Ppu = 96f, Frames = 12, Loop = true,
                    Layout = ELayout.FlatUnit, SortingOrder = -10, Draw = DrawSpring },
                //# 짙은 안개 — 루프, 전장 전체(30 x 18.75 단위) 고정 크기, 알파 최대 ~0.18. 루트 스케일 1, 캐릭터 아래(order -9).
                new FxSpec { Name = "StageFogFx", CellW = 128, CellH = 80, Ppu = 128f, Frames = 16, Loop = true,
                    Layout = ELayout.FlatFixed, ChildScaleX = 30f, ChildScaleY = 30f, SortingOrder = -9, Draw = DrawFog },
                //# 봉인 사슬 — 루프, 스포너 위 고정 크기 1.8 단위(제단 위로 0.6). 루트 스케일 1.
                new FxSpec { Name = "StageSealChainFx", CellW = 64, CellH = 64, Ppu = 64f, Frames = 8, Loop = true,
                    Layout = ELayout.Billboard, ChildScaleX = 1.8f, ChildScaleY = 1.8f, ChildOffsetY = 0.6f, SortingOrder = 6, Draw = DrawSealChain },
                //# 수호 결계 구체 — 루프, 영웅 추적(SetPosition), 고정 크기 2.8 단위(영웅 가슴 높이 1.0). 루트 스케일 1.
                new FxSpec { Name = "StageBulwarkFx", CellW = 64, CellH = 64, Ppu = 64f, Frames = 12, Loop = true,
                    Layout = ELayout.Billboard, ChildScaleX = 2.8f, ChildScaleY = 2.8f, ChildOffsetY = 1.0f, SortingOrder = 10, Draw = DrawBulwark },
                //# 화살비 — 1회 3.0s(예고 1.0s → 4틱 0.5s 간격 → 소멸), 마지막 프레임 투명으로 유지(규칙이 Release). 루트 스케일 = 반경 3.5 x 2 = 7.0.
                new FxSpec { Name = "StageArrowRainFx", CellW = 96, CellH = 96, Ppu = 96f, Frames = 36,
                    Layout = ELayout.FlatUnit, SortingOrder = -10, Draw = DrawArrowRain },
                //# 사냥꾼 덫 — 루프, 12초 존속(규칙이 Release). 루트 스케일 = 덫 반경 1.2 x 2 = 2.4.
                new FxSpec { Name = "StageTrapFx", CellW = 64, CellH = 64, Ppu = 64f, Frames = 8, Loop = true,
                    Layout = ELayout.FlatUnit, SortingOrder = -10, Draw = DrawTrap },
            };
        }

        //# ---- 빌드 한 건: 시트 → 임포트 → 프리팹 → Addressables ----
        private static void BuildOne(FxSpec spec, Material material, TextureImporter reference, AddressableAssetGroup group)
        {
            string prefabPath = FxDir + spec.Name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.LogError($"[ArcherFxBuilder] 이미 존재 - 덮어쓰지 않고 건너뜀: {prefabPath}");
                return;
            }

            string sheetPath = SheetDir + spec.Name + "_Sheet.png";
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), sheetPath)) == false)
            {
                WriteSheet(spec, sheetPath);
                ImportSheet(spec, sheetPath, reference);
            }
            else
            {
                Debug.LogError($"[ArcherFxBuilder] 시트 PNG 가 이미 있어 재사용(덮어쓰지 않음): {sheetPath}");
            }

            Sprite[] frames = LoadFrames(spec, sheetPath);
            if (frames == null)
                return;

            GameObject root = new GameObject(spec.Name, typeof(CHPoolable), typeof(SpriteSheetFx));
            GameObject child = new GameObject("AuraFx", typeof(SpriteRenderer));
            child.transform.SetParent(root.transform, false);
            ApplyLayout(spec, child.transform);

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sharedMaterial = material;
            renderer.sortingOrder = spec.SortingOrder;

            SerializedObject fx = new SerializedObject(root.GetComponent<SpriteSheetFx>());
            fx.FindProperty("_renderer").objectReferenceValue = renderer;
            SerializedProperty frameArray = fx.FindProperty("_frames");
            frameArray.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; ++i)
            {
                frameArray.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
            fx.FindProperty("_fps").floatValue = 12f;
            fx.FindProperty("_loop").boolValue = spec.Loop;
            fx.FindProperty("_billboard").boolValue = spec.Layout == ELayout.Billboard;
            fx.FindProperty("_autoPlay").boolValue = true;
            fx.FindProperty("_returnToPoolOnFinish").boolValue = spec.AutoReturn;
            fx.FindProperty("_poolable").objectReferenceValue = root.GetComponent<CHPoolable>();
            fx.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            RegisterAddressable(saved, prefabPath, spec.Name, group);
        }

        //# 자식 AuraFx 배치 — 지면 FX 는 X축 90° 로 눕혀 스프라이트 +Y 가 월드 +Z 가 되게 한다(기존 HeroNovaFx 와 동일 회전).
        private static void ApplyLayout(FxSpec spec, Transform child)
        {
            switch (spec.Layout)
            {
                case ELayout.FlatUnit:
                case ELayout.Line:
                    child.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    child.localScale = Vector3.one;
                    break;
                case ELayout.FlatFixed:
                    child.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    child.localScale = new Vector3(spec.ChildScaleX, spec.ChildScaleY, 1f);
                    break;
                default:
                    child.localRotation = Quaternion.identity;
                    child.localScale = new Vector3(spec.ChildScaleX, spec.ChildScaleY, 1f);
                    child.localPosition = new Vector3(0f, spec.ChildOffsetY, 0f);
                    break;
            }
        }

        //# ---- 시트 PNG + 임포트 ----
        private static void WriteSheet(FxSpec spec, string sheetPath)
        {
            int cols = Mathf.Min(6, spec.Frames);
            int rows = (spec.Frames + cols - 1) / cols;
            int width = cols * spec.CellW;
            int height = rows * spec.CellH;
            Color32[] sheet = new Color32[width * height];

            for (int f = 0; f < spec.Frames; ++f)
            {
                Canvas canvas = new Canvas(spec.CellW, spec.CellH);
                spec.Draw(canvas, f, spec.Frames);
                int col = f % cols;
                int row = f / cols;
                for (int y = 0; y < spec.CellH; ++y)
                {
                    for (int x = 0; x < spec.CellW; ++x)
                    {
                        //# 캔버스 y 는 위→아래, 텍스처 y 는 아래→위.
                        int texX = col * spec.CellW + x;
                        int texY = height - 1 - (row * spec.CellH + y);
                        sheet[texY * width + texX] = canvas.Pixels[y * spec.CellW + x];
                    }
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(sheet);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), sheetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(sheetPath);
        }

        private static void ImportSheet(FxSpec spec, string sheetPath, TextureImporter reference)
        {
            TextureImporter importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[ArcherFxBuilder] 임포터 없음: {sheetPath}");
                return;
            }

            //# 기존 FX 시트(HeroNovaFx_Sheet) 설정 복제 — Sprite(Multiple)·Point·무압축·알파 투명 — 후 PPU/슬라이스만 교체.
            TextureImporterSettings settings = new TextureImporterSettings();
            reference.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = spec.Ppu;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;

            int cols = Mathf.Min(6, spec.Frames);
            int rows = (spec.Frames + cols - 1) / cols;
            int height = rows * spec.CellH;
            SpriteMetaData[] metas = new SpriteMetaData[spec.Frames];
            for (int f = 0; f < spec.Frames; ++f)
            {
                int col = f % cols;
                int row = f / cols;
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"{spec.Name}_Sheet_{f}";
                meta.rect = new Rect(col * spec.CellW, height - (row + 1) * spec.CellH, spec.CellW, spec.CellH);
                meta.alignment = (int)SpriteAlignment.Custom;
                meta.pivot = spec.Pivot;
                metas[f] = meta;
            }
#pragma warning disable 618
            importer.spritesheet = metas;
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadFrames(FxSpec spec, string sheetPath)
        {
            Sprite[] frames = new Sprite[spec.Frames];
            string prefix = spec.Name + "_Sheet_";
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                Sprite sprite = asset as Sprite;
                if (sprite == null || sprite.name.StartsWith(prefix) == false)
                    continue;
                if (int.TryParse(sprite.name.Substring(prefix.Length), out int index) && index >= 0 && index < frames.Length)
                {
                    frames[index] = sprite;
                }
            }
            for (int i = 0; i < frames.Length; ++i)
            {
                if (frames[i] == null)
                {
                    Debug.LogError($"[ArcherFxBuilder] {spec.Name} 프레임 {i} 스프라이트 없음 - 프리팹 생성 중단");
                    return null;
                }
            }
            return frames;
        }

        //# ---- Addressables: HeroNovaFx 와 같은 그룹, 주소 = 파일명, 라벨 Resource ----
        private static AddressableAssetGroup FindAddressableGroup()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return null;
            AddressableAssetEntry reference = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(ReferencePrefabPath));
            return reference != null ? reference.parentGroup : settings.FindGroup("Resource");
        }

        private static void RegisterAddressable(GameObject prefab, string prefabPath, string address, AddressableAssetGroup group)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || group == null)
            {
                Debug.LogError($"[ArcherFxBuilder] Addressables 설정/그룹 없음 - {address} 수동 등록 필요");
                return;
            }
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath), group);
            entry.address = address;
            entry.SetLabel(AddressableLabel, true);
            EditorUtility.SetDirty(settings);
        }

        //# ================= 픽셀 드로잉 =================
        //# 캔버스 좌표: x 오른쪽, y 아래(이미지 좌표). 직선 알파 합성(over).
        private sealed class Canvas
        {
            public readonly int W;
            public readonly int H;
            public readonly Color32[] Pixels;

            public Canvas(int w, int h)
            {
                W = w;
                H = h;
                Pixels = new Color32[w * h];
            }

            public void Plot(int x, int y, Color32 color, float alpha)
            {
                if (x < 0 || y < 0 || x >= W || y >= H)
                    return;
                float sa = Mathf.Clamp01(alpha * (color.a / 255f));
                if (sa <= 0f)
                    return;
                Color32 dst = Pixels[y * W + x];
                float da = dst.a / 255f;
                float oa = sa + da * (1f - sa);
                float r = (color.r * sa + dst.r * da * (1f - sa)) / oa;
                float g = (color.g * sa + dst.g * da * (1f - sa)) / oa;
                float b = (color.b * sa + dst.b * da * (1f - sa)) / oa;
                Pixels[y * W + x] = new Color32((byte)Mathf.Round(r), (byte)Mathf.Round(g), (byte)Mathf.Round(b), (byte)Mathf.Round(oa * 255f));
            }

            //# 중심 (cx,cy), 반경 r 원판 — 색은 중심 0 ~ 가장자리 1 의 거리비로 (색, 알파) 반환.
            public void Disk(float cx, float cy, float r, Func<float, KeyValuePair<Color32, float>> shade)
            {
                int x0 = Mathf.FloorToInt(cx - r);
                int x1 = Mathf.CeilToInt(cx + r);
                int y0 = Mathf.FloorToInt(cy - r);
                int y1 = Mathf.CeilToInt(cy + r);
                for (int y = y0; y <= y1; ++y)
                {
                    for (int x = x0; x <= x1; ++x)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        if (d > r)
                            continue;
                        KeyValuePair<Color32, float> s = shade(d / r);
                        Plot(x, y, s.Key, s.Value);
                    }
                }
            }

            public void Fill(float cx, float cy, float r, Color32 color, float alpha)
            {
                Disk(cx, cy, r, t => new KeyValuePair<Color32, float>(color, alpha));
            }

            //# 두께 thick 의 링(도넛).
            public void Ring(float cx, float cy, float r, float thick, Color32 color, float alpha)
            {
                float outer = r + thick * 0.5f;
                float inner = r - thick * 0.5f;
                int x0 = Mathf.FloorToInt(cx - outer);
                int x1 = Mathf.CeilToInt(cx + outer);
                int y0 = Mathf.FloorToInt(cy - outer);
                int y1 = Mathf.CeilToInt(cy + outer);
                for (int y = y0; y <= y1; ++y)
                {
                    for (int x = x0; x <= x1; ++x)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        if (d >= inner && d <= outer)
                        {
                            Plot(x, y, color, alpha);
                        }
                    }
                }
            }

            //# 호(링의 일부) — 각도(도, 0=오른쪽, 시계 방향) [from, to] 구간만.
            public void Arc(float cx, float cy, float r, float thick, float fromDeg, float toDeg, Color32 color, float alpha)
            {
                float outer = r + thick * 0.5f;
                float inner = r - thick * 0.5f;
                int x0 = Mathf.FloorToInt(cx - outer);
                int x1 = Mathf.CeilToInt(cx + outer);
                int y0 = Mathf.FloorToInt(cy - outer);
                int y1 = Mathf.CeilToInt(cy + outer);
                for (int y = y0; y <= y1; ++y)
                {
                    for (int x = x0; x <= x1; ++x)
                    {
                        float dx = x + 0.5f - cx;
                        float dy = y + 0.5f - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d < inner || d > outer)
                            continue;
                        float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        float rel = Mathf.Repeat(a - fromDeg, 360f);
                        if (rel <= Mathf.Repeat(toDeg - fromDeg, 360f))
                        {
                            Plot(x, y, color, alpha);
                        }
                    }
                }
            }

            public void Line(float x0, float y0, float x1, float y1, float thick, Color32 color, float alpha)
            {
                float length = Mathf.Max(1f, Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)));
                int steps = Mathf.CeilToInt(length * 2f);
                for (int i = 0; i <= steps; ++i)
                {
                    float t = i / (float)steps;
                    float x = Mathf.Lerp(x0, x1, t);
                    float y = Mathf.Lerp(y0, y1, t);
                    if (thick <= 1.5f)
                    {
                        Plot(Mathf.FloorToInt(x), Mathf.FloorToInt(y), color, alpha);
                    }
                    else
                    {
                        Fill(x, y, thick * 0.5f, color, alpha);
                    }
                }
            }

            public void Rect(int x, int y, int w, int h, Color32 color, float alpha)
            {
                for (int yy = y; yy < y + h; ++yy)
                {
                    for (int xx = x; xx < x + w; ++xx)
                    {
                        Plot(xx, yy, color, alpha);
                    }
                }
            }
        }

        private static KeyValuePair<Color32, float> S(Color32 color, float alpha)
        {
            return new KeyValuePair<Color32, float>(color, alpha);
        }

        //# 결정적 의사난수 0~1 — 같은 (시드, 인덱스)는 항상 같은 값(프레임 간 일관).
        private static float Hash(int seed, int index)
        {
            uint h = (uint)(seed * 73856093) ^ (uint)(index * 19349663 + 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        //# ---- 1) 화살: 위(y=0)가 타깃 쪽 끝. 꼬리(아래)는 투명 → 머리(위)는 밝게. 3프레임 페이드. ----
        private static void DrawArrow(Canvas c, int f, int n)
        {
            float fade = f == 0 ? 1f : (f == 1 ? 0.6f : 0.25f);
            for (int y = 0; y < c.H; ++y)
            {
                float t = y / (float)(c.H - 1);          //# 0 = 머리, 1 = 꼬리
                float body = Mathf.Pow(1f - t, 1.4f);
                c.Plot(3, y, White, body * fade);
                c.Plot(4, y, Moss, body * fade);
                c.Plot(2, y, Moss, body * 0.45f * fade);
                c.Plot(5, y, Moss, body * 0.45f * fade);
            }
            //# 화살촉
            c.Plot(3, 0, White, fade);
            c.Plot(4, 0, White, fade);
            c.Plot(2, 2, White, fade);
            c.Plot(5, 2, White, fade);
        }

        //# ---- 2) 관통 빔: 폭 24 고정, 길이 방향으로 맥동 밴드가 위로 흐름. 6프레임. ----
        private static void DrawPiercing(Canvas c, int f, int n)
        {
            float[] widthScale = { 1f, 1f, 0.8f, 0.6f, 0.4f, 0.2f };
            float[] alphaScale = { 1f, 1f, 0.85f, 0.65f, 0.4f, 0.15f };
            float w = widthScale[f];
            float a = alphaScale[f];
            float cx = c.W * 0.5f;
            for (int y = 0; y < c.H; ++y)
            {
                float t = y / (float)(c.H - 1);          //# 0 = 끝(타깃 쪽), 1 = 시작(영웅)
                float taper = Mathf.Lerp(0.55f, 1f, t);   //# 끝으로 갈수록 가늘게
                bool band = ((y + f * 5) % 8) < 2;        //# 위로 흐르는 밝은 밴드
                for (int x = 0; x < c.W; ++x)
                {
                    float d = Mathf.Abs(x + 0.5f - cx);
                    if (d <= 2.2f * w * taper)
                    {
                        c.Plot(x, y, band ? White : Lavender, a);
                    }
                    else if (d <= 6f * w * taper)
                    {
                        c.Plot(x, y, Moss, 0.8f * a);
                    }
                    else if (d <= 11f * w * taper)
                    {
                        c.Plot(x, y, Violet, 0.35f * a);
                    }
                }
            }
        }

        //# ---- 3) 화살 소나기: 반경 5.6/2=2.8 → 지름 = 셀 폭. 3타(프레임 0/6/12) 낙하 + 임팩트, 쌓이는 화살, 마지막 2프레임 페이드. ----
        private static void DrawVolley(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float fade = f >= 17 ? 0.2f : (f >= 16 ? 0.5f : 1f);

            c.Ring(cx, cy, 45f, 2f, Moss, 0.35f * fade);
            c.Fill(cx, cy, 44f, Moss, 0.07f * fade);

            int[] bursts = { 0, 6, 12 };
            for (int k = 0; k < bursts.Length; ++k)
            {
                int p = f - bursts[k];
                bool stuck = p > 2;   //# 착탄한 화살은 박힌 채 남는다
                if (p < 0)
                    continue;
                for (int i = 0; i < 12; ++i)
                {
                    float angle = Hash(k + 1, i) * Mathf.PI * 2f;
                    float radius = Mathf.Sqrt(Hash(k + 11, i)) * 38f;
                    float ax = cx + Mathf.Cos(angle) * radius;
                    float ay = cy + Mathf.Sin(angle) * radius;
                    float drop = stuck ? 0f : (2 - p) * 16f;
                    float tipY = ay - drop;
                    if (stuck)
                    {
                        c.Line(ax, tipY, ax - 1.5f, tipY - 7f, 1f, Lavender, 0.55f * fade);
                        c.Plot(Mathf.FloorToInt(ax), Mathf.FloorToInt(tipY), White, 0.7f * fade);
                    }
                    else
                    {
                        c.Line(ax, tipY, ax - 2.5f, tipY - 14f, 1f, White, 0.9f * fade);
                        c.Plot(Mathf.FloorToInt(ax), Mathf.FloorToInt(tipY), Moss, fade);
                    }
                }
                if (p <= 3)
                {
                    c.Ring(cx, cy, 14f + p * 10f, 2f, Amber, (1f - p * 0.22f) * fade);
                }
            }
        }

        //# ---- 4) 회피 먼지: 지름 = 셀 폭(반경 2.0 → 4.0). 중심 섬광 + 바깥으로 퍼지는 먼지 뭉치 14개. 8프레임. ----
        private static void DrawEvade(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float t = f / (float)(n - 1);
            if (f < 3)
            {
                c.Fill(cx, cy, 18f - f * 5f, White, 0.9f - f * 0.25f);
            }
            float ringRadius = 12f + f * 4.8f;
            for (int i = 0; i < 14; ++i)
            {
                float angle = (i / 14f) * Mathf.PI * 2f + Hash(5, i) * 0.3f;
                float jitter = 0.85f + Hash(6, i) * 0.3f;
                float px = cx + Mathf.Cos(angle) * ringRadius * jitter;
                float py = cy + Mathf.Sin(angle) * ringRadius * jitter;
                float puff = 5f + Hash(7, i) * 3f + f * 0.5f;
                float alpha = Mathf.Clamp01(1f - t * 0.95f);
                c.Disk(px, py, puff, d => S(Color32.Lerp(White, Lavender, d), alpha * (0.85f - d * 0.45f)));
            }
            c.Ring(cx, cy, ringRadius, 1.5f, Lavender, 0.6f * (1f - t));
        }

        //# ---- 5) 성수 샘: 지름 = 셀 폭(반경 3.0 → 6.0). 맥동 링 + 회전 점선 + 떠오르는 물방울. 12프레임 이음매 없는 루프. ----
        private static void DrawSpring(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float phase = f / (float)n;
            float pulse = 0.65f + 0.3f * Mathf.Sin(phase * Mathf.PI * 2f);

            c.Disk(cx, cy, 44f, d => S(Color32.Lerp(White, Water, d), 0.08f + (1f - d) * 0.12f));
            c.Ring(cx, cy, 45f, 3f, Teal, pulse);
            c.Ring(cx, cy, 44f, 1f, White, 0.5f);

            //# 점선 링 — 한 칸(360/16)만큼 12프레임 동안 회전 → 루프 경계에서 연속.
            float step = 360f / 16f;
            float offset = phase * step;
            for (int i = 0; i < 16; ++i)
            {
                float from = i * step + offset;
                c.Arc(cx, cy, 35f, 2f, from, from + step * 0.5f, Water, 0.75f);
            }

            //# 물방울 — 각자 위상으로 한 번씩 떠올라 사라짐(주기 = 루프 길이).
            for (int i = 0; i < 9; ++i)
            {
                float t = Mathf.Repeat(phase + i / 9f, 1f);
                float angle = Hash(9, i) * Mathf.PI * 2f;
                float radius = Mathf.Sqrt(Hash(10, i)) * 30f;
                float x = cx + Mathf.Cos(angle) * radius;
                float y = cy + Mathf.Sin(angle) * radius * 0.6f - t * 14f;
                float alpha = Mathf.Sin(t * Mathf.PI);
                c.Plot(Mathf.FloorToInt(x), Mathf.FloorToInt(y), White, alpha);
                c.Plot(Mathf.FloorToInt(x), Mathf.FloorToInt(y) + 1, Teal, alpha * 0.6f);
            }
        }

        //# ---- 6) 짙은 안개: 128x80 타일 가능 주기 노이즈(정수 주파수·정수 속도 → 16프레임 이음매 없음), 4단계 포스터라이즈, 알파 ≤ 0.18(46/255). ----
        private static void DrawFog(Canvas c, int f, int n)
        {
            float phase = f / (float)n;
            int[,] waves =
            {
                //# kx, ky, speed(정수, 부호 = 방향)
                { 2, 1, 1 }, { 1, 3, -1 }, { 3, 2, 1 }, { 4, 1, -2 },
            };
            float[] amplitude = { 0.45f, 0.3f, 0.2f, 0.12f };
            for (int y = 0; y < c.H; ++y)
            {
                for (int x = 0; x < c.W; ++x)
                {
                    float v = 0f;
                    for (int k = 0; k < waves.GetLength(0); ++k)
                    {
                        float arg = (waves[k, 0] * x / (float)c.W + waves[k, 1] * y / (float)c.H + waves[k, 2] * phase + k * 0.37f) * Mathf.PI * 2f;
                        v += amplitude[k] * Mathf.Sin(arg);
                    }
                    float level = Mathf.Clamp01((v + 0.55f) / 1.1f);
                    float q = Mathf.Floor(level * 4f) / 4f;   //# 도트 느낌의 4단계
                    float alpha = q * (46f / 255f);
                    Color32 tint = Color32.Lerp(FogGrey, FogDusk, Mathf.Clamp01(level * 1.2f - 0.2f));
                    c.Plot(x, y, tint, alpha);
                }
            }
        }

        //# ---- 7) 봉인 사슬: 제단을 X 자로 가로지르는 사슬 2줄 + 중앙 자물쇠, 보라 룬 맥동. 8프레임 루프. ----
        private static void DrawSealChain(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float phase = f / (float)n;
            float glow = 0.5f + 0.35f * Mathf.Sin(phase * Mathf.PI * 2f);

            c.Fill(cx, cy, 22f, Violet, 0.12f + glow * 0.12f);
            c.Ring(cx, cy, 26f, 2f, Violet, 0.35f + glow * 0.4f);

            DrawChainLine(c, 6f, 6f, cx - 5f, cy - 5f, 0);
            DrawChainLine(c, c.W - 6f, 6f, cx + 5f, cy - 5f, 1);
            DrawChainLine(c, 6f, c.H - 6f, cx - 5f, cy + 5f, 0);
            DrawChainLine(c, c.W - 6f, c.H - 6f, cx + 5f, cy + 5f, 1);

            //# 자물쇠: 몸통 + 걸쇠
            c.Rect((int)cx - 6, (int)cy - 2, 12, 10, DarkIron, 1f);
            c.Rect((int)cx - 5, (int)cy - 1, 10, 8, Amber, 1f);
            c.Rect((int)cx - 1, (int)cy + 2, 2, 3, DarkIron, 1f);
            c.Arc(cx, cy - 2f, 4f, 2f, 180f, 360f, Iron, 1f);
        }

        //# 사슬 한 줄 — 타원 고리를 번갈아 90° 돌려 이은 모양.
        private static void DrawChainLine(Canvas c, float x0, float y0, float x1, float y1, int seed)
        {
            int links = 5;
            for (int i = 0; i < links; ++i)
            {
                float t = (i + 0.5f) / links;
                float x = Mathf.Lerp(x0, x1, t);
                float y = Mathf.Lerp(y0, y1, t);
                bool horizontal = (i + seed) % 2 == 0;
                float rx = horizontal ? 4.5f : 2.5f;
                float ry = horizontal ? 2.5f : 4.5f;
                for (int yy = Mathf.FloorToInt(y - ry - 1); yy <= Mathf.CeilToInt(y + ry + 1); ++yy)
                {
                    for (int xx = Mathf.FloorToInt(x - rx - 1); xx <= Mathf.CeilToInt(x + rx + 1); ++xx)
                    {
                        float dx = (xx + 0.5f - x) / rx;
                        float dy = (yy + 0.5f - y) / ry;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d <= 1f && d >= 0.45f)
                        {
                            c.Plot(xx, yy, d > 0.75f ? DarkIron : Iron, 1f);
                        }
                    }
                }
            }
        }

        //# ---- 8) 수호 결계: 금빛 구체(반투명 판 + 림) + 육각 분할선 + 림을 도는 하이라이트. 12프레임, 하이라이트가 한 바퀴 → 이음매 없음. ----
        private static void DrawBulwark(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float phase = f / (float)n;
            float shimmer = 0.75f + 0.2f * Mathf.Sin(phase * Mathf.PI * 2f);

            c.Disk(cx, cy, 30f, d => S(Color32.Lerp(Gold, Amber, d), 0.06f + d * 0.22f));
            c.Ring(cx, cy, 30f, 3f, Gold, shimmer);
            c.Ring(cx, cy, 28f, 1f, White, 0.45f);

            //# 육각 분할선(구체 표면 패턴)
            for (int i = 0; i < 6; ++i)
            {
                float a0 = i * Mathf.PI / 3f;
                float a1 = (i + 1) * Mathf.PI / 3f;
                float r = 17f;
                c.Line(cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r, cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r, 1f, Amber, 0.5f);
                c.Line(cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r, cx + Mathf.Cos(a0) * 30f, cy + Mathf.Sin(a0) * 30f, 1f, Amber, 0.3f);
            }

            float highlight = phase * 360f;
            c.Arc(cx, cy, 30f, 4f, highlight, highlight + 55f, White, 1f);
            c.Arc(cx, cy, 27f, 2f, highlight + 5f, highlight + 40f, White, 0.6f);
        }

        //# ---- 9) 화살비(보스 규칙): 지름 = 셀 폭(반경 3.5 → 7.0). 0~1.0s 예고(위험 링 + 차오르는 면) → 4틱(프레임 12/18/24/30) 낙하 → 소멸, 마지막 프레임 투명. ----
        private static void DrawArrowRain(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            int[] ticks = { 12, 18, 24, 30 };

            //# 소멸 구간(33~35) 전체 페이드, 마지막 프레임은 완전 투명.
            float fade = f >= 35 ? 0f : (f >= 34 ? 0.3f : (f >= 33 ? 0.6f : 1f));
            if (fade <= 0f)
                return;

            //# 예고: 면 알파 0.08 → 0.28, 링은 깜빡임.
            float warnT = Mathf.Clamp01(f / 11f);
            float blink = f < 12 ? (f % 4 < 2 ? 1f : 0.55f) : 0.6f;
            c.Fill(cx, cy, 46f, Danger, (0.08f + warnT * 0.2f) * fade);
            c.Ring(cx, cy, 46f, 3f, Danger, blink * fade);
            c.Ring(cx, cy, 30f, 1f, Amber, 0.5f * fade);
            c.Line(cx - 6f, cy, cx + 6f, cy, 1f, Amber, 0.7f * fade);
            c.Line(cx, cy - 6f, cx, cy + 6f, 1f, Amber, 0.7f * fade);

            for (int k = 0; k < ticks.Length; ++k)
            {
                int p = f - ticks[k];
                if (p < 0)
                    continue;
                bool falling = p <= 2;
                for (int i = 0; i < 18; ++i)
                {
                    float angle = Hash(21 + k, i) * Mathf.PI * 2f;
                    float radius = Mathf.Sqrt(Hash(31 + k, i)) * 40f;
                    float ax = cx + Mathf.Cos(angle) * radius;
                    float ay = cy + Mathf.Sin(angle) * radius;
                    if (falling)
                    {
                        float tipY = ay - (2 - p) * 14f;
                        c.Line(ax, tipY, ax - 2.5f, tipY - 14f, 1f, White, 0.95f * fade);
                        c.Plot(Mathf.FloorToInt(ax), Mathf.FloorToInt(tipY), Amber, fade);
                    }
                    else
                    {
                        //# 착탄 후 박힌 화살 — 틱이 쌓일수록 늘어남.
                        c.Line(ax, ay, ax - 1.5f, ay - 7f, 1f, Lavender, 0.5f * fade);
                    }
                }
                if (p <= 2)
                {
                    c.Ring(cx, cy, 16f + p * 12f, 2f, Amber, (0.95f - p * 0.25f) * fade);
                }
            }
        }

        //# ---- 10) 사냥꾼 덫: 지름 = 셀 폭(반경 1.2 → 2.4). 안쪽으로 향한 쇠 이빨 링 + 붉은 보라 기운 맥동 + 이끼 점. 8프레임 루프. ----
        private static void DrawTrap(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float phase = f / (float)n;
            float pulse = 0.5f + 0.35f * Mathf.Sin(phase * Mathf.PI * 2f);

            c.Disk(cx, cy, 28f, d => S(Color32.Lerp(Danger, Violet, d), 0.1f + (1f - d) * 0.2f * pulse));
            c.Ring(cx, cy, 28f, 3f, DarkIron, 1f);
            c.Ring(cx, cy, 29f, 1f, Iron, 0.9f);

            int teeth = 10;
            for (int i = 0; i < teeth; ++i)
            {
                float angle = (i / (float)teeth) * Mathf.PI * 2f;
                float bx = cx + Mathf.Cos(angle) * 27f;
                float by = cy + Mathf.Sin(angle) * 27f;
                float tx = cx + Mathf.Cos(angle) * 15f;
                float ty = cy + Mathf.Sin(angle) * 15f;
                c.Line(bx, by, tx, ty, 3f, Iron, 1f);
                c.Line(bx, by, tx, ty, 1f, White, 0.7f);
            }

            //# 이끼 점
            for (int i = 0; i < 8; ++i)
            {
                float angle = Hash(41, i) * Mathf.PI * 2f;
                float radius = 22f + Hash(42, i) * 5f;
                c.Plot(Mathf.FloorToInt(cx + Mathf.Cos(angle) * radius), Mathf.FloorToInt(cy + Mathf.Sin(angle) * radius), Moss, 0.9f);
            }
            c.Fill(cx, cy, 3f, Danger, 0.5f + pulse * 0.4f);
        }
    }
}
