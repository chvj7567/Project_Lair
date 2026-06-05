using System.Collections;
using System.Collections.Generic;
using Lair.UI;
using UnityEngine;

namespace Lair.Battle
{
    //# 스킬 해금 컷인 오케스트레이터(plain). 정지·쉐이크·배너를 A안 순차 구동.
    //# 실게임: Enqueue → (idle 시) _host.StartCoroutine(RunQueueCo). 테스트: DrainForTest 수동 펌핑.
    public class SkillUnlockCutsceneController
    {
        private const float ShakeDuration = 0.4f;
        private const float ShakeMagnitude = 0.3f;
        private const string Fallback = "영웅의 새 스킬 해제";

        private readonly PauseService _pause;
        private readonly ICameraShake _shake;
        private readonly ISkillUnlockBanner _banner;
        private readonly MonoBehaviour _host;   //# 코루틴 호스트(실게임). 테스트는 null.

        private readonly Queue<string> _pending = new();
        private bool _running;

        public SkillUnlockCutsceneController(PauseService pause, ICameraShake shake, ISkillUnlockBanner banner, MonoBehaviour host = null)
        {
            _pause = pause;
            _shake = shake;
            _banner = banner;
            _host = host;
        }

        //# 스킬명 누적. idle 이고 host 있으면 코루틴 구동.
        public void Enqueue(string skillName)
        {
            _pending.Enqueue(skillName);
            if (_running == false && _host != null)
                _host.StartCoroutine(RunQueueCo());
        }

        //# 라운드 리셋.
        public void Reset()
        {
            _pending.Clear();
            _running = false;
            _banner?.HideImmediate();
        }

        //# 테스트용 — host 없이 시퀀스 IEnumerator 직접 펌핑.
        public IEnumerator DrainForTest() => RunQueueCo();

        private IEnumerator RunQueueCo()
        {
            if (_running)
                yield break;
            _running = true;
            _pause?.Pause();
            //# TODO(sound): 컷인 시작 사운드 seam — 추후 CHMSound.Play(EAudio.SkillUnlock) 한 줄.
            while (_pending.Count > 0)
            {
                string name = _pending.Dequeue();
                _shake?.Shake(ShakeDuration, ShakeMagnitude);
                if (_banner != null)
                    yield return _banner.PlayCo(Format(name));
            }
            _pause?.Resume();
            _running = false;
        }

        private static string Format(string skillName)
        {
            if (string.IsNullOrEmpty(skillName))
                return Fallback;
            return $"영웅의 '{skillName}' 스킬 해제";
        }
    }
}
