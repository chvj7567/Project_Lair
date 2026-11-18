using System;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Play 종료 직후 에디터 메모리 정리 — 반복 Play 시 미사용 에셋·관리 힙이 쌓여 시스템 메모리가 고갈되는 것을 줄인다.
    [InitializeOnLoad]
    public static class PlayModeMemoryCleanup
    {
        static PlayModeMemoryCleanup()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;
            AsyncOperation unload = Resources.UnloadUnusedAssets();
            unload.completed += _ => CollectGarbage();
        }

        private static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
