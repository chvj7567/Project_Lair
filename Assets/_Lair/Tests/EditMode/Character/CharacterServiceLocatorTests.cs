using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# Character 서비스 로케이터 — lazy 해석·미부착 null·늦은 추가 해석 검증.
    public class CharacterServiceLocatorTests
    {
        [Test]
        public void 부착된_서비스는_Get으로_조회되고_미부착은_null()
        {
            GameObject go = new GameObject("char");
            Health health = go.AddComponent<Health>();      //# IHealth 구현
            LairCharacter character = go.AddComponent<LairCharacter>();

            //# lazy — Awake 강제 없이도 Get 호출 시점에 해석되어야 함(EditMode Awake 미실행 견딤)
            Assert.AreSame(health, character.Get<IHealth>());
            Assert.AreSame(health, (object)character.Health);
            Assert.IsNull(character.Get<IAttacker>());
            Assert.IsNull(character.Attacker);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryGet은_부착여부를_bool로_반환()
        {
            GameObject go = new GameObject("char");
            go.AddComponent<Health>();
            LairCharacter character = go.AddComponent<LairCharacter>();

            Assert.IsTrue(character.TryGet(out IHealth h));
            Assert.IsNotNull(h);
            Assert.IsFalse(character.TryGet(out IAttacker a));
            Assert.IsNull(a);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void 서비스를_소비자보다_늦게_추가해도_lazy로_해석된다()
        {
            GameObject go = new GameObject("char");
            LairCharacter character = go.AddComponent<LairCharacter>();
            //# Character 부착 후 서비스 추가 — eager Awake 방식이면 놓치지만 lazy 는 잡는다
            Health health = go.AddComponent<Health>();

            Assert.AreSame(health, character.Get<IHealth>());

            Object.DestroyImmediate(go);
        }

        //# ===== 엣지 케이스 보강 (test-engineer) — 미부착 TryGet·캐시 일관·타입 분리·프로퍼티 위임 =====

        //# 서비스가 하나도 없는 로케이터 — TryGet 은 false + out null (부분 조회 안전 계약).
        [Test]
        public void 미부착_서비스_TryGet은_false이고_out은_null()
        {
            GameObject go = new GameObject("char");
            LairCharacter character = go.AddComponent<LairCharacter>();

            Assert.IsFalse(character.TryGet(out IMover mover));
            Assert.IsNull(mover);
            Assert.IsFalse(character.TryGet(out IAttacker attacker));
            Assert.IsNull(attacker);

            Object.DestroyImmediate(go);
        }

        //# 같은 타입 반복 Get — 매 호출 동일 인스턴스를 반환(캐시 일관). 재해석돼도 동일 컴포넌트라 참조 불변.
        [Test]
        public void 같은_타입_반복_Get은_동일_인스턴스_반환()
        {
            GameObject go = new GameObject("char");
            Health health = go.AddComponent<Health>();
            LairCharacter character = go.AddComponent<LairCharacter>();

            IHealth first = character.Get<IHealth>();
            IHealth second = character.Get<IHealth>();
            IHealth third = character.Get<IHealth>();

            Assert.AreSame(health, first);
            Assert.AreSame(first, second);
            Assert.AreSame(second, third);

            Object.DestroyImmediate(go);
        }

        //# 여러 서비스 동시 부착 — 각 타입이 정확히 자기 구현체로 분리 반환(타입 혼선 없음).
        [Test]
        public void 여러_서비스_동시부착시_각_타입_정확히_분리_반환()
        {
            GameObject go = new GameObject("char");
            Health health = go.AddComponent<Health>();
            SimpleMover mover = go.AddComponent<SimpleMover>();
            SimpleRotator rotator = go.AddComponent<SimpleRotator>();
            HeroAttackGate gate = go.AddComponent<HeroAttackGate>();
            LairCharacter character = go.AddComponent<LairCharacter>();

            Assert.AreSame(health, character.Get<IHealth>());
            Assert.AreSame(mover, character.Get<IMover>());
            Assert.AreSame(rotator, character.Get<IRotator>());
            Assert.AreSame(gate, character.Get<IAttackGate>());

            Object.DestroyImmediate(go);
        }

        //# 편의 타입 프로퍼티는 Get<T>() 와 동일 인스턴스를 위임 반환. 미부착 타입은 프로퍼티도 null.
        [Test]
        public void 편의_프로퍼티는_Get과_동일_인스턴스_반환()
        {
            GameObject go = new GameObject("char");
            Health health = go.AddComponent<Health>();
            SimpleMover mover = go.AddComponent<SimpleMover>();
            LairCharacter character = go.AddComponent<LairCharacter>();

            Assert.AreSame(health, character.Health);
            Assert.AreSame(character.Get<IHealth>(), character.Health);
            Assert.AreSame(mover, character.Mover);
            Assert.AreSame(character.Get<IMover>(), character.Mover);
            //# 미부착 타입은 Get 과 동일하게 프로퍼티도 null.
            Assert.IsNull(character.Attacker);

            Object.DestroyImmediate(go);
        }
    }
}
