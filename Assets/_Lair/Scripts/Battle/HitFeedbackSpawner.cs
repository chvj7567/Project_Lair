using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 피격 피드백 스폰 계약. DamageFeedback 가 이 인터페이스로만 의존 (테스트 모킹 가능 — Rule 02 §5).
    public interface IHitFeedbackSpawner
    {
        //# victim 종류로 임팩트 프리팹 분기 — 몬스터=MonsterHitImpact, 영웅=HitImpact.
        void SpawnImpact(Vector3 pos, Color color, HitVictimKind victim);
        void SpawnPopup(Vector3 pos, int amount, Color color);
    }

    //# 무상태 스폰 대행. 워밍된 프리팹 핸들을 캐시해 동기 Pop (고빈도 타격에 async 로드 회피).
    public class HitFeedbackSpawner : MonoBehaviour, IHitFeedbackSpawner
    {
        public static HitFeedbackSpawner Instance { get; private set; }

        private GameObject _impactPrefab;          //# 영웅 피격 임팩트 (기존 HitImpact)
        private GameObject _monsterImpactPrefab;   //# 몬스터 피격 임팩트 (MonsterHitImpact, null 이면 _impactPrefab 폴백)
        private GameObject _popupPrefab;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        //# BattleController 가 워밍 후 호출 — 로드된 프리팹 핸들 주입.
        //# monsterImpactPrefab 미주입(null) 시 SpawnImpact 가 _impactPrefab 으로 폴백.
        public void Init(GameObject impactPrefab, GameObject monsterImpactPrefab, GameObject popupPrefab)
        {
            _impactPrefab = impactPrefab;
            _monsterImpactPrefab = monsterImpactPrefab;
            _popupPrefab = popupPrefab;
        }

        public void SpawnImpact(Vector3 pos, Color color, HitVictimKind victim)
        {
            //# 몬스터 피격이면 전용 프리팹, 미주입 시 영웅용으로 폴백.
            GameObject prefab = _impactPrefab;
            bool useMonsterCfxr = victim == HitVictimKind.Monster && _monsterImpactPrefab != null;
            if (useMonsterCfxr)
                prefab = _monsterImpactPrefab;
            if (prefab == null)
                return;
            CHPoolable p = CHMPool.Instance.Pop(prefab, null);
            if (p == null)
                return;
            p.transform.position = pos;
            //# 자동 Push 는 프리팹의 ReturnToPoolAfter 가 담당.
            //# 몬스터 전용 CFXR 은 고유색을 유지하려고 틴트를 건너뛴다. 폴백(프리미티브 HitImpact)은 틴트 유지.
            if (useMonsterCfxr)
                return;
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
