using ChvjUnityInfra;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// CHMSound 데모: 게임 enum으로 사운드 채널 자동 구성.
    /// 사전 준비:
    /// - Addressables에 BGM/Click/Coin 등 이름의 AudioClip 등록 + Label "Resource"
    /// - 또는 다른 키 이름 쓰려면 enum과 자산명 변경
    /// </summary>
    public class AudioDemo : MonoBehaviour
    {
        // 게임별 enum
        // - "None"/"Max"는 자동 skip (보편적 sentinel 컨벤션)
        // - BGM 채널은 Init의 bgmKeys로 명시 (이름 자유: BGM/Music/MainTheme 뭐든)
        // - 명시 안 하면 모두 PlayOneShot 효과음
        public enum EDemoSound
        {
            None = 0,
            BGM,
            Click,
            Coin,
        }

        private async void Start()
        {
            // 사운드는 CHMResource를 통해 로드되므로 먼저 Init
            await CHMResource.Instance.Init();

            // CHMSound 초기화 — BGM 키 명시 (없으면 모두 효과음)
            CHMSound.Instance.Init<EDemoSound>(EDemoSound.BGM);

            // BGM 재생 (자동 loop)
            CHMSound.Instance.Play(EDemoSound.BGM);

            // 효과음 (PlayOneShot — BGM과 동시 재생 가능)
            CHMSound.Instance.Play(EDemoSound.Click);

            // 피치 변경
            CHMSound.Instance.Play(EDemoSound.Coin, pitch: 1.5f);

            // 볼륨 조절 (PlayerPrefs에 자동 영구 저장)
            CHMSound.Instance.SetBGMVolume(0.5f);
            CHMSound.Instance.SetEffectVolume(0.8f);
            Debug.Log($"[AudioDemo] BGM volume = {CHMSound.Instance.BgmVolume}, " +
                      $"Effect volume = {CHMSound.Instance.EffectVolume}");
        }
    }
}
