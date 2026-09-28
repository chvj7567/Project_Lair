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

        //# ───────── 이하 test-engineer 보강분 — 조부모 스케일 체인 + 재호출 갱신 + 종족별 설계값 회귀 ─────────

        //# 부모의 lossyScale 은 조부모 스케일까지 누적된 값 — localScale 만 보면 안 된다.
        [Test]
        public void 조부모_스케일까지_반영된_lossyScale_기준으로_계산된다()
        {
            GameObject grandparent = new GameObject("Grandparent");
            grandparent.transform.localScale = new Vector3(2f, 2f, 2f);
            _root = new GameObject("MonsterRoot");
            _root.transform.SetParent(grandparent.transform, false);
            _root.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);   //# lossyScale.y = 2 × 1.5 = 3
            GameObject wrapper = new GameObject("HpBarWrapper");
            wrapper.transform.SetParent(_root.transform, false);
            MonsterHpBar bar = wrapper.AddComponent<MonsterHpBar>();

            bar.SetHeightAboveRoot(0.9f);

            Assert.AreEqual(0.9f / 3f, wrapper.transform.localPosition.y, 0.0001f,
                "조부모 스케일까지 포함된 lossyScale 을 상쇄한 로컬 높이");

            Object.DestroyImmediate(grandparent);
        }

        //# 강화 레벨업으로 티어가 바뀌어 재호출되면 이전 값이 아니라 최신 목표 높이로 갱신된다.
        [Test]
        public void 여러번_호출해도_최신_값으로_갱신된다()
        {
            _root = new GameObject("MonsterRoot");
            _root.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            GameObject wrapper = new GameObject("HpBarWrapper");
            wrapper.transform.SetParent(_root.transform, false);
            MonsterHpBar bar = wrapper.AddComponent<MonsterHpBar>();

            bar.SetHeightAboveRoot(0.747f);
            bar.SetHeightAboveRoot(0.934f);   //# 티어 상승 재호출

            Assert.AreEqual(0.934f / 0.9f, wrapper.transform.localPosition.y, 0.0001f,
                "재호출 시 이전 값이 아니라 최신 목표 높이로 갱신된다");
        }

        //# 회귀 고정 — 기획서 §3.3/§6.3.7 종족별 루트 스케일 × 티어0 HP바 높이 표 값.
        [TestCase(0.6f, 0.747f)]    //# Wisp
        [TestCase(1.3f, 1.434f)]    //# Wraith
        [TestCase(0.4f, 0.538f)]    //# Phantom
        public void 종족별_설계값_회귀_고정_티어0(float rootScale, float tier0Height)
        {
            _root = new GameObject("MonsterRoot");
            _root.transform.localScale = new Vector3(rootScale, rootScale, rootScale);
            GameObject wrapper = new GameObject("HpBarWrapper");
            wrapper.transform.SetParent(_root.transform, false);
            MonsterHpBar bar = wrapper.AddComponent<MonsterHpBar>();

            bar.SetHeightAboveRoot(tier0Height);

            Assert.AreEqual(tier0Height / rootScale, wrapper.transform.localPosition.y, 0.0001f,
                "기획서 §6.3.7 종족별 티어0 HP바 목표 높이 고정");
        }
    }
}
