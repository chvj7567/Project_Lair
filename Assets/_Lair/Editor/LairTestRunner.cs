using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Lair.EditorTools
{
    //# TestRunnerApi 래퍼 — UnityMCP editor_invoke_method 로 호출 가능.
    //# RunEditModeTests() / RunPlayModeTests() 가 비동기로 테스트를 시작하고,
    //# 완료 시 결과 JSON 을 파일로 출력. 컨트롤러는 그 파일을 폴링·읽어 결과 확인.
    //#
    //# PlayMode 테스트 노트:
    //#   Play 진입/종료 시 두 번의 도메인 리로드가 발생 → static 필드/콜백 인스턴스 모두 소실.
    //#   따라서 (a) 상태는 SessionState (도메인 리로드 생존) 에 보관,
    //#         (b) 콜백은 [InitializeOnLoadMethod] 로 도메인마다 재등록.
    public static class LairTestRunner
    {
        public const string ResultFile = "Library/lair-test-result.json";
        public const string ResultFilePlayMode = "Library/lair-test-result-playmode.json";

        //# SessionState 키 — 도메인 리로드 너머로 살아남는 상태
        private const string KeyActive       = "lair.testrunner.active";
        private const string KeyResultFile   = "lair.testrunner.resultFile";
        private const string KeyStartedAt    = "lair.testrunner.startedAt";
        private const string KeyFinishedAt   = "lair.testrunner.finishedAt";
        private const string KeyPass         = "lair.testrunner.pass";
        private const string KeyFail         = "lair.testrunner.fail";
        private const string KeySkip         = "lair.testrunner.skip";
        private const string KeyFailures     = "lair.testrunner.failures"; //# \n 으로 join

        [MenuItem("Lair/Tests/Run EditMode Tests")]
        public static void RunEditModeTests()
        {
            RunTests(TestMode.EditMode, ResultFile, excludeSimulation: false);
        }

        //# 일반 PlayMode 런 — 캠페인(BalanceSimulationTest [Category("Simulation")]) 제외.
        //# 캠페인 44판은 분 단위 wall-time 이라 일반 회귀 런에서 분리한다.
        [MenuItem("Lair/Tests/Run PlayMode Tests")]
        public static void RunPlayModeTests()
        {
            RunTests(TestMode.PlayMode, ResultFilePlayMode, excludeSimulation: true);
        }

        //# 캠페인 전용 런 — BalanceSimulationTest 만 실행.
        [MenuItem("Lair/Tests/Run PlayMode Simulation")]
        public static void RunPlayModeSimulation()
        {
            RunSimulationOnly(ResultFilePlayMode);
        }

        //# 공통 실행 진입점 — 모드/결과파일만 다르게.
        private static void RunTests(TestMode mode, string resultFile, bool excludeSimulation)
        {
            //# 시작 전 SessionState 리셋
            SessionState.SetBool(KeyActive, true);
            SessionState.SetString(KeyResultFile, resultFile);
            SessionState.SetString(KeyStartedAt, DateTime.Now.ToString("o"));
            SessionState.SetString(KeyFinishedAt, string.Empty);
            SessionState.SetInt(KeyPass, 0);
            SessionState.SetInt(KeyFail, 0);
            SessionState.SetInt(KeySkip, 0);
            SessionState.SetString(KeyFailures, string.Empty);

            //# "아직 미완료" 상태로 결과 파일 초기 기록 — 폴링 측이 명확히 인식
            WriteResultFromSession();

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            //# Unregister 먼저 — RegisterCallbacks 가 중복 등록 시 같은 콜백이
            //# 여러 번 호출돼 카운트가 N 배 되는 버그 방지 (참조 일치로 매칭)
            api.UnregisterCallbacks(_sharedCallbacks);
            api.RegisterCallbacks(_sharedCallbacks);

            var filter = new Filter { testMode = mode };
            //# PlayMode 일반 런은 [Category("Simulation")] 테스트 제외.
            //# Unity Filter 는 카테고리 negation 을 직접 지원 안 함 → groupNames 정규식 negative lookahead
            //# 로 BalanceSimulationTest 클래스를 제외. groupNames 는 풀네임(namespace + class)에 매칭.
            if (excludeSimulation)
            {
                filter.groupNames = new[] { "^(?!.*BalanceSimulationTest).*$" };
            }
            api.Execute(new ExecutionSettings(filter));

            Debug.Log($"[LairTestRunner] {mode} 테스트 시작 (excludeSimulation={excludeSimulation}) — 결과는 {resultFile} 에 기록");
        }

        //# 캠페인 전용 — BalanceSimulationTest 클래스만 실행.
        private static void RunSimulationOnly(string resultFile)
        {
            SessionState.SetBool(KeyActive, true);
            SessionState.SetString(KeyResultFile, resultFile);
            SessionState.SetString(KeyStartedAt, DateTime.Now.ToString("o"));
            SessionState.SetString(KeyFinishedAt, string.Empty);
            SessionState.SetInt(KeyPass, 0);
            SessionState.SetInt(KeyFail, 0);
            SessionState.SetInt(KeySkip, 0);
            SessionState.SetString(KeyFailures, string.Empty);

            WriteResultFromSession();

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.UnregisterCallbacks(_sharedCallbacks);
            api.RegisterCallbacks(_sharedCallbacks);
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                categoryNames = new[] { "Simulation" },
            }));

            Debug.Log($"[LairTestRunner] PlayMode Simulation 테스트 시작 — 결과는 {resultFile} 에 기록");
        }

        //# 도메인 리로드마다 자동 호출 — Play 진입/종료 시 콜백 재등록 필수
        [InitializeOnLoadMethod]
        private static void RegisterOnDomainLoad()
        {
            //# 활성 실행 중일 때만 재등록 (다른 테스트 러너 동작에 끼어들지 않도록)
            if (SessionState.GetBool(KeyActive, false) == false) return;

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            //# Unregister 먼저 — 중복 등록 방지
            api.UnregisterCallbacks(_sharedCallbacks);
            api.RegisterCallbacks(_sharedCallbacks);
        }

        //# 공유 콜백 인스턴스 — 도메인 1회당 1개. _sharedCallbacks 는 static 이지만
        //# 어차피 도메인 리로드 때 새로 생성되니 도메인별로 fresh.
        private static readonly Callbacks _sharedCallbacks = new Callbacks();

        [Serializable]
        private class Summary
        {
            public bool done;
            public string startedAt;
            public string finishedAt;
            public int pass;
            public int fail;
            public int skip;
            public List<string> failures;
        }

        private static void WriteResultFromSession()
        {
            var path = SessionState.GetString(KeyResultFile, ResultFile);
            if (string.IsNullOrEmpty(path)) path = ResultFile;

            var failuresJoined = SessionState.GetString(KeyFailures, string.Empty);
            var failures = string.IsNullOrEmpty(failuresJoined)
                ? new List<string>()
                : new List<string>(failuresJoined.Split('\n'));

            var s = new Summary
            {
                done       = !string.IsNullOrEmpty(SessionState.GetString(KeyFinishedAt, string.Empty)),
                startedAt  = SessionState.GetString(KeyStartedAt, string.Empty),
                finishedAt = SessionState.GetString(KeyFinishedAt, string.Empty),
                pass       = SessionState.GetInt(KeyPass, 0),
                fail       = SessionState.GetInt(KeyFail, 0),
                skip       = SessionState.GetInt(KeySkip, 0),
                failures   = failures,
            };

            var json = JsonUtility.ToJson(s, prettyPrint: true);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor t)
            {
                //# 시작 시점만 파일 한 번 더 갱신 (상태는 RunTests 에서 이미 세팅됨)
                WriteResultFromSession();
            }

            public void TestStarted(ITestAdaptor t) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                //# 활성 실행이 아니면 무시 — 다른 사용자 트리거 테스트와 충돌 방지
                if (SessionState.GetBool(KeyActive, false) == false) return;
                //# 그룹/스위트 노드는 결과 두 번 카운트되므로 leaf 만 집계
                if (result.HasChildren) return;

                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        SessionState.SetInt(KeyPass, SessionState.GetInt(KeyPass, 0) + 1);
                        break;
                    case TestStatus.Failed:
                        SessionState.SetInt(KeyFail, SessionState.GetInt(KeyFail, 0) + 1);
                        var msg = $"{result.Test.FullName} : {SafeTrim(result.Message)}";
                        var prev = SessionState.GetString(KeyFailures, string.Empty);
                        SessionState.SetString(KeyFailures,
                            string.IsNullOrEmpty(prev) ? msg : prev + "\n" + msg);
                        break;
                    case TestStatus.Skipped:
                    case TestStatus.Inconclusive:
                        SessionState.SetInt(KeySkip, SessionState.GetInt(KeySkip, 0) + 1);
                        break;
                }
                WriteResultFromSession();
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (SessionState.GetBool(KeyActive, false) == false) return;

                SessionState.SetString(KeyFinishedAt, DateTime.Now.ToString("o"));
                WriteResultFromSession();
                //# 완료 표시 후 활성 플래그 클리어 — 후속 외부 테스트 트리거 영향 방지
                SessionState.SetBool(KeyActive, false);

                Debug.Log($"[LairTestRunner] 완료 — Pass: {SessionState.GetInt(KeyPass, 0)}, " +
                          $"Fail: {SessionState.GetInt(KeyFail, 0)}, " +
                          $"Skip: {SessionState.GetInt(KeySkip, 0)}");
            }

            private static string SafeTrim(string s)
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                return s.Length <= 240 ? s : s.Substring(0, 240) + "…";
            }
        }
    }
}
