using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.Character
{
    //# hero-2d-conversion §5.3.1·§9 불변식 — 2D 클립은 OnAttackStrike 만 굽고 OnAttackEnd/OnSpawnAnimEnd 는
    //# 절대 굽지 않는다(락 해제·스폰 게이트는 fallback 1.8s 로만 열린다, §5.3.1 안 A 채택).
    //# AnimationClip.events 를 에셋에서 직접 읽어 실제 .anim 파일이 이 계약을 지키는지 검증(추정 아님).
    public class HeroClip2DEventBakeTests
    {
        private const string Dir = "Assets/_Lair/Art/Animations/Heroes2D/";

        private static AnimationClip Load(string clipName)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir + clipName + ".anim");
            Assert.IsNotNull(clip, $"클립 로드 실패: {Dir}{clipName}.anim");
            return clip;
        }

        //# strike 이벤트가 굽힌 3개 클립 — §5.3 목표(클립 로컬 초) 그대로.
        [TestCase("Knight_Slash01", 0.5000f)]
        [TestCase("Knight_Slash02", 0.5000f)]
        [TestCase("Knight_Stab", 0.8163f)]
        public void 공격클립은_OnAttackStrike_1개만_목표초에_있다(string clipName, float expectedTime)
        {
            AnimationClip clip = Load(clipName);
            AnimationEvent[] events = clip.events;

            Assert.AreEqual(1, events.Length, $"{clipName} 은 OnAttackStrike 1개만 가져야 한다(§5.3.1)");
            Assert.AreEqual("OnAttackStrike", events[0].functionName, $"{clipName} 유일 이벤트는 OnAttackStrike");
            Assert.AreEqual(expectedTime, events[0].time, 0.0005f, $"{clipName} strike 클립 로컬 초(§5.3·§11)");
        }

        //# 공격 3클립 + Spawn — OnAttackEnd/OnSpawnAnimEnd 를 굽지 않는다(락·스폰 게이트는 fallback 1.8s, §5.3.1).
        [TestCase("Knight_Slash01")]
        [TestCase("Knight_Slash02")]
        [TestCase("Knight_Stab")]
        [TestCase("Knight_Spawn")]
        public void 클립에_OnAttackEnd_OnSpawnAnimEnd_이벤트가_없다(string clipName)
        {
            AnimationClip clip = Load(clipName);
            foreach (AnimationEvent e in clip.events)
            {
                Assert.IsFalse(Regex.IsMatch(e.functionName, "OnAttackEnd|OnSpawnAnimEnd"),
                    $"{clipName} 에 {e.functionName}@{e.time} 이벤트가 있으면 안 됨(§5.3.1 — fallback 1.8s 로만 열림)");
            }
        }

        //# Spawn 은 이벤트 자체가 0개(§9 "선행 확인" — OnSpawnAnimEnd 는 구조적으로 발화 불가해 아예 굽지 않음).
        [Test]
        public void Spawn클립은_이벤트가_0개다()
        {
            AnimationClip clip = Load("Knight_Spawn");
            Assert.AreEqual(0, clip.events.Length, "Knight_Spawn 은 이벤트를 굽지 않는다(§5.3.1)");
        }

        //# 자유 결정 상태(Idle/Walk/Run/Hit/Death) — 게임플레이 이벤트 계약이 없다(§5.2). 이벤트 0개로 고정.
        [TestCase("Knight_Idle")]
        [TestCase("Knight_Walk")]
        [TestCase("Knight_Run")]
        [TestCase("Knight_Hit")]
        [TestCase("Knight_Death")]
        public void 자유결정_상태클립은_이벤트가_0개다(string clipName)
        {
            AnimationClip clip = Load(clipName);
            Assert.AreEqual(0, clip.events.Length, $"{clipName} 은 게임플레이 이벤트 계약이 없다(§5.2)");
        }
    }
}
