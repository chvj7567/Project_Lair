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
    //# RunEditModeTests() 가 비동기로 테스트를 시작하고, 완료 시 결과 JSON 을 파일로 출력.
    //# 컨트롤러는 그 파일을 폴링·읽어 결과 확인.
    public static class LairTestRunner
    {
        public const string ResultFile = "Library/lair-test-result.json";

        [MenuItem("Lair/Tests/Run EditMode Tests")]
        public static void RunEditModeTests()
        {
            //# 시작 전 결과 파일 초기화 — 폴링 측이 "아직 미완료" 를 명확히 인식
            WriteResult(new Summary
            {
                done = false,
                startedAt = DateTime.Now.ToString("o"),
                pass = 0, fail = 0, skip = 0,
                failures = new List<string>(),
            });

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
            }));

            Debug.Log($"[LairTestRunner] EditMode 테스트 시작 — 결과는 {ResultFile} 에 기록");
        }

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

        private static readonly Summary _state = new Summary
        {
            done = false, pass = 0, fail = 0, skip = 0, failures = new List<string>(),
        };

        private static void WriteResult(Summary s)
        {
            var json = JsonUtility.ToJson(s, prettyPrint: true);
            File.WriteAllText(ResultFile, json, new UTF8Encoding(false));
        }

        private class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor t)
            {
                _state.done = false;
                _state.pass = 0; _state.fail = 0; _state.skip = 0;
                _state.failures.Clear();
                _state.startedAt = DateTime.Now.ToString("o");
                WriteResult(_state);
            }

            public void TestStarted(ITestAdaptor t) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                //# 그룹/스위트 노드는 결과 두 번 카운트되므로 leaf 만 집계
                if (result.HasChildren) return;
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        _state.pass++;
                        break;
                    case TestStatus.Failed:
                        _state.fail++;
                        _state.failures.Add($"{result.Test.FullName} : {SafeTrim(result.Message)}");
                        break;
                    case TestStatus.Skipped:
                    case TestStatus.Inconclusive:
                        _state.skip++;
                        break;
                }
                WriteResult(_state);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                _state.done = true;
                _state.finishedAt = DateTime.Now.ToString("o");
                WriteResult(_state);
                Debug.Log($"[LairTestRunner] 완료 — Pass: {_state.pass}, Fail: {_state.fail}, Skip: {_state.skip}");
            }

            private static string SafeTrim(string s)
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                return s.Length <= 240 ? s : s.Substring(0, 240) + "…";
            }
        }
    }
}
