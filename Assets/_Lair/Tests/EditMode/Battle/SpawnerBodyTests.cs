using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# SpawnerBody 컴포넌트 본격 스위트.
    //# Renderer + Material[] 의존성, ISpawnerOutputProvider 구독/해제, 방어 케이스 망라.
    public class SpawnerBodyTests
    {
        private readonly List<GameObject> _spawned = new();
        private readonly List<Object> _assets = new();   //# Material 등 Unity Object 누수 방지.

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();

            foreach (var a in _assets)
                if (a != null) Object.DestroyImmediate(a);
            _assets.Clear();
        }

        //# 리플렉션 헬퍼 — 직렬화 필드 강제 주입.
        private static void SetPrivate(object target, string field, object value)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        //# Spawner.OnEnable 리플렉션 호출 — _currentType 초기화 + OnOutputTypeChanged 발행.
        private static void InvokeOnEnable(Component c)
        {
            var mi = c.GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "OnEnable 메서드 존재 확인");
            mi.Invoke(c, null);
        }

        //# SpawnerBody.OnEnable 리플렉션 호출.
        private static void InvokeBodyOnEnable(SpawnerBody body)
        {
            var mi = typeof(SpawnerBody).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SpawnerBody.OnEnable 메서드 존재 확인");
            mi.Invoke(body, null);
        }

        //# SpawnerBody.HandleTypeChanged 리플렉션 직접 호출 — ReplaceOutput 우회 케이스용.
        private static void InvokeHandleTypeChanged(SpawnerBody body, EMonster type)
        {
            var mi = typeof(SpawnerBody).GetMethod("HandleTypeChanged",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SpawnerBody.HandleTypeChanged 메서드 존재 확인");
            mi.Invoke(body, new object[] { type });
        }

        //# 머티리얼 배열 생성 (EMonster 순서: 0=Wisp, 1=Wraith, 2=Reaper, 3=Hex, 4=Plague, 5=Phantom).
        private Material[] MakeMaterials(int count)
        {
            var mats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                var m = new Material(Shader.Find("Standard"));
                _assets.Add(m);
                mats[i] = m;
            }
            return mats;
        }

        //# 부모 Spawner 위에 자식 SpawnerBody 를 배치하는 표준 설정.
        //# spawnerOutputType — Spawner 직렬화 출력 종.
        //# setupBody — Renderer + Materials 주입 여부.
        //# 자식 SpawnerBody 는 부모가 없는 상태에서 AddComponent 해야 자동 OnEnable 이
        //# GetComponentInParent 를 못 찾고 구독하지 않는다. SetParent 는 그 후에 한다.
        //# (AddComponent on active GameObject triggers OnEnable immediately in EditMode)
        private (Spawner spawner, SpawnerBody body, MeshRenderer renderer, Material[] materials)
            CreateSetup(EMonster spawnerOutputType = EMonster.Wisp, bool setupBody = true,
                int matCount = 6)
        {
            //# 부모 GameObject — Spawner.
            var parentGo = new GameObject("SpawnerParent");
            _spawned.Add(parentGo);
            var spawner = parentGo.AddComponent<Spawner>();
            SetPrivate(spawner, "_outputType", spawnerOutputType);
            SetPrivate(spawner, "_spawnPeriod", 9f);
            SetPrivate(spawner, "_initialDelay", 0f);

            //# 자식 GameObject — 부모 없이 생성 후 AddComponent → auto-OnEnable 시 _provider = null(구독 안 함).
            //# 이후 SetParent 로 부모 연결 → InvokeBodyOnEnable 에서 명시적으로 구독.
            var childGo = new GameObject("SpawnerBodyChild");
            _spawned.Add(childGo);
            var body = childGo.AddComponent<SpawnerBody>();
            childGo.transform.SetParent(parentGo.transform);

            //# Renderer 와 Material 주입.
            MeshRenderer renderer = null;
            Material[] mats = null;
            if (setupBody)
            {
                renderer = childGo.AddComponent<MeshRenderer>();
                mats = MakeMaterials(matCount);
                SetPrivate(body, "_renderer", renderer);
                SetPrivate(body, "_materials", mats);
            }

            return (spawner, body, renderer, mats);
        }

        //# ===== OnEnable 초기 동기화 =====

        //# Spawner.OnEnable 이 발행한 초기 종으로 SpawnerBody 가 머티리얼을 적용한다.
        //# 순서: Spawner.OnEnable 이벤트 발행 → SpawnerBody.OnEnable 구독 → 동기화.
        [Test]
        public void OnEnable_초기_Wisp종으로_머티리얼_적용()
        {
            var (spawner, body, renderer, mats) = CreateSetup(EMonster.Wisp);

            //# SpawnerBody.OnEnable 이 GetComponentInParent 로 Spawner 의 ISpawnerOutputProvider 를 찾고
            //# HandleTypeChanged 를 구독 후 즉시 동기화 호출.
            //# Spawner 의 OnEnable 을 먼저 호출해 _currentType 초기화.
            InvokeOnEnable(spawner);
            InvokeBodyOnEnable(body);

            Assert.AreEqual(mats[(int)EMonster.Wisp], renderer.sharedMaterial,
                "초기 Wisp 종 → mats[0] 적용");
        }

        [Test]
        public void OnEnable_초기_Wraith종으로_머티리얼_적용()
        {
            var (spawner, body, renderer, mats) = CreateSetup(EMonster.Wraith);
            InvokeOnEnable(spawner);
            InvokeBodyOnEnable(body);

            Assert.AreEqual(mats[(int)EMonster.Wraith], renderer.sharedMaterial,
                "초기 Wraith 종 → mats[1] 적용");
        }

        //# ===== ReplaceOutput 후 머티리얼 교체 =====

        //# ReplaceOutput(Wraith) 시 mats[(int)Wraith] 으로 sharedMaterial 교체.
        [Test]
        public void ReplaceOutput_Wraith_머티리얼_교체()
        {
            var (spawner, body, renderer, mats) = CreateSetup(EMonster.Wisp);
            InvokeOnEnable(spawner);
            InvokeBodyOnEnable(body);

            spawner.ReplaceOutput(EMonster.Wraith);

            Assert.AreEqual(mats[(int)EMonster.Wraith], renderer.sharedMaterial,
                "ReplaceOutput(Wraith) → mats[1] 적용");
        }

        //# Wisp → Wraith → Reaper 연속 교체 시 각 단계에서 정확히 교체.
        [Test]
        public void ReplaceOutput_연속교체_각각_올바른_머티리얼()
        {
            var (spawner, body, renderer, mats) = CreateSetup(EMonster.Wisp);
            InvokeOnEnable(spawner);
            InvokeBodyOnEnable(body);

            spawner.ReplaceOutput(EMonster.Wraith);
            Assert.AreEqual(mats[(int)EMonster.Wraith], renderer.sharedMaterial, "1차 교체 Wraith");

            spawner.ReplaceOutput(EMonster.Reaper);
            Assert.AreEqual(mats[(int)EMonster.Reaper], renderer.sharedMaterial, "2차 교체 Reaper");
        }

        //# HandleTypeChanged 직접 호출 — Hex, Phantom 경계값.
        [Test]
        public void HandleTypeChanged_Hex_인덱스_3_올바른_머티리얼()
        {
            var (spawner, body, renderer, mats) = CreateSetup(EMonster.Wisp);
            InvokeOnEnable(spawner);
            InvokeBodyOnEnable(body);

            InvokeHandleTypeChanged(body, EMonster.Hex);

            Assert.AreEqual(mats[(int)EMonster.Hex], renderer.sharedMaterial,
                "Hex(index=3) 머티리얼 적용");
        }

        [Test]
        public void HandleTypeChanged_Phantom_인덱스_5_올바른_머티리얼()
        {
            var (_, body, renderer, mats) = CreateSetup(EMonster.Wisp);

            //# HandleTypeChanged 직접 — SpawnerBody 독립 테스트.
            SetPrivate(body, "_renderer", renderer);
            SetPrivate(body, "_materials", mats);
            InvokeHandleTypeChanged(body, EMonster.Phantom);

            Assert.AreEqual(mats[(int)EMonster.Phantom], renderer.sharedMaterial,
                "Phantom(index=5) 머티리얼 적용");
        }

        //# ===== 방어 케이스 — null/범위 초과 =====

        //# _renderer null 시 HandleTypeChanged 는 예외 없이 무동작.
        [Test]
        public void _renderer_null_HandleTypeChanged_예외없음()
        {
            var (_, body, _, mats) = CreateSetup(EMonster.Wisp, setupBody: false);
            var fakeMats = MakeMaterials(6);
            //# _renderer 미주입, _materials 만 주입.
            SetPrivate(body, "_renderer", null);
            SetPrivate(body, "_materials", fakeMats);

            Assert.DoesNotThrow(() => InvokeHandleTypeChanged(body, EMonster.Wisp),
                "_renderer null 이면 HandleTypeChanged 예외 없이 무동작");
        }

        //# _materials null 시 HandleTypeChanged 는 예외 없이 무동작.
        [Test]
        public void _materials_null_HandleTypeChanged_예외없음()
        {
            var (_, body, _, _) = CreateSetup(EMonster.Wisp, setupBody: false);
            var go = new GameObject("Rend");
            _spawned.Add(go);
            var renderer = go.AddComponent<MeshRenderer>();
            //# _renderer 주입, _materials 미주입.
            SetPrivate(body, "_renderer", renderer);
            SetPrivate(body, "_materials", null);

            Assert.DoesNotThrow(() => InvokeHandleTypeChanged(body, EMonster.Wisp),
                "_materials null 이면 HandleTypeChanged 예외 없이 무동작");
        }

        //# _materials 배열이 3개뿐인데 index=5(Phantom) — 범위 초과, 교체 안 함, 예외 없음.
        [Test]
        public void _materials_범위초과_인덱스_교체안함_예외없음()
        {
            var (_, body, renderer, _) = CreateSetup(EMonster.Wisp, setupBody: false);

            //# Sentinel 머티리얼 — 교체가 안 일어나야 이 머티리얼이 그대로여야 함.
            var sentinel = new Material(Shader.Find("Standard"));
            _assets.Add(sentinel);
            renderer.sharedMaterial = sentinel;

            SetPrivate(body, "_renderer", renderer);
            //# 3개 배열 — index 5(Phantom) 는 범위 초과.
            SetPrivate(body, "_materials", MakeMaterials(3));

            Assert.DoesNotThrow(() => InvokeHandleTypeChanged(body, EMonster.Phantom),
                "범위 초과 인덱스 → 예외 없음");
            Assert.AreEqual(sentinel, renderer.sharedMaterial,
                "범위 초과 시 sharedMaterial 은 변경 안 됨");
        }

        //# _materials 배열 내 null 항목 — materials[index] == null 이면 교체 안 함.
        [Test]
        public void _materials_항목_null_교체안함_예외없음()
        {
            var (_, body, renderer, _) = CreateSetup(EMonster.Wisp, setupBody: false);

            var sentinel = new Material(Shader.Find("Standard"));
            _assets.Add(sentinel);
            renderer.sharedMaterial = sentinel;

            //# Wisp(0) 항목을 null 로.
            var mats = MakeMaterials(6);
            mats[(int)EMonster.Wisp] = null;
            SetPrivate(body, "_renderer", renderer);
            SetPrivate(body, "_materials", mats);

            Assert.DoesNotThrow(() => InvokeHandleTypeChanged(body, EMonster.Wisp),
                "materials[index]=null → 예외 없음");
            Assert.AreEqual(sentinel, renderer.sharedMaterial,
                "materials[index]=null 이면 sharedMaterial 변경 안 됨");
        }

        //# ===== OnDisable — 이벤트 해제 후 ReplaceOutput 이 SpawnerBody 를 건드리지 않음 =====

        [Test]
        public void OnDisable_후_ReplaceOutput_머티리얼_변경안됨()
        {
            var (spawner, body, renderer, mats) = CreateSetup(EMonster.Wisp);
            InvokeOnEnable(spawner);
            InvokeBodyOnEnable(body);

            //# Wisp 머티리얼이 설정된 상태.
            var slimeMat = mats[(int)EMonster.Wisp];
            Assert.AreEqual(slimeMat, renderer.sharedMaterial);

            //# SpawnerBody.OnDisable — 이벤트 구독 해제.
            var onDisable = typeof(SpawnerBody).GetMethod("OnDisable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(onDisable, "SpawnerBody.OnDisable 존재 확인");
            onDisable.Invoke(body, null);

            //# 이후 ReplaceOutput — SpawnerBody 가 구독 해제됐으므로 머티리얼 불변.
            spawner.ReplaceOutput(EMonster.Wraith);
            Assert.AreEqual(slimeMat, renderer.sharedMaterial,
                "OnDisable 후 ReplaceOutput — SpawnerBody 구독 해제 → 머티리얼 변경 안 됨");
        }
    }
}
