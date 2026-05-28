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
    //# M3 — 캐릭터 프리팹 자동 생성 + Addressables 등록.
    //# 영웅(Knight): 프리미티브 Capsule 그대로 유지.
    //# 몬스터: 빈 루트 GameObject + Visual(LittleGhost nested prefab) + Aura(Cylinder placeholder)
    //# + HpBarWrapper(머리 위 HP 바) 구조. 컴포넌트는 모두 루트에 부착.
    //# Rule 04 (프리팹화), Rule 08 (파일명 = Enum 값명), Rule 14 (Art/ 하위 분류) 자동 충족.
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

        //# 오오라(발 밑 디스크 placeholder) 디자인 상수.
        //# 자식이므로 루트 spec.Scale 에 따라 함께 스케일링됨 (작은 몬스터의 오오라는 자연히 작아짐).
        private const float AuraDiscScale = 1.4f;   //# X/Z 디스크 반지름 비율 (루트 1.0 기준)
        private const float AuraDiscHeight = 0.01f; //# 납작한 디스크
        private const float AuraLocalY = 0.005f;    //# 바닥 살짝 위로 띄움 (z-fighting 방지)

        //# 캐릭터 빌드 스펙 — 메시/색/스케일/Ghost 프리팹 경로.
        //# 영웅은 Mesh + ColorHex 로 프리미티브 경로. 몬스터는 GhostPrefabPath + ColorHex(=오오라 색).
        public class Spec
        {
            public string Name;
            public PrimitiveType Mesh;        //# 영웅 전용. 몬스터는 GhostPrefabPath 사용.
            public string ColorHex;           //# 영웅: body color / 몬스터: aura color
            public float Scale;
            public bool IsHero;
            public string GhostPrefabPath;    //# 몬스터 visual nested prefab 경로. null/빈=프리미티브 경로
        }

        public static readonly Spec[] AllSpecs = new[]
        {
            new Spec { Name = nameof(EHero.Knight),     Mesh = PrimitiveType.Capsule, ColorHex = "#3B82F6", Scale = 1.0f, IsHero = true,  GhostPrefabPath = null },
            new Spec { Name = nameof(EMonster.Wisp),    Mesh = PrimitiveType.Sphere,  ColorHex = "#22C55E", Scale = 0.6f, IsHero = false, GhostPrefabPath = "Assets/Little_GhostLP(FREE)/Prefabs/LittleGhost_N1.prefab" },
            new Spec { Name = nameof(EMonster.Wraith),  Mesh = PrimitiveType.Cube,    ColorHex = "#6B7280", Scale = 1.3f, IsHero = false, GhostPrefabPath = "Assets/Little_GhostLP(FREE)/Prefabs/LittleGhost_V1.prefab" },
            new Spec { Name = nameof(EMonster.Reaper),  Mesh = PrimitiveType.Capsule, ColorHex = "#EF4444", Scale = 0.9f, IsHero = false, GhostPrefabPath = "Assets/Little_GhostLP(FREE)/Prefabs/LittleGhost_H1.prefab" },
            new Spec { Name = nameof(EMonster.Hex),     Mesh = PrimitiveType.Capsule, ColorHex = "#EAB308", Scale = 0.8f, IsHero = false, GhostPrefabPath = "Assets/Little_GhostLP(FREE)/Prefabs/LittleGhost_M1.prefab" },
            new Spec { Name = nameof(EMonster.Plague),  Mesh = PrimitiveType.Cube,    ColorHex = "#A855F7", Scale = 0.5f, IsHero = false, GhostPrefabPath = "Assets/Little_GhostLP(FREE)/Prefabs/LittleGhost_V2.prefab" },
            new Spec { Name = nameof(EMonster.Phantom), Mesh = PrimitiveType.Sphere,  ColorHex = "#1F2937", Scale = 0.4f, IsHero = false, GhostPrefabPath = "Assets/Little_GhostLP(FREE)/Prefabs/LittleGhost_N2.prefab" },
        };

        [MenuItem("Lair/Setup/M3 - Build Character Prefabs")]
        public static void BuildAllCharacterPrefabs()
        {
            EnsureDir(PrefabDir);
            EnsureDir(MaterialDir);

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
            GameObject go;

            if (spec.IsHero || string.IsNullOrEmpty(spec.GhostPrefabPath))
            {
                //# === 영웅 경로 — 기존 프리미티브 ===
                go = GameObject.CreatePrimitive(spec.Mesh);
                go.name = spec.Name;
                go.transform.position = Vector3.zero;
                go.transform.localScale = Vector3.one * spec.Scale;

                //# Collider 제거 (Slice A 는 충돌 사용 안 함)
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);

                //# 머티리얼 생성 + 색상 적용 — 영웅 본체 색
                var mat = EnsureUrpLitMaterial($"{MaterialDir}/Mat_{spec.Name}.mat", spec.ColorHex);
                go.GetComponent<Renderer>().sharedMaterial = mat;
            }
            else
            {
                //# === 몬스터 경로 — 빈 루트 + LittleGhost nested + Aura placeholder ===
                go = new GameObject(spec.Name);
                go.transform.position = Vector3.zero;
                go.transform.localScale = Vector3.one * spec.Scale;

                //# 1) Visual — LittleGhost prefab 을 nested instance 로 부착
                var ghostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.GhostPrefabPath);
                if (ghostPrefab == null)
                {
                    Debug.LogError($"[CharacterPrefabBuilder] LittleGhost 프리팹 로드 실패: {spec.GhostPrefabPath}");
                }
                else
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(ghostPrefab, go.transform);
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;   //# 루트 spec.Scale 에 영향받음

                    //# LittleGhost 외부 에셋의 Rigidbody/Collider 제거 — 충돌 물리 영향 방지
                    foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true))
                        Object.DestroyImmediate(rb);
                    foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
                        Object.DestroyImmediate(col);
                }

                //# 2) Aura — Cylinder primitive 디스크 (정체성 색 placeholder)
                AttachAuraDisc(go, spec);

                //# 루트에 물리 컴포넌트 추가 — 몬스터끼리 수평 밀치기 허용
                CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 0.9f, 0f);
                capsule.radius = 0.5f;
                capsule.height = 1.8f;
                Rigidbody rigidbody = go.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = false;
                rigidbody.constraints = RigidbodyConstraints.FreezePositionY
                    | RigidbodyConstraints.FreezeRotationX
                    | RigidbodyConstraints.FreezeRotationZ;
            }

            //# 3) 컴포넌트 부착 — 추가 순서가 Awake 호출 순서이므로 Health 를 의존 컴포넌트보다 먼저
            go.AddComponent<SimpleMover>();
            go.AddComponent<Health>();
            go.AddComponent<MeleeAttacker>();
            //# 캐릭터 회전 — IRotator 구현체. AutoCombatAI 가 RequireComponent 로 의존.
            go.AddComponent<SimpleRotator>();
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
                //# B3 — 플레이그 특수능력: 공격 시 영웅 둔화
                if (spec.Name == nameof(EMonster.Plague))
                    go.AddComponent<PlagueSlowOnHit>();
            }
            go.AddComponent<AutoCombatAI>();
            //# 시각 피드백 + 사망 처리
            go.AddComponent<HitFlash>();
            go.AddComponent<DespawnOnDeath>();

            //# 4) 몬스터 머리 위 HP 바 — HpBar.prefab nested 부착 (영웅 제외 — HUD 에 있음)
            if (!spec.IsHero)
                AttachMonsterHpBar(go, spec.Scale);

            //# 5) 프리팹 저장 (덮어쓰기)
            var prefabPath = $"{PrefabDir}/{spec.Name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            //# 6) Addressables 등록 — 주소 = 파일명 (Rule 08)
            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = spec.Name;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[CharacterPrefabBuilder] {spec.Name} 빌드 완료 (address={entry.address}, label={ResourceLabel})");
        }

        //# 몬스터 발 밑 오오라 placeholder — 납작한 Cylinder primitive.
        //# 자식이므로 루트 spec.Scale 에 함께 스케일링됨. 머티리얼은 Mat_{Name}_Aura.mat 재사용.
        //# 이름이 "Aura" 로 시작 → HitFlash 가 플래시 대상에서 자동 제외.
        private static void AttachAuraDisc(GameObject root, Spec spec)
        {
            var aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aura.name = "Aura";
            aura.transform.SetParent(root.transform, false);
            aura.transform.localPosition = new Vector3(0f, AuraLocalY, 0f);
            aura.transform.localRotation = Quaternion.identity;
            aura.transform.localScale = new Vector3(AuraDiscScale, AuraDiscHeight, AuraDiscScale);

            //# Collider 제거 (placeholder 시각만)
            var col = aura.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            //# 정체성 색 머티리얼 (재사용 캐시)
            var auraMat = EnsureUrpLitMaterial($"{MaterialDir}/Mat_{spec.Name}_Aura.mat", spec.ColorHex);
            aura.GetComponent<Renderer>().sharedMaterial = auraMat;
        }

        //# URP/Lit 머티리얼을 지정 경로에 보장 — 기존 에셋이 있으면 색만 갱신, 없으면 생성.
        //# GUID 보존을 위해 가능한 한 in-place 갱신.
        private static Material EnsureUrpLitMaterial(string matPath, string colorHex)
        {
            if (!ColorUtility.TryParseHtmlString(colorHex, out var color))
                color = Color.magenta;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(UrpLitShaderName));
                ApplyColor(mat, color);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                //# 기존 에셋 — 색만 갱신 (GUID 보존)
                ApplyColor(mat, color);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void ApplyColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
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
            //# 이름이 "HpBarWrapper" 로 시작 → HitFlash 가 색 깜빡임에서 자동 제외 (ExcludeNamePrefixes "HpBar").
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
