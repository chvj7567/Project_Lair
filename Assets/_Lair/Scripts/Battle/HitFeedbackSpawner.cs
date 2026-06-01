using ChvjUnityInfra;
using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 피격 피드백 스폰 계약. DamageFeedback 가 이 인터페이스로만 의존 (테스트 모킹 가능 — Rule 02 §5).
    public interface IHitFeedbackSpawner
    {
        void SpawnImpact(Vector3 pos, Color color);
        void SpawnPopup(Vector3 pos, int amount, Color color);
    }

    //# 무상태 스폰 대행. 워밍된 프리팹 핸들을 캐시해 동기 Pop (고빈도 타격에 async 로드 회피).
    public class HitFeedbackSpawner : MonoBehaviour, IHitFeedbackSpawner
    {
        public static HitFeedbackSpawner Instance { get; private set; }

        private GameObject _impactPrefab;
        private GameObject _popupPrefab;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        //# BattleController 가 워밍 후 호출 — 로드된 프리팹 핸들 주입.
        public void Init(GameObject impactPrefab, GameObject popupPrefab)
        {
            _impactPrefab = impactPrefab;
            _popupPrefab = popupPrefab;
        }

        public void SpawnImpact(Vector3 pos, Color color)
        {
            if (_impactPrefab == null)
                return;
            CHPoolable p = CHMPool.Instance.Pop(_impactPrefab, null);
            if (p == null)
                return;
            p.transform.position = pos;
            //# 자동 Push 는 프리팹의 ReturnToPoolAfter 가 담당.
            ApplyColor(p.gameObject, color);
        }

        public void SpawnPopup(Vector3 pos, int amount, Color color)
        {
            if (_popupPrefab == null)
                return;
            CHPoolable p = CHMPool.Instance.Pop(_popupPrefab, null);
            if (p == null)
                return;
            DamagePopup popup = p.GetComponent<DamagePopup>();
            if (popup == null)
            {
                CHMPool.Instance.Push(p);
                return;
            }
            popup.Play(pos, amount, color);
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            Renderer rd = go.GetComponentInChildren<Renderer>();
            if (rd == null)
                return;
            Material mat = rd.material;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            mat.color = color;
        }
    }
}
