using System;
using ChvjUnityInfra;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// CHMResource 데모: Addressables 기반 통합 로더.
    /// 사전 준비:
    /// 1. Window > Asset Management > Addressables > Groups
    /// 2. 에셋들을 Addressables 등록 + Label "Resource" 부여
    /// 3. enum 이름과 파일명을 일치시킴 (예: EDemoAsset.MyPrefab → MyPrefab.prefab)
    /// </summary>
    public class ResourceDemo : MonoBehaviour
    {
        // 게임 측에서 정의하는 enum (실제 자산 파일명과 매칭)
        public enum EDemoAsset
        {
            MyPrefab,
            MyAudio,
            MyText,
        }

        private async void Start()
        {
            // 1) Init — Addressables 시스템 초기화 + "Resource" 라벨 인덱싱
            await CHMResource.Instance.Init();

            // 2) enum으로 로드 — 가장 흔한 패턴
            CHMResource.Instance.Load<GameObject>(EDemoAsset.MyPrefab, prefab =>
            {
                Debug.Log($"[ResourceDemo] Loaded prefab: {prefab?.name}");
            });

            CHMResource.Instance.Load<AudioClip>(EDemoAsset.MyAudio, clip =>
            {
                Debug.Log($"[ResourceDemo] Loaded clip: {clip?.name}");
            });

            CHMResource.Instance.Load<TextAsset>(EDemoAsset.MyText, text =>
            {
                Debug.Log($"[ResourceDemo] Loaded text: {text?.text}");
            });

            // 3) string 키로 로드 (escape hatch — enum + suffix 같은 케이스)
            // 예: EFont.Arvo 키에 폰트, "ArvoMaterial" 키에 머티리얼
            CHMResource.Instance.Load<Material>("MyFontMaterial", mat =>
            {
                Debug.Log($"[ResourceDemo] Loaded material: {mat?.name}");
            });

            // 4) Instantiate — Load + GameObject.Instantiate 한 번에
            CHMResource.Instance.Instantiate<GameObject>(EDemoAsset.MyPrefab, instance =>
            {
                instance.transform.SetParent(transform);
                Debug.Log($"[ResourceDemo] Instantiated: {instance.name}");
            });

            // 키가 없으면 콘솔에 다음 경고:
            // [CHMResource] Asset key not found: SomeKey
        }
    }
}
