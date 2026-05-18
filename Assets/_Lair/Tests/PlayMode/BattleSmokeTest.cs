using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    public class BattleSmokeTest
    {
        [UnityTest]
        public IEnumerator Battle씬_로드_5초후_영웅_살아있음()
        {
            //# 씬 로드 (Build Settings 의 Battle 이 Index 0)
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# BattleController 의 비동기 초기화(Addressables + UI + 스폰) 완료 대기
            //# Time.deltaTime 누적으로 5초 흐름 시뮬
            float elapsed = 0f;
            while (elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            //# 레지스트리에 영웅 1명 이상 등록돼야 함 (BattleController.SpawnHero 가 정상 동작)
            Assert.Greater(CharacterRegistry.Heroes.Count, 0,
                "영웅이 레지스트리에 등록돼야 함 (BattleController 가 SpawnHero 호출했는지 확인)");

            //# 5초 시점에 영웅 1명 이상 살아있어야 (5분 안에 안 죽음)
            bool heroAlive = false;
            foreach (var e in CharacterRegistry.Heroes)
            {
                if (e?.Health != null && e.Health.IsAlive) { heroAlive = true; break; }
            }
            Assert.IsTrue(heroAlive, "5초 시점에 영웅 살아있어야 함");

            yield return null;
        }
    }
}
