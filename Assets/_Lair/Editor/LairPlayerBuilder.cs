using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Android 플레이어(APK) 빌드 진입점. 메인 오케스트레이터가 UnityMCP 메뉴 실행으로 호출.
    //# 빌드 씬은 EditorBuildSettings 의 enabled 항목을 순서대로(중복 제거) 사용. 결과/에러를 콘솔에 명시 로깅.
    public static class LairPlayerBuilder
    {
        private const string OutputDir = "Builds/Android";

        [MenuItem("Lair/Build Android Player")]
        public static void BuildAndroid()
        {
            List<string> scenes = CollectEnabledScenes();
            if (scenes.Count == 0)
            {
                Debug.LogError("[LairPlayerBuilder] 빌드할 씬이 없음 — EditorBuildSettings 에 enabled 씬을 추가하세요");
                return;
            }

            Directory.CreateDirectory(OutputDir);
            string apkPath = $"{OutputDir}/{Application.productName}.apk";

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Debug.Log($"[LairPlayerBuilder] Android 빌드 시작 — scenes={scenes.Count}, out={apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[LairPlayerBuilder] 빌드 실패: result={summary.result}, errors={summary.totalErrors}");
                return;
            }

            double mb = summary.totalSize / (1024.0 * 1024.0);
            Debug.Log($"[LairPlayerBuilder] 빌드 완료 — {apkPath}, {mb:F1}MB, {summary.totalTime.TotalSeconds:F0}s");
        }

        //# EditorBuildSettings 의 enabled 씬 경로를 순서 보존 + 중복 제거로 수집.
        private static List<string> CollectEnabledScenes()
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled == false)
                    continue;

                if (seen.Add(scene.path) == false)
                    continue;

                result.Add(scene.path);
            }

            return result;
        }
    }
}
