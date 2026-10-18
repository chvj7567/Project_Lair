using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 성기사(챕터 5) 순수 로직 검증 - 응징 표적(GuardBehavior), 신앙의 가호(HeroAuraRunner 출처 한정 보정), 정화 대상 집계, 스킬 3종 (chapter-5 §3.3·§4·§9.1).
    //# 월드 의존(AutoCombatAI 응징 표적 해석 우선순위·FleeMode 우선·중앙 끌림 없음, 몬스터 기절·슬로우 구역, 회복의 HP 임계 재무장 없음, 다른 4영웅 경로 불변)은 PlayMode(G8)에서 확인한다.
    public class Chapter5PaladinLogicTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        [SetUp]
        public void FX_로그를_격리한다()
        {
            ArcherFxLogGuard.Suppress();
        }

        [TearDown]
        public void 생성물을_정리한다()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
            foreach (ScriptableObject asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }
            _assets.Clear();
            ArcherFxLogGuard.Restore();
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        //# ===== 설정 변환 =====

        [Test]
        public void GuardSettings와_DebuffResistSettings_From은_row_값을_그대로_옮긴다()
        {
            GuardSettings guard = GuardSettings.From(new HeroArchetype_GuardRow { heroId = "Paladin", retributionLockSec = 2f });
            DebuffResistSettings resist = DebuffResistSettings.From(new HeroArchetype_DebuffResistRow { heroId = "Paladin", durationMul = 0.6f, penaltyMul = 0.5f });

            Assert.AreEqual(2f, guard.RetributionLockSec, 0.0001f);
            Assert.AreEqual(0.6f, resist.DurationMul, 0.0001f);
            Assert.AreEqual(0.5f, resist.PenaltyMul, 0.0001f);
        }

        [Test]
        public void 실제_JSON의_성기사_Guard_DebuffResist_설정이_기획서_값이다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();

            GuardSettings guard = GuardSettings.From(db.GetGuard("Paladin"));
            DebuffResistSettings resist = DebuffResistSettings.From(db.GetDebuffResist("Paladin"));
            HeroArchetypeRow hero = db.GetHero("Paladin");

            Assert.AreEqual(2.0f, guard.RetributionLockSec, 0.0001f);
            Assert.AreEqual(0.6f, resist.DurationMul, 0.0001f);
            Assert.AreEqual(0.6f, resist.PenaltyMul, 0.0001f);
            Assert.AreEqual("Guardian", hero.aiType);
            Assert.AreEqual(1.6f, hero.range, 0.0001f);
            Assert.AreEqual(1.1f, hero.cooldown, 0.0001f);
            Assert.AreEqual(2.9f, hero.moveSpeed, 0.0001f);
        }

        //# ===== 응징 표적 (GuardBehavior) =====

        private FakeHealth NewMob(out Transform t)
        {
            GameObject go = new GameObject("mob");
            _spawned.Add(go);
            t = go.transform;
            return new FakeHealth();
        }

        private static GuardBehavior NewGuard(float lockSec = 2f)
        {
            return new GuardBehavior(new GuardSettings(lockSec));
        }

        [Test]
        public void 가해_통지_직후_잠금_시간_2_0초_동안은_가해자가_강제_표적이다()
        {
            GuardBehavior guard = NewGuard();
            FakeHealth mob = NewMob(out Transform t);

            guard.NotifyDebuffedBy(t, mob, 10f);

            Assert.IsTrue(guard.TryGetLockedTarget(10f, out Transform target, out IHealth health));
            Assert.AreSame(t, target);
            Assert.AreSame(mob, health);
            Assert.IsTrue(guard.TryGetLockedTarget(11.99f, out _, out _), "만료 직전");
        }

        [Test]
        public void 잠금_시간이_지나면_표적을_풀고_이후에도_없다()
        {
            GuardBehavior guard = NewGuard();
            FakeHealth mob = NewMob(out Transform t);
            guard.NotifyDebuffedBy(t, mob, 10f);

            Assert.IsFalse(guard.TryGetLockedTarget(12f, out Transform target, out IHealth health), "now >= lockUntil");
            Assert.IsNull(target);
            Assert.IsNull(health);
            Assert.IsFalse(guard.TryGetLockedTarget(10.5f, out _, out _), "만료로 기억을 지웠다");
        }

        [Test]
        public void 가해자가_죽거나_비활성화되면_잠금이_풀린다()
        {
            GuardBehavior dead = NewGuard();
            FakeHealth deadMob = NewMob(out Transform deadT);
            dead.NotifyDebuffedBy(deadT, deadMob, 0f);
            deadMob.ForceSetCurrent(0);
            Assert.IsFalse(dead.TryGetLockedTarget(0.5f, out _, out _), "사망");

            GuardBehavior inactive = NewGuard();
            FakeHealth pooledMob = NewMob(out Transform pooledT);
            inactive.NotifyDebuffedBy(pooledT, pooledMob, 0f);
            pooledT.gameObject.SetActive(false);
            Assert.IsFalse(inactive.TryGetLockedTarget(0.5f, out _, out _), "풀 반환(비활성)");
        }

        [Test]
        public void 새_가해자가_통지되면_표적이_교체되고_잠금_시간을_새로_잰다()
        {
            GuardBehavior guard = NewGuard();
            FakeHealth first = NewMob(out Transform firstT);
            FakeHealth second = NewMob(out Transform secondT);
            guard.NotifyDebuffedBy(firstT, first, 0f);

            guard.NotifyDebuffedBy(secondT, second, 1.5f);

            Assert.IsTrue(guard.TryGetLockedTarget(3.0f, out Transform target, out _), "첫 잠금(2.0)이었다면 만료, 새 잠금은 3.5 까지");
            Assert.AreSame(secondT, target);
            Assert.IsFalse(guard.TryGetLockedTarget(3.5f, out _, out _));
        }

        [Test]
        public void Reset은_표적과_잠금을_지우고_null_통지는_무시한다()
        {
            GuardBehavior guard = NewGuard();
            FakeHealth mob = NewMob(out Transform t);
            guard.NotifyDebuffedBy(t, mob, 0f);

            guard.Reset();
            Assert.IsFalse(guard.TryGetLockedTarget(0.1f, out _, out _));

            guard.NotifyDebuffedBy(null, mob, 0f);
            guard.NotifyDebuffedBy(t, null, 0f);
            Assert.IsFalse(guard.TryGetLockedTarget(0.1f, out _, out _), "가해자 정보가 불완전하면 기억하지 않는다");
        }

        [Test]
        public void 잠금_시간은_설정값을_따른다()
        {
            GuardBehavior guard = NewGuard(0.5f);
            FakeHealth mob = NewMob(out Transform t);
            guard.NotifyDebuffedBy(t, mob, 0f);

            Assert.IsTrue(guard.TryGetLockedTarget(0.49f, out _, out _));
            Assert.IsFalse(guard.TryGetLockedTarget(0.5f, out _, out _));
        }

        //# ===== 신앙의 가호 / 정화 대상 집계 (HeroAuraRunner) =====

        //# 출처 한정 보정 검증용 시한 Debuff 축 더블 - PoisonAura 는 OnAttached 가 월드 FX 를 요청해 EditMode 에서 쓸 수 없어 같은 마커만 가진 더블로 대신한다.
        private class AxisStubAura : IHeroAura, IDebuffAxisAura
        {
            public bool IsDebuffAxis => true;
            public void OnAttached(IHealth hero) { }
            public void Tick(IHealth hero, float dt) { }
            public void OnDetached(IHealth hero) { }
        }

        private HeroAuraRunner NewRunner(bool withResist)
        {
            GameObject go = new GameObject("PaladinHero");
            _spawned.Add(go);
            go.AddComponent<Health>();
            HeroAuraRunner runner = go.AddComponent<HeroAuraRunner>();
            if (withResist)
            {
                runner.ConfigureDebuffResist(new DebuffResistSettings(0.6f, 0.6f));
            }
            return runner;
        }

        private static object SlotOf(HeroAuraRunner runner, System.Type auraType)
        {
            FieldInfo field = typeof(HeroAuraRunner).GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "HeroAuraRunner._slots 존재 - 시그니처 변경 감지");
            foreach (object slot in (IList)field.GetValue(runner))
            {
                object aura = slot.GetType().GetField("Aura").GetValue(slot);
                if (aura.GetType() == auraType)
                    return slot;
            }
            return null;
        }

        private static float RemainOf(HeroAuraRunner runner, System.Type auraType)
        {
            object slot = SlotOf(runner, auraType);
            Assert.IsNotNull(slot, $"{auraType.Name} 슬롯");
            return (float)slot.GetType().GetField("Remain").GetValue(slot);
        }

        [Test]
        public void 대상_출처_시한_디버프는_부착_지속이_x0_6으로_줄어든다()
        {
            HeroAuraRunner runner = NewRunner(true);
            FakeMover mover = new FakeMover();
            FakeAttacker attacker = new FakeAttacker();

            runner.Attach(new SlowAura(mover, 0.8f, true), 1.5f);
            runner.Attach(new AxisStubAura(), 5f);
            runner.Attach(new FearAura(null), 3f);
            runner.Attach(new BleedAura(0.02f), 10f);
            runner.Attach(new WeakenAura(attacker, 0.5f), 10f);

            Assert.AreEqual(0.9f, RemainOf(runner, typeof(SlowAura)), 0.0001f, "역병귀 1.5 -> 0.9");
            Assert.AreEqual(3f, RemainOf(runner, typeof(AxisStubAura)), 0.0001f, "독 5 -> 3");
            Assert.AreEqual(1.8f, RemainOf(runner, typeof(FearAura)), 0.0001f, "공포 3 -> 1.8");
            Assert.AreEqual(6f, RemainOf(runner, typeof(BleedAura)), 0.0001f, "출혈 10 -> 6");
            Assert.AreEqual(6f, RemainOf(runner, typeof(WeakenAura)), 0.0001f, "약화 10 -> 6");
        }

        [Test]
        public void 같은_타입_재부착_연장분도_x0_6이다()
        {
            HeroAuraRunner runner = NewRunner(true);
            FakeMover mover = new FakeMover();

            runner.Attach(new SlowAura(mover, 0.8f, true), 1.5f);
            runner.Attach(new SlowAura(mover, 0.8f, true), 1.5f);

            Assert.AreEqual(1.8f, RemainOf(runner, typeof(SlowAura)), 0.0001f, "0.9 + 0.9");
        }

        [Test]
        public void 영구_디버프는_감소분이_x0_6이다_공격력_감소_0_75는_0_85_시너지_0_85는_0_91()
        {
            HeroAuraRunner cardRunner = NewRunner(true);
            FakeAttacker cardAttacker = new FakeAttacker();
            cardRunner.Attach(new HeroAttackDownAura(cardAttacker, 0.75f), -1f);

            HeroAuraRunner synergyRunner = NewRunner(true);
            FakeAttacker synergyAttacker = new FakeAttacker();
            synergyRunner.Attach(new HeroAttackDownAura(synergyAttacker, 0.85f), -1f);

            Assert.AreEqual(0.85f, cardAttacker.PowerScale, 0.0001f);
            Assert.AreEqual(0.91f, synergyAttacker.PowerScale, 0.0001f);
        }

        [Test]
        public void 영구_출혈_비율은_0_01에서_0_006으로_줄어든다()
        {
            HeroAuraRunner runner = NewRunner(true);
            runner.Attach(new EternalBleedAura(new FakeMover(), 0.01f), -1f);

            object slot = SlotOf(runner, typeof(EternalBleedAura));
            object aura = slot.GetType().GetField("Aura").GetValue(slot);
            float ratio = (float)typeof(EternalBleedAura).GetField("_ratio", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aura);

            Assert.AreEqual(0.006f, ratio, 0.000001f);
        }

        [Test]
        public void 제외_출처_Slow_카드_TimeStop_MarkOfDeath는_지속이_그대로다()
        {
            HeroAuraRunner runner = NewRunner(true);
            FakeMover mover = new FakeMover();

            runner.Attach(new SlowAura(mover, 0.6f), 10f);
            runner.Attach(new TimeStopAura(new FakeMover(), new FakeAttacker()), 5f);
            runner.Attach(new MarkOfDeathAura(1.5f), 5f);

            Assert.AreEqual(10f, RemainOf(runner, typeof(SlowAura)), 0.0001f, "Slow 카드(비-Debuff 축)");
            Assert.AreEqual(5f, RemainOf(runner, typeof(TimeStopAura)), 0.0001f);
            Assert.AreEqual(5f, RemainOf(runner, typeof(MarkOfDeathAura)), 0.0001f);
        }

        [Test]
        public void 저항이_설정되지_않은_영웅은_모든_출처가_불변이다()
        {
            HeroAuraRunner runner = NewRunner(false);
            FakeAttacker attacker = new FakeAttacker();

            runner.Attach(new BleedAura(0.02f), 10f);
            runner.Attach(new HeroAttackDownAura(attacker, 0.75f), -1f);

            Assert.AreEqual(10f, RemainOf(runner, typeof(BleedAura)), 0.0001f);
            Assert.AreEqual(0.75f, attacker.PowerScale, 0.0001f);
        }

        [Test]
        public void ClearDebuffResist는_풀_재사용_시_저항을_끈다()
        {
            HeroAuraRunner runner = NewRunner(true);
            runner.ClearDebuffResist();

            runner.Attach(new BleedAura(0.02f), 10f);

            Assert.AreEqual(10f, RemainOf(runner, typeof(BleedAura)), 0.0001f);
        }

        [Test]
        public void 가해_몬스터가_있는_Debuff_축_부착만_가해자_통지를_발행한다()
        {
            HeroAuraRunner runner = NewRunner(true);
            FakeMover mover = new FakeMover();
            FakeHealth mobHealth = NewMob(out Transform mobT);
            int notices = 0;
            Transform notifiedSource = null;
            runner.OnDebuffedBy += (source, health) =>
            {
                ++notices;
                notifiedSource = source;
            };

            runner.Attach(new SlowAura(mover, 0.8f, true), 1.5f, mobT, mobHealth);
            Assert.AreEqual(1, notices);
            Assert.AreSame(mobT, notifiedSource);

            runner.Attach(new BleedAura(0.02f), 10f);
            Assert.AreEqual(1, notices, "카드 출처(가해자 없음)는 응징을 발동하지 않는다");

            runner.Attach(new SlowAura(new FakeMover(), 0.6f), 10f, mobT, mobHealth);
            Assert.AreEqual(1, notices, "Debuff 축이 아닌 Slow 카드는 가해자가 있어도 통지하지 않는다");
        }

        [Test]
        public void 정화_대상은_시한_슬롯만_세고_Slow_카드_슬롯도_1종으로_센다()
        {
            HeroAuraRunner runner = NewRunner(false);
            FakeAttacker attacker = new FakeAttacker();
            Assert.AreEqual(0, runner.CountTimedDebuffs());

            runner.Attach(new SlowAura(new FakeMover(), 0.6f), 10f);
            Assert.AreEqual(1, runner.CountTimedDebuffs(), "Swarm Slow 카드만으로 생긴 슬롯 포함");

            runner.Attach(new BleedAura(0.02f), 10f);
            runner.Attach(new HeroAttackDownAura(attacker, 0.75f), -1f);
            runner.Attach(new TimeStopAura(new FakeMover(), new FakeAttacker()), 5f);

            Assert.AreEqual(2, runner.CountTimedDebuffs(), "영구(HeroAttackDown)·TimeStop 은 대상 아님");
        }

        [Test]
        public void 정화는_시한_슬롯만_전부_해제하고_영구_디버프는_유지하며_원복한다()
        {
            HeroAuraRunner runner = NewRunner(false);
            FakeMover mover = new FakeMover { Speed = 5f };
            FakeAttacker attacker = new FakeAttacker();
            runner.Attach(new SlowAura(mover, 0.6f), 10f);
            runner.Attach(new WeakenAura(attacker, 0.5f), 10f);
            runner.Attach(new HeroAttackDownAura(new FakeAttacker(), 0.75f), -1f);
            int hidden = 0;
            runner.OnStatusHidden += _ => ++hidden;

            int cleared = runner.ClearTimedDebuffs();

            Assert.AreEqual(2, cleared);
            Assert.AreEqual(0, runner.CountTimedDebuffs());
            Assert.AreEqual(5f, mover.Speed, 0.0001f, "둔화 원복");
            Assert.AreEqual(1f, attacker.PowerScale, 0.0001f, "약화 원복");
            Assert.AreEqual(2, hidden, "상태 아이콘 숨김");
            Assert.IsNotNull(SlotOf(runner, typeof(HeroAttackDownAura)), "영구 디버프는 해제하지 않는다");
            Assert.AreEqual(0, runner.ClearTimedDebuffs(), "두 번째 호출은 해제할 것이 없다");
        }

        //# ===== 스킬 3종 =====

        //# IPaladinSkillContext 호출 기록 더블 - 모든 멤버 구현(부분 더블 금지).
        private class RecordingPaladinContext : IPaladinSkillContext
        {
            public Vector3 HeroPosition { get; set; } = new Vector3(1f, 0f, 2f);
            public Transform HeroTransform { get; set; }
            public bool IsHeroActive { get; set; } = true;

            public bool HasTarget = true;
            public Vector3 TargetPosition = new Vector3(4f, 0f, 0f);
            public int Nearby;
            public int TimedDebuffs;

            public readonly List<float> TargetRanges = new List<float>();
            public readonly List<float> CountRadii = new List<float>();
            public readonly List<float[]> Hammers = new List<float[]>();
            public readonly List<Vector3> HammerCenters = new List<Vector3>();
            public int ClearCalls;
            public readonly List<float> Heals = new List<float>();
            public readonly List<float> TakenMuls = new List<float>();
            public readonly List<float[]> Zones = new List<float[]>();
            public readonly List<Vector3> ZoneCenters = new List<Vector3>();
            public int ZoneClears;

            public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { return 0; }
            public Vector3 MonsterCentroid(float radius) { return Vector3.zero; }

            public int CountMonstersInRadius(float radius)
            {
                CountRadii.Add(radius);
                return Nearby;
            }

            public bool TryGetHeroTargetPosition(float maxRange, out Vector3 position)
            {
                TargetRanges.Add(maxRange);
                position = TargetPosition;
                return HasTarget;
            }

            public int DamageAndStunMonstersInSphere(Vector3 center, float radius, int amount, float stunSec)
            {
                HammerCenters.Add(center);
                Hammers.Add(new[] { radius, amount, stunSec });
                return 1;
            }

            public int CountTimedDebuffs() { return TimedDebuffs; }

            public int ClearTimedDebuffs()
            {
                ++ClearCalls;
                int cleared = TimedDebuffs;
                TimedDebuffs = 0;
                return cleared;
            }

            public void HealHeroPercent(float percent) { Heals.Add(percent); }
            public void MultiplyHeroDamageTaken(float mul) { TakenMuls.Add(mul); }

            public void ApplyMonsterSlowZone(Vector3 center, float radius, float speedMul, float rangeMul, float minRange)
            {
                ZoneCenters.Add(center);
                Zones.Add(new[] { radius, speedMul, rangeMul, minRange });
            }

            public void ClearMonsterSlowZone() { ++ZoneClears; }
        }

        [Test]
        public void 스킬_기본_수치는_기획서_값이고_우선순위는_정화_성역_망치_순이다()
        {
            JudgmentHammerSkillData hammer = NewAsset<JudgmentHammerSkillData>();
            PurifyingLightSkillData purify = NewAsset<PurifyingLightSkillData>();
            SanctuarySkillData sanctuary = NewAsset<SanctuarySkillData>();

            Assert.AreEqual(110, hammer.Damage);
            Assert.AreEqual(6f, hammer.Cooldown, 0.0001f);
            Assert.AreEqual(2.4f, hammer.Radius, 0.0001f);
            Assert.AreEqual(0.6f, hammer.StunSec, 0.0001f);
            Assert.AreEqual(5f, hammer.CastRange, 0.0001f);
            Assert.AreEqual(2, purify.TriggerDebuffCount);
            Assert.AreEqual(0.08f, purify.HealPercent, 0.0001f);
            Assert.AreEqual(12f, purify.Cooldown, 0.0001f);
            Assert.AreEqual(6f, sanctuary.DurationSec, 0.0001f);
            Assert.AreEqual(0.6f, sanctuary.DamageTakenMul, 0.0001f);
            Assert.AreEqual(2.6f, sanctuary.Radius, 0.0001f);
            Assert.AreEqual(0.7f, sanctuary.MonsterSpeedMul, 0.0001f);
            Assert.AreEqual(18f, sanctuary.Cooldown, 0.0001f);
            Assert.AreEqual(3, sanctuary.TriggerMonsterCount);
            Assert.AreEqual(2.6f, sanctuary.TriggerRadius, 0.0001f);
            Assert.Greater(purify.CastPriority, sanctuary.CastPriority);
            Assert.Greater(sanctuary.CastPriority, hammer.CastPriority);
        }

        [Test]
        public void 심판의_망치는_쿨다운_6초_뒤_사거리_5_0_표적_위치에_반경_2_4_피해_110_기절_0_6으로_시전한다()
        {
            IHeroSkillRuntime runtime = NewAsset<JudgmentHammerSkillData>().CreateRuntime();
            RecordingPaladinContext ctx = new RecordingPaladinContext { TargetPosition = new Vector3(4f, 0f, 1f) };

            runtime.Tick(ctx, 5.9f);
            Assert.AreEqual(0, ctx.Hammers.Count, "쿨다운 6초 미경과");

            runtime.Tick(ctx, 0.2f);
            Assert.AreEqual(1, ctx.Hammers.Count);
            Assert.AreEqual(5f, ctx.TargetRanges[ctx.TargetRanges.Count - 1], 0.0001f, "표적 사거리 조회는 castRange");
            Assert.AreEqual(new Vector3(4f, 0f, 1f), ctx.HammerCenters[0]);
            Assert.AreEqual(2.4f, ctx.Hammers[0][0], 0.0001f);
            Assert.AreEqual(110f, ctx.Hammers[0][1]);
            Assert.AreEqual(0.6f, ctx.Hammers[0][2], 0.0001f);

            runtime.Tick(ctx, 5.9f);
            Assert.AreEqual(1, ctx.Hammers.Count, "시전 후 쿨다운 6초");
            runtime.Tick(ctx, 0.2f);
            Assert.AreEqual(2, ctx.Hammers.Count);
        }

        [Test]
        public void 심판의_망치는_사거리_안_표적이_없으면_시전하지_않고_쿨다운을_소모하지_않는다()
        {
            IHeroSkillRuntime runtime = NewAsset<JudgmentHammerSkillData>().CreateRuntime();
            RecordingPaladinContext ctx = new RecordingPaladinContext { HasTarget = false };

            runtime.Tick(ctx, 7f);
            Assert.AreEqual(0, ctx.Hammers.Count);

            ctx.HasTarget = true;
            runtime.Tick(ctx, 0.25f);
            Assert.AreEqual(1, ctx.Hammers.Count, "표적이 생기면 곧바로 시전");
        }

        [Test]
        public void 정화의_빛은_시한_디버프가_2종_이상일_때만_전부_해제하고_최대_HP_8퍼센트를_회복한다()
        {
            IHeroSkillRuntime runtime = NewAsset<PurifyingLightSkillData>().CreateRuntime();
            RecordingPaladinContext ctx = new RecordingPaladinContext { TimedDebuffs = 1 };

            runtime.Tick(ctx, 12.1f);
            Assert.AreEqual(0, ctx.ClearCalls, "1종이면 미시전");
            Assert.AreEqual(0, ctx.Heals.Count);

            ctx.TimedDebuffs = 2;
            runtime.Tick(ctx, 0.25f);
            Assert.AreEqual(1, ctx.ClearCalls);
            Assert.AreEqual(1, ctx.Heals.Count);
            Assert.AreEqual(0.08f, ctx.Heals[0], 0.0001f);
        }

        [Test]
        public void 정화의_빛은_시전_후_쿨다운_12초_동안_조건이_맞아도_다시_시전하지_않는다()
        {
            IHeroSkillRuntime runtime = NewAsset<PurifyingLightSkillData>().CreateRuntime();
            RecordingPaladinContext ctx = new RecordingPaladinContext { TimedDebuffs = 2 };
            runtime.Tick(ctx, 12.1f);
            ctx.TimedDebuffs = 3;

            runtime.Tick(ctx, 11.7f);
            Assert.AreEqual(1, ctx.ClearCalls);

            runtime.Tick(ctx, 0.5f);
            Assert.AreEqual(2, ctx.ClearCalls);
        }

        [Test]
        public void 성역은_반경_2_6_안_3마리_이상일_때만_받는_피해_x0_6과_둔화_구역을_펼치고_6초_뒤_역수로_복구한다()
        {
            IHeroSkillRuntime runtime = NewAsset<SanctuarySkillData>().CreateRuntime();
            RecordingPaladinContext ctx = new RecordingPaladinContext { Nearby = 2, HeroPosition = new Vector3(3f, 0f, 3f) };

            runtime.Tick(ctx, 18.1f);
            Assert.AreEqual(0, ctx.TakenMuls.Count, "2마리는 미시전");
            Assert.AreEqual(2.6f, ctx.CountRadii[0], 0.0001f);

            ctx.Nearby = 3;
            runtime.Tick(ctx, 0.25f);
            Assert.AreEqual(1, ctx.TakenMuls.Count);
            Assert.AreEqual(0.6f, ctx.TakenMuls[0], 0.0001f);
            Assert.AreEqual(new Vector3(3f, 0f, 3f), ctx.ZoneCenters[0], "시전 위치에 고정");
            Assert.AreEqual(2.6f, ctx.Zones[0][0], 0.0001f);
            Assert.AreEqual(0.7f, ctx.Zones[0][1], 0.0001f);
            Assert.AreEqual(1f, ctx.Zones[0][2], 0.0001f, "사거리 배율은 건드리지 않는다");

            ctx.HeroPosition = new Vector3(9f, 0f, 9f);
            runtime.Tick(ctx, 5.7f);
            Assert.AreEqual(0, ctx.ZoneClears, "6초 미경과");
            Assert.AreEqual(1, ctx.TakenMuls.Count);
            Assert.AreEqual(new Vector3(3f, 0f, 3f), ctx.ZoneCenters[ctx.ZoneCenters.Count - 1], "영웅이 움직여도 구역은 고정");

            runtime.Tick(ctx, 0.4f);
            Assert.AreEqual(1, ctx.ZoneClears);
            Assert.AreEqual(2, ctx.TakenMuls.Count);
            Assert.AreEqual(1f / 0.6f, ctx.TakenMuls[1], 0.0001f);
            Assert.AreEqual(1f, ctx.TakenMuls[0] * ctx.TakenMuls[1], 0.0001f);
        }

        [Test]
        public void 성역은_지속_중_재시전하지_않고_종료_후에도_쿨다운_18초가_남아_있다()
        {
            IHeroSkillRuntime runtime = NewAsset<SanctuarySkillData>().CreateRuntime();
            RecordingPaladinContext ctx = new RecordingPaladinContext { Nearby = 5 };
            runtime.Tick(ctx, 18.1f);

            runtime.Tick(ctx, 3f);
            Assert.AreEqual(1, ctx.TakenMuls.Count, "지속 중 재시전 없음");
            runtime.Tick(ctx, 3.1f);
            runtime.Tick(ctx, 0.5f);
            Assert.AreEqual(2, ctx.TakenMuls.Count, "종료 복구만 있고 재시전은 쿨다운 대기");
        }

        [Test]
        public void 성역은_지속_도중_비활성화되어도_배율을_한번만_원복한다()
        {
            IHeroSkillRuntime runtime = NewAsset<SanctuarySkillData>().CreateRuntime();
            RecordingPaladinContext ctx = new RecordingPaladinContext { Nearby = 3 };
            runtime.Tick(ctx, 18.1f);

            runtime.OnDeactivate();
            runtime.OnDeactivate();

            Assert.AreEqual(2, ctx.TakenMuls.Count, "시전 1 + 원복 1");
            Assert.AreEqual(1, ctx.ZoneClears);
        }

        [Test]
        public void 영웅이_행동_불가면_세_스킬_모두_시전하지_않는다()
        {
            RecordingPaladinContext ctx = new RecordingPaladinContext { IsHeroActive = false, Nearby = 10, TimedDebuffs = 5 };
            IHeroSkillRuntime hammer = NewAsset<JudgmentHammerSkillData>().CreateRuntime();
            IHeroSkillRuntime purify = NewAsset<PurifyingLightSkillData>().CreateRuntime();
            IHeroSkillRuntime sanctuary = NewAsset<SanctuarySkillData>().CreateRuntime();

            hammer.Tick(ctx, 30f);
            purify.Tick(ctx, 30f);
            sanctuary.Tick(ctx, 30f);

            Assert.AreEqual(0, ctx.Hammers.Count);
            Assert.AreEqual(0, ctx.ClearCalls);
            Assert.AreEqual(0, ctx.TakenMuls.Count);
        }

        [Test]
        public void 성기사_컨텍스트가_아니면_세_스킬_모두_시전하지_않고_오류를_한번만_남긴다()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "[JudgmentHammerRuntime] IPaladinSkillContext 미구현 컨텍스트 - 성기사 스킬 비활성");
            IHeroSkillRuntime hammer = NewAsset<JudgmentHammerSkillData>().CreateRuntime();
            IHeroSkillContext other = new NonPaladinContext();

            hammer.Tick(other, 10f);
            hammer.Tick(other, 10f);
        }

        private class NonPaladinContext : IHeroSkillContext
        {
            public Vector3 HeroPosition => Vector3.zero;
            public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { return 0; }
            public Vector3 MonsterCentroid(float radius) { return Vector3.zero; }
        }
    }
}
