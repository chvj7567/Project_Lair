using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools.OneShot
{
    //# 일회용 배경 빌더 (Rule 04 §3) — 챕터 2 어둠의 숲 전투 배경 Battle_Backdrop_DarkForest.png 를 만든 뒤 삭제한다. 기획서 chapter-system §5.5 · scene-2d-conversion §2.
    //# 새 시안 파이프라인 없이 기존 Battle_Backdrop.png(1600x704)를 읽어 절차 변환한다:
    //#  1) 색 매핑 — 원본의 "서로 다른 색"마다 결정적 함수로 새 색을 대응시킨다(팔레트 수는 늘지 않음, 도트 격자 유지).
    //#     석재(저채도/푸른 계열)는 명도 램프(황혼 보라 → 이끼 회녹)로, 강조색(금 점·붉은 흔적·중앙 보라 링)은 색상만 숲 톤으로 이동.
    //#     석재 램프의 중간 밝기를 원본과 비슷하게 맞춰 전장 중앙의 영웅/몬스터 가독성을 유지한다.
    //#  2) 상단 벽 띠 → 숲 가장자리(나무 기둥·뿌리·이끼) 로 교체, 아레나 바깥 어둠에 나무 실루엣·덤불·뿌리·바닥 얼룩, 아레나 테두리/이음매에 이끼 점 추가.
    //# 출력은 Backdrop2D 폴더에 PNG 로 쓰고 Battle_Backdrop 과 같은 임포트 설정(Point·무압축·PPU 48)으로 임포트 + SheetSpec 기록 + Addressables(라벨 Resource) 등록.
    //# 이미 있으면 덮어쓰지 않고 LogError 후 중단한다.
    public static class DarkForestBackdropBuilder
    {
        private const string Folder = "Assets/_Lair/Art/Sprites/Backdrop2D/";
        private const string SourcePath = Folder + "Battle_Backdrop.png";
        private const string OutputPath = Folder + "Battle_Backdrop_DarkForest.png";
        private const string SpecPath = Folder + "Backdrop2D_SheetSpec.json";
        private const string ReferencePrefabPath = "Assets/_Lair/Art/FX/HeroNovaFx.prefab";
        private const string AddressName = "Battle_Backdrop_DarkForest";
        private const string AddressableLabel = "Resource";

        //# 숲 팔레트 — 장식에만 쓰는 이산 색(그라디언트 없음). 황혼 보라 + 이끼 녹색.
        private static readonly Color32 BarkDark = new Color32(20, 14, 28, 255);
        private static readonly Color32 BarkMid = new Color32(32, 24, 40, 255);
        private static readonly Color32 BarkLight = new Color32(54, 42, 60, 255);
        private static readonly Color32 RootColor = new Color32(40, 31, 46, 255);
        private static readonly Color32 CanopyDark = new Color32(14, 28, 26, 255);
        private static readonly Color32 CanopyMid = new Color32(24, 46, 36, 255);
        private static readonly Color32 CanopyLight = new Color32(40, 78, 44, 255);
        private static readonly Color32 MossDark = new Color32(58, 96, 24, 255);
        private static readonly Color32 MossMid = new Color32(96, 150, 32, 255);
        private static readonly Color32 MossLight = new Color32(150, 204, 56, 255);
        private static readonly Color32 DuskA = new Color32(12, 8, 24, 255);
        private static readonly Color32 DuskB = new Color32(20, 14, 36, 255);
        private static readonly Color32 DuskC = new Color32(28, 20, 46, 255);

        [MenuItem("Lair/OneShot/Build DarkForest Backdrop")]
        public static void Run()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), OutputPath)))
            {
                Debug.LogError($"[DarkForestBackdropBuilder] 이미 존재 - 덮어쓰지 않고 중단: {OutputPath}");
                return;
            }
            TextureImporter reference = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
            if (reference == null)
            {
                Debug.LogError($"[DarkForestBackdropBuilder] 원본 임포터 없음: {SourcePath} - 중단");
                return;
            }

            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (source.LoadImage(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), SourcePath))) == false)
            {
                Debug.LogError($"[DarkForestBackdropBuilder] 원본 PNG 읽기 실패: {SourcePath}");
                Object.DestroyImmediate(source);
                return;
            }

            int width = source.width;
            int height = source.height;
            Color32[] image = ToTopDown(source.GetPixels32(), width, height);
            Object.DestroyImmediate(source);

            int sourceColors = CountColors(image);
            Convert(image, width, height);
            int resultColors = CountColors(image);
            Debug.Log($"[DarkForestBackdropBuilder] 팔레트 {sourceColors}색 → {resultColors}색 (장식 이산 색 포함)");

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(ToBottomUp(image, width, height));
            result.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), OutputPath), result.EncodeToPNG());
            Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(OutputPath);

            ConfigureImporter(reference);
            RecordSheetSpec(width, height);
            RegisterAddressable();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkForestBackdropBuilder] 완료 - 결과 PNG 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# ---- 변환 본체 ----
        private static void Convert(Color32[] img, int w, int h)
        {
            Color32 background = MostFrequentColor(img);
            int wallBottom = FindWallBottom(img, w, h);
            Ellipse arena = FindArena(img, w, h, wallBottom, background);

            //# 장식 판정용 마스크는 "색을 바꾸기 전" 원본 기준으로 만든다.
            bool[] outside = new bool[w * h];
            bool[] stone = new bool[w * h];
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    int i = y * w + x;
                    bool inArena = arena.Contains(x, y, 1.01f);
                    stone[i] = inArena;
                    outside[i] = y >= wallBottom && inArena == false;
                }
            }

            Dictionary<int, Color32> cache = new Dictionary<int, Color32>();
            for (int i = 0; i < img.Length; ++i)
            {
                Color32 c = img[i];
                int key = (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
                if (cache.TryGetValue(key, out Color32 mapped) == false)
                {
                    mapped = MapColor(c);
                    cache[key] = mapped;
                }
                img[i] = mapped;
            }

            DrawTreeline(img, w, wallBottom);
            DecorateOutside(img, w, h, outside, arena, wallBottom);
            DecorateArena(img, w, h, stone, arena);
        }

        //# 색 매핑 — 결정적(같은 입력 → 같은 출력)이라 원본 팔레트 크기를 넘지 않는다.
        private static Color32 MapColor(Color32 c)
        {
            if (c.a == 0)
                return c;

            float r = c.r;
            float g = c.g;
            float b = c.b;
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float luminance = 0.299f * r + 0.587f * g + 0.114f * b;
            float saturation = max <= 0f ? 0f : (max - min) / max;
            bool blueish = b >= r && b >= g;

            //# 강조색 — 금 점은 이끼 반딧불, 붉은 흔적은 장미빛 어둠, 중앙 보라 링은 보라 유지(조금 밝게).
            if (saturation > 0.35f && blueish == false)
            {
                if (r > 150f && g > 120f && b < 130f)
                    return new Color32(190, 230, 110, c.a);
                if (r > g * 1.3f && r > b * 1.1f)
                    return new Color32((byte)(r * 0.8f), (byte)(g * 0.55f), (byte)(b * 1.2f + 20f), c.a);
                return new Color32((byte)Mathf.Min(255f, r * 1.1f), (byte)(g * 1.05f), (byte)Mathf.Min(255f, b * 1.1f), c.a);
            }

            //# 석재/배경 — 명도 램프: 어둠 → 황혼 보라 → 이끼 회녹 → 이끼 하이라이트. 중간 구간 밝기는 원본과 비슷하게(가독성).
            float t = Mathf.Clamp01((luminance - 8f) / 92f);
            Color32 result;
            if (t < 0.12f)
            {
                result = Color32.Lerp(new Color32(10, 7, 20, 255), new Color32(20, 14, 38, 255), t / 0.12f);
            }
            else if (t < 0.45f)
            {
                result = Color32.Lerp(new Color32(20, 14, 38, 255), new Color32(46, 44, 70, 255), (t - 0.12f) / 0.33f);
            }
            else if (t < 0.75f)
            {
                result = Color32.Lerp(new Color32(46, 44, 70, 255), new Color32(74, 88, 84, 255), (t - 0.45f) / 0.30f);
            }
            else
            {
                result = Color32.Lerp(new Color32(74, 88, 84, 255), new Color32(124, 142, 98, 255), (t - 0.75f) / 0.25f);
            }
            result.a = c.a;
            return result;
        }

        //# ---- 상단 벽 띠 → 숲 가장자리 ----
        private static void DrawTreeline(Color32[] img, int w, int wallBottom)
        {
            //# 바탕: 황혼 어둠 3단
            for (int y = 0; y < wallBottom; ++y)
            {
                Color32 tone = y < wallBottom * 0.34f ? DuskA : (y < wallBottom * 0.7f ? DuskB : DuskC);
                for (int x = 0; x < w; ++x)
                {
                    img[y * w + x] = tone;
                }
            }

            //# 나무 기둥 — 약 140px 간격, 폭 34~58px.
            int trunkCount = 11;
            for (int k = 0; k < trunkCount; ++k)
            {
                int center = Mathf.RoundToInt((k + 0.5f) * w / trunkCount + (Hash(1, k) - 0.5f) * 40f);
                int trunkWidth = 34 + Mathf.RoundToInt(Hash(2, k) * 24f);
                DrawTrunk(img, w, wallBottom, center, trunkWidth, k);
            }

            //# 캐노피 술 — 상단 가장자리의 잎 덩어리.
            for (int x = 0; x < w; x += 6)
            {
                float blob = 8f + Hash(3, x) * 14f;
                for (int y = 0; y < blob; ++y)
                {
                    float edge = y / blob;
                    Color32 leaf = edge > 0.75f ? CanopyMid : CanopyDark;
                    if (Hash(4, x * 131 + y) > 0.93f)
                    {
                        leaf = CanopyLight;
                    }
                    SetPixel(img, w, x, y, leaf);
                    SetPixel(img, w, x + 1, y, leaf);
                    SetPixel(img, w, x + 2, y, leaf);
                }
            }

            //# 바닥 이끼 줄 — 벽 하단 경계 자리에 이끼 선(군데군데 끊김).
            for (int x = 0; x < w; ++x)
            {
                if (Hash(5, x / 4) > 0.25f)
                {
                    SetPixel(img, w, x, wallBottom - 2, MossDark);
                    SetPixel(img, w, x, wallBottom - 1, Hash(6, x) > 0.7f ? MossMid : MossDark);
                }
            }
        }

        private static void DrawTrunk(Color32[] img, int w, int wallBottom, int center, int trunkWidth, int seed)
        {
            int half = trunkWidth / 2;
            for (int y = 0; y < wallBottom; ++y)
            {
                //# 아래로 갈수록 뿌리처럼 퍼짐(마지막 14px).
                float flare = y > wallBottom - 14 ? (y - (wallBottom - 14)) * 0.9f : 0f;
                int left = center - half - Mathf.RoundToInt(flare);
                int right = center + half + Mathf.RoundToInt(flare);
                for (int x = left; x <= right; ++x)
                {
                    Color32 bark = BarkMid;
                    if (x - left < 3)
                    {
                        bark = BarkLight;       //# 왼쪽 하이라이트
                    }
                    else if (right - x < 4)
                    {
                        bark = BarkDark;        //# 오른쪽 그늘
                    }
                    else if (((x + seed * 3) % 7) == 0 && Hash(7, x * 53 + y / 5) > 0.35f)
                    {
                        bark = BarkDark;        //# 세로 나뭇결
                    }
                    //# 하단 이끼 군락
                    if (y > wallBottom * 0.55f && Hash(8, (x * 31 + y * 17 + seed) % 997) > 0.9f)
                    {
                        bark = MossDark;
                    }
                    SetPixel(img, w, x, y, bark);
                }
            }
        }

        //# ---- 아레나 바깥: 나무 실루엣·덤불·뿌리·바닥 얼룩 (바깥 마스크 위에만 그린다) ----
        private static void DecorateOutside(Color32[] img, int w, int h, bool[] outside, Ellipse arena, int wallBottom)
        {
            //# 바닥 얼룩 — 어둠에 아주 약한 변화(도트 노이즈).
            for (int y = wallBottom; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    if (outside[y * w + x] == false)
                        continue;
                    float n = Hash(11, x * 7919 + y);
                    if (n > 0.985f)
                    {
                        img[y * w + x] = CanopyDark;
                    }
                    else if (n > 0.965f)
                    {
                        img[y * w + x] = DuskC;
                    }
                }
            }

            //# 좌우 가장자리 큰 나무 실루엣 — 아레나 밖 좌우 끝에서 하단까지.
            int[] trunkX = { 22, 118, w - 26, w - 126 };
            for (int k = 0; k < trunkX.Length; ++k)
            {
                int trunkWidth = 40 + Mathf.RoundToInt(Hash(12, k) * 26f);
                for (int y = wallBottom; y < h; ++y)
                {
                    //# 살짝 휘는 줄기
                    int sway = Mathf.RoundToInt(Mathf.Sin(y * 0.012f + k) * 6f);
                    int left = trunkX[k] - trunkWidth / 2 + sway;
                    for (int x = left; x < left + trunkWidth; ++x)
                    {
                        if (x < 0 || x >= w || outside[y * w + x] == false)
                            continue;
                        Color32 bark = BarkDark;
                        if (x - left < 4)
                        {
                            bark = BarkMid;
                        }
                        else if (((x + k * 5) % 9) == 0 && Hash(13, y / 6 + x) > 0.4f)
                        {
                            bark = BarkMid;
                        }
                        if (Hash(14, x * 29 + y * 13) > 0.985f)
                        {
                            bark = MossDark;
                        }
                        img[y * w + x] = bark;
                    }
                }
            }

            //# 하단 덤불 띠
            for (int x = -10; x < w + 10; x += 46)
            {
                float radius = 20f + Hash(15, x + 100) * 16f;
                float cy = h - 4f - Hash(16, x + 100) * 10f;
                for (int y = Mathf.FloorToInt(cy - radius); y <= Mathf.CeilToInt(cy + radius); ++y)
                {
                    for (int xx = Mathf.FloorToInt(x - radius); xx <= Mathf.CeilToInt(x + radius); ++xx)
                    {
                        if (xx < 0 || xx >= w || y < 0 || y >= h || outside[y * w + xx] == false)
                            continue;
                        float dx = xx - x;
                        float dy = y - cy;
                        if (dx * dx + dy * dy > radius * radius)
                            continue;
                        Color32 leaf = dy < -radius * 0.35f ? CanopyMid : CanopyDark;
                        if (Hash(17, xx * 17 + y * 31) > 0.94f)
                        {
                            leaf = CanopyLight;
                        }
                        img[y * w + xx] = leaf;
                    }
                }
            }

            //# 테두리에서 바깥으로 뻗는 뿌리 — 14방향, 끝으로 갈수록 가늘게.
            for (int k = 0; k < 14; ++k)
            {
                float angle = (k / 14f) * Mathf.PI * 2f + Hash(18, k) * 0.2f;
                float length = 46f + Hash(19, k) * 56f;
                float px = arena.Cx + Mathf.Cos(angle) * arena.Rx * 1.01f;
                float py = arena.Cy + Mathf.Sin(angle) * arena.Ry * 1.01f;
                float dirX = Mathf.Cos(angle);
                float dirY = Mathf.Sin(angle) * 0.55f;
                for (float s = 0f; s < length; s += 1f)
                {
                    float taper = 1f - s / length;
                    int thickness = taper > 0.6f ? 3 : (taper > 0.3f ? 2 : 1);
                    float wobble = Mathf.Sin(s * 0.18f + k) * 2.5f;
                    int cx = Mathf.RoundToInt(px + dirX * s - dirY * wobble * 0.3f);
                    int cy = Mathf.RoundToInt(py + dirY * s + dirX * wobble * 0.2f);
                    for (int t = 0; t < thickness; ++t)
                    {
                        int rx = cx;
                        int ry = cy + t;
                        if (rx >= 0 && rx < w && ry >= 0 && ry < h && outside[ry * w + rx])
                        {
                            img[ry * w + rx] = t == 0 ? BarkLight : RootColor;
                        }
                    }
                    if (Hash(20, k * 997 + (int)s) > 0.96f && cx >= 0 && cx < w && cy >= 0 && cy < h && outside[cy * w + cx])
                    {
                        img[cy * w + cx] = MossMid;
                    }
                }
            }
        }

        //# ---- 아레나 위: 테두리/이음매 이끼 점 (중앙 전투 영역은 거의 건드리지 않아 가독성 유지) ----
        private static void DecorateArena(Color32[] img, int w, int h, bool[] stone, Ellipse arena)
        {
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    if (stone[y * w + x] == false)
                        continue;
                    float d = arena.Distance01(x, y);
                    //# 테두리 띠(0.9~1.0)는 이끼 25%, 안쪽(0.6~0.9)은 0.6%, 중앙(<0.6)은 0.05%.
                    float chance = d > 0.9f ? 0.04f : (d > 0.6f ? 0.0012f : 0.0001f);
                    if (Hash(31, x * 2909 + y) > 1f - chance)
                    {
                        PutMossCluster(img, w, h, stone, x, y);
                    }
                }
            }
        }

        private static void PutMossCluster(Color32[] img, int w, int h, bool[] stone, int x, int y)
        {
            int cols = 2 + (int)(Hash(32, x * 13 + y) * 3f);
            for (int dx = 0; dx < cols; ++dx)
            {
                int xx = x + dx;
                if (xx >= w || stone[y * w + xx] == false)
                    continue;
                float n = Hash(33, xx * 7 + y * 11);
                img[y * w + xx] = n > 0.75f ? MossLight : (n > 0.35f ? MossMid : MossDark);
                if (y + 1 < h && stone[(y + 1) * w + xx] && n > 0.5f)
                {
                    img[(y + 1) * w + xx] = MossDark;
                }
            }
        }

        //# ---- 분석 ----
        private struct Ellipse
        {
            public float Cx;
            public float Cy;
            public float Rx;
            public float Ry;

            public float Distance01(float x, float y)
            {
                float dx = (x - Cx) / Rx;
                float dy = (y - Cy) / Ry;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            public bool Contains(float x, float y, float scale)
            {
                return Distance01(x, y) <= scale;
            }
        }

        //# 가장 많이 쓰인 색 = 바깥 배경색.
        private static Color32 MostFrequentColor(Color32[] img)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            int bestKey = 0;
            int best = 0;
            foreach (Color32 c in img)
            {
                int key = (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
                counts.TryGetValue(key, out int n);
                counts[key] = ++n;
                if (n > best)
                {
                    best = n;
                    bestKey = key;
                }
            }
            return new Color32((byte)(bestKey >> 24), (byte)(bestKey >> 16), (byte)(bestKey >> 8), (byte)bestKey);
        }

        //# 벽 띠 하단 — 행 평균 명도가 20행 뒤 처음 어두운 배경 수준으로 떨어지는 행.
        private static int FindWallBottom(Color32[] img, int w, int h)
        {
            for (int y = 20; y < h / 2; ++y)
            {
                float sum = 0f;
                for (int x = 0; x < w; x += 4)
                {
                    Color32 c = img[y * w + x];
                    sum += 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                }
                float average = sum / (w / 4);
                if (average < 14f)
                    return y;
            }
            return 60;
        }

        //# 아레나 타원 — 벽 아래에서 배경색과 충분히 다른 픽셀의 경계 상자로 추정.
        private static Ellipse FindArena(Color32[] img, int w, int h, int wallBottom, Color32 background)
        {
            int minX = w;
            int maxX = 0;
            int minY = h;
            int maxY = 0;
            for (int y = wallBottom + 4; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    Color32 c = img[y * w + x];
                    int diff = Mathf.Abs(c.r - background.r) + Mathf.Abs(c.g - background.g) + Mathf.Abs(c.b - background.b);
                    if (diff > 24)
                    {
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }
            if (maxX <= minX || maxY <= minY)
            {
                Debug.LogError("[DarkForestBackdropBuilder] 아레나 영역을 찾지 못해 이미지 중앙 타원으로 가정");
                return new Ellipse { Cx = w * 0.5f, Cy = h * 0.5f, Rx = w * 0.39f, Ry = h * 0.37f };
            }
            return new Ellipse { Cx = (minX + maxX) * 0.5f, Cy = (minY + maxY) * 0.5f, Rx = (maxX - minX) * 0.5f, Ry = (maxY - minY) * 0.5f };
        }

        //# ---- 임포트·스펙·Addressables ----
        private static void ConfigureImporter(TextureImporter reference)
        {
            TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[DarkForestBackdropBuilder] 임포터 없음: {OutputPath}");
                return;
            }
            //# Battle_Backdrop 설정 그대로 — Sprite(Single)·Point·무압축·밉맵 off·PPU 48·maxTextureSize 2048.
            TextureImporterSettings settings = new TextureImporterSettings();
            reference.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);
            importer.textureCompression = reference.textureCompression;
            importer.maxTextureSize = reference.maxTextureSize;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        //# Backdrop2D_SheetSpec.json 에 항목 추가 — Backdrop2DImportTests("폴더의 PNG 는 전부 SheetSpec 에 기록")를 만족시킨다. 기존 서식(2칸 들여쓰기·LF·끝 개행 없음) 유지.
        private static void RecordSheetSpec(int width, int height)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), SpecPath);
            JObject root = JObject.Parse(File.ReadAllText(path));
            JObject files = (JObject)root["files"];
            if (files.ContainsKey("Battle_Backdrop_DarkForest.png"))
                return;

            JObject entry = new JObject
            {
                ["folder"] = "Backdrop2D",
                ["size"] = new JArray(width, height),
                ["cell"] = null,
                ["frames"] = 1,
                ["fps"] = null,
                ["pivot"] = new JArray(0.5, 0.5),
                ["topLeftDot"] = null,
                ["ppu"] = 48,
                ["content"] = new JArray(0, 0, width, height),
            };
            files.Property("Battle_Backdrop.png").AddAfterSelf(new JProperty("Battle_Backdrop_DarkForest.png", entry));
            string text = root.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n");
            File.WriteAllText(path, text, new System.Text.UTF8Encoding(false));
        }

        private static void RegisterAddressable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError($"[DarkForestBackdropBuilder] Addressables 설정 없음 - {AddressName} 수동 등록 필요");
                return;
            }
            AddressableAssetEntry reference = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(ReferencePrefabPath));
            AddressableAssetGroup group = reference != null ? reference.parentGroup : settings.FindGroup("Resource");
            if (group == null)
            {
                Debug.LogError($"[DarkForestBackdropBuilder] Addressables 그룹 없음 - {AddressName} 수동 등록 필요");
                return;
            }
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(OutputPath), group);
            entry.address = AddressName;
            entry.SetLabel(AddressableLabel, true);
            EditorUtility.SetDirty(settings);
        }

        //# ---- 유틸 ----
        private static Color32[] ToTopDown(Color32[] bottomUp, int w, int h)
        {
            Color32[] result = new Color32[w * h];
            for (int y = 0; y < h; ++y)
            {
                System.Array.Copy(bottomUp, (h - 1 - y) * w, result, y * w, w);
            }
            return result;
        }

        private static Color32[] ToBottomUp(Color32[] topDown, int w, int h)
        {
            return ToTopDown(topDown, w, h);
        }

        private static int CountColors(Color32[] img)
        {
            HashSet<int> set = new HashSet<int>();
            foreach (Color32 c in img)
            {
                set.Add((c.r << 24) | (c.g << 16) | (c.b << 8) | c.a);
            }
            return set.Count;
        }

        private static void SetPixel(Color32[] img, int w, int x, int y, Color32 color)
        {
            if (x < 0 || x >= w || y < 0 || y * w + x >= img.Length)
                return;
            img[y * w + x] = color;
        }

        //# 결정적 의사난수 0~1 — 같은 (시드, 인덱스)는 항상 같은 값(재실행해도 같은 결과).
        private static float Hash(int seed, int index)
        {
            uint h = (uint)(seed * 73856093) ^ (uint)(index * 19349663 + 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
