using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    //# 역병귀 독 통합 (monster-growth-currencies §15.1) — 실제 Awake/OnEnable · 러너 Update 프레임 진행으로 부착 → 중첩 → 1초 틱 → 3초 만료를 본다.
    public class PlagueToxinBattleIntegrationTest
    {
        private readonly List<Object> _spawned = new List<Object>();
        private Health _heroHp;
        private HeroAuraRunner _runner;

        [SetUp]
        public void SetUp()
        {
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();
            Time.timeScale = 1f;
            GameObject hero = new GameObject("Hero");
            _spawned.Add(hero);
            hero.AddComponent<LairCharacter>();
            _heroHp = hero.AddComponent<Health>();
            _heroHp.SetMax(10000);
            _runner = hero.AddComponent<HeroAuraRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
            {
                if (o != null)
                {
                    Object.Destroy(o);
                }
            }
            _spawned.Clear();
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();
            Time.timeScale = 1f;
        }

        //# 비활성으로 조립 후 활성 — Awake 가 모든 컴포넌트가 붙은 뒤 돈다.
        private PlagueToxinOnHit SpawnPlague(int power)
        {
            GameObject go = new GameObject("Plague");
            go.SetActive(false);
            _spawned.Add(go);
            go.AddComponent<LairCharacter>();
            go.AddComponent<Health>().SetMax(100);
            MeleeAttacker atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(2f, 1f, power);
            go.AddComponent<MonsterTag>().Configure(EMonster.Plague);
            PlagueToxinOnHit toxin = go.AddComponent<PlagueToxinOnHit>();
            go.SetActive(true);
            return toxin;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, target.GetType().Name + "." + field);
            fi.SetValue(target, value);
        }

        private float TimedRemain()
        {
            FieldInfo field = typeof(HeroAuraRunner).GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (object slot in (IList)field.GetValue(_runner))
            {
                if (slot.GetType().GetField("Aura").GetValue(slot) is PlagueToxinAura)
                    return (float)slot.GetType().GetField("Remain").GetValue(slot);
            }
            return -1f;
        }

        private static PlagueToxinAura FindToxin(HeroAuraRunner runner)
        {
            FieldInfo field = typeof(HeroAuraRunner).GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (object slot in (IList)field.GetValue(runner))
            {
                if (slot.GetType().GetField("Aura").GetValue(slot) is PlagueToxinAura toxin)
                    return toxin;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator 역병귀가_영웅을_4번_때리면_독이_3중첩되고_초당_틱_피해_뒤_3초에_전부_사라진다()
        {
            PlagueToxinOnHit plague = SpawnPlague(4);
            yield return null;
            plague.Configure(1f, 1f, 0);
            plague.BindHero(_heroHp, _runner);
            MeleeAttacker atk = plague.GetComponent<MeleeAttacker>();

            for (int i = 0; i < 4; ++i)
            {
                Assert.IsTrue(atk.TryAttack(_heroHp, Vector3.zero, Vector3.zero, 1000f + i * 5f));
            }
            PlagueToxinAura toxin = FindToxin(_runner);
            Assert.IsNotNull(toxin, "적중 경로(OnHit)로 독 부착");
            Assert.AreEqual(3, toxin.Stacks);
            int afterHits = _heroHp.Current;

            yield return new WaitForSeconds(1.2f);
            int tickLoss = afterHits - _heroHp.Current;
            float remainMid = TimedRemain();
            yield return new WaitForSeconds(2.2f);
            PlagueToxinAura expired = FindToxin(_runner);
            int afterExpiry = _heroHp.Current;
            yield return new WaitForSeconds(1.1f);

            Assert.AreEqual(12, tickLoss, "1초 경과 = 1틱 x (3중첩 x 공격력 4)");
            Assert.Greater(remainMid, 0f);
            Assert.Less(remainMid, 2f, "갱신은 3초 고정 - 누적 연장 아님");
            Assert.IsNull(expired, "3초 뒤 전 중첩 소멸");
            Assert.AreEqual(afterExpiry, _heroHp.Current, "만료 뒤엔 더 이상 독 피해 없음");
            Assert.AreEqual(0, _runner.CountTimedDebuffs());
        }

        [UnityTest]
        public IEnumerator 전투_스탯_적용을_거친_역병귀는_각성_독_지속과_받는_피해를_한_번만_반영해_독을_건다()
        {
            BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
            _spawned.Add(balance);
            SetPrivate(balance, "_monsters", new[]
            {
                new BalanceConfig.MonsterStatRow
                {
                    Key = EMonster.Plague,
                    Stat = new BalanceConfig.CharacterStat { Hp = 40, Power = 5, Range = 2f, Cooldown = 1f, MoveSpeed = 3f },
                },
            });
            GameObject bcGo = new GameObject("BattleControllerUT");
            bcGo.SetActive(false);
            _spawned.Add(bcGo);
            BattleController bc = bcGo.AddComponent<BattleController>();
            SetPrivate(bc, "_balance", balance);
            SetPrivate(bc, "_heroHealth", _heroHp);
            SetPrivate(bc, "_heroAuraRunner", _runner);
            bc.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.PoisonDuration, 1.6105f);
            bc.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.DamageTaken, 0.8f);
            PlagueToxinOnHit plague = SpawnPlague(1);
            yield return null;

            for (int i = 0; i < 3; ++i)
            {
                bc.ApplyMonsterStats(plague.gameObject, EMonster.Plague, resetCurrent: true);
            }
            Assert.IsTrue(plague.GetComponent<MeleeAttacker>().TryAttack(_heroHp, Vector3.zero, Vector3.zero, 1000f));

            Assert.AreEqual(0.8f, plague.GetComponent<Health>().GrowthDamageTakenMul, 0.0001f, "3회 재적용 - 복리 없음");
            Assert.AreEqual(3f * 1.6105f, plague.Duration, 0.0001f);
            Assert.IsNotNull(FindToxin(_runner), "ApplyMonsterStats 가 영웅을 주입");
            Assert.AreEqual(3f * 1.6105f, TimedRemain(), 0.05f);
            Assert.AreEqual(5f, FindToxin(_runner).PerStackDamage, 0.0001f, "raw 공격력 5");
        }
    }
}
