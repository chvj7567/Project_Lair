using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 씬 전환에도 유지되는 영속 볼륨 컨트롤러 — 런타임 라이브 조절용.
    //# DontDestroyOnLoad 싱글톤. 값 보관 + CHMSound 위임만 — 비즈니스 로직 없음.
    public class AudioVolumeSettings : MonoBehaviour
    {
        private static AudioVolumeSettings _instance;

        [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _effectVolume = 1f;

        [Header("효과음 채널별 (평타/스킬) — PlayerPrefs 미저장, 인스펙터가 소스")]
        [SerializeField, Range(0f, 1f)] private float _slash01 = 1f;
        [SerializeField, Range(0f, 1f)] private float _slash02 = 1f;
        [SerializeField, Range(0f, 1f)] private float _stab = 1f;
        [SerializeField, Range(0f, 1f)] private float _p1Skill = 1f;
        [SerializeField, Range(0f, 1f)] private float _p2Skill = 1f;
        [SerializeField, Range(0f, 1f)] private float _p3Skill = 1f;

        //# 중복 인스턴스 제거 후 단일 인스턴스를 씬 전환에도 유지.
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        //# 인스펙터 값을 CHMSound 에 적용. master 먼저 — 각 Set 이 master 곱을 즉시 반영하므로.
        public void Apply()
        {
            if (CHMSound.Instance == null)
                return;

            CHMSound.Instance.SetMasterVolume(_masterVolume);
            CHMSound.Instance.SetBGMVolume(_bgmVolume);
            CHMSound.Instance.SetEffectVolume(_effectVolume);

            //# 평타·스킬 채널별 배수 — 미대상(Hit/AcquireSkill/CardSelect)은 호출 안 함 → 1.0 유지.
            CHMSound.Instance.SetChannelVolume(EAudio.Slash01, _slash01);
            CHMSound.Instance.SetChannelVolume(EAudio.Slash02, _slash02);
            CHMSound.Instance.SetChannelVolume(EAudio.Stab, _stab);
            CHMSound.Instance.SetChannelVolume(EAudio.P1Skill, _p1Skill);
            CHMSound.Instance.SetChannelVolume(EAudio.P2Skill, _p2Skill);
            CHMSound.Instance.SetChannelVolume(EAudio.P3Skill, _p3Skill);
        }

        //# 플레이 중 슬라이더 조절 시 즉시 반영. 에디터 정지 상태는 CHMSound 미초기화라 스킵.
        private void OnValidate()
        {
            if (Application.isPlaying == false)
                return;

            Apply();
        }
    }
}
