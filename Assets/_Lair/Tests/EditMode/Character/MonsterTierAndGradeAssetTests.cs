using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Character
{
    //# 에셋·데이터 정합 게이트(읽기 전용) - monster-tier-sheets U-T1~U-T3·X-T1·X-T8, monster-grades T1·T2·T3·T8·T12.
    //# 시트 PNG 헤더·슬라이스 / 티어 SO 18개 / Monster_SheetSpec / 애니메이션 클립 / 등급 Prefab Variant 24 구조를 AssetDatabase 로 검사한다.
    //# Addressables 주소·라벨(U-T3 일부)은 Addressables 에디터 어셈블리 미참조라 이 스위트 밖이다.
    public class MonsterTierAndGradeAssetTests
    {
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/Monsters2D/";
        private const string SpecPath = SpriteDir + "Monster_SheetSpec.json";
        private const string TierDir = "Assets/_Lair/Art/MonsterTiers/";
        private const string CharDir = "Assets/_Lair/Art/Characters/";
        private const string ClipDir = "Assets/_Lair/Art/Animations/Monsters2D/";
        private static readonly string[] Suffixes = { "", "_T1", "_T2", "_T3" };
        private static readonly string[] Masks = { "", "_Emission" };
        private static readonly string[] ClipNames = { "Idle", "Move", "Attack", "Hit", "Death" };

        private static IEnumerable<string> BaseNames()
        {
            foreach (EMonster monster in MonsterGrades.BaseMonsters)
            {
                yield return monster.ToString();
            }
        }

        private static JObject Spec() => JObject.Parse(File.ReadAllText(SpecPath));

        private static JObject SpecMonster(string baseName)
        {
            foreach (JToken token in (JArray)Spec()["monsters"])
            {
                if ((string)token["base"] == baseName)
                    return (JObject)token;
            }
            Assert.Fail($"Monster_SheetSpec.json 에 {baseName} 없음");
            return null;
        }

        private static string[] StringArray(JToken token)
        {
            JArray array = (JArray)token;
            string[] result = new string[array.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (string)array[i];
            }
            return result;
        }

        //# ───────── Monster_SheetSpec.json (5행 규격) ─────────

        [Test]
        public void SheetSpec_은_5행_규격이고_Spawn_행이_출력에서_빠졌다()
        {
            JObject spec = Spec();

            Assert.AreEqual(5, (int)spec["rows"], "시트 행 수");
            Assert.AreEqual(6, (int)spec["cols"]);
            Assert.AreEqual(192, (int)spec["cell"]);
            Assert.AreEqual(new[] { "idle", "move", "attack", "hit", "death" }, StringArray(spec["outRowOrder"]), "출력 행 순서(spawn 삭제)");
            Assert.AreEqual(new[] { "spawn" }, StringArray(spec["source"]["discardRows"]), "원본에서 spawn 만 버림");
        }

        [Test]
        public void SheetSpec_에서_삭제된_필드는_어디에도_남아_있지_않다()
        {
            JObject spec = Spec();

            foreach (string removed in new[] { "rowsBody", "rowsOverlay", "topY", "ppuOverlay", "floatPxOverlay", "anchorOverrides" })
            {
                Assert.AreEqual(0, new List<JToken>(spec.SelectTokens("$.." + removed)).Count, $"삭제 필드 '{removed}' 잔존");
            }
        }

        [Test]
        public void SheetSpec_은_기준_6종과_종족당_티어_3개를_가진다()
        {
            JArray monsters = (JArray)Spec()["monsters"];
            Assert.AreEqual(6, monsters.Count);

            foreach (string baseName in BaseNames())
            {
                JArray tiers = (JArray)SpecMonster(baseName)["tiers"];
                Assert.AreEqual(3, tiers.Count, $"{baseName} 티어 수");
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(i + 1, (int)tiers[i]["tier"], $"{baseName} 티어 번호");
                    Assert.AreEqual($"{baseName}_Sheet_T{i + 1}", (string)tiers[i]["sheet"], $"{baseName} 시트 이름");
                    Assert.Greater((float)tiers[i]["hpBarHeight"], 0f, $"{baseName} T{i + 1} hpBarHeight");
                }
            }
        }

        //# ───────── 시트 PNG (X-T1·U-T1) ─────────

        private static void ReadPngSize(string path, out int width, out int height)
        {
            byte[] head = new byte[24];
            using (FileStream stream = File.OpenRead(path))
            {
                int read = stream.Read(head, 0, head.Length);
                Assert.AreEqual(head.Length, read, $"PNG 헤더 읽기 실패: {path}");
            }
            width = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
            height = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
        }

        [Test]
        public void 몸_T_발광마스크_시트_48장은_전부_1152x960_이다()
        {
            int checkedCount = 0;
            foreach (string baseName in BaseNames())
            {
                foreach (string suffix in Suffixes)
                {
                    foreach (string mask in Masks)
                    {
                        string path = $"{SpriteDir}{baseName}_Sheet{suffix}{mask}.png";
                        Assert.IsTrue(File.Exists(path), $"파일 없음: {path}");
                        ReadPngSize(path, out int width, out int height);
                        Assert.AreEqual(1152, width, $"{path} 가로");
                        Assert.AreEqual(960, height, $"{path} 세로(Spawn 행 제거 후 5행)");
                        checkedCount++;
                    }
                }
            }
            Assert.AreEqual(48, checkedCount, "T 18 + 마스크 18 + 풋내기 12");
        }

        [Test]
        public void 시트_48장은_모두_192x192_슬라이스_30개이고_30_35번_스프라이트가_없다()
        {
            List<string> problems = new List<string>();
            foreach (string baseName in BaseNames())
            {
                foreach (string suffix in Suffixes)
                {
                    foreach (string mask in Masks)
                    {
                        string stem = $"{baseName}_Sheet{suffix}{mask}";
                        string path = $"{SpriteDir}{stem}.png";
                        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                        HashSet<int> indexes = new HashSet<int>();
                        int sprites = 0;
                        foreach (Object obj in all)
                        {
                            Sprite sprite = obj as Sprite;
                            if (sprite == null)
                                continue;
                            sprites++;
                            if (sprite.rect.width != 192f || sprite.rect.height != 192f)
                            {
                                problems.Add($"{sprite.name} 크기 {sprite.rect.size}");
                            }
                            Match match = Regex.Match(sprite.name, "^" + Regex.Escape(stem) + @"_(\d+)$");
                            if (match.Success == false)
                            {
                                problems.Add($"{sprite.name} 이름 규칙 위반");
                                continue;
                            }
                            indexes.Add(int.Parse(match.Groups[1].Value));
                        }
                        if (sprites != 30)
                        {
                            problems.Add($"{stem} 스프라이트 {sprites}개(기대 30)");
                        }
                        for (int i = 0; i < 30; i++)
                        {
                            if (indexes.Contains(i) == false)
                            {
                                problems.Add($"{stem}_{i} 없음");
                            }
                        }
                        for (int i = 30; i <= 35; i++)
                        {
                            if (indexes.Contains(i))
                            {
                                problems.Add($"{stem}_{i} 잔존(Spawn 행)");
                            }
                        }
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        //# ───────── 티어 SO 18개 (U-T3·X-T8) ─────────

        [Test]
        public void EMonsterTierSheet_값마다_같은_이름의_SO_가_있고_합쳐_18개다()
        {
            string[] names = System.Enum.GetNames(typeof(EMonsterTierSheet));
            Assert.AreEqual(18, names.Length);
            foreach (string name in names)
            {
                MonsterTierSheetSet set = AssetDatabase.LoadAssetAtPath<MonsterTierSheetSet>($"{TierDir}{name}.asset");
                Assert.IsNotNull(set, $"{name}.asset - Enum 값명 = 파일명");
            }
        }

        [Test]
        public void 티어_SO_는_프레임_30개_이름_인덱스_일치_HP바_높이가_스펙과_같다()
        {
            List<string> problems = new List<string>();
            foreach (string baseName in BaseNames())
            {
                JArray tiers = (JArray)SpecMonster(baseName)["tiers"];
                for (int tier = 1; tier <= 3; tier++)
                {
                    string key = $"{baseName}T{tier}";
                    MonsterTierSheetSet set = AssetDatabase.LoadAssetAtPath<MonsterTierSheetSet>($"{TierDir}{key}.asset");
                    if (set == null)
                    {
                        problems.Add($"{key} 없음");
                        continue;
                    }
                    if (set.Count != MonsterTierSheetSet.FrameCount)
                    {
                        problems.Add($"{key} 프레임 {set.Count}개");
                    }
                    for (int i = 0; i < set.Count; i++)
                    {
                        Sprite frame = set.GetFrame(i);
                        if (frame == null)
                        {
                            problems.Add($"{key} frame {i} 비어 있음");
                        }
                        else if (frame.name != $"{baseName}_Sheet_T{tier}_{i}")
                        {
                            problems.Add($"{key} frame {i} 이름 {frame.name}");
                        }
                    }
                    float specHeight = (float)tiers[tier - 1]["hpBarHeight"];
                    if (set.HpBarHeightWorld <= 0f || Mathf.Abs(set.HpBarHeightWorld - specHeight) > 0.0001f)
                    {
                        problems.Add($"{key} hpBarHeightWorld {set.HpBarHeightWorld} (스펙 {specHeight})");
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        //# ───────── 애니메이션 클립 (U-T1·U-T6) ─────────

        [Test]
        public void 몬스터_클립_30개의_스프라이트_키프레임은_모두_0_29_범위의_몸_시트_칸이다()
        {
            List<string> problems = new List<string>();
            foreach (string baseName in BaseNames())
            {
                Regex pattern = new Regex("^" + baseName + @"_Sheet_(\d+)$");
                foreach (string clipName in ClipNames)
                {
                    string path = $"{ClipDir}{baseName}_{clipName}.anim";
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip == null)
                    {
                        problems.Add($"{path} 없음");
                        continue;
                    }
                    EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    if (bindings.Length == 0)
                    {
                        problems.Add($"{path} 스프라이트 커브 없음");
                    }
                    foreach (EditorCurveBinding binding in bindings)
                    {
                        foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                        {
                            Sprite sprite = key.value as Sprite;
                            if (sprite == null)
                            {
                                problems.Add($"{path} t={key.time} Missing 스프라이트");
                                continue;
                            }
                            Match match = pattern.Match(sprite.name);
                            if (match.Success == false || int.Parse(match.Groups[1].Value) >= 30)
                            {
                                problems.Add($"{path} t={key.time} {sprite.name}");
                            }
                        }
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        //# ───────── 프리팹: 1등급 기준 6종 (T8) ─────────

        private static GameObject LoadPrefab(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharDir}{name}.prefab");
            Assert.IsNotNull(prefab, $"{name}.prefab 없음");
            return prefab;
        }

        private static bool IsGradeFxName(string name) => name == "AuraGrade" || name.StartsWith("AuraFxTornado") || name.StartsWith("AuraFxSunburst");

        [Test]
        public void 기준_6종_프리팹은_등급_연출_컴포넌트와_연출_자식이_없다()
        {
            foreach (string baseName in BaseNames())
            {
                GameObject prefab = LoadPrefab(baseName);

                Assert.IsNull(prefab.GetComponentInChildren<MonsterGradeVisual>(true), $"{baseName} 에 MonsterGradeVisual");
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                {
                    Assert.IsFalse(IsGradeFxName(child.name), $"{baseName} 에 연출 자식 {child.name}");
                }
            }
        }

        [Test]
        public void 기준_6종과_등급_24종_프리팹은_티어_시트_텍스처와_티어_세트에_직접_의존하지_않는다()
        {
            List<string> problems = new List<string>();
            foreach (string baseName in BaseNames())
            {
                List<string> names = new List<string> { baseName };
                for (int grade = 2; grade <= 5; grade++)
                {
                    names.Add($"{baseName}G{grade}");
                }
                foreach (string name in names)
                {
                    foreach (string dependency in AssetDatabase.GetDependencies($"{CharDir}{name}.prefab", true))
                    {
                        if (dependency.Contains("_Sheet_T") || dependency.Contains("/MonsterTiers/"))
                        {
                            problems.Add($"{name} → {dependency}");
                        }
                    }
                }
            }
            Assert.IsEmpty(problems, "U-T2/G-T3 직접 참조 누수:\n" + string.Join("\n", problems));
        }

        //# ───────── 프리팹: 등급 Variant 24 (T1·T2·T3·T12) ─────────

        private static readonly Dictionary<int, string[]> ExpectedAdded = new Dictionary<int, string[]>
        {
            { 2, new[] { "AuraGrade" } },
            { 3, new[] { "AuraGrade" } },
            { 4, new[] { "AuraFxTornado0", "AuraFxTornado1", "AuraGrade" } },
            { 5, new[] { "AuraFxSunburst", "AuraFxTornado0", "AuraFxTornado1", "AuraFxTornado2", "AuraFxTornado3", "AuraGrade" } },
        };

        private static List<string> AddedNames(GameObject variant, GameObject basePrefab)
        {
            List<string> names = new List<string>();
            //# 루트 이름은 Variant 마다 다르므로(WispG5 vs Wisp) 양쪽 모두 루트를 제외한다.
            foreach (Transform t in variant.GetComponentsInChildren<Transform>(true))
            {
                if (t != variant.transform)
                {
                    names.Add(t.name);
                }
            }
            foreach (Transform t in basePrefab.GetComponentsInChildren<Transform>(true))
            {
                if (t != basePrefab.transform)
                {
                    names.Remove(t.name);
                }
            }
            names.Sort(System.StringComparer.Ordinal);
            return names;
        }

        private static Transform FindByName(GameObject prefab, string name)
        {
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            return null;
        }

        [Test]
        public void 등급_Variant_24종은_기준_프리팹의_Variant_이다()
        {
            foreach (string baseName in BaseNames())
            {
                for (int grade = 2; grade <= 5; grade++)
                {
                    string name = $"{baseName}G{grade}";
                    GameObject variant = LoadPrefab(name);

                    Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variant), $"{name} 은 Prefab Variant");
                    GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(variant);
                    Assert.IsNotNull(source, $"{name} 원본");
                    Assert.AreEqual($"{CharDir}{baseName}.prefab", AssetDatabase.GetAssetPath(source), $"{name} 의 원본 = 기준 프리팹");
                }
            }
        }

        [Test]
        public void 등급별_추가_자식은_오라1_2_3등급_기둥2_4등급_기둥4_후광1_5등급이고_전부_Aura_접두다()
        {
            foreach (string baseName in BaseNames())
            {
                GameObject basePrefab = LoadPrefab(baseName);
                for (int grade = 2; grade <= 5; grade++)
                {
                    string name = $"{baseName}G{grade}";
                    List<string> added = AddedNames(LoadPrefab(name), basePrefab);

                    Assert.AreEqual(ExpectedAdded[grade], added.ToArray(), $"{name} 추가 자식");
                    foreach (string childName in added)
                    {
                        Assert.IsTrue(childName.StartsWith("Aura"), $"{name}/{childName} - HitFlash·AttackJuice 제외 규칙(Aura 접두)");
                    }
                }
            }
        }

        [Test]
        public void 연출_자식은_꺼진_채로_저장되고_시트_컴포넌트를_가진다()
        {
            foreach (string baseName in BaseNames())
            {
                for (int grade = 2; grade <= 5; grade++)
                {
                    string name = $"{baseName}G{grade}";
                    GameObject variant = LoadPrefab(name);
                    foreach (string childName in ExpectedAdded[grade])
                    {
                        Transform child = FindByName(variant, childName);
                        Assert.IsNotNull(child, $"{name}/{childName}");
                        Assert.IsFalse(child.gameObject.activeSelf, $"{name}/{childName} 은 Play 가 켜기 전까지 비활성(풀 Push 상태와 동일)");
                        Assert.IsNotNull(child.GetComponent<GradeFxSheet>(), $"{name}/{childName} GradeFxSheet");
                    }
                }
            }
        }

        [Test]
        public void 등급_Variant_의_MonsterGradeVisual_배선은_연출_자식_수와_스펙_cellWorld_와_일치한다()
        {
            foreach (string baseName in BaseNames())
            {
                float cellWorld = (float)SpecMonster(baseName)["cellWorld"];
                for (int grade = 2; grade <= 5; grade++)
                {
                    string name = $"{baseName}G{grade}";
                    MonsterGradeVisual visual = LoadPrefab(name).GetComponentInChildren<MonsterGradeVisual>(true);
                    Assert.IsNotNull(visual, $"{name} MonsterGradeVisual");
                    Assert.IsNotNull(visual.GetComponent<LairCharacter>(), $"{name} MonsterGradeVisual 은 LairCharacter 와 같은 GameObject(Get<IMonsterGradeVisual> 조회·사망 구독 전제)");

                    SerializedObject so = new SerializedObject(visual);
                    Assert.AreEqual(cellWorld, so.FindProperty("_cellWorld").floatValue, 0.001f, $"{name} _cellWorld = 종족 cellWorld");
                    Assert.IsNotNull(so.FindProperty("_body").objectReferenceValue, $"{name} _body");
                    Assert.IsNotNull(so.FindProperty("_visualRoot").objectReferenceValue, $"{name} _visualRoot");
                    Assert.IsNotNull(so.FindProperty("_sigil").objectReferenceValue, $"{name} _sigil");
                    Assert.IsNotNull(so.FindProperty("_aura").objectReferenceValue, $"{name} _aura");

                    int expectedTornado = grade == 4 ? 2 : (grade == 5 ? 4 : 0);
                    SerializedProperty tornados = so.FindProperty("_tornados");
                    int wired = 0;
                    for (int i = 0; i < tornados.arraySize; i++)
                    {
                        if (tornados.GetArrayElementAtIndex(i).objectReferenceValue != null)
                        {
                            wired++;
                        }
                    }
                    Assert.AreEqual(expectedTornado, wired, $"{name} _tornados 배선 수(빠지면 슬롯이 조용히 건너뛰어짐)");
                    Assert.AreEqual(grade == 5, so.FindProperty("_sunburst").objectReferenceValue != null, $"{name} _sunburst");
                }
            }
        }

        [Test]
        public void 등급_Variant_는_크기_HP바_콜라이더를_덮어쓰지_않아_sizeMul_이_이중_곱셈되지_않는다()
        {
            foreach (string baseName in BaseNames())
            {
                GameObject basePrefab = LoadPrefab(baseName);
                for (int grade = 2; grade <= 5; grade++)
                {
                    string name = $"{baseName}G{grade}";
                    GameObject variant = LoadPrefab(name);

                    foreach (PropertyModification mod in PrefabUtility.GetPropertyModifications(variant) ?? new PropertyModification[0])
                    {
                        Assert.IsFalse(mod.propertyPath.StartsWith("m_LocalScale"), $"{name} 이 스케일을 덮어씀({mod.propertyPath}) - 런타임 Configure 가 sizeMul 을 곱하므로 이중 곱셈");
                        Assert.IsFalse(mod.propertyPath.Contains("_hpBarHeightByTier"), $"{name} 이 HP바 높이 표를 덮어씀 - ScaleBySize 와 이중 곱셈");
                        Assert.IsFalse(mod.propertyPath.StartsWith("m_Radius") || mod.propertyPath.StartsWith("m_Height") || mod.propertyPath.StartsWith("m_Center") || mod.propertyPath.StartsWith("m_Size"), $"{name} 이 콜라이더를 덮어씀({mod.propertyPath})");
                    }

                    Assert.AreEqual(basePrefab.transform.localScale, variant.transform.localScale, $"{name} 루트 스케일 = 기준");
                    Transform baseV2D = basePrefab.GetComponentInChildren<MonsterVisual2D>(true).transform;
                    Transform variantV2D = variant.GetComponentInChildren<MonsterVisual2D>(true).transform;
                    Assert.AreEqual(baseV2D.localScale, variantV2D.localScale, $"{name} Visual2D 스케일 = 기준(Configure 가 곱함)");
                    Transform baseSigil = FindByName(basePrefab, "AuraSigil");
                    Transform variantSigil = FindByName(variant, "AuraSigil");
                    if (baseSigil != null)
                    {
                        Assert.IsNotNull(variantSigil, $"{name} AuraSigil");
                        Assert.AreEqual(baseSigil.localScale, variantSigil.localScale, $"{name} AuraSigil 스케일 = 기준");
                    }
                    Assert.AreEqual(basePrefab.GetComponentsInChildren<Collider>(true).Length, variant.GetComponentsInChildren<Collider>(true).Length, $"{name} 콜라이더 수");
                    CapsuleCollider baseCapsule = basePrefab.GetComponent<CapsuleCollider>();
                    CapsuleCollider variantCapsule = variant.GetComponent<CapsuleCollider>();
                    if (baseCapsule != null)
                    {
                        Assert.IsNotNull(variantCapsule, $"{name} CapsuleCollider");
                        Assert.AreEqual(baseCapsule.radius, variantCapsule.radius, 0.0001f, $"{name} 반경");
                        Assert.AreEqual(baseCapsule.height, variantCapsule.height, 0.0001f, $"{name} 높이");
                    }
                }
            }
        }

        [Test]
        public void 등급_Variant_의_HP바_높이_표는_기준_프리팹과_같다()
        {
            foreach (string baseName in BaseNames())
            {
                MonsterEnhancementVisual baseVisual = LoadPrefab(baseName).GetComponentInChildren<MonsterEnhancementVisual>(true);
                Assert.IsNotNull(baseVisual, $"{baseName} MonsterEnhancementVisual");
                float[] baseTable = ReadFloatArray(baseVisual, "_hpBarHeightByTier");
                Assert.AreEqual(4, baseTable.Length, $"{baseName} HP바 표 길이(티어 0~3)");

                for (int grade = 2; grade <= 5; grade++)
                {
                    string name = $"{baseName}G{grade}";
                    MonsterEnhancementVisual variantVisual = LoadPrefab(name).GetComponentInChildren<MonsterEnhancementVisual>(true);
                    Assert.IsNotNull(variantVisual, $"{name} MonsterEnhancementVisual");
                    Assert.AreEqual(baseTable, ReadFloatArray(variantVisual, "_hpBarHeightByTier"), $"{name} HP바 표 = 기준(등급 배율은 런타임 합성)");
                }
            }
        }

        private static float[] ReadFloatArray(Object target, string field)
        {
            SerializedProperty prop = new SerializedObject(target).FindProperty(field);
            float[] values = new float[prop.arraySize];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = prop.GetArrayElementAtIndex(i).floatValue;
            }
            return values;
        }

        [Test]
        public void 등급_Variant_의_몬스터_태그_키는_자기_등급_enum_이다()
        {
            foreach (EMonster baseMonster in MonsterGrades.BaseMonsters)
            {
                for (int grade = 2; grade <= 5; grade++)
                {
                    EMonster graded = MonsterGrades.Of(baseMonster, grade);
                    GameObject variant = LoadPrefab(graded.ToString());
                    MonsterTag tag = variant.GetComponentInChildren<MonsterTag>(true);
                    Assert.IsNotNull(tag, $"{graded} MonsterTag");
                    Assert.AreEqual(graded, tag.Key, $"{graded} 의 MonsterTag.Key");
                }
            }
        }
    }
}
