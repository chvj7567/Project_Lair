using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.PlayMode
{
    //# 시뮬레이션 인프라 — 헤드리스 밸런스 시뮬레이션 캠페인의 [UnityTest] 진입점.
    //# Battle 씬을 N판 자동 플레이하고 RunRecorder jsonl 로부터 메트릭을 집계한다.
    //# 카드 픽 자동화는 BattleController.DebugAutoPicker(에디터 전용 훅)로만 구동 — 게임 로직 무수정.
    //#
    //# [Category("Simulation")] — 일반 PlayMode 스모크 런에서 분리/필터 가능하게 한다.
    //#   캠페인만 실행: Unity Test Runner CLI 에서
    //#     -testCategory "Simulation"
    //#   일반 스모크만 실행(캠페인 제외):
    //#     -testCategory "!Simulation"
    [Category("Simulation")]
    public class BalanceSimulationTest
    {
        //# Battle 씬 초기화/전투 한 판이 절대 넘지 않을 실제 벽시계 안전 한도(초).
        //# 5분 전투 / timeScale 15x ≈ 20초 + 비동기 초기화 여유.
        private const float WallTimeFailSafeSeconds = 90f;

        //# 전투 가속 배율. 컨셉상 5분 전투 → 약 20초로 단축.
        private const float SimTimeScale = 15f;

        //# 씬 비동기 초기화(Addressables/스폰) 완료를 기다리는 최대 벽시계(초).
        private const float InitTimeoutSeconds = 15f;

        [TearDown]
        public void TearDown()
        {
            //# 가속 배율이 캠페인 도중 예외로 남지 않도록 항상 원복.
            Time.timeScale = 1f;
        }

        //# 스모크 — 캠페인 하베스가 한 판을 끝까지 돌리고 메트릭을 집계하는지 검증.
        //# 본 멀티 전략 캠페인은 별도 작업. 여기선 N=1 (Random 전략)만 돌린다.
        [UnityTest]
        public IEnumerator 시뮬레이션_스모크_랜덤전략_1판_완주_메트릭집계()
        {
            const int gameCount = 1;
            //# 이번 스모크 이전에 쌓인 jsonl 줄 수 — 메트릭 집계의 기준선.
            int baseline = SimMetrics.CountExistingLines();

            for (int i = 0; i < gameCount; ++i)
            {
                yield return RunOneGame(ESimStrategy.Random);
            }

            //# jsonl flush 여유.
            yield return null;
            yield return null;

            var records = SimMetrics.ReadSince(baseline);
            Debug.Log(SimMetrics.Summarize(records, $"스모크 / Random / {gameCount}판"));

            Assert.AreEqual(gameCount, records.Count,
                $"{gameCount}판을 돌렸으니 baseline 이후 RunRecord 도 {gameCount}개여야 함");
        }

        //# 한 판 = 씬 로드 → 초기화 대기 → 픽 전략 주입 → 가속 틱 → 종료 감지 → 원복.
        private IEnumerator RunOneGame(ESimStrategy strategy)
        {
            //# a. Battle 씬 로드 (Build Settings Index 0). 정적 레지스트리는 재로드 시 갱신됨.
            yield return SceneManager.LoadSceneAsync(EScene.Battle.ToString());
            yield return null;

            //# b. BattleController.Start 의 비동기 초기화(Addressables/UI/스폰) 완료 대기.
            //#    프록시: BattleController 존재 + 영웅 1명 이상 레지스트리 등록.
            BattleController bc = null;
            float initElapsed = 0f;
            while (initElapsed < InitTimeoutSeconds)
            {
                if (bc == null) bc = Object.FindFirstObjectByType<BattleController>();
                bool heroReady = bc != null && CharacterRegistry.Heroes.Count > 0;
                if (heroReady) break;
                //# 초기화는 timeScale 영향 없는 벽시계로 측정.
                initElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(bc, "BattleController 가 씬에 있어야 함");
            Assert.Greater(CharacterRegistry.Heroes.Count, 0,
                "초기화 완료 시 영웅이 레지스트리에 등록돼야 함 (초기화 타임아웃 의심)");

            //# c. 카드 픽 자동화 훅 주입 — 이 시점부터 트리거 시 팝업 대신 전략이 픽.
            //#    DebugAutoPicker 는 BattleController 에 #if UNITY_EDITOR 로 선언 — 동일 가드로 접근.
#if UNITY_EDITOR
            bc.DebugAutoPicker = SimPickStrategy.Get(strategy);
#else
            //# 에디터 외 빌드 — 시뮬레이션 훅이 없으므로 캠페인 불가. 즉시 실패 처리.
            Assert.Fail("밸런스 시뮬레이션은 에디터 전용입니다 (DebugAutoPicker 훅이 #if UNITY_EDITOR).");
#endif

            //# d. 초기화가 끝난 뒤 전투를 가속.
            Time.timeScale = SimTimeScale;

            //# e. 종료 감지 — ResultPopup 등장(공개 산출물) 또는 영웅 전멸.
            //#    가속 timeScale 무관하게 벽시계 fail-safe 로 무한 루프 방지.
            float wallElapsed = 0f;
            bool ended = false;
            while (wallElapsed < WallTimeFailSafeSeconds)
            {
                if (Object.FindFirstObjectByType<ResultPopup>() != null) { ended = true; break; }
                if (AllHeroesDead()) { ended = true; break; }
                wallElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            //# f. RunRecorder.FinishRun(jsonl append) flush 여유.
            yield return null;
            yield return null;

            //# g. 가속 원복 — 다음 판/후속 테스트 영향 차단.
            Time.timeScale = 1f;

            Assert.IsTrue(ended,
                $"전투가 {WallTimeFailSafeSeconds}s 안에 종료돼야 함 (fail-safe 초과 — 종료 감지 실패 의심)");
        }

        //# 레지스트리의 모든 영웅이 사망/소멸 상태인지. 정적 리스트라 IsAlive 로 방어 필터.
        private static bool AllHeroesDead()
        {
            var heroes = CharacterRegistry.Heroes;
            if (heroes.Count == 0) return false;   //# 아직 스폰 전 — 전멸로 오판 금지.
            foreach (var e in heroes)
            {
                if (e?.Health != null && e.Health.IsAlive) return false;
            }
            return true;
        }
    }
}
