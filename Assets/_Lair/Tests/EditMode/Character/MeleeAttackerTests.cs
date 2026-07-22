using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    //# MeleeAttacker 는 MonoBehaviour — GameObject 에 AddComponent 로 붙여야 enabled 등 네이티브 멤버가 유효하다.
    public class MeleeAttackerTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
        }

        private MeleeAttacker NewAttacker(float range = 1.5f, float cd = 1.0f, int power = 50)
        {
            GameObject go = new GameObject("MeleeAttackerHost");
            _spawned.Add(go);
            //# EditMode 의 AddComponent 는 OnEnable 을 발화시키지 않는다(ExecuteAlways 아님).
            //# 쿨다운·배율 초기값은 필드 초기자가 보장하므로 발화 여부와 무관하게 동일하다.
            MeleeAttacker a = go.AddComponent<MeleeAttacker>();
            a.Configure(range, cd, power);
            return a;
        }

        [Test]
        public void 사거리_밖_TryAttack_거부()
        {
            MeleeAttacker atk = NewAttacker(range: 1.0f);
            FakeHealth target = new FakeHealth();

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(5, 0, 0), now: 0f);

            Assert.IsFalse(hit);
            Assert.AreEqual(0, target.DamageCallCount);
        }

        [Test]
        public void 사거리_내_쿨_0_데미지_Power_만큼_적용()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);

            Assert.IsTrue(hit);
            Assert.AreEqual(1, target.DamageCallCount);
            Assert.AreEqual(50, target.LastDamage);
        }

        [Test]
        public void 쿨다운_중_재시도_거부()
        {
            MeleeAttacker atk = NewAttacker(cd: 1.0f);
            FakeHealth target = new FakeHealth();
            atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0f);

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0.5f);

            Assert.IsFalse(hit);
            Assert.AreEqual(1, target.DamageCallCount);
        }

        [Test]
        public void 쿨다운_경과_후_재공격_가능()
        {
            MeleeAttacker atk = NewAttacker(cd: 1.0f);
            FakeHealth target = new FakeHealth();
            atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0f);

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 1.5f);

            Assert.IsTrue(hit);
            Assert.AreEqual(2, target.DamageCallCount);
        }

        //# ===== 영웅 흐름 역전 — TryBeginAttack / TryApplyStrike 2분할 (hero-animation-timing-sync §2.4) =====

        //# 정상 케이스: 개시(begin)는 데미지 미적용, strike 가 비로소 데미지 적용.
        [Test]
        public void TryBeginAttack_개시는_데미지_미적용_Strike가_적용()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            bool began = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);

            Assert.IsTrue(began, "사거리·쿨다운 통과 시 개시 성공");
            Assert.AreEqual(0, target.DamageCallCount, "개시 시점엔 데미지 미적용(windup 지연)");

            bool struck = atk.TryApplyStrike(now: 0.4f);

            Assert.IsTrue(struck, "strike 재검사 통과 시 데미지 적용");
            Assert.AreEqual(1, target.DamageCallCount);
            Assert.AreEqual(50, target.LastDamage);
        }

        //# 엣지: strike 전 windup 동안 타겟이 사거리 밖으로 이탈 → 헛스윙(데미지 0).
        //# 캐싱은 개시 시점 위치 기준이므로, 개시 시점에 사거리 밖이면 begin 자체가 실패.
        //# 여기서는 개시는 사거리 내, strike 재검사는 범위 밖 — Configure 후 range 축소로 재현.
        [Test]
        public void TryApplyStrike_캐싱타겟_사거리_재검사_실패시_헛스윙()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            //# 개시 — 사거리(2.0) 내 거리 1.0 통과, 타겟·위치 캐싱.
            bool began = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);
            Assert.IsTrue(began);

            //# windup 중 사거리 축소 → 캐싱 거리 1.0 이 새 사거리 0.5 를 초과 → strike 재검사 실패.
            atk.Configure(range: 0.5f, cooldown: 1.0f, power: 50);

            bool struck = atk.TryApplyStrike(now: 0.4f);

            Assert.IsFalse(struck, "재검사 실패 시 헛스윙");
            Assert.AreEqual(0, target.DamageCallCount, "헛스윙은 데미지 0");
        }

        //# PowerScale 은 strike 시점에 읽힌다(§2.4-a) — 개시 후 배율 변경이 strike 데미지에 반영.
        [Test]
        public void TryApplyStrike_PowerScale_Strike시점_배율_적용()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);
            //# windup 동안 약화 카드로 PowerScale 하락 → strike 데미지에 반영(개시 시점 1.0 이 아닌 0.5).
            atk.PowerScale = 0.5f;

            atk.TryApplyStrike(now: 0.4f);

            Assert.AreEqual(25, target.LastDamage, "strike 순간 PowerScale 0.5 → 50*0.5=25");
        }
    }
}
