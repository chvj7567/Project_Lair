using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Lair.Meta;
using Lair.UI;

namespace Lair.Tests.PlayMode
{
    //# 시뮬레이션 인프라 — 헤드리스 밸런스 시뮬레이션 캠페인의 [UnityTest] 진입점.
    //# Battle 씬을 N판 자동 플레이하고 RunRecorder jsonl 로부터 메트릭을 집계한다.
    [Category("Simulation")]
    public class BalanceSimulationTest : BattlePlayTestBase
    {
        //# Battle 씬 초기화/전투 한 판이 절대 넘지 않을 실제 벽시계 안전 한도(초).
        //# 5분 전투 / timeScale 15x ≈ 20초 + 비동기 초기화 여유.
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
        private const int CampaignGamesPerStrategy = 25;

        //# 가속 아티팩트 검증판 표본 수. 15x 와 평균 사망시각을 비교만 하면 되므로 소표본.
        private const int ValidationGames = 3;

        //# 종족 강화 밸런스 게이트(monster-species-enhancement §8.2) 시나리오당 표본 수 — 중앙값 안정성 위해 N≥30.
        private const int GateGamesPerScenario = 30;

        //# 게이트 시드 대상 6종 — Enhance_<EMonster> Id 로 MetaSession.Profile 에 강화 레벨 주입.
        private static readonly EMonster[] AllSpecies = (EMonster[])System.Enum.GetValues(typeof(EMonster));

        //# LairTestRunner.KeyExcludeSim 과 동일 문자열 — Editor 어셈블리 참조를 끌어오지 않으려 리터럴로 미러.
        //# 값이 바뀌면 LairTestRunner 와 이 상수를 함께 갱신해야 한다.
        private const string KeyExcludeSim = "lair.testrunner.excludeSim";

        [SetUp]
        public void SetUp()
        {
            //# 일반 PlayMode 런(Run PlayMode Tests)은 분 단위 캠페인을 제외한다.
            //# LairTestRunner 가 SessionState 신호를 세팅하면 본문 실행 전 전체 스킵.
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(KeyExcludeSim, false))
            {
                Assert.Ignore("일반 PlayMode 런은 시뮬레이션 제외 — Run PlayMode Simulation 으로 별도 실행");
            }
#endif
        }

        [TearDown]
        public void TearDown()
        {
            //# 가속 배율이 캠페인 도중 예외로 남지 않도록 항상 원복.
            Time.timeScale = 1f;
        }

        //# 종족 강화 게이트 시나리오가 시드한 강화 레벨을 매 테스트 후 0 으로 원복 — 후속(기존) 캠페인이 항등 프로필로 시작하도록 격리.
        //# MetaProfilePlayModeIsolation 은 세션 1회(OneTimeSetUp)만 클린하므로, 메서드 간 격리는 이 TearDown 이 책임진다.
        [TearDown]
        public void 종족강화_레벨_원복()
        {
            if (MetaSession.Profile != null)
            {
                CleanSpeciesLevels();
            }
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

        //# ===== 본 캠페인 — 5 전략(Random + 4축) × 각 N판 @15x (v0.6) =====

        [UnityTest]
        public IEnumerator 캠페인_Random전략_10판()
        {
            yield return RunCampaign(ESimStrategy.Random, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / Random / 15x");
        }

        [UnityTest]
        public IEnumerator 캠페인_TankAxisPriority전략_10판()
        {
            yield return RunCampaign(ESimStrategy.TankAxisPriority, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / TankAxisPriority / 15x");
        }

        [UnityTest]
        public IEnumerator 캠페인_DpsAxisPriority전략_10판()
        {
            yield return RunCampaign(ESimStrategy.DpsAxisPriority, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / DpsAxisPriority / 15x");
        }

        [UnityTest]
        public IEnumerator 캠페인_DebuffAxisPriority전략_10판()
        {
            yield return RunCampaign(ESimStrategy.DebuffAxisPriority, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / DebuffAxisPriority / 15x");
        }

        [UnityTest]
        public IEnumerator 캠페인_SwarmAxisPriority전략_10판()
        {
            yield return RunCampaign(ESimStrategy.SwarmAxisPriority, CampaignGamesPerStrategy, SimTimeScale,
                WallTimeFailSafeSeconds, "캠페인 / SwarmAxisPriority / 15x");
        }

        //# ===== 가속 아티팩트 검증 — Random @5x =====

        //# 15x 가속이 전투 결과를 왜곡하는지 검증. Random 전략을 5x 로 N판 돌려
        //# 같은 전략 15x(캠페인_Random전략_10판)의 평균 사망시각과 비교한다.
        [UnityTest]
        public IEnumerator 검증_Random_5배속_3판()
        {
            yield return RunCampaign(ESimStrategy.Random, ValidationGames, ValidationTimeScale,
                WallTimeFailSafe5xSeconds, "검증 / Random / 5x");
        }

        //# ===== 종족 강화 밸런스 게이트 (monster-species-enhancement §8.2 — 영웅 사망 시간 중앙값) =====
        //# 실행: Lair/Tests/Run PlayMode Simulation (일반 PlayMode 런은 [SetUp] KeyExcludeSim 가드로 자동 제외).
        //# 각 시나리오는 런 루프 前 MetaSession.Profile 에 Enhance_<종족> 레벨을 시드 → ApplyMetaBonuses 가 전투에 반영.
        //# jsonl 슬라이스는 [SPECIES-GATE] 로그의 jsonlFrom/jsonlCount 로 시나리오별 분리 집계(qa-simulator).

        //# 배선 검증(필수 선행) — Phantom Lv5 시드가 실제 전투 BattleController._speciesLevels 까지 도달하는지.
        //# 이게 통과해야 아래 게이트들의 시드가 baseline 과 다른 전투를 만든다는 게 증명된다(count 일치만으론 불충분).
        [UnityTest]
        public IEnumerator 배선검증_Phantom_Lv5_시드가_전투_speciesLevels에_반영된다()
        {
            SeedSpeciesLevels((EMonster.Phantom, 5));

            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync(EScene.Battle.ToString());
            yield return null;

            BattleController bc = null;
            float initElapsed = 0f;
            while (initElapsed < InitTimeoutSeconds)
            {
                if (bc == null) bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null && CharacterRegistry.Heroes.Count > 0) break;
                initElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 가 씬에 있어야 함 (초기화 타임아웃 의심)");

            Dictionary<EMonster, int> speciesLevels = ReadSpeciesLevels(bc);
            speciesLevels.TryGetValue(EMonster.Phantom, out int phantomLv);
            Assert.AreEqual(5, phantomLv,
                "시드한 Phantom Lv5 가 GetOrLoad→ApplyMetaBonuses 를 거쳐 _speciesLevels 에 반영돼야 함 (주입 배선 검증)");
            //# 미시드 종족은 0 — baseline 과 시드 시나리오가 실제로 다름을 보장하는 판별자.
            speciesLevels.TryGetValue(EMonster.Wisp, out int wispLv);
            Assert.AreEqual(0, wispLv, "미시드 종족(Wisp)은 0 이어야 함 (시드 누수 없음)");
        }

        //# 기준 — 강화0(전종 Lv0). 게이트 판정: 중앙값 2:00~4:00 (§8 튜닝 창). 강화와 무관한 기존 밸런스 회귀 신호이기도.
        //# [Timeout] — NUnit 기본 180s 로는 30판(한 판 ~15초)이 못 끝난다. 실측 ~450s + init 여유 → 900s(NUnit.Framework.Timeout).
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator 게이트기준_강화0_Random_30판()
        {
            yield return RunSpeciesGateCampaign("기준_강화0", System.Array.Empty<(EMonster, int)>(), GateGamesPerScenario);
        }

        //# A — 단일 종족 Lv5(나머지 Lv0). 대표 2종(Phantom: 스웜 다수 / Wraith: 소수 탱커)로 종족 편차 확인. 판정: 중앙값 ≥ 2:00.
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator 게이트A_Phantom_Lv5_Random_30판()
        {
            yield return RunSpeciesGateCampaign("A_Phantom_Lv5", new[] { (EMonster.Phantom, 5) }, GateGamesPerScenario);
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator 게이트A_Wraith_Lv5_Random_30판()
        {
            yield return RunSpeciesGateCampaign("A_Wraith_Lv5", new[] { (EMonster.Wraith, 5) }, GateGamesPerScenario);
        }

        //# B — 전 6종 Lv5. 최대 파워 상한. 판정: 중앙값 ≥ 1:30 (최소 반격 여지).
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator 게이트B_전6종_Lv5_Random_30판()
        {
            (EMonster, int)[] allMax =
            {
                (EMonster.Wisp, 5), (EMonster.Wraith, 5), (EMonster.Reaper, 5),
                (EMonster.Hex, 5), (EMonster.Plague, 5), (EMonster.Phantom, 5),
            };
            yield return RunSpeciesGateCampaign("B_전6종_Lv5", allMax, GateGamesPerScenario);
        }

        //# 한 게이트 시나리오 — 레벨 시드 → N판 Random @15x → 자기 슬라이스 median 집계 + 머신 파서블 태그 로그.
        private IEnumerator RunSpeciesGateCampaign(string scenarioTag, (EMonster species, int level)[] seed, int gameCount)
        {
            //# 시드는 반드시 런 루프(첫 LoadSceneAsync) 前 — ApplyMetaBonuses 는 BattleController.Start 에서 MetaSession.Profile 을 읽는다.
            SeedSpeciesLevels(seed);

            int baseline = SimMetrics.CountExistingLines();

            for (int i = 0; i < gameCount; ++i)
            {
                yield return RunOneGame(ESimStrategy.Random, SimTimeScale, WallTimeFailSafeSeconds);
            }

            yield return null;
            yield return null;

            List<RunRecord> records = SimMetrics.ReadSince(baseline);
            float median = SimMetrics.MedianDeathTime(records);

            //# 사람용 요약(median 포함) + 머신 파서블 태그 — qa 가 jsonl[jsonlFrom, jsonlFrom+jsonlCount) 를 시나리오로 매핑.
            Debug.Log(SimMetrics.Summarize(records, $"종족강화 게이트 / {scenarioTag} / Random / 15x / {gameCount}판 목표"));
            Debug.Log($"[SPECIES-GATE] scenario={scenarioTag} strategy=Random timeScale=15 " +
                $"jsonlFrom={baseline} jsonlCount={records.Count} medianDeathSec={median:0.0}");

            Assert.AreEqual(gameCount, records.Count,
                $"종족강화 게이트 [{scenarioTag}]: {gameCount}판을 돌렸으니 RunRecord 도 {gameCount}개여야 함");
        }

        //# 6종 강화 레벨을 전부 0 으로 클린 후, 지정 종족만 레벨 시드. 소울/전적 등 다른 프로필 필드는 건드리지 않는다.
        private static void SeedSpeciesLevels(params (EMonster species, int level)[] levels)
        {
            CleanSpeciesLevels();
            MetaProfile p = MetaSession.GetOrLoad();
            foreach ((EMonster species, int level) in levels)
            {
                p.SetShopLevel(SpeciesShopId(species), level);
            }
        }

        //# 6종 강화 레벨을 전부 0 으로 — MetaBattleBonus 는 level<=0 을 스킵하므로 항등(미강화)과 동치.
        private static void CleanSpeciesLevels()
        {
            MetaProfile p = MetaSession.GetOrLoad();
            foreach (EMonster s in AllSpecies)
            {
                p.SetShopLevel(SpeciesShopId(s), 0);
            }
        }

        private static string SpeciesShopId(EMonster species) => "Enhance_" + species;

        //# BattleController 의 private 종족 레벨 캐시 열람 — 시드 배선 검증 전용(테스트 리플렉션).
        private static Dictionary<EMonster, int> ReadSpeciesLevels(BattleController bc)
        {
            FieldInfo f = typeof(BattleController).GetField("_speciesLevels",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "_speciesLevels 필드 접근 실패 — 프로덕션 필드명 변경 의심");
            return (Dictionary<EMonster, int>)f.GetValue(bc);
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
                //# 가속 배율 재적용 — PauseService 언포즈(Pop depth0)가 Time.timeScale=1 을 하드코딩 복원하므로,
                //# 카드 트리거(액티브 30초/패시브 HP)마다 포즈→언포즈되며 15x 가 1 로 덮인다. 진짜 포즈(0)면 존중하고
                //# 언포즈로 1 이 된 순간만 다시 가속(DebugAutoPicker 픽은 즉시라 포즈는 1~수 프레임 — 픽 타이밍 무영향).
                if (Time.timeScale != 0f && Mathf.Approximately(Time.timeScale, timeScale) == false)
                {
                    Time.timeScale = timeScale;
                }
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
