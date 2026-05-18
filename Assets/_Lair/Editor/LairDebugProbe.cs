using Lair.Character;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# Slice A 검증용 디버그 프로브 — 런타임 객체의 상태를 콘솔로 덤프.
    //# Play 모드 중 editor_invoke_method 로 호출하여 runtime HP 등 확인.
    public static class LairDebugProbe
    {
        [MenuItem("Lair/Debug/Log UI State")]
        public static void LogUIState()
        {
            var canvas = GameObject.Find("UICanvas");
            if (canvas == null) { Debug.Log("[Probe-UI] UICanvas 없음"); return; }
            var c = canvas.GetComponent<Canvas>();
            Debug.Log($"[Probe-UI] UICanvas: enabled={canvas.activeInHierarchy}, renderMode={c?.renderMode}, sortOrder={c?.sortingOrder}, scaleFactor={c?.scaleFactor}");

            var texts = canvas.GetComponentsInChildren<Text>(true);
            Debug.Log($"[Probe-UI] Text 개수={texts.Length}");
            foreach (var t in texts)
            {
                string fontName = t.font != null ? t.font.name : "null";
                Debug.Log($"[Probe-UI]   Text '{t.name}' text='{t.text}' font={fontName} fontSize={t.fontSize} color={t.color} enabled={t.enabled} activeInHierarchy={t.gameObject.activeInHierarchy} canvasRenderer.cull={t.canvasRenderer.cull}");
            }

            var images = canvas.GetComponentsInChildren<Image>(true);
            Debug.Log($"[Probe-UI] Image 개수={images.Length}");
            foreach (var i in images)
            {
                Debug.Log($"[Probe-UI]   Image '{i.name}' enabled={i.enabled} color={i.color} fill={i.fillAmount}");
            }
        }

        [MenuItem("Lair/Debug/Kill All Heroes (Test Win Trigger)")]
        public static void KillAllHeroes()
        {
            //# 데미지 처리 중 CharacterRegistry.Heroes 가 수정될 수 있어 스냅샷
            var snapshot = CharacterRegistry.Heroes.ToArray();
            foreach (var e in snapshot)
            {
                if (e?.Health != null) e.Health.TakeDamage(int.MaxValue / 2);
            }
            Debug.Log($"[Probe] KillAllHeroes 호출 (대상 {snapshot.Length}명)");
        }

        [MenuItem("Lair/Debug/Log All Health")]
        public static void LogAllHealth()
        {
            Debug.Log($"[Probe] Heroes={CharacterRegistry.Heroes.Count}, Monsters={CharacterRegistry.Monsters.Count}");
            foreach (var e in CharacterRegistry.Heroes)
            {
                if (e?.Transform == null) continue;
                Debug.Log($"[Probe] Hero '{e.Transform.name}' — Current={e.Health.Current}/{e.Health.Max}, IsAlive={e.Health.IsAlive}, Pos={e.Transform.position}");
            }
            foreach (var e in CharacterRegistry.Monsters)
            {
                if (e?.Transform == null) continue;
                Debug.Log($"[Probe] Monster '{e.Transform.name}' — Current={e.Health.Current}/{e.Health.Max}, IsAlive={e.Health.IsAlive}, Pos={e.Transform.position}");
            }
        }
    }
}
