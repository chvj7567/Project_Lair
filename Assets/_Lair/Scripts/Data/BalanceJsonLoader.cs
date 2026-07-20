using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Lair.Data
{
    //# balance_config.json 런타임 로더. 파싱은 순수 함수로 분리(파일IO 없이 테스트 가능).
    public static class BalanceJsonLoader
    {
        public const string FileName = "balance_config.json";

        //# 빈/공백/깨진 JSON → null. 예외를 삼켜 호출부가 코드 기본값(CreateDefault) fallback 하도록.
        public static BalanceConfigDto Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<BalanceConfigDto>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BalanceJsonLoader] JSON 파싱 실패 — SO fallback: {e.Message}");
                return null;
            }
        }

        //# 런타임 로드. 에디터: Data/Json 정본 직접. 플레이어: persistentDataPath(첫 실행 시 StreamingAssets 사본 복사).
        public static async Task<BalanceConfigDto> LoadAsync()
        {
            string text = await ReadJsonAsync();
            return Parse(text);
        }

        private static async Task<string> ReadJsonAsync()
        {
#if UNITY_EDITOR
            //# 에디터: Data/Json 정본 직접 읽기(모든 JSON 통일 위치, git 추적). 동기 File.IO — CS1998 회피 no-op await.
            //# 플레이어는 빌드훅(BalanceJsonBuildCopier)이 이 정본을 StreamingAssets 로 복사한 것을 읽는다.
            await Task.CompletedTask;
            string editorPath = Path.Combine(Application.dataPath, "_Lair/Data/Json", FileName);
            if (File.Exists(editorPath) == false)
            {
                Debug.LogWarning($"[BalanceJsonLoader] Data/Json 파일 없음 — 코드 기본값 fallback: {editorPath}");
                return null;
            }
            return ReadFileSafe(editorPath);
#else
            string persistent = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(persistent) == false)
            {
                //# 첫 실행 — StreamingAssets 원본을 persistentDataPath 로 복사(쓰기가능 경로).
                string src = Path.Combine(Application.streamingAssetsPath, FileName);
                string seed = await ReadStreamingAssetAsync(src);
                if (string.IsNullOrEmpty(seed))
                {
                    Debug.LogWarning($"[BalanceJsonLoader] StreamingAssets 기본값 없음 — 코드 기본값 fallback: {src}");
                    return null;
                }
                //# 복사 쓰기 실패(권한/디스크) 시 null → 호출부 코드 기본값 fallback.
                if (WriteFileSafe(persistent, seed) == false)
                    return null;
            }
            return ReadFileSafe(persistent);
#endif
        }

        //# File 읽기 — 파일이 있어도 락/권한/IO 실패 시 경고 + null (호출부 코드 기본값 fallback). spec §4.2.
        //# 예: 외부 편집기가 정본을 쓰는 순간의 읽기 락 등.
        private static string ReadFileSafe(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BalanceJsonLoader] 파일 읽기 실패 — SO fallback: {path} ({e.Message})");
                return null;
            }
        }

        //# File 쓰기(첫 실행 복사) — 실패 시 경고 + false (호출부 SO fallback).
        private static bool WriteFileSafe(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BalanceJsonLoader] 파일 쓰기 실패 — SO fallback: {path} ({e.Message})");
                return false;
            }
        }

        //# StreamingAssets 읽기 — 안드로이드는 APK 내부 URL 이라 UnityWebRequest, 그 외는 File.IO.
        private static async Task<string> ReadStreamingAssetAsync(string path)
        {
            if (path.Contains("://"))
            {
                using (UnityWebRequest req = UnityWebRequest.Get(path))
                {
                    UnityWebRequestAsyncOperation op = req.SendWebRequest();
                    while (op.isDone == false)
                    {
                        await Task.Yield();
                    }
                    if (req.result != UnityWebRequest.Result.Success)
                        return null;
                    return req.downloadHandler.text;
                }
            }
            //# 부재는 상위에서 "기본값 없음" 으로 처리 → 여기선 silent null. 존재하나 읽기 실패면 ReadFileSafe 가 경고+null.
            return File.Exists(path) ? ReadFileSafe(path) : null;
        }
    }
}
