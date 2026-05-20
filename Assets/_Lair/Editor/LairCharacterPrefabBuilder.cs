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
        public const string PrefabDir = "Assets/_Lair/Prefabs/Characters";
        public const string MaterialDir = "Assets/_Lair/Prefabs/Characters";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        //# 캐릭터 빌드 스펙 — 기획서 §11.4 / 설계서 §3.4 표 기준
        public class Spec
        {
            public string Name;
            public PrimitiveType Mesh;
            public string ColorHex;
            public float Scale;
            public int Hp;
            public int Power;
            public float Range;
            public float Cooldown;
            public float MoveSpeed;
            public bool IsHero;
        }

        public static readonly Spec[] AllSpecs = new[]
        {
            new Spec { Name = nameof(EHero.Knight),    Mesh = PrimitiveType.Capsule, ColorHex = "#3B82F6", Scale = 1.0f, Hp = 1000, Power = 50, Range = 1.5f, Cooldown = 1.0f, MoveSpeed = 3.0f, IsHero = true  },
            new Spec { Name = nameof(EMonster.Slime),  Mesh = PrimitiveType.Sphere,  ColorHex = "#22C55E", Scale = 0.6f, Hp = 200,  Power = 10, Range = 1.0f, Cooldown = 1.0f, MoveSpeed = 1.5f, IsHero = false },
            new Spec { Name = nameof(EMonster.Golem),  Mesh = PrimitiveType.Cube,    ColorHex = "#6B7280", Scale = 1.2f, Hp = 500,  Power = 20, Range = 1.3f, Cooldown = 1.0f, MoveSpeed = 0.8f, IsHero = false },
            new Spec { Name = nameof(EMonster.Orc),    Mesh = PrimitiveType.Capsule, ColorHex = "#EF4444", Scale = 0.9f, Hp = 100,  Power = 20, Range = 1.0f, Cooldown = 0.5f, MoveSpeed = 2.5f, IsHero = false },
            //# B3 신규 몬스터 3종 — 기획서 §11.3 / §11.4
            new Spec { Name = nameof(EMonster.Archer), Mesh = PrimitiveType.Capsule, ColorHex = "#EAB308", Scale = 0.8f, Hp = 60,   Power = 30, Range = 5.0f, Cooldown = 1.0f, MoveSpeed = 2.0f, IsHero = false },
            new Spec { Name = nameof(EMonster.Spider), Mesh = PrimitiveType.Cube,    ColorHex = "#A855F7", Scale = 0.5f, Hp = 50,   Power = 5,  Range = 1.0f, Cooldown = 1.0f, MoveSpeed = 2.0f, IsHero = false },
            new Spec { Name = nameof(EMonster.Bat),    Mesh = PrimitiveType.Sphere,  ColorHex = "#1F2937", Scale = 0.3f, Hp = 30,   Power = 5,  Range = 1.0f, Cooldown = 0.8f, MoveSpeed = 3.5f, IsHero = false },
        };

        [MenuItem("Lair/Setup/M3 - Build Character Prefabs")]
        public static void BuildAllCharacterPrefabs()
        {
            EnsureDir(PrefabDir);

            //# Addressables 사전 확인 (M1 셋업이 안 됐을 경우 자동 호출)
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                LairSetup.EnsureAddressablesSetup();
                settings = AddressableAssetSettingsDefaultObject.Settings;
            }
            var group = settings.FindGroup(ResourceGroup);
            if (group == null)
            {
                LairSetup.EnsureAddressablesSetup();
                group = settings.FindGroup(ResourceGroup);
            }

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
            var mover = go.AddComponent<SimpleMover>();
            var health = go.AddComponent<Health>();
            var attacker = go.AddComponent<MeleeAttacker>();
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

            //# 5) [SerializeField] private 필드 주입 — SerializedObject 사용
            SetPrivateField(mover, "_speed", spec.MoveSpeed);
            SetPrivateField(health, "_max", spec.Hp);
            SetPrivateField(attacker, "_range", spec.Range);
            SetPrivateField(attacker, "_cooldown", spec.Cooldown);
            SetPrivateField(attacker, "_power", spec.Power);

            //# 5.5) 몬스터 발 밑 HP 바 (영웅 제외 — 영웅 HP 는 HUD 에 있음)
            if (!spec.IsHero)
                BuildMonsterHpBar(go, health, spec.Scale);

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

        //# 몬스터 머리 위 WorldSpace HP 바 (회색 배경 + 빨강 fill, 카메라 빌보드).
        //# 몬스터 자식이라 풀 Pop/Push 에 자동 동행. monsterScale 로 월드 크기 보정.
        private static void BuildMonsterHpBar(GameObject monster, Health health, float monsterScale)
        {
            const float BarPixelW = 120f;
            const float BarPixelH = 20f;
            const float TargetWorldW = 1.2f;   //# 모든 몬스터 동일한 월드 가로
            const float HeadLocalY  = 1.2f;    //# 머리 위 — 자식 localY (월드 = ×monsterScale)

            //# HpBar 루트 — WorldSpace Canvas. 회전은 MonsterHpBar 가 매 프레임 빌보드.
            var barGo = new GameObject("HpBar", typeof(RectTransform), typeof(Canvas));
            barGo.transform.SetParent(monster.transform, worldPositionStays: false);
            var canvas = barGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var barRt = (RectTransform)barGo.transform;
            barRt.sizeDelta = new Vector2(BarPixelW, BarPixelH);
            //# canvasScale = 목표월드폭 / 픽셀폭 / 몬스터scale → 몬스터 크기 무관 동일 월드 크기.
            float canvasScale = TargetWorldW / BarPixelW / monsterScale;
            barRt.localScale = Vector3.one * canvasScale;
            barRt.localRotation = Quaternion.identity;   //# 빌보드가 런타임에 회전
            //# 머리 위 — 자식이라 localY 가 monsterScale 로 곱해져 큰 몬스터일수록 더 위.
            barRt.localPosition = new Vector3(0f, HeadLocalY, 0f);

            //# 배경 (회색)
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(barGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            SetStretch(bgRt);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = LairUIPrefabBuilder.GetUISprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = HexColor("#374151");

            //# fill (빨강, 가로 Filled)
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            SetStretch(fillRt);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = LairUIPrefabBuilder.GetUISprite();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = HexColor("#DC2626");
            fillImg.fillAmount = 1f;

            //# MonsterHpBar 컴포넌트 — 몬스터 루트에 부착, 참조 주입.
            var bar = monster.AddComponent<MonsterHpBar>();
            SetPrivateField(bar, "_health", health);
            SetPrivateField(bar, "_fill", fillImg);
            SetPrivateField(bar, "_barRoot", barGo.transform);
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
