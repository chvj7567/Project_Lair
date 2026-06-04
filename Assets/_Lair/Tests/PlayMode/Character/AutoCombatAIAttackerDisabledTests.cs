using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Character
{
    //# TimeStop 버그 회귀 박제 — IAttacker.Enabled=false 면 AutoCombatAI 가 공격을 개시하지 않아 데미지가 0.
    //# 수정 전: AutoCombatAI.Update 가 _attacker.Enabled 를 보지 않아 사거리 안이면 TryAttack 으로 데미지가 들어감.
    //# 행위 검증 — 타겟 HP 변화로 공격 차단을 관찰(MeleeAttacker.enabled 직접 토글).
    //# 몬스터(DeferStrike=false) 즉시 경로를 사용 — 게이트/애니 리그 없이 HP 데미지가 관찰 가능.
    //# AutoCombatAIHysteresisTests 의 SpawnHeroTarget/SpawnMonster 패턴 재사용.
    public class AutoCombatAIAttackerDisabledTests
    {
        private const float MonsterRange = 1.5f;
        private const float MonsterSpeed = 3f;
        private const float MonsterCooldown = 0.2f;
        private const int MonsterPower = 10;

        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        private GameObject SpawnHeroTarget(Vector3 pos)
        {
            GameObject hero = new GameObject("atkoff_hero");
            hero.transform.position = pos;
            Health hp = hero.AddComponent<Health>();
            hp.SetMax(100000);
            CharacterRegistry.RegisterHero(hero.transform, hp);
            _spawned.Add(hero);
            return hero;
        }

        private GameObject SpawnMonster(Vector3 pos)
        {
            GameObject m = new GameObject("atkoff_monster");
            m.transform.position = pos;

            Rigidbody rb = m.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Health hp = m.AddComponent<Health>();
            hp.SetMax(100000);
            m.AddComponent<SimpleMover>().Speed = MonsterSpeed;
            m.AddComponent<MeleeAttacker>().Configure(MonsterRange, MonsterCooldown, MonsterPower);
            m.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 720f;
            m.AddComponent<MonsterTargetProvider>();
            m.AddComponent<AutoCombatAI>();
            _spawned.Add(m);
            return m;
        }

        private static IEnumerator WaitTicks(int ticks)
        {
            for (int i = 0; i < ticks; ++i)
                yield return new WaitForFixedUpdate();
        }

        //# 정상 케이스 — Enabled=false 면 사거리 안이어도 데미지 0 (TimeStop 의도).
        [UnityTest]
        public IEnumerator Attacker_Disabled_이면_사거리안에서도_공격하지_않는다()
        {
            GameObject hero = SpawnHeroTarget(new Vector3(0.5f, 0f, 0f));
            GameObject monster = SpawnMonster(Vector3.zero);
            Health heroHp = hero.GetComponent<Health>();
            IAttacker attacker = monster.GetComponent<MeleeAttacker>();

            //# TimeStop 모사 — 공격 차단.
            attacker.Enabled = false;

            int before = heroHp.Current;
            //# 쿨다운(0.2) × 여러 회 분량 충분히 진행 — Enabled 였다면 다수 타격이 들어갔을 시간.
            yield return WaitTicks(30);

            Assert.AreEqual(before, heroHp.Current,
                $"Enabled=false 인데 데미지가 들어감 — AutoCombatAI 가 공격을 개시함. before={before}, after={heroHp.Current}");
        }

        //# 엣지 케이스 — Enabled 복원(OnDetached) 후 공격이 재개돼 데미지가 들어간다.
        [UnityTest]
        public IEnumerator Attacker_재활성화시_공격이_재개된다()
        {
            GameObject hero = SpawnHeroTarget(new Vector3(0.5f, 0f, 0f));
            GameObject monster = SpawnMonster(Vector3.zero);
            Health heroHp = hero.GetComponent<Health>();
            IAttacker attacker = monster.GetComponent<MeleeAttacker>();

            attacker.Enabled = false;
            yield return WaitTicks(10);
            int afterDisabled = heroHp.Current;
            Assert.AreEqual(100000, afterDisabled, "비활성 구간에서는 데미지가 없어야 한다.");

            //# 복원 — TimeStopAura.OnDetached 모사.
            attacker.Enabled = true;
            yield return WaitTicks(30);

            Assert.Less(heroHp.Current, afterDisabled,
                $"재활성화 후 공격이 재개돼 데미지가 들어가야 한다. after={heroHp.Current}");
        }
    }
}
