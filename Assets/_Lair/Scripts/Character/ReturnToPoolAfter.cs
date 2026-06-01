using System.Collections;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Character
{
    //# OnEnable 후 _seconds 뒤 자동 CHMPool.Push. HitImpact 파티클 버스트 수명용.
    [RequireComponent(typeof(CHPoolable))]
    public class ReturnToPoolAfter : MonoBehaviour
    {
        [SerializeField] private float _seconds = 0.45f;   //# 기획서 §5 — 파티클 수명 0.35 + 여유 0.1
        private Coroutine _co;

        private void OnEnable() => _co = StartCoroutine(Co());

        private void OnDisable()
        {
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
        }

        private IEnumerator Co()
        {
            yield return new WaitForSeconds(_seconds);
            CHPoolable self = GetComponent<CHPoolable>();
            if (self != null)
                CHMPool.Instance.Push(self);
        }
    }
}
