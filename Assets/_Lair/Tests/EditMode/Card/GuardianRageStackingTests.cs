using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — GuardianRage 심화 회귀 (HP×2.0 제거 정합, spec 2026-06-01 §2B / 기획서 §5.3).
    //# 기존 GuardianRageTargetingTests 가 이미 커버: 적용 종 한정({Wisp,Wraith}) + 6종 망라 + HpMaxScale 불변.
    //# 본 스위트의 고유 가치 (중복 금지):
    //#   - ToughHide(영구) + GuardianRage(시한) 동시 적용 시 DamageTakenScale 곱연산 ×0.5×0.75=×0.375 (기획서 §5.3).
    //#   - GuardianRage 만료 후 DamageTakenScale 원복 (영구 ToughHide 있으면 ×0.75 만 남고, 없으면 1f 로 복귀).
    //#   - HpMaxScale 은 곱연산·만료 어느 경우에도 절대 불변.
    public class GuardianRageStackingTests
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

        //# ToughHide(영구) + GuardianRage(시한) 동시 → 서로 다른 EMonsterBuff 라 곱연산. Wisp 받피 ×0.375.
        [Test]
        public void GuardianRage_ToughHide_동시적용_받피_0점375_곱연산_HP불변()
        {
            Health hp = MakeMonster(EMonster.Wisp);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.ToughHide, -1f);     //# 영구
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);  //# 시한
            svc.Tick(0.016f);

            Assert.AreEqual(0.375f, hp.DamageTakenScale, 0.0001f, "×0.5 × ×0.75 = ×0.375 (곱연산)");
            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "HpMaxScale 곱연산에도 불변");
        }

        //# GuardianRage 만 적용 후 15초 경과(만료) → 받피 1f 로 원복. HP 불변.
        [Test]
        public void GuardianRage_만료후_받피_원복_HP불변()
        {
            Health hp = MakeMonster(EMonster.Wisp);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);
            svc.Tick(0.016f);
            Assert.AreEqual(0.5f, hp.DamageTakenScale, 0.0001f, "적용 직후 ×0.5");

            //# 15초 초과 경과 → 버프 만료 제거 → 다음 Tick 에서 scale 리셋만 남음.
            svc.Tick(16f);

            Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f, "만료 후 받피 원복");
            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "HpMaxScale 만료 후에도 불변");
        }

        //# ToughHide(영구) + GuardianRage(시한) 동시 → GuardianRage 만 만료 → ToughHide ×0.75 만 잔존.
        [Test]
        public void GuardianRage_만료후_ToughHide만_잔존_받피_0점75()
        {
            Health hp = MakeMonster(EMonster.Wraith);

            MonsterBuffService svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.ToughHide, -1f);     //# 영구
            svc.AddBuff(EMonsterBuff.GuardianRage, 15f);  //# 시한
            svc.Tick(0.016f);
            Assert.AreEqual(0.375f, hp.DamageTakenScale, 0.0001f, "동시 적용 — ×0.375");

            //# GuardianRage(15s) 만 만료, ToughHide(영구) 잔존.
            svc.Tick(16f);

            Assert.AreEqual(0.75f, hp.DamageTakenScale, 0.0001f, "GuardianRage 만료 후 ToughHide ×0.75 만 잔존");
            Assert.AreEqual(1f, hp.HpMaxScale, 0.0001f, "HpMaxScale 불변");
        }

        //# ===== Helper (GuardianRageTargetingTests 와 동일 패턴) =====

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
