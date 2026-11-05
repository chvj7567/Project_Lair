#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    //# 내보낸 pixelfx 프리팹이 실제 플레이 중에 프레임을 넘기고 끝나는지 확인.
    public class PixelFxPlayModeTests
    {
        private const string PrefabPath = "Assets/_Lair/Art/FX/PixelFx/SwordSlashFx.prefab";

        [UnityTest]
        public IEnumerator SwordSlashFx_PlaysFramesAndFinishes()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "먼저 Lair > PixelFx > Build Prefabs 실행");

            GameObject go = Object.Instantiate(prefab);
            PixelFxPlayer player = go.GetComponent<PixelFxPlayer>();
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            bool finished = false;
            player.Finished += () => finished = true;

            HashSet<Sprite> seen = new HashSet<Sprite>();
            float deadline = Time.realtimeSinceStartup + 5f;
            while (finished == false && Time.realtimeSinceStartup < deadline)
            {
                seen.Add(sr.sprite);
                yield return null;
            }

            Assert.IsTrue(finished, "한 번 재생이 5초 안에 끝나야 한다");
            Assert.Greater(seen.Count, 3, "여러 프레임이 실제로 교체되어야 한다");
            Assert.IsFalse(go.activeSelf);
            Object.Destroy(go);
        }
    }
}
#endif
