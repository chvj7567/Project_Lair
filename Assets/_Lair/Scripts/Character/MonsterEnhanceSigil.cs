using UnityEngine;

namespace Lair.Character
{
    //# 바닥 강화 문장(AuraSigil) — 2026-11-19 전 레벨 비표시로 결정. 프리팹 참조 보존을 위해 클래스만 남기고 항상 숨김.
    public class MonsterEnhanceSigil : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        }

        //# 풀 재사용·활성화 시마다 숨김(프리팹 저장 상태가 enabled 여도 안전).
        private void OnEnable()
        {
            if (_renderer != null) _renderer.enabled = false;
        }
    }
}
