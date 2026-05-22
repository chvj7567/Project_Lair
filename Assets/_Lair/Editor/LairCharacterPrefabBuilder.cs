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
        public const string HpBarFillSpritePath = "Assets/_Lair/Art/Sprites/HpBar.png";
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

        //# HP 바 프리팹(HpBar.prefab) 생성 — 순수 바 비주얼만. Canvas·MonsterHpBar 없음.
        //# 몬스터 머리 위는 AttachMonsterHpBar 가 래퍼(Canvas+MonsterHpBar)를 만들어 nest.
        //# HUD 영웅 바는 BuildBattleHud 가 이 프리팹을 nest 해 _heroHpFill 에 주입.
        //# Rule 04 — HpBar.prefab 1개를 몬스터·HUD 가 공유.
        public static void EnsureHpBarPrefab()
        {
            //# SaveAsPrefabAsset 가 기존 경로를 덮어쓰며 GUID 를 보존한다 — 삭제하지 않는다.
            //# (삭제하면 이 프리팹을 nest 한 캐릭터 프리팹이 "Missing Nested Prefab" 에러를 낸다.)
            const float BarPixelW = 120f;
            const float BarPixelH = 20f;

            //# png 를 Sprite(Single) 로 보장 후 로드 — textureType 만 바꾸면 Sprite 에셋이
            //# 안 생겨 LoadAssetAtPath<Sprite> 가 null. spriteImportMode=Single 까지 필수.
            Sprite LoadBarSprite(string path)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null && (imp.textureType != TextureImporterType.Sprite
                                    || imp.spriteImportMode != SpriteImportMode.Single))
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.SaveAndReimport();
                }
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp == null)
                    Debug.LogWarning($"[CharacterPrefabBuilder] HP 바 스프라이트 로드 실패: {path}");
                return sp;
            }

            var bgSprite   = LoadBarSprite(HpBarBgSpritePath);
            var fillSprite = LoadBarSprite(HpBarFillSpritePath);

            //# 루트 — RectTransform 만. Canvas·MonsterHpBar 없음 (순수 비주얼).
            var root = new GameObject("HpBar", typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(BarPixelW, BarPixelH);

            //# Background — HpBarBackground.png (게이지 빈 부분 트랙). 색 틴트 없음 — 흰색 기본.
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(root.transform, false);
            SetStretch((RectTransform)bgGo.transform);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = bgSprite != null ? bgSprite : LairUIPrefabBuilder.GetUISprite();
            bgImg.type = Image.Type.Simple;

            //# Fill — HpBar.png, 가로 Filled (Background 의 자식)
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

            PrefabUtility.SaveAsPrefabAsset(root, HpBarPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[CharacterPrefabBuilder] HpBar.prefab 생성 (순수 비주얼 — Canvas/MonsterHpBar 없음)");
        }

        //# 몬스터 자식으로 래퍼(WorldSpace Canvas + MonsterHpBar)를 만들고,
        //# 그 아래 HpBar.prefab 인스턴스를 nest. monsterScale 로 월드 크기 보정.
        //# Rule 04 — HpBar.prefab 은 순수 비주얼. Canvas·MonsterHpBar 는 래퍼가 담당.
        private static void AttachMonsterHpBar(GameObject monster, float monsterScale)
        {
            const float BarPixelW = 120f;
            const float TargetWorldW = 1.2f;   //# 모든 몬스터 동일한 월드 가로
            const float HeadLocalY  = 1.2f;    //# 머리 위 — 래퍼 localY (월드 = ×monsterScale)

            var hpBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HpBarPrefabPath);
            if (hpBarPrefab == null)
            {
                Debug.LogWarning("[CharacterPrefabBuilder] HpBar.prefab 미발견 — HP 바 생략");
                return;
            }

            //# 래퍼 GameObject — WorldSpace Canvas + MonsterHpBar 담당.
            var wrapper = new GameObject("HpBarWrapper", typeof(RectTransform), typeof(Canvas));
            wrapper.transform.SetParent(monster.transform, false);
            var wrapperCanvas = wrapper.GetComponent<Canvas>();
            wrapperCanvas.renderMode = RenderMode.WorldSpace;
            var wrapperRt = (RectTransform)wrapper.transform;
            //# canvasScale = 목표월드폭 / 픽셀폭 / 몬스터scale → 몬스터 크기 무관 동일 월드 크기.
            wrapperRt.sizeDelta = new Vector2(BarPixelW, 20f);
            wrapperRt.localScale = Vector3.one * (TargetWorldW / BarPixelW / monsterScale);
            wrapperRt.localRotation = Quaternion.identity;
            wrapperRt.localPosition = new Vector3(0f, HeadLocalY, 0f);

            //# HpBar.prefab 인스턴스 — 래퍼 자식으로 full-stretch.
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(hpBarPrefab, wrapper.transform);
            var instRt = (RectTransform)inst.transform;
            instRt.localScale = Vector3.one;
            instRt.localRotation = Quaternion.identity;
            instRt.localPosition = Vector3.zero;
            instRt.anchorMin = Vector2.zero;
            instRt.anchorMax = Vector2.one;
            instRt.offsetMin = Vector2.zero;
            instRt.offsetMax = Vector2.zero;

            //# Fill Image — 결정론적 경로 Background/Fill 로 탐색.
            var fillTf = inst.transform.Find("Background/Fill");
            var fillImg = fillTf != null ? fillTf.GetComponent<Image>() : null;
            if (fillImg == null)
                Debug.LogWarning("[CharacterPrefabBuilder] HpBar.prefab 내 Background/Fill Image 미발견");

            //# MonsterHpBar — 래퍼에 부착, _fill 주입.
            var bar = wrapper.AddComponent<MonsterHpBar>();
            SetPrivateField(bar, "_fill", fillImg);
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
