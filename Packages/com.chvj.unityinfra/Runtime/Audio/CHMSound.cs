using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// 사운드 매니저. 게임의 enum 타입을 Init<TAudio>(bgmKeys...)로 주입해 AudioSource를 자동 구성.
    /// - enum 항목 이름이 "None" 또는 "Max"면 건너뜀 (보편적 sentinel 컨벤션)
    /// - Init의 bgmKeys로 명시한 키는 loop=true (BGM 채널, 단일 재생)
    /// - 나머지는 PlayOneShot (효과음, 동시 재생)
    /// - 볼륨은 PlayerPrefs로 영구 저장
    /// 외부 라이브러리 의존 없음 (CHMResource 통해 AudioClip 로드).
    /// </summary>
    public class CHMSound : CHSingleton<CHMSound>
    {
        private const string BGMVolumeKey = "CHMSound.BGMVolume";
        private const string EffectVolumeKey = "CHMSound.EffectVolume";
        private const string MasterVolumeKey = "CHMSound.MasterVolume";

        private AudioSource[] _audioSourceArr;
        private Dictionary<int, AudioClip> _audioClipDict = new Dictionary<int, AudioClip>();
        private HashSet<int> _bgmIndices = new HashSet<int>();
        private bool _initialize = false;

        /// <summary>BGM 볼륨 (0~1). PlayerPrefs에 영구 저장.</summary>
        public float BgmVolume
        {
            get => PlayerPrefs.GetFloat(BGMVolumeKey, 1f);
            private set => PlayerPrefs.SetFloat(BGMVolumeKey, value);
        }

        /// <summary>효과음 볼륨 (0~1). PlayerPrefs에 영구 저장.</summary>
        public float EffectVolume
        {
            get => PlayerPrefs.GetFloat(EffectVolumeKey, 1f);
            private set => PlayerPrefs.SetFloat(EffectVolumeKey, value);
        }

        /// <summary>
        /// 마스터 볼륨 multiplier. 실제 재생 볼륨 = (BgmVolume 또는 EffectVolume) × MasterVolume.
        /// 기본 0.5 (전체 톤다운). PlayerPrefs에 영구 저장.
        /// </summary>
        public float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 0.5f);
            private set => PlayerPrefs.SetFloat(MasterVolumeKey, value);
        }

        /// <summary>기존 이름 호환 — <see cref="MasterVolume"/>의 별칭.</summary>
        public float Ratio => MasterVolume;

        /// <summary>
        /// 게임 enum 타입의 사운드 채널 자동 구성. bgmKeys로 BGM(loop) 채널 명시.
        /// 예: Init<EAudio>() — 모두 효과음
        ///     Init<EAudio>(EAudio.MainBGM) — MainBGM만 loop
        ///     Init<EAudio>(EAudio.Stage1BGM, EAudio.Stage2BGM) — 다중 BGM
        /// </summary>
        public void Init<TAudio>(params TAudio[] bgmKeys) where TAudio : struct, Enum
        {
            // 중복 호출은 무시 (다른 enum 타입으로 재초기화하려면 Shutdown 먼저).
            if (_initialize)
                return;

            _initialize = true;

            string[] names = Enum.GetNames(typeof(TAudio));
            Array values = Enum.GetValues(typeof(TAudio));

            int maxValue = 0;
            for (int i = 0; i < values.Length; i++)
            {
                int v = Convert.ToInt32(values.GetValue(i));
                if (v > maxValue) maxValue = v;
            }

            _audioSourceArr = new AudioSource[maxValue + 1];

            // BGM 키 인덱스 미리 수집
            if (bgmKeys != null)
            {
                foreach (var key in bgmKeys)
                {
                    _bgmIndices.Add(Convert.ToInt32(key));
                }
            }

            GameObject root = GameObject.Find("@CHMSound");
            if (root == null)
            {
                root = new GameObject("@CHMSound");
            }

            for (int i = 0; i < values.Length; i++)
            {
                int v = Convert.ToInt32(values.GetValue(i));
                string n = names[i];

                // "None"/"Max" sentinel 항목은 AudioSource 안 만듦 (보편적 enum 컨벤션)
                if (string.Equals(n, "None", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(n, "Max", StringComparison.OrdinalIgnoreCase))
                    continue;

                GameObject go = new GameObject(n);
                go.transform.parent = root.transform;
                var source = go.AddComponent<AudioSource>();
                _audioSourceArr[v] = source;

                // bgmKeys로 명시된 항목만 loop
                if (_bgmIndices.Contains(v))
                {
                    source.loop = true;
                }
            }

            DontDestroyOnLoad(root);
        }

        /// <summary>BGM 채널 볼륨 변경 + 현재 재생 중인 BGM에 즉시 반영. PlayerPrefs 저장.</summary>
        public void SetBGMVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            if (_audioSourceArr == null) return;
            foreach (int idx in _bgmIndices)
            {
                if (idx >= 0 && idx < _audioSourceArr.Length && _audioSourceArr[idx] != null)
                {
                    _audioSourceArr[idx].volume = BgmVolume * MasterVolume;
                }
            }
        }

        /// <summary>효과음 볼륨 변경. (PlayOneShot은 호출 시점 source.volume을 곱하므로 다음 재생부터 반영)</summary>
        public void SetEffectVolume(float volume)
        {
            EffectVolume = Mathf.Clamp01(volume);
            if (_audioSourceArr == null) return;
            for (int i = 0; i < _audioSourceArr.Length; i++)
            {
                if (_bgmIndices.Contains(i)) continue;
                if (_audioSourceArr[i] == null) continue;
                _audioSourceArr[i].volume = EffectVolume * MasterVolume;
            }
        }

        /// <summary>마스터 볼륨 변경 + 모든 채널에 즉시 반영. PlayerPrefs 저장.</summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            if (_audioSourceArr == null) return;
            for (int i = 0; i < _audioSourceArr.Length; i++)
            {
                if (_audioSourceArr[i] == null) continue;
                _audioSourceArr[i].volume = (_bgmIndices.Contains(i) ? BgmVolume : EffectVolume) * MasterVolume;
            }
        }

        /// <summary>특정 BGM 채널 정지. 효과음 채널엔 무효.</summary>
        public void Stop(Enum audioType)
        {
            if (_audioSourceArr == null) return;
            int v = Convert.ToInt32(audioType);
            if (v < 0 || v >= _audioSourceArr.Length || _audioSourceArr[v] == null) return;
            _audioSourceArr[v].Stop();
        }

        /// <summary>모든 BGM 채널 정지.</summary>
        public void StopAllBGM()
        {
            if (_audioSourceArr == null) return;
            foreach (int idx in _bgmIndices)
            {
                if (idx >= 0 && idx < _audioSourceArr.Length && _audioSourceArr[idx] != null)
                {
                    _audioSourceArr[idx].Stop();
                }
            }
        }

        /// <summary>모든 채널 일시정지(BGM·효과음). 게임 pause용.</summary>
        public void PauseAll()
        {
            if (_audioSourceArr == null) return;
            for (int i = 0; i < _audioSourceArr.Length; i++)
            {
                if (_audioSourceArr[i] != null) _audioSourceArr[i].Pause();
            }
        }

        /// <summary>일시정지된 채널 재개.</summary>
        public void UnPauseAll()
        {
            if (_audioSourceArr == null) return;
            for (int i = 0; i < _audioSourceArr.Length; i++)
            {
                if (_audioSourceArr[i] != null) _audioSourceArr[i].UnPause();
            }
        }

        /// <summary>해당 채널이 현재 재생 중인지. 채널 없으면 false.</summary>
        public bool IsPlaying(Enum audioType)
        {
            if (_audioSourceArr == null) return false;
            int v = Convert.ToInt32(audioType);
            if (v < 0 || v >= _audioSourceArr.Length || _audioSourceArr[v] == null) return false;
            return _audioSourceArr[v].isPlaying;
        }

        public async void Play(Enum audioType, float pitch = 1.0f)
        {
            try
            {
                if (_audioSourceArr == null)
                {
                    Debug.LogWarning("[CHMSound] Init<TAudio>()가 호출되지 않았습니다.");
                    return;
                }

                int v = Convert.ToInt32(audioType);
                if (v < 0 || v >= _audioSourceArr.Length || _audioSourceArr[v] == null)
                {
                    Debug.LogWarning($"[CHMSound] AudioSource not found for: {audioType}");
                    return;
                }

                AudioClip clip = await GetOrAddAudioClip(audioType, v);
                if (clip == null) return;

                AudioSource source = _audioSourceArr[v];
                source.pitch = pitch;

                if (_bgmIndices.Contains(v))
                {
                    source.volume = BgmVolume * Ratio;
                    if (source.isPlaying) return;
                    source.clip = clip;
                    source.Play();
                }
                else
                {
                    source.volume = EffectVolume * Ratio;
                    source.PlayOneShot(clip);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CHMSound] Play({audioType}) 예외: {ex}");
            }
        }

        private async Task<AudioClip> GetOrAddAudioClip(Enum audioType, int v)
        {
            if (_audioClipDict.TryGetValue(v, out var clip))
            {
                return clip;
            }

            var tcs = new TaskCompletionSource<AudioClip>();
            CHMResource.Instance.Load<AudioClip>(audioType, (loaded) =>
            {
                tcs.SetResult(loaded);
            });

            clip = await tcs.Task;
            if (clip == null)
            {
                return null;
            }

            _audioClipDict[v] = clip;
            return clip;
        }
    }
}
