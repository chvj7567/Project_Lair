using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.Battle
{
    public class LoadingController : MonoBehaviour
    {
        [SerializeField] private LoadingHud _hud;

        [Serializable]
        private class LoadingStringEntry { public string key; public string text; }

        async void Start()
        {
            //# 1. Addressables 카탈로그 초기화
            bool ok = await CHMResource.Instance.Init();
            if (ok == false)
            {
                Debug.LogError("[LoadingController] CHMResource.Init 실패");
                return;
            }

            //# 2. 패키지 초기화
            CHMUI.Instance.Init();
            CHMPool.Instance.Init();

            //# 3. 로딩 설명 JSON 로드
            Dictionary<string, string> descs = new Dictionary<string, string>();
            TextAsset loadingJson = await CHMResource.Instance.LoadAsync<TextAsset>(EData.LoadingStrings_Ko);
            if (loadingJson != null)
            {
                LoadingStringEntry[] entries = JsonArrayUtility.FromJsonArray<LoadingStringEntry>(loadingJson.text);
                foreach (LoadingStringEntry e in entries)
                    descs[e.key] = e.text;
            }
            else
            {
                Debug.LogWarning("[LoadingController] LoadingStrings_Ko 로드 실패 — 폴백 텍스트 사용");
            }

            descs.TryGetValue("__default", out string defaultDesc);
            if (string.IsNullOrEmpty(defaultDesc))
            {
                defaultDesc = "로딩 중...";
            }

            //# 4. 게임 문자열 JSON 로드 → CHText.StringProvider 등록
            TextAsset stringsJson = await CHMResource.Instance.LoadAsync<TextAsset>(EData.Strings_Ko);
            if (stringsJson != null)
            {
                StringTableProvider strTable = new StringTableProvider();
                strTable.Load(stringsJson);
                CHText.StringProvider = strTable;
            }
            else
            {
                Debug.LogError("[LoadingController] Strings_Ko 로드 실패 — CHText 문자열 미등록");
            }

            //# 5. Addressables 번들 워밍 + 진행률 표시
            await CHMResource.Instance.PreloadByLabelAsync(null, (ratio, key) =>
            {
                string desc = descs.TryGetValue(key, out string text) ? text : defaultDesc;
                _hud.SetProgress(ratio, desc);
            });

            //# 6. Battle 씬 자동 전환
            SceneManager.LoadScene(EScene.Battle.ToString());
        }
    }
}
