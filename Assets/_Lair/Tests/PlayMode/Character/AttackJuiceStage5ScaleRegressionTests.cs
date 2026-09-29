using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Character
{
    //# hero-2d-conversion §6.5·§9·§11-7 잠재 버그 회귀 고정 — AttackJuice._baseScale 은 Awake 1회 캐시(풀 최초
    //# 생성 시점의 스케일, 보통 1,1,1)라서 HeroStageVariantApplier.ApplyScale(스폰 뒤 스테이지5 ×1.4) 이후에도
    //# 공격 펀치 연출(PunchCo)이 끝나면 transform.localScale 을 캐시값(1,1,1)으로 되돌린다.
    //# 이 회귀는 3D 구현에도 이미 존재하는 사전 버그이며(§6.5), 본 2D 전환이 원인이 아니다.
    //# test-engineer 범위 — production 수정은 하지 않는다. 아래 테스트는 "기대 동작"을 기술하므로
    //# 버그가 남아있는 한 FAIL 이 정상이다(현재 실패 확인 필요 — production 수정은 gameplay-programmer 담당).
    public class AttackJuiceStage5ScaleRegressionTests
    {
        private GameObject _hero;
        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            if (_hero != null) Object.DestroyImmediate(_hero);
            if (_target != null) Object.DestroyImmediate(_target);
        }

        [UnityTest]
        public IEnumerator 스테이지5_확대_이후_공격_펀치_연출이_끝나도_스케일_1_4가_유지된다()
        {
            _hero = new GameObject("HeroStage5ScaleUT");
            MeleeAttacker attacker = _hero.AddComponent<MeleeAttacker>();
            attacker.Configure(5f, 1f, 10);
            _hero.AddComponent<AttackJuice>();
            //# Awake/OnEnable 완료 대기 — 이 시점 AttackJuice._baseScale 은 풀 최초 생성 스케일(1,1,1)로 캐시된다.
            yield return null;

            //# 스폰 후 HeroStageVariantApplier.ApplyScale(스테이지5, ScaleMultiplier=1.4) 재현.
            //# 실제 흐름도 Apply() 는 Awake/OnEnable 이후(스폰 시점)에 호출된다(§6.5).
            _hero.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

            _target = new GameObject("TargetUT");
            Health targetHealth = _target.AddComponent<Health>();
            targetHealth.SetMax(100);

            bool began = attacker.TryBeginAttack(targetHealth, _hero.transform.position, _target.transform.position, Time.time);
            Assert.IsTrue(began, "사거리 내 대상 — 공격 개시 성공(테스트 전제 성립 확인)");
            bool struck = attacker.TryApplyStrike(Time.time);
            Assert.IsTrue(struck, "재검사 통과 — strike 적중(테스트 전제 성립 확인) → AttackJuice.HandleHit 구동");

            //# AttackJuice.PunchCo 종료 대기 — _punchDuration(기본 0.12s) 보다 충분히 길게.
            yield return new WaitForSeconds(0.3f);

            Vector3 scale = _hero.transform.localScale;
            Assert.AreEqual(1.4f, scale.x, 0.01f,
                "기대: 스테이지5 확대(1.4)가 펀치 연출 후에도 유지된다. " +
                "실제(버그, §6.5): AttackJuice._baseScale 이 Awake 캐시(1,1,1)라 PunchCo 종료 시 1.0으로 되돌린다 — " +
                "이 FAIL 은 기존 버그의 회귀 고정이며 production 수정은 gameplay-programmer 담당(test-engineer 범위 밖).");
        }
    }
}
