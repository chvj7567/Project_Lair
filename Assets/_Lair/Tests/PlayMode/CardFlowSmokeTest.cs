using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.UI;

namespace Lair.Tests.PlayMode
{
    public class CardFlowSmokeTest
    {
        [TearDown]
        public void TearDown()
        {
            //# 본 테스트가 의도적으로 카드 팝업을 띄우므로 timeScale=0 잔존 — 후속 테스트 영향 차단.
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator HP_90퍼_트리거시_CardSelectionPopup_자동표시()
        {
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            //# 1) BattleController.Start 가 비동기 — 영웅 스폰 + HUD 표시 + CardPool 로드까지 대기.
            //# unscaledDeltaTime — 만약 초기화 중 영웅이 다른 트리거로 팝업을 띄워 timeScale=0
            //# 으로 돼도 대기 루프가 hang 하지 않도록 견고화 (밸런스 변경 회귀 방지).
            float elapsed = 0f;
            while (elapsed < 3f) { elapsed += Time.unscaledDeltaTime; yield return null; }

            Assert.Greater(CharacterRegistry.Heroes.Count, 0, "Hero 스폰 확인");

            //# 2) Hero 에게 강제 데미지 (90/80% 임계점 통과)
            foreach (var e in CharacterRegistry.Heroes)
            {
                if (e?.Health != null)
                    e.Health.TakeDamage(e.Health.Max / 5);   //# 20% 데미지 → 80% 도달
            }
            yield return null;
            yield return null;

            //# 3) CHMUI.ShowUIAsync 비동기 완료 대기 (unscaledDeltaTime — Time.timeScale=0 영향 X)
            elapsed = 0f;
            while (elapsed < 1.5f) { elapsed += Time.unscaledDeltaTime; yield return null; }

            //# 4) CardSelectionPopup 이 씬에 활성화돼 있어야
            var popup = Object.FindFirstObjectByType<CardSelectionPopup>();
            Assert.IsNotNull(popup, "CardSelectionPopup 자동 표시 확인");

            //# 5) Time.timeScale 이 0 (Pause 작동)
            Assert.AreEqual(0f, Time.timeScale, 0.01f, "PauseService 작동 확인");

            //# 정리
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
