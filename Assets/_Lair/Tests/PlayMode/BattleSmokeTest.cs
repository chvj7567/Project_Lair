using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    public class BattleSmokeTest : BattlePlayTestBase
    {
        [TearDown]
        public void TearDown()
        {
            //# 카드 팝업이 떴으면 Pause 가 timeScale=0 남길 수 있음 — 후속 테스트 영향 차단.
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Battle씬_로드_5초후_영웅_살아있음()
        {
            yield return EnsureCHMReady();
            //# 씬 로드 (Build Settings 의 Battle 이 Index 0)
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# BattleController 가 씬에 잡힐 때까지 unscaledDeltaTime 으로 대기 (timeScale 무관).
            BattleController bc = null;
            float waitInit = 0f;
            while (waitInit < 5f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null) break;
                waitInit += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 가 씬에 있어야 함");

#if UNITY_EDITOR
            //# 카드 팝업이 뜨면 PauseService 가 timeScale=0 → Time.deltaTime=0 → 대기 루프 hang.
            //# DebugAutoPicker 로 팝업을 우회해 즉시 첫 장 픽. 게임 진행 견고화 목적.
            bc.DebugAutoPicker = (choices, src) =>
                (choices != null && choices.Count > 0) ? choices[0] : null;
#endif

            //# BattleController 의 비동기 초기화(Addressables + UI + 스폰) 완료 대기.
            //# Time.deltaTime 대신 unscaledDeltaTime — 카드 팝업 hang 방지.
            float elapsed = 0f;
            while (elapsed < 5f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            //# 레지스트리에 영웅 1명 이상 등록돼야 함 (BattleController.SpawnHero 가 정상 동작)
            Assert.Greater(CharacterRegistry.Heroes.Count, 0,
                "영웅이 레지스트리에 등록돼야 함 (BattleController 가 SpawnHero 호출했는지 확인)");

            //# 5초 시점에 영웅 1명 이상 살아있어야 (5분 안에 안 죽음)
            bool heroAlive = false;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Heroes)
            {
                if (e?.Health != null && e.Health.IsAlive) { heroAlive = true; break; }
            }
            Assert.IsTrue(heroAlive, "5초 시점에 영웅 살아있어야 함");

            yield return null;
        }
    }
}
