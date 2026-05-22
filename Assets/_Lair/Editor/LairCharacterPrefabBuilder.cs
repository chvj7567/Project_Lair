using System.IO;
using Lair.Character;
using Lair.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# M3 — 캐릭터 프리팹 4종(Knight/Slime/Golem/Orc) 자동 생성 + Addressables 등록.
    //# 프리미티브 메시 + URP Lit 머티리얼 + 컴포넌트 조립 + 인스펙터 필드(SerializedObject) 설정.
    //# Rule 04 (프리팹화), Rule 08 (파일명 = Enum 값명) 자동 충족.
    public static class LairCharacterPrefabBuilder
    {
        public const string PrefabDir = "Assets/_Lair/Art/Characters";
        public const string MaterialDir = "Assets/_Lair/Art/Materials";
        public const string HpBarPrefabPath  = "Assets/_Lair/Art/UI/HpBar.prefab";
        public const string HpBarBgSpritePath = "Assets/_Lair/Art/Sprites/HpBarBackground.png";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        //# 캐릭터 빌드 스펙 — 메시/색/스케일만. 스탯은 BalanceConfig 가 단일 진실 (Slice C).
        public class Spec
        {
            public string Name;
            public PrimitiveType Mesh;
            public string ColorHex;
            public float Scale;
            public bool IsHero;
        }

        public static readonly Spec[] AllSpecs = new[]
        {
            new Spec { Name = nameof(EHero.Knight),    Mesh = PrimitiveType.Capsule, ColorHex = "#3B82F6", Scale = 1.0f, IsHero = true  },
            new Spec { Name = nameof(EMonster.Slime),  Mesh = PrimitiveType.Sphere,  ColorHex = "#22C55E", Scale = 0.6f, IsHero = false },
            new Spec { Name = nameof(EMonster.Golem),  Mesh = PrimitiveType.Cube,    ColorHex = "#6B7280", Scale = 1.2f, IsHero = false },
            new Spec { Name = nameof(EMonster.Orc),    Mesh = PrimitiveType.Capsule, ColorHex = "#EF4444", Scale = 0.9f, IsHero = false },
            new Spec { Name = nameof(EMonster.Archer), Mesh = PrimitiveType.Capsule, ColorHex = "#EAB308", Scale = 0.8f, IsHero = false },
            new Spec { Name = nameof(EMonster.Spider), Mesh = PrimitiveType.Cube,    ColorHex = "#A855F7", Scale = 0.5f, IsHero = false },
            new Spec { Name = nameof(EMonster.Bat),    Mesh = PrimitiveType.Sphere,  ColorHex = "#1F2937", Scale = 0.3f, IsHero = false },
        };

        [MenuItem("Lair/Setup/M3 - Build Character Prefabs")]
        public static void BuildAllCharacterPrefabs()
        {
            EnsureDir(PrefabDir);

            //# Addressables 사전 확인
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairCharacterPrefabBuilder] Addressables 미설정 — Window > Asset Management > Addressables Groups 로 초기화 필요");
                return;
            }
            var group = settings.FindGroup(ResourceGroup);
            if (group == null)
            {
                Debug.LogError("[LairCharacterPrefabBuilder] Addressables 'Resource' 그룹 미발견");
                return;
            }

            //# HP 바 프리팹 1회 생성 (Rule 04 — 6 몬스터 공용). 각 몬스터가 nested 로 참조.
            EnsureHpBarPrefab();

            foreach (var spec in AllSpecs)
            {
                BuildOne(spec, settings, group);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CharacterPrefabBuilder] {AllSpecs.Length} 개 프리팹 빌드 완료");
        }

        private static void BuildOne(Spec spec, AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            //# 1) 프리미티브 GameObject 생성
            var go = GameObject.CreatePrimitive(spec.Mesh);
            go.name = spec.Name;
            go.transform.position = Vector3.zero;
            go.transform.localScale = Vector3.one * spec.Scale;
            //# 거미 — 납작하게 (기획서 §11.4 Y 스케일 0.3 배)
            if (spec.Name == nameof(EMonster.Spider))
                go.transform.localScale = new Vector3(spec.Scale, spec.Scale * 0.3f, spec.Scale);

            //# 2) Collider 제거 (Slice A 는 충돌 사용 안 함)
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# 3) 머티리얼 생성 + 색상 적용
            var matPath = $"{MaterialDir}/Mat_{spec.Name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(UrpLitShaderName));
                if (ColorUtility.TryParseHtmlString(spec.ColorHex, out var color))
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                    mat.color = color;
                }
                AssetDatabase.CreateAsset(mat, matPath);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;

            //# 4) 컴포넌트 부착 — 추가 순서가 Awake 호출 순서이므로 Health 를 의존 컴포넌트보다 먼저
            go.AddComponent<SimpleMover>();
            go.AddComponent<Health>();
            go.AddComponent<MeleeAttacker>();
            if (spec.IsHero)
            {
                go.AddComponent<HeroTargetProvider>();
            }
            else
            {
                go.AddComponent<MonsterTargetProvider>();
                //# B1 — MonsterTag 부착 + EMonster Key 주입
                var tag = go.AddComponent<MonsterTag>();
                if (System.Enum.TryParse<EMonster>(spec.Name, out var key))
                    tag.Configure(key);
                //# B3 — 거미 특수능력: 공격 시 영웅 둔화
                if (spec.Name == nameof(EMonster.Spider))
                    go.AddComponent<SpiderSlowOnHit>();
            }
            go.AddComponent<AutoCombatAI>();
            //# 시각 피드백 + 사망 처리
            go.AddComponent<HitFlash>();
            go.AddComponent<DespawnOnDeath>();

            //# 5.5) 몬스터 머리 위 HP 바 — HpBar.prefab nested 부착 (영웅 제외 — HUD 에 있음)
            if (!spec.IsHero)
                AttachMonsterHpBar(go, spec.Scale);

            //# 6) 프리팹 저장 (덮어쓰기)
            var prefabPath = $"{PrefabDir}/{spec.Name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            //# 7) Addressables 등록 — 주소 = 파일명 (Rule 08)
            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = spec.Name;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[CharacterPrefabBuilder] {spec.Name} 빌드 완료 (address={entry.address}, label={ResourceLabel})");
        }

        //# HP 바 프리팹(HpBar.prefab) 1회 생성 — WorldSpace Canvas + MonsterHpBar + 배경/fill.
        //# 배경은 HpBarBackground.png, fill 은 빨강 단색 Filled.
        private static void EnsureHpBarPrefab()
        {
            //# 이미 있으면 보존 — 직접 편집한 HpBar.prefab 을 덮어쓰지 않음.
            //# 처음 1회만 빌더가 기본형 생성, 이후 스프라이트/디자인은 수동 관리.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HpBarPrefabPath) != null)
                return;

            const float BarPixelW = 120f;
            const float BarPixelH = 20f;

            //# png 를 Sprite(Single) 로 — textureType 만 바꾸면 Sprite 에셋이 안 생겨
            //# LoadAssetAtPath<Sprite> 가 null. spriteImportMode=Single 까지 필수.
            var imp = AssetImporter.GetAtPath(HpBarBgSpritePath) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite
                                || imp.spriteImportMode != SpriteImportMode.Single))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.SaveAndReimport();
            }
            var fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HpBarBgSpritePath);
            if (fillSprite == null)
                Debug.LogWarning($"[CharacterPrefabBuilder] HP 바 fill 스프라이트 로드 실패: {HpBarBgSpritePath}");

            //# 루트 — WorldSpace Canvas. 회전은 MonsterHpBar 가 매 프레임 빌보드.
            var root = new GameObject("HpBar", typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(BarPixelW, BarPixelH);

            //# 배경 — 회색 단색 트랙 (게이지 빈 부분)
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(root.transform, false);
            SetStretch((RectTransform)bgGo.transform);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = LairUIPrefabBuilder.GetUISprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = HexColor("#374151");

            //# fill — HpBarBackground.png, 가로 Filled (이미지 원본 색 → color 흰색)
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(bgGo.transform, false);
            SetStretch((RectTransform)fillGo.transform);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = fillSprite != null ? fillSprite : LairUIPrefabBuilder.GetUISprite();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = Color.white;
            fillImg.fillAmount = 1f;

            //# MonsterHpBar — 루트에 부착, _fill 주입 (프리팹 내부 참조).
            var bar = root.AddComponent<MonsterHpBar>();
            SetPrivateField(bar, "_fill", fillImg);

            PrefabUtility.SaveAsPrefabAsset(root, HpBarPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[CharacterPrefabBuilder] HpBar.prefab 생성");
        }

        //# 몬스터 자식으로 HpBar.prefab nested 인스턴스 부착. monsterScale 로 월드 크기 보정.
        private static void AttachMonsterHpBar(GameObject monster, float monsterScale)
        {
            const float BarPixelW = 120f;
            const float TargetWorldW = 1.2f;   //# 모든 몬스터 동일한 월드 가로
            const float HeadLocalY  = 1.2f;    //# 머리 위 — 자식 localY (월드 = ×monsterScale)

            var hpBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HpBarPrefabPath);
            if (hpBarPrefab == null)
            {
                Debug.LogWarning("[CharacterPrefabBuilder] HpBar.prefab 미발견 — HP 바 생략");
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(hpBarPrefab, monster.transform);
            var rt = (RectTransform)inst.transform;
            //# canvasScale = 목표월드폭 / 픽셀폭 / 몬스터scale → 몬스터 크기 무관 동일 월드 크기.
            rt.localScale = Vector3.one * (TargetWorldW / BarPixelW / monsterScale);
            rt.localRotation = Quaternion.identity;
            rt.localPosition = new Vector3(0f, HeadLocalY, 0f);
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        private static void SetPrivateField(Component target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[CharacterPrefabBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            switch (value)
            {
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case bool b: prop.boolValue = b; break;
                case string s: prop.stringValue = s; break;
                default: prop.objectReferenceValue = value as Object; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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
