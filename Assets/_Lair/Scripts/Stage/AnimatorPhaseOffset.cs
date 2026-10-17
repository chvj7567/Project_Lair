using UnityEngine;

namespace Lair.Stage
{
    //# 같은 Idle 클립을 쓰는 장식 몬스터들이 한 박자로 움직이지 않게 시작 위상(초)만큼 애니메이터를 미리 진행(scene-2d-conversion §6.2).
    public class AnimatorPhaseOffset : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _seconds;

        private void Start()
        {
            if (_animator != null && _seconds > 0f)
            {
                _animator.Update(_seconds);
            }
        }
    }
}
