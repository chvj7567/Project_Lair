using Lair.Card;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 특성 슬라이스 D 자체 테스트 (정상 + 엣지) — 본격 스위트는 test-engineer (monster-growth-currencies §11.7).
    public class MonsterTraitTests
    {
        [Test]
        public void 잔상_회피는_횟수만큼_피해를_무효화하고_이후엔_정상_피해다()
        {
            GameObject go = new GameObject("phantom");
            Health hp = go.AddComponent<Health>();
            hp.SetMax(30);
            int dodged = 0;
            hp.OnDodged += () => ++dodged;
            hp.GrantDodges(1);

            hp.TakeDamage(999);
            hp.TakeDamage(5);

            Assert.AreEqual(1, dodged, "즉사급 피해도 1회 회피 (E-18)");
            Assert.AreEqual(25, hp.Current);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void 저주_누적은_상한까지만_쌓이고_해제하면_받는_피해_배율이_원복된다()
        {
            GameObject go = new GameObject("hero");
            Health hp = go.AddComponent<Health>();
            CurseStackAura aura = new CurseStackAura(0.03f, 2);
            aura.OnAttached(hp);

            aura.Merge(new CurseStackAura(0.03f, 2));
            aura.Merge(new CurseStackAura(0.03f, 2));

            Assert.AreEqual(2, aura.Stacks, "최대 2중첩");
            Assert.AreEqual(1.06f, hp.DamageTakenScale, 0.0001f);
            aura.OnDetached(hp);
            Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f);
            Object.DestroyImmediate(go);
        }
    }
}
