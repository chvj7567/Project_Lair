using System;
using UnityEngine;

namespace Lair
{
    //# pixelfx 스프라이트 시트 프레임 재생기. 프레임/길이는 PixelFxImporter 가 프리팹 빌드 때 채운다.
    public class PixelFxPlayer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float[] _durations; //# 프레임별 초
        [SerializeField] private bool _loop;

        private float _time;
        private bool _playing;

        public event Action Finished;

        public bool IsPlaying => _playing;

        private void OnEnable() => Play();

        public void Play()
        {
            _time = 0f;
            _playing = true;
            Apply();
        }

        private void Update()
        {
            if (_playing == false)
                return;

            _time += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            int index = FrameAt(_durations, _time, _loop);
            if (index < 0)
            {
                _playing = false;
                Finished?.Invoke();
                gameObject.SetActive(false);
                return;
            }

            _renderer.sprite = _frames[index];
        }

        //# t 초 시점의 프레임 인덱스. 한 번 재생이 끝나면 -1.
        public static int FrameAt(float[] durations, float t, bool loop)
        {
            float total = 0f;
            for (int i = 0; i < durations.Length; i++)
            {
                total += durations[i];
            }

            if (total <= 0f)
                return -1;

            if (loop)
            {
                t %= total;
            }
            else if (t >= total)
            {
                return -1;
            }

            for (int i = 0; i < durations.Length; i++)
            {
                if (t < durations[i])
                    return i;

                t -= durations[i];
            }

            return durations.Length - 1;
        }
    }
}
