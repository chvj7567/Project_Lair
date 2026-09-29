using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# hero-2d-conversion §5.3.1·§11 — OnAttackEnd/OnSpawnAnimEnd 를 2D 클립에 굽지 않기로 한 결정에 따라,
    //# 세 fallback 필드가 실질적인 "공격 락 시간/스폰 대기 시간"이 됐다. 값이 임의로 바뀌면 DPS·스폰 타이밍이 바뀐다.
    //# 본격 회귀는 test-engineer — 여기선 실측값이 1.8 로 고정돼 있는지만 확인.
    public class HeroFallbackTimingTests
    {
        private const string KnightPrefabPath = "Assets/_Lair/Art/Characters/Knight.prefab";

        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
            _go = null;
        }

        private static float ReadPrivateFloat(object target, string fieldName)
        {
            FieldInfo f = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{fieldName} 필드 존재 확인 — 시그니처 변경 감지");
            return (float)f.GetValue(target);
        }

        //# HeroAttackGate/AutoCombatAI 는 Knight.prefab 에 실제로 부착돼 있다(§7.4 "유지" 목록).
        [Test]
        public void Knight_프리팹의_HeroAttackGate_AutoCombatAI_fallback은_1_8초로_고정돼있다()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath);
            Assert.IsNotNull(prefab, $"프리팹 로드 실패: {KnightPrefabPath}");

            HeroAttackGate gate = prefab.GetComponent<HeroAttackGate>();
            AutoCombatAI ai = prefab.GetComponent<AutoCombatAI>();
            Assert.IsNotNull(gate, "HeroAttackGate 존재");
            Assert.IsNotNull(ai, "AutoCombatAI 존재");

            Assert.AreEqual(1.8f, ReadPrivateFloat(gate, "_attackEndFallback"), 1e-4f,
                "HeroAttackGate._attackEndFallback — 공격 락 시간(§5.3.1)");
            Assert.AreEqual(1.8f, ReadPrivateFloat(ai, "_spawnGateFallback"), 1e-4f,
                "AutoCombatAI._spawnGateFallback — 교전 개시 시각(§5.3.1)");
        }

        //# HeroEntryDriver 는 Knight.prefab 에 baked 되어 있지 않다 — BattleController.SpawnHero 가
        //# BattleZone 존재 시 p.gameObject.AddComponent<HeroEntryDriver>() 로 런타임 부착한다(GetComponent 우선, 없으면 Add).
        //# 그래서 여기서 검증할 대상은 "프리팹의 값"이 아니라 "AddComponent 시 적용되는 C# 기본값" 이다.
        [Test]
        public void HeroEntryDriver_런타임_부착시_기본_fallback은_1_8초다()
        {
            _go = new GameObject("HeroEntryDriverFallbackUT");
            _go.AddComponent<LairCharacter>();
            HeroEntryDriver driver = _go.AddComponent<HeroEntryDriver>();

            Assert.AreEqual(1.8f, ReadPrivateFloat(driver, "_spawnGateFallback"), 1e-4f,
                "HeroEntryDriver._spawnGateFallback — march 시작 시각(§5.3.1), BattleController 런타임 AddComponent 기본값");
        }
    }
}
