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

        //# B1-M4 — 패시브 트리거 강제 발동용. 영웅에 100 데미지 주입.
        [MenuItem("Lair/Debug/Damage Hero 100")]
        public static void DamageHero100()
        {
            var snapshot = CharacterRegistry.Heroes.ToArray();
            foreach (var e in snapshot)
                if (e?.Health != null) e.Health.TakeDamage(100);
            Debug.Log("[Probe] Hero 에 100 데미지");
        }

        //# B1 fix 검증 — ReplaceSlimesToGolemEffect 를 직접 호출 (랜덤 드로우 우회).
        [MenuItem("Lair/Debug/Apply Replace Effect")]
        public static void ApplyReplaceEffect()
        {
            var bc = Object.FindFirstObjectByType<Lair.Battle.BattleController>();
            if (bc == null) { Debug.LogWarning("[Probe] BattleController 없음"); return; }
            var ctx = new Lair.Battle.BattleContext(bc);
            new Lair.Card.ReplaceSlimesToGolemEffect().Apply(ctx);
            Debug.Log("[Probe] ReplaceSlimesToGolemEffect 직접 호출");
        }

        //# 시각 검증 — PoisonAura 직접 영웅에 부착.
        [MenuItem("Lair/Debug/Apply Poison Aura")]
        public static void ApplyPoisonAura()
        {
            var bc = Object.FindFirstObjectByType<Lair.Battle.BattleController>();
            if (bc == null) { Debug.LogWarning("[Probe] BattleController 없음"); return; }
            var ctx = new Lair.Battle.BattleContext(bc);
            new Lair.Card.HeroPoisonAuraEffect().Apply(ctx);
            Debug.Log("[Probe] HeroPoisonAuraEffect 직접 호출");
        }

        //# B1-M4 — CardSelectionPopup 자동 클릭. 첫 슬롯의 CHButton onClick 호출.
        [MenuItem("Lair/Debug/Auto Pick First Card")]
        public static void AutoPickFirstCard()
        {
            var popup = Object.FindFirstObjectByType<Lair.UI.CardSelectionPopup>();
            if (popup == null) { Debug.LogWarning("[Probe] CardSelectionPopup 없음"); return; }
            var slots = popup.GetComponentsInChildren<Lair.UI.CardView>(includeInactive: false);
            if (slots == null || slots.Length == 0) { Debug.LogWarning("[Probe] CardView 슬롯 없음"); return; }
            var btn = slots[0].GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn == null) { Debug.LogWarning("[Probe] Button 없음"); return; }
            btn.onClick.Invoke();
            Debug.Log("[Probe] 첫 카드 자동 클릭");
        }
    }
}
