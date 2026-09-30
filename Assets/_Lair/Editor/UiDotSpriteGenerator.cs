using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 도트 던전 UI 공통 스프라이트(Px_*, 시너지 아이콘 4종)를 픽셀 배열로 생성하고 임포트 설정(Point/PPU 25/9-slice)을 지정한다.
    //# 일회용 authoring 툴 — 실행 후 삭제 (Rule 04 §3, 기획서 §2.1). 스프라이트 PNG + .meta 가 단일 진실.
    public static class UiDotSpriteGenerator
    {
        private const string UiDotDir = "Assets/_Lair/Art/Sprites/UiDot";
        private const string SynergyDir = "Assets/_Lair/Art/Sprites/SynergyIcons";
        private const int PixelsPerUnit = 25;

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Ink = Hex("07090e");
        private static readonly Color32 Stone0 = Hex("10141d");
        private static readonly Color32 Stone1 = Hex("171d2a");
        private static readonly Color32 Stone2 = Hex("222a3b");
        private static readonly Color32 Stone3 = Hex("313b52");
        private static readonly Color32 Stone4 = Hex("48546e");
        private static readonly Color32 White = Hex("ffffff");
        private static readonly Color32 Soul = Hex("5ef0b4");
        private static readonly Color32 Soul2 = Hex("1f9e74");
        private static readonly Color32 Gold = Hex("f7c64a");
        private static readonly Color32 Gold2 = Hex("b9832a");
        private static readonly Color32 Blood = Hex("e5484d");
        private static readonly Color32 Blood2 = Hex("8e1f2a");

        //# 12x12 도트 아이콘 팔레트 — 문자: . 투명 / k 윤곽 / 그 외 팔레트 (.mockups/build-ui-redesign.js GLYPHS 와 동일)
        private static readonly Dictionary<char, Color32> GlyphPalette = new Dictionary<char, Color32>
        {
            { 'k', Hex("07090e") }, { 'b', Hex("e8e1cf") }, { 's', Hex("b9b09a") }, { 'g', Hex("f7c64a") },
            { 'o', Hex("b9832a") }, { 't', Hex("5ef0b4") }, { 'd', Hex("1f9e74") }, { 'r', Hex("e5484d") },
            { 'p', Hex("b58cff") }, { 'w', Hex("8e98ad") },
        };

        private class Pix
        {
            public readonly int W;
            public readonly int H;
            public readonly Color32[] Data;

            public Pix(int w, int h)
            {
                W = w;
                H = h;
                Data = new Color32[w * h];
            }

            //# y = 0 이 위쪽 행 (텍스처 저장 시 뒤집는다)
            public void Set(int x, int y, Color32 c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H)
                    return;
                Data[y * W + x] = c;
            }

            public Color32 Get(int x, int y)
            {
                return Data[y * W + x];
            }

            public bool IsCorner(int x, int y)
            {
                return (x == 0 || x == W - 1) && (y == 0 || y == H - 1);
            }
        }

        private class SpriteDef
        {
            public string Dir;
            public string Name;
            public Pix Pix;
            public int BorderL;
            public int BorderR;
            public int BorderT;
            public int BorderB;
            public bool Repeat;
        }

        [MenuItem("Lair/UI/Generate UiDot Sprites")]
        public static void Generate()
        {
            EnsureFolder(UiDotDir);
            List<SpriteDef> defs = BuildDefs();

            for (int i = 0; i < defs.Count; i++)
            {
                WritePng(defs[i]);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            for (int i = 0; i < defs.Count; i++)
            {
                ApplyImportSettings(defs[i]);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[UiDotSpriteGenerator] 스프라이트 {defs.Count}종 생성 완료 — {UiDotDir}, {SynergyDir}");
        }

        private static List<SpriteDef> BuildDefs()
        {
            List<SpriteDef> defs = new List<SpriteDef>();

            defs.Add(Def("Px_Panel", Bevel(8, 8, Ink, Stone4, Stone3, Stone0, Stone0, Stone2), 3, 3, 3, 3));
            defs.Add(Def("Px_PanelDark", Bevel(8, 8, Ink, Ink, Stone1, Stone3, Stone1, Stone1), 3, 3, 3, 3));
            defs.Add(Def("Px_PanelSunk", Sunk(), 2, 2, 2, 2));
            defs.Add(Def("Px_Btn", Button(Stone4, Stone3, Stone1), 2, 2, 2, 3));
            defs.Add(Def("Px_BtnSoul", Button(Soul, Soul2, Hex("0e5a41")), 2, 2, 2, 3));
            defs.Add(Def("Px_BtnGold", Button(Hex("ffe08a"), Hex("d89a2e"), Hex("8a5a14")), 2, 2, 2, 3));
            defs.Add(Def("Px_BtnBlood", Button(Hex("ff7b7f"), Hex("b8323a"), Hex("6b141d")), 2, 2, 2, 3));
            defs.Add(Def("Px_BtnOff", Button(Hex("2a2f3a"), Hex("2a2f3a"), Hex("1b1f27")), 2, 2, 2, 3));
            defs.Add(Def("Px_Plaque", Plaque(), 8, 8, 0, 0));
            defs.Add(Def("Px_CloseX", CloseX(), 0, 0, 0, 0));
            defs.Add(Def("Px_Tab", Tab(Stone1, Clear), 3, 3, 3, 0));
            defs.Add(Def("Px_TabOn", Tab(Stone3, Gold), 3, 3, 3, 0));
            defs.Add(Def("Px_Ring", Ring(4, 4, White), 1, 1, 1, 1));
            defs.Add(Def("Px_CardRing", CardRing(), 3, 3, 3, 3));
            defs.Add(Def("Px_BarBg", BarBg(), 2, 2, 2, 2));
            defs.Add(Def("Px_BarFill", BarFill(), 0, 0, 0, 0));
            defs.Add(Def("Px_Solid", Fill(1, 1, White), 0, 0, 0, 0));
            defs.Add(Def("Px_Divider", Divider(), 1, 1, 0, 0));
            defs.Add(Def("Px_Badge_Gold", Badge(Gold2, Ink), 2, 2, 2, 2));
            defs.Add(Def("Px_Badge_Soul", Badge(Soul2, Ink), 2, 2, 2, 2));
            defs.Add(Def("Px_Badge_Red", Badge(Ink, Blood), 2, 2, 2, 2));
            SpriteDef hatch = Def("Px_Hatch", Hatch(), 0, 0, 0, 0);
            hatch.Repeat = true;
            defs.Add(hatch);
            defs.Add(Def("Px_SoulCoin", SoulCoin(), 0, 0, 0, 0));
            defs.Add(Def("Px_Star", FromMap(new[] { "..#..", "#####", ".###.", ".###.", ".#.#." }, new Dictionary<char, Color32> { { '#', White } }), 0, 0, 0, 0));
            defs.Add(Def("Px_Dot", Dot(), 0, 0, 0, 0));
            defs.Add(Def("Px_Lock", Lock(), 0, 0, 0, 0));
            defs.Add(Def("Px_ArrowR", Arrow(), 0, 0, 0, 0));
            defs.Add(Def("Px_Rays", Rays(160), 0, 0, 0, 0));

            defs.Add(Def("Px_Glyph_Castle", Glyph(GlyphCastle), 0, 0, 0, 0));
            defs.Add(Def("Px_Glyph_Shop", Glyph(GlyphShop), 0, 0, 0, 0));
            defs.Add(Def("Px_Glyph_Book", Glyph(GlyphBook), 0, 0, 0, 0));
            defs.Add(Def("Px_Glyph_Scroll", Glyph(GlyphScroll), 0, 0, 0, 0));
            defs.Add(Def("Px_Glyph_Tomb", Glyph(GlyphTomb), 0, 0, 0, 0));
            defs.Add(Def("Px_Glyph_Crown", Glyph(GlyphCrown), 0, 0, 0, 0));

            //# 시너지 아이콘 4종은 같은 파일명으로 덮어써 GUID 를 유지한다 (제안 8)
            defs.Add(Def("TANK", Glyph(GlyphShield), 0, 0, 0, 0, SynergyDir));
            defs.Add(Def("DPS", Glyph(GlyphSword), 0, 0, 0, 0, SynergyDir));
            defs.Add(Def("SWARM", Glyph(GlyphSwarm), 0, 0, 0, 0, SynergyDir));
            defs.Add(Def("DEBUFF", Glyph(GlyphCurse), 0, 0, 0, 0, SynergyDir));
            return defs;
        }

        private static SpriteDef Def(string name, Pix pix, int l, int r, int t, int b, string dir = UiDotDir)
        {
            return new SpriteDef { Dir = dir, Name = name, Pix = pix, BorderL = l, BorderR = r, BorderT = t, BorderB = b };
        }

        //# ---- 파일 쓰기 / 임포트 ----

        private static void WritePng(SpriteDef def)
        {
            Texture2D tex = new Texture2D(def.Pix.W, def.Pix.H, TextureFormat.RGBA32, false);
            Color32[] flipped = new Color32[def.Pix.Data.Length];
            for (int y = 0; y < def.Pix.H; y++)
            {
                for (int x = 0; x < def.Pix.W; x++)
                {
                    flipped[(def.Pix.H - 1 - y) * def.Pix.W + x] = def.Pix.Data[y * def.Pix.W + x];
                }
            }

            tex.SetPixels32(flipped);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes($"{def.Dir}/{def.Name}.png", bytes);
        }

        private static void ApplyImportSettings(SpriteDef def)
        {
            string path = $"{def.Dir}/{def.Name}.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[UiDotSpriteGenerator] 임포터를 찾지 못함: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = def.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.spriteBorder = new Vector4(def.BorderL, def.BorderB, def.BorderR, def.BorderT);

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        //# ---- 픽셀 빌더 ----

        private static Color32 Hex(string hex)
        {
            byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color32(r, g, b, 255);
        }

        private static Pix Fill(int w, int h, Color32 c)
        {
            Pix p = new Pix(w, h);
            for (int i = 0; i < p.Data.Length; i++)
            {
                p.Data[i] = c;
            }

            return p;
        }

        private static Pix FromMap(string[] rows, Dictionary<char, Color32> palette)
        {
            Pix p = new Pix(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char ch = rows[y][x];
                    if (ch == '.')
                        continue;
                    p.Set(x, y, palette[ch]);
                }
            }

            return p;
        }

        private static Pix Glyph(string[] rows)
        {
            return FromMap(rows, GlyphPalette);
        }

        //# 윤곽 1칸 + 안쪽 베벨 1칸 + 본체. 바깥 모서리 1칸은 투명(계단형 코너).
        private static Pix Bevel(int w, int h, Color32 ink, Color32 top, Color32 left, Color32 bottom, Color32 right, Color32 body)
        {
            Pix p = new Pix(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    Color32 c;
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    {
                        c = ink;
                    }
                    else if (y == 1)
                    {
                        c = top;
                    }
                    else if (x == 1)
                    {
                        c = left;
                    }
                    else if (y == h - 2)
                    {
                        c = bottom;
                    }
                    else if (x == w - 2)
                    {
                        c = right;
                    }
                    else
                    {
                        c = body;
                    }
                    p.Set(x, y, c);
                }
            }

            return p;
        }

        private static Pix Sunk()
        {
            Pix p = new Pix(6, 6);
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    Color32 c = Hex("0c1017");
                    if (y == 0 || x == 0)
                    {
                        c = Stone0;
                    }
                    else if (y == 5 || x == 5)
                    {
                        c = Stone3;
                    }
                    p.Set(x, y, c);
                }
            }

            return p;
        }

        private static Pix Button(Color32 top, Color32 body, Color32 bottom)
        {
            Pix p = new Pix(8, 9);
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    Color32 c;
                    if (y == 0 || y == 8 || x == 0 || x == 7)
                    {
                        c = Ink;
                    }
                    else if (y == 1)
                    {
                        c = top;
                    }
                    else if (y == 6 || y == 7)
                    {
                        c = bottom;
                    }
                    else
                    {
                        c = body;
                    }
                    p.Set(x, y, c);
                }
            }

            return p;
        }

        private static Pix Plaque()
        {
            Color32 wood = Hex("3a2f22");
            Color32 hi = Hex("6b563b");
            Color32 shadow = Hex("231a11");
            Pix p = new Pix(20, 13);
            for (int y = 0; y < 13; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    Color32 c;
                    if (x == 0 || y == 0 || x == 19 || y == 12)
                    {
                        c = Ink;
                    }
                    else if (y == 1)
                    {
                        c = hi;
                    }
                    else if (y == 11)
                    {
                        c = shadow;
                    }
                    else
                    {
                        c = wood;
                    }
                    p.Set(x, y, c);
                }
            }

            int[] rivetX = { 3, 16 };
            for (int i = 0; i < rivetX.Length; i++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        p.Set(rivetX[i] + dx, 6 + dy, Ink);
                    }
                }

                p.Set(rivetX[i], 6, Gold);
            }

            return p;
        }

        private static Pix CloseX()
        {
            Pix p = new Pix(11, 11);
            for (int y = 0; y < 11; y++)
            {
                for (int x = 0; x < 11; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    Color32 c;
                    if (x == 0 || y == 0 || x == 10 || y == 10)
                    {
                        c = Ink;
                    }
                    else if (y == 1)
                    {
                        c = Hex("ff7b7f");
                    }
                    else if (y == 9)
                    {
                        c = Blood2;
                    }
                    else
                    {
                        c = Hex("b8323a");
                    }
                    p.Set(x, y, c);
                }
            }

            Color32 mark = Hex("f5f0e0");
            for (int i = 3; i <= 7; i++)
            {
                p.Set(i, i, mark);
                p.Set(10 - i, i, mark);
            }

            return p;
        }

        //# 상/좌/우 윤곽, 하단 열림. cap 이 투명이 아니면 상단 안쪽 1칸을 cap 색으로 칠한다.
        private static Pix Tab(Color32 body, Color32 cap)
        {
            Pix p = new Pix(8, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if ((x == 0 || x == 7) && y == 0)
                        continue;
                    Color32 c;
                    if (y == 0 || x == 0 || x == 7)
                    {
                        c = Ink;
                    }
                    else if (y == 1 && cap.a != 0)
                    {
                        c = cap;
                    }
                    else
                    {
                        c = body;
                    }
                    p.Set(x, y, c);
                }
            }

            return p;
        }

        private static Pix Ring(int w, int h, Color32 c)
        {
            Pix p = new Pix(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    {
                        p.Set(x, y, c);
                    }
                }
            }

            return p;
        }

        private static Pix CardRing()
        {
            Pix p = new Pix(8, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    int edge = Mathf.Min(Mathf.Min(x, 7 - x), Mathf.Min(y, 7 - y));
                    if (edge == 0)
                    {
                        p.Set(x, y, Ink);
                    }
                    else if (edge == 1)
                    {
                        p.Set(x, y, White);
                    }
                    else if (edge == 2)
                    {
                        p.Set(x, y, Ink);
                    }
                }
            }

            return p;
        }

        private static Pix BarBg()
        {
            Pix p = new Pix(6, 6);
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    bool edge = x == 0 || y == 0 || x == 5 || y == 5;
                    p.Set(x, y, edge ? Ink : Hex("0b0e14"));
                }
            }

            return p;
        }

        //# 흰색 기준 명도 차만 준다 (위 밝게 / 아래 어둡게) — Image.color tint 로 색을 입힌다.
        private static Pix BarFill()
        {
            Color32[] rows = { White, new Color32(214, 214, 214, 255), new Color32(150, 150, 150, 255), new Color32(150, 150, 150, 255) };
            Pix p = new Pix(4, 4);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    p.Set(x, y, rows[y]);
                }
            }

            return p;
        }

        private static Pix Divider()
        {
            Pix p = new Pix(4, 2);
            for (int x = 0; x < 4; x++)
            {
                p.Set(x, 0, Ink);
                p.Set(x, 1, Stone3);
            }

            return p;
        }

        private static Pix Badge(Color32 ring, Color32 body)
        {
            Pix p = new Pix(5, 5);
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    if (p.IsCorner(x, y))
                        continue;
                    bool edge = x == 0 || y == 0 || x == 4 || y == 4;
                    p.Set(x, y, edge ? ring : body);
                }
            }

            return p;
        }

        private static Pix Hatch()
        {
            Color32 a = Hex("11151d");
            Color32 b = Hex("0c1017");
            Pix p = new Pix(4, 4);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    p.Set(x, y, (x + y) % 4 < 2 ? a : b);
                }
            }

            return p;
        }

        private static Pix SoulCoin()
        {
            return FromMap(
                new[] { ".sss.", "ssssS", "ssssS", "sssSS", ".SSS." },
                new Dictionary<char, Color32> { { 's', Soul }, { 'S', Soul2 } });
        }

        private static Pix Dot()
        {
            Pix p = new Pix(3, 3);
            p.Set(1, 0, Ink);
            p.Set(0, 1, Ink);
            p.Set(2, 1, Ink);
            p.Set(1, 2, Ink);
            p.Set(1, 1, White);
            return p;
        }

        private static Pix Lock()
        {
            return FromMap(
                new[]
                {
                    "..kkkk..",
                    ".kssssk.",
                    ".ks..sk.",
                    ".ks..sk.",
                    "kkkkkkkk",
                    "kssssssk",
                    "ksskkssk",
                    "kssskssk",
                    "kkkkkkkk",
                },
                new Dictionary<char, Color32> { { 'k', Ink }, { 's', Stone4 } });
        }

        //# ▶ 화살표 (◀ 는 X 스케일 -1). 좌측이 수직 절단면, 우측 끝이 뾰족.
        private static Pix Arrow()
        {
            int[] widths = { 2, 3, 4, 5, 4, 3, 2 };
            Pix p = new Pix(5, 7);
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < widths[y]; x++)
                {
                    p.Set(x, y, White);
                }
            }

            return p;
        }

        //# 8도 폭 광선을 22.5도(=360/16) 주기로 배치 — 이음매 없이 닫히도록 22도가 아닌 22.5도를 쓴다. 중앙·바깥은 페이드.
        private static Pix Rays(int size)
        {
            Pix p = new Pix(size, size);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > half)
                        continue;
                    float deg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float d = Mathf.Repeat(deg + 11.25f, 22.5f) - 11.25f;
                    if (Mathf.Abs(d) > 4f)
                        continue;
                    float fadeIn = Mathf.Clamp01((r - 8f) / 32f);
                    float fadeOut = Mathf.Clamp01((half - r) / 40f);
                    float a = 0.22f * fadeIn * fadeOut;
                    byte alpha = (byte)(Mathf.Round(a * 16f) / 16f * 255f);
                    if (alpha == 0)
                        continue;
                    p.Set(x, y, new Color32(Gold.r, Gold.g, Gold.b, alpha));
                }
            }

            return p;
        }

        //# ---- 12x12 도트 글리프 데이터 ----

        private static readonly string[] GlyphCastle =
        {
            "k.k.kkkk.k.k", "kbkbkbbkbkbk", "kbbbkbbkbbbk", "kkbbbbbbbbkk", ".kbbbbbbbbk.", ".kbkkbbkkbk.",
            ".kbkkbbkkbk.", ".kbbbbbbbbk.", ".kbbbkkbbbk.", ".kbbkggkbbk.", ".kbbkggkbbk.", ".kkkkkkkkkk.",
        };

        private static readonly string[] GlyphShop =
        {
            "....kkkk....", "...kgggok...", "..kggkkgok..", "..kgk..kgk..", ".kkkkkkkkkk.", ".kgggggggok.",
            "kgggkkggggok", "kggkggkgggok", "kgggggkggook", "kggkkkggoook", ".kgggggoook.", "..kkkkkkkk..",
        };

        private static readonly string[] GlyphBook =
        {
            ".kkkkkkkkkk.", "kppppkppppkk", "kppppkppppks", "kpbbpkpbbpks", "kppppkppppks", "kpbbpkpbbpks",
            "kppppkppppks", "kppppkppppks", "kpppkkkpppks", "kkkkksskkkks", ".ssssssssss.", ".kkkkkkkkkk.",
        };

        private static readonly string[] GlyphScroll =
        {
            ".kkkkkkkkk..", "kbbbbbbbbbk.", "kskkkkkkkbk.", ".kbbbbbbbbk.", ".kbkkkkkbbk.", ".kbbbbbbbbk.",
            ".kbkkkkbbbk.", ".kbbbbbbbbk.", ".kbkkkbrrbk.", ".kbbbbrrrbk.", "kbbbbbbrbbbk", ".kkkkkkkkkk.",
        };

        private static readonly string[] GlyphTomb =
        {
            "...kkkkkk...", "..kwwwwwwk..", ".kwwwkkwwwk.", ".kwwkkkkwwk.", ".kwwwkkwwwk.", ".kwwwkkwwwk.",
            ".kwwwwwwwwk.", ".kwkkkkkkwk.", ".kwwwwwwwwk.", ".kwwwwwwwwk.", "kkkkkkkkkkkk", "kddttddttddk",
        };

        private static readonly string[] GlyphCrown =
        {
            "............", "k....kk....k", "kk..kgok..kk", "kgk.kgok.kgk", "kgokggookgok", "kggggoggggok",
            "kgrggogtggok", "kggggoggggok", "kkkkkkkkkkkk", "kgggggggoook", "kkkkkkkkkkkk", "............",
        };

        private static readonly string[] GlyphShield =
        {
            "kkkkkkkkkkkk", "kbbbbbbbbbsk", "kbwwwwwwwwsk", "kbwwwbbwwwsk", "kbwwwbbwwwsk", "kbwbbbbbbwsk",
            "kbwwwbbwwwsk", ".kbwwbbwwsk.", ".kbwwwwwwsk.", "..kbwwwwsk..", "...kbwwsk...", "....kkkk....",
        };

        private static readonly string[] GlyphSword =
        {
            ".........kkk", "........kbbk", ".......kbbsk", "......kbbsk.", ".....kbbsk..", ".k..kbbsk...",
            "kgk.kbsk....", ".kgkbsk.....", "..kgkk......", ".kokgk......", "kok.kgk.....", "kk...k......",
        };

        private static readonly string[] GlyphSwarm =
        {
            "..kkk.......", ".kttdk..kkk.", ".ktkdk.kttdk", ".kttdk.ktkdk", "..kkk..kttdk", ".......kkkk.",
            "...kkk......", "..kttdk.kkk.", "..ktkdkkttdk", "..kttdkktkdk", "..kdkdkkttdk", "...k.k..kkk.",
        };

        private static readonly string[] GlyphCurse =
        {
            "...kkkkkk...", "..kppppppk..", ".kppppppppk.", ".kpkkppkkpk.", ".kpkbppkbpk.", ".kppppppppk.",
            "..kpppkppk..", "...kpppppk..", "...kpkpkpk..", "....kkkkk...", ".k........k.", "kpk......kpk",
        };
    }
}
