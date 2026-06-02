using System.Reflection;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.UI
{
    //# 카드 리뉴얼 — BuildSynergyCell 티어 마커 표시 회귀.
    //# 정상: ActiveTier 만큼 좌→우 마커 활성 + 각 sprite = Icon, 나머지 비활성.
    //# 엣지: 풀 재사용(OnEnable) 시 모든 마커 비활성 + sprite null.
    public class BuildSynergyCellIconTests
    {
        //# private [SerializeField] _tierMarkers 주입 — 셀 캡슐화 유지하며 테스트.
        private static void InjectMarkers(BuildSynergyCell cell, Image[] markers)
        {
            FieldInfo field = typeof(BuildSynergyCell)
                .GetField("_tierMarkers", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "_tierMarkers 필드 미발견");
            field.SetValue(cell, markers);
        }

        private static Sprite MakeSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        //# 마커 3칸을 가진 셀 생성. OnEnable 은 AddComponent 시점에 한 번 불리나, 주입 전이라 _tierMarkers null 가드로 통과.
        private static (BuildSynergyCell cell, Image[] markers, GameObject root) MakeCell()
        {
            GameObject root = new GameObject("TestSynergyCell");
            BuildSynergyCell cell = root.AddComponent<BuildSynergyCell>();
            Image[] markers = new Image[3];
            for (int i = 0; i < markers.Length; ++i)
            {
                GameObject markerGo = new GameObject($"Marker{i}");
                markerGo.transform.SetParent(root.transform, false);
                markers[i] = markerGo.AddComponent<Image>();
            }
            InjectMarkers(cell, markers);
            return (cell, markers, root);
        }

        private static BuildSynergyCellData MakeData(int activeTier, Sprite icon)
        {
            return new BuildSynergyCellData
            {
                Axis       = EBuildAxis.Dps,
                Color      = Color.red,
                Label      = "DPS",
                Icon       = icon,
                Count      = activeTier * 2,
                ActiveTier = activeTier,
            };
        }

        [Test]
        public void Bind_ActiveTier0_마커_전부_비활성()
        {
            (BuildSynergyCell cell, Image[] markers, GameObject root) = MakeCell();
            Sprite sprite = MakeSprite();

            cell.Bind(MakeData(0, sprite));

            for (int i = 0; i < markers.Length; ++i)
            {
                Assert.IsFalse(markers[i].gameObject.activeSelf, $"마커 {i} 비활성");
                Assert.IsNull(markers[i].sprite, $"마커 {i} sprite null");
            }

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Bind_ActiveTier2_앞_2칸만_활성_및_스프라이트_세팅()
        {
            (BuildSynergyCell cell, Image[] markers, GameObject root) = MakeCell();
            Sprite sprite = MakeSprite();

            cell.Bind(MakeData(2, sprite));

            Assert.IsTrue(markers[0].gameObject.activeSelf, "마커 0 활성");
            Assert.IsTrue(markers[1].gameObject.activeSelf, "마커 1 활성");
            Assert.IsFalse(markers[2].gameObject.activeSelf, "마커 2 비활성");
            Assert.AreEqual(sprite, markers[0].sprite, "마커 0 sprite = Icon");
            Assert.AreEqual(sprite, markers[1].sprite, "마커 1 sprite = Icon");
            Assert.IsNull(markers[2].sprite, "비활성 마커 sprite null");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Bind_ActiveTier3_세칸_모두_활성()
        {
            (BuildSynergyCell cell, Image[] markers, GameObject root) = MakeCell();
            Sprite sprite = MakeSprite();

            cell.Bind(MakeData(3, sprite));

            for (int i = 0; i < markers.Length; ++i)
            {
                Assert.IsTrue(markers[i].gameObject.activeSelf, $"마커 {i} 활성");
                Assert.AreEqual(sprite, markers[i].sprite, $"마커 {i} sprite = Icon");
            }

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Bind_티어_하락시_상위_마커_꺼짐()
        {
            (BuildSynergyCell cell, Image[] markers, GameObject root) = MakeCell();
            Sprite sprite = MakeSprite();

            //# Tier 2 → SetItemList 재바인딩으로 Tier 1 (OnEnable 미발생) 시 마커 1 잔존 금지.
            cell.Bind(MakeData(2, sprite));
            cell.Bind(MakeData(1, sprite));

            Assert.IsTrue(markers[0].gameObject.activeSelf, "마커 0 활성 유지");
            Assert.IsFalse(markers[1].gameObject.activeSelf, "마커 1 하락으로 꺼짐");
            Assert.IsNull(markers[1].sprite, "꺼진 마커 sprite null");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void OnEnable_풀재사용_모든_마커_리셋()
        {
            (BuildSynergyCell cell, Image[] markers, GameObject root) = MakeCell();
            Sprite sprite = MakeSprite();

            cell.Bind(MakeData(3, sprite));

            //# 풀 Push→Pop 재사용 = OnEnable. EditMode 는 SetActive 로 OnEnable 이 안 불리므로 리플렉션 직접 호출.
            MethodInfo onEnable = typeof(BuildSynergyCell)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(onEnable, "OnEnable 메서드 미발견");
            onEnable.Invoke(cell, null);

            for (int i = 0; i < markers.Length; ++i)
            {
                Assert.IsFalse(markers[i].gameObject.activeSelf, $"마커 {i} 리셋 비활성");
                Assert.IsNull(markers[i].sprite, $"마커 {i} 리셋 sprite null");
            }

            Object.DestroyImmediate(root);
        }
    }
}
