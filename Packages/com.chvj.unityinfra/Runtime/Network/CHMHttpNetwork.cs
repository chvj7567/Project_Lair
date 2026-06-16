using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ChvjUnityInfra
{
    //# HTTP 응답 결과 — 예외 throw 대신 값으로 성공/상태/본문 전달.
    public struct CHHttpResult
    {
        public bool IsSuccess;       //# 2xx 여부
        public long StatusCode;      //# HTTP 상태 (네트워크 에러면 0)
        public string Body;          //# 응답 본문(텍스트)
        public string Error;         //# 네트워크/프로토콜 에러 메시지(없으면 null)

        public bool IsConflict => StatusCode == 409;
        public bool IsNotFound => StatusCode == 404;
        public bool IsUnauthorized => StatusCode == 401;
    }

    //# 범용 async HTTP 래퍼. 게임 도메인 비종속(Rule 03 §1). UnityWebRequest 기반.
    public static class CHMHttpNetwork
    {
        public static int DefaultTimeoutSec = 10;

        public static Task<CHHttpResult> GetAsync(string url, string bearer = null, int? timeoutSec = null)
            => SendAsync(UnityWebRequest.kHttpVerbGET, url, null, bearer, timeoutSec);

        public static Task<CHHttpResult> PostAsync(string url, string jsonBody, string bearer = null, int? timeoutSec = null)
            => SendAsync(UnityWebRequest.kHttpVerbPOST, url, jsonBody, bearer, timeoutSec);

        public static Task<CHHttpResult> PutAsync(string url, string jsonBody, string bearer = null, int? timeoutSec = null)
            => SendAsync(UnityWebRequest.kHttpVerbPUT, url, jsonBody, bearer, timeoutSec);

        private static Task<CHHttpResult> SendAsync(string verb, string url, string jsonBody, string bearer, int? timeoutSec)
        {
            TaskCompletionSource<CHHttpResult> tcs = new TaskCompletionSource<CHHttpResult>();

            UnityWebRequest request = new UnityWebRequest(url, verb);
            if (string.IsNullOrEmpty(jsonBody) == false)
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(payload);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (string.IsNullOrEmpty(bearer) == false)
                request.SetRequestHeader("Authorization", $"Bearer {bearer}");
            request.timeout = timeoutSec ?? DefaultTimeoutSec;

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            op.completed += _ =>
            {
                CHHttpResult result = new CHHttpResult
                {
                    StatusCode = request.responseCode,
                    Body = request.downloadHandler != null ? request.downloadHandler.text : null,
                };
                //# ConnectionError/DataProcessingError 는 네트워크 실패. ProtocolError(4xx/5xx)는 상태코드로 전달.
                if (request.result == UnityWebRequest.Result.ConnectionError
                    || request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    result.IsSuccess = false;
                    result.Error = request.error;
                }
                else
                {
                    result.IsSuccess = request.responseCode >= 200 && request.responseCode < 300;
                    if (result.IsSuccess == false)
                        result.Error = request.error;
                }
                request.Dispose();
                tcs.TrySetResult(result);
            };

            return tcs.Task;
        }
    }
}
