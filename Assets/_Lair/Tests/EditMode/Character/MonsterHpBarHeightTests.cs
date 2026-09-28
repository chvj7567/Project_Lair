using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# HP바 높이가 부모(몬스터 루트) 스케일을 상쇄해 목표 월드 높이를 유지한다(monster-2d-conversion.md §6.3.7).
    //# 시드: gameplay-programmer. "정상 + 엣지(부모 스케일 0)" 만 — 종족별 표 전체 검증은 test-engineer.
    public class MonsterHpBarHeightTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void 루트_스케일이_반영된_로컬높이로_변환된다()
        {
            _root = new GameObject("MonsterRoot");
            _root.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            GameObject wrapper = new GameObject("HpBarWrapper");
            wrapper.transform.SetParent(_root.transform, false);
            MonsterHpBar bar = wrapper.AddComponent<MonsterHpBar>();

            bar.SetHeightAboveRoot(0.747f);

            Assert.AreEqual(0.747f / 0.6f, wrapper.transform.localPosition.y, 0.0001f);
        }

        //# 엣지 — 부모 스케일이 0(비정상 상태)이면 0 나눗셈 대신 1로 안전 처리.
        [Test]
        public void 부모스케일이_0이면_0나눗셈_없이_안전하게_처리된다()
        {
            _root = new GameObject("MonsterRoot");
            _root.transform.localScale = Vector3.zero;
            GameObject wrapper = new GameObject("HpBarWrapper");
            wrapper.transform.SetParent(_root.transform, false);
            MonsterHpBar bar = wrapper.AddComponent<MonsterHpBar>();

            Assert.DoesNotThrow(() => bar.SetHeightAboveRoot(0.5f));
            Assert.IsFalse(float.IsInfinity(wrapper.transform.localPosition.y));
            Assert.IsFalse(float.IsNaN(wrapper.transform.localPosition.y));
        }
    }
}
