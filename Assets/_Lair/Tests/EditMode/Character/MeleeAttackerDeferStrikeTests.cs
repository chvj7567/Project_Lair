using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    //# 영웅 흐름 역전(hero-animation-timing-sync §2·§B) 본격 엣지/회귀 — gameplay-programmer 신규 3건 너머 망라.
    //# 대상: MeleeAttacker.DeferStrike 분기 / 헛스윙 경계값 / 배율 시점(CooldownScale=begin, PowerScale=strike) / 쿨다운.
    //# MeleeAttacker 는 MonoBehaviour 라 반드시 GameObject 에 AddComponent 로 붙인다 — new 로 만들면 네이티브 객체가 없어
    //# TryApplyStrike 의 enabled 게이트(§2.5 TimeStop)에서 NRE. now 는 직접 주입 — Time.time 비의존, 결정적.
    public class MeleeAttackerDeferStrikeTests
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

        //# ===== DeferStrike 분기 — 몬스터(즉시) 경로 보존 =====

        //# 회귀: DeferStrike 기본 false. 몬스터는 TryAttack 즉시 경로로 데미지가 들어가야 한다(공통 컴포넌트 보존 §B.1).
        [Test]
        public void DeferStrike_기본값_false_몬스터_즉시경로()
        {
            MeleeAttacker atk = NewAttacker();
            Assert.IsFalse(atk.DeferStrike, "SerializeField 미설정 시 기본 false(몬스터)");
        }

        //# 회귀: 몬스터 TryAttack 즉시 경로는 begin/strike 캐싱과 무관하게 1콜로 데미지 적용.
        [Test]
        public void TryAttack_즉시경로_단일호출로_데미지적용()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 40);
            FakeHealth target = new FakeHealth();

            bool hit = atk.TryAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);

            Assert.IsTrue(hit);
            Assert.AreEqual(1, target.DamageCallCount);
            Assert.AreEqual(40, target.LastDamage);
        }

        //# 엣지: begin 없이 strike 단독 호출 — pendingTarget 캐시가 없으므로 헛스윙(데미지 0, false).
        //# 정의대로(§2.4) 캐싱 타겟 없으면 null → false.
        [Test]
        public void TryApplyStrike_begin없이_단독호출시_타겟캐시없음_false()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);

            bool struck = atk.TryApplyStrike(now: 0f);

            Assert.IsFalse(struck, "캐싱 타겟 없으면 strike 헛스윙");
        }

        //# 엣지: strike 는 1회 소비형 — begin 1회 후 strike 두 번째 호출은 pendingTarget=null 이라 false.
        [Test]
        public void TryApplyStrike_연속2회_두번째는_캐시소비후_false()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);
            bool first = atk.TryApplyStrike(now: 0.4f);
            bool second = atk.TryApplyStrike(now: 0.5f);

            Assert.IsTrue(first, "첫 strike 적용");
            Assert.IsFalse(second, "두 번째 strike 는 캐시 소비됨 → false(이중 데미지 방지)");
            Assert.AreEqual(1, target.DamageCallCount, "데미지는 1회만");
        }

        //# 엣지: begin 시점에 타겟이 이미 사망 → begin 자체 거부(데미지·캐싱 없음).
        [Test]
        public void TryBeginAttack_타겟_사망시_개시거부()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();
            target.TakeDamage(target.Current);   //# HP 0 → IsAlive=false
            Assert.IsFalse(target.IsAlive);

            bool began = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);

            Assert.IsFalse(began, "사망 타겟엔 개시 거부");
        }

        //# 엣지: begin 통과 후 strike 전에 타겟 사망 → strike 재검사 실패(헛스윙).
        [Test]
        public void TryApplyStrike_begin후_strike전_타겟사망시_헛스윙()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            bool began = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);
            Assert.IsTrue(began);

            //# windup 중 타겟 사망.
            target.TakeDamage(target.Current);
            Assert.IsFalse(target.IsAlive);

            int beforeCount = target.DamageCallCount;
            bool struck = atk.TryApplyStrike(now: 0.4f);

            Assert.IsFalse(struck, "strike 생존 재검사 실패 → 헛스윙");
            Assert.AreEqual(beforeCount, target.DamageCallCount, "헛스윙은 추가 데미지 0");
        }

        //# ===== 헛스윙 사거리 경계값 (§2.5) =====

        //# 경계: begin 시점 정확히 range == dist → 통과(<=). strike 도 정확히 range → 통과.
        [Test]
        public void 사거리_경계_정확히_range_일치시_begin과_strike_모두통과()
        {
            //# range 2.0, dist 2.0 — Vector3.Distance(0, (2,0,0)) == 2.0.
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            bool began = atk.TryBeginAttack(target, Vector3.zero, new Vector3(2f, 0, 0), now: 0f);
            Assert.IsTrue(began, "dist==range 는 사거리 내(<=)");

            bool struck = atk.TryApplyStrike(now: 0.4f);
            Assert.IsTrue(struck, "캐싱 dist==range strike 통과");
            Assert.AreEqual(1, target.DamageCallCount);
        }

        //# 경계: begin 시점 range+ε(미세 초과) → begin 거부(데미지·캐싱 없음).
        [Test]
        public void 사거리_경계_range_미세초과시_begin거부()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            bool began = atk.TryBeginAttack(target, Vector3.zero, new Vector3(2.001f, 0, 0), now: 0f);

            Assert.IsFalse(began, "range+ε 초과는 개시 거부");
            //# 거부됐으므로 캐시 없음 → 후속 strike 도 false.
            Assert.IsFalse(atk.TryApplyStrike(now: 0.4f), "begin 거부 후 strike 캐시 없음");
        }

        //# 경계: begin 은 사거리 내, windup 중 타겟이 range 밖으로 이동 → strike 재검사 실패(헛스윙).
        //# 캐싱은 begin 시점 selfPos/targetPos 기준이므로, 사거리 축소로 재현(기존 신규 테스트 보강 — 위치 이동 관점).
        [Test]
        public void 헛스윙_begin사거리내_strike시점_사거리밖_데미지0()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            //# begin: dist 1.5 < range 2.0 통과, 위치 캐싱.
            bool began = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1.5f, 0, 0), now: 0f);
            Assert.IsTrue(began);

            //# windup 중 타겟이 멀어진 효과 = range 를 캐싱 거리(1.5) 아래로 축소.
            atk.Configure(range: 1.0f, cooldown: 1.0f, power: 50);

            bool struck = atk.TryApplyStrike(now: 0.4f);

            Assert.IsFalse(struck, "캐싱 dist 1.5 > 새 range 1.0 → 헛스윙");
            Assert.AreEqual(0, target.DamageCallCount, "헛스윙 데미지 0");
        }

        //# ===== 배율 시점 (§2.4-a) =====

        //# CooldownScale 은 begin(개시) 시점 읽힘 — begin 후 strike 전 CooldownScale 변경은 이미 기록된 쿨다운에 영향 없음.
        //# 검증: begin 시 CooldownScale=2 로 쿨다운 게이트가 길어진다(다음 begin 차단 길어짐).
        [Test]
        public void CooldownScale_begin시점_읽힘_쿨다운게이트_확장()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, cd: 1.0f, power: 50);
            FakeHealth target = new FakeHealth();

            atk.CooldownScale = 2f;   //# 쿨다운 1.0 * 2 = 2.0s.
            bool first = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);
            Assert.IsTrue(first);

            //# now=1.5 < 2.0(=cd*scale) → 쿨다운 미경과로 두 번째 begin 거부.
            bool second = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 1.5f);
            Assert.IsFalse(second, "CooldownScale 2 적용 — 1.5s 는 쿨다운 2.0s 미경과");

            //# now=2.0 >= 2.0 → 통과.
            bool third = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 2.0f);
            Assert.IsTrue(third, "쿨다운 2.0s 경과 후 재개시 가능");
        }

        //# PowerScale 은 strike 시점 읽힘 — begin 시 1.0 이었어도 strike 직전 변경값이 데미지에 반영(§2.4-a).
        [Test]
        public void PowerScale_strike시점_읽힘_begin후_변경값_반영()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            //# begin 시점 PowerScale = 1.0 (기본).
            atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);

            //# windup 중 광폭화로 상승.
            atk.PowerScale = 2f;
            atk.TryApplyStrike(now: 0.4f);

            Assert.AreEqual(100, target.LastDamage, "strike 시점 PowerScale 2.0 → 50*2=100");
        }

        //# 배율 라운딩 — RoundToInt(50 * 0.7) = 35. 비정수 배율도 strike 시점 반영.
        [Test]
        public void PowerScale_비정수배율_strike시점_라운딩()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, power: 50);
            FakeHealth target = new FakeHealth();

            atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);
            atk.PowerScale = 0.7f;
            atk.TryApplyStrike(now: 0.4f);

            Assert.AreEqual(35, target.LastDamage, "RoundToInt(50*0.7)=35");
        }

        //# ===== 쿨다운 + 흐름 역전 상호작용 =====

        //# 쿨다운은 begin 시점에 기록 — strike 가 헛스윙(false)이어도 쿨다운은 이미 찍혀 다음 begin 은 정상 게이트.
        [Test]
        public void 쿨다운_begin시점기록_strike헛스윙이어도_게이트유지()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, cd: 1.0f, power: 50);
            FakeHealth target = new FakeHealth();

            atk.TryBeginAttack(target, Vector3.zero, new Vector3(1.5f, 0, 0), now: 0f);
            //# strike 헛스윙(사거리 축소).
            atk.Configure(range: 1.0f, cooldown: 1.0f, power: 50);
            atk.TryApplyStrike(now: 0.4f);

            //# 쿨다운 미경과(now 0.5 < 1.0) → 다음 begin 거부 — 헛스윙이라고 쿨다운이 풀리지 않는다.
            bool early = atk.TryBeginAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 0.5f);
            Assert.IsFalse(early, "헛스윙이어도 begin 쿨다운 기록은 유지");

            //# 쿨다운 경과 후 정상 재개시.
            bool ready = atk.TryBeginAttack(target, Vector3.zero, new Vector3(0.5f, 0, 0), now: 1.0f);
            Assert.IsTrue(ready, "쿨다운 경과 후 재개시 가능");
        }

        //# 쿨다운 내 재 begin 거부 — TryBeginAttack 이 쿨다운 게이트를 자체 검사(§3.3).
        [Test]
        public void TryBeginAttack_쿨다운내_재개시_거부()
        {
            MeleeAttacker atk = NewAttacker(range: 2.0f, cd: 1.0f, power: 50);
            FakeHealth target = new FakeHealth();

            atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0f);
            bool second = atk.TryBeginAttack(target, Vector3.zero, new Vector3(1, 0, 0), now: 0.5f);

            Assert.IsFalse(second, "쿨다운 1.0s 내 0.5s 재개시 거부");
        }
    }
}
