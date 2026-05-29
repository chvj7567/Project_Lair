using System.Collections;
using System.Collections.Generic;
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
    //# 캠페인 구조 — 전략별로 독립 [UnityTest] 메서드를 둔다(한 전략 실패가 다른 전략 결과를 잃지 않게):
    //#   - 캠페인_Random전략_10판         : Random  @15x, N=10
    //#   - 캠페인_TankerPriority전략_10판  : Tanker  @15x, N=10
    //#   - 캠페인_DealerPriority전략_10판  : Dealer  @15x, N=10
    //#   - 캠페인_AoEPriority전략_10판     : AoE     @15x, N=10
    //#   - 검증_Random_5배속_3판          : Random  @5x,  N=3  (15x 가속 아티팩트 검증용)
    //# 각 메서드는 시작 시 SimMetrics.CountExistingLines() 로 baseline 을 잡고,
    //# ReadSince(baseline) 으로 자기 슬라이스만 잘라 Summarize 한다 — RunRecord 에 전략 필드가 없으므로
    //# 순차 실행 + 메서드별 baseline 이 전략 분리의 유일한 안전 수단이다.
    //#
    //# [Category("Simulation")] — 일반 PlayMode 스모크 런에서 분리/필터 가능하게 한다.
    //#   캠페인만 실행: Unity Test Runner CLI 에서
    //#     -testCategory "Simulation"
    //#   일반 스모크만 실행(캠페인 제외):
    //#     -testCategory "!Simulation"
    [Category("Simulation")]
    public class BalanceSimulationTest : BattlePlayTestBase
    {
        //# Battle 씬 초기화/전투 한 판이 절대 넘지 않을 실제 벽시계 안전 한도(초).
        //# 5분 전투 / timeScale 15x ≈ 20초 + 비동기 초기화 여유.
        //# 5x 검증판은 5분 전투 ≈ 60초이므로 별도 한도(WallTimeFailSafe5xSeconds)를 쓴다.
        private const float WallTimeFailSafeSeconds = 90f;

        //# 5배속 검증판 전용 벽시계 안전 한도(초). 5분 전투 / 5x ≈ 60초 + 초기화 여유.
        private const float WallTimeFailSafe5xSeconds = 150f;

        //# 본 캠페인 전투 가속 배율. 컨셉상 5분 전투 → 약 20초로 단축.
        private const float SimTimeScale = 15f;

        //# 가속 아티팩트 검증용 저배율. 5분 전투 → 약 60초.
        private const float ValidationTimeScale = 5f;

        //# 씬 비동기 초기화(Addressables/스폰) 완료를 기다리는 최대 벽시계(초).
        private const float InitTimeoutSeconds = 15f;

        //# 본 캠페인 1전략당 표본 수. 선정 근거(리포트에도 명시):
        //#   - 4차 캠페인(v7): 교정계수 재보정을 위해 N=25 (3차 N=10보다 표본 확대).
        //#   - 4전략 × 25판 = 100판, 판당 ~30s(15x) → 총 ~50분.
        //#   - 분산이 크거나 목표 경계(2~4분)에 근접하면 리포트에서 N=30+ 후속 캠페인 권고.
        private const int CampaignGamesPerStrategy = 25;

        //# 가속 아티팩트 검증판 표본 수. 15x 와 평균 사망시각을 비교만 하면 되므로 소표본.
        private const int ValidationGames = 3;

        [TearDown]
        public void TearDown()
        {
            //# 가속 배율이 캠페인 도중 예외로 남지 않도록 항상 원복.
            Time.timeScale = 1f;
        }

        //# ===== 스모크 =====

        //# 스모크 — 캠페인 하베스가 한 판을 끝까지 돌리고 메트릭을 집계하는지 검증.
        //# 캠페인 본 메서드보다 먼저 도는 빠른 카나리 — 1판만 돌려 파이프라인 정상 여부를 확인한다.
        [UnityTest]
        public IEnumerator 시뮬레이션_스모크_랜덤전략_1판_완주_메트릭집계()
        {
            yield return RunCampaign(ESimStrategy.Random, 1, SimTimeScale,
                WallTimeFailSafeSeconds, "스모크 / Random / 15x");
        }

        //# ===== 본 캠페인 — 전략 4종 × 각 10판 @15x =====

        [UnityTest]
        public IEnumerator 캠페인_Random전략_10판()
        {
            yield return RunCampaign(ESimStrategy.Random, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / Random / 15x");
        }

        [UnityTest]
        public IEnumerator 캠페인_TankerPriority전략_10판()
        {
            yield return RunCampaign(ESimStrategy.TankerPriority, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / TankerPriority / 15x");
        }

        [UnityTest]
        public IEnumerator 캠페인_DealerPriority전략_10판()
        {
            yield return RunCampaign(ESimStrategy.DealerPriority, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / DealerPriority / 15x");
        }

        [UnityTest]
        public IEnumerator 캠페인_AoEPriority전략_10판()
        {
            yield return RunCampaign(ESimStrategy.AoEPriority, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / AoEPriority / 15x");
        }

        //# ===== 가속 아티팩트 검증 — Random @5x =====

        //# 15x 가속이 전투 결과를 왜곡하는지 검증. Random 전략을 5x 로 N판 돌려
        //# 같은 전략 15x(캠페인_Random전략_10판)의 평균 사망시각과 비교한다.
        //# 두 평균이 ~10% 이내면 15x 는 본 캠페인에 유효, 아니면 리포트에서 해당 수치를 교란값으로 명시.
        [UnityTest]
        public IEnumerator 검증_Random_5배속_3판()
        {
            yield return RunCampaign(ESimStrategy.Random, ValidationGames, ValidationTimeScale,
                WallTimeFailSafe5xSeconds, "검증 / Random / 5x");
        }

        //# ===== 공통 캠페인 루프 =====

        //# 한 전략을 gameCount 판 돌리고 자기 슬라이스만 잘라 메트릭을 Debug.Log 한다.
        //# baseline 은 이 메서드 시작 시점에 스냅샷 — 메서드별 독립 baseline 으로 전략 간 분리.
        private IEnumerator RunCampaign(ESimStrategy strategy, int gameCount, float timeScale,
            float wallTimeFailSafe, string label)
        {
            //# 이 캠페인 메서드 이전에 쌓인 jsonl 줄 수 — 메트릭 집계의 기준선.
            int baseline = SimMetrics.CountExistingLines();

            for (int i = 0; i < gameCount; ++i)
            {
                yield return RunOneGame(strategy, timeScale, wallTimeFailSafe);
            }

            //# jsonl flush 여유.
            yield return null;
            yield return null;

            List<RunRecord> records = SimMetrics.ReadSince(baseline);

            //# 메트릭을 Assert 보다 먼저 로그한다 — 한 판이 stall 해 표본이 모자라도 부분 결과를 남긴다.
            Debug.Log(SimMetrics.Summarize(records, $"{label} / {gameCount}판 목표"));

            Assert.AreEqual(gameCount, records.Count,
                $"{gameCount}판을 돌렸으니 baseline 이후 RunRecord 도 {gameCount}개여야 함 [{label}]");
        }

        //# 한 판 = 씬 로드 → 초기화 대기 → 픽 전략 주입 → 가속 틱 → 종료 감지 → 원복.
        private IEnumerator RunOneGame(ESimStrategy strategy, float timeScale, float wallTimeFailSafe)
        {
            //# a. CHMResource init 보장 (Loading 씬 건너뛰는 PlayMode 라 필수). 이후 Battle 씬 로드.
            yield return EnsureCHMReady();
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
            Time.timeScale = timeScale;

            //# e. 종료 감지 — ResultPopup 등장(공개 산출물) 또는 영웅 전멸.
            //#    가속 timeScale 무관하게 벽시계 fail-safe 로 무한 루프 방지.
            float wallElapsed = 0f;
            bool ended = false;
            while (wallElapsed < wallTimeFailSafe)
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
                $"전투가 {wallTimeFailSafe}s 안에 종료돼야 함 (fail-safe 초과 — 종료 감지 실패 의심)");
        }

        //# 레지스트리의 모든 영웅이 사망/소멸 상태인지. 정적 리스트라 IsAlive 로 방어 필터.
        private static bool AllHeroesDead()
        {
            List<CharacterRegistry.Entry> heroes = CharacterRegistry.Heroes;
            if (heroes.Count == 0) return false;   //# 아직 스폰 전 — 전멸로 오판 금지.
            foreach (CharacterRegistry.Entry e in heroes)
            {
                if (e?.Health != null && e.Health.IsAlive) return false;
            }
            return true;
        }
    }
}
