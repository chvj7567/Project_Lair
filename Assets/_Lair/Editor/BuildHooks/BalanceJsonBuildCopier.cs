using System.IO;
using Lair.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 빌드 전: Data/Json 정본을 StreamingAssets 로 복사(런타임이 읽도록). 빌드 후: 산출물에 실제 안착했는지 검증.
    //# 정본 = Assets/_Lair/Data/Json (git 추적). StreamingAssets 사본은 빌드 산출물(git 미추적).
    public class BalanceJsonBuildCopier : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string SrcDir = "Assets/_Lair/Data/Json";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string src = Path.Combine(SrcDir, BalanceJsonLoader.FileName);
            string dstDir = "Assets/StreamingAssets";
            string dst = Path.Combine(dstDir, BalanceJsonLoader.FileName);
            if (File.Exists(src) == false)
            {
                Debug.LogWarning($"[BalanceJsonBuildCopier] 정본 없음 — 복사 스킵: {src}");
                return;
            }
            if (Directory.Exists(dstDir) == false)
            {
                Directory.CreateDirectory(dstDir);
            }
            File.Copy(src, dst, true);
            AssetDatabase.Refresh();
            Debug.Log($"[BalanceJsonBuildCopier] {src} → {dst}");
        }

        //# 빌드 산출물 검증 — 밸런스 데이터가 조용히 누락되면(플레이어가 코드 기본값으로 출시) 빌드를 실패시킨다.
        //# Standalone 만 파일시스템 확정 검증. 아카이브 내부 플랫폼(Android/iOS 등)은 경고 로그로 best-effort.
        public void OnPostprocessBuild(BuildReport report)
        {
            string streamingDir = ResolveStreamingAssetsPath(report);
            if (streamingDir == null)
            {
                Debug.LogWarning($"[BalanceJsonBuildCopier] {report.summary.platform} 은 산출물 StreamingAssets 검증 스킵(아카이브 내부 — 확인 불가).");
                return;
            }
            string dst = Path.Combine(streamingDir, BalanceJsonLoader.FileName);
            if (File.Exists(dst) == false)
            {
                throw new BuildFailedException(
                    $"[BalanceJsonBuildCopier] 빌드 산출물에 밸런스 JSON 누락 — 플레이어가 코드 기본값으로 출시될 위험: {dst}");
            }
            Debug.Log($"[BalanceJsonBuildCopier] 산출물 밸런스 JSON 확인: {dst}");
        }

        //# 산출물의 StreamingAssets 절대경로 — Standalone 만 확정. 확인 불가 플랫폼은 null.
        private static string ResolveStreamingAssetsPath(BuildReport report)
        {
            string outputPath = report.summary.outputPath;
            if (string.IsNullOrEmpty(outputPath))
                return null;

            string dataFolder = $"{Application.productName}_Data";
            switch (report.summary.platform)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                {
                    string dir = Path.GetDirectoryName(outputPath);
                    return Path.Combine(dir, dataFolder, "StreamingAssets");
                }
                case BuildTarget.StandaloneOSX:
                    //# outputPath = <name>.app 번들 루트.
                    return Path.Combine(outputPath, "Contents", "Resources", "Data", "StreamingAssets");
                default:
                    return null;
            }
        }
    }
}
