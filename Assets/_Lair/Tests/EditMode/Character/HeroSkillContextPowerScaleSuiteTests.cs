using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Character
{
    //# 본격 스위트 — 영웅 스킬 데미지의 PowerScale 결합. 스케일 정확성/라운딩 경계/3 스킬 funnel/디버프 end-to-end/누적.
    //# 스모크 3건(정상 0.75·회귀 1.0·fallback null)은 HeroSkillContextPowerScaleTests.cs. 여기선 그 너머만.
    //#
    //# 설계 노트:
    //#   - 영웅 IAttacker 는 반드시 실제 MeleeAttacker(MonoBehaviour). HeroSkillContext 가 GetComponent<IAttacker> 로
    //#     캐싱하므로 POCO FakeAttacker 는 영웅에 부착 불가 → 스킬에 보이지 않는다. 디버프 Aura 도 같은 MeleeAttacker 인스턴스로 구동.
    //#   - 몬스터는 FakeHealth 를 RegisterMonster 로 등록 — LastDamage 로 적용 데미지 정확값을 HP 클램프 모호성 없이 단언.
    //#   - 기대 데미지는 손계산 상수. RoundToInt 로 재계산하지 않는다(impl 미러 → 버그 은폐 방지).
    //#   - RoundToInt 는 banker's rounding(half-to-even): 2.5→2, 3.5→4. 경계 케이스는 이를 정확히 반영.
    public class HeroSkillContextPowerScaleSuiteTests
    {
        private GameObject _hero;
        private MeleeAttacker _attacker;
        private GameObject _monster;
        private FakeHealth _health;

        [SetUp]
        public void SetUp()
        {
            _hero = new GameObject("Hero");
            _attacker = _hero.AddComponent<MeleeAttacker>();
            //# MeleeAttacker.OnEnable 이 PowerScale=1 로 리셋한 뒤 시작 — 명시적으로 한 번 더 고정.
            _attacker.PowerScale = 1f;

            _monster = new GameObject("Monster");
            _monster.transform.position = new Vector3(2f, 0f, 0f);   //# 반경 3 안.
            _health = new FakeHealth();
            _health.SetMax(100000, true);                            //# 클램프 안 닿게 충분히 큰 HP.
            CharacterRegistry.RegisterMonster(_monster.transform, _health);
            CharacterRegistry.SetMonsterEngaging(_monster.transform, true);
        }

        [TearDown]
        public void TearDown()
        {
            CharacterRegistry.UnregisterMonster(_monster.transform);
            Object.DestroyImmediate(_hero);
            Object.DestroyImmediate(_monster);
        }

        private HeroSkillContext NewContext() => new HeroSkillContext(_hero.transform);

        //# ===== 1. 스케일 정확성 — 여러 amount =====

        [Test]
        public void PowerScale_0_75_여러_amount_정확스케일()
        {
            _attacker.PowerScale = 0.75f;
            HeroSkillContext ctx = NewContext();

            //# (amount, 기대) — 손계산. 0.75 는 fp 정확. RoundToInt(half-to-even).
            (int amount, int expected)[] cases =
            {
                (4, 3),     //# 3.0
                (8, 6),     //# 6.0
                (40, 30),   //# 30.0
                (100, 75),  //# 75.0
                (200, 150), //# 150.0
            };

            foreach ((int amount, int expected) in cases)
            {
                ctx.DamageMonstersInRing(0f, 3f, amount, 0f);
                Assert.AreEqual(expected, _health.LastDamage,
                    $"amount={amount} × 0.75 → 기대 {expected}");
            }
        }

        [Test]
        public void PowerScale_0_5_절반_정확스케일()
        {
            _attacker.PowerScale = 0.5f;
            HeroSkillContext ctx = NewContext();

            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            Assert.AreEqual(50, _health.LastDamage, "100 × 0.5 = 50");

            ctx.DamageMonstersInRing(0f, 3f, 7, 0f);
            //# 7 × 0.5 = 3.5 → banker's rounding → 4.
            Assert.AreEqual(4, _health.LastDamage, "7 × 0.5 = 3.5 → RoundToInt 4");
        }

        [Test]
        public void PowerScale_1_25_증폭_정확스케일()
        {
            //# PowerScale 은 디버프뿐 아니라 버프(>1)도 표현 가능 — 스킬도 증폭 반영.
            _attacker.PowerScale = 1.25f;
            HeroSkillContext ctx = NewContext();

            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            Assert.AreEqual(125, _health.LastDamage, "100 × 1.25 = 125");
        }

        //# ===== 2. 회귀 — PowerScale 1.0 불변 =====

        [Test]
        public void PowerScale_1_회귀_여러_amount_불변()
        {
            _attacker.PowerScale = 1f;
            HeroSkillContext ctx = NewContext();

            foreach (int amount in new[] { 1, 13, 100, 999 })
            {
                ctx.DamageMonstersInRing(0f, 3f, amount, 0f);
                Assert.AreEqual(amount, _health.LastDamage, $"PowerScale 1.0 — amount {amount} 불변");
            }
        }

        //# ===== 3. 라운딩 경계 (banker's rounding 정확값) =====

        [Test]
        public void 라운딩_경계_0_75_정수내림올림()
        {
            _attacker.PowerScale = 0.75f;
            HeroSkillContext ctx = NewContext();

            //# 손계산: 1→0.75→1, 2→1.5→2, 3→2.25→2, 5→3.75→4, 6→4.5→4(half-to-even), 10→7.5→8.
            (int amount, int expected)[] cases =
            {
                (1, 1),    //# 0.75 → 1
                (2, 2),    //# 1.5  → 2
                (3, 2),    //# 2.25 → 2
                (5, 4),    //# 3.75 → 4
                (6, 4),    //# 4.5  → 4 (짝수쪽)
                (10, 8),   //# 7.5  → 8 (짝수쪽)
            };

            foreach ((int amount, int expected) in cases)
            {
                ctx.DamageMonstersInRing(0f, 3f, amount, 0f);
                Assert.AreEqual(expected, _health.LastDamage,
                    $"RoundToInt({amount} × 0.75) = {expected}");
            }
        }

        [Test]
        public void 라운딩_banker_절반_짝수쪽_정렬()
        {
            //# RoundToInt 가 round-half-up 이 아니라 half-to-even 임을 박제.
            _attacker.PowerScale = 0.25f;
            HeroSkillContext ctx = NewContext();

            //# 10 × 0.25 = 2.5 → 짝수쪽 2 (round-half-up 이면 3 — 이 단언이 그 차이를 잡는다).
            ctx.DamageMonstersInRing(0f, 3f, 10, 0f);
            Assert.AreEqual(2, _health.LastDamage, "2.5 → banker's rounding 2 (3 아님)");

            //# 6 × 0.25 = 1.5 → 짝수쪽 2.
            ctx.DamageMonstersInRing(0f, 3f, 6, 0f);
            Assert.AreEqual(2, _health.LastDamage, "1.5 → banker's rounding 2");

            //# 2 × 0.25 = 0.5 → 짝수쪽 0.
            ctx.DamageMonstersInRing(0f, 3f, 2, 0f);
            Assert.AreEqual(0, _health.LastDamage, "0.5 → banker's rounding 0");
        }

        //# ===== 4. IAttacker fallback =====

        [Test]
        public void IAttacker_부재_배율1_amount_그대로()
        {
            //# 영웅에 IAttacker(MeleeAttacker) 없는 별도 GameObject — GetComponent null → 배율 1.
            GameObject bareHero = new GameObject("BareHero");
            try
            {
                HeroSkillContext ctx = new HeroSkillContext(bareHero.transform);
                _monster.transform.position = new Vector3(2f, 0f, 0f);

                ctx.DamageMonstersInRing(0f, 3f, 137, 0f);
                Assert.AreEqual(137, _health.LastDamage, "IAttacker 부재 → 배율 1, amount 그대로");
            }
            finally
            {
                Object.DestroyImmediate(bareHero);
            }
        }

        [Test]
        public void Hero_Transform_null_배율1_NRE없음()
        {
            //# 생성자 _hero null 가드 — HeroPosition zero, _heroAttacker null → 배율 1. 대상 없으면 0 hit.
            HeroSkillContext ctx = new HeroSkillContext(null);
            int hit = ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            //# 영웅 원점(0,0,0) 기준 반경 3 — 몬스터(2,0,0) 포함. NRE 없이 배율 1 적용.
            Assert.AreEqual(1, hit, "hero null 이어도 원점 기준 판정 — NRE 없음");
            Assert.AreEqual(100, _health.LastDamage, "hero null → 배율 1 fallback");
        }

        //# ===== 5. 3 스킬 funnel — Ring/Cone/Spheres 동일 스케일 =====

        [Test]
        public void Ring_Cone_Spheres_세_스킬_모두_동일_스케일()
        {
            _attacker.PowerScale = 0.75f;
            HeroSkillContext ctx = NewContext();

            //# 몬스터 (2,0,0). Ring: 반경 3 포함. Cone: +X 방향 length 5·halfAngle 45 포함. Sphere: 중심 (2,0,0) 반경 1 포함.
            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            Assert.AreEqual(75, _health.LastDamage, "Ring 75");

            ctx.DamageMonstersInCone(Vector3.right, 5f, 45f, 100, 0f);
            Assert.AreEqual(75, _health.LastDamage, "Cone 75");

            List<Vector3> centers = new List<Vector3> { new Vector3(2f, 0f, 0f) };
            ctx.DamageMonstersInSpheres(centers, 1f, 100, 0f);
            Assert.AreEqual(75, _health.LastDamage, "Spheres 75");
        }

        [Test]
        public void Cone_라운딩_경계도_funnel_일관()
        {
            _attacker.PowerScale = 0.25f;
            HeroSkillContext ctx = NewContext();

            //# 10 × 0.25 = 2.5 → 2. Cone funnel 도 Ring 과 동일 라운딩.
            ctx.DamageMonstersInCone(Vector3.right, 5f, 45f, 10, 0f);
            Assert.AreEqual(2, _health.LastDamage, "Cone funnel banker's rounding 2");
        }

        [Test]
        public void Spheres_union_dedup_도_스케일_1회()
        {
            _attacker.PowerScale = 0.5f;
            HeroSkillContext ctx = NewContext();

            //# 한 몬스터가 두 구에 동시 진입 — union dedup 으로 1회만 데미지(스케일된).
            List<Vector3> centers = new List<Vector3>
            {
                new Vector3(2f, 0f, 0f),
                new Vector3(2.3f, 0f, 0f),
            };
            int hit = ctx.DamageMonstersInSpheres(centers, 1f, 80, 0f);
            Assert.AreEqual(1, hit, "두 구 동시 진입 — dedup 1 hit");
            Assert.AreEqual(1, _health.DamageCallCount, "TakeDamage 1회만");
            Assert.AreEqual(40, _health.LastDamage, "80 × 0.5 = 40, 중복 적용 없음");
        }

        //# ===== 6. 디버프 연동 end-to-end (핵심) =====

        [Test]
        public void HeroAttackDownAura_부착_스킬데미지_75퍼센트_endtoend()
        {
            //# 영웅 실제 MeleeAttacker 에 디버프 Aura 의 OnAttached 가 PowerScale *= 0.75. 같은 인스턴스로 스킬 시전.
            FakeHealth heroHealth = new FakeHealth();
            HeroAttackDownAura aura = new HeroAttackDownAura(_attacker, 0.75f);
            aura.OnAttached(heroHealth);
            Assert.AreEqual(0.75f, _attacker.PowerScale, 0.0001f, "Aura 부착 후 PowerScale 0.75");

            HeroSkillContext ctx = NewContext();
            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            Assert.AreEqual(75, _health.LastDamage, "디버프→스킬 end-to-end 75%");
        }

        [Test]
        public void WeakenAura_부착_감소_Detach_원복_liveread()
        {
            //# live-read 증명 — 동일 ctx 로 부착 전/후/복원 후를 모두 시전. ctx 가 PowerScale 을 적용시점에 읽음.
            FakeHealth heroHealth = new FakeHealth();
            HeroSkillContext ctx = NewContext();

            //# 부착 전 — 배율 1.
            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            Assert.AreEqual(100, _health.LastDamage, "부착 전 100");

            //# WeakenAura(0.5) 부착 — PowerScale 0.5.
            WeakenAura weaken = new WeakenAura(_attacker, 0.5f);
            weaken.OnAttached(heroHealth);
            Assert.AreEqual(0.5f, _attacker.PowerScale, 0.0001f, "Weaken 부착 후 0.5");
            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            Assert.AreEqual(50, _health.LastDamage, "부착 중 50 — 동일 ctx live read");

            //# Detach — _backup(1.0) 복원.
            weaken.OnDetached(heroHealth);
            Assert.AreEqual(1f, _attacker.PowerScale, 0.0001f, "Detach 후 1.0 복원");
            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            Assert.AreEqual(100, _health.LastDamage, "복원 후 100 — 동일 ctx live read 원복");
        }

        //# ===== 7. 다중 디버프 누적 — 곱연산이 스킬에 반영 =====

        [Test]
        public void HeroAttackDown_직접_2회_부착_곱연산_스킬반영()
        {
            //# OnAttached 직접 2회 호출(서로 다른 인스턴스) — 각 호출이 PowerScale *= 0.75 → ×0.5625.
            //# (HeroAuraRunner 의 같은-factor dedup 가드는 직접 호출 경로엔 없음.)
            FakeHealth heroHealth = new FakeHealth();
            new HeroAttackDownAura(_attacker, 0.75f).OnAttached(heroHealth);
            new HeroAttackDownAura(_attacker, 0.75f).OnAttached(heroHealth);
            Assert.AreEqual(0.5625f, _attacker.PowerScale, 0.0001f, "0.75 × 0.75 = 0.5625");

            HeroSkillContext ctx = NewContext();
            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            //# 100 × 0.5625 = 56.25 → RoundToInt → 56.
            Assert.AreEqual(56, _health.LastDamage, "누적 ×0.5625 → 56");
        }

        [Test]
        public void HeroAttackDown_더하기_Weaken_곱연산_스킬반영()
        {
            //# 서로 다른 type 디버프 — HeroAttackDown(0.75) × Weaken(0.5) = ×0.375.
            FakeHealth heroHealth = new FakeHealth();
            new HeroAttackDownAura(_attacker, 0.75f).OnAttached(heroHealth);
            new WeakenAura(_attacker, 0.5f).OnAttached(heroHealth);
            Assert.AreEqual(0.375f, _attacker.PowerScale, 0.0001f, "0.75 × 0.5 = 0.375");

            HeroSkillContext ctx = NewContext();
            ctx.DamageMonstersInRing(0f, 3f, 100, 0f);
            //# 100 × 0.375 = 37.5 → banker's rounding → 38.
            Assert.AreEqual(38, _health.LastDamage, "누적 ×0.375 → 37.5 → 38");
        }
    }
}
