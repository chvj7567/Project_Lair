using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// Core 매니저 데모: CHSingletonStatic / CHSingleton / CHUtil 확장 / JsonArrayUtility / CompositeDisposable.
    /// 빈 GameObject에 붙이고 Play.
    /// 외부 셋업 필요 없음.
    /// </summary>
    public class CoreDemo : MonoBehaviour
    {
        // 1. 정적 싱글톤 — 일반 클래스 (MonoBehaviour 아님)
        public class GameState : CHSingletonStatic<GameState>
        {
            public int playerLevel = 1;
            public string playerName = "Player";
        }

        // 2. MonoBehaviour 싱글톤 — Instance 접근 시 GameObject 자동 생성
        public class TimeKeeper : CHSingleton<TimeKeeper>
        {
            public float elapsed;
            private void Update() => elapsed += Time.deltaTime;
        }

        private void Start()
        {
            // ─── 1. 정적 싱글톤 ───
            GameState.Instance.playerLevel = 10;
            Debug.Log($"[CoreDemo] {GameState.Instance.playerName} Lv.{GameState.Instance.playerLevel}");

            // ─── 2. MonoBehaviour 싱글톤 ───
            // Instance 접근만으로 GameObject가 자동 생성됨 — Hierarchy 확인하면 "TimeKeeper" GO 생성됨
            float t = TimeKeeper.Instance.elapsed;
            Debug.Log($"[CoreDemo] TimeKeeper.elapsed = {t}");

            // ─── 3. CHUtil 확장 ───

            // GameObject.GetOrAddComponent<T>() — 있으면 가져오고 없으면 추가
            var rb = gameObject.GetOrAddComponent<Rigidbody>();
            Debug.Log($"[CoreDemo] Rigidbody attached: {rb != null}");

            // GameObject.FindChild<T>(name, recursive) — 자식에서 컴포넌트 찾기
            var firstChildText = gameObject.FindChild<Transform>("Anything"); // 없으면 null

            // List<T>.IsNullOrEmpty()
            List<int> nullList = null;
            List<int> emptyList = new List<int>();
            List<int> filledList = new List<int> { 1, 2, 3 };
            Debug.Log($"[CoreDemo] null={nullList.IsNullOrEmpty()} empty={emptyList.IsNullOrEmpty()} filled={filledList.IsNullOrEmpty()}");

            // ─── 4. JsonArrayUtility — JsonUtility로 최상위 배열 파싱 ───
            string json = "[{\"id\":1,\"name\":\"a\"},{\"id\":2,\"name\":\"b\"}]";
            Item[] items = JsonArrayUtility.FromJsonArray<Item>(json);
            Debug.Log($"[CoreDemo] items.Length = {items.Length}, items[0]={items[0].name}");

            // ─── 5. CompositeDisposable — IDisposable 묶음 관리 ───
            var disposables = new CompositeDisposable();
            disposables.Add(() => Debug.Log("[CoreDemo] disposable 1 disposed"));
            disposables.Add(() => Debug.Log("[CoreDemo] disposable 2 disposed"));
            disposables.Clear(); // 두 액션 모두 호출
        }

        [Serializable]
        private class Item
        {
            public int id;
            public string name;
        }
    }
}
