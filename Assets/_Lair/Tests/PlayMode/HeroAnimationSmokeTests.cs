using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    public class HeroAnimationSmokeTests
    {
        //# 프리팹 직접 로드 대신 컴포넌트 합성 — Addressable 의존 없이 드라이버 동작만 검증.
        [UnityTest]
        public IEnumerator Driver_OnSpawn_에러없이_Animator세팅()
        {
            GameObject go = new GameObject("HeroTest");
            go.AddComponent<Animator>();
            go.AddComponent<Health>();
            CharacterAnimationDriver driver = go.AddComponent<CharacterAnimationDriver>();

            //# Animator 컨트롤러 미할당 상태에서도 NRE 없이 OnEnable 통과(가드) 확인.
            yield return null;
            Assert.IsNotNull(driver);
            Object.Destroy(go);
        }
    }
}
