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
    //# 일회용 FX 빌더 (Rule 04 §3) — 암살자 FX 프리팹 5종을 한 번 만든 뒤 삭제한다. 기획서 chapter-3-hero §4·§6·§7.1.
    //# 실행: Unity -batchmode -nographics -quit -projectPath <경로> -executeMethod Lair.EditorTools.OneShot.AssassinFxBuilder.Run
    //# 도트 시트는 코드로 절차 생성(Texture2D 픽셀 드로잉 → PNG → Point/무압축 슬라이스 임포트, Art/Sprites/FX2D/<이름>_Sheet.png),
    //# 프리팹은 기존 FX(HeroNovaFx·궁수 FX) 구조 그대로 — 루트(CHPoolable + SpriteSheetFx) + 자식 AuraFx(SpriteRenderer, Mat_Monster2DUnlit).
    //# 기존 에셋은 덮어쓰지 않는다(프리팹/시트 PNG 가 이미 있으면 LogError 후 건너뜀). Resources/·Instantiate 직접 호출 없음 — 런타임은 CHMPool.
    //#
    //# 스케일 규약(소비 코드와 일치):
    //#  - AssassinSlashFx: RangedHitFx → HeroSkillFx.SpawnLineRequired — 로컬 +Z 길이 1 단위(피벗 하단 중앙), 폭 0.5 고정, 루트 스케일 (1, 1, 영웅~표적 거리).
    //#  - HeroSlipFx / HeroShadowStrikeFx: 호출부가 균일 스케일 1 로 스폰 → 크기를 프리팹 자식 스케일에 굽는다(2.4 / 2.6 단위).
    //#  - HeroBladeWhirlFx: 호출부 스케일 = radius 2.4 x 2 = 4.8 → 스프라이트 폭 = 1 월드 단위(PPU = 셀 폭).
    //#  - HeroSmokeBombFx: 호출부 스케일 = radius 3.0 x 2 = 6.0 → 스프라이트 폭 = 1 월드 단위.
    public static class AssassinFxBuilder
    {
        private const string FxDir = "Assets/_Lair/Art/FX/";
        private const string SheetDir = "Assets/_Lair/Art/Sprites/FX2D/";
        private const string MaterialPath = "Assets/_Lair/Art/Materials/Mat_Monster2DUnlit.mat";
        private const string ReferencePrefabPath = FxDir + "HeroNovaFx.prefab";
        private const string ReferenceSheetPath = SheetDir + "HeroNovaFx_Sheet.png";
        private const string AddressableLabel = "Resource";

        private enum ELayout
        {
            FlatUnit,    //# 지면에 눕힘, 폭 1 단위 (호출부가 지름으로 루트 스케일)
            FlatFixed,   //# 지면에 눕힘, 자식 스케일에 크기 고정 (호출부 루트 스케일 1)
            Line,        //# 지면에 눕힘, 로컬 +Z 길이 1 단위(피벗 하단 중앙), 폭 고정
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
            public bool AutoReturn = true;
            public ELayout Layout;
            public float ChildScale = 1f;
            public int SortingOrder;
            public Action<Canvas, int, int> Draw;
        }

        //# 팔레트 — 챕터 3 영웅 아트 A안(철회색·청록·인디고·마젠타·흑요석+금) + 그림자 성채 톤. 어두운 성채 바닥·몬스터와 구분되도록 밝은 림 우선.
        private static readonly Color32 Iron = new Color32(161, 161, 170, 255);
        private static readonly Color32 Teal = new Color32(20, 184, 160, 255);
        private static readonly Color32 TealLight = new Color32(94, 234, 212, 255);
        private static readonly Color32 Indigo = new Color32(99, 102, 241, 255);
        private static readonly Color32 IndigoLight = new Color32(129, 140, 248, 255);
        private static readonly Color32 Magenta = new Color32(217, 70, 217, 255);
        private static readonly Color32 MagentaLight = new Color32(232, 121, 249, 255);
        private static readonly Color32 Obsidian = new Color32(63, 63, 70, 255);
        private static readonly Color32 Void = new Color32(24, 24, 27, 255);
        private static readonly Color32 Gold = new Color32(253, 224, 71, 255);
        private static readonly Color32 White = new Color32(248, 250, 252, 255);
        private static readonly Color32 SmokeA = new Color32(113, 113, 140, 255);
        private static readonly Color32 SmokeB = new Color32(156, 150, 196, 255);

        [MenuItem("Lair/OneShot/Build Assassin FX")]
        public static void Run()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError($"[AssassinFxBuilder] 머티리얼 없음: {MaterialPath} - 중단");
                return;
            }
            TextureImporter referenceImporter = AssetImporter.GetAtPath(ReferenceSheetPath) as TextureImporter;
            if (referenceImporter == null)
            {
                Debug.LogError($"[AssassinFxBuilder] 기준 시트 임포터 없음: {ReferenceSheetPath} - 중단");
                return;
            }

            AddressableAssetGroup group = FindAddressableGroup();
            foreach (FxSpec spec in BuildSpecs())
            {
                BuildOne(spec, material, referenceImporter, group);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AssassinFxBuilder] 완료 - 프리팹 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# ---- FX 목록 + 규약 ----
        private static List<FxSpec> BuildSpecs()
        {
            return new List<FxSpec>
            {
                //# 영웅→표적 베기 직선 — 1회 0.25s(3프레임), 폭 0.5 단위. RangedHitFx 가 SpawnLineRequired(길이=영웅~표적 거리).
                new FxSpec { Name = "AssassinSlashFx", CellW = 16, CellH = 32, Ppu = 32f, Pivot = new Vector2(0.5f, 0f), Frames = 3,
                    Layout = ELayout.Line, SortingOrder = 4, Draw = DrawSlash },
                //# 슬립 잔상 — 1회 0.67s(8프레임), 균일 스케일 1 호출 → 자식 2.4 단위 고정.
                new FxSpec { Name = "HeroSlipFx", CellW = 64, CellH = 64, Ppu = 64f, Frames = 8,
                    Layout = ELayout.FlatFixed, ChildScale = 2.4f, SortingOrder = -10, Draw = DrawSlip },
                //# 그림자 습격 — 1회 0.83s(10프레임), 출발/도착에 각각 스폰(균일 1) → 자식 2.6 단위 고정.
                new FxSpec { Name = "HeroShadowStrikeFx", CellW = 64, CellH = 64, Ppu = 64f, Frames = 10,
                    Layout = ELayout.FlatFixed, ChildScale = 2.6f, SortingOrder = -10, Draw = DrawShadowStrike },
                //# 칼날 선풍 — 1회 1.17s(14프레임 ≈ 1.2s), 루트 스케일 = 반경 2.4 x 2 = 4.8.
                new FxSpec { Name = "HeroBladeWhirlFx", CellW = 96, CellH = 96, Ppu = 96f, Frames = 14,
                    Layout = ELayout.FlatUnit, SortingOrder = -10, Draw = DrawBladeWhirl },
                //# 연막 탄 지대 — 1회 3.0s(36프레임), 루트 스케일 = 반경 3.0 x 2 = 6.0.
                new FxSpec { Name = "HeroSmokeBombFx", CellW = 96, CellH = 96, Ppu = 96f, Frames = 36,
                    Layout = ELayout.FlatUnit, SortingOrder = -10, Draw = DrawSmoke },
            };
        }

        //# ---- 빌드 한 건: 시트 → 임포트 → 프리팹 → Addressables ----
        private static void BuildOne(FxSpec spec, Material material, TextureImporter reference, AddressableAssetGroup group)
        {
            string prefabPath = FxDir + spec.Name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.LogError($"[AssassinFxBuilder] 이미 존재 - 덮어쓰지 않고 건너뜀: {prefabPath}");
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
                Debug.LogError($"[AssassinFxBuilder] 시트 PNG 가 이미 있어 재사용(덮어쓰지 않음): {sheetPath}");
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
            fx.FindProperty("_billboard").boolValue = false;
            fx.FindProperty("_autoPlay").boolValue = true;
            fx.FindProperty("_returnToPoolOnFinish").boolValue = spec.AutoReturn;
            fx.FindProperty("_poolable").objectReferenceValue = root.GetComponent<CHPoolable>();
            fx.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            RegisterAddressable(saved, prefabPath, spec.Name, group);
        }

        //# 자식 AuraFx 배치 — 지면 FX 는 X축 90° 로 눕혀 스프라이트 +Y 가 월드 +Z 가 되게 한다(기존 HeroNovaFx·궁수 FX 와 동일 회전).
        private static void ApplyLayout(FxSpec spec, Transform child)
        {
            child.localRotation = Quaternion.Euler(90f, 0f, 0f);
            child.localScale = spec.Layout == ELayout.FlatFixed ? new Vector3(spec.ChildScale, spec.ChildScale, 1f) : Vector3.one;
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
                Debug.LogError($"[AssassinFxBuilder] 임포터 없음: {sheetPath}");
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
                    Debug.LogError($"[AssassinFxBuilder] {spec.Name} 프레임 {i} 스프라이트 없음 - 프리팹 생성 중단");
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
                Debug.LogError($"[AssassinFxBuilder] Addressables 설정/그룹 없음 - {address} 수동 등록 필요");
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

            public void Ring(float cx, float cy, float r, float thick, Color32 color, float alpha)
            {
                Arc(cx, cy, r, thick, 0f, 360f, color, alpha);
            }

            //# 호(링의 일부) — 각도(도, 0=오른쪽, 시계 방향) [from, to] 구간만. 360° 이상이면 전체 링.
            public void Arc(float cx, float cy, float r, float thick, float fromDeg, float toDeg, Color32 color, float alpha)
            {
                float outer = r + thick * 0.5f;
                float inner = r - thick * 0.5f;
                bool full = toDeg - fromDeg >= 360f;
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
                        if (full)
                        {
                            Plot(x, y, color, alpha);
                            continue;
                        }
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

        //# ---- 1) 베기: 위(y=0)가 표적 쪽. 교차하는 두 칼자국 + 중심선 잔광, 3프레임 페이드. ----
        private static void DrawSlash(Canvas c, int f, int n)
        {
            float fade = f == 0 ? 1f : (f == 1 ? 0.6f : 0.25f);
            float spread = 1f + f * 0.12f;
            float cx = c.W * 0.5f;
            for (int y = 2; y < c.H - 2; ++y)
            {
                c.Plot((int)cx, y, Iron, 0.35f * fade);
            }
            //# 두 칼자국 — 얇은 흰 심 + 청록 림.
            c.Line(cx - 5f * spread, 5f, cx + 5f * spread, 26f, 2f, TealLight, 0.7f * fade);
            c.Line(cx - 5f * spread, 5f, cx + 5f * spread, 26f, 1f, White, fade);
            c.Line(cx + 5f * spread, 7f, cx - 5f * spread, 28f, 2f, IndigoLight, 0.7f * fade);
            c.Line(cx + 5f * spread, 7f, cx - 5f * spread, 28f, 1f, White, fade);
            c.Plot((int)cx, 1, White, fade);
        }

        //# ---- 2) 슬립 잔상: 중심에서 번지는 그림자 먼지 + 퍼지는 인디고 링 + 청록 불꽃 8프레임. ----
        private static void DrawSlip(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float t = f / (float)(n - 1);
            float fade = Mathf.Clamp01(1f - t * 0.95f);
            if (f < 3)
            {
                c.Fill(cx, cy, 14f - f * 3.5f, Obsidian, 0.8f - f * 0.2f);
            }
            for (int i = 0; i < 10; ++i)
            {
                float angle = (i / 10f) * Mathf.PI * 2f + Hash(61, i) * 0.4f;
                float dist = 6f + f * 3.2f * (0.8f + Hash(62, i) * 0.4f);
                float px = cx + Mathf.Cos(angle) * dist;
                float py = cy + Mathf.Sin(angle) * dist;
                float puff = 3.5f + Hash(63, i) * 2.5f;
                c.Disk(px, py, puff, d => S(Color32.Lerp(Obsidian, Indigo, d), fade * (0.7f - d * 0.4f)));
            }
            c.Ring(cx, cy, 8f + f * 3.6f, 1.5f, IndigoLight, 0.75f * fade);
            for (int i = 0; i < 6; ++i)
            {
                float angle = Hash(64, i) * Mathf.PI * 2f;
                float dist = 10f + f * 3.8f;
                c.Plot(Mathf.FloorToInt(cx + Mathf.Cos(angle) * dist), Mathf.FloorToInt(cy + Mathf.Sin(angle) * dist), TealLight, fade);
            }
        }

        //# ---- 3) 그림자 습격: 0~3 수축하는 허공(흑요석 구체 + 마젠타 링) → 4~9 인디고/마젠타 방사 파열 + 금빛 섬광. 10프레임, 출발/도착 공용. ----
        private static void DrawShadowStrike(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            if (f < 4)
            {
                float shrink = 1f - f / 4f;
                c.Fill(cx, cy, 6f + 22f * shrink, Void, 0.85f);
                c.Ring(cx, cy, 8f + 22f * shrink, 2f, Magenta, 0.9f);
                c.Ring(cx, cy, 10f + 22f * shrink, 1f, MagentaLight, 0.6f);
                return;
            }

            float t = (f - 4) / 5f;
            float fade = Mathf.Clamp01(1f - t);
            c.Fill(cx, cy, 9f * (1f - t * 0.6f), f == 4 ? Gold : Void, 0.9f * fade);
            c.Ring(cx, cy, 8f + t * 22f, 2.5f, IndigoLight, 0.9f * fade);
            c.Ring(cx, cy, 6f + t * 17f, 1.5f, MagentaLight, 0.7f * fade);
            for (int i = 0; i < 12; ++i)
            {
                float angle = (i / 12f) * Mathf.PI * 2f + Hash(71, i) * 0.2f;
                float inner = 6f + t * 10f;
                float outer = inner + 7f + Hash(72, i) * 6f;
                c.Line(cx + Mathf.Cos(angle) * inner, cy + Mathf.Sin(angle) * inner, cx + Mathf.Cos(angle) * outer, cy + Mathf.Sin(angle) * outer, 1f, i % 2 == 0 ? MagentaLight : TealLight, fade);
            }
        }

        //# ---- 4) 칼날 선풍: 지름 = 셀 폭(반경 2.4 → 4.8). 4개의 칼날 호가 2회전하며 안쪽으로 모였다 사라짐 + 중앙 청록 소용돌이. 14프레임. ----
        private static void DrawBladeWhirl(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float t = f / (float)(n - 1);
            float fade = f >= 11 ? (f >= 13 ? 0.25f : 0.6f) : 1f;
            float rotation = t * 720f;
            float radius = 40f - t * 6f;

            c.Disk(cx, cy, 42f, d => S(Color32.Lerp(Teal, Indigo, d), (0.05f + (1f - d) * 0.07f) * fade));
            for (int k = 0; k < 4; ++k)
            {
                float from = rotation + k * 90f;
                c.Arc(cx, cy, radius, 6f, from, from + 55f, Teal, 0.75f * fade);
                c.Arc(cx, cy, radius, 3f, from + 4f, from + 52f, Iron, fade);
                c.Arc(cx, cy, radius, 1f, from + 10f, from + 48f, White, fade);
                //# 칼끝 불꽃
                float tip = (from + 55f) * Mathf.Deg2Rad;
                c.Fill(cx + Mathf.Cos(tip) * radius, cy + Mathf.Sin(tip) * radius, 2f, White, fade);
            }
            c.Ring(cx, cy, 14f + 4f * Mathf.Sin(t * Mathf.PI * 4f), 1.5f, TealLight, 0.6f * fade);
        }

        //# ---- 5) 연막 탄: 지름 = 셀 폭(반경 3.0 → 6.0). 0~7 부풀며 퍼짐 → 8~29 천천히 일렁이는 연기 + 옅은 청록 경계 → 30~35 페이드. 36프레임(3.0s). ----
        //# 알파는 최대 ~0.5 로 제한해 지대 안 몬스터(둔화 대상)가 보이게 한다.
        private static void DrawSmoke(Canvas c, int f, int n)
        {
            float cx = c.W * 0.5f;
            float cy = c.H * 0.5f;
            float grow = Mathf.Clamp01(f / 7f);
            float fade = f >= 30 ? Mathf.Clamp01(1f - (f - 29) / 6f) : 1f;
            float drift = f * 0.18f;
            float baseRadius = 44f * (0.35f + 0.65f * grow);

            //# 연기 뭉치 — 고정 시드 위치 + 프레임별 완만한 일렁임.
            for (int i = 0; i < 16; ++i)
            {
                float angle = Hash(81, i) * Mathf.PI * 2f + Mathf.Sin(drift + i) * 0.08f;
                float dist = Mathf.Sqrt(Hash(82, i)) * baseRadius * 0.78f;
                float px = cx + Mathf.Cos(angle) * dist;
                float py = cy + Mathf.Sin(angle) * dist + Mathf.Sin(drift * 1.3f + i * 0.7f) * 1.5f;
                float puff = (9f + Hash(83, i) * 7f) * (0.5f + 0.5f * grow);
                Color32 tone = i % 3 == 0 ? SmokeB : SmokeA;
                c.Disk(px, py, puff, d => S(Color32.Lerp(tone, Obsidian, d * 0.8f), fade * (0.5f - d * 0.32f)));
            }

            //# 지대 경계 — 옅은 점선 링(청록), 16칸 호가 천천히 회전.
            float step = 360f / 16f;
            for (int i = 0; i < 16; ++i)
            {
                float from = i * step + f * 1.5f;
                c.Arc(cx, cy, baseRadius, 1.5f, from, from + step * 0.5f, TealLight, 0.45f * fade * grow);
            }
        }
    }
}
