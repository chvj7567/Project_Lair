using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools.OneShot
{
    //# 일회용 배경 빌더 (Rule 04 §3) — 챕터 3 그림자 성채 전투 배경 Battle_Backdrop_ShadowCitadel.png 를 만든 뒤 삭제한다. 기획서 chapter-3-hero §6·D-C3-12 · scene-2d-conversion §2.
    //# 실행: Unity -batchmode -nographics -quit -projectPath <경로> -executeMethod Lair.EditorTools.OneShot.ShadowCitadelBackdropBuilder.Run
    //# DarkForest 방식 — 기존 Battle_Backdrop.png(1600x704)를 읽어 절차 변환한다(새 시안 파이프라인 없음):
    //#  1) 색 매핑 — 원본의 "서로 다른 색"마다 결정적 함수로 새 색을 대응(팔레트 수는 늘지 않음, 도트 격자 유지).
    //#     석재(저채도/푸른 계열)는 명도 램프(흑요석 → 인디고 석재 → 차가운 회청)로, 강조색(금 점·붉은 흔적·중앙 보라 링)은 색상만 성채 톤으로 이동.
    //#     석재 램프의 중간 밝기를 원본과 비슷하게 맞춰 전장 중앙의 영웅/몬스터 가독성을 유지한다.
    //#  2) 상단 벽 띠 → 성벽(벽돌·흉벽·기둥·깃발·늘어진 쇠사슬) 로 교체, 아레나 바깥 어둠에 석조 타일 이음매·기둥 실루엣·쇠사슬·파편, 아레나 테두리에 룬 점·균열 추가.
    //# 출력은 Backdrop2D 폴더에 PNG 로 쓰고 Battle_Backdrop 과 같은 임포트 설정(Point·무압축·PPU 48)으로 임포트 + SheetSpec 기록 + Addressables(라벨 Resource) 등록.
    //# 이미 있으면 덮어쓰지 않고 LogError 후 중단한다.
    public static class ShadowCitadelBackdropBuilder
    {
        private const string Folder = "Assets/_Lair/Art/Sprites/Backdrop2D/";
        private const string SourcePath = Folder + "Battle_Backdrop.png";
        private const string OutputPath = Folder + "Battle_Backdrop_ShadowCitadel.png";
        private const string SpecPath = Folder + "Backdrop2D_SheetSpec.json";
        private const string SpecKey = "Battle_Backdrop_ShadowCitadel.png";
        private const string ReferencePrefabPath = "Assets/_Lair/Art/FX/HeroNovaFx.prefab";
        private const string AddressName = "Battle_Backdrop_ShadowCitadel";
        private const string AddressableLabel = "Resource";

        //# 성채 팔레트 — 장식에만 쓰는 이산 색(그라디언트 없음). 흑요석/인디고 석재 + 마젠타 깃발 + 쇠사슬.
        private static readonly Color32 NightA = new Color32(7, 7, 16, 255);
        private static readonly Color32 NightB = new Color32(13, 13, 26, 255);
        private static readonly Color32 NightC = new Color32(20, 20, 38, 255);
        private static readonly Color32 BrickDark = new Color32(20, 19, 32, 255);
        private static readonly Color32 BrickMid = new Color32(32, 31, 48, 255);
        private static readonly Color32 BrickLight = new Color32(50, 49, 72, 255);
        private static readonly Color32 Mortar = new Color32(11, 10, 20, 255);
        private static readonly Color32 PillarDark = new Color32(16, 16, 28, 255);
        private static readonly Color32 PillarMid = new Color32(28, 28, 44, 255);
        private static readonly Color32 PillarLight = new Color32(58, 58, 82, 255);
        private static readonly Color32 BannerDark = new Color32(78, 24, 88, 255);
        private static readonly Color32 BannerMid = new Color32(120, 40, 132, 255);
        private static readonly Color32 BannerTrim = new Color32(190, 160, 50, 255);
        private static readonly Color32 ChainDark = new Color32(36, 36, 48, 255);
        private static readonly Color32 ChainLight = new Color32(104, 104, 122, 255);
        private static readonly Color32 RuneIndigo = new Color32(99, 102, 241, 255);
        private static readonly Color32 RuneDim = new Color32(58, 60, 150, 255);
        private static readonly Color32 CrackDark = new Color32(9, 9, 18, 255);

        [MenuItem("Lair/OneShot/Build ShadowCitadel Backdrop")]
        public static void Run()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), OutputPath)))
            {
                Debug.LogError($"[ShadowCitadelBackdropBuilder] 이미 존재 - 덮어쓰지 않고 중단: {OutputPath}");
                return;
            }
            TextureImporter reference = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
            if (reference == null)
            {
                Debug.LogError($"[ShadowCitadelBackdropBuilder] 원본 임포터 없음: {SourcePath} - 중단");
                return;
            }

            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (source.LoadImage(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), SourcePath))) == false)
            {
                Debug.LogError($"[ShadowCitadelBackdropBuilder] 원본 PNG 읽기 실패: {SourcePath}");
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
            Debug.Log($"[ShadowCitadelBackdropBuilder] 팔레트 {sourceColors}색 → {resultColors}색 (장식 이산 색 포함)");

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
            Debug.Log("[ShadowCitadelBackdropBuilder] 완료 - 결과 PNG 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
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

            DrawCastleWall(img, w, wallBottom);
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

            //# 강조색 — 금 점은 어두운 금빛 불씨, 붉은 흔적은 마젠타 어둠, 중앙 보라 링은 인디고로 이동.
            if (saturation > 0.35f && blueish == false)
            {
                if (r > 150f && g > 120f && b < 130f)
                    return new Color32(196, 170, 70, c.a);
                if (r > g * 1.3f && r > b * 1.1f)
                    return new Color32((byte)Mathf.Min(255f, r * 0.85f + 20f), (byte)(g * 0.4f), (byte)Mathf.Min(255f, b * 1.3f + 30f), c.a);
                return new Color32((byte)(r * 0.8f), (byte)(g * 0.85f), (byte)Mathf.Min(255f, b * 1.2f + 20f), c.a);
            }

            //# 석재/배경 — 명도 램프: 흑요석 → 인디고 석재 → 차가운 회청 → 달빛 없는 하이라이트. 중간 구간 밝기는 원본과 비슷하게(가독성).
            float t = Mathf.Clamp01((luminance - 8f) / 92f);
            Color32 result;
            if (t < 0.12f)
            {
                result = Color32.Lerp(new Color32(8, 8, 18, 255), new Color32(16, 16, 32, 255), t / 0.12f);
            }
            else if (t < 0.45f)
            {
                result = Color32.Lerp(new Color32(16, 16, 32, 255), new Color32(44, 44, 70, 255), (t - 0.12f) / 0.33f);
            }
            else if (t < 0.75f)
            {
                result = Color32.Lerp(new Color32(44, 44, 70, 255), new Color32(78, 80, 104, 255), (t - 0.45f) / 0.30f);
            }
            else
            {
                result = Color32.Lerp(new Color32(78, 80, 104, 255), new Color32(118, 122, 146, 255), (t - 0.75f) / 0.25f);
            }
            result.a = c.a;
            return result;
        }

        //# ---- 상단 벽 띠 → 성벽 ----
        private static void DrawCastleWall(Color32[] img, int w, int wallBottom)
        {
            //# 바탕: 밤하늘 3단(흉벽 사이 빈틈으로 보임)
            for (int y = 0; y < wallBottom; ++y)
            {
                Color32 tone = y < wallBottom * 0.34f ? NightA : (y < wallBottom * 0.7f ? NightB : NightC);
                for (int x = 0; x < w; ++x)
                {
                    img[y * w + x] = tone;
                }
            }

            //# 벽체 — 흉벽 높이(14px) 아래부터 벽돌 줄(높이 10px, 줄마다 반 칸 엇갈림).
            int crenelHeight = 14;
            int bodyTop = crenelHeight;
            for (int y = bodyTop; y < wallBottom; ++y)
            {
                int courseIndex = (y - bodyTop) / 10;
                int within = (y - bodyTop) % 10;
                int offset = (courseIndex % 2) * 20;
                for (int x = 0; x < w; ++x)
                {
                    int brickX = (x + offset) % 40;
                    Color32 brick = BrickMid;
                    if (within == 9 || brickX == 0)
                    {
                        brick = Mortar;
                    }
                    else if (within == 0)
                    {
                        brick = BrickLight;
                    }
                    else if (Hash(101, ((x + offset) / 40) * 31 + courseIndex) > 0.7f)
                    {
                        brick = BrickDark;
                    }
                    img[y * w + x] = brick;
                }
            }

            //# 흉벽 — 48px 간격 돌출 블록(폭 30, 높이 14), 틈은 밤하늘.
            for (int x0 = 4; x0 < w; x0 += 48)
            {
                for (int y = 0; y < crenelHeight; ++y)
                {
                    for (int x = x0; x < x0 + 30; ++x)
                    {
                        Color32 stoneColor = y < 2 ? BrickLight : BrickMid;
                        if (x - x0 < 2)
                        {
                            stoneColor = BrickLight;
                        }
                        else if (x0 + 30 - x <= 2)
                        {
                            stoneColor = BrickDark;
                        }
                        SetPixel(img, w, x, y, stoneColor);
                    }
                }
            }

            //# 기둥 — 약 200px 간격, 폭 36. 기둥머리와 받침이 있는 세로 기둥.
            int pillarCount = 8;
            int[] pillarCenters = new int[pillarCount];
            for (int k = 0; k < pillarCount; ++k)
            {
                pillarCenters[k] = Mathf.RoundToInt((k + 0.5f) * w / pillarCount);
                DrawPillar(img, w, wallBottom, pillarCenters[k], 36, k);
            }

            //# 기둥 사이 깃발(마젠타) + 늘어진 쇠사슬
            for (int k = 0; k < pillarCount - 1; ++k)
            {
                int mid = (pillarCenters[k] + pillarCenters[k + 1]) / 2;
                if (k % 2 == 0)
                {
                    DrawBanner(img, w, wallBottom, mid);
                }
                else
                {
                    DrawChain(img, w, wallBottom, pillarCenters[k] + 18, pillarCenters[k + 1] - 18, crenelHeight + 4);
                }
            }

            //# 성벽 하단 그림자 줄 — 벽과 바닥 경계.
            for (int x = 0; x < w; ++x)
            {
                SetPixel(img, w, x, wallBottom - 3, Mortar);
                SetPixel(img, w, x, wallBottom - 2, PillarDark);
                SetPixel(img, w, x, wallBottom - 1, Hash(102, x / 4) > 0.2f ? NightB : NightA);
            }
        }

        private static void DrawPillar(Color32[] img, int w, int wallBottom, int center, int width, int seed)
        {
            int half = width / 2;
            for (int y = 0; y < wallBottom; ++y)
            {
                bool capital = y < 22 && y >= 10;
                bool baseBlock = y > wallBottom - 16;
                int extra = capital || baseBlock ? 5 : 0;
                for (int x = center - half - extra; x <= center + half + extra; ++x)
                {
                    Color32 col = PillarMid;
                    int local = x - (center - half - extra);
                    int total = width + extra * 2;
                    if (local < 3)
                    {
                        col = PillarLight;
                    }
                    else if (total - local <= 4)
                    {
                        col = PillarDark;
                    }
                    else if (((x + seed * 3) % 9) == 0 && Hash(103, x * 53 + y / 6) > 0.35f)
                    {
                        col = PillarDark;   //# 세로 홈
                    }
                    if (capital && (y == 10 || y == 21))
                    {
                        col = PillarLight;
                    }
                    SetPixel(img, w, x, y, col);
                }
            }
        }

        //# 깃발 — 폭 34, 길이 wallBottom*0.6, 끝은 V 자. 가장자리 금 띠 + 중앙 룬 한 점.
        private static void DrawBanner(Color32[] img, int w, int wallBottom, int center)
        {
            int top = 22;
            int length = Mathf.RoundToInt(wallBottom * 0.62f);
            int half = 17;
            for (int y = top; y < top + length; ++y)
            {
                int notch = y > top + length - 12 ? (y - (top + length - 12)) : 0;
                for (int x = center - half; x <= center + half; ++x)
                {
                    int local = x - (center - half);
                    //# V 자 끝 — 중앙이 먼저 잘림.
                    if (notch > 0 && Mathf.Abs(x - center) < notch)
                        continue;
                    Color32 col = local < 5 ? BannerDark : BannerMid;
                    if (local < 2 || local > half * 2 - 2)
                    {
                        col = BannerTrim;
                    }
                    else if (((y - top) % 14) == 0)
                    {
                        col = BannerDark;   //# 주름
                    }
                    SetPixel(img, w, x, y, col);
                }
            }
            for (int dy = -2; dy <= 2; ++dy)
            {
                SetPixel(img, w, center, top + length / 2 + dy, BannerTrim);
                SetPixel(img, w, center + dy, top + length / 2, BannerTrim);
            }
            //# 걸이 막대
            for (int x = center - half - 3; x <= center + half + 3; ++x)
            {
                SetPixel(img, w, x, top - 2, ChainLight);
                SetPixel(img, w, x, top - 1, ChainDark);
            }
        }

        //# 늘어진 쇠사슬 — 두 점 사이 포물선, 고리 점 번갈아 밝게.
        private static void DrawChain(Color32[] img, int w, int wallBottom, int xLeft, int xRight, int yTop)
        {
            int span = xRight - xLeft;
            float sag = wallBottom * 0.34f;
            for (int x = xLeft; x <= xRight; ++x)
            {
                float t = (x - xLeft) / (float)span;
                int y = yTop + Mathf.RoundToInt(4f * sag * t * (1f - t));
                bool light = ((x - xLeft) / 4) % 2 == 0;
                SetPixel(img, w, x, y, light ? ChainLight : ChainDark);
                SetPixel(img, w, x, y + 1, ChainDark);
                if (light)
                {
                    SetPixel(img, w, x, y - 1, ChainDark);
                }
            }
        }

        //# ---- 아레나 바깥: 석조 타일 이음매·기둥 실루엣·쇠사슬·파편 (바깥 마스크 위에만 그린다) ----
        private static void DecorateOutside(Color32[] img, int w, int h, bool[] outside, Ellipse arena, int wallBottom)
        {
            //# 바닥 석조 타일 이음매 — 64px 격자의 아주 어두운 줄 + 드문 파편 점.
            for (int y = wallBottom; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    if (outside[y * w + x] == false)
                        continue;
                    bool seamX = x % 64 == 0;
                    bool seamY = (y - wallBottom) % 64 == 0;
                    float n = Hash(111, x * 7919 + y);
                    if ((seamX || seamY) && n > 0.15f)
                    {
                        img[y * w + x] = PillarDark;
                    }
                    else if (n > 0.987f)
                    {
                        img[y * w + x] = NightC;
                    }
                    else if (n > 0.972f)
                    {
                        img[y * w + x] = BrickDark;
                    }
                }
            }

            //# 좌우 가장자리 기둥 실루엣 — 아레나 밖 좌우 끝에서 하단까지.
            int[] pillarX = { 24, 112, w - 28, w - 120 };
            for (int k = 0; k < pillarX.Length; ++k)
            {
                int pillarWidth = 38 + Mathf.RoundToInt(Hash(112, k) * 18f);
                int left = pillarX[k] - pillarWidth / 2;
                for (int y = wallBottom; y < h; ++y)
                {
                    for (int x = left; x < left + pillarWidth; ++x)
                    {
                        if (x < 0 || x >= w || outside[y * w + x] == false)
                            continue;
                        Color32 col = PillarDark;
                        if (x - left < 4)
                        {
                            col = PillarMid;
                        }
                        else if (((x + k * 5) % 9) == 0 && Hash(113, y / 6 + x) > 0.4f)
                        {
                            col = PillarMid;
                        }
                        if ((y - wallBottom) % 96 < 3)
                        {
                            col = PillarMid;   //# 기둥 마디
                        }
                        img[y * w + x] = col;
                    }
                }
            }

            //# 하단 파편 더미 띠
            for (int x = -10; x < w + 10; x += 52)
            {
                float radius = 14f + Hash(114, x + 100) * 12f;
                float cy = h - 3f - Hash(115, x + 100) * 8f;
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
                        Color32 rubble = dy < -radius * 0.35f ? BrickMid : BrickDark;
                        if (Hash(116, xx * 17 + y * 31) > 0.94f)
                        {
                            rubble = BrickLight;
                        }
                        img[y * w + xx] = rubble;
                    }
                }
            }

            //# 테두리에서 바깥으로 뻗는 쇠사슬 — 14방향, 고리 번갈아 밝게, 끝으로 갈수록 끊김.
            for (int k = 0; k < 14; ++k)
            {
                float angle = (k / 14f) * Mathf.PI * 2f + Hash(117, k) * 0.2f;
                float length = 46f + Hash(118, k) * 56f;
                float px = arena.Cx + Mathf.Cos(angle) * arena.Rx * 1.01f;
                float py = arena.Cy + Mathf.Sin(angle) * arena.Ry * 1.01f;
                float dirX = Mathf.Cos(angle);
                float dirY = Mathf.Sin(angle) * 0.55f;
                for (float s = 0f; s < length; s += 1f)
                {
                    if (s > length * 0.7f && Hash(119, k * 997 + (int)s / 3) > 0.55f)
                        continue;
                    float wobble = Mathf.Sin(s * 0.2f + k) * 1.8f;
                    int cx = Mathf.RoundToInt(px + dirX * s - dirY * wobble * 0.3f);
                    int cy = Mathf.RoundToInt(py + dirY * s + dirX * wobble * 0.2f);
                    bool light = ((int)s / 4) % 2 == 0;
                    for (int t = 0; t < 2; ++t)
                    {
                        int ry = cy + t;
                        if (cx >= 0 && cx < w && ry >= 0 && ry < h && outside[ry * w + cx])
                        {
                            img[ry * w + cx] = t == 0 && light ? ChainLight : ChainDark;
                        }
                    }
                }
            }
        }

        //# ---- 아레나 위: 테두리 룬 점·균열 (중앙 전투 영역은 거의 건드리지 않아 가독성 유지) ----
        private static void DecorateArena(Color32[] img, int w, int h, bool[] stone, Ellipse arena)
        {
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    if (stone[y * w + x] == false)
                        continue;
                    float d = arena.Distance01(x, y);
                    //# 테두리 띠(0.9~1.0)는 룬/균열 4%, 안쪽(0.6~0.9)은 0.12%, 중앙(<0.6)은 0.01%.
                    float chance = d > 0.9f ? 0.04f : (d > 0.6f ? 0.0012f : 0.0001f);
                    if (Hash(131, x * 2909 + y) > 1f - chance)
                    {
                        PutRuneOrCrack(img, w, h, stone, x, y);
                    }
                }
            }
        }

        private static void PutRuneOrCrack(Color32[] img, int w, int h, bool[] stone, int x, int y)
        {
            bool rune = Hash(132, x * 13 + y) > 0.6f;
            int cols = 2 + (int)(Hash(133, x * 7 + y) * 3f);
            for (int dx = 0; dx < cols; ++dx)
            {
                int xx = x + dx;
                if (xx >= w || stone[y * w + xx] == false)
                    continue;
                float n = Hash(134, xx * 7 + y * 11);
                if (rune)
                {
                    img[y * w + xx] = n > 0.55f ? RuneIndigo : RuneDim;
                }
                else
                {
                    img[y * w + xx] = CrackDark;
                    if (y + 1 < h && stone[(y + 1) * w + xx] && n > 0.5f)
                    {
                        img[(y + 1) * w + xx] = CrackDark;
                    }
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
                Debug.LogError("[ShadowCitadelBackdropBuilder] 아레나 영역을 찾지 못해 이미지 중앙 타원으로 가정");
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
                Debug.LogError($"[ShadowCitadelBackdropBuilder] 임포터 없음: {OutputPath}");
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
            if (files.ContainsKey(SpecKey))
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
            //# DarkForest 항목 뒤(없으면 Battle_Backdrop 뒤)에 삽입.
            JProperty anchor = files.Property("Battle_Backdrop_DarkForest.png") ?? files.Property("Battle_Backdrop.png");
            anchor.AddAfterSelf(new JProperty(SpecKey, entry));
            string text = root.ToString(Newtonsoft.Json.Formatting.Indented).Replace("\r\n", "\n");
            File.WriteAllText(path, text, new System.Text.UTF8Encoding(false));
        }

        private static void RegisterAddressable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError($"[ShadowCitadelBackdropBuilder] Addressables 설정 없음 - {AddressName} 수동 등록 필요");
                return;
            }
            AddressableAssetEntry reference = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(ReferencePrefabPath));
            AddressableAssetGroup group = reference != null ? reference.parentGroup : settings.FindGroup("Resource");
            if (group == null)
            {
                Debug.LogError($"[ShadowCitadelBackdropBuilder] Addressables 그룹 없음 - {AddressName} 수동 등록 필요");
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
