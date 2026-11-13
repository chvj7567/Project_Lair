using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lair.Tests.EditMode
{
    //# 전투 쪽 육성 효과 본격 스위트 (monster-growth-currencies §3.2~§3.4 · §15) — 역병귀 독, 끈질긴 저주, 특성 6종, 각성·둥지 배율.
    //# EditMode 에선 Awake/OnEnable 이 돌지 않아 필요한 컴포넌트만 리플렉션으로 부른다. 시간 흐름(만료)은 PlayMode 통합 테스트 소관.
    public class GrowthBattleEffectSuiteTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private List<CharacterRegistry.Entry> _prevMonsters;
        private List<CharacterRegistry.Entry> _prevHeroes;
        private MonsterTraitService _traits;

        private Health _heroHp;
        private HeroAuraRunner _runner;
        private MeleeAttacker _heroAtk;

        [SetUp]
        public void 영웅과_정적_레지스트리를_준비한다()
        {
            _prevMonsters = new List<CharacterRegistry.Entry>(CharacterRegistry.Monsters);
            _prevHeroes = new List<CharacterRegistry.Entry>(CharacterRegistry.Heroes);
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();

            GameObject hero = Make("Hero", Vector3.zero);
            hero.AddComponent<LairCharacter>();
            _heroHp = hero.AddComponent<Health>();
            _heroHp.SetMax(10000);
            _runner = hero.AddComponent<HeroAuraRunner>();
            Invoke(_runner, "Awake");
            _heroAtk = hero.AddComponent<MeleeAttacker>();
            _heroAtk.Configure(5f, 1f, 40);
        }

        [TearDown]
        public void 생성물과_정적_레지스트리를_원복한다()
        {
            _traits?.Dispose();
            _traits = null;
            foreach (GameObject go in _objects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _objects.Clear();
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Monsters.AddRange(_prevMonsters);
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Heroes.AddRange(_prevHeroes);
        }

        //# ===== 헬퍼 =====

        private GameObject Make(string name, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            _objects.Add(go);
            return go;
        }

        private static void Invoke(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, target.GetType().Name + "." + method);
            mi.Invoke(target, null);
        }

        private GameObject Monster(EMonster species, Vector3 position, int hp = 1000, int power = 1, bool register = true)
        {
            GameObject go = Make(species.ToString(), position);
            go.AddComponent<LairCharacter>();
            Health h = go.AddComponent<Health>();
            h.SetMax(hp);
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(10f, 1f, power);
            go.AddComponent<MonsterTag>().Configure(species);
            if (register)
            {
                CharacterRegistry.RegisterMonster(go.transform, h, atk);
                CharacterRegistry.SetMonsterEngaging(go.transform, true);
            }
            return go;
        }

        //# 역병귀 = 몬스터 + PlagueToxinOnHit (Awake·OnEnable 을 직접 불러 공격 구독 · 영웅 주입까지).
        private PlagueToxinOnHit Plague(Vector3 position, int power, float damageMul = 1f, float durationMul = 1f, int maxStackBonus = 0)
        {
            GameObject go = Monster(EMonster.Plague, position, 1000, power);
            PlagueToxinOnHit toxin = go.AddComponent<PlagueToxinOnHit>();
            Invoke(toxin, "Awake");
            Invoke(toxin, "OnEnable");
            toxin.Configure(damageMul, durationMul, maxStackBonus);
            toxin.BindHero(_heroHp, _runner);
            return toxin;
        }

        private MonsterTraitService Traits(params (EMonster, int)[] tiers)
        {
            Dictionary<EMonster, int> map = new Dictionary<EMonster, int>();
            foreach ((EMonster m, int t) in tiers)
            {
                map[m] = t;
            }
            _traits = new MonsterTraitService(GrowthTestKit.Shipped(), map, _heroHp, _runner, _heroAtk);
            return _traits;
        }

        private static float _clock = 100f;

        private static void Hit(MeleeAttacker attacker, Health target)
        {
            _clock += 10f;
            Assert.IsTrue(attacker.TryAttack(target, Vector3.zero, Vector3.zero, _clock), "적중해야 함");
        }

        private T AuraOf<T>(out float remain, out bool indefinite) where T : class, IHeroAura
        {
            FieldInfo field = typeof(HeroAuraRunner).GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (object slot in (IList)field.GetValue(_runner))
            {
                if (slot.GetType().GetField("Aura").GetValue(slot) is T aura)
                {
                    remain = (float)slot.GetType().GetField("Remain").GetValue(slot);
                    indefinite = (bool)slot.GetType().GetField("Indefinite").GetValue(slot);
                    return aura;
                }
            }
            remain = 0f;
            indefinite = false;
            return null;
        }

        private T AuraOf<T>(out float remain) where T : class, IHeroAura => AuraOf<T>(out remain, out bool _);

        //# 실전 프레임 순서 재현 — 버프 오버레이 리셋(MonsterBuffService) → 특성 배율 곱(MonsterTraitService).
        private static void Frames(MonsterBuffService buffs, MonsterTraitService traits, float seconds, float dt = 0.02f)
        {
            int frames = Mathf.RoundToInt(seconds / dt);
            for (int i = 0; i < frames; ++i)
            {
                buffs.Tick(dt);
                traits.Tick(dt);
            }
        }

        //# ===== 역병귀 독 (§15.1) =====

        [Test]
        public void 역병귀_적중_4번은_독_3중첩_지속_3초이고_1초_틱은_공격력x중첩이다()
        {
            PlagueToxinOnHit plague = Plague(Vector3.zero, 4);
            MeleeAttacker atk = plague.GetComponent<MeleeAttacker>();
            List<int> counts = new List<int>();
            _runner.OnStatusCountChanged += (key, n) => counts.Add(n);

            for (int i = 0; i < 4; ++i)
            {
                Hit(atk, _heroHp);
            }
            PlagueToxinAura toxin = AuraOf<PlagueToxinAura>(out float remain);
            int before = _heroHp.Current;
            toxin.Tick(_heroHp, 1f);

            Assert.AreEqual(3, toxin.Stacks);
            Assert.AreEqual(3f, remain, 0.0001f, "재적중은 3초 갱신 - 3+3+3+3 누적 아님");
            Assert.AreEqual(12, before - _heroHp.Current, "3중첩 x 공격력 4");
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 3 }, counts, "상태 아이콘 xN 갱신");
        }

        [Test]
        public void 독_1중첩_피해는_역병귀의_최종_공격력과_독_피해_배율에_비례한다()
        {
            PlagueToxinOnHit plague = Plague(Vector3.zero, 4, damageMul: 1.4f);
            MeleeAttacker atk = plague.GetComponent<MeleeAttacker>();
            atk.PowerScale = 1.5f;

            for (int i = 0; i < 3; ++i)
            {
                Hit(atk, _heroHp);
            }
            PlagueToxinAura toxin = AuraOf<PlagueToxinAura>(out float _);

            Assert.AreEqual(4f * 1.5f * 1.4f, toxin.PerStackDamage, 0.0001f);
            Assert.AreEqual(25, toxin.DamagePerTick(), "Round(3 x 8.4 = 25.2)");
        }

        [Test]
        public void 약한_역병귀가_다시_때려도_1중첩_피해는_큰_값을_유지한다()
        {
            PlagueToxinAura toxin = new PlagueToxinAura(5f, 1, 3);

            toxin.Merge(new PlagueToxinAura(2f, 1, 3));

            Assert.AreEqual(5f, toxin.PerStackDamage, 0.0001f);
            Assert.AreEqual(2, toxin.Stacks);
        }

        [Test]
        public void 독_틱_피해는_최소_1이다()
        {
            Assert.AreEqual(1, new PlagueToxinAura(0.2f, 1, 3).DamagePerTick());
        }

        [Test]
        public void Debuff_Tier1_이면_독이_5중첩까지_쌓이고_이미_3중첩인_독에도_소급된다()
        {
            PlagueToxinOnHit plague = Plague(Vector3.zero, 2);
            MeleeAttacker atk = plague.GetComponent<MeleeAttacker>();
            for (int i = 0; i < 3; ++i)
            {
                Hit(atk, _heroHp);
            }

            plague.Configure(1f, 1f, DebuffSynergyTier1.MaxStackBonus);
            for (int i = 0; i < 4; ++i)
            {
                Hit(atk, _heroHp);
            }

            Assert.AreEqual(5, plague.MaxStack);
            Assert.AreEqual(5, AuraOf<PlagueToxinAura>(out float _).Stacks);
        }

        [Test]
        public void 독_설정을_여러_번_다시_대입해도_배율이_복리로_쌓이지_않는다()
        {
            PlagueToxinOnHit plague = Plague(Vector3.zero, 2);

            for (int i = 0; i < 5; ++i)
            {
                plague.Configure(1.2763f, 1.6105f, 2);
            }

            Assert.AreEqual(3f * 1.6105f, plague.Duration, 0.0001f, "독 지속 3초 → 각성 Lv5 4.83초");
            Assert.AreEqual(5, plague.MaxStack);
            Assert.AreEqual(2f * 1.2763f, plague.PerStackDamage, 0.0001f);
        }

        [Test]
        public void 역병귀가_영웅이_아닌_대상을_때리면_독이_붙지_않는다()
        {
            PlagueToxinOnHit plague = Plague(Vector3.zero, 2);
            GameObject other = Monster(EMonster.Wisp, Vector3.zero);

            Hit(plague.GetComponent<MeleeAttacker>(), other.GetComponent<Health>());

            Assert.IsNull(AuraOf<PlagueToxinAura>(out float _));
        }

        [Test]
        public void 성기사_신앙의_가호는_독_지속을_0점6배로_줄이고_정화의_빛이_독을_지운_뒤_다시_1중첩부터_쌓인다()
        {
            _runner.ConfigureDebuffResist(new DebuffResistSettings(0.6f, 0.6f));
            PlagueToxinOnHit plague = Plague(Vector3.zero, 2);
            MeleeAttacker atk = plague.GetComponent<MeleeAttacker>();
            for (int i = 0; i < 3; ++i)
            {
                Hit(atk, _heroHp);
            }
            AuraOf<PlagueToxinAura>(out float resisted);

            int cleared = _runner.ClearTimedDebuffs();
            Hit(atk, _heroHp);

            Assert.AreEqual(1.8f, resisted, 0.0001f, "3 x 0.6 - 중첩 수는 보정하지 않는다");
            Assert.AreEqual(1, cleared);
            Assert.AreEqual(1, AuraOf<PlagueToxinAura>(out float _).Stacks);
        }

        //# ===== 끈질긴 저주 (§15.3) =====

        [Test]
        public void 끈질긴_저주_3픽은_독_공포_출혈_무력화_지속을_1점75배로_늘린다()
        {
            for (int i = 0; i < 3; ++i)
            {
                _runner.AddDebuffDurationBonus(0.25f);
            }

            _runner.Attach(new PlagueToxinAura(2f, 1, 3), 3f);
            _runner.Attach(new FearAura(null), 3f);
            _runner.Attach(new BleedAura(), 10f);
            _runner.Attach(new WeakenAura(null), 10f);

            Assert.AreEqual(1.75f, _runner.DebuffDurationMul, 0.0001f);
            AuraOf<PlagueToxinAura>(out float toxin);
            AuraOf<FearAura>(out float fear);
            AuraOf<BleedAura>(out float bleed);
            AuraOf<WeakenAura>(out float weaken);
            Assert.AreEqual(5.25f, toxin, 0.0001f);
            Assert.AreEqual(5.25f, fear, 0.0001f);
            Assert.AreEqual(17.5f, bleed, 0.0001f);
            Assert.AreEqual(17.5f, weaken, 0.0001f);
        }

        [Test]
        public void 끈질긴_저주는_영구_디버프와_Swarm_둔화에는_적용되지_않는다()
        {
            _runner.AddDebuffDurationBonus(0.75f);

            _runner.Attach(new HeroAttackDownAura(null), -1f);
            _runner.Attach(new SlowAura(null, 0.6f, false), 5f);

            AuraOf<HeroAttackDownAura>(out float _, out bool indefinite);
            AuraOf<SlowAura>(out float slow);
            Assert.IsTrue(indefinite, "영구는 영구 그대로");
            Assert.AreEqual(5f, slow, 0.0001f, "Debuff 축 아님");
        }

        [Test]
        public void 끈질긴_저주와_신앙의_가호는_곱으로_합성되고_0_이하_보너스는_무시된다()
        {
            _runner.ConfigureDebuffResist(new DebuffResistSettings(0.6f, 0.6f));
            _runner.AddDebuffDurationBonus(0.75f);
            _runner.AddDebuffDurationBonus(0f);
            _runner.AddDebuffDurationBonus(-1f);

            _runner.Attach(new BleedAura(), 10f);

            AuraOf<BleedAura>(out float bleed);
            Assert.AreEqual(10f * 1.75f * 0.6f, bleed, 0.0001f, "성기사 상대 3픽 = x1.05");
        }

        [Test]
        public void 영웅_러너가_풀로_돌아가면_끈질긴_저주_보너스가_초기화된다()
        {
            _runner.AddDebuffDurationBonus(0.5f);

            Invoke(_runner, "OnDisable");

            Assert.AreEqual(1f, _runner.DebuffDurationMul, 0.0001f);
        }

        [Test]
        public void 출하_끈질긴_저주_카드_효과는_픽당_0점25다()
        {
            CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Lair/Art/Cards/Items/LingeringCurse.asset");
            Assert.IsNotNull(card);
            Assert.IsInstanceOf<LingeringCurseEffect>(card.Effect);
            FieldInfo f = typeof(LingeringCurseEffect).GetField("_durationBonusPerPick", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.AreEqual(0.25f, (float)f.GetValue(card.Effect), 0.0001f);
            Assert.AreEqual(EBuildAxis.Debuff, card.Axis);
        }

        //# ===== 특성: 가시 껍질 (Wisp) =====

        [Test]
        public void 가시_껍질은_도깨비불이_실제로_잃은_HP_기준으로_반사한다()
        {
            Traits((EMonster.Wisp, 3));
            GameObject wisp = Monster(EMonster.Wisp, Vector3.zero, 100);
            wisp.GetComponent<Health>().ConfigureGrowthTaken(0.5f, 1f);
            _traits.OnMonsterConfigured(wisp, EMonster.Wisp, true);

            Hit(_heroAtk, wisp.GetComponent<Health>());

            Assert.AreEqual(80, wisp.GetComponent<Health>().Current, "40 x 각성 0.5 = 20");
            Assert.AreEqual(10000 - 5, _heroHp.Current, "실제 감소 20 x 0.25 = 5 (40 기준이면 10)");
        }

        [Test]
        public void 가시_껍질은_오버킬이면_남은_HP_만큼만_반사한다()
        {
            Traits((EMonster.Wisp, 3));
            GameObject wisp = Monster(EMonster.Wisp, Vector3.zero, 8);
            _traits.OnMonsterConfigured(wisp, EMonster.Wisp, true);

            Hit(_heroAtk, wisp.GetComponent<Health>());

            Assert.AreEqual(10000 - 2, _heroHp.Current, "실제 감소 8 x 0.25 = 2");
        }

        [Test]
        public void 가시_껍질_반사량의_0점5는_올림한다()
        {
            Traits((EMonster.Wisp, 3));
            GameObject wisp = Monster(EMonster.Wisp, Vector3.zero, 10);
            _traits.OnMonsterConfigured(wisp, EMonster.Wisp, true);

            Hit(_heroAtk, wisp.GetComponent<Health>());

            Assert.AreEqual(10000 - 3, _heroHp.Current, "실제 감소 10 x 0.25 = 2.5 → 3 (독 틱과 같은 AwayFromZero)");
        }

        [Test]
        public void 가시_껍질_반사는_최소_1이다()
        {
            Traits((EMonster.Wisp, 1));
            _heroAtk.Configure(5f, 1f, 4);
            GameObject wisp = Monster(EMonster.Wisp, Vector3.zero, 100);
            _traits.OnMonsterConfigured(wisp, EMonster.Wisp, true);

            Hit(_heroAtk, wisp.GetComponent<Health>());

            Assert.AreEqual(10000 - 1, _heroHp.Current, "Max(1, Round(4 x 0.10))");
        }

        [Test]
        public void 가시_껍질은_영웅_스킬_피해와_다른_종족_피격은_반사하지_않는다()
        {
            Traits((EMonster.Wisp, 3), (EMonster.Wraith, 3));
            GameObject wisp = Monster(EMonster.Wisp, new Vector3(2f, 0f, 0f), 1000);
            GameObject wraith = Monster(EMonster.Wraith, Vector3.zero, 1000);
            _traits.OnMonsterConfigured(wisp, EMonster.Wisp, true);
            _traits.OnMonsterConfigured(wraith, EMonster.Wraith, true);

            new HeroSkillContext(_heroHp.transform).DamageMonstersInRing(0f, 3f, 100, 0f);
            Hit(_heroAtk, wraith.GetComponent<Health>());

            Assert.Less(wisp.GetComponent<Health>().Current, 1000, "스킬은 맞았다");
            Assert.AreEqual(10000, _heroHp.Current);
        }

        [Test]
        public void 특성_단계가_0이면_가시_껍질이_동작하지_않는다()
        {
            Traits((EMonster.Wisp, 0));
            GameObject wisp = Monster(EMonster.Wisp, Vector3.zero, 100);
            _traits.OnMonsterConfigured(wisp, EMonster.Wisp, true);

            Hit(_heroAtk, wisp.GetComponent<Health>());

            Assert.IsFalse(_traits.HasAny);
            Assert.AreEqual(10000, _heroHp.Current);
        }

        //# ===== 특성: 원한 (Wraith) =====

        [Test]
        public void 원한은_쓰러진_망령_반경_3_안의_몬스터만_5초간_공격력을_올리고_매_프레임_복리로_쌓이지_않는다()
        {
            Traits((EMonster.Wraith, 2));
            MonsterBuffService buffs = new MonsterBuffService();
            GameObject wraith = Monster(EMonster.Wraith, Vector3.zero, 10);
            GameObject near = Monster(EMonster.Reaper, new Vector3(2f, 0f, 0f));
            GameObject far = Monster(EMonster.Reaper, new Vector3(5f, 0f, 0f));
            _traits.OnMonsterConfigured(wraith, EMonster.Wraith, true);

            wraith.GetComponent<Health>().TakeDamage(999);
            Frames(buffs, _traits, 1f);
            float nearDuring = near.GetComponent<MeleeAttacker>().PowerScale;
            float farDuring = far.GetComponent<MeleeAttacker>().PowerScale;
            Frames(buffs, _traits, 4.5f);

            Assert.AreEqual(1.14f, nearDuring, 0.0001f, "2단계 x1.14 - 50프레임 지나도 1.14");
            Assert.AreEqual(1f, farDuring, 0.0001f);
            Assert.AreEqual(1f, near.GetComponent<MeleeAttacker>().PowerScale, 0.0001f, "5초 뒤 해제");
        }

        [Test]
        public void 원한끼리는_중첩되지_않고_지속만_갱신된다()
        {
            Traits((EMonster.Wraith, 3));
            MonsterBuffService buffs = new MonsterBuffService();
            GameObject a = Monster(EMonster.Wraith, Vector3.zero, 10);
            GameObject b = Monster(EMonster.Wraith, new Vector3(0.5f, 0f, 0f), 10);
            GameObject ally = Monster(EMonster.Hex, new Vector3(1f, 0f, 0f));
            _traits.OnMonsterConfigured(a, EMonster.Wraith, true);
            _traits.OnMonsterConfigured(b, EMonster.Wraith, true);

            a.GetComponent<Health>().TakeDamage(999);
            Frames(buffs, _traits, 3f);
            b.GetComponent<Health>().TakeDamage(999);
            Frames(buffs, _traits, 3f);

            Assert.AreEqual(1.2f, ally.GetComponent<MeleeAttacker>().PowerScale, 0.0001f, "x1.2 x1.2 아님, 두 번째 사망으로 5초 갱신돼 6초째에도 유지");
        }

        //# ===== 특성: 처형 (Reaper) =====

        [TestCase(70, 1.15f)]
        [TestCase(69, 1f)]
        public void 처형은_영웅_HP_30퍼센트_이하일_때만_사신_피해를_올린다(int damage, float expected)
        {
            Traits((EMonster.Reaper, 1));
            MonsterBuffService buffs = new MonsterBuffService();
            _heroHp.SetMax(100);
            GameObject reaper = Monster(EMonster.Reaper, Vector3.zero);
            _traits.OnMonsterConfigured(reaper, EMonster.Reaper, true);

            _heroHp.TakeDamage(damage);
            Frames(buffs, _traits, 0.5f);

            Assert.AreEqual(expected, reaper.GetComponent<MeleeAttacker>().PowerScale, 0.0001f, "영웅 HP " + _heroHp.Current + "%");
        }

        [Test]
        public void 처형_3단계는_x1점40이고_다른_종족에는_붙지_않는다()
        {
            Traits((EMonster.Reaper, 3));
            MonsterBuffService buffs = new MonsterBuffService();
            _heroHp.SetMax(100);
            GameObject reaper = Monster(EMonster.Reaper, Vector3.zero);
            GameObject hex = Monster(EMonster.Hex, Vector3.zero);
            _traits.OnMonsterConfigured(reaper, EMonster.Reaper, true);

            _heroHp.TakeDamage(90);
            Frames(buffs, _traits, 0.5f);

            Assert.AreEqual(1.4f, reaper.GetComponent<MeleeAttacker>().PowerScale, 0.0001f);
            Assert.AreEqual(1f, hex.GetComponent<MeleeAttacker>().PowerScale, 0.0001f);
        }

        //# ===== 특성: 저주 누적 (Hex) =====

        private void HexHits(int count)
        {
            GameObject hex = Monster(EMonster.Hex, Vector3.zero);
            _traits.OnMonsterConfigured(hex, EMonster.Hex, true);
            MeleeAttacker atk = hex.GetComponent<MeleeAttacker>();
            for (int i = 0; i < count; ++i)
            {
                Hit(atk, _heroHp);
            }
        }

        [Test]
        public void 저주_누적_3단계는_적중마다_3퍼센트씩_최대_5중첩까지_영웅_받는_피해를_올린다()
        {
            Traits((EMonster.Hex, 3));

            HexHits(7);

            CurseStackAura curse = AuraOf<CurseStackAura>(out float remain);
            Assert.AreEqual(5, curse.Stacks);
            Assert.AreEqual(1.15f, _heroHp.DamageTakenScale, 0.0001f);
            Assert.AreEqual(4f, remain, 0.0001f, "마지막 적중 후 4초 유지");
        }

        [Test]
        public void 성기사_상대_저주_누적은_가산분에_0점6이_곱해진다()
        {
            _runner.ConfigureDebuffResist(new DebuffResistSettings(0.6f, 0.6f));
            Traits((EMonster.Hex, 3));

            HexHits(5);

            Assert.AreEqual(1f + 0.03f * 0.6f * 5f, _heroHp.DamageTakenScale, 0.0001f);
        }

        [Test]
        public void 저주_누적은_끈질긴_저주로_늘지_않고_정화의_빛으로_지워지지_않는다()
        {
            _runner.AddDebuffDurationBonus(0.75f);
            Traits((EMonster.Hex, 1));

            HexHits(2);
            int cleared = _runner.ClearTimedDebuffs();

            AuraOf<CurseStackAura>(out float remain);
            Assert.AreEqual(4f, remain, 0.0001f);
            Assert.AreEqual(0, cleared, "정화 목록 = 둔화·독·공포·출혈·무력화 (§15.5)");
            Assert.AreEqual(1.02f, _heroHp.DamageTakenScale, 0.0001f);
        }

        [Test]
        public void 저주_누적은_영웅의_다른_받는_피해_배율과_곱으로_합성되고_해제되면_원래대로다()
        {
            _heroHp.DamageTakenScale = 0.6f;
            Traits((EMonster.Hex, 3));

            HexHits(5);
            float during = _heroHp.DamageTakenScale;
            Invoke(_runner, "OnDisable");

            Assert.AreEqual(0.6f * 1.15f, during, 0.0001f, "수호 결계 x0.6 등과 곱");
            Assert.AreEqual(0.6f, _heroHp.DamageTakenScale, 0.0001f);
        }

        //# ===== 특성: 전염 (Plague) =====

        [Test]
        public void 전염_2단계는_반경_4_안의_영웅에게_독_2중첩을_새로_건다()
        {
            Traits((EMonster.Plague, 2));
            PlagueToxinOnHit plague = Plague(new Vector3(3f, 0f, 0f), 4);
            _traits.OnMonsterConfigured(plague.gameObject, EMonster.Plague, true);

            plague.GetComponent<Health>().TakeDamage(99999);

            PlagueToxinAura toxin = AuraOf<PlagueToxinAura>(out float remain);
            Assert.IsNotNull(toxin);
            Assert.AreEqual(2, toxin.Stacks);
            Assert.AreEqual(4f, toxin.PerStackDamage, 0.0001f, "그 역병귀의 독 피해");
            Assert.AreEqual(3f, remain, 0.0001f);
        }

        [Test]
        public void 전염은_기존_독에_중첩을_더하되_상한_3을_넘지_않고_Tier1_이면_5까지다()
        {
            Traits((EMonster.Plague, 3));
            PlagueToxinOnHit first = Plague(Vector3.zero, 2);
            MeleeAttacker atk = first.GetComponent<MeleeAttacker>();
            Hit(atk, _heroHp);
            Hit(atk, _heroHp);
            PlagueToxinOnHit dying = Plague(Vector3.zero, 2);
            _traits.OnMonsterConfigured(dying.gameObject, EMonster.Plague, true);

            dying.GetComponent<Health>().TakeDamage(99999);
            int capped = AuraOf<PlagueToxinAura>(out float _).Stacks;

            _runner.ClearTimedDebuffs();
            Hit(atk, _heroHp);
            Hit(atk, _heroHp);
            PlagueToxinOnHit tier1 = Plague(Vector3.zero, 2, maxStackBonus: 2);
            _traits.OnMonsterConfigured(tier1.gameObject, EMonster.Plague, true);
            tier1.GetComponent<Health>().TakeDamage(99999);

            Assert.AreEqual(3, capped, "2 + 3 → 상한 3 (E-14)");
            Assert.AreEqual(5, AuraOf<PlagueToxinAura>(out float _).Stacks, "2 + 3 = 5");
        }

        [Test]
        public void 전염은_반경_밖의_영웅에게는_효과가_없다()
        {
            Traits((EMonster.Plague, 3));
            PlagueToxinOnHit plague = Plague(new Vector3(4.5f, 0f, 0f), 4);
            _traits.OnMonsterConfigured(plague.gameObject, EMonster.Plague, true);

            plague.GetComponent<Health>().TakeDamage(99999);

            Assert.IsNull(AuraOf<PlagueToxinAura>(out float _));
        }

        //# ===== 특성: 잔상 (Phantom) =====

        private Health Phantom(out SimpleMover mover, bool spawned = true)
        {
            GameObject go = Monster(EMonster.Phantom, Vector3.zero, 30);
            mover = go.AddComponent<SimpleMover>();
            _traits.OnMonsterConfigured(go, EMonster.Phantom, spawned);
            return go.GetComponent<Health>();
        }

        [Test]
        public void 잔상_3단계는_스폰_후_피격_2회를_무효화하고_MISS_통지를_낸다()
        {
            Traits((EMonster.Phantom, 3));
            Health hp = Phantom(out SimpleMover _);
            int dodged = 0;
            hp.OnDodged += () => ++dodged;

            hp.TakeDamage(999);
            hp.TakeDamage(999);
            hp.TakeDamage(5);

            Assert.AreEqual(2, dodged);
            Assert.AreEqual(25, hp.Current);
        }

        [Test]
        public void 잔상_회피_직후_0점5초간_이동속도가_오르고_매_프레임_복리로_쌓이지_않는다()
        {
            Traits((EMonster.Phantom, 2));
            MonsterBuffService buffs = new MonsterBuffService();
            Health hp = Phantom(out SimpleMover mover);

            hp.TakeDamage(10);
            Frames(buffs, _traits, 0.3f);
            float during = mover.SpeedScale;
            Frames(buffs, _traits, 0.4f);

            Assert.AreEqual(1.5f, during, 0.0001f, "2단계 x1.5");
            Assert.AreEqual(1f, mover.SpeedScale, 0.0001f);
        }

        //# IHitFeedbackSpawner 스파이 — 세 멤버 모두 구현(부분 더블 금지).
        private class SpySpawner : IHitFeedbackSpawner
        {
            public readonly List<string> Texts = new List<string>();
            public int Popups;
            public int Impacts;

            public void SpawnImpact(Vector3 pos, Color color, HitVictimKind victim) => ++Impacts;
            public void SpawnPopup(Vector3 pos, int amount, Color color) => ++Popups;
            public void SpawnTextPopup(Vector3 pos, string text, Color color) => Texts.Add(text);
        }

        [Test]
        public void 잔상_회피는_피해_숫자_대신_MISS_를_한_번_띄우고_피격_연출은_없다()
        {
            Traits((EMonster.Phantom, 1));
            Health hp = Phantom(out SimpleMover _);
            DamageFeedback feedback = hp.gameObject.AddComponent<DamageFeedback>();
            Invoke(feedback, "Awake");
            Invoke(feedback, "OnEnable");
            SpySpawner spy = new SpySpawner();
            feedback.SetSpawnerForTest(spy);

            feedback.StampDamageColor(Color.white);
            hp.TakeDamage(999);

            CollectionAssert.AreEqual(new[] { "MISS" }, spy.Texts);
            Assert.AreEqual(0, spy.Popups, "피해 숫자 없음");
            Assert.AreEqual(0, spy.Impacts);
            Assert.AreEqual(30, hp.Current);
        }

        [Test]
        public void 소급_재적용은_잔상_회피를_다시_채우지_않고_풀_재사용은_회피를_비운다()
        {
            Traits((EMonster.Phantom, 1));
            Health hp = Phantom(out SimpleMover _);
            hp.TakeDamage(5);
            _traits.OnMonsterConfigured(hp.gameObject, EMonster.Phantom, false);
            hp.TakeDamage(5);
            int afterRetro = hp.Current;

            hp.GrantDodges(1);
            Invoke(hp, "OnEnable");
            hp.TakeDamage(5);

            Assert.AreEqual(25, afterRetro, "첫 피격만 회피, 소급(spawned=false)은 충전 없음");
            Assert.AreEqual(20, hp.Current, "OnEnable 이 회피 충전을 비운다");
        }

        //# ===== 각성 배율 (§3.2) =====

        [Test]
        public void 출하_데이터_각성_Lv5_는_종족별_역할_스탯에만_기획서_배율을_준다()
        {
            MetaConfig cfg = WorldMapTestSupport.ShippedMeta();
            MetaProfile p = new MetaProfile();
            foreach (EMonster m in MonsterGrades.BaseMonsters)
            {
                p.SetShopLevel("Awaken_" + m, 5);
            }

            MetaBattleBonus bonus = MetaBattleBonus.From(p, cfg, GrowthTestKit.Shipped());

            Assert.AreEqual(0.8154f, bonus.GetAwakenMul(EMonster.Wisp, EMonsterStatKind.DamageTaken), 0.0001f);
            Assert.AreEqual(0.4437f, bonus.GetAwakenMul(EMonster.Wraith, EMonsterStatKind.KnockbackTaken), 0.0001f);
            Assert.AreEqual(0.8587f, bonus.GetAwakenMul(EMonster.Wraith, EMonsterStatKind.DamageTaken), 0.0001f);
            Assert.AreEqual(0.7738f, bonus.GetAwakenMul(EMonster.Reaper, EMonsterStatKind.Cooldown), 0.0001f);
            Assert.AreEqual(1.3382f, bonus.GetAwakenMul(EMonster.Hex, EMonsterStatKind.Range), 0.0001f);
            Assert.AreEqual(1.2763f, bonus.GetAwakenMul(EMonster.Plague, EMonsterStatKind.PoisonDamage), 0.0001f);
            Assert.AreEqual(1.6105f, bonus.GetAwakenMul(EMonster.Plague, EMonsterStatKind.PoisonDuration), 0.0001f);
            Assert.AreEqual(1.3382f, bonus.GetAwakenMul(EMonster.Phantom, EMonsterStatKind.MoveSpeed), 0.0001f);
            Assert.AreEqual(1f, bonus.GetAwakenMul(EMonster.Wisp, EMonsterStatKind.Range), 0.0001f, "역할 밖 스탯 무영향");
            Assert.AreEqual(1f, bonus.GetAwakenMul(EMonster.Hex, EMonsterStatKind.DamageTaken), 0.0001f);
            Assert.AreEqual(1f, bonus.GetSpeciesMul(EMonster.Wisp), 0.0001f, "체급 축에 새지 않음");
        }

        [Test]
        public void 세이브_레벨이_MaxLevel_을_넘으면_각성_둥지_특성_모두_만렙으로_자른다()
        {
            MetaConfig cfg = WorldMapTestSupport.ShippedMeta();
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("Awaken_Wisp", 9);
            p.SetShopLevel("Nest_Wisp", 9);
            p.SetShopLevel("Trait_Wisp", 9);

            MetaBattleBonus bonus = MetaBattleBonus.From(p, cfg, GrowthTestKit.Shipped());

            Assert.AreEqual(Mathf.Pow(0.96f, 5), bonus.GetAwakenMul(EMonster.Wisp, EMonsterStatKind.DamageTaken), 0.0001f, "E-2");
            Assert.AreEqual(3, bonus.GetNestLevel(EMonster.Wisp));
            Assert.AreEqual(3, bonus.GetTraitTier(EMonster.Wisp));
        }

        [Test]
        public void 육성_데이터가_없으면_레벨이_있어도_각성_둥지_특성이_적용되지_않는다()
        {
            MetaConfig cfg = WorldMapTestSupport.ShippedMeta();
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("Awaken_Plague", 3);
            p.SetShopLevel("Nest_Plague", 2);
            p.SetShopLevel("Trait_Plague", 2);

            MetaBattleBonus bonus = MetaBattleBonus.From(p, cfg, null);

            Assert.AreEqual(1f, bonus.GetAwakenMul(EMonster.Plague, EMonsterStatKind.PoisonDamage), 0.0001f);
            Assert.AreEqual(0, bonus.GetNestLevel(EMonster.Plague));
            Assert.AreEqual(0, bonus.GetTraitTier(EMonster.Plague));
        }

        [Test]
        public void 몬스터_받는_피해는_버프_오버레이_각성_스폰보호를_모두_곱한다()
        {
            Health hp = Monster(EMonster.Wisp, Vector3.zero, 1000).GetComponent<Health>();
            hp.ConfigureGrowthTaken(0.8f, 1f);
            hp.DamageTakenScale = 0.5f;
            hp.GrantSpawnGuard(0.5f, 3f);

            hp.TakeDamage(100);

            Assert.AreEqual(0.2f, hp.EffectiveDamageTakenMul, 0.0001f);
            Assert.AreEqual(980, hp.Current, "100 x 0.5 x 0.8 x 0.5 = 20");
        }

        [Test]
        public void 각성_배율과_스폰보호는_다시_대입해도_복리로_쌓이지_않고_풀_재사용에서_초기화된다()
        {
            Health hp = Monster(EMonster.Wraith, Vector3.zero, 1000).GetComponent<Health>();

            for (int i = 0; i < 3; ++i)
            {
                hp.ConfigureGrowthTaken(0.8587f, 0.4437f);
                hp.GrantSpawnGuard(0.5f, 3f);
            }
            hp.GrantSpawnGuard(0.1f, 0f);
            float stacked = hp.EffectiveDamageTakenMul;
            float knock = hp.KnockbackTakenMul;
            Invoke(hp, "OnEnable");

            Assert.AreEqual(0.8587f * 0.5f, stacked, 0.0001f, "0초 보호는 무시");
            Assert.AreEqual(0.4437f, knock, 0.0001f);
            Assert.AreEqual(1f, hp.EffectiveDamageTakenMul, 0.0001f);
            Assert.AreEqual(1f, hp.KnockbackTakenMul, 0.0001f);
        }

        [Test]
        public void 망령_각성_넉백_배율은_영웅_스킬_넉백_거리에_곱해진다()
        {
            GameObject awakened = Monster(EMonster.Wraith, new Vector3(2f, 0f, 0f));
            GameObject plain = Monster(EMonster.Wraith, new Vector3(-2f, 0f, 0f));
            awakened.GetComponent<Health>().ConfigureGrowthTaken(1f, 0.4437f);

            new HeroSkillContext(_heroHp.transform).DamageMonstersInRing(0f, 3f, 1, 3f);

            Assert.AreEqual(2f + 3f * 0.4437f, awakened.transform.position.x, 0.001f);
            Assert.AreEqual(-5f, plain.transform.position.x, 0.001f);
        }

        //# ===== 둥지 숙련 (§3.3) =====

        [Test]
        public void 둥지_숙련은_레벨만큼_효과가_누적되고_만렙_초과는_만렙으로_자른다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();

            NestMasteryEffects lv0 = growth.NestEffectsUpTo(0);
            NestMasteryEffects lv1 = growth.NestEffectsUpTo(1);
            NestMasteryEffects lv2 = growth.NestEffectsUpTo(2);
            NestMasteryEffects over = growth.NestEffectsUpTo(9);

            Assert.AreEqual(0f, lv0.FirstSpawnAdvanceSec, 0.0001f);
            Assert.AreEqual(1f, lv0.PeriodMul, 0.0001f);
            Assert.AreEqual(2f, lv1.FirstSpawnAdvanceSec, 0.0001f);
            Assert.AreEqual(1f, lv1.PeriodMul, 0.0001f);
            Assert.AreEqual(0f, lv1.GuardSec, 0.0001f);
            Assert.AreEqual(0.93f, lv2.PeriodMul, 0.0001f);
            Assert.AreEqual(1f, lv2.GuardDamageTakenMul, 0.0001f);
            Assert.AreEqual(2f, over.FirstSpawnAdvanceSec, 0.0001f);
            Assert.AreEqual(0.93f, over.PeriodMul, 0.0001f, "x0.93^n 이 아님 - Lv2 효과 1회");
            Assert.AreEqual(0.5f, over.GuardDamageTakenMul, 0.0001f);
            Assert.AreEqual(3f, over.GuardSec, 0.0001f);
        }

        [Test]
        public void 첫_스폰_앞당김은_서두르는_둥지와_더해지고_0_아래로_내려가지_않으며_첫_스폰_후엔_무시된다()
        {
            Spawner sp = Make("Spawner", Vector3.zero).AddComponent<Spawner>();
            FieldInfo delay = typeof(Spawner).GetField("_firstSpawnDelay", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo done = typeof(Spawner).GetField("_firstSpawnDone", BindingFlags.NonPublic | BindingFlags.Instance);

            sp.SetFirstSpawnDelay(5f);
            sp.AdvanceFirstSpawnDelay(1.5f);
            sp.AdvanceFirstSpawnDelay(2f);
            float combined = (float)delay.GetValue(sp);
            sp.AdvanceFirstSpawnDelay(2f);
            float floored = (float)delay.GetValue(sp);
            sp.SetFirstSpawnDelay(4f);
            done.SetValue(sp, true);
            sp.AdvanceFirstSpawnDelay(2f);

            Assert.AreEqual(1.5f, combined, 0.0001f, "5 - (유물 1.5 + 숙련 2)");
            Assert.AreEqual(0f, floored, 0.0001f);
            Assert.AreEqual(4f, (float)delay.GetValue(sp), 0.0001f);
        }

        [Test]
        public void 둥지_주기_배율은_상점_주기_배율과_곱연산이다()
        {
            Spawner sp = Make("Spawner", Vector3.zero).AddComponent<Spawner>();
            sp.SetBasePeriod(10f);

            sp.ScalePeriod(0.93f);
            sp.ScalePeriod(0.9f);

            Assert.AreEqual(10f * 0.93f * 0.9f, sp.SpawnPeriod, 0.0001f);
        }
    }
}
