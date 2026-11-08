using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# 쇼케이스 모드 복구(village-monster-showcase §3, 게이트 T-S1) — 시드: gameplay-programmer. 정상(켜면 전부 끄고 끄면 복구) + 엣지(켜기 전에 꺼져 있던 것은 복구 후에도 꺼짐).
    public class MonsterShowcaseTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private MonsterShowcase Build(out Behaviour on, out Behaviour off, out BoxCollider box, out GameObject hidden)
        {
            _root = new GameObject("MonsterRoot");
            on = _root.AddComponent<Light>();
            off = _root.AddComponent<AudioListener>();
            off.enabled = false;
            box = _root.AddComponent<BoxCollider>();
            hidden = new GameObject("HpBarWrapper");
            hidden.transform.SetParent(_root.transform, false);
            MonsterShowcase showcase = _root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new[] { on, off }, new Collider[] { box }, new[] { hidden });
            return showcase;
        }

        [Test]
        public void 쇼케이스를_켜면_전투_요소가_꺼지고_끄면_복구된다()
        {
            MonsterShowcase showcase = Build(out Behaviour on, out Behaviour off, out BoxCollider box, out GameObject hidden);

            showcase.SetShowcaseMode(true);
            Assert.IsFalse(on.enabled);
            Assert.IsFalse(box.enabled);
            Assert.IsFalse(hidden.activeSelf);

            showcase.SetShowcaseMode(false);
            Assert.IsTrue(on.enabled);
            Assert.IsTrue(box.enabled);
            Assert.IsTrue(hidden.activeSelf);
        }

        [Test]
        public void 켜기_전에_꺼져_있던_컴포넌트는_복구_뒤에도_꺼져_있다()
        {
            MonsterShowcase showcase = Build(out Behaviour on, out Behaviour off, out BoxCollider box, out GameObject hidden);

            showcase.SetShowcaseMode(true);
            showcase.SetShowcaseMode(false);

            Assert.IsFalse(off.enabled, "원래 꺼져 있던 컴포넌트를 켜 버리면 전투 풀이 오염된다");
        }
    }
}
