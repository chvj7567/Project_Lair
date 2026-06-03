using System.IO;
using Lair.Character;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 영웅 스켈레톤 + 애니메이션 교체 — Task 5·6·7·8 의 에셋 이관 / Knight.controller / 프리팹 후처리.
    //# 메뉴 3개를 순서대로 실행: 1.Migrate → 2.Build Controller → 3.Apply To Knight Prefab.
    //# 코드 레이어(Controller/Driver/Sink/IAnimatorSink)는 선행 완료 — 본 빌더는 에셋만 다룬다.
    public static class LairHeroSkeletonBuilder
    {
        //# 이관 원본 경로
        private const string SrcMeshFbx   = "Assets/SazenGames/Skeleton/Art/Meshes/Skeleton_Model_110.fbx";
        private const string SrcAnimDir   = "Assets/SazenGames/Skeleton/Art/Animations";
        private const string SrcMaterial  = "Assets/SazenGames/Skeleton/Art/Materials/Skeleton_Mat.mat";
        private static readonly string[] SrcTextures =
        {
            "Assets/SazenGames/Skeleton/Art/Textures/Skeleton_BaseColor.psd",
            "Assets/SazenGames/Skeleton/Art/Textures/Skeleton_Emissive.png",
        };

        //# 이관 대상 경로 (Rule 04 §2 — Art 하위 타입별 정리)
        private const string DstSkeletonDir = "Assets/_Lair/Art/Characters/Skeleton";
        private const string DstAnimClipDir = "Assets/_Lair/Art/Characters/Skeleton/Animations";
        private const string DstMaterialDir = "Assets/_Lair/Art/Materials";
        private const string DstSpriteDir   = "Assets/_Lair/Art/Sprites";

        private const string DstMeshFbx     = DstSkeletonDir + "/Skeleton_Model_110.fbx";
        private const string ControllerPath = "Assets/_Lair/Art/Animations/Knight.controller";
        private const string KnightPrefabPath = "Assets/_Lair/Art/Characters/Knight.prefab";

        //# 사용 클립 9종 (기획서 §1.1 — 미사용 10종은 이관 제외, YAGNI).
        private static readonly string[] UsedClipFiles =
        {
            "Skeleton_idle",
            "Skeleton_walk_forward",
            "Skeleton_run_forward",
            "Skeleton_slash01",
            "Skeleton_slash02",
            "Skeleton_stab",
            "Skeleton_take_damage",
            "Skeleton_death",
            "Skeleton_spawn",
        };

        //# Knight 비주얼 자식 이름 — 기획서 §3.3 #2: Aura/HpBar prefix 금지(HitFlash 제외 회피).
        private const string VisualName = "Visual";

        //# 기존 캡슐 치수 기준 접지 보정 (CapsuleCollider center.y=0.9 / height=1.8).
        //# 스켈레톤 모델 원점이 발밑이면 localPosition.y=0 으로 접지됨. 보정값은 빌드 후 육안 검증.
        private const float VisualLocalY = 0f;

        // ============================================================
        // 메뉴 1 — 에셋 이관 (Task 5)
        // ============================================================

        [MenuItem("Lair/Setup/Skeleton - 1. Migrate Assets")]
        public static void MigrateAssets()
        {
            EnsureDir(DstSkeletonDir);
            EnsureDir(DstAnimClipDir);
            EnsureDir(DstMaterialDir);
            EnsureDir(DstSpriteDir);

            int moved = 0;

            //# 1) 메시 FBX
            moved += MoveAssetIdempotent(SrcMeshFbx, DstMeshFbx);

            //# 2) 사용 클립 FBX 9종
            foreach (string clipName in UsedClipFiles)
            {
                string src = $"{SrcAnimDir}/{clipName}.fbx";
                string dst = $"{DstAnimClipDir}/{clipName}.fbx";
                moved += MoveAssetIdempotent(src, dst);
            }

            //# 3) 머티리얼
            moved += MoveAssetIdempotent(SrcMaterial, $"{DstMaterialDir}/Skeleton_Mat.mat");

            //# 4) 텍스처 (psd/png → Sprites 폴더. Rule 04 §2 — 이미지 에셋은 Sprites/)
            foreach (string tex in SrcTextures)
            {
                string fileName = Path.GetFileName(tex);
                moved += MoveAssetIdempotent(tex, $"{DstSpriteDir}/{fileName}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HeroSkeletonBuilder] 에셋 이관 완료 — 이동 {moved}건 (이미 이관된 건 skip). " +
                      $"미사용 클립/Demo/Scripts/Documentation 은 이관 제외(YAGNI).");
        }

        //# MoveAsset 멱등 래퍼 — 대상이 이미 있으면 skip, 원본 없으면 경고. 반환=이동 수행 시 1.
        private static int MoveAssetIdempotent(string src, string dst)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null)
            {
                Debug.Log($"[HeroSkeletonBuilder] 이미 이관됨 — skip: {dst}");
                return 0;
            }
            if (AssetDatabase.LoadAssetAtPath<Object>(src) == null)
            {
                Debug.LogWarning($"[HeroSkeletonBuilder] 원본 미발견 — skip: {src}");
                return 0;
            }
            string error = AssetDatabase.MoveAsset(src, dst);
            if (string.IsNullOrEmpty(error) == false)
            {
                Debug.LogError($"[HeroSkeletonBuilder] MoveAsset 실패 ({src} → {dst}): {error}");
                return 0;
            }
            return 1;
        }

        // ============================================================
        // 메뉴 2 — Knight.controller 생성 (Task 6·7)
        // ============================================================

        [MenuItem("Lair/Setup/Skeleton - 2. Build Controller")]
        public static void BuildController()
        {
            EnsureDir(Path.GetDirectoryName(ControllerPath));

            //# 이관 선행 확인 — 메시 FBX 가 이관 위치에 있어야 클립도 로드 가능.
            if (AssetDatabase.LoadAssetAtPath<Object>(DstMeshFbx) == null)
            {
                Debug.LogError("[HeroSkeletonBuilder] 메시 FBX 미이관 — 메뉴 1(Migrate Assets) 을 먼저 실행하세요.");
                return;
            }

            //# 컨트롤러 신규 생성 (기존 있으면 덮어쓰기 — 멱등).
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            //# 파라미터 (기획서 §5.4)
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Spawn", AnimatorControllerParameterType.Trigger);

            //# 클립 로드 (take 이름 무관 — 파일 경로의 AnimationClip sub-asset 추출)
            AnimationClip idleClip   = LoadClip("Skeleton_idle");
            AnimationClip walkClip   = LoadClip("Skeleton_walk_forward");
            AnimationClip runClip    = LoadClip("Skeleton_run_forward");
            AnimationClip slash01Clip = LoadClip("Skeleton_slash01");
            AnimationClip slash02Clip = LoadClip("Skeleton_slash02");
            AnimationClip stabClip   = LoadClip("Skeleton_stab");
            AnimationClip hitClip    = LoadClip("Skeleton_take_damage");
            AnimationClip deathClip  = LoadClip("Skeleton_death");
            AnimationClip spawnClip  = LoadClip("Skeleton_spawn");

            AnimatorStateMachine root = controller.layers[0].stateMachine;

            //# === 상태 배치 ===
            //# Spawn — Entry 기본 상태. 종료 후 Exit Time 으로 Idle.
            AnimatorState spawnState = root.AddState("Spawn");
            spawnState.motion = spawnClip;
            root.defaultState = spawnState;

            AnimatorState idleState = root.AddState("Idle");
            idleState.motion = idleClip;

            //# Move — 1D BlendTree on Speed: walk@1.0 / run@2.0.
            BlendTree moveTree;
            AnimatorState moveState = controller.CreateBlendTreeInController("Move", out moveTree);
            moveTree.blendType = BlendTreeType.Simple1D;
            moveTree.blendParameter = "Speed";
            moveTree.useAutomaticThresholds = false;
            moveTree.AddChild(walkClip, 1.0f);
            moveTree.AddChild(runClip, 2.0f);

            //# Attack — AttackVariant Int 로 분기되는 3개 root 직속 상태 (0=slash01 / 1=slash02 / 2=stab).
            //# AnyState→각 상태가 Attack trigger + AttackVariant 값 조건으로 선택. 서브머신 없이 단순화.
            AnimatorState slash01State = root.AddState("Slash01");
            slash01State.motion = slash01Clip;
            AnimatorState slash02State = root.AddState("Slash02");
            slash02State.motion = slash02Clip;
            AnimatorState stabState = root.AddState("Stab");
            stabState.motion = stabClip;

            AnimatorState hitState = root.AddState("Hit");
            hitState.motion = hitClip;

            AnimatorState deathState = root.AddState("Death");
            deathState.motion = deathClip;

            //# === 전이 ===

            //# Spawn → Idle : Exit Time (클립 종료 후 자동).
            AddExitTransition(spawnState, idleState);

            //# Idle ↔ Move : Speed 0.1 임계, Has Exit Time OFF (즉시 반응).
            AnimatorStateTransition idleToMove = idleState.AddTransition(moveState);
            idleToMove.hasExitTime = false;
            idleToMove.duration = 0.1f;
            idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            AnimatorStateTransition moveToIdle = moveState.AddTransition(idleState);
            moveToIdle.hasExitTime = false;
            moveToIdle.duration = 0.1f;
            moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            //# AnyState → 각 공격 상태 : Attack trigger + AttackVariant 값으로 클립 선택.
            AnimatorStateTransition anyToSlash01 = root.AddAnyStateTransition(slash01State);
            anyToSlash01.hasExitTime = false;
            anyToSlash01.duration = 0.05f;
            anyToSlash01.canTransitionToSelf = false;
            anyToSlash01.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToSlash01.AddCondition(AnimatorConditionMode.Equals, 0f, "AttackVariant");

            AnimatorStateTransition anyToSlash02 = root.AddAnyStateTransition(slash02State);
            anyToSlash02.hasExitTime = false;
            anyToSlash02.duration = 0.05f;
            anyToSlash02.canTransitionToSelf = false;
            anyToSlash02.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToSlash02.AddCondition(AnimatorConditionMode.Equals, 1f, "AttackVariant");

            AnimatorStateTransition anyToStab = root.AddAnyStateTransition(stabState);
            anyToStab.hasExitTime = false;
            anyToStab.duration = 0.05f;
            anyToStab.canTransitionToSelf = false;
            anyToStab.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToStab.AddCondition(AnimatorConditionMode.Equals, 2f, "AttackVariant");

            //# 공격 변형 상태 → Idle : Exit Time (스윙 종료 후 복귀).
            AddExitTransition(slash01State, idleState);
            AddExitTransition(slash02State, idleState);
            AddExitTransition(stabState, idleState);

            //# AnyState → Hit : Hit trigger (스팸 가드는 Controller 로직이 담당).
            AnimatorStateTransition anyToHit = root.AddAnyStateTransition(hitState);
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0.05f;
            anyToHit.canTransitionToSelf = false;
            anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

            //# Hit → Idle : Exit Time (리액션 종료 후 복귀).
            AddExitTransition(hitState, idleState);

            //# AnyState → Death : Dead == true (최우선 인터럽트). self 전이 금지.
            AnimatorStateTransition anyToDeath = root.AddAnyStateTransition(deathState);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.05f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //# 누락 클립 검증 로그
            int nullClips = CountNulls(idleClip, walkClip, runClip, slash01Clip, slash02Clip,
                                       stabClip, hitClip, deathClip, spawnClip);
            if (nullClips > 0)
            {
                Debug.LogError($"[HeroSkeletonBuilder] 클립 {nullClips}개 로드 실패 — 메뉴 1 이관 또는 take 매칭 확인 필요.");
            }

            Debug.Log($"[HeroSkeletonBuilder] Knight.controller 생성 완료 " +
                      $"(상태: Spawn/Idle/Move(BlendTree)/Attack(Slash01·Slash02·Stab)/Hit/Death, 클립 로드 실패 {nullClips}건)");
        }

        //# FBX 내부 AnimationClip sub-asset 추출 — take 이름이 파일명과 달라(예: 'root|combat idle ')
        //# 경로의 AnimationClip 타입을 찾아 반환. __preview__ 클립은 제외.
        private static AnimationClip LoadClip(string fileName)
        {
            string path = $"{DstAnimClipDir}/{fileName}.fbx";
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in all)
            {
                AnimationClip clip = obj as AnimationClip;
                if (clip == null)
                    continue;
                if (clip.name.StartsWith("__preview__"))
                    continue;
                return clip;
            }
            Debug.LogWarning($"[HeroSkeletonBuilder] AnimationClip 미발견: {path}");
            return null;
        }

        //# 단발 상태 → 대상 상태 Exit Time 전이 (클립 종료 후 자동 복귀).
        private static void AddExitTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 0.9f;
            t.duration = 0.1f;
        }

        // ============================================================
        // 메뉴 3 — Knight.prefab 후처리 (Task 8)
        // ============================================================

        [MenuItem("Lair/Setup/Skeleton - 3. Apply To Knight Prefab")]
        public static void ApplyToKnightPrefab()
        {
            GameObject meshFbx = AssetDatabase.LoadAssetAtPath<GameObject>(DstMeshFbx);
            if (meshFbx == null)
            {
                Debug.LogError("[HeroSkeletonBuilder] 메시 FBX 미발견 — 메뉴 1(Migrate Assets) 을 먼저 실행하세요.");
                return;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[HeroSkeletonBuilder] Knight.controller 미발견 — 메뉴 2(Build Controller) 를 먼저 실행하세요.");
                return;
            }

            //# 메시 FBX 의 Humanoid Avatar 추출 (Animator.avatar 할당용).
            Avatar avatar = LoadAvatar(DstMeshFbx);
            if (avatar == null)
            {
                Debug.LogWarning("[HeroSkeletonBuilder] 메시 FBX 에서 Avatar 미발견 — Rig=Humanoid 인지 확인하세요.");
            }

            //# Knight.prefab 을 인스턴스화해 후처리 (PrefabUtility.LoadPrefabContents 로 격리 편집).
            GameObject root = PrefabUtility.LoadPrefabContents(KnightPrefabPath);

            //# 1) 캡슐 MeshFilter + MeshRenderer 제거 (게임 컴포넌트·콜라이더·리지드바디 유지).
            MeshFilter mf = root.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Object.DestroyImmediate(mf, true);
            }
            MeshRenderer mr = root.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Object.DestroyImmediate(mr, true);
            }

            //# 2) 기존 Visual 자식이 있으면 제거 (멱등 재실행 대비).
            Transform existing = root.transform.Find(VisualName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            //# 3) 스켈레톤 비주얼을 자식으로 부착 — 메시 FBX instantiate(SkinnedMeshRenderer 포함).
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(meshFbx, root.transform);
            visual.name = VisualName;
            visual.transform.localPosition = new Vector3(0f, VisualLocalY, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            //# 4) Animator 설정 — controller + avatar (Visual 의 Animator. FBX instantiate 시 자동 부착).
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            if (avatar != null)
            {
                animator.avatar = avatar;
            }
            animator.applyRootMotion = false;

            //# 5) 루트에 CharacterAnimationDriver 부착 + _animator 참조 연결 (멱등).
            CharacterAnimationDriver driver = root.GetComponent<CharacterAnimationDriver>();
            if (driver == null)
            {
                driver = root.AddComponent<CharacterAnimationDriver>();
            }
            SetSerializedReference(driver, "_animator", animator);
            //# 표현 수치 확인/세팅 (기획서 §5.5 — 기본값과 동일하나 명시).
            SetSerializedFloat(driver, "_walkSpeed", 1.0f);
            SetSerializedFloat(driver, "_runSpeed", 2.0f);
            SetSerializedFloat(driver, "_hitReactionCooldown", 0.4f);
            SetSerializedFloat(driver, "_attackSuppressWindow", 0.5f);

            //# 6) 머티리얼 _BaseColor 검증 로그 (기획서 §3.3 #1 — HitFlash 플래시 의존).
            VerifyBaseColor(visual);

            //# 7) 저장 (GUID 보존 — LoadPrefabContents 짝).
            PrefabUtility.SaveAsPrefabAsset(root, KnightPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HeroSkeletonBuilder] Knight.prefab 후처리 완료 — 캡슐 제거 + Visual(스켈레톤) 자식 부착 + " +
                      $"Animator(controller={controller.name}, avatar={(avatar != null ? avatar.name : "없음")}) + Driver 연결. " +
                      $"둔화 Sphere·공포 Cube·HP바는 루트 기준 유지(기획서 §3.3 #3).");
        }

        //# FBX 경로의 Humanoid Avatar sub-asset 추출.
        private static Avatar LoadAvatar(string fbxPath)
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object obj in all)
            {
                Avatar avatar = obj as Avatar;
                if (avatar != null)
                    return avatar;
            }
            return null;
        }

        //# Visual 하위 렌더러 머티리얼이 _BaseColor 를 갖는 URP Lit 인지 검증 로그.
        private static void VerifyBaseColor(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[HeroSkeletonBuilder] Visual 하위 렌더러 0개 — 비주얼 메시 확인 필요.");
                return;
            }
            foreach (Renderer r in renderers)
            {
                Material mat = r.sharedMaterial;
                if (mat == null)
                {
                    Debug.LogWarning($"[HeroSkeletonBuilder] {r.name} 머티리얼 없음.");
                    continue;
                }
                bool hasBaseColor = mat.HasProperty("_BaseColor");
                if (hasBaseColor)
                {
                    Debug.Log($"[HeroSkeletonBuilder] _BaseColor 검증 OK — {mat.name} (shader={mat.shader.name}). HitFlash 플래시 동작 가능.");
                }
                else
                {
                    Debug.LogWarning($"[HeroSkeletonBuilder] _BaseColor 미보유 — {mat.name} (shader={mat.shader.name}). " +
                                     $"HitFlash 피격 플래시가 안 보일 수 있음(기획서 §3.3 #1).");
                }
            }
        }

        // ============================================================
        // 공통 헬퍼
        // ============================================================

        private static void SetSerializedReference(Component target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[HeroSkeletonBuilder] 필드 미발견: {target.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedFloat(Component target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[HeroSkeletonBuilder] 필드 미발견: {target.GetType().Name}.{field}");
                return;
            }
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int CountNulls(params Object[] objs)
        {
            int count = 0;
            foreach (Object o in objs)
            {
                if (o == null)
                {
                    count++;
                }
            }
            return count;
        }

        private static void EnsureDir(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
