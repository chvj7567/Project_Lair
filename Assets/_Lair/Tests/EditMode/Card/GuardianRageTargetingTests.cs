using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — GuardianRage 적용 종 한정 회귀 (기획서 §10.1 디자인 단정).
    //# EffectsRenewal2026Tests 는 (Wisp·Reaper) 2종만 확인. 본 스위트는 6종 전체 망라 + Wraith 까지 검증.
    //# 적용 종 {Wisp, Wraith} 만 HP×2.0 + DamageTaken×0.5, 그 외(Reaper·Hex·Plague·Phantom) 모두 무영향.
    public class GuardianRageTargetingTests
    {
        [TearDown]
        public void CleanRegistry()
        {
            for (int i = CharacterRegistry.Monsters.Count - 1; i >= 0; --i)
            {
                CharacterRegistry.Entry e = CharacterRegistry.Monsters[i];
                if (e?.Transform != null) Object.DestroyImmediate(e.Transform.gameObject);
            }
            CharacterRegistry.Monsters.Clear();
        }

        //# Wisp / Wraith — 적용 종 한정에 포함되어 HP×2.0 + DamageTaken×0.5.
        [Test]
        public void GuardianRage_Wisp_Wraith_적용_종_HP_2배_받는데미지_0점5()
        {
            Health hpWisp = MakeMonster(EMonster.Wisp);
            Health hpWraith = MakeMonster(EMonster.Wraith);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);

            Assert.AreEqual(2f, hpWisp.HpMaxScale, 0.0001f, "Wisp HpMaxScale = 2.0");
            Assert.AreEqual(0.5f, hpWisp.DamageTakenScale, 0.0001f, "Wisp DamageTakenScale = 0.5");
            Assert.AreEqual(2f, hpWraith.HpMaxScale, 0.0001f, "Wraith HpMaxScale = 2.0");
            Assert.AreEqual(0.5f, hpWraith.DamageTakenScale, 0.0001f, "Wraith DamageTakenScale = 0.5");
        }

        //# Reaper — 적용 종 외 (Dps 축) → 변화 없음.
        [Test]
        public void GuardianRage_Reaper_적용외_HpMax_DamageTaken_변화없음()
        {
            Health hp = MakeMonster(EMonster.Reaper);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);

            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "Reaper HpMaxScale 유지");
            Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f, "Reaper DamageTakenScale 유지");
        }

        //# Hex — 적용 종 외 (Dps 축) → 변화 없음.
        [Test]
        public void GuardianRage_Hex_적용외_HpMax_DamageTaken_변화없음()
        {
            Health hp = MakeMonster(EMonster.Hex);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);

            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "Hex HpMaxScale 유지");
            Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f, "Hex DamageTakenScale 유지");
        }

        //# Plague — 적용 종 외 (Debuff 축) → 변화 없음.
        [Test]
        public void GuardianRage_Plague_적용외_HpMax_DamageTaken_변화없음()
        {
            Health hp = MakeMonster(EMonster.Plague);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);

            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "Plague HpMaxScale 유지");
            Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f, "Plague DamageTakenScale 유지");
        }

        //# Phantom — 적용 종 외 (Swarm 축) → 변화 없음.
        [Test]
        public void GuardianRage_Phantom_적용외_HpMax_DamageTaken_변화없음()
        {
            Health hp = MakeMonster(EMonster.Phantom);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);

            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "Phantom HpMaxScale 유지");
            Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f, "Phantom DamageTakenScale 유지");
        }

        //# 혼합 — Wisp + Reaper + Phantom 동시 등록 → Wisp 만 적용.
        [Test]
        public void GuardianRage_혼합_종_Wisp만_적용_Reaper_Phantom_불변()
        {
            Health hpWisp = MakeMonster(EMonster.Wisp);
            Health hpReaper = MakeMonster(EMonster.Reaper);
            Health hpPhantom = MakeMonster(EMonster.Phantom);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);

            Assert.AreEqual(2f, hpWisp.HpMaxScale, 0.0001f, "Wisp 적용 종 → HpMaxScale 2");
            Assert.AreEqual(1f, hpReaper.HpMaxScale, 0.0001f, "Reaper 적용외 → HpMaxScale 1");
            Assert.AreEqual(1f, hpPhantom.HpMaxScale, 0.0001f, "Phantom 적용외 → HpMaxScale 1");
        }

        //# ===== Helper =====

        private static Health MakeMonster(EMonster type)
        {
            GameObject go = new GameObject($"monster_{type}");
            go.AddComponent<MonsterTag>().Configure(type);
            Health hp = go.AddComponent<Health>();
            hp.SetMax(100, resetCurrent: true);
            CharacterRegistry.RegisterMonster(go.transform, hp);
            return hp;
        }
    }
}
